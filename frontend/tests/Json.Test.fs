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
        | Error err -> failwith ($"Decode failed: {err}")
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
)
