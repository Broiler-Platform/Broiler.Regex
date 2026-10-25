using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>What a project of this repository is, for the rules that hold it.</summary>
internal enum SharedProjectKind
{
    /// <summary>The library the engines consume, Broiler.Regex. Every rule and every contract check holds it.</summary>
    Library,

    /// <summary>
    /// The Native AOT probe: an executable. SL3 but for I/O (it writes its report to a file), SL5's probe
    /// properties and SL10 hold it (docs/conventions.md, "Scope").
    /// </summary>
    Probe,

    /// <summary>A test project. SL10 holds it.</summary>
    Test,
}

/// <summary>One project of this repository, read from its project file; <see cref="Name"/> is its assembly's.</summary>
internal sealed record SharedProject(string Name, string Path, SharedProjectKind Kind, XDocument Document)
{
    internal string Directory => System.IO.Path.GetDirectoryName(Path)!;

    internal IEnumerable<string> Items(string itemType) => Document.Descendants(itemType)
        .Select(static element => (string?)element.Attribute("Include"))
        .Where(static include => include is not null)
        .Select(static include => include!);
}

/// <summary>
/// The repository as the rules read it: its projects, the files beside them, and the build output the test
/// project's references put beside this assembly.
/// </summary>
/// <remarks>
/// Broiler.Unicode keeps the programme's projects beside older packages and reads a set of directories named by
/// prefix. Every project of this repository is the programme's, so the rules read all of them: the library at
/// <c>Broiler.Regex/</c>, its tests at <c>Broiler.Regex.Tests/</c>, the probe under <c>probes/</c> and the
/// architecture tests under <c>tests/</c>. A new project in one of these places is read the day it is added, so
/// a new library cannot pass by being unknown: SL1's layering table has no row for it.
/// </remarks>
internal static class SharedTree
{
    /// <summary>The repository root: the first directory above the test assembly that holds the solution.</summary>
    internal static string Root { get; } = FindRoot();

    internal static string TestProjectDirectory => Path.Combine(Root, "tests", "Regex.Architecture.Tests");

    internal static string Witnesses => Path.Combine(TestProjectDirectory, "witnesses");

    internal static string Documents => Path.Combine(Root, "docs");

    /// <summary>Every directory that holds a project, in ordinal order of its path relative to the root.</summary>
    internal static IReadOnlyList<string> ProjectDirectories { get; } = FindProjectDirectories();

    /// <summary>Every project, in ordinal order of assembly name.</summary>
    internal static IReadOnlyList<SharedProject> Projects { get; } = ReadProjects();

    /// <summary>Every library: a project that is neither the probe nor a test.</summary>
    internal static IReadOnlyList<SharedProject> Libraries { get; } =
        Projects.Where(static p => p.Kind == SharedProjectKind.Library).ToList();

    internal static SharedProject Project(string name) => Projects.Single(p => p.Name == name);

    /// <summary>The libraries rule or check <paramref name="rule"/> holds.</summary>
    internal static IEnumerable<SharedProject> LibrariesHeldBy(string rule) => Libraries.Where(p => Holds(p.Kind, rule));

    /// <summary>Whether rule or check <paramref name="rule"/> holds a project of <paramref name="kind"/>; the reasons are on <see cref="SharedProjectKind"/>.</summary>
    internal static bool Holds(SharedProjectKind kind, string rule) => kind switch
    {
        SharedProjectKind.Library => true,
        SharedProjectKind.Probe => rule is "SL3" or "SL5" or "SL10",
        SharedProjectKind.Test => rule == "SL10",
        _ => false,
    };

    /// <summary>
    /// The files of the projects a rule about sources reads: every file of the project directories outside build
    /// output and outside the witnesses, which exist to break the rules.
    /// </summary>
    internal static IEnumerable<string> SourceFiles(string pattern) =>
        ProjectDirectories
            .SelectMany(directory => Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
            .Where(static path => !IsExcluded(path))
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// Every file of the repository SL10 holds to LF line endings: all of them outside version control's own
    /// directory, build output, test results and packed artefacts, except the witnesses, which exist to break the
    /// rules, and the package icon, which is binary.
    /// </summary>
    internal static IEnumerable<string> RepositoryTextFiles() =>
        Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
            .Where(static path => !IsExcluded(path) && !path.EndsWith(".png", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    internal static string Relative(string path) =>
        Path.GetRelativePath(Root, path).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>The built assembly of a library, from this test's output directory.</summary>
    internal static string AssemblyPath(string assemblyName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                assemblyName + ".dll is not in the test output. Every library is referenced by this project so that "
                + "the rules read the build that just happened.", path);
        }

        return path;
    }

    /// <summary>The configuration this test assembly was built in, which the probe was built in too.</summary>
    internal static string Configuration => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

    private static bool IsExcluded(string path)
    {
        var relative = Relative(path);
        var segments = relative.Split('/');
        return segments.Any(static s => s is "bin" or "obj" or "TestResults" or ".git" or ".vs" or "artifacts")
            || relative.StartsWith("tests/Regex.Architecture.Tests/witnesses/", StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Broiler.Regex.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("no Broiler.Regex.slnx above " + AppContext.BaseDirectory);
    }

    private static List<string> FindProjectDirectories()
    {
        var directories = new List<string>();
        foreach (var top in Directory.EnumerateDirectories(Root))
        {
            if (Directory.EnumerateFiles(top, "*.csproj", SearchOption.TopDirectoryOnly).Any())
                directories.Add(top);
        }

        foreach (var area in (string[])["probes", "tests"])
        {
            var parent = Path.Combine(Root, area);
            if (!Directory.Exists(parent))
                continue;
            directories.AddRange(Directory.EnumerateDirectories(parent)
                .Where(static d => Directory.EnumerateFiles(d, "*.csproj", SearchOption.TopDirectoryOnly).Any()));
        }

        directories.Sort((a, b) => string.CompareOrdinal(Relative(a), Relative(b)));
        return directories;
    }

    private static List<SharedProject> ReadProjects()
    {
        var projects = new List<SharedProject>();
        foreach (var path in ProjectDirectories.SelectMany(static d => Directory.EnumerateFiles(d, "*.csproj", SearchOption.TopDirectoryOnly)))
        {
            var document = XDocument.Load(path);
            var assemblyName = document.Descendants("AssemblyName").Select(static e => e.Value.Trim()).FirstOrDefault();
            var name = string.IsNullOrEmpty(assemblyName) ? Path.GetFileNameWithoutExtension(path) : assemblyName;
            var isTest = document.Descendants("IsTestProject").Any(static e => e.Value.Trim() == "true");
            var isExe = document.Descendants("OutputType").Any(static e => e.Value.Trim() == "Exe");
            var kind = isTest ? SharedProjectKind.Test : isExe ? SharedProjectKind.Probe : SharedProjectKind.Library;
            projects.Add(new SharedProject(name, path, kind, document));
        }

        projects.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        return projects;
    }
}
