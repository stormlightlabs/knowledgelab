module Domain

open System

/// Note represents a single note/document in the workspace
type Note = {
  Id : string
  Title : string
  Path : string
  Content : string
  Frontmatter : Map<string, obj>
  Blocks : Block list
  Links : Link list
  Tags : Tag list
  CreatedAt : DateTime
  ModifiedAt : DateTime
}

/// Block represents an outline-style content block within a note
and Block = {
  Id : string
  NoteId : string
  Content : string
  Level : int
  Parent : string
  Children : string list
  Position : int
  Type : BlockType
}

and BlockType =
  | Paragraph
  | Heading
  | ListItem
  | Code
  | Quote

/// Link represents a connection between notes
and Link = {
  Source : string
  Target : string
  DisplayText : string
  Type : LinkType
  BlockRef : string
}

and LinkType =
  | Wiki
  | Markdown
  | Embed
  | Block

/// Tag represents a topic or category marker
and Tag = { Name : string; NoteId : string }

/// NoteSummary provides a lightweight note representation for lists
type NoteSummary = {
  Id : string
  Title : string
  Path : string
  Tags : Tag list
  ModifiedAt : DateTime
}

/// Workspace represents a workspace configuration
type Workspace = {
  Id : string
  Name : string
  RootPath : string
  IgnorePatterns : string list
  CreatedAt : DateTime
  LastOpenedAt : DateTime
}

/// WorkspaceConfig holds workspace-specific settings
type WorkspaceConfig = {
  DailyNoteFormat : string
  DailyNoteFolder : string
  DefaultTags : string list
}

/// WorkspaceInfo provides basic workspace information
type WorkspaceInfo = {
  Workspace : Workspace
  Config : WorkspaceConfig
  NoteCount : int
  TotalBlocks : int
}

/// Graph represents the complete note graph structure
type Graph = { Nodes : string list; Edges : GraphEdge list }

/// GraphEdge represents a connection between two notes
and GraphEdge = { Source : string; Target : string; Type : string }

/// SearchQuery represents a search request with filters
type SearchQuery = {
  Query : string
  Tags : string list
  PathPrefix : string
  DateFrom : DateTime option
  DateTo : DateTime option
  Limit : int
}

/// SearchResult represents a single search result with ranking score
type SearchResult = {
  NoteId : string
  Title : string
  Path : string
  Score : float
  Tags : string list
  ModifiedAt : DateTime
}

/// GraphNode represents a node in the force-directed graph with simulation data
type GraphNode = {
  Id : string
  Label : string
  Group : int
  Degree : int
  mutable X : float option
  mutable Y : float option
  mutable Vx : float option
  mutable Vy : float option
}

/// GraphLink represents an edge between two nodes in the graph
type GraphLink = { Source : string; Target : string; Value : float }

/// GraphData holds the complete graph structure for D3 force simulation
type GraphData = { Nodes : GraphNode list; Links : GraphLink list }

/// ZoomState tracks the current pan and zoom transformation
type ZoomState = {
  Scale : float
  TranslateX : float
  TranslateY : float
} with

  static member Default = { Scale = 1.0; TranslateX = 0.0; TranslateY = 0.0 }

/// GraphEngine determines the rendering mode for the graph
type GraphEngine =
  | Svg
  | Canvas

/// FileFilter represents a file type filter for dialogs
type FileFilter = { DisplayName : string; Pattern : string }

/// DialogType represents different types of message dialogs
type DialogType =
  | InfoDialog
  | WarningDialog
  | ErrorDialog
  | QuestionDialog

  /// Helper to convert self to string for API calls
  member x.ToStr() : string =
    match x with
    | InfoDialog -> "info"
    | WarningDialog -> "warning"
    | ErrorDialog -> "error"
    | QuestionDialog -> "question"

/// GeneralSettings contains application-wide preferences
type GeneralSettings = {
  Theme : string
  Language : string
  AutoSave : bool
  AutoSaveInterval : int
}

/// EditorSettings contains editor-specific preferences
type EditorSettings = {
  FontFamily : string
  FontSize : int
  LineHeight : float
  TabSize : int
  VimMode : bool
  SpellCheck : bool
}

/// Settings represents the application-wide settings stored in settings.toml
type Settings = { General : GeneralSettings; Editor : EditorSettings }

/// WorkspaceUI contains UI state for a workspace
type WorkspaceUI = {
  ActivePage : string
  SidebarVisible : bool
  SidebarWidth : int
  RightPanelVisible : bool
  RightPanelWidth : int
  PinnedPages : string list
  RecentPages : string list
  GraphLayout : string
}

/// WorkspaceSnapshot represents the UI state for a specific workspace
type WorkspaceSnapshot = { UI : WorkspaceUI }
