# Graph Database

The graph database stores the relationships between your notes, creating a knowledge graph that powers features like backlinks, forward links, and graph visualizations.

## What's Stored

The graph database tracks three main types of data:

### Pages

Every note in your workspace is represented as a page in the graph. Each page stores:

- Unique ID (the note's relative path)
- Title
- Creation and modification timestamps

### Blocks

Content within notes can be broken into blocks (paragraphs, lists, code blocks). Each block stores:

- Unique block ID
- Parent page reference
- Content text
- Position within the page

### Links

Connections between pages (wikilinks like `[[other-note]]`). Each link stores:

- Source page (where the link appears)
- Target page (where the link points)
- Link text (what's displayed)
- Creation timestamp

## How It Works

### Automatic Indexing

When you create or modify a note:

1. The file is parsed for wikilinks and other metadata
2. Pages are created/updated in the database
3. Links are extracted and indexed
4. Backlinks are automatically maintained

### Foreign Key Constraints

The database enforces referential integrity:

- Deleting a page automatically removes its blocks
- Deleting a page automatically removes its incoming and outgoing links
- This prevents orphaned data and keeps the graph consistent

## Database Location

The graph database is stored per-workspace:

- macOS: `~/Library/Application Support/notes/workspaces/{workspace-id}/graph.db`
- Linux: `~/.config/notes/workspaces/{workspace-id}/graph.db`
- Windows: `%APPDATA%\notes\workspaces\{workspace-id}\graph.db`

Each workspace has its own isolated database, so notes from different workspaces never interfere with each other.

## Migrations

The database schema is versioned and migrations run automatically on app startup:

- First launch: Creates initial schema (pages, blocks, links)
- Updates: Future migrations will be applied incrementally
- Safe: Migrations run in transactions - either all changes succeed or none do

The `schema_meta` table tracks which migrations have been applied.

## Data Lifecycle

### On Application Start

1. Database connection opens
2. Foreign key constraints enabled
3. Migrations applied (if needed)
4. Ready for queries

### On Workspace Open

1. Existing graph database loaded (or created if new workspace)
2. Initial scan of workspace files
3. Pages and links indexed in background

### On Note Save

1. Note parsed for links and metadata
2. Page created/updated in database
3. Old links removed
4. New links inserted
5. Backlink counts updated

### On Note Delete

1. Page removed from database
2. Foreign key constraints cascade:
   - All blocks for the page deleted
   - All links to/from the page deleted

### On Application Close

1. Any pending writes flushed
2. Database connection closed cleanly

## Backup

To backup your graph data, simply copy the `graph.db` file. The database is a single SQLite file containing all graph data for the workspace.

For complete workspace backup, back up the entire workspace directory which includes:

- The graph database (`graph.db`)
- Workspace UI state (`workspace.toml`)
- Your note files (Markdown files)
