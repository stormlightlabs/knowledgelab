module Model

open Elmish
open System
open Domain

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

/// App state holds all application data and UI state
type State = {
  Workspace : WorkspaceInfo option
  Notes : NoteSummary list
  CurrentNote : Note option
  CurrentRoute : Route
  VisiblePanels : Set<Panel>
  SearchQuery : string
  SearchResults : SearchResult list
  Graph : Graph option
  SelectedNode : string option
  HoveredNode : string option
  ZoomState : ZoomState
  GraphEngine : GraphEngine
  Tags : string list
  Backlinks : Link list
  Loading : bool
  Error : string option
} with

  static member Default = {
    Workspace = None
    Notes = []
    CurrentNote = None
    CurrentRoute = WorkspacePicker
    VisiblePanels = Set.ofList [ Backlinks ]
    SearchQuery = ""
    SearchResults = []
    Graph = None
    SelectedNode = None
    HoveredNode = None
    ZoomState = ZoomState.Default
    GraphEngine = Svg
    Tags = []
    Backlinks = []
    Loading = false
    Error = None
  }

/// Messages represent all possible user actions and events
type Msg =
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
  | SetError of string
  | ClearError

let Init () = State.Default, Cmd.none

let Update (msg : Msg) (state : State) : (State * Cmd<Msg>) =
  match msg with
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
    Cmd.OfPromise.either Api.listNotes () (List.ofArray >> Ok >> NotesLoaded) (fun ex ->
      NotesLoaded(Error ex.Message))
  | NotesLoaded(Ok notes) -> { state with Notes = notes; Loading = false; Error = None }, Cmd.none
  | NotesLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | SelectNote noteId ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getNote noteId (Ok >> NoteLoaded) (fun ex ->
      NoteLoaded(Error ex.Message))
  | NoteLoaded(Ok note) ->
    {
      state with
          CurrentNote = Some note
          CurrentRoute = NoteEditor note.Id
          Loading = false
          Error = None
    },
    Cmd.ofMsg (LoadBacklinks note.Id)
  | NoteLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
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
    {
      state with
          CurrentNote = Some note
          CurrentRoute = NoteEditor note.Id
          Loading = false
          Error = None
    },
    Cmd.ofMsg LoadNotes
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
  | NavigateTo route -> { state with CurrentRoute = route }, Cmd.none
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
      Tags = []
      PathPrefix = ""
      DateFrom = None
      DateTo = None
      Limit = 50
    }

    { state with Loading = true },
    Cmd.OfPromise.either Api.search query (List.ofArray >> Ok >> SearchCompleted) (fun ex ->
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
    Cmd.OfPromise.either Api.getAllTags () (List.ofArray >> Ok >> TagsLoaded) (fun ex ->
      TagsLoaded(Error ex.Message))
  | TagsLoaded(Ok tags) -> { state with Tags = tags; Loading = false; Error = None }, Cmd.none
  | TagsLoaded(Error err) -> { state with Loading = false; Error = Some err }, Cmd.none
  | LoadBacklinks noteId ->
    { state with Loading = true },
    Cmd.OfPromise.either Api.getBacklinks noteId (Array.toList >> Ok >> BacklinksLoaded) (fun ex ->
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
    Cmd.OfPromise.either
      (Api.createNote dailyNoteTitle)
      dailyNoteFolder
      (Ok >> NoteCreated)
      (fun ex -> NoteCreated(Error ex.Message))
  | UpdateNoteContent content ->
    match state.CurrentNote with
    | Some note ->
      let updatedNote = { note with Content = content }

      { state with CurrentNote = Some updatedNote }, Cmd.ofMsg (SaveNote updatedNote)
    | None -> state, Cmd.none
  | GraphNodeHovered nodeId -> { state with HoveredNode = nodeId }, Cmd.none
  | GraphZoomChanged zoomState -> { state with ZoomState = zoomState }, Cmd.none
  | GraphEngineChanged engine -> { state with GraphEngine = engine }, Cmd.none
  | SetError err -> { state with Error = Some err }, Cmd.none
  | ClearError -> { state with Error = None }, Cmd.none
