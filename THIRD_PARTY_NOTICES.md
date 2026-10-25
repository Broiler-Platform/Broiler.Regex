# Third-party notices

`Broiler.Regex/Unicode/Generated/CaseFoldingData.g.cs` is a generated table derived from
the Unicode Character Database (`CaseFolding.txt`, `UnicodeData.txt` and
`SpecialCasing.txt`); the generator is
[`Broiler.Regex/Unicode/tools/generate-case-folding.py`](Broiler.Regex/Unicode/tools/generate-case-folding.py).
The property and emoji tables this engine resolves `\p{…}` and the properties of strings
against are not in this repository: they come from the packages `Broiler.UniCode.Properties`
and `Broiler.UniCode.Emoji.StringProperties`, built from the
[Broiler.Unicode](https://github.com/Broiler-Platform/Broiler.Unicode) repository, and each
package carries that repository's `THIRD_PARTY_NOTICES.md` for the data it generates.

Those data files and the tables derived from them remain subject to the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html).

The Apache License 2.0 in [`LICENSE`](LICENSE) applies to Broiler's source code and does
not replace the Unicode Terms of Use for Unicode-provided data.
