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

	if len(note.Tags) != 2 {
		t.Errorf("Note.Tags length = %d, want 2", len(note.Tags))
	}

	if len(note.Tags) > 0 && note.Tags[0].Name != "test" {
		t.Errorf("Note.Tags[0].Name = %q, want %q", note.Tags[0].Name, "test")
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

	note, err := noteService.CreateNote("Save Test", "")
	if err != nil {
		t.Fatalf("CreateNote() error = %v", err)
	}

	note.Content = "Updated content"
	note.Frontmatter["custom"] = "value"

	err = noteService.SaveNote(note)
	if err != nil {
		t.Fatalf("SaveNote() error = %v", err)
	}

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

	note, err := noteService.CreateNote("Delete Test", "")
	if err != nil {
		t.Fatalf("CreateNote() error = %v", err)
	}

	err = noteService.DeleteNote(note.ID)
	if err != nil {
		t.Fatalf("DeleteNote() error = %v", err)
	}

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

	titles := []string{"Note 1", "Note 2", "Note 3"}
	for _, title := range titles {
		_, err := noteService.CreateNote(title, "")
		if err != nil {
			t.Fatalf("CreateNote() error = %v", err)
		}
	}

	summaries, err := noteService.ListNotes()
	if err != nil {
		t.Fatalf("ListNotes() error = %v", err)
	}

	if len(summaries) != len(titles) {
		t.Errorf("ListNotes() returned %d notes, want %d", len(summaries), len(titles))
	}

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

func TestNoteService_BlockNestingLevel(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-note-nesting")
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
		content string
		want    []struct {
			content string
			level   int
		}
	}{
		{
			name:    "flat list",
			content: "- Item 1\n- Item 2\n- Item 3",
			want: []struct {
				content string
				level   int
			}{
				{"Item 1", 1},
				{"Item 2", 1},
				{"Item 3", 1},
			},
		},
		{
			name:    "nested list",
			content: "- Top level\n  - Nested level 1\n    - Nested level 2\n- Back to top",
			want: []struct {
				content string
				level   int
			}{
				{"Top level", 1},
				{"Nested level 1", 2},
				{"Nested level 2", 3},
				{"Back to top", 1},
			},
		},
		{
			name:    "blockquote nesting",
			content: "> Quote level 1\n> > Quote level 2",
			want: []struct {
				content string
				level   int
			}{
				{"Quote level 1", 1},
				{"Quote level 2", 2},
			},
		},
		{
			name:    "mixed content with nesting",
			content: "# Heading\n\nParagraph at root\n\n- List item 1\n  - Nested item\n- List item 2",
			want: []struct {
				content string
				level   int
			}{
				{"Heading", 0},
				{"Paragraph at root", 0},
				{"List item 1", 1},
				{"Nested item", 2},
				{"List item 2", 1},
			},
		},
		{
			name:    "deeply nested list",
			content: "- L1\n  - L2\n    - L3\n      - L4",
			want: []struct {
				content string
				level   int
			}{
				{"L1", 1},
				{"L2", 2},
				{"L3", 3},
				{"L4", 4},
			},
		},
		{
			name:    "list with paragraphs",
			content: "Simple paragraph\n\n- List item\n  - Nested item\n\nAnother paragraph",
			want: []struct {
				content string
				level   int
			}{
				{"Simple paragraph", 0},
				{"List item", 1},
				{"Nested item", 2},
				{"Another paragraph", 0},
			},
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

			if len(note.Blocks) != len(tt.want) {
				t.Errorf("Got %d blocks, want %d", len(note.Blocks), len(tt.want))
				for i, block := range note.Blocks {
					t.Logf("Block %d: content=%q, level=%d, type=%s", i, block.Content, block.Level, block.Type)
				}
				return
			}

			for i, want := range tt.want {
				block := note.Blocks[i]
				if !strings.Contains(block.Content, want.content) {
					t.Errorf("Block %d: content = %q, want to contain %q", i, block.Content, want.content)
				}
				if block.Level != want.level {
					t.Errorf("Block %d (%q): level = %d, want %d", i, want.content, block.Level, want.level)
				}
			}
		})
	}
}

func TestNoteService_RenderMarkdown(t *testing.T) {
	tmpDir := filepath.Join(os.TempDir(), "test-workspace-render-md")
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
		name     string
		markdown string
		wantHTML string
	}{
		{
			name:     "simple text",
			markdown: "Hello, world!",
			wantHTML: "<p>Hello, world!</p>",
		},
		{
			name:     "heading",
			markdown: "# Heading 1",
			wantHTML: "<h1>Heading 1</h1>",
		},
		{
			name:     "bold text",
			markdown: "This is **bold** text",
			wantHTML: "<strong>bold</strong>",
		},
		{
			name:     "italic text",
			markdown: "This is _italic_ text",
			wantHTML: "<em>italic</em>",
		},
		{
			name:     "inline code",
			markdown: "This is `code` text",
			wantHTML: "<code>code</code>",
		},
		{
			name:     "link",
			markdown: "[Link text](https://example.com)",
			wantHTML: "<a href=\"https://example.com\">Link text</a>",
		},
		{
			name:     "unordered list",
			markdown: "- Item 1\n- Item 2\n- Item 3",
			wantHTML: "<ul>",
		},
		{
			name:     "ordered list",
			markdown: "1. First\n2. Second\n3. Third",
			wantHTML: "<ol>",
		},
		{
			name:     "code block",
			markdown: "```\ncode block\n```",
			wantHTML: "<pre><code>code block",
		},
		{
			name:     "multiple paragraphs",
			markdown: "First paragraph\n\nSecond paragraph",
			wantHTML: "<p>First paragraph</p>\n<p>Second paragraph</p>",
		},
		{
			name:     "empty string",
			markdown: "",
			wantHTML: "",
		},
		{
			name:     "whitespace only",
			markdown: "   \n  \n  ",
			wantHTML: "",
		},
		{
			name:     "special characters",
			markdown: "Text with <special> & \"characters\"",
			wantHTML: "&amp;",
		},
		{
			name:     "nested formatting",
			markdown: "**Bold with _italic_ inside**",
			wantHTML: "<strong>Bold with <em>italic</em> inside</strong>",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			html, err := noteService.RenderMarkdown(tt.markdown)
			if err != nil {
				t.Errorf("RenderMarkdown() error = %v", err)
				return
			}

			if !strings.Contains(html, tt.wantHTML) {
				t.Errorf("RenderMarkdown() = %q, want to contain %q", html, tt.wantHTML)
			}
		})
	}
}
