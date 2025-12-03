module ModelSettingsTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update (Settings)",
  fun () ->
    Jest.test (
      "SettingsLoaded success updates settings",
      fun () ->
        let initialState = State.Default

        let testSettings = {
          General = {
            Theme = "dark"
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

        let newState, _ = Update (SettingsLoaded(Ok testSettings)) initialState
        Jest.expect(newState.Settings.IsSome).toEqual true
        Jest.expect(newState.Error).toEqual None

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.Theme).toEqual "dark"
          Jest.expect(s.Editor.FontSize).toEqual 14
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load settings"
        let newState, _ = Update (SettingsLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Settings).toEqual None
    )

    Jest.test (
      "SettingsChanged updates settings and triggers save",
      fun () ->
        let initialState = State.Default

        let testSettings = {
          General = {
            Theme = "light"
            Language = "en"
            AutoSave = false
            AutoSaveInterval = 60
            Base16Theme = None
            ColorOverrides = Map.empty
          }
          Editor = {
            FontFamily = "Inter"
            FontSize = 16
            LineHeight = 1.8
            TabSize = 4
            VimMode = true
            SpellCheck = false
          }
        }

        let newState, _ = Update (SettingsChanged testSettings) initialState
        Jest.expect(newState.Settings.IsSome).toEqual true

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.Theme).toEqual "light"
          Jest.expect(s.Editor.FontSize).toEqual 16
          Jest.expect(s.Editor.VimMode).toEqual true
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsSaved success clears error",
      fun () ->
        let initialState = { State.Default with Error = Some "Previous error" }
        let newState, _ = Update (SettingsSaved(Ok())) initialState
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "SettingsSaved error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to save settings"
        let newState, _ = Update (SettingsSaved(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
    )

    Jest.test (
      "DebouncedSettingsSave triggers save when settings exist",
      fun () ->
        let testSettings = {
          General = {
            Theme = "dark"
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

        let initialState = { State.Default with Settings = Some testSettings }
        let newState, _ = Update DebouncedSettingsSave initialState
        Jest.expect(newState.SettingsSaveTimer).toEqual None
    )

    Jest.test (
      "DebouncedSettingsSave does nothing when no settings",
      fun () ->
        let initialState = { State.Default with Settings = None }
        let newState, cmd = Update DebouncedSettingsSave initialState
        Jest.expect(newState).toEqual initialState
    )

    Jest.test (
      "SettingsChanged immediately updates settings state",
      fun () ->
        let initialState = State.Default

        let testSettings = {
          General = {
            Theme = "light"
            Language = "es"
            AutoSave = false
            AutoSaveInterval = 60
            Base16Theme = None
            ColorOverrides = Map.empty
          }
          Editor = {
            FontFamily = "JetBrains Mono"
            FontSize = 16
            LineHeight = 1.8
            TabSize = 4
            VimMode = true
            SpellCheck = false
          }
        }

        let newState, _ = Update (SettingsChanged testSettings) initialState
        Jest.expect(newState.Settings).toEqual (Some testSettings)

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.Theme).toEqual "light"
          Jest.expect(s.General.Language).toEqual "es"
          Jest.expect(s.General.AutoSave).toEqual false
          Jest.expect(s.General.AutoSaveInterval).toEqual 60
          Jest.expect(s.Editor.FontFamily).toEqual "JetBrains Mono"
          Jest.expect(s.Editor.FontSize).toEqual 16
          Jest.expect(s.Editor.LineHeight).toEqual 1.8
          Jest.expect(s.Editor.TabSize).toEqual 4
          Jest.expect(s.Editor.VimMode).toEqual true
          Jest.expect(s.Editor.SpellCheck).toEqual false
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsChanged with theme change",
      fun () ->
        let initialSettings = {
          General = {
            Theme = "dark"
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

        let initialState = { State.Default with Settings = Some initialSettings }

        let updatedSettings = {
          initialSettings with
              General = { initialSettings.General with Theme = "light" }
        }

        let newState, _ = Update (SettingsChanged updatedSettings) initialState

        match newState.Settings with
        | Some s -> Jest.expect(s.General.Theme).toEqual "light"
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsChanged with font size change",
      fun () ->
        let initialSettings = {
          General = {
            Theme = "dark"
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

        let initialState = { State.Default with Settings = Some initialSettings }

        let updatedSettings = {
          initialSettings with
              Editor = { initialSettings.Editor with FontSize = 18 }
        }

        let newState, _ = Update (SettingsChanged updatedSettings) initialState

        match newState.Settings with
        | Some s -> Jest.expect(s.Editor.FontSize).toEqual 18
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsChanged edge case - minimum values",
      fun () ->
        let settings = {
          General = {
            Theme = "dark"
            Language = "en"
            AutoSave = true
            AutoSaveInterval = 5
            Base16Theme = None
            ColorOverrides = Map.empty
          }
          Editor = {
            FontFamily = "monospace"
            FontSize = 10
            LineHeight = 1.0
            TabSize = 2
            VimMode = false
            SpellCheck = true
          }
        }

        let initialState = State.Default
        let newState, _ = Update (SettingsChanged settings) initialState

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.AutoSaveInterval).toEqual 5
          Jest.expect(s.Editor.FontSize).toEqual 10
          Jest.expect(s.Editor.LineHeight).toEqual 1.0
        | None -> failwith "Expected settings to be present"
    )

    Jest.test (
      "SettingsChanged edge case - maximum values",
      fun () ->
        let settings = {
          General = {
            Theme = "dark"
            Language = "en"
            AutoSave = true
            AutoSaveInterval = 120
            Base16Theme = None
            ColorOverrides = Map.empty
          }
          Editor = {
            FontFamily = "monospace"
            FontSize = 24
            LineHeight = 3.0
            TabSize = 8
            VimMode = false
            SpellCheck = true
          }
        }

        let initialState = State.Default
        let newState, _ = Update (SettingsChanged settings) initialState

        match newState.Settings with
        | Some s ->
          Jest.expect(s.General.AutoSaveInterval).toEqual 120
          Jest.expect(s.Editor.FontSize).toEqual 24
          Jest.expect(s.Editor.LineHeight).toEqual 3.0
          Jest.expect(s.Editor.TabSize).toEqual 8
        | None -> failwith "Expected settings to be present"
    )
)
