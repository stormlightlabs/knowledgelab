# Tasks

High-level TODO list for the Obsidian / Logseq–style desktop app built with
**F# (Fable + Elmish + React)** and **Wails (Go)**.

The target is a local-first, Markdown- and outline-based PKM with backlinks and
graph view similar to Obsidian and Logseq.

## Backend (Go + Wails)

Implemented complete Go backend with domain types (Note, Block, Link, Tag, Workspace), filesystem service with file watching, note CRUD operations with frontmatter support, graph/index service for wikilink parsing and backlink queries, BM25 full-text search with tag/date/path filtering, Wails-bindable API methods with JSON DTOs for frontend interop, and unit tests covering Markdown parsing, graph construction, and search indexing.

## Frontend (F# Fable + Elmish + React)

Implemented complete F# frontend using Elmish MVU architecture with Model/Msg/update/view, routing and navigation, workspace picker UI, Markdown editor with support for headings/lists/code/wikilinks, filterable note list sidebar, backlinks panel with click-to-navigate, daily notes "Today" button, three-panel layout (sidebar/editor/right panel), minimal settings panel with theme/font controls, D3-force graph view with both SVG and Canvas renderers supporting pan/zoom/tooltips/hover highlighting/click-through navigation, and comprehensive tests for update logic, routing, backlinks, daily notes, and graph interactions.

## PKM & Data Model Parity

### Completed Foundation

Implemented local-first Markdown storage with workspace management, wikilink parsing and indexing, bidirectional backlinks panel, interactive D3-force graph view with SVG/Canvas rendering, daily notes with "Today" button, and BM25-powered full-text search with tag/date/path filtering.

### Obsidian-Compatible Features

#### Tags System

- [ ] Tag parsing:
    - [ ] Parse inline `#tags` from note body.
    - [ ] Parse frontmatter `tags:` field (array or comma-separated).
    - [ ] Support nested tags (`#parent/child`).
- [ ] Tag index:
    - [ ] Build tag index from parsed tags across all notes.
    - [ ] Track tag occurrence counts.
    - [ ] Update index incrementally on note changes.
- [ ] Tag browser UI:
    - [ ] Tag list panel showing all tags with counts.
    - [ ] Click tag to filter note list.
    - [ ] Nested tag tree view for hierarchical tags.
- [ ] Tag-based filtering:
    - [ ] Filter note list by single tag.
    - [ ] Multi-tag filtering with AND/OR logic.
    - [ ] Tag search and autocomplete in search box.

#### Templates

- [ ] Template creation:
    - [ ] Designate templates folder (e.g., `/templates/`).
    - [ ] Parse template files with frontmatter.
- [ ] Template insertion:
    - [ ] Insert template into current note via command/menu.
    - [ ] Template picker UI (list available templates).
- [ ] Template variables:
    - [ ] Support `{{date}}` and `{{time}}` placeholders.
    - [ ] Support `{{title}}` for note title insertion.
    - [ ] Integrate with daily note templates.

#### Core Plugin Architecture

- [ ] Define internal extension points:
    - [ ] Toolbar command registration API.
    - [ ] Sidebar panel registration API.
    - [ ] Note context menu action hooks.
- [ ] Implement plugin registry:
    - [ ] Register built-in features (daily notes, backlinks, graph, search) as internal plugins.
    - [ ] Enable/disable individual plugins via settings.
    - [ ] Plugin lifecycle hooks (init, load, unload).
- [ ] Document plugin constraints:
    - [ ] Write internal extension API documentation.
    - [ ] Define plugin security and sandbox model.
    - [ ] Plan future public plugin API roadmap.

### Logseq-Compatible Features

#### Block-Based Outliner (Light)

- [ ] Block ID support:
    - [ ] Parse Logseq-style block IDs (`^block-id` at end of line).
    - [ ] Generate unique block IDs on demand.
    - [ ] Preserve block IDs round-trip on note edit.
- [ ] Block operations:
    - [ ] Block indentation with Tab key.
    - [ ] Block outdentation with Shift+Tab.
    - [ ] Block-level navigation with Up/Down arrow keys.
    - [ ] Block focus/zoom (collapse siblings, show only focused branch).

#### Task Management (Basic)

- [ ] Task parsing:
    - [ ] Parse `- [ ]` unchecked task syntax.
    - [ ] Parse `- [x]` completed task syntax.
    - [ ] Distinguish tasks from regular list items.
- [ ] Task state tracking:
    - [ ] Toggle task completion in editor (click checkbox or keyboard shortcut).
    - [ ] Track task metadata (created date, completed date).
- [ ] Task views:
    - [ ] Task aggregation panel showing all open tasks across notes.
    - [ ] Filter tasks by completion status.
    - [ ] Filter tasks by note or date range.

### Markdown Dialect & Syntax

#### Frontmatter (YAML)

- [ ] Parse YAML frontmatter:
    - [ ] Extract `---` delimited frontmatter block.
    - [ ] Parse YAML to structured key-value data.
    - [ ] Handle YAML parse errors gracefully with user feedback.
- [ ] Preserve frontmatter on edit:
    - [ ] Round-trip frontmatter without unintended changes.
    - [ ] Update specific frontmatter fields programmatically.
- [ ] Support standard fields:
    - [ ] `aliases` (array of alternative note titles).
    - [ ] `tags` (array of tags, supplementing inline tags).
    - [ ] `type` (note type or template identifier).
    - [ ] `created` and `modified` timestamps.

#### Editor Enhancements

- [ ] Keyboard shortcuts for formatting:
    - [ ] Bold text (`Ctrl/Cmd+B`).
    - [ ] Italic text (`Ctrl/Cmd+I`).
    - [ ] Inline code (`Ctrl/Cmd+E`).
    - [ ] Insert/edit link (`Ctrl/Cmd+K`).
    - [ ] Set heading level (`Ctrl/Cmd+1` through `Ctrl/Cmd+6`).

#### Dialect Specification & Documentation

- [ ] Document Markdown flavor:
    - [ ] Specify CommonMark base with Obsidian/Logseq extensions.
    - [ ] List supported syntax (wikilinks, tags, tasks, frontmatter, block IDs).
- [ ] Document wikilink resolution rules:
    - [ ] Title-based resolution vs. path-based.
    - [ ] Alias handling via frontmatter `aliases` field.
- [ ] Define daily note conventions:
    - [ ] Canonical location (e.g., `/daily/` or `/journal/`).
    - [ ] Naming convention (e.g., `YYYY-MM-DD.md`).
- [ ] Define block ID format:
    - [ ] Syntax: `^[a-z0-9-]+` at end of line.
    - [ ] Serialization and uniqueness constraints.
- [ ] Write import guides:
    - [ ] Obsidian import guide (1:1 features, degraded features, manual steps).
    - [ ] Logseq import guide (Markdown support, Org-mode limitations, block ID handling).

### Advanced Features (Deferred to Post-v1)

The following features are deferred to future releases after v1 launch:

- **Community plugin API**: Public plugin API with marketplace/registry for third-party extensions.
- **Sync/Publish**: Optional cloud-enabled sync and publish features (explicit opt-in, privacy-first design).
- **Advanced block operations**: Block references, block embedding, block-level properties (`key:: value`).
- **Datalog-style queries**: Advanced query language for filtering and aggregating blocks, tasks, tags, and properties.
- **Whiteboards**: Visual whiteboard canvas for spatially arranging notes and blocks.
- **Advanced task states**: Logseq-style task states beyond basic checkbox (`TODO`, `DOING`, `DONE`, `WAITING`, `CANCELLED`).
- **Org-mode compatibility**: Optional Org-mode parsing layer for headings, TODO keywords, scheduled/deadline timestamps.

## Parking Lot

- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Benchmarks for graph build on large workspaces.
- [ ] Add `go test ./...` CI job.
- [ ] Test serialization/deserialization of models used in Wails calls.

## Configuration

Here’s the same milestone broken down as simple task lists:

### App data directories

- [x] `AppDirs` struct (`ConfigRoot`, `WorkspaceRoot`, `SettingsPath`, `WorkspacePath`, `DBPath`).
- [x] Implement `NewAppDirs(appName, workspaceName)` using `os.UserConfigDir` + `filepath.Join`.
- [x] Add `Ensure()` method to create workspace directories with `os.MkdirAll`.
- [x] Add unit tests for path construction (no disk IO where possible).

### `settings.toml` with BurntSushi & `workspace.toml` for UI state

- [x] Define v1 `settings.toml` schema (general + editor sections).
- [x] Implement `Settings` Go struct mirroring the schema.
- [x] Implement `DefaultSettings()` helper.
- [x] Implement `LoadSettings(path) (Settings, error)` using `toml.DecodeFile`.
- [x] Implement `SaveSettings(path, Settings) error` using `toml.NewEncoder`.
- [x] Add round-trip tests for settings
- [x] Define `workspace.toml` schema (active page, sidebar, pinned pages).
- [x] Implement `WorkspaceSnapshot` Go struct.
- [x] Implement `LoadWorkspaceSnapshot(path) (WorkspaceSnapshot, error)`.
- [x] Implement `SaveWorkspaceSnapshot(path, WorkspaceSnapshot) error`.
- [x] Debounce saves on the frontend and add documentation
- [x] Add round-trip tests for workspace snapshot.
- Note that these should be stored in os.UserConfigDir

For tests:

- [x] Create `testdata/settings_v1.toml` and `testdata/workspace_v1.toml`.
- [x] Add tests loading fixtures into Go structs and asserting values.
- [x] Add struct → TOML → struct round-trip tests.

### SQLite graph (schema + migrations)

- [x] Use `mattn/go-sqlite3`
- [x] Implement `OpenGraphDB(dbPath) (*sql.DB, error)` + `PRAGMA foreign_keys=ON`.
- [x] Write initial migration for `pages`, `blocks`, `links`, `schema_meta`.
- [x] Implement `Migrate(db *sql.DB) error` with simple versioning.
- [x] Implement basic CRUD: `CreatePage`, `GetPageByID`, `GetBlocksForPage`, `GetBacklinks`.
- [x] Add indexes on `blocks.page_id` and `links.to_page_id`.
- [x] Add tests using a temp DB (migrations + basic CRUD + backlinks).
    - [x] Add DB tests using `t.TempDir()` + temp SQLite file.
- NOTE: Migrations should be checked and applied on application load

#### Repository façade for Wails & Wails bindings

- [x] Implement `WorkspaceStore` (wraps `AppDirs`, calls TOML helpers).
- [x] Implement `GraphStore` (wraps `*sql.DB`, exposes graph CRUD/query).
- [x] Implement `NewStores(appName, workspaceName)` to construct dirs + DB + stores.
- [ ] Bind `WorkspaceStore` and `GraphStore` in `options.App{ Bind: [...] }`.

### F#/Fable integration

- [ ] Define F# record types for `Settings`, `WorkspaceUi`, and root `Model`.
- [ ] Implement `init` that calls `LoadSettings` + `LoadWorkspaceSnapshot` via bindings.
- [ ] Add `HydrateFromDisk` message and handler in `update`.
- [ ] Wire `SettingsChanged` and `WorkspaceChanged` messages to call save APIs (with debounce).
