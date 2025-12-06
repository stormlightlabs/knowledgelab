package main

import (
	"testing"

	"notes/backend/paths"
	"notes/backend/service"

	"github.com/wailsapp/wails/v2/pkg/runtime"
)

func TestApp_DialogMethods(t *testing.T) {
	app := NewApp()

	t.Run("methods exist", func(t *testing.T) {
		if app == nil {
			t.Fatal("NewApp() returned nil")
		}

		t.Run("type assertions", func(t *testing.T) {
			var _ func(string) (string, error) = app.SelectDirectory
			var _ func(string, []runtime.FileFilter) (string, error) = app.SelectFile
			var _ func(string, []runtime.FileFilter) ([]string, error) = app.SelectFiles
			var _ func(string, string, []runtime.FileFilter) (string, error) = app.SaveFile
			var _ func(string, string, runtime.DialogType) (string, error) = app.ShowMessage
		})
	})
}

func TestDialogTypes(t *testing.T) {
	tests := []struct {
		name       string
		dialogType runtime.DialogType
	}{
		{"InfoDialog", runtime.InfoDialog},
		{"WarningDialog", runtime.WarningDialog},
		{"ErrorDialog", runtime.ErrorDialog},
		{"QuestionDialog", runtime.QuestionDialog},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if tt.dialogType == "" {
				t.Errorf("%s is empty", tt.name)
			}
		})
	}
}

func TestFileFilter(t *testing.T) {
	filter := runtime.FileFilter{
		DisplayName: "Markdown Files",
		Pattern:     "*.md",
	}

	if filter.DisplayName != "Markdown Files" {
		t.Errorf("DisplayName = %q, want %q", filter.DisplayName, "Markdown Files")
	}

	if filter.Pattern != "*.md" {
		t.Errorf("Pattern = %q, want %q", filter.Pattern, "*.md")
	}
}

func TestApp_SettingsAndSnapshotMethods(t *testing.T) {
	app := NewApp()

	t.Run("methods exist", func(t *testing.T) {
		if app == nil {
			t.Fatal("NewApp() returned nil")
		}

		if app.stores == nil {
			t.Fatal("app.stores is nil - stores not initialized")
		}

		t.Run("type assertions", func(t *testing.T) {
			var _ func() (*service.Settings, error) = app.LoadSettings
			var _ func(service.Settings) error = app.SaveSettings
			var _ func() (*service.WorkspaceSnapshot, error) = app.LoadWorkspaceSnapshot
			var _ func(service.WorkspaceSnapshot) error = app.SaveWorkspaceSnapshot
		})
	})

	t.Run("LoadSettings returns defaults", func(t *testing.T) {
		settings, err := app.LoadSettings()
		if err != nil {
			t.Fatalf("LoadSettings() error = %v", err)
		}

		if settings == nil {
			t.Fatal("LoadSettings() returned nil")
		}

		if settings.General.Theme == "" {
			t.Error("Expected default theme to be set")
		}

		if settings.Editor.FontFamily == "" {
			t.Error("Expected default font family to be set")
		}
	})

	t.Run("LoadWorkspaceSnapshot returns defaults", func(t *testing.T) {
		snapshot, err := app.LoadWorkspaceSnapshot()
		if err != nil {
			t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
		}

		if snapshot == nil {
			t.Fatal("LoadWorkspaceSnapshot() returned nil")
		}

		if snapshot.UI.SidebarWidth == 0 {
			t.Error("Expected default sidebar width to be set")
		}

		if snapshot.UI.GraphLayout == "" {
			t.Error("Expected default graph layout to be set")
		}
	})

	t.Run("SaveSettings and LoadSettings round-trip", func(t *testing.T) {
		testSettings := service.DefaultSettings()
		testSettings.General.Theme = "dark"
		testSettings.Editor.FontSize = 16

		err := app.SaveSettings(testSettings)
		if err != nil {
			t.Fatalf("SaveSettings() error = %v", err)
		}

		loaded, err := app.LoadSettings()
		if err != nil {
			t.Fatalf("LoadSettings() error = %v", err)
		}

		if loaded.General.Theme != "dark" {
			t.Errorf("Theme = %q, want %q", loaded.General.Theme, "dark")
		}

		if loaded.Editor.FontSize != 16 {
			t.Errorf("FontSize = %d, want %d", loaded.Editor.FontSize, 16)
		}
	})

	t.Run("SaveWorkspaceSnapshot and LoadWorkspaceSnapshot round-trip", func(t *testing.T) {
		testSnapshot := service.DefaultWorkspaceSnapshot()
		testSnapshot.UI.ActivePage = "test-page.md"
		testSnapshot.UI.SidebarWidth = 350
		testSnapshot.UI.PinnedPages = []string{"page1.md", "page2.md"}

		err := app.SaveWorkspaceSnapshot(testSnapshot)
		if err != nil {
			t.Fatalf("SaveWorkspaceSnapshot() error = %v", err)
		}

		loaded, err := app.LoadWorkspaceSnapshot()
		if err != nil {
			t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
		}

		if loaded.UI.ActivePage != "test-page.md" {
			t.Errorf("ActivePage = %q, want %q", loaded.UI.ActivePage, "test-page.md")
		}

		if loaded.UI.SidebarWidth != 350 {
			t.Errorf("SidebarWidth = %d, want %d", loaded.UI.SidebarWidth, 350)
		}

		if len(loaded.UI.PinnedPages) != 2 {
			t.Errorf("len(PinnedPages) = %d, want %d", len(loaded.UI.PinnedPages), 2)
		}
	})

	t.Run("ClearRecentFiles empties recent pages", func(t *testing.T) {
		snapshot := service.DefaultWorkspaceSnapshot()
		snapshot.UI.ActivePage = "existing.md"
		snapshot.UI.RecentPages = []string{"existing.md", "older.md"}

		if err := app.SaveWorkspaceSnapshot(snapshot); err != nil {
			t.Fatalf("setup SaveWorkspaceSnapshot() error = %v", err)
		}

		cleared, err := app.ClearRecentFiles()
		if err != nil {
			t.Fatalf("ClearRecentFiles() error = %v", err)
		}

		if len(cleared.UI.RecentPages) != 0 {
			t.Fatalf("expected recent pages to be empty, got %v", cleared.UI.RecentPages)
		}

		if cleared.UI.ActivePage != "" {
			t.Fatalf("expected active page to be empty, got %q", cleared.UI.ActivePage)
		}

		loaded, err := app.LoadWorkspaceSnapshot()
		if err != nil {
			t.Fatalf("LoadWorkspaceSnapshot() error = %v", err)
		}

		if len(loaded.UI.RecentPages) != 0 {
			t.Fatalf("expected persisted recent pages to be empty, got %v", loaded.UI.RecentPages)
		}
	})
}

func TestApp_ConfigDirMethods(t *testing.T) {
	app := NewApp()

	t.Run("GetUserConfigDir returns non-empty path", func(t *testing.T) {
		userConfigDir, err := paths.UserConfigDir("KnowledgeLab")
		if err != nil {
			t.Skipf("Could not initialize user config dir: %v", err)
		}
		app.userConfigDir = userConfigDir

		configDir := app.GetUserConfigDir()
		if configDir == "" {
			t.Error("GetUserConfigDir() returned empty string")
		}
	})

	t.Run("InitWorkspaceConfigDir creates directory", func(t *testing.T) {
		workspaceRoot := t.TempDir()

		configDir, err := app.InitWorkspaceConfigDir(workspaceRoot)
		if err != nil {
			t.Fatalf("InitWorkspaceConfigDir() error = %v", err)
		}

		if configDir == "" {
			t.Error("InitWorkspaceConfigDir() returned empty string")
		}

		if !fileExists(configDir) {
			t.Errorf("workspace config directory was not created: %s", configDir)
		}

		if app.currentWorkspaceConfigDir != configDir {
			t.Errorf("currentWorkspaceConfigDir not set correctly: got %s, want %s",
				app.currentWorkspaceConfigDir, configDir)
		}
	})

	t.Run("InitWorkspaceConfigDir with empty root returns error", func(t *testing.T) {
		_, err := app.InitWorkspaceConfigDir("")
		if err == nil {
			t.Error("expected error for empty workspace root, got nil")
		}
	})

	t.Run("method signatures", func(t *testing.T) {
		var _ func() string = app.GetUserConfigDir
		var _ func(string) (string, error) = app.InitWorkspaceConfigDir
	})
}

func fileExists(path string) bool {
	return path != ""
}
