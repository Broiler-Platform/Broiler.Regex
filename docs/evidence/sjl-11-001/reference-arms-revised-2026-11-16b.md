# Bundle SJL-11-001's reference-arm rule, revised on 2026-11-16 by the owner's ruling: population P4 without the 19 patterns whose arbiter cost is in the pattern

**Written:** 2026-11-16, at SJL-11.2, **after** task 2.13's runs (unjudged), their classification by the classifier
`@3`, the reduction of 2026-11-15, and their classification again by `@4` and `@5` under option R were seen
([`docs/roadmap.md`](../../roadmap.md), the records of task 2.13, of 2026-11-15 and of 2026-11-16, and the record this
file is filed with). [`reference-arms.md`](reference-arms.md) states no revision rule of its own; the bundle's is
[`decision-rule.md`](decision-rule.md)'s "Revisions": "A rule revised after any run it judges has been seen is a new
dated file in this directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
evidence." **This revision was written after the runs it would judge, their classification and the reduction were
seen, so the population change it makes is weaker evidence**: the patterns it takes out of P4 were found by those
runs. It is a new dated file beside `reference-arms.md` and
[`reference-arms-revised-2026-11-16.md`](reference-arms-revised-2026-11-16.md) (option R), quotes `reference-arms.md`
whole at its end (its blob `724943b3` on Broiler.Regex's `main` at `f1816df9`) and the clause of option R it builds on,
and leaves both files unedited. **Owner:** the Broiler.RegEx maintainer. **Reviewer:** none (EX-30).
*Corrected 2026-11-16, after review and before this file reached the default branch, as `reference-arms.md` was on
2026-10-25: the rule block now says that the exclusion is the enumeration of the 19 by id, the three limbs being how
they were found and limb (3) not a property of the pattern, and names a crash as a stop in limb (3), as
`exclusion.py` reads it; the note's count of the inputs no prefix reaches, its table's last column, its account of the
known gap and its account of the patterns limb (1) alone selects are corrected, and alternatives 1, 3 and 4 are
restated. The ids, the listing and the classification are unchanged. Each passage replaced is quoted in "Corrected
before merge" below.*

## The owner's decision

The question put to the owner on 2026-11-16 was whether the 19 P4 patterns, each with a quantifier minimum of
4,294,967,296, should be dropped from the test population: the arbiter can never decide them however far the subject
is reduced, and their 1,269 inputs (3,789 differences) otherwise keep class (c) from emptying. **The owner answered,
on 2026-11-16: "You can exclude with note regarding the 19 patterns".**

That is the owner's own decision. It is not a choice taken under the standing direction of 2026-10-18 ("Please do all
recommended decisions and skip waiting for review."), and it is not option P as the roadmap's record of 2026-11-16
drafted it (below): the ruling names the 19 patterns, and this file lists them by id and states how they were found.
What this file adds to the ruling is its wording: the enumeration and the limbs it was found by, the extent of an
excluded pattern (every flag set and input of it), how its inputs are listed, and the note.

## What it revises

- **Population P4**, `reference-arms.md`'s line "P4 2^16 patterns drawn with seed 0x534A4C31312D3031 from ECMA-262
  22.2.1's grammar with Annex B, under each of the flag sets "", i, m, s, u, v, iu, iv, y and d, each against 16
  subjects drawn from the pattern's own characters, their case variants, astral code points and lone surrogates, of
  length 0 to 32; a draw is a SplitMix64 output read as the generator's README states".
- **Option R's clause on the inputs no prefix reaches**, which this ruling builds on and does not change: "; an input
  no prefix of whose subject the arbiter reaches, not even the shortest, stays class (c)" (option R's rule block), and
  its effect as option R states it: "**Inputs no prefix reaches stay class (c)**: their patterns quantify a body that
  matches the empty string with a minimum past the arbiter's cut-off, so the arbiter's cost is in the pattern, and no
  cut of the subject reduces it." and "**So class (c) cannot empty under this revision alone.** It empties only if the
  owner also reads those inputs otherwise, which this file does not do: excluding from population P4 the patterns
  whose arbiter cost is in the pattern itself would change the population after its run was seen, and is put to the
  owner as a drafted option in the roadmap's record of 2026-11-16, not taken here."
- **The drafted option P** (the roadmap's record of 2026-11-16, not a rule): "P4 ... excluding a pattern drawn with a
  quantifier whose minimum is at least the arbiter's cut-off over a body that can match the empty string, read from
  the pattern's text; the excluded patterns are listed with the population's manifest". That property alone takes
  278 patterns (below), so this file narrows it to the 19 the ruling names.

## The revision

```rule
population: as reference-arms.md's, P4 read without the patterns the owner's ruling of 2026-11-16 excludes:
  P4 as reference-arms.md's line, excluding by the owner's ruling of 2026-11-16 the 19 patterns drawn that it names, enumerated by id in scripts/reference-arms/p4-excluded.tsv: P4-01347, P4-01872, P4-02466, P4-09242, P4-11265, P4-11714, P4-14530, P4-17689, P4-18962, P4-29456, P4-29657, P4-30238, P4-33179, P4-40469, P4-42576, P4-54314, P4-58688, P4-63268, P4-65213; an excluded pattern is excluded under every flag set, by acceptance and by every exec
  the exclusion is that enumeration, not a property: each of the 19 has, under one of its flag sets, the three limbs below, which say how the 19 were found and are checked against task 2.13's run; limb (3) reads that run's comparison, so the limbs select nothing but the ids
    (1) the pattern's limb: read under the flags (u or v: 22.2.1's grammar; otherwise Annex B's), it quantifies with a braced minimum of at least 2^32 (4,294,967,296) an atom that can match the empty string (an assertion, a look-around, a back-reference, an empty group, a group one of whose alternatives can, or under v a class that holds the empty string)
    (2) the arbiter's limb: on one of the pattern's exec inputs in P4, the arbiter at its cut-off reaches no prefix of the subject, not even the shortest (scripts/reference-arms/reduce.py's outcome none: the subject cut to the lastIndex under g or y, to the empty string otherwise), asked of every exec input of the pattern under the flag sets where limb (1) holds
    (3) the run's limb, not a property of the pattern: on one such input the comparison of task 2.13's run holds a difference where one side stops, at a limit or by a crash, and the other answers, null or a match: a stop against an answer the arbiter cannot check, which class (c) holds
  limbs (1) and (2), the narrowest property found of the pattern and the arbiter, hold for 30 P4 patterns: the 19 and 11 more, which this revision does not exclude; whether the exclusion is to be stated by that property instead is put to the owner
excluded inputs: the population is generated and every arm runs over it as before, the excluded patterns' inputs among them; their differences are compared and classified apart, as excluded by the owner's ruling of 2026-11-16, each with S's answer and the arm's; they are in no admitted class and are not class (c), and are reported with every reading, outside the judged population
MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference outside the excluded patterns is in class (a), (a'), (b) or (b') and is listed, the excluded patterns' differences are listed apart, every class (b) difference the specification admits is in a vector at a named corpus revision, no class (c) difference remains, and both negative controls were reported unclassified.
NOT MET otherwise.
```

**How it is read.** By `scripts/reference-arms/classify.py` `@6` with `--exclude`, given
`scripts/reference-arms/p4-excluded.tsv`, beside `--reduction` as option R reads it: every difference of a listed
pattern is classed `excluded`, listed in `classified.tsv.gz` with S's answer and the arm's, counted apart in
`classification.json`, and the arbiter is not asked for it. The listing is the enumeration; it was written by
`scripts/reference-arms/exclusion.py select`, which applies limb (1) to every pattern of the population, limb (2) to
every exec input of a pattern under the flag sets where limb (1) holds, and limb (3) to the comparison's differences on
the inputs where both hold, and which writes it again byte for byte from task 2.13's run. Where option R stands, its clause "an input no prefix of whose subject the arbiter reaches,
not even the shortest, stays class (c)" no longer reaches an excluded pattern's input; if the owner overturns option R,
the exclusion stands, since limb (2) reads `reduce.py`'s outcome, which the filed rule's "until reduced to an input it
can" already admits, and the MET line then reads "(a), (a') or (b)". The listing is fixed: a later run of the arms over
the same population excludes the same 19 patterns, whatever its comparison holds. Every other clause of
`reference-arms.md`, and of option R while it stands, is as written.

## How the 19 were found, and what the limbs select

Each limb was checked over the population of task 2.13's run (`population/` at `07d7825`, P1 to P7), with the arbiter
`regexpref@1` at Broiler.JS `b2c2e224` and its cut-off of 1,000,000 Matcher invocations, by `exclusion.py select`:
- **Limb (1) alone**, the drafted option P's property, holds for no pattern of P1, P2, P3, P5, P6 or P7, and for **278
  of P4's 65,536 patterns** (2,326 pattern-flag pairs): the 19 and 259 others. "At least 2^32" and "at least the
  arbiter's cut-off" select the same patterns, since every braced minimum over such an atom in P4 is 4,294,967,296. Of
  the 259, 202 are refused by the arbiter on every exec input under every flag set where limb (1) holds; of the other
  57, 11 have limb (2) (below), and on the 46 others' exec inputs the arbiter reaches the shortest prefix, because the
  quantified body fails where it is tried, as P4-38461's `(?=b\W){4294967296}?` does at the prefixes of 2 and 11 units
  the reduction of 2026-11-15 reached.
- **Limbs (1) and (2)**, a property of the pattern and the arbiter alone, asked of the 37,216 exec inputs of the 278
  under the flag sets where limb (1) holds: **30 patterns**, the 19 and 11 more (P4-02713, P4-03054, P4-13955,
  P4-21879, P4-26857, P4-27341, P4-32242, P4-41818, P4-48952, P4-55682, P4-55791). On the 11's unreachable inputs every
  side stops (S at a ceiling, V at its ceiling, J by the harness: class (b) under row B6 and decision D10) or all
  answer alike (P4-48952's 3 inputs and 14 of P4-03054's, null), so none of their differences is class (c), and the
  ruling does not name them.
- **Limbs (1), (2) and (3)**: **exactly the 19**, under the 93 pattern-flag pairs the table below lists.
- So **no property found of the pattern and the arbiter alone selects exactly the 19**: the narrowest, limbs (1) and
  (2), selects 30. Limb (3) is not a property of the pattern: it reads task 2.13's comparison, as the ruling did when
  it named the 19 by what their inputs do to class (c), so against that run the three limbs select the 19 ids and
  nothing else, and against any other run they state nothing the ids do not. **The exclusion is therefore the
  enumeration by id**, as the rule block says, and the limbs are how the 19 were found; that limb (3) reads the run is
  part of why this revision is weaker evidence. The 11 are not excluded. Limbs (1) and (2), the narrowest property
  found that holds all 19, are offered to the owner as the exclusion's statement instead: taking them would exclude the
  11 too and move no class (c) difference.

## The note regarding the 19 patterns (the owner's)

- **What they are.** 19 of P4's 65,536 drawn patterns, each run, as every P4 pattern is, under the ten flag sets, by
  acceptance and against 16 subjects: 3,230 inputs (190 acceptance cases, 3,040 execs). Each quantifies with the
  minimum 4,294,967,296 an atom that can match the empty string:

  | Id | Pattern (its source text) | The quantifier | Flag sets where limbs (1) to (3) hold |
  |---|---|---|---|
  | P4-01347 | `((?<!$-{1,})){4294967296}⟨U+03A3⟩{0,2}\|\u{10400}\x6b*k` | `((?<!$-{1,})){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-01872 | `⟨U+0130⟩\|(K?⟨U+D83D⟩+]\|(?!\q\|\d){4294967296}⟨U+D83D⟩(?<=(?=⟨U+0131⟩\k+)))*` | `(?!\q\|\d){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-02466 | `(?<\u{62}>){4294967296}(?<!(?<![^\w&&[^]&&\d]\|^))B?` | `(?<\u{62}>){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-09242 | `(?:\b){4294967296}S\b[\sK\n]` | `(?:\b){4294967296}` | `y` |
  | P4-11265 | `(?<\u{62}>(?<!\B\b)){4294967296}\|(?=\b\|)+?1\|\0\B` | `(?<\u{62}>(?<!\B\b)){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-11714 | `(?:^){4294967296}?(?<\u0061>-){2}[^[]{4294967296}` | `(?:^){4294967296}?` | "", `d`, `i`, `m`, `s` |
  | P4-14530 | `[⟨U+03C3⟩-\S⟨U+212A⟩]{0,2}?(?!⟨U+0131⟩{0,2}⟨U+1E9E⟩){4294967296}K` | `(?!⟨U+0131⟩{0,2}⟨U+1E9E⟩){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-17689 | `(?:){4294967296}\P{L}{4294967296}` | `(?:){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-18962 | `\s(?!⟨U+DE00⟩⟨U+DE00⟩{0,2}?)\|,\b\|()(?<\u0061>){4294967296}$` | `(?<\u0061>){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-29456 | `(?<⟨U+1D400⟩>\n$)\|(?:0?⟨U+000A⟩??){4294967296}($\b){2}\|.+s` | `(?:0?⟨U+000A⟩??){4294967296}` | `y` |
  | P4-29657 | `1[^]\|(?!\B(?:\|)){4294967296}⟨U+D83D⟩` | `(?!\B(?:\|)){4294967296}` | `y` |
  | P4-30238 | `(?:(?<=^)){4294967296}?a{1,}\|\_` | `(?:(?<=^)){4294967296}?` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-33179 | `(?=,⟨U+00E9⟩\|((\S{0,2}\S*){4294967296}?){2}[[⟨U+00B5⟩]_]⟨U+1E9E⟩{0,2})` | `(\S{0,2}\S*){4294967296}?` | `y` |
  | P4-40469 | `($){4294967296}\uD83D\uDE00+.` | `($){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-42576 | `(?=){4294967296}\|(?!⟨U+03C3⟩{0,2}(?<⟨U+03C0⟩>\Ba)\cJ+)B⟨U+00A0⟩[]*?\|⟨U+03A3⟩\d⟨U+03A3⟩` | `(?=){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-54314 | `.??(?-i:$){4294967296}` | `(?-i:$){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-58688 | `[--⟨U+00A0⟩\S]\|$S{\B\|(?m:[s\D]{0,2}^){4294967296}` | `(?m:[s\D]{0,2}^){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-63268 | `(?=(?=)){4294967296}(\|$\b.i)` | `(?=(?=)){4294967296}` | "", `d`, `i`, `m`, `s`, `y` |
  | P4-65213 | `((?!$(?!_*⟨U+D83D⟩\uD83D\uDE00⟨U+00DF⟩){4294967296}?\uD83D\uDE00)⟨U+D83D⟩\|,\2\|K\p{ID_Start}(?<a>(\r)))\s\|⟨U+2028⟩` | `(?!_*⟨U+D83D⟩\uD83D\uDE00⟨U+00DF⟩){4294967296}?` | "", `d`, `i`, `m`, `s`, `y` |

  Each pattern's characters are written as they are, ASCII printable ones as themselves and every other code point
  or lone surrogate as ⟨U+XXXX⟩; `|` is written `\|` in the table. The last column is the flag sets where the three
  limbs hold (`p4-excluded.tsv`'s fourth field), not every flag set with an input no prefix reaches. **The arbiter
  reaches no prefix of 2,077 of the 3,040 execs**, not even the shortest, under each of the ten flag sets (272 under
  each of "", `d`, `i`, `m` and `s`, 237 under `y`, 112 under each of `u` and `iu`, 128 under each of `v` and `iv`):
  - **1,269 of them, all without `u` or `v`, carry the 3,789 differences of arm J that the classifier `@5` left in
    class (c)** (1,263 under the invariant culture, 1,268 under `tr-TR`, 1,258 under `lt-LT`). They are the 19
    patterns' inputs on which `reduce.py`, which takes every class (c) difference the arbiter cannot reach, answered
    `none`: it was asked of 1,406 of their inputs and reached a prefix of 137;
  - **on the other 808** (480 of them under `u`, `v`, `iu` or `iv`) there is no class (c) difference: on 656 every
    side stops (S at a ceiling, V at its ceiling, J by its harness in each culture: class (b) under row B6 and decision
    D10), and on 152 no arm differs from S.

  On the other 963 execs the arbiter answers at the shortest prefix: it refuses the pattern (672, all under `u`, `v`,
  `iu` or `iv`) or answers null (291).
- **Why the arbiter cannot decide the inputs no prefix reaches.** The arbiter, `regexpref.py`'s RepeatMatcher,
  follows 22.2.2.3.1: it runs a quantified body its minimum number of times before the check that stops an empty
  iteration applies. Where the body matches the empty string at the position where it is tried, that is 4,294,967,296
  runs of the body at one position, past the cut-off of 1,000,000 Matcher invocations, however short the subject: the
  cost is in the pattern. So the declared reduction, which cuts only the subject, reached no prefix of those 1,269
  inputs, not even the empty one (the shortest, the subject cut to the lastIndex under `y`), and the 808 others are
  the same at their shortest prefix. Where the body fails where it is tried, or the arbiter refuses
  the pattern, the arbiter answers: P4-09242's `(?:\b){4294967296}` is out of its reach under `y` alone.
- **The answers are kept and reported.** S's answers and each arm's (V's and J's in its three cultures) on every input
  of the 19 patterns are retained in the runs' files and the comparison as before, and the classification lists each
  difference of theirs apart, `excluded`, with both answers. They are outside the judged population, and are reported
  with every reading.
- **A known gap, not a verified agreement.** Nothing on the 2,077 execs no prefix reaches is checked against the
  specification: neither S's stop nor any arm's answer is shown right or wrong by the arbiter. The exclusion is a gap
  of the reference arms that the bundle names, not an agreement of S with either engine. On the 19 patterns' other
  inputs the arbiter does answer, and the exclusion, which takes every flag set and input of the 19, also takes out of
  the judged population **663 class (a) corrections the arbiter confirmed** (arm J accepting under `v` or `iv` what S
  and the arbiter refuse: 561 under row S12 and 102 under row S8) **and 411 class (b') differences** settled at a
  prefix the arbiter reaches. They stay listed apart with their answers, but they no longer count as verified
  corrections or admitted differences.
- **Where S, V and J differ on them, and how.** On the 1,269 inputs no prefix reaches that carry the class (c)
  differences, the record of 2026-11-15: "S
  stops at its step, trail or frame ceiling (2,393, 1,381 and 15), and J answers null (3,270) or a match (519)", counted
  over the 3,789 differences: **S against J, on every one**. Arm J does not stop: it answers, null or a match, where S
  stops at a bound it declares; on 18 more input-culture pairs J's harness stopped too, and both stops are class (b)
  (decision D10), not among the 3,789. That record does not give **arm V's answers**; the retained runs do: V stops on
  all 1,269, at its ceiling on 1,104 (S's answer is another stop, the difference class (b) under row B6 by the
  classifier `@5`) and at its allowance on 165, where its answer, `limit:steps`, is S's and there is no difference.
  So S and V both stop on every one, and only J answers. The 19 patterns' differences elsewhere are on 1,070 inputs,
  and the classifier `@5` classes their 4,059 differences, none class (c):
  - on 656 of the other 808 execs no prefix reaches, every side stops: arm J's (b) under decision D10 1,968 and arm
    V's (b) under row B6 656;
  - on 401 of the 963 execs the arbiter answers at the shortest prefix, and on 13 acceptance cases: arm J's (b) under
    decision D10 168 (both stop), (b') 411 (S stops, J answers, and at a prefix the arbiter reaches the two agree), (a)
    under row S12 561 and under row S8 102 (J accepts under `v` or `iv` what S and the grammar refuse; 39 of them by
    acceptance); and arm V's (b) under row B6 193.

  With the 1,104 of V and the 18 of J on the 1,269, **8,970 differences are excluded, 3,789 of them class (c)**.

## The alternatives, each the owner's

1. **The 1,269 inputs alone**, not the 19 patterns: the same effect on class (c), since no other difference of the
   patterns is class (c), and their 4,059 other differences (on 1,070 inputs, above) would stay in their classes. Not
   taken: the question put and the ruling name the patterns, and P4's unit is the pattern drawn.
2. **Limb (1) alone** (the drafted option P): 278 patterns, 259 the ruling does not name. Not taken: it excludes
   patterns whose inputs the arbiter decides.
3. **Limbs (1) and (2) alone**: 30 patterns, 11 the ruling does not name, none of whose differences is class (c). The
   narrowest property found of the pattern and the arbiter that holds all 19, and so offered to the owner. Not taken
   here: it excludes patterns the ruling does not name, whose differences the rule already admits.
4. **Limbs (1), (2) and (3) as the definition**: the same 19 against task 2.13's run. Not taken: limb (3) reads that
   run's comparison, so the limbs state nothing a later population could be read by that the ids do not. The rule
   block takes the enumeration by id, and gives the limbs as how the 19 were found.

## Corrected before merge

On 2026-11-16, after review and before this file reached the default branch, the passages below were replaced by the
text above (the dated note under "Written"). The review found that the note counted as "the inputs no prefix reaches"
only the 1,269 that carry class (c) differences, where the arbiter reaches no prefix of 2,077 execs of the 19; that the
known gap was stated of every input of the 19, where the arbiter answers on 963 of their execs and the exclusion also
takes 663 class (a) and 411 class (b') differences; that limb (3), which reads the run, was presented as part of a
property, where with it the limbs select nothing but the ids; and that `exclusion.py` reads a crash as a stop, which
limb (3) did not say. The ids, `p4-excluded.tsv` and the classification are unchanged. Each passage as it was:

- "The owner's decision", its second paragraph's end:

  > the ruling names the 19 patterns, and this file states the property that selects them and lists them. What this
  > file adds to the ruling is its wording: the property, the extent of an excluded pattern (every flag set and
  > input of it), how its inputs are listed, and the note.

- The rule block's population line and its limbs:

  > ```
  >   P4 as reference-arms.md's line, excluding by the owner's ruling of 2026-11-16 a pattern drawn that has, under one of its flag sets, the three limbs below; an excluded pattern is excluded under every flag set, by acceptance and by every exec
  >     (1) the pattern's limb: read under the flags (u or v: 22.2.1's grammar; otherwise Annex B's), it quantifies with a braced minimum of at least 2^32 (4,294,967,296) an atom that can match the empty string (an assertion, a look-around, a back-reference, an empty group, a group one of whose alternatives can, or under v a class that holds the empty string)
  >     (2) the arbiter's limb: on one of the pattern's exec inputs in P4, the arbiter at its cut-off reaches no prefix of the subject, not even the shortest (scripts/reference-arms/reduce.py's outcome none: the subject cut to the lastIndex under g or y, to the empty string otherwise), asked of every exec input of the pattern under the flag sets where limb (1) holds
  >     (3) the comparison's limb: on one such input the comparison of task 2.13's run holds a difference where one side stops at a limit and the other answers, null or a match: a stop against an answer the arbiter cannot check, which class (c) holds
  >   the excluded patterns are 19, listed by id in scripts/reference-arms/p4-excluded.tsv: P4-01347, P4-01872, P4-02466, P4-09242, P4-11265, P4-11714, P4-14530, P4-17689, P4-18962, P4-29456, P4-29657, P4-30238, P4-33179, P4-40469, P4-42576, P4-54314, P4-58688, P4-63268, P4-65213
  > ```

- "How it is read":

  > The listing is written by `scripts/reference-arms/exclusion.py select`, which applies limb (1) to every pattern
  > of the population, limb (2) to every exec input of a pattern under the flag sets where limb (1) holds, and limb
  > (3) to the comparison's differences on the inputs where both hold.

- The section's heading:

  > ## How the property was chosen, and what it selects

- Limb (1)'s count:

  > Of the 259, 202 are refused under every flag set; on the others' inputs the arbiter reaches a prefix,

- The last item of that section:

  > So **no property found of the pattern and the arbiter alone selects exactly the 19**: the narrowest, limbs (1)
  > and (2), selects 30. Limb (3) reads the run, as the ruling did when it named the 19 by what their inputs do to
  > class (c), and is part of why this revision is weaker evidence. The 11 are not excluded; the owner may extend
  > the ruling to them, which would move no class (c) difference.

- The table's heading:

  > | Id | Pattern (its source text) | The quantifier | Flag sets of the inputs no prefix reaches |

- The paragraph after the table:

  > The inputs no prefix reaches, under the flag sets listed, are **1,269, all execs without `u` or `v`, where task
  > 2.13's comparison holds 3,789 differences of arm J that the classifier `@5` left in class (c)** (1,263 under the
  > invariant culture, 1,268 under `tr-TR`, 1,258 under `lt-LT`).

- "Why the arbiter cannot decide them":

  > **Why the arbiter cannot decide them.** The arbiter, `regexpref.py`'s RepeatMatcher, follows 22.2.2.3.1: it runs
  > a quantified body its minimum number of times before the check that stops an empty iteration applies. Where the
  > body matches the empty string, that is 4,294,967,296 runs of the body at one position, past the cut-off of
  > 1,000,000 Matcher invocations, whatever the subject: the cost is in the pattern. So the declared reduction,
  > which cuts only the subject, reached no prefix of these inputs, not even the empty one (the shortest, the
  > subject cut to the lastIndex under `y`).

- "A known gap, not a verified agreement":

  > Nothing on these inputs is checked against the specification: neither S's stop nor any arm's answer is shown
  > right or wrong by the arbiter. The exclusion is a gap of the reference arms that the bundle names, not an
  > agreement of S with either engine.

- "Where S, V and J differ on them, and how": its opening:

  > **Where S, V and J differ on them, and how** (the 1,269 inputs no prefix reaches). The record of 2026-11-15:

- Its end:

  > On the 19 patterns' other inputs (with a difference: 1,070) the classifier `@5` classes 4,059 differences, none
  > class (c): arm J's (b) under decision D10 2,136 (both stop), (b') 411 (S stops, J answers, and at a prefix the
  > arbiter reaches the two agree), (a) under row S12 561 and under row S8 102 (J accepts under `v` or `iv` what S
  > and the grammar refuse); and arm V's (b) under row B6 849. With the 1,104 of V and the 18 of J on the inputs no
  > prefix reaches, **8,970 differences are excluded, 3,789 of them class (c)**.

- Alternative 1's reason:

  > since none of the patterns' other differences is class (c), and 4,059 differences of their other inputs would
  > stay in their classes.

- Alternative 3's reason:

  > Not taken: it excludes patterns the ruling does not name, whose differences the rule already admits.

- Alternative 4:

  > 4. **The ids alone**, with no property: the same 19. Not taken as the definition, because it states nothing the
  > next population could be read by; the ids are listed beside the limbs, which are stated and checked.

## The rule it revises, quoted whole

`reference-arms.md` at its blob `724943b3ad8394544b29d28ef6a11869e644a8b3` on Broiler.Regex's `main` at `f1816df9`
(the blob it had at `b6580b8c`), each line prefixed with "> ":

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
