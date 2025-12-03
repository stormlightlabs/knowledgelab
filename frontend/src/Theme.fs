/// Theme application logic for base16 color schemes
module Theme

open Fable.Core.JsInterop
open Domain
open Browser.Dom
open System

let private normalizeHexColor (value : string) =
  if String.IsNullOrWhiteSpace value then
    None
  else
    let trimmed = value.Trim()

    if trimmed.StartsWith "#" then
      Some trimmed
    else
      Some $"#{trimmed}"

let private setCssVar (style : obj) (name : string) (value : string) =
  match normalizeHexColor value with
  | Some normalized -> style?setProperty (name, normalized)
  | None -> ()

/// Applies a base16 theme with optional color overrides by setting CSS variables on the document root
let ApplyTheme (theme : Base16Theme) (overrides : Map<string, string>) : unit =
  let root = document.documentElement
  let style = root?style

  let baseColors = [
    "00", theme.Palette.Base00
    "01", theme.Palette.Base01
    "02", theme.Palette.Base02
    "03", theme.Palette.Base03
    "04", theme.Palette.Base04
    "05", theme.Palette.Base05
    "06", theme.Palette.Base06
    "07", theme.Palette.Base07
    "08", theme.Palette.Base08
    "09", theme.Palette.Base09
    "0A", theme.Palette.Base0A
    "0B", theme.Palette.Base0B
    "0C", theme.Palette.Base0C
    "0D", theme.Palette.Base0D
    "0E", theme.Palette.Base0E
    "0F", theme.Palette.Base0F
  ]

  // Apply theme colors, preferring overrides when present
  baseColors
  |> List.iter (fun (suffix, baseValue) ->
    let finalValue =
      match overrides.TryFind($"base{suffix}") with
      | Some overrideValue -> overrideValue
      | None -> baseValue

    setCssVar style (sprintf "--base%s" suffix) finalValue
    setCssVar style (sprintf "--color-base%s" suffix) finalValue)

  let overrideInfo =
    if overrides.IsEmpty then
      ""
    else
      $" with {overrides.Count} override(s)"

  printfn "Applied theme: %s (%s)%s" theme.Name theme.Variant overrideInfo

/// Creates a custom theme by applying color overrides to a base theme
let CreateCustomTheme (baseTheme : Base16Theme) (overrides : Map<string, string>) : Base16Theme =
  let applyOverride colorName baseValue =
    match overrides.TryFind(colorName) with
    | Some overrideValue -> overrideValue
    | None -> baseValue

  let customPalette = {
    Base00 = applyOverride "base00" baseTheme.Palette.Base00
    Base01 = applyOverride "base01" baseTheme.Palette.Base01
    Base02 = applyOverride "base02" baseTheme.Palette.Base02
    Base03 = applyOverride "base03" baseTheme.Palette.Base03
    Base04 = applyOverride "base04" baseTheme.Palette.Base04
    Base05 = applyOverride "base05" baseTheme.Palette.Base05
    Base06 = applyOverride "base06" baseTheme.Palette.Base06
    Base07 = applyOverride "base07" baseTheme.Palette.Base07
    Base08 = applyOverride "base08" baseTheme.Palette.Base08
    Base09 = applyOverride "base09" baseTheme.Palette.Base09
    Base0A = applyOverride "base0A" baseTheme.Palette.Base0A
    Base0B = applyOverride "base0B" baseTheme.Palette.Base0B
    Base0C = applyOverride "base0C" baseTheme.Palette.Base0C
    Base0D = applyOverride "base0D" baseTheme.Palette.Base0D
    Base0E = applyOverride "base0E" baseTheme.Palette.Base0E
    Base0F = applyOverride "base0F" baseTheme.Palette.Base0F
  }

  {
    baseTheme with
        Name = $"{baseTheme.Name} (Custom)"
        Slug = $"{baseTheme.Slug}-custom"
        Palette = customPalette
  }
