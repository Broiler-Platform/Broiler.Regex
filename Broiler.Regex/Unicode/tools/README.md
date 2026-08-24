# Case-fold data generation

`generate-case-folding.py` regenerates `../Generated/CaseFoldingData.g.cs` from the
Unicode Character Database. It is pinned to the same UCD revision as the property tables
in the nested `Broiler.Unicode` component, so `\p{…}` and `Canonicalize` cannot disagree
about which Unicode version they describe.

```sh
base=https://www.unicode.org/Public/17.0.0/ucd
for f in CaseFolding UnicodeData SpecialCasing; do curl -sO "$base/$f.txt"; done
python generate-case-folding.py
mv CaseFoldingData.g.cs ../Generated/
rm CaseFolding.txt UnicodeData.txt SpecialCasing.txt
```

Two tables come out, one per branch of ECMA-262 §22.2.2.9.4 Canonicalize:

- `SimpleFold` — the "common + simple" case folding (`CaseFolding.txt` status `C` and
  `S`), which is what Canonicalize returns under `u`/`v` with `i`.
- `UpperFold` — the non-Unicode branch, precomputed per UTF-16 code unit: the Default
  Case Conversion `toUppercase` of the single code unit, dropped when it is not a single
  code unit or when a non-ASCII input would fold into ASCII. `toUppercase` is the
  unconditional `SpecialCasing.txt` mapping where one exists, otherwise the simple
  uppercase of `UnicodeData.txt` field 12.

Both are emitted as one string of comma-separated `lo[.hi]:[-]delta` runs in hexadecimal.
The inverse (fold orbit) map is rebuilt at run time from these runs, so it is not
generated here.

The generated table and the UCD files it reads are covered by the repository's
[third-party notices](../../../THIRD_PARTY_NOTICES.md).
