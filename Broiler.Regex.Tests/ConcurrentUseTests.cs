using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Broiler.Regex.Matching;
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
/// look-behind, captures and named groups, and a nested pattern.
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
    internal static string Render(RegexMatch match, IReadOnlyDictionary<string, int> names)
    {
        if (!match.Success)
            return "nomatch";
        var text = new StringBuilder();
        foreach (var group in match.Groups)
            text.Append(group.Success ? group.Index + "," + (group.Index + group.Length) : "-").Append(' ');
        foreach (var (name, index) in names.OrderBy(static n => n.Key, StringComparer.Ordinal))
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

    [Fact]
    public void Warm_concurrent_compiles_and_matches_answer_as_one_thread_does()
    {
        var expected = SingleThreaded();
        Assert.DoesNotContain("nomatch", expected); // not vacuous: every case matches
        var shared = Cases.Select(static c => TestRegex.Compile(c.Pattern, c.Flags)).ToArray();

        var failures = new List<string>();
        var barrier = new Barrier(Threads);
        var workers = Enumerable.Range(0, Threads).Select(t => new Thread(() =>
        {
            barrier.SignalAndWait();
            for (var round = 0; round < 8; round++)
            {
                for (var i = 0; i < Cases.Length; i++)
                {
                    // Odd threads compile their own instance; every thread matches the shared one.
                    var (pattern, flags, subject) = Cases[(i + t) % Cases.Length];
                    var k = (i + t) % Cases.Length;
                    var answers = new List<string> { Render(shared[k].Match(subject), shared[k].GroupNames) };
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
            }
        })).ToList();

        workers.ForEach(static w => w.Start());
        workers.ForEach(static w => w.Join());
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
                object Compile(int k) => Activator.CreateInstance(regexType, Cases[k].Pattern, Cases[k].Flags, UnicodeTables.Instance)!;
                string Answer(object regex, int k) => RenderByReflection(match.Invoke(regex, [Cases[k].Subject, 0])!, (IReadOnlyDictionary<string, int>)names.GetValue(regex)!);

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
    private static string RenderByReflection(object match, IReadOnlyDictionary<string, int> names)
    {
        var type = match.GetType();
        if (!(bool)type.GetProperty("Success")!.GetValue(match)!)
            return "nomatch";
        var text = new StringBuilder();
        foreach (var group in (System.Collections.IEnumerable)type.GetProperty("Groups")!.GetValue(match)!)
        {
            var groupType = group.GetType();
            var index = (int)groupType.GetProperty("Index")!.GetValue(group)!;
            var length = (int)groupType.GetProperty("Length")!.GetValue(group)!;
            text.Append((bool)groupType.GetProperty("Success")!.GetValue(group)! ? index + "," + (index + length) : "-").Append(' ');
        }
        foreach (var (name, index) in names.OrderBy(static n => n.Key, StringComparer.Ordinal))
            text.Append(name).Append('=').Append(index).Append(' ');
        return text.ToString();
    }
}
