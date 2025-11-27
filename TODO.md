# Tasks

High-level TODO list for the Obsidian / Logseq–style desktop app built with
**F# (Fable + Elmish + React)** and **Wails (Go)**.

The target is a local-first, Markdown- and outline-based PKM with backlinks and
graph view similar to Obsidian and Logseq.

## Backend (Go + Wails)

### Domain & Services

- [x] Define core domain types:
    - [x] `Note`, `Block` (for outline-style content), `Link`, `Tag`, `DailyNote`.
    - [x] `Vault` / `Workspace` configuration (root folder, ignore patterns).
- [x] Implement filesystem service:
    - [x] Open/create vault at a given path.
    - [x] Load all Markdown / outline files from disk.
    - [x] Watch filesystem for changes and emit events.
- [x] Implement note service:
    - [x] CRUD for notes and blocks (via filesystem).
    - [x] Frontmatter support (optional, versioned).
- [x] Implement graph/index service:
    - [x] Parse links (`[[wikilinks]]`, tags) to build bidirectional graph (backlinks).
    - [x] Expose graph queries (neighbors, backlinks, paths).
- [x] Implement search service:
    - [x] Basic full-text search over titles + bodies (BM25).
    - [x] Filter by tag, date, path.

### API Surface to Frontend

- [ ] Define Wails-bindable Go methods:
    - [ ] `OpenVault(path)` → `VaultInfo`.
    - [ ] `ListNotes()` → `[NoteSummary]`.
    - [ ] `GetNote(id)` / `SaveNote(note)` / `DeleteNote(id)`.
    - [ ] `GetBacklinks(id)` / `GetGraph()` / `Search(query)`.
- [ ] Standardize JSON DTOs for Fable interop.
- [ ] Add basic error mapping (domain errors → user-friendly messages).

### Tests & Tooling

- [ ] Add `go test ./...` CI job.
- [x] Unit tests for:
    - [x] Markdown parsing + link extraction.
    - [x] Graph construction.
    - [x] Search indexing and matching.
- [ ] Benchmarks for graph build on large vaults (optional).

## Frontend (F# Fable + Elmish + React)

### Frontend Skeleton

- [ ] Initialize Fable + Elmish + React app in `frontend/`.
- [ ] Define root MVU types:
    - [ ] `Model` (app-wide state: vault, notes, selection, panels).
    - [ ] `Msg` (all UI events).
    - [ ] `update : Msg -> Model -> Model * Cmd<Msg>`.
    - [ ] `view : Model -> ReactElement`.
- [ ] Implement routing/navigation (e.g. active note, graph, settings).
- [ ] Wire startup to call Wails backend for initial vault state.

### Core Screens

- [ ] **Vault picker**:
    - [ ] UI to open/create vault using backend API.
- [ ] **Editor view**:
    - [ ] Markdown / outline editor component.
    - [ ] Support headings, lists, code, inline links (`[[...]]`).
    - [ ] Keyboard shortcuts for basic formatting.
- [ ] **Note list / outline sidebar**:
    - [ ] Filterable list of notes (by title, tag).
    - [ ] Double-click to open in editor.
- [ ] **Backlinks panel**:
    - [ ] Show list of notes linking to the current note.
    - [ ] Click to navigate.
- [ ] **Daily notes**:
    - [ ] “Today” button to jump/create daily note (Obsidian/Logseq-style).

### Graph View

- [ ] Select a graph visualization library (or simple custom canvas).
- [ ] Implement:
    - [ ] Nodes = notes, edges = links.
    - [ ] Hover to show note title; click to open note.
    - [ ] Basic pan/zoom.
- [ ] Wiring:
    - [ ] `Model` includes graph data from backend.
    - [ ] `Msg` for selecting nodes, refreshing graph.
    - [ ] `update` integrates graph actions.

### Theming & UX

- [ ] Implement light/dark theme toggling.
- [ ] Basic layout:
    - [ ] Sidebar (notes), main editor, right panel (backlinks/graph).
- [ ] Add minimal settings panel (theme, font size, vault path overview).

### Frontend Testing

- [ ] Add tests for:
    - [ ] `update` logic (pure Elmish tests).
    - [ ] Routing behavior.
    - [ ] Serialization/deserialization of models used in Wails calls.

## PKM & Data Model Parity

- [ ] Analyze core Obsidian features: local Markdown, links, plugins, graph, daily notes.
    - [ ] Map each to “must-have” vs “later”.
- [ ] Analyze core Logseq features: local outliner, daily journals, backlinks, graph.
    - [ ] Decide how much outliner behavior to support in v1.
- [ ] Define canonical Markdown / outline dialect for this app:
    - [ ] Wikilinks, tags, fenced code, tasks (`[ ]` / `[x]`), headings, blocks.
- [ ] Document migration/import strategy:
    - [ ] Point at existing Obsidian / Logseq vaults without modifying them.
    - [ ] Clearly describe what is supported / unsupported.

## Plugin & Extensibility

- [ ] Define internal “extension points”:
    - [ ] Toolbar commands.
    - [ ] Sidebar panels.
    - [ ] Note-level actions (e.g. “Copy link”, “Open in file manager”).
- [ ] Implement minimal internal plugin-like registry (no user scripts yet):
    - [ ] Register built-in features (daily notes, backlinks, graph) via the same mechanism.
- [ ] Document constraints for a future public plugin API, inspired by Obsidian’s plugin ecosystem.

## Parking Lot

- [ ] Advanced search (filters, saved searches, query language).
- [ ] Inline outlining workflows closer to Logseq (block refs, nested blocks)
- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Optional sync/export features with explicit privacy design.
