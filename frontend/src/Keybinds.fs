/// Keyboard shortcut handlers for the application
module Keybinds

open Fable.Core
open Fable.Core.JsInterop
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

/// Handles keyboard events and returns the appropriate message if a shortcut matches
let handleKeydown (e : KeyboardEvent) : Msg option =
  if isCmdOrCtrl "n" e then
    Some(ShowModal CreateNoteDialog)
  elif isCmdOrCtrl "k" e then
    Some(ShowModal SearchDialog)
  elif isCmdOrCtrl "s" e then
    None
  elif isCmdOrCtrl "b" e then
    Some(TogglePanel Panel.Backlinks)
  elif isCtrlShift "f" e then
    Some(TogglePanel Panel.SearchPanel)
  elif isCtrlShift "t" e then
    Some(TogglePanel Panel.TagsPanel)
  elif isCtrlShift "g" e then
    Some(NavigateTo GraphViewRoute)
  elif isCtrlShift "l" e then
    Some(NavigateTo NoteList)
  elif isCtrlShift "p" e then
    Some(SetPreviewMode SplitView)
  elif isKey "escape" e then
    Some CloseModal
  else
    None

/// Keyboard shortcut help text for UI display
type ShortcutHelp = { Keys : string; Description : string; Category : string }

/// Returns all available keyboard shortcuts for help/documentation
let getAllShortcuts () : ShortcutHelp list =
  let cmdKey = if isMacOS () then "Cmd" else "Ctrl"

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
      Description = "Toggle backlinks panel"
      Category = "Panels"
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
      Keys = $"{cmdKey}+Shift+P"
      Description = "Toggle preview mode"
      Category = "Editor"
    }
    {
      Keys = "Escape"
      Description = "Close modal/dialog"
      Category = "General"
    }
  ]
