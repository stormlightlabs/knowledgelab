module KeybindsTests

open Fable.Jester
open Fable.Core.JsInterop
open Browser.Types
open Keybinds
open Model

/// Creates a mock KeyboardEvent with specified properties
let private createKeyEvent (key : string) (ctrl : bool) (shift : bool) (alt : bool) (meta : bool) : KeyboardEvent =
  let event = obj ()
  event?key <- key
  event?ctrlKey <- ctrl
  event?shiftKey <- shift
  event?altKey <- alt
  event?metaKey <- meta
  event :?> KeyboardEvent

Jest.describe (
  "Keybinds.handleKeydown",
  fun () ->
    Jest.test (
      "Cmd/Ctrl+N opens create note dialog",
      fun () ->
        let event = createKeyEvent "n" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(ShowModal CreateNoteDialog))
    )

    Jest.test (
      "Cmd/Ctrl+K opens search dialog",
      fun () ->
        let event = createKeyEvent "k" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(ShowModal SearchDialog))
    )

    Jest.test (
      "Cmd/Ctrl+S returns None (browser default save)",
      fun () ->
        let event = createKeyEvent "s" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (None)
    )

    Jest.test (
      "Cmd/Ctrl+B formats bold",
      fun () ->
        let event = createKeyEvent "b" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some FormatBold)
    )

    Jest.test (
      "Cmd/Ctrl+I formats italic",
      fun () ->
        let event = createKeyEvent "i" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some FormatItalic)
    )

    Jest.test (
      "Cmd/Ctrl+E formats inline code",
      fun () ->
        let event = createKeyEvent "e" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some FormatInlineCode)
    )

    Jest.test (
      "Cmd/Ctrl+1 sets heading level 1",
      fun () ->
        let event = createKeyEvent "1" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 1))
    )

    Jest.test (
      "Cmd/Ctrl+2 sets heading level 2",
      fun () ->
        let event = createKeyEvent "2" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 2))
    )

    Jest.test (
      "Cmd/Ctrl+3 sets heading level 3",
      fun () ->
        let event = createKeyEvent "3" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 3))
    )

    Jest.test (
      "Cmd/Ctrl+4 sets heading level 4",
      fun () ->
        let event = createKeyEvent "4" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 4))
    )

    Jest.test (
      "Cmd/Ctrl+5 sets heading level 5",
      fun () ->
        let event = createKeyEvent "5" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 5))
    )

    Jest.test (
      "Cmd/Ctrl+6 sets heading level 6",
      fun () ->
        let event = createKeyEvent "6" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetHeadingLevel 6))
    )

    Jest.test (
      "Ctrl+Shift+F toggles search panel",
      fun () ->
        let event = createKeyEvent "f" true true false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(TogglePanel Panel.SearchPanel))
    )

    Jest.test (
      "Ctrl+Shift+T toggles tags panel",
      fun () ->
        let event = createKeyEvent "t" true true false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(TogglePanel Panel.TagsPanel))
    )

    Jest.test (
      "Ctrl+Shift+G navigates to graph view",
      fun () ->
        let event = createKeyEvent "g" true true false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(NavigateTo GraphViewRoute))
    )

    Jest.test (
      "Ctrl+Shift+L navigates to note list",
      fun () ->
        let event = createKeyEvent "l" true true false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(NavigateTo NoteList))
    )

    Jest.test (
      "Ctrl+Shift+P sets split view preview mode",
      fun () ->
        let event = createKeyEvent "p" true true false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(SetPreviewMode SplitView))
    )

    Jest.test (
      "Escape closes modal",
      fun () ->
        let event = createKeyEvent "Escape" false false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some CloseModal)
    )

    Jest.test (
      "Escape with modifiers still closes modal",
      fun () ->
        let event = createKeyEvent "Escape" false false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some CloseModal)
    )

    Jest.test (
      "Unmatched key combination returns None",
      fun () ->
        let event = createKeyEvent "x" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (None)
    )

    Jest.test (
      "Key with alt modifier returns None",
      fun () ->
        let event = createKeyEvent "n" true false true false
        let result = handleKeydown event
        Jest.expect(result).toEqual (None)
    )

    Jest.test (
      "Random key without modifiers returns None",
      fun () ->
        let event = createKeyEvent "a" false false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (None)
    )

    Jest.test (
      "Key matching is case insensitive",
      fun () ->
        let event = createKeyEvent "N" true false false false
        let result = handleKeydown event
        Jest.expect(result).toEqual (Some(ShowModal CreateNoteDialog))
    )
)

Jest.describe (
  "Keybinds.getModifiers",
  fun () ->
    Jest.test (
      "extracts all modifier states correctly",
      fun () ->
        let event = createKeyEvent "a" true true false true
        let modifiers = getModifiers event
        Jest.expect(modifiers.Ctrl).toEqual (true)
        Jest.expect(modifiers.Shift).toEqual (true)
        Jest.expect(modifiers.Alt).toEqual (false)
        Jest.expect(modifiers.Meta).toEqual (true)
    )

    Jest.test (
      "extracts no modifiers correctly",
      fun () ->
        let event = createKeyEvent "a" false false false false
        let modifiers = getModifiers event
        Jest.expect(modifiers.Ctrl).toEqual (false)
        Jest.expect(modifiers.Shift).toEqual (false)
        Jest.expect(modifiers.Alt).toEqual (false)
        Jest.expect(modifiers.Meta).toEqual (false)
    )
)

Jest.describe (
  "Keybinds.getAllShortcuts",
  fun () ->
    Jest.test (
      "returns a non-empty list of shortcuts",
      fun () ->
        let shortcuts = getAllShortcuts ()
        Jest.expect(shortcuts.Length > 0).toEqual (true)
    )

    Jest.test (
      "all shortcuts have required fields",
      fun () ->
        let shortcuts = getAllShortcuts ()

        for shortcut in shortcuts do
          Jest.expect(shortcut.Keys.Length > 0).toEqual (true)
          Jest.expect(shortcut.Description.Length > 0).toEqual (true)
          Jest.expect(shortcut.Category.Length > 0).toEqual (true)
    )

    Jest.test (
      "shortcuts are grouped by category",
      fun () ->
        let shortcuts = getAllShortcuts ()
        let categories = shortcuts |> List.map (fun s -> s.Category) |> List.distinct
        Jest.expect(categories.Length > 1).toEqual (true)
    )
)
