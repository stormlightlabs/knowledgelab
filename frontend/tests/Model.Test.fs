module ModelTests

open Fable.Jester
open Model
open Domain

Jest.describe (
    "Model.Update",
    fun () ->
        Jest.test (
            "NavigateTo changes the current route",
            fun () ->
                let initialState = State.Default
                let newRoute = NoteList
                let newState, _ = Update (NavigateTo newRoute) initialState
                Jest.expect(newState.CurrentRoute).toEqual (newRoute)
        )

        Jest.test (
            "ClearError removes error message",
            fun () ->
                let stateWithError =
                    { State.Default with
                        Error = Some "Test error" }

                let newState, _ = Update ClearError stateWithError
                Jest.expect(newState.Error).toEqual (None)
        )

        Jest.test (
            "TogglePanel adds panel when not present",
            fun () ->
                let initialState =
                    { State.Default with
                        VisiblePanels = Set.empty }

                let newState, _ = Update (TogglePanel Backlinks) initialState
                Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (true)
        )

        Jest.test (
            "TogglePanel removes panel when present",
            fun () ->
                let initialState =
                    { State.Default with
                        VisiblePanels = Set.ofList [ Backlinks ] }

                let newState, _ = Update (TogglePanel Backlinks) initialState
                Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (false)
        )

        Jest.test (
            "UpdateSearchQuery updates the search query",
            fun () ->
                let initialState = State.Default
                let query = "test query"
                let newState, _ = Update (UpdateSearchQuery query) initialState
                Jest.expect(newState.SearchQuery).toEqual (query)
        )

        Jest.test (
            "SetError sets error message",
            fun () ->
                let initialState = State.Default
                let errorMsg = "Test error"
                let newState, _ = Update (SetError errorMsg) initialState
                Jest.expect(newState.Error).toEqual (Some errorMsg)
        )

        Jest.test (
            "Init returns default state",
            fun () ->
                let state, _ = Init()
                Jest.expect(state.Workspace).toEqual (None)
                Jest.expect(state.Notes.IsEmpty).toEqual (true)
                Jest.expect(state.CurrentNote).toEqual (None)
                Jest.expect(state.CurrentRoute).toEqual (VaultPicker)
        )
)
