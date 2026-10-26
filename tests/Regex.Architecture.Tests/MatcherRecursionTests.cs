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
/// <b>The graph.</b> A node is a method the assembly defines and gives a body; an edge goes from a method to every method
/// of the assembly that an instruction of its body names as the target of <c>call</c>, <c>callvirt</c>, <c>newobj</c>,
/// <c>ldftn</c>, <c>ldvirtftn</c> or <c>jmp</c>: a method definition, a generic method's instantiation of one, or a member
/// reference to a method of a generic type the assembly defines, resolved to the definition by its name and signature. A
/// delegate creation counts as reaching its target, since the delegate's invocation would. The cycles are the strongly
/// connected components of the whole assembly's graph, so a cycle that leaves the namespace and comes back is found.
/// </para>
/// <para>
/// <b>What it does not read, and why that is enough here.</b> A virtual call is resolved to the method it names, not to
/// overrides; so the namespace is held to declaring no virtual or abstract method and implementing no interface, which
/// the test asserts, and every call made in it is then resolved statically. A call to another assembly ends the path:
/// the base class library does not call back into this library, and a delegate of the caller's, such as its meter, is the
/// caller's code, which may call a match of its own; that is the caller's recursion, not the library's.
/// </para>
/// </remarks>
public class MatcherRecursionTests
{
    private const string MatcherNamespace = "Broiler.Regex.Engine";

    [Fact]
    public void A4_No_method_of_the_matcher_namespace_reaches_itself_in_the_IL_call_graph()
    {
        var graph = CallGraph.Read(File.ReadAllBytes(SharedTree.AssemblyPath("Broiler.Regex")));
        Assert.Empty(graph.CyclesThrough(MatcherNamespace));

        // Not vacuous: the loop is read, its calls into the machine's helpers are resolved, and the program's entry reaches
        // the loop.
        const string Attempt = "Broiler.Regex.Engine.RegexMachine.Attempt(System.Int32)";
        Assert.Contains(Attempt, graph.Methods);
        Assert.Contains("Broiler.Regex.Engine.RegexMachine.Pay()", graph.Callees(Attempt));
        Assert.Contains("Broiler.Regex.Engine.RegexMachine.PushFrame(System.Int32,System.Int32)", graph.Callees(Attempt));
        Assert.True(graph.Reaches(graph.Methods.Single(static m => m.StartsWith("Broiler.Regex.Engine.RegexProgram.Match(", StringComparison.Ordinal)), Attempt));

        // The namespace's calls are all resolved statically: nothing in it is virtual or abstract, and no type in it
        // implements an interface.
        Assert.Empty(graph.VirtualMethodsIn(MatcherNamespace));
        Assert.Empty(graph.InterfaceImplementationsIn(MatcherNamespace));

        // And the reading finds recursion where the library has it: the parser's descent recurses, under the nesting limit.
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

        // A recursion planted in the loop itself: the charging the loop calls calling the loop back.
        var loop = sources.Select(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)
            ? (Path: s.Path, Text: s.Text.Replace("        _threshold = Math.Min(_paid, _stepCeiling);\n        return true;", "        _threshold = Math.Min(_paid, _stepCeiling);\n        return Attempt(0) != Outcome.Exceeded;", StringComparison.Ordinal))
            : s).ToList();
        Assert.NotEqual(sources.Single(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)).Text, loop.Single(static s => s.Path.EndsWith("Engine/RegexMachine.cs", StringComparison.Ordinal)).Text);
        Assert.Contains(CallGraph.Read(Compile(loop)).CyclesThrough(MatcherNamespace), static c => c.Contains("Broiler.Regex.Engine.RegexMachine.Attempt(System.Int32)", StringComparison.Ordinal)
            && c.Contains("Broiler.Regex.Engine.RegexMachine.Pay()", StringComparison.Ordinal));
    }

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

        internal IReadOnlyCollection<string> Methods => _edges.Keys;

        internal IReadOnlyCollection<string> Callees(string method) => _edges[method];

        internal IEnumerable<string> VirtualMethodsIn(string ns) => _virtuals.Where(m => InNamespace(_namespaces[m], ns));

        internal IEnumerable<string> InterfaceImplementationsIn(string ns) => _interfaces.Where(t => t.StartsWith(ns + ".", StringComparison.Ordinal));

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
            }

            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                var name = NameOf(handle);
                graph._edges.TryAdd(name, new HashSet<string>(StringComparer.Ordinal));
                graph._namespaces[name] = OutermostNamespace(metadata, method.GetDeclaringType());
                if ((method.Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) != 0)
                    graph._virtuals.Add(name);
                if (method.RelativeVirtualAddress == 0)
                    continue;

                var il = reader.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
                while (il.RemainingBytes > 0)
                {
                    var opCode = ReadOpCode(ref il);
                    if (opCode is ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Ldftn or ILOpCode.Ldvirtftn or ILOpCode.Jmp)
                    {
                        foreach (var target in Resolve(metadata, names, MetadataTokens.EntityHandle(il.ReadInt32())))
                            graph._edges[name].Add(NameOf(target));
                    }
                    else
                    {
                        SkipOperand(ref il, opCode);
                    }
                }
            }

            return graph;
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
