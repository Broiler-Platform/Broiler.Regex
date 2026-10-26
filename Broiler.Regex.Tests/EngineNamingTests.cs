using System;
using System.Linq;
using System.Reflection;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Decision D5 of Broiler.VM's JSD-0081 makes Broiler.VM's matcher the specification of the new backend's design and
/// units, and ports none of its code: the backend is written in this library's own vocabulary, so it declares none of the
/// names of that matcher's private members that task 2.4a's review and its verification found declared here, and nothing
/// of this library is named as Broiler.VM's ceilings are unless it holds their values.
/// </summary>
public class EngineNamingTests
{
    /// <summary>
    /// The private names of Broiler.VM's <c>JsRegExpMatcher</c> the review found declared in the new backend, the two its
    /// verification found after the fixes (<c>GiveBack</c>, <c>Emitter</c>), the one task 2.4b's own check found on an
    /// instruction it added (<c>WordBoundary</c>, a node kind of the VM's parser, now <c>RegexOp.WordEdge</c>; the
    /// library's own syntax tree keeps the public <c>AnchorKind.WordBoundary</c> it has had since before SJL-11), and the
    /// names of the VM's look-around machinery that task 2.4b's barrier frames could have taken.
    /// </summary>
    private static readonly string[] Copied =
    [
        "Char", "Split", "Jump", "JumpIfAtLeast", "JumpIfBelow", "EmptyCheck", "Accept",
        "PushFrame", "Unwind", "Attempt", "Read", "Write", "Advance", "IsLineTerminator",
        "frameTop", "trailTop", "MaximumFrames", "MaximumTrail", "GiveBack", "Emitter",
        "WordBoundary", "AssertBegin", "AssertEnd", "AssertKind", "CloseAssertion", "PushAssertion", "assertions",
        "assertionTop", "Backward", "Fold", "Lines", "BackRef", "Bol", "Eol", "EmitLook", "EmitCapture",
    ];

    /// <summary>Every member the new backend's namespace and its program compiler declare, by name, compiler-generated ones left out.</summary>
    private static string[] DeclaredNames()
    {
        var types = typeof(RegexProgram).Assembly.GetTypes()
            .Where(static t => t.Namespace == "Broiler.Regex.Engine" || t == typeof(ProgramCompiler) || t.DeclaringType == typeof(ProgramCompiler));
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return types.SelectMany(static t => t.GetMembers(all).Select(static m => m.Name).Append(t.Name))
            .Where(static n => !n.Contains('<', StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    [Fact]
    public void The_new_backend_declares_none_of_Broiler_VM_s_matcher_s_private_names()
    {
        var declared = DeclaredNames();
        Assert.Contains(nameof(RegexProgram.Match), declared);
        Assert.Empty(declared.Where(n => Copied.Any(c => string.Equals(n.TrimStart('_'), c, StringComparison.OrdinalIgnoreCase))));
    }

    [Fact]
    public void Nothing_of_the_library_is_named_as_Broiler_VM_s_ceilings_unless_it_holds_decision_D10_s_values()
    {
        // Decision D10 calls the ceilings both engines pass "this family's MaximumFrames and MaximumTrail", 1 << 20 and
        // 1 << 21; a member of this library under either name would read as them to an adapter that ports the name.
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var named = typeof(RegexProgram).Assembly.GetTypes()
            .SelectMany(static t => t.GetFields(all))
            .Where(static f => f.Name is "MaximumFrames" or "MaximumTrail")
            .Select(static f => f.DeclaringType + "." + f.Name + " = " + (f.IsLiteral || f.IsStatic ? f.GetValue(null) : "an instance field"))
            .Where(static text => !text.EndsWith(".MaximumFrames = 1048576", StringComparison.Ordinal) && !text.EndsWith(".MaximumTrail = 2097152", StringComparison.Ordinal));
        Assert.Empty(named);
    }
}
