# AGENTS.md

This project is a local-first, graph-based notes application inspired by Obsidian and Logseq, built with **F# (Fable + React/Elmish)** on the frontend and **Wails (Go)** on the desktop/backend side.

Between this & [CONTRIBUTING](./CONTRIBUTING.md), these are canonical guidelines for working on this project.

**Before touching any code** (agentic or human), read `CONTRIBUTING.md` the latest `STATUS.md` in the project root.
These files are updated frequently and contain task-specific constraints.

## System Overview

- **Domain**
  - Personal Knowledge Management (PKM)/Zettlekasten local Markdown + graph views similar to Obsidian core/community plugins and Logseq’s graph/database approach.
- **Frontend**:
  - F# compiled to JS via **Fable**, using **Elmish** MVU (Model–View–Update) with React components.
- **Desktop Shell / Backend**:
  - **Wails** Go app hosting the frontend UI and providing native capabilities (file access, windowing, system APIs) via Go bindings.

High-level responsibilities:

- **Frontend (F#)**: state management (MVU), rendering, commands dispatched to backend, plugin-like UI features.
- **Backend (Go via Wails)**: workspace management, file/graph index, sync, search, background jobs.

### Architecture

#### Backend (Go + Wails)

The backend is built with Go and provides:

- **Domain types**: Note, Block, Link, Tag, Workspace
- **Services**: Filesystem, Notes, Graph, Search
- **Wails API**: Exposes Go methods to the frontend via JSON-RPC

Located in `/backend`:

- `backend/domain` - Core domain types and errors
- `backend/service` - Business logic services
- `app.go` - Wails application and API bindings
- `main.go` - Application entry point

#### Frontend (F# + Fable + Elmish)

The frontend uses the Model-View-Update (MVU) architecture:

- **Domain**: F# types that mirror Go domain types
- **Api**: Wails runtime bindings for calling backend
- **Model**: Elmish state, messages, and update logic
- **View**: React components using Feliz

Located in `/frontend/src`:

- `Domain.fs` - Frontend domain types
- `Api.fs` - Wails API bindings
- `Model.fs` - MVU state and update logic
- `View.fs` - UI components
- `Program.fs` - Application entry point

### Data & Plugin Philosophy

The app aims to behave like existing PKM tools:

- Files are local, user-owned Markdown or similar formats.
- A graph/index (akin to Logseq’s graph or Obsidian’s backlinks/core plugins) is built on top of that content, not instead of it.

Design constraints:

- **No cloud by default**: all features must work offline and store data locally.
- Never silently change or migrate user data without an explicit, versioned migration step.
- Any "plugin-like" feature must be:
  - Local-first.
  - Reversible (easy to disable or uninstall).

## Frontend (F# Fable + Elmish + React)

Key patterns:

- Single source of truth via MVU (TEA):
  - `Model` = app state
  - `Msg` = events
  - `update` = pure state transition
  - `view` = React/JSX or Fable.React components

Contribution rules:

1. Prefer **small, composable Elmish sub-modules** for new features (e.g. panels, dialogs).
2. Avoid global state mutations outside `update`.
3. Keep view components presentational; push logic into `update`/domain.
4. When modifying routing or app shell, open an issue with a short design note before implementing.

## Backend (Wails + Go)

Wails wraps a standard Go app with a webview frontend and a runtime API

Contribution rules:

1. Domain logic should live in pure Go packages under `backend/internal/...`.
2. Wails bindings (`Bind`) should be thin adapters, not business logic.
3. All filesystem access must go through audited service interfaces (for easier testing and sandboxing).
4. Add unit tests for new domain services and table-driven tests where possible.

## Testing

### Backend Tests

```bash
cd backend
go test ./...
```

Tests use Go's standard testing package and cover:

- Markdown parsing and link extraction
- Graph construction and queries
- Search indexing and BM25 ranking
- Filesystem operations

### Frontend Tests

```bash
cd frontend
pnpm test
```

Tests use Fable.Jester (Jest bindings for F#) and cover: MVU update logic, State transitions, Routing behavior.
The Wails API is mocked for testing, so tests run without the Go backend.
