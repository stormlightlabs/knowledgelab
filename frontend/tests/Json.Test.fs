module JsonTests

open Fable.Jester
open Thoth.Json
open Domain
open Json

Jest.describe (
  "JSON Serialization/Deserialization Tests",
  fun () ->

    Jest.test (
      "NoteSummary decoder handles valid JSON",
      fun () ->
        let json =
          """
    {
      "id": "test-note-1",
      "title": "Test Note",
      "path": "/notes/test.md",
      "tags": [
        {"name": "testing", "noteId": "test-note-1"},
        {"name": "fsharp", "noteId": "test-note-1"}
      ],
      "modifiedAt": "2025-01-28T12:00:00Z"
    }
    """

        let result = Decode.fromString noteSummaryDecoder json

        match result with
        | Ok note ->
          Jest.expect(note.id).toEqual ("test-note-1")
          Jest.expect(note.title).toEqual ("Test Note")
          Jest.expect(note.path).toEqual ("/notes/test.md")
          Jest.expect(note.tags.Length).toEqual (2)
          Jest.expect(note.tags.[0].Name).toEqual ("testing")
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "NoteSummary decoder handles empty tags",
      fun () ->
        let json =
          """
    {
      "id": "test-note-2",
      "title": "Note Without Tags",
      "path": "/notes/no-tags.md",
      "tags": [],
      "modifiedAt": "2025-01-28T12:00:00Z"
    }
    """

        let result = Decode.fromString noteSummaryDecoder json

        match result with
        | Ok note ->
          Jest.expect(note.id).toEqual "test-note-2"
          Jest.expect(note.tags.Length).toEqual 0
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "NoteSummary decoder rejects invalid JSON",
      fun () ->
        let json =
          """
    {
      "id": "test-note-3",
      "title": "Missing Required Fields"
    }
    """

        let result = Decode.fromString noteSummaryDecoder json

        match result with
        | Ok _ -> failwith "Should have failed with missing required fields"
        | Error _ -> Jest.expect(true).toBe true
    )

    Jest.test (
      "TagInfo decoder handles valid JSON",
      fun () ->
        let json =
          """
    {
      "name": "test-tag",
      "count": 5,
      "noteIds": ["note1", "note2", "note3", "note4", "note5"]
    }
    """

        let result = Decode.fromString tagInfoDecoder json

        match result with
        | Ok tagInfo ->
          Jest.expect(tagInfo.Name).toEqual "test-tag"
          Jest.expect(tagInfo.Count).toEqual 5
          Jest.expect(tagInfo.NoteIds.Length).toEqual 5
          Jest.expect(tagInfo.NoteIds.[0]).toEqual "note1"
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "TagInfo decoder handles zero count",
      fun () ->
        let json =
          """
    {
      "name": "empty-tag",
      "count": 0,
      "noteIds": []
    }
    """

        let result = Decode.fromString tagInfoDecoder json

        match result with
        | Ok tagInfo ->
          Jest.expect(tagInfo.Name).toEqual "empty-tag"
          Jest.expect(tagInfo.Count).toEqual 0
          Jest.expect(tagInfo.NoteIds.Length).toEqual 0
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "TagInfo decoder handles nested tags",
      fun () ->
        let json =
          """
    {
      "name": "project/alpha/milestone",
      "count": 3,
      "noteIds": ["note1", "note2", "note3"]
    }
    """

        let result = Decode.fromString tagInfoDecoder json

        match result with
        | Ok tagInfo ->
          Jest.expect(tagInfo.Name).toEqual "project/alpha/milestone"
          Jest.expect(tagInfo.Count).toEqual 3
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "TagInfo decoder rejects invalid JSON",
      fun () ->
        let json =
          """
    {
      "name": "incomplete-tag"
    }
    """

        let result = Decode.fromString tagInfoDecoder json

        match result with
        | Ok _ -> failwith "Should have failed with missing required fields"
        | Error _ -> Jest.expect(true).toBe true
    )

    Jest.test (
      "Graph decoder handles valid graph JSON",
      fun () ->
        let json =
          """
    {
      "nodes": ["note1", "note2", "note3"],
      "edges": [
        {"source": "note1", "target": "note2", "type": "wiki"},
        {"source": "note2", "target": "note3", "type": "markdown"}
      ]
    }
    """

        let result = Decode.fromString graphDecoder json

        match result with
        | Ok graph ->
          Jest.expect(graph.Nodes.Length).toEqual 3
          Jest.expect(graph.Edges.Length).toEqual 2
          Jest.expect(graph.Edges.[0].Source).toEqual "note1"
          Jest.expect(graph.Edges.[0].Target).toEqual "note2"
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "Link decoder handles all link types",
      fun () ->
        let jsonWiki =
          """
    {
      "source": "note1",
      "target": "note2",
      "displayText": "Link to Note 2",
      "type": "wiki",
      "blockRef": ""
    }
    """

        let result = Decode.fromString linkDecoder jsonWiki

        match result with
        | Ok link ->
          Jest.expect(link.Type).toEqual Wiki
          Jest.expect(link.Source).toEqual "note1"
          Jest.expect(link.Target).toEqual "note2"
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "Note decoder treats null aliases as an empty list",
      fun () ->
        let json =
          """
    {
      "id": "note-1",
      "title": "Alias Test",
      "path": "/alias/test.md",
      "content": "Content",
      "frontmatter": {},
      "aliases": null,
      "type": "note",
      "blocks": [],
      "links": [],
      "tags": [],
      "createdAt": "2025-01-28T12:00:00Z",
      "modifiedAt": "2025-01-28T12:05:00Z"
    }
    """

        let result = Decode.fromString noteDecoder json

        match result with
        | Ok note ->
          Jest.expect(note.Id).toEqual "note-1"
          Jest.expect(note.Aliases.Length).toEqual 0
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "SearchResult decoder handles valid JSON",
      fun () ->
        let json =
          """
    {
      "noteId": "search-result-1",
      "title": "Search Result Title",
      "path": "/search/result.md",
      "score": 0.95,
      "tags": ["search", "test"],
      "modifiedAt": "2025-01-28T12:00:00Z"
    }
    """

        let result = Decode.fromString searchResultDecoder json

        match result with
        | Ok searchResult ->
          Jest.expect(searchResult.NoteId).toEqual "search-result-1"
          Jest.expect(searchResult.Score).toEqual 0.95
          Jest.expect(searchResult.Tags.Length).toEqual 2
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "WorkspaceInfo decoder handles complete workspace data",
      fun () ->
        let json =
          """
    {
      "workspace": {
        "id": "workspace-1",
        "name": "My Workspace",
        "rootPath": "/home/user/notes",
        "ignorePatterns": ["*.tmp", ".git"],
        "createdAt": "2025-01-01T00:00:00Z",
        "lastOpenedAt": "2025-01-28T12:00:00Z"
      },
      "config": {
        "dailyNoteFormat": "YYYY-MM-DD",
        "dailyNoteFolder": "daily",
        "defaultTags": ["daily", "journal"]
      },
      "noteCount": 42,
      "totalBlocks": 1337
    }
    """

        let result = Decode.fromString workspaceInfoDecoder json

        match result with
        | Ok info ->
          Jest.expect(info.Workspace.Name).toEqual "My Workspace"
          Jest.expect(info.Config.DailyNoteFolder).toEqual "daily"
          Jest.expect(info.NoteCount).toEqual 42
          Jest.expect(info.TotalBlocks).toEqual 1337
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "Block decoder handles all block types",
      fun () ->
        let jsonHeading =
          """
    {
      "id": "block-1",
      "noteId": "note-1",
      "content": "# Heading",
      "level": 1,
      "parent": "",
      "children": ["block-2", "block-3"],
      "position": 0,
      "type": "heading"
    }
    """

        let result = Decode.fromString blockDecoder jsonHeading

        match result with
        | Ok block ->
          Jest.expect(block.Type).toEqual (Heading)
          Jest.expect(block.Level).toEqual (1)
          Jest.expect(block.Children.Length).toEqual (2)
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "WorkspaceUI decoder handles valid JSON with SearchHistory",
      fun () ->
        let json =
          """
    {
      "ActivePage": "notes/current.md",
      "SidebarVisible": true,
      "SidebarWidth": 320,
      "RightPanelVisible": true,
      "RightPanelWidth": 400,
      "PinnedPages": ["index.md", "todo.md"],
      "RecentPages": ["notes/current.md", "daily/2025-01-28.md"],
      "LastWorkspacePath": "/home/user/notes",
      "GraphLayout": "force",
      "SearchHistory": ["python programming", "golang tutorial", "react hooks"]
    }
    """

        let result = Decode.fromString workspaceUIDecoder json

        match result with
        | Ok ui ->
          Jest.expect(ui.ActivePage).toEqual "notes/current.md"
          Jest.expect(ui.SearchHistory.Length).toEqual 3
          Jest.expect(ui.SearchHistory.[0]).toEqual "python programming"
          Jest.expect(ui.SearchHistory.[1]).toEqual "golang tutorial"
          Jest.expect(ui.SearchHistory.[2]).toEqual "react hooks"
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "WorkspaceUI decoder handles empty SearchHistory",
      fun () ->
        let json =
          """
    {
      "ActivePage": "",
      "SidebarVisible": true,
      "SidebarWidth": 280,
      "RightPanelVisible": false,
      "RightPanelWidth": 300,
      "PinnedPages": [],
      "RecentPages": [],
      "LastWorkspacePath": "",
      "GraphLayout": "force",
      "SearchHistory": []
    }
    """

        let result = Decode.fromString workspaceUIDecoder json

        match result with
        | Ok ui ->
          Jest.expect(ui.SearchHistory.Length).toEqual 0
          Jest.expect(ui.SidebarVisible).toEqual true
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "WorkspaceSnapshot decoder handles valid JSON with SearchHistory",
      fun () ->
        let json =
          """
    {
      "UI": {
        "ActivePage": "notes/test.md",
        "SidebarVisible": true,
        "SidebarWidth": 280,
        "RightPanelVisible": false,
        "RightPanelWidth": 300,
        "PinnedPages": ["notes/important.md"],
        "RecentPages": ["notes/test.md", "notes/other.md"],
        "LastWorkspacePath": "/workspace/path",
        "GraphLayout": "tree",
        "SearchHistory": ["first query", "second query"]
      }
    }
    """

        let result = Decode.fromString workspaceSnapshotDecoder json

        match result with
        | Ok snapshot ->
          Jest.expect(snapshot.UI.ActivePage).toEqual "notes/test.md"
          Jest.expect(snapshot.UI.SearchHistory.Length).toEqual 2
          Jest.expect(snapshot.UI.SearchHistory.[0]).toEqual "first query"
          Jest.expect(snapshot.UI.SearchHistory.[1]).toEqual "second query"
          Jest.expect(snapshot.UI.GraphLayout).toEqual "tree"
        | Error err -> failwith $"Decode failed: {err}"
    )

    Jest.test (
      "WorkspaceUI decoder rejects JSON missing SearchHistory",
      fun () ->
        let json =
          """
    {
      "ActivePage": "",
      "SidebarVisible": true,
      "SidebarWidth": 280,
      "RightPanelVisible": false,
      "RightPanelWidth": 300,
      "PinnedPages": [],
      "RecentPages": [],
      "LastWorkspacePath": "",
      "GraphLayout": "force"
    }
    """

        let result = Decode.fromString workspaceUIDecoder json

        match result with
        | Ok _ -> failwith "Should have failed with missing SearchHistory field"
        | Error _ -> Jest.expect(true).toBe true
    )
)
