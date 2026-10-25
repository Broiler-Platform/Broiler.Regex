using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// The external tool rules run: MSBuild, to evaluate a project. Broiler.JS's suite also runs Python for
/// its corpus generators; this repository regenerates no data (rule SL7), so MSBuild is the only one.
/// </summary>
internal static class Tools
{
    /// <summary>The project's evaluated properties, as MSBuild computes them with every import applied.</summary>
    internal static IReadOnlyDictionary<string, string> Evaluate(string project, IEnumerable<string> properties)
    {
        var names = properties.Distinct(StringComparer.Ordinal).ToList();
        var arguments = new List<string> { "msbuild", project, "-nologo", "-p:Configuration=Release" };
        arguments.AddRange(names.Select(static n => "-getProperty:" + n));
        var output = Run(Dotnet, arguments, Path.GetDirectoryName(project)!);
        using var json = JsonDocument.Parse(output);
        return json.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(static p => p.Name, static p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    private static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    private static string Run(string program, IEnumerable<string> arguments, string directory)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(program + " " + string.Join(' ', arguments) + " exited " + process.ExitCode + ":\n" + output + error.Result);
        return output;
    }
}
