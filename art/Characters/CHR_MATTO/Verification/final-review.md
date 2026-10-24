# CHR_MATTO pilot final self-review — 2026-10-24

**검증: OK** — pilot character and reusable Humanoid contract v1.

The user-provided Matto turnaround, actual Blender renders, and actual Unity
Play Mode Camera.main captures were directly inspected. This acceptance includes
visual inspection and does not rely on Avatar validity or navigation success alone.

## Delivered

- Editable `CHR_MATTO.blend`, reopened successfully after saving. Packed atlas,
  57 bones, 21 mesh parts over three LODs, 25 shape keys excluding Basis, five
  authored actions. The rest skeleton is a real T-pose; its rest coordinates
  match `humanoid_v1.json`. Source art and authoring scripts are preserved.
- Production FBX: `game/UnityProject/Assets/_Game/Characters/CHR_MATTO/Model/CHR_MATTO.fbx`.
- Separate five-take animation FBX, Humanoid Avatar, AnimatorController, toon
  atlas material, compressed 1024-square texture, prefab and LODGroup.
- Only Matto, catalog index 3, replaces its CharacterActor placeholder. The
  other five characters have not been produced.

## Visual review

- Blender front/side/back/isometric, rest T-pose, Idle/Walk/Investigate/Suspect/
  Deduction and required facial shapes inspected. No severe exposed arm/body,
  head/hair, beret/hair or leg/outfit intersection in the representative poses.
- Locked white long hair, red eyes, black beret, deep-red bow, black/white outfit,
  pleated skirt, heart, thigh harness and shoes remain readable. Sock asymmetry
  follows the upper turnaround. Beret's circular insignia is no longer buried
  in the hat surface.
- Unity actual imported model is upright after Animator evaluation, with identity
  root rotation and scale 1. An earlier axis/bind presentation failure was fixed
  by converting temporary export data rather than leaving a root-axis correction.
- Unity nine rooms × three zooms reviewed, including surface/basement cutaway.
  Close views show face/colours; default/full views preserve white-hair/black-beret
  silhouette and outfit contrast. Full zoom is for map readability, not expressions.
- Per-frame Unity facial captures confirm actual eye closure and mouth/expression
  deformation. An earlier same-frame capture cache issue was corrected before
  this final review. Hands are lowered for facial inspection.
- Four actual moving service-stair cases captured at three zooms: 4→7, 7→4,
  5→8, 8→5. Stair/door clearance and scale remain natural for the pilot.

Evidence: `Review/Blender_Turnaround.jpg`, `Blender_Actions.jpg`, `Blender_Faces.jpg`,
`Unity_Actions.jpg`, `Unity_Faces.jpg`, `Unity_ServiceStairs.jpg`, and
`Unity_Zoom_9.jpg`, `Unity_Zoom_19.jpg`, `Unity_Zoom_32.jpg`.

## Play and asset checks

- Actual Matto CharacterActor traversed all 11 ordinary links both ways: **22**
  successful movements, each observing its real Move/Walk Animator state.
- Complete NavMesh paths; no excessive detours, off-mesh fallback, unsupported
  actor roots, wall/door penetrations, sampled skinned-mesh solid-wall intrusion,
  excessive rotation discontinuities, buried feet or persistent double-foot floating.
- Nine rooms, 11 basic links, six actors, 54 StandPoints and nine ActionPoints
  retained. Existing single-player E2E: **2,461 assertions**. Restart, RuntimeGameUI,
  camera and cutaway smoke checks pass. Rules/AI/UI/Mansion/multiplayer source
  files were not changed by this character task.
- Humanoid Avatar valid/human; 54 explicit mappings, five imported human-motion
  clips and the existing nine-state CharacterActor integer interface.
- All 21 imported meshes have usable finite normal/tangent streams and normalized
  skin weights. Material count 1, texture count 1, atlas 1024×1024. Seven renderers
  selected per LOD, 21 stored. The common skeleton has 57 bones.
- Tris: LOD0 **25,610**, LOD1 **15,261** (59.59%), LOD2 **6,280** (24.52%).
  Stored all-LOD total **47,151**. Resource budgets are checked; no device FPS claim.

Detailed machine results: `play-results.txt`, `performance-results.txt`,
`runtime-axis-diagnostics.txt`, and the parent-folder import/Blender reports.

## Reuse and remaining polish

Freeze shared names/hierarchy, shape contract, scale, export conversion and
Humanoid import settings in `../CHARACTER_STANDARD.md` and `../humanoid_v1.json`.
Reuse these for Naz/Jiro/Marihi/Boki/Munire while keeping their locked identities.

The five clips are representative pilot motions. Fine fabric painting, richer
hair secondary motion and the final 18-clip library remain later art polish;
they are not represented as complete here. No blocking pilot scale, deformation,
navigation or visual-identification defect remains in the inspected cases.

No OS mouse/keyboard, automated screen clicks, cursor manipulation, Windows
settings changes or external GUI automation was used. Blender operations used
MCP; Unity operations used project files and Editor/runtime APIs.

Blender 연결: OK

검증: OK
