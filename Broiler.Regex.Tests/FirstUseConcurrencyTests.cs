using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Check A-11's concurrency clause for a process's first compilations (convention 10): concurrent compiles and
/// matches answer as single-threaded ones do, from the first one on.
/// </summary>
/// <remarks>
/// <para>
/// Until task 2.3 of SJL-11.2, <c>CodePointSet</c> sorted and merged its ranges in place the first time it was read,
/// without a lock, so a set every compilation reads, such as a static one, had to leave its type's initialization
/// already normalized: otherwise the first compilations of a process, on several threads, rewrote the one shared
/// list at once, and one could lose a range for the rest of the process or throw an exception that is not a
/// <see cref="RegexSyntaxException"/>. A set is now immutable and normalized when it is built; the tests keep the
/// shape of the defect so that it cannot come back.
/// </para>
/// <para>
/// Each test loads a fresh copy of the library into a collectible <see cref="AssemblyLoadContext"/>, so its
/// statics start from type initialization whatever this process's other tests have compiled. The library's
/// dependencies, <c>Broiler.Unicode.Algorithms</c> among them, come from the default context, so the tables
/// handed in are the ones every other test reads.
/// </para>
/// </remarks>
public class FirstUseConcurrencyTests
{
    private sealed class FreshLibrary() : AssemblyLoadContext("fresh Broiler.Regex", isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    private static T WithFreshLibrary<T>(Func<Assembly, T> body)
    {
        var context = new FreshLibrary();
        try
        {
            return body(context.LoadFromAssemblyPath(typeof(BroilerRegex).Assembly.Location));
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    /// The cause, read directly: a <c>CodePointSet</c> has no field that a read could write, as every instance field
    /// is readonly and none holds a collection whose contents can change, and every static set of the library leaves
    /// its type's initialization normalized, so no read of a shared set writes to it (convention 10, decision D31).
    /// </summary>
    [Fact]
    public void Code_point_sets_have_no_field_a_read_writes_and_every_static_one_is_normalized()
    {
        var problems = WithFreshLibrary(static library =>
        {
            var setType = library.GetType("Broiler.Regex.Ast.CodePointSet", throwOnError: true)!;
            var found = new List<string>();
            foreach (var field in setType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsInitOnly)
                    found.Add("CodePointSet." + field.Name + " is not readonly");
                if (field.FieldType.IsArray || field.FieldType.Namespace == "System.Collections.Generic")
                    found.Add("CodePointSet." + field.Name + " is a " + field.FieldType.Name + ", whose contents can change");
            }

            var fields = library.GetTypes()
                .SelectMany(static type => type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(field => field.FieldType == setType && !field.IsLiteral)
                .ToList();
            Assert.NotEmpty(fields);

            // Read only after the check above: on a set that normalizes on its first read, this read would hide what
            // type initialization left.
            var ranges = setType.GetProperty("Ranges")!;
            foreach (var field in fields)
            {
                RuntimeHelpers.RunClassConstructor(field.DeclaringType!.TypeHandle);
                var list = (IReadOnlyList<(int Lo, int Hi)>)ranges.GetValue(field.GetValue(null))!;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Lo > list[i].Hi || (i > 0 && (long)list[i].Lo <= (long)list[i - 1].Hi + 1))
                        found.Add(field.DeclaringType.FullName + "." + field.Name + " is not sorted, disjoint and apart at range " + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            return found;
        });

        Assert.Empty(problems);
    }

    /// <summary>
    /// The effect: many threads making a fresh library's first compilations at once all compile, and
    /// <c>\w</c> still matches each of its four ranges afterwards. A race needs the threads to meet inside one
    /// write to a shared set, so this can pass with a defect present; the test above is the witness.
    /// </summary>
    [Fact]
    public void Concurrent_first_compilations_of_a_fresh_library_agree_with_a_single_thread()
    {
        const int Rounds = 24;
        const int Threads = 16;

        var failures = new List<string>();
        for (var round = 0; round < Rounds; round++)
        {
            failures.AddRange(WithFreshLibrary(static library =>
            {
                var regexType = library.GetType("Broiler.Regex.BroilerRegex", throwOnError: true)!;
                var found = new List<string>();
                var barrier = new Barrier(Threads);
                var workers = Enumerable.Range(0, Threads).Select(_ => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        var regex = Activator.CreateInstance(regexType, @"\w", "", UnicodeTables.Instance)!;
                        var isMatch = regexType.GetMethod("IsMatch", [typeof(string), typeof(int)])!;
                        foreach (var subject in new[] { "0", "9", "A", "Z", "_", "a", "z" })
                        {
                            if (!(bool)isMatch.Invoke(regex, [subject, 0])!)
                            {
                                lock (found)
                                    found.Add(@"\w does not match " + subject);
                            }
                        }
                    }
                    catch (TargetInvocationException e)
                    {
                        lock (found)
                            found.Add(e.InnerException!.GetType().Name + ": " + e.InnerException.Message);
                    }
                })).ToList();

                workers.ForEach(static worker => worker.Start());
                workers.ForEach(static worker => worker.Join());
                return found;
            }));
        }

        Assert.Empty(failures);
    }
}
