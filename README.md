# Notes (codename: Knowledge Lab)

A local-first, Markdown-based personal knowledge management application inspired by Obsidian and Logseq, built with F# (Fable + Elmish) and Go (Wails).

## Features

- Local-first Markdown note storage
- Wikilinks and bidirectional linking
- Full-text search with BM25 ranking
- Tag support
- Graph visualization

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) & [AGENTS](./AGENTS.md) for development guidelines and architecture details.

<details>
<summary>Local Development</summary>

### Prerequisites

- **Go** 1.24 or later
- **.NET** 9.0 SDK
- **Node.js** 20+ with pnpm
- **Wails** v2.11.0

### Project Structure

```sh
notes/
├── backend/
│   ├── domain/          # Core types and errors
│   └── service/         # Business logic (notes, graph, search)
├── frontend/
│   ├── src/             # F# source files
│   ├── tests/           # Fable.Jester tests
│   ├── __mocks__/       # Test mocks for Wails API
│   └── dist/            # Compiled JavaScript output
├── app.go               # Wails application
├── main.go              # Application entry point
└── wails.json           # Wails configuration
```

### Setup

#### Backend

```bash
# Install the wails CLI
go install github.com/wailsapp/wails/v2/cmd/wails@latest

# Install Go dependencies
go mod tidy

# Run tests
go test ./...
```

#### Frontend

```bash
cd frontend

# Install deps & restore .NET tools
dotnet restore && dotnet tool restore

# Install npm dependencies & run tests
pnpm install && pnpm test
```

### Running the app locally

From the project root, run `wails dev`. This will:

1. Start the Wails development server
2. Compile the F# frontend code then run Vite dev server with hot reload
3. Launch the application window

The frontend will be available at the URL shown in the console, and changes to *JS* or Go code will trigger automatic recompilation.

In order to trigger reloads when you update the fable app, you must simultaneously run

```sh
pnpm watch # or
dotnet fable watch .
```

from `frontend/`

</details>

## License

AGPL-3.0-or-later. See [`LICENSE`](./LICENSE) for details.

## References

[STATUS](./.claude/commands/STATUS.md) command & [AGENTS](./AGENTS.md) adapted from [plyr.fm](https://tangled.org/zzstoatzz.io/plyr.fm) by [zzstoatzz.io](https://bsky.app/profile/zzstoatzz.io)
