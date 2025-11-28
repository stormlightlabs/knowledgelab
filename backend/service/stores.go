package service

import (
	"database/sql"
	"fmt"
)

// WorkspaceStore manages application settings and workspace snapshots.
// Wraps AppDirs and provides high-level access to TOML configuration files.
type WorkspaceStore struct {
	dirs *AppDirs
}

// NewWorkspaceStore creates a new WorkspaceStore for the specified workspace.
func NewWorkspaceStore(dirs *AppDirs) *WorkspaceStore {
	return &WorkspaceStore{
		dirs: dirs,
	}
}

// LoadSettings loads application-wide settings from settings.toml.
func (ws *WorkspaceStore) LoadSettings() (Settings, error) {
	return LoadSettings(ws.dirs.SettingsPath)
}

// SaveSettings saves application-wide settings to settings.toml.
func (ws *WorkspaceStore) SaveSettings(settings Settings) error {
	return SaveSettings(ws.dirs.SettingsPath, settings)
}

// LoadSnapshot loads workspace-specific UI state from workspace.toml.
func (ws *WorkspaceStore) LoadSnapshot() (WorkspaceSnapshot, error) {
	return LoadWorkspaceSnapshot(ws.dirs.WorkspacePath)
}

// SaveSnapshot saves workspace-specific UI state to workspace.toml.
// Frontend should debounce calls to this method (500-1000ms recommended).
func (ws *WorkspaceStore) SaveSnapshot(snapshot WorkspaceSnapshot) error {
	return SaveWorkspaceSnapshot(ws.dirs.WorkspacePath, snapshot)
}

// GetDirs returns the AppDirs for direct path access if needed.
func (ws *WorkspaceStore) GetDirs() *AppDirs {
	return ws.dirs
}

// GraphStore manages the graph database for pages, blocks, and links.
// Wraps a SQL database connection and provides high-level graph operations.
type GraphStore struct {
	db *sql.DB
}

// NewGraphStore creates a new GraphStore with an open database connection.
// The database should already have migrations applied.
func NewGraphStore(db *sql.DB) *GraphStore {
	return &GraphStore{
		db: db,
	}
}

// CreatePage inserts a new page into the graph.
func (gs *GraphStore) CreatePage(page Page) error {
	return CreatePage(gs.db, page)
}

// GetPageByID retrieves a page by its ID.
func (gs *GraphStore) GetPageByID(id string) (*Page, error) {
	return GetPageByID(gs.db, id)
}

// GetBlocksForPage retrieves all blocks for a specific page.
func (gs *GraphStore) GetBlocksForPage(pageID string) ([]Block, error) {
	return GetBlocksForPage(gs.db, pageID)
}

// GetBacklinks retrieves all links pointing to a specific page.
func (gs *GraphStore) GetBacklinks(toPageID string) ([]Link, error) {
	return GetBacklinks(gs.db, toPageID)
}

// CreateLink inserts a new link between two pages.
func (gs *GraphStore) CreateLink(link Link) (int, error) {
	return CreateLink(gs.db, link)
}

// DeletePage removes a page and all associated blocks and links.
func (gs *GraphStore) DeletePage(pageID string) error {
	return DeletePage(gs.db, pageID)
}

// Close closes the database connection.
func (gs *GraphStore) Close() error {
	return gs.db.Close()
}

// Stores holds both WorkspaceStore and GraphStore for a workspace.
// Provides a unified interface for all persistence operations.
type Stores struct {
	Workspace *WorkspaceStore
	Graph     *GraphStore
}

// NewStores creates and initializes both WorkspaceStore and GraphStore.
// Creates necessary directories and applies database migrations.
//
// Parameters:
//   - appName: the application name (e.g., "notes")
//   - workspaceName: the workspace identifier (e.g., workspace hash or "default")
//
// Returns an error if directory creation, database opening, or migration fails.
func NewStores(appName, workspaceName string) (*Stores, error) {
	dirs, err := NewAppDirs(appName, workspaceName)
	if err != nil {
		return nil, fmt.Errorf("failed to create app dirs: %w", err)
	}

	if err := dirs.Ensure(); err != nil {
		return nil, fmt.Errorf("failed to ensure directories: %w", err)
	}

	db, err := OpenGraphDB(dirs.DBPath)
	if err != nil {
		return nil, fmt.Errorf("failed to open graph database: %w", err)
	}

	if err := Migrate(db); err != nil {
		db.Close()
		return nil, fmt.Errorf("failed to migrate database: %w", err)
	}

	return &Stores{
		Workspace: NewWorkspaceStore(dirs),
		Graph:     NewGraphStore(db),
	}, nil
}

// Close closes the graph database connection.
// Should be called when the application shuts down.
func (s *Stores) Close() error {
	return s.Graph.Close()
}
