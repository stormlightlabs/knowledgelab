package main

import (
	"testing"

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
