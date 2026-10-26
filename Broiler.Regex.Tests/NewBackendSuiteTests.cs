using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Broiler.Regex.Tests;

/// <summary>
/// The old suite on the new backend (plan row 2.4b, "the old suite's cases pass"): every case of every behaviour test of
/// this project, the same methods with the same data, run again with <see cref="TestRegex"/>'s switch on, so that each
/// instance a case builds through the public members' shape matches through the new backend's internal route
/// (<c>BroilerRegex.OnTheNewBackend</c>). xunit runs each case on the old backend; this test runs it on the new one, and
/// asserts that it passes there too.
/// </summary>
/// <remarks>
/// <para>
/// <b>What runs.</b> Every public method of a test class marked <c>[Fact]</c> or <c>[Theory]</c>, each theory once per row
/// of its <c>[InlineData]</c> and <c>[MemberData]</c>, on a new instance of its class, as xunit runs it. A case xunit skips
/// is skipped here too, unless its reason names task 2.4c: those are the old backend's known gaps and defects that the
/// route through the new backend closes (defect D1's rows, the nesting shapes the old matcher does not prove), so here
/// they run and must pass. A case's exception fails this test, which names each.
/// </para>
/// <para>
/// <b>What is left out, and why.</b> <see cref="Excluded"/> lists each class or method, with its reason: this test; the
/// differential comparison, which runs both backends itself; the adversarial corpus's child processes, which run the
/// new backend's entry already; and the tests that show the old backend's own answer to a defect.
/// </para>
/// </remarks>
public class NewBackendSuiteTests(ITestOutputHelper output)
{
    /// <summary>The classes and methods left out of the run on the new backend, each with its reason.</summary>
    internal static readonly Dictionary<string, string> Excluded = new(StringComparer.Ordinal)
    {
        [nameof(NewBackendSuiteTests)] = "this test",
        [nameof(DifferentialTests)] = "compares the two backends itself",
        [nameof(AdversarialCorpusTests)] = "runs every row through the new backend's entry in a child process already",
        [nameof(OldBackendDefectTests) + "." + nameof(OldBackendDefectTests.D2_the_old_backend_answers_with_the_empty_string_first)] =
            "shows the old backend's answer to its defect D2",
        [nameof(OldBackendDefectTests) + "." + nameof(OldBackendDefectTests.M7_the_old_backend_compares_the_code_units_that_end_at_the_index)] =
            "shows the old backend's answer to JSD-0081's row M7",
    };

    [Fact]
    public void Every_behaviour_test_passes_on_the_new_backend_too()
    {
        var cases = 0;
        var throughTheRoute = 0;
        var unskipped = 0;
        var skipped = 0;
        var methods = 0;
        var failures = new List<string>();
        foreach (var type in typeof(NewBackendSuiteTests).Assembly.GetTypes().Where(static t => t.IsPublic && t.IsClass && !t.IsAbstract).OrderBy(static t => t.Name, StringComparer.Ordinal))
        {
            if (Excluded.ContainsKey(type.Name))
                continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(static m => m.Name, StringComparer.Ordinal))
            {
                var fact = method.GetCustomAttribute<FactAttribute>();
                if (fact is null || Excluded.ContainsKey(type.Name + "." + method.Name))
                    continue;

                methods++;
                foreach (var (arguments, skip) in Rows(type, method, fact))
                {
                    if (skip is not null && !skip.Contains("2.4c", StringComparison.Ordinal))
                    {
                        skipped++;
                        continue;
                    }

                    unskipped += skip is null ? 0 : 1;
                    cases++;
                    var count = new TestRegex.StrongBox();
                    var failure = Run(type, method, arguments, count);
                    throughTheRoute += count.Value > 0 ? 1 : 0;
                    if (failure is not null)
                        failures.Add(type.Name + "." + method.Name + "(" + string.Join(", ", arguments.Select(Show)) + "): " + failure);
                }
            }
        }

        output.WriteLine($"{methods} test methods, {cases} cases run on the new backend, {throughTheRoute} of them built at least one instance on its route; " +
            $"{unskipped} cases xunit skips until task 2.4c ran and {skipped} other skipped cases did not; {Excluded.Count} classes or methods left out; {failures.Count} failed");
        Assert.True(failures.Count == 0, failures.Count + " cases fail on the new backend:\n" + string.Join('\n', failures.Take(80)));
        Assert.True(throughTheRoute >= 700, "only " + throughTheRoute + " cases built an instance on the new backend's route");
    }

    /// <summary>The rows a test method runs with: none for a fact, each data row for a theory, with the reason it is skipped, if it is.</summary>
    private static IEnumerable<(object?[] Arguments, string? Skip)> Rows(Type type, MethodInfo method, FactAttribute fact)
    {
        if (fact is not TheoryAttribute)
        {
            yield return ([], fact.Skip);
            yield break;
        }

        foreach (var inline in method.GetCustomAttributes<InlineDataAttribute>())
        {
            // [InlineData(null)] is a call of the params constructor with a null array: one argument, null.
            foreach (var row in inline.GetData(method))
                yield return (row ?? [null], inline.Skip ?? fact.Skip);
        }

        foreach (var member in method.GetCustomAttributes<MemberDataAttribute>())
        {
            var source = member.MemberType ?? type;
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var data = source.GetProperty(member.MemberName, all)?.GetValue(null)
                ?? source.GetMethod(member.MemberName, all)?.Invoke(null, member.Parameters)
                ?? source.GetField(member.MemberName, all)?.GetValue(null)
                ?? throw new InvalidOperationException("no member " + member.MemberName + " on " + source.Name);
            foreach (var row in (IEnumerable)data)
                yield return ((object?[])row, member.Skip ?? fact.Skip);
        }
    }

    /// <summary>Runs one case with the switch on, on a new instance of its class; answers its failure, or null.</summary>
    private static string? Run(Type type, MethodInfo method, object?[] arguments, TestRegex.StrongBox count)
    {
        var outer = TestRegex.OnTheNewBackend.Value;
        TestRegex.OnTheNewBackend.Value = count;
        try
        {
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
            var result = method.Invoke(instance, Converted(method.GetParameters(), arguments));
            if (result is Task task)
                task.GetAwaiter().GetResult();
            (instance as IDisposable)?.Dispose();
            return null;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            return e.InnerException.GetType().Name + ": " + FirstLine(e.InnerException.Message);
        }
        catch (Exception e)
        {
            return e.GetType().Name + ": " + FirstLine(e.Message);
        }
        finally
        {
            TestRegex.OnTheNewBackend.Value = outer;
        }
    }

    /// <summary>The arguments as xunit passes them: each converted to its parameter's type, and a missing one its default.</summary>
    private static object?[] Converted(ParameterInfo[] parameters, object?[] arguments)
    {
        var converted = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i >= arguments.Length)
            {
                converted[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                continue;
            }

            var target = Nullable.GetUnderlyingType(parameters[i].ParameterType) ?? parameters[i].ParameterType;
            var argument = arguments[i];
            converted[i] = argument is null || target.IsInstanceOfType(argument)
                ? argument
                : target.IsEnum ? Enum.ToObject(target, argument) : Convert.ChangeType(argument, target, System.Globalization.CultureInfo.InvariantCulture);
        }

        return converted;
    }

    private static string FirstLine(string text)
    {
        var lines = text.Split('\n');
        return lines.Length <= 6 ? text.Replace('\n', ' ') : string.Join(' ', lines.Take(6)) + " …";
    }

    private static string Show(object? argument) => argument switch
    {
        null => "null",
        string text => "\"" + DifferentialTests.Escape(text) + "\"",
        Array array => "[" + string.Join(", ", array.Cast<object?>().Select(Show)) + "]",
        _ => argument.ToString() ?? "",
    };
}
