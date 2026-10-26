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
/// <c>CodePointSet</c> sorts and merges its ranges in place the first time it is read, without a lock. A set
/// every compilation reads, such as a static one, must therefore leave its type's initialization already
/// normalized: otherwise the first compilations of a process, on several threads, rewrite the one shared list
/// at once, and one can lose a range for the rest of the process or throw an exception that is not a
/// <see cref="RegexSyntaxException"/>.
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
    /// The cause, read directly: after its type is initialized, and before anything reads it, every static
    /// <c>CodePointSet</c> of the library is already normalized, so no first read writes to it.
    /// </summary>
    [Fact]
    public void Every_static_code_point_set_leaves_type_initialization_normalized()
    {
        var unnormalized = WithFreshLibrary(static library =>
        {
            var setType = library.GetType("Broiler.Regex.Ast.CodePointSet", throwOnError: true)!;
            var normalized = setType.GetField("_normalized", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var fields = library.GetTypes()
                .SelectMany(static type => type.GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(field => field.FieldType == setType && !field.IsLiteral)
                .ToList();
            Assert.NotEmpty(fields);

            var found = new List<string>();
            foreach (var field in fields)
            {
                RuntimeHelpers.RunClassConstructor(field.DeclaringType!.TypeHandle);
                if (!(bool)normalized.GetValue(field.GetValue(null))!)
                    found.Add(field.DeclaringType.FullName + "." + field.Name);
            }

            return found;
        });

        Assert.Empty(unnormalized);
    }

    /// <summary>
    /// The effect: many threads making a fresh library's first compilations at once all compile, and
    /// <c>\w</c> still matches each of its four ranges afterwards. A race needs the threads to meet inside one
    /// normalization, so this can pass with the defect present; the test above is the witness.
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
