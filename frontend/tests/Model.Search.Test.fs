module ModelSearchTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Search State Management",
  fun () ->
    Jest.test (
      "SearchQueryChanged updates query and triggers search",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SearchQueryChanged "test query") initialState
        Jest.expect(newState.Search.Query).toEqual "test query"
        Jest.expect(newState.Search.IsLoading).toEqual true
    )

    Jest.test (
      "SearchResultsReceived updates results and clears loading",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    IsLoading = true
                    Query = "test"
              }
        }

        let testResults = [
          {
            NoteId = "note1"
            Title = "Test Note"
            Path = "/notes/test.md"
            Score = 10.5
            Tags = [ "test"; "sample" ]
            ModifiedAt = System.DateTime.Now
            Snippet = "This is a test snippet..."
          }
        ]

        let newState, _ = Update (SearchResultsReceived(Ok testResults)) initialState
        Jest.expect(newState.Search.Results.Length).toEqual 1
        Jest.expect(newState.Search.IsLoading).toEqual false
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "SearchResultsReceived handles errors",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    IsLoading = true
                    Query = "test"
              }
        }

        let newState, _ = Update (SearchResultsReceived(Error "Search failed")) initialState
        Jest.expect(newState.Search.IsLoading).toEqual false
        Jest.expect(newState.Search.Results.Length).toEqual 0
        Jest.expect(newState.Error).toEqual (Some "Search failed: Search failed")
    )

    Jest.test (
      "SearchCleared resets search state",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                Query = "test query"
                Results = [
                  {
                    NoteId = "note1"
                    Title = "Test"
                    Path = "/test.md"
                    Score = 5.0
                    Tags = []
                    ModifiedAt = System.DateTime.Now
                    Snippet = "snippet"
                  }
                ]
                IsLoading = false
                Filters = State.Default.Search.Filters
                DebounceTimer = None
                ShowTagAutocomplete = false
                TagAutocompleteQuery = ""
                AvailableTags = []
                ShowHistoryAutocomplete = false
                SelectedHistoryIndex = None
              }
        }

        let newState, _ = Update SearchCleared initialState
        Jest.expect(newState.Search.Query).toEqual ""
        Jest.expect(newState.Search.Results.Length).toEqual 0
        Jest.expect(newState.Search.IsLoading).toEqual false
    )

    Jest.test (
      "UpdateSearchQuery updates query without triggering search",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (UpdateSearchQuery "new query") initialState
        Jest.expect(newState.Search.Query).toEqual "new query"
        Jest.expect(newState.Search.IsLoading).toEqual false
    )

    Jest.test (
      "PerformSearch sets loading state and initiates search",
      fun () ->
        let initialState = {
          State.Default with
              Search = { State.Default.Search with Query = "test" }
        }

        let newState, _ = Update PerformSearch initialState
        Jest.expect(newState.Search.IsLoading).toEqual true
    )

    Jest.test (
      "UpdateSearchFilters updates search filters",
      fun () ->
        let initialState = State.Default

        let newFilters = {
          Tags = [ "tag1"; "tag2" ]
          PathPrefix = "/notes/"
          DateFrom = Some(System.DateTime(2024, 1, 1))
          DateTo = Some(System.DateTime(2024, 12, 31))
        }

        let newState, _ = Update (UpdateSearchFilters newFilters) initialState
        Jest.expect(newState.Search.Filters.Tags.Length).toEqual 2
        Jest.expect(newState.Search.Filters.PathPrefix).toEqual "/notes/"
        Jest.expect(newState.Search.Filters.DateFrom.IsSome).toEqual true
        Jest.expect(newState.Search.Filters.DateTo.IsSome).toEqual true
    )

    Jest.test (
      "SearchCompleted (deprecated) updates results",
      fun () ->
        let initialState = State.Default

        let testResults = [
          {
            NoteId = "note1"
            Title = "Result 1"
            Path = "/note1.md"
            Score = 8.0
            Tags = [ "test" ]
            ModifiedAt = System.DateTime.Now
            Snippet = "Test snippet"
          }
        ]

        let newState, _ = Update (SearchCompleted(Ok testResults)) initialState
        Jest.expect(newState.Search.Results.Length).toEqual 1
        Jest.expect(newState.Search.IsLoading).toEqual false
    )

    Jest.test (
      "Search with empty query",
      fun () ->
        let initialState = {
          State.Default with
              Search = { State.Default.Search with Query = "" }
        }

        let newState, _ = Update PerformSearch initialState
        Jest.expect(newState.Search.IsLoading).toEqual true
    )

    Jest.test (
      "Search results preserve snippet data",
      fun () ->
        let initialState = {
          State.Default with
              Search = { State.Default.Search with IsLoading = true }
        }

        let snippet = "This is a longer test snippet that should be preserved"

        let testResults = [
          {
            NoteId = "note1"
            Title = "Test Note"
            Path = "/test.md"
            Score = 10.0
            Tags = []
            ModifiedAt = System.DateTime.Now
            Snippet = snippet
          }
        ]

        let newState, _ = Update (SearchResultsReceived(Ok testResults)) initialState
        Jest.expect(newState.Search.Results.[0].Snippet).toEqual snippet
    )

    Jest.test (
      "SearchQueryChanged with empty query clears search",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                Query = "previous query"
                Results = [
                  {
                    NoteId = "note1"
                    Title = "Test"
                    Path = "/test.md"
                    Score = 5.0
                    Tags = []
                    ModifiedAt = System.DateTime.Now
                    Snippet = "snippet"
                  }
                ]
                IsLoading = false
                Filters = State.Default.Search.Filters
                DebounceTimer = None
                ShowTagAutocomplete = false
                TagAutocompleteQuery = ""
                AvailableTags = []
                ShowHistoryAutocomplete = false
                SelectedHistoryIndex = None
              }
        }

        let newState, _ = Update (SearchQueryChanged "") initialState
        Jest.expect(newState.Search.Query).toEqual ""
        Jest.expect(newState.Search.Results.Length).toEqual 0
        Jest.expect(newState.Search.IsLoading).toEqual false
        Jest.expect(newState.Search.ShowTagAutocomplete).toEqual false
    )

    Jest.test (
      "SearchQueryChanged with non-empty query sets loading and debounces",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (SearchQueryChanged "test query") initialState
        Jest.expect(newState.Search.Query).toEqual "test query"
        Jest.expect(newState.Search.IsLoading).toEqual true
    )

    Jest.test (
      "DebouncedSearch performs actual search",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    Query = "test search"
                    IsLoading = false
              }
        }

        let newState, _ = Update DebouncedSearch initialState
        Jest.expect(newState.Search.IsLoading).toEqual true
        Jest.expect(newState.Search.Query).toEqual "test search"
    )

    Jest.test (
      "SearchCleared cancels debounce timer",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    Query = "test"
                    DebounceTimer = Some 123
              }
        }

        let newState, _ = Update SearchCleared initialState
        Jest.expect(newState.Search.Query).toEqual ""
        Jest.expect(newState.Search.DebounceTimer).toEqual None
    )

    Jest.test (
      "UpdateTagAutocomplete filters tags by query",
      fun () ->
        let initialState = {
          State.Default with
              TagInfos = [
                { Name = "project"; Count = 5; NoteIds = [] }
                { Name = "personal"; Count = 3; NoteIds = [] }
                { Name = "work"; Count = 2; NoteIds = [] }
              ]
        }

        let newState, _ = Update (UpdateTagAutocomplete(true, "pro")) initialState
        Jest.expect(newState.Search.ShowTagAutocomplete).toEqual true
        Jest.expect(newState.Search.AvailableTags.Length).toEqual 1
        Jest.expect(newState.Search.AvailableTags.[0]).toEqual "project"
    )

    Jest.test (
      "UpdateTagAutocomplete hides when no matches",
      fun () ->
        let initialState = {
          State.Default with
              TagInfos = [
                { Name = "project"; Count = 5; NoteIds = [] }
                { Name = "personal"; Count = 3; NoteIds = [] }
              ]
        }

        let newState, _ = Update (UpdateTagAutocomplete(true, "xyz")) initialState
        Jest.expect(newState.Search.ShowTagAutocomplete).toEqual false
        Jest.expect(newState.Search.AvailableTags.Length).toEqual 0
    )

    Jest.test (
      "UpdateTagAutocomplete limits results to 10",
      fun () ->
        let tagInfos = [ for i in 1..20 -> { Name = $"tag{i}"; Count = i; NoteIds = [] } ]

        let initialState = { State.Default with TagInfos = tagInfos }

        let newState, _ = Update (UpdateTagAutocomplete(true, "tag")) initialState
        Jest.expect(newState.Search.AvailableTags.Length).toEqual 10
    )

    Jest.test (
      "SearchResultsReceived error clears results and shows error",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    Query = "test"
                    IsLoading = true
                    Results = [
                      {
                        NoteId = "note1"
                        Title = "Test"
                        Path = "/test.md"
                        Score = 5.0
                        Tags = []
                        ModifiedAt = System.DateTime.Now
                        Snippet = "snippet"
                      }
                    ]
              }
        }

        let newState, _ = Update (SearchResultsReceived(Error "Network error")) initialState
        Jest.expect(newState.Search.IsLoading).toEqual false
        Jest.expect(newState.Search.Results.Length).toEqual 0
        Jest.expect(newState.Error).toEqual (Some "Search failed: Network error")
    )

    Jest.test (
      "ShowSearchHistory shows history autocomplete",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (ShowSearchHistory true) initialState
        Jest.expect(newState.Search.ShowHistoryAutocomplete).toEqual true
        Jest.expect(newState.Search.SelectedHistoryIndex).toEqual None
    )

    Jest.test (
      "ShowSearchHistory hides history autocomplete",
      fun () ->
        let initialState = {
          State.Default with
              Search = {
                State.Default.Search with
                    ShowHistoryAutocomplete = true
                    SelectedHistoryIndex = Some 1
              }
        }

        let newState, _ = Update (ShowSearchHistory false) initialState
        Jest.expect(newState.Search.ShowHistoryAutocomplete).toEqual false
        Jest.expect(newState.Search.SelectedHistoryIndex).toEqual None
    )

    Jest.test (
      "DebouncedSearch adds query to search history",
      fun () ->
        let initialState = {
          State.Default with
              WorkspaceSnapshot =
                Some {
                  UI = {
                    ActivePage = ""
                    SidebarVisible = true
                    SidebarWidth = 280
                    RightPanelVisible = false
                    RightPanelWidth = 300
                    PinnedPages = []
                    RecentPages = []
                    LastWorkspacePath = ""
                    GraphLayout = "force"
                    SearchHistory = []
                    NotesSortBy = None
                    NotesSortOrder = None
                  }
                }
              Search = {
                State.Default.Search with
                    Query = "test query"
                    IsLoading = false
              }
        }

        let newState, _ = Update DebouncedSearch initialState
        Jest.expect(newState.Search.IsLoading).toEqual true
        Jest.expect(newState.Search.ShowHistoryAutocomplete).toEqual false

        match newState.WorkspaceSnapshot with
        | Some snapshot ->
          Jest.expect(snapshot.UI.SearchHistory.Length).toEqual 1
          Jest.expect(snapshot.UI.SearchHistory.[0]).toEqual "test query"
        | None -> failwith "Expected workspace snapshot to exist"
    )
)
