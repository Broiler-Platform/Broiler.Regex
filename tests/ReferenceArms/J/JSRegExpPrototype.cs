// Arm J of bundle sjl-11-001's reference-arm comparison (docs/evidence/sjl-11-001/reference-arms.md): a test-only copy.
// Source:  Broiler.JS, Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExpPrototype.cs at 1f91b8f1, and the
//          constructor's assignment in Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs (:389-412), git blobs
//          112362ce710bdf4f9920444fb81ff96210d7b7e2 and 4217ce7529a53ca35038074b76efb8899f3a7dd3, Apache-2.0,
//          Broiler Platform contributors.
// Members: the matcher's call in ExecMatch (:150-180): the start at lastIndex as the harness read it, RunMatch, and the
//          sticky emulation (:179); and the constructor's `(value, globalSearch, …) = CreateRegex(…)` (:391).
// Changes: each marked ARM CHANGE: lastIndex's ToLength and its writes, the legacy statics (:187-200) and the result
//          object (BuildExecResult) are not copied; the start is the harness's; ArmTiering is not called (tiering off).

using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.RegExp;

public partial class JSRegExp
{
    /// <summary>JSRegExp(string pattern, string flags) (:389-398), the assignment from CreateRegex alone.</summary>
    internal static JSRegExp Create(string pattern, string flags)
    {
        var r = new JSRegExp();
        bool ignoreCase, multiline, hasIndices;
        string normalized;
        (r.value, r.globalSearch, ignoreCase, multiline, hasIndices, r.sticky, r.unicode, r.unicodeSets, normalized) = CreateRegex(pattern, flags, out r.captureMap, out r.broiler);
        _ = (ignoreCase, multiline, hasIndices, normalized); // ARM CHANGE: the instance's other fields are not read here
        return r;
    }

    /// <summary>ExecMatch (:150-180), from the start the harness read.</summary>
    internal RegexMatchData ExecMatchAt(string input, int startPosition)
    {
        // ARM CHANGE: GetObservableLastIndex, the past-the-end test and the start's choice (:155-173) are the harness's.
        var match = RunMatch(input, startPosition);

        if (sticky && (!match.Success || match.Index != startPosition))
            match = RegexMatchData.NoMatch;

        // ARM CHANGE: the lastIndex writes and the legacy statics (:181-200) are not copied.
        return match;
    }
}
