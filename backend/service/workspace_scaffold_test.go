package service

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestScaffoldWorkspace(t *testing.T) {
	tempDir := t.TempDir()
	workspacePath := filepath.Join(tempDir, "test-workspace")

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("Failed to create filesystem service: %v", err)
	}
	defer fs.Close()

	notes := NewNoteService(fs)

	err = notes.ScaffoldWorkspace(workspacePath)
	if err != nil {
		t.Fatalf("ScaffoldWorkspace failed: %v", err)
	}

	if _, err := os.Stat(workspacePath); os.IsNotExist(err) {
		t.Errorf("Workspace directory was not created")
	}

	welcomePath := filepath.Join(workspacePath, "Welcome.md")
	if _, err := os.Stat(welcomePath); os.IsNotExist(err) {
		t.Errorf("Welcome.md file was not created")
	}

	content, err := os.ReadFile(welcomePath)
	if err != nil {
		t.Fatalf("Failed to read Welcome.md: %v", err)
	}

	contentStr := string(content)

	expectedSections := []string{
		"# Welcome to Knowledge Lab",
		"## Getting Started",
		"### Tags",
		"### Wikilinks & Backlinks",
		"### Tasks & TODOs",
		"#tutorial",
		"#getting-started",
		"[[My First Idea]]",
		"- [ ]",
		"- [x]",
	}

	for _, section := range expectedSections {
		if !strings.Contains(contentStr, section) {
			t.Errorf("Welcome.md missing expected section: %s", section)
		}
	}

	if strings.Contains(contentStr, "{{.CreatedAt}}") {
		t.Errorf("Welcome.md still contains template variable {{.CreatedAt}}")
	}

	if !strings.HasPrefix(contentStr, "---\ntitle: Welcome to Knowledge Lab") {
		t.Errorf("Welcome.md missing frontmatter or has incorrect format")
	}
}

func TestScaffoldWorkspace_ExistingDirectory(t *testing.T) {
	tempDir := t.TempDir()

	workspacePath := filepath.Join(tempDir, "existing-workspace")
	if err := os.MkdirAll(workspacePath, 0755); err != nil {
		t.Fatalf("Failed to create test directory: %v", err)
	}

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("Failed to create filesystem service: %v", err)
	}
	defer fs.Close()

	notes := NewNoteService(fs)

	err = notes.ScaffoldWorkspace(workspacePath)
	if err != nil {
		t.Fatalf("ScaffoldWorkspace failed on existing directory: %v", err)
	}

	welcomePath := filepath.Join(workspacePath, "Welcome.md")
	if _, err := os.Stat(welcomePath); os.IsNotExist(err) {
		t.Errorf("Welcome.md file was not created in existing directory")
	}
}

func TestScaffoldWorkspace_InvalidPath(t *testing.T) {
	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("Failed to create filesystem service: %v", err)
	}
	defer fs.Close()

	notes := NewNoteService(fs)

	invalidPath := "/dev/null/cannot-create-directory-here"
	err = notes.ScaffoldWorkspace(invalidPath)
	if err == nil {
		t.Errorf("ScaffoldWorkspace should fail with invalid path but didn't")
	}
}
