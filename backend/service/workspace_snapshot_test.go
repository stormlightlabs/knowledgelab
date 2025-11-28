package service

import (
	"os"
	"path/filepath"
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
