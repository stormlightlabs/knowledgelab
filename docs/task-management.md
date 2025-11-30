# Task Management

Stormlight Labs Note Taker treats tasks as plain Markdown checkboxes, so your to‑dos live right next to the rest of your notes.
The app merely indexes what is already in your `.md` files, letting you review and filter tasks without jumping between documents.

## Writing Tasks

- Start a line with `- [ ]` for an open task or `- [x]` / `- [X]` for a completed one.
- Indentation is fine; the checkbox is detected even if the task sits inside a nested list.
- YAML frontmatter is ignored, so fields in the header never create extra tasks.

````markdown
## Action Items

- [ ] Outline migration plan ^task-migration-plan
- [x] Ship Search beta
    - [ ] Backfill scores for archived notes
````

### Optional Block IDs

Add a Logseq-style marker at the end of the line (`Task title ^friendly-id`) to keep a task’s identity stable when you move it between notes. If you skip the marker, the app generates an ID automatically—it just might change if the text is heavily rewritten.

## How Tasks Stay in Sync

Whenever a note is saved or re-indexed, every checkbox is scanned and copied into the workspace’s local database:

1. Each task records its note, line number, checkbox text, and whether it is completed.
2. Created/Completed timestamps are preserved so history survives edits.
3. Deleting or renaming a note automatically removes its tasks from the list.

Because all of this stays on disk inside your workspace, backing up `.knowledgelab/` (or the whole project) preserves both the Markdown and the task history. Even if the database disappears, reopening the workspace will rebuild it from the source `.md` files.

## Completing Tasks

- **Preview panel**: Click the checkbox. The app flips the Markdown line, saves the note, and refreshes the task list.
- **Editor**: Press `Cmd/Ctrl + T` to toggle the checkbox at your cursor without leaving edit mode.
- Git and other tools see the same result because the Markdown file is always updated first.

## Tasks Panel

Open the panel with `Cmd/Ctrl + Shift + X` or via the right-hand layout toggle. The panel shows:

- Pending vs. completed counts.
- A searchable list of tasks with note path, creation date, and completion date when applicable.
- Click any row to jump straight to the parent note.

### Filters

Use the filters at the top to narrow the list:

- **Status**: show all, only pending, or only completed items.
- **Note**: focus on tasks from a specific note.
- **Created After / Before**: confine results to a date range.
- **Completed After / Before**: spotlight recent wins.
- Clear filters with one click to return to the full list.

## Backup & Portability

Task data lives in `.knowledgelab/workspaces/{workspace-id}/graph.db` alongside the graph, backlinks, and search index.
Copy that directory (or the entire workspace) to preserve everything. Since the checkbox syntax is plain Markdown, you can always recreate the task list just by re-indexing your notes on a fresh install.
