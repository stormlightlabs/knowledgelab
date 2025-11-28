package service

import (
	"fmt"
	"os"

	"github.com/BurntSushi/toml"
)

// WorkspaceSnapshot represents the UI state for a specific workspace.
// Stored in workspace.toml within the workspace directory, this tracks ephemeral UI state like active note, sidebar visibility, and pinned pages.
// This state is workspace-specific and should be saved/loaded with debouncing on the frontend to avoid excessive disk writes.
type WorkspaceSnapshot struct {
	UI WorkspaceUI `toml:"ui"`
}

// WorkspaceUI contains UI state for a workspace.
type WorkspaceUI struct {
	// ActivePage is the ID (relative path) of the currently open note
	ActivePage string `toml:"active_page"`
	// SidebarVisible indicates whether the sidebar is shown
	SidebarVisible bool `toml:"sidebar_visible"`
	// SidebarWidth specifies the sidebar width in pixels
	SidebarWidth int `toml:"sidebar_width"`
	// RightPanelVisible indicates whether the right panel (backlinks, graph) is shown
	RightPanelVisible bool `toml:"right_panel_visible"`
	// RightPanelWidth specifies the right panel width in pixels
	RightPanelWidth int `toml:"right_panel_width"`
	// PinnedPages contains IDs of pinned notes for quick access
	PinnedPages []string `toml:"pinned_pages"`
	// RecentPages contains IDs of recently opened notes (most recent first)
	RecentPages []string `toml:"recent_pages"`
	// GraphLayout stores the last graph view layout (e.g., "force", "tree")
	GraphLayout string `toml:"graph_layout"`
}

// DefaultWorkspaceSnapshot returns a WorkspaceSnapshot with sensible defaults.
func DefaultWorkspaceSnapshot() WorkspaceSnapshot {
	return WorkspaceSnapshot{
		UI: WorkspaceUI{
			ActivePage:        "",
			SidebarVisible:    true,
			SidebarWidth:      280,
			RightPanelVisible: false,
			RightPanelWidth:   300,
			PinnedPages:       []string{},
			RecentPages:       []string{},
			GraphLayout:       "force",
		},
	}
}

// LoadWorkspaceSnapshot loads workspace UI state from a TOML file.
// If the file doesn't exist, returns default snapshot without error.
// Returns an error only if the file exists but cannot be parsed.
func LoadWorkspaceSnapshot(path string) (WorkspaceSnapshot, error) {
	// If file doesn't exist, return defaults
	if _, err := os.Stat(path); os.IsNotExist(err) {
		return DefaultWorkspaceSnapshot(), nil
	}

	var snapshot WorkspaceSnapshot
	if _, err := toml.DecodeFile(path, &snapshot); err != nil {
		return WorkspaceSnapshot{}, fmt.Errorf("failed to decode workspace snapshot: %w", err)
	}

	return snapshot, nil
}

// SaveWorkspaceSnapshot saves workspace UI state to a TOML file.
// Creates the file if it doesn't exist, overwrites if it does.
//
// Debounce interval: 500-1000ms for UI state changes like active page, sidebar width, etc.
func SaveWorkspaceSnapshot(path string, snapshot WorkspaceSnapshot) error {
	f, err := os.Create(path)
	if err != nil {
		return fmt.Errorf("failed to create workspace snapshot file: %w", err)
	}
	defer f.Close()

	encoder := toml.NewEncoder(f)
	if err := encoder.Encode(snapshot); err != nil {
		return fmt.Errorf("failed to encode workspace snapshot: %w", err)
	}

	return nil
}
