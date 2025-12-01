package service

import (
	"regexp"
	"strings"
	"sync"

	"notes/backend/domain"

	"github.com/yuin/goldmark"
	"github.com/yuin/goldmark/ast"
	"github.com/yuin/goldmark/text"
	"go.abhg.dev/goldmark/wikilink"
)

// GraphService manages the note graph, links, and backlinks.
// It builds an index of all connections between notes for efficient querying.
type GraphService struct {
	mu sync.RWMutex
	// links maps source note ID to all its outgoing links
	links map[string][]domain.Link
	// backlinks maps target note ID to all notes linking to it
	backlinks map[string][]domain.Link
	// tags maps tag name to note IDs containing that tag
	tags   map[string][]string
	parser goldmark.Markdown
}

// NewGraphService creates a new graph service.
func NewGraphService() *GraphService {
	md := goldmark.New(
		goldmark.WithExtensions(
			&wikilink.Extender{},
		),
	)

	return &GraphService{
		links:     make(map[string][]domain.Link),
		backlinks: make(map[string][]domain.Link),
		tags:      make(map[string][]string),
		parser:    md,
	}
}

// IndexNote parses a note and updates the graph index with its links and tags.
func (s *GraphService) IndexNote(note *domain.Note) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	noteID := note.ID

	delete(s.links, noteID)
	s.removeNoteFromBacklinks(noteID)
	s.removeNoteFromTags(noteID)

	links := s.extractLinks(note)
	tags := s.extractTags(note)

	s.links[noteID] = links

	for _, link := range links {
		targetID := link.Target
		s.backlinks[targetID] = append(s.backlinks[targetID], link)
	}

	for _, tag := range tags {
		s.tags[tag.Name] = append(s.tags[tag.Name], noteID)
	}

	note.Links = links
	note.Tags = tags

	return nil
}

// RemoveNote removes a note from the graph index.
func (s *GraphService) RemoveNote(noteID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.removeNoteFromBacklinks(noteID) // Must call before deleting links
	delete(s.links, noteID)
	delete(s.backlinks, noteID) // Remove all backlinks TO this note
	s.removeNoteFromTags(noteID)
}

// GetBacklinks returns all notes linking to the specified note.
func (s *GraphService) GetBacklinks(noteID string) []domain.Link {
	s.mu.RLock()
	defer s.mu.RUnlock()

	links := s.backlinks[noteID]
	result := make([]domain.Link, len(links))
	copy(result, links)

	return result
}

// GetOutgoingLinks returns all links from the specified note.
func (s *GraphService) GetOutgoingLinks(noteID string) []domain.Link {
	s.mu.RLock()
	defer s.mu.RUnlock()

	links := s.links[noteID]
	result := make([]domain.Link, len(links))
	copy(result, links)

	return result
}

// GetGraph returns the complete graph structure.
func (s *GraphService) GetGraph() *Graph {
	s.mu.RLock()
	defer s.mu.RUnlock()

	nodes := make([]string, 0, len(s.links))
	edges := []GraphEdge{}

	nodeSet := make(map[string]bool)
	for source := range s.links {
		nodeSet[source] = true
	}
	for target := range s.backlinks {
		nodeSet[target] = true
	}
	for node := range nodeSet {
		nodes = append(nodes, node)
	}

	for source, links := range s.links {
		for _, link := range links {
			edges = append(edges, GraphEdge{
				Source: source,
				Target: link.Target,
				Type:   string(link.Type),
			})
		}
	}

	return &Graph{
		Nodes: nodes,
		Edges: edges,
	}
}

// GetNeighbors returns all notes directly connected to the specified note.
// Includes both incoming (backlinks) and outgoing links.
func (s *GraphService) GetNeighbors(noteID string) []string {
	s.mu.RLock()
	defer s.mu.RUnlock()

	neighborSet := make(map[string]bool)

	for _, link := range s.links[noteID] {
		neighborSet[link.Target] = true
	}

	for _, link := range s.backlinks[noteID] {
		neighborSet[link.Source] = true
	}

	neighbors := make([]string, 0, len(neighborSet))
	for neighbor := range neighborSet {
		neighbors = append(neighbors, neighbor)
	}

	return neighbors
}

// GetNotesWithTag returns all note IDs that have the specified tag.
func (s *GraphService) GetNotesWithTag(tagName string) []string {
	s.mu.RLock()
	defer s.mu.RUnlock()

	notes := s.tags[tagName]
	result := make([]string, len(notes))
	copy(result, notes)

	return result
}

// GetAllTags returns all unique tags in the workspace.
func (s *GraphService) GetAllTags() []string {
	s.mu.RLock()
	defer s.mu.RUnlock()

	tags := make([]string, 0, len(s.tags))
	for tag := range s.tags {
		tags = append(tags, tag)
	}

	return tags
}

// GetAllTagsWithCounts returns all tags with their occurrence counts and note IDs.
// Results are sorted by tag name for consistency.
func (s *GraphService) GetAllTagsWithCounts() []domain.TagInfo {
	s.mu.RLock()
	defer s.mu.RUnlock()

	tagInfos := make([]domain.TagInfo, 0, len(s.tags))
	for tagName, noteIDs := range s.tags {
		noteIDsCopy := make([]string, len(noteIDs))
		copy(noteIDsCopy, noteIDs)

		tagInfos = append(tagInfos, domain.TagInfo{
			Name:    tagName,
			Count:   len(noteIDs),
			NoteIDs: noteIDsCopy,
		})
	}

	return tagInfos
}

// GetTagInfo returns information about a specific tag including occurrence count and note IDs.
// Returns nil if the tag doesn't exist in the index.
func (s *GraphService) GetTagInfo(tagName string) *domain.TagInfo {
	s.mu.RLock()
	defer s.mu.RUnlock()

	noteIDs, exists := s.tags[tagName]
	if !exists {
		return nil
	}

	noteIDsCopy := make([]string, len(noteIDs))
	copy(noteIDsCopy, noteIDs)

	return &domain.TagInfo{
		Name:    tagName,
		Count:   len(noteIDs),
		NoteIDs: noteIDsCopy,
	}
}

// extractLinks parses note content to find all links (wikilinks and markdown links).
func (s *GraphService) extractLinks(note *domain.Note) []domain.Link {
	content := []byte(note.Content)
	links := []domain.Link{}

	doc := s.parser.Parser().Parse(text.NewReader(content))

	ast.Walk(doc, func(n ast.Node, entering bool) (ast.WalkStatus, error) {
		if !entering {
			return ast.WalkContinue, nil
		}

		switch node := n.(type) {
		case *wikilink.Node:
			target := string(node.Target)
			displayText := target

			blockRef := ""
			linkType := domain.LinkTypeWiki
			if strings.Contains(target, "#") {
				parts := strings.SplitN(target, "#", 2)
				target = parts[0]
				blockRef = parts[1]
				linkType = domain.LinkTypeBlock
			}

			if node.Embed {
				linkType = domain.LinkTypeEmbed
			}

			if !strings.HasSuffix(target, ".md") && target != "" {
				target = target + ".md"
			}

			links = append(links, domain.Link{
				Source:      note.ID,
				Target:      target,
				DisplayText: displayText,
				Type:        linkType,
				BlockRef:    blockRef,
			})

		case *ast.Link:
			dest := string(node.Destination)
			if !strings.HasPrefix(dest, "http://") && !strings.HasPrefix(dest, "https://") {
				displayText := nodeText(node, content)

				links = append(links, domain.Link{
					Source:      note.ID,
					Target:      dest,
					DisplayText: displayText,
					Type:        domain.LinkTypeMarkdown,
				})
			}
		}

		return ast.WalkContinue, nil
	})

	return links
}

// extractTags parses note content and frontmatter to find all tags.
func (s *GraphService) extractTags(note *domain.Note) []domain.Tag {
	tags := []domain.Tag{}
	tagSet := make(map[string]bool)

	if fmTags, ok := note.Frontmatter["tags"]; ok {
		switch t := fmTags.(type) {
		case []interface{}:
			for _, tag := range t {
				if tagStr, ok := tag.(string); ok {
					tagName := strings.TrimPrefix(tagStr, "#")
					if !tagSet[tagName] {
						tags = append(tags, domain.Tag{Name: tagName, NoteID: note.ID})
						tagSet[tagName] = true
					}
				}
			}
		case []string:
			for _, tag := range t {
				tagName := strings.TrimPrefix(tag, "#")
				if !tagSet[tagName] {
					tags = append(tags, domain.Tag{Name: tagName, NoteID: note.ID})
					tagSet[tagName] = true
				}
			}
		case string:
			tagName := strings.TrimPrefix(t, "#")
			if !tagSet[tagName] {
				tags = append(tags, domain.Tag{Name: tagName, NoteID: note.ID})
				tagSet[tagName] = true
			}
		}
	}

	// Updated regex to support nested tags (e.g., #parent/child)
	// Matches the pattern used in NoteService.extractInlineTags
	tagRegex := regexp.MustCompile(`(?:^|[^a-zA-Z0-9])#([a-zA-Z_][a-zA-Z0-9_-]*(?:/[a-zA-Z0-9_-]+)*)`)
	matches := tagRegex.FindAllStringSubmatch(note.Content, -1)

	for _, match := range matches {
		if len(match) > 1 {
			tagName := match[1]
			if !tagSet[tagName] {
				tags = append(tags, domain.Tag{Name: tagName, NoteID: note.ID})
				tagSet[tagName] = true
			}
		}
	}

	return tags
}

// removeNoteFromBacklinks removes all backlink entries for a note.
func (s *GraphService) removeNoteFromBacklinks(noteID string) {

	if links, ok := s.links[noteID]; ok {
		for _, link := range links {
			targetBacklinks := s.backlinks[link.Target]
			filtered := make([]domain.Link, 0, len(targetBacklinks))
			for _, bl := range targetBacklinks {
				if bl.Source != noteID {
					filtered = append(filtered, bl)
				}
			}
			if len(filtered) > 0 {
				s.backlinks[link.Target] = filtered
			} else {
				delete(s.backlinks, link.Target)
			}
		}
	}
}

// removeNoteFromTags removes a note from all tag indexes.
func (s *GraphService) removeNoteFromTags(noteID string) {
	for tagName, notes := range s.tags {
		filtered := make([]string, 0, len(notes))
		for _, nid := range notes {
			if nid != noteID {
				filtered = append(filtered, nid)
			}
		}
		if len(filtered) > 0 {
			s.tags[tagName] = filtered
		} else {
			delete(s.tags, tagName)
		}
	}
}

// Graph represents the complete note graph structure.
type Graph struct {
	Nodes []string    `json:"nodes"`
	Edges []GraphEdge `json:"edges"`
}

// GraphEdge represents a connection between two notes.
type GraphEdge struct {
	Source string `json:"source"`
	Target string `json:"target"`
	Type   string `json:"type"`
}
