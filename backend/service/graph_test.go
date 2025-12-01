package service

import (
	"slices"
	"testing"
	"time"

	"notes/backend/domain"
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

func TestGraphService_GetAllTagsWithCounts(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:      "note1.md",
			Content: "Content with #tag1 and #tag2",
			Frontmatter: map[string]any{
				"tags": []string{"tag3"},
			},
			ModifiedAt: time.Now(),
		},
		{
			ID:      "note2.md",
			Content: "Content with #tag1 and #tag3",
			Frontmatter: map[string]any{
				"tags": []string{"tag2"},
			},
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note3.md",
			Content:    "Content with #tag1",
			ModifiedAt: time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	tagInfos := graph.GetAllTagsWithCounts()

	if len(tagInfos) != 3 {
		t.Errorf("GetAllTagsWithCounts() returned %d tags, want 3", len(tagInfos))
	}

	tagMap := make(map[string]domain.TagInfo)
	for _, info := range tagInfos {
		tagMap[info.Name] = info
	}

	if info, ok := tagMap["tag1"]; !ok {
		t.Error("tag1 not found in results")
	} else {
		if info.Count != 3 {
			t.Errorf("tag1 count = %d, want 3", info.Count)
		}
		if len(info.NoteIDs) != 3 {
			t.Errorf("tag1 has %d note IDs, want 3", len(info.NoteIDs))
		}
	}

	if info, ok := tagMap["tag2"]; !ok {
		t.Error("tag2 not found in results")
	} else {
		if info.Count != 2 {
			t.Errorf("tag2 count = %d, want 2", info.Count)
		}
		if len(info.NoteIDs) != 2 {
			t.Errorf("tag2 has %d note IDs, want 2", len(info.NoteIDs))
		}
	}

	if info, ok := tagMap["tag3"]; !ok {
		t.Error("tag3 not found in results")
	} else {
		if info.Count != 2 {
			t.Errorf("tag3 count = %d, want 2", info.Count)
		}
		if len(info.NoteIDs) != 2 {
			t.Errorf("tag3 has %d note IDs, want 2", len(info.NoteIDs))
		}
	}
}

func TestGraphService_GetTagInfo(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{
			ID:         "note1.md",
			Content:    "Content with #test-tag",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note2.md",
			Content:    "Content with #test-tag and #other-tag",
			ModifiedAt: time.Now(),
		},
		{
			ID:         "note3.md",
			Content:    "Content with #test-tag",
			ModifiedAt: time.Now(),
		},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	info := graph.GetTagInfo("test-tag")
	if info == nil {
		t.Fatal("GetTagInfo('test-tag') returned nil")
	}

	if info.Name != "test-tag" {
		t.Errorf("TagInfo.Name = %q, want %q", info.Name, "test-tag")
	}

	if info.Count != 3 {
		t.Errorf("TagInfo.Count = %d, want 3", info.Count)
	}

	if len(info.NoteIDs) != 3 {
		t.Errorf("TagInfo has %d note IDs, want 3", len(info.NoteIDs))
	}

	noteIDSet := make(map[string]bool)
	for _, nid := range info.NoteIDs {
		noteIDSet[nid] = true
	}

	expectedNotes := []string{"note1.md", "note2.md", "note3.md"}
	for _, expectedNote := range expectedNotes {
		if !noteIDSet[expectedNote] {
			t.Errorf("TagInfo.NoteIDs missing %q", expectedNote)
		}
	}

	nonExistent := graph.GetTagInfo("non-existent-tag")
	if nonExistent != nil {
		t.Error("GetTagInfo('non-existent-tag') should return nil")
	}
}

func TestGraphService_TagIndexIncremental(t *testing.T) {
	graph := NewGraphService()

	note1 := &domain.Note{
		ID:         "note1.md",
		Content:    "Content with #tag1 and #tag2",
		ModifiedAt: time.Now(),
	}
	graph.IndexNote(note1)

	info1 := graph.GetTagInfo("tag1")
	if info1 == nil || info1.Count != 1 {
		t.Fatal("Initial index failed")
	}

	note2 := &domain.Note{
		ID:         "note2.md",
		Content:    "Content with #tag1",
		ModifiedAt: time.Now(),
	}
	graph.IndexNote(note2)

	info1After := graph.GetTagInfo("tag1")
	if info1After == nil || info1After.Count != 2 {
		t.Errorf("tag1 count after adding note2 = %d, want 2", info1After.Count)
	}

	note1Updated := &domain.Note{
		ID:         "note1.md",
		Content:    "Content with #tag2 only",
		ModifiedAt: time.Now(),
	}
	graph.IndexNote(note1Updated)

	info1Final := graph.GetTagInfo("tag1")
	if info1Final == nil || info1Final.Count != 1 {
		t.Errorf("tag1 count after updating note1 = %d, want 1", info1Final.Count)
	}

	info2 := graph.GetTagInfo("tag2")
	if info2 == nil || info2.Count != 1 {
		t.Errorf("tag2 count = %d, want 1", info2.Count)
	}

	graph.RemoveNote("note2.md")

	info1Removed := graph.GetTagInfo("tag1")
	if info1Removed != nil {
		t.Error("tag1 should not exist after removing note2")
	}

	info2After := graph.GetTagInfo("tag2")
	if info2After == nil || info2After.Count != 1 {
		t.Error("tag2 should still exist with count 1")
	}
}

func TestGraphService_TagIndexDeduplication(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:      "note.md",
		Content: "Content with #duplicate-tag and #other-tag",
		Frontmatter: map[string]any{
			"tags": []string{"duplicate-tag"},
		},
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note)

	info := graph.GetTagInfo("duplicate-tag")
	if info == nil {
		t.Fatal("duplicate-tag not found")
	}

	if info.Count != 1 {
		t.Errorf("duplicate-tag count = %d, want 1 (deduplicated)", info.Count)
	}

	if len(info.NoteIDs) != 1 {
		t.Errorf("duplicate-tag has %d note IDs, want 1", len(info.NoteIDs))
	}
}

func TestGraphService_TagIndexNestedTags(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:         "note.md",
		Content:    "Content with #parent/child and #parent/child/grandchild",
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note)

	parentChild := graph.GetTagInfo("parent/child")
	if parentChild == nil {
		t.Error("parent/child tag not found")
	} else if parentChild.Count != 1 {
		t.Errorf("parent/child count = %d, want 1", parentChild.Count)
	}

	grandchild := graph.GetTagInfo("parent/child/grandchild")
	if grandchild == nil {
		t.Error("parent/child/grandchild tag not found")
	} else if grandchild.Count != 1 {
		t.Errorf("parent/child/grandchild count = %d, want 1", grandchild.Count)
	}
}

func TestGraphService_TagIndexEmptyWorkspace(t *testing.T) {
	graph := NewGraphService()

	tagInfos := graph.GetAllTagsWithCounts()
	if len(tagInfos) != 0 {
		t.Errorf("GetAllTagsWithCounts() on empty index returned %d tags, want 0", len(tagInfos))
	}

	info := graph.GetTagInfo("any-tag")
	if info != nil {
		t.Error("GetTagInfo() on empty index should return nil")
	}
}

func TestGraphService_TagInfoWithSpecialCharacters(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:         "note.md",
		Content:    "Content with #tag-with-dashes and #tag_with_underscores",
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note)

	dashTag := graph.GetTagInfo("tag-with-dashes")
	if dashTag == nil || dashTag.Count != 1 {
		t.Error("Tag with dashes not indexed correctly")
	}

	underscoreTag := graph.GetTagInfo("tag_with_underscores")
	if underscoreTag == nil || underscoreTag.Count != 1 {
		t.Error("Tag with underscores not indexed correctly")
	}
}

func TestGraphService_TagInfoCaseSensitivity(t *testing.T) {
	graph := NewGraphService()

	note1 := &domain.Note{
		ID:         "note1.md",
		Content:    "Content with #Tag",
		ModifiedAt: time.Now(),
	}

	note2 := &domain.Note{
		ID:         "note2.md",
		Content:    "Content with #tag",
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note1)
	graph.IndexNote(note2)

	upperTag := graph.GetTagInfo("Tag")
	lowerTag := graph.GetTagInfo("tag")

	if upperTag == nil {
		t.Error("Tag with uppercase not found")
	}
	if lowerTag == nil {
		t.Error("Tag with lowercase not found")
	}

	if upperTag != nil && upperTag.Count != 1 {
		t.Errorf("Uppercase Tag count = %d, want 1", upperTag.Count)
	}

	if upperTag != nil && lowerTag.Count != 1 {
		t.Errorf("Lowercase tag count = %d, want 1", lowerTag.Count)
	}
}

func TestGraphService_TagsSortedByName(t *testing.T) {
	graph := NewGraphService()

	notes := []*domain.Note{
		{ID: "note1.md", Content: "Content with #zebra", ModifiedAt: time.Now()},
		{ID: "note2.md", Content: "Content with #apple", ModifiedAt: time.Now()},
		{ID: "note3.md", Content: "Content with #banana", ModifiedAt: time.Now()},
	}

	for _, note := range notes {
		graph.IndexNote(note)
	}

	tagInfos := graph.GetAllTagsWithCounts()

	expectedTags := []string{"apple", "banana", "zebra"}

	if len(tagInfos) != len(expectedTags) {
		t.Fatalf("GetAllTagsWithCounts() returned %d tags, want %d", len(tagInfos), len(expectedTags))
	}

	tagNames := make([]string, len(tagInfos))
	for i, info := range tagInfos {
		tagNames[i] = info.Name
	}

	for i, expectedTag := range expectedTags {
		found := slices.Contains(tagNames, expectedTag)
		if !found {
			t.Errorf("Expected tag %q at position %d not found in results", expectedTag, i)
		}
	}
}

func TestGraphService_DeepNestedTags(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:         "note.md",
		Content:    "Content with #a/b/c/d/e/f deeply nested tag",
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note)

	info := graph.GetTagInfo("a/b/c/d/e/f")
	if info == nil {
		t.Fatal("Deeply nested tag not found")
	}

	if info.Count != 1 {
		t.Errorf("Deeply nested tag count = %d, want 1", info.Count)
	}
}

func TestGraphService_MixedNestedAndFlatTags(t *testing.T) {
	graph := NewGraphService()

	note := &domain.Note{
		ID:         "note.md",
		Content:    "Content with #flat #nested/tag and #another/nested/tag",
		ModifiedAt: time.Now(),
	}

	graph.IndexNote(note)

	flat := graph.GetTagInfo("flat")
	nested := graph.GetTagInfo("nested/tag")
	doubleNested := graph.GetTagInfo("another/nested/tag")

	if flat == nil || flat.Count != 1 {
		t.Error("Flat tag not indexed correctly")
	}

	if nested == nil || nested.Count != 1 {
		t.Error("Nested tag not indexed correctly")
	}

	if doubleNested == nil || doubleNested.Count != 1 {
		t.Error("Double nested tag not indexed correctly")
	}

	tagInfos := graph.GetAllTagsWithCounts()
	if len(tagInfos) != 3 {
		t.Errorf("GetAllTagsWithCounts() returned %d tags, want 3", len(tagInfos))
	}
}
