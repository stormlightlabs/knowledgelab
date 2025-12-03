package service

import (
	"fmt"
	"os"

	"github.com/BurntSushi/toml"
)

// Settings represents the application-wide settings stored in settings.toml.
// Contains general application preferences and editor configuration that persist across all workspaces.
type Settings struct {
	General GeneralSettings `toml:"general"`
	Editor  EditorSettings  `toml:"editor"`
}

// GeneralSettings contains application-wide preferences.
type GeneralSettings struct {
	// Theme specifies the UI theme (e.g., "light", "dark", "auto")
	Theme string `toml:"theme"`
	// Language specifies the UI language (e.g., "en", "es", "fr")
	Language string `toml:"language"`
	// AutoSave enables automatic saving of notes
	AutoSave bool `toml:"auto_save"`
	// AutoSaveInterval specifies the interval in seconds between auto-saves
	AutoSaveInterval int `toml:"auto_save_interval"`
	// Base16Theme specifies the selected base16 theme slug (optional)
	Base16Theme *string `toml:"base16_theme,omitempty"`
	// ColorOverrides contains custom color overrides for base16 colors
	ColorOverrides map[string]string `toml:"color_overrides,omitempty"`
}

// EditorSettings contains editor-specific preferences.
type EditorSettings struct {
	// FontFamily specifies the editor font (e.g., "monospace", "Inter")
	FontFamily string `toml:"font_family"`
	// FontSize specifies the editor font size in pixels
	FontSize int `toml:"font_size"`
	// LineHeight specifies the line height multiplier (e.g., 1.5)
	LineHeight float64 `toml:"line_height"`
	// TabSize specifies the number of spaces per tab
	TabSize int `toml:"tab_size"`
	// VimMode enables vim keybindings in the editor
	VimMode bool `toml:"vim_mode"`
	// SpellCheck enables spell checking in the editor
	SpellCheck bool `toml:"spell_check"`
}

// DefaultSettings returns a Settings instance with sensible defaults.
func DefaultSettings() Settings {
	return Settings{
		General: GeneralSettings{
			Theme:            "auto",
			Language:         "en",
			AutoSave:         true,
			AutoSaveInterval: 30,
		},
		Editor: EditorSettings{
			FontFamily: "monospace",
			FontSize:   14,
			LineHeight: 1.6,
			TabSize:    2,
			VimMode:    false,
			SpellCheck: true,
		},
	}
}

// LoadSettings loads settings from a TOML file.
// If the file doesn't exist, returns default settings without error.
// Returns an error only if the file exists but cannot be parsed.
func LoadSettings(path string) (Settings, error) {
	if _, err := os.Stat(path); os.IsNotExist(err) {
		return DefaultSettings(), nil
	}

	var settings Settings
	if _, err := toml.DecodeFile(path, &settings); err != nil {
		return Settings{}, fmt.Errorf("failed to decode settings file: %w", err)
	}

	return settings, nil
}

// SaveSettings saves settings to a TOML file.
// Creates the file if it doesn't exist, overwrites if it does.
func SaveSettings(path string, settings Settings) error {
	f, err := os.Create(path)
	if err != nil {
		return fmt.Errorf("failed to create settings file: %w", err)
	}
	defer f.Close()

	encoder := toml.NewEncoder(f)
	if err := encoder.Encode(settings); err != nil {
		return fmt.Errorf("failed to encode settings: %w", err)
	}

	return nil
}
