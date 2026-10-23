# Single-player usability pass 1

Changed only presentation/query paths; multiplayer, balance, card data, map topology, AI decisions and ability budgets are unchanged. GameManager's existing movement cost body is shared by execution and non-mutating preview. Read-only action availability uses the existing phase/role/room/remaining-use guards and adds explanatory text; it never spends resources.

## User-facing changes
- Pinned initial-placement vs normal-turn instructions, current turn/P/info-action status, own deduction budget, pre-R6 and R12 requirements. Pinned end-turn/deduction buttons avoid scrolling to finish a turn.
- Cyan current-room, green affordable connected-room and yellow selected-room outlines with a text legend. Room click and Canvas move use the same feedback, including disconnected/current room, P shortage and consumed one-attempt route block. Preview queries do not consume route blocks or ability uses.
- Per-action enabled states and short reasons; selected target/ability room and complete culprit/location/weapon tuple remain visible. Existing target/weapon/location selection buttons are reused. Culprit's own power counts and fake-deduction budget are shown accurately.
- Completed AI-turn count is reported without artificial delays or changing AI scheduling. The existing synchronous AI loop remains synchronous.
- Session public log is retained by an event subscription so bursts of AI actions do not erase old entries. Important public announcements remain summarized; private information is separately labeled with an unread count and acknowledgment. This does not reveal other players' cards or private notes.
- Existing prototype card strings are unchanged. Theme room aliases are displayed beside card-location choices/current location so Cruise/Museum room labels can be understood alongside legacy logs/cards.
- All action-camera overrides still expire automatically; successful movement cancels an old action focus and Restart restores the default 19-unit zoom/tracking state. Free rotation and the three zoom constants are unchanged.
- Basic label backgrounds improve graybox contrast; room/token labels stay outside the Canvas panels and are hidden on GameOver. No final fonts/icons/art were produced.
- Cutaway now restores/disables click colliders along with hidden walls/floors, preventing invisible upper geometry from intercepting room clicks. Already-baked NavMesh remains unchanged.
- Existing OnGUI remains available through F10, hidden by default. Restart resets UI selections, log summaries, progress and camera state.

## Verification
Final RunSinglePlayerE2E.ps1 run: all six processes exited 0 in a fresh project copy. The three E2E suites still pass 24 seeded games each (72 total; 7450 assertions); Mansion/Cruise/Museum have 22/22/23 R12 games respectively and normal earlier wins in the remainder. Existing map/event/CharacterActor/action-camera/UI suites all pass.

UsabilityVerification additionally checks setup guidance, disabled actions/reasons, actual outline colors, connection/P/route-block failure feedback, information-action exhaustion, R6 availability, R12 final requirement and priority, deduction preview, event summary clear, spectator advancement, real GameOver/Restart buttons, camera reset and hidden collider visibility. It completes one additional full game per theme using only Canvas button callbacks after setup (no round/answer/role modification during those runs).

Rendered Play Mode captures of setup/R12/result were inspected for all three themes at a 1920x1080-style Game View. Label overlap with HUD/result was found and patched. Test capture fixtures refresh after restoring their temporary spectator state. Final runtime rendering and automated checks are distinct from a long manual user session.

Remaining real-play checks: prolonged mouse/keyboard sessions, subjective zoom/readability preference, smaller desktop displays and Android touch/device performance. These are not replaced by deterministic button-callback tests. No final UI design or art polish is claimed.
