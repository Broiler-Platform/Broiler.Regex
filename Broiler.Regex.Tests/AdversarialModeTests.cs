using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Broiler.Regex.Probe;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The probe's <c>--adversarial</c> mode (task 2.14 of SJL-11.2; bundle <c>sjl-11-001</c>'s part C) over scratch corpora
/// built from the real corpus's header: each row run in a child process and reported against its declared status, in
/// ordinal order of the ids, with the corpus's SHA-256; a row that differs and a child that ends abnormally each reported
/// and each failing the run; a <c>validate</c> row waiting for task 2.8 while the probe's runner does not run the
/// operation; and a corpus the mode cannot read refused. The whole corpus is run so by CI's Native AOT job, under the JIT
/// and the Native AOT image, and its rows one by one by <see cref="AdversarialCorpusTests"/>; part C's two controls, which
/// need scratch builds of the probe, by CI's <c>adversarial-controls</c> job.
/// </summary>
public class AdversarialModeTests
{
    /// <summary>The real corpus's lines up to its column line: its header, which declares the stack, and its columns.</summary>
    private static string Header()
    {
        var lines = File.ReadAllLines(AdversarialCorpusTests.CorpusPath);
        return string.Join('\n', lines.TakeWhile(static line => !line.StartsWith("id\t", StringComparison.Ordinal))) + "\n" + string.Join('\t', AdversarialCorpus.Columns) + "\n";
    }

    private static string Row(string id, string op, string pattern, string subject, string status) =>
        string.Join('\t', id, id[..1], op, pattern, "-", subject, "meter=none steps=100000000 frames=4194304 trail=8388608 nesting=512 compile=none", status,
            "-", "-", "-", "-", "-") + "\n";

    private static (int Exit, string Output, string Report, string Digest) Run(string corpus)
    {
        var directory = Directory.CreateTempSubdirectory("adversarial-mode-");
        try
        {
            var path = Path.Combine(directory.FullName, "corpus.tsv");
            File.WriteAllText(path, corpus, new UTF8Encoding(false));
            var reportPath = Path.Combine(directory.FullName, "report.txt");
            var (exit, output, _) = AdversarialCorpusTests.RunProbe("--adversarial", path, "--report", reportPath);
            var report = File.Exists(reportPath) ? File.ReadAllText(reportPath) : "";
            return (exit, output, report, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>Whether the probe's runner answers <c>validate</c> yet: until task 2.8 it answers the operation <c>unsupported</c>.</summary>
    private static bool ValidateWaits =>
        EngineRun.Answer("validate", "a", "-", "", EngineRun.ParsePolicy("meter=none steps=none frames=1 trail=1 nesting=512 compile=none"), out _) == "unsupported";

    [Fact]
    public void Each_row_is_reported_in_id_order_and_a_row_that_differs_or_a_child_that_ends_abnormally_fails_the_run()
    {
        // In file order: a row that gives its declared status, a child that ends abnormally (a start past the subject's
        // end, a broken precondition the row's run does not answer as a status), a row declared otherwise than it answers,
        // and a validate row.
        var nested = "\"(\" * 512 + \"a\" + \")\" * 512";
        var corpus = Header()
            + Row("O11", "match@1", "\"a\"", "\"aba\"", "match 2,3")
            + Row("A07", "match@9", "\"a\"", "\"aba\"", "match 0,1")
            + Row("O03", "match@0", "\"a\"", "\"aba\"", "match 2,3")
            + Row("N03", "validate", nested, "-", "limit:nesting");
        var (exit, output, report, digest) = Run(corpus);

        Assert.Equal(1, exit);
        Assert.Equal(report, output);
        var lines = report.Split('\n');
        Assert.Equal("Broiler.Regex.Probe adversarial corpus adversarial-1", lines[0]);
        Assert.Equal("corpus sha256 " + digest, lines[1]);
        Assert.StartsWith("corpus-format 1, stack-bytes 1048576, rows 4,", lines[2], StringComparison.Ordinal);

        Assert.StartsWith("A07 A match@9 declared \"match 0,1\": exit ", lines[3], StringComparison.Ordinal);
        Assert.EndsWith(", no status line: ends abnormally", lines[3], StringComparison.Ordinal);
        Assert.DoesNotContain("exit 0,", lines[3], StringComparison.Ordinal);
        if (ValidateWaits)
            Assert.Equal("N03 N validate declared \"limit:nesting\": not run: waits for task 2.8 (the validate entry)", lines[4]);
        else
            Assert.Matches("^N03 N validate declared \"limit:nesting\": exit 0, status line \"N03 limit:nesting\"(, units [0-9]+)?: gives its declared status$", lines[4]);
        Assert.Equal("O03 O match@0 declared \"match 2,3\": exit 0, status line \"O03 match 0,1\", units 2: differs", lines[5]);
        Assert.Equal("O11 O match@1 declared \"match 2,3\": exit 0, status line \"O11 match 2,3\", units 3: gives its declared status", lines[6]);

        Assert.Equal(ValidateWaits
            ? "rows 4: 1 give their declared status, 1 differ, 1 end abnormally, 1 wait for task 2.8 (the validate entry)"
            : "rows 4: 2 give their declared status, 1 differ, 1 end abnormally, 0 wait for task 2.8 (the validate entry)", lines[7]);
        Assert.Equal(ValidateWaits ? "waiting: N03" : "waiting: none", lines[8]);
        Assert.Equal("reported: A07 O03", lines[9]);
        Assert.Equal("", lines[10]);
        Assert.Equal(11, lines.Length);
    }

    [Fact]
    public void A_corpus_whose_rows_give_their_declared_statuses_passes_and_its_report_is_the_same_twice()
    {
        var corpus = Header()
            + Row("O11", "match@1", "\"a\"", "\"aba\"", "match 2,3")
            + Row("O09", "anchored@0", "\"b\"", "\"ab\"", "nomatch")
            + Row("N01", "match@0", "\"(\" * 511 + \"a\" + \")\" * 511", "\"a\"", "match 0,1 0,1^511");
        var first = Run(corpus);
        var second = Run(corpus);

        Assert.Equal(0, first.Exit);
        Assert.Equal(first.Report, second.Report);
        Assert.Contains("\nN01 N match@0 declared \"match 0,1 0,1^511\": exit 0, status line \"N01 match 0,1 0,1^511\", units 1024: gives its declared status\n", first.Report, StringComparison.Ordinal);
        Assert.EndsWith("rows 3: 3 give their declared status, 0 differ, 0 end abnormally, 0 wait for task 2.8 (the validate entry)\nwaiting: none\nreported: none\n", first.Report, StringComparison.Ordinal);
    }

    [Fact]
    public void A_corpus_the_mode_cannot_read_is_refused_with_exit_2()
    {
        Assert.Equal(2, AdversarialCorpusTests.RunProbe("--adversarial").Exit);
        Assert.Equal(2, AdversarialCorpusTests.RunProbe("--adversarial", Path.Combine(Path.GetTempPath(), "no-such-directory-" + Guid.NewGuid().ToString("N"), "corpus.tsv")).Exit);
        Assert.Equal(2, Run(Header().Replace("# stack-bytes: ", "# stack: ", StringComparison.Ordinal) + Row("O11", "match@1", "\"a\"", "\"aba\"", "match 2,3")).Exit);

        var directory = Directory.CreateTempSubdirectory("adversarial-mode-");
        try
        {
            var path = Path.Combine(directory.FullName, "corpus.tsv");
            File.WriteAllBytes(path, [.. Encoding.UTF8.GetBytes(Header()), 0xFF, (byte)'\n']);
            Assert.Equal(2, AdversarialCorpusTests.RunProbe("--adversarial", path).Exit);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void A_child_gives_its_row_s_status_only_when_it_exits_0_with_one_line_naming_the_row()
    {
        var row = AdversarialCorpus.Parse(Header() + Row("N01", "match@0", "\"a\"", "\"a\"", "match 0,1 0,1^3")).Rows[0];
        AdversarialMode.Verdict Judge(int? exit, string output) => AdversarialMode.Judge(row, new AdversarialMode.Child(exit, output, null), out _);

        Assert.Equal(AdversarialMode.Verdict.Given, Judge(0, "N01 match 0,1 0,1^3\n"));
        Assert.Equal(AdversarialMode.Verdict.Given, Judge(0, "N01 match 0,1 0,1 0,1 0,1\n"));
        Assert.Equal(AdversarialMode.Verdict.Differs, Judge(0, "N01 match 0,1 0,1^2\n"));
        Assert.Equal(AdversarialMode.Verdict.Differs, Judge(0, "N01 limit:steps\n"));

        // An exit other than 0, a child stopped at the limit, no line, two lines, an unterminated line, another row's line.
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(134, "N01 match 0,1 0,1^3\n"));
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(null, "N01 match 0,1 0,1^3\n"));
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(0, ""));
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(0, "N01 match 0,1 0,1^3\nN01 match 0,1 0,1^3\n"));
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(0, "N01 match 0,1 0,1^3"));
        Assert.Equal(AdversarialMode.Verdict.Abnormal, Judge(0, "N02 match 0,1 0,1^3\n"));

        Assert.Equal(1024UL, AdversarialMode.Units("units 1024\n"));
        Assert.Null(AdversarialMode.Units("Stack overflow.\nunits 1024\n"));
        Assert.Null(AdversarialMode.Units(""));
    }

    [Fact]
    public void The_reader_refuses_a_corpus_that_is_not_its_README_s_format()
    {
        var row = Row("O11", "match@1", "\"a\"", "\"aba\"", "match 2,3");
        Assert.Equal(1048576, AdversarialCorpus.Parse(Header() + row).StackBytes);
        Assert.Equal(new[] { "O11" }, AdversarialCorpus.Parse(Header() + row).Rows.Select(static r => r.Id));
        Assert.Equal(65, AdversarialCorpus.Read(AdversarialCorpusTests.CorpusPath).Rows.Count);

        string[] refused =
        [
            Header().Replace("# corpus-format: 1", "# corpus-format: 2", StringComparison.Ordinal) + row,
            Header().Replace("# stack-bytes: 1048576", "# stack-bytes: x", StringComparison.Ordinal) + row,
            Header().Replace("\tjs_outcome\n", "\n", StringComparison.Ordinal) + row,
            Header() + row + row,
            Header() + row.Replace("O11\tO", "O1\tO", StringComparison.Ordinal),
            Header() + row.Replace("O11\tO", "O11\tK", StringComparison.Ordinal),
            Header() + row.Replace("\t-\t-\t-\t-\t-\n", "\t-\t-\t-\t-\n", StringComparison.Ordinal),
            Header() + row + "# a header after the rows\n",
        ];
        foreach (var text in refused)
            Assert.Throws<FormatException>(() => AdversarialCorpus.Parse(text));
    }
}
