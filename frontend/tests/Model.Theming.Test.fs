module ModelThemingTests

open Fable.Jester
open Model
open Domain

/// Helper to create a sample Base16Theme
let createSampleTheme slug name variant = {
  System = "base16"
  Name = name
  Author = "Test Author"
  Slug = slug
  Variant = variant
  Palette = {
    Base00 = "2e3440"
    Base01 = "3b4252"
    Base02 = "434c5e"
    Base03 = "4c566a"
    Base04 = "d8dee9"
    Base05 = "e5e9f0"
    Base06 = "eceff4"
    Base07 = "8fbcbb"
    Base08 = "bf616a"
    Base09 = "d08770"
    Base0A = "ebcb8b"
    Base0B = "a3be8c"
    Base0C = "88c0d0"
    Base0D = "81a1c1"
    Base0E = "b48ead"
    Base0F = "5e81ac"
  }
}

Jest.describe (
  "Model Theme Update Tests",
  fun () ->

    Jest.test (
      "LoadThemes message initiates theme loading",
      fun () ->
        let state = State.Default
        let newState, _ = Update LoadThemes state

        Jest.expect(newState).toEqual state
    )

    Jest.test (
      "ThemesLoaded with Ok updates AvailableThemes",
      fun () ->
        let state = State.Default
        let themes = [ "nord"; "solarized-light"; "dracula" ]
        let newState, _ = Update (ThemesLoaded(Ok themes)) state

        Jest.expect(newState.AvailableThemes.Length).toEqual 3
        Jest.expect(newState.AvailableThemes.[0]).toEqual "nord"
        Jest.expect(newState.AvailableThemes.[1]).toEqual "solarized-light"
        Jest.expect(newState.AvailableThemes.[2]).toEqual "dracula"
    )

    Jest.test (
      "ThemesLoaded with Error sets error message",
      fun () ->
        let state = State.Default
        let errorMsg = "Failed to load themes from backend"
        let newState, _ = Update (ThemesLoaded(Error errorMsg)) state

        Jest.expect(newState.Error).not.toEqual None
        Jest.expect(newState.Error.Value).toContain "Failed to load themes"
    )

    Jest.test (
      "LoadTheme message initiates specific theme loading",
      fun () ->
        let state = State.Default
        let newState, _ = Update (LoadTheme "nord") state

        Jest.expect(newState).toEqual state
    )

    Jest.test (
      "ThemeLoaded with Ok updates CurrentTheme and triggers ApplyTheme",
      fun () ->
        let state = State.Default
        let theme = createSampleTheme "nord" "Nord" "dark"
        let newState, _ = Update (ThemeLoaded(Ok theme)) state

        Jest.expect(newState.CurrentTheme).not.toEqual None
        Jest.expect(newState.CurrentTheme.Value.Name).toEqual "Nord"
        Jest.expect(newState.CurrentTheme.Value.Slug).toEqual "nord"
        Jest.expect(newState.CurrentTheme.Value.Variant).toEqual "dark"
    )

    Jest.test (
      "ThemeLoaded with Error sets error message and doesn't update CurrentTheme",
      fun () ->
        let state = State.Default
        let errorMsg = "Theme file not found"
        let newState, _ = Update (ThemeLoaded(Error errorMsg)) state

        Jest.expect(newState.CurrentTheme).toEqual None
        Jest.expect(newState.Error).not.toEqual None
        Jest.expect(newState.Error.Value).toContain "Failed to load theme"
    )

    Jest.test (
      "ApplyTheme message preserves state",
      fun () ->
        let theme = createSampleTheme "nord" "Nord" "dark"
        let state = { State.Default with CurrentTheme = Some theme }
        let newState, _ = Update (ApplyTheme theme) state

        Jest.expect(newState).toEqual state
    )

    Jest.test (
      "Theme workflow: LoadThemes -> ThemesLoaded -> LoadTheme -> ThemeLoaded",
      fun () ->
        let state = State.Default

        let state1, _ = Update LoadThemes state
        Jest.expect(state1.AvailableThemes.Length).toEqual 0

        let themes = [ "nord"; "dracula" ]
        let state2, _ = Update (ThemesLoaded(Ok themes)) state1
        Jest.expect(state2.AvailableThemes.Length).toEqual 2

        let state3, _ = Update (LoadTheme "nord") state2
        Jest.expect(state3.CurrentTheme).toEqual None

        let theme = createSampleTheme "nord" "Nord" "dark"
        let state4, _ = Update (ThemeLoaded(Ok theme)) state3
        Jest.expect(state4.CurrentTheme).not.toEqual None
        Jest.expect(state4.CurrentTheme.Value.Slug).toEqual "nord"
    )

    Jest.test (
      "ThemeLoaded replaces existing CurrentTheme",
      fun () ->
        let oldTheme = createSampleTheme "nord" "Nord" "dark"
        let state = { State.Default with CurrentTheme = Some oldTheme }

        let newTheme = createSampleTheme "solarized-light" "Solarized Light" "light"
        let newState, _ = Update (ThemeLoaded(Ok newTheme)) state

        Jest.expect(newState.CurrentTheme).not.toEqual None
        Jest.expect(newState.CurrentTheme.Value.Slug).toEqual "solarized-light"
        Jest.expect(newState.CurrentTheme.Value.Variant).toEqual "light"
    )

    Jest.test (
      "Empty theme list is handled correctly",
      fun () ->
        let state = State.Default
        let newState, _ = Update (ThemesLoaded(Ok [])) state

        Jest.expect(newState.AvailableThemes.Length).toEqual 0
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "Multiple LoadTheme messages can be processed sequentially",
      fun () ->
        let state = State.Default

        let theme1 = createSampleTheme "nord" "Nord" "dark"
        let state1, _ = Update (ThemeLoaded(Ok theme1)) state
        Jest.expect(state1.CurrentTheme.Value.Slug).toEqual "nord"

        let theme2 = createSampleTheme "dracula" "Dracula" "dark"
        let state2, _ = Update (ThemeLoaded(Ok theme2)) state1
        Jest.expect(state2.CurrentTheme.Value.Slug).toEqual "dracula"

        let theme3 = createSampleTheme "solarized-light" "Solarized Light" "light"
        let state3, _ = Update (ThemeLoaded(Ok theme3)) state2
        Jest.expect(state3.CurrentTheme.Value.Slug).toEqual "solarized-light"
    )

    Jest.test (
      "Theme error doesn't clear AvailableThemes",
      fun () ->
        let themes = [ "nord"; "dracula" ]
        let state = { State.Default with AvailableThemes = themes }

        let newState, _ = Update (ThemeLoaded(Error "Failed to load")) state

        Jest.expect(newState.AvailableThemes.Length).toEqual 2
        Jest.expect(newState.AvailableThemes.[0]).toEqual "nord"
        Jest.expect(newState.AvailableThemes.[1]).toEqual "dracula"
        Jest.expect(newState.Error).not.toEqual None
    )

    Jest.test (
      "Theme variants are preserved correctly",
      fun () ->
        let darkTheme = createSampleTheme "nord" "Nord" "dark"
        let state1, _ = Update (ThemeLoaded(Ok darkTheme)) State.Default
        Jest.expect(state1.CurrentTheme.Value.Variant).toEqual "dark"

        let lightTheme = createSampleTheme "solarized-light" "Solarized Light" "light"
        let state2, _ = Update (ThemeLoaded(Ok lightTheme)) state1
        Jest.expect(state2.CurrentTheme.Value.Variant).toEqual "light"
    )

    Jest.test (
      "Theme palette is preserved during updates",
      fun () ->
        let theme = createSampleTheme "nord" "Nord" "dark"
        let state, _ = Update (ThemeLoaded(Ok theme)) State.Default

        Jest.expect(state.CurrentTheme.Value.Palette.Base00).toEqual "2e3440"
        Jest.expect(state.CurrentTheme.Value.Palette.Base05).toEqual "e5e9f0"
        Jest.expect(state.CurrentTheme.Value.Palette.Base0D).toEqual "81a1c1"
        Jest.expect(state.CurrentTheme.Value.Palette.Base0F).toEqual "5e81ac"
    )
)
