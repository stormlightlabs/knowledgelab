# Tasks

High-level TODO list for the Obsidian / Logseq–style desktop app built with
**F# (Fable + Elmish + React)** and **Wails (Go)**.

The target is a local-first, Markdown- and outline-based PKM with backlinks and
graph view similar to Obsidian and Logseq.

## Backend (Go + Wails)

### Domain & Services

- [x] Define core domain types:
    - [x] `Note`, `Block` (for outline-style content), `Link`, `Tag`, `DailyNote`.
    - [x] `Workspace` configuration (root folder, ignore patterns).
- [x] Implement filesystem service:
    - [x] Open/create workspace at a given path.
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
    - [x] `OpenWorkspace(path)` → `WorkspaceInfo`.
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
    - [x] `Model` (app-wide state: workspace, notes, selection, panels).
    - [x] `Msg` (all UI events).
    - [x] `update : Msg -> Model -> Model * Cmd<Msg>`.
    - [x] `view : Model -> ReactElement`.
- [x] Implement routing/navigation (e.g. active note, graph, settings).
- [x] Wire startup to call Wails backend for initial workspace state.

### Core Screens

- [x] **Workspace picker**:
    - [x] UI to open/create workspace using backend API.
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
- [x] Add minimal settings panel (theme, font size, workspace path overview).

### Graph View

- [x] Add D3 (force simulation) as the layout engine for the note graph
- [x] Data & layout:
    - [x] Define `GraphNode` and `GraphLink` types in the frontend model (ids, labels, degree, etc.).
    - [x] Expose a `GraphData` record `{ nodes : GraphNode list; links : GraphLink list }`.
    - [x] From backend graph API, normalize data into `GraphData` suitable for D3-force (source/target IDs, weights).
    - [x] Create a D3-force simulation (via JS interop) that:
        - [x] Uses forces for link distance, node repulsion, and centering.
        - [x] Updates node positions (`x`, `y`) on each tick.
        - [x] Exposes a way to (re)start the simulation when data changes.
- [x] SVG renderer:
    - [x] Implement an `SvgGraph.view` that:
        - [x] Renders links as `<line>` elements from `source.x, source.y` to `target.x, target.y`.
        - [x] Renders nodes as `<circle>` (or similar) with `cx, cy` from simulation positions.
        - [ ] Uses CSS classes to style nodes/edges based on state (selected, hovered, dimmed).
    - [x] Implement hover & click:
        - [ ] Hover: show note title in a tooltip/overlay; highlight node and its neighbors.
        - [x] Click: dispatch a message to open the note in the editor and center it in the graph.
- [ ] Pan / zoom:
    - [ ] Wrap the SVG in a `<g>` with a transform and use a D3 zoom behavior to:
        - [ ] Support mouse wheel zoom.
        - [ ] Support click-and-drag panning.
    - [ ] Keep zoom state (scale, translation) in the Elmish model or in a small JS module with messages to sync if needed.
- [x] Wiring (Elmish):
    - [x] `Model`:
        - [x] Add `GraphData` and `GraphState` (e.g. selected node id, hovered node id, zoom state, engine = Svg|Canvas).
    - [x] `Msg`:
        - [x] Add messages such as:
            - [x] `GraphNodeClicked of NoteId`
            - [x] `GraphNodeHovered of NoteId option`
            - [x] `GraphZoomChanged of ZoomState`
            - [x] `GraphEngineChanged of GraphEngine`
    - [x] `update`:
        - [x] Handle graph-related messages:
            - [x] Update selection/hover state.
            - [x] Trigger note opening when a node is clicked.
            - [ ] Refresh graph when backend graph data changes (e.g. on note save).
    - [x] `view`:
        - [x] Route to `SvgGraph.view` (and later `CanvasGraph.view`) based on `model.GraphEngine`.
- [ ] Canvas renderer:
    - [ ] Add a `CanvasGraph.view` that reuses the same D3-force simulation but draws nodes/links on `<canvas>`.
    - [ ] Expose a user setting to switch between SVG and Canvas for large graphs.

### Frontend Testing

- [x] Add tests for:
    - [x] `update` logic (pure Elmish tests).
    - [x] Routing behavior.
    - [x] Backlinks and daily notes features.
    - [x] Graph view functionality (load, hover, zoom, engine switching).
    - [ ] Serialization/deserialization of models used in Wails calls.

## PKM & Data Model Parity

- [ ] Analyze core Obsidian features: local Markdown, links, plugins, graph, daily notes.
    - [ ] Map each to "must-have" vs "later".
- [ ] Analyze core Logseq features: local outliner, daily journals, backlinks, graph.
    - [ ] Decide how much outliner behavior to support in v1.
- [ ] Define canonical Markdown / outline dialect for this app:
    - [ ] Wikilinks, tags, fenced code, tasks (`[ ]` / `[x]`), headings, blocks.

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
- [ ] Benchmarks for graph build on large workspaces
- [ ] Add `go test ./...` CI job.
