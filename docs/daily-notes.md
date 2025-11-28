# Daily Notes

Daily notes follow a special naming convention and location.

## Default Convention

- **Location**: `/daily/` folder (configurable)
- **Format**: `YYYY-MM-DD.md` (e.g., `2025-01-27.md`)
- **Auto-Creation**: Created automatically when opened

## Configuration

```toml
# workspace config
[config]
daily_note_format = "2006-01-02"  # Go time format
daily_note_folder = "daily"       # Relative to workspace root
```

## Quick Access

Daily notes are accessible via:

- Keyboard shortcut (configured in app)
- Command palette (planned)
- Calendar view (planned)
