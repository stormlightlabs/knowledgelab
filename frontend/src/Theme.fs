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
      Some($"#{trimmed}")

let private setCssVar (style : obj) (name : string) (value : string) =
  match normalizeHexColor value with
  | Some normalized -> style?setProperty (name, normalized)
  | None -> ()

/// Applies a base16 theme by setting CSS variables on the document root
let ApplyTheme (theme : Base16Theme) : unit =
  let root = document.documentElement
  let style = root?style

  let paletteEntries = [
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

  paletteEntries
  |> List.iter (fun (suffix, value) ->
    setCssVar style (sprintf "--base%s" suffix) value
    setCssVar style (sprintf "--color-base%s" suffix) value)

  printfn "Applied theme: %s (%s)" theme.Name theme.Variant
