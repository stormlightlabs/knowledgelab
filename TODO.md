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
        - [x] Uses CSS classes to style nodes/edges based on state (selected, hovered, dimmed).
    - [x] Implement hover & click:
        - [x] Hover: show note title in a tooltip/overlay; highlight node and its neighbors.
        - [x] Click: dispatch a message to open the note in the editor and center it in the graph.
- [x] Pan / zoom:
    - [x] Wrap the SVG in a `<g>` with a transform and use a D3 zoom behavior to:
        - [x] Support mouse wheel zoom.
        - [x] Support click-and-drag panning.
    - [x] Keep zoom state (scale, translation) in the Elmish model or in a small JS module with messages to sync if needed.
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
            - [x] Refresh graph when backend graph data changes (e.g. on note save).
    - [x] `view`:
        - [x] Route to `SvgGraph.view` (and later `CanvasGraph.view`) based on `model.GraphEngine`.
- [x] Canvas renderer:
    - [x] Add a `CanvasGraph.view` that reuses the same D3-force simulation but draws nodes/links on `<canvas>`.
    - [x] Expose a user setting to switch between SVG and Canvas for large graphs.

### Frontend Testing

- [x] Add tests for:
    - [x] `update` logic (pure Elmish tests).
    - [x] Routing behavior.
    - [x] Backlinks and daily notes features.
    - [x] Graph view functionality (load, hover, zoom, engine switching, neighbor highlighting).
    - [ ] Serialization/deserialization of models used in Wails calls.

## PKM & Data Model Parity

### Obsidian Feature Parity

Obsidian is a local-first knowledge base built on a folder of Markdown files, with links as first-class citizens and a highly extensible core+plugin model.

| Feature           | Obsidian Behavior / Reference                                                                      | Priority      | Implementation Notes                                                                             |
| ----------------- | -------------------------------------------------------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------ |
| Local Markdown    | Vault = local folder of plain-text `.md` files; editor is "just" a Markdown editor on top of that. | **Must-have** | Directly mirror: vault = directory on disk; no DB required for v1.                               |
| Links / Wikilinks | Links and backlinks are core; graph + backlinks pane treat links as first-class.                   | **Must-have** | Support `[[Note Title]]` and standard Markdown links; index links for backlinks + graph.         |
| Backlinks Pane    | Core plugin shows "links from other notes to current one".                                         | **Must-have** | Backlinks panel in UI; powered by graph index.                                                   |
| Graph View        | Built-in graph view shows network of linked notes.                                                 | **Must-have** | D3-based SVG graph (nodes = notes, edges = links) with pan/zoom and click-through.               |
| Daily Notes       | Core plugin opens/creates note for today’s date for journals/to-dos.                               | **Must-have** | "Today" note pattern; configurable date template; stored under `/daily/` or similar.             |
| Tags              | Tag pane shows tags & counts across the vault.                                                     | **Must-have** | `#tag` and frontmatter tags; tag index + simple tag browser.                                     |
| Core Plugins      | Features like search, outline, backlinks, daily notes, graph, templates are "core plugins".        | **Must-have** | Implement as built-ins but architect as "internal extensions" to prepare for a plugin API later. |
| Community Plugins | Large ecosystem for extra workflows (AI, Zotero, etc.).                                            | **Later**     | Provide internal extension points in v1; public plugin API + marketplace is a post-MVP goal.     |
| Sync / Publish    | Obsidian Sync & Publish are paid, cloud-enabled features.                                          | **Later**     | Keep design local-first; future optional sync/export story must be explicit & opt-in.            |

### Logseq Feature Parity

Logseq is a local-first, block-based outliner that stores notes as a graph of Markdown/Org-mode files, with daily journals, backlinks, and graph view.

#### Logseq Core Features vs This App

| Feature                | Logseq Behavior / Reference                                                      | Priority              | Implementation Notes                                                                                    |
| ---------------------- | -------------------------------------------------------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------- |
| Local-first storage    | All data stored locally in Markdown/Org-mode files; no cloud required.           | **Must-have**         | Same as Obsidian parity: local files only; DB is an optimization, not a requirement, for v1.            |
| Block-based outliner   | Every line is a "block" in a bulleted outliner; pages are collections of blocks. | **Must-have (light)** | Support block IDs and basic nested blocks in Markdown; full Logseq-style block operations can be later. |
| Daily Journals         | Daily journal page is the primary entry point for notes.                         | **Must-have**         | Mirror Obsidian daily notes but allow block-centric journaling; treat journals as first-class.          |
| Backlinks              | Bi-directional links & backlink view across blocks/pages.                        | **Must-have**         | Backlinks index should work for both page and block targets where IDs exist.                            |
| Graph View             | Interactive graph of pages/blocks and their links.                               | **Must-have**         | Same graph engine as Obsidian parity; later allow block-level nodes when block granularity matures.     |
| Tasks / TODOs          | Treats tasks as first-class (`TODO`, `DOING`, `DONE`, etc.).                     | **Must-have (basic)** | Support `[ ]` / `[x]` tasks with simple filters; advanced task states and agenda views can be later.    |
| Queries (Datalog-like) | Advanced DB/graph queries over blocks, tags, properties.                         | **Later**             | Defer Datalog/DB-style queries; start with simple search + tag filtering in v1.                         |
| Whiteboards            | Visual whiteboard space for arranging blocks/notes.                              | **Later**             | Align with plugin/extensibility phase; not necessary for core parity at launch.                         |
| Plugins & Themes       | Plugin and theme ecosystem (PDF, Zotero, etc)                                    | **Later**             | Expose theming hooks early; full plugin API when internal extension points are stable.                  |

#### Outliner Behavior Target for v1

| Aspect              | Logseq Style                                   | v1 Target for This App                                                                           |
| ------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Structure           | Everything is a bullet "block", deeply nested. | Mixed model: standard Markdown documents with support for nested lists + optional block IDs.     |
| Primary entry point | Daily journal page as main landing surface.    | Daily note/journal view that can act as the default home screen.                                 |
| Block references    | Re-use and reference blocks across pages.      | Defer true block references; v1 only needs stable note-level links + block anchors for headings. |
| Queries over blocks | Query language for tasks/blocks/tags.          | Out of scope for v1; design data model so queries can be bolted on later.                        |

### Canonical Markdown / Outline Dialect

The app should define a clear, Obsidian- and Logseq-compatible Markdown dialect (with room for Org-style constructs later).
Obsidian uses standard Markdown plus wikilinks and frontmatter.
Logseq supports Markdown and Org-mode ("Orgdown") for outlining and tasks.

#### Syntax Surface (v1 vs Later)

| Syntax / Feature        | Example                           | Semantics in This App                                              | Priority                   |
| ----------------------- | --------------------------------- | ------------------------------------------------------------------ | -------------------------- |
| Headings                | `# Title`, `## Section`           | Standard Markdown headings; define document structure and anchors. | **Must-have**              |
| Paragraphs              | Blank-line-separated text         | Base unit of freeform writing.                                     | **Must-have**              |
| Wikilinks               | `[[Note Title]]`                  | Internal links resolved to notes by title / alias.                 | **Must-have**              |
| Standard links          | `[label](url)`                    | External links or explicit relative links within vault.            | **Must-have**              |
| Tags                    | `#tag`, frontmatter `tags:`       | Used for tag index, search filters, and simple saved views.        | **Must-have**              |
| Tasks (checkbox syntax) | `- [ ] todo`, `- [x] done`        | Basic TODOs with completed state; logseq-style states later.       | **Must-have** (basic)      |
| Lists / Outlines        | `- item`, `- nested`              | Nested bullets; foundation for light outliner behaviors.           | **Must-have**              |
| Code fences             | ` `lang                           | Syntax-highlighted code; preserved round-trip.                     | **Must-have**              |
| Inline code             | `` `code` ``                      | Literal inline snippets.                                           | **Must-have**              |
| Frontmatter (YAML)      | `---` … `---`                     | Per-note metadata (aliases, tags, type, etc.).                     | **Must-have**              |
| Block IDs               | `- item ^block-id` (Logseq style) | Stable block identifiers for future block refs and queries.        | **Later, but model-ready** |
| Properties / attributes | `key:: value`                     | Structured per-block metadata; basis for queries.                  | **Later**                  |
| Org-mode compatibility  | `* Heading`, `TODO`, `SCHEDULED`  | Optional Org-like parsing layer for Org users.                     | **Later**                  |

#### Dialect Decisions Checklist

| Task                                                                              | Status |
| --------------------------------------------------------------------------------- | ------ |
| Specify Markdown flavor (CommonMark + Obsidian/Logseq extensions) in docs.        | [ ]    |
| Document supported wikilink resolution rules (title vs path, aliases).            | [ ]    |
| Define canonical locations for daily notes/journals and naming convention.        | [ ]    |
| Decide on block ID format and how it’s serialized (even if not fully used in v1). | [ ]    |
| Write import notes for Obsidian vaults (what’s 1:1, what’s degraded).             | [ ]    |
| Write import notes for Logseq graphs (Markdown only at first; Org later).         | [ ]    |

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
