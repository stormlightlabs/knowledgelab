module View

open Feliz
open Model
open Domain

/// Renders the vault picker screen when no workspace is open
let vaultPicker (state: State) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "flex items-center justify-center h-screen bg-gray-50"
          prop.children
              [ Html.div
                    [ prop.className "bg-white p-8 rounded-lg shadow-lg max-w-md w-full"
                      prop.children
                          [ Html.h1 [ prop.className "text-2xl font-bold mb-4"; prop.text "Open Workspace" ]
                            Html.p
                                [ prop.className "text-gray-600 mb-4"
                                  prop.text "Select a folder to open as your notes workspace" ]
                            Html.button
                                [ prop.className
                                      "w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded"
                                  prop.text "Choose Folder"
                                  prop.onClick (fun _ ->
                                      // TODO: Open folder picker dialog
                                      dispatch (OpenVault "./test-workspace")) ] ] ] ] ]

/// Renders a note list item
let noteListItem (note: NoteSummary) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "p-3 hover:bg-gray-100 cursor-pointer border-b border-gray-200"
          prop.onClick (fun _ -> dispatch (SelectNote note.Id))
          prop.children
              [ Html.div [ prop.className "font-semibold text-gray-900"; prop.text note.Title ]
                Html.div [ prop.className "text-sm text-gray-500"; prop.text note.Path ]
                Html.div
                    [ prop.className "flex gap-2 mt-1"
                      prop.children
                          [ for tag in note.Tags do
                                Html.span
                                    [ prop.className "text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded"
                                      prop.text $"#{tag.Name}" ] ] ] ] ]

/// Renders the notes list sidebar
let notesList (state: State) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "w-64 bg-white border-r border-gray-200 flex flex-col"
          prop.children
              [ Html.div
                    [ prop.className "p-4 border-b border-gray-200"
                      prop.children
                          [ Html.h2 [ prop.className "font-bold text-lg"; prop.text "Notes" ]
                            Html.button
                                [ prop.className
                                      "mt-2 w-full bg-blue-500 hover:bg-blue-600 text-white text-sm font-bold py-1 px-2 rounded"
                                  prop.text "New Note"
                                  prop.onClick (fun _ -> dispatch (CreateNote("Untitled", ""))) ] ] ]
                Html.div
                    [ prop.className "flex-1 overflow-y-auto"
                      prop.children
                          [ for note in state.Notes do
                                noteListItem note dispatch ] ] ] ]

/// Renders the note editor
let noteEditor (note: Note) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "flex-1 flex flex-col bg-white"
          prop.children
              [ Html.div
                    [ prop.className "p-4 border-b border-gray-200"
                      prop.children
                          [ Html.h1 [ prop.className "text-2xl font-bold"; prop.text note.Title ]
                            Html.div [ prop.className "text-sm text-gray-500 mt-1"; prop.text note.Path ] ] ]
                Html.div
                    [ prop.className "flex-1 p-6 overflow-y-auto"
                      prop.children
                          [ Html.textarea
                                [ prop.className "w-full h-full font-mono text-sm border border-gray-300 rounded p-4"
                                  prop.value note.Content
                                  prop.placeholder "Start writing..."
                                  prop.onChange (fun (value: string) ->
                                      let updatedNote = { note with Content = value }
                                      dispatch (SaveNote updatedNote)) ] ] ] ] ]

/// Renders the main content area based on current route
let mainContent (state: State) (dispatch: Msg -> unit) =
    match state.CurrentRoute with
    | VaultPicker -> vaultPicker state dispatch
    | NoteList ->
        Html.div
            [ prop.className "flex-1 flex items-center justify-center bg-gray-50"
              prop.children
                  [ Html.div
                        [ prop.className "text-center"
                          prop.children
                              [ Html.h2
                                    [ prop.className "text-xl font-semibold text-gray-600"
                                      prop.text "Select a note to begin" ] ] ] ] ]
    | NoteEditor _ ->
        match state.CurrentNote with
        | Some note -> noteEditor note dispatch
        | None ->
            Html.div
                [ prop.className "flex-1 flex items-center justify-center"
                  prop.text "Loading..." ]
    | GraphView ->
        Html.div
            [ prop.className "flex-1 flex items-center justify-center"
              prop.text "Graph view (coming soon)" ]
    | Settings ->
        Html.div
            [ prop.className "flex-1 flex items-center justify-center"
              prop.text "Settings (coming soon)" ]

/// Renders error notification if present
let errorNotification (error: string option) (dispatch: Msg -> unit) =
    match error with
    | Some err ->
        Html.div
            [ prop.className "fixed top-4 right-4 bg-red-500 text-white px-4 py-3 rounded shadow-lg"
              prop.children
                  [ Html.span [ prop.text err ]
                    Html.button
                        [ prop.className "ml-4 font-bold"
                          prop.text "×"
                          prop.onClick (fun _ -> dispatch ClearError) ] ] ]
    | None -> Html.none

/// Main application view
let render (state: State) (dispatch: Msg -> unit) =
    Html.div
        [ prop.className "h-screen w-full bg-gray-50 flex overflow-hidden"
          prop.children
              [ if state.Workspace.IsSome && state.CurrentRoute <> VaultPicker then
                    notesList state dispatch

                mainContent state dispatch

                errorNotification state.Error dispatch

                if state.Loading then
                    Html.div
                        [ prop.className "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center"
                          prop.children
                              [ Html.div [ prop.className "bg-white p-6 rounded-lg shadow-xl"; prop.text "Loading..." ] ] ] ] ]
