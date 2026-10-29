using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Broiler.Regex.Probe;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// SL1 to SL10 and the contract checks C1, C3 and C5. Each has a direction over this repository as it stands,
/// which asserts exactly the rule's rows of the starting violation list (<see cref="StartingViolations"/>) and that
/// the rule read a real subject, and a witness direction over an input made to break it. A test method is named
/// for its rule, so the register can find it. Which projects a rule holds is <see cref="SharedTree.Holds"/>'s
/// answer, and the reasons are on <see cref="SharedProjectKind"/>.
/// </summary>
public class SharedRuleTests
{
    private const string Library = "Broiler.Regex";

    /// <summary>
    /// The last version of the package published to nuget.org, which package validation reads as its baseline
    /// (convention 16). It moves when a later version is published.
    /// </summary>
    private const string LastPublishedVersion = "0.1.0-preview.4";

    private static IReadOnlyDictionary<string, AssemblyFacts> Assemblies { get; } =
        SharedTree.Libraries.ToDictionary(static p => p.Name, static p => AssemblyFacts.Read(SharedTree.AssemblyPath(p.Name)), StringComparer.Ordinal);

    private static IEnumerable<AssemblyFacts> AssembliesHeldBy(string rule) =>
        SharedTree.LibrariesHeldBy(rule).Select(static p => Assemblies[p.Name]);

    private static SharedProject Probe => SharedTree.Projects.Single(static p => p.Kind == SharedProjectKind.Probe);

    /// <summary>The probe as built beside this test, under the JIT, in this test's configuration.</summary>
    private static AssemblyFacts ProbeAssembly =>
        AssemblyFacts.Read(Path.Combine(Probe.Directory, "bin", SharedTree.Configuration, "net10.0", Probe.Name + ".dll"));

    private static IReadOnlyList<string> LibraryApi => ApiListing.Read(SharedTree.AssemblyPath(Library));

    private static readonly string[] SuppressionProperties = ["NoWarn", "WarningsNotAsErrors", "SuppressTrimAnalysisWarnings"];

    [Fact]
    public void The_rules_read_every_project_of_the_repository()
    {
        Assert.Equal([(Library, SharedProjectKind.Library)], SharedTree.Libraries.Select(static p => (p.Name, p.Kind)));
        Assert.Equal("Broiler.Regex.Probe", Probe.Name);
        Assert.Equal(
            ["Broiler.Regex.Tests", "Regex.Architecture.Tests", "Regex.Fuzz"],
            SharedTree.Projects.Where(static p => p.Kind == SharedProjectKind.Test).Select(static p => p.Name));
        Assert.All(Assemblies.Values, static a => Assert.NotEmpty(a.TypeReferences));

        // The rules over member and type references read their places from the PDB beside each assembly, so that the
        // starting violation list is asserted place by place; an assembly read without one would pass on wording alone.
        Assert.All(Assemblies.Values, static a => Assert.True(a.HasPlaces, a.Name + " was read without its PDB"));
        Assert.True(ProbeAssembly.HasPlaces, "the probe was read without its PDB");
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL1_No_library_references_an_engine_Broiler_VM_or_anything_outside_the_layering()
    {
        var violations = new List<Finding>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL1"))
        {
            violations.AddRange(SharedRules.SL1(library.Name, library.Directory, library.Document).Select(static v => new Finding(v)));
            using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(library.Directory, "obj", "project.assets.json")));
            violations.AddRange(SharedRules.SL1Restore(library.Name, assets).Select(static v => new Finding(v)));

            // Not vacuous: the project names packages, and its restore resolves them.
            Assert.NotEmpty(library.Items("PackageReference"));
            Assert.Contains(assets.RootElement.GetProperty("libraries").EnumerateObject(), static l => l.Value.GetProperty("type").GetString() == "package");
        }

        violations.AddRange(AssembliesHeldBy("SL1").SelectMany(SharedRules.SL1AssemblyFindings));
        Assert.Contains(Assemblies[Library].AssemblyReferences, static r => !r.StartsWith("System", StringComparison.Ordinal));
        StartingViolations.AssertExactly("SL1", violations);
    }

    [Theory]
    [InlineData("SL1-engine-project-reference.csproj.witness", "references the project ..\\..\\broiler.js")]
    [InlineData("SL1-broiler-vm-package-reference.csproj.witness", "references the package Broiler.VM.Abstractions")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references the package System.Text.Json")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references the package Broiler.Unicode.Tables")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references Broiler.Regex.Probe, against the layering")]
    public void SL1_A_witness_project_is_refused(string witness, string expected)
    {
        var library = SharedTree.Project(Library);
        var violations = SharedRules.SL1(library.Name, library.Directory, XDocument.Parse(Witness.Text(witness))).ToList();
        Assert.Contains(violations, v => v.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void SL1_The_restore_half_refuses_a_package_outside_the_layering_restored_directly_or_transitively()
    {
        // A restore that resolves the provider, the owner's tables, a base class library package and the SDK's own
        // ILLink package, beside a project: the tables and the package outside the allow-list are refused, the
        // provider, the SDK's package and the project are not.
        using var assets = JsonDocument.Parse(Witness.Text("SL1-restores-a-package-outside-the-layering.assets.json.witness"));
        Assert.Equal(
            [
                "Broiler.Regex restores the package Broiler.Unicode.Tables/0.1.0-preview.5, outside the allow-list or against the layering",
                "Broiler.Regex restores the package System.Text.Json/10.0.0, outside the allow-list or against the layering",
            ],
            SharedRules.SL1Restore(Library, assets));
    }

    [Fact]
    public void SL1_The_assembly_half_refuses_a_reference_outside_the_framework_and_the_layering()
    {
        // This test assembly references xunit and the library, neither of which the layering gives a library,
        // so it is the witness for the assembly half as Broiler.VM's is for B1.
        var self = AssemblyFacts.Read(typeof(SharedRuleTests).Assembly.Location);
        var violations = SharedRules.SL1Assembly(self).ToList();
        Assert.Contains(violations, static v => v.EndsWith("references the assembly xunit.core", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("references the assembly Broiler.Regex", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL2_No_library_holds_mutable_static_state()
    {
        StartingViolations.AssertExactly("SL2", AssembliesHeldBy("SL2").SelectMany(SharedRules.SL2));

        // Not vacuous: the library declares a constant, which SL2 reads as a static field and passes as a literal,
        // and a static field holding an object with no state, which it reads and passes.
        var library = Assemblies[Library];
        Assert.Contains(library.StaticFields, static f => f.Type == "Broiler.Regex.Ast.CodePointSet" && f.Name == "MaxCodePoint" && f.IsLiteral);
        Assert.Contains(library.StaticFields, static f => f.Type == "Broiler.Regex.Ast.EmptyNode" && f.Name == "Instance" && f.IsReadOnly);
        Assert.DoesNotContain(SharedRules.SL2(library), static v => v.Contains("EmptyNode.Instance", StringComparison.Ordinal));

        // And it reads what the static constructor creates: RegexMatch.Empty's no-match result holds the empty
        // read-only dictionary that ReadOnlyDictionary.Empty returns behind its NamedGroups interface, which SL2 reads
        // and passes (the type is the getter's return type as its signature names it, over the class's own type
        // parameters). Until task 2.3 it held a new Dictionary there, and SL2 refused the field.
        Assert.Equal(
            ["System.Collections.ObjectModel.ReadOnlyDictionary`2<!0,!1>"],
            library.StaticInitializerCreations["Broiler.Regex.RegexMatch.Empty"]);
    }

    [Fact]
    public void SL2_The_witness_is_refused_for_each_kind_of_state()
    {
        var violations = SharedRules.SL2(Witness.Compile("SL2-mutable-static-state.cs.witness")).ToList();
        Assert.Contains(violations, static v => v.Contains("AmbientState.Calls is a mutable static field", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Table is a static array", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Cache is a static System.Collections.Generic.Dictionary`2", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Mode is a settable static property", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("[ThreadStatic]", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("names System.Threading.ThreadLocal`1", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("names System.Threading.AsyncLocal`1", StringComparison.Ordinal));

        // A static get-only auto-property of a mutable type: its backing field is a static field like any other.
        Assert.Contains(violations, static v => v.Contains("AmbientState.Lookup is a static System.Collections.Generic.Dictionary`2", StringComparison.Ordinal));

        // This repository's addition: a static field holding an object of the library's own type that changes on
        // a read, the shape of a lazily normalised set, directly, through a base class and through a readonly field
        // of another class; an immutable one passes. The row names the static field; the reasons are every field.
        var witness = Witness.Compile("SL2-mutable-static-state.cs.witness");
        const string OwnClass = ", an object of the library's own class that can be changed after construction";
        Assert.Contains("SL2_mutable_static_state: Witness.AmbientState.Shared is a static Witness.LazySet" + OwnClass, violations);
        Assert.Contains("SL2_mutable_static_state: Witness.AmbientState.Derived is a static Witness.DerivedSet" + OwnClass, violations);
        Assert.Contains("SL2_mutable_static_state: Witness.AmbientState.Held is a static Witness.Holder" + OwnClass, violations);
        Assert.Equal(["Witness.LazySet.items is a System.Collections.Generic.List`1<System.Int32>", "Witness.LazySet.normalized is not readonly"], SharedRules.MutableInstanceState(witness, "Witness.LazySet"));
        Assert.Equal(SharedRules.MutableInstanceState(witness, "Witness.LazySet"), SharedRules.MutableInstanceState(witness, "Witness.DerivedSet"));
        Assert.Equal(["Witness.Holder.inner holds a Witness.LazySet, which can be changed after construction"], SharedRules.MutableInstanceState(witness, "Witness.Holder"));
        Assert.DoesNotContain(violations, static v => v.Contains("AmbientState.Fixed", StringComparison.Ordinal));

        // A mutable collection the initializer creates behind a read-only interface, held directly and by a class
        // of the witness's own, the RegexMatch.Empty shape; the same shapes over an empty array or an immutable
        // dictionary pass.
        const string Hidden = " whose initializer keeps a mutable collection behind a collection interface, which can be changed after construction";
        Assert.Contains("SL2_mutable_static_state: Witness.AmbientState.Wrapped is a static System.Collections.Generic.IReadOnlyList`1<System.Int32>" + Hidden, violations);
        Assert.Contains("SL2_mutable_static_state: Witness.AmbientState.Hidden is a static Witness.Snapshot" + Hidden, violations);
        Assert.DoesNotContain(violations, static v => v.Contains("AmbientState.Frozen", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("AmbientState.None", StringComparison.Ordinal));
    }

    /// <summary>
    /// SL2's stated limits (the register's SL2 row), pinned on their witness, so that a change that makes the rule more
    /// precise fails here and changes the limits' text with it. The first: what an initializer creates over-approximates
    /// what the field holds, so an immutable value built from a mutable temporary and held behind a collection interface
    /// is refused, and the same value in a field declared by its immutable type passes. The second: a field whose
    /// declared type cannot vouch for what it holds passes whatever it holds, and <see cref="SharedRules.SL2DeclaredTypeGap"/>
    /// names it.
    /// </summary>
    [Fact]
    public void SL2_The_stated_limits_hold_as_the_register_states_them()
    {
        var witness = Witness.Compile("SL2-stated-limits.cs.witness");
        var violations = SharedRules.SL2(witness).ToList();
        const string Hidden = " whose initializer keeps a mutable collection behind a collection interface, which can be changed after construction";

        // A collection expression behind IReadOnlyList: its array, which the compiler's read-only list wraps, counts.
        Assert.Contains("SL2_stated_limits: Witness.StatedLimits.Listed is a static System.Collections.Generic.IReadOnlyList`1<System.String>" + Hidden, violations);
        Assert.Contains("System.String[]", witness.StaticInitializerCreations["Witness.StatedLimits.Listed"]);

        // ToFrozenDictionary behind IReadOnlyDictionary: the dictionary it copies counts.
        Assert.Contains("SL2_stated_limits: Witness.StatedLimits.Copied is a static System.Collections.Generic.IReadOnlyDictionary`2<System.String,System.Int32>" + Hidden, violations);
        Assert.Contains("System.Collections.Generic.Dictionary`2<System.String,System.Int32>", witness.StaticInitializerCreations["Witness.StatedLimits.Copied"]);

        // The same values declared by their immutable types pass.
        Assert.DoesNotContain(violations, static v => v.Contains("StatedLimits.ListedImmutable", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("StatedLimits.CopiedFrozen", StringComparison.Ordinal));

        // The second: SL2 reads declared types, and what an initializer creates only as base class library collections,
        // so a field whose type cannot vouch for what it holds passes whatever it holds, and SL2DeclaredTypeGap names it.
        foreach (var name in (string[])["Boxed", "Base", "Comparer", "Packed", "Counter", "Done"])
            Assert.DoesNotContain(violations, v => v.Contains("StatedLimits." + name + " ", StringComparison.Ordinal));
        Assert.Equal(
            [
                "Witness.StatedLimits.Base is typed Witness.Node, a class that Witness.CountingNode derives from, whose fields SL2 does not read",
                "Witness.StatedLimits.Boxed is typed System.Object, which can hold any object",
                "Witness.StatedLimits.Comparer is typed System.Collections.Generic.IComparer`1<System.Int32>, an interface whose initializer constructs Witness.CountingComparer, which SL2 does not read",
                "Witness.StatedLimits.Counter is typed Witness.Next, a delegate, whose target SL2 does not read",
                "Witness.StatedLimits.Packed is typed Witness.Packet, a struct, whose fields SL2 does not read",
            ],
            SharedRules.SL2DeclaredTypeGap(witness).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// SL2's second stated limit over the library: every static field it passes whose declared type cannot vouch for
    /// what it holds is listed here, after a reader has checked it, so a new one fails until it is checked or SL2
    /// reads it.
    /// </summary>
    [Fact]
    public void SL2_The_library_holds_no_static_field_its_declared_type_cannot_vouch_for_but_those_read()
    {
        // None is left: the old matcher's identity continuation, a static lambda typed as a delegate whose target SL2
        // does not read, went with Matching/ in task 2.4c. EmptyNode.Instance and AnyCharNode.Instance are sealed
        // classes with no field, which the declared type vouches for, and RegexMatch's shared results are sealed
        // classes of get-only members.
        Assert.Empty(SharedRules.SL2DeclaredTypeGap(Assemblies[Library]));
    }

    /// <summary>
    /// Every <c>CodePointSet</c> is immutable after construction (convention 10, decision D31), read as SL2 reads the
    /// objects its static fields hold: no instance field that is not readonly, none of a mutable collection or array
    /// type, and none holding a class of the library's that can change, in the class or a base class it declares.
    /// SL2 reaches a set only through a static field that holds one; this reads the class itself, so the check holds
    /// for the sets a compiled pattern keeps per instance, which the register's third stated limit leaves to check A-11,
    /// whatever the library's static fields hold. <c>FirstUseConcurrencyTests</c> reads the static sets' ranges.
    /// </summary>
    [Fact]
    public void SL2_A_code_point_set_has_no_instance_field_that_can_change_after_construction()
    {
        const string Set = "Broiler.Regex.Ast.CodePointSet";
        var library = Assemblies[Library];
        Assert.Empty(SharedRules.MutableInstanceState(library, Set));

        // Not vacuous: the class is the library's and the reading saw its field, the ranges.
        Assert.Contains(Set, library.Classes);
        Assert.Contains(library.InstanceFields, static f => f.Type == Set && f.Name == "_ranges" && f.IsReadOnly);
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL3_No_library_reaches_dynamic_code_hidden_machinery_IO_or_the_environment()
    {
        var violations = AssembliesHeldBy("SL3").SelectMany(static a => SharedRules.SL3Findings(a)).ToList();

        // The probe is an executable that writes its report to a file; SL3 holds it all the same but for I/O,
        // since it is the code that runs under Native AOT (docs/conventions.md, "Scope").
        var probe = ProbeAssembly;
        Assert.Contains("System.IO.File", probe.TypeReferences);
        violations.AddRange(SharedRules.SL3Findings(probe, allowIo: true));
        StartingViolations.AssertExactly("SL3", violations);
    }

    [Fact]
    public void SL3_The_witness_is_refused_for_each_kind_of_machinery()
    {
        var violations = SharedRules.SL3(Witness.Compile("SL3-dynamic-code-and-io.cs.witness")).ToList();
        string[] expected =
        [
            "calls System.Activator.CreateInstance(System.Type)", "calls System.Type.GetType(System.String)",
            "calls System.Type.MakeGenericType(System.Type[])", "calls System.Enum.GetValues(System.Type)",
            "calls System.Reflection.MethodBase.Invoke(System.Object,System.Object[])", "names System.IO.File",
            "calls System.Environment.GetEnvironmentVariable(System.String): an environment read", "applies a module initializer",
            "declares the platform invoke libc!getpid", "names System.Reflection.MethodInfo: reflection",
        ];
        foreach (var fragment in expected)
            Assert.Contains(violations, v => v.Contains(fragment, StringComparison.Ordinal));

        // The probe's exemption is for I/O only: the same witness under it still fails on the rest.
        var asProbe = SharedRules.SL3(Witness.Compile("SL3-dynamic-code-and-io.cs.witness"), allowIo: true).ToList();
        Assert.DoesNotContain(asProbe, static v => v.Contains("System.IO.File", StringComparison.Ordinal));
        Assert.Contains(asProbe, static v => v.Contains("applies a module initializer", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL4_No_library_uses_platform_globalization_regular_expressions_or_a_culture()
    {
        StartingViolations.AssertExactly("SL4", AssembliesHeldBy("SL4").SelectMany(SharedRules.SL4Findings));

        // Not vacuous: the library compares strings ordinally and tests surrogates, and SL4 reads those references
        // with their parameter types and passes them.
        var library = Assemblies[Library];
        Assert.Contains("System.Char.IsHighSurrogate(System.Char)", library.MemberReferences);
        Assert.Contains(library.MemberReferences, static m => m.StartsWith("System.String.CompareOrdinal(", StringComparison.Ordinal));
    }

    [Fact]
    public void SL4_The_witness_is_refused_for_each_platform_dependence()
    {
        var violations = SharedRules.SL4(Witness.Compile("SL4-platform-globalization.cs.witness")).ToList();
        string[] expected =
        [
            "names System.Text.RegularExpressions.Regex", "calls System.String.Normalize(System.Text.NormalizationForm)",
            "calls System.String.ToUpper()", "calls System.Char.IsLetter(System.Char)", "calls System.Char.IsDigit(System.Char)",
            "calls System.Char.ToLowerInvariant(System.Char)",
            "names System.Globalization.CultureInfo: platform globalization", "calls System.Double.Parse(System.String), which parses by the current culture",
            "calls System.String.IndexOf(System.String), which compares by the current culture",
            "interpolates a string without a format provider", "calls System.DateTime.get_Now()", "names System.TimeZoneInfo",
            "calls System.Globalization.NumberFormatInfo.get_CurrentInfo(): only NumberFormatInfo.InvariantInfo is culture-free",
            "calls System.Convert.ToInt32(System.String), which converts by the current culture",
            "calls System.String.Format(System.String,System.Object), which formats by the current culture",
            "calls System.Text.StringBuilder.Append(System.Double), which formats by the current culture",
            "calls System.Double.ToString(System.String), which formats by the current culture",
        ];
        foreach (var fragment in expected)
            Assert.Contains(violations, v => v.Contains(fragment, StringComparison.Ordinal));

        // A provider passed is not refused, by signature: NumberFormatInfo.InvariantInfo's formatting passes.
        Assert.DoesNotContain(violations, static v => v.Contains("System.Int32.ToString(System.IFormatProvider)", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("get_InvariantInfo", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL5_The_library_and_the_probe_hold_to_AOT_and_trimming_with_nothing_suppressed()
    {
        // A suppressed code's places are the NoWarn and WarningsNotAsErrors elements that name it, in the project file
        // and every build file of the repository its evaluation imports.
        static IReadOnlyList<(string Path, string Text)> FilesOf(SharedProject project)
        {
            var files = BuildFilesOf(project).Select(static f => (SharedTree.Relative(f), File.ReadAllText(f))).ToList();

            // Not vacuous: the evaluation's imports are read, the project file and the three build files among them.
            foreach (var file in (string[])[SharedTree.Relative(project.Path), "Directory.Build.props", "Directory.Packages.props", "eng/Broiler.Packaging.props"])
                Assert.Contains(files, f => f.Item1 == file);
            return files;
        }

        var violations = new List<Finding>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL5"))
        {
            var evaluated = Tools.Evaluate(library.Path, SharedRules.LibraryProperties.Select(static p => p.Property).Concat(SuppressionProperties));
            violations.AddRange(SharedRules.SL5PropertyFindings(library.Name, evaluated, SharedRules.LibraryProperties, FilesOf(library)));
        }

        var probeProperties = Tools.Evaluate(Probe.Path, SharedRules.ProbeProperties.Select(static p => p.Property).Concat(SuppressionProperties));
        violations.AddRange(SharedRules.SL5PropertyFindings(Probe.Name, probeProperties, SharedRules.ProbeProperties, FilesOf(Probe)));

        violations.AddRange(AssembliesHeldBy("SL5").SelectMany(SharedRules.SL5Assembly).Select(static v => new Finding(v)));
        violations.AddRange(SharedRules.SL5Assembly(ProbeAssembly).Select(static v => new Finding(v)));
        StartingViolations.AssertExactly("SL5", violations);
    }

    /// <summary>
    /// The inventory, in the shape of Broiler.VM's N36: every suppression in the library's and the probe's sources
    /// and project files, and in the build files they import, is listed by file in docs/suppressions.md, and when
    /// there is none, the inventory says None.
    /// </summary>
    [Fact]
    public void SL5_The_suppression_inventory_lists_every_suppression_and_says_None_when_there_is_none()
    {
        // The shipped code and the code the probe compiles, and the build files every one of them imports. The test
        // projects are the rules' own sources, whose text names every marker the scan looks for.
        var scanned = SharedTree.Projects.Where(static p => p.Kind != SharedProjectKind.Test).Select(static p => p.Directory + Path.DirectorySeparatorChar).ToList();
        var files = SharedTree.SourceFiles("*.cs")
            .Concat(SharedTree.SourceFiles("*.csproj"))
            .Where(path => scanned.Any(directory => path.StartsWith(directory, StringComparison.Ordinal)))
            .Concat(SharedTree.Projects.Where(static p => p.Kind != SharedProjectKind.Test).SelectMany(BuildFilesOf))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var found = files.SelectMany(static path => SharedRules.Suppressions(SharedTree.Relative(path), File.ReadAllText(path))).ToList();
        // The inventory is the "### Inventory" subsection of docs/suppressions.md, up to the next "## " section;
        // the accepted API breaks that follow it are package validation's, not this rule's.
        var inventory = File.ReadAllText(Path.Combine(SharedTree.Documents, "suppressions.md"));
        var section = inventory[inventory.IndexOf("\n### Inventory\n", StringComparison.Ordinal)..];
        var next = section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        section = next < 0 ? section : section[..(next + 1)];

        if (found.Count == 0)
            Assert.Contains("\nNone.\n", section, StringComparison.Ordinal);
        foreach (var suppression in found)
            Assert.Contains("`" + suppression.Split(':')[0] + "`", section, StringComparison.Ordinal);

        // Not vacuous: the scan read the library's and the probe's project files, the sources, and the build files.
        foreach (var project in SharedTree.Projects.Where(static p => p.Kind != SharedProjectKind.Test))
            Assert.Contains(project.Path, files);
        Assert.Contains(files, static p => p.EndsWith("RegexParser.cs", StringComparison.Ordinal));
        foreach (var buildFile in (string[])["Directory.Build.props", "Directory.Packages.props", "eng/Broiler.Packaging.props"])
            Assert.Contains(files, p => SharedTree.Relative(p) == buildFile);
    }

    /// <summary>
    /// The files of this repository that MSBuild's evaluation of a project imports, the project file among them, by
    /// full path: not a list kept here, so a build file a later change adds (a Directory.Build.targets, another
    /// eng/*.props) is read the day it is imported. Build output, NuGet's generated obj/ files among it, is not the
    /// repository's, and the packages' own build files lie outside it.
    /// </summary>
    private static IReadOnlyList<string> BuildFilesOf(SharedProject project) =>
        Tools.ImportedFiles(project.Path).Where(SharedTree.IsRepositoryFile).ToList();

    [Fact]
    public void SL5_The_witnesses_are_refused_in_the_project_in_metadata_and_in_the_inventory()
    {
        var scratch = Directory.CreateTempSubdirectory("sl5-witness-");
        try
        {
            var project = Path.Combine(scratch.FullName, "Witness.csproj");
            File.WriteAllText(project, Witness.Text("SL5-library-suppresses-a-trim-warning.csproj.witness"));
            var evaluated = Tools.Evaluate(project, SharedRules.LibraryProperties.Select(static p => p.Property).Concat(SuppressionProperties));
            var violations = SharedRules.SL5Properties("Witness", evaluated, SharedRules.LibraryProperties).ToList();
            Assert.Contains("Witness: NoWarn suppresses IL2026", violations);
            Assert.Contains("Witness: WarningsNotAsErrors exempts IL3050", violations);
            Assert.DoesNotContain(violations, static v => v.Contains("is '", StringComparison.Ordinal));

            // The build files a code's places are read from are the evaluation's imports, not a list: a
            // Directory.Build.targets beside the project, which no list names, is imported and read, and a code it
            // suppresses has its place there as well as in the project file.
            var targets = Path.Combine(scratch.FullName, "Directory.Build.targets");
            File.WriteAllText(targets, "<Project>\n  <PropertyGroup>\n    <NoWarn>$(NoWarn);IL2026</NoWarn>\n  </PropertyGroup>\n</Project>\n");
            // Told by the scratch directory's unique name, which a short or differently cased path keeps.
            var imported = Tools.ImportedFiles(project).Where(f => Path.GetFileName(Path.GetDirectoryName(f)) == scratch.Name).ToList();
            Assert.Equal(["Directory.Build.targets", "Witness.csproj"], imported.Select(Path.GetFileName));
            var files = imported.Select(static f => (Path.GetFileName(f), File.ReadAllText(f))).ToList();
            var withTargets = Tools.Evaluate(project, SharedRules.LibraryProperties.Select(static p => p.Property).Concat(SuppressionProperties));
            var places = SharedRules.SL5PropertyFindings("Witness", withTargets, SharedRules.LibraryProperties, files).Single(static f => f.Violation == "Witness: NoWarn suppresses IL2026").Places;
            var projectText = File.ReadAllText(project);
            var projectLine = 1 + projectText.AsSpan(0, projectText.IndexOf("<NoWarn>", StringComparison.Ordinal)).Count('\n');
            Assert.Equal([new SourcePlace("Directory.Build.targets", 3, 3), new SourcePlace("Witness.csproj", projectLine, projectLine)], places!);
        }
        finally
        {
            scratch.Delete(recursive: true);
        }

        Assert.NotEmpty(SharedRules.SL5Assembly(Witness.Compile("SL5-source-suppresses-a-trim-warning.cs.witness")));
        Assert.NotEmpty(SharedRules.Suppressions("x.cs", Witness.Text("SL5-source-suppresses-a-trim-warning.cs.witness")));
        Assert.NotEmpty(SharedRules.Suppressions("x.csproj", Witness.Text("SL5-library-suppresses-a-trim-warning.csproj.witness")));

        // The reset the probe's project file holds is not a suppression; one code more is.
        Assert.Empty(SharedRules.Suppressions("x.csproj", "<NoWarn>1701;1702</NoWarn>"));
        Assert.NotEmpty(SharedRules.Suppressions("x.csproj", "<NoWarn>1701;1702;IL2026</NoWarn>"));

        // A code's places are every element that suppresses it, in any of the files, so a second one is a new place.
        var witnessText = Witness.Text("SL5-library-suppresses-a-trim-warning.csproj.witness");
        var noWarnLine = 1 + witnessText.AsSpan(0, witnessText.IndexOf("<NoWarn>", StringComparison.Ordinal)).Count('\n');
        Assert.Equal(
            [new SourcePlace("w.csproj", noWarnLine, noWarnLine), new SourcePlace("d.props", 2, 3)],
            SharedRules.ElementPlaces([("w.csproj", witnessText), ("d.props", "<Project>\n  <NoWarn Condition=\"true\">$(NoWarn);\n    IL2026</NoWarn>\n  <NoWarnX>IL2026</NoWarnX>\n</Project>\n")], "NoWarn", "IL2026"));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL6_The_public_API_matches_the_baseline_in_both_directions_and_package_validation_reads_the_last_published_version()
    {
        var update = Environment.GetEnvironmentVariable(ApiListing.UpdateVariable) == "1";
        var violations = new List<string>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL6"))
        {
            var listing = ApiListing.Read(SharedTree.AssemblyPath(library.Name));
            Assert.NotEmpty(listing);
            var baseline = ApiListing.BaselinePath(library);
            if (update)
                File.WriteAllText(baseline, ApiListing.Render(library.Name, listing, ApiListing.ReadProvisional(baseline)));
            violations.AddRange(SharedRules.SL6(library.Name, listing, ApiListing.ReadBaseline(baseline)));
            violations.AddRange(SharedRules.SL6Provisional(library.Name, listing, ApiListing.ReadProvisional(baseline)));

            var evaluated = Tools.Evaluate(library.Path, ["IsPackable", "EnablePackageValidation", "PackageValidationBaselineVersion"]);
            Assert.Equal("true", evaluated["IsPackable"]);
            violations.AddRange(SharedRules.SL6PackageValidation(library.Name, evaluated, LastPublishedVersion));
        }

        StartingViolations.AssertExactly("SL6", violations);
    }

    [Fact]
    public void SL6_A_provisional_mark_whose_member_is_gone_is_refused_and_a_rewrite_keeps_the_marks_of_listed_members()
    {
        string[] listing = ["Witness.Status enum", "Witness.Status.Overflow = 3"];
        string[] marks = ["Witness.Status.Overflow = 3", "Witness.Status.Malformed = 1"];
        Assert.Equal(
            ["Witness's baseline marks as provisional public API the assembly does not have: Witness.Status.Malformed = 1"],
            SharedRules.SL6Provisional("Witness", listing, marks));

        var rewritten = ApiListing.Render("Witness", listing, marks);
        Assert.Contains("\n" + ApiListing.ProvisionalMark + "Witness.Status.Overflow = 3\n", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("Malformed", rewritten, StringComparison.Ordinal);
        var lines = rewritten.Split('\n').Where(static l => l.Length > 0).ToList();
        Assert.Equal(listing, lines.Where(static l => !l.StartsWith('#')));
        Assert.Equal(["Witness.Status.Overflow = 3"], lines.Where(static l => l.StartsWith(ApiListing.ProvisionalMark, StringComparison.Ordinal)).Select(static l => l[ApiListing.ProvisionalMark.Length..]));
        Assert.Empty(SharedRules.SL6("Witness", listing, lines.Where(static l => !l.StartsWith('#')).ToList()));
    }

    [Fact]
    public void SL6_A_drift_in_either_direction_is_refused()
    {
        var listing = ApiListing.Read(Witness.Emit("SL6-api-drift.cs.witness"));
        var baseline = Witness.Text("SL6-api-drift.PublicAPI.txt.witness").Split('\n').Where(static l => l.Length > 0 && !l.StartsWith('#')).ToList();
        var violations = SharedRules.SL6("Witness", listing, baseline).ToList();
        Assert.Contains("Witness has public API the baseline does not record: Witness.Status.Overflow = 3", violations);
        Assert.Contains("Witness's baseline records public API the assembly does not have: Witness.Status.Malformed = 1", violations);
        Assert.Equal(2, violations.Count);

        // And a packed project without package validation, or validated against another version, is refused.
        var packed = new Dictionary<string, string>(StringComparer.Ordinal) { ["IsPackable"] = "true", ["EnablePackageValidation"] = "false", ["PackageValidationBaselineVersion"] = "0.1.0-preview.3" };
        Assert.Equal(
            ["Witness is packed without package validation", "Witness validates its package against '0.1.0-preview.3', not the last published version " + LastPublishedVersion],
            SharedRules.SL6PackageValidation("Witness", packed, LastPublishedVersion));
    }

    // ---------------------------------------------------------------------------------------------

    private static IEnumerable<(string Path, string Text)> LibraryFiles(SharedProject library) =>
        SharedTree.SourceFiles("*")
            .Where(path => path.StartsWith(library.Directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(static path => (SharedTree.Relative(path), File.ReadAllText(path)));

    [Fact]
    public void SL7_The_library_holds_no_generated_data_it_cannot_regenerate_from_a_pin()
    {
        var violations = new List<string>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL7"))
        {
            var files = LibraryFiles(library).ToList();

            // Not vacuous: the rule reads every file of the library's directory, its project file and its sources.
            Assert.Contains(files, static f => f.Path == "Broiler.Regex/Broiler.Regex.csproj");
            Assert.Contains(files, static f => f.Path == "Broiler.Regex/Parsing/RegexParser.cs");
            violations.AddRange(SharedRules.SL7(files));
        }

        StartingViolations.AssertExactly("SL7", violations);
    }

    [Fact]
    public void SL7_A_generated_source_and_a_generator_script_are_refused()
    {
        Assert.Equal(
            [
                "Broiler.Regex/Unicode/Witness.cs is generated data, and this repository pins no input it could be regenerated from",
                "Broiler.Regex/tools/generate.py is a generator script in a library, which holds no generated data",
            ],
            SharedRules.SL7([("Broiler.Regex/Unicode/Witness.cs", Witness.Text("SL7-generated-source.cs.witness")), ("Broiler.Regex/tools/generate.py", "print()\n")]));

        // A hand-written source that names the marker below its head is not generated data.
        Assert.Empty(SharedRules.SL7([("Broiler.Regex/Ok.cs", "namespace A;\n\n\n\n\n\n// <auto-generated> appears in prose here\n")]));
    }

    // ---------------------------------------------------------------------------------------------

    private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private static IEnumerable<string> LibraryProjectPaths =>
        SharedTree.LibrariesHeldBy("SL8").Select(static p => SharedTree.Relative(p.Path));

    [Fact]
    public void SL8_The_assurance_scheme_covers_every_library_it_holds_and_CI_gates_on_it()
    {
        Assert.Equal(["Broiler.Regex/Broiler.Regex.csproj"], LibraryProjectPaths);

        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.Root, "assurance.config.json")), Lenient);
        var violations = SharedRules.SL8(config, LibraryProjectPaths).ToList();

        foreach (var artefact in (string[])["CODE-ASSURANCE.md", "HUMAN_REVIEW.units.md", "assurance.manifest.json"])
            Assert.True(File.Exists(Path.Combine(SharedTree.Root, artefact)), artefact + " has not been generated");

        // Every source of every library SL8 holds is in the manifest's files array, which is what makes the record
        // complete: a file the tool did not cover is absent.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.Root, "assurance.manifest.json")));
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().Select(static f => f.GetProperty("file").GetString()!).ToHashSet(StringComparer.Ordinal);
        foreach (var library in SharedTree.LibrariesHeldBy("SL8"))
        {
            var sources = SharedTree.SourceFiles("*.cs")
                .Where(p => p.StartsWith(library.Directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(SharedTree.Relative)
                .ToList();
            Assert.Contains("Broiler.Regex/Parsing/RegexParser.cs", files);
            violations.AddRange(SharedRules.SL8Manifest(files, sources));
        }

        var workflow = File.ReadAllText(Path.Combine(SharedTree.Root, ".github", "workflows", "ci.yml"));
        if (!workflow.Contains("assurance check --root Broiler.RegEx", StringComparison.Ordinal))
            violations.Add(".github/workflows/ci.yml runs no assurance check over Broiler.RegEx");

        StartingViolations.AssertExactly("SL8", violations);
    }

    [Fact]
    public void SL8_The_witness_configuration_is_refused()
    {
        using var config = JsonDocument.Parse(Witness.Text("SL8-config-omits-a-library.json.witness"), Lenient);
        var violations = SharedRules.SL8(config, LibraryProjectPaths).ToList();
        Assert.Contains("assurance.config.json does not cover Broiler.Regex/Broiler.Regex.csproj", violations);
        Assert.Contains("assurance.config.json does not forbid preprocessor directives", violations);
        Assert.Contains("assurance.config.json does not use the strict exemption predicate", violations);
        Assert.Contains("assurance.config.json excludes 'Broiler.Regex/Parsing/RegexParser.cs' without a reason", violations);
        Assert.Contains("assurance.config.json excludes 'Broiler.Regex/Parsing/RegexParser.cs', which can hide a unit of a library SL8 holds", violations);

        // A configuration that covers the library under the strict predicate, forbids directives and excludes
        // nothing is accepted; and a source the manifest does not list is refused.
        using var accepted = JsonDocument.Parse("""{ "projects": ["Broiler.Regex/Broiler.Regex.csproj"], "exemptionPredicate": "strict", "forbidDirectives": true }""");
        Assert.Empty(SharedRules.SL8(accepted, LibraryProjectPaths));
        Assert.Equal(
            ["assurance.manifest.json does not list Broiler.Regex/X.cs, a source of a library SL8 holds"],
            SharedRules.SL8Manifest(new HashSet<string>(StringComparer.Ordinal) { "Broiler.Regex/Y.cs" }, ["Broiler.Regex/X.cs", "Broiler.Regex/Y.cs"]));
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>One run over compiled cases through an answer function: every case's answer, in order.</summary>
    private static string Run(IReadOnlyList<CompiledCase> compiled, Func<CompiledCase, string> answer)
    {
        var report = new StringBuilder();
        foreach (var item in compiled)
            report.Append(item.Case.Id).Append(' ').Append(answer(item)).Append('\n');
        return report.ToString();
    }

    /// <summary>
    /// Determinism: the probe's population, compiled once as an engine compiles a pattern once, run twice in
    /// sequence and twice on two threads through the same instances, gives the same answers each time
    /// (conventions 6 and 10). The vectors replace the population when SJL-11.2 adds them.
    /// </summary>
    [Fact]
    public async Task SL9_Two_runs_and_two_threads_over_the_probe_population_give_identical_output()
    {
        var compiled = ProbePopulation.Compile(ProbePopulation.Cases());
        string[] sequential = [Run(compiled, ProbePopulation.Answer), Run(compiled, ProbePopulation.Answer)];
        var threads = await Task.WhenAll(Task.Run(() => Run(compiled, ProbePopulation.Answer)), Task.Run(() => Run(compiled, ProbePopulation.Answer)));
        string[] runs = [.. sequential, .. threads];

        StartingViolations.AssertExactly("SL9", SharedRules.SL9(runs));

        // Not vacuous: every case is answered, and the population reaches matches, refusals and the Unicode data.
        Assert.Equal(compiled.Length, runs[0].Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains(" match ", runs[0], StringComparison.Ordinal);
        Assert.Contains(" syntax @", runs[0], StringComparison.Ordinal);
        Assert.Contains("\nP08 match ", runs[0], StringComparison.Ordinal);
    }

    [Fact]
    public void SL9_The_witness_producer_is_refused()
    {
        var compiled = ProbePopulation.Compile(ProbePopulation.Cases());
        var witness = new NondeterministicAnswerWitness();
        Assert.NotEmpty(SharedRules.SL9([Run(compiled, witness.Answer), Run(compiled, witness.Answer)]));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL10_No_source_holds_a_preprocessor_directive_and_no_file_a_CR()
    {
        var sources = SharedTree.SourceFiles("*.cs").ToList();
        Assert.Contains(sources, static s => s.EndsWith("RegexParser.cs", StringComparison.Ordinal));
        Assert.Contains(sources, static s => s.EndsWith("Program.cs", StringComparison.Ordinal));
        var violations = sources.SelectMany(static p => SharedRules.SL10Directives(SharedTree.Relative(p), File.ReadAllText(p))).ToList();

        var text = SharedTree.RepositoryTextFiles().ToList();
        Assert.Contains(text, static p => SharedTree.Relative(p) == "tests/Adversarial/corpus.tsv");
        Assert.Contains(text, static p => SharedTree.Relative(p) == ".github/workflows/ci.yml");
        Assert.Contains(text, static p => SharedTree.Relative(p) == "docs/roadmap.md");
        violations.AddRange(text.SelectMany(static p => SharedRules.SL10LineEndings(SharedTree.Relative(p), File.ReadAllBytes(p))));

        // Read by line: Git checks .gitattributes out with the platform's line endings. Every text file of the
        // repository is given LF by the root's line.
        if (!File.ReadAllLines(Path.Combine(SharedTree.Root, ".gitattributes")).Contains("* text=auto eol=lf"))
            violations.Add(".gitattributes gives the repository's text files no LF line endings");

        StartingViolations.AssertExactly("SL10", violations);
    }

    [Fact]
    public void SL10_The_witnesses_are_refused()
    {
        var directives = SharedRules.SL10Directives("w.cs", Witness.Text("SL10-preprocessor-directive.cs.witness")).ToList();
        Assert.Equal(["w.cs:7: #if DEBUG", "w.cs:9: #else", "w.cs:11: #endif", "w.cs:13: #pragma warning disable CS0618", "w.cs:15: #pragma warning restore CS0618"], directives);

        Assert.NotEmpty(SharedRules.SL10LineEndings("w.cs", File.ReadAllBytes(Witness.PathOf("SL10-crlf-line-endings.cs.witness"))));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void C1_No_public_signature_names_a_collection_a_delegate_object_or_Type()
    {
        var listing = LibraryApi;
        StartingViolations.AssertExactly("C1", SharedRules.C1(Library, listing));

        // Not vacuous: the listing holds signatures of neutral types, which C1 reads and passes.
        Assert.Contains("Broiler.Regex.BroilerRegex.get_CaptureCount() method System.Int32", listing);
        Assert.DoesNotContain(SharedRules.C1(Library, listing), static v => v.EndsWith("get_CaptureCount() method System.Int32", StringComparison.Ordinal));
    }

    [Fact]
    public void C1_The_witness_is_refused_for_each_non_neutral_type()
    {
        var violations = SharedRules.C1("Witness", ApiListing.Read(Witness.Emit("C1-collection-in-a-signature.cs.witness")), ["Witness.Meter"]).ToList();
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Groups(System.String) method static System.Collections.Generic.IReadOnlyList`1<System.Int32>", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Map(System.Func`2<System.Int32,System.Int32>) method static System.Int32", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Box(System.Object) method static System.Int32", StringComparison.Ordinal));

        // A delegate of the witness's own that convention 1 does not admit runs the caller's code like Func: its
        // declaration, its runtime-made members that name object, and a member that takes it are refused.
        Assert.Contains(violations, static v => v.EndsWith(": Witness.Filter delegate", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith(": Witness.Filter..ctor(System.Object, System.IntPtr) method System.Void", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains(": Witness.Filter.BeginInvoke(", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith(": Witness.Hooks.Count(System.ReadOnlySpan`1<System.Char>, Witness.Filter) method static System.Int32", StringComparison.Ordinal));

        // The neutral member and the record's own machinery, which names object and Type because the platform
        // does, are not refused; nor is the delegate admitted, its runtime-made members or a member taking it.
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Api.Length(", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Span.", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Meter", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Api.Metered(", StringComparison.Ordinal));
        Assert.Equal(7, violations.Count);

        // The library admits its meter alone: a planted delegate of its own is refused as the filter is.
        Assert.Equal(["Broiler.Regex.RegexWorkCharge"], SharedRules.AdmittedDelegates);
    }

    [Fact]
    public void C3_No_public_type_reports_a_failure_the_contract_admits_as_an_exception()
    {
        StartingViolations.AssertExactly("C3", SharedRules.C3(Library, LibraryApi));
    }

    [Fact]
    public void C3_The_witness_is_refused()
    {
        var violations = SharedRules.C3("Witness", ApiListing.Read(Witness.Emit("C3-exception-for-an-expected-failure.cs.witness"))).ToList();
        Assert.Equal(["Witness declares a public exception type for a failure its contract admits: Witness.PatternException sealed class : System.Exception"], violations);
    }

    [Fact]
    public void C5_No_library_makes_the_stack_a_limit()
    {
        StartingViolations.AssertExactly("C5", AssembliesHeldBy("C5").SelectMany(SharedRules.C5Findings));
    }

    [Fact]
    public void C5_The_witness_is_refused()
    {
        var violations = SharedRules.C5(Witness.Compile("C5-stack-probe.cs.witness")).ToList();
        Assert.Contains(violations, static v => v.EndsWith("calls System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack(): the stack is a limit", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("calls System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack(): the stack is a limit", StringComparison.Ordinal));
    }
}

/// <summary>
/// SL9's witness: an answer that depends on process history, a counter, which is what convention 6 forbids. Two
/// runs through it differ.
/// </summary>
internal sealed class NondeterministicAnswerWitness
{
    private int calls;

    internal string Answer(CompiledCase item) => (++calls).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
