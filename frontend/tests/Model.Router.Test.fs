module ModelRouterTests

open Fable.Jester
open Model

Jest.describe (
  "Model.Router",
  fun () ->
    Jest.test (
      "parseUrl empty segments returns WorkspacePicker",
      fun () ->
        let route = Model.parseUrl []
        Jest.expect(route).toEqual WorkspacePicker
    )

    Jest.test (
      "parseUrl [\"notes\"] returns NoteList",
      fun () ->
        let route = Model.parseUrl [ "notes" ]
        Jest.expect(route).toEqual NoteList
    )

    Jest.test (
      "parseUrl [\"notes\"; noteId] returns NoteEditor",
      fun () ->
        let route = Model.parseUrl [ "notes"; "test-note-id" ]
        Jest.expect(route).toEqual (NoteEditor "test-note-id")
    )

    Jest.test (
      "parseUrl [\"graph\"] returns GraphViewRoute",
      fun () ->
        let route = Model.parseUrl [ "graph" ]
        Jest.expect(route).toEqual GraphViewRoute
    )

    Jest.test (
      "parseUrl [\"settings\"] returns Settings",
      fun () ->
        let route = Model.parseUrl [ "settings" ]
        Jest.expect(route).toEqual Settings
    )

    Jest.test (
      "parseUrl with invalid segments returns WorkspacePicker",
      fun () ->
        let route = Model.parseUrl [ "invalid"; "route" ]
        Jest.expect(route).toEqual WorkspacePicker
    )

    Jest.test (
      "routeToUrl WorkspacePicker returns empty list",
      fun () ->
        let url = Model.routeToUrl WorkspacePicker
        Jest.expect(url |> List.toArray).toEqual ([||])
    )

    Jest.test (
      "routeToUrl NoteList returns [\"notes\"]",
      fun () ->
        let url = Model.routeToUrl NoteList
        Jest.expect(url |> List.toArray).toEqual ([| "notes" |])
    )

    Jest.test (
      "routeToUrl NoteEditor returns [\"notes\"; noteId]",
      fun () ->
        let url = Model.routeToUrl (NoteEditor "my-note")
        Jest.expect(url |> List.toArray).toEqual ([| "notes"; "my-note" |])
    )

    Jest.test (
      "routeToUrl GraphViewRoute returns [\"graph\"]",
      fun () ->
        let url = Model.routeToUrl GraphViewRoute
        Jest.expect(url |> List.toArray).toEqual ([| "graph" |])
    )

    Jest.test (
      "routeToUrl Settings returns [\"settings\"]",
      fun () ->
        let url = Model.routeToUrl Settings
        Jest.expect(url |> List.toArray).toEqual ([| "settings" |])
    )

    Jest.test (
      "UrlChanged updates current route and URL",
      fun () ->
        let initialState = State.Default
        let segments = [ "notes"; "test-note" ]
        let newState, _ = Update (UrlChanged segments) initialState
        Jest.expect(newState.CurrentUrl |> List.toArray).toEqual (segments |> List.toArray)
        Jest.expect(newState.CurrentRoute).toEqual (NoteEditor "test-note")
    )

    Jest.test (
      "UrlChanged to graph route updates correctly",
      fun () ->
        let initialState = State.Default
        let segments = [ "graph" ]
        let newState, _ = Update (UrlChanged segments) initialState
        Jest.expect(newState.CurrentUrl |> List.toArray).toEqual (segments |> List.toArray)
        Jest.expect(newState.CurrentRoute).toEqual GraphViewRoute
    )
)
