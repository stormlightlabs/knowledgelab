package domain

import "time"

// Note represents a single note/document in the workspace.
// Notes are stored as Markdown files on disk and may contain frontmatter, wikilinks, tags, and outline blocks.
type Note struct {
	ID          string         `json:"id"`                          // Unique identifier (typically file path relative to workspace)
	Title       string         `json:"title"`                       // Note title (from frontmatter or first heading)
	Path        string         `json:"path"`                        // Relative path within workspace
	Content     string         `json:"content"`                     // Full Markdown content
	Frontmatter map[string]any `json:"frontmatter"`                 // Additional YAML frontmatter fields (beyond standard fields)
	Aliases     []string       `json:"aliases"`                     // Alternative note titles for wikilink resolution
	Type        string         `json:"type"`                        // Note type or template identifier (e.g., "daily", "meeting", "project")
	Blocks      []Block        `json:"blocks"`                      // Outline-style blocks
	Links       []Link         `json:"links"`                       // Outgoing links found in content
	Tags        []Tag          `json:"tags"`                        // Tags found in content and frontmatter
	CreatedAt   time.Time      `json:"createdAt" ts_type:"string"`  // Note creation time (from frontmatter or file metadata)
	ModifiedAt  time.Time      `json:"modifiedAt" ts_type:"string"` // Last modification time (auto-updated on save)
}

// Block represents an outline-style content block within a note.
// Blocks enable outline editing where each block can be independently referenced and linked.
type Block struct {
	ID       string    `json:"id"`       // Unique block identifier
	NoteID   string    `json:"noteId"`   // Parent note ID
	Content  string    `json:"content"`  // Block content (single paragraph/list item)
	Level    int       `json:"level"`    // Nesting level (0 = top-level)
	Parent   string    `json:"parent"`   // Parent block ID (empty for top-level)
	Children []string  `json:"children"` // Child block IDs
	Position int       `json:"position"` // Position within parent
	Type     BlockType `json:"type"`     // Block type (paragraph, heading, list, etc.)
}

// BlockType categorizes different types of outline blocks.
type BlockType string

const (
	BlockTypeParagraph BlockType = "paragraph"
	BlockTypeHeading   BlockType = "heading"
	BlockTypeListItem  BlockType = "list-item"
	BlockTypeCode      BlockType = "code"
	BlockTypeQuote     BlockType = "quote"
)

// Link represents a connection between notes.
// Supports both wikilinks ([[target]]) and standard Markdown links.
type Link struct {
	Source      string   `json:"source"`      // Source note ID
	Target      string   `json:"target"`      // Target note ID or reference
	DisplayText string   `json:"displayText"` // Link display text
	Type        LinkType `json:"type"`        // Link type
	BlockRef    string   `json:"blockRef"`    // Optional block reference (e.g., [[note#block]])
}

// LinkType categorizes different types of links.
type LinkType string

const (
	LinkTypeWiki     LinkType = "wiki"     // [[wikilink]]
	LinkTypeMarkdown LinkType = "markdown" // [text](url)
	LinkTypeEmbed    LinkType = "embed"    // ![[embed]]
	LinkTypeBlock    LinkType = "block"    // [[note#block]] or [[#block]]
)

// Tag represents a topic or category marker.
// Tags can appear in content (#tag) or in frontmatter.
type Tag struct {
	Name   string `json:"name"`   // Tag name (without #)
	NoteID string `json:"noteId"` // Note containing this tag
}

// TagInfo provides aggregated information about a tag across the workspace.
// Used for tag browsing, filtering, and displaying tag statistics.
type TagInfo struct {
	Name    string   `json:"name"`    // Tag name (without #)
	Count   int      `json:"count"`   // Number of notes containing this tag
	NoteIDs []string `json:"noteIds"` // IDs of notes containing this tag
}

// DailyNote represents a date-based journal entry.
// Daily notes follow a naming convention (e.g., "2025-01-27.md")
// and provide quick access to journaling workflows.
type DailyNote struct {
	Date   time.Time `json:"date" ts_type:"string"` // Journal date
	NoteID string    `json:"noteId"`                // Associated note ID
}

// Workspace represents a workspace configuration.
// A workspace is a directory containing Markdown notes with associated metadata, configuration, and ignore patterns.
type Workspace struct {
	ID             string    `json:"id"`                            // Unique workspace identifier
	Name           string    `json:"name"`                          // Human-readable workspace name
	RootPath       string    `json:"rootPath"`                      // Absolute path to workspace root directory
	IgnorePatterns []string  `json:"ignorePatterns"`                // File patterns to ignore (e.g., .git, node_modules)
	CreatedAt      time.Time `json:"createdAt" ts_type:"string"`    // Workspace creation time
	LastOpenedAt   time.Time `json:"lastOpenedAt" ts_type:"string"` // Last time workspace was opened
}

// WorkspaceConfig holds workspace-specific settings and preferences.
type WorkspaceConfig struct {
	DailyNoteFormat string   `json:"dailyNoteFormat"` // Date format for daily notes (e.g., "2006-01-02")
	DailyNoteFolder string   `json:"dailyNoteFolder"` // Folder for daily notes (empty = workspace root)
	DefaultTags     []string `json:"defaultTags"`     // Tags to auto-add to new notes
}

// NoteSummary provides a lightweight note representation for lists and indexes.
// Used when loading all notes to avoid loading full content into memory.
type NoteSummary struct {
	ID         string    `json:"id"`
	Title      string    `json:"title"`
	Path       string    `json:"path"`
	Tags       []Tag     `json:"tags"`
	ModifiedAt time.Time `json:"modifiedAt" ts_type:"string"`
}

// WorkspaceInfo provides basic workspace information.
type WorkspaceInfo struct {
	Workspace   Workspace       `json:"workspace"`
	Config      WorkspaceConfig `json:"config"`
	NoteCount   int             `json:"noteCount"`
	TotalBlocks int             `json:"totalBlocks"`
}

// Task represents a task item parsed from markdown checkbox syntax.
// Tasks are list items with `- [ ]` (unchecked) or `- [x]` (completed) markers.
// Tasks reuse the block infrastructure and are stored with metadata in SQLite.
type Task struct {
	ID          string     `json:"id"`                           // Task identifier (reuses block ID)
	BlockID     string     `json:"blockId"`                      // Same as ID for consistency
	NoteID      string     `json:"noteId"`                       // Parent note ID
	NotePath    string     `json:"notePath"`                     // Relative path of containing note
	Content     string     `json:"content"`                      // Task text without checkbox marker
	IsCompleted bool       `json:"isCompleted"`                  // Completion status
	CreatedAt   time.Time  `json:"createdAt" ts_type:"string"`   // When task was first created
	CompletedAt *time.Time `json:"completedAt" ts_type:"string"` // When task was completed (nil if pending)
	LineNumber  int        `json:"lineNumber"`                   // Line number in note (0-indexed)
}

// TaskFilter specifies criteria for filtering tasks.
// All filter fields are optional (nil/empty means no filter on that criterion).
type TaskFilter struct {
	Status             *bool      `json:"status"`             // nil = all, true = completed, false = pending
	NoteID             string     `json:"noteId"`             // Filter by specific note ID
	CreatedAfter       *time.Time `json:"createdAfter"`       // Tasks created after this time
	CreatedBefore      *time.Time `json:"createdBefore"`      // Tasks created before this time
	CompletedAfter     *time.Time `json:"completedAfter"`     // Tasks completed after this time
	CompletedBefore    *time.Time `json:"completedBefore"`    // Tasks completed before this time
	NoteModifiedAfter  *time.Time `json:"noteModifiedAfter"`  // Filter by note modified date (after)
	NoteModifiedBefore *time.Time `json:"noteModifiedBefore"` // Filter by note modified date (before)
}

// TaskInfo provides aggregated task statistics and data.
// Used for task panel display and summary views.
type TaskInfo struct {
	Tasks          []Task `json:"tasks"`          // Filtered task list
	TotalCount     int    `json:"totalCount"`     // Total tasks matching filter
	CompletedCount int    `json:"completedCount"` // Number of completed tasks
	PendingCount   int    `json:"pendingCount"`   // Number of pending tasks
}
