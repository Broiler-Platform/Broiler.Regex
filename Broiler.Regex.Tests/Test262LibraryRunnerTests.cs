using System;
using System.IO;
using System.Linq;
using Broiler.Regex.Test262Library;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The library runner of bundle <c>sjl-11-001</c>'s check B-2 (task 2.10 of SJL-11.2): its declared grammar, its
/// structured reader of the generated files and its judgement, over sources written here. The run over test262 itself is
/// CI's <c>test262-library</c> job, through the probe's <c>--test262</c> mode under the JIT and Native AOT; test262 is not
/// in this repository.
/// </summary>
public class Test262LibraryRunnerTests
{
    private static readonly LibraryRun Runner = new(UnicodeTables.Instance);

    // ---- The declared grammar --------------------------------------------------------------------------------------

    [Fact]
    public void A_slash_after_a_punctuator_or_a_keyword_begins_a_literal_and_after_a_name_divides()
    {
        var tokens = JavaScriptTokens.Scan("x = /a[/]b/gi; y = a / b / c; return /c/u; f(/d/)");
        var literals = tokens.Where(static t => t.Kind == TokenKind.RegularExpression).Select(static t => t.Text + "|" + t.Flags);
        Assert.Equal(["a[/]b|gi", "c|u", "d|"], literals);
    }

    [Fact]
    public void An_escape_in_the_flags_is_part_of_them_and_a_comment_is_no_literal()
    {
        var tokens = JavaScriptTokens.Scan("$DONOTEVALUATE();\n/./\\u0067;\n/*/\n// /x/\n");
        var literal = Assert.Single(tokens, static t => t.Kind == TokenKind.RegularExpression);
        Assert.Equal(".", literal.Text);
        Assert.Equal("\\u0067", literal.Flags);
    }

    [Fact]
    public void A_literal_never_runs_past_a_line_terminator()
    {
        Assert.DoesNotContain(JavaScriptTokens.Scan("(/a\n/)"), static t => t.Kind == TokenKind.RegularExpression);
        Assert.DoesNotContain(JavaScriptTokens.Scan("(/a\\\n/)"), static t => t.Kind == TokenKind.RegularExpression);
    }

    [Fact]
    public void A_string_literal_is_read_by_its_escapes()
    {
        Assert.Equal("\u231A\uFE0F\uD834\uDF06\u00A9\n\0Ax", JavaScriptTokens.StringValue("\\u231A\\uFE0F\\u{1D306}\\xA9\\n\\0\\101\\x"));
        Assert.Equal("\uD83D\uDE00", JavaScriptTokens.StringValue("\\uD83D\\uDE00"));
    }

    // ---- The structured reader -------------------------------------------------------------------------------------

    [Fact]
    public void BuildString_concatenates_the_lone_code_points_then_each_range_as_utf16()
    {
        var file = GeneratedReader.Read("""
            const matchSymbols = buildString({
              loneCodePoints: [0x00AD, 0x1D306],
              ranges: [
                [0x00DC00, 0x00DC01],
                [0x000041, 0x000042]
              ]
            });
            testPropertyEscapes(
              /^\p{Any}+$/u,
              matchSymbols,
              "\\p{Any}"
            );
            """);
        var call = Assert.IsType<PropertyEscapesCall>(Assert.Single(file.Calls));
        Assert.Equal("\u00AD\uD834\uDF06\uDC00\uDC01AB", call.Subject);
        Assert.Equal(new Literal("^\\p{Any}+$", "u"), call.RegExp);
        Assert.Equal("\\p{Any}", call.Expression);
    }

    [Fact]
    public void The_property_of_strings_helpers_and_the_assertion_are_read()
    {
        var file = GeneratedReader.Read("""
            testExtendedCharacterClass({
              regExp: /^[[0-9]--\d]+$/v,
              expression: "[[0-9]--\d]",
              matchStrings: [],
              nonMatchStrings: [
                "0",
                "6\uFE0F\u20E3"
              ],
            });
            testPropertyOfStrings({
              regExp: /^\p{Basic_Emoji}+$/v,
              expression: "\\p{Basic_Emoji}",
              matchStrings: ["\u231A"],
            });
            assert(
              !/\P{Any}/u.test(""),
              "message"
            );
            """);
        Assert.Equal(3, file.Calls.Count);
        var first = Assert.IsType<PropertyOfStringsCall>(file.Calls[0]);
        Assert.Empty(first.MatchStrings);
        Assert.Equal(["0", "6\uFE0F\u20E3"], first.NonMatchStrings!);
        Assert.Null(Assert.IsType<PropertyOfStringsCall>(file.Calls[1]).NonMatchStrings);
        Assert.Equal("", Assert.IsType<NoMatchAssertion>(file.Calls[2]).Subject);
    }

    [Fact]
    public void A_statement_the_reader_does_not_know_makes_the_file_unreadable()
    {
        var unreadable = Assert.Throws<FormatException>(() => GeneratedReader.Read("var x = 1;"));
        Assert.Contains("does not know", unreadable.Message, StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => GeneratedReader.Read("testPropertyEscapes(/a/u, missing, \"a\");"));
    }

    // ---- The judgement ---------------------------------------------------------------------------------------------

    private const string Negative = "/*---\nnegative:\n  phase: parse\n  type: SyntaxError\n---*/\n$DONOTEVALUATE();\n";
    private const string Positive = "/*---\ndescription: x\n---*/\n";

    [Fact]
    public void A_negative_literal_file_passes_when_a_literal_is_refused_and_fails_otherwise()
    {
        Assert.Equal("pass", Runner.Judge(Population.Literals, Negative + "/(?<a>.)\\k<b>/u;").Verdict);
        Assert.Equal("pass", Runner.Judge(Population.Literals, Negative + "/./gg;").Verdict);
        Assert.Equal("pass", Runner.Judge(Population.Literals, Negative + "/./\\u0067;").Verdict);
        Assert.Equal("FAIL", Runner.Judge(Population.Literals, Negative + "/(?<a>.)\\k<a>/u;").Verdict);
    }

    [Fact]
    public void A_positive_literal_file_passes_when_every_literal_is_accepted()
    {
        Assert.Equal("pass", Runner.Judge(Population.Literals, Positive + "var a = /\\k<a>(?<a>x)/; var b = /{/;").Verdict);
        var (verdict, detail) = Runner.Judge(Population.Literals, Positive + "var a = /x/; var b = /(?<a>.)\\k<b>/u;");
        Assert.Equal("FAIL", verdict);
        Assert.Contains("refused", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_with_no_literal_is_unreadable()
    {
        Assert.Equal("unreadable", Runner.Judge(Population.Literals, Negative + "/*/").Verdict);
    }

    [Fact]
    public void A_property_escape_file_passes_when_every_listed_code_point_matches()
    {
        const string File = Positive + """
            const matchSymbols = buildString({ loneCodePoints: [], ranges: [[0x000000, 0x00007F]] });
            testPropertyEscapes(/^\p{ASCII}+$/u, matchSymbols, "\\p{ASCII}");
            const nonMatchSymbols = buildString({ loneCodePoints: [], ranges: [[0x00DC00, 0x00DFFF], [0x000080, 0x00DBFF], [0x00E000, 0x10FFFF]] });
            testPropertyEscapes(/^\P{ASCII}+$/u, nonMatchSymbols, "\\P{ASCII}");
            """;
        Assert.Equal(("pass", "2 calls, 2 assertions"), Runner.Judge(Population.PropertyEscapes, File));
        var (verdict, detail) = Runner.Judge(Population.PropertyEscapes, File.Replace("0x00007F]]", "0x000080]]", StringComparison.Ordinal));
        Assert.Equal("FAIL", verdict);
        Assert.Contains("1 symbols do not match, the first U+000080 false", detail, StringComparison.Ordinal);
    }

    /// <summary>As <c>testPropertyOfStrings</c> reads them, the non-match strings are each tested only when their join matches.</summary>
    [Fact]
    public void A_set_file_fails_when_a_non_match_string_matches()
    {
        const string File = Positive + """
            testExtendedCharacterClass({
              regExp: /^[[0-9]--\d]+$/v,
              expression: "[[0-9]--\d]",
              matchStrings: [],
              nonMatchStrings: ["0", "1"],
            });
            """;
        Assert.Equal("pass", Runner.Judge(Population.UnicodeSets, File).Verdict);
        var (verdict, detail) = Runner.Judge(Population.UnicodeSets, File.Replace("--\\d]+$/v", "--[a]]+$/v", StringComparison.Ordinal));
        Assert.Equal("FAIL", verdict);
        Assert.Contains("should not match U+000030", detail, StringComparison.Ordinal);
    }

    // ---- The run and the unreadable list ---------------------------------------------------------------------------

    [Fact]
    public void The_run_fails_on_an_unlisted_unreadable_file_and_on_a_listed_file_whose_reading_is_not_the_lists()
    {
        var root = Directory.CreateTempSubdirectory("test262-library-");
        try
        {
            foreach (var (_, directory) in LibraryRun.Directories())
                Directory.CreateDirectory(Path.Combine(root.FullName, directory));
            var literals = Path.Combine(root.FullName, "test", "language", "literals", "regexp");
            File.WriteAllText(Path.Combine(literals, "good.js"), Positive + "var a = /a/u;");
            File.WriteAllText(Path.Combine(literals, "lexical.js"), Negative + "/*/");
            File.WriteAllText(Path.Combine(literals, "x_FIXTURE.js"), "garbage");

            var unlisted = Runner.Run(root.FullName, "");
            Assert.False(unlisted.Passed);
            Assert.Contains("FAIL literals test/language/literals/regexp/lexical.js: unreadable and not listed", unlisted.Report, StringComparison.Ordinal);

            var listed = Runner.Run(root.FullName, "# list\ntest/language/literals/regexp/lexical.js\tunreadable\tthe error is lexical\n");
            Assert.True(listed.Passed, listed.Report);
            Assert.Contains("pass literals test/language/literals/regexp/good.js: positive, 1 literals, 0 refused", listed.Report, StringComparison.Ordinal);
            Assert.DoesNotContain("FIXTURE", listed.Report, StringComparison.Ordinal);

            var stale = Runner.Run(root.FullName, "test/language/literals/regexp/good.js\tunreadable\twhy\ntest/language/literals/regexp/lexical.js\tunreadable\twhy\n");
            Assert.False(stale.Passed);
            Assert.Contains("listed as unreadable, but it passes", stale.Report, StringComparison.Ordinal);

            // A listed file's reading must be the one the list gives (added 2026-11-15, after the review of the tail).
            var other = Runner.Run(root.FullName, "test/language/literals/regexp/lexical.js\tFAIL\twhy\n");
            Assert.False(other.Passed);
            Assert.Contains("FAIL literals test/language/literals/regexp/lexical.js: listed as reading FAIL, but it reads unreadable", other.Report, StringComparison.Ordinal);
            Assert.Throws<FormatException>(() => LibraryRun.ReadUnreadableList("test/language/literals/regexp/lexical.js\twhy\n"));
        }
        finally
        {
            root.Delete(true);
        }
    }
}
