# Mansion environment structure audit — 2026-10-24

This pass supersedes the earlier partial stair/structural verification. The user's explicit eleven basic links plus secret 9↔6 and ART_GUIDE govern the current Mansion. MAPS.md contains older contradictory theme topology; it was not used to change the locked live graph.

## Structural changes

The previous environment repeated portal frames at both ends of connections, exposed oversized platforms and thin floor borders, and used wall openings projected from older endpoints. The service stairs also had duplicated upper landing slabs. Blender and Unity were not displaying the same complete environment: Unity had installed only the earlier repair subset.

597 original pieces were removed or replaced, reducing 926 original meshes to 560 classified meshes:

- 329 old corridor/frame/rail/platform pieces replaced by nine compact continuous passage slabs, nine actual wall portal assemblies and their edge guards. Each portal assembly has two jambs and one header, rather than two freestanding doors for the same connection.
- 235 old wall pieces replaced by 78 perimeter wall segments with explicit openings matching the locked graph.
- 15 thin floor borders and ten unnecessary pilasters removed.
- Two unaligned secret panels replaced by wall-aligned panels at the actual secret entries. Hovering runtime cyan entrance cubes are suppressed when mapped panels exist.
- Two coplanar upper landing slabs removed; the original room floors provide the upper landings. Four redundant exposed stair stringers removed.
- No additional functional stair flight was needed or retained. The two existing functional flights remain; the Hall gallery stair is explicitly decorative with no Collider or movement node.

All classified parts use Room, Floor, Wall, Door, Passage, Stair, Railing, Support or Gallery roles. There are no unclassified exported meshes. The complete named deletion list and reasons are in cleanup_changes.json.

## Retained doors, connections and stairs

| DoorID | Purpose / final structure |
|---|---|
| DOOR_01_02 | Hall↔Library, short 1.4m portal |
| DOOR_01_03 | Hall↔Lounge, 1.8m portal |
| DOOR_01_05 | Hall↔Kitchen, 1.3m service portal |
| DOOR_01_06 | Hall↔Greenhouse, 2.2m portal |
| DOOR_02_04 | Library↔Dining, 2m wing passage |
| DOOR_03_05 | Lounge↔Kitchen, compact L passage; kitchen east opening moved to Blender Y=-4.9 to clear the stair approach |
| DOOR_04_05 | Dining↔Kitchen, 1.6m service passage |
| DOOR_04_07 | Dining west service stair; southward flight, lower turn and return to Boiler south wall |
| DOOR_05_08 | Kitchen east/rear service stair; southward flight, lower turn and return to Wine south wall |
| DOOR_07_08 | Boiler↔Wine, 2m basement passage |
| DOOR_08_09 | Wine↔Servants, 1.6m staff passage |
| DOOR_09_06_SECRET | Servants north panel ↔ Greenhouse west panel; existing event transition, no exposed airborne tunnel |

Both service flights retain 1.6m width, 6.6m horizontal run, 4m descent and 23 treads, with level upper and lower approach space, rails, floor openings and solid collision ramps. The Hall gallery stays on the rear/left perimeter, leaving the six-player central floor free. Its end is a balustraded gallery, not a fake destination room.

Room centres and footprints, nine RoomNodes, 54 StandPoints and nine ActionPoints remain unchanged. Entry positions are imported from the same layout.json used by Blender. Minimal changed entries are 03→05, 05→03, 08→09 and 09→08; existing service entries remain as previously aligned. All DoorIDs and logical edges are retained.

## Unity installation and validation

The authoritative full FBX is installed through MansionEnvironmentCleanup, SceneLayoutVersion 8. Imported meshes have explicit baked coordinates and physical roles; decorative gallery and room art do not create navigation routes. Runtime Mansion NavMesh no longer manufactures centre-to-centre off-mesh links when actual geometry is disconnected.

The audit exposed and fixed two genuine failures before acceptance: the 03↔05 kitchen approach detoured through Hall, and bottom vestibule rail names were accidentally included in a floor replacement. The final geometry uses the intended corridor and both service flights in both directions. Failed intermediate results are not acceptance evidence.

The final actual CharacterActor run covers all eleven connections in both directions, using one naturally calculated path each time, with no forced midpoint route and no off-mesh fallback. It checks destination arrival, route length, corridor/room containment, continuous foot support, capsule penetration and bounded rotation on every frame. The secret transition tests the actual actor and existing 1P event rule in both directions. Existing single-player E2E has 2,461 passing assertions, including game restart/UI and event behavior. Multiplayer logic was not changed.

Blender verification checks all classifications, one floor per room, two functional flights, matching protected references, all 54 StandPoint footprints, ramp/wall intersections and 2.05m headroom across the flights. See blender-verification.json.

Visual evidence includes Blender top/front/side/iso and three zooms for all nine rooms (63 captures), and Unity Play camera captures for all nine rooms at zoom 9/19/32 plus top/front/side/iso. The actual fixed camera remains at 42 degrees with its existing offset and zoom levels. Cutaway additionally hides wall renderers actually occluding the active room, including Hall walls in front of Greenhouse; it leaves physical Colliders enabled. Visual/Play_UI.png renders the existing UI Canvas through the play camera for review, restoring its mode afterward.

The final reviewer must inspect the captures directly; automated movement success alone is not visual approval. Final review and machine result are recorded separately in final-review.json and After/play-results.txt.

## Reproduction

Blender scripts must run through Blender MCP only, in order: cleanup_environment.py, export_environment.py, verify_environment.py, capture_environment.py. Cleanup uses Mansion_BeforeEnvironmentCleanup.blend as its reproducible baseline. Export creates Mansion_Environment_Pass01.fbx and copies it into Unity Assets/_Game/Art/Mansion with environment_manifest.json.

In the already-open Unity Editor, write Refresh to the existing stair-architecture.request endpoint, then After to mansion-structure.request. Wait for completion before requesting Visual. These are project Editor APIs, not desktop input automation. The validation exits Play mode without closing Unity. review_environment.py makes contact sheets from the original captures.
