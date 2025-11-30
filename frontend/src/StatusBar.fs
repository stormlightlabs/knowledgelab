module StatusBar

open System

/// Represents status bar statistics for the current note
type StatusBarStats = {
  WordCount : int
  CharCount : int
  LineNumber : int
  ColumnNumber : int
  IsSaved : bool
}

/// Counts words in the given text using whitespace as delimiter.
/// Handles multiple spaces, tabs, and newlines correctly.
let countWords (text : string) : int =
  if String.IsNullOrWhiteSpace(text) then
    0
  else
    text.Split([| ' '; '\t'; '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries).Length

/// Counts characters in the given text (excluding whitespace for more accurate count)
let countCharacters (text : string) : int =
  if String.IsNullOrWhiteSpace(text) then 0 else text.Length

/// Counts characters excluding whitespace
let countCharactersNoSpaces (text : string) : int =
  if String.IsNullOrWhiteSpace(text) then
    0
  else
    text |> Seq.filter (fun c -> not (Char.IsWhiteSpace(c))) |> Seq.length

/// Calculates line number from cursor position.
/// Line numbers are 1-indexed.
let calculateLineNumber (content : string) (cursorPosition : int) : int =
  if String.IsNullOrEmpty(content) || cursorPosition < 0 then
    1
  else
    let safePosition = min cursorPosition content.Length
    let textBeforeCursor = content.Substring(0, safePosition)
    let lineCount = textBeforeCursor.Split('\n').Length
    lineCount

/// Calculates column number from cursor position.
/// Column numbers are 1-indexed.
let calculateColumnNumber (content : string) (cursorPosition : int) : int =
  if String.IsNullOrEmpty(content) || cursorPosition < 0 then
    1
  else
    let safePosition = min cursorPosition content.Length
    let textBeforeCursor = content.Substring(0, safePosition)
    let lastNewlineIndex = textBeforeCursor.LastIndexOf('\n')

    if lastNewlineIndex = -1 then
      safePosition + 1
    else
      safePosition - lastNewlineIndex

/// Calculates status bar statistics from note content and editor state
let calculateStats (content : string) (cursorPosition : int option) (isDirty : bool) : StatusBarStats =
  let wordCount = countWords content
  let charCount = countCharacters content
  let position = cursorPosition |> Option.defaultValue 0
  let lineNumber = calculateLineNumber content position
  let columnNumber = calculateColumnNumber content position

  {
    WordCount = wordCount
    CharCount = charCount
    LineNumber = lineNumber
    ColumnNumber = columnNumber
    IsSaved = not isDirty
  }
