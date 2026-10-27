using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// The API documentation the package ships, <c>Broiler.Regex.xml</c>, keeps every member's comment. The build
/// suppresses CS1570, the compiler's warning for a comment that is not well-formed XML
/// (<c>eng/Broiler.Packaging.props</c>, shared with the other packages), and the compiler then writes the member's
/// comment out of the file and leaves only a remark in its place. Without this test a member's summary, remarks and
/// parameter contract could leave the package with no build warning.
/// </summary>
public class DocumentationFileTests
{
    [Fact]
    public void The_documentation_file_the_package_ships_drops_no_member_s_comment()
    {
        var assembly = SharedTree.AssemblyPath("Broiler.Regex");
        var documentation = Path.ChangeExtension(assembly, ".xml");
        Assert.True(File.Exists(documentation), documentation + " is not beside the assembly");
        var lines = File.ReadAllLines(documentation);

        Assert.Empty(lines.Where(static line => line.Contains("Badly formed XML comment", StringComparison.Ordinal)).Select(static line => line.Trim()));

        // Not vacuous: the file holds the public offset entry's comment, its parameter contract for a start inside a
        // surrogate pair among it.
        Assert.Contains(lines, static line => line.Contains("<member name=\"M:Broiler.Regex.BroilerRegex.Match(System.ReadOnlySpan{System.Char},System.Int32,System.Boolean,", StringComparison.Ordinal));
        Assert.Contains(lines, static line => line.Contains("This entry takes the start as given.", StringComparison.Ordinal));

        // The usual cause, a control character a tool wrote for an escape such as \b, is found where it is written.
        var control = SharedTree.SourceFiles("*.cs")
            .SelectMany(static path => File.ReadAllLines(path).Select((line, index) => (Path: SharedTree.Relative(path), Line: index + 1, Text: line)))
            .Where(static line => line.Text.Any(static c => char.IsControl(c) && c != '\t'))
            .Select(static line => line.Path + ":" + line.Line);
        Assert.Empty(control);
    }
}
