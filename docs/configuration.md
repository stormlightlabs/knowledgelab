# Configuration

Your notes app stores configuration in two places: application-wide settings and workspace-specific UI state.

## Settings (Application-Wide)

Settings apply across all workspaces and persist between app launches. These control general preferences like theme, language, and editor behavior.

### General Settings

- Theme - Light, dark, or auto (follows system)
- Language - UI language preference
- Auto-save - Enable automatic note saving
- Auto-save interval - How often to auto-save (in seconds)

### Editor Settings

- Font family - The font used in the editor
- Font size - Text size in pixels
- Line height - Spacing between lines (multiplier)
- Tab size - Number of spaces per tab
- Vim mode - Enable vim keybindings
- Spell check - Enable spell checking

### Location

Settings are stored in `settings.toml`:

- macOS: `~/Library/Application Support/notes/settings.toml`
- Linux: `~/.config/notes/settings.toml`
- Windows: `%APPDATA%\notes\settings.toml`

### Format

The settings file uses TOML format (human-readable and editable):

```toml
[general]
theme = "dark"
language = "en"
auto_save = true
auto_save_interval = 30

[editor]
font_family = "JetBrains Mono"
font_size = 14
line_height = 1.6
tab_size = 2
vim_mode = false
spell_check = true
```

### Defaults

On first launch, the app creates default settings:

- Theme: Auto (follows system)
- Language: English
- Auto-save: Enabled (30 second interval)
- Font: Monospace, 14px, 1.6 line height
- Tab size: 2 spaces
- Vim mode: Disabled
- Spell check: Enabled

### Manual Editing

You can manually edit `settings.toml` while the app is closed. Changes will be loaded on next launch. Invalid TOML will reset to defaults.

## Workspace Snapshots (Per-Workspace)

Each workspace remembers its own UI state - what note you had open, panel sizes, pinned pages, etc. This lets you switch between workspaces and pick up right where you left off.

### UI State

- Active page - The currently open note
- Sidebar visibility - Whether the sidebar is shown
- Sidebar width - Width in pixels
- Right panel visibility - Whether backlinks/graph panel is shown
- Right panel width - Width in pixels
- Pinned pages - Quick-access pinned notes
- Recent pages - Recently opened notes (for history)
- Graph layout - Graph view layout preference (force, tree)

### Location

Each workspace has its own snapshot file:

- macOS: `~/Library/Application Support/notes/workspaces/{workspace-id}/workspace.toml`
- Linux: `~/.config/notes/workspaces/{workspace-id}/workspace.toml`
- Windows: `%APPDATA%\notes\workspaces\{workspace-id}\workspace.toml`

The `{workspace-id}` is a hash of your workspace path, ensuring each workspace has isolated state.

### Format

```toml
[ui]
active_page = "notes/my-note.md"
sidebar_visible = true
sidebar_width = 280
right_panel_visible = false
right_panel_width = 300
pinned_pages = ["daily/2025-01-15.md", "index.md"]
recent_pages = ["notes/my-note.md", "daily/2025-01-14.md"]
graph_layout = "force"
```

### Defaults

New workspaces start with:

- No active page
- Sidebar visible at 280px
- Right panel hidden (300px default width)
- No pinned pages
- Empty recent pages history
- Force-directed graph layout

### Auto-Save Behavior

Workspace state saves automatically as you work, but with debouncing:

- Changes are batched and saved every 500-1000ms
- Prevents excessive disk writes during rapid interactions
- Ensures state is persisted without impacting performance

## Storage Structure

Your configuration directory looks like this:

```sh
notes/                           # App root
├── settings.toml                # App-wide settings
└── workspaces/
    ├── abc123/                  # Workspace 1
    │   ├── workspace.toml       # UI state
    │   └── graph.db             # Graph database
    └── def456/                  # Workspace 2
        ├── workspace.toml
        └── graph.db
```

## When Settings Change

### Settings

Settings change when you:

- Adjust preferences in the settings panel
- Change editor options (font, theme)
- Toggle features (vim mode, spell check)

Changes save immediately when you modify them in the UI.

### Workspace Snapshots

Workspace state changes when you:

- Open a different note
- Resize panels
- Toggle sidebar visibility
- Pin/unpin pages
- Navigate to a note (adds to recent pages)

Changes save automatically with debouncing (not every keystroke, but regularly enough to preserve your state).

## Resetting Configuration

### Reset Settings

Delete `settings.toml` to restore defaults. The app will recreate it on next launch.

### Reset Workspace State

Delete `workspace.toml` in the workspace directory to reset that workspace's UI state. The app will recreate it with defaults.

### Reset Everything

Delete the entire app configuration directory to start fresh. All settings and UI state will be reset, but your note files remain untouched (they're stored separately in your workspace folders).

## Migration and Compatibility

The configuration format is versioned and automatically migrates on app updates:

- Old settings are preserved when possible
- New features get sensible defaults
- Invalid values fall back to defaults

You'll never have to manually migrate configuration files.

## Privacy

All configuration is stored locally on your computer:

- No cloud sync, telemetry or analytics
- Settings never leave your machine
