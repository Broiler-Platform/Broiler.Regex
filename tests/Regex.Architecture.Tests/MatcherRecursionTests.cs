using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// Bundle <c>sjl-11-001</c>'s check A-4, the matcher's half (requirement R4 as decision D6 of Broiler.VM's JSD-0081 reads
/// it, "no CLR recursion in the matcher"; convention 5): no method of the matcher's namespace,
/// <c>Broiler.Regex.Engine</c>, reaches itself, directly or through a cycle, in the IL call graph of the library's built
/// assembly. The program compiler lives outside that namespace (task 2.4a), and the parser recurses under the caller's
/// nesting limit, which the nesting tests bound; the old closure matcher of <c>Matching/</c> is outside it too, and task
/// 2.4c deletes it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The graph.</b> A node is a method the assembly defines; an edge goes from a method to every method of the assembly
/// that an instruction of its body names as the target of <c>call</c>, <c>callvirt</c>, <c>newobj</c>, <c>ldftn</c>,
/// <c>ldvirtftn</c> or <c>jmp</c>: a method definition, a generic method's instantiation of one, or a member reference to
/// a method of a generic type the assembly defines, resolved to the definition by its name and signature. Two edges stand
/// for a dispatch: the meter's invocation, <c>RegexWorkCharge.Invoke</c>, goes to every method the library itself makes a
/// meter of; and a <c>callvirt</c> to another assembly's method goes to every virtual method of this one with the same
/// name and arity, which could override or implement it. The cycles are the strongly connected components of the whole
/// assembly's graph, so a cycle that leaves the namespace and comes back is found.
/// </para>
/// <para>
/// <b>What it cannot follow is refused.</b> A static call names its target, but a dispatch does not. So the namespace
/// declares no virtual or abstract method and implements no interface, and no dispatch elsewhere lands in it; and no
/// method the namespace reaches, its own included, takes a function pointer (<c>ldftn</c>, <c>ldvirtftn</c>), invokes a
/// delegate other than the meter, calls a virtual, abstract or interface method of this assembly through
/// <c>callvirt</c>, or uses <c>calli</c>. Every path out of the namespace is then in the graph. A call to another
/// assembly ends it. That rests on what the namespace calls there calling no code of this library's, which the graph
/// does not read: the types it calls are pinned (<see cref="ReviewedExternalTypes"/>), each reviewed, so that a new one
/// fails the test until it is. They are the runtime's array pool, arrays, spans, <c>GC</c>, <c>Math</c>,
/// <c>BitOperations</c>, <c>char</c>, <c>Nullable</c>, <c>object</c> and the argument checks, and, since task 2.4b, the
/// Unicode owner's case folding, which under <c>i</c> reads the provider it was built over at match time. That provider
/// and the meter are the caller's code, which may call a match of its own: that is the caller's recursion, not the
/// library's. The provider stays the caller's because no type of the library implements its interface, which is refused
/// wherever it is declared. The in-memory witness plants each shape the graph once missed (a closure calling its own
/// delegate, a delegate a type initializer caches, an override and an interface implementation in another namespace
/// that call back, a meter the library makes of a method of the namespace, a provider of the library's own wrapping the
/// caller's tables that matches again) and is refused for each.
/// </para>
/// </remarks>
public class MatcherRecursionTests
{
    private const string MatcherNamespace = "Broiler.Regex.Engine";

    /// <summary>The Unicode owner's provider interface: the case folding the machine calls under <c>i</c> reads the tables through it.</summary>
    private const string ProviderInterface = "Broiler.Unicode.Algorithms.IUnicodeTables";

    /// <summary>
    /// What check A-4 refuses in a graph: every cycle through the namespace; every way into the namespace the graph
    /// could not follow, a virtual or abstract method or an interface implementation declared in it; every transfer out
    /// of a method the namespace reaches that the graph cannot follow (<see cref="CallGraph.UnresolvedFrom"/>); and
    /// every type of the library that implements the provider's interface, which the case folding calls during a match
    /// from the Unicode owner's assembly, where the graph does not reach.
    /// </summary>
    private static List<string> Refusals(CallGraph graph) =>
    [
        .. graph.CyclesThrough(MatcherNamespace),
        .. graph.VirtualMethodsIn(MatcherNamespace).Select(static m => m + " is virtual"),
        .. graph.InterfaceImplementationsIn(MatcherNamespace).Select(static t => t + " implements an interface"),
        .. graph.UnresolvedFrom(MatcherNamespace),
        .. graph.ImplementationsOf(ProviderInterface).Select(static t => t + " implements " + ProviderInterface + ", which the case folding calls during a match"),
    ];

    /// <summary>
    /// The types of other assemblies whose members a method the namespace reaches calls or makes, its own methods
    /// included: the calls the graph does not follow, each reviewed for a way back into the library. The runtime's
    /// shared array pool, arrays, spans and their extensions, <c>GC</c>'s array allocation, <c>Math</c>,
    /// <c>BitOperations</c>, <c>char</c>, <c>Nullable</c>, <c>object</c>'s constructor and the argument checks call no
    /// code of the library's; the Unicode owner's case folding (task 2.4b: <c>SimpleFold</c>, <c>SimpleUpper</c>,
    /// <c>Orbit</c> and <c>UpperOrbit</c>, at match time under <c>i</c>) reads the provider it was built over, which is
    /// the caller's, as no type of the library implements its interface (<see cref="Refusals"/>). A type that joins
    /// this list is a call to review before the list is moved.
    /// </summary>
    private static readonly string[] ReviewedExternalTypes =
    [
        "Broiler.Unicode.Algorithms.UnicodeCaseFolding", "System.ArgumentException", "System.ArgumentOutOfRangeException",
        "System.Array", "System.Buffers.ArrayPool`1", "System.Char", "System.GC", "System.Math", "System.MemoryExtensions",
        "System.Nullable`1", "System.Numerics.BitOperations", "System.Object", "System.ReadOnlySpan`1", "System.Span`1",
    ];

    [Fact]
    public void A4_No_method_of_the_matcher_namespace_reaches_itself_in_the_IL_call_graph()
    {
        var graph = CallGraph.Read(File.ReadAllBytes(SharedTree.AssemblyPath("Broiler.Regex")));
        Assert.Empty(Refusals(graph));

        // Not vacuous: the loop is read, its calls into the machine's helpers are resolved, and the program's entry reaches
        // the loop.
        const string Loop = "Broiler.Regex.Engine.RegexMachine.MatchFrom(System.Int32)";
        Assert.Contains(Loop, graph.Methods);
        Assert.Contains("Broiler.Regex.Engine.RegexMachine.Afford(System.Int32)", graph.Callees(Loop));
        Assert.Contains("Broiler.Regex.Engine.RegexMachine.Pay()", graph.Callees("Broiler.Regex.Engine.RegexMachine.Afford(System.Int32)"));
        Assert.Contains("Broiler.Regex.Engine.RegexMachine.PushChoicePoint(System.Int32,System.Int32)", graph.Callees(Loop));
        Assert.True(graph.Reaches(graph.Methods.Single(static m => m.StartsWith("Broiler.Regex.Engine.RegexProgram.Match(", StringComparison.Ordinal)), Loop));

        // The only method outside the namespace that it reaches is the meter's invocation, the caller's code; the calls
        // into other assemblies are those reviewed, the case folding among them, which reads the caller's provider, the
        // caller's code too, as no type of the library implements its interface (Refusals).
        Assert.Equal(["Broiler.Regex.RegexWorkCharge.Invoke(System.UInt64)"], graph.ReachedFrom(MatcherNamespace).Where(static m => !m.StartsWith(MatcherNamespace + ".", StringComparison.Ordinal)));
        Assert.Equal(ReviewedExternalTypes, graph.ExternalTypesFrom(MatcherNamespace));
        Assert.Contains("Broiler.Unicode.Algorithms.UnicodeCaseFolding", graph.ExternalTypesFrom(MatcherNamespace));

        // And the reading finds what the library has elsewhere: the old closure matcher of Matching/ makes and invokes
        // delegates, which the graph cannot follow; the parser's descent recurses, under the nesting limit.
        Assert.Contains(graph.UnresolvedFrom("Broiler.Regex.Matching"), static u => u.Contains(" Ldftn ", StringComparison.Ordinal));
        Assert.Contains(graph.UnresolvedFrom("Broiler.Regex.Matching"), static u => u.Contains(" invokes ", StringComparison.Ordinal));
        Assert.Contains(graph.CyclesThrough("Broiler.Regex.Parsing"), static c => c.Contains("RegexParser.ParseDisjunction()", StringComparison.Ordinal));
    }

    [Fact]
    public void A4_A_recursive_helper_planted_in_a_copy_of_the_library_is_refused()
    {
        // The library's sources, compiled here as they are: no cycle through the namespace, as in the built assembly.
        var sources = LibrarySources();
        Assert.Empty(CallGraph.Read(Compile(sources)).CyclesThrough(MatcherNamespace));

        // The same sources with a recursive helper planted in the namespace in each shape the graph reads: a direct call,
        // a cycle through another namespace, one through a member of a generic type's instance, and one through a delegate
        // the namespace creates.
        var planted = CallGraph.Read(Compile([.. sources, ("Broiler.Regex/Engine/PlantedRecursion.cs", PlantedRecursion)])).CyclesThrough(MatcherNamespace);
        Assert.Contains(planted, static c => c == "Broiler.Regex.Engine.PlantedRecursion.Direct(System.Int32) -> Broiler.Regex.Engine.PlantedRecursion.Direct(System.Int32)");
        Assert.Contains(planted, static c => c.Contains("Broiler.Regex.Parsing.PlantedPeer.Back(System.Int32)", StringComparison.Ordinal)
            && c.Contains("Broiler.Regex.Engine.PlantedRecursion.ThroughAnotherNamespace(System.Int32)", StringComparison.Ordinal));
        Assert.Contains(planted, static c => c.Contains("Broiler.Regex.Engine.PlantedBox`1.Call(System.Int32)", StringComparison.Ordinal)
            && c.Contains("Broiler.Regex.Engine.PlantedRecursion.ThroughGeneric(System.Int32)", StringComparison.Ordinal));
        Assert.Contains(planted, static c => c.Contains("Broiler.Regex.Engine.PlantedRecursion.Delegated(System.Int32)", StringComparison.Ordinal)
            && c.Contains("Broiler.Regex.Engine.PlantedRecursion.ThroughDelegate()", StringComparison.Ordinal));

        // The shapes the graph cannot follow by a static call, each a recursion that runs (the review nested 201 CLR
        // frames through each with n = 200): a closure calling its own delegate, a delegate cached in a static field by
        // the type's initializer, an override in another namespace that calls back, and an interface implemented in
        // another namespace that calls back. Each is refused, by its cycle or by the dispatch the graph cannot follow.
        var missed = Refusals(CallGraph.Read(Compile([.. sources, ("Broiler.Regex/Engine/PlantedMiss.cs", PlantedMiss)])));
        string[] shapes = ["Lambda", "Stored", "Virtual", "Interface", "Metered"];
        Assert.Empty(shapes.Where(shape => !missed.Any(r => r.Contains("Broiler.Regex.Engine.PlantedMiss", StringComparison.Ordinal) && r.Contains(shape, StringComparison.Ordinal))));

        // A recursion planted in the loop itself: the charging the loop calls calling the loop back.
        var loop = sources.Select(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)
            ? (Path: s.Path, Text: s.Text.Replace("        _threshold = Math.Min(_paid, _stepCeiling);\n        return true;", "        _threshold = Math.Min(_paid, _stepCeiling);\n        return MatchFrom(0) != Outcome.Exceeded;", StringComparison.Ordinal))
            : s).ToList();
        Assert.NotEqual(sources.Single(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)).Text, loop.Single(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)).Text);
        Assert.Contains(CallGraph.Read(Compile(loop)).CyclesThrough(MatcherNamespace), static c => c.Contains("Broiler.Regex.Engine.RegexMachine.MatchFrom(System.Int32)", StringComparison.Ordinal)
            && c.Contains("Broiler.Regex.Engine.RegexMachine.Pay()", StringComparison.Ordinal));

        // A recursion through the caller's provider, which the graph cannot follow into the Unicode owner's assembly:
        // the case folding the machine calls under i reads the tables it was built over, and a provider of the
        // library's own that matches again would close a cycle match -> folding -> provider -> match. The library wraps
        // the caller's tables in such a provider here; it is refused as an implementation of the provider's interface.
        const string Folding = "        _folding = new UnicodeCaseFolding(tables);";
        var unicode = sources.Single(static s => s.Path.EndsWith("Unicode/RegexUnicode.cs", StringComparison.Ordinal));
        Assert.Contains(Folding, unicode.Text);
        var wrapped = sources.Select(s => s.Path == unicode.Path
            ? (Path: s.Path, Text: s.Text.Replace(Folding, "        _folding = new UnicodeCaseFolding(new PlantedTables(tables));", StringComparison.Ordinal))
            : s).ToList();
        var provider = Refusals(CallGraph.Read(Compile([.. wrapped, ("Broiler.Regex/Unicode/PlantedTables.cs", PlantedTables)])));
        Assert.Contains(provider, static r => r.StartsWith("Broiler.Regex.Unicode.PlantedTables implements Broiler.Unicode.Algorithms.IUnicodeTables", StringComparison.Ordinal));
    }

    private const string PlantedTables = """
        namespace Broiler.Regex.Unicode
        {
            internal sealed class PlantedTables(Broiler.Unicode.Algorithms.IUnicodeTables inner) : Broiler.Unicode.Algorithms.IUnicodeTables
            {
                internal static Broiler.Regex.Engine.RegexProgram? Again;

                public string UnicodeVersion => inner.UnicodeVersion;

                public System.ReadOnlySpan<byte> Table(Broiler.Unicode.Algorithms.UnicodeTable table)
                {
                    Again?.Match(System.ReadOnlySpan<char>.Empty, 0, false, System.Span<int>.Empty, null, default, out _, out _);
                    return inner.Table(table);
                }
            }
        }
        """;

    private const string PlantedRecursion = """
        namespace Broiler.Regex.Engine
        {
            internal static class PlantedRecursion
            {
                internal static int Direct(int n) => n == 0 ? 0 : Direct(n - 1);

                internal static int ThroughAnotherNamespace(int n) => n == 0 ? 0 : Broiler.Regex.Parsing.PlantedPeer.Back(n - 1);

                internal static int ThroughGeneric(int n) => n == 0 ? 0 : PlantedBox<int>.Call(n - 1);

                internal static System.Func<int, int> ThroughDelegate() => Delegated;

                internal static int Delegated(int n) => n == 0 ? 0 : ThroughDelegate()(n - 1);
            }

            internal static class PlantedBox<T>
            {
                internal static int Call(int n) => PlantedRecursion.ThroughGeneric(n);
            }
        }

        namespace Broiler.Regex.Parsing
        {
            internal static class PlantedPeer
            {
                internal static int Back(int n) => Broiler.Regex.Engine.PlantedRecursion.ThroughAnotherNamespace(n);
            }
        }
        """;

    private const string PlantedMiss = """
        namespace Broiler.Regex.Engine
        {
            internal static class PlantedMiss
            {
                internal static int Lambda(int n)
                {
                    System.Func<int, int>? step = null;
                    step = k => k == 0 ? 0 : step!(k - 1);
                    return step(n);
                }

                private static readonly System.Func<int, int> Cached = StoredStep;

                internal static int Stored(int n) => Cached(n);

                private static int StoredStep(int n) => n == 0 ? 0 : Cached(n - 1);

                internal static int Virtual(Broiler.Regex.Parsing.PlantedBase step, int n) => n == 0 ? 0 : step.Step(n - 1);

                internal static int Interface(Broiler.Regex.Parsing.IPlantedStep step, int n) => n == 0 ? 0 : step.Step(n - 1);

                internal static void Metered(ulong n)
                {
                    if (n > 0)
                        Broiler.Regex.Parsing.PlantedMeter.Charge(n - 1);
                }
            }
        }

        namespace Broiler.Regex.Parsing
        {
            internal abstract class PlantedBase
            {
                internal abstract int Step(int n);
            }

            internal sealed class PlantedDerived : PlantedBase
            {
                internal override int Step(int n) => Broiler.Regex.Engine.PlantedMiss.Virtual(this, n);
            }

            internal interface IPlantedStep
            {
                int Step(int n);
            }

            internal sealed class PlantedImpl : IPlantedStep
            {
                public int Step(int n) => Broiler.Regex.Engine.PlantedMiss.Interface(this, n);
            }

            internal static class PlantedMeter
            {
                internal static readonly Broiler.Regex.RegexWorkCharge Charge = Broiler.Regex.Engine.PlantedMiss.Metered;
            }
        }
        """;

    /// <summary>Every C# source of the library, with its path relative to the repository root, outside build output.</summary>
    private static List<(string Path, string Text)> LibrarySources()
    {
        var library = Path.Combine(SharedTree.Root, "Broiler.Regex");
        return Directory.EnumerateFiles(library, "*.cs", SearchOption.AllDirectories)
            .Where(static p => SharedTree.IsRepositoryFile(p))
            .Order(StringComparer.Ordinal)
            .Select(static p => (Path: SharedTree.Relative(p), Text: File.ReadAllText(p)))
            .ToList();
    }

    /// <summary>Compiles sources as the library is compiled, against the platform and the Unicode owner's algorithms, and answers the image.</summary>
    private static byte[] Compile(IEnumerable<(string Path, string Text)> sources)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = sources.Select(s => CSharpSyntaxTree.ParseText(s.Text, options, path: s.Path, encoding: System.Text.Encoding.UTF8));
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = trusted.Split(Path.PathSeparator)
            .Where(static p => Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll")
            .Select(static p => MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(SharedTree.AssemblyPath("Broiler.Unicode.Algorithms")));
        var compilation = CSharpCompilation.Create(
            "Broiler.Regex",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, optimizationLevel: OptimizationLevel.Release));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException("the library's copy does not compile:\n" + string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    /// <summary>The call graph of one assembly's methods, read from their IL without loading the assembly.</summary>
    private sealed class CallGraph
    {
        private readonly Dictionary<string, HashSet<string>> _edges = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _namespaces = new(StringComparer.Ordinal);
        private readonly List<string> _virtuals = [];
        private readonly List<string> _interfaces = [];
        private readonly List<(string Type, string Interface)> _implementations = [];
        private readonly Dictionary<string, SortedSet<string>> _external = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _unresolved = new(StringComparer.Ordinal);

        /// <summary>The meter's delegate type: its invocation is followed to every method the library makes a meter of.</summary>
        private const string Meter = "Broiler.Regex.RegexWorkCharge";

        private const string MeterInvoke = Meter + ".Invoke(System.UInt64)";

        internal IReadOnlyCollection<string> Methods => _edges.Keys;

        internal IReadOnlyCollection<string> Callees(string method) => _edges[method];

        internal IEnumerable<string> VirtualMethodsIn(string ns) => _virtuals.Where(m => InNamespace(_namespaces[m], ns));

        internal IEnumerable<string> InterfaceImplementationsIn(string ns) => _interfaces.Where(t => t.StartsWith(ns + ".", StringComparison.Ordinal));

        /// <summary>Every type of the assembly that implements the interface named <paramref name="name"/>, wherever it is declared.</summary>
        internal IEnumerable<string> ImplementationsOf(string name) => _implementations.Where(i => i.Interface == name).Select(static i => i.Type);

        /// <summary>The types of other assemblies whose members a method that namespace <paramref name="ns"/> reaches calls or makes.</summary>
        internal List<string> ExternalTypesFrom(string ns) =>
            ReachedFrom(ns).Where(_external.ContainsKey).SelectMany(m => _external[m]).Distinct().Order(StringComparer.Ordinal).ToList();

        internal static CallGraph Read(byte[] image)
        {
            var graph = new CallGraph();
            using var reader = new PEReader(ImmutableArray.Create(image));
            var metadata = reader.GetMetadataReader();
            var names = new SignatureNames(metadata);

            string NameOf(MethodDefinitionHandle handle)
            {
                var method = metadata.GetMethodDefinition(handle);
                var signature = method.DecodeSignature(names, null);
                return SignatureNames.Qualified(metadata, method.GetDeclaringType()) + "." + metadata.GetString(method.Name)
                    + "(" + string.Join(",", signature.ParameterTypes) + ")";
            }

            foreach (var typeHandle in metadata.TypeDefinitions)
            {
                var type = metadata.GetTypeDefinition(typeHandle);
                if (type.GetInterfaceImplementations().Count > 0)
                    graph._interfaces.Add(SignatureNames.Qualified(metadata, typeHandle));
                foreach (var implementation in type.GetInterfaceImplementations())
                {
                    var implemented = metadata.GetInterfaceImplementation(implementation).Interface;
                    graph._implementations.Add((SignatureNames.Qualified(metadata, typeHandle), TypeName(metadata, names, implemented)));
                }
            }

            // The virtual methods of the assembly by simple name and arity, which a virtual call to another assembly's
            // method of that name and arity may reach through an override or an implementation.
            var overridable = new Dictionary<(string Name, int Arity), List<string>>();
            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if ((method.Attributes & MethodAttributes.Virtual) == 0)
                    continue;
                var simple = metadata.GetString(method.Name);
                var key = (simple[(simple.LastIndexOf('.') + 1)..], method.DecodeSignature(names, null).ParameterTypes.Length);
                if (!overridable.TryGetValue(key, out var list))
                    overridable[key] = list = [];
                list.Add(NameOf(handle));
            }

            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                var name = NameOf(handle);
                graph._edges.TryAdd(name, new HashSet<string>(StringComparer.Ordinal));
                graph._namespaces[name] = OutermostNamespace(metadata, method.GetDeclaringType());
                if ((method.Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) != 0)
                    graph._virtuals.Add(name);
            }

            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress == 0)
                    continue;

                var name = NameOf(handle);
                var functions = new List<string>();
                var il = reader.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
                while (il.RemainingBytes > 0)
                {
                    var opCode = ReadOpCode(ref il);
                    if (opCode is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Ldftn or ILOpCode.Ldvirtftn or ILOpCode.Jmp)
                    {
                        var operand = MetadataTokens.EntityHandle(il.ReadInt32());
                        var targets = Resolve(metadata, names, operand).Select(NameOf).ToList();
                        foreach (var target in targets)
                            graph._edges[name].Add(target);
                        if (targets.Count == 0 && DeclaringType(metadata, names, operand) is { } external)
                        {
                            if (!graph._external.TryGetValue(name, out var types))
                                graph._external[name] = types = new SortedSet<string>(StringComparer.Ordinal);
                            types.Add(external);
                        }

                        switch (opCode)
                        {
                            case ILOpCode.Ldftn or ILOpCode.Ldvirtftn:
                                // A function pointer: the delegate made of it may be invoked anywhere, by anyone.
                                functions.AddRange(targets);
                                graph.Unresolved(name, opCode + " " + string.Join(", ", targets.DefaultIfEmpty(Display(metadata, names, operand))));
                                break;
                            case ILOpCode.Newobj when targets.Count == 1 && targets[0].StartsWith(Meter + "..ctor(", StringComparison.Ordinal):
                                // A meter the library makes itself: its invocation reaches the method it was made of.
                                foreach (var function in functions)
                                    graph._meterTargets.Add(function);
                                break;
                            case ILOpCode.Call or ILOpCode.Callvirt:
                            {
                                var display = targets.Count > 0 ? targets[0] : Display(metadata, names, operand);
                                var simple = display[..display.IndexOf('(', StringComparison.Ordinal)];
                                simple = simple[(simple.LastIndexOf('.') + 1)..];
                                if (display == MeterInvoke)
                                    break;
                                if (simple is "Invoke" or "BeginInvoke" or "EndInvoke" or "DynamicInvoke")
                                    graph.Unresolved(name, "invokes " + display);
                                else if (opCode == ILOpCode.Callvirt && targets.Count > 0 && graph._virtuals.Contains(targets[0]) && !IsFinal(metadata, operand))
                                    graph.Unresolved(name, "dispatches through " + targets[0]);
                                else if (opCode == ILOpCode.Callvirt && targets.Count == 0
                                    && overridable.TryGetValue((simple, Arity(metadata, names, operand)), out var overrides))
                                {
                                    // Another assembly's virtual method: joined to every method of this one that could
                                    // override or implement it, by name and arity.
                                    foreach (var candidate in overrides)
                                        graph._edges[name].Add(candidate);
                                }

                                break;
                            }
                        }
                    }
                    else
                    {
                        if (opCode == ILOpCode.Calli)
                            graph.Unresolved(name, "calli");
                        SkipOperand(ref il, opCode);
                    }
                }
            }

            if (graph._edges.TryGetValue(MeterInvoke, out var meter))
                meter.UnionWith(graph._meterTargets);
            return graph;
        }

        private readonly HashSet<string> _meterTargets = new(StringComparer.Ordinal);

        private void Unresolved(string method, string what)
        {
            if (!_unresolved.TryGetValue(method, out var list))
                _unresolved[method] = list = [];
            list.Add(what);
        }

        /// <summary>
        /// Every transfer out of a method that a method of namespace <paramref name="ns"/> reaches, itself included, that
        /// the graph cannot follow to a method: a function pointer taken (<c>ldftn</c>, <c>ldvirtftn</c>), a delegate
        /// invoked other than the meter, a virtual, abstract or interface method of this assembly called through
        /// <c>callvirt</c>, and <c>calli</c>. Where none is left, every path out of the namespace's methods is in the graph,
        /// up to the calls into other assemblies.
        /// </summary>
        internal List<string> UnresolvedFrom(string ns) =>
            ReachedFrom(ns)
                .Where(_unresolved.ContainsKey)
                .SelectMany(m => _unresolved[m].Select(what => m + " " + what))
                .ToList();

        /// <summary>Every method a method of namespace <paramref name="ns"/> reaches, the namespace's own included, in order.</summary>
        internal List<string> ReachedFrom(string ns)
        {
            var reached = new HashSet<string>(_edges.Keys.Where(m => InNamespace(_namespaces[m], ns)), StringComparer.Ordinal);
            var pending = new Stack<string>(reached);
            while (pending.Count > 0)
            {
                foreach (var next in _edges[pending.Pop()])
                {
                    if (reached.Add(next))
                        pending.Push(next);
                }
            }

            return reached.Order(StringComparer.Ordinal).ToList();
        }

        /// <summary>Whether a method definition operand is sealed against overriding, so <c>callvirt</c> reaches it alone.</summary>
        private static bool IsFinal(MetadataReader metadata, EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;
            return operand.Kind == HandleKind.MethodDefinition
                && (metadata.GetMethodDefinition((MethodDefinitionHandle)operand).Attributes & MethodAttributes.Final) != 0;
        }

        /// <summary>A method operand's name, for one this assembly does not define.</summary>
        private static string Display(MetadataReader metadata, SignatureNames names, EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;
            if (operand.Kind != HandleKind.MemberReference)
                return operand.Kind + "()";
            var member = metadata.GetMemberReference((MemberReferenceHandle)operand);
            var parent = member.Parent.Kind switch
            {
                HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)member.Parent),
                HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)member.Parent),
                HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)member.Parent).DecodeSignature(names, null),
                _ => member.Parent.Kind.ToString(),
            };
            return parent + "." + metadata.GetString(member.Name) + "(" + string.Join(",", member.DecodeMethodSignature(names, null).ParameterTypes) + ")";
        }

        /// <summary>A type's name, a generic instance's without its arguments.</summary>
        private static string TypeName(MetadataReader metadata, SignatureNames names, EntityHandle type)
        {
            var name = type.Kind switch
            {
                HandleKind.TypeReference => SignatureNames.Qualified(metadata, (TypeReferenceHandle)type),
                HandleKind.TypeDefinition => SignatureNames.Qualified(metadata, (TypeDefinitionHandle)type),
                HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)type).DecodeSignature(names, null),
                _ => type.Kind.ToString(),
            };
            var arguments = name.IndexOf('<', StringComparison.Ordinal);
            return arguments < 0 ? name : name[..arguments];
        }

        /// <summary>The type that declares a method operand of another assembly, or null for one this assembly defines.</summary>
        private static string? DeclaringType(MetadataReader metadata, SignatureNames names, EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;
            if (operand.Kind != HandleKind.MemberReference)
                return null;
            var parent = metadata.GetMemberReference((MemberReferenceHandle)operand).Parent;
            if (parent.Kind == HandleKind.TypeDefinition)
                return null;
            if (parent.Kind == HandleKind.TypeSpecification)
            {
                var blob = metadata.GetBlobReader(metadata.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
                if (blob.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
                {
                    blob.ReadSignatureTypeCode();
                    if (blob.ReadTypeHandle().Kind == HandleKind.TypeDefinition)
                        return null;
                }
            }

            return TypeName(metadata, names, parent);
        }

        /// <summary>A method operand's parameter count.</summary>
        private static int Arity(MetadataReader metadata, SignatureNames names, EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;
            return operand.Kind switch
            {
                HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)operand).DecodeMethodSignature(names, null).ParameterTypes.Length,
                HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)operand).DecodeSignature(names, null).ParameterTypes.Length,
                _ => -1,
            };
        }

        /// <summary>The method definitions an instruction's operand names: none for a method of another assembly.</summary>
        private static IEnumerable<MethodDefinitionHandle> Resolve(MetadataReader metadata, SignatureNames names, EntityHandle operand)
        {
            if (operand.Kind == HandleKind.MethodSpecification)
                operand = metadata.GetMethodSpecification((MethodSpecificationHandle)operand).Method;
            if (operand.Kind == HandleKind.MethodDefinition)
            {
                yield return (MethodDefinitionHandle)operand;
                yield break;
            }

            if (operand.Kind != HandleKind.MemberReference)
                yield break;

            // A member of a generic type's instance: the definition with the same name and signature in the generic type,
            // when the assembly defines it.
            var member = metadata.GetMemberReference((MemberReferenceHandle)operand);
            if (member.GetKind() != MemberReferenceKind.Method || member.Parent.Kind != HandleKind.TypeSpecification)
                yield break;
            var blob = metadata.GetBlobReader(metadata.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature);
            if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                yield break;
            blob.ReadSignatureTypeCode();
            var generic = blob.ReadTypeHandle();
            if (generic.Kind != HandleKind.TypeDefinition)
                yield break;

            var wanted = metadata.GetString(member.Name);
            var shape = string.Join(",", member.DecodeMethodSignature(names, null).ParameterTypes);
            foreach (var candidate in metadata.GetTypeDefinition((TypeDefinitionHandle)generic).GetMethods())
            {
                var definition = metadata.GetMethodDefinition(candidate);
                if (metadata.GetString(definition.Name) == wanted && string.Join(",", definition.DecodeSignature(names, null).ParameterTypes) == shape)
                    yield return candidate;
            }
        }

        /// <summary>
        /// Every strongly connected component of the graph that holds a method of namespace <paramref name="ns"/> and is a
        /// cycle (more than one method, or one that calls itself), written as a path through it back to its first method.
        /// </summary>
        internal List<string> CyclesThrough(string ns)
        {
            var cycles = new List<string>();
            foreach (var component in StronglyConnected())
            {
                var first = component.First();
                if (component.Count == 1 && !_edges[first].Contains(first))
                    continue;
                if (!component.Any(m => InNamespace(_namespaces[m], ns)))
                    continue;

                var start = component.Where(m => InNamespace(_namespaces[m], ns)).Order(StringComparer.Ordinal).First();
                cycles.Add(string.Join(" -> ", PathBack(start, component)));
            }

            return cycles;
        }

        /// <summary>Whether <paramref name="to"/> is reachable from <paramref name="from"/>.</summary>
        internal bool Reaches(string from, string to)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { from };
            var pending = new Stack<string>([from]);
            while (pending.Count > 0)
            {
                foreach (var next in _edges[pending.Pop()])
                {
                    if (next == to)
                        return true;
                    if (seen.Add(next))
                        pending.Push(next);
                }
            }

            return false;
        }

        /// <summary>A shortest path from <paramref name="start"/> back to itself inside <paramref name="component"/>.</summary>
        private List<string> PathBack(string start, HashSet<string> component)
        {
            var parent = new Dictionary<string, string>(StringComparer.Ordinal);
            var queue = new Queue<string>([start]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in _edges[current].Where(component.Contains).Order(StringComparer.Ordinal))
                {
                    if (next == start)
                    {
                        var path = new List<string> { start };
                        for (var at = current; at != start; at = parent[at])
                            path.Insert(1, at);
                        path.Add(start);
                        return path;
                    }

                    if (parent.TryAdd(next, current))
                        queue.Enqueue(next);
                }
            }

            return [start];
        }

        /// <summary>Tarjan's strongly connected components, iteratively.</summary>
        private List<HashSet<string>> StronglyConnected()
        {
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            var low = new Dictionary<string, int>(StringComparer.Ordinal);
            var onStack = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            var components = new List<HashSet<string>>();
            var counter = 0;
            foreach (var root in _edges.Keys.Order(StringComparer.Ordinal))
            {
                if (index.ContainsKey(root))
                    continue;
                var work = new Stack<(string Node, IEnumerator<string> Next)>();
                index[root] = low[root] = counter++;
                stack.Push(root);
                onStack.Add(root);
                work.Push((root, _edges[root].Order(StringComparer.Ordinal).GetEnumerator()));
                while (work.Count > 0)
                {
                    var (node, next) = work.Peek();
                    if (next.MoveNext())
                    {
                        var child = next.Current;
                        if (!index.ContainsKey(child))
                        {
                            index[child] = low[child] = counter++;
                            stack.Push(child);
                            onStack.Add(child);
                            work.Push((child, _edges[child].Order(StringComparer.Ordinal).GetEnumerator()));
                        }
                        else if (onStack.Contains(child))
                        {
                            low[node] = Math.Min(low[node], index[child]);
                        }

                        continue;
                    }

                    work.Pop();
                    if (work.Count > 0)
                        low[work.Peek().Node] = Math.Min(low[work.Peek().Node], low[node]);
                    if (low[node] != index[node])
                        continue;
                    var component = new HashSet<string>(StringComparer.Ordinal);
                    string member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        component.Add(member);
                    }
                    while (member != node);
                    components.Add(component);
                }
            }

            return components;
        }

        private static bool InNamespace(string declared, string ns) =>
            declared == ns || declared.StartsWith(ns + ".", StringComparison.Ordinal);

        /// <summary>The namespace of a type, or of the outermost type it is nested in, as compiler-generated closures are.</summary>
        private static string OutermostNamespace(MetadataReader metadata, TypeDefinitionHandle handle)
        {
            var type = metadata.GetTypeDefinition(handle);
            while (!type.GetDeclaringType().IsNil)
                type = metadata.GetTypeDefinition(type.GetDeclaringType());
            return metadata.GetString(type.Namespace);
        }

        private static ILOpCode ReadOpCode(ref BlobReader il)
        {
            int code = il.ReadByte();
            if (code == 0xFE)
                code = 0xFE00 | il.ReadByte();
            return (ILOpCode)code;
        }

        private static void SkipOperand(ref BlobReader il, ILOpCode code)
        {
            if (code == ILOpCode.Switch)
            {
                il.Offset += 4 * il.ReadInt32();
                return;
            }

            if (code.IsBranch())
            {
                il.Offset += code.GetBranchOperandSize();
                return;
            }

            il.Offset += code switch
            {
                ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s
                    or ILOpCode.Ldc_i4_s or ILOpCode.Unaligned => 1,
                ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Ldloca or ILOpCode.Stloc => 2,
                ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Ldstr or ILOpCode.Calli
                    or ILOpCode.Ldfld or ILOpCode.Ldflda or ILOpCode.Stfld or ILOpCode.Ldsfld or ILOpCode.Ldsflda or ILOpCode.Stsfld
                    or ILOpCode.Ldtoken or ILOpCode.Box or ILOpCode.Unbox or ILOpCode.Unbox_any or ILOpCode.Castclass or ILOpCode.Isinst
                    or ILOpCode.Newarr or ILOpCode.Ldelema or ILOpCode.Ldelem or ILOpCode.Stelem or ILOpCode.Ldobj or ILOpCode.Stobj
                    or ILOpCode.Cpobj or ILOpCode.Initobj or ILOpCode.Sizeof or ILOpCode.Mkrefany or ILOpCode.Refanyval
                    or ILOpCode.Constrained => 4,
                ILOpCode.Ldc_i8 or ILOpCode.Ldc_r8 => 8,

                // no. (0xFE19) takes a one-byte mask; System.Reflection.Metadata names no member for it.
                _ => (int)code == 0xFE19 ? 1 : 0,
            };
        }
    }
}
