# Third-party notices

`Broiler.Regex/Unicode/Generated/CaseFoldingData.g.cs` is a generated table derived from
the Unicode Character Database (`CaseFolding.txt`, `UnicodeData.txt` and
`SpecialCasing.txt`); the generator is
[`Broiler.Regex/Unicode/tools/generate-case-folding.py`](Broiler.Regex/Unicode/tools/generate-case-folding.py).
The nested [`Broiler.Unicode`](Broiler.Unicode) component, whose property tables this
engine resolves `\p{…}` against, carries [its own notice](Broiler.Unicode/THIRD_PARTY_NOTICES.md)
for the data it generates.

Those data files and the tables derived from them remain subject to the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html).

The Apache License 2.0 in [`LICENSE`](LICENSE) applies to Broiler's source code and does
not replace the Unicode Terms of Use for Unicode-provided data.
