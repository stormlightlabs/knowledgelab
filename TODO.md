# Tasks

High-level TODO list for the Obsidian / Logseq–style desktop app built with **F# (Fable + Elmish + React)** and **Wails (Go)**.

The target is a local-first, Markdown- and outline-based PKM with backlinks and graph view similar to Obsidian and Logseq.

## Backend (Go + Wails)

Note CRUD with frontmatter support, wikilink parsing, backlinks, BM25 full-text search, and comprehensive test coverage.

## Frontend (F# Fable + Elmish + React)

Elmish MVU with three-panel layout, D3-force graph view (SVG/Canvas), daily notes, and comprehensive tests.

## PKM & Data Model Parity

Local-first Markdown storage with wikilinks, backlinks, graph view, daily notes, and BM25 search.

## Obsidian-Compatible Features

### Tags System

Implemented tag parsing, indexing, and browsing with a filtering UI.

- [ ] Tag search and autocomplete in search box.

### Templates

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

## Logseq-Compatible Features

### Block-Based Outliner

- Block ID support with parsing, generation (UUID v4), and round-trip preservation of Logseq-style block IDs.
- Block operations including indentation (Tab/Shift+Tab), arrow key navigation, and focus/zoom state tracking.

### Task Management

- Task parsing with support for unchecked (`- [ ]`) and completed (`- [x]`) task syntax, distinguished from regular list items.
- Task state tracking with completion toggling via checkbox click or keyboard shortcut, including created and completed date metadata.
- Task views with aggregation panel showing all tasks across notes, filterable by completion status, note, and date ranges.

## Markdown Dialect & Syntax

**Frontmatter (YAML)**: Parse, preserve, and round-trip YAML frontmatter with support for aliases, tags, type, and timestamps.

**Editor Enhancements**: Keyboard shortcuts for bold, italic, inline code, links, and headings (Cmd/Ctrl+B/I/E/K/1-6).

**Dialect Specification & Documentation**: CommonMark with Obsidian/Logseq extensions, wikilink resolution, daily notes, block IDs, and import guides.

## Advanced Features (Deferred to Post-v1)

The following features are deferred to future releases after v1 launch:

- **Advanced block operations**: Block references, block embedding, block-level properties (`key:: value`).
- **Datalog-style queries**: Advanced query language for filtering and aggregating blocks, tasks, tags, and properties.
- **Whiteboards**: Visual whiteboard canvas for spatially arranging notes and blocks.
- **Advanced task states**: Logseq-style task states beyond basic checkbox (`TODO`, `DOING`, `DONE`, `WAITING`, `CANCELLED`).

### Plugin Architecture

- [ ] Define internal extension points:
  - [ ] Toolbar command registration API.
  - [ ] Sidebar panel registration API.
  - [ ] Note context menu action hooks.
- [ ] Implement plugin registry
- [ ] Plugin lifecycle hooks (init, load, unload)
- [ ] Document plugin constraints
- [ ] Public plugin API with marketplace/registry for third-party extensions

## MVP Roadmap

**State Management (Model.fs)**: Search state, editor state (preview, cursor, selection), UI state (panels, modals), and keyboard shortcut handlers (`Keybinds.fs`).

### Core Functionality

#### Search & Discovery: Core Infrastructure

- Backend
  - [x] Backend BM25 search index:
    - [x] Implement BM25 scoring engine in `backend/service/search`
    - [x] Index note titles, content, and frontmatter on workspace load
  - [x] Fuzzy matching & typo tolerance:
    - [x] Integrate edit distance scoring for query terms
    - [x] Boost exact matches over fuzzy matches in ranking
  - [x] Backend search query API:
    - [x] Add `Search(query, limit)` method to Wails bindings
    - [x] Return ranked results with scores and matched snippets

- Frontend
  - [ ] Search state management:
    - [ ] Define `SearchState` type with query, results, loading states
    - [ ] Add search messages (`SearchQueryChanged`, `SearchResultsReceived`, `SearchCleared`)
  - [ ] Basic search UI component:
    - [ ] Search input box in sidebar with clear button
    - [x] Keyboard shortcut (Cmd/Ctrl+K) to focus search
  - [ ] Search results list renderer:
    - [ ] Render results list below search input
    - [ ] Handle click to open note in editor

#### Search UX & Discovery

- [ ] Live search with debouncing:
  - [ ] Add 300ms debounce timer in frontend update logic
  - [ ] Cancel pending searches when query changes
- [ ] Search result highlighting:
  - [ ] Backend extracts matched text snippets with context windows
  - [ ] Frontend renders highlighted terms using CSS classes
- [ ] Search history & autocomplete:
  - [ ] Store search history in workspace snapshot (last 20 queries)
  - [ ] Render autocomplete dropdown with arrow key navigation
- [ ] Empty states & error handling:
  - [ ] Show "No results found" with search tips
  - [ ] Display error toast when search API fails

#### Editor

- [x] Editor with formatting shortcuts, cursor tracking, preview mode, split view, toolbar, status bar, and Shiki syntax highlighting.
- [ ] Wikilink autocomplete dropdown

#### Settings

- Settings panel with controls (theme picker, font size slider, editor preferences, auto-save, vim mode, spell check) and live preview of settings changes via SettingsChanged message with debounced save

#### Notes List

- [ ] Sorting options (title, date modified, date created)
- [ ] Empty state when no notes exist
- [ ] Note metadata display (created/modified dates in list items)

### UI Polish

- [x] Smooth transitions for route changes and panel toggles
- [x] Recent files list in workspace snapshot
- [ ] Loading skeleton states
- [ ] Auto-save indicator in editor
- [ ] Toast notifications for actions
- [ ] Empty states with helpful messaging
- [ ] Confirmation dialogs for destructive actions
- [ ] Resizable panels (sidebar, backlinks panel)
- [ ] Focus management for keyboard navigation

### History Stack

Undo/redo with keyboard shortcuts, toolbar buttons, per-note history preservation, and change tracking

## Parking Lot

- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Benchmarks for graph build on large workspaces.
- [ ] Add `go test ./...` CI job.
- [x] Test serialization/deserialization of models used in Wails calls.
- [ ] Virtualization for large note lists (>100 notes)
- [ ] Keybind help screen/view

### Command Palette

- [ ] Command palette for quick actions
- [ ] Implement command palette state

## Configuration

TOML-based settings and workspace snapshots with SQLite graph database, debounced saves (800ms).

User-level config (XDG/AppData) and workspace-level config (`.knowledgelab/`) with `backend/paths` package, Wails integration, and comprehensive tests.

## Base16 Theming Engine

- [ ] Color variable audit:
  - [ ] Survey existing CSS for all color values.
  - [ ] Map current colors to base16 semantic roles (base00-base0F).
  - [ ] Identify additional colors needed for base24 extension.
- [ ] Base16 variable migration:
  - [ ] Replace hardcoded colors with `--color-base0X` CSS variables.
  - [ ] Define semantic color mappings (background, foreground, accents).
  - [ ] Ensure all UI components use base16 variables consistently.
- [ ] Theme file support:
  - [ ] Parse base16 theme files (YAML format).
  - [ ] Load bundled default themes (light, dark variants) & support user-provided theme files from config directory.
- [ ] Settings integration:
  - [ ] Store selected theme name & color override values in user settings.
  - [ ] Apply theme on app startup and settings changes.
- [ ] Theme picker UI:
  - [ ] Add theme selector dropdown in settings panel.
  - [ ] Display theme preview (live before saving) with sample colors.
- [ ] Color customization UI:
  - [ ] Add color picker for individual base16 color overrides with ability to reset to theme or color to default (Iceberg.vim).
  - [ ] Export custom theme as YAML file.
- [ ] Documentation (`docs/theming.md`):
  - [ ] base16 color role descriptions.
  - [ ] Document theme file format and location.
  - [ ] Provide examples of custom theme creation.
