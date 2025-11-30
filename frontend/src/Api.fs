/// Wails runtime bindings for calling Go backend methods
module Api

open Fable.Core
open Fable.Core.JsInterop
open Domain
open Thoth.Json

/// Raw Wails API imports that return untyped objects
module Raw =
  [<Import("OpenWorkspace", from = "@wailsjs/go/main/App")>]
  let openWorkspace (path : string) : JS.Promise<obj> = jsNative

  [<Import("ListNotes", from = "@wailsjs/go/main/App")>]
  let listNotes () : JS.Promise<obj> = jsNative

  [<Import("GetNote", from = "@wailsjs/go/main/App")>]
  let getNote (id : string) : JS.Promise<obj> = jsNative

  [<Import("SaveNote", from = "@wailsjs/go/main/App")>]
  let saveNote (note : Note) : JS.Promise<unit> = jsNative

  [<Import("DeleteNote", from = "@wailsjs/go/main/App")>]
  let deleteNote (id : string) : JS.Promise<unit> = jsNative

  [<Import("CreateNote", from = "@wailsjs/go/main/App")>]
  let createNote (title : string) (folder : string) : JS.Promise<obj> = jsNative

  [<Import("GetBacklinks", from = "@wailsjs/go/main/App")>]
  let getBacklinks (noteId : string) : JS.Promise<obj> = jsNative

  [<Import("GetGraph", from = "@wailsjs/go/main/App")>]
  let getGraph () : JS.Promise<obj> = jsNative

  [<Import("Search", from = "@wailsjs/go/main/App")>]
  let search (query : SearchQuery) : JS.Promise<obj> = jsNative

  [<Import("GetNotesWithTag", from = "@wailsjs/go/main/App")>]
  let getNotesWithTag (tagName : string) : JS.Promise<obj> = jsNative

  [<Import("GetAllTags", from = "@wailsjs/go/main/App")>]
  let getAllTags () : JS.Promise<obj> = jsNative

  [<Import("RenderMarkdown", from = "@wailsjs/go/main/App")>]
  let renderMarkdown (markdown : string) : JS.Promise<string> = jsNative

  [<Import("SelectDirectory", from = "@wailsjs/go/main/App")>]
  let selectDirectory (title : string) : JS.Promise<string> = jsNative

  [<Import("SelectFile", from = "@wailsjs/go/main/App")>]
  let selectFile (title : string) (filters : FileFilter array) : JS.Promise<string> = jsNative

  [<Import("SelectFiles", from = "@wailsjs/go/main/App")>]
  let selectFiles (title : string) (filters : FileFilter array) : JS.Promise<string array> =
    jsNative

  [<Import("SaveFile", from = "@wailsjs/go/main/App")>]
  let saveFile
    (title : string)
    (defaultFilename : string)
    (filters : FileFilter array)
    : JS.Promise<string> =
    jsNative

  [<Import("ShowMessage", from = "@wailsjs/go/main/App")>]
  let showMessage (title : string) (message : string) (dialogType : string) : JS.Promise<string> =
    jsNative

  [<Import("LoadSettings", from = "@wailsjs/go/main/App")>]
  let loadSettings () : JS.Promise<obj> = jsNative

  [<Import("SaveSettings", from = "@wailsjs/go/main/App")>]
  let saveSettings (settings : Settings) : JS.Promise<unit> = jsNative

  [<Import("LoadWorkspaceSnapshot", from = "@wailsjs/go/main/App")>]
  let loadWorkspaceSnapshot () : JS.Promise<obj> = jsNative

  [<Import("SaveWorkspaceSnapshot", from = "@wailsjs/go/main/App")>]
  let saveWorkspaceSnapshot (snapshot : WorkspaceSnapshot) : JS.Promise<unit> = jsNative

  [<Import("ClearRecentFiles", from = "@wailsjs/go/main/App")>]
  let clearRecentFiles () : JS.Promise<obj> = jsNative

/// Helper to decode JSON response
let decodeResponse<'T> (decoder : Decoder<'T>) (response : obj) : 'T =
  let json = JS.JSON.stringify response

  match Decode.fromString decoder json with
  | Ok value -> value
  | Error err -> failwith $"JSON decode error: {err}"

/// Typed API wrappers that use Thoth.Json decoders
let openWorkspace (path : string) : JS.Promise<WorkspaceInfo> =
  Raw.openWorkspace path |> Promise.map (decodeResponse Json.workspaceInfoDecoder)

let listNotes () : JS.Promise<NoteSummary array> =
  Raw.listNotes ()
  |> Promise.map (fun response ->
    let json = JS.JSON.stringify response

    match Decode.fromString (Decode.list Json.noteSummaryDecoder) json with
    | Ok notes -> Array.ofList notes
    | Error err -> failwith $"JSON decode error: {err}")

let getNote (id : string) : JS.Promise<Note> =
  Raw.getNote id |> Promise.map (decodeResponse Json.noteDecoder)

let saveNote = Raw.saveNote
let deleteNote = Raw.deleteNote

let createNote (title : string) (folder : string) : JS.Promise<Note> =
  Raw.createNote title folder |> Promise.map (decodeResponse Json.noteDecoder)

let getBacklinks (noteId : string) : JS.Promise<Link array> =
  Raw.getBacklinks noteId
  |> Promise.map (fun response ->
    let json = JS.JSON.stringify response

    match Decode.fromString (Decode.list Json.linkDecoder) json with
    | Ok links -> Array.ofList links
    | Error err -> failwith $"JSON decode error: {err}")

let getGraph () : JS.Promise<Graph> =
  Raw.getGraph () |> Promise.map (decodeResponse Json.graphDecoder)

let search (query : SearchQuery) : JS.Promise<SearchResult array> =
  Raw.search query
  |> Promise.map (fun response ->
    let json = JS.JSON.stringify response

    match Decode.fromString (Decode.list Json.searchResultDecoder) json with
    | Ok results -> Array.ofList results
    | Error err -> failwith $"JSON decode error: {err}")

let getNotesWithTag (tagName : string) : JS.Promise<string array> =
  Raw.getNotesWithTag tagName
  |> Promise.map (fun response ->
    let json = JS.JSON.stringify response

    match Decode.fromString (Decode.list Decode.string) json with
    | Ok tags -> Array.ofList tags
    | Error err -> failwith $"JSON decode error: {err}")

let getAllTags () : JS.Promise<string array> =
  Raw.getAllTags ()
  |> Promise.map (fun response ->
    let json = JS.JSON.stringify response

    match Decode.fromString (Decode.list Decode.string) json with
    | Ok tags -> Array.ofList tags
    | Error err -> failwith $"JSON decode error: {err}")

let renderMarkdown = Raw.renderMarkdown
let selectDirectory = Raw.selectDirectory
let selectFile = Raw.selectFile
let selectFiles = Raw.selectFiles
let saveFile = Raw.saveFile
let showMessage = Raw.showMessage

let loadSettings () : JS.Promise<Settings> =
  Raw.loadSettings () |> Promise.map (decodeResponse Json.settingsDecoder)

let saveSettings = Raw.saveSettings

let loadWorkspaceSnapshot () : JS.Promise<WorkspaceSnapshot> =
  Raw.loadWorkspaceSnapshot ()
  |> Promise.map (decodeResponse Json.workspaceSnapshotDecoder)

let saveWorkspaceSnapshot = Raw.saveWorkspaceSnapshot

let clearRecentFiles () : JS.Promise<WorkspaceSnapshot> =
  Raw.clearRecentFiles ()
  |> Promise.map (decodeResponse Json.workspaceSnapshotDecoder)
