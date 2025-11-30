package service

import (
	"database/sql"
	"path/filepath"
	"testing"
	"time"
)

func TestOpenGraphDB(t *testing.T) {
	tempDir := t.TempDir()
	dbPath := filepath.Join(tempDir, "test.db")

	db, err := OpenGraphDB(dbPath)
	if err != nil {
		t.Fatalf("OpenGraphDB() error = %v", err)
	}
	defer db.Close()

	var fkEnabled int
	err = db.QueryRow("PRAGMA foreign_keys").Scan(&fkEnabled)
	if err != nil {
		t.Fatalf("failed to check foreign keys: %v", err)
	}

	if fkEnabled != 1 {
		t.Error("foreign keys should be enabled")
	}
}

func TestMigrate(t *testing.T) {
	tempDir := t.TempDir()
	dbPath := filepath.Join(tempDir, "test.db")

	db, err := OpenGraphDB(dbPath)
	if err != nil {
		t.Fatalf("OpenGraphDB() error = %v", err)
	}
	defer db.Close()

	err = Migrate(db)
	if err != nil {
		t.Fatalf("Migrate() error = %v", err)
	}

	var count int
	err = db.QueryRow("SELECT COUNT(*) FROM schema_meta").Scan(&count)
	if err != nil {
		t.Fatalf("schema_meta table should exist: %v", err)
	}

	var version int
	err = db.QueryRow("SELECT MAX(version) FROM schema_meta").Scan(&version)
	if err != nil {
		t.Fatalf("failed to get version: %v", err)
	}

	if version != 2 {
		t.Errorf("expected version 2, got %d", version)
	}

	tables := []string{"pages", "blocks", "links", "tasks"}
	for _, table := range tables {
		var exists int
		query := "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?"
		err = db.QueryRow(query, table).Scan(&exists)
		if err != nil || exists != 1 {
			t.Errorf("table %s should exist", table)
		}
	}

	indexes := []string{"idx_blocks_page_id", "idx_links_to_page_id", "idx_links_from_page_id", "idx_tasks_note_id", "idx_tasks_status", "idx_tasks_created", "idx_tasks_completed"}
	for _, index := range indexes {
		var exists int
		query := "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name=?"
		err = db.QueryRow(query, index).Scan(&exists)
		if err != nil || exists != 1 {
			t.Errorf("index %s should exist", index)
		}
	}

	err = Migrate(db)
	if err != nil {
		t.Fatalf("Migrate() should be idempotent, got error: %v", err)
	}

	err = db.QueryRow("SELECT MAX(version) FROM schema_meta").Scan(&version)
	if err != nil {
		t.Fatalf("failed to get version: %v", err)
	}

	if version != 2 {
		t.Errorf("expected version 2 after second migration, got %d", version)
	}
}

func TestCreatePage(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	now := time.Now()
	page := Page{
		ID:         "test-page",
		Title:      "Test Page",
		CreatedAt:  now,
		ModifiedAt: now,
	}

	err := CreatePage(db, page)
	if err != nil {
		t.Fatalf("CreatePage() error = %v", err)
	}

	retrieved, err := GetPageByID(db, "test-page")
	if err != nil {
		t.Fatalf("GetPageByID() error = %v", err)
	}

	if retrieved == nil {
		t.Fatal("page should exist")
	}

	if retrieved.ID != page.ID {
		t.Errorf("ID mismatch: got %q, want %q", retrieved.ID, page.ID)
	}

	if retrieved.Title != page.Title {
		t.Errorf("Title mismatch: got %q, want %q", retrieved.Title, page.Title)
	}
}

func TestGetPageByID_NotFound(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	page, err := GetPageByID(db, "nonexistent")
	if err != nil {
		t.Fatalf("GetPageByID() error = %v", err)
	}

	if page != nil {
		t.Error("page should be nil for nonexistent ID")
	}
}

func TestGetBlocksForPage(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	now := time.Now()
	page := Page{
		ID:         "page-with-blocks",
		Title:      "Page With Blocks",
		CreatedAt:  now,
		ModifiedAt: now,
	}
	CreatePage(db, page)

	blocks := []Block{
		{ID: "block1", PageID: "page-with-blocks", Content: "First block", Position: 1, CreatedAt: now},
		{ID: "block2", PageID: "page-with-blocks", Content: "Second block", Position: 2, CreatedAt: now},
		{ID: "block3", PageID: "page-with-blocks", Content: "Third block", Position: 3, CreatedAt: now},
	}

	for _, block := range blocks {
		_, err := db.Exec(
			"INSERT INTO blocks (id, page_id, content, position, created_at) VALUES (?, ?, ?, ?, ?)",
			block.ID, block.PageID, block.Content, block.Position, block.CreatedAt,
		)
		if err != nil {
			t.Fatalf("failed to insert block: %v", err)
		}
	}

	retrieved, err := GetBlocksForPage(db, "page-with-blocks")
	if err != nil {
		t.Fatalf("GetBlocksForPage() error = %v", err)
	}

	if len(retrieved) != 3 {
		t.Fatalf("expected 3 blocks, got %d", len(retrieved))
	}

	for i, block := range retrieved {
		expectedPosition := i + 1
		if block.Position != expectedPosition {
			t.Errorf("block %d: expected position %d, got %d", i, expectedPosition, block.Position)
		}
	}
}

func TestGetBacklinks(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	now := time.Now()

	pages := []Page{
		{ID: "target", Title: "Target Page", CreatedAt: now, ModifiedAt: now},
		{ID: "source1", Title: "Source 1", CreatedAt: now, ModifiedAt: now},
		{ID: "source2", Title: "Source 2", CreatedAt: now, ModifiedAt: now},
	}

	for _, page := range pages {
		CreatePage(db, page)
	}

	links := []Link{
		{FromPageID: "source1", ToPageID: "target", LinkText: "Link from source 1", CreatedAt: now},
		{FromPageID: "source2", ToPageID: "target", LinkText: "Link from source 2", CreatedAt: now},
	}

	for _, link := range links {
		_, err := CreateLink(db, link)
		if err != nil {
			t.Fatalf("CreateLink() error = %v", err)
		}
	}

	backlinks, err := GetBacklinks(db, "target")
	if err != nil {
		t.Fatalf("GetBacklinks() error = %v", err)
	}

	if len(backlinks) != 2 {
		t.Fatalf("expected 2 backlinks, got %d", len(backlinks))
	}

	fromPages := make(map[string]bool)
	for _, link := range backlinks {
		fromPages[link.FromPageID] = true
		if link.ToPageID != "target" {
			t.Errorf("backlink should point to target, got %q", link.ToPageID)
		}
	}

	if !fromPages["source1"] || !fromPages["source2"] {
		t.Error("backlinks should include both source1 and source2")
	}
}

func TestDeletePage_CascadeBlocks(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	now := time.Now()

	page := Page{
		ID:         "page-to-delete",
		Title:      "Page To Delete",
		CreatedAt:  now,
		ModifiedAt: now,
	}
	CreatePage(db, page)

	_, err := db.Exec(
		"INSERT INTO blocks (id, page_id, content, position, created_at) VALUES (?, ?, ?, ?, ?)",
		"block1", "page-to-delete", "Some content", 1, now,
	)
	if err != nil {
		t.Fatalf("failed to insert block: %v", err)
	}

	err = DeletePage(db, "page-to-delete")
	if err != nil {
		t.Fatalf("DeletePage() error = %v", err)
	}

	retrieved, err := GetPageByID(db, "page-to-delete")
	if err != nil {
		t.Fatalf("GetPageByID() error = %v", err)
	}

	if retrieved != nil {
		t.Error("page should be deleted")
	}

	var blockCount int
	err = db.QueryRow("SELECT COUNT(*) FROM blocks WHERE page_id = ?", "page-to-delete").Scan(&blockCount)
	if err != nil {
		t.Fatalf("failed to count blocks: %v", err)
	}

	if blockCount != 0 {
		t.Error("blocks should be cascaded on page delete")
	}
}

func TestDeletePage_CascadeLinks(t *testing.T) {
	db := setupTestDB(t)
	defer db.Close()

	now := time.Now()

	pages := []Page{
		{ID: "page1", Title: "Page 1", CreatedAt: now, ModifiedAt: now},
		{ID: "page2", Title: "Page 2", CreatedAt: now, ModifiedAt: now},
	}

	for _, page := range pages {
		CreatePage(db, page)
	}

	link := Link{
		FromPageID: "page1",
		ToPageID:   "page2",
		LinkText:   "Link to page 2",
		CreatedAt:  now,
	}
	_, err := CreateLink(db, link)
	if err != nil {
		t.Fatalf("CreateLink() error = %v", err)
	}

	err = DeletePage(db, "page1")
	if err != nil {
		t.Fatalf("DeletePage() error = %v", err)
	}

	var linkCount int
	err = db.QueryRow("SELECT COUNT(*) FROM links WHERE from_page_id = ? OR to_page_id = ?", "page1", "page1").Scan(&linkCount)
	if err != nil {
		t.Fatalf("failed to count links: %v", err)
	}

	if linkCount != 0 {
		t.Error("links should be cascaded on page delete")
	}
}

// setupTestDB creates a test database with migrations applied.
func setupTestDB(t *testing.T) *sql.DB {
	t.Helper()

	tempDir := t.TempDir()
	dbPath := filepath.Join(tempDir, "test.db")

	db, err := OpenGraphDB(dbPath)
	if err != nil {
		t.Fatalf("OpenGraphDB() error = %v", err)
	}

	err = Migrate(db)
	if err != nil {
		db.Close()
		t.Fatalf("Migrate() error = %v", err)
	}

	return db
}
