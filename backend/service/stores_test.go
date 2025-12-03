package service

import (
	"os"
	"path/filepath"
	"testing"
	"time"
)

func TestNewStores(t *testing.T) {
	tempDir := t.TempDir()

	stores, err := NewStores(filepath.Join(tempDir, "testapp"), "testworkspace", nil)
	if err != nil {
		t.Fatalf("NewStores() error = %v", err)
	}
	defer stores.Close(nil)

	if stores.Workspace == nil {
		t.Fatal("WorkspaceStore should be initialized")
	}

	if stores.Graph == nil {
		t.Fatal("GraphStore should be initialized")
	}

	dirs := stores.Workspace.GetDirs()
	if _, err := os.Stat(dirs.ConfigRoot); os.IsNotExist(err) {
		t.Error("ConfigRoot directory should exist")
	}

	if _, err := os.Stat(dirs.WorkspaceRoot); os.IsNotExist(err) {
		t.Error("WorkspaceRoot directory should exist")
	}

	if _, err := os.Stat(dirs.DBPath); os.IsNotExist(err) {
		t.Error("Database file should exist")
	}
}

func TestWorkspaceStore_Settings(t *testing.T) {
	tempDir := t.TempDir()
	stores, err := NewStores(filepath.Join(tempDir, "testapp"), "testworkspace", nil)
	if err != nil {
		t.Fatalf("NewStores() error = %v", err)
	}
	defer stores.Close(nil)

	settings, err := stores.Workspace.LoadSettings()
	if err != nil {
		t.Fatalf("LoadSettings() error = %v", err)
	}

	defaults := DefaultSettings()
	if settings.General.Theme != defaults.General.Theme {
		t.Errorf("expected default theme %q, got %q", defaults.General.Theme, settings.General.Theme)
	}

	settings.General.Theme = "dark"
	settings.Editor.VimMode = true

	err = stores.Workspace.SaveSettings(settings)
	if err != nil {
		t.Fatalf("SaveSettings() error = %v", err)
	}

	loaded, err := stores.Workspace.LoadSettings()
	if err != nil {
		t.Fatalf("LoadSettings() error = %v", err)
	}

	if loaded.General.Theme != "dark" {
		t.Errorf("expected theme 'dark', got %q", loaded.General.Theme)
	}

	if !loaded.Editor.VimMode {
		t.Error("expected vim_mode to be true")
	}
}

func TestWorkspaceStore_Snapshot(t *testing.T) {
	tempDir := t.TempDir()
	stores, err := NewStores(filepath.Join(tempDir, "testapp"), "testworkspace", nil)
	if err != nil {
		t.Fatalf("NewStores() error = %v", err)
	}
	defer stores.Close(nil)

	snapshot, err := stores.Workspace.LoadSnapshot()
	if err != nil {
		t.Fatalf("LoadSnapshot() error = %v", err)
	}

	defaults := DefaultWorkspaceSnapshot()
	if snapshot.UI.SidebarWidth != defaults.UI.SidebarWidth {
		t.Errorf("expected default sidebar_width %d, got %d", defaults.UI.SidebarWidth, snapshot.UI.SidebarWidth)
	}

	snapshot.UI.ActivePage = "my-note.md"
	snapshot.UI.SidebarWidth = 350
	snapshot.UI.PinnedPages = []string{"index.md", "todo.md"}

	err = stores.Workspace.SaveSnapshot(snapshot)
	if err != nil {
		t.Fatalf("SaveSnapshot() error = %v", err)
	}

	loaded, err := stores.Workspace.LoadSnapshot()
	if err != nil {
		t.Fatalf("LoadSnapshot() error = %v", err)
	}

	if loaded.UI.ActivePage != "my-note.md" {
		t.Errorf("expected active_page 'my-note.md', got %q", loaded.UI.ActivePage)
	}

	if loaded.UI.SidebarWidth != 350 {
		t.Errorf("expected sidebar_width 350, got %d", loaded.UI.SidebarWidth)
	}

	if len(loaded.UI.PinnedPages) != 2 {
		t.Errorf("expected 2 pinned pages, got %d", len(loaded.UI.PinnedPages))
	}
}

func TestGraphStore_Integration(t *testing.T) {
	tempDir := t.TempDir()
	stores, err := NewStores(filepath.Join(tempDir, "testapp"), "testworkspace", nil)
	if err != nil {
		t.Fatalf("NewStores() error = %v", err)
	}
	defer stores.Close(nil)

	now := time.Now()

	page := Page{
		ID:         "test-page",
		Title:      "Test Page",
		CreatedAt:  now,
		ModifiedAt: now,
	}

	err = stores.Graph.CreatePage(page)
	if err != nil {
		t.Fatalf("CreatePage() error = %v", err)
	}

	retrieved, err := stores.Graph.GetPageByID("test-page")
	if err != nil {
		t.Fatalf("GetPageByID() error = %v", err)
	}

	if retrieved == nil {
		t.Fatal("page should exist")
	}

	if retrieved.Title != "Test Page" {
		t.Errorf("expected title 'Test Page', got %q", retrieved.Title)
	}

	page2 := Page{
		ID:         "linked-page",
		Title:      "Linked Page",
		CreatedAt:  now,
		ModifiedAt: now,
	}

	err = stores.Graph.CreatePage(page2)
	if err != nil {
		t.Fatalf("CreatePage() error = %v", err)
	}

	link := Link{
		FromPageID: "test-page",
		ToPageID:   "linked-page",
		LinkText:   "Link to Linked Page",
		CreatedAt:  now,
	}

	linkID, err := stores.Graph.CreateLink(link)
	if err != nil {
		t.Fatalf("CreateLink() error = %v", err)
	}

	if linkID == 0 {
		t.Error("link ID should be non-zero")
	}

	backlinks, err := stores.Graph.GetBacklinks("linked-page")
	if err != nil {
		t.Fatalf("GetBacklinks() error = %v", err)
	}

	if len(backlinks) != 1 {
		t.Fatalf("expected 1 backlink, got %d", len(backlinks))
	}

	if backlinks[0].FromPageID != "test-page" {
		t.Errorf("expected backlink from 'test-page', got %q", backlinks[0].FromPageID)
	}

	err = stores.Graph.DeletePage("test-page")
	if err != nil {
		t.Fatalf("DeletePage() error = %v", err)
	}

	deleted, err := stores.Graph.GetPageByID("test-page")
	if err != nil {
		t.Fatalf("GetPageByID() error = %v", err)
	}

	if deleted != nil {
		t.Error("page should be deleted")
	}

	backlinks, err = stores.Graph.GetBacklinks("linked-page")
	if err != nil {
		t.Fatalf("GetBacklinks() error = %v", err)
	}

	if len(backlinks) != 0 {
		t.Error("backlinks should be cascaded on page delete")
	}
}
