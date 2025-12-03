package service

import (
	"strings"
	"testing"
	"time"

	"notes/backend/domain"
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
		wantFirst string
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

	search.RemoveNote("note1.md")

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
		want  int
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

func TestSearchService_ScoreSorting(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "best-match.md",
			Title:      "Go Programming Language Go Go",
			Path:       "best-match.md",
			Content:    "Go is awesome Go Go Go",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "medium-match.md",
			Title:      "Programming",
			Path:       "medium-match.md",
			Content:    "Go is a language",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "low-match.md",
			Title:      "Introduction",
			Path:       "low-match.md",
			Content:    "This mentions Go once",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "Go",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) != 3 {
		t.Fatalf("Search() returned %d results, want 3", len(results))
	}

	for i := 0; i < len(results)-1; i++ {
		if results[i].Score < results[i+1].Score {
			t.Errorf("Results not sorted correctly: result[%d].Score = %f < result[%d].Score = %f",
				i, results[i].Score, i+1, results[i+1].Score)
		}
	}

	if results[0].NoteID != "best-match.md" {
		t.Errorf("First result = %q, want %q (scores: %v)",
			results[0].NoteID, "best-match.md",
			[]float64{results[0].Score, results[1].Score, results[2].Score})
	}
}

func TestSearchService_FrontmatterIndexing(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:      "note1.md",
			Title:   "Note 1",
			Path:    "note1.md",
			Content: "Basic content",
			Aliases: []string{"FirstNote", "N1"},
			Type:    "article",
			Frontmatter: map[string]any{
				"author":   "John Doe",
				"category": "technology",
				"keywords": []interface{}{"golang", "search"},
			},
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note2.md",
			Title:      "Note 2",
			Path:       "note2.md",
			Content:    "Different content",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	tests := []struct {
		name      string
		query     string
		wantNotes []string
	}{
		{
			name:      "search by alias",
			query:     "FirstNote",
			wantNotes: []string{"note1.md"},
		},
		{
			name:      "search by type",
			query:     "article",
			wantNotes: []string{"note1.md"},
		},
		{
			name:      "search by frontmatter string field",
			query:     "technology",
			wantNotes: []string{"note1.md"},
		},
		{
			name:      "search by frontmatter array value",
			query:     "golang",
			wantNotes: []string{"note1.md"},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			results, err := search.Search(SearchQuery{
				Query: tt.query,
				Limit: 10,
			})
			if err != nil {
				t.Fatalf("Search() error = %v", err)
			}

			if len(results) != len(tt.wantNotes) {
				t.Errorf("Search(%q) returned %d results, want %d", tt.query, len(results), len(tt.wantNotes))
			}

			for i, wantID := range tt.wantNotes {
				if i >= len(results) {
					break
				}
				if results[i].NoteID != wantID {
					t.Errorf("Result[%d].NoteID = %q, want %q", i, results[i].NoteID, wantID)
				}
			}
		})
	}
}

func TestSearchService_ExactMatchBoosting(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "exact-title.md",
			Title:      "Go Programming",
			Path:       "exact-title.md",
			Content:    "This is about something else",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "exact-content.md",
			Title:      "Something",
			Path:       "exact-content.md",
			Content:    "Go Programming is great",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "partial.md",
			Title:      "Programming",
			Path:       "partial.md",
			Content:    "Various topics about Go",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "Go Programming",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) < 2 {
		t.Fatalf("Search() returned %d results, want at least 2", len(results))
	}

	if results[0].NoteID != "exact-title.md" {
		t.Errorf("First result = %q, want %q (exact title match should rank first)", results[0].NoteID, "exact-title.md")
	}
}

func TestSearchService_FuzzyMatching(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "note1.md",
			Title:      "Programming Guide",
			Path:       "note1.md",
			Content:    "Learn about programing concepts",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note2.md",
			Title:      "Unrelated",
			Path:       "note2.md",
			Content:    "Completely different topic",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "programming",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) == 0 {
		t.Fatal("Search() found no results for fuzzy match")
	}

	found := false
	for _, result := range results {
		if result.NoteID == "note1.md" {
			found = true
			break
		}
	}

	if !found {
		t.Error("Fuzzy match should find note with similar spelling")
	}
}

func TestSearchService_SnippetExtraction(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "long-note.md",
			Title:      "Long Note",
			Path:       "long-note.md",
			Content:    "This is a very long piece of content that contains many words and sentences. The important keyword appears somewhere in the middle of this text. We want to extract a snippet that shows the context around this keyword.",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "short-note.md",
			Title:      "Short Note",
			Path:       "short-note.md",
			Content:    "keyword here",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "keyword",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) < 2 {
		t.Fatalf("Search() returned %d results, want at least 2", len(results))
	}

	for _, result := range results {
		if result.Snippet == "" {
			t.Errorf("Result for %q has empty snippet", result.NoteID)
		}

		if result.NoteID == "long-note.md" {
			if !strings.Contains(strings.ToLower(result.Snippet), "keyword") {
				t.Errorf("Snippet for long note should contain 'keyword', got: %q", result.Snippet)
			}

			if !strings.Contains(result.Snippet, "...") {
				t.Errorf("Long note snippet should contain ellipsis, got: %q", result.Snippet)
			}
		}
	}
}

func TestLevenshteinDistance(t *testing.T) {
	tests := []struct {
		s1       string
		s2       string
		expected int
	}{
		{"", "", 0},
		{"hello", "", 5},
		{"", "hello", 5},
		{"hello", "hello", 0},
		{"hello", "helo", 1},
		{"programming", "programing", 1},
		{"kitten", "sitting", 3},
		{"saturday", "sunday", 3},
	}

	for _, tt := range tests {
		t.Run(tt.s1+"_"+tt.s2, func(t *testing.T) {
			result := levenshteinDistance(tt.s1, tt.s2)
			if result != tt.expected {
				t.Errorf("levenshteinDistance(%q, %q) = %d, want %d", tt.s1, tt.s2, result, tt.expected)
			}
		})
	}
}

func TestSearchService_EmptySnippet(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "note.md",
			Title:      "Test Note",
			Path:       "note.md",
			Content:    "Some content here",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) == 0 {
		t.Fatal("Search() with empty query should return all notes")
	}

	for _, result := range results {
		if result.Snippet != "" {
			t.Errorf("Empty query should return empty snippets, got: %q", result.Snippet)
		}
	}
}

func TestSearchService_CombinedScoring(t *testing.T) {
	search := NewSearchService()

	notes := []domain.Note{
		{
			ID:         "exact-match.md",
			Title:      "Exact Python Match",
			Path:       "exact-match.md",
			Content:    "This note has Python in title and content",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "fuzzy-match.md",
			Title:      "Pyton Guide",
			Path:       "fuzzy-match.md",
			Content:    "Guide about Pyton programming",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "bm25-match.md",
			Title:      "Programming",
			Path:       "bm25-match.md",
			Content:    "Python Python Python Python Python",
			ModifiedAt: time.Now(),
		},
	}

	err := search.IndexAll(notes)
	if err != nil {
		t.Fatalf("IndexAll() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "Python",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) < 3 {
		t.Fatalf("Search() returned %d results, want at least 3", len(results))
	}

	if results[0].NoteID != "exact-match.md" {
		t.Logf("Result scores: %v", []struct {
			id    string
			score float64
		}{
			{results[0].NoteID, results[0].Score},
			{results[1].NoteID, results[1].Score},
			{results[2].NoteID, results[2].Score},
		})
		t.Errorf("First result = %q, want %q (exact match should rank highest)", results[0].NoteID, "exact-match.md")
	}

	for i, result := range results {
		if result.Score <= 0 {
			t.Errorf("Result[%d].Score = %f, want > 0", i, result.Score)
		}
	}
}

func TestSearchService_SnippetHighlighting(t *testing.T) {
	search := NewSearchService()

	tests := []struct {
		name         string
		content      string
		query        string
		wantContains []string
	}{
		{
			name:         "single term highlighting",
			content:      "This is a test document with the keyword test appearing multiple times for testing.",
			query:        "test",
			wantContains: []string{"[[test]]"},
		},
		{
			name:         "multiple term highlighting",
			content:      "Go programming language is great. Go is simple and Go is fast.",
			query:        "Go programming",
			wantContains: []string{"[[Go]]", "[[programming]]"},
		},
		{
			name:         "case insensitive highlighting",
			content:      "Python Programming and python development are related topics.",
			query:        "python",
			wantContains: []string{"[[Python]]", "[[python]]"},
		},
		{
			name:         "overlapping matches merged",
			content:      "Programming programmer programs",
			query:        "program",
			wantContains: []string{"[[Program", "program]]"},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			note := &domain.Note{
				ID:         "test.md",
				Title:      "Test",
				Path:       "test.md",
				Content:    tt.content,
				ModifiedAt: time.Now(),
			}

			err := search.IndexNote(note)
			if err != nil {
				t.Fatalf("IndexNote() error = %v", err)
			}

			results, err := search.Search(SearchQuery{
				Query: tt.query,
				Limit: 10,
			})

			if err != nil {
				t.Fatalf("Search() error = %v", err)
			}

			if len(results) == 0 {
				t.Fatal("Search() returned no results")
			}

			snippet := results[0].Snippet
			t.Logf("Snippet: %q", snippet)

			for _, want := range tt.wantContains {
				if !strings.Contains(snippet, want) {
					t.Errorf("Snippet should contain %q, got: %q", want, snippet)
				}
			}
		})
	}
}

func TestHighlightMatches(t *testing.T) {
	search := NewSearchService()

	tests := []struct {
		name         string
		snippet      string
		queryTokens  []string
		wantContains []string
	}{
		{
			name:         "single match",
			snippet:      "The quick brown fox",
			queryTokens:  []string{"quick"},
			wantContains: []string{"[[quick]]"},
		},
		{
			name:         "multiple non-overlapping matches",
			snippet:      "The quick brown fox jumps quickly",
			queryTokens:  []string{"quick"},
			wantContains: []string{"[[quick]]", "[[quick]]ly"},
		},
		{
			name:         "case insensitive",
			snippet:      "Python and PYTHON and python",
			queryTokens:  []string{"python"},
			wantContains: []string{"[[Python]]", "[[PYTHON]]", "[[python]]"},
		},
		{
			name:         "no matches",
			snippet:      "The quick brown fox",
			queryTokens:  []string{"elephant"},
			wantContains: []string{},
		},
		{
			name:         "overlapping tokens merged",
			snippet:      "programming language",
			queryTokens:  []string{"program", "programming"},
			wantContains: []string{"[[programming]]"},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := search.highlightMatches(tt.snippet, tt.queryTokens)

			for _, want := range tt.wantContains {
				if !strings.Contains(result, want) {
					t.Errorf("highlightMatches() result should contain %q, got: %q", want, result)
				}
			}

			if len(tt.wantContains) == 0 && strings.Contains(result, "[[") {
				t.Errorf("highlightMatches() should not add markers when no matches, got: %q", result)
			}

			openCount := strings.Count(result, "[[")
			closeCount := strings.Count(result, "]]")
			if openCount != closeCount {
				t.Errorf("highlightMatches() unbalanced markers: %d [[ vs %d ]]", openCount, closeCount)
			}
		})
	}
}

func TestSearchService_SnippetPreservesOriginalCase(t *testing.T) {
	search := NewSearchService()

	note := &domain.Note{
		ID:         "test.md",
		Title:      "Test",
		Path:       "test.md",
		Content:    "This contains UPPERCASE and lowercase versions of Python.",
		ModifiedAt: time.Now(),
	}

	err := search.IndexNote(note)
	if err != nil {
		t.Fatalf("IndexNote() error = %v", err)
	}

	results, err := search.Search(SearchQuery{
		Query: "python",
		Limit: 10,
	})

	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}

	if len(results) == 0 {
		t.Fatal("Search() returned no results")
	}

	snippet := results[0].Snippet
	if !strings.Contains(snippet, "[[Python]]") {
		t.Errorf("Snippet should preserve original case 'Python', got: %q", snippet)
	}
}
