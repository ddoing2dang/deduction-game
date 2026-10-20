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
