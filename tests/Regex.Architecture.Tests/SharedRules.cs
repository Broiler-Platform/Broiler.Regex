using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// A violation a rule finds, with the places in the source where it is when the rule reads them: from the IL and the
/// PDB for a rule over member and type references (SL1's assembly half, SL3, SL4, C5), from the project files for
/// SL5's suppression codes. Null places mean the violation names its one place itself: a field, a member, a file,
/// a package or a setting. <see cref="StartingViolations.AssertExactly(string, IEnumerable{Finding})"/> asserts a
/// placed violation's rows place by place.
/// </summary>
internal sealed record Finding(string Violation, IReadOnlyList<SourcePlace>? Places = null);

/// <summary>
/// Broiler.RegEx's rules of the programme's group SL (governance section 3.2), and the three contract checks
/// beside them, each a function from what it reads to the violations it finds. An empty answer is a pass; the
/// tests also assert that each rule read a real subject, because a rule that passes by finding nothing passes
/// most easily over nothing. Ported from Broiler.Unicode's Unicode.Architecture.Tests; what differs is this
/// repository's subject, and each difference is named where it is.
/// </summary>
internal static class SharedRules
{
    // ---------------------------------------------------------------------------------------------
    // SL1: references, in the layering direction only.

    /// <summary>
    /// The layering of the programme as it reaches this repository: each library, by assembly name, and what it
    /// may reference. Broiler.Regex reads its Unicode data through the Unicode owner's provider and references
    /// Broiler.Unicode.Algorithms alone (requirement R11, decision D11 of Broiler.VM's JSD-0081); the owner's
    /// tables, Broiler.Unicode.Tables, are test-only data. A library that is not here has no place in the
    /// layering yet, which is a violation rather than a pass.
    /// </summary>
    internal static readonly FrozenDictionary<string, string[]> Layering = new Dictionary<string, string[]>
    {
        ["Broiler.Regex"] = ["Broiler.Unicode.Algorithms"],
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The component packages a library may reference at all, where the layering lets it: the programme's
    /// architecture puts Broiler.Unicode below Broiler.Regex, and of its packages the provider's alone.
    /// </summary>
    internal static readonly string[] ComponentPackagePrefixes = ["Broiler.Unicode.Algorithms"];

    /// <summary>Packages the SDK adds to every trimmable project on its own; they flow to no consumer.</summary>
    internal static readonly FrozenSet<string> ImplicitPackages = FrozenSet.Create(StringComparer.Ordinal, "Microsoft.NET.ILLink.Tasks");

    /// <summary>SL1 over a project file: every project and package reference against the layering.</summary>
    internal static IEnumerable<string> SL1(string projectName, string projectDirectory, XDocument project)
    {
        if (!Layering.TryGetValue(projectName, out var allowed))
        {
            yield return projectName + " has no place in the layering; add it to SharedRules.Layering with the edges the programme's architecture gives it";
            yield break;
        }

        foreach (var include in project.Descendants("ProjectReference").Select(static e => (string?)e.Attribute("Include")).OfType<string>())
        {
            var target = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
            var referenced = SharedTree.Projects.FirstOrDefault(p => string.Equals(p.Path, target, StringComparison.Ordinal));
            if (referenced is null)
                yield return projectName + " references the project " + include + ", outside this repository's projects: an engine, Broiler.VM or anything else";
            else if (!allowed.Contains(referenced.Name, StringComparer.Ordinal))
                yield return projectName + " references " + referenced.Name + ", against the layering";
        }

        foreach (var include in project.Descendants("PackageReference").Select(static e => (string?)e.Attribute("Include")).OfType<string>())
        {
            if (!IsAllowedPackage(include, allowed))
                yield return projectName + " references the package " + include + ", outside the allow-list or against the layering";
        }
    }

    /// <summary>SL1 over a restore: every package the assets file resolved, direct or transitive.</summary>
    internal static IEnumerable<string> SL1Restore(string projectName, JsonDocument assets)
    {
        var allowed = Layering.TryGetValue(projectName, out var edges) ? edges : [];
        foreach (var library in assets.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (library.Value.GetProperty("type").GetString() != "package")
                continue;
            var id = library.Name[..library.Name.IndexOf('/', StringComparison.Ordinal)];
            if (!ImplicitPackages.Contains(id) && !IsAllowedPackage(id, allowed))
                yield return projectName + " restores the package " + library.Name + ", outside the allow-list or against the layering";
        }
    }

    /// <summary>SL1 over a built assembly: every assembly it references is the framework's or a library it may reference.</summary>
    internal static IEnumerable<string> SL1Assembly(AssemblyFacts assembly) => SL1AssemblyFindings(assembly).Select(static f => f.Violation);

    /// <summary>
    /// <see cref="SL1Assembly"/> with each reference's places: every instruction that names a type of the assembly,
    /// as the declaring type of the member it calls or as its type operand.
    /// </summary>
    internal static IEnumerable<Finding> SL1AssemblyFindings(AssemblyFacts assembly)
    {
        var allowed = Layering.TryGetValue(assembly.Name, out var edges) ? edges : [];
        return assembly.AssemblyReferences
            .Where(reference => !IsFramework(reference) && !IsAllowedPackage(reference, allowed))
            .Select(reference => new Finding(assembly.Name + " references the assembly " + reference, assembly.PlacesOf("assembly:" + reference)));
    }

    private static bool IsAllowedPackage(string id, string[] allowed) =>
        allowed.Contains(id, StringComparer.Ordinal)
        && ComponentPackagePrefixes.Any(prefix => id.StartsWith(prefix, StringComparison.Ordinal));

    private static bool IsFramework(string assemblyName) =>
        assemblyName is "System" or "netstandard" or "mscorlib" || assemblyName.StartsWith("System.", StringComparison.Ordinal);

    // ---------------------------------------------------------------------------------------------
    // SL2: no mutable static state.

    /// <summary>Types whose instances can be changed after construction, so that a static readonly field of one is still mutable state.</summary>
    private static readonly string[] MutableTypes =
    [
        "System.Collections.Generic.List`1", "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.SortedSet`1", "System.Collections.Generic.SortedDictionary`2", "System.Collections.Generic.SortedList`2",
        "System.Collections.Generic.Queue`1", "System.Collections.Generic.Stack`1", "System.Collections.Generic.LinkedList`1",
        "System.Collections.Generic.PriorityQueue`2", "System.Collections.Concurrent.ConcurrentDictionary`2",
        "System.Collections.Concurrent.ConcurrentBag`1", "System.Collections.Concurrent.ConcurrentQueue`1",
        "System.Collections.Concurrent.ConcurrentStack`1", "System.Collections.ArrayList", "System.Collections.Hashtable",
        "System.Text.StringBuilder", "System.Random",
    ];

    private static bool IsMutableType(string fieldType) =>
        fieldType.EndsWith(']') || MutableTypes.Any(t => fieldType == t || fieldType.StartsWith(t + "<", StringComparison.Ordinal));

    /// <summary>
    /// SL2 over a built assembly. What differs from Broiler.Unicode's rule:
    /// <list type="bullet">
    /// <item>A static readonly field whose type is a class the library itself declares is mutable state too when that
    /// class's instances can change after construction: an instance field that is not readonly, one of a mutable
    /// collection or array type, or a readonly one holding such a class of the library's, in it or in a base class it
    /// declares. Convention 7 asks that static data be immutable, and convention 10 that a shared object be safe to
    /// read concurrently; a class that normalises itself in place on a read is neither, whether the type is the base
    /// class library's or the library's own. The row names the static field alone, so that a change that fixes one of
    /// the class's fields and leaves another changes no row; <see cref="MutableInstanceState"/> says which.</item>
    /// <item>A static readonly field whose type is a collection interface, or a class of the library's that keeps a
    /// collection interface in an instance field, is mutable state when its initializer creates a mutable collection
    /// or array (<see cref="AssemblyFacts.StaticInitializerCreations"/>): a read-only interface hides a mutable
    /// instance from the declared types, as <c>RegexMatch.Empty</c>'s dictionary is hidden.</item>
    /// <item>A static auto-property's backing field is read as a static field, so a get-only property of a mutable
    /// type is mutable state although no setter exists.</item>
    /// </list>
    /// </summary>
    internal static IEnumerable<string> SL2(AssemblyFacts assembly)
    {
        foreach (var field in assembly.StaticFields)
        {
            var where = assembly.Name + ": " + field.Type + "." + field.Name;
            if (field.IsLiteral)
                continue;
            if (!field.IsReadOnly)
                yield return where + " is a mutable static field";
            else if (field.FieldType.EndsWith(']'))
                yield return where + " is a static array, whose elements can be changed: use a constant span or an immutable collection";
            else if (MutableTypes.Any(t => field.FieldType == t || field.FieldType.StartsWith(t + "<", StringComparison.Ordinal)))
                yield return where + " is a static " + field.FieldType + ", which can be changed after construction";
            else if (MutableInstanceState(assembly, field.FieldType).Count > 0)
                yield return where + " is a static " + field.FieldType + ", an object of the library's own class that can be changed after construction";
            else if (HidesBehindAnInterface(assembly, field.FieldType, []) && CreatesMutableState(assembly, field))
                yield return where + " is a static " + field.FieldType + " whose initializer keeps a mutable collection behind a collection interface, which can be changed after construction";
        }

        foreach (var property in assembly.SettableStaticProperties)
            yield return assembly.Name + ": " + property + " is a settable static property";

        if (assembly.CustomAttributeTypes.Contains("System.ThreadStaticAttribute"))
            yield return assembly.Name + " declares a [ThreadStatic] field";

        foreach (var type in assembly.TypeReferences.Where(static t => t is "System.Threading.ThreadLocal`1" or "System.Threading.AsyncLocal`1"))
            yield return assembly.Name + " names " + type;
    }

    /// <summary>
    /// Every instance field that lets an object of <paramref name="type"/>, a class the assembly declares, change after
    /// construction, in it or in a base class the assembly declares: one that is not readonly, one of a mutable
    /// collection or array type, and a readonly one holding a class of the assembly's that can change. Empty when
    /// there is none, or when the type is not a class of this assembly.
    /// </summary>
    internal static IReadOnlyList<string> MutableInstanceState(AssemblyFacts assembly, string type) =>
        MutableInstanceState(assembly, type, new HashSet<string>(StringComparer.Ordinal));

    private static List<string> MutableInstanceState(AssemblyFacts assembly, string type, HashSet<string> visiting)
    {
        var reasons = new List<string>();
        if (!visiting.Add(type))
            return reasons;
        for (var current = type; current is not null; current = assembly.BaseTypes.GetValueOrDefault(current))
        {
            foreach (var field in assembly.InstanceFields.Where(f => f.Type == current))
            {
                if (!field.IsReadOnly)
                    reasons.Add(field.Type + "." + field.Name + " is not readonly");
                else if (IsMutableType(field.FieldType))
                    reasons.Add(field.Type + "." + field.Name + " is a " + field.FieldType);
                else if (assembly.Classes.Contains(field.FieldType) && MutableInstanceState(assembly, field.FieldType, visiting).Count > 0)
                    reasons.Add(field.Type + "." + field.Name + " holds a " + field.FieldType + ", which can be changed after construction");
            }
        }

        visiting.Remove(type);
        return reasons;
    }

    /// <summary>
    /// Whether a value of <paramref name="type"/> can hold a collection behind an interface: the type is a collection
    /// interface, or a class of the assembly's with an instance field of one, or a readonly field of such a class, in
    /// it or in a base class.
    /// </summary>
    private static bool HidesBehindAnInterface(AssemblyFacts assembly, string type, HashSet<string> visiting)
    {
        if (IsCollectionInterface(type))
            return true;
        if (!assembly.Classes.Contains(type) || !visiting.Add(type))
            return false;
        for (var current = type; current is not null; current = assembly.BaseTypes.GetValueOrDefault(current))
        {
            if (assembly.InstanceFields.Any(f => f.Type == current && (IsCollectionInterface(f.FieldType) || HidesBehindAnInterface(assembly, f.FieldType, visiting))))
                return true;
        }

        return false;
    }

    /// <summary>An interface of <c>System.Collections</c> or its namespaces, generic or not: <c>IReadOnlyDictionary`2</c>, <c>IList</c>.</summary>
    private static bool IsCollectionInterface(string type)
    {
        var definition = type.Split('<')[0];
        if (!definition.StartsWith("System.Collections.", StringComparison.Ordinal))
            return false;
        var name = definition[(definition.LastIndexOf('.') + 1)..];
        return name.Length > 1 && name[0] == 'I' && char.IsAsciiLetterUpper(name[1]);
    }

    /// <summary>Whether the static constructor creates a mutable collection or a non-empty array for <paramref name="field"/>.</summary>
    private static bool CreatesMutableState(AssemblyFacts assembly, StaticField field) =>
        assembly.StaticInitializerCreations.TryGetValue(field.Type + "." + field.Name, out var created) && created.Any(IsMutableType);

    // ---------------------------------------------------------------------------------------------
    // SL3: no dynamic code, no hidden machinery, no I/O, no environment.

    /// <summary>Broiler.VM's B5 list, by exact member name, and convention 8's additions.</summary>
    private static readonly string[] DynamicMembers =
    [
        "System.Reflection.Assembly.Load", "System.Reflection.Assembly.LoadFrom", "System.Reflection.Assembly.LoadFile",
        "System.Reflection.Assembly.UnsafeLoadFrom", "System.Reflection.Assembly.GetTypes", "System.Reflection.Assembly.GetType",
        "System.Type.GetType", "System.Activator.CreateInstance", "System.Activator.CreateInstanceFrom",
        "System.Reflection.MethodBase.Invoke", "System.Linq.Expressions.LambdaExpression.Compile",
        "System.Runtime.InteropServices.NativeLibrary.Load", "System.Runtime.InteropServices.NativeLibrary.TryLoad",
        "System.Runtime.InteropServices.NativeLibrary.GetExport", "System.Runtime.InteropServices.NativeLibrary.TryGetExport",
        "System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer",
        "System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate",
        "System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod",

        // Convention 8: generic instantiation over a run-time Type, and the Type-taking enum readers.
        "System.Type.MakeGenericType", "System.Type.MakeArrayType", "System.Reflection.MethodInfo.MakeGenericMethod",
        "System.Enum.GetValues", "System.Enum.GetNames", "System.Enum.GetValuesAsUnderlyingType",
    ];

    /// <summary>Namespaces and types where naming the thing at all is the violation.</summary>
    private static readonly string[] HiddenMachineryTypePrefixes =
    [
        "System.Reflection.Emit.", "System.Runtime.Loader.", "Microsoft.CSharp.RuntimeBinder.",
        "System.Runtime.CompilerServices.CallSite", "System.Dynamic.",

        // Reflection-based serialization.
        "System.Text.Json.JsonSerializer", "System.Xml.Serialization.", "System.Runtime.Serialization.",

        // File, network, process and environment.
        "System.IO.File", "System.IO.Directory", "System.IO.FileInfo", "System.IO.DirectoryInfo", "System.IO.FileSystemInfo",
        "System.IO.FileStream", "System.IO.DriveInfo", "System.IO.StreamReader", "System.IO.StreamWriter",
        "System.IO.FileSystemWatcher", "System.IO.Path", "System.IO.Pipes.", "System.IO.MemoryMappedFiles.",
        "System.IO.IsolatedStorage.", "System.Net.", "System.Console", "System.Diagnostics.Process",
        "System.AppContext", "Microsoft.Win32.",
    ];

    /// <summary>
    /// The one member of <c>System.Environment</c> that reads nothing of the environment: the compiler's iterator
    /// and async machinery asks it which thread it is on. Every other member of the type is an environment read and
    /// is refused by member, not by type, for that reason.
    /// </summary>
    private const string CompilerThreadIdentity = "System.Environment.get_CurrentManagedThreadId()";

    /// <summary>
    /// SL3 over a built assembly. <paramref name="allowIo"/> is the probe's: an executable writes its report to a
    /// file, and every other clause still holds it.
    /// </summary>
    internal static IEnumerable<string> SL3(AssemblyFacts assembly, bool allowIo = false) => SL3Findings(assembly, allowIo).Select(static f => f.Violation);

    /// <summary><see cref="SL3"/> with the places of each member, type and assembly it refuses.</summary>
    internal static IEnumerable<Finding> SL3Findings(AssemblyFacts assembly, bool allowIo = false)
    {
        foreach (var member in assembly.MemberReferences)
        {
            var name = member.Split('(')[0];
            if (DynamicMembers.Contains(name, StringComparer.Ordinal))
            {
                // Enum.GetValues<T>() and its siblings are the generic, AOT-safe forms; only the Type-taking overloads are refused.
                if (name.StartsWith("System.Enum.", StringComparison.Ordinal) && !member.Contains("System.Type", StringComparison.Ordinal))
                    continue;
                yield return new Finding(assembly.Name + " calls " + member, assembly.PlacesOf("member:" + member));
            }
            else if (name.StartsWith("System.Environment.", StringComparison.Ordinal) && member != CompilerThreadIdentity && !allowIo)
            {
                yield return new Finding(assembly.Name + " calls " + member + ": an environment read", assembly.PlacesOf("member:" + member));
            }
        }

        foreach (var type in assembly.TypeReferences)
        {
            if (allowIo && IsIoType(type))
                continue;
            if (HiddenMachineryTypePrefixes.Any(prefix => type == prefix || type.StartsWith(prefix, StringComparison.Ordinal) && !IsException(type)))
                yield return new Finding(assembly.Name + " names " + type, assembly.PlacesOf("type:" + type));
            else if (type.StartsWith("System.Reflection.", StringComparison.Ordinal) && !type.EndsWith("Attribute", StringComparison.Ordinal))
                yield return new Finding(assembly.Name + " names " + type + ": reflection", assembly.PlacesOf("type:" + type));
        }

        if (assembly.AssemblyReferences.Contains("Microsoft.CSharp"))
            yield return new Finding(assembly.Name + " references Microsoft.CSharp: dynamic", assembly.PlacesOf("assembly:Microsoft.CSharp"));

        if (assembly.CustomAttributeTypes.Contains("System.Runtime.CompilerServices.ModuleInitializerAttribute"))
            yield return new Finding(assembly.Name + " applies a module initializer");

        foreach (var invoke in assembly.PlatformInvokes)
            yield return new Finding(assembly.Name + " declares the platform invoke " + invoke);
    }

    private static bool IsException(string type) => type.EndsWith("Exception", StringComparison.Ordinal);

    private static bool IsIoType(string type) =>
        type.StartsWith("System.IO.", StringComparison.Ordinal) || type is "System.Console" or "System.Environment" or "System.AppContext";

    // ---------------------------------------------------------------------------------------------
    // SL4: no platform globalization, no regular expressions, no culture.

    /// <summary>Types whose answers come from the platform's culture, time zone or Unicode data, or from a clock.</summary>
    private static readonly string[] PlatformTypePrefixes =
    [
        "System.Text.RegularExpressions.", "System.TimeZoneInfo", "System.Text.Rune", "System.Diagnostics.Stopwatch",
        "System.Globalization.StringNormalizationExtensions",
    ];

    /// <summary>Members that read a culture, the platform's Unicode tables or a clock.</summary>
    private static readonly string[] PlatformMembers =
    [
        "System.String.Normalize", "System.String.IsNormalized",
        "System.String.ToUpper", "System.String.ToLower", "System.String.ToUpperInvariant", "System.String.ToLowerInvariant",
        "System.Char.ToUpper", "System.Char.ToLower", "System.Char.ToUpperInvariant", "System.Char.ToLowerInvariant",
        "System.Char.GetUnicodeCategory", "System.Char.GetNumericValue",
        "System.Char.IsLetter", "System.Char.IsDigit", "System.Char.IsLetterOrDigit", "System.Char.IsNumber", "System.Char.IsUpper",
        "System.Char.IsLower", "System.Char.IsWhiteSpace", "System.Char.IsPunctuation", "System.Char.IsSymbol", "System.Char.IsSeparator",
        "System.Char.IsControl",
        "System.DateTime.get_Now", "System.DateTime.get_UtcNow", "System.DateTime.get_Today",
        "System.DateTimeOffset.get_Now", "System.DateTimeOffset.get_UtcNow",
        "System.String.Compare", "System.String.CompareTo",
    ];

    /// <summary>String members whose overload without a <c>StringComparison</c> compares by the current culture.</summary>
    private static readonly string[] CultureDefaultStringMembers =
    [
        "System.String.StartsWith(System.String", "System.String.EndsWith(System.String", "System.String.IndexOf(System.String",
        "System.String.LastIndexOf(System.String",
    ];

    private static readonly string[] NumericTypes =
    [
        "System.Byte", "System.SByte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
        "System.Int128", "System.UInt128", "System.IntPtr", "System.UIntPtr", "System.Single", "System.Double", "System.Half",
        "System.Decimal", "System.Numerics.BigInteger",
    ];

    internal static IEnumerable<string> SL4(AssemblyFacts assembly) => SL4Findings(assembly).Select(static f => f.Violation);

    /// <summary><see cref="SL4"/> with the places of each member and type it refuses.</summary>
    internal static IEnumerable<Finding> SL4Findings(AssemblyFacts assembly)
    {
        foreach (var type in assembly.TypeReferences)
        {
            if (PlatformTypePrefixes.Any(prefix => type.StartsWith(prefix, StringComparison.Ordinal)))
                yield return new Finding(assembly.Name + " names " + type, assembly.PlacesOf("type:" + type));
            else if (type.StartsWith("System.Globalization.", StringComparison.Ordinal)
                && type is not "System.Globalization.NumberFormatInfo" and not "System.Globalization.NumberStyles")
                yield return new Finding(assembly.Name + " names " + type + ": platform globalization", assembly.PlacesOf("type:" + type));
        }

        foreach (var (member, violation) in SL4Members(assembly))
            yield return new Finding(violation, assembly.PlacesOf("member:" + member));
    }

    private static IEnumerable<(string Member, string Violation)> SL4Members(AssemblyFacts assembly)
    {
        foreach (var member in assembly.MemberReferences)
        {
            var name = member.Split('(')[0];
            var parameters = member[(member.IndexOf('(', StringComparison.Ordinal) + 1)..^1];
            var hasProvider = parameters.Contains("System.IFormatProvider", StringComparison.Ordinal);

            if (PlatformMembers.Contains(name, StringComparer.Ordinal))
            {
                // The Comparison-taking Compare overloads are as culture-free as the others are not.
                if (name is "System.String.Compare" && parameters.Contains("System.StringComparison", StringComparison.Ordinal))
                    continue;
                yield return (member, assembly.Name + " calls " + member);
            }
            else if (CultureDefaultStringMembers.Any(prefix => member.StartsWith(prefix, StringComparison.Ordinal))
                && !parameters.Contains("System.StringComparison", StringComparison.Ordinal))
            {
                yield return (member, assembly.Name + " calls " + member + ", which compares by the current culture");
            }
            else if (name.StartsWith("System.Globalization.NumberFormatInfo.", StringComparison.Ordinal)
                && name != "System.Globalization.NumberFormatInfo.get_InvariantInfo")
            {
                yield return (member, assembly.Name + " calls " + member + ": only NumberFormatInfo.InvariantInfo is culture-free");
            }
            else if (NumericTypes.Any(t => name.StartsWith(t + ".", StringComparison.Ordinal))
                && name.EndsWith("Parse", StringComparison.Ordinal) && !hasProvider)
            {
                yield return (member, assembly.Name + " calls " + member + ", which parses by the current culture");
            }
            else if (NumericTypes.Any(t => name == t + ".ToString" || name == t + ".TryFormat") && parameters.Length > 0 && !hasProvider)
            {
                yield return (member, assembly.Name + " calls " + member + ", which formats by the current culture");
            }
            else if (name.StartsWith("System.Convert.", StringComparison.Ordinal) && !name.Contains("Base64", StringComparison.Ordinal)
                && !name.Contains("Hex", StringComparison.Ordinal)
                && (name == "System.Convert.ToString" || parameters.Contains("System.String", StringComparison.Ordinal)) && !hasProvider)
            {
                yield return (member, assembly.Name + " calls " + member + ", which converts by the current culture");
            }
            else if (name == "System.String.Format" && !parameters.StartsWith("System.IFormatProvider", StringComparison.Ordinal))
            {
                yield return (member, assembly.Name + " calls " + member + ", which formats by the current culture");
            }
            else if (name == "System.Text.StringBuilder.Append" && NumericTypes.Contains(parameters, StringComparer.Ordinal))
            {
                yield return (member, assembly.Name + " calls " + member + ", which formats by the current culture");
            }
            else if (member is "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(System.Int32,System.Int32)"
                || member == "System.Text.StringBuilder+AppendInterpolatedStringHandler..ctor(System.Int32,System.Int32,System.Text.StringBuilder)")
            {
                yield return (member, assembly.Name + " interpolates a string without a format provider, which formats its values by the current culture");
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL5: Native AOT and trimming, nothing suppressed.

    /// <summary>The evaluated properties the library must have, and the values.</summary>
    internal static readonly (string Property, string Value)[] LibraryProperties =
    [
        ("IsAotCompatible", "true"), ("IsTrimmable", "true"), ("EnableTrimAnalyzer", "true"), ("EnableAotAnalyzer", "true"),
        ("EnableSingleFileAnalyzer", "true"), ("TreatWarningsAsErrors", "true"), ("TargetFramework", "net10.0"),
        ("Nullable", "enable"), ("ImplicitUsings", "disable"), ("Deterministic", "true"), ("AllowUnsafeBlocks", "false"),
    ];

    /// <summary>Broiler.VM's root settings, which the probe publishes with (convention 12), and convention 17's.</summary>
    internal static readonly (string Property, string Value)[] ProbeProperties =
    [
        ("PublishAot", "true"), ("TrimMode", "full"), ("InvariantGlobalization", "true"), ("UseSystemResourceKeys", "true"),
        ("TrimmerSingleWarn", "false"), ("TreatWarningsAsErrors", "true"), ("IsAotCompatible", "true"), ("TargetFramework", "net10.0"),
        ("Nullable", "enable"), ("ImplicitUsings", "disable"),
    ];

    /// <summary>The compiler's own informational warnings the SDK puts in NoWarn for every project; they are not trim or AOT warnings.</summary>
    /// <remarks>
    /// IL2121 is in the same position for a project the SDK trims: Microsoft.NET.ILLink.targets adds it to every
    /// such project. It reports an <c>UnconditionalSuppressMessage</c> that suppresses nothing, and SL5 refuses
    /// every such attribute, so it has nothing left to report.
    /// </remarks>
    internal static readonly FrozenSet<string> SdkNoWarn = FrozenSet.Create(StringComparer.Ordinal, "1701", "1702", "IL2121");

    internal static IEnumerable<string> SL5Properties(string project, IReadOnlyDictionary<string, string> evaluated, (string Property, string Value)[] required) =>
        SL5PropertyFindings(project, evaluated, required, []).Select(static f => f.Violation);

    /// <summary>
    /// <see cref="SL5Properties"/> with the places of each suppressed code: every <c>NoWarn</c> or
    /// <c>WarningsNotAsErrors</c> element of <paramref name="files"/> (the project file and the build files it
    /// imports, by path relative to the root, with their text) that names it. A code can be suppressed in several.
    /// </summary>
    internal static IEnumerable<Finding> SL5PropertyFindings(string project, IReadOnlyDictionary<string, string> evaluated, (string Property, string Value)[] required, IReadOnlyList<(string Path, string Text)> files)
    {
        foreach (var (property, value) in required)
        {
            evaluated.TryGetValue(property, out var actual);
            if (!string.Equals(actual, value, StringComparison.OrdinalIgnoreCase))
                yield return new Finding(project + ": " + property + " is '" + actual + "', not '" + value + "'");
        }

        foreach (var code in Codes(evaluated, "NoWarn").Where(static c => !SdkNoWarn.Contains(c)))
            yield return new Finding(project + ": NoWarn suppresses " + code, files.Count == 0 ? null : ElementPlaces(files, "NoWarn", code));
        foreach (var code in Codes(evaluated, "WarningsNotAsErrors"))
            yield return new Finding(project + ": WarningsNotAsErrors exempts " + code, files.Count == 0 ? null : ElementPlaces(files, "WarningsNotAsErrors", code));
        if (evaluated.TryGetValue("SuppressTrimAnalysisWarnings", out var suppress) && suppress.Equals("true", StringComparison.OrdinalIgnoreCase))
            yield return new Finding(project + ": SuppressTrimAnalysisWarnings is true");
    }

    /// <summary>The line spans of every <c>&lt;element&gt;</c> of the files whose codes include <paramref name="code"/>.</summary>
    internal static IReadOnlyList<SourcePlace> ElementPlaces(IReadOnlyList<(string Path, string Text)> files, string element, string code)
    {
        var places = new List<SourcePlace>();
        foreach (var (path, text) in files)
        {
            for (var at = text.IndexOf("<" + element, StringComparison.Ordinal); at >= 0; at = text.IndexOf("<" + element, at + 1, StringComparison.Ordinal))
            {
                var open = text.IndexOf('>', at);
                var close = open < 0 ? -1 : text.IndexOf("</" + element + ">", open, StringComparison.Ordinal);
                if (close < 0 || text[open - 1] == '/' || !char.IsWhiteSpace(text[at + element.Length + 1]) && text[at + element.Length + 1] != '>')
                    continue;
                var codes = text[(open + 1)..close].Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (codes.Contains(code, StringComparer.Ordinal))
                    places.Add(new SourcePlace(path, LineOf(text, at), LineOf(text, close)));
            }
        }

        return places;
    }

    private static int LineOf(string text, int index) => 1 + text.AsSpan(0, index).Count('\n');

    private static IEnumerable<string> Codes(IReadOnlyDictionary<string, string> evaluated, string property) =>
        evaluated.TryGetValue(property, out var value)
            ? value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>SL5 over a built assembly: a suppression the compiler keeps in metadata.</summary>
    internal static IEnumerable<string> SL5Assembly(AssemblyFacts assembly) =>
        assembly.CustomAttributeTypes
            .Where(static a => a is "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute")
            .Select(a => assembly.Name + " applies " + a);

    /// <summary>
    /// The inventory half, in the shape of Broiler.VM's N36: every suppression in a source or project file of the
    /// library and the probe and the build files they import, by file. Text, so a suppression the compiler drops
    /// (<c>SuppressMessage</c> is conditional) is found too.
    /// </summary>
    internal static IEnumerable<string> Suppressions(string relativePath, string text)
    {
        var markers = relativePath.EndsWith(".cs", StringComparison.Ordinal)
            ? new[] { "#pragma warning disable", "SuppressMessage", "UnconditionalSuppressMessage" }
            : new[] { "<NoWarn>", "<WarningsNotAsErrors>", "<SuppressTrimAnalysisWarnings>" };
        foreach (var marker in markers)
        {
            for (var at = text.IndexOf(marker, StringComparison.Ordinal); at >= 0; at = text.IndexOf(marker, at + 1, StringComparison.Ordinal))
            {
                // An element that sets the property empty resets it. One that sets NoWarn to the SDK's own codes
                // alone resets it too: that is what the probe's project file does, to undo the packaging file's additions.
                if (marker[0] == '<' && IsReset(marker, text, at + marker.Length))
                    continue;
                yield return relativePath + ": " + marker.Trim('<', '>');
            }
        }
    }

    private static bool IsReset(string marker, string text, int valueStart)
    {
        var close = text.IndexOf("</", valueStart, StringComparison.Ordinal);
        if (close < 0)
            return false;
        var codes = text[valueStart..close].Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return codes.Length == 0 || marker == "<NoWarn>" && codes.All(SdkNoWarn.Contains);
    }

    // ---------------------------------------------------------------------------------------------
    // SL6: the public API baseline, both directions.

    internal static IEnumerable<string> SL6(string assembly, IReadOnlyList<string> listing, IReadOnlyList<string> baseline)
    {
        var current = listing.ToHashSet(StringComparer.Ordinal);
        var recorded = baseline.ToHashSet(StringComparer.Ordinal);
        foreach (var line in listing.Where(l => !recorded.Contains(l)))
            yield return assembly + " has public API the baseline does not record: " + line;
        foreach (var line in baseline.Where(l => !current.Contains(l)))
            yield return assembly + "'s baseline records public API the assembly does not have: " + line;
    }

    /// <summary>SL6's package-validation half: a packed library validates its package against the last published one.</summary>
    internal static IEnumerable<string> SL6PackageValidation(string project, IReadOnlyDictionary<string, string> evaluated, string baselineVersion)
    {
        if (evaluated.GetValueOrDefault("IsPackable") != "true")
            yield break;
        if (evaluated.GetValueOrDefault("EnablePackageValidation") != "true")
            yield return project + " is packed without package validation";
        if (evaluated.GetValueOrDefault("PackageValidationBaselineVersion") != baselineVersion)
            yield return project + " validates its package against '" + evaluated.GetValueOrDefault("PackageValidationBaselineVersion") + "', not the last published version " + baselineVersion;
    }

    // ---------------------------------------------------------------------------------------------
    // SL7: generated data, of which this repository may hold none.

    /// <summary>The marker a generated C# source carries in its first lines, and the scripts a generator is written in.</summary>
    private static readonly string[] GeneratorScriptExtensions = [".py", ".ps1", ".sh", ".mjs", ".js"];

    /// <summary>
    /// SL7 over the files of a library's directory, each with its path relative to the root and its text. What
    /// differs from Broiler.Unicode's rule, which regenerates its tables from its pin and compares them byte for
    /// byte: this repository pins no data, because decision D11 of Broiler.VM's JSD-0081 puts every table the
    /// library reads in the Unicode owner's provider. So a generated source in the library, which nothing here
    /// can regenerate from a pinned, hashed input, is a violation, and so is a data generator in it. Generated
    /// data a later change adds comes with its pin and a regeneration test in this rule.
    /// </summary>
    internal static IEnumerable<string> SL7(IEnumerable<(string Path, string Text)> files)
    {
        foreach (var (path, text) in files)
        {
            var head = string.Join('\n', text.Split('\n').Take(5));
            if (path.EndsWith(".g.cs", StringComparison.Ordinal) || head.Contains("<auto-generated", StringComparison.Ordinal))
                yield return path + " is generated data, and this repository pins no input it could be regenerated from";
            else if (GeneratorScriptExtensions.Any(e => path.EndsWith(e, StringComparison.Ordinal)))
                yield return path + " is a generator script in a library, which holds no generated data";
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL8: assurance coverage.

    /// <summary>
    /// SL8's structural half: the assurance configuration, <c>assurance.config.json</c> at the root, covers every
    /// library SL8 holds, under the strict exemption predicate, with preprocessor directives forbidden, and hides
    /// no source of one. That every relevant unit carries its block, and that the artefacts are current, is the
    /// assurance tool's <c>check</c>, which CI runs.
    /// </summary>
    internal static IEnumerable<string> SL8(JsonDocument config, IEnumerable<string> libraryProjects)
    {
        var root = config.RootElement;
        var projects = root.TryGetProperty("projects", out var list)
            ? list.EnumerateArray().Select(static p => p.GetString()!).ToHashSet(StringComparer.Ordinal)
            : [];
        var held = libraryProjects.ToList();
        foreach (var project in held.Where(p => !projects.Contains(p)))
            yield return "assurance.config.json does not cover " + project;

        // An exclusion is accepted when it gives its reason and hides no file of a library SL8 holds. Broiler.Unicode
        // excludes generated tables that live in a library SL8 does not hold; this repository's one exclusion is a
        // generated table inside the library itself, which R11 deletes (decision D11).
        var exclusions = root.TryGetProperty("exclude", out var exclude) && exclude.ValueKind == JsonValueKind.Array
            ? exclude.EnumerateArray().ToList()
            : [];
        foreach (var exclusion in exclusions)
        {
            var glob = exclusion.ValueKind == JsonValueKind.Object && exclusion.TryGetProperty("glob", out var g) ? g.GetString() ?? string.Empty : string.Empty;
            if (exclusion.ValueKind != JsonValueKind.Object || !exclusion.TryGetProperty("reason", out var reason) || string.IsNullOrWhiteSpace(reason.GetString()))
                yield return "assurance.config.json excludes '" + glob + "' without a reason";
            if (glob.Length == 0 || glob[0] == '*' || held.Any(p => glob.StartsWith(p[..(p.IndexOf('/', StringComparison.Ordinal) + 1)], StringComparison.Ordinal)))
                yield return "assurance.config.json excludes '" + glob + "', which can hide a unit of a library SL8 holds";
        }

        if (!root.TryGetProperty("forbidDirectives", out var forbid) || forbid.ValueKind != JsonValueKind.True)
            yield return "assurance.config.json does not forbid preprocessor directives";
        if (!root.TryGetProperty("exemptionPredicate", out var predicate) || predicate.GetString() != "strict")
            yield return "assurance.config.json does not use the strict exemption predicate";
    }

    /// <summary>SL8's completeness half: every source of a library SL8 holds is in the manifest's files array.</summary>
    internal static IEnumerable<string> SL8Manifest(IReadOnlySet<string> manifestFiles, IEnumerable<string> librarySources) =>
        librarySources
            .Where(source => !manifestFiles.Contains(source))
            .Select(static source => "assurance.manifest.json does not list " + source + ", a source of a library SL8 holds");

    // ---------------------------------------------------------------------------------------------
    // SL9: determinism.

    internal static IEnumerable<string> SL9(IReadOnlyList<string> runs)
    {
        for (var i = 1; i < runs.Count; i++)
        {
            if (!string.Equals(runs[0], runs[i], StringComparison.Ordinal))
                yield return "run " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " differs from run 1";
        }
    }

    // ---------------------------------------------------------------------------------------------
    // SL10: no preprocessor directives, LF line endings.

    internal static IEnumerable<string> SL10Directives(string relativePath, string text)
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text);
        foreach (var trivia in tree.GetRoot().DescendantTrivia(descendIntoTrivia: true).Where(static t => t.IsDirective))
        {
            var line = trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return relativePath + ":" + line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ": " + trivia.ToString().Trim();
        }
    }

    internal static IEnumerable<string> SL10LineEndings(string relativePath, byte[] content)
    {
        var at = Array.IndexOf(content, (byte)'\r');
        if (at >= 0)
            yield return relativePath + " holds a CR at byte " + at.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // ---------------------------------------------------------------------------------------------
    // The contract checks: conventions 1, 3 and 5 where metadata decides them.

    /// <summary>
    /// The base class library types convention 1 keeps out of a public signature: collections and the delegates
    /// that run a caller's code, <c>object</c> and <c>Type</c>. JSD-0081's section X3 names the first two in
    /// Broiler.Regex at <c>c1422f4d</c>.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex NonNeutralType = new(
        @"System\.Collections\.|System\.(Func|Action|Predicate|Comparison|Converter)`|System\.Action\b|\bSystem\.Object\b|\bSystem\.Type\b|\bSystem\.Delegate\b",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// The members a record or a class gets from <c>System.Object</c> or the compiler's record machinery, which
    /// convention 1 cannot mean: they name <c>object</c> and <c>Type</c> because the platform does.
    /// </summary>
    private static readonly string[] ObjectMachinery = [".Equals(System.Object) method ", ".get_EqualityContract() method ", ".PrintMembers(System.Text.StringBuilder) method "];

    /// <summary>
    /// C1 over a public API listing: every line that names a type convention 1 keeps out. A listing line names the
    /// library's own type or member first, under the library's namespace, so the base class library types it
    /// names are its signature's.
    /// </summary>
    internal static IEnumerable<string> C1(string assembly, IReadOnlyList<string> listing) =>
        listing
            .Where(static line => !ObjectMachinery.Any(m => line.Contains(m, StringComparison.Ordinal)))
            .Where(static line => NonNeutralType.IsMatch(line))
            .Select(line => assembly + " names a base class library collection, delegate, object or Type in a public signature: " + line);

    /// <summary>
    /// C3 over a public API listing: every public type that derives from an exception. Convention 3 makes an
    /// expected failure a status, and a library throws only on a broken precondition, which a base class library
    /// exception states; a public exception type of the library's own is how it reports a failure its contract
    /// admits. ONE STATED LIMIT: an exception that a base class library call throws for an input the contract admits
    /// (the escaped group names of Broiler.VM's correspondence row S2) is invisible in metadata; bundle
    /// sjl-11-001's check A-11 decides it.
    /// </summary>
    internal static IEnumerable<string> C3(string assembly, IReadOnlyList<string> listing) =>
        listing
            .Where(static line => line.Contains(" class : ", StringComparison.Ordinal)
                && line[(line.IndexOf(" : ", StringComparison.Ordinal) + 3)..].Split(", ")[0].EndsWith("Exception", StringComparison.Ordinal))
            .Select(line => assembly + " declares a public exception type for a failure its contract admits: " + line);

    /// <summary>The members that measure or guard the stack, which convention 5 says is never a limit.</summary>
    private static readonly string[] StackProbes =
    [
        "System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack",
        "System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack",
        "System.Threading.Thread.get_MaxStackSize",
    ];

    /// <summary>
    /// C5 over a built assembly: a call that asks how much stack is left. Convention 5 says a limit is never the
    /// stack, because how deep the stack lets code recurse differs between the JIT and Native AOT; a library that
    /// probes it has made the stack its limit. ONE STATED LIMIT: recursion itself is not read here; bundle
    /// sjl-11-001's check A-4 decides it over the matcher's IL call graph.
    /// </summary>
    internal static IEnumerable<string> C5(AssemblyFacts assembly) => C5Findings(assembly).Select(static f => f.Violation);

    /// <summary><see cref="C5"/> with the places of each stack probe.</summary>
    internal static IEnumerable<Finding> C5Findings(AssemblyFacts assembly) =>
        assembly.MemberReferences
            .Where(static member => StackProbes.Contains(member.Split('(')[0], StringComparer.Ordinal))
            .Select(member => new Finding(assembly.Name + " calls " + member + ": the stack is a limit", assembly.PlacesOf("member:" + member)));
}
