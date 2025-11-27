package service

import (
	"testing"
	"time"

	"notes/backend/internal/domain"
)

func TestSearchService_IndexNote(t *testing.T) {
	search := NewSearchService()

	note := &domain.Note{
		ID:      "test.md",
		Title:   "Test Note",
		Path:    "test.md",
		Content: "This is test content",
		Tags: []domain.Tag{
			{Name: "test"},
		},
		ModifiedAt: time.Now(),
	}

	err := search.IndexNote(note)
	if err != nil {
		t.Fatalf("IndexNote() error = %v", err)
	}

	// Verify note can be found
	results, err := search.Search(SearchQuery{
		Query: "test",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) == 0 {
		t.Error("Search() did not find indexed note")
	}
}

func TestSearchService_Search(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "golang.md",
			Title:      "Go Programming",
			Path:       "golang.md",
			Content:    "Go is a programming language",
			Tags:       []domain.Tag{{Name: "programming"}},
			ModifiedAt: time.Now(),
		},
		{
			ID:         "python.md",
			Title:      "Python Programming",
			Path:       "python.md",
			Content:    "Python is also a programming language",
			Tags:       []domain.Tag{{Name: "programming"}},
			ModifiedAt: time.Now(),
		},
		{
			ID:         "cooking.md",
			Title:      "Cooking",
			Path:       "cooking.md",
			Content:    "How to cook pasta",
			Tags:       []domain.Tag{{Name: "recipes"}},
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	tests := []struct {
		name      string
		query     SearchQuery
		wantCount int
		wantFirst string // ID of first result (if any)
	}{
		{
			name: "simple query",
			query: SearchQuery{
				Query: "programming",
				Limit: 10,
			},
			wantCount: 2,
		},
		{
			name: "specific query",
			query: SearchQuery{
				Query: "pasta",
				Limit: 10,
			},
			wantCount: 1,
			wantFirst: "cooking.md",
		},
		{
			name: "filter by tag",
			query: SearchQuery{
				Query: "",
				Tags:  []string{"recipes"},
				Limit: 10,
			},
			wantCount: 1,
			wantFirst: "cooking.md",
		},
		{
			name: "query with tag filter",
			query: SearchQuery{
				Query: "programming",
				Tags:  []string{"programming"},
				Limit: 10,
			},
			wantCount: 2,
		},
		{
			name: "no results",
			query: SearchQuery{
				Query: "nonexistent",
				Limit: 10,
			},
			wantCount: 0,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			results, err := search.Search(tt.query)
			if err != nil {
				t.Fatalf("Search() error = %v", err)
			}

			if len(results) != tt.wantCount {
				t.Errorf("Search() returned %d results, want %d", len(results), tt.wantCount)
			}

			if tt.wantFirst != "" && len(results) > 0 {
				if results[0].NoteID != tt.wantFirst {
					t.Errorf("First result ID = %q, want %q", results[0].NoteID, tt.wantFirst)
				}
			}
		})
	}
}

func TestSearchService_RemoveNote(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "note1.md",
			Title:      "Note 1",
			Path:       "note1.md",
			Content:    "Content 1",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note2.md",
			Title:      "Note 2",
			Path:       "note2.md",
			Content:    "Content 2",
			ModifiedAt: time.Now(),
		},
	}

	search.IndexAll(notes)

	// Remove first note
	search.RemoveNote("note1.md")

	// Search should only find note2
	results, err := search.Search(SearchQuery{
		Query: "Content",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 1 {
		t.Errorf("Search() returned %d results after remove, want 1", len(results))
	}

	if len(results) > 0 && results[0].NoteID == "note1.md" {
		t.Error("Search() returned removed note")
	}
}

func TestSearchService_PathFilter(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "folder1/note.md",
			Title:      "Note in Folder 1",
			Path:       "folder1/note.md",
			Content:    "Test content",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "folder2/note.md",
			Title:      "Note in Folder 2",
			Path:       "folder2/note.md",
			Content:    "Test content",
			ModifiedAt: time.Now(),
		},
	}

	search.IndexAll(notes)

	results, err := search.Search(SearchQuery{
		Query:      "",
		PathPrefix: "folder1/",
		Limit:      10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 1 {
		t.Errorf("Search() with path filter returned %d results, want 1", len(results))
	}

	if len(results) > 0 && results[0].Path != "folder1/note.md" {
		t.Errorf("Search() returned wrong path: %q", results[0].Path)
	}
}

func TestSearchService_DateFilter(t *testing.T) {
	search := NewSearchService()

	now := time.Now()
	yesterday := now.Add(-24 * time.Hour)
	tomorrow := now.Add(24 * time.Hour)

	notes := []domain.Note{
		{
			ID:         "old.md",
			Title:      "Old Note",
			Path:       "old.md",
			Content:    "Old content",
			ModifiedAt: yesterday,
		},
		{
			ID:         "new.md",
			Title:      "New Note",
			Path:       "new.md",
			Content:    "New content",
			ModifiedAt: now,
		},
	}

	search.IndexAll(notes)

	// Filter for notes from today onwards
	results, err := search.Search(SearchQuery{
		Query:    "",
		DateFrom: &now,
		Limit:    10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 1 {
		t.Errorf("Search() with date filter returned %d results, want 1", len(results))
	}

	if len(results) > 0 && results[0].NoteID != "new.md" {
		t.Errorf("Search() returned wrong note: %q", results[0].NoteID)
	}

	// Filter for notes up to tomorrow
	results, err = search.Search(SearchQuery{
		Query:  "",
		DateTo: &tomorrow,
		Limit:  10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 2 {
		t.Errorf("Search() with DateTo filter returned %d results, want 2", len(results))
	}
}

func TestSearchService_Limit(t *testing.T) {
	search := NewSearchService()

	notes := make([]domain.Note, 10)
	for i := 0; i < 10; i++ {
		notes[i] = domain.Note{
			ID:         string(rune('a'+i)) + ".md",
			Title:      "Note",
			Path:       string(rune('a'+i)) + ".md",
			Content:    "Test content",
			ModifiedAt: time.Now(),
		}
	}

	search.IndexAll(notes)

	results, err := search.Search(SearchQuery{
		Query: "Test",
		Limit: 5,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 5 {
		t.Errorf("Search() with limit 5 returned %d results", len(results))
	}
}

func TestSearchService_Tokenization(t *testing.T) {
	search := NewSearchService()

	tests := []struct {
		name  string
		input string
		want  int // minimum expected tokens
	}{
		{
			name:  "simple text",
			input: "hello world",
			want:  2,
		},
		{
			name:  "with punctuation",
			input: "Hello, world! How are you?",
			want:  4,
		},
		{
			name:  "with newlines",
			input: "First line\nSecond line",
			want:  4,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tokens := search.tokenize(tt.input)
			if len(tokens) < tt.want {
				t.Errorf("tokenize() returned %d tokens, want at least %d", len(tokens), tt.want)
			}
		})
	}
}
