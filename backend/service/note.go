package service

import (
	"bytes"
	"crypto/sha256"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"notes/backend/domain"

	"github.com/yuin/goldmark"
	"github.com/yuin/goldmark/ast"
	"github.com/yuin/goldmark/text"
	"gopkg.in/yaml.v3"
)

// NoteService handles note operations including CRUD and parsing.
type NoteService struct {
	fs     *FilesystemService
	parser goldmark.Markdown
}

// NewNoteService creates a new note service.
func NewNoteService(fs *FilesystemService) *NoteService {
	return &NoteService{
		fs:     fs,
		parser: goldmark.New(),
	}
}

// GetNote retrieves a note by its ID (relative path).
func (s *NoteService) GetNote(id string) (*domain.Note, error) {
	// Read file content
	content, err := s.fs.ReadFile(id)
	if err != nil {
		return nil, err
	}

	// Get file info for timestamps
	workspace, err := s.fs.GetCurrentWorkspace()
	if err != nil {
		return nil, err
	}

	fullPath := filepath.Join(workspace.RootPath, id)
	info, err := os.Stat(fullPath)
	if err != nil {
		return nil, fmt.Errorf("failed to stat file: %w", err)
	}

	// Parse note
	note, err := s.parseNote(id, content, info)
	if err != nil {
		return nil, err
	}

	return note, nil
}

// ListNotes returns summaries of all notes in the workspace.
func (s *NoteService) ListNotes() ([]domain.NoteSummary, error) {
	files, err := s.fs.LoadMarkdownFiles()
	if err != nil {
		return nil, err
	}

	workspace, err := s.fs.GetCurrentWorkspace()
	if err != nil {
		return nil, err
	}

	summaries := make([]domain.NoteSummary, 0, len(files))
	for _, relPath := range files {
		// Read file
		content, err := s.fs.ReadFile(relPath)
		if err != nil {
			continue // Skip files we can't read
		}

		// Get file info
		fullPath := filepath.Join(workspace.RootPath, relPath)
		info, err := os.Stat(fullPath)
		if err != nil {
			continue
		}

		// Parse frontmatter and extract title/tags quickly
		title, tags := s.extractTitleAndTags(content)
		if title == "" {
			title = strings.TrimSuffix(filepath.Base(relPath), filepath.Ext(relPath))
		}

		summaries = append(summaries, domain.NoteSummary{
			ID:         relPath,
			Title:      title,
			Path:       relPath,
			Tags:       tags,
			ModifiedAt: info.ModTime(),
		})
	}

	return summaries, nil
}

// SaveNote creates or updates a note.
func (s *NoteService) SaveNote(note *domain.Note) error {
	// Generate content with frontmatter
	content := s.serializeNote(note)

	// Write to filesystem
	return s.fs.WriteFile(note.Path, content)
}

// DeleteNote removes a note from the workspace.
func (s *NoteService) DeleteNote(id string) error {
	return s.fs.DeleteFile(id)
}

// CreateNote creates a new note with the given title and content.
func (s *NoteService) CreateNote(title, folder string) (*domain.Note, error) {
	// Generate filename from title
	filename := sanitizeFilename(title) + ".md"
	relPath := filename
	if folder != "" {
		relPath = filepath.Join(folder, filename)
	}

	// Check if file already exists
	if _, err := s.fs.ReadFile(relPath); err == nil {
		return nil, &domain.ErrAlreadyExists{Resource: "note", ID: relPath}
	}

	// Create note with basic content
	content := "# " + title + "\n\n"

	note := &domain.Note{
		ID:          relPath,
		Title:       title,
		Path:        relPath,
		Content:     content,
		Frontmatter: make(map[string]any),
		Blocks:      []domain.Block{},
		Links:       []domain.Link{},
		Tags:        []domain.Tag{},
		CreatedAt:   time.Now(),
		ModifiedAt:  time.Now(),
	}

	// Save to disk
	if err := s.SaveNote(note); err != nil {
		return nil, err
	}

	return note, nil
}

// parseNote converts raw content into a structured Note.
// It extracts frontmatter, parses Markdown structure, and identifies blocks.
func (s *NoteService) parseNote(id string, content []byte, info os.FileInfo) (*domain.Note, error) {
	// Extract frontmatter
	frontmatter, body, err := s.extractFrontmatter(content)
	if err != nil {
		return nil, &domain.ErrInvalidFrontmatter{Path: id, Reason: err.Error()}
	}

	// Extract title (from frontmatter or first heading)
	title := s.extractTitle(frontmatter, body)
	if title == "" {
		title = strings.TrimSuffix(filepath.Base(id), filepath.Ext(id))
	}

	// Parse Markdown to extract blocks
	blocks := s.extractBlocks(id, body)

	// Note: Links and tags extraction will be done by the graph service
	// to avoid circular dependencies

	note := &domain.Note{
		ID:          id,
		Title:       title,
		Path:        id,
		Content:     string(body),
		Frontmatter: frontmatter,
		Blocks:      blocks,
		Links:       []domain.Link{},
		Tags:        []domain.Tag{},
		CreatedAt:   info.ModTime(), // Use file mtime as approximation
		ModifiedAt:  info.ModTime(),
	}

	return note, nil
}

// extractFrontmatter parses YAML frontmatter from content.
// Returns frontmatter map, body content, and any error.
func (s *NoteService) extractFrontmatter(content []byte) (map[string]any, []byte, error) {
	// Check for frontmatter delimiters
	if !bytes.HasPrefix(content, []byte("---\n")) && !bytes.HasPrefix(content, []byte("---\r\n")) {
		return make(map[string]any), content, nil
	}

	// Find end of frontmatter
	lines := bytes.Split(content, []byte("\n"))
	endIdx := -1
	for i := 1; i < len(lines); i++ {
		line := bytes.TrimSpace(lines[i])
		if bytes.Equal(line, []byte("---")) {
			endIdx = i
			break
		}
	}

	if endIdx == -1 {
		return make(map[string]any), content, nil
	}

	// Extract frontmatter content
	fmContent := bytes.Join(lines[1:endIdx], []byte("\n"))
	body := bytes.Join(lines[endIdx+1:], []byte("\n"))

	// Parse YAML
	var frontmatter map[string]any
	if len(fmContent) > 0 {
		if err := yaml.Unmarshal(fmContent, &frontmatter); err != nil {
			return nil, nil, fmt.Errorf("failed to parse frontmatter: %w", err)
		}
	}

	if frontmatter == nil {
		frontmatter = make(map[string]any)
	}

	return frontmatter, body, nil
}

// extractTitle gets the title from frontmatter or first heading.
func (s *NoteService) extractTitle(frontmatter map[string]any, content []byte) string {
	// Check frontmatter first
	if title, ok := frontmatter["title"].(string); ok && title != "" {
		return title
	}

	// Parse Markdown to find first heading
	doc := s.parser.Parser().Parse(text.NewReader(content))
	var title string
	ast.Walk(doc, func(n ast.Node, entering bool) (ast.WalkStatus, error) {
		if entering && n.Kind() == ast.KindHeading {
			heading := n.(*ast.Heading)
			if heading.Level == 1 {
				title = string(heading.Text(content))
				return ast.WalkStop, nil
			}
		}
		return ast.WalkContinue, nil
	})

	return title
}

// extractTitleAndTags quickly extracts title and tags without full parsing.
// Used for listing notes efficiently.
func (s *NoteService) extractTitleAndTags(content []byte) (string, []domain.Tag) {
	frontmatter, body, err := s.extractFrontmatter(content)
	if err != nil {
		return "", nil
	}

	title := s.extractTitle(frontmatter, body)

	// Extract tags from frontmatter
	var tags []domain.Tag
	if fmTags, ok := frontmatter["tags"]; ok {
		switch t := fmTags.(type) {
		case []interface{}:
			for _, tag := range t {
				if tagStr, ok := tag.(string); ok {
					tags = append(tags, domain.Tag{Name: tagStr})
				}
			}
		case []string:
			for _, tag := range t {
				tags = append(tags, domain.Tag{Name: tag})
			}
		}
	}

	return title, tags
}

// extractBlocks parses Markdown content into outline blocks.
// Each paragraph, heading, list item, etc. becomes a separate block.
func (s *NoteService) extractBlocks(noteID string, content []byte) []domain.Block {
	doc := s.parser.Parser().Parse(text.NewReader(content))

	blocks := []domain.Block{}
	blockIdx := 0

	ast.Walk(doc, func(n ast.Node, entering bool) (ast.WalkStatus, error) {
		if !entering {
			return ast.WalkContinue, nil
		}

		var blockType domain.BlockType
		var shouldCreate bool

		switch n.Kind() {
		case ast.KindParagraph:
			blockType = domain.BlockTypeParagraph
			shouldCreate = true
		case ast.KindHeading:
			blockType = domain.BlockTypeHeading
			shouldCreate = true
		case ast.KindListItem:
			blockType = domain.BlockTypeListItem
			shouldCreate = true
		case ast.KindFencedCodeBlock, ast.KindCodeBlock:
			blockType = domain.BlockTypeCode
			shouldCreate = true
		case ast.KindBlockquote:
			blockType = domain.BlockTypeQuote
			shouldCreate = true
		}

		if shouldCreate {
			blockContent := string(n.Text(content))
			blockID := generateBlockID(noteID, blockIdx)

			blocks = append(blocks, domain.Block{
				ID:       blockID,
				NoteID:   noteID,
				Content:  blockContent,
				Level:    0, // TODO: Calculate nesting level
				Parent:   "",
				Children: []string{},
				Position: blockIdx,
				Type:     blockType,
			})

			blockIdx++
		}

		return ast.WalkContinue, nil
	})

	return blocks
}

// serializeNote converts a Note back to Markdown with frontmatter.
func (s *NoteService) serializeNote(note *domain.Note) []byte {
	var buf bytes.Buffer

	// Write frontmatter if present
	if len(note.Frontmatter) > 0 {
		buf.WriteString("---\n")

		// Ensure title is in frontmatter
		if note.Title != "" {
			note.Frontmatter["title"] = note.Title
		}

		fmBytes, err := yaml.Marshal(note.Frontmatter)
		if err == nil {
			buf.Write(fmBytes)
		}
		buf.WriteString("---\n\n")
	}

	// Write content
	buf.WriteString(note.Content)

	return buf.Bytes()
}

// sanitizeFilename converts a title to a valid filename.
func sanitizeFilename(title string) string {
	// Replace invalid characters
	invalid := []string{"/", "\\", ":", "*", "?", "\"", "<", ">", "|"}
	filename := title
	for _, char := range invalid {
		filename = strings.ReplaceAll(filename, char, "-")
	}

	// Trim spaces and dots
	filename = strings.TrimSpace(filename)
	filename = strings.Trim(filename, ".")

	// Limit length
	if len(filename) > 200 {
		filename = filename[:200]
	}

	if filename == "" {
		filename = "Untitled"
	}

	return filename
}

// generateBlockID creates a unique identifier for a block.
func generateBlockID(noteID string, position int) string {
	data := fmt.Sprintf("%s:%d", noteID, position)
	hash := sha256.Sum256([]byte(data))
	return fmt.Sprintf("%x", hash[:8])
}
