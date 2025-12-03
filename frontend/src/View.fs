module View

open System
open System.IO
open Feliz
open Feliz.Router
open Model
open Domain
open StatusBar

module WorkspacePicker =
  /// Renders a recent file item in the workspace picker
  let private recentFileItem (workspacePath : string) (noteId : string) (dispatch : Msg -> unit) =
    let resolvedWorkspace =
      if String.IsNullOrWhiteSpace workspacePath then
        ""
      else
        workspacePath

    let fullPath =
      if String.IsNullOrWhiteSpace resolvedWorkspace then
        noteId
      else
        $"{resolvedWorkspace}/{noteId}"

    Html.div [
      prop.key $"{resolvedWorkspace}:{noteId}"
      prop.className "p-3 hover:bg-base02 cursor-pointer border border-base02 default-transition rounded"
      prop.onClick (fun _ -> dispatch (OpenRecentFile(resolvedWorkspace, noteId)))
      prop.children [
        Html.div [ prop.className "font-medium text-base05"; prop.text noteId ]
        Html.div [ prop.className "text-xs text-base03 mt-1"; prop.text fullPath ]
      ]
    ]

  /// Renders the workspace picker screen when no workspace is open
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "flex items-center justify-center h-screen bg-base00 w-full"
      prop.children [
        Html.div [
          prop.className "text-center max-w-2xl w-full px-4"
          prop.children [
            Html.div [
              prop.className "mb-8"
              prop.children [
                Html.h1 [
                  prop.className "text-5xl font-bold mb-4 text-base05"
                  prop.text "Knowledge Lab"
                ]
                Html.p [
                  prop.className "text-lg text-base04 mb-2"
                  prop.text "Your local-first, graph-based knowledge workspace"
                ]
                Html.p [
                  prop.className "text-sm text-base03"
                  prop.text "Build your personal knowledge graph with markdown, wikilinks, and powerful search"
                ]
              ]
            ]
            Html.div [
              prop.className "bg-base01 p-8 rounded-lg shadow-xl border border-base02"
              prop.children [
                Html.h2 [
                  prop.className "text-xl font-semibold mb-4 text-base05"
                  prop.text "Get Started"
                ]
                Html.p [
                  prop.className "text-base03 mb-6"
                  prop.text "Open a folder to use as your notes workspace."
                ]
                Html.p [
                  prop.className "text-base03 mb-6"
                  prop.text "All your notes will be stored locally as markdown files."
                ]
                Html.div [
                  prop.className "w-full grid gap-4 grid-cols-2"
                  prop.children [
                    Html.button [
                      prop.className
                        "bg-blue hover:bg-blue-bright text-base00 font-bold py-3 px-6 rounded default-transition shadow-md hover:shadow-lg"
                      prop.text "Open Folder"
                      prop.onClick (fun _ -> dispatch SelectWorkspaceFolder)
                    ]
                    Html.button [
                      prop.className
                        "bg-blue hover:bg-blue-bright text-base00 font-bold py-3 px-6 rounded default-transition shadow-md hover:shadow-lg"
                      prop.text "Create New Workspace"
                      prop.onClick (fun _ -> dispatch CreateWorkspace)
                    ]
                  ]
                ]

              ]
            ]

            match state.WorkspaceSnapshot with
            | Some snapshot ->
              let recentPages =
                snapshot.UI.RecentPages |> List.filter (String.IsNullOrWhiteSpace >> not)

              let workspacePath = snapshot.UI.LastWorkspacePath

              if List.isEmpty recentPages then
                Html.none
              else
                Html.div [
                  prop.className "mt-6 bg-base01 p-6 rounded-lg shadow-xl border border-base02"
                  prop.children [
                    Html.div [
                      prop.className "flex items-center justify-between mb-4"
                      prop.children [
                        Html.h3 [ prop.className "text-lg font-semibold text-base05"; prop.text "Recent Files" ]
                        Html.button [
                          prop.className
                            "text-xs text-base03 hover:text-base05 default-transition underline-offset-2 hover:underline"
                          prop.text "Clear"
                          prop.onClick (fun _ -> dispatch ClearRecentFiles)
                        ]
                      ]
                    ]
                    Html.div [
                      prop.className "space-y-2 max-h-60 overflow-y-auto"
                      prop.children (
                        recentPages
                        |> List.truncate 10
                        |> List.map (fun noteId -> recentFileItem workspacePath noteId dispatch)
                      )
                    ]
                  ]
                ]
            | None -> Html.none
          ]
        ]
      ]
    ]

module Sidebar =
  module NoteList =
    /// Renders a note list item
    let private noteListItem (note : NoteSummary) (dispatch : Msg -> unit) =
      Html.div [
        prop.key $"{note.id}"
        prop.className "p-3 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
        prop.onClick (fun _ -> dispatch (SelectNote note.id))
        prop.children [
          Html.div [ prop.className "font-semibold text-base05"; prop.text note.title ]
          Html.div [ prop.className "text-sm text-base03"; prop.text note.path ]
          Html.div [
            prop.className "flex gap-2 mt-1"
            prop.children (
              if isNull (box note.tags) then [] else note.tags
              |> List.map (fun t ->
                Html.span [
                  prop.key $"#{t.NoteId}:{t.Name}"
                  prop.className "text-xs bg-blue text-base00 px-2 py-1 rounded"
                  prop.text $"#{t.Name}"
                ])
            )
          ]
        ]
      ]

    /// Filters notes based on selected tags and filter mode
    let private filterNotesByTags
      (notes : NoteSummary list)
      (selectedTags : string list)
      (filterMode : TagFilterMode)
      : NoteSummary list =
      if List.isEmpty selectedTags then
        notes
      else
        notes
        |> List.filter (fun note ->
          let noteTags = note.tags |> List.map (fun t -> t.Name) |> Set.ofList
          let selectedSet = Set.ofList selectedTags

          match filterMode with
          | And -> Set.isSubset selectedSet noteTags
          | Or -> Set.intersect selectedSet noteTags |> Set.isEmpty |> not)

    /// Renders the notes list sidebar
    let Render (state : State) (dispatch : Msg -> unit) =
      let filteredNotes =
        filterNotesByTags state.Notes state.SelectedTags state.TagFilterMode

      Html.div [
        prop.className "w-64 bg-base01 border-r border-base02 flex flex-col h-full"
        prop.children [
          Html.div [
            prop.className "p-4 border-b border-base02 shrink-0"
            prop.children [
              Html.div [
                prop.className "flex items-center justify-between mb-2"
                prop.children [
                  Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Notes" ]
                  if not (List.isEmpty state.SelectedTags) then
                    Html.span [
                      prop.className "text-xs bg-blue text-base00 px-2 py-1 rounded"
                      prop.text $"{filteredNotes.Length}/{state.Notes.Length}"
                    ]
                ]
              ]
              Html.button [
                prop.className
                  "mt-2 w-full bg-blue hover:bg-blue-bright text-base00 text-sm font-bold py-1 px-2 rounded default-transition"
                prop.text "New Note"
                prop.onClick (fun _ -> dispatch (CreateNote("Untitled", "")))
              ]
              Html.button [
                prop.className
                  "mt-2 w-full bg-green hover:bg-green-bright text-base00 text-sm font-bold py-1 px-2 rounded default-transition"
                prop.text "Today's Note"
                prop.onClick (fun _ -> dispatch OpenDailyNote)
              ]
            ]
          ]
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children (
              if List.isEmpty filteredNotes then
                [
                  Html.div [
                    prop.className "p-4 text-center text-base03 text-sm"
                    prop.text "No notes match the selected tags"
                  ]
                ]
              else
                filteredNotes |> List.map (fun note -> noteListItem note dispatch)
            )
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
        prop.className "flex items-center gap-2 px-4 py-2 bg-base01 border-b border-base02"
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
        prop.className "flex-1 p-6 overflow-y-auto prose prose-invert max-w-none"
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
  let statusBar (note : Note) (cursorPosition : int option) (isDirty : bool) =
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
      prop.className "flex-1 flex flex-col bg-base00 h-full overflow-hidden"
      prop.children [
        Html.div [
          prop.className "p-4 border-b border-base02 shrink-0"
          prop.children [
            Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text note.Title ]
            Html.div [ prop.className "text-sm text-base03 mt-1"; prop.text note.Path ]
          ]
        ]

        Toolbar.Render state dispatch

        match state.EditorState.PreviewMode with
        | EditOnly ->
          Html.div [
            prop.className "flex-1 flex flex-col overflow-hidden min-h-0"
            prop.children [
              Html.textarea [
                prop.className
                  "flex-1 w-full font-mono text-sm bg-base00 text-base05 border border-base02 rounded m-6 p-4 focus:outline-none focus:border-blue resize-none default-transition"
                prop.value note.Content
                prop.placeholder "Start writing..."
                prop.onChange (fun (value : string) -> dispatch (UpdateNoteContent value))
                prop.onSelect (fun (e : Browser.Types.Event) ->
                  let target = e.target :?> Browser.Types.HTMLTextAreaElement
                  let start = int target.selectionStart
                  let end_ = int target.selectionEnd
                  dispatch (UpdateSelection(Some start, Some end_)))
                prop.onClick (fun (e : Browser.Types.MouseEvent) ->
                  let target = e.target :?> Browser.Types.HTMLTextAreaElement
                  let pos = int target.selectionStart
                  dispatch (UpdateCursorPosition(Some pos)))
                prop.onKeyUp (fun (e : Browser.Types.KeyboardEvent) ->
                  let target = e.target :?> Browser.Types.HTMLTextAreaElement
                  let pos = int target.selectionStart
                  dispatch (UpdateCursorPosition(Some pos)))
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
                  Html.textarea [
                    prop.className
                      "flex-1 w-full font-mono text-sm bg-base00 text-base05 border border-base02 rounded m-6 p-4 focus:outline-none focus:border-blue resize-none default-transition"
                    prop.value note.Content
                    prop.placeholder "Start writing..."
                    prop.onChange (fun (value : string) -> dispatch (UpdateNoteContent value))
                    prop.onSelect (fun (e : Browser.Types.Event) ->
                      let target = e.target :?> Browser.Types.HTMLTextAreaElement
                      let start = int target.selectionStart
                      let end_ = int target.selectionEnd
                      dispatch (UpdateSelection(Some start, Some end_)))
                    prop.onClick (fun (e : Browser.Types.MouseEvent) ->
                      let target = e.target :?> Browser.Types.HTMLTextAreaElement
                      let pos = int target.selectionStart
                      dispatch (UpdateCursorPosition(Some pos)))
                    prop.onKeyUp (fun (e : Browser.Types.KeyboardEvent) ->
                      let target = e.target :?> Browser.Types.HTMLTextAreaElement
                      let pos = int target.selectionStart
                      dispatch (UpdateCursorPosition(Some pos)))
                  ]
                ]
              ]
              PreviewPanel.Render state dispatch
            ]
          ]

        statusBar note state.EditorState.CursorPosition state.EditorState.IsDirty
      ]
    ]

module SearchPanel =
  /// Renders a search result item
  let private searchResultItem (result : SearchResult) (dispatch : Msg -> unit) =
    Html.div [
      prop.key result.NoteId
      prop.className "p-3 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
      prop.onClick (fun _ -> dispatch (SelectNote result.NoteId))
      prop.children [
        Html.div [ prop.className "font-medium text-base05 mb-1"; prop.text result.Title ]
        Html.div [ prop.className "text-xs text-base03 mb-1"; prop.text result.Path ]
        if result.Snippet <> "" then
          Html.div [
            prop.className "text-sm text-base04 mt-1 line-clamp-2"
            prop.text result.Snippet
          ]
        if not (List.isEmpty result.Tags) then
          Html.div [
            prop.className "flex gap-1 mt-2 flex-wrap"
            prop.children (
              result.Tags
              |> List.map (fun tag ->
                Html.span [
                  prop.key tag
                  prop.className "text-xs bg-blue text-base00 px-2 py-0.5 rounded"
                  prop.text $"#{tag}"
                ])
            )
          ]
      ]
    ]

  /// Renders the search panel
  [<ReactComponent>]
  let Render (state : State) (dispatch : Msg -> unit) =
    let handleInputChange (value : string) =
      dispatch (SearchQueryChanged value)

      let lastWord =
        if value.Contains(" ") then
          value.Split(' ') |> Array.last
        else
          value

      if lastWord.StartsWith("#") && lastWord.Length > 1 then
        let tagQuery = lastWord.Substring(1)
        dispatch (UpdateTagAutocomplete(true, tagQuery))
      else
        dispatch (UpdateTagAutocomplete(false, ""))

    Html.div [
      prop.className "w-80 bg-base01 border-l border-base02 flex flex-col h-full default-transition"
      prop.children [
        Html.div [
          prop.className "p-4 border-b border-base02 shrink-0"
          prop.children [
            Html.h2 [ prop.className "font-bold text-lg text-base05 mb-3"; prop.text "Search" ]
            Html.div [
              prop.className "relative"
              prop.children [
                Html.input [
                  prop.type' "text"
                  prop.className
                    "w-full px-3 py-2 pr-20 bg-base00 text-base05 border border-base02 rounded focus:outline-none focus:border-blue transition-colors"
                  prop.placeholder "Search notes... (use #tag for tags)"
                  prop.value state.Search.Query
                  prop.onChange handleInputChange
                ]
                if state.Search.Query <> "" then
                  Html.button [
                    prop.className
                      "absolute right-2 top-1/2 -translate-y-1/2 px-2 py-1 text-xs bg-base02 hover:bg-red text-base04 hover:text-base00 rounded transition-all"
                    prop.text "Clear"
                    prop.onClick (fun _ -> dispatch SearchCleared)
                  ]

                if
                  state.Search.ShowTagAutocomplete
                  && not (List.isEmpty state.Search.AvailableTags)
                then
                  Html.div [
                    prop.className
                      "absolute top-full left-0 right-0 mt-1 bg-base00 border border-base02 rounded shadow-lg z-50 max-h-48 overflow-y-auto"
                    prop.children (
                      state.Search.AvailableTags
                      |> List.map (fun tag ->
                        Html.div [
                          prop.key tag
                          prop.className "px-3 py-2 hover:bg-base02 cursor-pointer transition-colors"
                          prop.onClick (fun _ ->
                            let words = state.Search.Query.Split(' ')
                            let newWords = words.[.. words.Length - 2] |> Array.append [| $"#{tag}" |]
                            let newQuery = System.String.Join(" ", newWords)
                            dispatch (SearchQueryChanged newQuery)
                            dispatch (UpdateTagAutocomplete(false, "")))
                          prop.children [ Html.span [ prop.className "text-blue"; prop.text $"#{tag}" ] ]
                        ])
                    )
                  ]
              ]
            ]
            if state.Search.IsLoading then
              Html.div [ prop.className "text-xs text-base03 mt-2"; prop.text "Searching..." ]
            elif state.Search.Query <> "" then
              Html.div [
                prop.className "text-xs text-base03 mt-2"
                prop.text $"{state.Search.Results.Length} results"
              ]
          ]
        ]

        if state.Search.Query = "" then
          Html.div [
            prop.className "flex-1 flex items-center justify-center p-6"
            prop.children [
              Html.div [
                prop.className "text-center text-base03 text-sm max-w-xs"
                prop.children [
                  Html.div [ prop.className "text-base font-semibold mb-3"; prop.text "Search Tips" ]
                  Html.div [
                    prop.className "text-xs space-y-2 text-left"
                    prop.children [
                      Html.div [ prop.text "• Type keywords to search note content" ]
                      Html.div [ prop.text "• Use #tag to filter by tags" ]
                      Html.div [ prop.text "• Search is fuzzy - close matches work" ]
                      Html.div [ prop.text "• Results ranked by relevance (BM25)" ]
                      Html.div [
                        prop.className "mt-3 pt-2 border-t border-base02 text-center italic"
                        prop.text "Press Cmd/Ctrl+K to focus search"
                      ]
                    ]
                  ]
                ]
              ]
            ]
          ]
        elif state.Search.IsLoading then
          Html.div [
            prop.className "flex-1 flex items-center justify-center p-4"
            prop.children [ Html.div [ prop.className "text-base03 text-sm"; prop.text "Searching..." ] ]
          ]
        elif List.isEmpty state.Search.Results then
          Html.div [
            prop.className "flex-1 flex items-center justify-center p-6"
            prop.children [
              Html.div [
                prop.className "text-center text-base03 text-sm max-w-xs"
                prop.children [
                  Html.div [
                    prop.className "text-base font-semibold mb-2 text-yellow"
                    prop.text "No results found"
                  ]
                  Html.div [
                    prop.className "text-xs space-y-2"
                    prop.children [
                      Html.div [ prop.text $"No matches for '{state.Search.Query}'" ]
                      Html.div [ prop.className "mt-3 pt-2 border-t border-base02"; prop.text "Try:" ]
                      Html.div [ prop.text "• Different keywords" ]
                      Html.div [ prop.text "• Checking spelling" ]
                      Html.div [ prop.text "• Broader search terms" ]
                      Html.div [ prop.text "• Using #tags to filter" ]
                    ]
                  ]
                ]
              ]
            ]
          ]
        else
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children (state.Search.Results |> List.map (fun r -> searchResultItem r dispatch))
          ]
      ]
    ]

module TagsPanel =
  /// Renders a tag item with count and selection state
  let private tagItem (tagInfo : TagInfo) (isSelected : bool) (dispatch : Msg -> unit) =
    Html.div [
      prop.key tagInfo.Name
      prop.className (
        "p-2 hover:bg-base02 cursor-pointer border-b border-base02 transition-all flex items-center justify-between "
        + if isSelected then "bg-blue bg-opacity-20" else ""
      )
      prop.onClick (fun _ -> dispatch (ToggleTagFilter tagInfo.Name))
      prop.children [
        Html.div [
          prop.className "flex items-center gap-2 flex-1 min-w-0"
          prop.children [
            Html.span [
              prop.className (if isSelected then "text-blue" else "text-base05")
              prop.text $"#{tagInfo.Name}"
            ]
          ]
        ]
        Html.span [
          prop.className "text-xs bg-base02 text-base04 px-2 py-0.5 rounded shrink-0"
          prop.text (string tagInfo.Count)
        ]
      ]
    ]

  /// Groups tags by top-level parent (for nested tags like project/alpha)
  let private groupTagsByParent (tagInfos : TagInfo list) : Map<string, TagInfo list> =
    tagInfos
    |> List.groupBy (fun t ->
      let parts = t.Name.Split('/')
      if parts.Length > 1 then parts.[0] else "")
    |> Map.ofList

  /// Renders nested tags with indentation
  let private renderNestedTags (tagInfos : TagInfo list) (selectedTags : string list) (dispatch : Msg -> unit) =
    let grouped = groupTagsByParent tagInfos
    let rootTags = grouped |> Map.tryFind "" |> Option.defaultValue []
    let nestedGroups = grouped |> Map.remove ""

    [
      yield!
        rootTags
        |> List.map (fun t -> tagItem t (selectedTags |> List.contains t.Name) dispatch)

      for KeyValue(parent, children) in nestedGroups do
        yield
          Html.div [
            prop.className "mt-1"
            prop.children [
              Html.div [
                prop.className "px-2 py-1 text-xs font-semibold text-base04 bg-base00"
                prop.text parent
              ]
              Html.div [
                prop.className "pl-2"
                prop.children (
                  children
                  |> List.map (fun t -> tagItem t (selectedTags |> List.contains t.Name) dispatch)
                )
              ]
            ]
          ]
    ]

  /// Renders the filter mode toggle (AND/OR)
  let private filterModeToggle (mode : TagFilterMode) (selectedTags : string list) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "flex items-center gap-2 p-2 border-b border-base02 bg-base00"
      prop.children [
        Html.span [ prop.className "text-xs text-base04"; prop.text "Filter mode:" ]
        Html.button [
          prop.className (
            "px-2 py-1 text-xs rounded transition-all "
            + if mode = And then
                "bg-blue text-base00"
              else
                "bg-base02 text-base04"
          )
          prop.text "AND"
          prop.onClick (fun _ -> dispatch (SetTagFilterMode And))
        ]
        Html.button [
          prop.className (
            "px-2 py-1 text-xs rounded transition-all "
            + if mode = Or then
                "bg-blue text-base00"
              else
                "bg-base02 text-base04"
          )
          prop.text "OR"
          prop.onClick (fun _ -> dispatch (SetTagFilterMode Or))
        ]
        if not (List.isEmpty selectedTags) then
          Html.button [
            prop.className "ml-auto px-2 py-1 text-xs rounded bg-red text-base00 hover:bg-red-bright transition-all"
            prop.text "Clear"
            prop.onClick (fun _ -> dispatch ClearTagFilters)
          ]
      ]
    ]

  /// Renders the tags panel
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "w-64 bg-base01 border-l border-base02 flex flex-col h-full default-transition"
      prop.children [
        Html.div [
          prop.className "p-4 border-b border-base02 shrink-0"
          prop.children [
            Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Tags" ]
            Html.div [
              prop.className "text-xs text-base03 mt-1"
              prop.text $"{state.TagInfos.Length} tags"
            ]
          ]
        ]

        if not (List.isEmpty state.SelectedTags) then
          filterModeToggle state.TagFilterMode state.SelectedTags dispatch
        else
          Html.none

        if state.TagInfos.IsEmpty then
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children [
              Html.div [
                prop.className "p-4 text-center text-base03 text-sm"
                prop.text "No tags found"
              ]
            ]
          ]
        else
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children (
              state.TagInfos |> List.sortBy (fun t -> t.Name) |> renderNestedTags
              <| state.SelectedTags
              <| dispatch
            )
          ]
      ]
    ]

module BacklinksPanel =
  /// Renders a backlink item
  let private backlinkItem (link : Link) (dispatch : Msg -> unit) =
    Html.div [
      prop.key $"{link.DisplayText}_{link.Source}:{link.Target}"
      prop.className "p-2 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
      prop.onClick (fun _ -> dispatch (SelectNote link.Source))
      prop.children [
        Html.div [ prop.className "text-sm text-base05"; prop.text link.DisplayText ]
        Html.div [ prop.className "text-xs text-base03"; prop.text $"From: {link.Source}" ]
      ]
    ]

  /// Renders the backlinks panel
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "w-64 bg-base01 border-l border-base02 flex flex-col h-full default-transition"
      prop.children [
        Html.div [
          prop.className "p-4 border-b border-base02 shrink-0"
          prop.children [
            Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Backlinks" ]
            Html.div [
              prop.className "text-xs text-base03 mt-1"
              prop.text $"{state.Backlinks.Length} links"
            ]
          ]
        ]
        if state.Backlinks.IsEmpty then
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children [
              Html.div [
                prop.className "p-4 text-center text-base03 text-sm"
                prop.text "No backlinks found"
              ]
            ]
          ]
        else
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children (state.Backlinks |> List.map (fun link -> backlinkItem link dispatch))
          ]
      ]
    ]

module TaskPanel =
  /// Renders a task item with completion checkbox and metadata
  let private taskItem (task : Task) (dispatch : Msg -> unit) =
    let formattedCreatedAt = task.CreatedAt.ToString("yyyy-MM-dd")

    Html.div [
      prop.key task.Id
      prop.className "p-3 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
      prop.onClick (fun _ -> dispatch (SelectNote task.NoteId))
      prop.children [
        Html.div [
          prop.className "flex items-start gap-2"
          prop.children [
            Html.div [
              prop.className "shrink-0 mt-0.5"
              prop.children [
                if task.IsCompleted then
                  Html.span [ prop.className "text-green text-base"; prop.text "\u2611" ]
                else
                  Html.span [ prop.className "text-base03 text-base"; prop.text "\u2610" ]
              ]
            ]
            Html.div [
              prop.className "flex-1 min-w-0"
              prop.children [
                Html.div [
                  prop.className (
                    if task.IsCompleted then
                      "text-sm text-base04 line-through"
                    else
                      "text-sm text-base05"
                  )
                  prop.text task.Content
                ]
                Html.div [ prop.className "text-xs text-base03 mt-1"; prop.text task.NotePath ]
                Html.div [
                  prop.className "text-xs text-base03 mt-0.5"
                  prop.children [
                    Html.text $"Created: {formattedCreatedAt}"
                    match task.CompletedAt with
                    | Some completedAt ->
                      let formattedCompletedAt = completedAt.ToString("yyyy-MM-dd")
                      Html.text $" | Completed: {formattedCompletedAt}"
                    | None -> Html.none
                  ]
                ]
              ]
            ]
          ]
        ]
      ]
    ]

  /// Formats a date for the date input field (YYYY-MM-DD)
  let private formatDateForInput (date : DateTime option) : string =
    match date with
    | Some d -> d.ToString("yyyy-MM-dd")
    | None -> ""

  /// Parses a date from the date input field (YYYY-MM-DD)
  let private parseDateFromInput (value : string) : DateTime option =
    if String.IsNullOrWhiteSpace value then
      None
    else
      match DateTime.TryParse(value) with
      | true, date -> Some date
      | false, _ -> None

  /// Renders filter controls for the task panel
  let private filterControls (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "p-3 border-b border-base02 bg-base00"
      prop.children [
        Html.div [
          prop.className "flex flex-col gap-2"
          prop.children [
            Html.div [
              prop.children [
                Html.label [ prop.className "text-xs text-base04 mb-1 block"; prop.text "Status" ]
                Html.select [
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (
                    match state.TaskFilter.Status with
                    | None -> "all"
                    | Some true -> "completed"
                    | Some false -> "pending"
                  )
                  prop.onChange (fun (value : string) ->
                    let newStatus =
                      match value with
                      | "completed" -> Some true
                      | "pending" -> Some false
                      | _ -> None

                    dispatch (UpdateTaskFilter { state.TaskFilter with Status = newStatus }))
                  prop.children [
                    Html.option [ prop.value "all"; prop.text "All Tasks" ]
                    Html.option [ prop.value "pending"; prop.text "Pending" ]
                    Html.option [ prop.value "completed"; prop.text "Completed" ]
                  ]
                ]
              ]
            ]

            Html.div [
              prop.children [
                Html.label [ prop.className "text-xs text-base04 mb-1 block"; prop.text "Note" ]
                Html.select [
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (state.TaskFilter.NoteId |> Option.defaultValue "all")
                  prop.onChange (fun (value : string) ->
                    let newNoteId = if value = "all" then None else Some value
                    dispatch (UpdateTaskFilter { state.TaskFilter with NoteId = newNoteId }))
                  prop.children (
                    Html.option [ prop.value "all"; prop.text "All Notes" ]
                    :: (state.Notes
                        |> List.map (fun note -> Html.option [ prop.value note.id; prop.text note.title ]))
                  )
                ]
              ]
            ]

            Html.div [
              prop.children [
                Html.label [ prop.className "text-xs text-base04 mb-1 block"; prop.text "Created After" ]
                Html.input [
                  prop.type' "date"
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (formatDateForInput state.TaskFilter.CreatedAfter)
                  prop.onChange (fun (value : string) ->
                    let newDate = parseDateFromInput value
                    dispatch (UpdateTaskFilter { state.TaskFilter with CreatedAfter = newDate }))
                ]
              ]
            ]

            Html.div [
              prop.children [
                Html.label [ prop.className "text-xs text-base04 mb-1 block"; prop.text "Created Before" ]
                Html.input [
                  prop.type' "date"
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (formatDateForInput state.TaskFilter.CreatedBefore)
                  prop.onChange (fun (value : string) ->
                    let newDate = parseDateFromInput value
                    dispatch (UpdateTaskFilter { state.TaskFilter with CreatedBefore = newDate }))
                ]
              ]
            ]

            Html.div [
              prop.children [
                Html.label [ prop.className "text-xs text-base04 mb-1 block"; prop.text "Completed After" ]
                Html.input [
                  prop.type' "date"
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (formatDateForInput state.TaskFilter.CompletedAfter)
                  prop.onChange (fun (value : string) ->
                    let newDate = parseDateFromInput value
                    dispatch (UpdateTaskFilter { state.TaskFilter with CompletedAfter = newDate }))
                ]
              ]
            ]

            Html.div [
              prop.children [
                Html.label [
                  prop.className "text-xs text-base04 mb-1 block"
                  prop.text "Completed Before"
                ]
                Html.input [
                  prop.type' "date"
                  prop.className "w-full px-2 py-1 text-sm bg-base01 text-base05 border border-base02 rounded"
                  prop.value (formatDateForInput state.TaskFilter.CompletedBefore)
                  prop.onChange (fun (value : string) ->
                    let newDate = parseDateFromInput value
                    dispatch (UpdateTaskFilter { state.TaskFilter with CompletedBefore = newDate }))
                ]
              ]
            ]

            Html.button [
              prop.className
                "mt-2 w-full bg-base02 hover:bg-base03 text-base05 text-xs font-medium py-1 px-2 rounded default-transition"
              prop.text "Clear Filters"
              prop.onClick (fun _ -> dispatch (UpdateTaskFilter TaskFilter.Default))
            ]
          ]
        ]
      ]
    ]

  /// Renders the tasks panel
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "w-80 bg-base01 border-l border-base02 flex flex-col h-full default-transition"
      prop.children [
        Html.div [
          prop.className "p-4 border-b border-base02 shrink-0"
          prop.children [
            Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Tasks" ]
            Html.div [
              prop.className "text-xs text-base03 mt-1"
              prop.text
                $"{state.AllTasks |> List.filter (fun t -> not t.IsCompleted) |> List.length} pending, {state.AllTasks |> List.filter (fun t -> t.IsCompleted) |> List.length} completed"
            ]
          ]
        ]
        filterControls state dispatch
        if state.IsLoadingTasks then
          Html.div [
            prop.className "flex-1 flex items-center justify-center"
            prop.children [ Html.div [ prop.className "text-base03"; prop.text "Loading tasks..." ] ]
          ]
        elif state.AllTasks.IsEmpty then
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children [
              Html.div [
                prop.className "p-4 text-center text-base03 text-sm"
                prop.text "No tasks found"
              ]
            ]
          ]
        else
          Html.div [
            prop.className "flex-1 overflow-y-auto min-h-0"
            prop.children (state.AllTasks |> List.map (fun task -> taskItem task dispatch))
          ]
      ]
    ]

module SettingsPanel =
  /// Renders a settings section with a title and content
  let private section (title : string) children =
    Html.div [
      prop.className "bg-base01 p-4 rounded border border-base02 mb-4"
      prop.children (
        Html.h3 [ prop.className "font-semibold text-base05 mb-3"; prop.text title ]
        :: children
      )
    ]

  /// Renders a labeled select dropdown
  let private selectField
    (label : string)
    (value : string)
    (options : List<string * string>)
    (onChange : string -> unit)
    =
    Html.div [
      prop.className "mb-3"
      prop.children [
        Html.label [ prop.className "block text-sm font-medium text-base04 mb-1"; prop.text label ]
        Html.select [
          prop.className
            "w-full bg-base00 border border-base02 rounded px-3 py-2 text-base05 focus:outline-none focus:border-blue"
          prop.value value
          prop.onChange (fun (v : string) -> onChange v)
          prop.children (
            options
            |> List.map (fun (v, label) -> Html.option [ prop.value v; prop.text label ])
          )
        ]
      ]
    ]

  /// Renders a labeled number input with range
  let private numberField label (value : int) (min : int) (max : int) onChange =
    Html.div [
      prop.className "mb-3"
      prop.children [
        Html.label [
          prop.className "block text-sm font-medium text-base04 mb-1"
          prop.text $"{label}: {value}"
        ]
        Html.input [
          prop.type' "range"
          prop.min min
          prop.max max
          prop.value value
          prop.className "w-full accent-blue"
          prop.onChange (fun (v : int) -> onChange v)
        ]
      ]
    ]

  /// Renders a labeled checkbox
  let private checkboxField (label : string) isChecked onChange =
    Html.div [
      prop.className "mb-3 flex items-center"
      prop.children [
        Html.input [
          prop.type' "checkbox"
          prop.isChecked isChecked
          prop.className "mr-2 accent-blue"
          prop.onChange (fun (v : bool) -> onChange v)
        ]
        Html.label [ prop.className "text-sm text-base04"; prop.text label ]
      ]
    ]

  /// Renders the settings panel with actual controls
  let Render (state : State) (dispatch : Msg -> unit) =
    let settings =
      state.Settings
      |> Option.defaultValue {
        General = {
          Theme = "auto"
          Language = "en"
          AutoSave = true
          AutoSaveInterval = 30
        }
        Editor = {
          FontFamily = "monospace"
          FontSize = 14
          LineHeight = 1.6
          TabSize = 2
          VimMode = false
          SpellCheck = true
        }
      }

    let updateGeneral (updater : GeneralSettings -> GeneralSettings) =
      let updated = { settings with General = updater settings.General }
      dispatch (SettingsChanged updated)

    let updateEditor (updater : EditorSettings -> EditorSettings) =
      let updated = { settings with Editor = updater settings.Editor }
      dispatch (SettingsChanged updated)

    Html.div [
      prop.className "flex-1 flex flex-col bg-base00 p-6 overflow-y-auto"
      prop.children [
        Html.h1 [ prop.className "text-2xl font-bold text-base05 mb-6"; prop.text "Settings" ]

        section "Workspace" [
          match state.Workspace with
          | Some ws ->
            Html.div [
              prop.children [
                Html.div [
                  prop.className "text-sm text-base03 mb-1"
                  prop.text $"Name: {ws.Workspace.Name}"
                ]
                Html.div [
                  prop.className "text-sm text-base03 mb-1"
                  prop.text $"Path: {ws.Workspace.RootPath}"
                ]
                Html.div [ prop.className "text-sm text-base03"; prop.text $"Notes: {ws.NoteCount}" ]
              ]
            ]
          | None -> Html.div [ prop.className "text-sm text-base03"; prop.text "No workspace open" ]
        ]

        section "General" [
          selectField
            "Theme"
            settings.General.Theme
            [ "auto", "Auto (System)"; "light", "Light"; "dark", "Dark" ]
            (fun theme -> updateGeneral (fun g -> { g with Theme = theme }))

          selectField
            "Language"
            settings.General.Language
            [ "en", "English"; "es", "Español"; "fr", "Français"; "de", "Deutsch" ]
            (fun lang -> updateGeneral (fun g -> { g with Language = lang }))

          checkboxField "Auto Save" settings.General.AutoSave (fun enabled ->
            updateGeneral (fun g -> { g with AutoSave = enabled }))

          numberField "Auto Save Interval (seconds)" settings.General.AutoSaveInterval 5 120 (fun interval ->
            updateGeneral (fun g -> { g with AutoSaveInterval = interval }))
        ]

        section "Editor" [
          selectField
            "Font Family"
            settings.Editor.FontFamily
            [
              "monospace", "Monospace"
              "JetBrains Mono", "JetBrains Mono"
              "Fira Code", "Fira Code"
              "Consolas", "Consolas"
              "Monaco", "Monaco"
            ]
            (fun font -> updateEditor (fun e -> { e with FontFamily = font }))

          numberField "Font Size" settings.Editor.FontSize 10 24 (fun size ->
            updateEditor (fun e -> { e with FontSize = size }))

          numberField "Line Height" (int (settings.Editor.LineHeight * 10.0)) 10 30 (fun height ->
            updateEditor (fun e -> { e with LineHeight = float height / 10.0 }))

          numberField "Tab Size" settings.Editor.TabSize 2 8 (fun size ->
            updateEditor (fun e -> { e with TabSize = size }))

          checkboxField "Vim Mode" settings.Editor.VimMode (fun enabled ->
            updateEditor (fun e -> { e with VimMode = enabled }))

          checkboxField "Spell Check" settings.Editor.SpellCheck (fun enabled ->
            updateEditor (fun e -> { e with SpellCheck = enabled }))
        ]
      ]
    ]

module Notification =
  module Error =
    /// Renders error notification if present
    let Render (error : string option) (dispatch : Msg -> unit) =
      match error with
      | Some err ->
        Html.div [
          prop.className "fixed top-4 right-4 bg-red text-white px-4 py-3 rounded shadow-lg"
          prop.children [
            Html.span [ prop.text err ]
            Html.button [
              prop.className "ml-4 font-bold"
              prop.text "×"
              prop.onClick (fun _ -> dispatch ClearError)
            ]
          ]
        ]
      | None -> Html.none

  module Success =
    /// Renders success notification if present
    let Render (message : string option) (dispatch : Msg -> unit) =
      match message with
      | Some msg ->
        Html.div [
          prop.className "fixed top-4 left-4 bg-green text-base00 px-4 py-3 rounded shadow-lg"
          prop.children [
            Html.span [ prop.text msg ]
            Html.button [
              prop.className "ml-4 font-bold"
              prop.text "×"
              prop.onClick (fun _ -> dispatch ClearSuccess)
            ]
          ]
        ]
      | None -> Html.none

module NavigationBar =
  /// Renders the top navigation bar
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "h-12 bg-base01 border-b border-base02 flex items-center px-4 gap-2 shrink-0"
      prop.children [
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text "Notes"
          prop.onClick (fun _ -> dispatch (NavigateTo NoteList))
        ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text "Graph"
          prop.onClick (fun _ -> dispatch (NavigateTo GraphViewRoute))
        ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text "Settings"
          prop.onClick (fun _ -> dispatch (NavigateTo Settings))
        ]
        Html.div [ prop.className "flex-1" ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text (
            if state.VisiblePanels.Contains Backlinks then
              "Hide Backlinks"
            else
              "Show Backlinks"
          )
          prop.onClick (fun _ -> dispatch (TogglePanel Backlinks))
        ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text (
            if state.VisiblePanels.Contains TasksPanel then
              "Hide Tasks"
            else
              "Show Tasks"
          )
          prop.onClick (fun _ -> dispatch (TogglePanel TasksPanel))
        ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text (
            if state.VisiblePanels.Contains TagsPanel then
              "Hide Tags"
            else
              "Show Tags"
          )
          prop.onClick (fun _ -> dispatch (TogglePanel TagsPanel))
        ]
        Html.button [
          prop.className "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
          prop.text (
            if state.VisiblePanels.Contains SearchPanel then
              "Hide Search"
            else
              "Show Search"
          )
          prop.onClick (fun _ -> dispatch (TogglePanel SearchPanel))
        ]
      ]
    ]

/// Main application content
module App =
  /// Renders the main content area based on current route
  let private mainContent (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "flex-1 flex flex-col h-full overflow-hidden default-transition"
      prop.children [
        match state.CurrentRoute with
        | WorkspacePicker -> WorkspacePicker.Render state dispatch
        | NoteList ->
          Html.div [
            prop.className "flex-1 flex items-center justify-center bg-base00"
            prop.children [
              Html.div [
                prop.className "text-center"
                prop.children [
                  Html.h2 [
                    prop.className "text-xl font-semibold text-base03"
                    prop.text "Select a note to begin"
                  ]
                ]
              ]
            ]
          ]
        | NoteEditor _ ->
          match state.CurrentNote with
          | Some note -> NoteEditor.Render note state dispatch
          | None ->
            Html.div [
              prop.className "flex-1 flex items-center justify-center bg-base00 text-base05"
              prop.text "Loading..."
            ]
        | GraphViewRoute -> GraphView.render state dispatch
        | Settings -> SettingsPanel.Render state dispatch
      ]
    ]

  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "h-screen w-full bg-base00 flex flex-col overflow-hidden"
      prop.children [
        if state.Workspace.IsSome && state.CurrentRoute <> WorkspacePicker then
          Html.div [
            prop.key "navigation-bar"
            prop.children [ NavigationBar.Render state dispatch ]
          ]

        Html.div [
          prop.key "main-content-container"
          prop.className "flex-1 flex overflow-hidden min-h-0"
          prop.children [
            if state.Workspace.IsSome && state.CurrentRoute <> WorkspacePicker then
              Html.div [
                prop.key "notes-list"
                prop.className "default-transition"
                prop.children [ Sidebar.NoteList.Render state dispatch ]
              ]

            mainContent state dispatch

            if
              state.VisiblePanels.Contains Backlinks
              && state.CurrentRoute <> WorkspacePicker
              && state.CurrentRoute <> Settings
            then
              Html.div [
                prop.key "backlinks-panel"
                prop.className "default-transition"
                prop.children [ BacklinksPanel.Render state dispatch ]
              ]

            if
              state.VisiblePanels.Contains TasksPanel
              && state.CurrentRoute <> WorkspacePicker
              && state.CurrentRoute <> Settings
            then
              Html.div [
                prop.key "tasks-panel"
                prop.className "default-transition"
                prop.children [ TaskPanel.Render state dispatch ]
              ]

            if
              state.VisiblePanels.Contains TagsPanel
              && state.CurrentRoute <> WorkspacePicker
              && state.CurrentRoute <> Settings
            then
              Html.div [
                prop.key "tags-panel"
                prop.className "default-transition"
                prop.children [ TagsPanel.Render state dispatch ]
              ]

            if
              state.VisiblePanels.Contains SearchPanel
              && state.CurrentRoute <> WorkspacePicker
              && state.CurrentRoute <> Settings
            then
              Html.div [
                prop.key "search-panel"
                prop.className "default-transition"
                prop.children [ SearchPanel.Render state dispatch ]
              ]
          ]
        ]

        Html.div [
          prop.key "notification-stack"
          prop.children [
            Notification.Error.Render state.Error dispatch
            Notification.Success.Render state.Success dispatch
          ]
        ]

        if state.Loading then
          Html.div [
            prop.key "loading-overlay"
            prop.className "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center default-transition"
            prop.children [
              Html.div [
                prop.className "bg-base01 text-base05 p-6 rounded-lg shadow-xl"
                prop.text "Loading..."
              ]
            ]
          ]

        Html.footer [
          prop.className "flex items-center justify-center text-base04 text-xs gap-1 p-4"
          prop.children [
            Html.span [ prop.text "Made with ⚡️ in Austin, TX by" ]
            // TODO: open in browser
            Html.a [
              prop.className "hover:text-base0A"
              prop.href "https://desertthunder.dev"
              prop.target "_blank"
              prop.text "Owais"
            ]
          ]
        ]
      ]
    ]

/// Main application view with router
let Render (state : State) (dispatch : Msg -> unit) =
  React.router [
    router.onUrlChanged (UrlChanged >> dispatch)
    router.children [ App.Render state dispatch ]
  ]
