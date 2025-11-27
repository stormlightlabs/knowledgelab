package main

import (
	"context"
	"fmt"

	"notes/backend/domain"
	"notes/backend/service"
)

// App struct holds application services and state.
// Services are initialized during startup and exposed to the frontend via Wails bindings.
type App struct {
	ctx      context.Context
	fs       *service.FilesystemService
	notes    *service.NoteService
	graph    *service.GraphService
	search   *service.SearchService
	indexing bool
}

// NewApp creates a new App application struct with all services initialized.
func NewApp() *App {
	fs, err := service.NewFilesystemService()
	if err != nil {
		panic(fmt.Sprintf("failed to create filesystem service: %v", err))
	}

	notes := service.NewNoteService(fs)
	graph := service.NewGraphService()
	search := service.NewSearchService()

	return &App{
		fs:     fs,
		notes:  notes,
		graph:  graph,
		search: search,
	}
}

// startup is called when the app starts. The context is saved
// so we can call the runtime methods
func (a *App) startup(ctx context.Context) {
	a.ctx = ctx
}

// shutdown is called when the app is closing
func (a *App) shutdown(ctx context.Context) {
	if a.fs != nil {
		a.fs.Close()
	}
}

// OpenWorkspace opens a workspace at the specified path and builds the initial index.
// Returns workspace information including note count and configuration.
func (a *App) OpenWorkspace(path string) (*domain.WorkspaceInfo, error) {
	info, err := a.fs.OpenWorkspace(path)
	if err != nil {
		return nil, a.wrapError("failed to open workspace", err)
	}

	go a.buildInitialIndex()

	return info, nil
}

// ListNotes returns a summary of all notes in the current workspace.
// Summaries include basic metadata without full content for performance.
func (a *App) ListNotes() ([]domain.NoteSummary, error) {
	summaries, err := a.notes.ListNotes()
	if err != nil {
		return nil, a.wrapError("failed to list notes", err)
	}

	return summaries, nil
}

// GetNote retrieves the full content and metadata for a specific note by ID.
// The ID is the note's relative path within the workspace.
func (a *App) GetNote(id string) (*domain.Note, error) {
	note, err := a.notes.GetNote(id)
	if err != nil {
		return nil, a.wrapError("failed to get note", err)
	}

	return note, nil
}

// SaveNote creates or updates a note in the workspace.
// After saving, the note is re-indexed for search and graph updates.
func (a *App) SaveNote(note *domain.Note) error {
	if err := a.notes.SaveNote(note); err != nil {
		return a.wrapError("failed to save note", err)
	}

	if err := a.graph.IndexNote(note); err != nil {
		return a.wrapError("failed to index note in graph", err)
	}

	if err := a.search.IndexNote(note); err != nil {
		return a.wrapError("failed to index note in search", err)
	}

	return nil
}

// DeleteNote removes a note from the workspace.
// The note is removed from filesystem, graph, and search indexes.
func (a *App) DeleteNote(id string) error {
	if err := a.notes.DeleteNote(id); err != nil {
		return a.wrapError("failed to delete note", err)
	}

	a.graph.RemoveNote(id)
	a.search.RemoveNote(id)

	return nil
}

// CreateNote creates a new note with the specified title in an optional folder.
// Returns the created note with generated ID and default content.
func (a *App) CreateNote(title, folder string) (*domain.Note, error) {
	note, err := a.notes.CreateNote(title, folder)
	if err != nil {
		return nil, a.wrapError("failed to create note", err)
	}

	if err := a.graph.IndexNote(note); err != nil {
		return nil, a.wrapError("failed to index new note in graph", err)
	}

	if err := a.search.IndexNote(note); err != nil {
		return nil, a.wrapError("failed to index new note in search", err)
	}

	return note, nil
}

// GetBacklinks returns all notes that link to the specified note.
// Used to display backlinks panel in the UI.
func (a *App) GetBacklinks(noteID string) ([]domain.Link, error) {
	links := a.graph.GetBacklinks(noteID)
	return links, nil
}

// GetGraph returns the complete note graph structure.
// Includes all notes as nodes and links as edges.
func (a *App) GetGraph() (*service.Graph, error) {
	graph := a.graph.GetGraph()
	return graph, nil
}

// Search performs a full-text search with optional filters.
// Supports filtering by tags, path prefix, and date range.
func (a *App) Search(query service.SearchQuery) ([]service.SearchResult, error) {
	results, err := a.search.Search(query)
	if err != nil {
		return nil, a.wrapError("failed to search", err)
	}

	return results, nil
}

// GetNotesWithTag returns all notes that contain the specified tag.
func (a *App) GetNotesWithTag(tagName string) ([]string, error) {
	noteIDs := a.graph.GetNotesWithTag(tagName)
	return noteIDs, nil
}

// GetAllTags returns all unique tags across all notes in the workspace.
func (a *App) GetAllTags() ([]string, error) {
	tags := a.graph.GetAllTags()
	return tags, nil
}

// buildInitialIndex loads all notes and builds graph and search indexes.
// Runs asynchronously after workspace is opened.
func (a *App) buildInitialIndex() {
	a.indexing = true
	defer func() { a.indexing = false }()

	// Get all notes
	summaries, err := a.notes.ListNotes()
	if err != nil {
		fmt.Printf("Failed to list notes during indexing: %v\n", err)
		return
	}

	// Load and index each note
	notes := make([]domain.Note, 0, len(summaries))
	for _, summary := range summaries {
		note, err := a.notes.GetNote(summary.ID)
		if err != nil {
			fmt.Printf("Failed to load note %s: %v\n", summary.ID, err)
			continue
		}

		if err := a.graph.IndexNote(note); err != nil {
			fmt.Printf("Failed to index note %s in graph: %v\n", summary.ID, err)
		}

		notes = append(notes, *note)
	}

	if err := a.search.IndexAll(notes); err != nil {
		fmt.Printf("Failed to build search index: %v\n", err)
	}

	fmt.Printf("Indexed %d notes\n", len(notes))
}

// wrapError converts domain errors to user-friendly messages.
// Maps specific error types to appropriate frontend error messages.
func (a *App) wrapError(msg string, err error) error {
	switch e := err.(type) {
	case *domain.ErrNotFound:
		return fmt.Errorf("%s: %s not found: %s", msg, e.Resource, e.ID)
	case *domain.ErrAlreadyExists:
		return fmt.Errorf("%s: %s already exists: %s", msg, e.Resource, e.ID)
	case *domain.ErrInvalidPath:
		return fmt.Errorf("%s: invalid path '%s': %s", msg, e.Path, e.Reason)
	case *domain.ErrInvalidFrontmatter:
		return fmt.Errorf("%s: invalid frontmatter in '%s': %s", msg, e.Path, e.Reason)
	case *domain.ErrWorkspaceNotOpen:
		return fmt.Errorf("%s: no workspace is open", msg)
	default:
		return fmt.Errorf("%s: %w", msg, err)
	}
}
