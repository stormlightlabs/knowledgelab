module ModelWorkspaceTests

open Fable.Jester
open Elmish
open Model
open Domain

let private collectCommands (cmd : Cmd<Msg>) =
  let dispatched = ResizeArray<Msg>()
  cmd |> List.iter (fun sub -> sub (fun msg -> dispatched.Add msg))
  dispatched |> Seq.toList

Jest.describe (
  "Model.Update (Workspace)",
  fun () ->
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
      "WorkspaceSnapshotLoaded auto-opens last workspace when remembered path exists",
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
            LastWorkspacePath = "/workspace/path"
            GraphLayout = "force"
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let _, cmd = Update (WorkspaceSnapshotLoaded(Ok snapshot)) State.Default
        let dispatched = collectCommands cmd

        match dispatched with
        | [ OpenWorkspace path ] -> Jest.expect(path).toEqual "/workspace/path"
        | _ -> failwith "Expected OpenWorkspace command to be dispatched"
    )

    Jest.test (
      "WorkspaceSnapshotLoaded skips auto-open when no workspace path is stored",
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let _, cmd = Update (WorkspaceSnapshotLoaded(Ok snapshot)) State.Default
        let dispatched = collectCommands cmd
        Jest.expect(dispatched.Length).toEqual 0
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
      "CreateWorkspace sets loading state",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update CreateWorkspace initialState
        Jest.expect(newState.Loading).toEqual true
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
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
            SearchHistory = []
            NotesSortBy = None
            NotesSortOrder = None
          }
        }

        let initialState = State.Default
        let newState, _ = Update (WorkspaceSnapshotChanged testSnapshot) initialState
        Jest.expect(newState.WorkspaceSnapshot).toEqual (Some testSnapshot)
    )
)
