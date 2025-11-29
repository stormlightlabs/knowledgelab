/// Wails runtime bindings for calling Go backend methods
module Api

open Fable.Core
open Domain

[<Import("OpenWorkspace", from = "@wailsjs/go/main/App")>]
let openWorkspace (path : string) : JS.Promise<WorkspaceInfo> = jsNative

[<Import("ListNotes", from = "@wailsjs/go/main/App")>]
let listNotes () : JS.Promise<NoteSummary array> = jsNative

[<Import("GetNote", from = "@wailsjs/go/main/App")>]
let getNote (id : string) : JS.Promise<Note> = jsNative

[<Import("SaveNote", from = "@wailsjs/go/main/App")>]
let saveNote (note : Note) : JS.Promise<unit> = jsNative

[<Import("DeleteNote", from = "@wailsjs/go/main/App")>]
let deleteNote (id : string) : JS.Promise<unit> = jsNative

[<Import("CreateNote", from = "@wailsjs/go/main/App")>]
let createNote (title : string) (folder : string) : JS.Promise<Note> = jsNative

[<Import("GetBacklinks", from = "@wailsjs/go/main/App")>]
let getBacklinks (noteId : string) : JS.Promise<Link array> = jsNative

[<Import("GetGraph", from = "@wailsjs/go/main/App")>]
let getGraph () : JS.Promise<Graph> = jsNative

[<Import("Search", from = "@wailsjs/go/main/App")>]
let search (query : SearchQuery) : JS.Promise<SearchResult array> = jsNative

[<Import("GetNotesWithTag", from = "@wailsjs/go/main/App")>]
let getNotesWithTag (tagName : string) : JS.Promise<string array> = jsNative

[<Import("GetAllTags", from = "@wailsjs/go/main/App")>]
let getAllTags () : JS.Promise<string array> = jsNative

[<Import("RenderMarkdown", from = "@wailsjs/go/main/App")>]
let renderMarkdown (markdown : string) : JS.Promise<string> = jsNative

[<Import("SelectDirectory", from = "@wailsjs/go/main/App")>]
let selectDirectory (title : string) : JS.Promise<string> = jsNative

[<Import("SelectFile", from = "@wailsjs/go/main/App")>]
let selectFile (title : string) (filters : FileFilter array) : JS.Promise<string> = jsNative

[<Import("SelectFiles", from = "@wailsjs/go/main/App")>]
let selectFiles (title : string) (filters : FileFilter array) : JS.Promise<string array> = jsNative

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
let loadSettings () : JS.Promise<Settings> = jsNative

[<Import("SaveSettings", from = "@wailsjs/go/main/App")>]
let saveSettings (settings : Settings) : JS.Promise<unit> = jsNative

[<Import("LoadWorkspaceSnapshot", from = "@wailsjs/go/main/App")>]
let loadWorkspaceSnapshot () : JS.Promise<WorkspaceSnapshot> = jsNative

[<Import("SaveWorkspaceSnapshot", from = "@wailsjs/go/main/App")>]
let saveWorkspaceSnapshot (snapshot : WorkspaceSnapshot) : JS.Promise<unit> = jsNative
