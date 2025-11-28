package service

import (
	"fmt"
	"os"
	"path/filepath"
)

// AppDirs manages application data directories and paths.
// Provides a centralized location for all app-related paths including
// configuration, workspace metadata, and database storage.
type AppDirs struct {
	// ConfigRoot is the base directory for all app configuration (e.g., ~/.config/notes)
	ConfigRoot string
	// WorkspaceRoot is the directory containing workspace-specific data
	WorkspaceRoot string
	// SettingsPath is the full path to the settings.toml file
	SettingsPath string
	// WorkspacePath is the full path to the workspace.toml file
	WorkspacePath string
	// DBPath is the full path to the SQLite database file
	DBPath string
}

// NewAppDirs creates a new AppDirs instance with all paths initialized.
// Uses os.UserConfigDir to find the platform-appropriate config directory,
// then creates subdirectories for the app and workspace.
//
// Parameters:
//   - appName: the application name (e.g., "notes")
//   - workspaceName: the workspace identifier (e.g., "default" or workspace hash)
//
// Returns an error if the user config directory cannot be determined.
func NewAppDirs(appName, workspaceName string) (*AppDirs, error) {
	userConfigDir, err := os.UserConfigDir()
	if err != nil {
		return nil, fmt.Errorf("failed to get user config directory: %w", err)
	}

	configRoot := filepath.Join(userConfigDir, appName)
	workspaceRoot := filepath.Join(configRoot, "workspaces", workspaceName)

	return &AppDirs{
		ConfigRoot:    configRoot,
		WorkspaceRoot: workspaceRoot,
		SettingsPath:  filepath.Join(configRoot, "settings.toml"),
		WorkspacePath: filepath.Join(workspaceRoot, "workspace.toml"),
		DBPath:        filepath.Join(workspaceRoot, "graph.db"),
	}, nil
}

// Ensure creates all required directories for the application.
// Creates ConfigRoot and WorkspaceRoot with appropriate permissions (0755).
// Safe to call multiple times - will not fail if directories already exist.
//
// Returns an error if directory creation fails.
func (a *AppDirs) Ensure() error {
	if err := os.MkdirAll(a.ConfigRoot, 0755); err != nil {
		return fmt.Errorf("failed to create config directory: %w", err)
	}

	if err := os.MkdirAll(a.WorkspaceRoot, 0755); err != nil {
		return fmt.Errorf("failed to create workspace directory: %w", err)
	}

	return nil
}
