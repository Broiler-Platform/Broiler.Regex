using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Broiler.Regex.VectorsRunner;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Bundle <c>sjl-11-001</c>'s part D, the vectors at library level (task 2.12 of SJL-11.2): the pinned copy of Broiler.JS's
/// <c>conformance/vectors/regexp/</c> under <c>tests/Vectors/</c>, its pin checked before every run, and the runner of
/// <c>Vectors/</c>, which maps each op to the library's public surface. MET iff every normative vector whose op the
/// library implements gives its <c>out</c>, every status vector its status, every impl-defined vector is reported with
/// the library's answer, the JIT's and the Native AOT probe's reports are identical, and every op the library does not
/// implement, the object model's, is listed by name. The probe's <c>--vectors</c> mode runs the same code
/// (<see cref="VectorsProbeTests"/>); CI compares its two reports.
/// </summary>
public class VectorsRunTests
{
    internal static string VectorsRoot => Path.Combine(AdversarialCorpusTests.Root, "tests", "Vectors");

    private static readonly string[] PinnedFiles =
    [
        "budget.vectors", "case-insensitive.vectors", "last-index.vectors", "literals.vectors", "matching.vectors",
        "properties.vectors", "syntax.vectors", "text.vectors",
    ];

    // ---- The pin ---------------------------------------------------------------------------------------------------

    [Fact]
    public void The_copy_is_the_one_the_pin_names()
    {
        var corpus = VectorCorpus.Load(VectorsRoot);
        Assert.Equal("b2c2e2243a086285d806866a64b83297791a8ec5", corpus.Pin.Commit);
        Assert.Equal(32, corpus.Pin.Revision);
        Assert.Equal("conformance/vectors/regexp/", corpus.Pin.Source);
        Assert.Equal(PinnedFiles, corpus.Pin.Files.Select(static file => file.Name));
        Assert.Empty(corpus.Pin.Check(Path.Combine(VectorsRoot, "regexp")));
        Assert.Equal(1420, corpus.Records.Count);
        Assert.Equal(corpus.Records.Count, corpus.Records.Select(static vector => vector.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(corpus.Records, static vector => Assert.Equal("regexp", vector["area"]));
    }

    /// <summary>The control the rule's population asks of the pin: one changed byte refuses the run, and nothing is read.</summary>
    [Fact]
    public void A_changed_byte_refuses_the_run()
    {
        using var copy = new ScratchCopy();
        var path = Path.Combine(copy.Root, "regexp", "matching.vectors");
        var bytes = File.ReadAllBytes(path);
        var at = Array.IndexOf(bytes, (byte)'a', bytes.Length / 2);
        bytes[at] = (byte)'b';
        File.WriteAllBytes(path, bytes);

        var refusal = Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root));
        var problem = Assert.Single(refusal.Problems);
        Assert.StartsWith("matching.vectors: SHA-256 ", problem, StringComparison.Ordinal);
        Assert.EndsWith("where the pin gives 348d7c0d6806ab2c51fc0b5f313d93f8b52a894db31972932331825a0a88650a", problem, StringComparison.Ordinal);

        // Restored, the copy runs again.
        bytes[at] = (byte)'a';
        File.WriteAllBytes(path, bytes);
        Assert.Equal(1420, VectorCorpus.Load(copy.Root).Records.Count);
    }

    [Fact]
    public void A_missing_longer_or_unnamed_file_refuses_the_run()
    {
        using (var copy = new ScratchCopy())
        {
            File.Delete(Path.Combine(copy.Root, "regexp", "text.vectors"));
            Assert.Equal(["text.vectors: missing"], Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root)).Problems);
        }

        using (var copy = new ScratchCopy())
        {
            File.AppendAllText(Path.Combine(copy.Root, "regexp", "budget.vectors"), "\n");
            var problems = Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root)).Problems;
            Assert.Equal(2, problems.Count);
            Assert.Equal("budget.vectors: 44503 bytes, where the pin gives 44502", problems[0]);
        }

        using (var copy = new ScratchCopy())
        {
            File.WriteAllText(Path.Combine(copy.Root, "regexp", "extra.vectors"), "schema: 1\n");
            Assert.Equal(["extra.vectors: a file the pin does not name"], Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root)).Problems);
        }

        using (var copy = new ScratchCopy())
        {
            File.Delete(Path.Combine(copy.Root, "regexp.pin"));
            Assert.EndsWith("regexp.pin: no pin file", Assert.Single(Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root)).Problems), StringComparison.Ordinal);
        }

        using (var copy = new ScratchCopy())
        {
            var pin = Path.Combine(copy.Root, "regexp.pin");
            File.WriteAllText(pin, File.ReadAllText(pin).Replace("broiler-js-commit: b2c2e224", "broiler-js-commit: b2c2e22", StringComparison.Ordinal));
            Assert.Contains("no commit of forty hexadecimal digits", Assert.Single(Assert.Throws<VectorPinException>(() => VectorCorpus.Load(copy.Root)).Problems), StringComparison.Ordinal);
        }
    }

    // ---- The format ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("schema: 1\n\nid: x\nbogus: 1\n", "the key bogus is not the schema's")]
    [InlineData("schema: 1\n\nid: x\nid: y\n", "the key id twice in one record")]
    [InlineData("# a comment\nid: x\n", "is not \"schema: 1\"")]
    [InlineData("schema: 1\n\nid: x\n", "a record without area")]
    [InlineData("schema: 1\r\n", "a CR")]
    public void The_reader_refuses_what_the_schema_does_not_have(string text, string message)
        => Assert.Contains(message, Assert.Throws<FormatException>(() => VectorFile.Read("f.vectors", text)).Message, StringComparison.Ordinal);

    [Fact]
    public void Values_and_alternatives_read_and_render_as_section_3_writes_them()
    {
        var inputs = VectorNotation.Inputs("str:\"a\\\"b\\\\c\\u2028\\uD834\" int:-3 str:\"\"");
        Assert.Equal("a\"b\\c\u2028\uD834", inputs[0].Str);
        Assert.Equal(-3, inputs[1].Int);
        Assert.Equal("", inputs[2].Str);
        Assert.Equal("str:\"a\\\"b\\\\c\\u2028\\uD834\"", VectorNotation.Str(inputs[0].Str));
        Assert.Equal("f64:0xBFF0000000000000", VectorNotation.F64(-1));
        Assert.Equal("f64:0x0000000000000000", VectorNotation.F64(0));

        Assert.Equal(["value: str:\"x | y\"", "value: str:\"z\""], VectorNotation.Alternatives("value: str:\"x | y\" | value: str:\"z\""));
        Assert.Equal(["status: exhausted"], VectorNotation.Alternatives("status: exhausted"));
        Assert.Equal(["error: SyntaxError", "value: bool:true"], VectorNotation.Alternatives("error: SyntaxError | value: bool:true"));
        Assert.Throws<FormatException>(() => VectorNotation.Alternatives("value: bool:true |value: bool:false"));
    }

    // ---- The op mapping --------------------------------------------------------------------------------------------

    private static string Call(string op, string inputs) =>
        new VectorLibrary(UnicodeTables.Instance).Call(Vector(op, inputs, "value: bool:true", "normative"));

    private static VectorRecord Vector(string op, string inputs, string output, string cls, string? perEngine = null)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "regexp/test/0001", ["area"] = "regexp", ["op"] = op, ["in"] = inputs, ["out"] = output, ["class"] = cls,
            ["prov"] = "ref:regexpref@1", ["script"] = "0", ["since"] = "31",
        };
        if (perEngine is not null)
        {
            fields["per-engine.broiler-js"] = perEngine;
            fields["per-engine.broiler-vm"] = perEngine;
        }

        return new VectorRecord("test.vectors", fields);
    }

    [Theory]
    [InlineData("RegExpInitialize", "str:\"a(\" str:\"\"", "error: SyntaxError")]
    [InlineData("RegExpInitialize", "str:\"a\" str:\"gg\"", "error: SyntaxError")]
    [InlineData("RegExpInitialize", "str:\"a\" str:\"uv\"", "error: SyntaxError")]
    [InlineData("RegExpInitialize", "str:\"[a-z]\" str:\"v\"", "value: bool:true")]
    [InlineData("RegExpInitialize, nested pattern", "str:\"(?:\" int:511 str:\"a\" str:\")\" str:\"\"", "value: bool:true")]
    [InlineData("RegExpInitialize, nested pattern", "str:\"(?:\" int:512 str:\"a\" str:\")\" str:\"\"", "status: limit-nesting")]
    [InlineData("IsValidRegularExpressionLiteral", "str:\"(?ii:a)\" str:\"\"", "error: SyntaxError")]
    [InlineData("IsValidRegularExpressionLiteral", "str:\"a\" str:\"x\"", "error: SyntaxError")]
    [InlineData("RegExpBuiltinExec", "str:\"(?<a>x)|(?<a>y)\" str:\"d\" str:\"zy\" int:0",
        "value: str:\"lastIndex=0;index=1;length=3;0=1:y;1=u;2=1:y;groups=1;<a>=1:y;indices=3;0=1-2;1=u;2=1-2;igroups=1;<a>=1-2\"")]
    [InlineData("RegExpBuiltinExec", "str:\"a\" str:\"g\" str:\"aba\" int:1", "value: str:\"lastIndex=3;index=2;length=1;0=1:a;groups=u;indices=u\"")]
    [InlineData("RegExpBuiltinExec", "str:\"a\" str:\"\" str:\"aba\" int:2", "value: str:\"lastIndex=2;index=0;length=1;0=1:a;groups=u;indices=u\"")]
    [InlineData("RegExpBuiltinExec", "str:\"a\" str:\"y\" str:\"ba\" int:0", "value: str:\"lastIndex=0;null\"")]
    [InlineData("RegExpBuiltinExec", "str:\"a\" str:\"g\" str:\"aba\" int:4", "value: str:\"lastIndex=0;null\"")]
    [InlineData("RegExpBuiltinExec", "str:\"(?:)\" str:\"gu\" str:\"\\uD834\\uDF06\" int:1", "value: str:\"lastIndex=0;index=0;length=1;0=0:;groups=u;indices=u\"")]
    [InlineData("RegExpBuiltinExec, repeated inputs", "str:\"(a)\" int:3 str:\"\" str:\"\" str:\"a\" int:5 str:\"\" int:0",
        "value: str:\"lastIndex=0;index=0;length=4;0=3;1=1;2=1;3=1\"")]
    [InlineData("RegExpBuiltinExec, step budget", "str:\"(a+)+b\" str:\"\" str:\"a\" int:30 str:\"\" int:0 int:1000", "status: exhausted")]
    [InlineData("RegExpBuiltinExec, step budget", "str:\"(a+)+b\" str:\"\" str:\"a\" int:8 str:\"b\" int:0 int:1000000",
        "value: str:\"lastIndex=0;index=0;length=2;0=9:aaaaaaaab;1=8:aaaaaaaa;groups=u;indices=u\"")]
    [InlineData("RegExp.prototype.test", "str:\"[a-z]\" str:\"iu\" str:\"\\u212A\"", "value: bool:true")]
    [InlineData("RegExp.prototype.test", "str:\"[a-z]\" str:\"i\" str:\"\\u212A\"", "value: bool:false")]
    [InlineData("RegExp.prototype [ %Symbol.match% ]", "str:\"(?:)\" str:\"gu\" str:\"\\uD834\\uDF06\"", "value: str:\"2;0:;0:\"")]
    [InlineData("RegExp.prototype [ %Symbol.match% ]", "str:\"(a)(b)?\" str:\"\" str:\"xa\"", "value: str:\"3;1:a;1:a;u\"")]
    [InlineData("RegExp.prototype [ %Symbol.match% ]", "str:\"q\" str:\"g\" str:\"xa\"", "value: str:\"null\"")]
    [InlineData("RegExp.prototype [ %Symbol.search% ]", "str:\"b\" str:\"y\" str:\"aab\"", "value: f64:0xBFF0000000000000")]
    [InlineData("RegExp.prototype [ %Symbol.search% ]", "str:\"b\" str:\"g\" str:\"aab\"", "value: f64:0x4000000000000000")]
    [InlineData("RegExp.escape", "str:\"abc\"", "value: str:\"\\\\x61bc\"")]
    [InlineData("RegExp.escape, round trip", "str:\"a.b*c\" str:\"\"", "value: bool:true")]
    [InlineData("get RegExp.prototype.flags", "str:\"ygmisd\"", "value: str:\"dgimsy\"")]
    [InlineData("EscapeRegExpPattern, one P and F", "str:\"[/]\" str:\"\"", "value: bool:true")]
    [InlineData("EscapeRegExpPattern, a literal of S", "str:\"\\\\\\u000A\" str:\"\" str:\"\\u000A\"", "value: bool:true")]
    [InlineData("EscapeRegExpPattern, a literal of S", "str:\"/\" str:\"\" str:\"/\"", "value: bool:true")]
    [InlineData("RegExp.prototype [ %Symbol.split% ]", "str:\",\" str:\"\" str:\"a,b\"", "status: not-implemented")]
    [InlineData("String.prototype.search", "str:\"0\"", "status: not-implemented")]
    [InlineData("RegExp.prototype.compile", "str:\"0\"", "fault: an op the runner neither maps nor lists as not implemented: RegExp.prototype.compile")]
    public void Each_op_calls_the_library_and_renders_its_answer(string op, string inputs, string expected)
        => Assert.Equal(expected, Call(op, inputs));

    [Fact]
    public void A_regular_expression_body_is_read_as_the_lexical_grammar_writes_it()
    {
        Assert.True(VectorLibrary.IsRegularExpressionBody("[/]\\/a"));
        Assert.True(VectorLibrary.IsRegularExpressionBody("(?:)"));
        Assert.False(VectorLibrary.IsRegularExpressionBody(""));
        Assert.False(VectorLibrary.IsRegularExpressionBody("*a"));
        Assert.False(VectorLibrary.IsRegularExpressionBody("a/b"));
        Assert.False(VectorLibrary.IsRegularExpressionBody("[a"));
        Assert.False(VectorLibrary.IsRegularExpressionBody("a\\"));
        Assert.False(VectorLibrary.IsRegularExpressionBody("a\\\u2028"));
    }

    [Fact]
    public void A_vector_is_judged_by_its_class()
    {
        Assert.Equal("match", VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:true", "normative"), "value: bool:true").Verdict);
        Assert.Equal("difference", VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:true", "divergence"), "error: SyntaxError").Verdict);
        Assert.Equal("match", VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:false | value: bool:true", "normative"), "value: bool:true").Verdict);
        Assert.Equal("difference", VectorRun.Judge(Vector("RegExpBuiltinExec, step budget", "", "status: exhausted", "normative"), "status: limit-frames").Verdict);
        Assert.Equal("not-implemented", VectorRun.Judge(Vector("String.prototype.search", "", "error: TypeError", "normative"), "status: not-implemented").Verdict);
        Assert.Equal("difference", VectorRun.Judge(Vector("X", "", "value: bool:true", "normative"), "fault: an op").Verdict);
        Assert.Equal("reported; meets out, per-engine.broiler-js, per-engine.broiler-vm",
            VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:true", "impl-defined", "value: bool:true"), "value: bool:true").Verdict);
        Assert.Equal("reported; meets per-engine.broiler-js (D7), per-engine.broiler-vm (D7)",
            VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:true", "impl-defined", "error: SyntaxError"), "status: limit-nesting").Verdict);
        Assert.Equal("reported; meets none",
            VectorRun.Judge(Vector("RegExpInitialize", "", "value: bool:true", "impl-defined", "value: bool:true"), "error: SyntaxError").Verdict);
    }

    // ---- The reading -----------------------------------------------------------------------------------------------

    /// <summary>
    /// The impl-defined vectors past decision D7's nesting limit: the specification sets no bound, both engines pass the
    /// library 512 and refuse past it with a SyntaxError, which they render from the library's nesting status.
    /// </summary>
    private static readonly string[] NestingStatusVectors =
    [
        "regexp/literals/s3-nesting/0002", "regexp/matching/s3-nesting/0004", "regexp/matching/s3-nesting/0005",
        "regexp/matching/s3-nesting/0006", "regexp/syntax/s3-nesting/0011", "regexp/syntax/s3-nesting/0012",
        "regexp/syntax/s3-nesting/0013", "regexp/syntax/s3-nesting/0014", "regexp/syntax/s3-nesting/0015",
        "regexp/syntax/s3-nesting/0016", "regexp/syntax/s3-nesting/0017", "regexp/syntax/s3-nesting/0018",
        "regexp/syntax/s3-nesting/0019", "regexp/syntax/s3-nesting/0020", "regexp/syntax/s3b-deep-nesting/0001",
        "regexp/syntax/s3b-deep-nesting/0002", "regexp/syntax/s3b-deep-nesting/0003",
    ];

    [Fact]
    public void Every_vector_the_rule_holds_to_its_out_gives_it()
    {
        var corpus = VectorCorpus.Load(VectorsRoot);
        List<VectorResult> results = [];
        var thread = new Thread(() => results = VectorRun.Run(corpus, UnicodeTables.Instance), VectorRun.StackBytes);
        thread.Start();
        thread.Join();

        // Every normative and divergence vector whose op the library implements, the status vectors among them.
        Assert.Empty(VectorRun.Differences(results));
        var held = results.Where(static result => result.Vector.Class != "impl-defined" && result.Verdict != VectorResult.NotImplemented).ToList();
        Assert.Equal(1308, held.Count);
        Assert.All(held, static result => Assert.Equal(VectorResult.Match, result.Verdict));
        var status = results.Where(static result => result.IsStatusVector).ToList();
        Assert.Equal(18, status.Count);
        Assert.All(status, static result => Assert.Equal("status: exhausted", result.Rendered));

        // Every impl-defined vector reported: each meets its out and both engines' expectations, or, past the nesting
        // limit, both engines' expectations through decision D7.
        var reported = results.Where(static result => result.Vector.Class == "impl-defined").ToList();
        Assert.Equal(85, reported.Count);
        Assert.Equal(NestingStatusVectors, reported.Where(static result => result.Rendered == "status: limit-nesting").Select(static result => result.Vector.Id).Order(StringComparer.Ordinal));
        Assert.All(reported, result => Assert.Equal(
            NestingStatusVectors.Contains(result.Vector.Id)
                ? "reported; meets per-engine.broiler-js (D7), per-engine.broiler-vm (D7)"
                : "reported; meets out, per-engine.broiler-js, per-engine.broiler-vm",
            result.Verdict));

        // Every op the library does not implement, by name: the object model's.
        var notImplemented = results.Where(static result => result.Verdict == VectorResult.NotImplemented).GroupBy(static result => result.Vector.Op)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
        Assert.Equal(new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["RegExp.prototype [ %Symbol.split% ]"] = 12,
            ["String.prototype.search"] = 6,
            ["get RegExp.prototype.flags, ordinary object"] = 1,
            ["get RegExp.prototype.flags, read order"] = 1,
            ["RegExp.prototype accessors"] = 4,
            ["RegularExpressionLiteral, eval code"] = 3,
        }, notImplemented);
        Assert.Equal(VectorLibrary.NotImplementedOps.Keys.Order(StringComparer.Ordinal), notImplemented.Keys.Order(StringComparer.Ordinal));
        Assert.All(results.Select(static result => result.Vector.Op).Distinct(), static op => Assert.True(VectorLibrary.Implements(op) ^ VectorLibrary.NotImplementedOps.ContainsKey(op), op));
    }

    [Fact]
    public void The_report_is_deterministic_and_ordered_by_id()
    {
        var corpus = VectorCorpus.Load(VectorsRoot);
        var first = VectorRun.RunOnSmallestStack(corpus, UnicodeTables.Instance);
        var second = VectorRun.RunOnSmallestStack(VectorCorpus.Load(VectorsRoot), UnicodeTables.Instance);
        var concurrent = new string[2];
        var threads = Enumerable.Range(0, 2).Select(i => new Thread(() => concurrent[i] = VectorRun.Report(corpus, VectorRun.Run(corpus, UnicodeTables.Instance)), VectorRun.StackBytes)).ToArray();
        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();
        Assert.Equal(first, second);
        Assert.Equal(first, concurrent[0]);
        Assert.Equal(first, concurrent[1]);

        Assert.DoesNotContain('\r', first);
        var lines = first.Split('\n');
        Assert.Contains("# pin: Broiler.JS b2c2e2243a086285d806866a64b83297791a8ec5, corpus revision 32, conformance/vectors/regexp/, 8 files, regexp.pin SHA-256 ", first, StringComparison.Ordinal);
        Assert.Contains("\n# differences: 0\n", first, StringComparison.Ordinal);
        var ids = lines.SkipWhile(static line => !line.StartsWith("id\t", StringComparison.Ordinal)).Skip(1).Where(static line => line.Length > 0).Select(static line => line[..line.IndexOf('\t', StringComparison.Ordinal)]).ToList();
        Assert.Equal(1420, ids.Count);
        Assert.Equal(ids.Order(StringComparer.Ordinal), ids);
        Assert.DoesNotContain(AdversarialCorpusTests.Root, first, StringComparison.Ordinal);
        Assert.DoesNotContain(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), first, StringComparison.Ordinal);
    }

    /// <summary>A scratch copy of <c>tests/Vectors</c>, removed when disposed.</summary>
    internal sealed class ScratchCopy : IDisposable
    {
        internal ScratchCopy()
        {
            Root = Path.Combine(Path.GetTempPath(), "broiler-regex-vectors-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(Root, "regexp"));
            File.Copy(Path.Combine(VectorsRoot, "regexp.pin"), Path.Combine(Root, "regexp.pin"));
            foreach (var file in Directory.GetFiles(Path.Combine(VectorsRoot, "regexp")))
                File.Copy(file, Path.Combine(Root, "regexp", Path.GetFileName(file)));
        }

        internal string Root { get; }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
