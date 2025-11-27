module ModelTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update",
  fun () ->
    Jest.test (
      "NavigateTo changes the current route",
      fun () ->
        let initialState = State.Default
        let newRoute = NoteList
        let newState, _ = Update (NavigateTo newRoute) initialState
        Jest.expect(newState.CurrentRoute).toEqual (newRoute)
    )

    Jest.test (
      "ClearError removes error message",
      fun () ->
        let stateWithError = { State.Default with Error = Some "Test error" }

        let newState, _ = Update ClearError stateWithError
        Jest.expect(newState.Error).toEqual (None)
    )

    Jest.test (
      "TogglePanel adds panel when not present",
      fun () ->
        let initialState = { State.Default with VisiblePanels = Set.empty }

        let newState, _ = Update (TogglePanel Backlinks) initialState
        Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (true)
    )

    Jest.test (
      "TogglePanel removes panel when present",
      fun () ->
        let initialState = {
          State.Default with
              VisiblePanels = Set.ofList [ Backlinks ]
        }

        let newState, _ = Update (TogglePanel Backlinks) initialState
        Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (false)
    )

    Jest.test (
      "UpdateSearchQuery updates the search query",
      fun () ->
        let initialState = State.Default
        let query = "test query"
        let newState, _ = Update (UpdateSearchQuery query) initialState
        Jest.expect(newState.SearchQuery).toEqual (query)
    )

    Jest.test (
      "SetError sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Test error"
        let newState, _ = Update (SetError errorMsg) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
    )

    Jest.test (
      "Init returns default state",
      fun () ->
        let state, _ = Init()
        Jest.expect(state.Workspace).toEqual (None)
        Jest.expect(state.Notes.IsEmpty).toEqual (true)
        Jest.expect(state.CurrentNote).toEqual (None)
        Jest.expect(state.CurrentRoute).toEqual (WorkspacePicker)
    )

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
      "UpdateNoteContent updates current note content",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Old content"
          Frontmatter = Map.empty
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
      "GraphLoaded success updates graph",
      fun () ->
        let initialState = State.Default

        let testGraph = {
          Nodes = [ "note1"; "note2"; "note3" ]
          Edges = [
            { Source = "note1"; Target = "note2"; Type = "wiki" }
            { Source = "note2"; Target = "note3"; Type = "wiki" }
          ]
        }

        let newState, _ = Update (GraphLoaded(Ok testGraph)) initialState
        Jest.expect(newState.Graph.IsSome).toEqual (true)
        Jest.expect(newState.Loading).toEqual (false)
        Jest.expect(newState.Error).toEqual (None)
    )

    Jest.test (
      "GraphLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load graph"
        let newState, _ = Update (GraphLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Loading).toEqual (false)
    )

    Jest.test (
      "GraphNodeHovered updates hovered node",
      fun () ->
        let initialState = State.Default
        let nodeId = "note1"
        let newState, _ = Update (GraphNodeHovered(Some nodeId)) initialState
        Jest.expect(newState.HoveredNode).toEqual (Some nodeId)
    )

    Jest.test (
      "GraphZoomChanged updates zoom state",
      fun () ->
        let initialState = State.Default

        let newZoomState = { Scale = 1.5; TranslateX = 10.0; TranslateY = 20.0 }

        let newState, _ = Update (GraphZoomChanged newZoomState) initialState
        Jest.expect(newState.ZoomState.Scale).toEqual (1.5)
        Jest.expect(newState.ZoomState.TranslateX).toEqual (10.0)
        Jest.expect(newState.ZoomState.TranslateY).toEqual (20.0)
    )

    Jest.test (
      "GraphEngineChanged updates graph engine",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (GraphEngineChanged Canvas) initialState
        Jest.expect(newState.GraphEngine).toEqual (Canvas)
    )
)
