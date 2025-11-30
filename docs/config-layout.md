# Configuration Layout

Configuration is stored at two levels: user-level (machine-specific) and workspace-level (portable).

## Paths

### User-Level (Machine-Specific)

Global preferences, recently opened workspaces, graph databases, and UI state.

| Platform   | Path                                                          |
| ---------- | ------------------------------------------------------------- |
| macOS      | `~/Library/Application Support/KnowledgeLab/`                 |
| Linux/Unix | `$XDG_CONFIG_HOME/KnowledgeLab/` or `~/.config/KnowledgeLab/` |
| Windows    | `%APPDATA%\KnowledgeLab\`                                     |

Contents:

```sh
KnowledgeLab/
├── settings.toml              # Global settings
└── workspaces/                # Per-workspace state
    └── {workspace-id}/
        ├── graph.db           # SQLite graph index
        └── workspace.toml     # UI state (panels, recent files)
```

### Workspace-Level (Portable)

Project-specific settings and templates. Can be committed to version control.

```sh
/path/to/notes/
├── .knowledgelab/
│   ├── config.toml           # Workspace settings
│   └── templates/            # Note templates
└── daily/                    # Your notes
```

## What Goes Where

### User-Level

- Global theme, font size, window position
- Recently opened workspaces
- Graph database and search index (can be rebuilt)

### Workspace-Level

- Note templates
- Daily note location and format
- Workspace-specific conventions

## Implementation

Functions in `backend/paths/paths.go`:

- `UserConfigDir(appName)` - Platform-appropriate user config directory
  - Respects `XDG_CONFIG_HOME` on Linux/Unix
  - Creates directory with 0700 permissions
- `WorkspaceConfigDir(workspaceRoot, appName)` - Workspace config directory
  - Returns `{workspaceRoot}/.knowledgelab`
  - Creates directory with 0755 permissions

## Version Control

Workspace `.knowledgelab/` can be committed to Git. Exclude temporary state:

```gitignore
.knowledgelab/cache/
.knowledgelab/.tmp/
```
