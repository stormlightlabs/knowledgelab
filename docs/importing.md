# Import Guides

## Importing from Obsidian

**Supported**:

- Wikilinks `[[note]]`
- Frontmatter (title, tags, aliases)
- Inline tags `#tag`
- Block references (basic)

**Gracefully Degraded**:

- Dataview queries (not executed, preserved as code blocks)
- Canvas files (not supported, skipped)
- Obsidian-specific plugins (disabled, may need manual replacement)

**Manual Steps**:

1. Copy vault folder to workspace location
2. Open workspace in application
3. Verify wikilinks resolve correctly
4. Check frontmatter compatibility
5. Install equivalent features if needed

## Importing from Logseq

**Markdown Support**:

- Wikilinks work identically
- Frontmatter supported
- Tags supported

**Org-mode Limitations**:

- Org-mode files not natively supported
- Convert to Markdown first using something like pandoc:

  ```bash
  pandoc file.org -f org -t markdown -o file.md
  ```

**Block ID Handling**:

- Logseq block IDs preserved if at line end
- New block IDs generated for unmarked blocks
- Block references may need manual verification

**Manual Steps**:

1. Convert Org files to Markdown (if using Org-mode)
2. Copy graph folder to workspace
3. Verify block references
4. Check for broken wikilinks
5. Update TODO syntax if needed (Logseq uses different markers)
