module Editor

open Feliz
open Browser.Dom
open Browser.Types
open Model
open Domain
open StatusBar
open Syntax

let private normalizeLineEndings (value : string) =
  if isNull value then
    ""
  else
    value.Replace("\r\n", "\n").Replace("\r", "\n")

let private escapeHtml (input : string) =
  if isNull input then
    ""
  else
    input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

let private renderToken (token : Token) =
  let escaped = escapeHtml token.content

  let styleParts =
    [
      token.color |> Option.map (fun color -> $"color:{color}")
      token.fontStyle
      |> Option.bind (fun style ->
        [
          if style &&& 1 = 1 then Some "font-style:italic" else None
          if style &&& 2 = 2 then Some "font-weight:600" else None
          if style &&& 4 = 4 then
            Some "text-decoration:underline"
          else
            None
        ]
        |> List.choose id
        |> function
          | [] -> None
          | styles -> String.concat ";" styles |> Some)
    ]
    |> List.choose id

  if List.isEmpty styleParts then
    escaped
  else
    let styleAttr = String.concat ";" styleParts
    $"<span style=\"{styleAttr}\">{escaped}</span>"

let private tokensToHtml (result : CodeToTokensResult) =
  if isNull (box result.tokens) || result.tokens.Length = 0 then
    "&nbsp;"
  else
    result.tokens
    |> Array.mapi (fun idx line ->
      let lineHtml =
        if isNull (box line) || line.Length = 0 then
          "&nbsp;"
        else
          line |> Array.map renderToken |> String.concat ""

      if idx = result.tokens.Length - 1 then
        lineHtml
      else
        lineHtml + "<br />")
    |> String.concat ""

let private fallbackHighlight (content : string) =
  content
  |> normalizeLineEndings
  |> escapeHtml
  |> fun text -> text.Replace("\n", "<br />")

let private getSelectionOffsets (element : HTMLElement) : (int * int) option =
  let selection = window.getSelection ()

  if isNull selection || selection.rangeCount = 0 then
    None
  else
    let range = selection.getRangeAt 0.

    if
      element.contains (range.startContainer) |> not
      || element.contains (range.endContainer) |> not
    then
      None
    else
      let preSelectionRange = range.cloneRange ()
      preSelectionRange.selectNodeContents (element)
      preSelectionRange.setEnd (range.startContainer, range.startOffset)
      let start = preSelectionRange.toString().Length
      let selectionLength = range.toString().Length
      Some(start, start + selectionLength)

let private updateSelectionState (dispatch : Msg -> unit) (element : HTMLElement) =
  match getSelectionOffsets element with
  | Some(startPos, endPos) ->
    dispatch (UpdateSelection(Some startPos, Some endPos))
    dispatch (UpdateCursorPosition(Some endPos))
  | None -> ()

let private editorHighlightTheme (state : State) =
  match state.CurrentTheme with
  | Some theme when theme.Variant.ToLower() = "light" -> "github-light"
  | _ -> "vitesse-dark"

module private Editable =
  [<ReactComponent>]
  let MarkdownSurface (note : Note) (state : State) (dispatch : Msg -> unit) =
    let editorRef = React.useRef<Browser.Types.HTMLElement option> (None)
    let highlightRef = React.useRef<Browser.Types.HTMLElement option> (None)

    let highlightedHtml, setHighlightedHtml =
      React.useState (fun () -> fallbackHighlight note.Content)

    let themeName = editorHighlightTheme state

    React.useEffect (
      (fun () ->
        promise {
          try
            let! tokens = codeToTokens (normalizeLineEndings note.Content) { lang = "md"; theme = themeName }
            setHighlightedHtml (tokensToHtml tokens)
          with ex ->
            Browser.Dom.console.error ("Failed to highlight markdown editor content:", ex)
            setHighlightedHtml (fallbackHighlight note.Content)
        }
        |> ignore),
      [| box note.Content; box themeName |]
    )

    React.useEffect (
      (fun () ->
        match editorRef.current with
        | Some element ->
          let domValue =
            match element.textContent with
            | null -> ""
            | value -> normalizeLineEndings value

          let desired = normalizeLineEndings note.Content

          if domValue <> desired then
            element.textContent <- desired
        | None -> ()),
      [| box note.Content |]
    )

    let syncScroll (scrollTop : float) (scrollLeft : float) =
      match highlightRef.current with
      | Some backdrop ->
        backdrop.scrollTop <- scrollTop
        backdrop.scrollLeft <- scrollLeft
      | None -> ()

    let handleSelection () =
      match editorRef.current with
      | Some element -> updateSelectionState dispatch element
      | None -> ()

    let handleInput (_ : Event) =
      match editorRef.current with
      | Some element ->
        let text =
          match element.textContent with
          | null -> ""
          | value -> normalizeLineEndings value

        dispatch (UpdateNoteContent text)
        handleSelection ()
      | None -> ()

    let handlePaste (e : ClipboardEvent) =
      e.preventDefault ()

      let pastedText =
        match e.clipboardData with
        | null -> ""
        | data -> data.getData ("text/plain")

      document.execCommand ("insertText", false, pastedText) |> ignore

    let handleKeyDown (e : KeyboardEvent) =
      match Keybinds.getKeyPattern e with
      | Keybinds.CmdCtrl "z" ->
        e.preventDefault ()
        dispatch Undo
      | Keybinds.CmdCtrlShift "z"
      | Keybinds.CtrlShift "z"
      | Keybinds.CmdCtrl "y" ->
        e.preventDefault ()
        dispatch Redo
      | _ ->
        if e.key = "Tab" then
          e.preventDefault ()

          if e.shiftKey then
            dispatch BlockOutdent
          else
            dispatch BlockIndent

    Html.div [
      prop.className "relative flex-1 h-full w-full overflow-hidden bg-base00"
      prop.children [
        Html.div [
          prop.ref highlightRef
          prop.className
            "absolute inset-0 overflow-auto pointer-events-none px-8 py-4 font-mono text-sm whitespace-pre-wrap wrap-break-word text-base05"
          prop.dangerouslySetInnerHTML highlightedHtml
        ]
        Html.div [
          prop.ref editorRef
          prop.className
            "absolute inset-0 overflow-auto px-8 py-4 font-mono text-sm text-transparent caret-blue focus:outline-none selection:bg-blue/20 whitespace-pre-wrap wrap-break-word"
          prop.contentEditable true
          prop.role "textbox"
          prop.custom ("spellCheck", false)
          prop.tabIndex 0
          prop.custom ("autocorrect", "off")
          prop.custom ("autocomplete", "off")
          prop.custom ("autocapitalize", "off")
          prop.onInput handleInput
          prop.onKeyUp (fun _ -> handleSelection ())
          prop.onMouseUp (fun _ -> handleSelection ())
          prop.onKeyDown handleKeyDown
          prop.onBlur (fun _ -> handleSelection ())
          prop.onPaste handlePaste
          prop.onScroll (fun (e : Event) ->
            let target = e.target :?> HTMLElement
            syncScroll target.scrollTop target.scrollLeft)
          prop.style [
            style.backgroundColor "transparent"
            style.custom (
              "caret-color",
              state.CurrentTheme
              |> Option.map (fun theme -> theme.Palette.Base0D)
              |> Option.defaultValue "#7dd3fc"
            )
          ]
        ]
      ]
    ]

module NoteEditor =
  module Toolbar =
    /// Renders a toolbar button with hover state, optional active state, and disabled state
    let private toolbarButton
      (label : string)
      (title : string)
      (isActive : bool)
      (isDisabled : bool)
      (onClick : unit -> unit)
      =
      Html.button [
        prop.className (
          "px-3 py-1.5 rounded text-sm font-medium transition-all "
          + if isDisabled then
              "text-base03 cursor-not-allowed opacity-50"
            elif isActive then
              "bg-blue text-base00"
            else
              "text-base05 hover:bg-base02"
        )
        prop.title title
        prop.text label
        prop.disabled isDisabled
        prop.onClick (fun _ ->
          if not isDisabled then
            onClick ())
      ]

    /// Renders the editor toolbar with formatting buttons and preview toggles
    let Render (state : State) (dispatch : Msg -> unit) =
      Html.div [
        prop.className "flex items-center gap-2 pl-0 px-4 py-2 bg-base01 border-b border-base02 shrink-0"
        prop.children [
          Html.div [
            prop.className "flex items-center gap-1"
            prop.children [
              toolbarButton "B" "Bold (Ctrl/Cmd+B)" false false (fun () -> dispatch FormatBold)
              toolbarButton "I" "Italic (Ctrl/Cmd+I)" false false (fun () -> dispatch FormatItalic)
              toolbarButton "</>" "Code (Ctrl/Cmd+E)" false false (fun () -> dispatch FormatInlineCode)

              Html.div [ prop.className "w-px h-6 bg-base02 mx-1" ]

              Html.select [
                prop.className
                  "px-2 py-1 rounded text-sm bg-base00 text-base05 border border-base02 hover:border-blue transition-all"
                prop.title "Heading Level (Ctrl/Cmd+1-6)"
                prop.onChange (fun (value : string) ->
                  match System.Int32.TryParse(value) with
                  | true, level when level >= 1 && level <= 6 -> dispatch (SetHeadingLevel level)
                  | _ -> ())
                prop.children [
                  Html.option [ prop.value "0"; prop.text "Paragraph" ]
                  Html.option [ prop.value "1"; prop.text "Heading 1" ]
                  Html.option [ prop.value "2"; prop.text "Heading 2" ]
                  Html.option [ prop.value "3"; prop.text "Heading 3" ]
                  Html.option [ prop.value "4"; prop.text "Heading 4" ]
                  Html.option [ prop.value "5"; prop.text "Heading 5" ]
                  Html.option [ prop.value "6"; prop.text "Heading 6" ]
                ]
              ]

              Html.div [ prop.className "w-px h-6 bg-base02 mx-1" ]

              toolbarButton "↶" "Undo (Ctrl/Cmd+Z)" false (List.isEmpty state.EditorState.UndoStack) (fun () ->
                dispatch Undo)
              toolbarButton "↷" "Redo (Ctrl/Cmd+Shift+Z)" false (List.isEmpty state.EditorState.RedoStack) (fun () ->
                dispatch Redo)
            ]
          ]

          Html.div [ prop.className "flex-1" ]

          Html.div [
            prop.className "flex items-center gap-1"
            prop.children [
              toolbarButton "Edit" "Edit Only Mode" (state.EditorState.PreviewMode = EditOnly) false (fun () ->
                dispatch (SetPreviewMode EditOnly))
              toolbarButton "Preview" "Preview Only Mode" (state.EditorState.PreviewMode = PreviewOnly) false (fun () ->
                dispatch (SetPreviewMode PreviewOnly))
              toolbarButton
                "Split"
                "Split View Mode (Ctrl/Cmd+Shift+P)"
                (state.EditorState.PreviewMode = SplitView)
                false
                (fun () -> dispatch (SetPreviewMode SplitView))
            ]
          ]
        ]
      ]

  module PreviewPanel =
    /// Extracts line numbers of tasks from markdown content
    let private getTaskLineNumbers (content : string) : int list =
      let lines = content.Split('\n')
      let taskPattern = System.Text.RegularExpressions.Regex(@"^-\s+\[\s*[xX\s]\s*\]\s+")

      lines
      |> Array.mapi (fun i line -> (i + 1, line))
      |> Array.filter (fun (_, line) -> taskPattern.IsMatch(line.TrimStart()))
      |> Array.map fst
      |> Array.toList

    /// Renders the markdown preview panel with clickable task checkboxes
    [<ReactComponent>]
    let Render (state : State) (dispatch : Msg -> unit) =
      let containerRef = React.useRef None

      React.useEffect (
        (fun () ->
          match containerRef.current, state.CurrentNote with
          | Some(container : Browser.Types.HTMLElement), Some note ->
            let taskLines = getTaskLineNumbers note.Content
            let nodeList = container.querySelectorAll ("input[type='checkbox']")

            let len = int nodeList.length

            let checkboxes = [|
              for i in 0 .. len - 1 do
                let idx = float i
                yield nodeList.item idx :?> Browser.Types.HTMLInputElement
            |]

            checkboxes
            |> Array.iteri (fun i (checkbox : Browser.Types.HTMLInputElement) ->
              if i < taskLines.Length then
                let lineNumber = taskLines.[i]

                checkbox.onclick <-
                  fun (e : Browser.Types.MouseEvent) ->
                    e.preventDefault ()
                    dispatch (ToggleTaskAtLine lineNumber)
                    null)
          | _ -> ()),
        [| box state.EditorState.RenderedPreview; box state.CurrentNote |]
      )

      Html.div [
        prop.ref containerRef
        prop.className "flex-1 p-6 overflow-y-auto prose prose-invert max-w-none min-h-0"
        prop.children [
          match state.EditorState.RenderedPreview with
          | Some content -> Html.div [ prop.className "rendered-markdown"; prop.dangerouslySetInnerHTML content ]
          | None ->
            Html.div [
              prop.className "text-base03 text-center mt-8"
              prop.text "Loading preview..."
            ]
        ]
      ]

  /// Renders the status bar with save state, word/char count, cursor position, and filename
  [<ReactComponent>]
  let StatusBarView (note : Note) (cursorPosition : int option) (isDirty : bool) =
    let stats = StatusBar.calculateStats note.Content cursorPosition isDirty
    let showFullPath, setShowFullPath = React.useState false

    Html.div [
      prop.className
        "h-6 bg-base01 border-t border-base02 flex items-center justify-between px-4 text-xs text-base04 shrink-0"
      prop.children [
        Html.div [
          prop.className "flex items-center gap-4"
          prop.children [
            Html.span [
              prop.className (if stats.IsSaved then "text-green" else "text-yellow")
              prop.text (if stats.IsSaved then "Saved" else "Unsaved")
            ]
            Html.span [
              prop.className "cursor-pointer hover:text-blue default-transition"
              prop.title (
                if showFullPath then
                  note.Path
                else
                  "Click to show full path"
              )
              prop.onClick (fun _ -> setShowFullPath (not showFullPath))
              prop.text (
                if showFullPath then
                  note.Path
                else
                  let parts = note.Path.Split('/')

                  if parts.Length > 0 then
                    parts.[parts.Length - 1]
                  else
                    note.Path
              )
            ]
          ]
        ]
        Html.div [
          prop.className "flex items-center gap-4"
          prop.children [
            Html.span [ prop.text $"Words: {stats.WordCount}" ]
            Html.span [ prop.text $"Characters: {stats.CharCount}" ]
            Html.span [ prop.text $"Line {stats.LineNumber}, Column {stats.ColumnNumber}" ]
          ]
        ]
      ]
    ]

  /// Renders the note editor
  let Render (note : Note) (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "flex-1 flex flex-col bg-base00 h-full min-h-0 overflow-hidden w-full"
      prop.children [
        Html.div [
          prop.className "border-b border-base02 shrink-0 bg-base00"
          prop.children [
            Html.div [
              prop.className "p-4"
              prop.children [
                Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text note.Title ]
                Html.div [ prop.className "text-sm text-base03 mt-1"; prop.text note.Path ]
              ]
            ]
          ]
        ]

        Toolbar.Render state dispatch

        match state.EditorState.PreviewMode with
        | EditOnly ->
          Html.div [
            prop.className "flex-1 flex flex-col overflow-hidden min-h-0"
            prop.children [
              Html.div [
                prop.className "flex-1 flex flex-col min-h-0"
                prop.children [ Editable.MarkdownSurface note state dispatch ]
              ]
            ]
          ]
        | PreviewOnly -> PreviewPanel.Render state dispatch
        | SplitView ->
          Html.div [
            prop.className "flex-1 flex overflow-hidden min-h-0"
            prop.children [
              Html.div [
                prop.className "flex-1 flex flex-col overflow-hidden border-r border-base02 min-h-0"
                prop.children [
                  Html.div [
                    prop.className "flex-1 flex flex-col min-h-0"
                    prop.children [ Editable.MarkdownSurface note state dispatch ]
                  ]
                ]
              ]
              PreviewPanel.Render state dispatch
            ]
          ]

        StatusBarView note state.EditorState.CursorPosition state.EditorState.IsDirty
      ]
    ]
