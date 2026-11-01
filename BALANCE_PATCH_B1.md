# B1 balance patch — 2026-11-01

This changelog describes the rules-document patch, not a verified Unity gameplay build.

## Follow-up revision (2026-11-01)
Investigation cap revised from 4 to **3 per investigator per theme**, after reviewing the exploratory simulation. Other B1 abilities and scoring remain unchanged. This is a rule-document update, not a Unity runtime validation.

## Confirmed changes
- Investigators: 3 count-only investigations, 3 suspicions, 1 cooperation and 2 formal deductions per theme. Formal deductions start at R6.
- Scoring: single correct investigator 2 points; two or more correct in the same round 1 point each; culprit 0/1/2 points for first solution in R6–8 / R9–12 / no solution by R12. Three theme scores accumulate.
- Abilities: investigator Sprint and Tracking 3 uses, Analysis 2; Interrogation returns separate YES/NO for location and weapon; Return selects a visited room within two previous turns; Detour crosses up to two connected edges for 1P plus any extra costs; ReSuspicion counts toward the 3-use cap.
- Culprit: Lock affects up to two edges per use, Disrupt delays one true public investigation count to next round, RouteBlock has 2 uses, Smoke remains 1 use. Only one culprit power may activate each round, and none may prevent R12 final deductions.
- Initial setup uses 3 movement points before R1; R1 itself starts with 2 points. New theme resets secret information and ability counts but preserves cumulative scores.
- Existing timers and role counts are preserved.

## Not yet determined
## Follow-up rule LOCK (2026-11-02)
- Each investigator may make only one formal deduction per round and no more than two per theme. After a failed first deduction in R6, the second is allowed in R7, but never in the same round. No extra round-skip cooldown.
- Two failed formal deductions eliminate the investigator. An R11 first attempt and an R12 second attempt are legal, and the R12 mandatory deduction observes the one-per-round restriction. This is the same for humans and AI.
- This is a rule-document correction; Unity runtime and Play Mode have not been verified against it.

## Not yet determined
- Ties after three themes.
- Which players receive the culprit role in each theme.
- Final nine weapon names for each theme.

## Verification limits
The repository's existing Unity GameManager still contains the old single-win and investigation logic. The existing play simulator also models prior rules. No claim of updated in-game behavior or measured win-rate balance is made until code integration and Unity Play Mode tests occur.

Source files: ABILITIES.md, GAME_RULES.md, DESIGN_BASELINE.md, EVENTS.md, AI.md, simulation/README.md.
