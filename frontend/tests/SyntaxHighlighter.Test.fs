module SyntaxHighlighterTests

open Fable.Jester
open SyntaxHighlighter

Jest.describe (
  "SyntaxHighlighter.highlightCodeBlocks",
  fun () ->
    Jest.test (
      "returns HTML unchanged when no code blocks present",
      promise {
        let html = "<p>Hello world</p><div>No code here</div>"
        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "highlights JavaScript code block",
      promise {
        let html = """<pre><code class="language-javascript">const x = 42;</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "const"
        Jest.expect(result).toContain "42"
      }
    )

    Jest.test (
      "highlights Python code block",
      promise {
        let html =
          """<pre><code class="language-python">def hello():
    print("world")</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "def"
        Jest.expect(result).toContain "hello"
      }
    )

    Jest.test (
      "highlights TypeScript code block",
      promise {
        let html =
          """<pre><code class="language-typescript">interface User {
  name: string;
  age: number;
}</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "interface"
        Jest.expect(result).toContain "User"
      }
    )

    Jest.test (
      "handles code block without language class",
      promise {
        let html = """<pre><code>plain text code</code></pre>"""
        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "plain text code"
      }
    )

    Jest.test (
      "handles empty code block",
      promise {
        let html = """<pre><code class="language-javascript"></code></pre>"""
        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "highlights multiple code blocks",
      promise {
        let html =
          """<p>First example:</p>
<pre><code class="language-javascript">const x = 1;</code></pre>
<p>Second example:</p>
<pre><code class="language-python">y = 2</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "const"
        Jest.expect(result).toContain "="
      }
    )

    Jest.test (
      "handles code blocks with special characters",
      promise {
        let html =
          """<pre><code class="language-javascript">const str = "hello &amp; goodbye";</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "preserves surrounding HTML",
      promise {
        let html =
          """<h1>Title</h1>
<p>Some text</p>
<pre><code class="language-javascript">const x = 1;</code></pre>
<p>More text</p>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toContain "Title"
        Jest.expect(result).toContain "Some text"
        Jest.expect(result).toContain "More text"
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "handles code blocks with common languages",
      promise {
        let languages = [ "rust"; "go"; "java"; "cpp"; "c"; "ruby"; "php"; "swift" ]

        for lang in languages do
          let html = $"""<pre><code class="language-{lang}">code here</code></pre>"""
          let! result = highlightCodeBlocks html
          Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "handles code blocks with markup languages",
      promise {
        let html =
          """<pre><code class="language-html">&lt;div class="test"&gt;Hello&lt;/div&gt;</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "div"
      }
    )

    Jest.test (
      "handles JSON code blocks",
      promise {
        let html =
          """<pre><code class="language-json">{
  "name": "test",
  "value": 123
}</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "name"
        Jest.expect(result).toContain "test"
      }
    )

    Jest.test (
      "handles YAML code blocks",
      promise {
        let html =
          """<pre><code class="language-yaml">name: test
value: 123</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "name"
      }
    )

    Jest.test (
      "handles shell/bash code blocks",
      promise {
        let html =
          """<pre><code class="language-bash">echo "Hello World"
ls -la</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "echo"
      }
    )

    Jest.test (
      "handles SQL code blocks",
      promise {
        let html =
          """<pre><code class="language-sql">SELECT * FROM users WHERE id = 1;</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "SELECT"
      }
    )

    Jest.test (
      "handles nested HTML structure",
      promise {
        let html =
          """<div class="container">
  <article>
    <section>
      <pre><code class="language-javascript">const x = 42;</code></pre>
    </section>
  </article>
</div>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toContain "container"
        Jest.expect(result).toContain "article"
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "handles code blocks with inline comments",
      promise {
        let html =
          """<pre><code class="language-javascript">// This is a comment
const x = 42; // inline comment</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "comment"
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "handles code blocks with block comments",
      promise {
        let html =
          """<pre><code class="language-javascript">/* Multi-line
   comment */
const x = 42;</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "Multi-line"
      }
    )
)

Jest.describe (
  "SyntaxHighlighter.highlightCodeBlocksWithTheme",
  fun () ->
    Jest.test (
      "highlights with nord theme",
      promise {
        let html = """<pre><code class="language-javascript">const x = 42;</code></pre>"""

        let! result = highlightCodeBlocksWithTheme html "nord"
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "highlights with vitesse-dark theme",
      promise {
        let html = """<pre><code class="language-javascript">const x = 42;</code></pre>"""

        let! result = highlightCodeBlocksWithTheme html "vitesse-dark"
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "const"
      }
    )

    Jest.test (
      "highlights with different themes for same code",
      promise {
        let html =
          """<pre><code class="language-python">def hello():
    pass</code></pre>"""

        let! result1 = highlightCodeBlocksWithTheme html "nord"
        let! result2 = highlightCodeBlocksWithTheme html "vitesse-dark"

        Jest.expect(result1).toBeTruthy ()
        Jest.expect(result2).toBeTruthy ()
        Jest.expect(result1).toContain "def"
        Jest.expect(result2).toContain "def"
      }
    )
)

Jest.describe (
  "SyntaxHighlighter edge cases",
  fun () ->
    Jest.test (
      "handles malformed HTML gracefully",
      promise {
        let html = """<pre><code class="language-javascript">const x = 42;"""
        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "handles unknown language gracefully",
      promise {
        let html = """<pre><code class="language-unknownlang">some code here</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "some code here"
      }
    )

    Jest.test (
      "handles empty HTML",
      promise {
        let html = ""
        let! result = highlightCodeBlocks html
        Jest.expect(result).toEqual ""
      }
    )

    Jest.test (
      "handles whitespace-only code block",
      promise {
        let html =
          """<pre><code class="language-javascript">

  </code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "handles code block with only newlines",
      promise {
        let html =
          """<pre><code class="language-javascript">


</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "handles very long code block",
      promise {
        let longCode = String.replicate 1000 "const x = 42;\n"
        let html = $"""<pre><code class="language-javascript">{longCode}</code></pre>"""
        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
      }
    )

    Jest.test (
      "handles code with unicode characters",
      promise {
        let html =
          """<pre><code class="language-javascript">const greeting = "你好世界";</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "你好世界"
      }
    )

    Jest.test (
      "handles code with emojis",
      promise {
        let html =
          """<pre><code class="language-javascript">const emoji = "🚀💻🎉";</code></pre>"""

        let! result = highlightCodeBlocks html
        Jest.expect(result).toBeTruthy ()
        Jest.expect(result).toContain "🚀💻🎉"
      }
    )
)
