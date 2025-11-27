package service

import (
	"testing"
	"time"

	"notes/backend/internal/domain"
)

func TestGraphService_IndexNote(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:      "test.md",
		Title:   "Test Note",
		Content: "This is a [[link]] to another note and a #tag",
		Frontmatter: map[string]any{
			"tags": []string{"frontmatter-tag"},
		},
		ModifiedAt: time.Now(),
	}

	err := graph.IndexNote(note)
	if err != nil {
		t.Fatalf("IndexNote() error = %v", err)
	}

	if len(note.Links) == 0 {
		t.Error("IndexNote() did not extract links")
	}

	if len(note.Tags) == 0 {
		t.Error("IndexNote() did not extract tags")
	}

	hasInlineTag := false
	hasFrontmatterTag := false
	for _, tag := range note.Tags {
		if tag.Name == "tag" {
			hasInlineTag = true
		}
		if tag.Name == "frontmatter-tag" {
			hasFrontmatterTag = true
		}
	}

	if !hasInlineTag {
		t.Error("IndexNote() did not extract inline tag")
	}
	if !hasFrontmatterTag {
		t.Error("IndexNote() did not extract frontmatter tag")
	}
}

func TestGraphService_Backlinks(t *testing.T) {
	graph := NewGraphService()

	note1 := &domain.Note{
		ID:          "note1.md",
		Title:       "Note 1",
		Content:     "This links to [[note2]]",
		Frontmatter: make(map[string]any),
		ModifiedAt:  time.Now(),
	}

	note2 := &domain.Note{
		ID:          "note2.md",
		Title:       "Note 2",
		Content:     "No links here",
		Frontmatter: make(map[string]any),
		ModifiedAt:  time.Now(),
	}

	graph.IndexNote(note1)
	graph.IndexNote(note2)

	if len(note1.Links) == 0 {
		t.Fatalf("IndexNote() did not extract any links from note1. Content: %q", note1.Content)
	}

	t.Logf("Extracted %d links from note1: %+v", len(note1.Links), note1.Links)

	backlinks := graph.GetBacklinks("note2.md")

	if len(backlinks) == 0 {
		t.Errorf("GetBacklinks() returned no backlinks to note2.md")
		t.Logf("All backlinks: %+v", graph.backlinks)
	}

	if len(backlinks) > 0 && backlinks[0].Source != "note1.md" {
		t.Errorf("Backlink source = %q, want %q", backlinks[0].Source, "note1.md")
	}
}

func TestGraphService_GetGraph(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:         "a.md",
			Title:      "A",
			Content:    "Links to [[b]] and [[c]]",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "b.md",
			Title:      "B",
			Content:    "Links to [[c]]",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "c.md",
			Title:      "C",
			Content:    "No links",
			ModifiedAt: time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	g := graph.GetGraph()

	if len(g.Nodes) != 3 {
		t.Errorf("GetGraph() nodes = %d, want 3", len(g.Nodes))
	}

	if len(g.Edges) != 3 {
		t.Errorf("GetGraph() edges = %d, want 3", len(g.Edges))
	}
}

func TestGraphService_GetNeighbors(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:         "center.md",
			Title:      "Center",
			Content:    "Links to [[out]]",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "in.md",
			Title:      "In",
			Content:    "Links to [[center]]",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "out.md",
			Title:      "Out",
			Content:    "No links",
			ModifiedAt: time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	neighbors := graph.GetNeighbors("center.md")

	if len(neighbors) != 2 {
		t.Errorf("GetNeighbors() = %d neighbors, want 2", len(neighbors))
	}

	hasIn := false
	hasOut := false
	for _, n := range neighbors {
		if n == "in.md" {
			hasIn = true
		}
		if n == "out.md" {
			hasOut = true
		}
	}

	if !hasIn || !hasOut {
		t.Errorf("GetNeighbors() missing expected neighbors: hasIn=%v, hasOut=%v", hasIn, hasOut)
	}
}

func TestGraphService_Tags(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:         "note1.md",
			Content:    "Content with #tag1 #tag2",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note2.md",
			Content:    "Content with #tag1",
			ModifiedAt: time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	tag1Notes := graph.GetNotesWithTag("tag1")
	if len(tag1Notes) != 2 {
		t.Errorf("GetNotesWithTag('tag1') = %d notes, want 2", len(tag1Notes))
	}

	tag2Notes := graph.GetNotesWithTag("tag2")
	if len(tag2Notes) != 1 {
		t.Errorf("GetNotesWithTag('tag2') = %d notes, want 1", len(tag2Notes))
	}

	allTags := graph.GetAllTags()
	if len(allTags) != 2 {
		t.Errorf("GetAllTags() = %d tags, want 2", len(allTags))
	}
}

func TestGraphService_RemoveNote(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:          "note1.md",
			Content:     "Links to [[note2]]",
			Frontmatter: make(map[string]any),
			ModifiedAt:  time.Now(),
		},
		{
			ID:          "note2.md",
			Content:     "No links",
			Frontmatter: make(map[string]any),
			ModifiedAt:  time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	backlinksBeforeRemove := graph.GetBacklinks("note2.md")
	t.Logf("Backlinks to note2 before removal: %+v", backlinksBeforeRemove)

	graph.RemoveNote("note1.md")

	backlinks := graph.GetBacklinks("note2.md")
	t.Logf("Backlinks to note2 after removal: %+v", backlinks)
	if len(backlinks) != 0 {
		t.Errorf("RemoveNote() did not remove backlinks, got %d backlinks", len(backlinks))
	}

	g := graph.GetGraph()
	if len(g.Edges) != 0 {
		t.Error("RemoveNote() did not remove edges")
	}
}

func TestGraphService_WikilinkFormats(t *testing.T) {
	graph := NewGraphService()

	tests := []struct {
		name        string
		content     string
		wantLinks   int
		wantTargets []string
	}{
		{
			name:        "simple wikilink",
			content:     "[[target]]",
			wantLinks:   1,
			wantTargets: []string{"target.md"},
		},
		{
			name:        "wikilink with display text",
			content:     "[[target|display]]",
			wantLinks:   1,
			wantTargets: []string{"target.md"},
		},
		{
			name:        "block reference",
			content:     "[[note#block]]",
			wantLinks:   1,
			wantTargets: []string{"note.md"},
		},
		{
			name:        "embed link",
			content:     "![[image]]",
			wantLinks:   1,
			wantTargets: []string{"image.md"},
		},
		{
			name:        "multiple links",
			content:     "[[first]] and [[second]]",
			wantLinks:   2,
			wantTargets: []string{"first.md", "second.md"},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			note := &domain.Note{
				ID:         "test.md",
				Content:    tt.content,
				ModifiedAt: time.Now(),
			}

			graph.IndexNote(note)

			if len(note.Links) != tt.wantLinks {
				t.Errorf("IndexNote() found %d links, want %d", len(note.Links), tt.wantLinks)
			}

			for i, target := range tt.wantTargets {
				if i >= len(note.Links) {
					break
				}
				if note.Links[i].Target != target {
					t.Errorf("Link[%d].Target = %q, want %q", i, note.Links[i].Target, target)
				}
			}
		})
	}
}
