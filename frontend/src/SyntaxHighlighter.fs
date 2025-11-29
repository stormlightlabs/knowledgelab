/// Syntax highlighting post-processor for markdown preview
/// Finds code blocks in rendered HTML and applies Shiki highlighting
module SyntaxHighlighter

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

/// Represents a code block found in HTML
type CodeBlock = {
  Element : Element
  Code : string
  Language : string option
}

[<Emit("new DOMParser()")>]
let private createDOMParser () : obj = jsNative

[<Emit("$0.parseFromString($1, $2)")>]
let private parseFromString (parser : obj) (html : string) (contentType : string) : Document =
  jsNative

/// Extracts code blocks from HTML content
let private findCodeBlocks (html : string) : CodeBlock list =
  let parser = createDOMParser ()
  let doc = parseFromString parser html "text/html"
  let codeElements = doc.querySelectorAll "pre > code"

  let blocks = ResizeArray<CodeBlock>()

  for i in 0 .. int codeElements.length - 1 do
    let element = codeElements.[i]
    let code = element.textContent

    let language =
      element?className
      |> string
      |> fun className ->
          if className.Contains "language-" then
            className.Split [| ' ' |]
            |> Array.tryFind (fun c -> c.StartsWith "language-")
            |> Option.map (fun c -> c.Replace("language-", ""))
          else
            None

    blocks.Add {
      Element = element
      Code = if isNull code then "" else code
      Language = language
    }

  List.ofSeq blocks

/// Highlights a single code block using Shiki
let private highlightBlock (block : CodeBlock) : JS.Promise<string> = promise {
  try
    let lang = block.Language |> Option.defaultValue "plaintext"

    let! html =
      Syntax.codeToHtml block.Code {
        Syntax.CodeToHtmlOptions.lang = lang
        theme = "vitesse-dark"
      }

    return html
  with ex ->
    Browser.Dom.console.error ("Failed to highlight code block:", ex)
    return $"<pre><code>{block.Code}</code></pre>"
}

/// Applies Shiki syntax highlighting to all code blocks in HTML
let highlightCodeBlocks (html : string) : JS.Promise<string> = promise {
  try
    let blocks = findCodeBlocks html

    if blocks.IsEmpty then
      return html
    else
      let parser = createDOMParser ()
      let doc = parseFromString parser html "text/html"

      let! highlightedBlocks = blocks |> List.map highlightBlock |> Promise.all

      for i in 0 .. blocks.Length - 1 do
        let block = blocks.[i]
        let highlightedHtml = highlightedBlocks.[i]

        let tempDiv = Browser.Dom.document.createElement "div"
        tempDiv.innerHTML <- highlightedHtml

        let newPre = tempDiv.querySelector "pre"
        let preElement = block.Element.parentElement

        if
          not (isNull newPre)
          && not (isNull preElement)
          && not (isNull preElement.parentElement)
        then
          preElement.parentElement.replaceChild (newPre, preElement) |> ignore

      return doc.body.innerHTML
  with ex ->
    Browser.Dom.console.error ("Failed to highlight code blocks:", ex)
    return html
}

/// Highlights code blocks with a specific theme
let highlightCodeBlocksWithTheme (html : string) (theme : string) : JS.Promise<string> = promise {
  try
    let blocks = findCodeBlocks html

    if blocks.IsEmpty then
      return html
    else
      let parser = createDOMParser ()
      let doc = parseFromString parser html "text/html"

      let! highlightedBlocks =
        blocks
        |> List.map (fun block -> promise {
          try
            let lang = block.Language |> Option.defaultValue "plaintext"

            let! html =
              Syntax.codeToHtml block.Code { Syntax.CodeToHtmlOptions.lang = lang; theme = theme }

            return html
          with ex ->
            Browser.Dom.console.error ("Failed to highlight code block:", ex)
            return $"<pre><code>{block.Code}</code></pre>"
        })
        |> Promise.all

      for i in 0 .. blocks.Length - 1 do
        let block = blocks.[i]
        let highlightedHtml = highlightedBlocks.[i]

        let tempDiv = Browser.Dom.document.createElement "div"
        tempDiv.innerHTML <- highlightedHtml

        let newPre = tempDiv.querySelector "pre"
        let preElement = block.Element.parentElement

        if
          not (isNull newPre)
          && not (isNull preElement)
          && not (isNull preElement.parentElement)
        then
          preElement.parentElement.replaceChild (newPre, preElement) |> ignore

      return doc.body.innerHTML
  with ex ->
    Browser.Dom.console.error ("Failed to highlight code blocks:", ex)
    return html
}
