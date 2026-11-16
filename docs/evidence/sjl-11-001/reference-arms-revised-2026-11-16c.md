# Bundle SJL-11-001's reference-arm rule, revised on 2026-11-16 by the owner's ruling on P3's `O06`: an input the arbiter cannot reach, classed by the row that names it

**Written:** 2026-11-16, at SJL-11.2, **after** task 2.13's runs (unjudged), their classification by the classifiers
`@3` to `@7`, the reduction of 2026-11-15, and the input this file moves, P3's `O06`, were seen
([`docs/roadmap.md`](../../roadmap.md), the records of task 2.13 and of 2026-11-15 and 2026-11-16, and the record this
file is filed with). [`reference-arms.md`](reference-arms.md) states no revision rule of its own; the bundle's is
[`decision-rule.md`](decision-rule.md)'s "Revisions": "A rule revised after any run it judges has been seen is a new
dated file in this directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
evidence." **This revision was written after the runs it would judge, their classification and the reduction were
seen, so its decision is weaker evidence**: the clause it adds was drafted for one input those runs found, and that
input's answers were known when it was written. It is a new dated file beside `reference-arms.md`,
[`reference-arms-revised-2026-11-16.md`](reference-arms-revised-2026-11-16.md) (option R) and
[`reference-arms-revised-2026-11-16b.md`](reference-arms-revised-2026-11-16b.md) (the owner's ruling on population
P4), the next name in this directory's convention. It quotes whole the text it revises, option R's rule block and
`reference-arms.md`, and leaves every one of those files unedited. **Owner:** the Broiler.RegEx maintainer.
**Reviewer:** none (EX-30). **It holds no figure** but those the ruling quotes: the counts it moves are in the
roadmap's record of 2026-11-16 filed with it.

## The owner's decision

The question put to the owner on 2026-11-16 was, verbatim: "O06 is /^[a]*$/u matched against 10,000,001 'a'. The
library and the Broiler.VM regex engine both match the whole input; Broiler.JS answers null. The reference checker
can't reach that length, but cut down to 999,992 characters, the library, Broiler.JS and the checker all agree on a
full match. Broiler.VM's row B1 names Broiler.JS's cause: an early step-budget exit. The current rule still keeps it
unclassified, because neither side stopped at a declared limit. How should the reference-arms rule read it?"

The options were "Classify by its row" (recommended) and "Keep it unclassified".

**The owner chose "Classify by its row"**, described as: "A dated rule revision lets an unreachable input be classed
by the row that names it, but only if the row gives the mechanism, every other engine agrees with the library, and a
stated check shows the library's answer is right. O06 becomes a Broiler.JS correction (class a, row B1)."

**That is the owner's own decision.** It is not a choice taken under the owner's standing direction of 2026-10-18
("Please do all recommended decisions and skip waiting for review."), as option R was. What this file adds to the
ruling is its wording: the clause below, carried from a draft written in the notes of Broiler.VM's stage of
2026-11-16 (the stage of Broiler-Platform/Broiler.VM#215; the draft is not part of that pull request and was committed
nowhere), with its conditions checked against the ruling's words.

## What it revises

- **Option R's arbiter clause**, the rule block of `reference-arms-revised-2026-11-16.md`, quoted whole below, whose
  last limb reads: "; an input at whose reduced case S and the arm answer alike, S's answer there being the arbiter's,
  is listed in a class (b') "beyond the arbiter", with its prefix and both answers there, and is not class (c),
  provided that at the original input one side is stopped by a bound it declares (S's step, frame or trail ceiling,
  arm V's allowance, or for arm J the harness's stop the stop list holds); without such a stop it stays class (c)".
  Under it, `O06` stays class (c) whatever row names it: the arbiter cannot reach the input, its reduced case shows no
  difference, and at the input neither side stopped at a bound it declares (S matches the whole subject; arm J answers
  `null`).
- Through option R, **`reference-arms.md`'s arbiter clause**, "a difference the arbiter cannot reach in its step
  cut-off is class (c) until reduced to an input it can", and its class (a), "correction - an arm is wrong and S is
  right by the arbiter; listed with the engine it corrects (J, V or both), the input, and its row of the
  correspondence"; `reference-arms.md` is quoted whole at the end. The clause below admits to class (a) an input where
  "S is right" is shown otherwise than by the arbiter at the input itself, under the conditions it states.

The revision of 2026-11-16b (population P4 without the 19 patterns the owner's ruling names) is not revised: an
excluded pattern's input is in no class, and this clause reaches none of them.

## The revision

```rule
arbiter: as reference-arms-revised-2026-11-16.md's (option R), whose last limb reads on, after "without such a stop it stays class (c)":
  ; an input the arbiter cannot reach, at whose reduced case S, the arm and the arbiter answer alike, and at which
    neither side stopped at a bound it declares, is classed by the row of the correspondence that names it, as
    the row's correction, and listed in class (a) "beyond the arbiter, by row" with its prefix, the answers at it and
    the row, provided that:
    (1) the row names the input (its pattern, flags and subject, or a class of subjects the input is in) and the
        mechanism by which the arm's answer at the input differs from its answer at the prefix;
    (2) every other arm that implements the operation answers the input as S does;
    (3) S's answer at the input is the text's by evidence other than the arbiter: it is the answer the reduced case's
        agreed answer extends to, as the pattern requires (for `^[a]*$` over a subject of `a` alone, a match of the
        whole subject), checked by a predicate the classifier states;
    otherwise it stays class (c)
admitted difference classes: as reference-arms.md's, option R's and the revision of 2026-11-16b's; class (a) lists the inputs of the clause above apart, as "beyond the arbiter, by row", each with its prefix, the answers at it and the row
```

The MET line is unchanged: the inputs this clause admits are in class (a), which every MET line of the rule, as filed
and as revised, admits.

The clause is the draft's, word for word, between "; an input the arbiter cannot reach" and "otherwise it stays class
(c)". The lines around it say where it stands in option R's rule block and that class (a) lists these inputs apart.

## Its conditions, checked against the ruling's words

| The ruling's words | The clause |
|---|---|
| "lets an unreachable input be classed by the row that names it" | "an input the arbiter cannot reach ... is classed by the row of the correspondence that names it", and condition (1), "the row names the input (its pattern, flags and subject, or a class of subjects the input is in)" |
| "but only if the row gives the mechanism" | condition (1), "and the mechanism by which the arm's answer at the input differs from its answer at the prefix" |
| "every other engine agrees with the library" | condition (2), "every other arm that implements the operation answers the input as S does": each arm copies an engine (arm V Broiler.VM's, arm J Broiler.JS's), and S is the library |
| "a stated check shows the library's answer is right" | condition (3), "S's answer at the input is the text's by evidence other than the arbiter ... checked by a predicate the classifier states" |
| "O06 becomes a Broiler.JS correction (class a, row B1)" | "classed by the row ... as the row's correction, and listed in class (a)"; row B1's class names Broiler.JS (and Broiler.Regex's preview.4) as corrected |

Where the clause says more than the ruling, it narrows it; nothing in it widens the ruling:
- **Its scope.** "at whose reduced case S, the arm and the arbiter answer alike, and at which neither side stopped at a
  bound it declares" restricts the clause to the inputs option R leaves in class (c) after its other limbs: an input
  whose reduced case differs is classed as that difference, and one where a side stopped is (b') or (c), as option R
  reads them. The ruling's question is about such an input ("cut down to 999,992 characters, the library, Broiler.JS
  and the checker all agree", "neither side stopped at a declared limit").
- **Condition (1)** asks the row to name the input as well as the mechanism, which the ruling's "the row that names it"
  says.
- **Condition (3)** asks the check to tie S's answer at the input to the reduced case's agreed answer, which is the
  arbiter's, so the check does not stand alone.

**Row B1 and the mechanism.** Broiler.VM's correspondence at `626e4ed8` (the merge of Broiler-Platform/Broiler.VM#215)
holds row B1's widening of 2026-11-16 (decision D-jsexec), which names `/^[a]*$/u` over 10,000,001 `a` (`P3-O06`) and
gives the mechanism: "the class's run ends at the budget's early exit this row cites (R1), so `$` fails at every start
(an inference from this row's `/[a]*/u` and `/^a*$/u` answers, not traced in preview.4)". The row gives the mechanism
as an inference, not traced in preview.4's source; condition (1) asks that the row give it, which it does, and the
ruling's description names that cause ("Broiler.VM's row B1 names Broiler.JS's cause: an early step-budget exit").

## How it is read

By `scripts/reference-arms/classify.py` `@8`, with `--reduction`, where option R's last limb would leave an input in
class (c):
- **condition (1)**: a row of `BEYOND_ROWS` for the arm's engine whose predicate reads the input the row names (row
  B1's: `^[a]*$` under `u`, a subject of 10,000,001 `a`, from lastIndex 0), and whose line in the correspondence holds
  the row's words naming the input and the mechanism (`check_beyond_rows`; the classifier refuses a correspondence
  without them, exit 2);
- **condition (2)**: every run of every arm other than S and the difference's arm, in the run's answer files for the
  input's population, holds the case and answers it exactly as S does, and there is at least one;
- **condition (3)**, the stated predicate (`whole_run_answer`): for `^X*$`, X one ASCII letter or digit or a class of
  them with no negation, range or escape, under flags without `i`, `m`, `g` and `y`, from lastIndex 0, over a subject
  every code unit of which X holds, the text's answer is a match of the whole subject with no capture (`^` holds at 0,
  the greedy `*` first takes every unit, X holding each, and `$` holds at the end, so the first path in the
  backtracking order succeeds); the clause holds where that answer is S's at the input and is the reduced case's
  agreed answer, the arbiter's, at the reduced subject. For any other pattern, flags or subject the predicate claims no
  answer, and the input stays class (c).
The difference is then class (a) under the row, its line in `classified.tsv.gz` naming "beyond the arbiter, by row",
the prefix, the answers at it, the other arms' answers and the row. Without `--reduction` no input is classed by this
clause, and the classification reads the rule as filed.

## What it leaves

- It reaches only the inputs a row names by the words `BEYOND_ROWS` reads: on task 2.13's run, `O06`, in each of arm
  J's three runs. Every other input option R leaves in class (c) stays there.
- It does not change how the runs are made, the arbiter, its cut-off or the reduction.
- **It declares nothing met.** Whether class (c) is empty is read from the classification, in the roadmap's record.

## The alternative, the owner's

**"Keep it unclassified"**: option R as written, under which `O06` stays class (c) and the reference arms read NOT MET
at the candidate reading while it stays there. The owner did not choose it. The draft named other ways out under it,
none taken: raising the arbiter's cut-off past ten million steps for this input, having arm J declare preview.4's
budget exit as a stop, or excluding the input by the owner's ruling as the 19 P4 patterns were.

## The text it revises, quoted whole

**Option R's rule block**, from `reference-arms-revised-2026-11-16.md` at its blob
`566299980362880867d019e2f12248fc560ad601` on Broiler.Regex's `main` at `23deeb63`, each line prefixed with "> ":

> ```rule
> arbiter: as reference-arms.md's arbiter clause, which reads on, after "until reduced to an input it can":
>   ; a reduction is declared before the run it reads: scripts/reference-arms/reduce.py (sjl-11-001-reduction@1, at
>     Broiler.Regex 68caeeb), which cuts the exec's subject, from the lastIndex the exec reads (0 without g or y), to a
>     prefix the arbiter reaches with one more code unit past it, found by bisection at the same cut-off, and runs the
>     arms, the comparison and the classifier over the reduced cases unchanged
>   ; an input whose reduced case differs, in the same kinds of answer or in others, is classed as that difference
>   ; an input no prefix of whose subject the arbiter reaches, not even the shortest, stays class (c)
>   ; an input at whose reduced case S and the arm answer alike, S's answer there being the arbiter's, is listed in a
>     class (b') "beyond the arbiter", with its prefix and both answers there, and is not class (c), provided that at
>     the original input one side is stopped by a bound it declares (S's step, frame or trail ceiling, arm V's
>     allowance, or for arm J the harness's stop the stop list holds); without such a stop it stays class (c)
> admitted difference classes: as reference-arms.md's, and
>   (b') beyond the arbiter - as above; listed with its prefix and both answers at it
> MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a), (a'), (b) or (b') and is listed, every class (b) difference the specification admits is in a vector at a named corpus revision, no class (c) difference remains, and both negative controls were reported unclassified.
> NOT MET otherwise.
> ```

**`reference-arms.md`** at its blob `724943b3ad8394544b29d28ef6a11869e644a8b3` on Broiler.Regex's `main` at `23deeb63`
(the blob it had at `b6580b8c` and `f1816df9`, which option R and the revision of 2026-11-16b quote), each line prefixed
with "> ":

> # Bundle SJL-11-001's reference-arm rule: Broiler.Regex against both engines' pre-cut-over regular expressions
>
> **Written:** 2026-10-20, at SJL-11.1, before either arm, the population's generator or the new backend exists.
> *Revised 2026-10-25, before any run and before this change reached the default branch, to agree with Broiler.VM's
> JSD-0081 as filed: class (b) admits row M4's empty match by that record's decision D41, as its decisions D24 and D41
> say; nothing else changed.*
> *Revised again the same day, after review and still before any run: sections "What this file is" and "What it does
> not decide" are added, which the programme's validation document asks of every rule (section 4); and the last
> sentence of class (c), which read "a difference with arm V on any answer but the rows the correspondence names as
> Broiler.VM's corrections (duplicate named groups among them) is class (c)", also excepts row M4's empty match, which
> class (b) holds by decision D41. Nothing else changed.*
> **Milestone judged:** SJL-11's evidence layer L2 (validation.md's matrix), for clauses 1 and 3. **Owner:** the
> Broiler.RegEx maintainer. **Reviewer:** none (EX-30). **It holds no figure.** **The members each arm copies** are the
> ones Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md` names, by file and line, at its revisions.
>
> **What this file is.** The predeclared decision of the library-level reference-arm comparison of the
> regular-expression area: template T4 of the programme's validation document,
> `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4, at evidence layer L2, with the procedure of its
> section 3, in the shape of Broiler.Unicode's bundle `sjl-7-001`. It declares the comparison before either arm, the
> population's generator or the new backend exists: the arms and what each copies, the operations compared, every
> class of the population with the seeds of its random parts, the arbiter, the admitted classes and the negative
> control. The counts and seeds below are parameters of that population, not results of a run.
>
> ```rule
> evidence class: conformance at library level (not a measurement)
> arms:
>   S  Broiler.Regex at the commit under test, through the public API the engines call: compile with a policy and the provider, validate, match at an index anchored or not with offsets, the helpers
>   V  a test-only copy of Broiler.VM's JsRegExpMatcher (Parser, Emitter, Runner, JsRegExpCase, JsRegExpCharSet) at the correspondence's Broiler.VM revision, with the Unicode calls it makes re-pointed to Broiler.Unicode.Algorithms over Broiler.Unicode.Tables as test-only data, its charge delegate kept and every charge recorded
>   J  a test-only copy of Broiler.JS's CreateRegex, translator, GapScan, TryRouteToBroiler and RunMatch at the correspondence's Broiler.JS revision, over Broiler.Regex 0.1.0-preview.4 from nuget.org and .NET Regex; built as its own executable, because it references the package whose identity S has; run under the invariant culture, under tr-TR and under lt-LT
>   each copy records the source path and git blob of every file it copies and each package version it calls; each change is recorded beside it; a member that throws is copied to throw a recorded exception instead; an arm whose copy changes a result is a defect of the arm, fixed in the arm, and the comparison is run again
> operations: acceptance of a pattern under its flags, with the refusal compared by its kind; exec at a lastIndex under flags d g i m s u v y, by the offsets of every capture and the participating index of every name; a policy's outcome kind (completed, exhausted, refused for nesting); argument conversion, lastIndex handling, the protocols and error construction stay in the engines and are not compared
> population:
>   P1 every regexp vector's pattern, flags, subject and lastIndex at the corpus revision bundle sjl-11-001 names
>   P2 every regular-expression literal and every RegExp call with string-literal arguments the generator extracts from test262 at the pin under test/built-ins/RegExp, test/language/literals/regexp, test/annexB and the String.prototype methods above, by acceptance, each against every string literal of its own file, up to the generator's declared cap per file
>   P3 every row of tests/Adversarial/corpus.tsv declared to complete, under an ample policy
>   P4 2^16 patterns drawn with seed 0x534A4C31312D3031 from ECMA-262 22.2.1's grammar with Annex B, under each of the flag sets "", i, m, s, u, v, iu, iv, y and d, each against 16 subjects drawn from the pattern's own characters, their case variants, astral code points and lone surrogates, of length 0 to 32; a draw is a SplitMix64 output read as the generator's README states
>   P5 every code point as a one-character pattern and as a one-member class, under i, iu and iv, against each member of its simple-folding orbit and its simple upper case
>   P6 the property names of Broiler.Unicode's reference population U2, as \p{...} and \P{...} under u and v
>   P7 every code point as the first and as a later character of a group name, raw and escaped, with and without u
>   every operation runs over every class whose inputs it takes; an arm is called only for what it implements, and the bundle names what it does not
> configuration: each arm its own executable, built in Release and run under the JIT only, never in the Native AOT probe, writing its results per operation and input to a file; the comparison a script over the files; the population generated at the commit the bundle names, its SplitMix64 tested against published reference outputs
> comparison: per operation, input and arm, S's result against the arm's, exactly; charges are recorded and compared with no arm, because the units move by JSD-0081's decision D8
> arbiter: scripts/conformance/regexpref.py (ref:regexpref@1) at the Broiler.JS commit the bundle names, written from ECMA-262 22.2's text and reading the pinned UCD, never the agreement of the arms; a difference the arbiter cannot reach in its step cut-off is class (c) until reduced to an input it can
> admitted difference classes:
>   (a) correction - an arm is wrong and S is right by the arbiter; listed with the engine it corrects (J, V or both), the input, and its row of the correspondence
>   (a') correction by data decision - arm J answers from .NET's or the host's Unicode data and S from UCD 17.0.0 through the provider, under SJL-D-6 (c); listed with its row
>   (b) implementation-defined - an outcome kind that follows from a policy the specification leaves to the implementation (a nesting limit, a frame or trail ceiling, an allowance) where the arm's policy differs from S's declared one; listed, and moved into a vector at a named corpus revision when the specification admits both answers; and, by JSD-0081's decision D41 and not by the specification's text, row M4's empty match - the empty match from a lastIndex inside a surrogate pair under u or v, whose Match Record the text leaves ill-formed - until the text is corrected; listed with the decision, and in a vector of the implementation-defined class with each engine's answer
> every other difference is class (c), a defect of S, fixed before the candidate reading, after which the whole comparison is run again and both runs are retained; a difference with arm V on any answer but the rows the correspondence names as Broiler.VM's corrections (duplicate named groups among them) and row M4's empty match, which class (b) holds by decision D41, is class (c)
> negative control: a copy of arm V with one result changed for one input of P4, and a copy of arm J with one result changed for one input of P1, each run once; the comparison reports each as a difference and does not classify it; the copies are then removed
> MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a), (a') or (b) and is listed, every class (b) difference the specification admits is in a vector at a named corpus revision, no class (c) difference remains, and both negative controls were reported unclassified.
> NOT MET otherwise.
> ```
>
> **How it is read.** As Broiler.Unicode's bundle `sjl-7-001`'s `reference-arms.md`: the comparison writes
> `summary.json` and `differences.tsv.gz` and classifies nothing; the classification is a script written before the
> judged run. **A difference the correspondence does not name falsifies JSD-0081**, and Broiler.VM's record is revised
> before either cut-over lands. The listings are regenerated byte for byte from the commit and the population; the
> bundle keeps their digests and line counts. The arms retire at SJL-16, or stay as a frozen population by decision.
>
> ## What this file does not decide
>
> - **Either engine's parity.** Each engine's cut-over, test262 in it, Broiler.VM's fuel parity, each engine's
>   measurement and the engine-level adversarial corpus are judged by each engine's own rule in its bundle
>   `sjl-11-001`; this population meets no test262 clause.
> - **The vectors' expected values.** Broiler.JS's corpus and its reference `regexpref` decide them; the arms are
>   compared with S and with the arbiter, never with each other, and never decide which engine is better.
> - **What stays in the engines**: argument conversion, `lastIndex` handling, the protocols and error construction,
>   which are not compared here.
> - **Charges.** They are recorded and compared with no arm, because the units move by JSD-0081's decision D8.
> - **The component clauses' other parts**: the contract, test262 in this repository's CI, the adversarial corpus at
>   library level, the vectors at library level and the process are [`decision-rule.md`](decision-rule.md)'s.
> - **Performance.** Nothing here is timed.
