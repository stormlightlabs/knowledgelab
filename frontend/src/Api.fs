module Api

open Fable.Core
open Domain

/// Wails runtime bindings for calling Go backend methods.
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
