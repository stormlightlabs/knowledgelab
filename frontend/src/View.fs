module View

open System
open Feliz
open Feliz.Router
open Model
open Domain
open StatusBar

/// Renders a recent file item in the workspace picker
let recentFileItem (noteId : string) (dispatch : Msg -> unit) =
  Html.div [
    prop.key noteId
    prop.className
      "p-3 hover:bg-base02 cursor-pointer border-b border-base02 default-transition rounded"
    prop.onClick (fun _ -> dispatch (SelectNote noteId))
    prop.children [
      Html.div [ prop.className "font-medium text-base05"; prop.text noteId ]
      Html.div [ prop.className "text-xs text-base03 mt-1"; prop.text "Recent file" ]
    ]
  ]

/// Renders the workspace picker screen when no workspace is open
let workspacePicker (state : State) (dispatch : Msg -> unit) =
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
                prop.text
                  "Build your personal knowledge graph with markdown, wikilinks, and powerful search"
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
                prop.text
                  "Open a folder to use as your notes workspace. All your notes will be stored locally as markdown files."
              ]
              Html.button [
                prop.className
                  "w-full bg-blue hover:bg-blue-bright text-base00 font-bold py-3 px-6 rounded default-transition shadow-md hover:shadow-lg"
                prop.text "Open Folder"
                prop.onClick (fun _ -> dispatch SelectWorkspaceFolder)
              ]
            ]
          ]

          match state.WorkspaceSnapshot with
          | Some snapshot ->
            let recentPages =
              snapshot.UI.RecentPages |> List.filter (String.IsNullOrWhiteSpace >> not)

            if List.isEmpty recentPages then
              Html.none
            else
              Html.div [
                prop.className "mt-6 bg-base01 p-6 rounded-lg shadow-xl border border-base02"
                prop.children [
                  Html.h3 [
                    prop.className "text-lg font-semibold mb-4 text-base05"
                    prop.text "Recent Files"
                  ]
                  Html.div [
                    prop.className "space-y-2 max-h-60 overflow-y-auto"
                    prop.children (
                      recentPages
                      |> List.truncate 10
                      |> List.map (fun noteId -> recentFileItem noteId dispatch)
                    )
                  ]
                ]
              ]
          | None -> Html.none
        ]
      ]
    ]
  ]

/// Renders a note list item
let noteListItem (note : NoteSummary) (dispatch : Msg -> unit) =
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

/// Renders the notes list sidebar
let notesList (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "w-64 bg-base01 border-r border-base02 flex flex-col h-full"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02 shrink-0"
        prop.children [
          Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Notes" ]
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
        prop.children (state.Notes |> List.map (fun note -> noteListItem note dispatch))
      ]
    ]
  ]

/// Renders a toolbar button with hover state and optional active state
let toolbarButton (label : string) (title : string) (isActive : bool) (onClick : unit -> unit) =
  Html.button [
    prop.className (
      "px-3 py-1.5 rounded text-sm font-medium transition-all "
      + if isActive then
          "bg-blue text-base00"
        else
          "text-base05 hover:bg-base02"
    )
    prop.title title
    prop.text label
    prop.onClick (fun _ -> onClick ())
  ]

/// Renders the editor toolbar with formatting buttons and preview toggles
let editorToolbar (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "flex items-center gap-2 px-4 py-2 bg-base01 border-b border-base02"
    prop.children [
      Html.div [
        prop.className "flex items-center gap-1"
        prop.children [
          toolbarButton "B" "Bold (Ctrl/Cmd+B)" false (fun () -> dispatch FormatBold)
          toolbarButton "I" "Italic (Ctrl/Cmd+I)" false (fun () -> dispatch FormatItalic)
          toolbarButton "</>" "Code (Ctrl/Cmd+E)" false (fun () -> dispatch FormatInlineCode)

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
        ]
      ]

      Html.div [ prop.className "flex-1" ]

      Html.div [
        prop.className "flex items-center gap-1"
        prop.children [
          toolbarButton
            "Edit"
            "Edit Only Mode"
            (state.EditorState.PreviewMode = EditOnly)
            (fun () -> dispatch (SetPreviewMode EditOnly))
          toolbarButton
            "Preview"
            "Preview Only Mode"
            (state.EditorState.PreviewMode = PreviewOnly)
            (fun () -> dispatch (SetPreviewMode PreviewOnly))
          toolbarButton
            "Split"
            "Split View Mode (Ctrl/Cmd+Shift+P)"
            (state.EditorState.PreviewMode = SplitView)
            (fun () -> dispatch (SetPreviewMode SplitView))
        ]
      ]
    ]
  ]

/// Renders the markdown preview panel
let previewPanel (html : string option) =
  Html.div [
    prop.className "flex-1 p-6 overflow-y-auto prose prose-invert max-w-none"
    prop.children [
      match html with
      | Some content ->
        Html.div [ prop.className "rendered-markdown"; prop.dangerouslySetInnerHTML content ]
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
let noteEditor (note : Note) (state : State) (dispatch : Msg -> unit) =
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

      editorToolbar state dispatch

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
      | PreviewOnly -> previewPanel state.EditorState.RenderedPreview
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
            previewPanel state.EditorState.RenderedPreview
          ]
        ]

      statusBar note state.EditorState.CursorPosition state.EditorState.IsDirty
    ]
  ]

/// Renders a backlink item
let backlinkItem (link : Link) (dispatch : Msg -> unit) =
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
let backlinksPanel (state : State) (dispatch : Msg -> unit) =
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

/// Renders the settings panel
let settingsPanel (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "flex-1 flex flex-col bg-base00 p-6"
    prop.children [
      Html.h1 [ prop.className "text-2xl font-bold text-base05 mb-4"; prop.text "Settings" ]
      Html.div [
        prop.className "bg-base01 p-4 rounded border border-base02 mb-4"
        prop.children [
          Html.h3 [ prop.className "font-semibold text-base05 mb-2"; prop.text "Workspace" ]
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
                Html.div [
                  prop.className "text-sm text-base03 mb-1"
                  prop.text $"Notes: {ws.NoteCount}"
                ]
              ]
            ]
          | None ->
            Html.div [ prop.className "text-sm text-base03"; prop.text "No workspace open" ]
        ]
      ]
      Html.div [
        prop.className "bg-base01 p-4 rounded border border-base02"
        prop.children [
          Html.h3 [ prop.className "font-semibold text-base05 mb-2"; prop.text "Theme" ]
          Html.div [ prop.className "text-sm text-base03"; prop.text "Iceberg Dark (active)" ]
        ]
      ]
    ]
  ]

/// Renders the main content area based on current route
let mainContent (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "flex-1 flex flex-col h-full overflow-hidden default-transition"
    prop.children [
      match state.CurrentRoute with
      | WorkspacePicker -> workspacePicker state dispatch
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
        | Some note -> noteEditor note state dispatch
        | None ->
          Html.div [
            prop.className "flex-1 flex items-center justify-center bg-base00 text-base05"
            prop.text "Loading..."
          ]
      | GraphViewRoute -> GraphView.render state dispatch
      | Settings -> settingsPanel state dispatch
    ]
  ]

/// Renders error notification if present
let errorNotification (error : string option) (dispatch : Msg -> unit) =
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

/// Renders the top navigation bar
let navigationBar (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "h-12 bg-base01 border-b border-base02 flex items-center px-4 gap-2 shrink-0"
    prop.children [
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
        prop.text "Notes"
        prop.onClick (fun _ -> dispatch (NavigateTo NoteList))
      ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
        prop.text "Graph"
        prop.onClick (fun _ -> dispatch (NavigateTo GraphViewRoute))
      ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
        prop.text "Settings"
        prop.onClick (fun _ -> dispatch (NavigateTo Settings))
      ]
      Html.div [ prop.className "flex-1" ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 default-transition"
        prop.text (
          if state.VisiblePanels.Contains Backlinks then
            "Hide Backlinks"
          else
            "Show Backlinks"
        )
        prop.onClick (fun _ -> dispatch (TogglePanel Backlinks))
      ]
    ]
  ]

/// Main application content
module App =
  let Render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      prop.className "h-screen w-full bg-base00 flex flex-col overflow-hidden"
      prop.children [
        if state.Workspace.IsSome && state.CurrentRoute <> WorkspacePicker then
          Html.div [ prop.key "navigation-bar"; prop.children [ navigationBar state dispatch ] ]

        Html.div [
          prop.key "main-content-container"
          prop.className "flex-1 flex overflow-hidden min-h-0"
          prop.children [
            if state.Workspace.IsSome && state.CurrentRoute <> WorkspacePicker then
              Html.div [
                prop.key "notes-list"
                prop.className "default-transition"
                prop.children [ notesList state dispatch ]
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
                prop.children [ backlinksPanel state dispatch ]
              ]
          ]
        ]

        Html.div [
          prop.key "error-notification"
          prop.children [ errorNotification state.Error dispatch ]
        ]

        if state.Loading then
          Html.div [
            prop.key "loading-overlay"
            prop.className
              "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center default-transition"
            prop.children [
              Html.div [
                prop.className "bg-base01 text-base05 p-6 rounded-lg shadow-xl"
                prop.text "Loading..."
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
