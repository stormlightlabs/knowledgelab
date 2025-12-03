module Model

open Elmish
open System
open Domain
open Fable.Core
open Feliz.Router

/// Route represents the current view/page in the application
type Route =
  | WorkspacePicker
  | NoteList
  | NoteEditor of noteId : string
  | GraphViewRoute
  | Settings

/// Panel represents optional side panels that can be shown
type Panel =
  | Backlinks
  | TagsPanel
  | SearchPanel
  | TasksPanel

/// Search filters for advanced search functionality
type SearchFilters = {
  Tags : string list
  PathPrefix : string
  DateFrom : DateTime option
  DateTo : DateTime option
}

/// Search state holds all search-related data and UI state
type SearchState = {
  Query : string
  Results : SearchResult list
  IsLoading : bool
  Filters : SearchFilters
  DebounceTimer : int option
  ShowTagAutocomplete : bool
  TagAutocompleteQuery : string
  AvailableTags : string list
}

/// Tag filtering mode for multi-tag selection
type TagFilterMode =
  /// Notes must have ALL selected tags
  | And
  /// Notes must have ANY of the selected tags
  | Or

/// Editor preview mode
type PreviewMode =
  | EditOnly
  | PreviewOnly
  | SplitView

/// Snapshot of editor content and cursor state for undo/redo
type EditorSnapshot = {
  Content : string
  CursorPosition : int option
  SelectionStart : int option
  SelectionEnd : int option
}

/// Maximum number of undo/redo history entries to maintain
[<Literal>]
let MaxHistorySize = 100

/// Editor state including preview mode, cursor position, selection, and undo/redo stacks
type EditorState = {
  PreviewMode : PreviewMode
  CursorPosition : int option
  SelectionStart : int option
  SelectionEnd : int option
  IsDirty : bool
  RenderedPreview : string option
  FocusedBlock : string option
  UndoStack : EditorSnapshot list
  RedoStack : EditorSnapshot list
  LastChangeTimestamp : DateTime option
}

/// Modal dialog types that can be shown
type ModalDialog =
  | CreateNoteDialog
  | DeleteConfirmDialog of noteId : string
  | SettingsDialog
  | SearchDialog
  | NoModal

/// UI state for panel sizes and modals
type UIState = {
  SidebarWidth : int
  RightPanelWidth : int
  ActiveModal : ModalDialog
}

/// App state holds all application data and UI state
type State = {
  Workspace : WorkspaceInfo option
  Notes : NoteSummary list
  CurrentNote : Note option
  CurrentRoute : Route
  CurrentUrl : string list
  VisiblePanels : Set<Panel>
  Search : SearchState
  Graph : Graph option
  SelectedNode : string option
  HoveredNode : string option
  ZoomState : ZoomState
  GraphEngine : GraphEngine
  Tags : string list
  TagInfos : TagInfo list
  SelectedTags : string list
  TagFilterMode : TagFilterMode
  Backlinks : Link list
  AllTasks : Task list
  TaskFilter : TaskFilter
  IsLoadingTasks : bool
  Settings : Settings option
  WorkspaceSnapshot : WorkspaceSnapshot option
  SettingsSaveTimer : int option
  SnapshotSaveTimer : int option
  EditorState : EditorState
  UIState : UIState
  NoteHistories : Map<string, EditorSnapshot list * EditorSnapshot list>
  Loading : bool
  Error : string option
  Success : string option
  PendingWorkspacePath : string option
  PendingNoteToOpen : string option
} with

  static member Default = {
    Workspace = None
    Notes = []
    CurrentNote = None
    CurrentRoute = WorkspacePicker
    CurrentUrl = []
    VisiblePanels = Set.ofList [ Backlinks ]
    Search = {
      Query = ""
      Results = []
      IsLoading = false
      Filters = {
        Tags = []
        PathPrefix = ""
        DateFrom = None
        DateTo = None
      }
      DebounceTimer = None
      ShowTagAutocomplete = false
      TagAutocompleteQuery = ""
      AvailableTags = []
    }
    Graph = None
    SelectedNode = None
    HoveredNode = None
    ZoomState = ZoomState.Default
    GraphEngine = Svg
    Tags = []
    TagInfos = []
    SelectedTags = []
    TagFilterMode = Or
    Backlinks = []
    AllTasks = []
    TaskFilter = {
      Status = None
      NoteId = None
      CreatedAfter = None
      CreatedBefore = None
      CompletedAfter = None
      CompletedBefore = None
      NoteModifiedAfter = None
      NoteModifiedBefore = None
    }
    IsLoadingTasks = false
    Settings = None
    WorkspaceSnapshot = None
    SettingsSaveTimer = None
    SnapshotSaveTimer = None
    EditorState = {
      PreviewMode = EditOnly
      CursorPosition = None
      SelectionStart = None
      SelectionEnd = None
      IsDirty = false
      RenderedPreview = None
      FocusedBlock = None
      UndoStack = []
      RedoStack = []
      LastChangeTimestamp = None
    }
    UIState = {
      SidebarWidth = 280
      RightPanelWidth = 300
      ActiveModal = NoModal
    }
    NoteHistories = Map.empty
    Loading = false
    Error = None
    Success = None
    PendingWorkspacePath = None
    PendingNoteToOpen = None
  }

/// Messages represent all possible user actions and events
type Msg =
  | UrlChanged of string list
  | SelectWorkspaceFolder
  | CreateWorkspace
  | OpenWorkspace of path : string
  | WorkspaceOpened of Result<WorkspaceInfo, string>
  | LoadNotes
  | NotesLoaded of Result<NoteSummary list, string>
  | SelectNote of noteId : string
  | OpenRecentFile of workspacePath : string * noteId : string
  | ClearRecentFiles
  | RecentFilesCleared of Result<WorkspaceSnapshot, string>
  | NoteLoaded of Result<Note, string>
  | SaveNote of Note
  | NoteSaved of Result<unit, string>
  | SaveNoteExplicitly
  | ExplicitSaveCompleted of Result<unit, string>
  | CreateNote of title : string * folder : string
  | NoteCreated of Result<Note, string>
  | DeleteNote of noteId : string
  | NoteDeleted of Result<unit, string>
  | NavigateTo of Route
  | TogglePanel of Panel
  | SearchQueryChanged of query : string
  | SearchResultsReceived of Result<SearchResult list, string>
  | SearchCleared
  | UpdateSearchQuery of query : string
  | PerformSearch
  | SearchCompleted of Result<SearchResult list, string>
  | DebouncedSearch
  | UpdateTagAutocomplete of show : bool * query : string
  | LoadGraph
  | GraphLoaded of Result<Graph, string>
  | LoadTags
  | TagsLoaded of Result<string list, string>
  | LoadTagsWithCounts
  | TagsWithCountsLoaded of Result<TagInfo list, string>
  | ToggleTagFilter of tagName : string
  | SetTagFilterMode of TagFilterMode
  | ClearTagFilters
  | LoadBacklinks of noteId : string
  | BacklinksLoaded of Result<Link list, string>
  | OpenDailyNote
  | UpdateNoteContent of content : string
  | GraphNodeHovered of noteId : string option
  | GraphZoomChanged of ZoomState
  | GraphEngineChanged of GraphEngine
  | HydrateFromDisk
  | SettingsLoaded of Result<Settings, string>
  | WorkspaceSnapshotLoaded of Result<WorkspaceSnapshot, string>
  | SettingsChanged of Settings
  | DebouncedSettingsSave
  | SettingsSaved of Result<unit, string>
  | WorkspaceSnapshotChanged of WorkspaceSnapshot
  | DebouncedSnapshotSave
  | WorkspaceSnapshotSaved of Result<unit, string>
  | UpdateSearchFilters of SearchFilters
  | SetPreviewMode of PreviewMode
  | UpdateCursorPosition of int option
  | UpdateSelection of start : int option * end_ : int option
  | MarkEditorDirty of bool
  | SetSidebarWidth of int
  | SetRightPanelWidth of int
  | ShowModal of ModalDialog
  | CloseModal
  | SetError of string
  | ClearError
  | ClearSuccess
  | FormatBold
  | FormatItalic
  | FormatInlineCode
  | SetHeadingLevel of int
  | RenderPreview of markdown : string
  | PreviewRendered of Result<string, string>
  | SyntaxHighlightingApplied of Result<string, string>
  | BlockIndent
  | BlockOutdent
  | BlockNavigateUp
  | BlockNavigateDown
  | BlockFocusToggle of blockId : string option
  | ToggleTaskAtCursor
  | ToggleTaskAtLine of lineNumber : int
  | TaskToggled of Result<unit, string>
  | LoadAllTasks
  | LoadTasksForNote of noteId : string
  | TasksLoaded of Result<TaskInfo, string>
  | UpdateTaskFilter of TaskFilter
  | PushEditorSnapshot
  | Undo
  | Redo

/// Debounce delay in milliseconds for settings/snapshot saves
[<Literal>]
let private DebounceDelayMs = 800

/// Debounce delay for search queries in milliseconds
[<Literal>]
let private SearchDebounceDelayMs = 300

/// JavaScript setTimeout interop for debouncing
[<Emit("setTimeout(() => $0(), $1)")>]
let private setTimeout (callback : unit -> unit) (delay : int) : int = jsNative

/// JavaScript clearTimeout interop for canceling debounced operations
[<Emit("clearTimeout($0)")>]
let private clearTimeout (timerId : int) : unit = jsNative

/// Safely converts an array to a list, handling null/undefined cases
let private safeArrayToList (arr : 'a array) : 'a list =
  if isNull (box arr) then [] else Array.toList arr

/// Converts URL segments to a Route
let parseUrl (segments : string list) : Route =
  match segments with
  | [] -> WorkspacePicker
  | [ "notes" ] -> NoteList
  | [ "notes"; noteId ] -> NoteEditor noteId
  | [ "graph" ] -> GraphViewRoute
  | [ "settings" ] -> Settings
  | _ -> WorkspacePicker

/// Converts a Route to URL segments for navigation
let routeToUrl (route : Route) : string list =
  match route with
  | WorkspacePicker -> []
  | NoteList -> [ "notes" ]
  | NoteEditor noteId -> [ "notes"; noteId ]
  | GraphViewRoute -> [ "graph" ]
  | Settings -> [ "settings" ]

/// Creates a debounced command that dispatches a message after a delay
let private debounceCmd (msg : Msg) : Cmd<Msg> =
  let delayedDispatch (dispatch : Msg -> unit) =
    setTimeout (fun () -> dispatch msg) DebounceDelayMs |> ignore

  [ delayedDispatch ]

/// Creates a debounced command for search with custom delay
let private debounceSearchCmd (msg : Msg) (delay : int) : Cmd<Msg> =
  let delayedDispatch (dispatch : Msg -> unit) =
    setTimeout (fun () -> dispatch msg) delay |> ignore

  [ delayedDispatch ]

/// Cancels a pending debounced operation if one exists
let private cancelTimer (timerId : int option) : unit =
  match timerId with
  | Some id -> clearTimeout id
  | None -> ()

/// Maximum number of recent files to track
[<Literal>]
let private MaxRecentFiles = 20

/// Removes blank or whitespace-only entries from the recent files list
let private sanitizeRecentPages (recentPages : string list) =
  recentPages |> List.filter (String.IsNullOrWhiteSpace >> not)

let private sanitizeSnapshot (snapshot : WorkspaceSnapshot) =
  let sanitizedUI = {
    snapshot.UI with
        RecentPages = sanitizeRecentPages snapshot.UI.RecentPages
        LastWorkspacePath =
          match snapshot.UI.LastWorkspacePath with
          | null -> ""
          | value -> value.Trim()
  }

  { snapshot with UI = sanitizedUI }

/// Adds a note ID to the recent pages list, maintaining max size and moving duplicates to front
let private addToRecentPages (noteId : string) (recentPages : string list) : string list =
  if String.IsNullOrWhiteSpace noteId then
    sanitizeRecentPages recentPages
  else
    let filtered = recentPages |> sanitizeRecentPages |> List.filter ((<>) noteId)

    let newList = noteId :: filtered
    newList |> List.truncate MaxRecentFiles

/// Applies markdown formatting to the selected text or inserts formatting markers at cursor
let private applyMarkdownFormat
  (content : string)
  (selectionStart : int option)
  (selectionEnd : int option)
  (prefix : string)
  (suffix : string)
  : string * int * int =
  match selectionStart, selectionEnd with
  | Some start, Some end_ when start < end_ ->
    let selectedText = content.Substring(start, end_ - start)
    let formattedText = prefix + selectedText + suffix

    let newContent =
      content.Substring(0, start) + formattedText + content.Substring(end_)

    let newStart = start + prefix.Length
    let newEnd = newStart + selectedText.Length
    (newContent, newStart, newEnd)
  | Some pos, _ ->
    let formattedText = prefix + suffix
    let newContent = content.Substring(0, pos) + formattedText + content.Substring(pos)
    let newPos = pos + prefix.Length
    (newContent, newPos, newPos)
  | _ -> (content, 0, 0)

/// Applies heading formatting to the current line
let private applyHeadingFormat (content : string) (cursorPosition : int option) (level : int) : string * int =
  match cursorPosition with
  | Some pos ->
    let lines = content.Split('\n')
    let mutable currentPos = 0
    let mutable lineIndex = 0
    let mutable found = false

    for i in 0 .. lines.Length - 1 do
      if not found && currentPos + lines.[i].Length >= pos then
        lineIndex <- i
        found <- true
      elif not found then
        currentPos <- currentPos + lines.[i].Length + 1

    let currentLine = lines.[lineIndex]
    let trimmedLine = currentLine.TrimStart('#', ' ')
    let headingPrefix = String.replicate level "#" + " "
    let newLine = headingPrefix + trimmedLine

    lines.[lineIndex] <- newLine
    let newContent = System.String.Join("\n", lines)
    let newPos = currentPos + headingPrefix.Length

    (newContent, newPos)
  | None -> (content, 0)

/// Applies indentation (2 spaces) to the current line or selected lines
let private applyBlockIndent
  (content : string)
  (cursorPosition : int option)
  (selectionStart : int option)
  (selectionEnd : int option)
  : string * int option =
  match cursorPosition with
  | Some pos ->
    let lines = content.Split('\n')
    let mutable currentPos = 0
    let mutable startLine = 0
    let mutable endLine = 0
    let mutable found = false

    let targetPos =
      match selectionStart, selectionEnd with
      | Some s, Some e when s < e -> s
      | _ -> pos

    let endPos =
      match selectionStart, selectionEnd with
      | Some s, Some e when s < e -> e
      | _ -> pos

    for i in 0 .. lines.Length - 1 do
      if not found && currentPos + lines.[i].Length >= targetPos then
        startLine <- i
        found <- true

      if currentPos + lines.[i].Length >= endPos then
        endLine <- i
      elif not found then
        currentPos <- currentPos + lines.[i].Length + 1
      else
        currentPos <- currentPos + lines.[i].Length + 1

    for i in startLine..endLine do
      lines.[i] <- "  " + lines.[i]

    let newContent = System.String.Join("\n", lines)
    let newCursor = Some(pos + 2)

    (newContent, newCursor)
  | None -> (content, None)

/// Removes indentation (up to 2 spaces) from the current line or selected lines
let private applyBlockOutdent
  (content : string)
  (cursorPosition : int option)
  (selectionStart : int option)
  (selectionEnd : int option)
  : string * int option =
  match cursorPosition with
  | Some pos ->
    let lines = content.Split('\n')
    let mutable currentPos = 0
    let mutable startLine = 0
    let mutable endLine = 0
    let mutable found = false

    let targetPos =
      match selectionStart, selectionEnd with
      | Some s, Some e when s < e -> s
      | _ -> pos

    let endPos =
      match selectionStart, selectionEnd with
      | Some s, Some e when s < e -> e
      | _ -> pos

    for i in 0 .. lines.Length - 1 do
      if not found && currentPos + lines.[i].Length >= targetPos then
        startLine <- i
        found <- true

      if currentPos + lines.[i].Length >= endPos then
        endLine <- i
      elif not found then
        currentPos <- currentPos + lines.[i].Length + 1
      else
        currentPos <- currentPos + lines.[i].Length + 1

    let mutable removedChars = 0

    for i in startLine..endLine do
      let line = lines.[i]

      if line.StartsWith("  ") then
        lines.[i] <- line.Substring(2)

        if i = startLine then
          removedChars <- 2
      elif line.StartsWith(" ") then
        lines.[i] <- line.Substring(1)

        if i = startLine then
          removedChars <- 1

    let newContent = System.String.Join("\n", lines)
    let newCursor = Some(max 0 (pos - removedChars))

    (newContent, newCursor)
  | None -> (content, None)

/// Navigates cursor to the previous or next line (direction: -1 for up, 1 for down)
let private navigateBlock (content : string) (cursorPosition : int option) (direction : int) : int option =
  match cursorPosition with
  | Some pos ->
    let lines = content.Split('\n')
    let mutable currentPos = 0
    let mutable lineIndex = 0
    let mutable columnInLine = 0
    let mutable found = false

    for i in 0 .. lines.Length - 1 do
      if not found && currentPos + lines.[i].Length >= pos then
        lineIndex <- i
        columnInLine <- pos - currentPos
        found <- true
      elif not found then
        currentPos <- currentPos + lines.[i].Length + 1

    let targetLine = lineIndex + direction

    if targetLine >= 0 && targetLine < lines.Length then
      let mutable targetPos = 0

      for i in 0 .. targetLine - 1 do
        targetPos <- targetPos + lines.[i].Length + 1

      let targetColumn = min columnInLine lines.[targetLine].Length
      Some(targetPos + targetColumn)
    else
      Some pos
  | None -> None

/// Creates an editor snapshot from current note and editor state
let private createEditorSnapshot (note : Note) (editorState : EditorState) : EditorSnapshot = {
  Content = note.Content
  CursorPosition = editorState.CursorPosition
  SelectionStart = editorState.SelectionStart
  SelectionEnd = editorState.SelectionEnd
}

/// Pushes a snapshot to the undo stack, maintaining max size
let private pushToUndoStack (snapshot : EditorSnapshot) (undoStack : EditorSnapshot list) : EditorSnapshot list =
  (snapshot :: undoStack) |> List.truncate MaxHistorySize

/// Restores editor and note state from a snapshot
let private restoreFromSnapshot
  (snapshot : EditorSnapshot)
  (note : Note)
  (editorState : EditorState)
  : Note * EditorState =
  let restoredNote = { note with Content = snapshot.Content }

  let restoredEditorState = {
    editorState with
        CursorPosition = snapshot.CursorPosition
        SelectionStart = snapshot.SelectionStart
        SelectionEnd = snapshot.SelectionEnd
        IsDirty = true
  }

  (restoredNote, restoredEditorState)

/// Debounce delay for grouping rapid edits (in milliseconds)
[<Literal>]
let private EditGroupingDelayMs = 1000

/// Checks if two timestamps are within the edit grouping window
let private shouldGroupEdits (lastChange : DateTime option) (currentTime : DateTime) : bool =
  match lastChange with
  | Some lastTime ->
    let elapsed = (currentTime - lastTime).TotalMilliseconds
    elapsed < float EditGroupingDelayMs
  | None -> false

/// Saves the current note's undo/redo stacks to the per-note history map
let private saveCurrentNoteHistory (state : State) : State =
  match state.CurrentNote with
  | Some note ->
    let history = (state.EditorState.UndoStack, state.EditorState.RedoStack)

    {
      state with
          NoteHistories = state.NoteHistories.Add(note.Id, history)
    }
  | None -> state

/// Restores undo/redo stacks from the per-note history map for a specific note
let private restoreNoteHistory
  (noteId : string)
  (editorState : EditorState)
  (noteHistories : Map<string, EditorSnapshot list * EditorSnapshot list>)
  : EditorState =
  match noteHistories.TryFind noteId with
  | Some(undoStack, redoStack) -> {
      editorState with
          UndoStack = undoStack
          RedoStack = redoStack
    }
  | None -> { editorState with UndoStack = []; RedoStack = [] }

/// Updates the workspace snapshot with a new recent page and triggers save
let private updateRecentPage (noteId : string) (state : State) : State * Cmd<Msg> =
  if String.IsNullOrWhiteSpace noteId then
    state, Cmd.none
  else
    match state.WorkspaceSnapshot with
    | Some snapshot ->
      let resolvedWorkspacePath =
        match state.Workspace with
        | Some ws when not (String.IsNullOrWhiteSpace ws.Workspace.RootPath) -> ws.Workspace.RootPath
        | _ -> snapshot.UI.LastWorkspacePath

      let updatedUI = {
        snapshot.UI with
            RecentPages = addToRecentPages noteId snapshot.UI.RecentPages
            ActivePage = noteId
            LastWorkspacePath = resolvedWorkspacePath
      }

      let updatedSnapshot = { snapshot with UI = updatedUI }

      { state with WorkspaceSnapshot = Some updatedSnapshot }, Cmd.ofMsg (WorkspaceSnapshotChanged updatedSnapshot)
    | None -> state, Cmd.none

let Init () =
  let currentUrl = Router.currentUrl ()
  let currentRoute = parseUrl currentUrl

  {
    State.Default with
        CurrentUrl = currentUrl
        CurrentRoute = currentRoute
  },
  Cmd.ofMsg HydrateFromDisk

let Update (msg : Msg) (state : State) : (State * Cmd<Msg>) =
  match msg with
  | UrlChanged segments ->
    let route = parseUrl segments
    { state with CurrentUrl = segments; CurrentRoute = route }, Cmd.none
  | SelectWorkspaceFolder ->
    state,
    Cmd.OfPromise.either
      (fun () -> Api.selectDirectory "Select Workspace Folder")
      ()
      (fun path ->
        if System.String.IsNullOrEmpty(path) then
          ClearError
        else
          OpenWorkspace path)
      (fun ex -> SetError ex.Message)
  | CreateWorkspace ->
    { state with Loading = true },
    Cmd.OfPromise.either
      Api.createNewWorkspace
      ()
      (fun workspaceOpt ->
        match workspaceOpt with
        | Some workspace -> WorkspaceOpened(Ok workspace)
        | None -> ClearError)
      (fun ex -> WorkspaceOpened(Error ex.Message))
  | OpenWorkspace path ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.openWorkspace path (Ok >> WorkspaceOpened) (fun ex -> WorkspaceOpened(Error ex.Message))
  | WorkspaceOpened(Ok workspace) ->
    let targetWorkspacePath = workspace.Workspace.RootPath

    let pendingNote =
      match state.PendingWorkspacePath, state.PendingNoteToOpen with
      | Some pendingPath, Some note when
        not (String.IsNullOrWhiteSpace pendingPath)
        && pendingPath.Equals(targetWorkspacePath, StringComparison.OrdinalIgnoreCase)
        ->
        Some note
      | _ -> None

    let baseState = {
      state with
          Workspace = Some workspace
          Loading = false
          CurrentRoute = NoteList
          Error = None
          PendingWorkspacePath = None
          PendingNoteToOpen = None
    }

    let stateWithSnapshot, snapshotCmd =
      match baseState.WorkspaceSnapshot with
      | Some snapshot when snapshot.UI.LastWorkspacePath <> targetWorkspacePath ->
        let updatedSnapshot = {
          snapshot with
              UI = { snapshot.UI with LastWorkspacePath = targetWorkspacePath }
        }

        { baseState with WorkspaceSnapshot = Some updatedSnapshot },
        Cmd.ofMsg (WorkspaceSnapshotChanged updatedSnapshot)
      | _ -> baseState, Cmd.none

    let pendingNoteCmd =
      match pendingNote with
      | Some noteId -> Cmd.ofMsg (SelectNote noteId)
      | None -> Cmd.none

    stateWithSnapshot,
    Cmd.batch [
      Cmd.ofMsg LoadNotes
      Cmd.ofMsg LoadAllTasks
      Cmd.ofMsg LoadTagsWithCounts
      snapshotCmd
      pendingNoteCmd
    ]
  | WorkspaceOpened(Error err) ->
    {
      state with
          Loading = false
          Error = Some err
          PendingWorkspacePath = None
          PendingNoteToOpen = None
    },
    Cmd.none
  | LoadNotes ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.listNotes () (safeArrayToList >> Ok >> NotesLoaded) (fun ex ->
      NotesLoaded(Error ex.Message))
  | NotesLoaded(Ok notes) ->
    Browser.Dom.console.log ("NotesLoaded - count:", notes.Length)

    notes
    |> List.iteri (fun i note ->
      Browser.Dom.console.log ($"Note {i}:", note)
      Browser.Dom.console.log ($"  id:", note.id)
      Browser.Dom.console.log ($"  title:", note.title)
      Browser.Dom.console.log ($"  path:", note.path))

    { state with Notes = notes; Loading = false; Error = None }, Cmd.none
  | NotesLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | SelectNote noteId ->
    Browser.Dom.console.log ("SelectNote - Requesting note:", noteId)

    let stateWithSavedHistory = saveCurrentNoteHistory state

    { stateWithSavedHistory with Loading = true },
    Cmd.OfPromise.either Api.getNote noteId (Ok >> NoteLoaded) (fun ex -> NoteLoaded(Error ex.Message))
  | OpenRecentFile(workspacePath, noteId) ->
    let trimmedWorkspace = if isNull workspacePath then "" else workspacePath.Trim()

    if String.IsNullOrWhiteSpace noteId then
      state, Cmd.none
    elif String.IsNullOrWhiteSpace trimmedWorkspace then
      {
        state with
            Error = Some "This recent file is missing its workspace path. Please open the workspace manually."
      },
      Cmd.none
    else
      let isWorkspaceActive =
        state.Workspace
        |> Option.map (fun ws -> ws.Workspace.RootPath.Equals(trimmedWorkspace, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultValue false

      if isWorkspaceActive then
        state, Cmd.ofMsg (SelectNote noteId)
      else
        {
          state with
              PendingWorkspacePath = Some trimmedWorkspace
              PendingNoteToOpen = Some noteId
        },
        Cmd.ofMsg (OpenWorkspace trimmedWorkspace)
  | ClearRecentFiles ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.clearRecentFiles () (Ok >> RecentFilesCleared) (fun ex ->
      RecentFilesCleared(Error ex.Message))
  | RecentFilesCleared(Ok snapshot) ->
    let sanitizedSnapshot = sanitizeSnapshot snapshot

    {
      state with
          WorkspaceSnapshot = Some sanitizedSnapshot
          Loading = false
          Error = None
          Success = Some "Recent files cleared"
    },
    Cmd.none
  | RecentFilesCleared(Error err) ->
    {
      state with
          Loading = false
          Error = Some err
          Success = None
    },
    Cmd.none
  | NoteLoaded(Ok note) ->
    Browser.Dom.console.log ("NoteLoaded - Note data:", note)
    Browser.Dom.console.log ("  Id:", note.Id)
    Browser.Dom.console.log ("  Title:", note.Title)
    Browser.Dom.console.log ("  Path:", note.Path)
    Browser.Dom.console.log ("  Content length:", note.Content.Length)

    Browser.Dom.console.log ("  Content preview:", note.Content.Substring(0, min 100 note.Content.Length))

    let baseEditorState = { state.EditorState with IsDirty = false }

    let restoredEditorState =
      restoreNoteHistory note.Id baseEditorState state.NoteHistories

    let stateWithNote = {
      state with
          CurrentNote = Some note
          CurrentRoute = NoteEditor note.Id
          Loading = false
          Error = None
          EditorState = restoredEditorState
    }

    let stateWithRecent, recentCmd = updateRecentPage note.Id stateWithNote
    stateWithRecent, Cmd.batch [ Cmd.ofMsg (LoadBacklinks note.Id); recentCmd ]
  | NoteLoaded(Error err) ->
    Browser.Dom.console.error ("NoteLoaded Error:", err)
    { state with Loading = false; Error = Some err }, Cmd.none
  | SaveNote note ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.saveNote note (fun _ -> NoteSaved(Ok())) (fun ex -> NoteSaved(Error ex.Message))
  | NoteSaved(Ok()) ->
    { state with Loading = false; Error = None }, Cmd.batch [ Cmd.ofMsg LoadNotes; Cmd.ofMsg LoadGraph ]
  | NoteSaved(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | SaveNoteExplicitly ->
    match state.CurrentNote with
    | Some note ->
      { state with Loading = true },
      Cmd.OfPromise.either Api.saveNote note (fun _ -> ExplicitSaveCompleted(Ok())) (fun ex ->
        ExplicitSaveCompleted(Error ex.Message))
    | None -> state, Cmd.none
  | ExplicitSaveCompleted(Ok()) ->
    let clearedEditorState = {
      state.EditorState with
          UndoStack = []
          RedoStack = []
          IsDirty = false
    }

    {
      state with
          Loading = false
          Error = None
          Success = Some "Note saved"
          EditorState = clearedEditorState
    },
    Cmd.batch [ Cmd.ofMsg LoadNotes; Cmd.ofMsg LoadGraph ]
  | ExplicitSaveCompleted(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | CreateNote(title, folder) ->
    { state with Loading = true },
    Cmd.OfPromise.either (Api.createNote title) folder (Ok >> NoteCreated) (fun ex -> NoteCreated(Error ex.Message))
  | NoteCreated(Ok note) ->
    let stateWithNote = {
      state with
          CurrentNote = Some note
          CurrentRoute = NoteEditor note.Id
          Loading = false
          Error = None
          EditorState = { state.EditorState with IsDirty = false }
    }

    let stateWithRecent, recentCmd = updateRecentPage note.Id stateWithNote
    stateWithRecent, Cmd.batch [ Cmd.ofMsg LoadNotes; recentCmd ]
  | NoteCreated(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | DeleteNote noteId ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.deleteNote noteId (fun _ -> NoteDeleted(Ok())) (fun ex -> NoteDeleted(Error ex.Message))
  | NoteDeleted(Ok()) ->
    {
      state with
          CurrentNote = None
          CurrentRoute = NoteList
          Loading = false
          Error = None
    },
    Cmd.ofMsg LoadNotes
  | NoteDeleted(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | NavigateTo route ->
    let urlSegments = routeToUrl route
    Router.navigate (urlSegments |> List.toArray)

    {
      state with
          CurrentRoute = route
          CurrentUrl = urlSegments
    },
    Cmd.none
  | TogglePanel panel ->
    let newPanels =
      if state.VisiblePanels.Contains panel then
        state.VisiblePanels.Remove panel
      else
        state.VisiblePanels.Add panel

    { state with VisiblePanels = newPanels }, Cmd.none
  | SearchQueryChanged query ->
    cancelTimer state.Search.DebounceTimer

    if String.IsNullOrWhiteSpace query then
      let newSearch = {
        state.Search with
            Query = query
            Results = []
            IsLoading = false
            DebounceTimer = None
            ShowTagAutocomplete = false
      }
      { state with Search = newSearch }, Cmd.none
    else
      let newSearch = {
        state.Search with
            Query = query
            IsLoading = true
            DebounceTimer = None
      }
      { state with Search = newSearch }, debounceSearchCmd DebouncedSearch SearchDebounceDelayMs
  | SearchResultsReceived(Ok results) ->
    let newSearch = { state.Search with Results = results; IsLoading = false }
    { state with Search = newSearch; Error = None }, Cmd.none
  | SearchResultsReceived(Error err) ->
    let newSearch = { state.Search with IsLoading = false; Results = [] }
    { state with Search = newSearch; Error = Some $"Search failed: {err}" }, Cmd.none
  | DebouncedSearch ->
    let query = {
      Query = state.Search.Query
      Tags = state.Search.Filters.Tags
      PathPrefix = state.Search.Filters.PathPrefix
      DateFrom = state.Search.Filters.DateFrom
      DateTo = state.Search.Filters.DateTo
      Limit = 50
    }

    let newSearch = { state.Search with IsLoading = true }

    { state with Search = newSearch },
    Cmd.OfPromise.either Api.search query (safeArrayToList >> Ok >> SearchResultsReceived) (fun ex ->
      SearchResultsReceived(Error ex.Message))
  | SearchCleared ->
    cancelTimer state.Search.DebounceTimer
    let newSearch = {
      state.Search with
          Query = ""
          Results = []
          IsLoading = false
          DebounceTimer = None
          ShowTagAutocomplete = false
    }

    { state with Search = newSearch }, Cmd.none
  | UpdateSearchQuery query ->
    let newSearch = { state.Search with Query = query }
    { state with Search = newSearch }, Cmd.none
  | PerformSearch ->
    let query = {
      Query = state.Search.Query
      Tags = state.Search.Filters.Tags
      PathPrefix = state.Search.Filters.PathPrefix
      DateFrom = state.Search.Filters.DateFrom
      DateTo = state.Search.Filters.DateTo
      Limit = 50
    }

    let newSearch = { state.Search with IsLoading = true }

    { state with Search = newSearch },
    Cmd.OfPromise.either Api.search query (safeArrayToList >> Ok >> SearchResultsReceived) (fun ex ->
      SearchResultsReceived(Error ex.Message))
  | SearchCompleted(Ok results) ->
    let newSearch = { state.Search with Results = results; IsLoading = false }
    { state with Search = newSearch; Error = None }, Cmd.none
  | SearchCompleted(Error err) ->
    let newSearch = { state.Search with IsLoading = false }
    { state with Search = newSearch; Error = Some err }, Cmd.none
  | LoadGraph ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getGraph () (Ok >> GraphLoaded) (fun ex -> GraphLoaded(Error ex.Message))
  | GraphLoaded(Ok graph) ->
    {
      state with
          Graph = Some graph
          Loading = false
          Error = None
    },
    Cmd.none
  | GraphLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | LoadTags ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getAllTags () (safeArrayToList >> Ok >> TagsLoaded) (fun ex ->
      TagsLoaded(Error ex.Message))
  | TagsLoaded(Ok tags) -> { state with Tags = tags; Loading = false; Error = None }, Cmd.none
  | TagsLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | LoadTagsWithCounts ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getAllTagsWithCounts () (Array.toList >> Ok >> TagsWithCountsLoaded) (fun ex ->
      TagsWithCountsLoaded(Error ex.Message))
  | TagsWithCountsLoaded(Ok tagInfos) ->
    {
      state with
          TagInfos = tagInfos
          Loading = false
          Error = None
    },
    Cmd.none
  | TagsWithCountsLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | ToggleTagFilter tagName ->
    let newSelectedTags =
      if state.SelectedTags |> List.contains tagName then
        state.SelectedTags |> List.filter ((<>) tagName)
      else
        tagName :: state.SelectedTags

    { state with SelectedTags = newSelectedTags }, Cmd.none
  | SetTagFilterMode mode -> { state with TagFilterMode = mode }, Cmd.none
  | ClearTagFilters -> { state with SelectedTags = [] }, Cmd.none
  | LoadBacklinks noteId ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getBacklinks noteId (safeArrayToList >> Ok >> BacklinksLoaded) (fun ex ->
      BacklinksLoaded(Error ex.Message))
  | BacklinksLoaded(Ok links) ->
    {
      state with
          Backlinks = links
          Loading = false
          Error = None
    },
    Cmd.none
  | BacklinksLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | OpenDailyNote ->
    let today = DateTime.Now
    let dailyNoteTitle = today.ToString("yyyy-MM-dd")
    let dailyNoteFolder = "daily"

    { state with Loading = true },
    Cmd.OfPromise.either (Api.createNote dailyNoteTitle) dailyNoteFolder (Ok >> NoteCreated) (fun ex ->
      NoteCreated(Error ex.Message))
  | UpdateNoteContent content ->
    match state.CurrentNote with
    | Some note ->
      let updatedNote = { note with Content = content }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = { state.EditorState with IsDirty = true }
      },
      Cmd.batch [ Cmd.ofMsg PushEditorSnapshot; Cmd.ofMsg (SaveNote updatedNote) ]
    | None -> state, Cmd.none
  | GraphNodeHovered nodeId -> { state with HoveredNode = nodeId }, Cmd.none
  | GraphZoomChanged zoomState -> { state with ZoomState = zoomState }, Cmd.none
  | GraphEngineChanged engine -> { state with GraphEngine = engine }, Cmd.none
  | HydrateFromDisk ->
    state,
    Cmd.batch [
      Cmd.OfPromise.either Api.loadSettings () (Ok >> SettingsLoaded) (fun ex -> SettingsLoaded(Error ex.Message))
      Cmd.OfPromise.either Api.loadWorkspaceSnapshot () (Ok >> WorkspaceSnapshotLoaded) (fun ex ->
        WorkspaceSnapshotLoaded(Error ex.Message))
    ]
  | SettingsLoaded(Ok settings) -> { state with Settings = Some settings; Error = None }, Cmd.none
  | SettingsLoaded(Error err) -> { state with Error = Some err }, Cmd.none
  | WorkspaceSnapshotLoaded(Ok snapshot) ->
    let sanitizedSnapshot = sanitizeSnapshot snapshot

    Browser.Dom.console.log ("WorkspaceSnapshotLoaded - Snapshot data:", sanitizedSnapshot)
    Browser.Dom.console.log ("  ActivePage:", sanitizedSnapshot.UI.ActivePage)
    Browser.Dom.console.log ("  RecentPages:", sanitizedSnapshot.UI.RecentPages)
    Browser.Dom.console.log ("  RecentPages count:", sanitizedSnapshot.UI.RecentPages.Length)
    Browser.Dom.console.log ("  LastWorkspacePath:", sanitizedSnapshot.UI.LastWorkspacePath)

    {
      state with
          WorkspaceSnapshot = Some sanitizedSnapshot
          Error = None
    },
    Cmd.none
  | WorkspaceSnapshotLoaded(Error err) -> { state with Error = Some err }, Cmd.none
  | SettingsChanged settings ->
    cancelTimer state.SettingsSaveTimer
    { state with Settings = Some settings }, debounceCmd DebouncedSettingsSave
  | DebouncedSettingsSave ->
    match state.Settings with
    | Some settings ->
      { state with SettingsSaveTimer = None },
      Cmd.OfPromise.either Api.saveSettings settings (fun _ -> SettingsSaved(Ok())) (fun ex ->
        SettingsSaved(Error ex.Message))
    | None -> state, Cmd.none
  | SettingsSaved(Ok()) -> { state with Error = None }, Cmd.none
  | SettingsSaved(Error err) -> { state with Error = Some err }, Cmd.none
  | WorkspaceSnapshotChanged snapshot ->
    cancelTimer state.SnapshotSaveTimer
    { state with WorkspaceSnapshot = Some snapshot }, debounceCmd DebouncedSnapshotSave
  | DebouncedSnapshotSave ->
    match state.WorkspaceSnapshot with
    | Some snapshot ->
      { state with SnapshotSaveTimer = None },
      Cmd.OfPromise.either Api.saveWorkspaceSnapshot snapshot (fun _ -> WorkspaceSnapshotSaved(Ok())) (fun ex ->
        WorkspaceSnapshotSaved(Error ex.Message))
    | None -> state, Cmd.none
  | WorkspaceSnapshotSaved(Ok()) -> { state with Error = None }, Cmd.none
  | WorkspaceSnapshotSaved(Error err) -> { state with Error = Some err }, Cmd.none
  | UpdateSearchFilters filters ->
    let newSearch = { state.Search with Filters = filters }
    { state with Search = newSearch }, Cmd.none
  | UpdateTagAutocomplete(show, query) ->
    let filteredTags =
      if String.IsNullOrWhiteSpace query then
        []
      else
        state.TagInfos
        |> List.filter (fun t -> t.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        |> List.map (fun t -> t.Name)
        |> List.truncate 10

    let newSearch = {
      state.Search with
          ShowTagAutocomplete = show && not (List.isEmpty filteredTags)
          TagAutocompleteQuery = query
          AvailableTags = filteredTags
    }
    { state with Search = newSearch }, Cmd.none
  | SetPreviewMode mode ->
    let newState = {
      state with
          EditorState = { state.EditorState with PreviewMode = mode }
    }

    let cmd =
      match mode, state.CurrentNote with
      | PreviewOnly, Some note
      | SplitView, Some note -> Cmd.ofMsg (RenderPreview note.Content)
      | _ -> Cmd.none

    newState, cmd
  | UpdateCursorPosition pos ->
    {
      state with
          EditorState = { state.EditorState with CursorPosition = pos }
    },
    Cmd.none
  | UpdateSelection(start, end_) ->
    {
      state with
          EditorState = {
            state.EditorState with
                SelectionStart = start
                SelectionEnd = end_
          }
    },
    Cmd.none
  | MarkEditorDirty isDirty ->
    {
      state with
          EditorState = { state.EditorState with IsDirty = isDirty }
    },
    Cmd.none
  | SetSidebarWidth width ->
    {
      state with
          UIState = { state.UIState with SidebarWidth = width }
    },
    Cmd.none
  | SetRightPanelWidth width ->
    {
      state with
          UIState = { state.UIState with RightPanelWidth = width }
    },
    Cmd.none
  | ShowModal modal ->
    {
      state with
          UIState = { state.UIState with ActiveModal = modal }
    },
    Cmd.none
  | CloseModal ->
    {
      state with
          UIState = { state.UIState with ActiveModal = NoModal }
    },
    Cmd.none
  | SetError err -> { state with Error = Some err; Success = None }, Cmd.none
  | ClearError -> { state with Error = None }, Cmd.none
  | ClearSuccess -> { state with Success = None }, Cmd.none
  | FormatBold ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newStart, newEnd =
        applyMarkdownFormat note.Content state.EditorState.SelectionStart state.EditorState.SelectionEnd "**" "**"

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  SelectionStart = Some newStart
                  SelectionEnd = Some newEnd
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | FormatItalic ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newStart, newEnd =
        applyMarkdownFormat note.Content state.EditorState.SelectionStart state.EditorState.SelectionEnd "_" "_"

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  SelectionStart = Some newStart
                  SelectionEnd = Some newEnd
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | FormatInlineCode ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newStart, newEnd =
        applyMarkdownFormat note.Content state.EditorState.SelectionStart state.EditorState.SelectionEnd "`" "`"

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  SelectionStart = Some newStart
                  SelectionEnd = Some newEnd
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | SetHeadingLevel level ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newPos =
        applyHeadingFormat note.Content state.EditorState.CursorPosition level

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  CursorPosition = Some newPos
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | RenderPreview markdown ->
    state,
    Cmd.OfPromise.either Api.renderMarkdown markdown (Ok >> PreviewRendered) (fun err ->
      PreviewRendered(Error(string err)))
  | PreviewRendered(Ok html) ->
    state,
    Cmd.OfPromise.either SyntaxHighlighter.highlightCodeBlocks html (Ok >> SyntaxHighlightingApplied) (fun err ->
      SyntaxHighlightingApplied(Error(string err)))
  | PreviewRendered(Error err) -> { state with Error = Some err }, Cmd.none
  | SyntaxHighlightingApplied(Ok html) ->
    {
      state with
          EditorState = { state.EditorState with RenderedPreview = Some html }
    },
    Cmd.none
  | SyntaxHighlightingApplied(Error err) -> { state with Error = Some err }, Cmd.none
  | BlockIndent ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newCursor =
        applyBlockIndent
          note.Content
          state.EditorState.CursorPosition
          state.EditorState.SelectionStart
          state.EditorState.SelectionEnd

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  CursorPosition = newCursor
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | BlockOutdent ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newCursor =
        applyBlockOutdent
          note.Content
          state.EditorState.CursorPosition
          state.EditorState.SelectionStart
          state.EditorState.SelectionEnd

      let updatedNote = { note with Content = newContent }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = {
              state.EditorState with
                  CursorPosition = newCursor
                  IsDirty = true
            }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | BlockNavigateUp ->
    match state.CurrentNote with
    | Some note ->
      let newCursor = navigateBlock note.Content state.EditorState.CursorPosition -1

      {
        state with
            EditorState = { state.EditorState with CursorPosition = newCursor }
      },
      Cmd.none
    | None -> state, Cmd.none
  | BlockNavigateDown ->
    match state.CurrentNote with
    | Some note ->
      let newCursor = navigateBlock note.Content state.EditorState.CursorPosition 1

      {
        state with
            EditorState = { state.EditorState with CursorPosition = newCursor }
      },
      Cmd.none
    | None -> state, Cmd.none
  | BlockFocusToggle blockId ->
    {
      state with
          EditorState = { state.EditorState with FocusedBlock = blockId }
    },
    Cmd.none
  | ToggleTaskAtCursor ->
    match state.CurrentNote, state.EditorState.CursorPosition with
    | Some note, Some cursorPos ->
      let lines = note.Content.Split('\n')
      let lineNumber = lines.[..cursorPos].Length
      state, Cmd.ofMsg (ToggleTaskAtLine lineNumber)
    | _ -> state, Cmd.none
  | ToggleTaskAtLine lineNumber ->
    match state.CurrentNote with
    | Some note ->
      { state with Loading = true },
      Cmd.OfPromise.either
        (fun () -> Api.toggleTaskInNote note.Id lineNumber)
        ()
        (fun _ -> TaskToggled(Ok()))
        (fun ex -> TaskToggled(Error ex.Message))
    | None -> state, Cmd.none
  | TaskToggled(Ok()) ->
    match state.CurrentNote with
    | Some note ->
      { state with Loading = false; Error = None }, Cmd.batch [ Cmd.ofMsg (SelectNote note.Id); Cmd.ofMsg LoadAllTasks ]
    | None -> { state with Loading = false; Error = None }, Cmd.none
  | TaskToggled(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | LoadAllTasks ->
    { state with IsLoadingTasks = true },
    Cmd.OfPromise.either Api.getAllTasks state.TaskFilter (Ok >> TasksLoaded) (fun ex -> TasksLoaded(Error ex.Message))
  | LoadTasksForNote noteId ->
    let filter = { state.TaskFilter with NoteId = Some noteId }

    { state with IsLoadingTasks = true },
    Cmd.OfPromise.either Api.getAllTasks filter (Ok >> TasksLoaded) (fun ex -> TasksLoaded(Error ex.Message))
  | TasksLoaded(Ok taskInfo) ->
    {
      state with
          AllTasks = taskInfo.Tasks
          IsLoadingTasks = false
          Error = None
    },
    Cmd.none
  | TasksLoaded(Error err) -> { state with IsLoadingTasks = false; Error = Some err }, Cmd.none
  | UpdateTaskFilter filter -> { state with TaskFilter = filter }, Cmd.ofMsg LoadAllTasks
  | PushEditorSnapshot ->
    match state.CurrentNote with
    | Some note ->
      let currentTime = DateTime.Now
      let snapshot = createEditorSnapshot note state.EditorState

      if shouldGroupEdits state.EditorState.LastChangeTimestamp currentTime then
        {
          state with
              EditorState = {
                state.EditorState with
                    LastChangeTimestamp = Some currentTime
              }
        },
        Cmd.none
      else
        {
          state with
              EditorState = {
                state.EditorState with
                    UndoStack = pushToUndoStack snapshot state.EditorState.UndoStack
                    RedoStack = []
                    LastChangeTimestamp = Some currentTime
              }
        },
        Cmd.none
    | None -> state, Cmd.none
  | Undo ->
    match state.CurrentNote, state.EditorState.UndoStack with
    | Some note, head :: tail ->
      let currentSnapshot = createEditorSnapshot note state.EditorState

      let restoredNote, restoredEditorState =
        restoreFromSnapshot head note state.EditorState

      {
        state with
            CurrentNote = Some restoredNote
            EditorState = {
              restoredEditorState with
                  UndoStack = tail
                  RedoStack = currentSnapshot :: state.EditorState.RedoStack
                  LastChangeTimestamp = Some DateTime.Now
            }
      },
      Cmd.ofMsg (SaveNote restoredNote)
    | _ -> state, Cmd.none
  | Redo ->
    match state.CurrentNote, state.EditorState.RedoStack with
    | Some note, head :: tail ->
      let currentSnapshot = createEditorSnapshot note state.EditorState

      let restoredNote, restoredEditorState =
        restoreFromSnapshot head note state.EditorState

      {
        state with
            CurrentNote = Some restoredNote
            EditorState = {
              restoredEditorState with
                  UndoStack = currentSnapshot :: state.EditorState.UndoStack
                  RedoStack = tail
                  LastChangeTimestamp = Some DateTime.Now
            }
      },
      Cmd.ofMsg (SaveNote restoredNote)
    | _ -> state, Cmd.none
