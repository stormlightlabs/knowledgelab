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
        Jest.expect(newState.Graph.IsSome).toEqual true
        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Error).toEqual None
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

    Jest.test (
      "GraphNodeHovered clears hover when None",
      fun () ->
        let initialState = { State.Default with HoveredNode = Some "note1" }
        let newState, _ = Update (GraphNodeHovered None) initialState
        Jest.expect(newState.HoveredNode).toEqual (None)
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
      "SettingsLoaded success updates settings",
      fun () ->
        let initialState = State.Default

        let testSettings = {
          General = {
            Theme = "dark"
            Language = "en"
            AutoSave = true
            AutoSaveInterval = 30
          }
          Editor = {
            FontFamily = "monospace"
            FontSize = 14
            LineHeight = 1.6
            TabSize = 2
            VimMode = false
            SpellCheck = true
          }
        }

        let newState, _ = Update (SettingsLoaded(Ok testSettings)) initialState
        Jest.expect(newState.Settings.IsSome).toEqual true
        Jest.expect(newState.Error).toEqual None

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.Theme).toEqual "dark"
          Jest.expect(s.Editor.FontSize).toEqual 14
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load settings"
        let newState, _ = Update (SettingsLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Settings).toEqual None
    )

    Jest.test (
      "WorkspaceSnapshotLoaded success updates snapshot",
      fun () ->
        let initialState = State.Default

        let testSnapshot = {
          UI = {
            ActivePage = "test-note.md"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = [ "page1.md"; "page2.md" ]
            RecentPages = [ "recent1.md" ]
            GraphLayout = "force"
          }
        }

        let newState, _ = Update (WorkspaceSnapshotLoaded(Ok testSnapshot)) initialState
        Jest.expect(newState.WorkspaceSnapshot.IsSome).toEqual true
        Jest.expect(newState.Error).toEqual None

        match newState.WorkspaceSnapshot with
        | Some s ->
          Jest.expect(s.UI.ActivePage).toEqual "test-note.md"
          Jest.expect(s.UI.SidebarWidth).toEqual 280
          Jest.expect(s.UI.PinnedPages.Length).toEqual 2
        | None -> failwith "Expected workspace snapshot to be present"
    )

    Jest.test (
      "WorkspaceSnapshotLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load workspace snapshot"
        let newState, _ = Update (WorkspaceSnapshotLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.WorkspaceSnapshot).toEqual None
    )

    Jest.test (
      "SettingsChanged updates settings and triggers save",
      fun () ->
        let initialState = State.Default

        let testSettings = {
          General = {
            Theme = "light"
            Language = "en"
            AutoSave = false
            AutoSaveInterval = 60
          }
          Editor = {
            FontFamily = "Inter"
            FontSize = 16
            LineHeight = 1.8
            TabSize = 4
            VimMode = true
            SpellCheck = false
          }
        }

        let newState, _ = Update (SettingsChanged testSettings) initialState
        Jest.expect(newState.Settings.IsSome).toEqual true

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.Theme).toEqual "light"
          Jest.expect(s.Editor.FontSize).toEqual 16
          Jest.expect(s.Editor.VimMode).toEqual true
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "WorkspaceSnapshotChanged updates snapshot and triggers save",
      fun () ->
        let initialState = State.Default

        let testSnapshot = {
          UI = {
            ActivePage = "new-note.md"
            SidebarVisible = false
            SidebarWidth = 350
            RightPanelVisible = true
            RightPanelWidth = 400
            PinnedPages = [ "pinned1.md" ]
            RecentPages = []
            GraphLayout = "tree"
          }
        }

        let newState, _ = Update (WorkspaceSnapshotChanged testSnapshot) initialState
        Jest.expect(newState.WorkspaceSnapshot.IsSome).toEqual true

        match newState.WorkspaceSnapshot with
        | Some s ->
          Jest.expect(s.UI.ActivePage).toEqual "new-note.md"
          Jest.expect(s.UI.SidebarVisible).toEqual false
          Jest.expect(s.UI.SidebarWidth).toEqual 350
          Jest.expect(s.UI.GraphLayout).toEqual "tree"
        | None -> failwith "Expected workspace snapshot to be present"
    )

    Jest.test (
      "SettingsSaved success clears error",
      fun () ->
        let initialState = { State.Default with Error = Some "Previous error" }
        let newState, _ = Update (SettingsSaved(Ok())) initialState
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "SettingsSaved error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to save settings"
        let newState, _ = Update (SettingsSaved(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
    )

    Jest.test (
      "WorkspaceSnapshotSaved success clears error",
      fun () ->
        let initialState = { State.Default with Error = Some "Previous error" }
        let newState, _ = Update (WorkspaceSnapshotSaved(Ok())) initialState
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "WorkspaceSnapshotSaved error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to save workspace snapshot"
        let newState, _ = Update (WorkspaceSnapshotSaved(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
    )
)

Jest.describe (
  "GraphView.buildNeighborMap",
  fun () ->
    Jest.test (
      "builds neighbor map with bidirectional links",
      fun () ->
        let links = [
          { Source = "note1"; Target = "note2"; Value = 1.0 }
          { Source = "note2"; Target = "note3"; Value = 1.0 }
        ]

        let neighborMap = GraphView.buildNeighborMap links

        Jest.expect(neighborMap.ContainsKey "note1").toEqual (true)
        Jest.expect(neighborMap.ContainsKey "note2").toEqual (true)
        Jest.expect(neighborMap.ContainsKey "note3").toEqual (true)

        Jest.expect(neighborMap.["note1"].Contains "note2").toEqual (true)
        Jest.expect(neighborMap.["note2"].Contains "note1").toEqual (true)
        Jest.expect(neighborMap.["note2"].Contains "note3").toEqual (true)
        Jest.expect(neighborMap.["note3"].Contains "note2").toEqual (true)
    )

    Jest.test (
      "areNeighbors returns true for connected nodes",
      fun () ->
        let links = [ { Source = "note1"; Target = "note2"; Value = 1.0 } ]
        let neighborMap = GraphView.buildNeighborMap links

        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note2").toEqual (true)
        Jest.expect(GraphView.areNeighbors neighborMap "note2" "note1").toEqual (true)
    )

    Jest.test (
      "areNeighbors returns false for unconnected nodes",
      fun () ->
        let links = [ { Source = "note1"; Target = "note2"; Value = 1.0 } ]
        let neighborMap = GraphView.buildNeighborMap links
        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note3").toEqual (false)
    )

    Jest.test (
      "areNeighbors returns false for nonexistent nodes",
      fun () ->
        let neighborMap = GraphView.buildNeighborMap []
        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note2").toEqual (false)
    )
)

Jest.describe (
  "GraphView.graphToGraphData",
  fun () ->
    Jest.test (
      "converts Graph to GraphData with correct degree calculation",
      fun () ->
        let graph = {
          Nodes = [ "note1"; "note2"; "note3" ]
          Edges = [
            { Source = "note1"; Target = "note2"; Type = "wiki" }
            { Source = "note2"; Target = "note3"; Type = "wiki" }
            { Source = "note1"; Target = "note3"; Type = "wiki" }
          ]
        }

        let graphData = GraphView.graphToGraphData graph

        Jest.expect(graphData.Nodes.Length).toEqual (3)
        Jest.expect(graphData.Links.Length).toEqual (3)

        let note1 = graphData.Nodes |> List.find (fun n -> n.Id = "note1")
        Jest.expect(note1.Degree).toEqual (2)

        let note2 = graphData.Nodes |> List.find (fun n -> n.Id = "note2")
        Jest.expect(note2.Degree).toEqual (2)

        let note3 = graphData.Nodes |> List.find (fun n -> n.Id = "note3")
        Jest.expect(note3.Degree).toEqual (2)
    )

    Jest.test (
      "handles nodes with no connections",
      fun () ->
        let graph = { Nodes = [ "note1"; "note2" ]; Edges = [] }
        let graphData = GraphView.graphToGraphData graph

        Jest.expect(graphData.Nodes.Length).toEqual (2)
        Jest.expect(graphData.Links.Length).toEqual (0)

        let note1 = graphData.Nodes |> List.find (fun n -> n.Id = "note1")
        Jest.expect(note1.Degree).toEqual (0)
    )
)
