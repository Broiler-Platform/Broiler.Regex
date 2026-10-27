namespace Broiler.Regex.Tests;

// Ported as data from test262, the ECMAScript conformance suite (https://github.com/tc39/test262), at commit
// ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the programme's pin: the source cases of staging/sm/RegExp/escape.js:
// each test(re, source) line's pattern as written (a literal's body, or the string RegExp is given) and the source it
// expects. test262 is made available under the BSD License (3-clause), whose text, copyright notice and disclaimer
// are kept beside this file in test262-LICENSE.txt, as its condition 1 requires. Each file ported carries its own
// notice, kept here as the file states it:
//   test/staging/sm/RegExp/escape.js: Copyright (C) 2024 Mozilla Corporation. All rights reserved.
// THIS SOFTWARE IS PROVIDED BY THE ECMA INTERNATIONAL "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES ARE
// DISCLAIMED (the license's disclaimer, in full in test262-LICENSE.txt). The data is test-only: it is compiled into
// the test assembly and into no package.

/// <summary>test262's <c>source</c> cases at the programme's pin, as data (the comment above).</summary>
internal static class Test262SourceCases
{
    /// <summary>Each case: the test262 file it is from, the pattern and the source text test262 expects.</summary>
    internal static (string File, string Pattern, string Source)[] Cases() =>
    [
        ("test/staging/sm/RegExp/escape.js", "\\\\n", "\\\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\n", "\\\\\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\\\n", "\\\\\\\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\n", "\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\\u000A", "\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\\\n", "\\\\n"),
        ("test/staging/sm/RegExp/escape.js", "\\\\r", "\\\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\r", "\\\\\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\\\r", "\\\\\\\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\r", "\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\\u000D", "\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\\\r", "\\\\r"),
        ("test/staging/sm/RegExp/escape.js", "\\\\u2028", "\\\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\u2028", "\\\\\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\\\u2028", "\\\\\\\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\u2028", "\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\\u2028", "\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\\\u2028", "\\\\u2028"),
        ("test/staging/sm/RegExp/escape.js", "\\\\u2029", "\\\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\u2029", "\\\\\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\\\u2029", "\\\\\\\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\u2029", "\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\\u2029", "\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\\\u2029", "\\\\u2029"),
        ("test/staging/sm/RegExp/escape.js", "\\/", "\\/"),
        ("test/staging/sm/RegExp/escape.js", "\\\\\\/", "\\\\\\/"),
        ("test/staging/sm/RegExp/escape.js", "/", "\\/"),
        ("test/staging/sm/RegExp/escape.js", "/", "\\/"),
        ("test/staging/sm/RegExp/escape.js", "\\/", "\\/"),
        ("test/staging/sm/RegExp/escape.js", "\\/", "\\/"),
        ("test/staging/sm/RegExp/escape.js", "\\\\/", "\\\\\\/"),
        ("test/staging/sm/RegExp/escape.js", "[/]", "[/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\/]", "[\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\\\/]", "[\\\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\\\\\/]", "[\\\\\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[/]", "[/]"),
        ("test/staging/sm/RegExp/escape.js", "[/]", "[/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\/]", "[\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\/]", "[\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\\\/]", "[\\\\/]"),
        ("test/staging/sm/RegExp/escape.js", "[/]", "[/]"),
        ("test/staging/sm/RegExp/escape.js", "[\\/]", "[\\/]"),
        ("test/staging/sm/RegExp/escape.js", "\\[\\/\\]", "\\[\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[\\\\\\/\\]", "\\[\\\\\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[/\\]", "\\[\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[/\\]", "\\[\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[\\/\\]", "\\[\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[\\/\\]", "\\[\\/\\]"),
        ("test/staging/sm/RegExp/escape.js", "\\[\\\\/\\]", "\\[\\\\\\/\\]"),
    ];
}
