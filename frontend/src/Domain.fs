module Domain

open System

/// Note represents a single note/document in the workspace
type Note =
    { Id: string
      Title: string
      Path: string
      Content: string
      Frontmatter: Map<string, obj>
      Blocks: Block list
      Links: Link list
      Tags: Tag list
      CreatedAt: DateTime
      ModifiedAt: DateTime }

/// Block represents an outline-style content block within a note
and Block =
    { Id: string
      NoteId: string
      Content: string
      Level: int
      Parent: string
      Children: string list
      Position: int
      Type: BlockType }

and BlockType =
    | Paragraph
    | Heading
    | ListItem
    | Code
    | Quote

/// Link represents a connection between notes
and Link =
    { Source: string
      Target: string
      DisplayText: string
      Type: LinkType
      BlockRef: string }

and LinkType =
    | Wiki
    | Markdown
    | Embed
    | Block

/// Tag represents a topic or category marker
and Tag = { Name: string; NoteId: string }

/// NoteSummary provides a lightweight note representation for lists
type NoteSummary =
    { Id: string
      Title: string
      Path: string
      Tags: Tag list
      ModifiedAt: DateTime }

/// Workspace represents a workspace configuration
type Workspace =
    { Id: string
      Name: string
      RootPath: string
      IgnorePatterns: string list
      CreatedAt: DateTime
      LastOpenedAt: DateTime }

/// WorkspaceConfig holds workspace-specific settings
type WorkspaceConfig =
    { DailyNoteFormat: string
      DailyNoteFolder: string
      DefaultTags: string list }

/// WorkspaceInfo provides basic workspace information
type WorkspaceInfo =
    { Workspace: Workspace
      Config: WorkspaceConfig
      NoteCount: int
      TotalBlocks: int }

/// Graph represents the complete note graph structure
type Graph =
    { Nodes: string list
      Edges: GraphEdge list }

/// GraphEdge represents a connection between two notes
and GraphEdge =
    { Source: string
      Target: string
      Type: string }

/// SearchQuery represents a search request with filters
type SearchQuery =
    { Query: string
      Tags: string list
      PathPrefix: string
      DateFrom: DateTime option
      DateTo: DateTime option
      Limit: int }

/// SearchResult represents a single search result with ranking score
type SearchResult =
    { NoteId: string
      Title: string
      Path: string
      Score: float
      Tags: string list
      ModifiedAt: DateTime }
