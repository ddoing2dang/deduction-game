# Current full environment pass (2026-10-24)

The complete environment is now imported into Unity at SceneLayoutVersion 8. See [StructureAudit/REPORT.md](StructureAudit/REPORT.md) for the current structural inventory, deletion list, FBX workflow, all-link physical traversal results and Blender/Unity camera evidence. Earlier partial stair/repair reports below are historical and do not supersede this audit.

# Mansion stair architecture revision — 2026-10-24

Current geometry, Entry changes, visual assessment and verification evidence are documented in [STAIR_ARCHITECTURE_REPORT.md](STAIR_ARCHITECTURE_REPORT.md).
The current stair source is `redesign_stairs.py`; run it only through Blender MCP. It uses `Mansion_BeforeStairArchitecture.blend` as the local revision baseline, followed by `verify_stair_architecture.py`, `export_repair.py` and `capture_stair_architecture.py`.

The section below records the **previous structural repair**, before the current architectural redesign. Its unchanged-Entry assertions and old verification JSON do not describe the current four revised stair Entries, and its movement success is not visual approval.

## Previous structural repair (historical)

Editable source: Mansion_Environment_Pass01.blend.
Export: Mansion_Environment_Pass01.fbx.
Unity copy: game/UnityProject/Assets/_Game/Art/Mansion/Mansion_Environment_Pass01.fbx.

Only these areas are installed into Unity by MansionStructuralRepair:
- DOOR_03_05: L-shaped service passage; 2m incoming leg and 2.2m kitchen vestibule; trim, guards, supporting beams/posts. Kitchen portal corner clearance is local to this existing connection. Central Hall walls/openings are unchanged.
- Central Hall decorative stair/gallery: final tread and gallery both 2.16m; approach railing removed; side/rear balustrades, fascia and corbels. No Collider, RoomNode or Entry is created for the gallery.
- DOOR_04_07 and DOOR_05_08: perimeter stair flights, landing connections, actual upper-floor openings and local basement wall/door returns. Existing DoorID and navigation anchors are retained. Visual treads and smooth collision envelopes are separate.
- Cutaway keeps physical Colliders enabled and hides the covering room's foreground wall with its floor.
- Stair guards/frames are non-walkable NavMesh sources. Mansion NavMesh detail heights improve agreement with the stair collision envelope.
- FBX import transforms are baked with the explicit Blender-to-Unity axis convention; only manifest-listed repaired mesh parts replace graybox geometry. Unrelated room art is not installed.

Evidence:
- repair_blender_verification.json: no Hall passage intersections, no stair-envelope/wall intersections, no tread or collision-envelope headroom obstruction within 1.3m, no upper-room StandPoint floor loss, unchanged reference positions, zero gallery height mismatch.
- UnityVerification/playmode-results.txt: all six directional traversals, no off-mesh links or other-room detours; character collision capsule uses 0.3m radius; all 54 StandPoints; nine rooms; eleven locked links; unchanged navigation transform snapshot; existing event hierarchy; retained physical Colliders during cutaway.
- UnityVerification/Room_*.png: in-Play camera captures at the existing near zoom.
- UnityVerification/stair-nav-diagnostics.txt: local stair NavMesh sampling and physics ray measurements.
- Editor verification caps simulation delta to 1/60s while running so background Editor stalls do not create large integration steps. Character speed/radius and NavMesh path/collision criteria are not relaxed. The previous maximum delta is restored on completion.
- ART_GUIDE.md, MAPS.md, game rules, MansionLayout, MapGraph, CharacterActor and MansionEventView are unchanged.

Use Deduction Game > Verify Mansion Structural Repair in Unity to repeat.
verify_repair.py is a read-only Blender verification script. export_repair.py exports the authoritative edited .blend. The older build_mansion.py / repair_structure.py are initial construction scaffolds, not regeneration scripts for the final manually refined scene.
