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

/// Search filters for advanced search functionality
type SearchFilters = {
  Tags : string list
  PathPrefix : string
  DateFrom : DateTime option
  DateTo : DateTime option
}

/// Editor preview mode
type PreviewMode =
  | EditOnly
  | PreviewOnly
  | SplitView

/// Editor state including preview mode, cursor position, and selection
type EditorState = {
  PreviewMode : PreviewMode
  CursorPosition : int option
  SelectionStart : int option
  SelectionEnd : int option
  IsDirty : bool
  RenderedPreview : string option
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
  SearchQuery : string
  SearchFilters : SearchFilters
  SearchResults : SearchResult list
  Graph : Graph option
  SelectedNode : string option
  HoveredNode : string option
  ZoomState : ZoomState
  GraphEngine : GraphEngine
  Tags : string list
  Backlinks : Link list
  Settings : Settings option
  WorkspaceSnapshot : WorkspaceSnapshot option
  SettingsSaveTimer : int option
  SnapshotSaveTimer : int option
  EditorState : EditorState
  UIState : UIState
  Loading : bool
  Error : string option
} with

  static member Default = {
    Workspace = None
    Notes = []
    CurrentNote = None
    CurrentRoute = WorkspacePicker
    CurrentUrl = []
    VisiblePanels = Set.ofList [ Backlinks ]
    SearchQuery = ""
    SearchFilters = {
      Tags = []
      PathPrefix = ""
      DateFrom = None
      DateTo = None
    }
    SearchResults = []
    Graph = None
    SelectedNode = None
    HoveredNode = None
    ZoomState = ZoomState.Default
    GraphEngine = Svg
    Tags = []
    Backlinks = []
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
    }
    UIState = {
      SidebarWidth = 280
      RightPanelWidth = 300
      ActiveModal = NoModal
    }
    Loading = false
    Error = None
  }

/// Messages represent all possible user actions and events
type Msg =
  | UrlChanged of string list
  | SelectWorkspaceFolder
  | OpenWorkspace of path : string
  | WorkspaceOpened of Result<WorkspaceInfo, string>
  | LoadNotes
  | NotesLoaded of Result<NoteSummary list, string>
  | SelectNote of noteId : string
  | NoteLoaded of Result<Note, string>
  | SaveNote of Note
  | NoteSaved of Result<unit, string>
  | CreateNote of title : string * folder : string
  | NoteCreated of Result<Note, string>
  | DeleteNote of noteId : string
  | NoteDeleted of Result<unit, string>
  | NavigateTo of Route
  | TogglePanel of Panel
  | UpdateSearchQuery of query : string
  | PerformSearch
  | SearchCompleted of Result<SearchResult list, string>
  | LoadGraph
  | GraphLoaded of Result<Graph, string>
  | LoadTags
  | TagsLoaded of Result<string list, string>
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
  | FormatBold
  | FormatItalic
  | FormatInlineCode
  | SetHeadingLevel of int
  | RenderPreview of markdown : string
  | PreviewRendered of Result<string, string>
  | SyntaxHighlightingApplied of Result<string, string>

/// Debounce delay in milliseconds
[<Literal>]
let private DebounceDelayMs = 800

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
let private applyHeadingFormat
  (content : string)
  (cursorPosition : int option)
  (level : int)
  : string * int =
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

/// Updates the workspace snapshot with a new recent page and triggers save
let private updateRecentPage (noteId : string) (state : State) : State * Cmd<Msg> =
  if String.IsNullOrWhiteSpace noteId then
    state, Cmd.none
  else
    match state.WorkspaceSnapshot with
    | Some snapshot ->
      let updatedUI = {
        snapshot.UI with
            RecentPages = addToRecentPages noteId snapshot.UI.RecentPages
            ActivePage = noteId
      }

      let updatedSnapshot = { snapshot with UI = updatedUI }

      { state with WorkspaceSnapshot = Some updatedSnapshot },
      Cmd.ofMsg (WorkspaceSnapshotChanged updatedSnapshot)
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
  | OpenWorkspace path ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.openWorkspace path (Ok >> WorkspaceOpened) (fun ex ->
      WorkspaceOpened(Error ex.Message))
  | WorkspaceOpened(Ok workspace) ->
    {
      state with
          Workspace = Some workspace
          Loading = false
          CurrentRoute = NoteList
          Error = None
    },
    Cmd.ofMsg LoadNotes
  | WorkspaceOpened(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
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

    { state with Loading = true },
    Cmd.OfPromise.either Api.getNote noteId (Ok >> NoteLoaded) (fun ex ->
      NoteLoaded(Error ex.Message))
  | NoteLoaded(Ok note) ->
    Browser.Dom.console.log ("NoteLoaded - Note data:", note)
    Browser.Dom.console.log ("  Id:", note.Id)
    Browser.Dom.console.log ("  Title:", note.Title)
    Browser.Dom.console.log ("  Path:", note.Path)
    Browser.Dom.console.log ("  Content length:", note.Content.Length)

    Browser.Dom.console.log (
      "  Content preview:",
      note.Content.Substring(0, min 100 note.Content.Length)
    )

    let stateWithNote = {
      state with
          CurrentNote = Some note
          CurrentRoute = NoteEditor note.Id
          Loading = false
          Error = None
          EditorState = { state.EditorState with IsDirty = false }
    }

    let stateWithRecent, recentCmd = updateRecentPage note.Id stateWithNote
    stateWithRecent, Cmd.batch [ Cmd.ofMsg (LoadBacklinks note.Id); recentCmd ]
  | NoteLoaded(Error err) ->
    Browser.Dom.console.error ("NoteLoaded Error:", err)
    { state with Loading = false; Error = Some err }, Cmd.none
  | SaveNote note ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.saveNote note (fun _ -> NoteSaved(Ok())) (fun ex ->
      NoteSaved(Error ex.Message))
  | NoteSaved(Ok()) ->
    { state with Loading = false; Error = None },
    Cmd.batch [ Cmd.ofMsg LoadNotes; Cmd.ofMsg LoadGraph ]
  | NoteSaved(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | CreateNote(title, folder) ->
    { state with Loading = true },
    Cmd.OfPromise.either (Api.createNote title) folder (Ok >> NoteCreated) (fun ex ->
      NoteCreated(Error ex.Message))
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
    Cmd.OfPromise.either Api.deleteNote noteId (fun _ -> NoteDeleted(Ok())) (fun ex ->
      NoteDeleted(Error ex.Message))
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
  | UpdateSearchQuery query -> { state with SearchQuery = query }, Cmd.none
  | PerformSearch ->
    let query = {
      Query = state.SearchQuery
      Tags = state.SearchFilters.Tags
      PathPrefix = state.SearchFilters.PathPrefix
      DateFrom = state.SearchFilters.DateFrom
      DateTo = state.SearchFilters.DateTo
      Limit = 50
    }

    { state with Loading = true },
    Cmd.OfPromise.either Api.search query (safeArrayToList >> Ok >> SearchCompleted) (fun ex ->
      SearchCompleted(Error ex.Message))
  | SearchCompleted(Ok results) ->
    {
      state with
          SearchResults = results
          Loading = false
          Error = None
    },
    Cmd.none
  | SearchCompleted(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | LoadGraph ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getGraph () (Ok >> GraphLoaded) (fun ex ->
      GraphLoaded(Error ex.Message))
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
  | LoadBacklinks noteId ->
    { state with Loading = true },
    Cmd.OfPromise.either
      Api.getBacklinks
      noteId
      (safeArrayToList >> Ok >> BacklinksLoaded)
      (fun ex -> BacklinksLoaded(Error ex.Message))
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
    Cmd.OfPromise.either
      (Api.createNote dailyNoteTitle)
      dailyNoteFolder
      (Ok >> NoteCreated)
      (fun ex -> NoteCreated(Error ex.Message))
  | UpdateNoteContent content ->
    match state.CurrentNote with
    | Some note ->
      let updatedNote = { note with Content = content }

      {
        state with
            CurrentNote = Some updatedNote
            EditorState = { state.EditorState with IsDirty = true }
      },
      Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | GraphNodeHovered nodeId -> { state with HoveredNode = nodeId }, Cmd.none
  | GraphZoomChanged zoomState -> { state with ZoomState = zoomState }, Cmd.none
  | GraphEngineChanged engine -> { state with GraphEngine = engine }, Cmd.none
  | HydrateFromDisk ->
    state,
    Cmd.batch [
      Cmd.OfPromise.either Api.loadSettings () (Ok >> SettingsLoaded) (fun ex ->
        SettingsLoaded(Error ex.Message))
      Cmd.OfPromise.either Api.loadWorkspaceSnapshot () (Ok >> WorkspaceSnapshotLoaded) (fun ex ->
        WorkspaceSnapshotLoaded(Error ex.Message))
    ]
  | SettingsLoaded(Ok settings) -> { state with Settings = Some settings; Error = None }, Cmd.none
  | SettingsLoaded(Error err) -> { state with Error = Some err }, Cmd.none
  | WorkspaceSnapshotLoaded(Ok snapshot) ->
    let sanitizedSnapshot =
      let sanitizedUI = {
        snapshot.UI with
            RecentPages = sanitizeRecentPages snapshot.UI.RecentPages
      }

      { snapshot with UI = sanitizedUI }

    Browser.Dom.console.log ("WorkspaceSnapshotLoaded - Snapshot data:", sanitizedSnapshot)
    Browser.Dom.console.log ("  ActivePage:", sanitizedSnapshot.UI.ActivePage)
    Browser.Dom.console.log ("  RecentPages:", sanitizedSnapshot.UI.RecentPages)
    Browser.Dom.console.log ("  RecentPages count:", sanitizedSnapshot.UI.RecentPages.Length)

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
      Cmd.OfPromise.either
        Api.saveWorkspaceSnapshot
        snapshot
        (fun _ -> WorkspaceSnapshotSaved(Ok()))
        (fun ex -> WorkspaceSnapshotSaved(Error ex.Message))
    | None -> state, Cmd.none
  | WorkspaceSnapshotSaved(Ok()) -> { state with Error = None }, Cmd.none
  | WorkspaceSnapshotSaved(Error err) -> { state with Error = Some err }, Cmd.none
  | UpdateSearchFilters filters -> { state with SearchFilters = filters }, Cmd.none
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
  | SetError err -> { state with Error = Some err }, Cmd.none
  | ClearError -> { state with Error = None }, Cmd.none
  | FormatBold ->
    match state.CurrentNote with
    | Some note ->
      let newContent, newStart, newEnd =
        applyMarkdownFormat
          note.Content
          state.EditorState.SelectionStart
          state.EditorState.SelectionEnd
          "**"
          "**"

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
        applyMarkdownFormat
          note.Content
          state.EditorState.SelectionStart
          state.EditorState.SelectionEnd
          "_"
          "_"

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
        applyMarkdownFormat
          note.Content
          state.EditorState.SelectionStart
          state.EditorState.SelectionEnd
          "`"
          "`"

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
    Cmd.OfPromise.either
      SyntaxHighlighter.highlightCodeBlocks
      html
      (Ok >> SyntaxHighlightingApplied)
      (fun err -> SyntaxHighlightingApplied(Error(string err)))
  | PreviewRendered(Error err) -> { state with Error = Some err }, Cmd.none
  | SyntaxHighlightingApplied(Ok html) ->
    {
      state with
          EditorState = { state.EditorState with RenderedPreview = Some html }
    },
    Cmd.none
  | SyntaxHighlightingApplied(Error err) -> { state with Error = Some err }, Cmd.none
