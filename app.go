package main

import (
	"context"
	"fmt"
	"regexp"
	"strings"
	"time"

	"notes/backend/domain"
	"notes/backend/paths"
	"notes/backend/service"

	"github.com/wailsapp/wails/v2/pkg/runtime"
)

// App struct holds application services and state.
// Services are initialized during startup and exposed to the frontend via Wails bindings.
type App struct {
	ctx                       context.Context
	fs                        *service.FilesystemService
	notes                     *service.NoteService
	graph                     *service.GraphService
	search                    *service.SearchService
	tasks                     *service.TaskService
	themes                    *service.ThemeService
	stores                    *service.Stores
	indexing                  bool
	userConfigDir             string
	currentWorkspaceConfigDir string
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
	themes := service.NewThemeService()

	stores, err := service.NewStores("notes", "default", nil)
	if err != nil {
		panic(fmt.Sprintf("failed to create stores: %v", err))
	}

	tasks := service.NewTaskService(stores.Task)

	return &App{
		fs:     fs,
		notes:  notes,
		graph:  graph,
		search: search,
		tasks:  tasks,
		themes: themes,
		stores: stores,
	}
}

// startup is called when the app starts. The context is saved so we can call the runtime methods.
// Service initialization order: attach logger contexts -> initialize user config paths.
func (a *App) startup(ctx context.Context) {
	a.ctx = ctx
	a.logInfo("Application starting")

	if a.fs != nil {
		a.fs.SetLogger(ctx)
	}
	if a.tasks != nil {
		a.tasks.SetLogger(ctx)
	}

	userConfigDir, err := paths.UserConfigDir("KnowledgeLab")
	if err != nil {
		a.logWarning("failed to initialize user config directory: %v", err)
	} else {
		a.userConfigDir = userConfigDir
		a.logInfo("User config directory initialized path=%s", userConfigDir)
	}

	a.logInfo("Application startup complete")
}

// shutdown is called when the app is closing.
// Resource cleanup order: filesystem service -> stores (database).
func (a *App) shutdown(ctx context.Context) {
	a.logInfo("Application shutdown initiated")

	if a.fs != nil {
		start := time.Now()
		a.fs.Close()
		elapsed := time.Since(start)
		a.logInfo("Filesystem closed (%dms)", elapsed.Milliseconds())
	}

	if a.stores != nil {
		a.stores.Close(nil)
	}

	a.logInfo("Application shutdown complete")
}

// CreateNewWorkspace scaffolds a new workspace at the selected directory path.
// Creates the workspace directory, adds a welcome tutorial note, and opens the workspace.
func (a *App) CreateNewWorkspace() (*domain.WorkspaceInfo, error) {
	path, err := runtime.OpenDirectoryDialog(a.ctx, runtime.OpenDialogOptions{
		Title:                      "Select Folder for New Workspace",
		CanCreateDirectories:       true,
		TreatPackagesAsDirectories: false,
	})
	if err != nil {
		return nil, a.wrapError("failed to open directory picker", err)
	}

	if path == "" {
		return nil, nil
	}

	if err := a.notes.ScaffoldWorkspace(path); err != nil {
		return nil, a.wrapError("failed to scaffold workspace", err)
	}

	return a.OpenWorkspace(path)
}

// OpenWorkspace opens a workspace at the specified path and builds the initial index.
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
// After saving, the note is re-indexed for search, graph, and task updates.
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

	tasks := a.notes.ExtractTasks(note.ID, note.Path, []byte(note.Content))
	if err := a.tasks.IndexNote(note.ID, note.Path, tasks, note.ModifiedAt); err != nil {
		return a.wrapError("failed to index tasks", err)
	}

	return nil
}

// DeleteNote removes a note from the workspace.
// The note is removed from filesystem, graph, search, and task indexes.
func (a *App) DeleteNote(id string) error {
	if err := a.notes.DeleteNote(id); err != nil {
		return a.wrapError("failed to delete note", err)
	}

	a.graph.RemoveNote(id)
	a.search.RemoveNote(id)

	if err := a.tasks.RemoveNote(id); err != nil {
		return a.wrapError("failed to remove tasks", err)
	}

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

	tasks := a.notes.ExtractTasks(note.ID, note.Path, []byte(note.Content))
	if err := a.tasks.IndexNote(note.ID, note.Path, tasks, note.ModifiedAt); err != nil {
		return nil, a.wrapError("failed to index tasks", err)
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

// GetAllTagsWithCounts returns all tags with occurrence counts and note IDs.
// Results are sorted by tag name.
func (a *App) GetAllTagsWithCounts() ([]domain.TagInfo, error) {
	tagInfos := a.graph.GetAllTagsWithCounts()
	return tagInfos, nil
}

// GetTagInfo returns information about a specific tag including count and note IDs.
func (a *App) GetTagInfo(tagName string) (*domain.TagInfo, error) {
	tagInfo := a.graph.GetTagInfo(tagName)
	if tagInfo == nil {
		return nil, &domain.ErrNotFound{Resource: "tag", ID: tagName}
	}
	return tagInfo, nil
}

// RenderMarkdown converts markdown content to HTML.
// Used by the frontend for preview mode rendering.
func (a *App) RenderMarkdown(markdown string) (string, error) {
	html, err := a.notes.RenderMarkdown(markdown)
	if err != nil {
		return "", a.wrapError("failed to render markdown", err)
	}
	return html, nil
}

// buildInitialIndex loads all notes and builds graph and search indexes.
func (a *App) buildInitialIndex() {
	a.indexing = true
	defer func() { a.indexing = false }()

	overallStart := time.Now()
	a.logInfo("Starting initial workspace index build")

	listStart := time.Now()
	summaries, err := a.notes.ListNotes()
	if err != nil {
		a.logError("failed to list notes during indexing: %v", err)
		return
	}
	a.logInfo("Listed %d notes (%dms)", len(summaries), time.Since(listStart).Milliseconds())

	noteLoadStart := time.Now()
	notes := make([]domain.Note, 0, len(summaries))
	for i, summary := range summaries {
		note, err := a.notes.GetNote(summary.ID)
		if err != nil {
			a.logWarning("failed to load note %s: %v", summary.ID, err)
			continue
		}

		if err := a.graph.IndexNote(note); err != nil {
			a.logWarning("failed to index note %s in graph: %v", summary.ID, err)
		}

		tasks := a.notes.ExtractTasks(note.ID, note.Path, []byte(note.Content))
		if err := a.tasks.IndexNote(note.ID, note.Path, tasks, note.ModifiedAt); err != nil {
			a.logWarning("failed to index tasks for note %s: %v", summary.ID, err)
		}

		notes = append(notes, *note)

		if (i+1)%50 == 0 {
			a.logInfo("Indexed %d/%d notes...", i+1, len(summaries))
		}
	}
	a.logInfo("Loaded and indexed notes (%dms)", time.Since(noteLoadStart).Milliseconds())

	searchStart := time.Now()
	if err := a.search.IndexAll(notes); err != nil {
		a.logError("failed to build search index: %v", err)
	} else {
		a.logInfo("Search index built (%dms)", time.Since(searchStart).Milliseconds())
	}

	a.logInfo("Initial index build complete: indexed %d notes (%dms total)", len(notes), time.Since(overallStart).Milliseconds())
}

// SelectDirectory opens a native directory picker dialog.
// Returns the selected directory path or empty string if cancelled.
func (a *App) SelectDirectory(title string) (string, error) {
	return runtime.OpenDirectoryDialog(a.ctx, runtime.OpenDialogOptions{
		Title: title,
	})
}

// SelectFile opens a native file picker dialog.
// Returns the selected file path or empty string if cancelled.
func (a *App) SelectFile(title string, filters []runtime.FileFilter) (string, error) {
	return runtime.OpenFileDialog(a.ctx, runtime.OpenDialogOptions{
		Title:   title,
		Filters: filters,
	})
}

// SelectFiles opens a native file picker dialog for multiple files.
// Returns the selected file paths or empty slice if cancelled.
func (a *App) SelectFiles(title string, filters []runtime.FileFilter) ([]string, error) {
	return runtime.OpenMultipleFilesDialog(a.ctx, runtime.OpenDialogOptions{
		Title:   title,
		Filters: filters,
	})
}

// SaveFile opens a native save file dialog.
// Returns the selected save path or empty string if cancelled.
func (a *App) SaveFile(title, defaultFilename string, filters []runtime.FileFilter) (string, error) {
	return runtime.SaveFileDialog(a.ctx, runtime.SaveDialogOptions{
		Title:           title,
		DefaultFilename: defaultFilename,
		Filters:         filters,
	})
}

// ShowMessage displays a message dialog to the user.
// Returns the user's response (e.g., "Yes", "No", "OK", "Cancel").
func (a *App) ShowMessage(title, message string, dialogType runtime.DialogType) (string, error) {
	return runtime.MessageDialog(a.ctx, runtime.MessageDialogOptions{
		Type:    dialogType,
		Title:   title,
		Message: message,
	})
}

// LoadSettings loads application-wide settings from disk.
func (a *App) LoadSettings() (*service.Settings, error) {
	settings, err := a.stores.Workspace.LoadSettings()
	if err != nil {
		return nil, a.wrapError("failed to load settings", err)
	}
	return &settings, nil
}

// SaveSettings saves application-wide settings to disk.
func (a *App) SaveSettings(settings service.Settings) error {
	if err := a.stores.Workspace.SaveSettings(settings); err != nil {
		return a.wrapError("failed to save settings", err)
	}
	return nil
}

// LoadWorkspaceSnapshot loads workspace-specific UI state from disk.
func (a *App) LoadWorkspaceSnapshot() (*service.WorkspaceSnapshot, error) {
	snapshot, err := a.stores.Workspace.LoadSnapshot()
	if err != nil {
		return nil, a.wrapError("failed to load workspace snapshot", err)
	}
	return &snapshot, nil
}

// SaveWorkspaceSnapshot saves workspace-specific UI state to disk.
// Frontend should debounce calls (500-1000ms) to avoid excessive writes.
func (a *App) SaveWorkspaceSnapshot(snapshot service.WorkspaceSnapshot) error {
	if err := a.stores.Workspace.SaveSnapshot(snapshot); err != nil {
		return a.wrapError("failed to save workspace snapshot", err)
	}
	return nil
}

// ClearRecentFiles removes all recent pages from the workspace snapshot and persists the change.
func (a *App) ClearRecentFiles() (*service.WorkspaceSnapshot, error) {
	snapshot, err := a.stores.Workspace.LoadSnapshot()
	if err != nil {
		return nil, a.wrapError("failed to load workspace snapshot", err)
	}

	snapshot.UI.RecentPages = []string{}
	snapshot.UI.ActivePage = ""

	if err := a.stores.Workspace.SaveSnapshot(snapshot); err != nil {
		return nil, a.wrapError("failed to save workspace snapshot", err)
	}

	return &snapshot, nil
}

// LoadAppSnapshot loads application-wide state from disk.
// Contains global app state like the last opened workspace path.
func (a *App) LoadAppSnapshot() (*service.AppSnapshot, error) {
	snapshot, err := a.stores.Workspace.LoadAppSnapshot()
	if err != nil {
		return nil, a.wrapError("failed to load app snapshot", err)
	}
	return &snapshot, nil
}

// SaveAppSnapshot saves application-wide state to disk.
// Frontend should debounce calls (500-1000ms) to avoid excessive writes.
func (a *App) SaveAppSnapshot(snapshot service.AppSnapshot) error {
	if err := a.stores.Workspace.SaveAppSnapshot(snapshot); err != nil {
		return a.wrapError("failed to save app snapshot", err)
	}
	return nil
}

// CloseWorkspace closes the current workspace and cleans up resources.
// Should be called before opening a new workspace or when the user explicitly closes the workspace.
func (a *App) CloseWorkspace() error {
	a.logInfo("Closing workspace")

	a.indexing = false

	if a.fs != nil {
		if err := a.fs.Close(); err != nil {
			a.logWarning("Error closing filesystem service: %v", err)
		}
	}

	a.logInfo("Workspace closed")
	return nil
}

// GetUserConfigDir returns the user-level configuration directory path.
// This directory contains global application settings and per-workspace metadata.
func (a *App) GetUserConfigDir() string {
	return a.userConfigDir
}

// InitWorkspaceConfigDir initializes and returns the workspace-level configuration directory.
// This directory lives inside the workspace root and can be committed to version control.
// Creates the directory if it doesn't exist.
func (a *App) InitWorkspaceConfigDir(workspaceRoot string) (string, error) {
	configDir, err := paths.WorkspaceConfigDir(workspaceRoot, "KnowledgeLab")
	if err != nil {
		return "", a.wrapError("failed to initialize workspace config directory", err)
	}

	a.currentWorkspaceConfigDir = configDir
	return configDir, nil
}

// GetAllTasks returns all tasks across all notes, optionally filtered.
// Supports filtering by completion status, note ID, and date ranges.
func (a *App) GetAllTasks(filter domain.TaskFilter) (*domain.TaskInfo, error) {
	taskInfo, err := a.tasks.GetAllTasks(filter)
	if err != nil {
		return nil, a.wrapError("failed to get tasks", err)
	}
	return &taskInfo, nil
}

// GetTasksForNote returns all tasks in a specific note.
func (a *App) GetTasksForNote(noteID string) ([]domain.Task, error) {
	tasks, err := a.tasks.GetTasksForNote(noteID)
	if err != nil {
		return nil, a.wrapError("failed to get tasks for note", err)
	}
	return tasks, nil
}

// ToggleTaskInNote toggles a task's completion status at the specified line number.
// Re-parses and re-indexes the note after the toggle.
func (a *App) ToggleTaskInNote(noteID string, lineNumber int) error {
	note, err := a.notes.GetNote(noteID)
	if err != nil {
		return a.wrapError("failed to get note", err)
	}

	lines := strings.Split(note.Content, "\n")
	if lineNumber < 0 || lineNumber >= len(lines) {
		return a.wrapError("invalid line number", fmt.Errorf("line %d out of range", lineNumber))
	}

	line := strings.TrimSpace(lines[lineNumber])
	taskPattern := regexp.MustCompile(`^-\s+\[([ xX])\]\s+(.*)$`)

	if matches := taskPattern.FindStringSubmatch(line); matches != nil {
		checkboxState := matches[1]
		taskContent := matches[2]

		indent := ""
		for i := 0; i < len(lines[lineNumber]); i++ {
			if lines[lineNumber][i] == ' ' || lines[lineNumber][i] == '\t' {
				indent += string(lines[lineNumber][i])
			} else {
				break
			}
		}

		if checkboxState == " " {
			lines[lineNumber] = indent + "- [x] " + taskContent
		} else {
			lines[lineNumber] = indent + "- [ ] " + taskContent
		}

		note.Content = strings.Join(lines, "\n")
		return a.SaveNote(note)
	}
	return a.wrapError("line is not a task", fmt.Errorf("line %d does not contain a task", lineNumber))
}

// ListThemes returns a list of all available theme slugs.
func (a *App) ListThemes() ([]string, error) {
	return a.themes.ListThemes()
}

// LoadTheme loads a theme by its slug and returns the full theme data.
func (a *App) LoadTheme(slug string) (*domain.Base16Theme, error) {
	return a.themes.LoadTheme(slug)
}

// GetDefaultTheme returns the default theme.
func (a *App) GetDefaultTheme() (*domain.Base16Theme, error) {
	return a.themes.GetDefaultTheme()
}

// SaveCustomTheme saves a custom theme to a YAML file.
// Opens a save file dialog for the user to choose the destination.
func (a *App) SaveCustomTheme(theme *domain.Base16Theme, suggestedFilename string) (string, error) {
	if suggestedFilename == "" {
		suggestedFilename = fmt.Sprintf("%s-custom.yaml", theme.Slug)
	}

	filepath, err := runtime.SaveFileDialog(a.ctx, runtime.SaveDialogOptions{
		DefaultFilename: suggestedFilename,
		Title:           "Save Custom Theme",
		Filters: []runtime.FileFilter{
			{
				DisplayName: "YAML Files (*.yaml, *.yml)",
				Pattern:     "*.yaml;*.yml",
			},
		},
	})

	if err != nil {
		return "", fmt.Errorf("failed to open save dialog: %w", err)
	}

	if filepath == "" {
		return "", fmt.Errorf("save cancelled")
	}

	savedPath, err := a.themes.SaveCustomTheme(theme, filepath)
	if err != nil {
		return "", err
	}

	a.logInfo("Custom theme saved path=%s", savedPath)
	return savedPath, nil
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

func (a *App) logInfo(format string, args ...any) {
	if a.ctx != nil {
		runtime.LogInfof(a.ctx, format, args...)
		return
	}
	fmt.Printf("INFO: "+format+"\n", args...)
}

func (a *App) logWarning(format string, args ...any) {
	if a.ctx != nil {
		runtime.LogWarningf(a.ctx, format, args...)
		return
	}
	fmt.Printf("WARN: "+format+"\n", args...)
}

func (a *App) logError(format string, args ...any) {
	if a.ctx != nil {
		runtime.LogErrorf(a.ctx, format, args...)
		return
	}
	fmt.Printf("ERROR: "+format+"\n", args...)
}
