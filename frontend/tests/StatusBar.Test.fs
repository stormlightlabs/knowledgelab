module StatusBar.Test

open Fable.Jester
open StatusBar

Jest.describe (
  "StatusBar module",
  fun () ->
    Jest.describe (
      "countWords",
      fun () ->
        Jest.test (
          "counts words in simple sentence",
          fun () ->
            let result = countWords "Hello world this is a test"
            Jest.expect(result).toEqual 6
        )

        Jest.test (
          "handles empty string",
          fun () ->
            let result = countWords ""
            Jest.expect(result).toEqual 0
        )

        Jest.test (
          "handles whitespace-only string",
          fun () ->
            let result = countWords "   \t  \n  "
            Jest.expect(result).toEqual 0
        )

        Jest.test (
          "handles multiple spaces between words",
          fun () ->
            let result = countWords "word1    word2  \t  word3"
            Jest.expect(result).toEqual 3
        )

        Jest.test (
          "handles newlines as word separators",
          fun () ->
            let result = countWords "line1\nline2\nline3"
            Jest.expect(result).toEqual 3
        )

        Jest.test (
          "counts words in markdown with special characters",
          fun () ->
            let result = countWords "**bold** _italic_ `code` [link](url)"
            Jest.expect(result).toEqual 4
        )

        Jest.test (
          "handles single word",
          fun () ->
            let result = countWords "word"
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "handles text with tabs",
          fun () ->
            let result = countWords "word1\tword2\tword3"
            Jest.expect(result).toEqual 3
        )
    )

    Jest.describe (
      "countCharacters",
      fun () ->
        Jest.test (
          "counts characters including spaces",
          fun () ->
            let result = countCharacters "Hello world"
            Jest.expect(result).toEqual 11
        )

        Jest.test (
          "handles empty string",
          fun () ->
            let result = countCharacters ""
            Jest.expect(result).toEqual 0
        )

        Jest.test (
          "counts newlines as characters",
          fun () ->
            let result = countCharacters "line1\nline2"
            Jest.expect(result).toEqual 11
        )

        Jest.test (
          "counts special characters",
          fun () ->
            let result = countCharacters "!@#$%^&*()"
            Jest.expect(result).toEqual 10
        )
    )

    Jest.describe (
      "countCharactersNoSpaces",
      fun () ->
        Jest.test (
          "counts characters excluding whitespace",
          fun () ->
            let result = countCharactersNoSpaces "Hello world"
            Jest.expect(result).toEqual 10
        )

        Jest.test (
          "handles empty string",
          fun () ->
            let result = countCharactersNoSpaces ""
            Jest.expect(result).toEqual 0
        )

        Jest.test (
          "excludes newlines and tabs",
          fun () ->
            let result = countCharactersNoSpaces "abc\n\tdef"
            Jest.expect(result).toEqual 6
        )
    )

    Jest.describe (
      "calculateLineNumber",
      fun () ->
        Jest.test (
          "returns 1 for position 0",
          fun () ->
            let result = calculateLineNumber "Hello world" 0
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 1 for empty content",
          fun () ->
            let result = calculateLineNumber "" 0
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 1 for negative position",
          fun () ->
            let result = calculateLineNumber "Hello" -1
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 1 for position in first line",
          fun () ->
            let result = calculateLineNumber "Hello\nWorld" 3
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 2 for position after first newline",
          fun () ->
            let result = calculateLineNumber "Hello\nWorld" 6
            Jest.expect(result).toEqual 2
        )

        Jest.test (
          "returns 2 for position at first newline",
          fun () ->
            let result = calculateLineNumber "Hello\nWorld" 5
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "handles multiple lines correctly",
          fun () ->
            let content = "Line 1\nLine 2\nLine 3\nLine 4"
            Jest.expect(calculateLineNumber content 0).toEqual 1
            Jest.expect(calculateLineNumber content 7).toEqual 2
            Jest.expect(calculateLineNumber content 14).toEqual 3
            Jest.expect(calculateLineNumber content 21).toEqual 4
        )

        Jest.test (
          "handles position at end of content",
          fun () ->
            let content = "Line 1\nLine 2"
            let result = calculateLineNumber content content.Length
            Jest.expect(result).toEqual 2
        )

        Jest.test (
          "handles position beyond content length",
          fun () ->
            let content = "Hello"
            let result = calculateLineNumber content 100
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "handles content with empty lines",
          fun () ->
            let content = "Line 1\n\nLine 3"
            Jest.expect(calculateLineNumber content 7).toEqual 2
            Jest.expect(calculateLineNumber content 8).toEqual 3
        )
    )

    Jest.describe (
      "calculateColumnNumber",
      fun () ->
        Jest.test (
          "returns 1 for position 0",
          fun () ->
            let result = calculateColumnNumber "Hello world" 0
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 1 for empty content",
          fun () ->
            let result = calculateColumnNumber "" 0
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "returns 1 for negative position",
          fun () ->
            let result = calculateColumnNumber "Hello" -1
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "calculates column in first line correctly",
          fun () ->
            let result = calculateColumnNumber "Hello world" 6
            Jest.expect(result).toEqual 7
        )

        Jest.test (
          "calculates column after newline correctly",
          fun () ->
            let result = calculateColumnNumber "Hello\nWorld" 6
            Jest.expect(result).toEqual 1
        )

        Jest.test (
          "calculates column in middle of second line",
          fun () ->
            let result = calculateColumnNumber "Hello\nWorld" 9
            Jest.expect(result).toEqual 4
        )

        Jest.test (
          "handles multiple lines",
          fun () ->
            let content = "Line 1\nLine 2\nLine 3"
            Jest.expect(calculateColumnNumber content 0).toEqual 1
            Jest.expect(calculateColumnNumber content 6).toEqual 7
            Jest.expect(calculateColumnNumber content 7).toEqual 1
            Jest.expect(calculateColumnNumber content 10).toEqual 4
        )

        Jest.test (
          "handles position at end of line",
          fun () ->
            let content = "Hello\nWorld"
            let result = calculateColumnNumber content 5
            Jest.expect(result).toEqual 6
        )

        Jest.test (
          "handles position beyond content length",
          fun () ->
            let content = "Hello"
            let result = calculateColumnNumber content 100
            Jest.expect(result).toEqual 6
        )

        Jest.test (
          "handles empty lines",
          fun () ->
            let content = "Line 1\n\nLine 3"
            Jest.expect(calculateColumnNumber content 7).toEqual 1
            Jest.expect(calculateColumnNumber content 8).toEqual 1
        )
    )

    Jest.describe (
      "calculateStats",
      fun () ->
        Jest.test (
          "calculates all stats correctly for simple content",
          fun () ->
            let content = "Hello world"
            let stats = calculateStats content (Some 6) false
            Jest.expect(stats.WordCount).toEqual 2
            Jest.expect(stats.CharCount).toEqual 11
            Jest.expect(stats.LineNumber).toEqual 1
            Jest.expect(stats.ColumnNumber).toEqual 7
            Jest.expect(stats.IsSaved).toEqual true
        )

        Jest.test (
          "handles dirty flag correctly",
          fun () ->
            let stats = calculateStats "test" (Some 0) true
            Jest.expect(stats.IsSaved).toEqual false
        )

        Jest.test (
          "handles None cursor position",
          fun () ->
            let stats = calculateStats "Hello world" None false
            Jest.expect(stats.LineNumber).toEqual 1
            Jest.expect(stats.ColumnNumber).toEqual 1
        )

        Jest.test (
          "calculates stats for multi-line content",
          fun () ->
            let content = "Line 1\nLine 2\nLine 3"
            let stats = calculateStats content (Some 14) false
            Jest.expect(stats.WordCount).toEqual 6
            Jest.expect(stats.CharCount).toEqual 20
            Jest.expect(stats.LineNumber).toEqual 3
            Jest.expect(stats.ColumnNumber).toEqual 1
        )

        Jest.test (
          "handles empty content",
          fun () ->
            let stats = calculateStats "" (Some 0) false
            Jest.expect(stats.WordCount).toEqual 0
            Jest.expect(stats.CharCount).toEqual 0
            Jest.expect(stats.LineNumber).toEqual 1
            Jest.expect(stats.ColumnNumber).toEqual 1
        )

        Jest.test (
          "handles large markdown document",
          fun () ->
            let content =
              "# Heading\n\nThis is a paragraph with **bold** and _italic_ text.\n\n- List item 1\n- List item 2\n\n```code\nlet x = 1\n```"

            let stats = calculateStats content (Some 50) false
            Jest.expect(stats.WordCount).toBeGreaterThan 0
            Jest.expect(stats.CharCount).toBeGreaterThan 0
            Jest.expect(stats.LineNumber).toBeGreaterThanOrEqual 1
        )

        Jest.test (
          "handles content with special unicode characters",
          fun () ->
            let content = "Hello 世界 🌍"
            let stats = calculateStats content (Some 0) false
            Jest.expect(stats.WordCount).toEqual 3
            Jest.expect(stats.CharCount).toBeGreaterThan 0
        )

        Jest.test (
          "handles cursor at various positions in multiline content",
          fun () ->
            let content = "First line\nSecond line\nThird line"

            let stats1 = calculateStats content (Some 0) false
            Jest.expect(stats1.LineNumber).toEqual 1
            Jest.expect(stats1.ColumnNumber).toEqual 1

            let stats2 = calculateStats content (Some 11) false
            Jest.expect(stats2.LineNumber).toEqual 2
            Jest.expect(stats2.ColumnNumber).toEqual 1

            let stats3 = calculateStats content (Some 23) false
            Jest.expect(stats3.LineNumber).toEqual 3
            Jest.expect(stats3.ColumnNumber).toEqual 1
        )
    )
)
