/// Shiki syntax highlighter bindings for code block highlighting
module Syntax

open Fable.Core

/// Options for the codeToHtml function
type CodeToHtmlOptions = { lang : string; theme : string }

/// Options for the codeToTokens function
type CodeToTokensOptions = { lang : string; theme : string }

/// Options for the codeToHast function
type CodeToHastOptions = { lang : string; theme : string }

/// Token information returned by codeToTokens
type Token = {
  content : string
  color : string option
  fontStyle : int option
}

/// Token line containing an array of tokens
type TokenLine = Token array

/// Result of codeToTokens containing token lines
type CodeToTokensResult = { tokens : TokenLine array }

/// HAST (Hypertext Abstract Syntax Tree) node
type HastNode = obj

/// Options for creating a highlighter instance
type HighlighterOptions = { themes : string array; langs : string array }

/// Highlighter instance with methods for highlighting code
[<AllowNullLiteral>]
type IHighlighter =
  /// Highlight code to HTML using the highlighter instance
  abstract codeToHtml : code : string * options : CodeToHtmlOptions -> string

  /// Convert code to tokens using the highlighter instance
  abstract codeToTokens : code : string * options : CodeToTokensOptions -> CodeToTokensResult

  /// Convert code to HAST using the highlighter instance
  abstract codeToHast : code : string * options : CodeToHastOptions -> HastNode

  /// Load additional theme dynamically
  abstract loadTheme : theme : string -> JS.Promise<unit>

  /// Load additional language dynamically
  abstract loadLanguage : lang : string -> JS.Promise<unit>

/// Quick conversion of code to highlighted HTML
[<Import("codeToHtml", from = "shiki")>]
let codeToHtml (code : string) (options : CodeToHtmlOptions) : JS.Promise<string> = jsNative

/// Convert code to intermediate token data for custom rendering
[<Import("codeToTokens", from = "shiki")>]
let codeToTokens (code : string) (options : CodeToTokensOptions) : JS.Promise<CodeToTokensResult> = jsNative

/// Convert code to HAST (Hypertext Abstract Syntax Tree) for advanced processing
[<Import("codeToHast", from = "shiki")>]
let codeToHast (code : string) (options : CodeToHastOptions) : JS.Promise<HastNode> = jsNative

/// Create a reusable highlighter instance for synchronous operations.
/// The highlighter instance should be a long-lived singleton for performance.
[<Import("createHighlighter", from = "shiki")>]
let createHighlighter (options : HighlighterOptions) : JS.Promise<IHighlighter> = jsNative

/// Helper function to create options for codeToHtml
let htmlOptions lang theme = { lang = lang; theme = theme }

/// Helper function to create options for codeToTokens
let tokenOptions lang theme = { lang = lang; theme = theme }

/// Helper function to create options for codeToHast
let hastOptions lang theme = { lang = lang; theme = theme }

/// Helper function to create highlighter options
let highlighterOptions themes langs = { themes = themes; langs = langs }
