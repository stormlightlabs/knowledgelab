module ModelEditorTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update (Editor State)",
  fun () ->
    Jest.test (
      "SetPreviewMode updates editor preview mode",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SetPreviewMode SplitView) initialState
        Jest.expect(newState.EditorState.PreviewMode).toEqual SplitView
    )

    Jest.test (
      "UpdateCursorPosition updates editor cursor position",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (UpdateCursorPosition(Some 42)) initialState
        Jest.expect(newState.EditorState.CursorPosition).toEqual (Some 42)
    )

    Jest.test (
      "UpdateSelection updates editor selection",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (UpdateSelection(Some 10, Some 20)) initialState
        Jest.expect(newState.EditorState.SelectionStart).toEqual (Some 10)
        Jest.expect(newState.EditorState.SelectionEnd).toEqual (Some 20)
    )

    Jest.test (
      "MarkEditorDirty updates editor dirty flag",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (MarkEditorDirty true) initialState
        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "PushEditorSnapshot pushes current state to undo stack",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Initial content"
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
                    CursorPosition = Some 10
                    SelectionStart = Some 5
                    SelectionEnd = Some 15
              }
        }

        let newState, _ = Update PushEditorSnapshot initialState
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 0

        let snapshot = newState.EditorState.UndoStack.[0]
        Jest.expect(snapshot.Content).toEqual "Initial content"
        Jest.expect(snapshot.CursorPosition).toEqual (Some 10)
        Jest.expect(snapshot.SelectionStart).toEqual (Some 5)
        Jest.expect(snapshot.SelectionEnd).toEqual (Some 15)
    )

    Jest.test (
      "PushEditorSnapshot clears redo stack on new edit",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let snapshot = {
          Content = "Old content"
          CursorPosition = Some 5
          SelectionStart = None
          SelectionEnd = None
        }

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    RedoStack = [ snapshot ]
                    LastChangeTimestamp = Some(System.DateTime.Now.AddSeconds(-2.0))
              }
        }

        let newState, _ = Update PushEditorSnapshot initialState
        Jest.expect(newState.EditorState.RedoStack.Length).toEqual 0
    )

    Jest.test (
      "PushEditorSnapshot groups rapid edits within debounce window",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Content"
          Frontmatter = Map.empty
          Aliases = []
          Type = ""
          Blocks = []
          Links = []
          Tags = []
          CreatedAt = System.DateTime.Now
          ModifiedAt = System.DateTime.Now
        }

        let recentTimestamp = System.DateTime.Now.AddMilliseconds(-500.0)

        let initialState = {
          State.Default with
              CurrentNote = Some testNote
              EditorState = {
                State.Default.EditorState with
                    UndoStack = []
                    LastChangeTimestamp = Some recentTimestamp
              }
        }

        let newState, _ = Update PushEditorSnapshot initialState
        Jest.expect(newState.EditorState.UndoStack.Length).toEqual 0
    )

    Jest.test (
      "UpdateNoteContent triggers PushEditorSnapshot",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Original content"
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
                    UndoStack = []
                    LastChangeTimestamp = Some(System.DateTime.Now.AddSeconds(-2.0))
              }
        }

        let stateAfterUpdate, _ = Update (UpdateNoteContent "New content") initialState

        let stateAfterSnapshot, _ = Update PushEditorSnapshot stateAfterUpdate

        Jest.expect(stateAfterSnapshot.EditorState.UndoStack.Length).toEqual 1
        Jest.expect(stateAfterSnapshot.EditorState.UndoStack.[0].Content).toEqual "Original content"
    )
)

Jest.describe (
  "Model.Update (Editor Formatting)",
  fun () ->
    Jest.test (
      "FormatBold wraps selected text with bold markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
                    SelectionStart = Some 0
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatBold initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "**Hello** world"
        | None -> failwith "Expected note to be present"

        Jest.expect(newState.EditorState.IsDirty).toEqual true
    )

    Jest.test (
      "FormatBold inserts markers at cursor when no selection",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
                    SelectionStart = Some 5
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatBold initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Hello**** world"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatItalic wraps selected text with italic markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
                    SelectionStart = Some 6
                    SelectionEnd = Some 11
              }
        }

        let newState, _ = Update FormatItalic initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "Hello _world_"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatInlineCode wraps selected text with code markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "Hello world"
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
                    SelectionStart = Some 0
                    SelectionEnd = Some 5
              }
        }

        let newState, _ = Update FormatInlineCode initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "`Hello` world"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "SetHeadingLevel adds heading markers to current line",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "This is a heading\nSecond line"
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
              EditorState = { State.Default.EditorState with CursorPosition = Some 5 }
        }

        let newState, _ = Update (SetHeadingLevel 2) initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "## This is a heading\nSecond line"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "SetHeadingLevel removes existing heading markers",
      fun () ->
        let testNote = {
          Id = "test-id"
          Title = "Test Note"
          Path = "/test/path"
          Content = "### Already a heading\nSecond line"
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
              EditorState = { State.Default.EditorState with CursorPosition = Some 5 }
        }

        let newState, _ = Update (SetHeadingLevel 1) initialState

        match newState.CurrentNote with
        | Some note -> Jest.expect(note.Content).toEqual "# Already a heading\nSecond line"
        | None -> failwith "Expected note to be present"
    )

    Jest.test (
      "FormatBold does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatBold initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "FormatItalic does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatItalic initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "FormatInlineCode does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update FormatInlineCode initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )

    Jest.test (
      "SetHeadingLevel does nothing when no current note",
      fun () ->
        let initialState = { State.Default with CurrentNote = None }
        let newState, _ = Update (SetHeadingLevel 1) initialState
        Jest.expect(newState.CurrentNote).toEqual None
    )
)
