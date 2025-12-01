---
title: Welcome to Knowledge Lab
tags: [tutorial, getting-started]
created: {{.CreatedAt}}
---

Welcome to your new knowledge workspace! This guide will help you learn the ropes.

## What is Knowledge Lab?

Knowledge Lab is a local-first, graph-based notes application inspired by Obsidian and Logseq. All your notes are stored as simple markdown files on your computer, giving you full ownership and control.

## Getting Started

### Creating Notes

- Use the "New Note" button in the sidebar to create a new note
- Use "Today's Note" to create or open a daily note (format: YYYY-MM-DD)
- Notes are stored as `.md` (markdown) files in your workspace folder

### Tags

Tags help you organize and categorize your notes. You can add tags in two ways:

1. **Frontmatter tags** (like this document):

   ```yaml
   ---
   tags: [tutorial, getting-started]
   ---
   ```

2. **Inline tags** using the `#` symbol:
   - #project/alpha
   - #idea
   - #important

Try creating your own tags! The Tags panel on the right shows all tags with counts.

- [ ] #tutorial Create a note with a custom tag
- [ ] #tutorial Explore the Tags panel and try filtering notes by tag

### Wikilinks & Backlinks

Connect your notes using wikilinks: `[[Note Name]]`

For example, you could link to [[My First Idea]] or [[Project Planning]]. When you create these links, you can click them to create or navigate to those notes.

The Backlinks panel shows all notes that link to the current note, creating a knowledge graph.

- [ ] #tutorial Create a wikilink to a new note
- [ ] #tutorial Check the Backlinks panel to see connections

### Tasks & TODOs

Knowledge Lab tracks tasks across all your notes. Any checkbox in markdown becomes a trackable task:

- [ ] #tutorial Click this checkbox to mark it complete
- [ ] #tutorial Create a new task in another note
- [x] #tutorial This is an example of a completed task

The Tasks panel shows all tasks across your workspace with powerful filtering options.

### Markdown Formatting

Knowledge Lab supports standard markdown:

- **Bold text** with ` **text** `
- *Italic text* with ` *text* `
- `Inline code` with backticks
- Code blocks with triple backticks

```javascript
// Example code block
function hello() {
  console.log("Hello, Knowledge Lab!");
}
```

- [ ] #tutorial Try formatting some text with markdown

### Preview Modes

Use the toolbar buttons to switch between:

- **Edit Only** - Pure markdown editing
- **Preview Only** - Rendered markdown view
- **Split View** - Edit and preview side-by-side (Ctrl/Cmd+Shift+P)

### Graph View

Click "Graph" in the navigation to see your knowledge graph. Notes are nodes, and wikilinks are edges connecting them.

- Hover over nodes to highlight connections
- Click nodes to navigate to that note
- Watch your graph grow as you create more interconnected notes

- [ ] #tutorial Create a few connected notes and explore the graph view

### Search

Use the search feature to find content across all your notes:

- Full-text search with BM25 ranking
- Filter by tags, paths, and dates
- Results show matching snippets

## Next Steps

Now that you understand the basics, here are some ideas for building your knowledge base:

- [ ] #getting-started Create a note about a project you're working on
- [ ] #getting-started Create a daily note for today
- [ ] #getting-started Organize notes with a tag taxonomy (e.g., #project/name, #area/topic)
- [ ] #getting-started Link related notes together with wikilinks
- [ ] #getting-started Explore the settings to customize your experience

## Your Notes Are Yours

Remember: All your notes are stored as plain markdown files in the workspace folder you selected. You can:

- Edit them with any text editor
- Version them with git
- Back them up however you like
- Move them between different knowledge management tools

Knowledge Lab builds an index and graph on top of your files, but never locks you in.

---

Happy note-taking! If you want to remove this tutorial, just delete the `Welcome.md` file.
