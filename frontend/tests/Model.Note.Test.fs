module ModelNoteTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update (Notes)",
  fun () ->
    Jest.test (
      "BacklinksLoaded success updates backlinks",
      fun () ->
        let initialState = State.Default

        let testLinks = [
          {
            Source = "note1"
            Target = "note2"
            DisplayText = "Link to note2"
            Type = Wiki
            BlockRef = ""
          }
          {
            Source = "note3"
            Target = "note2"
            DisplayText = "Another link"
            Type = Wiki
            BlockRef = ""
          }
        ]

        let newState, _ = Update (BacklinksLoaded(Ok testLinks)) initialState
        Jest.expect(newState.Backlinks.Length).toEqual (testLinks.Length)
        Jest.expect(newState.Loading).toEqual (false)
        Jest.expect(newState.Error).toEqual (None)
    )

    Jest.test (
      "BacklinksLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load backlinks"
        let newState, _ = Update (BacklinksLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Loading).toEqual (false)
    )

    Jest.test (
      "ToggleTagFilter adds tag when not selected",
      fun () ->
        let initialState = { State.Default with SelectedTags = [] }
        let newState, _ = Update (ToggleTagFilter "test-tag") initialState
        Jest.expect(newState.SelectedTags |> List.toArray).toEqual ([| "test-tag" |])
    )

    Jest.test (
      "ToggleTagFilter removes tag when already selected",
      fun () ->
        let initialState = {
          State.Default with
              SelectedTags = [ "test-tag"; "other-tag" ]
        }

        let newState, _ = Update (ToggleTagFilter "test-tag") initialState
        Jest.expect(newState.SelectedTags |> List.toArray).toEqual ([| "other-tag" |])
    )

    Jest.test (
      "ToggleTagFilter handles multiple tag selections",
      fun () ->
        let initialState = State.Default
        let state1, _ = Update (ToggleTagFilter "tag1") initialState
        let state2, _ = Update (ToggleTagFilter "tag2") state1
        let state3, _ = Update (ToggleTagFilter "tag3") state2
        Jest.expect(state3.SelectedTags.Length).toEqual (3)
        Jest.expect(state3.SelectedTags).toContain ("tag1")
        Jest.expect(state3.SelectedTags).toContain ("tag2")
        Jest.expect(state3.SelectedTags).toContain ("tag3")
    )

    Jest.test (
      "SetTagFilterMode changes filter mode to And",
      fun () ->
        let initialState = { State.Default with TagFilterMode = Or }
        let newState, _ = Update (SetTagFilterMode And) initialState
        Jest.expect(newState.TagFilterMode).toEqual (And)
    )

    Jest.test (
      "SetTagFilterMode changes filter mode to Or",
      fun () ->
        let initialState = { State.Default with TagFilterMode = And }
        let newState, _ = Update (SetTagFilterMode Or) initialState
        Jest.expect(newState.TagFilterMode).toEqual (Or)
    )

    Jest.test (
      "ClearTagFilters removes all selected tags",
      fun () ->
        let initialState = {
          State.Default with
              SelectedTags = [ "tag1"; "tag2"; "tag3" ]
        }

        let newState, _ = Update ClearTagFilters initialState
        Jest.expect(newState.SelectedTags |> List.toArray).toEqual ([||])
    )

    Jest.test (
      "TagsWithCountsLoaded success updates tag infos",
      fun () ->
        let initialState = State.Default

        let testTagInfos = [
          {
            Name = "tag1"
            Count = 5
            NoteIds = [ "note1"; "note2"; "note3"; "note4"; "note5" ]
          }
          {
            Name = "tag2"
            Count = 3
            NoteIds = [ "note1"; "note3"; "note6" ]
          }
        ]

        let newState, _ = Update (TagsWithCountsLoaded(Ok testTagInfos)) initialState
        Jest.expect(newState.TagInfos.Length).toEqual (2)
        Jest.expect(newState.Loading).toEqual (false)
        Jest.expect(newState.Error).toEqual (None)
    )

    Jest.test (
      "TagsWithCountsLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load tags"
        let newState, _ = Update (TagsWithCountsLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Loading).toEqual (false)
    )

    Jest.test (
      "UpdateNoteContent updates current note content",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Old content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let initialState = { State.Default with CurrentNote = Some testNote }
        let newContent = "New content"
        let newState, _ = Update (UpdateNoteContent newContent) initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual (newContent)
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "UpdateNoteContent does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update (UpdateNoteContent "New content") initialState
        Jest.expect(newState.CurrentNote).toEqual (None)
    )

    Jest.test (
      "SelectNote triggers NoteLoaded message",
      fun () ->
        let initialState = { State.Default with Loading = false }
        let newState, _ = Update (SelectNote "note1") initialState
        Jest.expect(newState.Loading).toEqual (true)
    )

    Jest.test (
      "NoteSaved success triggers graph reload",
      fun () ->
        let initialState = { State.Default with Loading = true }
        let newState, cmd = Update (NoteSaved(Ok())) initialState

        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "NoteLoaded updates recent pages in workspace snapshot",
      fun () ->
        let testNote = {
          Id = "note-123"
          Title = "Test Note"
          Path = "/test/note.md"
          Content = "Content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let testSnapshot = {
          UI = {
            ActivePage = ""
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = []
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let initialState = { State.Default with WorkspaceSnapshot = Some testSnapshot }
        let newState, _ = Update (NoteLoaded(Ok testNote)) initialState

        match newState.WorkspaceSnapshot with
        | Some snapshot ->
          Jest.expect(snapshot.UI.ActivePage).toEqual testNote.Id
          Jest.expect(snapshot.UI.RecentPages.Length).toEqual 1
          Jest.expect(snapshot.UI.RecentPages.[0]).toEqual testNote.Id
        | None -> failwith "Expected workspace snapshot to be present"
    )

    Jest.test (
      "NoteLoaded ignores blank note IDs when updating recent pages",
      fun () ->
        let testNote = {
          Id = ""
          Title = "Invalid Note"
          Path = "/invalid.md"
          Content = "Content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let testSnapshot = {
          UI = {
            ActivePage = "existing.md"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = [ "existing.md" ]
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let initialState = { State.Default with WorkspaceSnapshot = Some testSnapshot }
        let newState, _ = Update (NoteLoaded(Ok testNote)) initialState

        match newState.WorkspaceSnapshot with
        | Some snapshot ->
          Jest.expect(snapshot.UI.RecentPages.Length).toEqual 1
          Jest.expect(snapshot.UI.RecentPages.[0]).toEqual "existing.md"
        | None -> failwith "Expected workspace snapshot to be present"
    )

    Jest.test (
      "NoteLoaded with existing recent pages moves duplicate to front",
      fun () ->
        let testNote = {
          Id = "note-b"
          Title = "Test Note B"
          Path = "/test/b.md"
          Content = "Content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let testSnapshot = {
          UI = {
            ActivePage = "note-a"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = [ "note-a"; "note-b"; "note-c" ]
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let initialState = { State.Default with WorkspaceSnapshot = Some testSnapshot }
        let newState, _ = Update (NoteLoaded(Ok testNote)) initialState

        match newState.WorkspaceSnapshot with
        | Some snapshot ->
          Jest.expect(snapshot.UI.RecentPages.[0]).toEqual "note-b"
          Jest.expect(snapshot.UI.RecentPages.[1]).toEqual "note-a"
          Jest.expect(snapshot.UI.RecentPages.[2]).toEqual "note-c"
          Jest.expect(snapshot.UI.RecentPages.Length).toEqual 3
        | None -> failwith "Expected workspace snapshot to be present"
    )

    Jest.test (
      "UpdateNoteContent marks editor as dirty",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Old content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = { State.Default.EditorState with IsDirty = false }
        }

        let newState, _ = Update (UpdateNoteContent "New content") initialState
        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "UpdateSearchQuery updates the search query",
      fun () ->
        let initialState = State.Default
        let query = "test query"
        let newState, _ = Update (UpdateSearchQuery query) initialState
        Jest.expect(newState.Search.Query).toEqual query
    )

    Jest.test (
      "UpdateSearchFilters updates search filters",
      fun () ->
        let initialState = State.Default

        let filters = {
          Tags = [ "tag1"; "tag2" ]
          PathPrefix = "notes/"
          DateFrom = Some(System.DateTime(2025, 1, 1))
          DateTo = Some(System.DateTime(2025, 12, 31))
        }

        let newState, _ = Update (UpdateSearchFilters filters) initialState
        Jest.expect(newState.Search.Filters.Tags.Length).toEqual 2
        Jest.expect(newState.Search.Filters.PathPrefix).toEqual "notes/"
        Jest.expect(newState.Search.Filters.DateFrom.IsSome).toEqual true
    )
)
