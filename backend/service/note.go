package service

import (
	"bytes"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"notes/backend/domain"

	"github.com/google/uuid"
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
	content, err := s.fs.ReadFile(id)
	if err != nil {
		return nil, err
	}

	workspace, err := s.fs.GetCurrentWorkspace()
	if err != nil {
		return nil, err
	}

	fullPath := filepath.Join(workspace.RootPath, id)
	info, err := os.Stat(fullPath)
	if err != nil {
		return nil, fmt.Errorf("failed to stat file: %w", err)
	}

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
		content, err := s.fs.ReadFile(relPath)
		if err != nil {
			continue
		}

		fullPath := filepath.Join(workspace.RootPath, relPath)
		info, err := os.Stat(fullPath)
		if err != nil {
			continue
		}

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

func (s *NoteService) SaveNote(note *domain.Note) error {
	note.ModifiedAt = time.Now()
	content := s.serializeNote(note)
	return s.fs.WriteFile(note.Path, content)
}

// DeleteNote removes a note from the workspace.
func (s *NoteService) DeleteNote(id string) error {
	return s.fs.DeleteFile(id)
}

func (s *NoteService) CreateNote(title, folder string) (*domain.Note, error) {
	filename := sanitizeFilename(title) + ".md"
	relPath := filename
	if folder != "" {
		relPath = filepath.Join(folder, filename)
	}

	if _, err := s.fs.ReadFile(relPath); err == nil {
		return nil, &domain.ErrAlreadyExists{Resource: "note", ID: relPath}
	}

	content := "# " + title + "\n\n"

	now := time.Now()
	note := &domain.Note{
		ID:          relPath,
		Title:       title,
		Path:        relPath,
		Content:     content,
		Frontmatter: make(map[string]any),
		Aliases:     []string{},
		Type:        "",
		Blocks:      []domain.Block{},
		Links:       []domain.Link{},
		Tags:        []domain.Tag{},
		CreatedAt:   now,
		ModifiedAt:  now,
	}

	if err := s.SaveNote(note); err != nil {
		return nil, err
	}

	return note, nil
}

// RenderMarkdown converts markdown content to HTML using goldmark.
func (s *NoteService) RenderMarkdown(markdown string) (string, error) {
	var buf bytes.Buffer
	if err := s.parser.Convert([]byte(markdown), &buf); err != nil {
		return "", fmt.Errorf("failed to render markdown: %w", err)
	}
	return buf.String(), nil
}

// parseNote converts raw content into a structured Note.
// It extracts frontmatter, parses Markdown structure, and identifies blocks.
func (s *NoteService) parseNote(id string, content []byte, info os.FileInfo) (*domain.Note, error) {
	frontmatter, body, fields, err := s.extractFrontmatter(content)
	if err != nil {
		return nil, &domain.ErrInvalidFrontmatter{Path: id, Reason: err.Error()}
	}

	title := fields.Title
	if title == "" {
		title = s.extractTitleFromContent(body)
	}
	if title == "" {
		title = strings.TrimSuffix(filepath.Base(id), filepath.Ext(id))
	}

	tags := make([]domain.Tag, 0, len(fields.Tags))
	for _, tagName := range fields.Tags {
		tags = append(tags, domain.Tag{Name: tagName, NoteID: id})
	}

	createdAt := fields.Created
	if createdAt.IsZero() {
		createdAt = info.ModTime()
	}

	modifiedAt := fields.Modified
	if modifiedAt.IsZero() {
		modifiedAt = info.ModTime()
	}

	blocks := s.extractBlocks(id, body)
	note := &domain.Note{
		ID:          id,
		Title:       title,
		Path:        id,
		Content:     string(body),
		Frontmatter: frontmatter,
		Aliases:     fields.Aliases,
		Type:        fields.Type,
		Blocks:      blocks,
		Links:       []domain.Link{},
		Tags:        tags,
		CreatedAt:   createdAt,
		ModifiedAt:  modifiedAt,
	}

	return note, nil
}

// extractFrontmatter parses YAML frontmatter from content and extracts standard fields.
// Returns frontmatter map (without standard fields), body content, and parsed standard fields.
func (s *NoteService) extractFrontmatter(content []byte) (map[string]any, []byte, *frontmatterFields, error) {
	if !bytes.HasPrefix(content, []byte("---\n")) && !bytes.HasPrefix(content, []byte("---\r\n")) {
		return make(map[string]any), content, &frontmatterFields{}, nil
	}

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
		return make(map[string]any), content, &frontmatterFields{}, nil
	}

	fmContent := bytes.Join(lines[1:endIdx], []byte("\n"))
	body := bytes.Join(lines[endIdx+1:], []byte("\n"))

	var rawFrontmatter map[string]any
	if len(fmContent) > 0 {
		if err := yaml.Unmarshal(fmContent, &rawFrontmatter); err != nil {
			return nil, nil, nil, fmt.Errorf("failed to parse frontmatter: %w", err)
		}
	}

	if rawFrontmatter == nil {
		rawFrontmatter = make(map[string]any)
	}

	fields := &frontmatterFields{}

	if title, ok := rawFrontmatter["title"].(string); ok {
		fields.Title = title
		delete(rawFrontmatter, "title")
	}

	if aliases, ok := rawFrontmatter["aliases"]; ok {
		fields.Aliases = parseStringArray(aliases)
		delete(rawFrontmatter, "aliases")
	}

	if noteType, ok := rawFrontmatter["type"].(string); ok {
		fields.Type = noteType
		delete(rawFrontmatter, "type")
	}

	if tags, ok := rawFrontmatter["tags"]; ok {
		fields.Tags = parseStringArray(tags)
		delete(rawFrontmatter, "tags")
	}

	if created, ok := rawFrontmatter["created"]; ok {
		if createdTime, err := parseTime(created); err == nil {
			fields.Created = createdTime
			delete(rawFrontmatter, "created")
		}
	}

	if modified, ok := rawFrontmatter["modified"]; ok {
		if modifiedTime, err := parseTime(modified); err == nil {
			fields.Modified = modifiedTime
			delete(rawFrontmatter, "modified")
		}
	}

	return rawFrontmatter, body, fields, nil
}

// frontmatterFields holds standard frontmatter fields extracted separately from generic fields
type frontmatterFields struct {
	Title    string
	Aliases  []string
	Type     string
	Tags     []string
	Created  time.Time
	Modified time.Time
}

// parseStringArray converts various YAML array formats to []string
func parseStringArray(value any) []string {
	switch v := value.(type) {
	case []any:
		result := make([]string, 0, len(v))
		for _, item := range v {
			if str, ok := item.(string); ok {
				result = append(result, str)
			}
		}
		return result
	case []string:
		return v
	case string:
		return []string{v}
	default:
		return []string{}
	}
}

// parseTime parses various time formats from frontmatter
func parseTime(value any) (time.Time, error) {
	switch v := value.(type) {
	case time.Time:
		return v, nil
	case string:
		formats := []string{
			time.RFC3339,
			"2006-01-02T15:04:05Z07:00",
			"2006-01-02 15:04:05",
			"2006-01-02",
		}
		for _, format := range formats {
			if t, err := time.Parse(format, v); err == nil {
				return t, nil
			}
		}
		return time.Time{}, fmt.Errorf("failed to parse time: %s", v)
	default:
		return time.Time{}, fmt.Errorf("unsupported time type: %T", v)
	}
}

// extractTitleFromContent gets the title from first heading.
func (s *NoteService) extractTitleFromContent(content []byte) string {
	doc := s.parser.Parser().Parse(text.NewReader(content))
	var title string
	ast.Walk(doc, func(n ast.Node, entering bool) (ast.WalkStatus, error) {
		if entering && n.Kind() == ast.KindHeading {
			heading := n.(*ast.Heading)
			if heading.Level == 1 {
				title = nodeText(heading, content)
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
	_, body, fields, err := s.extractFrontmatter(content)
	if err != nil {
		return "", nil
	}

	title := fields.Title
	if title == "" {
		title = s.extractTitleFromContent(body)
	}

	tags := make([]domain.Tag, 0, len(fields.Tags))
	for _, tagName := range fields.Tags {
		tags = append(tags, domain.Tag{Name: tagName})
	}

	return title, tags
}

// extractBlocks parses Markdown content into outline blocks.
// Each paragraph, heading, list item, etc. becomes a separate block.
// Supports Logseq-style block IDs (^block-id at end of line).
func (s *NoteService) extractBlocks(noteID string, content []byte) []domain.Block {
	doc := s.parser.Parser().Parse(text.NewReader(content))

	blocks := []domain.Block{}
	blockIdx := 0
	listDepth := 0
	quoteDepth := 0

	ast.Walk(doc, func(n ast.Node, entering bool) (ast.WalkStatus, error) {
		switch n.Kind() {
		case ast.KindList:
			if entering {
				listDepth++
			} else {
				listDepth--
			}
		case ast.KindBlockquote:
			if entering {
				quoteDepth++
			} else {
				quoteDepth--
			}
		}

		if !entering {
			return ast.WalkContinue, nil
		}

		var blockType domain.BlockType
		var shouldCreate bool
		var level int

		switch n.Kind() {
		case ast.KindParagraph:
			parent := n.Parent()
			if parent != nil && (parent.Kind() == ast.KindListItem || parent.Kind() == ast.KindBlockquote) {
				return ast.WalkContinue, nil
			}
			blockType = domain.BlockTypeParagraph
			shouldCreate = true
			level = quoteDepth
		case ast.KindHeading:
			blockType = domain.BlockTypeHeading
			shouldCreate = true
			level = 0
		case ast.KindListItem:
			blockType = domain.BlockTypeListItem
			shouldCreate = true
			level = listDepth
		case ast.KindFencedCodeBlock, ast.KindCodeBlock:
			blockType = domain.BlockTypeCode
			shouldCreate = true
			level = 0
		case ast.KindBlockquote:
			blockType = domain.BlockTypeQuote
			shouldCreate = true
			level = quoteDepth
		}

		if shouldCreate {
			blockContent := nodeText(n, content)
			blockID, cleanContent := parseBlockID(blockContent)

			blocks = append(blocks, domain.Block{
				ID:       blockID,
				NoteID:   noteID,
				Content:  cleanContent,
				Level:    level,
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

	completeFrontmatter := make(map[string]any)

	for k, v := range note.Frontmatter {
		completeFrontmatter[k] = v
	}

	if note.Title != "" {
		completeFrontmatter["title"] = note.Title
	}

	if len(note.Aliases) > 0 {
		completeFrontmatter["aliases"] = note.Aliases
	}

	if note.Type != "" {
		completeFrontmatter["type"] = note.Type
	}

	if len(note.Tags) > 0 {
		tagNames := make([]string, 0, len(note.Tags))
		for _, tag := range note.Tags {
			tagNames = append(tagNames, tag.Name)
		}
		completeFrontmatter["tags"] = tagNames
	}

	if !note.CreatedAt.IsZero() {
		completeFrontmatter["created"] = note.CreatedAt.Format(time.RFC3339)
	}

	if !note.ModifiedAt.IsZero() {
		completeFrontmatter["modified"] = note.ModifiedAt.Format(time.RFC3339)
	}

	if len(completeFrontmatter) > 0 {
		buf.WriteString("---\n")

		fmBytes, err := yaml.Marshal(completeFrontmatter)
		if err == nil {
			buf.Write(fmBytes)
		}
		buf.WriteString("---\n\n")
	}

	buf.WriteString(note.Content)

	return buf.Bytes()
}

// sanitizeFilename converts a title to a valid filename.
func sanitizeFilename(title string) string {
	invalid := []string{"/", "\\", ":", "*", "?", "\"", "<", ">", "|"}
	filename := title
	for _, char := range invalid {
		filename = strings.ReplaceAll(filename, char, "-")
	}

	filename = strings.TrimSpace(filename)
	filename = strings.Trim(filename, ".")

	if len(filename) > 200 {
		filename = filename[:200]
	}

	if filename == "" {
		filename = "Untitled"
	}

	return filename
}

// parseBlockID extracts a Logseq-style block ID from content (^block-id at end of line).
// Returns the block ID and content with the ID marker removed.
// If no ID is found, generates a new one.
func parseBlockID(content string) (string, string) {
	trimmed := strings.TrimSpace(content)

	if len(trimmed) > 1 && trimmed[0] == '^' {
		potentialID := trimmed[1:]
		if isValidBlockID(potentialID) {
			return potentialID, ""
		}
	}

	if len(trimmed) > 2 && strings.Contains(trimmed, " ^") {
		lastSpaceIdx := strings.LastIndex(trimmed, " ^")
		potentialID := trimmed[lastSpaceIdx+2:] // +2 to skip " ^"

		if isValidBlockID(potentialID) {
			cleanContent := strings.TrimSpace(trimmed[:lastSpaceIdx])
			return potentialID, cleanContent
		}
	}

	return generateBlockID(), content
}

// isValidBlockID checks if a string is a valid block ID.
// Valid IDs contain only alphanumeric characters, dashes, and underscores.
func isValidBlockID(id string) bool {
	if len(id) == 0 {
		return false
	}

	for _, ch := range id {
		if !((ch >= 'a' && ch <= 'z') ||
			(ch >= 'A' && ch <= 'Z') ||
			(ch >= '0' && ch <= '9') ||
			ch == '-' || ch == '_') {
			return false
		}
	}

	return true
}

// generateBlockID creates a unique identifier for a block using UUID v4.
func generateBlockID() string {
	return uuid.New().String()
}

// nodeText extracts text content from an AST node by walking its children.
func nodeText(n ast.Node, source []byte) string {
	var buf bytes.Buffer
	ast.Walk(n, func(node ast.Node, entering bool) (ast.WalkStatus, error) {
		if entering {
			switch v := node.(type) {
			case *ast.Text:
				buf.Write(v.Segment.Value(source))
				if v.SoftLineBreak() {
					buf.WriteByte('\n')
				}
			case *ast.String:
				buf.Write(v.Value)
			}
		}
		return ast.WalkContinue, nil
	})
	return buf.String()
}
