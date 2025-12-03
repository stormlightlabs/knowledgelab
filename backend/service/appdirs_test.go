package service

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestNewAppDirs(t *testing.T) {
	tests := []struct {
		name          string
		appName       string
		workspaceName string
	}{
		{
			name:          "default workspace",
			appName:       "notes",
			workspaceName: "default",
		},
		{
			name:          "workspace with hash",
			appName:       "notes",
			workspaceName: "abc123def456",
		},
		{
			name:          "different app name",
			appName:       "myapp",
			workspaceName: "workspace1",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			dirs, err := NewAppDirs(tt.appName, tt.workspaceName, nil)
			if err != nil {
				t.Fatalf("NewAppDirs() error = %v", err)
			}

			userConfigDir, err := os.UserConfigDir()
			if err != nil {
				t.Fatalf("os.UserConfigDir() error = %v", err)
			}

			expectedConfigRoot := filepath.Join(userConfigDir, tt.appName)
			if dirs.ConfigRoot != expectedConfigRoot {
				t.Errorf("ConfigRoot = %v, want %v", dirs.ConfigRoot, expectedConfigRoot)
			}

			expectedWorkspaceRoot := filepath.Join(expectedConfigRoot, "workspaces", tt.workspaceName)
			if dirs.WorkspaceRoot != expectedWorkspaceRoot {
				t.Errorf("WorkspaceRoot = %v, want %v", dirs.WorkspaceRoot, expectedWorkspaceRoot)
			}

			expectedSettingsPath := filepath.Join(expectedConfigRoot, "settings.toml")
			if dirs.SettingsPath != expectedSettingsPath {
				t.Errorf("SettingsPath = %v, want %v", dirs.SettingsPath, expectedSettingsPath)
			}

			expectedWorkspacePath := filepath.Join(expectedWorkspaceRoot, "workspace.toml")
			if dirs.WorkspacePath != expectedWorkspacePath {
				t.Errorf("WorkspacePath = %v, want %v", dirs.WorkspacePath, expectedWorkspacePath)
			}

			expectedDBPath := filepath.Join(expectedWorkspaceRoot, "graph.db")
			if dirs.DBPath != expectedDBPath {
				t.Errorf("DBPath = %v, want %v", dirs.DBPath, expectedDBPath)
			}
		})
	}
}

func TestAppDirs_Ensure(t *testing.T) {
	tempDir := t.TempDir()

	dirs := &AppDirs{
		ConfigRoot:    filepath.Join(tempDir, "notes"),
		WorkspaceRoot: filepath.Join(tempDir, "notes", "workspaces", "test"),
		SettingsPath:  filepath.Join(tempDir, "notes", "settings.toml"),
		WorkspacePath: filepath.Join(tempDir, "notes", "workspaces", "test", "workspace.toml"),
		DBPath:        filepath.Join(tempDir, "notes", "workspaces", "test", "graph.db"),
	}

	err := dirs.Ensure(nil)
	if err != nil {
		t.Fatalf("Ensure() error = %v", err)
	}

	if _, err := os.Stat(dirs.ConfigRoot); os.IsNotExist(err) {
		t.Errorf("ConfigRoot directory was not created: %v", dirs.ConfigRoot)
	}

	if _, err := os.Stat(dirs.WorkspaceRoot); os.IsNotExist(err) {
		t.Errorf("WorkspaceRoot directory was not created: %v", dirs.WorkspaceRoot)
	}

	err = dirs.Ensure(nil)
	if err != nil {
		t.Fatalf("Ensure() second call error = %v", err)
	}
}

func TestAppDirs_PathStructure(t *testing.T) {
	dirs, err := NewAppDirs("testapp", "testworkspace", nil)
	if err != nil {
		t.Fatalf("NewAppDirs() error = %v", err)
	}

	isUnder := func(child, parent string) bool {
		rel, err := filepath.Rel(parent, child)
		if err != nil {
			return false
		}

		return !filepath.IsAbs(rel) && !strings.HasPrefix(rel, "..")
	}

	if !isUnder(dirs.SettingsPath, dirs.ConfigRoot) {
		t.Errorf("SettingsPath %v is not under ConfigRoot %v", dirs.SettingsPath, dirs.ConfigRoot)
	}

	if !isUnder(dirs.WorkspaceRoot, dirs.ConfigRoot) {
		t.Errorf("WorkspaceRoot %v is not under ConfigRoot %v", dirs.WorkspaceRoot, dirs.ConfigRoot)
	}

	if !isUnder(dirs.WorkspacePath, dirs.WorkspaceRoot) {
		t.Errorf("WorkspacePath %v is not under WorkspaceRoot %v", dirs.WorkspacePath, dirs.WorkspaceRoot)
	}

	if !isUnder(dirs.DBPath, dirs.WorkspaceRoot) {
		t.Errorf("DBPath %v is not under WorkspaceRoot %v", dirs.DBPath, dirs.WorkspaceRoot)
	}
}
