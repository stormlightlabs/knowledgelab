package service

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestDefaultWorkspaceSnapshot(t *testing.T) {
	snapshot := DefaultWorkspaceSnapshot()

	if snapshot.UI.ActivePage != "" {
		t.Errorf("expected empty active_page, got %q", snapshot.UI.ActivePage)
	}

	if !snapshot.UI.SidebarVisible {
		t.Error("expected sidebar_visible to be true")
	}

	if snapshot.UI.SidebarWidth != 280 {
		t.Errorf("expected sidebar_width 280, got %d", snapshot.UI.SidebarWidth)
	}

	if snapshot.UI.RightPanelVisible {
		t.Error("expected right_panel_visible to be false")
	}

	if snapshot.UI.RightPanelWidth != 300 {
		t.Errorf("expected right_panel_width 300, got %d", snapshot.UI.RightPanelWidth)
	}

	if len(snapshot.UI.PinnedPages) != 0 {
		t.Errorf("expected empty pinned_pages, got %v", snapshot.UI.PinnedPages)
	}

	if len(snapshot.UI.RecentPages) != 0 {
		t.Errorf("expected empty recent_pages, got %v", snapshot.UI.RecentPages)
	}

	if snapshot.UI.GraphLayout != "force" {
		t.Errorf("expected graph_layout 'force', got %q", snapshot.UI.GraphLayout)
	}
}

func TestLoadWorkspaceSnapshot_FromFixture(t *testing.T) {
	fixturePath := filepath.Join("testdata", "workspace_v1.toml")

	snapshot, err := LoadWorkspaceSnapshot(fixturePath)
	if err != nil {
		t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
	}

	if snapshot.UI.ActivePage != "notes/my-note.md" {
		t.Errorf("expected active_page 'notes/my-note.md', got %q", snapshot.UI.ActivePage)
	}

	if !snapshot.UI.SidebarVisible {
		t.Error("expected sidebar_visible to be true")
	}

	if snapshot.UI.SidebarWidth != 320 {
		t.Errorf("expected sidebar_width 320, got %d", snapshot.UI.SidebarWidth)
	}

	if !snapshot.UI.RightPanelVisible {
		t.Error("expected right_panel_visible to be true")
	}

	if snapshot.UI.RightPanelWidth != 400 {
		t.Errorf("expected right_panel_width 400, got %d", snapshot.UI.RightPanelWidth)
	}

	expectedPinned := []string{"daily/2025-01-15.md", "projects/important.md"}
	if len(snapshot.UI.PinnedPages) != len(expectedPinned) {
		t.Fatalf("expected %d pinned pages, got %d", len(expectedPinned), len(snapshot.UI.PinnedPages))
	}
	for i, page := range expectedPinned {
		if snapshot.UI.PinnedPages[i] != page {
			t.Errorf("pinned_pages[%d]: expected %q, got %q", i, page, snapshot.UI.PinnedPages[i])
		}
	}

	expectedRecent := []string{"notes/my-note.md", "daily/2025-01-14.md", "ideas/brainstorm.md"}
	if len(snapshot.UI.RecentPages) != len(expectedRecent) {
		t.Fatalf("expected %d recent pages, got %d", len(expectedRecent), len(snapshot.UI.RecentPages))
	}
	for i, page := range expectedRecent {
		if snapshot.UI.RecentPages[i] != page {
			t.Errorf("recent_pages[%d]: expected %q, got %q", i, page, snapshot.UI.RecentPages[i])
		}
	}

	if snapshot.UI.GraphLayout != "tree" {
		t.Errorf("expected graph_layout 'tree', got %q", snapshot.UI.GraphLayout)
	}
}

func TestLoadWorkspaceSnapshot_NonExistent(t *testing.T) {
	snapshot, err := LoadWorkspaceSnapshot(filepath.Join(t.TempDir(), "nonexistent.toml"))
	if err != nil {
		t.Fatalf("LoadWorkspaceSnapshot() should not error on non-existent file, got %v", err)
	}

	defaults := DefaultWorkspaceSnapshot()
	if snapshot.UI.SidebarWidth != defaults.UI.SidebarWidth {
		t.Errorf("expected default sidebar_width %d, got %d", defaults.UI.SidebarWidth, snapshot.UI.SidebarWidth)
	}
}

func TestSaveWorkspaceSnapshot_RoundTrip(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "workspace.toml")

	original := WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "projects/new-idea.md",
			SidebarVisible:    false,
			SidebarWidth:      350,
			RightPanelVisible: true,
			RightPanelWidth:   450,
			PinnedPages:       []string{"index.md", "todo.md"},
			RecentPages:       []string{"projects/new-idea.md", "daily/2025-01-20.md"},
			GraphLayout:       "tree",
			NotesSortBy:       nil,
			NotesSortOrder:    nil,
		},
	}

	err := SaveWorkspaceSnapshot(snapshotPath, original)
	if err != nil {
		t.Fatalf("SaveWorkspaceSnapshot() error = %v", err)
	}

	if _, err := os.Stat(snapshotPath); os.IsNotExist(err) {
		t.Fatal("workspace snapshot file was not created")
	}

	loaded, err := LoadWorkspaceSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
	}

	if loaded.UI.ActivePage != original.UI.ActivePage {
		t.Errorf("active_page mismatch: got %q, want %q", loaded.UI.ActivePage, original.UI.ActivePage)
	}

	if loaded.UI.SidebarVisible != original.UI.SidebarVisible {
		t.Errorf("sidebar_visible mismatch: got %v, want %v", loaded.UI.SidebarVisible, original.UI.SidebarVisible)
	}

	if loaded.UI.SidebarWidth != original.UI.SidebarWidth {
		t.Errorf("sidebar_width mismatch: got %d, want %d", loaded.UI.SidebarWidth, original.UI.SidebarWidth)
	}

	if loaded.UI.RightPanelVisible != original.UI.RightPanelVisible {
		t.Errorf("right_panel_visible mismatch: got %v, want %v", loaded.UI.RightPanelVisible, original.UI.RightPanelVisible)
	}

	if loaded.UI.RightPanelWidth != original.UI.RightPanelWidth {
		t.Errorf("right_panel_width mismatch: got %d, want %d", loaded.UI.RightPanelWidth, original.UI.RightPanelWidth)
	}

	if len(loaded.UI.PinnedPages) != len(original.UI.PinnedPages) {
		t.Fatalf("pinned_pages length mismatch: got %d, want %d", len(loaded.UI.PinnedPages), len(original.UI.PinnedPages))
	}
	for i := range original.UI.PinnedPages {
		if loaded.UI.PinnedPages[i] != original.UI.PinnedPages[i] {
			t.Errorf("pinned_pages[%d] mismatch: got %q, want %q", i, loaded.UI.PinnedPages[i], original.UI.PinnedPages[i])
		}
	}

	if len(loaded.UI.RecentPages) != len(original.UI.RecentPages) {
		t.Fatalf("recent_pages length mismatch: got %d, want %d", len(loaded.UI.RecentPages), len(original.UI.RecentPages))
	}
	for i := range original.UI.RecentPages {
		if loaded.UI.RecentPages[i] != original.UI.RecentPages[i] {
			t.Errorf("recent_pages[%d] mismatch: got %q, want %q", i, loaded.UI.RecentPages[i], original.UI.RecentPages[i])
		}
	}

	if loaded.UI.GraphLayout != original.UI.GraphLayout {
		t.Errorf("graph_layout mismatch: got %q, want %q", loaded.UI.GraphLayout, original.UI.GraphLayout)
	}
}

func TestRecentPages_AddToEmpty(t *testing.T) {
	snapshot := DefaultWorkspaceSnapshot()
	if len(snapshot.UI.RecentPages) != 0 {
		t.Fatalf("expected empty recent_pages, got %d items", len(snapshot.UI.RecentPages))
	}

	snapshot.UI.RecentPages = append([]string{"notes/first.md"}, snapshot.UI.RecentPages...)

	if len(snapshot.UI.RecentPages) != 1 {
		t.Fatalf("expected 1 recent page, got %d", len(snapshot.UI.RecentPages))
	}
	if snapshot.UI.RecentPages[0] != "notes/first.md" {
		t.Errorf("expected 'notes/first.md', got %q", snapshot.UI.RecentPages[0])
	}
}

func TestRecentPages_DuplicateMovesToFront(t *testing.T) {
	snapshot := WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "",
			SidebarVisible:    true,
			SidebarWidth:      280,
			RightPanelVisible: false,
			RightPanelWidth:   300,
			PinnedPages:       []string{},
			RecentPages:       []string{"notes/a.md", "notes/b.md", "notes/c.md"},
			GraphLayout:       "force",
		},
	}

	noteToOpen := "notes/b.md"

	filtered := []string{}
	for _, page := range snapshot.UI.RecentPages {
		if page != noteToOpen {
			filtered = append(filtered, page)
		}
	}

	snapshot.UI.RecentPages = append([]string{noteToOpen}, filtered...)

	if len(snapshot.UI.RecentPages) != 3 {
		t.Fatalf("expected 3 recent pages, got %d", len(snapshot.UI.RecentPages))
	}
	if snapshot.UI.RecentPages[0] != "notes/b.md" {
		t.Errorf("expected 'notes/b.md' at front, got %q", snapshot.UI.RecentPages[0])
	}
	if snapshot.UI.RecentPages[1] != "notes/a.md" {
		t.Errorf("expected 'notes/a.md' at index 1, got %q", snapshot.UI.RecentPages[1])
	}
	if snapshot.UI.RecentPages[2] != "notes/c.md" {
		t.Errorf("expected 'notes/c.md' at index 2, got %q", snapshot.UI.RecentPages[2])
	}
}

func TestRecentPages_TruncateToMaxSize(t *testing.T) {
	const maxRecent = 20

	recentPages := []string{}
	for i := range 25 {
		recentPages = append(recentPages, fmt.Sprintf("notes/file-%d.md", i))
	}

	snapshot := WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "",
			SidebarVisible:    true,
			SidebarWidth:      280,
			RightPanelVisible: false,
			RightPanelWidth:   300,
			PinnedPages:       []string{},
			RecentPages:       recentPages,
			GraphLayout:       "force",
		},
	}

	if len(snapshot.UI.RecentPages) > maxRecent {
		snapshot.UI.RecentPages = snapshot.UI.RecentPages[:maxRecent]
	}

	if len(snapshot.UI.RecentPages) != maxRecent {
		t.Errorf("expected %d recent pages after truncation, got %d", maxRecent, len(snapshot.UI.RecentPages))
	}
}

func TestRecentPages_ActivePageUpdated(t *testing.T) {
	snapshot := WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "notes/old.md",
			SidebarVisible:    true,
			SidebarWidth:      280,
			RightPanelVisible: false,
			RightPanelWidth:   300,
			PinnedPages:       []string{},
			RecentPages:       []string{"notes/old.md"},
			GraphLayout:       "force",
		},
	}

	newPage := "notes/new.md"
	snapshot.UI.ActivePage = newPage

	filtered := []string{}
	for _, page := range snapshot.UI.RecentPages {
		if page != newPage {
			filtered = append(filtered, page)
		}
	}
	snapshot.UI.RecentPages = append([]string{newPage}, filtered...)

	if snapshot.UI.ActivePage != newPage {
		t.Errorf("expected active_page to be %q, got %q", newPage, snapshot.UI.ActivePage)
	}
	if snapshot.UI.RecentPages[0] != newPage {
		t.Errorf("expected most recent page to be %q, got %q", newPage, snapshot.UI.RecentPages[0])
	}
}

func TestRecentPages_Persistence(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "workspace.toml")

	original := WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "notes/current.md",
			SidebarVisible:    true,
			SidebarWidth:      280,
			RightPanelVisible: false,
			RightPanelWidth:   300,
			PinnedPages:       []string{},
			RecentPages:       []string{"notes/current.md", "notes/previous.md", "notes/older.md"},
			GraphLayout:       "force",
		},
	}

	err := SaveWorkspaceSnapshot(snapshotPath, original)
	if err != nil {
		t.Fatalf("SaveWorkspaceSnapshot() error = %v", err)
	}

	loaded, err := LoadWorkspaceSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
	}

	if len(loaded.UI.RecentPages) != len(original.UI.RecentPages) {
		t.Fatalf("recent_pages length mismatch: got %d, want %d", len(loaded.UI.RecentPages), len(original.UI.RecentPages))
	}

	for i, page := range original.UI.RecentPages {
		if loaded.UI.RecentPages[i] != page {
			t.Errorf("recent_pages[%d] mismatch: got %q, want %q", i, loaded.UI.RecentPages[i], page)
		}
	}
}

func TestSearchHistory_DefaultEmpty(t *testing.T) {
	snapshot := DefaultWorkspaceSnapshot()
	if len(snapshot.UI.SearchHistory) != 0 {
		t.Errorf("expected empty search_history, got %d items", len(snapshot.UI.SearchHistory))
	}
}

func TestSearchHistory_AddNewQuery(t *testing.T) {
	snapshot := DefaultWorkspaceSnapshot()

	query := "test search query"
	snapshot.UI.SearchHistory = append([]string{query}, snapshot.UI.SearchHistory...)

	if len(snapshot.UI.SearchHistory) != 1 {
		t.Fatalf("expected 1 search history item, got %d", len(snapshot.UI.SearchHistory))
	}
	if snapshot.UI.SearchHistory[0] != query {
		t.Errorf("expected %q, got %q", query, snapshot.UI.SearchHistory[0])
	}
}

func TestSearchHistory_DuplicateMovesToFront(t *testing.T) {
	snapshot := WorkspaceSnapshot{
		UI: WorkspaceUI{
			SearchHistory: []string{"query1", "query2", "query3"},
		},
	}

	queryToRepeat := "query2"

	filtered := []string{}
	for _, q := range snapshot.UI.SearchHistory {
		if q != queryToRepeat {
			filtered = append(filtered, q)
		}
	}

	snapshot.UI.SearchHistory = append([]string{queryToRepeat}, filtered...)

	if len(snapshot.UI.SearchHistory) != 3 {
		t.Fatalf("expected 3 search history items, got %d", len(snapshot.UI.SearchHistory))
	}
	if snapshot.UI.SearchHistory[0] != "query2" {
		t.Errorf("expected 'query2' at front, got %q", snapshot.UI.SearchHistory[0])
	}
	if snapshot.UI.SearchHistory[1] != "query1" {
		t.Errorf("expected 'query1' at index 1, got %q", snapshot.UI.SearchHistory[1])
	}
	if snapshot.UI.SearchHistory[2] != "query3" {
		t.Errorf("expected 'query3' at index 2, got %q", snapshot.UI.SearchHistory[2])
	}
}

func TestSearchHistory_TruncateToMaxSize(t *testing.T) {
	const maxSearchHistory = 20

	searchHistory := []string{}
	for range 25 {
		searchHistory = append(searchHistory, fmt.Sprintf("query-%d", len(searchHistory)))
	}

	snapshot := WorkspaceSnapshot{
		UI: WorkspaceUI{
			SearchHistory: searchHistory,
		},
	}

	if len(snapshot.UI.SearchHistory) > maxSearchHistory {
		snapshot.UI.SearchHistory = snapshot.UI.SearchHistory[:maxSearchHistory]
	}

	if len(snapshot.UI.SearchHistory) != maxSearchHistory {
		t.Errorf("expected %d search history items after truncation, got %d", maxSearchHistory, len(snapshot.UI.SearchHistory))
	}
}

func TestSearchHistory_Persistence(t *testing.T) {
	tempDir := t.TempDir()
	snapshotPath := filepath.Join(tempDir, "workspace.toml")

	original := WorkspaceSnapshot{
		UI: WorkspaceUI{
			SearchHistory: []string{"python programming", "golang tutorial", "react hooks"},
		},
	}

	err := SaveWorkspaceSnapshot(snapshotPath, original)
	if err != nil {
		t.Fatalf("SaveWorkspaceSnapshot() error = %v", err)
	}

	loaded, err := LoadWorkspaceSnapshot(snapshotPath)
	if err != nil {
		t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
	}

	if len(loaded.UI.SearchHistory) != len(original.UI.SearchHistory) {
		t.Fatalf("search_history length mismatch: got %d, want %d", len(loaded.UI.SearchHistory), len(original.UI.SearchHistory))
	}

	for i, query := range original.UI.SearchHistory {
		if loaded.UI.SearchHistory[i] != query {
			t.Errorf("search_history[%d] mismatch: got %q, want %q", i, loaded.UI.SearchHistory[i], query)
		}
	}
}

func TestSearchHistory_EmptyQueriesNotAdded(t *testing.T) {
	snapshot := DefaultWorkspaceSnapshot()

	emptyQueries := []string{"", "   ", "\t", "\n"}

	for _, query := range emptyQueries {
		trimmed := strings.TrimSpace(query)
		if trimmed != "" {
			snapshot.UI.SearchHistory = append([]string{trimmed}, snapshot.UI.SearchHistory...)
		}
	}

	if len(snapshot.UI.SearchHistory) != 0 {
		t.Errorf("expected empty search_history after filtering whitespace, got %d items", len(snapshot.UI.SearchHistory))
	}
}
