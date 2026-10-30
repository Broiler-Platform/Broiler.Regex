using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The library's nesting maximum on the smallest stack the adversarial corpus declares, 1 MiB (requirement R4,
/// decision D6 of Broiler.VM's JSD-0081; bundle sjl-11-001's check A-4), on a thread each test starts itself: a
/// pattern of any shape nested to <see cref="BroilerRegex.MaximumNestingLimit"/> parses and compiles, and one level past
/// it is refused with the nesting status, never a stack overflow. A pattern at the maximum also matches, through
/// <see cref="BroilerRegex"/>, for the shapes of <see cref="MatchedShapes"/> and of <see cref="ShapesTheOldMatcherDidNotProve"/>: the
/// old matcher's continuations recursed per level and per term, so before task 2.4c routed matching through task 2.4a's
/// backend the second list's shapes were not proven, and some of them exhausted the stack. The new backend matches
/// every shape at the maximum on a 1 MiB thread, since task 2.4b those holding back-references, assertions and
/// look-behinds too (<see cref="MachineTests.A_pattern_at_the_nesting_maximum_matches_on_a_1_MiB_thread"/>). The Native AOT probe runs the same proof on a thread of the same stack (its population's
/// N cases).
/// </summary>
/// <remarks>
/// The shapes are those the maximum was measured on at task 2.3's review (<see cref="BroilerRegex.MaximumNestingLimit"/>
/// records the measurement): each kind of level alone, and mixtures of a capture, an alternation, a sequence, a
/// back-reference, an assertion and a quantified inner group on each level, forward and inside a look-behind. Every
/// subject reaches the innermost level. A shape is in <see cref="MatchedShapes"/> when, on linux-x64, its deepest
/// matching depth on a 1 MiB thread was at least twice the maximum under the JIT in Release, at least 1.4 times it in
/// Debug and four times it under Native AOT, so that the larger frames of another platform leave it room.
/// </remarks>
public class NestingMaximumTests
{
    private const int StackBytes = 1 << 20;

    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));

    private static string Decimal(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// A pattern of <paramref name="levels"/> levels of one shape below the pattern's own Disjunction, so of depth
    /// <paramref name="levels"/> + 1, and a subject that reaches its innermost level.
    /// </summary>
    internal static (string Pattern, RegexFlags Flags, string Subject) Shape(string name, int levels)
    {
        var n = levels;
        var xax = Repeat("x", n) + "a" + Repeat("x", n);
        var inner = Repeat("x", n - 1) + "a" + Repeat("x", n - 1);
        string Each(Func<int, string> open) => string.Concat(Enumerable.Range(0, n).Select(open));
        string EachInner(Func<int, string> open) => string.Concat(Enumerable.Range(0, n - 1).Select(open));
        return name switch
        {
            // One kind of level alone.
            "capturing" => (Repeat("(", n) + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "unicode capturing" => (Repeat("(", n) + "a" + Repeat(")", n), RegexFlags.Unicode, "a"),
            "non-capturing" => (Repeat("(?:", n) + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "named" => (Each(static i => "(?<n" + Decimal(i) + ">") + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "look-ahead" => (Repeat("(?=", n) + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "negative look-ahead" => (Repeat("(?!", n) + "b" + Repeat(")", n), RegexFlags.None, n % 2 == 1 ? "a" : "b"),
            "look-behind" => ("a" + Repeat("(?<=", n) + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "capturing look-behind" => ("a" + Each(static i => i % 2 == 1 ? "(" : "(?<=") + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "modifier" => (Repeat("(?i:", n) + "a" + Repeat(")", n), RegexFlags.None, "A"),
            "v class" => (Repeat("[", n) + "a" + Repeat("]", n), RegexFlags.UnicodeSets, "a"),
            "vi class" => (Repeat("[", n) + "a" + Repeat("]", n), RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, "A"),
            "v complement" => (Repeat("[^", n) + "a" + Repeat("]", n), RegexFlags.UnicodeSets, n % 2 == 0 ? "a" : "b"),
            // Under vi, \P{Lu} is the characters with no simple case folding of their own less the folded Lu, so it
            // holds 1 and not A (which folds to a); [^\P{Lu}] is the folded Lu, which A matches. Odd levels match A.
            "vi complemented property" => (Repeat("[^", n) + @"\P{Lu}" + Repeat("]", n), RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, n % 2 == 1 ? "A" : "1"),
            "v intersection" => (Repeat("[a&&", n) + "a" + Repeat("]", n), RegexFlags.UnicodeSets, "a"),
            // [a--b] is {a}, [a--[a--b]] is empty, and so on: an odd number of levels, as at the maximum, matches a.
            "v subtraction" => (Repeat("[a--", n) + "b" + Repeat("]", n), RegexFlags.UnicodeSets, "a"),
            "v union" => (Repeat("[b", n) + "a" + Repeat("]", n), RegexFlags.UnicodeSets, "a"),
            "repeated capturing group" => (Repeat("(", n) + "a" + Repeat(")+", n), RegexFlags.None, "a"),
            "repeated non-capturing group" => (Repeat("(?:", n) + "a" + Repeat(")+", n), RegexFlags.None, "a"),
            "optional capturing group" => (Repeat("(", n) + "a" + Repeat(")?", n), RegexFlags.None, "a"),

            // A sequence or an alternation on each level, matched to the innermost.
            "alternation" => (Repeat("(?:b|", n) + "a" + Repeat(")", n), RegexFlags.None, "a"),
            "sequence" => (Repeat("(?:x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "alternation of sequences" => (Repeat("(?:b|x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "modifier sequence" => (Repeat("(?i:x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax.ToUpperInvariant()),
            "modifier alternation of sequences" => (Repeat("(?i:b|x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax.ToUpperInvariant()),
            "look-ahead sequence" => (Repeat("(?=x", n) + "a" + Repeat(")", n), RegexFlags.None, Repeat("x", n) + "a"),
            "look-behind alternation of sequences" => (@"[\s\S]*(?<=" + Repeat("(?:b|x", n - 1) + "a" + Repeat("x)", n - 1) + ")", RegexFlags.None, inner),
            "groups over v classes" => (MixedGroups(n - n / 4) + Repeat("[", n / 4) + "a" + Repeat("]", n / 4) + Repeat(")", n - n / 4), RegexFlags.UnicodeSets, "a"),
            "modifiers over v classes" => (Repeat("(?i:", n / 2) + Repeat("[", n - n / 2) + "a" + Repeat("]", n - n / 2) + Repeat(")", n / 2), RegexFlags.UnicodeSets, "A"),

            // Mixtures whose match at the maximum the old matcher did not prove (ShapesTheOldMatcherDidNotProve).
            "capturing sequence" => (Repeat("(x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "capturing alternation of sequences" => (Repeat("(b|x", n) + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "named alternation of sequences" => (Each(static i => "(?<g" + Decimal(i) + ">b|x") + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "mixed alternation of sequences" => (
                Each(static i => (i % 4) switch { 0 => "(b|x", 1 => "(?:b|x", 2 => "(?<g" + Decimal(i) + ">b|x", _ => "(?i:b|x" }) + "a" + Repeat("x)", n),
                RegexFlags.None, xax),
            "capturing alternation of optional sequences" => (Repeat("(b|x?", n) + "a" + Repeat("x?)", n), RegexFlags.None, xax),
            "capturing alternation of sequences with assertions" => (Repeat(@"(b|x\B", n) + "a" + Repeat(@"\Bx)", n), RegexFlags.None, xax),
            "capturing alternation of sequences with back-references" => (Each(static i => @"(b|x\" + Decimal(i + 1)) + "a" + Repeat("x)", n), RegexFlags.None, xax),
            "capturing alternation of sequences with back-references and assertions" => (
                Each(static i => @"(b|x\" + Decimal(i + 1) + @"\B") + "a" + Repeat(@"\Bx)", n), RegexFlags.None, xax),
            "capturing alternation of sequences with back-references on both sides" => (
                Each(static i => @"(b|x\" + Decimal(i + 1) + @"\B") + "a" + string.Concat(Enumerable.Range(0, n).Reverse().Select(static i => @"\" + Decimal(i + 1) + "x)")),
                RegexFlags.None, xax),
            "repeated alternation of sequences" => (Repeat("(?:b|x", n) + "a" + Repeat("x)+", n), RegexFlags.None, xax),
            "repeated capturing alternation of sequences" => (Repeat("(b|x", n) + "a" + Repeat("x)+", n), RegexFlags.None, xax),
            "repeated capturing alternation of sequences with back-references" => (Each(static i => @"(b|x\" + Decimal(i + 1)) + "a" + Repeat("x)+", n), RegexFlags.None, xax),
            "repeated capturing alternation of sequences with back-references and assertions" => (
                Each(static i => @"(b|x\" + Decimal(i + 1) + @"\B") + "a" + Repeat("x)+", n), RegexFlags.None, xax),
            "look-behind capturing alternation of sequences" => (@"[\s\S]*(?<=" + Repeat("(b|x", n - 1) + "a" + Repeat("x)", n - 1) + ")", RegexFlags.None, inner),
            "look-behind capturing alternation of sequences with back-references and assertions" => (
                @"[\s\S]*(?<=" + EachInner(static i => @"(b|x\" + Decimal(i + 1) + @"\B") + "a" + Repeat(@"\Bx)", n - 1) + ")", RegexFlags.None, inner),
            _ => throw new ArgumentException(name),
        };
    }

    /// <summary>Capturing, non-capturing, named, look-ahead and modifier groups in turn, <paramref name="count"/> of them.</summary>
    private static string MixedGroups(int count) =>
        string.Concat(Enumerable.Range(0, count).Select(static i => (i % 5) switch { 0 => "(", 1 => "(?:", 2 => "(?<g" + Decimal(i) + ">", 3 => "(?=", _ => "(?i:" }));

    /// <summary>
    /// The shapes a pattern at the maximum matches on a 1 MiB thread, with the margins the class remarks require (each
    /// kind of level alone; an alternation, a sequence or an alternation of sequences without a capture on each level;
    /// a repeated or optional group; a look-ahead sequence; mixtures of group kinds over <c>v</c> classes).
    /// </summary>
    public static TheoryData<string> MatchedShapes => new()
    {
        "capturing", "unicode capturing", "non-capturing", "named", "look-ahead", "negative look-ahead", "look-behind",
        "capturing look-behind", "modifier", "v class", "vi class", "v complement", "vi complemented property",
        "v intersection", "v subtraction", "v union", "repeated capturing group", "repeated non-capturing group",
        "optional capturing group", "alternation", "sequence", "alternation of sequences", "modifier sequence",
        "modifier alternation of sequences", "look-ahead sequence", "look-behind alternation of sequences",
        "groups over v classes", "modifiers over v classes",
    };

    /// <summary>
    /// The shapes whose match at the maximum the old matcher did not prove: a capture with a sequence, an alternation, a
    /// back-reference, an assertion or a quantifier on each level cost its continuations more stack per level than a
    /// 1 MiB thread holds 512 times over with a margin, and some failed at the maximum. The new backend, with no CLR recursion
    /// (A-4's matcher half), matches each of them at the maximum, task 2.4a's for those holding no back-reference,
    /// assertion or look-behind and task 2.4b's for the rest; <see cref="BroilerRegex"/> reaches it at task 2.4c. The depths each first failed at on
    /// linux-x64 are in <see cref="BroilerRegex.MaximumNestingLimit"/>'s remarks.
    /// </summary>
    public static TheoryData<string> ShapesTheOldMatcherDidNotProve => new()
    {
        "capturing sequence", "capturing alternation of sequences", "named alternation of sequences",
        "mixed alternation of sequences", "capturing alternation of optional sequences",
        "capturing alternation of sequences with assertions", "capturing alternation of sequences with back-references",
        "capturing alternation of sequences with back-references and assertions",
        "capturing alternation of sequences with back-references on both sides", "repeated alternation of sequences",
        "repeated capturing alternation of sequences", "repeated capturing alternation of sequences with back-references",
        "repeated capturing alternation of sequences with back-references and assertions",
        "look-behind capturing alternation of sequences",
        "look-behind capturing alternation of sequences with back-references and assertions",
    };

    /// <summary>Every shape: the two lists together.</summary>
    public static TheoryData<string> AllShapes
    {
        get
        {
            var all = new TheoryData<string>();
            foreach (var shape in MatchedShapes.Concat(ShapesTheOldMatcherDidNotProve))
                all.Add(shape.Data);
            return all;
        }
    }

    /// <summary>Runs <paramref name="body"/> on a new thread of a 1 MiB stack, and rethrows what it throws.</summary>
    private static void OnASmallStack(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
        }, StackBytes);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    [Fact]
    public void The_shapes_are_listed_once_and_nest_to_the_depth_they_are_built_for()
    {
        var all = AllShapes.Select(static s => s.Data).ToList();
        Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
        foreach (var shape in all)
        {
            // Not vacuous: three levels, depth 4, compile under a limit of 4 and are refused under 3, and the subject is
            // matched. Three, like the 511 levels at the maximum, is odd, which the alternating shapes need.
            var (pattern, flags, subject) = Shape(shape, 3);
            Assert.Equal(RegexStatus.Success, TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, 4, out var regex, out _));
            Assert.Equal(RegexStatus.LimitExceeded, TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, 3, out _, out _));
            Assert.True(regex!.Match(subject).Success, shape);
        }
    }

    [Theory]
    [MemberData(nameof(AllShapes))]
    public void A_pattern_at_the_maximum_parses_and_compiles_on_a_1_MiB_stack(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (pattern, flags, _) = Shape(shape, max - 1);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.Success, TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, max, out var regex, out _));
            Assert.NotNull(regex);

            // The constructor parses under the same maximum.
            Assert.Equal(pattern, TestRegex.Compile(pattern, flags).Pattern);
        });
    }

    [Theory]
    [MemberData(nameof(MatchedShapes))]
    public void A_pattern_at_the_maximum_matches_on_a_1_MiB_stack(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (pattern, flags, subject) = Shape(shape, max - 1);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.Success, TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, max, out var regex, out _));
            Assert.True(regex!.Match(subject).Success);
            Assert.True(TestRegex.Compile(pattern, flags).IsMatch(subject));
        });
    }

    /// <summary>
    /// The shapes the old backend's matcher did not prove at the maximum, which could end the test host with a stack
    /// overflow and were tracked, not run, until task 2.4c routed <see cref="BroilerRegex"/> through task 2.4a's backend,
    /// which has no CLR recursion (check A-4): each now matches on a 1 MiB stack, as every other shape does.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShapesTheOldMatcherDidNotProve))]
    public void A_pattern_at_the_maximum_of_a_shape_the_old_matcher_did_not_prove_matches_on_a_1_MiB_stack(string shape)
    {
        A_pattern_at_the_maximum_matches_on_a_1_MiB_stack(shape);
    }

    [Theory]
    [MemberData(nameof(AllShapes))]
    public void One_level_past_the_maximum_is_refused_on_a_1_MiB_stack(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (pattern, flags, _) = Shape(shape, max);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.LimitExceeded, TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, max, out var regex, out var limit));
            Assert.Equal(RegexLimit.Nesting, limit);
            Assert.Null(regex);
            Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags));
        });
    }

    [Fact]
    public void A_pattern_far_past_the_maximum_is_refused_on_a_1_MiB_stack()
    {
        var (groups, _, _) = Shape("capturing", 1_000_000);
        var (classes, flags, _) = Shape("v class", 1_000_000);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.LimitExceeded, TestRegex.TryCompile(groups, RegexFlags.None, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _, out _));
            Assert.Equal(RegexStatus.LimitExceeded, TestRegex.TryCompile(classes, flags, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _, out _));
        });
    }
}
