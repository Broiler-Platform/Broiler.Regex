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
            ["Broiler.Regex.Tests", "Regex.Architecture.Tests"],
            SharedTree.Projects.Where(static p => p.Kind == SharedProjectKind.Test).Select(static p => p.Name));
        Assert.All(Assemblies.Values, static a => Assert.NotEmpty(a.TypeReferences));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL1_No_library_references_an_engine_Broiler_VM_or_anything_outside_the_layering()
    {
        var violations = new List<string>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL1"))
        {
            violations.AddRange(SharedRules.SL1(library.Name, library.Directory, library.Document));
            using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(library.Directory, "obj", "project.assets.json")));
            violations.AddRange(SharedRules.SL1Restore(library.Name, assets));

            // Not vacuous: the project names packages, and its restore resolves them.
            Assert.NotEmpty(library.Items("PackageReference"));
            Assert.Contains(assets.RootElement.GetProperty("libraries").EnumerateObject(), static l => l.Value.GetProperty("type").GetString() == "package");
        }

        violations.AddRange(AssembliesHeldBy("SL1").SelectMany(SharedRules.SL1Assembly));
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

        // This repository's addition: a static field holding an object of the library's own type that changes on
        // a read, the shape of a lazily normalised set, directly and through a base class; an immutable one passes.
        Assert.Contains(violations, static v => v.Contains("AmbientState.Shared is a static Witness.LazySet, which can be changed after construction: Witness.LazySet.normalized is not readonly", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Derived is a static Witness.DerivedSet, which can be changed after construction: Witness.LazySet.normalized is not readonly", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("AmbientState.Fixed", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL3_No_library_reaches_dynamic_code_hidden_machinery_IO_or_the_environment()
    {
        var violations = AssembliesHeldBy("SL3").SelectMany(static a => SharedRules.SL3(a)).ToList();

        // The probe is an executable that writes its report to a file; SL3 holds it all the same but for I/O,
        // since it is the code that runs under Native AOT (docs/conventions.md, "Scope").
        var probe = ProbeAssembly;
        Assert.Contains("System.IO.File", probe.TypeReferences);
        violations.AddRange(SharedRules.SL3(probe, allowIo: true));
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
            "declares the platform invoke libc!getpid",
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
        StartingViolations.AssertExactly("SL4", AssembliesHeldBy("SL4").SelectMany(SharedRules.SL4));

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
        ];
        foreach (var fragment in expected)
            Assert.Contains(violations, v => v.Contains(fragment, StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL5_The_library_and_the_probe_hold_to_AOT_and_trimming_with_nothing_suppressed()
    {
        var violations = new List<string>();
        foreach (var library in SharedTree.LibrariesHeldBy("SL5"))
        {
            var evaluated = Tools.Evaluate(library.Path, SharedRules.LibraryProperties.Select(static p => p.Property).Concat(SuppressionProperties));
            violations.AddRange(SharedRules.SL5Properties(library.Name, evaluated, SharedRules.LibraryProperties));
        }

        var probeProperties = Tools.Evaluate(Probe.Path, SharedRules.ProbeProperties.Select(static p => p.Property).Concat(SuppressionProperties));
        violations.AddRange(SharedRules.SL5Properties(Probe.Name, probeProperties, SharedRules.ProbeProperties));

        violations.AddRange(AssembliesHeldBy("SL5").SelectMany(SharedRules.SL5Assembly));
        violations.AddRange(SharedRules.SL5Assembly(ProbeAssembly));
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
            .Concat(BuildFiles.Select(static f => Path.Combine(SharedTree.Root, f)))
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
        Assert.Contains(files, static p => p.EndsWith("Directory.Build.props", StringComparison.Ordinal));
    }

    /// <summary>The build files every project imports, relative to the root.</summary>
    private static readonly string[] BuildFiles = ["Directory.Build.props", "Directory.Packages.props", "eng/Broiler.Packaging.props"];

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
                File.WriteAllText(baseline, ApiListing.Render(library.Name, listing));
            violations.AddRange(SharedRules.SL6(library.Name, listing, ApiListing.ReadBaseline(baseline)));

            var evaluated = Tools.Evaluate(library.Path, ["IsPackable", "EnablePackageValidation", "PackageValidationBaselineVersion"]);
            Assert.Equal("true", evaluated["IsPackable"]);
            violations.AddRange(SharedRules.SL6PackageValidation(library.Name, evaluated, LastPublishedVersion));
        }

        StartingViolations.AssertExactly("SL6", violations);
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
        Assert.Contains(sources, static s => s.EndsWith("CaseFoldingData.g.cs", StringComparison.Ordinal));
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
        var violations = SharedRules.C1("Witness", ApiListing.Read(Witness.Emit("C1-collection-in-a-signature.cs.witness"))).ToList();
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Groups(System.String) method static System.Collections.Generic.IReadOnlyList`1<System.Int32>", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Map(System.Func`2<System.Int32,System.Int32>) method static System.Int32", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("Witness.Api.Box(System.Object) method static System.Int32", StringComparison.Ordinal));

        // The neutral member and the record's own machinery, which names object and Type because the platform
        // does, are not refused.
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Api.Length(", StringComparison.Ordinal));
        Assert.DoesNotContain(violations, static v => v.Contains("Witness.Span.", StringComparison.Ordinal));
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
        StartingViolations.AssertExactly("C5", AssembliesHeldBy("C5").SelectMany(SharedRules.C5));
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
