using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Check A-11's concurrency clause (convention 10, decision D31 of Broiler.VM's JSD-0081): the same patterns compiled
/// and matched from many threads at once, through shared instances and through instances each thread compiles,
/// answer exactly as one thread does, both warm and from a fresh load of the library.
/// </summary>
/// <remarks>
/// The cases reach every part of a compiled pattern a match reads: classes and their sets, case folding in both
/// modes, <c>\w</c> and <c>\b</c> under <c>iu</c>, <c>v</c>-mode set operations over properties and strings,
/// look-behind, captures and named groups, a name two groups share (requirement R6), and a nested pattern.
/// </remarks>
public class ConcurrentUseTests
{
    private const int Threads = 16;

    internal static readonly (string Pattern, string Flags, string Subject)[] Cases =
    [
        (@"\w+", "iu", "-ſK_9-"),
        (@"\W", "iu", "ſ!"),
        (@"\bfoo\b", "iu", "a FOO b"),
        (@"[\p{L}--[a-z]]+", "v", "abcDÉF"),
        (@"[^\P{Lowercase_Letter}]+", "iv", "1aBc"),
        (@"[\w--\d]+", "vi", "12ab"),
        (@"[\q{abc|d}\p{RGI_Emoji}]+", "v", "xabc👍🏽d"),
        (@"\p{Script=Greek}+", "u", "xαβγ"),
        (@"[\d\s]+", "", "x1 2 3"),
        (@"(?<=(\d+)(\d+))$", "", "1053"),
        (@"(?<year>\d{4})-(?<month>\d{2})", "", "on 2026-10"),
        (@"(?:(?<a>x)|(?<a>y))\k<a>(?<b>z)?", "", "xyyz"),
        (@"(a|ab)(c|bcd)(d*)", "", "abcd"),
        (@"(?:(a)|b)+", "", "ab"),
        (@"(?i:[a-z]+)\d", "", "ABC1"),
        (@"\u{1F600}+", "u", "x😀😀"),
        (string.Concat(Enumerable.Repeat("(?:", 100)) + "a" + string.Concat(Enumerable.Repeat(")", 100)), "", "ba"),

        // A class of 4,000 code points written in descending order, apart from each other: until task 2.3 its set was
        // a list sorted and merged in place by its first match, which concurrent first matches raced on.
        ("[" + string.Concat(Enumerable.Range(0, 4000).Reverse().Select(static k => (char)(0x4E00 + 2 * k))) + "]+", "", "x\u58A4\u58A6\u58A5"),
    ];

    /// <summary>A match as the tests compare it: the span of the match and of every group, and the group names.</summary>
    internal static string Render(RegexMatch match, RegexGroupNames names)
    {
        if (!match.Success)
            return "nomatch";
        var text = new StringBuilder();
        foreach (var group in match.Groups)
            text.Append(group.Success ? group.Index + "," + (group.Index + group.Length) : "-").Append(' ');
        var named = Enumerable.Range(1, match.Groups.Count - 1).Select(g => (Name: names.NameOfGroup(g), Group: g)).Where(static n => n.Name is not null);
        return Named(text, named.Select(static n => (n.Name!, n.Group)));
    }

    /// <summary>Each name and group pair, by name and then by group, after <paramref name="text"/>.</summary>
    private static string Named(StringBuilder text, IEnumerable<(string Name, int Group)> named)
    {
        foreach (var (name, index) in named.OrderBy(static n => n.Name, StringComparer.Ordinal).ThenBy(static n => n.Group))
            text.Append(name).Append('=').Append(index).Append(' ');
        return text.ToString();
    }

    /// <summary>Every case's answer from one thread, which the concurrent runs must equal.</summary>
    private static string[] SingleThreaded() =>
        [.. Cases.Select(static c =>
        {
            var regex = TestRegex.Compile(c.Pattern, c.Flags);
            return Render(regex.Match(c.Subject), regex.GroupNames);
        })];

    /// <summary>
    /// The warm library: its types are initialized and its code compiled by the single-threaded run first. In each
    /// lockstep round the shared instances are new, and every thread waits at a barrier before each case and then
    /// matches the same shared instance, so a case's first match on its instance is made by all the threads at once,
    /// which is where a compiled pattern that changes itself on a read would race. A staggered pass follows, each
    /// thread starting at another case. An exception a worker meets is a failure of the test, not of the test host.
    /// </summary>
    /// <remarks>
    /// Against <c>0.1.0-preview.4</c>'s <c>CodePointSet</c>, which sorted and merged its ranges in place on its first
    /// read, the lockstep rounds fail where a staggered pass alone almost never does: the threads rarely make one
    /// instance's first match at the same time unless they wait for each other.
    /// </remarks>
    [Fact]
    public void Warm_concurrent_compiles_and_matches_answer_as_one_thread_does()
    {
        var expected = SingleThreaded();
        Assert.DoesNotContain("nomatch", expected); // not vacuous: every case matches

        var failures = new List<string>();
        void Check(int t, int k, BroilerRegex shared)
        {
            try
            {
                // Odd threads compile their own instance too; every thread matches the shared one.
                var (pattern, flags, subject) = Cases[k];
                var answers = new List<string> { Render(shared.Match(subject), shared.GroupNames) };
                if (t % 2 == 1)
                {
                    var own = TestRegex.Compile(pattern, flags);
                    answers.Add(Render(own.Match(subject), own.GroupNames));
                }

                foreach (var answer in answers.Where(a => a != expected[k]))
                {
                    lock (failures)
                        failures.Add("case " + k + ": " + answer + " where one thread answers " + expected[k]);
                }
            }
            catch (Exception e)
            {
                lock (failures)
                    failures.Add("case " + k + ": " + e.GetType().Name + ": " + e.Message);
            }
        }

        void OnEveryThread(Action<int, Barrier> body)
        {
            var barrier = new Barrier(Threads);
            var workers = Enumerable.Range(0, Threads).Select(t => new Thread(() => body(t, barrier))).ToList();
            workers.ForEach(static w => w.Start());
            workers.ForEach(static w => w.Join());
        }

        for (var round = 0; round < 8; round++)
        {
            var shared = Cases.Select(static c => TestRegex.Compile(c.Pattern, c.Flags)).ToArray();
            OnEveryThread((t, barrier) =>
            {
                for (var k = 0; k < Cases.Length; k++)
                {
                    barrier.SignalAndWait();
                    Check(t, k, shared[k]);
                }
            });
        }

        var staggered = Cases.Select(static c => TestRegex.Compile(c.Pattern, c.Flags)).ToArray();
        OnEveryThread((t, barrier) =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < Cases.Length; i++)
                Check(t, (i + t) % Cases.Length, staggered[(i + t) % Cases.Length]);
        });

        Assert.Empty(failures);
    }

    private sealed class FreshLibrary() : AssemblyLoadContext("fresh Broiler.Regex", isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    [Fact]
    public void Concurrent_first_compiles_and_matches_of_a_fresh_library_answer_as_one_thread_does()
    {
        var expected = SingleThreaded();
        var failures = new List<string>();
        for (var round = 0; round < 8; round++)
        {
            var context = new FreshLibrary();
            try
            {
                var library = context.LoadFromAssemblyPath(typeof(BroilerRegex).Assembly.Location);
                var regexType = library.GetType("Broiler.Regex.BroilerRegex", throwOnError: true)!;
                var match = regexType.GetMethod("Match", [typeof(string), typeof(int)])!;
                var names = regexType.GetProperty("GroupNames")!;
                var nameOfGroup = names.PropertyType.GetMethod("NameOfGroup")!;
                object Compile(int k) => Activator.CreateInstance(regexType, Cases[k].Pattern, Cases[k].Flags, UnicodeTables.Instance)!;
                string Answer(object regex, int k) => RenderByReflection(match.Invoke(regex, [Cases[k].Subject, 0])!, names.GetValue(regex)!, nameOfGroup);

                // The shared instances are compiled before the threads start, so that their first matches meet.
                var shared = Enumerable.Range(0, Cases.Length).Select(Compile).ToArray();
                var barrier = new Barrier(Threads);
                var workers = Enumerable.Range(0, Threads).Select(t => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    for (var i = 0; i < Cases.Length; i++)
                    {
                        var k = (i + t) % Cases.Length;
                        try
                        {
                            foreach (var answer in new[] { Answer(shared[k], k), Answer(Compile(k), k) }.Where(a => a != expected[k]))
                            {
                                lock (failures)
                                    failures.Add("case " + k + ": " + answer + " where one thread answers " + expected[k]);
                            }
                        }
                        catch (TargetInvocationException e)
                        {
                            lock (failures)
                                failures.Add("case " + k + ": " + e.InnerException!.GetType().Name);
                        }
                    }
                })).ToList();

                workers.ForEach(static w => w.Start());
                workers.ForEach(static w => w.Join());
            }
            finally
            {
                context.Unload();
            }
        }

        Assert.Empty(failures);
    }

    /// <summary><see cref="Render"/> over a <c>RegexMatch</c> of a fresh copy of the library, read by reflection.</summary>
    private static string RenderByReflection(object match, object names, MethodInfo nameOfGroup)
    {
        var type = match.GetType();
        if (!(bool)type.GetProperty("Success")!.GetValue(match)!)
            return "nomatch";
        var text = new StringBuilder();
        var count = 0;
        foreach (var group in (System.Collections.IEnumerable)type.GetProperty("Groups")!.GetValue(match)!)
        {
            var groupType = group.GetType();
            var index = (int)groupType.GetProperty("Index")!.GetValue(group)!;
            var length = (int)groupType.GetProperty("Length")!.GetValue(group)!;
            text.Append((bool)groupType.GetProperty("Success")!.GetValue(group)! ? index + "," + (index + length) : "-").Append(' ');
            count++;
        }
        var named = Enumerable.Range(1, count - 1).Select(g => (Name: (string?)nameOfGroup.Invoke(names, [g]), Group: g)).Where(static n => n.Name is not null);
        return Named(text, named.Select(static n => (n.Name!, n.Group)));
    }
}
