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
	// AppSnapshotPath is the full path to the app.toml file (global app state)
	AppSnapshotPath string
}

// NewAppDirs creates a new AppDirs instance with all paths initialized.
// Uses [os.UserConfigDir] to find the platform-appropriate config directory, then creates subdirectories for the app and workspace.
func NewAppDirs(appName, workspaceName string, logger *runtimeLogger) (*AppDirs, error) {
	userConfigDir, err := os.UserConfigDir()
	if err != nil {
		return nil, fmt.Errorf("failed to get user config directory: %w", err)
	}

	configRoot := filepath.Join(userConfigDir, appName)
	workspaceRoot := filepath.Join(configRoot, "workspaces", workspaceName)

	dirs := &AppDirs{
		ConfigRoot:      configRoot,
		WorkspaceRoot:   workspaceRoot,
		SettingsPath:    filepath.Join(configRoot, "settings.toml"),
		WorkspacePath:   filepath.Join(workspaceRoot, "workspace.toml"),
		DBPath:          filepath.Join(workspaceRoot, "graph.db"),
		AppSnapshotPath: filepath.Join(configRoot, "app.toml"),
	}

	if logger != nil {
		logger.Debugf("AppDirs initialized configRoot=%s workspaceRoot=%s", configRoot, workspaceRoot)
	}

	return dirs, nil
}

// Ensure creates all required directories for the application.
// Creates ConfigRoot and WorkspaceRoot with appropriate permissions (0755).
// Safe to call multiple times - will not fail if directories already exist.
func (a *AppDirs) Ensure(logger *runtimeLogger) error {
	var timer *Timer
	if logger != nil {
		timer = logger.StartTimer("Directories ensured")
	}

	if err := os.MkdirAll(a.ConfigRoot, 0755); err != nil {
		if timer != nil {
			timer.CompleteWithError(err, "configRoot=%s", a.ConfigRoot)
		}
		return fmt.Errorf("failed to create config directory: %w", err)
	}

	if err := os.MkdirAll(a.WorkspaceRoot, 0755); err != nil {
		if timer != nil {
			timer.CompleteWithError(err, "workspaceRoot=%s", a.WorkspaceRoot)
		}
		return fmt.Errorf("failed to create workspace directory: %w", err)
	}

	if timer != nil {
		timer.Complete("configRoot=%s workspaceRoot=%s", a.ConfigRoot, a.WorkspaceRoot)
	}

	return nil
}
