// TODO: convert migrations to embedded sql files
package service

import (
	"database/sql"
	"fmt"
	"time"

	"notes/backend/domain"

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

	if version < 2 {
		if err := applyMigration2(db); err != nil {
			return fmt.Errorf("failed to apply migration 2: %w", err)
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

// applyMigration2 creates the tasks table for task management.
func applyMigration2(db *sql.DB) error {
	tx, err := db.Begin()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.Exec(`
		CREATE TABLE tasks (
			id TEXT PRIMARY KEY,
			note_id TEXT NOT NULL,
			note_path TEXT NOT NULL,
			content TEXT NOT NULL,
			is_completed BOOLEAN NOT NULL DEFAULT 0,
			created_at DATETIME NOT NULL,
			completed_at DATETIME,
			line_number INTEGER NOT NULL
		)
	`); err != nil {
		return fmt.Errorf("failed to create tasks table: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_tasks_note_id ON tasks(note_id)`); err != nil {
		return fmt.Errorf("failed to create tasks note_id index: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_tasks_status ON tasks(is_completed)`); err != nil {
		return fmt.Errorf("failed to create tasks status index: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_tasks_created ON tasks(created_at)`); err != nil {
		return fmt.Errorf("failed to create tasks created_at index: %w", err)
	}

	if _, err := tx.Exec(`CREATE INDEX idx_tasks_completed ON tasks(completed_at)`); err != nil {
		return fmt.Errorf("failed to create tasks completed_at index: %w", err)
	}

	if _, err := tx.Exec(
		"INSERT INTO schema_meta (version, applied_at) VALUES (?, ?)",
		2,
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

// SaveTask inserts or updates a task in the database.
// Uses INSERT OR REPLACE to handle both create and update operations.
func SaveTask(db *sql.DB, task *domain.Task) error {
	query := `
		INSERT OR REPLACE INTO tasks (
			id, note_id, note_path, content, is_completed, created_at, completed_at, line_number
		) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
	`
	_, err := db.Exec(
		query,
		task.ID,
		task.NoteID,
		task.NotePath,
		task.Content,
		task.IsCompleted,
		task.CreatedAt,
		task.CompletedAt,
		task.LineNumber,
	)
	if err != nil {
		return fmt.Errorf("failed to save task: %w", err)
	}
	return nil
}

// GetTaskByID retrieves a task by its ID.
func GetTaskByID(db *sql.DB, id string) (*domain.Task, error) {
	query := `
		SELECT id, note_id, note_path, content, is_completed, created_at, completed_at, line_number
		FROM tasks WHERE id = ?
	`
	var task domain.Task
	var completedAt sql.NullTime

	err := db.QueryRow(query, id).Scan(
		&task.ID,
		&task.NoteID,
		&task.NotePath,
		&task.Content,
		&task.IsCompleted,
		&task.CreatedAt,
		&completedAt,
		&task.LineNumber,
	)

	if err == sql.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, fmt.Errorf("failed to get task: %w", err)
	}

	if completedAt.Valid {
		task.CompletedAt = &completedAt.Time
	}
	task.BlockID = task.ID

	return &task, nil
}

// GetTasksForNote retrieves all tasks for a specific note.
func GetTasksForNote(db *sql.DB, noteID string) ([]domain.Task, error) {
	query := `
		SELECT id, note_id, note_path, content, is_completed, created_at, completed_at, line_number
		FROM tasks WHERE note_id = ? ORDER BY line_number
	`
	rows, err := db.Query(query, noteID)
	if err != nil {
		return nil, fmt.Errorf("failed to get tasks for note: %w", err)
	}
	defer rows.Close()

	var tasks []domain.Task
	for rows.Next() {
		var task domain.Task
		var completedAt sql.NullTime

		if err := rows.Scan(
			&task.ID,
			&task.NoteID,
			&task.NotePath,
			&task.Content,
			&task.IsCompleted,
			&task.CreatedAt,
			&completedAt,
			&task.LineNumber,
		); err != nil {
			return nil, fmt.Errorf("failed to scan task: %w", err)
		}

		if completedAt.Valid {
			task.CompletedAt = &completedAt.Time
		}
		task.BlockID = task.ID

		tasks = append(tasks, task)
	}

	return tasks, rows.Err()
}

// DeleteTasksForNote removes all tasks associated with a note.
func DeleteTasksForNote(db *sql.DB, noteID string) error {
	query := `DELETE FROM tasks WHERE note_id = ?`
	_, err := db.Exec(query, noteID)
	if err != nil {
		return fmt.Errorf("failed to delete tasks for note: %w", err)
	}
	return nil
}
