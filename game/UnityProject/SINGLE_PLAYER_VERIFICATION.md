# Single-player full-loop verification

Run Tests/RunSinglePlayerE2E.ps1 -UnityPath <Unity.exe>. The script copies Assets/Packages/ProjectSettings to a fresh temporary project, preserves logs and enforces a 120-second process timeout per suite. It never resets/deletes the source checkout. Six suites cover all three map themes and their existing presentation regression tests.

SinglePlayerE2E is editor-only. Reflection sets System.Random seeds and focused test fixtures; no runtime seed switch or modified random policy exists. FullGames uses seeds 0–23, actual setup/movement/action/end-turn methods and the unchanged five-AI execution path. It does not set round, phase, answer or roles while progressing full games. The test driver uses an oracle only to choose legal paths toward key/chest for coverage; production players/AI do not receive that information. Human final incorrect guesses use their own card, a legitimate known non-answer. Focused fixtures separately cover edge conditions and exact answers; those are not counted as complete games.

Results on Unity 6000.3.26f1:
- Mansion: 24/24 normal GameOver; 22 reached R12; 2461 assertions.
- Cruise: 24/24 normal GameOver; 22 reached R12; 2500 assertions.
- Museum: 24/24 normal GameOver; 23 reached R12; 2489 assertions.
- Total: 72 complete games, 67 at R12, 7450 assertions, no turn deadlock.
- Per-theme coverage: initial six-player placement 3P, normal turns 2P/disruption 1P, AI participation, witness stages, forensics arrival, legal key/chest navigation, three random extra-event types, restart state.
- Focused coverage: ten investigator abilities, four culprit powers/counts/expiry, affordability, failed-action non-consumption, private info-action usage, R6/once-per-round/two-attempt deduction limits, R12 forced deduction, fake deduction, correct-answer immediate winner, spectator continuation, event activation/restoration and key/chest timing.
- Existing map/agent/CharacterActor/Canvas UI/action camera/event suites pass on all themes. The independent Unity SearchDatabase startup exception can appear; presentation tests explicitly unpause the editor.

Fixed existing defects without changing LOCK rules:
- Public action entry points now reject setup, GameOver and inactive/spectating turns; Analysis also respects room action restrictions.
- R12 final deduction takes precedence over blackout/smoke; normal AI deductions respect room restrictions.
- An unaffordable move does not consume an armed Gatekeeper/Tracking/Detour use.
- Invalid smoke overlapping blackout leaves existing smoke state and remaining uses intact.
- Return retains the previous turn's origin rather than replacing it with the current origin.
- ReSuspicion prevents immediate same-turn voluntary deduction; R12 forced priority remains intact.
- Restart resets selections, actor seats/navigation and transient presentation as well as game/event/player state.

The previous actor regression's artificial movement destination could overlap a newly restored fixed StandPoint after Restart. It now targets an unoccupied room position; both original separation assertions and actual travel/facing/arrival assertions remain enabled. No failing check was removed or relaxed.

This verifies the existing single-player skeleton and the listed flows, not every future art/device/timing scenario. Final art and new gameplay features were not added; card data, map topology, ability budgets, win conditions and ordinary randomness remain unchanged.
