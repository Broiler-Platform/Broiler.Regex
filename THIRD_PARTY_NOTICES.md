# Third-party notices

The `Broiler.Regex` package carries no Unicode data. Since requirement R11 (task 2.2 of milestone SJL-11.2,
2026-10-26), every Unicode answer it gives - the property escapes `\p{…}`, the properties of strings, case
folding and white space among them - is computed by the Unicode owner's algorithms,
the package `Broiler.Unicode.Algorithms` from the
[Broiler.Unicode](https://github.com/Broiler-Platform/Broiler.Unicode) repository, over tables the caller hands
in through its `IUnicodeTables` interface. `Broiler.Unicode.Algorithms` carries no tables either. The tables are
derived from the Unicode Character Database and the Unicode emoji data files; whoever embeds them (a JavaScript
engine, or `Broiler.Unicode.Tables`, which this repository's tests and Native AOT probe use) carries them with
Broiler.Unicode's `THIRD_PARTY_NOTICES.md` for the data it generates.

Until then this repository held a generated case-folding table derived from the Unicode Character Database
(`CaseFolding.txt`, `UnicodeData.txt` and `SpecialCasing.txt`) and its generator, both deleted by that change, and
the package depended on `Broiler.UniCode.Properties` and `Broiler.UniCode.Emoji.StringProperties` for its
property and emoji tables.

Those data files and the tables derived from them remain subject to the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html).

The Apache License 2.0 in [`LICENSE`](LICENSE) applies to Broiler's source code and does
not replace the Unicode Terms of Use for Unicode-provided data.

## The conformance vectors in the tests

`tests/Vectors/regexp/` (task 2.12 of milestone SJL-11.2, 2026-11-02) is a byte-for-byte copy of the regular-expression
area of the shared conformance corpus, `conformance/vectors/regexp/` of
[Broiler.JS](https://github.com/Broiler-Platform/Broiler.JS) at commit `b2c2e2243a086285d806866a64b83297791a8ec5`
(corpus revision 32), pinned by `tests/Vectors/regexp.pin`. It is Copyright 2026 Broiler Platform contributors under
the Apache License 2.0, this repository's own licence. Its expected values for property escapes, case folding and white
space are computed over the Unicode Character Database 17.0.0, which remains subject to the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html); its notes name test262 files by path and carry no
test262 text. [`tests/Vectors/README.md`](tests/Vectors/README.md) says where it comes from and how it is refreshed.
The package carries none of it.

## test262 cases in the tests

The test project `Broiler.Regex.Tests`, which is not packed, carries cases ported as data from **test262**, the
ECMAScript conformance suite ([tc39/test262](https://github.com/tc39/test262)), at commit
`ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, the programme's pin (task 2.7 of milestone SJL-11.2, 2026-10-27):
`Broiler.Regex.Tests/Test262EscapeCases.cs` holds the cases of `test/built-ins/RegExp/escape/`, and
`Broiler.Regex.Tests/Test262SourceCases.cs` those of `test/staging/sm/RegExp/escape.js`, and
`Broiler.Regex.Tests/Test262DuplicateNamedGroupsTests.cs` (task 2.6) the assertions of the files whose features list
`regexp-duplicate-named-groups` and the syntax tests of `test/language/literals/regexp/named-groups/` and
`test/built-ins/RegExp/named-groups/`. Each file names the test262 files it is from with the copyright notice each
states. test262 is Copyright (C) 2012 Ecma International, made
available under the BSD License (3-clause), whose text, notice and disclaimer are kept beside the data at
[`Broiler.Regex.Tests/test262-LICENSE.txt`](Broiler.Regex.Tests/test262-LICENSE.txt), as its condition 1 requires. The
package carries none of it.
