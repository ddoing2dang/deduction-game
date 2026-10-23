# Mansion Environment Pass02 — 2026-10-24

Stage 1 fixes the Wine cellar entrance. Stage 2 adds restrained room identities without changing the locked room footprints, graph or navigation references. Blender work runs through MCP only; Unity changes use project files and Editor APIs. No desktop input automation is used.

## Wine entrance cause and fix

The obstruction was **not a blocked NavMesh path**. Four `R08_Hero_WineRack_0..3` shelves and six `R08_Hero_RackDivider_0..5` dividers occupied the west-wall 7↔8 doorway's inner approach. Its centre was Blender (-2.9,-5.4), and its long axis crossed the opening at Y=-6.6. These room-art meshes had no Collider, so the earlier movement-only check could pass while the visible rack resembled prison bars.

The complete ten-part rack is rotated 90 degrees onto the north wall at (1,-3.45), away from all three entrances. It receives physical collision and later a backing panel and bottles so it reads as storage. No doorway, stair or required fall guard is removed. DoorIDs, all Entries, 54 StandPoints and nine ActionPoints are unchanged in this pass. The stage-1 baseline and gate are `Mansion_EntranceFixed.blend` and `Entrance/gate.json`; only after reviewing that stage was Pass02 built.

Stage-1 evidence includes inside/outside views for each of DOOR_05_08, DOOR_07_08 and DOOR_08_09, fixed play camera zooms 9/19/32, all 22 basic directional traversals (including the six Wine directions), secret transitions and 2,461 existing single-player assertions. The lower landing's outer edge guard is retained solely for fall protection; the route and door centre remain open.

## Room art

| Room | Pass02 additions |
|---|---|
| 1 Hall | Burgundy geometric rug, brass border, perimeter console and lamp, wall mouldings; existing decorative gallery/stair and chandelier retained |
| 2 Library | Fifty low-poly books in the existing case, dark writing desk, chair, open book, drawer and reading lamp |
| 3 Lounge | Blue upholstered sofa, wall-side rug, coffee table/tray, portrait frame; central six-person area remains free |
| 4 Dining | Southern banquet table, six chairs, runner, plates and candles; existing sideboard moved east to clear the 2↔4 Entry |
| 5 Kitchen | West-wall sink/cabinet/worktop, tap, shelves and jars, cooker burners; service stair retained |
| 6 Greenhouse | Transparent wall panes with retained physical boundaries/mullions, three restrained plant clusters, pots and potting shelf |
| 7 Boiler | Cylindrical vessel, copper bands/flue, connected pipe bank, valve disks and gauge panel |
| 8 Wine | Relocated wall rack, backing panel, 18 repeated bottles with necks/labels, two barrels and a crate in the southwest storage corner |
| 9 Servants | Plain blanket/pillow, bedside cabinet and lamp, simple locker handles; modest existing bed retained |

Dining furniture is south of the protected player cluster; the north/west service approach remains clear. Lounge coffee-table placement was adjusted during Blender clearance review. All room furniture is classified as non-walkable NavMesh geometry, preventing low seat/table surfaces from becoming navigable floor. All actual physical collision remains enabled during cutaway.

## Geometry, sharing and materials

- Total placed geometry, including the same two hidden stair collision envelopes: **6,796 → 13,980 tris**, an increase of 7,184.
- 323 new art instances share 23 mesh assets. Unity preserves their object transforms and shared Mesh references rather than baking one mesh per instance. Runtime validation checks every one of the 878 installed environment bounds against Blender, verifies triangle totals and shared references.
- Flat material palette: dark timber, muted blue/burgundy, warm ivory, brass/copper, modest greenery and translucent glass. No new raster textures or high-resolution texture dependencies.
- Unity assigns manifest palette materials explicitly, including transparency and GPU instancing. This avoids dependence on FBX linked-instance material-slot mapping.
- No final lighting/VFX, characters, Cruise/Museum, multiplayer or game rules were changed. This is an intermediate environment art pass.

## Cutaway and visible actors

When an upper room floor is cut away for basement play, its room art is hidden as well, avoiding floating kitchen furniture over the cellar. Furniture assemblies that actually cover a StandPoint actor's body can be hidden as a group. General floor visibility rays affect walls only; they do not erase the room's hero furniture merely because it occupies a corner. The original camera angle, offset, controls and zoom levels are retained.

Final/Visual contains actual Play Mode camera captures with all six CharacterActors placed at each room's six StandPoints. It covers all nine rooms, three zooms, top/front/side/iso and all six Wine inside/outside views. The visual check also casts to each actual actor and rejects visible opaque environment occlusion. Final/Visual/Play_UI.png renders the existing Canvas through the play camera, with Canvas mode restored afterward.

## Reproduction and evidence

1. `fix_wine_entrance.py` runs only through Blender MCP against `Mansion_BeforePass02.blend`, reproducing the entrance relocation.
2. After the entrance gate is passed, `build_pass02.py` opens `Mansion_EntranceFixed.blend`, builds the art and preserves all REF transforms.
3. `verify_pass02.py` checks reference positions, actor/Entry/Action footprints and visible-mesh bundles across all three Wine entrances. `export_pass02.py` exports `Mansion_Environment_Pass02.fbx` and its manifest into Unity's Mansion art folder. `capture_pass02.py` makes isolated-room Blender evidence.
4. Unity `MansionEnvironmentCleanup` installs the manifest-selected full model. `ProjectBootstrap.SceneLayoutVersion` is 9.
5. In the open Editor, write Refresh to the existing `stair-architecture.request`, then After to `mansion-pass02.request`. Wait for full movement/event/E2E completion before requesting Visual. The Editor exits Play mode and stays open.

Final machine evidence: `blender-verification.json`, `Final/After/play-results.txt`, and `Final/Visual/art-results.txt`. Human-visible acceptance requires direct capture inspection and is recorded separately in final-review.json. Intermediate failed results are not acceptance evidence.

Source: `art/Mansion/Pass02/Mansion_Environment_Pass02.blend`.
Export: `art/Mansion/Pass02/Mansion_Environment_Pass02.fbx`.
Unity copy: `game/UnityProject/Assets/_Game/Art/Mansion/Mansion_Environment_Pass02.fbx`.
