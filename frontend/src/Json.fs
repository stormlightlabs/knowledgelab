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
      Aliases = get.Required.Field "aliases" (Decode.list Decode.string)
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
