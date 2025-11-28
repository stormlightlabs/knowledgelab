package service

import (
	"database/sql"
	"fmt"
	"time"

	_ "github.com/mattn/go-sqlite3"
)

// OpenGraphDB opens a SQLite database for storing graph data (pages, blocks, links).
// Enables foreign key constraints via PRAGMA for referential integrity.
// The database file is created if it doesn't exist.
func OpenGraphDB(dbPath string) (*sql.DB, error) {
	db, err := sql.Open("sqlite3", dbPath)
	if err != nil {
		return nil, fmt.Errorf("failed to open database: %w", err)
	}

	if _, err := db.Exec("PRAGMA foreign_keys = ON"); err != nil {
		db.Close()
		return nil, fmt.Errorf("failed to enable foreign keys: %w", err)
	}

	return db, nil
}

// Migrate applies database migrations to bring the schema up to date.
// Uses a simple versioning system via the schema_meta table.
// Safe to call on every application startup.
func Migrate(db *sql.DB) error {
	if err := createSchemaMeta(db); err != nil {
		return err
	}

	version, err := getCurrentVersion(db)
	if err != nil {
		return err
	}

	if version < 1 {
		if err := applyMigration1(db); err != nil {
			return fmt.Errorf("failed to apply migration 1: %w", err)
		}
	}

	return nil
}

// createSchemaMeta creates the schema_meta table if it doesn't exist.
func createSchemaMeta(db *sql.DB) error {
	query := `
		CREATE TABLE IF NOT EXISTS schema_meta (
			version INTEGER PRIMARY KEY,
			applied_at DATETIME NOT NULL
		)
	`
	_, err := db.Exec(query)
	return err
}

// getCurrentVersion returns the current schema version.
func getCurrentVersion(db *sql.DB) (int, error) {
	var version int
	err := db.QueryRow("SELECT COALESCE(MAX(version), 0) FROM schema_meta").Scan(&version)
	if err != nil {
		return 0, fmt.Errorf("failed to get schema version: %w", err)
	}
	return version, nil
}

// applyMigration1 creates the initial schema: pages, blocks, links tables.
func applyMigration1(db *sql.DB) error {
	tx, err := db.Begin()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.Exec(`
		CREATE TABLE pages (
			id TEXT PRIMARY KEY,
			title TEXT NOT NULL,
			created_at DATETIME NOT NULL,
			modified_at DATETIME NOT NULL
		)
	`); err != nil {
		return fmt.Errorf("failed to create pages table: %w", err)
	}

	if _, err := tx.Exec(`
		CREATE TABLE blocks (
			id TEXT PRIMARY KEY,
			page_id TEXT NOT NULL,
			content TEXT NOT NULL,
			position INTEGER NOT NULL,
			created_at DATETIME NOT NULL,
			FOREIGN KEY (page_id) REFERENCES pages(id) ON DELETE CASCADE
		)
	`); err != nil {
		return fmt.Errorf("failed to create blocks table: %w", err)
	}

	if _, err := tx.Exec(`
		CREATE TABLE links (
			id INTEGER PRIMARY KEY AUTOINCREMENT,
			from_page_id TEXT NOT NULL,
			to_page_id TEXT NOT NULL,
			link_text TEXT NOT NULL,
			created_at DATETIME NOT NULL,
			FOREIGN KEY (from_page_id) REFERENCES pages(id) ON DELETE CASCADE,
			FOREIGN KEY (to_page_id) REFERENCES pages(id) ON DELETE CASCADE
		)
	`); err != nil {
		return fmt.Errorf("failed to create links table: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_blocks_page_id ON blocks(page_id)`); err != nil {
		return fmt.Errorf("failed to create blocks index: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_links_to_page_id ON links(to_page_id)`); err != nil {
		return fmt.Errorf("failed to create links index: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_links_from_page_id ON links(from_page_id)`); err != nil {
		return fmt.Errorf("failed to create links from_page index: %w", err)
	}

	if _, err := tx.Exec(
		"INSERT INTO schema_meta (version, applied_at) VALUES (?, ?)",
		1,
		time.Now(),
	); err != nil {
		return fmt.Errorf("failed to record migration: %w", err)
	}

	return tx.Commit()
}

// Page represents a note/page in the graph database.
type Page struct {
	ID         string
	Title      string
	CreatedAt  time.Time
	ModifiedAt time.Time
}

// Block represents a content block within a page.
type Block struct {
	ID        string
	PageID    string
	Content   string
	Position  int
	CreatedAt time.Time
}

// Link represents a connection between two pages.
type Link struct {
	ID         int
	FromPageID string
	ToPageID   string
	LinkText   string
	CreatedAt  time.Time
}

// CreatePage inserts a new page into the database.
func CreatePage(db *sql.DB, page Page) error {
	query := `INSERT INTO pages (id, title, created_at, modified_at) VALUES (?, ?, ?, ?)`
	_, err := db.Exec(query, page.ID, page.Title, page.CreatedAt, page.ModifiedAt)
	if err != nil {
		return fmt.Errorf("failed to create page: %w", err)
	}
	return nil
}

// GetPageByID retrieves a page by its ID.
func GetPageByID(db *sql.DB, id string) (*Page, error) {
	query := `SELECT id, title, created_at, modified_at FROM pages WHERE id = ?`
	var page Page
	err := db.QueryRow(query, id).Scan(&page.ID, &page.Title, &page.CreatedAt, &page.ModifiedAt)
	if err == sql.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("failed to get page: %w", err)
	}
	return &page, nil
}

// GetBlocksForPage retrieves all blocks for a specific page, ordered by position.
func GetBlocksForPage(db *sql.DB, pageID string) ([]Block, error) {
	query := `SELECT id, page_id, content, position, created_at FROM blocks WHERE page_id = ? ORDER BY position`
	rows, err := db.Query(query, pageID)
	if err != nil {
		return nil, fmt.Errorf("failed to get blocks: %w", err)
	}
	defer rows.Close()

	var blocks []Block
	for rows.Next() {
		var block Block
		if err := rows.Scan(&block.ID, &block.PageID, &block.Content, &block.Position, &block.CreatedAt); err != nil {
			return nil, fmt.Errorf("failed to scan block: %w", err)
		}
		blocks = append(blocks, block)
	}

	return blocks, rows.Err()
}

// GetBacklinks retrieves all links pointing to a specific page.
func GetBacklinks(db *sql.DB, toPageID string) ([]Link, error) {
	query := `SELECT id, from_page_id, to_page_id, link_text, created_at FROM links WHERE to_page_id = ?`
	rows, err := db.Query(query, toPageID)
	if err != nil {
		return nil, fmt.Errorf("failed to get backlinks: %w", err)
	}
	defer rows.Close()

	var links []Link
	for rows.Next() {
		var link Link
		if err := rows.Scan(&link.ID, &link.FromPageID, &link.ToPageID, &link.LinkText, &link.CreatedAt); err != nil {
			return nil, fmt.Errorf("failed to scan link: %w", err)
		}
		links = append(links, link)
	}

	return links, rows.Err()
}

// CreateLink inserts a new link between two pages.
// Returns the auto-generated link ID.
func CreateLink(db *sql.DB, link Link) (int, error) {
	query := `INSERT INTO links (from_page_id, to_page_id, link_text, created_at) VALUES (?, ?, ?, ?)`
	result, err := db.Exec(query, link.FromPageID, link.ToPageID, link.LinkText, link.CreatedAt)
	if err != nil {
		return 0, fmt.Errorf("failed to create link: %w", err)
	}

	id, err := result.LastInsertId()
	if err != nil {
		return 0, fmt.Errorf("failed to get link ID: %w", err)
	}

	return int(id), nil
}

// DeletePage removes a page and all associated blocks and links.
func DeletePage(db *sql.DB, pageID string) error {
	query := `DELETE FROM pages WHERE id = ?`
	_, err := db.Exec(query, pageID)
	if err != nil {
		return fmt.Errorf("failed to delete page: %w", err)
	}
	return nil
}
