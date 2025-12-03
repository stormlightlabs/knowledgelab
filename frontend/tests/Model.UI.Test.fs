module ModelUITests

open Fable.Jester
open Model

Jest.describe (
  "Model.Update (UI State)",
  fun () ->
    Jest.test (
      "TogglePanel adds panel when not present",
      fun () ->
        let initialState = { State.Default with VisiblePanels = Set.empty }
        let newState, _ = Update (TogglePanel Backlinks) initialState
        Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (true)
    )

    Jest.test (
      "TogglePanel removes panel when present",
      fun () ->
        let initialState = {
          State.Default with
              VisiblePanels = Set.ofList [ Backlinks ]
        }

        let newState, _ = Update (TogglePanel Backlinks) initialState
        Jest.expect(newState.VisiblePanels.Contains Backlinks).toEqual (false)
    )

    Jest.test (
      "SetSidebarWidth updates UI sidebar width",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetSidebarWidth 350) initialState
        Jest.expect(newState.UIState.SidebarWidth).toEqual 350
    )

    Jest.test (
      "SetRightPanelWidth updates UI right panel width",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetRightPanelWidth 450) initialState
        Jest.expect(newState.UIState.RightPanelWidth).toEqual 450
    )

    Jest.test (
      "ShowModal updates active modal",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (ShowModal CreateNoteDialog) initialState
        Jest.expect(newState.UIState.ActiveModal).toEqual CreateNoteDialog
    )

    Jest.test (
      "CloseModal clears active modal",
      fun () ->
        let initialState = {
          State.Default with
              UIState = {
                State.Default.UIState with
                    ActiveModal = CreateNoteDialog
              }
        }

        let newState, _ = Update CloseModal initialState
        Jest.expect(newState.UIState.ActiveModal).toEqual NoModal
    )

    Jest.test (
      "ClearError removes error message",
      fun () ->
        let stateWithError = { State.Default with Error = Some "Test error" }
        let newState, _ = Update ClearError stateWithError
        Jest.expect(newState.Error).toEqual (None)
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
      "ClearSuccess removes success notification",
      fun () ->
        let initialState = { State.Default with Success = Some "Saved" }
        let newState, _ = Update ClearSuccess initialState
        Jest.expect(newState.Success).toEqual None
    )
)
