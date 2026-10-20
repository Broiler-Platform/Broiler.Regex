# Bundle SJL-11-001's reference-arm rule: Broiler.Regex against both engines' pre-cut-over regular expressions

**Written:** 2026-10-20, at SJL-11.1, before either arm, the population's generator or the new backend exists.
**Milestone judged:** SJL-11's evidence layer L2 (validation.md's matrix), for clauses 1 and 3. **Owner:** the
Broiler.RegEx maintainer. **Reviewer:** none (EX-30). **It holds no figure.** **The members each arm copies** are the
ones Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md` names, by file and line, at its revisions.

```rule
evidence class: conformance at library level (not a measurement)
arms:
  S  Broiler.Regex at the commit under test, through the public API the engines call: compile with a policy and the provider, validate, match at an index anchored or not with offsets, the helpers
  V  a test-only copy of Broiler.VM's JsRegExpMatcher (Parser, Emitter, Runner, JsRegExpCase, JsRegExpCharSet) at the correspondence's Broiler.VM revision, with the Unicode calls it makes re-pointed to Broiler.Unicode.Algorithms over Broiler.Unicode.Tables as test-only data, its charge delegate kept and every charge recorded
  J  a test-only copy of Broiler.JS's CreateRegex, translator, GapScan, TryRouteToBroiler and RunMatch at the correspondence's Broiler.JS revision, over Broiler.Regex 0.1.0-preview.4 from nuget.org and .NET Regex; built as its own executable, because it references the package whose identity S has; run under the invariant culture, under tr-TR and under lt-LT
  each copy records the source path and git blob of every file it copies and each package version it calls; each change is recorded beside it; a member that throws is copied to throw a recorded exception instead; an arm whose copy changes a result is a defect of the arm, fixed in the arm, and the comparison is run again
operations: acceptance of a pattern under its flags, with the refusal compared by its kind; exec at a lastIndex under flags d g i m s u v y, by the offsets of every capture and the participating index of every name; a policy's outcome kind (completed, exhausted, refused for nesting); argument conversion, lastIndex handling, the protocols and error construction stay in the engines and are not compared
population:
  P1 every regexp vector's pattern, flags, subject and lastIndex at the corpus revision bundle sjl-11-001 names
  P2 every regular-expression literal and every RegExp call with string-literal arguments the generator extracts from test262 at the pin under test/built-ins/RegExp, test/language/literals/regexp, test/annexB and the String.prototype methods above, by acceptance, each against every string literal of its own file, up to the generator's declared cap per file
  P3 every row of tests/Adversarial/corpus.tsv declared to complete, under an ample policy
  P4 2^16 patterns drawn with seed 0x534A4C31312D3031 from ECMA-262 22.2.1's grammar with Annex B, under each of the flag sets "", i, m, s, u, v, iu, iv, y and d, each against 16 subjects drawn from the pattern's own characters, their case variants, astral code points and lone surrogates, of length 0 to 32; a draw is a SplitMix64 output read as the generator's README states
  P5 every code point as a one-character pattern and as a one-member class, under i, iu and iv, against each member of its simple-folding orbit and its simple upper case
  P6 the property names of Broiler.Unicode's reference population U2, as \p{...} and \P{...} under u and v
  P7 every code point as the first and as a later character of a group name, raw and escaped, with and without u
  every operation runs over every class whose inputs it takes; an arm is called only for what it implements, and the bundle names what it does not
configuration: each arm its own executable, built in Release and run under the JIT only, never in the Native AOT probe, writing its results per operation and input to a file; the comparison a script over the files; the population generated at the commit the bundle names, its SplitMix64 tested against published reference outputs
comparison: per operation, input and arm, S's result against the arm's, exactly; charges are recorded and compared with no arm, because the units move by JSD-0081's decision D8
arbiter: scripts/conformance/regexpref.py (ref:regexpref@1) at the Broiler.JS commit the bundle names, written from ECMA-262 22.2's text and reading the pinned UCD, never the agreement of the arms; a difference the arbiter cannot reach in its step cut-off is class (c) until reduced to an input it can
admitted difference classes:
  (a) correction - an arm is wrong and S is right by the arbiter; listed with the engine it corrects (J, V or both), the input, and its row of the correspondence
  (a') correction by data decision - arm J answers from .NET's or the host's Unicode data and S from UCD 17.0.0 through the provider, under SJL-D-6 (c); listed with its row
  (b) implementation-defined - an outcome kind that follows from a policy the specification leaves to the implementation (a nesting limit, a frame or trail ceiling, an allowance) where the arm's policy differs from S's declared one; listed, and moved into a vector at a named corpus revision when the specification admits both answers
every other difference is class (c), a defect of S, fixed before the candidate reading, after which the whole comparison is run again and both runs are retained; a difference with arm V on any answer but the rows the correspondence names as Broiler.VM's corrections (duplicate named groups among them) is class (c)
negative control: a copy of arm V with one result changed for one input of P4, and a copy of arm J with one result changed for one input of P1, each run once; the comparison reports each as a difference and does not classify it; the copies are then removed
MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a), (a') or (b) and is listed, every class (b) difference the specification admits is in a vector at a named corpus revision, no class (c) difference remains, and both negative controls were reported unclassified.
NOT MET otherwise.
```

**How it is read.** As Broiler.Unicode's bundle `sjl-7-001`'s `reference-arms.md`: the comparison writes
`summary.json` and `differences.tsv.gz` and classifies nothing; the classification is a script written before the
judged run. **A difference the correspondence does not name falsifies JSD-0081**, and Broiler.VM's record is revised
before either cut-over lands. The listings are regenerated byte for byte from the commit and the population; the
bundle keeps their digests and line counts. The arms retire at SJL-16, or stay as a frozen population by decision.
