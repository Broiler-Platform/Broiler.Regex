using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The library's nesting maximum is proven on the smallest stack the adversarial corpus declares, 1 MiB
/// (requirement R4, decision D6 of Broiler.VM's JSD-0081; bundle sjl-11-001's check A-4): a pattern at
/// <see cref="BroilerRegex.MaximumNestingLimit"/> compiles and matches on a thread of that stack, which each test starts
/// itself, and one level past it is refused with the nesting status, never a stack overflow. The Native AOT probe runs
/// the same proof on a thread of the same stack (its population's N cases).
/// </summary>
/// <remarks>
/// The shapes are those the maximum was measured on: each kind of level alone, and the shapes that cost the most
/// stack for a level in the parser (a <c>v</c>-mode class, a modifier group) and in the matcher (an alternation of
/// sequences).
/// </remarks>
public class NestingMaximumTests
{
    private const int StackBytes = 1 << 20;

    /// <summary>
    /// A pattern of <paramref name="levels"/> levels of one shape, and a subject it matches: <c>open</c> repeated, the
    /// atom, <c>close</c> repeated; the subject repeats <c>before</c> and <c>after</c> once a level around the atom's.
    /// </summary>
    private static (string Pattern, RegexFlags Flags, string Subject) Shape(string name, int levels)
    {
        var (prefix, open, atom, close, flags, before, middle, after) = name switch
        {
            "capturing" => ("", "(", "a", ")", RegexFlags.None, "", "a", ""),
            "unicode capturing" => ("", "(", "a", ")", RegexFlags.Unicode, "", "a", ""),
            "non-capturing" => ("", "(?:", "a", ")", RegexFlags.None, "", "a", ""),
            "named" => ("", "", "a", ")", RegexFlags.None, "", "a", ""),
            "look-ahead" => ("", "(?=", "a", ")", RegexFlags.None, "", "a", ""),
            "negative look-ahead" => ("", "(?!", "b", ")", RegexFlags.None, "", levels % 2 == 1 ? "a" : "b", ""),
            "look-behind" => ("a", "(?<=", "a", ")", RegexFlags.None, "", "a", ""),
            "capturing look-behind" => ("a", "(?<=", "a", ")", RegexFlags.None, "", "a", ""),
            "modifier" => ("", "(?i:", "a", ")", RegexFlags.None, "", "A", ""),
            "v class" => ("", "[", "a", "]", RegexFlags.UnicodeSets, "", "a", ""),
            "vi class" => ("", "[", "a", "]", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, "", "A", ""),
            "v intersection" => ("", "[a&&", "a", "]", RegexFlags.UnicodeSets, "", "a", ""),
            "v union" => ("", "[b", "a", "]", RegexFlags.UnicodeSets, "", "a", ""),
            "alternation" => ("", "(?:b|", "a", ")", RegexFlags.None, "", "a", ""),
            "sequence" => ("", "(?:x", "a", "x)", RegexFlags.None, "x", "a", "x"),
            "alternation of sequences" => ("", "(?:b|x", "a", "x)", RegexFlags.None, "x", "a", "x"),
            "repeated alternation of sequences" => ("", "(?:b|x", "a", "x)*", RegexFlags.None, "", "b", ""),
            "modifier sequence" => ("", "(?i:x", "a", "x)", RegexFlags.None, "X", "A", "X"),
            "repeated capturing group" => ("", "(", "a", ")+", RegexFlags.None, "", "a", ""),
            _ => throw new ArgumentException(name),
        };

        var pattern = new StringBuilder(prefix);
        for (var i = 0; i < levels; i++)
        {
            pattern.Append(name switch
            {
                "named" => "(?<n" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ">",
                "capturing look-behind" when i % 2 == 1 => "(",
                _ => open,
            });
        }
        pattern.Append(atom);
        for (var i = 0; i < levels; i++)
            pattern.Append(close);

        var subject = new StringBuilder(prefix);
        for (var i = 0; i < levels; i++)
            subject.Append(before);
        subject.Append(middle);
        for (var i = 0; i < levels; i++)
            subject.Append(after);
        return (pattern.ToString(), flags, subject.ToString());
    }

    public static TheoryData<string> Shapes => new()
    {
        "capturing", "unicode capturing", "non-capturing", "named", "look-ahead", "negative look-ahead", "look-behind",
        "capturing look-behind", "modifier", "v class", "vi class", "v intersection", "v union", "alternation", "sequence",
        "alternation of sequences", "repeated alternation of sequences", "modifier sequence", "repeated capturing group",
    };

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

    [Theory]
    [MemberData(nameof(Shapes))]
    public void A_pattern_at_the_maximum_compiles_and_matches_on_a_1_MiB_stack(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (pattern, flags, subject) = Shape(shape, max - 1);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.Ok, BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, max, out var regex, out _));
            Assert.True(regex!.Match(subject).Success);

            // The constructor parses under the same maximum.
            Assert.True(TestRegex.Compile(pattern, flags).IsMatch(subject));
        });
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void One_level_past_the_maximum_is_refused_on_a_1_MiB_stack(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (pattern, flags, _) = Shape(shape, max);
        OnASmallStack(() =>
        {
            Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, max, out var regex, out var limit));
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
            Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(groups, RegexFlags.None, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _, out _));
            Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(classes, flags, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _, out _));
        });
    }
}
