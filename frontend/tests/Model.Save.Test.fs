module ModelSaveTests

open Fable.Jester
open Elmish
open Model
open Domain

Jest.describe (
  "Model.Update (Save)",
  fun () ->
    Jest.test (
      "SaveNoteExplicitly triggers save for current note",
      fun () ->
        let testNote = {
          Id = "test-note-id"
          Title = "Test Note"
          Path = "/test"
          Content = "Test content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [
                      {
                        Content = "Old content"
                        CursorPosition = Some 5
                        SelectionStart = None
                        SelectionEnd = None
                      }
                    ]
                    RedoStack = []
              }
        }

        let newState, _ = Update SaveNoteExplicitly initialState

        Jest.expect(newState.Loading).toEqual true
    )

    Jest.test (
      "SaveNoteExplicitly does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }

        let newState, _ = Update SaveNoteExplicitly initialState

        Jest.expect(newState.Loading).toEqual false
    )

    Jest.test (
      "ExplicitSaveCompleted clears undo and redo stacks",
      fun () ->
        let undoSnapshot = {
          Content = "Old content 1"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let redoSnapshot = {
          Content = "New content 1"
          CursorPosition = Some 10
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ undoSnapshot ]
                    RedoStack = [ redoSnapshot ]
                    IsDirty = true
              }
        }

        let newState, _ = Update (ExplicitSaveCompleted(Ok())) initialState

        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 0
        Jest.expect(newState.EditorState.IsDirty).toEqual false
        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Success).toEqual (Some "Note saved")
    )

    Jest.test (
      "ExplicitSaveCompleted handles errors",
      fun () ->
        let initialState = State.Default

        let newState, _ = Update (ExplicitSaveCompleted(Error "Save failed")) initialState

        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Error).toEqual (Some "Save failed")
    )

    Jest.test (
      "Explicit save workflow: SaveNoteExplicitly -> ExplicitSaveCompleted clears history",
      fun () ->
        let testNote = {
          Id = "test-note-id"
          Title = "Test Note"
          Path = "/test"
          Content = "Current content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let undoSnapshot = {
          Content = "Previous content"
          CursorPosition = Some 8
          SelectionStart = None
          SelectionEnd = None
        }

        let stateBeforeSave = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ undoSnapshot; undoSnapshot ]
                    RedoStack = [ undoSnapshot ]
                    IsDirty = true
              }
        }

        let stateAfterSave, _ = Update SaveNoteExplicitly stateBeforeSave

        Jest.expect(stateAfterSave.EditorState.UndoStack.Length).toEqual 2
        Jest.expect(stateAfterSave.EditorState.RedoStack.Length).toEqual 1

        let stateAfterCompleted, _ = Update (ExplicitSaveCompleted(Ok())) stateAfterSave

        Jest.expect(stateAfterCompleted.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(stateAfterCompleted.EditorState.RedoStack.Length).toEqual 0
        Jest.expect(stateAfterCompleted.EditorState.IsDirty).toEqual false
    )
)
