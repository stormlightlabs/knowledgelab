package service

import (
	"embed"
	"fmt"
	"path/filepath"
	"strings"

	"notes/backend/domain"

	"gopkg.in/yaml.v3"
)

//go:embed themes/*.yaml themes/*.yml
var themesFS embed.FS

// ThemeService provides access to bundled base16 themes.
// Themes are embedded in the binary and loaded on demand.
type ThemeService struct {
	themes map[string]*domain.Base16Theme // Cache of loaded themes by slug
}

// NewThemeService creates a new theme service instance.
func NewThemeService() *ThemeService {
	return &ThemeService{
		themes: make(map[string]*domain.Base16Theme),
	}
}

// ListThemes returns a list of all available theme slugs.
// Themes are identified by their filename without extension (e.g., "gruvbox-dark").
func (s *ThemeService) ListThemes() ([]string, error) {
	entries, err := themesFS.ReadDir("themes")
	if err != nil {
		return nil, fmt.Errorf("failed to read themes directory: %w", err)
	}

	var themes []string
	for _, entry := range entries {
		if entry.IsDir() {
			continue
		}

		name := entry.Name()
		ext := filepath.Ext(name)
		if ext == ".yaml" || ext == ".yml" {
			slug := strings.TrimSuffix(name, ext)
			themes = append(themes, slug)
		}
	}

	return themes, nil
}

// LoadTheme loads a theme by its slug (filename without extension).
// Results are cached, so subsequent calls for the same theme are fast.
func (s *ThemeService) LoadTheme(slug string) (*domain.Base16Theme, error) {
	if cached, ok := s.themes[slug]; ok {
		return cached, nil
	}

	var data []byte
	var err error

	data, err = themesFS.ReadFile(filepath.Join("themes", slug+".yaml"))
	if err != nil {
		data, err = themesFS.ReadFile(filepath.Join("themes", slug+".yml"))
		if err != nil {
			return nil, fmt.Errorf("theme '%s' not found", slug)
		}
	}

	var theme domain.Base16Theme
	if err := yaml.Unmarshal(data, &theme); err != nil {
		return nil, fmt.Errorf("failed to parse theme '%s': %w", slug, err)
	}

	if theme.Slug == "" {
		theme.Slug = slug
	}

	s.themes[slug] = &theme

	return &theme, nil
}

// GetDefaultTheme returns the default theme (iceberg).
// Falls back to the first available theme if default is not found.
func (s *ThemeService) GetDefaultTheme() (*domain.Base16Theme, error) {
	theme, err := s.LoadTheme("iceberg")
	if err == nil {
		return theme, nil
	}

	themes, err := s.ListThemes()
	if err != nil {
		return nil, fmt.Errorf("failed to list themes: %w", err)
	}

	if len(themes) == 0 {
		return nil, fmt.Errorf("no themes available")
	}

	return s.LoadTheme(themes[0])
}
