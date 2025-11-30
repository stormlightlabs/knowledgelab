/// Keyboard shortcut handlers for the application
module Keybinds

open Fable.Core
open Browser.Types
open Model

/// Keyboard modifier state
type Modifiers = { Ctrl : bool; Shift : bool; Alt : bool; Meta : bool }

/// Checks if the current platform is macOS by checking navigator.userAgent or platform
[<Emit("window.navigator.userAgent.toLowerCase().includes('mac') || window.navigator.platform.toLowerCase().includes('mac')")>]
let private isMacOS () : bool = jsNative

/// Extracts modifier state from a keyboard event
let getModifiers (e : KeyboardEvent) : Modifiers = {
  Ctrl = e.ctrlKey
  Shift = e.shiftKey
  Alt = e.altKey
  Meta = e.metaKey
}

/// Returns true if the event is Cmd/Ctrl+Key (platform-aware)
let isCmdOrCtrl (key : string) (e : KeyboardEvent) : bool =
  let modifierPressed = if isMacOS () then e.metaKey else e.ctrlKey
  modifierPressed && e.key.ToLower() = key.ToLower()

/// Returns true if the event is Ctrl+Shift+Key
let isCtrlShift (key : string) (e : KeyboardEvent) : bool =
  e.ctrlKey
  && e.shiftKey
  && not e.altKey
  && not e.metaKey
  && e.key.ToLower() = key.ToLower()

/// Returns true if the event is just the specified key without modifiers
let isKey (key : string) (e : KeyboardEvent) : bool =
  not e.ctrlKey
  && not e.shiftKey
  && not e.altKey
  && not e.metaKey
  && e.key.ToLower() = key.ToLower()

/// Keyboard shortcut patterns
type KeyPattern =
  | CmdCtrl of string
  | CtrlShift of string
  | Shift of string
  | Plain of string
  | NoMatch

/// Extracts the key pattern from a keyboard event
let getKeyPattern (e : KeyboardEvent) : KeyPattern =
  let key = e.key.ToLower()

  let cmdModifier =
    try
      if isMacOS () then e.metaKey else e.ctrlKey
    with _ ->
      e.ctrlKey

  let hasCtrlShift = e.ctrlKey && e.shiftKey && not e.altKey && not e.metaKey
  let hasShift = e.shiftKey && not e.ctrlKey && not e.altKey && not e.metaKey
  let noModifiers = not e.ctrlKey && not e.shiftKey && not e.altKey && not e.metaKey

  if cmdModifier && not e.shiftKey && not e.altKey then
    CmdCtrl key
  elif hasCtrlShift then
    CtrlShift key
  elif hasShift then
    Shift key
  elif noModifiers then
    Plain key
  else
    NoMatch

/// Handles keyboard events and returns the appropriate message if a shortcut matches
let handleKeydown (e : KeyboardEvent) : Msg option =
  match getKeyPattern e with
  | CmdCtrl "n" -> Some(ShowModal CreateNoteDialog)
  | CmdCtrl "k" -> Some(ShowModal SearchDialog)
  | CmdCtrl "s" -> None
  | CmdCtrl "b" -> Some FormatBold
  | CmdCtrl "i" -> Some FormatItalic
  | CmdCtrl "e" -> Some FormatInlineCode
  | CmdCtrl "1" -> Some(SetHeadingLevel 1)
  | CmdCtrl "2" -> Some(SetHeadingLevel 2)
  | CmdCtrl "3" -> Some(SetHeadingLevel 3)
  | CmdCtrl "4" -> Some(SetHeadingLevel 4)
  | CmdCtrl "5" -> Some(SetHeadingLevel 5)
  | CmdCtrl "6" -> Some(SetHeadingLevel 6)
  | CmdCtrl "t" -> Some ToggleTaskAtCursor
  | CtrlShift "f" -> Some(TogglePanel Panel.SearchPanel)
  | CtrlShift "t" -> Some(TogglePanel Panel.TagsPanel)
  | CtrlShift "x" -> Some(TogglePanel Panel.TasksPanel)
  | CtrlShift "g" -> Some(NavigateTo GraphViewRoute)
  | CtrlShift "l" -> Some(NavigateTo NoteList)
  | CtrlShift "p" -> Some(SetPreviewMode SplitView)
  | Plain "tab" -> Some BlockIndent
  | Shift "tab" -> Some BlockOutdent
  | Plain "arrowup" -> Some BlockNavigateUp
  | Plain "arrowdown" -> Some BlockNavigateDown
  | Plain "escape" -> Some CloseModal
  | _ -> None

/// Keyboard shortcut help text for UI display
type ShortcutHelp = { Keys : string; Description : string; Category : string }

/// Returns all available keyboard shortcuts for help/documentation
let getAllShortcuts () : ShortcutHelp list =
  let cmdKey =
    try
      if isMacOS () then "Cmd" else "Ctrl"
    with _ ->
      "Ctrl"

  [
    {
      Keys = $"{cmdKey}+N"
      Description = "Create new note"
      Category = "File"
    }
    {
      Keys = $"{cmdKey}+S"
      Description = "Save note (auto-saves by default)"
      Category = "File"
    }
    {
      Keys = $"{cmdKey}+K"
      Description = "Open search/command palette"
      Category = "Navigation"
    }
    {
      Keys = $"{cmdKey}+Shift+L"
      Description = "Go to note list"
      Category = "Navigation"
    }
    {
      Keys = $"{cmdKey}+Shift+G"
      Description = "Go to graph view"
      Category = "Navigation"
    }
    {
      Keys = $"{cmdKey}+B"
      Description = "Format bold (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+Shift+F"
      Description = "Toggle search panel"
      Category = "Panels"
    }
    {
      Keys = $"{cmdKey}+Shift+T"
      Description = "Toggle tags panel"
      Category = "Panels"
    }
    {
      Keys = $"{cmdKey}+Shift+X"
      Description = "Toggle tasks panel"
      Category = "Panels"
    }
    {
      Keys = $"{cmdKey}+T"
      Description = "Toggle task at cursor"
      Category = "Editor"
    }
    {
      Keys = $"{cmdKey}+Shift+P"
      Description = "Toggle preview mode"
      Category = "Editor"
    }
    {
      Keys = $"{cmdKey}+I"
      Description = "Format italic (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+E"
      Description = "Format inline code (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+1"
      Description = "Set heading level 1 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+2"
      Description = "Set heading level 2 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+3"
      Description = "Set heading level 3 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+4"
      Description = "Set heading level 4 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+5"
      Description = "Set heading level 5 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = $"{cmdKey}+6"
      Description = "Set heading level 6 (in editor)"
      Category = "Formatting"
    }
    {
      Keys = "Escape"
      Description = "Close modal/dialog"
      Category = "General"
    }
  ]
