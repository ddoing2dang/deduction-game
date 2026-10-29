# Visual Gold entrance repair and luxury refinement

## Scope and actual diagnosis

The bridge in front of the decorative entrance is the active LOCK `DOOR_04_05` dining-to-kitchen route. It is not a leftover and was not deleted. Its passage floor, endpoints, DoorID and original Unity collision meshes are preserved. Eighteen tall wooden railing render components were reshaped into stone edging at <=0.08 m. Original collision boundaries remain conservative and do not expose a new route into the nonplayable exterior.

The exterior ground previously subtracted the projected footprints of basement floors/passages as permanent holes. The replacement ground skin subtracts ground-level gameplay floors/passages and actual stair footprints instead. Existing dynamic ground cutaway remains responsible for revealing basement gameplay. The ground has no Collider and cannot become a new NavMesh route. A 100-point vertical sample across the entrance forecourt finds no missing ground; the existing 4-to-5 bridge floor supplies the surface at its own location.

## Art changes

- Continuous ivory/verde marble forecourt and an entrance runner, with the existing driveway position retained.
- Fine walnut grain for the entrance inset panels; brass laurel/lozenge crest beneath the cornice, existing brass pulls and paired inside/outside sconces retained.
- Two compact CC0 planted urns at the actual exterior landing margins.
- Central hall west-wall carved console, bust and oil lamp, with an oval gilt mirror; all placed outside protected navigation clearances.
- Existing hall staircase shape and location retained, with dark polished walnut, thin stone treads and brass nosings. Gallery coffer outlines and rug braid improve the architectural finish without adding columns or barriers.
- Library and lounge receive gilt landscape frames, pleated dark velvet drapes and compact chandeliers. Dining receives a framed landscape and its sconce is shifted along the same solid wall to avoid overlap.
- Forest/burgundy rugs with restrained borders in the library, dining room, conservatory and servants' room; existing hall and lounge rugs retain their footprints and receive narrow braid.
- Upper wall panel outlines complete the kitchen/conservatory/service-room finishes. Boiler room/cellar fixtures and access routes are preserved.
- Room fill becomes warm neutral `(1, .90, .78)` rather than a strong orange cast. No character, game-rule, AI, UI or multiplayer assets/code are edited.

All assets reuse the existing CC0 pack and authored materials. CC0 placement count is 60, using the same 21 model types. Total placed environment geometry is 550,557 tris / 809 render parts / 56 materials / 117 game textures, all <=2048. No device FPS benchmark is claimed.

## Files and images

- Authoring source: `Mansion_VisualGold.blend`, backed up with the previous FBX, scene, placements and manifest in ignored `Backup/LuxuryRefinement/`.
- Unity FBX: `game/UnityProject/Assets/_Game/Art/MansionVisualGold/Mansion_VisualGold.fbx`.
- Blender EEVEE: `Review/Luxury/Exterior_Front.png`, `Estate.png`, `Entrance_Close.png`, `Entrance_Interior.png`, `Hall_HalfTop.png`, `Hall_Side.png`, `Room_01.png` through `Room_09.png`.
- Unity actual Play camera: `Verification/FrontDoor_Exterior.png`, `FrontDoor_Interior.png`, `FrontDoor_Forecourt.png`, nine-room 9/19/32 zoom frames, side/action/event views and bidirectional service-stair frames.

## Validation evidence

- `luxury-ground-validation.json`: 100 forecourt surface samples, zero gaps, 18 visual rails <=0.08 m; bridge retained.
- `blender-final-audit.json`: unchanged 94 reference transforms, zero protected-placement conflicts and furniture intersections, Pass02 source preserved.
- `luxury-character-preservation.json`: SHA256 of 179 existing game character files unchanged.
- `luxury-preservation.json`: exact layout/DoorID/room-floor protection and unchanged baseline Unity collider assets.
- `unity-install-results.txt`, `unity-bounds-errors.txt`, `Verification/runtime-spatial-errors.txt`: 85 protected Unity points and imported bounds checks.
- Fresh `Verification/play-results.txt`: 22 actual actor traversals, secret passage both ways, 54 StandPoints, 9 ActionPoints, three zooms, cutaway, action camera return and 2D handoff, public events, Restart and existing single-player assertions. Door/ornament interaction and NavMesh-link absence is checked explicitly.
- `luxury-final-verification.json` records the final result and exact saved-file/report hashes after direct image review.

The audit's atomic report replacement retries a brief Windows read lock (at most 190 ms); gameplay assertions are unchanged. Final verification starts from the beginning after that logging fix. No OS input, screen clicking, cursor control or external GUI automation is used.

`refine_luxury.py` and `finish_luxury_details.py` are stage scripts for the pre-refinement backup, not scripts to rerun blindly on the completed scene. `capture_luxury_review.py` renders the final scene. Export now expands Blender `//` image paths before Windows path parsing, so texture names remain meaningful and deterministic. Layout version 12 reapplies the finished visual assets through the existing Unity editor pipeline.
