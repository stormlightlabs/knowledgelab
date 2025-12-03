package service

import (
	"testing"
)

func TestThemeService_ListThemes(t *testing.T) {
	service := NewThemeService()

	themes, err := service.ListThemes()
	if err != nil {
		t.Fatalf("Failed to list themes: %v", err)
	}

	if len(themes) == 0 {
		t.Fatal("Expected at least one theme, got none")
	}

	t.Logf("Found %d themes", len(themes))

	expectedThemes := []string{
		"iceberg",
		"gruvbox-dark",
		"nord",
		"rose-pine-moon",
		"catppuccin-mocha",
	}

	themeMap := make(map[string]bool)
	for _, theme := range themes {
		themeMap[theme] = true
	}

	for _, expected := range expectedThemes {
		if !themeMap[expected] {
			t.Errorf("Expected theme '%s' not found in list", expected)
		}
	}
}

func TestThemeService_LoadTheme(t *testing.T) {
	service := NewThemeService()

	tests := []struct {
		slug    string
		wantErr bool
	}{
		{"iceberg", false},
		{"gruvbox-dark", false},
		{"nord", false},
		{"rose-pine-moon", false},
		{"catppuccin-mocha", false},
		{"rose-pine", false},
		{"nonexistent-theme", true},
	}

	for _, tt := range tests {
		t.Run(tt.slug, func(t *testing.T) {
			theme, err := service.LoadTheme(tt.slug)

			if tt.wantErr {
				if err == nil {
					t.Error("Expected error, got nil")
				}
				return
			}

			if err != nil {
				t.Fatalf("Failed to load theme: %v", err)
			}

			if theme.System == "" {
				t.Error("Theme system is empty")
			}
			if theme.Name == "" {
				t.Error("Theme name is empty")
			}
			if theme.Author == "" {
				t.Error("Theme author is empty")
			}
			if theme.Variant != "light" && theme.Variant != "dark" {
				t.Errorf("Invalid variant: %s", theme.Variant)
			}

			if theme.Slug == "" {
				t.Error("Theme slug is empty")
			}

			if theme.Palette.Base00 == "" {
				t.Error("Base00 is empty")
			}
			if theme.Palette.Base0D == "" {
				t.Error("Base0D is empty")
			}

			t.Logf("✓ Loaded theme '%s' by %s (%s)", theme.Name, theme.Author, theme.Variant)
		})
	}
}

func TestThemeService_LoadTheme_Caching(t *testing.T) {
	service := NewThemeService()

	theme1, err := service.LoadTheme("gruvbox-dark")
	if err != nil {
		t.Fatalf("Failed to load theme: %v", err)
	}

	theme2, err := service.LoadTheme("gruvbox-dark")
	if err != nil {
		t.Fatalf("Failed to load cached theme: %v", err)
	}

	if theme1 != theme2 {
		t.Error("Expected cached theme to return same pointer")
	}

	t.Log("✓ Theme caching works correctly")
}

func TestThemeService_GetDefaultTheme(t *testing.T) {
	service := NewThemeService()

	theme, err := service.GetDefaultTheme()
	if err != nil {
		t.Fatalf("Failed to get default theme: %v", err)
	}

	if theme.Name == "" {
		t.Error("Default theme has no name")
	}

	if theme.Slug != "iceberg" {
		t.Errorf("Expected default theme slug 'iceberg', got %q", theme.Slug)
	}

	t.Logf("✓ Default theme: %s", theme.Name)
}

func TestThemeYAMLStructure(t *testing.T) {
	service := NewThemeService()

	themes := []string{"iceberg", "gruvbox-dark", "nord", "rose-pine-moon"}

	for _, slug := range themes {
		t.Run(slug, func(t *testing.T) {
			theme, err := service.LoadTheme(slug)
			if err != nil {
				t.Fatalf("Failed to load theme: %v", err)
			}

			palette := theme.Palette
			colors := []string{
				palette.Base00, palette.Base01, palette.Base02, palette.Base03,
				palette.Base04, palette.Base05, palette.Base06, palette.Base07,
				palette.Base08, palette.Base09, palette.Base0A, palette.Base0B,
				palette.Base0C, palette.Base0D, palette.Base0E, palette.Base0F,
			}

			for i, color := range colors {
				if color == "" {
					t.Errorf("base%02X is empty", i)
				}

				if len(color) != 6 && len(color) != 7 {
					t.Errorf("base%02X has invalid length: %d (got %s)", i, len(color), color)
				}
			}
		})
	}
}
