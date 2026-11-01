// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Arm J's stand-ins: not copies. They stand for the members of Broiler.JS at 1f91b8f1 that the copied files call and
// that hold no regular-expression behaviour, so that the copies compile unchanged around them (tests/ReferenceArms/
// README.md, "Arm J"). Each names what it stands for.

#nullable enable

namespace Broiler.JavaScript.Runtime
{
    /// <summary>Stands for Broiler.JavaScript.Runtime/JSException.cs: the error the copies throw, here with its message.</summary>
    public sealed class JSException(string message) : System.Exception(message)
    {
    }
}

namespace Broiler.JavaScript.Engine.Core
{
    using Broiler.JavaScript.Runtime;

    /// <summary>Stands for JSEngine.NewSyntaxError (Broiler.JavaScript.Engine/Core/JSEngine.cs:135): a SyntaxError.</summary>
    internal static class JSEngine
    {
        internal static JSException NewSyntaxError(string message) => new("SyntaxError: " + message);
    }
}

namespace Broiler.JavaScript.ExpressionCompiler
{
    /// <summary>The copied JSRegExp.cs names this namespace in a using and reads nothing of it.</summary>
    internal static class NamespaceStandIn
    {
    }
}

namespace Broiler.ECMAScript.Text
{
    /// <summary>The copied JSRegExp.cs names this namespace in a using and reads nothing of it.</summary>
    internal static class NamespaceStandIn
    {
    }
}

namespace Broiler.JavaScript.BuiltIns.RegExp
{
    /// <summary>Stands for RegexTieringDiagnostics.RecordPatternBuilt (RegexTieringDiagnostics.cs), a counter.</summary>
    internal static class RegexTieringDiagnostics
    {
        internal static void RecordPatternBuilt()
        {
        }
    }

    /// <summary>Stands for RegexTiering.Decide (RegexTiering.cs), which RunMatch reaches only when tiering is switched on, as it is not here.</summary>
    internal static class RegexTiering
    {
        internal static System.Text.RegularExpressions.Regex Decide(System.Text.RegularExpressions.Regex value, string input, int start) => value;
    }

    /// <summary>
    /// The instance fields of JSRegExp that RunMatch, FromNet and the arm read (JSRegExp.cs at 1f91b8f1: `value`, `captureMap`,
    /// `unicode`, `unicodeSets`, `globalSearch` and `sticky`, which its constructor sets from CreateRegex's answer, and
    /// `tierCountdown`, zero unless tiering is on). Declared here because their declarations are among the lines the copy
    /// leaves out; `broiler` is declared by the copied JSRegExp.Broiler.cs.
    /// </summary>
    public partial class JSRegExp
    {
        internal System.Text.RegularExpressions.Regex? value;
        internal JSRegExp.CaptureGroupMap? captureMap;
        internal bool unicode;
        internal bool unicodeSets;
        internal bool globalSearch;
        internal bool sticky;
        internal int tierCountdown;
    }
}
