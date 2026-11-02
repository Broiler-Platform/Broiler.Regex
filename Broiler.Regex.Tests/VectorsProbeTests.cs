using System;
using System.IO;
using Broiler.Regex.VectorsRunner;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Bundle <c>sjl-11-001</c>'s part D "under the JIT and through the Native AOT probe": the probe's <c>--vectors</c> mode
/// (<c>probes/Regex.Probe/VectorsMode.cs</c>) compiles in the runner of <c>Vectors/</c> and runs it over the pinned copy;
/// here under the JIT, as a child process of the probe this build made, and in CI under Native AOT too, where the two
/// reports are compared byte for byte.
/// </summary>
public class VectorsProbeTests
{
    /// <summary>The probe's <c>--vectors</c> mode under the JIT runs the same code: the same report, exit 0; a changed byte, exit 2 and no report.</summary>
    [Fact]
    public void The_probe_reports_what_the_tests_read()
    {
        var expected = VectorRun.RunOnSmallestStack(VectorCorpus.Load(VectorsRunTests.VectorsRoot), UnicodeTables.Instance);
        var (exit, output, error) = AdversarialCorpusTests.RunProbe("--vectors", VectorsRunTests.VectorsRoot);
        Assert.True(exit == 0, error);
        Assert.Equal(expected, output);

        using var copy = new VectorsRunTests.ScratchCopy();
        File.AppendAllText(Path.Combine(copy.Root, "regexp", "syntax.vectors"), "#");
        (exit, output, error) = AdversarialCorpusTests.RunProbe("--vectors", copy.Root);
        Assert.Equal(2, exit);
        Assert.Equal("", output);
        Assert.StartsWith("refused: the vectors' copy is not the pinned one: syntax.vectors: ", error, StringComparison.Ordinal);

        Assert.Equal(2, AdversarialCorpusTests.RunProbe("--vectors").Exit);
    }

}
