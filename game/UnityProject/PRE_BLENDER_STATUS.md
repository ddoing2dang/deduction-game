# PRE-BLENDER CHECKPOINT

Status: gameplay/core graybox complete enough to begin art production.

Implemented:
- Windows 64-bit project bootstrap
- 6 fixed players: 1 human + 5 AI
- pre-R1 3P setup placement
- R1-R12 normal turns with 2P
- 9-room connected graybox map
- PC camera controls
- case answer creation: culprit + location + weapon
- card dealing and undealt cards
- public suspicion declaration/result
- private revealed-card knowledge
- cooperation private exchange
- witness clues R3/R6/R9/R12
- forensics arrival R7 and lobby check action
- key -> secret chest flow
- exactly one extra event: blackout / broken door / secret passage
- investigator abilities
- culprit powers
- deduction from R6, max two attempts
- R12 forced investigator deduction
- culprit fake deduction requirement
- AI movement, information actions and deduction
- game over / restart
- runtime player tokens
- Windows quality defaults

Art boundary:
- Replace graybox rooms with final modeled environments
- Replace capsule tokens with final character assets
- Replace debug OnGUI with final art UI
- Lighting/material/post FX pass after environment assets exist
