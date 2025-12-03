module Panels

open System
open Feliz
open Browser.Types
open Model
open Domain

module SearchPanel =
  /// Parses a snippet with [[ ]] markers and returns highlighted React elements
  let private parseHighlightedSnippet (snippet : string) : Fable.React.ReactElement list =
    let rec parse (text : string) (acc : Fable.React.ReactElement list) : Fable.React.ReactElement list =
      let startIdx = text.IndexOf("[[")
      let endIdx = text.IndexOf("]]")

      if startIdx = -1 || endIdx = -1 || startIdx >= endIdx then
        if text.Length > 0 then acc @ [ Html.text text ] else acc
      else
        let beforeMarker = text.Substring(0, startIdx)
        let highlighted = text.Substring(startIdx + 2, endIdx - startIdx - 2)
        let afterMarker = text.Substring(endIdx + 2)

        let newAcc =
          if beforeMarker.Length > 0 then
            acc @ [ Html.text beforeMarker ]
          else
            acc

        let newAcc =
          newAcc
          @ [ Html.span [ prop.className "search-highlight"; prop.text highlighted ] ]

        parse afterMarker newAcc

    parse snippet []

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
            prop.children (parseHighlightedSnippet result.Snippet)
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
      prop.className "flex-1 flex flex-col min-h-0 default-transition"
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
                  prop.onFocus (fun _ ->
                    if state.Search.Query = "" then
                      dispatch (ShowSearchHistory true))
                  prop.onBlur (fun _ -> dispatch (ShowSearchHistory false))
                  prop.onKeyDown (fun (e : Browser.Types.KeyboardEvent) ->
                    match e.key with
                    | "ArrowDown" when state.Search.ShowHistoryAutocomplete ->
                      e.preventDefault ()
                      dispatch (NavigateSearchHistory 1)
                    | "ArrowUp" when state.Search.ShowHistoryAutocomplete ->
                      e.preventDefault ()
                      dispatch (NavigateSearchHistory -1)
                    | "Enter" when state.Search.ShowHistoryAutocomplete && state.Search.SelectedHistoryIndex.IsSome ->
                      e.preventDefault ()

                      match state.WorkspaceSnapshot with
                      | Some snapshot ->
                        match state.Search.SelectedHistoryIndex with
                        | Some idx when idx < snapshot.UI.SearchHistory.Length ->
                          let selectedQuery = snapshot.UI.SearchHistory.[idx]
                          dispatch (SelectSearchHistoryItem selectedQuery)
                        | _ -> ()
                      | _ -> ()
                    | _ -> ())
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
                        Html.button [
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

                if
                  state.Search.ShowHistoryAutocomplete
                  && state.WorkspaceSnapshot.IsSome
                  && not (List.isEmpty state.WorkspaceSnapshot.Value.UI.SearchHistory)
                then
                  Html.div [
                    prop.className
                      "absolute top-full left-0 right-0 mt-1 bg-base00 border border-base02 rounded shadow-lg z-50 max-h-64 overflow-y-auto"
                    prop.onMouseDown (fun e -> e.preventDefault ())
                    prop.children [
                      Html.div [
                        prop.className "px-3 py-2 text-xs text-base03 border-b border-base02 font-semibold"
                        prop.text "Recent Searches"
                      ]
                      Html.div [
                        prop.children (
                          state.WorkspaceSnapshot.Value.UI.SearchHistory
                          |> List.mapi (fun idx query ->
                            let isSelected = state.Search.SelectedHistoryIndex = Some idx

                            Html.div [
                              prop.key $"history-{idx}"
                              prop.className (
                                if isSelected then
                                  "px-3 py-2 bg-base02 cursor-pointer transition-colors"
                                else
                                  "px-3 py-2 hover:bg-base02 cursor-pointer transition-colors"
                              )
                              prop.onMouseDown (fun e ->
                                e.preventDefault ()
                                dispatch (SelectSearchHistoryItem query))
                              prop.children [ Html.span [ prop.className "text-base05"; prop.text query ] ]
                            ])
                        )
                      ]
                    ]
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
      prop.className "flex-1 flex flex-col min-h-0 default-transition"
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
      prop.className "flex-1 flex flex-col min-h-0 default-transition"
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
      prop.role "button"
      prop.tabIndex 0
      prop.onClick (fun _ -> dispatch (ToggleTaskFromPanel task))
      prop.onKeyDown (fun (ev : KeyboardEvent) ->
        if ev.key = "Enter" || ev.key = " " then
          ev.preventDefault ()
          dispatch (ToggleTaskFromPanel task))
      prop.children [
        Html.div [
          prop.className "flex items-start gap-2"
          prop.children [
            Html.div [
              prop.className "shrink-0 mt-0.5 text-lg"
              prop.children [
                if task.IsCompleted then
                  Html.span [ prop.className "text-green"; prop.text "\u2611" ]
                else
                  Html.span [ prop.className "text-base03"; prop.text "\u2610" ]
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
                Html.div [
                  prop.className "text-xs text-base03 mt-1 flex items-center gap-2"
                  prop.children [
                    Html.span [ prop.text task.NotePath ]
                    Html.button [
                      prop.className "text-xs text-base0D hover:text-base0C underline focus:outline-none"
                      prop.onClick (fun (ev : MouseEvent) ->
                        ev.stopPropagation ()
                        dispatch (SelectNote task.NoteId))
                      prop.text "Open note"
                    ]
                  ]
                ]
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
      prop.className "flex-1 flex flex-col min-h-0 default-transition"
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

  /// Base16 color descriptions
  let private base16Descriptions =
    Map.ofList [
      "base00", "Default Background"
      "base01", "Lighter Background (status bars)"
      "base02", "Selection Background"
      "base03", "Comments, Secondary Text"
      "base04", "Dark Foreground"
      "base05", "Default Foreground"
      "base06", "Light Foreground"
      "base07", "Lightest Foreground"
      "base08", "Red (errors, variables)"
      "base09", "Orange (numbers)"
      "base0A", "Yellow (classes, search)"
      "base0B", "Green (strings, success)"
      "base0C", "Cyan (regex, links)"
      "base0D", "Blue (functions, primary)"
      "base0E", "Magenta (keywords)"
      "base0F", "Brown (deprecated)"
    ]

  /// Gets the color value from theme or override
  let private getColorValue
    (colorName : string)
    (theme : Base16Theme option)
    (overrides : Map<string, string>)
    : string =
    match overrides.TryFind(colorName) with
    | Some overrideValue -> overrideValue
    | None ->
      match theme with
      | Some t ->
        match colorName with
        | "base00" -> t.Palette.Base00
        | "base01" -> t.Palette.Base01
        | "base02" -> t.Palette.Base02
        | "base03" -> t.Palette.Base03
        | "base04" -> t.Palette.Base04
        | "base05" -> t.Palette.Base05
        | "base06" -> t.Palette.Base06
        | "base07" -> t.Palette.Base07
        | "base08" -> t.Palette.Base08
        | "base09" -> t.Palette.Base09
        | "base0A" -> t.Palette.Base0A
        | "base0B" -> t.Palette.Base0B
        | "base0C" -> t.Palette.Base0C
        | "base0D" -> t.Palette.Base0D
        | "base0E" -> t.Palette.Base0E
        | "base0F" -> t.Palette.Base0F
        | _ -> "#000000"
      | None -> "#000000"

  /// Renders a single color customization row
  let private colorCustomizationRow
    (colorName : string)
    (theme : Base16Theme option)
    (overrides : Map<string, string>)
    (dispatch : Msg -> unit)
    =
    let currentColor = getColorValue colorName theme overrides

    let description =
      base16Descriptions.TryFind(colorName) |> Option.defaultValue colorName

    let hasOverride = overrides.ContainsKey(colorName)

    Html.div [
      prop.className "flex items-center gap-3 mb-2 p-2 rounded hover:bg-base01 transition-colors"
      prop.children [
        Html.div [
          prop.className "w-12 h-8 rounded border-2 border-base02 shrink-0"
          prop.style [ style.backgroundColor currentColor ]
        ]
        Html.div [
          prop.className "flex-1 min-w-0"
          prop.children [
            Html.div [
              prop.className "font-mono text-sm font-semibold text-base05"
              prop.text colorName
            ]
            Html.div [ prop.className "text-xs text-base03"; prop.text description ]
          ]
        ]
        Html.input [
          prop.type' "color"
          prop.value currentColor
          prop.className "w-12 h-8 cursor-pointer"
          prop.onChange (fun (value : string) -> dispatch (UpdateColorOverride(colorName, value)))
        ]
        Html.div [
          prop.className "font-mono text-xs text-base04 w-20 text-center"
          prop.text currentColor
        ]
        if hasOverride then
          Html.button [
            prop.className
              "px-2 py-1 text-xs bg-base02 hover:bg-red text-base05 hover:text-base00 rounded transition-all"
            prop.text "Reset"
            prop.onClick (fun _ -> dispatch (ResetColorOverride colorName))
          ]
        else
          Html.div [ prop.className "w-14" ]
      ]
    ]

  /// Renders theme preview swatches
  let private themePreview (theme : Base16Theme option) (overrides : Map<string, string>) =
    match theme with
    | Some t ->
      let colors = [
        "base00"
        "base01"
        "base02"
        "base03"
        "base04"
        "base05"
        "base06"
        "base07"
        "base08"
        "base09"
        "base0A"
        "base0B"
        "base0C"
        "base0D"
        "base0E"
        "base0F"
      ]

      Html.div [
        prop.className "grid grid-cols-8 gap-1 mt-2"
        prop.children (
          colors
          |> List.map (fun colorName ->
            let color = getColorValue colorName (Some t) overrides

            Html.div [
              prop.key colorName
              prop.title $"{colorName}: {color}"
              prop.className "h-8 rounded border border-base02 cursor-help"
              prop.style [ style.backgroundColor color ]
            ])
        )
      ]
    | None -> Html.none

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
          Base16Theme = None
          ColorOverrides = Map.empty
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

        section "Color Theme" [
          Html.div [
            prop.className "mb-3"
            prop.children [
              Html.label [
                prop.className "block text-sm font-medium text-base04 mb-1"
                prop.text "Base16 Theme"
              ]
              Html.select [
                prop.className
                  "w-full bg-base00 border border-base02 rounded px-3 py-2 text-base05 focus:outline-none focus:border-blue"
                prop.value (state.CurrentTheme |> Option.map (fun t -> t.Slug) |> Option.defaultValue "")
                prop.onChange (fun (slug : string) ->
                  if not (System.String.IsNullOrEmpty slug) then
                    dispatch (LoadTheme slug))
                prop.children (
                  Html.option [ prop.value ""; prop.text "Select a theme..." ]
                  :: (state.AvailableThemes
                      |> List.map (fun slug -> Html.option [ prop.value slug; prop.text slug ]))
                )
              ]
            ]
          ]

          match state.CurrentTheme with
          | Some theme ->
            Html.div [
              prop.className "mb-3"
              prop.children [
                Html.div [
                  prop.className "text-sm text-base04 mb-1"
                  prop.text $"Current: {theme.Name} by {theme.Author} ({theme.Variant})"
                ]
                themePreview (Some theme) state.ColorOverrides
              ]
            ]
          | None -> Html.none
        ]

        section "Color Customization" [
          Html.div [
            prop.className "mb-3"
            prop.children [
              Html.p [
                prop.className "text-sm text-base04 mb-3"
                prop.text "Customize individual base16 colors. Changes are applied in real-time."
              ]

              if state.ColorOverrides.IsEmpty then
                Html.div [
                  prop.className "text-xs text-base03 italic p-3 bg-base00 rounded border border-base02"
                  prop.text "No color overrides. Select colors below to customize the theme."
                ]
              else
                Html.div [
                  prop.className
                    "flex items-center justify-between mb-3 p-2 bg-blue bg-opacity-10 rounded border border-blue"
                  prop.children [
                    Html.span [
                      prop.className "text-sm text-blue"
                      prop.text $"{state.ColorOverrides.Count} color(s) customized"
                    ]
                    Html.button [
                      prop.className
                        "px-3 py-1 text-xs bg-red hover:bg-red-bright text-base00 rounded transition-all font-medium"
                      prop.text "Reset All"
                      prop.onClick (fun _ -> dispatch ResetAllColorOverrides)
                    ]
                  ]
                ]
            ]
          ]

          Html.div [
            prop.className "space-y-1 max-h-96 overflow-y-auto pr-2"
            prop.children (
              [
                "base00"
                "base01"
                "base02"
                "base03"
                "base04"
                "base05"
                "base06"
                "base07"
                "base08"
                "base09"
                "base0A"
                "base0B"
                "base0C"
                "base0D"
                "base0E"
                "base0F"
              ]
              |> List.map (fun colorName ->
                colorCustomizationRow colorName state.CurrentTheme state.ColorOverrides dispatch)
            )
          ]

          if not state.ColorOverrides.IsEmpty then
            Html.div [
              prop.className "mt-4 pt-4 border-t border-base02"
              prop.children [
                Html.button [
                  prop.className
                    "w-full bg-blue hover:bg-blue-bright text-base00 font-medium py-2 px-4 rounded transition-all"
                  prop.text "Export Custom Theme as YAML"
                  prop.onClick (fun _ -> dispatch ExportCustomTheme)
                ]
                Html.p [
                  prop.className "text-xs text-base03 mt-2 text-center"
                  prop.text "Save your customized theme as a YAML file"
                ]
              ]
            ]
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
