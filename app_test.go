package main

import (
	"testing"

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
}
