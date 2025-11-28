# Tasks

High-level TODO list for the Obsidian / Logseq–style desktop app built with
**F# (Fable + Elmish + React)** and **Wails (Go)**.

The target is a local-first, Markdown- and outline-based PKM with backlinks and
graph view similar to Obsidian and Logseq.

## Backend (Go + Wails)

Note CRUD with frontmatter support, wikilink parsing, backlinks, BM25 full-text search, and comprehensive test coverage.

## Frontend (F# Fable + Elmish + React)

Elmish MVU with three-panel layout, D3-force graph view (SVG/Canvas), daily notes, and comprehensive tests.

## PKM & Data Model Parity

Local-first Markdown storage with wikilinks, backlinks, graph view, daily notes, and BM25 search.

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

## UI/UX Polish (MVP)

### State Management (Model.fs)

- [ ] Add search state (query, filters, results)
- [ ] Add editor state (preview mode, cursor position, selection)
- [ ] Add UI state (panel sizes, active modals/dialogs)
- [ ] Add keyboard shortcut handlers in new module (`Keybinds.fs`)

### Core Functionality (View.fs)

Search & Discovery:

- [ ] Search UI with fuzzy matching
- [ ] Search input in sidebar with live results
- [ ] Keyboard shortcuts for common actions (Cmd/Ctrl+N, Cmd/Ctrl+K, etc.)

Editor:

- [ ] Markdown preview mode or split view
- [ ] Editor toolbar with preview toggle, formatting buttons, word/char count and what's in editor [enhancements](#editor-enhancements)
  - Interop with shiki
- [ ] Syntax highlighting for code blocks
- [ ] Wikilink autocomplete dropdown

Settings:

- [ ] Settings panel with actual controls (theme picker, font size slider, editor preferences)
- [ ] Live preview of settings changes

Notes List:

- [ ] Sorting options (title, date modified, date created)
- [ ] Empty state when no notes exist
- [ ] Note metadata display (created/modified dates in list items)

### UI Polish

Loading & Transitions:

- [ ] Loading skeleton states instead of generic "Loading..." overlay
- [ ] Smooth transitions for route changes and panel toggles
- [ ] Auto-save indicator in editor

Feedback & Messaging:

- [ ] Toast notifications for actions instead of fixed error banner
- [ ] Empty states with helpful messaging (no notes, no backlinks, etc.)
- [ ] Confirmation dialogs for destructive actions (delete note)

Layout & Navigation:

- [ ] Resizable panels (sidebar, backlinks panel)
- [ ] Implement recent files list in workspace snapshot
- [ ] Focus management for keyboard navigation
- [ ] Implement undo/redo for editor

## Parking Lot

- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Benchmarks for graph build on large workspaces.
- [ ] Add `go test ./...` CI job.
- [ ] Test serialization/deserialization of models used in Wails calls.
- [ ] Virtualization for large note lists (>100 notes)

### Command Palette

- [ ] Command palette for quick actions
- [ ] Implement command palette state

## Configuration

TOML-based settings and workspace snapshots with SQLite graph database, debounced saves (800ms).
