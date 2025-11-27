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

- [x] Define Wails-bindable Go methods:
    - [x] `OpenVault(path)` → `VaultInfo`.
    - [x] `ListNotes()` → `[NoteSummary]`.
    - [x] `GetNote(id)` / `SaveNote(note)` / `DeleteNote(id)`.
    - [x] `GetBacklinks(id)` / `GetGraph()` / `Search(query)`.
- [x] Standardize JSON DTOs for Fable interop.
- [x] Add basic error mapping (domain errors → user-friendly messages).

### Tests & Tooling

- [x] Unit tests for:
    - [x] Markdown parsing + link extraction.
    - [x] Graph construction.
    - [x] Search indexing and matching.

## Frontend (F# Fable + Elmish + React)

### Frontend Skeleton

- [x] Define root MVU types:
    - [x] `Model` (app-wide state: vault, notes, selection, panels).
    - [x] `Msg` (all UI events).
    - [x] `update : Msg -> Model -> Model * Cmd<Msg>`.
    - [x] `view : Model -> ReactElement`.
- [x] Implement routing/navigation (e.g. active note, graph, settings).
- [x] Wire startup to call Wails backend for initial vault state.

### Core Screens

- [x] **Vault picker**:
    - [x] UI to open/create vault using backend API.
- [x] **Editor view**:
    - [x] Markdown / outline editor component.
    - [x] Support headings, lists, code, inline links (`[[...]]`).
    - [ ] Keyboard shortcuts for basic formatting.
- [x] **Note list / outline sidebar**:
    - [x] Filterable list of notes (by title, tag).
    - [x] Double-click to open in editor.
- [x] **Backlinks panel**:
    - [x] Show list of notes linking to the current note.
    - [x] Click to navigate.
- [x] **Daily notes**:
    - [x] "Today" button to jump/create daily note

### Theming & UX

- [x] Basic layout:
    - [x] Sidebar (notes), main editor, right panel (backlinks/graph).
- [x] Add minimal settings panel (theme, font size, vault path overview).

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

### Frontend Testing

- [x] Add tests for:
    - [x] `update` logic (pure Elmish tests).
    - [x] Routing behavior.
    - [x] Backlinks and daily notes features.
    - [ ] Serialization/deserialization of models used in Wails calls.

## PKM & Data Model Parity

- [ ] Analyze core Obsidian features: local Markdown, links, plugins, graph, daily notes.
    - [ ] Map each to "must-have" vs "later".
- [ ] Analyze core Logseq features: local outliner, daily journals, backlinks, graph.
    - [ ] Decide how much outliner behavior to support in v1.
- [ ] Define canonical Markdown / outline dialect for this app:
    - [ ] Wikilinks, tags, fenced code, tasks (`[ ]` / `[x]`), headings, blocks.
- [ ] Document migration/import strategy:
    - [ ] Point at existing Obsidian / Logseq vaults without modifying them.
    - [ ] Clearly describe what is supported / unsupported.

## Plugin & Extensibility

- [ ] Define internal "extension points":
    - [ ] Toolbar commands.
    - [ ] Sidebar panels.
    - [ ] Note-level actions (e.g. "Copy link", "Open in file manager").
- [ ] Implement minimal internal plugin-like registry (no user scripts yet):
    - [ ] Register built-in features (daily notes, backlinks, graph) via the same mechanism.
- [ ] Document constraints for a future public plugin API, inspired by Obsidian’s plugin ecosystem.

## Parking Lot

- [ ] Advanced search (filters, saved searches, query language).
- [ ] Inline outlining workflows closer to Logseq (block refs, nested blocks)
- [ ] Basic mobile-friendly layout (for small windows).
- [ ] Optional sync/export features with explicit privacy design.
- [ ] Benchmarks for graph build on large vaults
- [ ] Add `go test ./...` CI job.
