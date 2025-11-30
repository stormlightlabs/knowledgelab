module ModelTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update",
  fun () ->
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
            LastWorkspacePath = "/workspace"
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
      "WorkspaceSnapshotLoaded filters out blank recent entries",
      fun () ->
        let snapshotWithBlanks = {
          UI = {
            ActivePage = ""
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = [ ""; "note-a.md"; " " ]
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
          }
        }

        let newState, _ =
          Update (WorkspaceSnapshotLoaded(Ok snapshotWithBlanks)) State.Default

        match newState.WorkspaceSnapshot with
        | Some s ->
          Jest.expect(s.UI.RecentPages.Length).toEqual 1
          Jest.expect(s.UI.RecentPages.[0]).toEqual "note-a.md"
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
      "ClearRecentFiles triggers loading state",
      fun () ->
        let snapshot = {
          UI = {
            ActivePage = "note-a"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = [ "note-a"; "note-b" ]
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
          }
        }

        let initialState = { State.Default with WorkspaceSnapshot = Some snapshot }
        let newState, _ = Update ClearRecentFiles initialState
        Jest.expect(newState.Loading).toEqual true
    )

    Jest.test (
      "RecentFilesCleared success updates snapshot",
      fun () ->
        let snapshot = {
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
          }
        }

        let initialState = { State.Default with Loading = true }
        let newState, _ = Update (RecentFilesCleared(Ok snapshot)) initialState

        match newState.WorkspaceSnapshot with
        | Some updated -> Jest.expect(updated.UI.RecentPages.Length).toEqual 0
        | None -> failwith "Expected snapshot"

        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Success).toEqual (Some "Recent files cleared")
    )

    Jest.test (
      "RecentFilesCleared error sets error message",
      fun () ->
        let errorMsg = "Failed to clear"
        let initialState = { State.Default with Loading = true }
        let newState, _ = Update (RecentFilesCleared(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Success).toEqual None
    )

    Jest.test (
      "ClearSuccess removes success notification",
      fun () ->
        let initialState = { State.Default with Success = Some "Saved" }
        let newState, _ = Update ClearSuccess initialState
        Jest.expect(newState.Success).toEqual None
    )

    Jest.test (
      "OpenRecentFile without workspace path sets friendly error",
      fun () ->
        let newState, _ = Update (OpenRecentFile("", "note.md")) State.Default
        Jest.expect(newState.Error.IsSome).toEqual true
        Jest.expect(newState.PendingWorkspacePath).toEqual None
        Jest.expect(newState.PendingNoteToOpen).toEqual None
    )

    Jest.test (
      "OpenRecentFile stores pending state when workspace needs to change",
      fun () ->
        let workspacePath = "/workspace"
        let noteId = "note.md"
        let newState, _ = Update (OpenRecentFile(workspacePath, noteId)) State.Default
        Jest.expect(newState.PendingWorkspacePath).toEqual (Some workspacePath)
        Jest.expect(newState.PendingNoteToOpen).toEqual (Some noteId)
    )

    Jest.test (
      "WorkspaceOpened updates snapshot with last workspace path",
      fun () ->
        let snapshot = {
          UI = {
            ActivePage = ""
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = []
            LastWorkspacePath = ""
            GraphLayout = "force"
          }
        }

        let workspaceInfo = {
          Workspace = {
            Id = "ws-id"
            Name = "Workspace"
            RootPath = "/workspace"
            IgnorePatterns = []
            CreatedAt = System.DateTime.Now
            LastOpenedAt = System.DateTime.Now
          }
          Config = {
            DailyNoteFormat = "yyyy-MM-dd"
            DailyNoteFolder = ""
            DefaultTags = []
          }
          NoteCount = 0
          TotalBlocks = 0
        }

        let initialState = {
          State.Default with
              WorkspaceSnapshot = Some snapshot
              PendingWorkspacePath = Some "/workspace"
              PendingNoteToOpen = Some "note.md"
        }

        let newState, _ = Update (WorkspaceOpened(Ok workspaceInfo)) initialState

        match newState.WorkspaceSnapshot with
        | Some updatedSnapshot -> Jest.expect(updatedSnapshot.UI.LastWorkspacePath).toEqual "/workspace"
        | None -> failwith "Expected workspace snapshot to be present"

        Jest.expect(newState.PendingWorkspacePath).toEqual None
        Jest.expect(newState.PendingNoteToOpen).toEqual None
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
            LastWorkspacePath = "/workspace"
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

    Jest.test (
      "DebouncedSettingsSave triggers save when settings exist",
      fun () ->
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

        let initialState = { State.Default with Settings = Some testSettings }
        let newState, _ = Update DebouncedSettingsSave initialState
        Jest.expect(newState.SettingsSaveTimer).toEqual None
    )

    Jest.test (
      "DebouncedSettingsSave does nothing when no settings",
      fun () ->
        let initialState = { State.Default with Settings = None }
        let newState, cmd = Update DebouncedSettingsSave initialState
        Jest.expect(newState).toEqual initialState
    )

    Jest.test (
      "DebouncedSnapshotSave triggers save when snapshot exists",
      fun () ->
        let testSnapshot = {
          UI = {
            ActivePage = "test.md"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = []
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
          }
        }

        let initialState = { State.Default with WorkspaceSnapshot = Some testSnapshot }
        let newState, _ = Update DebouncedSnapshotSave initialState
        Jest.expect(newState.SnapshotSaveTimer).toEqual None
    )

    Jest.test (
      "DebouncedSnapshotSave does nothing when no snapshot",
      fun () ->
        let initialState = { State.Default with WorkspaceSnapshot = None }
        let newState, cmd = Update DebouncedSnapshotSave initialState
        Jest.expect(newState).toEqual initialState
    )

    Jest.test (
      "SettingsChanged updates settings immediately",
      fun () ->
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

        let initialState = State.Default
        let newState, _ = Update (SettingsChanged testSettings) initialState
        Jest.expect(newState.Settings).toEqual (Some testSettings)
    )

    Jest.test (
      "WorkspaceSnapshotChanged updates snapshot immediately",
      fun () ->
        let testSnapshot = {
          UI = {
            ActivePage = "test.md"
            SidebarVisible = true
            SidebarWidth = 280
            RightPanelVisible = false
            RightPanelWidth = 300
            PinnedPages = []
            RecentPages = []
            LastWorkspacePath = "/workspace"
            GraphLayout = "force"
          }
        }

        let initialState = State.Default
        let newState, _ = Update (WorkspaceSnapshotChanged testSnapshot) initialState
        Jest.expect(newState.WorkspaceSnapshot).toEqual (Some testSnapshot)
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
        Jest.expect(newState.SearchFilters.Tags.Length).toEqual 2
        Jest.expect(newState.SearchFilters.PathPrefix).toEqual "notes/"
        Jest.expect(newState.SearchFilters.DateFrom.IsSome).toEqual true
    )

    Jest.test (
      "SetPreviewMode updates editor preview mode",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetPreviewMode SplitView) initialState
        Jest.expect(newState.EditorState.PreviewMode).toEqual SplitView
    )

    Jest.test (
      "UpdateCursorPosition updates editor cursor position",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (UpdateCursorPosition(Some 42)) initialState
        Jest.expect(newState.EditorState.CursorPosition).toEqual (Some 42)
    )

    Jest.test (
      "UpdateSelection updates editor selection",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (UpdateSelection(Some 10, Some 20)) initialState
        Jest.expect(newState.EditorState.SelectionStart).toEqual (Some 10)
        Jest.expect(newState.EditorState.SelectionEnd).toEqual (Some 20)
    )

    Jest.test (
      "MarkEditorDirty updates editor dirty flag",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (MarkEditorDirty true) initialState
        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "SetSidebarWidth updates UI sidebar width",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetSidebarWidth 350) initialState
        Jest.expect(newState.UIState.SidebarWidth).toEqual 350
    )

    Jest.test (
      "SetRightPanelWidth updates UI right panel width",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetRightPanelWidth 450) initialState
        Jest.expect(newState.UIState.RightPanelWidth).toEqual 450
    )

    Jest.test (
      "ShowModal updates active modal",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (ShowModal CreateNoteDialog) initialState
        Jest.expect(newState.UIState.ActiveModal).toEqual CreateNoteDialog
    )

    Jest.test (
      "CloseModal clears active modal",
      fun () ->
        let initialState = {
          State.Default with
              UIState = {
                State.Default.UIState with
                    ActiveModal = CreateNoteDialog
              }
        }

        let newState, _ = Update CloseModal initialState
        Jest.expect(newState.UIState.ActiveModal).toEqual NoModal
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
      "FormatBold wraps selected text with bold markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
              EditorState = {
                State.Default.EditorState with
                    SelectionStart = Some 0
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatBold initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "**Hello** world"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "FormatBold inserts markers at cursor when no selection",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
              EditorState = {
                State.Default.EditorState with
                    SelectionStart = Some 5
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatBold initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Hello**** world"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatItalic wraps selected text with italic markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
              EditorState = {
                State.Default.EditorState with
                    SelectionStart = Some 6
                    SelectionEnd = Some 11
              }
        }

        let newState, _ = Update FormatItalic initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Hello _world_"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatInlineCode wraps selected text with code markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
              EditorState = {
                State.Default.EditorState with
                    SelectionStart = Some 0
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatInlineCode initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "`Hello` world"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "SetHeadingLevel adds heading markers to current line",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "This is a heading\nSecond line"
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
              EditorState = { State.Default.EditorState with CursorPosition = Some 5 }
        }

        let newState, _ = Update (SetHeadingLevel 2) initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "## This is a heading\nSecond line"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "SetHeadingLevel removes existing heading markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "### Already a heading\nSecond line"
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
              EditorState = { State.Default.EditorState with CursorPosition = Some 5 }
        }

        let newState, _ = Update (SetHeadingLevel 1) initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "# Already a heading\nSecond line"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatBold does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatBold initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "FormatItalic does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatItalic initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "FormatInlineCode does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatInlineCode initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "SetHeadingLevel does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update (SetHeadingLevel 1) initialState
        Jest.expect(newState.CurrentNote).toEqual None
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
  "Model.Router",
  fun () ->
    Jest.test (
      "parseUrl empty segments returns WorkspacePicker",
      fun () ->
        let route = Model.parseUrl []
        Jest.expect(route).toEqual WorkspacePicker
    )

    Jest.test (
      "parseUrl [\"notes\"] returns NoteList",
      fun () ->
        let route = Model.parseUrl [ "notes" ]
        Jest.expect(route).toEqual NoteList
    )

    Jest.test (
      "parseUrl [\"notes\"; noteId] returns NoteEditor",
      fun () ->
        let route = Model.parseUrl [ "notes"; "test-note-id" ]
        Jest.expect(route).toEqual (NoteEditor "test-note-id")
    )

    Jest.test (
      "parseUrl [\"graph\"] returns GraphViewRoute",
      fun () ->
        let route = Model.parseUrl [ "graph" ]
        Jest.expect(route).toEqual GraphViewRoute
    )

    Jest.test (
      "parseUrl [\"settings\"] returns Settings",
      fun () ->
        let route = Model.parseUrl [ "settings" ]
        Jest.expect(route).toEqual Settings
    )

    Jest.test (
      "parseUrl with invalid segments returns WorkspacePicker",
      fun () ->
        let route = Model.parseUrl [ "invalid"; "route" ]
        Jest.expect(route).toEqual WorkspacePicker
    )

    Jest.test (
      "routeToUrl WorkspacePicker returns empty list",
      fun () ->
        let url = Model.routeToUrl WorkspacePicker
        Jest.expect(url |> List.toArray).toEqual ([||])
    )

    Jest.test (
      "routeToUrl NoteList returns [\"notes\"]",
      fun () ->
        let url = Model.routeToUrl NoteList
        Jest.expect(url |> List.toArray).toEqual ([| "notes" |])
    )

    Jest.test (
      "routeToUrl NoteEditor returns [\"notes\"; noteId]",
      fun () ->
        let url = Model.routeToUrl (NoteEditor "my-note")
        Jest.expect(url |> List.toArray).toEqual ([| "notes"; "my-note" |])
    )

    Jest.test (
      "routeToUrl GraphViewRoute returns [\"graph\"]",
      fun () ->
        let url = Model.routeToUrl GraphViewRoute
        Jest.expect(url |> List.toArray).toEqual ([| "graph" |])
    )

    Jest.test (
      "routeToUrl Settings returns [\"settings\"]",
      fun () ->
        let url = Model.routeToUrl Settings
        Jest.expect(url |> List.toArray).toEqual ([| "settings" |])
    )

    Jest.test (
      "UrlChanged updates current route and URL",
      fun () ->
        let initialState = State.Default
        let segments = [ "notes"; "test-note" ]
        let newState, _ = Update (UrlChanged segments) initialState
        Jest.expect(newState.CurrentUrl |> List.toArray).toEqual (segments |> List.toArray)
        Jest.expect(newState.CurrentRoute).toEqual (NoteEditor "test-note")
    )

    Jest.test (
      "UrlChanged to graph route updates correctly",
      fun () ->
        let initialState = State.Default
        let segments = [ "graph" ]
        let newState, _ = Update (UrlChanged segments) initialState
        Jest.expect(newState.CurrentUrl |> List.toArray).toEqual (segments |> List.toArray)
        Jest.expect(newState.CurrentRoute).toEqual GraphViewRoute
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

Jest.test (
  "SettingsChanged immediately updates settings state",
  fun () ->
    let initialState = State.Default

    let testSettings = {
      General = {
        Theme = "light"
        Language = "es"
        AutoSave = false
        AutoSaveInterval = 60
      }
      Editor = {
        FontFamily = "JetBrains Mono"
        FontSize = 16
        LineHeight = 1.8
        TabSize = 4
        VimMode = true
        SpellCheck = false
      }
    }

    let newState, _ = Update (SettingsChanged testSettings) initialState
    Jest.expect(newState.Settings).toEqual (Some testSettings)

    match newState.Settings with
    | Some s ->
      Jest.expect(s.General.Theme).toEqual "light"
      Jest.expect(s.General.Language).toEqual "es"
      Jest.expect(s.General.AutoSave).toEqual false
      Jest.expect(s.General.AutoSaveInterval).toEqual 60
      Jest.expect(s.Editor.FontFamily).toEqual "JetBrains Mono"
      Jest.expect(s.Editor.FontSize).toEqual 16
      Jest.expect(s.Editor.LineHeight).toEqual 1.8
      Jest.expect(s.Editor.TabSize).toEqual 4
      Jest.expect(s.Editor.VimMode).toEqual true
      Jest.expect(s.Editor.SpellCheck).toEqual false
    | None -> failwith "Expected settings to be present"
)

Jest.test (
  "SettingsChanged with theme change",
  fun () ->
    let initialSettings = {
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

    let initialState = { State.Default with Settings = Some initialSettings }

    let updatedSettings = {
      initialSettings with
          General = { initialSettings.General with Theme = "light" }
    }

    let newState, _ = Update (SettingsChanged updatedSettings) initialState

    match newState.Settings with
    | Some s -> Jest.expect(s.General.Theme).toEqual "light"
    | None -> failwith "Expected settings to be present"
)

Jest.test (
  "SettingsChanged with font size change",
  fun () ->
    let initialSettings = {
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

    let initialState = { State.Default with Settings = Some initialSettings }

    let updatedSettings = {
      initialSettings with
          Editor = { initialSettings.Editor with FontSize = 18 }
    }

    let newState, _ = Update (SettingsChanged updatedSettings) initialState

    match newState.Settings with
    | Some s -> Jest.expect(s.Editor.FontSize).toEqual 18
    | None -> failwith "Expected settings to be present"
)

Jest.test (
  "SettingsChanged edge case - minimum values",
  fun () ->
    let settings = {
      General = {
        Theme = "dark"
        Language = "en"
        AutoSave = true
        AutoSaveInterval = 5
      }
      Editor = {
        FontFamily = "monospace"
        FontSize = 10
        LineHeight = 1.0
        TabSize = 2
        VimMode = false
        SpellCheck = true
      }
    }

    let initialState = State.Default
    let newState, _ = Update (SettingsChanged settings) initialState

    match newState.Settings with
    | Some s ->
      Jest.expect(s.General.AutoSaveInterval).toEqual 5
      Jest.expect(s.Editor.FontSize).toEqual 10
      Jest.expect(s.Editor.LineHeight).toEqual 1.0
    | None -> failwith "Expected settings to be present"
)

Jest.test (
  "SettingsChanged edge case - maximum values",
  fun () ->
    let settings = {
      General = {
        Theme = "dark"
        Language = "en"
        AutoSave = true
        AutoSaveInterval = 120
      }
      Editor = {
        FontFamily = "monospace"
        FontSize = 24
        LineHeight = 3.0
        TabSize = 8
        VimMode = false
        SpellCheck = true
      }
    }

    let initialState = State.Default
    let newState, _ = Update (SettingsChanged settings) initialState

    match newState.Settings with
    | Some s ->
      Jest.expect(s.General.AutoSaveInterval).toEqual 120
      Jest.expect(s.Editor.FontSize).toEqual 24
      Jest.expect(s.Editor.LineHeight).toEqual 3.0
      Jest.expect(s.Editor.TabSize).toEqual 8
    | None -> failwith "Expected settings to be present"
)
