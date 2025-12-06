package service

import (
	"fmt"
	"os"

	"github.com/BurntSushi/toml"
)

// AppSnapshot represents global application state that persists across workspace changes.
// Stored in app.toml at the application config root, this tracks app-wide settings like the last opened workspace.
// This state is application-wide, not workspace-specific.
type AppSnapshot struct {
	// LastWorkspacePath stores the absolute path to the most recently opened workspace.
	// Used to auto-open the last workspace on application startup.
	LastWorkspacePath string `toml:"last_workspace_path"`
}

// DefaultAppSnapshot returns an AppSnapshot with sensible defaults.
func DefaultAppSnapshot() AppSnapshot {
	return AppSnapshot{LastWorkspacePath: ""}
}

// LoadAppSnapshot loads application-wide state from a TOML file.
// If the file doesn't exist, returns default snapshot without error.
func LoadAppSnapshot(path string) (AppSnapshot, error) {
	if _, err := os.Stat(path); os.IsNotExist(err) {
		return DefaultAppSnapshot(), nil
	}

	var snapshot AppSnapshot
	if _, err := toml.DecodeFile(path, &snapshot); err != nil {
		return AppSnapshot{}, fmt.Errorf("failed to decode app snapshot: %w", err)
	}

	return snapshot, nil
}

// SaveAppSnapshot saves application-wide state to a TOML file.
// Creates the file if it doesn't exist, overwrites if it does.
func SaveAppSnapshot(path string, snapshot AppSnapshot) error {
	f, err := os.Create(path)
	if err != nil {
		return fmt.Errorf("failed to create app snapshot file: %w", err)
	}
	defer f.Close()

	encoder := toml.NewEncoder(f)
	if err := encoder.Encode(snapshot); err != nil {
		return fmt.Errorf("failed to encode app snapshot: %w", err)
	}

	return nil
}
