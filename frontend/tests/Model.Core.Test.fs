module ModelCoreTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.State.Default",
  fun () ->
    Jest.test (
      "State.Default initializes with empty state",
      fun () ->
        let defaultState = State.Default
        Jest.expect(defaultState.CurrentNote).toEqual None
        Jest.expect(defaultState.Loading).toEqual false
        Jest.expect(defaultState.Error).toEqual None
        Jest.expect(defaultState.Success).toEqual None
        Jest.expect(defaultState.Backlinks.Length).toEqual 0
        Jest.expect(defaultState.SelectedTags.Length).toEqual 0
    )

    Jest.test (
      "State.Default initializes with default UI state",
      fun () ->
        let defaultState = State.Default
        Jest.expect(defaultState.UIState.ActiveModal).toEqual NoModal
        Jest.expect(defaultState.UIState.SidebarWidth).toEqual 280
        Jest.expect(defaultState.UIState.RightPanelWidth).toEqual 300
    )

    Jest.test (
      "State.Default initializes with default editor state",
      fun () ->
        let defaultState = State.Default
        Jest.expect(defaultState.EditorState.PreviewMode).toEqual EditOnly
        Jest.expect(defaultState.EditorState.IsDirty).toEqual false
        Jest.expect(defaultState.EditorState.CursorPosition).toEqual None
        Jest.expect(defaultState.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(defaultState.EditorState.RedoStack.Length).toEqual 0
    )

    Jest.test (
      "State.Default initializes with default graph state",
      fun () ->
        let defaultState = State.Default
        Jest.expect(defaultState.Graph).toEqual None
        Jest.expect(defaultState.HoveredNode).toEqual None
        Jest.expect(defaultState.ZoomState.Scale).toEqual 1.0
        Jest.expect(defaultState.GraphEngine).toEqual Svg
    )

    Jest.test (
      "State.Default initializes with empty search state",
      fun () ->
        let defaultState = State.Default
        Jest.expect(defaultState.Search.Query).toEqual ""
        Jest.expect(defaultState.Search.Results.Length).toEqual 0
        Jest.expect(defaultState.Search.IsLoading).toEqual false
    )
)
