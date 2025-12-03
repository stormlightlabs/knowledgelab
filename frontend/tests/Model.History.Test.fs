module ModelHistoryTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update (History)",
  fun () ->
    Jest.test (
      "Undo restores previous content and cursor position",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let previousSnapshot = {
          Content = "Previous content"
          CursorPosition = Some 8
          SelectionStart = Some 5
          SelectionEnd = Some 10
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ previousSnapshot ]
                    CursorPosition = Some 15
                    SelectionStart = None
                    SelectionEnd = None
              }
        }

        let newState, _ = Update Undo initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Previous content"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.CursorPosition).toEqual (Some 8)
        Jest.expect(newState.EditorState.SelectionStart).toEqual (Some 5)
        Jest.expect(newState.EditorState.SelectionEnd).toEqual (Some 10)
        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "Undo pushes current state to redo stack",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let previousSnapshot = {
          Content = "Previous content"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ previousSnapshot ]
                    CursorPosition = Some 15
                    RedoStack = []
              }
        }

        let newState, _ = Update Undo initialState
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 1

        let redoSnapshot = newState.EditorState.RedoStack.[0]
        Jest.expect(redoSnapshot.Content).toEqual "Current content"
        Jest.expect(redoSnapshot.CursorPosition).toEqual (Some 15)
    )

    Jest.test (
      "Undo removes item from undo stack",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let snapshot1 = {
          Content = "State 1"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let snapshot2 = {
          Content = "State 2"
          CursorPosition = Some 10
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ snapshot2; snapshot1 ]
              }
        }

        let newState, _ = Update Undo initialState
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(newState.EditorState.UndoStack.[0].Content).toEqual "State 1"
    )

    Jest.test (
      "Undo does nothing when undo stack is empty",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = { State.Default.EditorState with UndoStack = [] }
        }

        let newState, _ = Update Undo initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Current content"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 0
    )

    Jest.test (
      "Undo does nothing when no current note",
      fun () ->
        let initialState = {
          State.Default with
              CurrentNote = None
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [
                      {
                        Content = "Some content"
                        CursorPosition = Some 5
                        SelectionStart = None
                        SelectionEnd = None
                      }
                    ]
              }
        }

        let newState, _ = Update Undo initialState
        Jest.expect(newState.CurrentNote).toEqual None
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 1
    )

    Jest.test (
      "Redo restores next state from redo stack",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let nextSnapshot = {
          Content = "Next content"
          CursorPosition = Some 12
          SelectionStart = Some 8
          SelectionEnd = Some 12
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    RedoStack = [ nextSnapshot ]
                    CursorPosition = Some 5
              }
        }

        let newState, _ = Update Redo initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Next content"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.CursorPosition).toEqual (Some 12)
        Jest.expect(newState.EditorState.SelectionStart).toEqual (Some 8)
        Jest.expect(newState.EditorState.SelectionEnd).toEqual (Some 12)
    )

    Jest.test (
      "Redo pushes current state to undo stack",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let nextSnapshot = {
          Content = "Next content"
          CursorPosition = Some 10
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    RedoStack = [ nextSnapshot ]
                    UndoStack = []
                    CursorPosition = Some 5
              }
        }

        let newState, _ = Update Redo initialState
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 1

        let undoSnapshot = newState.EditorState.UndoStack.[0]
        Jest.expect(undoSnapshot.Content).toEqual "Current content"
        Jest.expect(undoSnapshot.CursorPosition).toEqual (Some 5)
    )

    Jest.test (
      "Redo does nothing when redo stack is empty",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
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

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = { State.Default.EditorState with RedoStack = [] }
        }

        let newState, _ = Update Redo initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Current content"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 0
    )

    Jest.test (
      "Redo does nothing when no current note",
      fun () ->
        let initialState = {
          State.Default with
              CurrentNote = None
              EditorState = {
                State.Default.EditorState with
                    RedoStack = [
                      {
                        Content = "Some content"
                        CursorPosition = Some 5
                        SelectionStart = None
                        SelectionEnd = None
                      }
                    ]
              }
        }

        let newState, _ = Update Redo initialState
        Jest.expect(newState.CurrentNote).toEqual None
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 1
    )

    Jest.test (
      "Undo/Redo full cycle maintains state integrity",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Version 2"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let snapshot1 = {
          Content = "Version 1"
          CursorPosition = Some 9
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ snapshot1 ]
                    CursorPosition = Some 9
              }
        }

        let afterUndo, _ = Update Undo initialState

        match afterUndo.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Version 1"
        | None -> failwith "Expected note after undo"

        Jest.expect(afterUndo.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(afterUndo.EditorState.RedoStack.Length).toEqual 1

        let afterRedo, _ = Update Redo afterUndo

        match afterRedo.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Version 2"
        | None -> failwith "Expected note after redo"

        Jest.expect(afterRedo.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(afterRedo.EditorState.RedoStack.Length).toEqual 0
    )

    Jest.test (
      "Undo stack respects max history size",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Current"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let createSnapshot i = {
          Content = $"Version {i}"
          CursorPosition = Some i
          SelectionStart = None
          SelectionEnd = None
        }

        let undoStack = [ for i in 1..100 -> createSnapshot i ]

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = undoStack
                    LastChangeTimestamp = Some(System.DateTime.Now.AddSeconds(-2.0))
              }
        }

        let newState, _ = Update PushEditorSnapshot initialState
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 100
    )

    Jest.test (
      "SelectNote saves current note's undo/redo history",
      fun () ->
        let currentNote = {
          Id = "current-note-id"
          Title = "Current Note"
          Path = "/current"
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
          Content = "Old content"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let redoSnapshot = {
          Content = "Newer content"
          CursorPosition = Some 10
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some currentNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ undoSnapshot ]
                    RedoStack = [ redoSnapshot ]
              }
        }

        let newState, _ = Update (SelectNote "other-note-id") initialState

        match newState.NoteHistories.TryFind "current-note-id" with
        | Some(savedUndo, savedRedo) ->
          Jest.expect(savedUndo.Length).toEqual 1
          Jest.expect(savedUndo.[0].Content).toEqual "Old content"
          Jest.expect(savedRedo.Length).toEqual 1
          Jest.expect(savedRedo.[0].Content).toEqual "Newer content"
        | None -> failwith "Expected note history to be saved"
    )

    Jest.test (
      "NoteLoaded restores saved undo/redo history",
      fun () ->
        let noteToLoad = {
          Id = "note-to-load-id"
          Title = "Note to Load"
          Path = "/load"
          Content = "Loaded content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let savedUndoSnapshot = {
          Content = "Saved undo content"
          CursorPosition = Some 8
          SelectionStart = Some 5
          SelectionEnd = Some 10
        }

        let savedRedoSnapshot = {
          Content = "Saved redo content"
          CursorPosition = Some 12
          SelectionStart = None
          SelectionEnd = None
        }

        let noteHistories =
          Map.empty.Add("note-to-load-id", ([ savedUndoSnapshot ], [ savedRedoSnapshot ]))

        let initialState = { State.Default with NoteHistories = noteHistories }

        let newState, _ = Update (NoteLoaded(Ok noteToLoad)) initialState

        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(newState.EditorState.UndoStack.[0].Content).toEqual "Saved undo content"
        Jest.expect(newState.EditorState.UndoStack.[0].CursorPosition).toEqual (Some 8)
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 1
        Jest.expect(newState.EditorState.RedoStack.[0].Content).toEqual "Saved redo content"
    )

    Jest.test (
      "NoteLoaded with no saved history starts with empty stacks",
      fun () ->
        let noteToLoad = {
          Id = "new-note-id"
          Title = "New Note"
          Path = "/new"
          Content = "New content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let initialState = { State.Default with NoteHistories = Map.empty }

        let newState, _ = Update (NoteLoaded(Ok noteToLoad)) initialState

        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 0
    )

    Jest.test (
      "Switching between notes preserves independent undo histories",
      fun () ->
        let note1 = {
          Id = "note-1"
          Title = "Note 1"
          Path = "/note1"
          Content = "Note 1 content v2"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let note2 = {
          Id = "note-2"
          Title = "Note 2"
          Path = "/note2"
          Content = "Note 2 content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let note1UndoSnapshot = {
          Content = "Note 1 content v1"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let stateWithNote1 = {
          State.Default with
              CurrentNote = Some note1
              EditorState = {
                State.Default.EditorState with
                    UndoStack = [ note1UndoSnapshot ]
                    RedoStack = []
              }
        }

        let stateAfterSelectNote2, _ = Update (SelectNote "note-2") stateWithNote1

        Jest.expect(stateAfterSelectNote2.NoteHistories.ContainsKey "note-1").toEqual true

        let stateWithNote2Loaded, _ = Update (NoteLoaded(Ok note2)) stateAfterSelectNote2

        Jest.expect(stateWithNote2Loaded.EditorState.UndoStack.Length).toEqual 0
        Jest.expect(stateWithNote2Loaded.EditorState.RedoStack.Length).toEqual 0

        let stateAfterSelectNote1, _ = Update (SelectNote "note-1") stateWithNote2Loaded

        let stateWithNote1Reloaded, _ = Update (NoteLoaded(Ok note1)) stateAfterSelectNote1

        Jest.expect(stateWithNote1Reloaded.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(stateWithNote1Reloaded.EditorState.UndoStack.[0].Content).toEqual "Note 1 content v1"
    )


)
