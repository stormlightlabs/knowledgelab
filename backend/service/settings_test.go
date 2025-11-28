package service

import (
	"os"
	"path/filepath"
	"testing"
)

func TestDefaultSettings(t *testing.T) {
	settings := DefaultSettings()

	if settings.General.Theme != "auto" {
		t.Errorf("expected theme 'auto', got %q", settings.General.Theme)
	}

	if settings.General.Language != "en" {
		t.Errorf("expected language 'en', got %q", settings.General.Language)
	}

	if !settings.General.AutoSave {
		t.Error("expected auto_save to be true")
	}

	if settings.General.AutoSaveInterval != 30 {
		t.Errorf("expected auto_save_interval 30, got %d", settings.General.AutoSaveInterval)
	}

	if settings.Editor.FontFamily != "monospace" {
		t.Errorf("expected font_family 'monospace', got %q", settings.Editor.FontFamily)
	}

	if settings.Editor.FontSize != 14 {
		t.Errorf("expected font_size 14, got %d", settings.Editor.FontSize)
	}

	if settings.Editor.LineHeight != 1.6 {
		t.Errorf("expected line_height 1.6, got %f", settings.Editor.LineHeight)
	}

	if settings.Editor.TabSize != 2 {
		t.Errorf("expected tab_size 2, got %d", settings.Editor.TabSize)
	}

	if settings.Editor.VimMode {
		t.Error("expected vim_mode to be false")
	}

	if !settings.Editor.SpellCheck {
		t.Error("expected spell_check to be true")
	}
}

func TestLoadSettings_FromFixture(t *testing.T) {
	fixturePath := filepath.Join("testdata", "settings_v1.toml")

	settings, err := LoadSettings(fixturePath)
	if err != nil {
		t.Fatalf("LoadSettings() error = %v", err)
	}

	if settings.General.Theme != "dark" {
		t.Errorf("expected theme 'dark', got %q", settings.General.Theme)
	}

	if settings.General.Language != "en" {
		t.Errorf("expected language 'en', got %q", settings.General.Language)
	}

	if !settings.General.AutoSave {
		t.Error("expected auto_save to be true")
	}

	if settings.General.AutoSaveInterval != 60 {
		t.Errorf("expected auto_save_interval 60, got %d", settings.General.AutoSaveInterval)
	}

	if settings.Editor.FontFamily != "JetBrains Mono" {
		t.Errorf("expected font_family 'JetBrains Mono', got %q", settings.Editor.FontFamily)
	}

	if settings.Editor.FontSize != 16 {
		t.Errorf("expected font_size 16, got %d", settings.Editor.FontSize)
	}

	if settings.Editor.LineHeight != 1.8 {
		t.Errorf("expected line_height 1.8, got %f", settings.Editor.LineHeight)
	}

	if settings.Editor.TabSize != 4 {
		t.Errorf("expected tab_size 4, got %d", settings.Editor.TabSize)
	}

	if !settings.Editor.VimMode {
		t.Error("expected vim_mode to be true")
	}

	if settings.Editor.SpellCheck {
		t.Error("expected spell_check to be false")
	}
}

func TestLoadSettings_NonExistent(t *testing.T) {
	settings, err := LoadSettings(filepath.Join(t.TempDir(), "nonexistent.toml"))
	if err != nil {
		t.Fatalf("LoadSettings() should not error on non-existent file, got %v", err)
	}

	defaults := DefaultSettings()
	if settings.General.Theme != defaults.General.Theme {
		t.Errorf("expected default theme %q, got %q", defaults.General.Theme, settings.General.Theme)
	}
}

func TestSaveSettings_RoundTrip(t *testing.T) {
	tempDir := t.TempDir()
	settingsPath := filepath.Join(tempDir, "settings.toml")

	original := Settings{
		General: GeneralSettings{
			Theme:            "light",
			Language:         "es",
			AutoSave:         false,
			AutoSaveInterval: 120,
		},
		Editor: EditorSettings{
			FontFamily: "Fira Code",
			FontSize:   18,
			LineHeight: 2.0,
			TabSize:    8,
			VimMode:    true,
			SpellCheck: false,
		},
	}

	err := SaveSettings(settingsPath, original)
	if err != nil {
		t.Fatalf("SaveSettings() error = %v", err)
	}

	if _, err := os.Stat(settingsPath); os.IsNotExist(err) {
		t.Fatal("settings file was not created")
	}

	loaded, err := LoadSettings(settingsPath)
	if err != nil {
		t.Fatalf("LoadSettings() error = %v", err)
	}

	if loaded.General.Theme != original.General.Theme {
		t.Errorf("theme mismatch: got %q, want %q", loaded.General.Theme, original.General.Theme)
	}

	if loaded.General.Language != original.General.Language {
		t.Errorf("language mismatch: got %q, want %q", loaded.General.Language, original.General.Language)
	}

	if loaded.General.AutoSave != original.General.AutoSave {
		t.Errorf("auto_save mismatch: got %v, want %v", loaded.General.AutoSave, original.General.AutoSave)
	}

	if loaded.General.AutoSaveInterval != original.General.AutoSaveInterval {
		t.Errorf("auto_save_interval mismatch: got %d, want %d", loaded.General.AutoSaveInterval, original.General.AutoSaveInterval)
	}

	if loaded.Editor.FontFamily != original.Editor.FontFamily {
		t.Errorf("font_family mismatch: got %q, want %q", loaded.Editor.FontFamily, original.Editor.FontFamily)
	}

	if loaded.Editor.FontSize != original.Editor.FontSize {
		t.Errorf("font_size mismatch: got %d, want %d", loaded.Editor.FontSize, original.Editor.FontSize)
	}

	if loaded.Editor.LineHeight != original.Editor.LineHeight {
		t.Errorf("line_height mismatch: got %f, want %f", loaded.Editor.LineHeight, original.Editor.LineHeight)
	}

	if loaded.Editor.TabSize != original.Editor.TabSize {
		t.Errorf("tab_size mismatch: got %d, want %d", loaded.Editor.TabSize, original.Editor.TabSize)
	}

	if loaded.Editor.VimMode != original.Editor.VimMode {
		t.Errorf("vim_mode mismatch: got %v, want %v", loaded.Editor.VimMode, original.Editor.VimMode)
	}

	if loaded.Editor.SpellCheck != original.Editor.SpellCheck {
		t.Errorf("spell_check mismatch: got %v, want %v", loaded.Editor.SpellCheck, original.Editor.SpellCheck)
	}
}
