# Art integration checkpoint

## Implemented
- Existing game rules, clue profiles and mansion graph are unchanged.
- `Assets/_Game/Resources/PresentationCatalog.asset` has six character prefab/portrait slots and nine mansion prefab/background slots. Empty prefab slots keep the existing graybox/capsule fallback.
- Character slot order: NAZ, JIRO, MARIHI, MATTO, BOKI, MUNIRE (GameDefinitions order). Room slot order: MansionLayout IDs 1–9.
- Character prefabs use meter scale and a foot-level root. Runtime labels remain on the root; do not add role-specific appearance.
- Room prefab roots use local origin at the room center, Unity Y-up, scale one. Runtime installation preserves the existing floor selection collider and graph.
- Optional RoomPresentation holds six foot-level StandPoints and an ActionPoint. Existing occupants keep their stand points when somebody leaves. New occupants choose the nearest free point.
- `Deduction Game/Create Presentation Slots` creates the LOCK character folder structure and selects the catalog without overwriting an existing catalog.
- `Deduction Game/Add Selected Room Anchors` adds editable anchors to a selected RoomNodeView only when none exist; it does not regenerate the scene.

## Asset handoff
Use ART_GUIDE.md for all character/model/animation naming and budgets. Assign finished prefabs in the catalog before Play. Room visuals must not add gameplay logic or input-intercepting colliders. Portrait/background slots are reserved for the future room UI; they are not yet rendered.

## Remaining production order
1. Mansion environment and character assets following the selected reference designs.
2. DoorID/Entry anchors, NavMesh travel, front-wall cutaway and event visuals, validated on the mansion before reusing them on the cruise/museum.
3. Room-action 2D UI using catalog portraits/backgrounds; replace debug OnGUI after the complete UI flow is connected.
4. Full single-player verification on Windows/Android before room-code multiplayer.

## Existing document conflicts (not changed)
- MAPS.md contains older room/connection descriptions; ART_GUIDE.md has the newer explicit mansion LOCK already used by MapGraph/MansionLayout.
- GameDefinitions.Profiles has older clue descriptions inconsistent with selected art (for example Munire short hair). Changing the matrix affects witness clues and is deferred until a consistent, approved matrix preserves the exactly-two-characters-per-feature requirement.
- ARCHITECTURE.md describes R1 3P; the implemented and locked flow is pre-round setup 3P, then R1 onward 2P.

## Validation
All runtime and editor C# sources compile together against the installed Unity 6000.3.26f1 reference assemblies with UNITY_EDITOR enabled (zero errors/warnings). Unity batch startup exited with code 1 during licensing/WMI initialization before script compilation; Play mode, visual layout and imported assets remain unverified.
