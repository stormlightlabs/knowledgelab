package service

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestNoteService_CreateNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-create")
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
		name    string
		title   string
		folder  string
		wantErr bool
	}{
		{
			name:    "simple note",
			title:   "Test Note",
			folder:  "",
			wantErr: false,
		},
		{
			name:    "note in folder",
			title:   "Folder Note",
			folder:  "subfolder",
			wantErr: false,
		},
		{
			name:    "note with special chars",
			title:   "Test: Note/With*Special?Chars",
			folder:  "",
			wantErr: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			note, err := noteService.CreateNote(tt.title, tt.folder)
			if (err != nil) != tt.wantErr {
				t.Errorf("CreateNote() error = %v, wantErr %v", err, tt.wantErr)
				return
			}

			if !tt.wantErr {
				if note == nil {
					t.Error("CreateNote() returned nil note")
					return
				}

				if note.Title != tt.title {
					t.Errorf("Note.Title = %q, want %q", note.Title, tt.title)
				}

				// Verify file was created
				content, err := fs.ReadFile(note.Path)
				if err != nil {
					t.Errorf("Failed to read created note: %v", err)
				}

				if len(content) == 0 {
					t.Error("Created note has no content")
				}
			}
		})
	}
}

func TestNoteService_GetNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-get")
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

	// Create a test note
	testContent := []byte("---\ntitle: Test Note\ntags:\n  - test\n  - example\n---\n\n# Test Note\n\nThis is a test note.")
	testPath := "test.md"
	fs.WriteFile(testPath, testContent)

	note, err := noteService.GetNote(testPath)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	if note.Title != "Test Note" {
		t.Errorf("Note.Title = %q, want %q", note.Title, "Test Note")
	}

	if len(note.Frontmatter) == 0 {
		t.Error("Note.Frontmatter is empty")
	}

	if len(note.Blocks) == 0 {
		t.Error("Note.Blocks is empty, expected parsed blocks")
	}
}

func TestNoteService_SaveNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-save")
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

	// Create a note
	note, err := noteService.CreateNote("Save Test", "")
	if err != nil {
		t.Fatalf("CreateNote() error = %v", err)
	}

	// Modify note
	note.Content = "Updated content"
	note.Frontmatter["custom"] = "value"

	// Save
	err = noteService.SaveNote(note)
	if err != nil {
		t.Fatalf("SaveNote() error = %v", err)
	}

	// Read back
	loaded, err := noteService.GetNote(note.Path)
	if err != nil {
		t.Fatalf("GetNote() error = %v", err)
	}

	if !strings.Contains(loaded.Content, "Updated content") {
		t.Error("Saved content not found in loaded note")
	}

	if loaded.Frontmatter["custom"] != "value" {
		t.Error("Frontmatter custom field not preserved")
	}
}

func TestNoteService_DeleteNote(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-delete")
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

	// Create a note
	note, err := noteService.CreateNote("Delete Test", "")
	if err != nil {
		t.Fatalf("CreateNote() error = %v", err)
	}

	// Delete
	err = noteService.DeleteNote(note.ID)
	if err != nil {
		t.Fatalf("DeleteNote() error = %v", err)
	}

	// Verify deleted
	_, err = noteService.GetNote(note.ID)
	if err == nil {
		t.Error("GetNote() should fail after delete")
	}
}

func TestNoteService_ListNotes(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-list")
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

	// Create multiple notes
	titles := []string{"Note 1", "Note 2", "Note 3"}
	for _, title := range titles {
		_, err := noteService.CreateNote(title, "")
		if err != nil {
			t.Fatalf("CreateNote() error = %v", err)
		}
	}

	// List notes
	summaries, err := noteService.ListNotes()
	if err != nil {
		t.Fatalf("ListNotes() error = %v", err)
	}

	if len(summaries) != len(titles) {
		t.Errorf("ListNotes() returned %d notes, want %d", len(summaries), len(titles))
	}

	// Verify summaries have required fields
	for _, summary := range summaries {
		if summary.ID == "" {
			t.Error("Summary missing ID")
		}
		if summary.Title == "" {
			t.Error("Summary missing Title")
		}
		if summary.Path == "" {
			t.Error("Summary missing Path")
		}
	}
}

func TestNoteService_FrontmatterParsing(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-fm")
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
		name       string
		content    string
		wantTitle  string
		wantFields map[string]interface{}
	}{
		{
			name:      "with frontmatter",
			content:   "---\ntitle: FM Note\nauthor: Test\n---\n\nContent",
			wantTitle: "FM Note",
			wantFields: map[string]interface{}{
				"author": "Test",
			},
		},
		{
			name:      "without frontmatter",
			content:   "# Heading Note\n\nContent",
			wantTitle: "Heading Note",
		},
		{
			name:      "empty frontmatter",
			content:   "---\n---\n\n# Content Note",
			wantTitle: "Content Note",
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

			if note.Title != tt.wantTitle {
				t.Errorf("Note.Title = %q, want %q", note.Title, tt.wantTitle)
			}

			for key, want := range tt.wantFields {
				if got, ok := note.Frontmatter[key]; !ok || got != want {
					t.Errorf("Frontmatter[%q] = %v, want %v", key, got, want)
				}
			}
		})
	}
}
