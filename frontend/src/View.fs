module View

open Feliz
open Model
open Domain

/// Renders the vault picker screen when no workspace is open
let vaultPicker (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "flex items-center justify-center h-screen bg-base00"
    prop.children [
      Html.div [
        prop.className "bg-base01 p-8 rounded-lg shadow-lg max-w-md w-full"
        prop.children [
          Html.h1 [
            prop.className "text-2xl font-bold mb-4 text-base05"
            prop.text "Open Workspace"
          ]
          Html.p [
            prop.className "text-base03 mb-4"
            prop.text "Select a folder to open as your notes workspace"
          ]
          Html.button [
            prop.className
              "w-full bg-blue hover:bg-blue-bright text-base00 font-bold py-2 px-4 rounded transition-all"
            prop.text "Choose Folder"
            // TODO: Open folder picker dialog
            prop.onClick (fun _ -> dispatch (OpenVault "./test-workspace"))
          ]
        ]
      ]
    ]
  ]

/// Renders a note list item
let noteListItem (note : NoteSummary) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "p-3 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
    prop.onClick (fun _ -> dispatch (SelectNote note.Id))
    prop.children [
      Html.div [ prop.className "font-semibold text-base05"; prop.text note.Title ]
      Html.div [ prop.className "text-sm text-base03"; prop.text note.Path ]
      Html.div [
        prop.className "flex gap-2 mt-1"
        prop.children [
          for tag in note.Tags do
            Html.span [
              prop.className "text-xs bg-blue text-base00 px-2 py-1 rounded"
              prop.text $"#{tag.Name}"
            ]
        ]
      ]
    ]
  ]

/// Renders the notes list sidebar
let notesList (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "w-64 bg-base01 border-r border-base02 flex flex-col"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Notes" ]
          Html.button [
            prop.className
              "mt-2 w-full bg-blue hover:bg-blue-bright text-base00 text-sm font-bold py-1 px-2 rounded transition-all"
            prop.text "New Note"
            prop.onClick (fun _ -> dispatch (CreateNote("Untitled", "")))
          ]
          Html.button [
            prop.className
              "mt-2 w-full bg-green hover:bg-green-bright text-base00 text-sm font-bold py-1 px-2 rounded transition-all"
            prop.text "Today's Note"
            prop.onClick (fun _ -> dispatch OpenDailyNote)
          ]
        ]
      ]
      Html.div [
        prop.className "flex-1 overflow-y-auto"
        prop.children [
          for note in state.Notes do
            noteListItem note dispatch
        ]
      ]
    ]
  ]

/// Renders the note editor
let noteEditor (note : Note) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "flex-1 flex flex-col bg-base00"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text note.Title ]
          Html.div [ prop.className "text-sm text-base03 mt-1"; prop.text note.Path ]
        ]
      ]
      Html.div [
        prop.className "flex-1 p-6 overflow-y-auto"
        prop.children [
          Html.textarea [
            prop.className
              "w-full h-full font-mono text-sm bg-base00 text-base05 border border-base02 rounded p-4 focus:outline-none focus:border-blue resize-none"
            prop.value note.Content
            prop.placeholder "Start writing..."
            prop.onChange (fun (value : string) -> dispatch (UpdateNoteContent value))
          ]
        ]
      ]
    ]
  ]

/// Renders a backlink item
let backlinkItem (link : Link) (dispatch : Msg -> unit) =
  Html.div [
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
    prop.className "w-64 bg-base01 border-l border-base02 flex flex-col"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h2 [ prop.className "font-bold text-lg text-base05"; prop.text "Backlinks" ]
          Html.div [
            prop.className "text-xs text-base03 mt-1"
            prop.text $"{state.Backlinks.Length} links"
          ]
        ]
      ]
      Html.div [
        prop.className "flex-1 overflow-y-auto"
        prop.children [
          if state.Backlinks.IsEmpty then
            Html.div [
              prop.className "p-4 text-center text-base03 text-sm"
              prop.text "No backlinks found"
            ]
          else
            for link in state.Backlinks do
              backlinkItem link dispatch
        ]
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
  match state.CurrentRoute with
  | VaultPicker -> vaultPicker state dispatch
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
    | Some note -> noteEditor note dispatch
    | None ->
      Html.div [
        prop.className "flex-1 flex items-center justify-center bg-base00 text-base05"
        prop.text "Loading..."
      ]
  | GraphView ->
    Html.div [
      prop.className "flex-1 flex items-center justify-center bg-base00 text-base05"
      prop.text "Graph view (coming soon)"
    ]
  | Settings -> settingsPanel state dispatch

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
    prop.className "h-12 bg-base01 border-b border-base02 flex items-center px-4 gap-2"
    prop.children [
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 transition-all"
        prop.text "Notes"
        prop.onClick (fun _ -> dispatch (NavigateTo NoteList))
      ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 transition-all"
        prop.text "Graph"
        prop.onClick (fun _ -> dispatch (NavigateTo GraphView))
      ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 transition-all"
        prop.text "Settings"
        prop.onClick (fun _ -> dispatch (NavigateTo Settings))
      ]
      Html.div [ prop.className "flex-1" ]
      Html.button [
        prop.className
          "px-3 py-1 rounded text-sm font-medium text-base05 hover:bg-base02 transition-all"
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

/// Main application view
let render (state : State) (dispatch : Msg -> unit) =
  Html.div [
    prop.className "h-screen w-full bg-base00 flex flex-col overflow-hidden"
    prop.children [
      if state.Workspace.IsSome && state.CurrentRoute <> VaultPicker then
        navigationBar state dispatch

      Html.div [
        prop.className "flex-1 flex overflow-hidden"
        prop.children [
          if state.Workspace.IsSome && state.CurrentRoute <> VaultPicker then
            notesList state dispatch

          mainContent state dispatch

          if
            state.VisiblePanels.Contains Backlinks
            && state.CurrentRoute <> VaultPicker
            && state.CurrentRoute <> Settings
          then
            backlinksPanel state dispatch
        ]
      ]

      errorNotification state.Error dispatch

      if state.Loading then
        Html.div [
          prop.className "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center"
          prop.children [
            Html.div [
              prop.className "bg-base01 text-base05 p-6 rounded-lg shadow-xl"
              prop.text "Loading..."
            ]
          ]
        ]
    ]
  ]
