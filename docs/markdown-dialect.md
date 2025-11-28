# Markdown Dialect Specification

This application uses a Markdown dialect based on **CommonMark** with extensions inspired by Obsidian and Logseq.
This document specifies the supported syntax, parsing rules, and conventions.

## Base

The core Markdown parsing is based on CommonMark, implemented using the `goldmark` library for Go. All standard CommonMark syntax is supported.

## Frontmatter

Notes support YAML frontmatter for metadata. Frontmatter is optional and appears at the beginning of a file.
TOML support is planned.

### Syntax

```markdown
---
title: Note Title
aliases:
  - Alternative Name
  - Shorthand
type: meeting
tags:
  - project
  - important
created: 2025-01-15T10:00:00Z
modified: 2025-01-20T15:30:00Z
custom_field: custom_value
---

# Note Content

Body of the note...
```

### Standard Fields

The following fields have special meaning and are extracted into typed fields:

#### `title` (string)

- Note title used in lists, links, and navigation
- Falls back to first H1 heading if not specified
- Falls back to filename if neither frontmatter nor H1 present
- Example: `title: Meeting Notes`

#### `aliases` (array or string)

- Alternative titles for wikilink resolution
- Single string or array of strings
- Enables `[[alias]]` to resolve to this note
- Example: `aliases: [shortname, alternate-title]`

#### `type` (string)

- Note type or template identifier
- Used for categorization and template application
- Common values: `daily`, `meeting`, `project`, `person`
- Example: `type: meeting`

#### `tags` (array or string)

- Topic tags for organization and filtering
- Supplements inline `#tags` found in content
- Single string or array of strings
- Example: `tags: [project-x, priority-high]`

#### `created` (timestamp)

- Note creation timestamp
- Auto-populated from file metadata if not specified
- Supported formats: RFC3339, ISO8601, `YYYY-MM-DD`
- Example: `created: 2025-01-15T10:00:00Z`

#### `modified` (timestamp)

- Last modification timestamp
- **Auto-updated on every save**
- Overrides file system metadata
- Supported formats: RFC3339, ISO8601, `YYYY-MM-DD`
- Example: `modified: 2025-01-20T15:30:00Z`

### Additional Fields

Any additional YAML fields are preserved in a generic map and round-trip through save operations. Examples:

```yaml
---
author: John Doe
project: ProjectX
status: draft
priority: 3
---
```

These fields are not parsed into specific types but are accessible via the `Frontmatter` map.

### Parsing Rules

1. **Delimiters**: Frontmatter must start with `---` on the first line
2. **Closing**: Frontmatter ends with a line containing only `---`
3. **YAML Format**: Content between delimiters must be valid YAML
4. **Error Handling**: Invalid YAML frontmatter causes note loading to fail with a descriptive error
5. **Whitespace**: Blank lines after closing `---` are preserved
6. **Standard Field Extraction**: Standard fields are removed from the generic frontmatter map

### Round-Trip Preservation

When saving notes:

- Standard fields are written back to frontmatter
- Additional fields are preserved exactly
- Timestamp fields are formatted as RFC3339
- Empty arrays/fields are omitted from output

## Wikilinks

Wikilinks provide wiki-style linking between notes using `[[target]]` syntax.

### Basic Syntax

```markdown
[[Note Title]]
[[folder/subfolder/note]]
```

### Display Text

```markdown
[[Note Title|Custom Display Text]]
```

### Resolution Rules

Wikilinks are resolved in the following order:

1. **Exact Match**: Match against note titles (case-sensitive)
2. **Alias Match**: Match against note aliases from frontmatter
3. **Path Match**: Match against relative file paths
4. **Fallback**: Create broken link indicator if no match found

Examples:

```yaml
---
title: Project Management
aliases:
  - PM
  - proj-mgmt
---
```

All of these resolve to the same note:

- `[[Project Management]]`
- `[[PM]]`
- `[[proj-mgmt]]`

### Block References

Reference specific blocks within notes:

```markdown
[[Note Title#^block-id]]
[[#^block-id]]  # Same note
```

Block IDs use the format `^[a-z0-9-]+` and appear at the end of lines.

### Embed Links

Embed note content inline:

```markdown
![[Note Title]]
![[Note Title#^block-id]]
```

## Tags

Tags categorize and organize notes.

### Inline Tags

```markdown
#tag-name
#nested/tag/structure
```

### Frontmatter Tags

```yaml
---
tags:
  - project-x
  - meeting
  - priority-high
---
```

Both inline and frontmatter tags are indexed and searchable.

### Tag Naming

- Alphanumeric characters, hyphens, underscores, forward slashes
- Case-sensitive
- No spaces (use hyphens: `#my-tag`)
- Nested with forward slashes: `#projects/active/priority`

## Block Structure

Content is parsed into outline blocks for granular editing and linking.

### Block Types

- **Paragraph**: Regular text paragraphs
- **Heading**: H1-H6 headings
- **List Item**: Bulleted and numbered list items
- **Code Block**: Fenced code blocks
- **Quote**: Blockquote sections

### Block IDs

Each block receives a unique identifier:

- Format: SHA-256 hash of note ID and position
- Used for block references and links
- Stable across edits if position doesn't change
