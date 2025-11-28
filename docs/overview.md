# What is the Stormlight Labs Note Taker?

The Stormlight Labs Note Taker is a local-first, graph-based notes application for building personal knowledge management systems.

## Key Features

### Knowledge Graph

Every note becomes a node in your knowledge graph. Wikilinks create edges between notes, enabling bidirectional linking and graph visualizations. The graph database automatically maintains backlinks and forward links.

### Markdown Dialect

Write notes in CommonMark with powerful extensions like wikilinks (`[[note]]`), frontmatter metadata, inline tags (`#tag`), and block references. Your notes remain readable plain text files.

### Local-First Architecture

All data stays on your machine. No cloud sync, no telemetry, no analytics. Your workspace is just a folder of Markdown files with a SQLite database for indexing.

### Workspace Isolation

Each workspace has isolated state, configuration, and graph database. Switch between personal notes, work projects, and research vaults seamlessly.
