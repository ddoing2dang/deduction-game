# Balance B1 simulator status (2026-11-01)

**Legacy simulator — not valid for the current B1 rules.** The existing play_simulator.py models an earlier investigation mechanic and immediately ends on the first correct accusation. It cannot establish current win rates.

Current B1 implementation targets:
- Nine rooms and nine weapons per theme, six players and unchanged card dealing.
- Four private three-field investigation queries per investigator; public match count only.
- R6+ final guesses, R12 forced guess, two attempts per investigator.
- End-of-round shared scoring: one successful investigator receives 2 points, multiple successful investigators receive 1 each.
- Culprit survival score: 0 points if solved R6-R8, 1 point if solved R9-R12, 2 points if unsolved after R12.
- Three themes, separate session state for each; only cumulative scores persist.
- Ten investigator abilities and four culprit powers follow ABILITIES.md B1.
- Delayed result is always truthful; no unauthorized access to private submitted guesses.
- Play-mode and runnable simulations must be completed before reporting gameplay balance as validated.

The Unity runtime is not updated by this documentation-only change. Tie-breakers and role assignment across themes remain unspecified.

---

# Simulation inference engine

This folder contains the executable inference layer used for balance simulation. It is simulation code, not a change to final game rules.

Implemented knowledge constraints:
- 6 players, each exactly 1 location + 1 weapon.
- 1 answer location/weapon and 2 undealt location/weapon cards.
- Own hand and privately shown evidence.
- Public first-provider ordering: players checked before the provider are known to hold neither queried card.
- Provider owns at least one of queried location/weapon.
- No-provider investigations.
- Observation category result.
- Interrogation YES/NO.
- Secret chest undealt-card knowledge.
- Exact chained deduction through finite-domain constraint solving.

Character/culprit inference is intentionally separate because character cards are not distributed.

Run: `python simulation/inference_engine.py`
Expected sanity-test output begins with `PASS`.


## Play simulator
`play_simulator.py` connects the exact card inference engine to a six-seat, twelve-round executable match loop: deal, investigations, first-provider evidence, private shown cards, inference updates, chest knowledge, R6+ deductions, two-attempt elimination, and R12 forced deduction.

Important modeling boundary: culprit-character inference is deliberately not faked. Character cards do not exist, so until behavioral/ability evidence is modeled, character guesses remain uncertain. Movement, map topology, investigator abilities, culprit abilities, and the three additional events are not yet claimed as simulated by this runner.

Run from the simulation directory:
`python play_simulator.py -n 1000`
