/// JSON deserialization for Wails API responses.
///
/// ### Wails/Go backend JSON serialization:
/// - Domain types (Note, Link, Tag, etc.) use camelCase: "id", "content", "title"
/// - Settings types use PascalCase: "General", "Editor", "Theme"
/// - Workspace types use PascalCase: "UI", "ActivePage", "RecentPages"
/// - Go often sends `null` instead of empty slices; decoders must coerce `null` to sensible defaults.
///
/// Fable's default JSON deserialization expects exact property name matches, so we use
/// Thoth.Json decoders that correctly map Go's JSON (camelCase OR PascalCase) to F#'s PascalCase types.
///
/// ### Guidelines that prevent regressions:
/// - ALL Wails API responses returning complex types MUST use these decoders; never consume raw
///   records from `jsNative`.
/// - Wrap every backend call as `JS.Promise<obj>` in `Api.fs` and decode using `decodeResponse`.
/// - When a Go slice/map could be `null`, decode with `Decode.oneOf` + `Decode.nil []` (or `{}`) to
///   coerce nulls into safe defaults—see `noteDecoder`'s `aliases` field.
/// - When adding/updating domain types, extend this module first, then cover the decoder with
///   `frontend/tests/Json.Test.fs`.
/// - Keep `frontend/__mocks__/@wailsjs/go/main/App.js` in sync with the APIs you import so Jest does
///   not load stale exports.
///
/// See module Api (`Api.fs`) for complete examples of proper API wrapper implementation.
module Json

open Thoth.Json
open Domain

/// Decodes a Tag from JSON
let tagDecoder : Decoder<Tag> =
  Decode.object (fun get -> {
    Name = get.Required.Field "name" Decode.string
    NoteId = get.Required.Field "noteId" Decode.string
  })

/// Decodes a NoteSummary from JSON with lowercase field names
let noteSummaryDecoder : Decoder<NoteSummary> =
  Decode.object (fun get -> {
    id = get.Required.Field "id" Decode.string
    title = get.Required.Field "title" Decode.string
    path = get.Required.Field "path" Decode.string
    tags = get.Required.Field "tags" (Decode.list tagDecoder)
    modifiedAt = get.Required.Field "modifiedAt" Decode.datetimeUtc
  })

/// Decodes a Block from JSON
let blockDecoder : Decoder<Block> =
  Decode.object (fun get ->
    let blockType =
      match get.Required.Field "type" Decode.string with
      | "paragraph" -> Paragraph
      | "heading" -> Heading
      | "listItem" -> ListItem
      | "code" -> Code
      | "quote" -> Quote
      | _ -> Paragraph

    {
      Id = get.Required.Field "id" Decode.string
      NoteId = get.Required.Field "noteId" Decode.string
      Content = get.Required.Field "content" Decode.string
      Level = get.Required.Field "level" Decode.int
      Parent = get.Required.Field "parent" Decode.string
      Children = get.Required.Field "children" (Decode.list Decode.string)
      Position = get.Required.Field "position" Decode.int
      Type = blockType
    })

/// Decodes a Link from JSON
let linkDecoder : Decoder<Link> =
  Decode.object (fun get ->
    let linkType =
      match get.Required.Field "type" Decode.string with
      | "wiki" -> Wiki
      | "markdown" -> Markdown
      | "embed" -> Embed
      | "block" -> Block
      | _ -> Wiki

    {
      Source = get.Required.Field "source" Decode.string
      Target = get.Required.Field "target" Decode.string
      DisplayText = get.Required.Field "displayText" Decode.string
      Type = linkType
      BlockRef = get.Required.Field "blockRef" Decode.string
    })

/// Decodes a Note from JSON
let noteDecoder : Decoder<Note> =
  Decode.object (fun get ->
    let frontmatterDict = get.Required.Field "frontmatter" (Decode.dict Decode.value)

    {
      Id = get.Required.Field "id" Decode.string
      Title = get.Required.Field "title" Decode.string
      Path = get.Required.Field "path" Decode.string
      Content = get.Required.Field "content" Decode.string
      Frontmatter = frontmatterDict |> Seq.map (fun kvp -> (kvp.Key, kvp.Value)) |> Map.ofSeq
      Aliases =
        get.Required.Field "aliases" (Decode.oneOf [ Decode.list Decode.string; Decode.nil [] ])
      Type = get.Required.Field "type" Decode.string
      Blocks = get.Required.Field "blocks" (Decode.list blockDecoder)
      Links = get.Required.Field "links" (Decode.list linkDecoder)
      Tags = get.Required.Field "tags" (Decode.list tagDecoder)
      CreatedAt = get.Required.Field "createdAt" Decode.datetimeUtc
      ModifiedAt = get.Required.Field "modifiedAt" Decode.datetimeUtc
    })

/// Decodes a GraphEdge from JSON
let graphEdgeDecoder : Decoder<GraphEdge> =
  Decode.object (fun get -> {
    Source = get.Required.Field "source" Decode.string
    Target = get.Required.Field "target" Decode.string
    Type = get.Required.Field "type" Decode.string
  })

/// Decodes a Graph from JSON
let graphDecoder : Decoder<Graph> =
  Decode.object (fun get -> {
    Nodes = get.Required.Field "nodes" (Decode.list Decode.string)
    Edges = get.Required.Field "edges" (Decode.list graphEdgeDecoder)
  })

/// Decodes a SearchResult from JSON
let searchResultDecoder : Decoder<SearchResult> =
  Decode.object (fun get -> {
    NoteId = get.Required.Field "noteId" Decode.string
    Title = get.Required.Field "title" Decode.string
    Path = get.Required.Field "path" Decode.string
    Score = get.Required.Field "score" Decode.float
    Tags = get.Required.Field "tags" (Decode.list Decode.string)
    ModifiedAt = get.Required.Field "modifiedAt" Decode.datetimeUtc
  })

/// Decodes a Workspace from JSON
let workspaceDecoder : Decoder<Workspace> =
  Decode.object (fun get -> {
    Id = get.Required.Field "id" Decode.string
    Name = get.Required.Field "name" Decode.string
    RootPath = get.Required.Field "rootPath" Decode.string
    IgnorePatterns = get.Required.Field "ignorePatterns" (Decode.list Decode.string)
    CreatedAt = get.Required.Field "createdAt" Decode.datetimeUtc
    LastOpenedAt = get.Required.Field "lastOpenedAt" Decode.datetimeUtc
  })

/// Decodes a WorkspaceConfig from JSON
let workspaceConfigDecoder : Decoder<WorkspaceConfig> =
  Decode.object (fun get -> {
    DailyNoteFormat = get.Required.Field "dailyNoteFormat" Decode.string
    DailyNoteFolder = get.Required.Field "dailyNoteFolder" Decode.string
    DefaultTags = get.Required.Field "defaultTags" (Decode.list Decode.string)
  })

/// Decodes WorkspaceInfo from JSON
let workspaceInfoDecoder : Decoder<WorkspaceInfo> =
  Decode.object (fun get -> {
    Workspace = get.Required.Field "workspace" workspaceDecoder
    Config = get.Required.Field "config" workspaceConfigDecoder
    NoteCount = get.Required.Field "noteCount" Decode.int
    TotalBlocks = get.Required.Field "totalBlocks" Decode.int
  })

/// Decodes GeneralSettings from JSON (Go sends PascalCase for Settings fields)
let generalSettingsDecoder : Decoder<GeneralSettings> =
  Decode.object (fun get -> {
    Theme = get.Required.Field "Theme" Decode.string
    Language = get.Required.Field "Language" Decode.string
    AutoSave = get.Required.Field "AutoSave" Decode.bool
    AutoSaveInterval = get.Required.Field "AutoSaveInterval" Decode.int
  })

/// Decodes EditorSettings from JSON (Go sends PascalCase for Settings fields)
let editorSettingsDecoder : Decoder<EditorSettings> =
  Decode.object (fun get -> {
    FontFamily = get.Required.Field "FontFamily" Decode.string
    FontSize = get.Required.Field "FontSize" Decode.int
    LineHeight = get.Required.Field "LineHeight" Decode.float
    TabSize = get.Required.Field "TabSize" Decode.int
    VimMode = get.Required.Field "VimMode" Decode.bool
    SpellCheck = get.Required.Field "SpellCheck" Decode.bool
  })

/// Decodes Settings from JSON (Go sends PascalCase for Settings)
let settingsDecoder : Decoder<Settings> =
  Decode.object (fun get -> {
    General = get.Required.Field "General" generalSettingsDecoder
    Editor = get.Required.Field "Editor" editorSettingsDecoder
  })

/// Decodes WorkspaceUI from JSON (Go sends PascalCase for WorkspaceUI)
let workspaceUIDecoder : Decoder<WorkspaceUI> =
  Decode.object (fun get -> {
    ActivePage = get.Required.Field "ActivePage" Decode.string
    SidebarVisible = get.Required.Field "SidebarVisible" Decode.bool
    SidebarWidth = get.Required.Field "SidebarWidth" Decode.int
    RightPanelVisible = get.Required.Field "RightPanelVisible" Decode.bool
    RightPanelWidth = get.Required.Field "RightPanelWidth" Decode.int
    PinnedPages = get.Required.Field "PinnedPages" (Decode.list Decode.string)
    RecentPages = get.Required.Field "RecentPages" (Decode.list Decode.string)
    GraphLayout = get.Required.Field "GraphLayout" Decode.string
  })

/// Decodes WorkspaceSnapshot from JSON (Go sends PascalCase for WorkspaceSnapshot)
let workspaceSnapshotDecoder : Decoder<WorkspaceSnapshot> =
  Decode.object (fun get -> { UI = get.Required.Field "UI" workspaceUIDecoder })
