module SyntaxTests

open Fable.Jester
open Syntax

Jest.describe (
  "Syntax.htmlOptions",
  fun () ->
    Jest.test (
      "creates CodeToHtmlOptions with correct lang and theme",
      fun () ->
        let options = htmlOptions "javascript" "vitesse-dark"
        Jest.expect(options.lang).toEqual "javascript"
        Jest.expect(options.theme).toEqual "vitesse-dark"
    )

    Jest.test (
      "handles different language identifiers",
      fun () ->
        let options = htmlOptions "typescript" "nord"
        Jest.expect(options.lang).toEqual "typescript"
        Jest.expect(options.theme).toEqual "nord"
    )

    Jest.test (
      "creates options for various languages",
      fun () ->
        let jsOpts = htmlOptions "javascript" "nord"
        let pyOpts = htmlOptions "python" "vitesse-dark"
        let tsOpts = htmlOptions "typescript" "catppuccin-mocha"

        Jest.expect(jsOpts.lang).toEqual "javascript"
        Jest.expect(pyOpts.lang).toEqual "python"
        Jest.expect(tsOpts.lang).toEqual "typescript"

        Jest.expect(jsOpts.theme).toEqual "nord"
        Jest.expect(pyOpts.theme).toEqual "vitesse-dark"
        Jest.expect(tsOpts.theme).toEqual "catppuccin-mocha"
    )
)

Jest.describe (
  "Syntax.tokenOptions",
  fun () ->
    Jest.test (
      "creates CodeToTokensOptions with correct lang and theme",
      fun () ->
        let options = tokenOptions "python" "min-dark"
        Jest.expect(options.lang).toEqual "python"
        Jest.expect(options.theme).toEqual "min-dark"
    )

    Jest.test (
      "handles different language and theme combinations",
      fun () ->
        let opt1 = tokenOptions "rust" "nord"
        let opt2 = tokenOptions "go" "vitesse-dark"

        Jest.expect(opt1.lang).toEqual "rust"
        Jest.expect(opt1.theme).toEqual "nord"
        Jest.expect(opt2.lang).toEqual "go"
        Jest.expect(opt2.theme).toEqual "vitesse-dark"
    )
)

Jest.describe (
  "Syntax.hastOptions",
  fun () ->
    Jest.test (
      "creates CodeToHastOptions with correct lang and theme",
      fun () ->
        let options = hastOptions "css" "catppuccin-mocha"
        Jest.expect(options.lang).toEqual "css"
        Jest.expect(options.theme).toEqual "catppuccin-mocha"
    )

    Jest.test (
      "creates options for markup languages",
      fun () ->
        let htmlOpts = hastOptions "html" "nord"
        let xmlOpts = hastOptions "xml" "vitesse-dark"

        Jest.expect(htmlOpts.lang).toEqual "html"
        Jest.expect(xmlOpts.lang).toEqual "xml"
    )
)

Jest.describe (
  "Syntax.highlighterOptions",
  fun () ->
    Jest.test (
      "creates HighlighterOptions with correct themes and langs",
      fun () ->
        let themes = [| "nord"; "vitesse-dark" |]
        let langs = [| "javascript"; "typescript"; "python" |]
        let options = highlighterOptions themes langs
        Jest.expect(options.themes).toEqual themes
        Jest.expect(options.langs).toEqual langs
    )

    Jest.test (
      "handles single theme and language",
      fun () ->
        let options = highlighterOptions [| "nord" |] [| "javascript" |]
        Jest.expect(options.themes.Length).toEqual 1
        Jest.expect(options.langs.Length).toEqual 1
        Jest.expect(options.themes.[0]).toEqual "nord"
        Jest.expect(options.langs.[0]).toEqual "javascript"
    )

    Jest.test (
      "handles multiple themes and languages",
      fun () ->
        let themes = [| "nord"; "vitesse-dark"; "catppuccin-mocha"; "min-dark" |]
        let langs = [| "javascript"; "typescript"; "python"; "rust"; "go" |]
        let options = highlighterOptions themes langs

        Jest.expect(options.themes.Length).toEqual 4
        Jest.expect(options.langs.Length).toEqual 5
    )

    Jest.test (
      "preserves theme and language order",
      fun () ->
        let themes = [| "catppuccin-mocha"; "nord"; "vitesse-dark" |]
        let langs = [| "python"; "javascript"; "typescript" |]
        let options = highlighterOptions themes langs

        Jest.expect(options.themes.[0]).toEqual "catppuccin-mocha"
        Jest.expect(options.themes.[1]).toEqual "nord"
        Jest.expect(options.themes.[2]).toEqual "vitesse-dark"

        Jest.expect(options.langs.[0]).toEqual "python"
        Jest.expect(options.langs.[1]).toEqual "javascript"
        Jest.expect(options.langs.[2]).toEqual "typescript"
    )
)

Jest.describe (
  "Syntax type definitions",
  fun () ->
    Jest.test (
      "CodeToHtmlOptions has required fields",
      fun () ->
        let options : CodeToHtmlOptions = { lang = "js"; theme = "nord" }
        Jest.expect(options.lang).toBeTruthy ()
        Jest.expect(options.theme).toBeTruthy ()
    )

    Jest.test (
      "CodeToTokensOptions has required fields",
      fun () ->
        let options : CodeToTokensOptions = { lang = "ts"; theme = "vitesse-dark" }
        Jest.expect(options.lang).toBeTruthy ()
        Jest.expect(options.theme).toBeTruthy ()
    )

    Jest.test (
      "CodeToHastOptions has required fields",
      fun () ->
        let options : CodeToHastOptions = { lang = "html"; theme = "min-dark" }
        Jest.expect(options.lang).toBeTruthy ()
        Jest.expect(options.theme).toBeTruthy ()
    )

    Jest.test (
      "HighlighterOptions has required fields",
      fun () ->
        let options : HighlighterOptions = { themes = [| "nord" |]; langs = [| "javascript" |] }

        Jest.expect(options.themes).toBeTruthy ()
        Jest.expect(options.langs).toBeTruthy ()
    )

    Jest.test (
      "Token type has expected structure",
      fun () ->
        let token : Token = {
          content = "const"
          color = Some "#d8dee9"
          fontStyle = Some 0
        }

        Jest.expect(token.content).toEqual "const"
        Jest.expect(token.color).toEqual (Some "#d8dee9")
        Jest.expect(token.fontStyle).toEqual (Some 0)
    )

    Jest.test (
      "Token type allows None for optional fields",
      fun () ->
        let token : Token = { content = "text"; color = None; fontStyle = None }

        Jest.expect(token.content).toEqual "text"
        Jest.expect(token.color).toEqual None
        Jest.expect(token.fontStyle).toEqual None
    )
)
