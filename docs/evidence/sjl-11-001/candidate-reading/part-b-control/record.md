# Part B's control (bundle sjl-11-001), taken 2026-11-18 under the owner's permission of 2026-11-18 ("Allow scratch branch")

- Scratch branch `sjl-11-2-control-b` of Broiler-Platform/Broiler.Regex, from `49d7be1f3ef148f9c262f0e52b8c360c07fd838d`; never merged, no PR; deleted after both runs.
- Plant `deac674863f3bf859c8e6564634b0df499c62f14`: `RegexParser.Parse`'s undeclared-name check gains `!_unicode &&` (accepts \k<name> to an undeclared name under u).
  - CI run 44237117988 (workflow_dispatch, ci.yml): **failure**, 9 of 14 jobs red.
  - B-1 host job 134575819645: red, test262-host verdict red on exactly the five `test/language/literals/regexp/named-groups/invalid-dangling-groupname{,-2,-3,-4,-without-group}-u.js` (negative test expected SyntaxError).
  - B-2 library job linux-x64 134575819815 and win-x64 134575819759: red, `FAIL literals` on the same five files ("no literal refused").
  - Also red from the same plant: ubuntu-24.04 / Release 134575819708 and windows-latest / Release 134575819702 (19 unit tests, e.g. Test262DuplicateNamedGroupsTests, AnnexBGrammarTests), Native AOT probe linux-x64 134575819845 and win-x64 134575819322 (adversarial rows O18, O19 differ), Adversarial corpus part C's controls 134575819690 (O18, O19 unexplained), Assurance check (SL8) 134575819693 (RegexParser.cs fingerprint drift).
  - Green: both architecture-test jobs, the three fuzz jobs.
- Revert `d0dd222d3f1a4f9a7dd5ff4449b0803cbe94a5cb` (tree equal to `49d7be1f`'s): CI run 44238736222: **success**, 14 of 14 jobs green (host job 134581114325, library jobs 134581114075 linux-x64 and 134581114076 win-x64).
- Job logs of run 44237117988's failed jobs kept in run1/ (scratch).
- Branch deletion refused by the session git remote (push of a delete: "unexpected disconnect"; ls-remote still shows d0dd222). The branch stays on GitHub at d0dd222 (tree = 49d7be1f) until the owner deletes it; it is not merged and has no PR.
