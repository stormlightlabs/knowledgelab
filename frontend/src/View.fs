module View

open System
open Feliz
open Feliz.Router
open Model
open Domain
open Panels
open Editor

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
      prop.className "flex items-center justify-center min-h-screen bg-base00 w-full p-4"
      prop.children [
        Html.div [
          prop.className "text-center max-w-2xl w-full"
          prop.children [
            Html.div [
              prop.className "mb-6 md:mb-8"
              prop.children [
                Html.h1 [
                  prop.className "text-3xl md:text-5xl font-bold mb-2 md:mb-4 text-base05"
                  prop.text "Knowledge Lab"
                ]
                Html.p [
                  prop.className "text-base md:text-lg text-base04 mb-1 md:mb-2"
                  prop.text "Your local-first, graph-based knowledge workspace"
                ]
                Html.p [
                  prop.className "text-xs md:text-sm text-base03"
                  prop.text "Build your personal knowledge graph with markdown, wikilinks, and powerful search"
                ]
              ]
            ]
            Html.div [
              prop.className "bg-base01 p-4 md:p-8 rounded-lg shadow-xl border border-base02"
              prop.children [
                Html.h2 [
                  prop.className "text-lg md:text-xl font-semibold mb-3 md:mb-4 text-base05"
                  prop.text "Get Started"
                ]
                Html.p [
                  prop.className "text-sm md:text-base text-base03 mb-4 md:mb-6"
                  prop.text "Open a folder to use as your notes workspace."
                ]
                Html.p [
                  prop.className "text-sm md:text-base text-base03 mb-4 md:mb-6"
                  prop.text "All your notes will be stored locally as markdown files."
                ]
                Html.div [
                  prop.className "w-full grid gap-3 md:gap-4 grid-cols-1 sm:grid-cols-2"
                  prop.children [
                    Html.button [
                      prop.className
                        "bg-blue hover:bg-blue-bright text-base00 font-bold py-3 px-4 md:px-6 rounded default-transition shadow-md hover:shadow-lg text-sm md:text-base"
                      prop.text "Open Folder"
                      prop.onClick (fun _ -> dispatch SelectWorkspaceFolder)
                    ]
                    Html.button [
                      prop.className
                        "bg-blue hover:bg-blue-bright text-base00 font-bold py-3 px-4 md:px-6 rounded default-transition shadow-md hover:shadow-lg text-sm md:text-base"
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

              if List.isEmpty recentPages then
                Html.none
              else
                Html.div [
                  prop.className "mt-4 md:mt-6 bg-base01 p-4 md:p-6 rounded-lg shadow-xl border border-base02"
                  prop.children [
                    Html.div [
                      prop.className "flex items-center justify-between mb-3 md:mb-4"
                      prop.children [
                        Html.h3 [
                          prop.className "text-base md:text-lg font-semibold text-base05"
                          prop.text "Recent Files"
                        ]
                        Html.button [
                          prop.className
                            "text-xs text-base03 hover:text-base05 default-transition underline-offset-2 hover:underline"
                          prop.text "Clear"
                          prop.onClick (fun _ -> dispatch ClearRecentFiles)
                        ]
                      ]
                    ]
                    Html.div [
                      prop.className "space-y-2 max-h-48 md:max-h-60 overflow-y-auto"
                      prop.children (
                        recentPages
                        |> List.truncate 10
                        |> List.map (fun noteId -> recentFileItem "" noteId dispatch)
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
    /// Formats a DateTime as a relative time string (e.g., "2 days ago", "1 week ago")
    let private formatRelativeTime (date : DateTime) : string =
      let now = DateTime.UtcNow
      let diff = now - date

      if diff.TotalMinutes < 1.0 then
        "just now"
      elif diff.TotalMinutes < 60.0 then
        $"{int diff.TotalMinutes}m ago"
      elif diff.TotalHours < 24.0 then
        $"{int diff.TotalHours}h ago"
      elif diff.TotalDays < 7.0 then
        $"{int diff.TotalDays}d ago"
      elif diff.TotalDays < 30.0 then
        $"{int (diff.TotalDays / 7.0)}w ago"
      elif diff.TotalDays < 365.0 then
        $"{int (diff.TotalDays / 30.0)}mo ago"
      else
        $"{int (diff.TotalDays / 365.0)}y ago"

    /// Renders a note list item
    let private noteListItem (note : NoteSummary) (dispatch : Msg -> unit) =
      Html.div [
        prop.key $"{note.id}"
        prop.className "p-3 hover:bg-base02 cursor-pointer border-b border-base02 transition-all"
        prop.onClick (fun _ -> dispatch (SelectNote note.id))
        prop.children [
          Html.div [ prop.className "font-semibold text-base05 truncate"; prop.text note.title ]
          Html.div [ prop.className "text-sm text-base03 truncate"; prop.text note.path ]
          Html.div [
            prop.className "text-xs text-base04 mt-1 truncate"
            prop.text $"Modified {formatRelativeTime note.modifiedAt} • Created {formatRelativeTime note.createdAt}"
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

      let sortByText =
        match state.NotesSortBy with
        | Title -> "Title"
        | ModifiedDate -> "Modified"
        | CreatedDate -> "Created"

      let sortOrderIcon =
        match state.NotesSortOrder with
        | Ascending -> "↑"
        | Descending -> "↓"

      Html.div [
        prop.className "w-full bg-base01 border-r border-base02 flex flex-col h-full"
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

              Html.div [
                prop.className "flex gap-2 mb-2"
                prop.children [
                  Html.div [
                    prop.className "flex-1"
                    prop.children [
                      Html.select [
                        prop.className
                          "w-full bg-base02 text-base05 text-sm px-2 py-1 rounded border border-base03 cursor-pointer"
                        prop.value sortByText
                        prop.onChange (fun (value : string) ->
                          let sortBy =
                            match value with
                            | "Title" -> Title
                            | "Created" -> CreatedDate
                            | _ -> ModifiedDate

                          dispatch (SetNotesSortBy sortBy))
                        prop.children [
                          Html.option [ prop.text "Title"; prop.value "Title" ]
                          Html.option [ prop.text "Modified"; prop.value "Modified" ]
                          Html.option [ prop.text "Created"; prop.value "Created" ]
                        ]
                      ]
                    ]
                  ]
                  Html.button [
                    prop.className
                      "bg-base02 hover:bg-base03 text-base05 text-sm px-3 py-1 rounded border border-base03 default-transition"
                    prop.text sortOrderIcon
                    prop.onClick (fun _ -> dispatch ToggleNotesSortOrder)
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
              if List.isEmpty state.Notes then
                [
                  Html.div [
                    prop.className "p-8 text-center"
                    prop.children [
                      Html.div [ prop.className "text-base04 text-sm mb-2"; prop.text "No notes yet" ]
                      Html.div [
                        prop.className "text-base03 text-xs"
                        prop.text "Create your first note to get started"
                      ]
                    ]
                  ]
                ]
              elif List.isEmpty filteredNotes then
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
      prop.className
        "h-12 bg-base01 border-b border-base02 flex items-center px-2 md:px-4 gap-1 md:gap-2 shrink-0 overflow-x-auto"
      prop.children [
        Html.button [
          prop.className
            "px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
          prop.text "Notes"
          prop.onClick (fun _ -> dispatch (NavigateTo NoteList))
        ]
        Html.button [
          prop.className
            "px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
          prop.text "Graph"
          prop.onClick (fun _ -> dispatch (NavigateTo GraphViewRoute))
        ]
        Html.button [
          prop.className
            "px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
          prop.text "Settings"
          prop.onClick (fun _ -> dispatch (NavigateTo Settings))
        ]
        Html.div [ prop.className "flex-1 min-w-0" ]
        Html.button [
          prop.className
            "px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-red border border-base03 hover:border-red default-transition whitespace-nowrap"
          prop.text "Close Workspace"
          prop.onClick (fun _ -> dispatch CloseWorkspace)
        ]
        Html.div [
          prop.className "flex items-center gap-1 md:gap-2 shrink-0"
          prop.children [
            Html.button [
              prop.className
                "px-2 py-1 rounded text-xs font-semibold text-base05 border border-base03 hover:border-base05 transition-colors whitespace-nowrap"
              prop.children [
                Html.span [
                  prop.className "hidden lg:inline"
                  prop.text (
                    if state.UIState.IsSidebarCollapsed then
                      "Show Sidebar"
                    else
                      "Hide Sidebar"
                  )
                ]
                Html.span [
                  prop.className "inline lg:hidden"
                  prop.text (if state.UIState.IsSidebarCollapsed then "☰" else "✕")
                ]
              ]
              prop.onClick (fun _ -> dispatch ToggleSidebarCollapsed)
            ]

            Html.button [
              prop.className
                "hidden sm:block px-2 py-1 rounded text-xs font-semibold text-base05 border border-base03 hover:border-base05 transition-colors whitespace-nowrap"
              prop.children [
                Html.span [
                  prop.className "hidden lg:inline"
                  prop.text (
                    if state.UIState.AreRightPanelsCollapsed then
                      "Show Panels"
                    else
                      "Hide Panels"
                  )
                ]
                Html.span [
                  prop.className "inline lg:hidden"
                  prop.text (if state.UIState.AreRightPanelsCollapsed then "≡" else "✕")
                ]
              ]
              prop.onClick (fun _ -> dispatch ToggleRightPanelsCollapsed)
            ]

            Html.button [
              prop.className
                "hidden lg:block px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
              prop.text (
                if state.VisiblePanels.Contains Backlinks then
                  "Hide Backlinks"
                else
                  "Show Backlinks"
              )
              prop.onClick (fun _ -> dispatch (TogglePanel Backlinks))
            ]
            Html.button [
              prop.className
                "hidden lg:block px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
              prop.text (
                if state.VisiblePanels.Contains TasksPanel then
                  "Hide Tasks"
                else
                  "Show Tasks"
              )
              prop.onClick (fun _ -> dispatch (TogglePanel TasksPanel))
            ]
            Html.button [
              prop.className
                "hidden lg:block px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
              prop.text (
                if state.VisiblePanels.Contains TagsPanel then
                  "Hide Tags"
                else
                  "Show Tags"
              )
              prop.onClick (fun _ -> dispatch (TogglePanel TagsPanel))
            ]
            Html.button [
              prop.className
                "hidden lg:block px-2 md:px-3 py-1 rounded text-xs md:text-sm font-medium text-base05 hover:bg-base02 default-transition whitespace-nowrap"
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
    let showWorkspaceChrome =
      state.Workspace.IsSome && state.CurrentRoute <> WorkspacePicker

    let showAuxPanels = showWorkspaceChrome && state.CurrentRoute <> Settings

    let leftSidebar =
      if showWorkspaceChrome then
        let collapsed = state.UIState.IsSidebarCollapsed
        let sidebarWidth = if collapsed then 0.0 else float state.UIState.SidebarWidth

        Html.div [
          prop.key "notes-list"
          prop.className (
            "relative shrink-0 h-full transition-all duration-300 ease-in-out"
            + if not collapsed then " sm:w-60 lg:w-72" else " w-0"
          )
          prop.style [
            if not collapsed then
              style.width (length.px sidebarWidth)
          ]
          prop.children [
            Html.div [
              prop.className (
                "h-full overflow-hidden default-transition"
                + if collapsed then
                    " opacity-0 pointer-events-none -translate-x-2"
                  else
                    " opacity-100 translate-x-0"
              )
              prop.children [ Sidebar.NoteList.Render state dispatch ]
            ]
          ]
        ]
      else
        Html.none

    let availablePanels =
      if showAuxPanels then
        panelOrder |> List.filter (fun panel -> state.VisiblePanels.Contains panel)
      else
        []

    let rightSidebar =
      match availablePanels with
      | [] -> Html.none
      | panels ->
        let collapsed = state.UIState.AreRightPanelsCollapsed

        let panelWidth =
          if collapsed then
            0.0
          else
            float state.UIState.RightPanelWidth

        let activePanel =
          match state.UIState.ActivePanel with
          | Some panel when panels |> List.contains panel -> panel
          | _ -> panels.Head

        let panelLabel panel =
          match panel with
          | Backlinks -> "Backlinks"
          | TasksPanel -> "Tasks"
          | TagsPanel -> "Tags"
          | SearchPanel -> "Search"

        let panelContent =
          match activePanel with
          | Backlinks -> BacklinksPanel.Render state dispatch
          | TasksPanel -> TaskPanel.Render state dispatch
          | TagsPanel -> TagsPanel.Render state dispatch
          | SearchPanel -> SearchPanel.Render state dispatch

        Html.div [
          prop.key "right-panels-wrapper"
          prop.className (
            "relative shrink-0 h-full transition-all duration-300 ease-in-out"
            + if not collapsed then " sm:w-60 lg:w-80" else " w-0"
          )
          prop.style [
            if not collapsed then
              style.width (length.px panelWidth)
          ]
          prop.children [
            Html.div [
              prop.className (
                "h-full flex flex-col bg-base01 border-l border-base02 overflow-hidden default-transition"
                + if collapsed then
                    " opacity-0 pointer-events-none translate-x-2"
                  else
                    " opacity-100 translate-x-0"
              )
              prop.children [
                Html.div [
                  prop.className "flex items-center border-b border-base02 overflow-x-auto"
                  prop.children (
                    panels
                    |> List.map (fun panel ->
                      let isActive = panel = activePanel

                      Html.button [
                        prop.key (panelLabel panel)
                        prop.className (
                          "px-2 md:px-3 py-2 text-xs font-semibold uppercase tracking-wide transition-colors border-b-2 whitespace-nowrap"
                          + if isActive then
                              " border-blue text-base05"
                            else
                              " border-transparent text-base04 hover:text-base05"
                        )
                        prop.onClick (fun _ -> dispatch (SetActivePanel panel))
                        prop.text (panelLabel panel)
                      ])
                  )
                ]
                Html.div [
                  prop.className "flex-1 flex flex-col min-h-0"
                  prop.children [ panelContent ]
                ]
              ]
            ]
          ]
        ]

    Html.div [
      prop.className "h-screen w-full bg-base00 flex flex-col overflow-hidden"
      prop.children [
        if showWorkspaceChrome then
          Html.div [
            prop.key "navigation-bar"
            prop.children [ NavigationBar.Render state dispatch ]
          ]

        Html.div [
          prop.key "main-content-container"
          prop.className "flex-1 flex overflow-hidden min-h-0"
          prop.children [ leftSidebar; mainContent state dispatch; rightSidebar ]
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
