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
