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

Implemented tag parsing, indexing, and browsing with a filtering UI and autocomplete.

## Logseq-Compatible Features

### Block-Based Outliner

Block ID support (UUID v4), parsing, round-trip preservation, indentation (Tab/Shift+Tab), arrow key navigation, and focus/zoom state tracking.

### Task Management

Task parsing (`- [ ]` / `- [x]`), completion toggling (checkbox/keyboard), date metadata tracking, aggregation panel with filtering by status/note/date.

## Markdown Dialect & Syntax

**Frontmatter (YAML)**: Parse, preserve, and round-trip YAML frontmatter with support for aliases, tags, type, and timestamps.

**Editor Enhancements**: Keyboard shortcuts for bold, italic, inline code, links, and headings (Cmd/Ctrl+B/I/E/K/1-6).

**Dialect Specification & Documentation**: CommonMark with Obsidian/Logseq extensions, wikilink resolution, daily notes, block IDs, and import guides.

## MVP Roadmap

**State Management (Model.fs)**: search state, editor state (preview, cursor, selection, history), UI state (panels, modals), and keyboard shortcuts.

### Core Functionality

#### Search & Discovery: Core Infrastructure

BM25 search engine with fuzzy matching, edit distance scoring, indexed titles/content/frontmatter,
SearchState with loading states, search panel UI (Cmd/Ctrl+K), results rendering with snippets/tags, and click-to-open functionality.

#### Search UX & Discovery

Live search with debouncing, snippet highlighting with [[ ]] markers rendered via CSS, search history autocomplete (last 20 queries) with arrow key navigation, and empty state handling.

#### Editor

Formatting shortcuts (bold/italic/code/links/headings), cursor tracking, preview modes (edit/split/preview), toolbar, status bar, and Shiki syntax highlighting.

- [ ] Wikilink autocomplete dropdown

#### Settings

- Settings panel with controls (theme picker, font size slider, editor preferences, auto-save, vim mode, spell check) and live preview of settings changes with debounced save

#### Notes List

- [x] Sorting options (title, date modified, date created)
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

Undo/redo (Cmd/Ctrl+Z/Shift+Z), toolbar buttons, per-note history preservation, and change tracking with timestamps.

## Configuration

TOML-based settings, workspace snapshots with debounced saves (800ms), user-level (XDG/AppData) and workspace-level config (`.knowledgelab/`), and a SQLite graph database.

## Base16 Theming Engine

- [x] Color variable audit:
  - [x] Survey existing CSS for all color values.
  - [x] Map current colors to base16 semantic roles (base00-base0F)
- [x] Base16 variable migration:
  - [x] Replace hardcoded colors with `--color-base0X` CSS variables.
  - [x] Define semantic color mappings (background, foreground, accents).
  - [x] Ensure all UI components use base16 variables consistently.
- [x] Theme file support:
  - [x] Support curated subset of themes from [tinted-theming](https://github.com/tinted-theming/schemes/tree/spec-0.11/base16)
  - [x] Parse base16 theme files (YAML format).
  - [x] Load bundled default themes (light, dark variants) & support user-provided theme files from config directory.
- [x] Settings integration:
  - [ ] Store selected theme name & color override values in user settings.
  - [x] Apply theme on app startup and settings changes.
- [ ] Theme picker UI:
  - [ ] Add theme selector dropdown in settings panel.
  - [ ] Display theme preview (live before saving) with sample colors.
- [ ] Color customization UI:
  - [ ] Add color picker for individual base16 color overrides with ability to reset to theme or color to default (Iceberg.vim).
  - [ ] Export custom theme as YAML file.
- [x] Documentation (`docs/theming.md`):
  - [x] base16 color role descriptions.
  - [x] Document theme file format and location.
  - [x] Provide examples of custom theme creation.

## Parking Lot

- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Keybind help screen/view
- [ ] Ensure snippet highlighting doesn't collide with wikilinks

### v1

- [x] Test serialization/deserialization of models used in Wails calls.
- [ ] Add `go test ./...` CI job.

### Post-v1

- [ ] Benchmarks for graph build on large workspaces.
- [ ] Virtualization for large note lists (>100 notes)

### Command Palette

- [ ] Command palette for quick actions
- [ ] Implement command palette state

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
