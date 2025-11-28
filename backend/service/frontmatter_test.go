package service

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestFrontmatter_Aliases(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-aliases")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	tests := []struct {
		name             string
		content          string
		wantAliases      []string
		wantAliasesCount int
	}{
		{
			name:             "aliases as array",
			content:          "---\ntitle: Test\naliases:\n  - alias1\n  - alias2\n---\n\nContent",
			wantAliases:      []string{"alias1", "alias2"},
			wantAliasesCount: 2,
		},
		{
			name:             "aliases as single string",
			content:          "---\ntitle: Test\naliases: single-alias\n---\n\nContent",
			wantAliases:      []string{"single-alias"},
			wantAliasesCount: 1,
		},
		{
			name:             "no aliases",
			content:          "---\ntitle: Test\n---\n\nContent",
			wantAliasesCount: 0,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			path := tt.name + ".md"
			fs.WriteFile(path, []byte(tt.content))

			note, err := noteService.GetNote(path)
			if err != nil {
				t.Fatalf("GetNote() error = %v", err)
			}

			if len(note.Aliases) != tt.wantAliasesCount {
				t.Errorf("Aliases length = %d, want %d", len(note.Aliases), tt.wantAliasesCount)
			}

			for i, want := range tt.wantAliases {
				if i < len(note.Aliases) && note.Aliases[i] != want {
					t.Errorf("Aliases[%d] = %q, want %q", i, note.Aliases[i], want)
				}
			}
		})
	}
}

func TestFrontmatter_Type(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-type")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	content := "---\ntitle: Meeting Notes\ntype: meeting\n---\n\nMeeting content"
	path := "meeting.md"
	fs.WriteFile(path, []byte(content))

	note, err := noteService.GetNote(path)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	if note.Type != "meeting" {
		t.Errorf("Type = %q, want %q", note.Type, "meeting")
	}
}

func TestFrontmatter_Timestamps(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-timestamps")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	createdTime := "2025-01-15T10:00:00Z"
	modifiedTime := "2025-01-20T15:30:00Z"
	content := "---\ntitle: Test\ncreated: " + createdTime + "\nmodified: " + modifiedTime + "\n---\n\nContent"
	path := "timestamps.md"
	fs.WriteFile(path, []byte(content))

	note, err := noteService.GetNote(path)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	expectedCreated, _ := time.Parse(time.RFC3339, createdTime)
	expectedModified, _ := time.Parse(time.RFC3339, modifiedTime)

	if !note.CreatedAt.Equal(expectedCreated) {
		t.Errorf("CreatedAt = %v, want %v", note.CreatedAt, expectedCreated)
	}

	if !note.ModifiedAt.Equal(expectedModified) {
		t.Errorf("ModifiedAt = %v, want %v", note.ModifiedAt, expectedModified)
	}
}

func TestFrontmatter_TimestampAutoUpdate(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-autoupdate")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	note, err := noteService.CreateNote("Auto Update Test", "")
	if err != nil {
		t.Fatalf("CreateNote() error = %v", err)
	}

	originalModified := note.ModifiedAt

	time.Sleep(10 * time.Millisecond)

	note.Content = "Updated content"
	err = noteService.SaveNote(note)
	if err != nil {
		t.Fatalf("SaveNote() error = %v", err)
	}

	if !note.ModifiedAt.After(originalModified) {
		t.Error("ModifiedAt should be updated after save")
	}
}

func TestFrontmatter_RoundTrip(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-roundtrip")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	content := `---
title: Round Trip Test
aliases:
  - rt-test
  - roundtrip
type: test-note
tags:
  - testing
  - roundtrip
custom_field: custom_value
---

# Round Trip Test

This is test content.`

	path := "roundtrip.md"
	fs.WriteFile(path, []byte(content))

	note, err := noteService.GetNote(path)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	if note.Title != "Round Trip Test" {
		t.Errorf("Title = %q, want %q", note.Title, "Round Trip Test")
	}

	if len(note.Aliases) != 2 {
		t.Errorf("Aliases length = %d, want 2", len(note.Aliases))
	}

	if note.Type != "test-note" {
		t.Errorf("Type = %q, want %q", note.Type, "test-note")
	}

	if len(note.Tags) != 2 {
		t.Errorf("Tags length = %d, want 2", len(note.Tags))
	}

	if note.Frontmatter["custom_field"] != "custom_value" {
		t.Errorf("custom_field = %v, want %q", note.Frontmatter["custom_field"], "custom_value")
	}

	err = noteService.SaveNote(note)
	if err != nil {
		t.Fatalf("SaveNote() error = %v", err)
	}

	reloaded, err := noteService.GetNote(path)
	if err != nil {
		t.Fatalf("GetNote() after save error = %v", err)
	}

	if reloaded.Title != note.Title {
		t.Errorf("Reloaded Title = %q, want %q", reloaded.Title, note.Title)
	}

	if len(reloaded.Aliases) != len(note.Aliases) {
		t.Errorf("Reloaded Aliases length = %d, want %d", len(reloaded.Aliases), len(note.Aliases))
	}

	if reloaded.Type != note.Type {
		t.Errorf("Reloaded Type = %q, want %q", reloaded.Type, note.Type)
	}

	if reloaded.Frontmatter["custom_field"] != "custom_value" {
		t.Errorf("Reloaded custom_field = %v, want %q", reloaded.Frontmatter["custom_field"], "custom_value")
	}
}

func TestFrontmatter_PreservesAdditionalFields(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-fm-additional")
	os.RemoveAll(tmpDir)
	defer os.RemoveAll(tmpDir)

	fs, err := NewFilesystemService()
	if err != nil {
		t.Fatalf("NewFilesystemService() error = %v", err)
	}
	defer fs.Close()

	_, err = fs.OpenWorkspace(tmpDir)
	if err != nil {
		t.Fatalf("OpenWorkspace() error = %v", err)
	}

	noteService := NewNoteService(fs)

	content := `---
title: Test
author: John Doe
project: ProjectX
status: draft
---

Content`

	path := "additional.md"
	fs.WriteFile(path, []byte(content))

	note, err := noteService.GetNote(path)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	if note.Frontmatter["author"] != "John Doe" {
		t.Errorf("author = %v, want %q", note.Frontmatter["author"], "John Doe")
	}

	if note.Frontmatter["project"] != "ProjectX" {
		t.Errorf("project = %v, want %q", note.Frontmatter["project"], "ProjectX")
	}

	err = noteService.SaveNote(note)
	if err != nil {
		t.Fatalf("SaveNote() error = %v", err)
	}

	raw, err := fs.ReadFile(path)
	if err != nil {
		t.Fatalf("ReadFile() error = %v", err)
	}

	content = string(raw)
	if !strings.Contains(content, "author: John Doe") {
		t.Error("author field not preserved in saved file")
	}

	if !strings.Contains(content, "project: ProjectX") {
		t.Error("project field not preserved in saved file")
	}
}
