# Notes

A local-first, Markdown-based personal knowledge management (PKM) application inspired by Obsidian and Logseq, built with F# (Fable + Elmish + React) on the frontend and Wails (Go) on the backend.

## Features

- Local-first Markdown note storage
- Wikilinks and bidirectional linking
- Full-text search with BM25 ranking
- Tag support
- Graph visualization
- Real-time filesystem watching

## Development Setup

### Prerequisites

- **Go** 1.24 or later
- **.NET** 9.0 SDK
- **Node.js** 20+ with pnpm
- **Wails** v2.11.0

### Install & Setup Wails Backend

```bash
go install github.com/wailsapp/wails/v2/cmd/wails@latest

# Install Go dependencies
go mod tidy

# Run backend tests
cd backend && go test ./...
```

### Frontend Setup

```bash
cd frontend

# Restore .NET tools
dotnet tool restore

# Install npm dependencies
pnpm install

# Run frontend tests
pnpm test
```

## Running the Application

### Development Mode

```bash
# From project root
wails dev
```

This will:

1. Start the Wails development server
2. Compile the F# frontend code
3. Run Vite dev server with hot reload
4. Launch the application window

The frontend will be available at the URL shown in the console, and changes to F# or Go code will trigger automatic recompilation.

### Build

```bash
wails build
```

The compiled application will be in `./build/bin/`.

## Project Structure

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

## Frontend Scripts

See the front-ends [package.json](./frontend/package.json) for dev scripts

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) & [AGENTS](./AGENTS.md) for development guidelines and architecture details.

## License

AGPL-3.0-or-later. See [`LICENSE`](./LICENSE) for details.

## References

[STATUS](./.claude/commands/STATUS.md) command & [AGENTS](./AGENTS.md) adapted from [plyr.fm](https://tangled.org/zzstoatzz.io/plyr.fm) by [zzstoatzz.io](https://bsky.app/profile/zzstoatzz.io)
