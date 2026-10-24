# CHR_MATTO approved VRoid game finalization

The approved visual source is preserved byte for byte. The separate game asset
uses its VRoid mesh and skeleton, not the discarded procedural Matto pilot.
Blender was operated through MCP. Unity was operated through project files and
Editor/runtime APIs. No OS input, cursor control or external GUI automation was used.

## Deliverables and preservation

- Approved source: `VRoidVisual/CHR_MATTO_VRoid_Visual.blend` under the character folder.
- Editable game source: `Game/CHR_MATTO_GAME.blend` under the character folder.
- Runtime model: `Assets/_Game/Characters/CHR_MATTO/Model/CHR_MATTO.fbx`.
- Runtime prefab: `Assets/_Game/Characters/CHR_MATTO/Prefabs/CHR_MATTO.prefab`.
- Current-skeleton motion source: `Animations/MATTO_VRoid_Actions.fbx` in that Unity folder.
- Approved source SHA256: `145420f730c065b405198f761e8e618b8b29c063428fae2b3c46225a4918a71a`.

The game file was reopened successfully through Blender MCP. Its hidden source
archive retains the approved meshes and original expression data. It is excluded
from the FBX. The approved source file is never saved over.

LOD0 face Basis coordinates have maximum delta 0.0 from the approved Face Basis.
Direct comparisons of approved Blender, game Blender and Unity captures preserve
face design, eye shape, white hair silhouette, fitted beret/emblem, outfit colors
and proportions. Blender and Unity illumination differ; neither renderer is a
pixel-identical reference to the other.

## Runtime budget

| Item | Final value |
| --- | ---: |
| LOD0 | 30,385 triangles |
| LOD1 | 16,539 triangles, 54.43% |
| LOD2 | 9,059 triangles, 29.81% |
| Skeleton | 102 bones, 54 explicit Humanoid mappings |
| SkinnedMeshRenderer | 21 stored, 7 active in each LOD |
| Unique materials | 9 |
| Referenced textures | 10, maximum 2048 pixels per dimension |
| Skin influences | At most 4 per vertex, normalized |

The original 162-bone rig was pruned only for unused/hidden garment branches.
The 98 retained bones plus four waist cloth helpers form the 102-bone game rig.
Fingers, necessary hair joints and facial deformation survive. Per-renderer GPU
bone palettes are bounded: Face 3, Body 54–56, Hair 40, Beret 1, Outfit 15–17,
Accessory 5, Shoes 4. These are binding palette reductions, not deleted rig joints.

Face/body albedo and normal atlases use 2K; hair textures use 512x1024; tartan is
1024 square; linear palette is 128 square; matcap is 8 square. Android uses ASTC
4x4 except the small linear color palette. PC preserves uncompressed source
color. No physical Android device frame-rate measurement is claimed.

## Deformation, expressions and animation

Direct Blender render review covers T/A pose, Idle, Walk, arms up, arms forward,
knee bend and sitting bend. Weight repairs concern the shoulders, skirt and
thigh strap; approved neutral coordinates and silhouette remain intact. Waist
cloth pivots keep the sitting skirt over the lap. LOD2 retains original approved
blouse/corset shells to prevent clothing-layer poke-through.

All three face LODs contain 25 shared expression aliases. The required eleven
have nonzero authored deformation and were checked in Unity:
Blink_L, Blink_R, Smile, Angry, Sad, Surprise, Mouth_A, Mouth_I, Mouth_U,
Mouth_E, Mouth_O. Neutral morph defaults are zero on every LOD. Source VRoid
expressions remain archived. Neutral, blink and mouth deformation were also
rendered across all three Blender LODs.

Unity Avatar is valid and human. Scale is one; the model root rotation is
identity. The production model imports no animation, cameras, lights or collider.
Normals/tangents are finite and usable. The five representative Humanoid clips
are Idle, Walk, Investigate, Suspect and Deduction, authored on the current rig.
All nine existing CharacterState transitions were tested. Cooperate reuses
Investigate; UseAbility reuses Deduction; Win/Lose reuse Idle with expressions.
These are representative clips, not the final eighteen-animation library.

Hair secondary motion is limited to sixteen selected joints with small angular
movement. The lightweight visual adapter also provides blink and cloth response.
The existing CharacterActor navigation, rules and state interface remain intact.

## Actual Mansion and regression evidence

`play-results.txt` records the actual runtime checks. Captures use Camera.main,
not a replacement scene render. Direct image review is required in addition to
the automated terminal success marker.

The eleven basic connections were traversed in both directions by the new Matto
CharacterActor: 1–2, 1–3, 1–5, 1–6, 2–4, 3–5, 4–5, 4–7, 5–8, 7–8 and 8–9.
The 22 traversals check complete NavMesh paths, walking Animator state, grounded
support, wall/door and sampled skinned-mesh clearance, bounded turning and no
long detour. Both service stairs were captured during actual movement.

All 54 StandPoints were occupied by Matto with ground and mesh-clearance checks.
All nine ActionPoints were occupied in the Investigate state with ground and
mesh-clearance checks; their saved positions remain unchanged. Nine rooms were captured at close/default/
overall orthographic sizes 9/19/32, including ground and basement cutaway.
The actual deduction camera cue reaches its existing close zoom and returns to
the default zoom after cancellation. Both camera states were directly viewed.

Existing single-player E2E passed 2,461 assertions. Restart retains all six actors,
exactly one Matto, ready RuntimeGameUI, camera and cutaway. Game rules, AI, UI,
Mansion geometry and multiplayer implementation have no task changes.

## Directly reviewed captures

See `Review/Approved_Blender_Unity_Face.png` and
`Review/Approved_Blender_Unity_Full.png` for approved/game/Unity comparisons.
The Review folder also contains Blender weights, actions, LODs and facial grids;
Unity nine-state actions, expressions, LODs, service stairs and nine-room grids
for all three zoom levels. Full-resolution source captures are retained locally
outside Git; curated review sheets and text evidence are versioned.

No remaining blocking visual deformation was found in the reviewed poses and
camera captures. Reduced far LODs intentionally simplify small ornament and
cloth detail; near-camera face design and the approved LOD0 appearance remain.
Android resource settings are prepared, but device-specific performance remains
a separate hardware measurement.

Final result: 22/22 basic traversals, 54/54 StandPoints, 9/9 ActionPoints,
9/9 Animator states and 2,461 single-player assertions passed. Curated images
were directly reviewed after the final runtime run. The final reporting-only
change labels six main/albedo textures separately from ten total referenced
textures; Editor API version v12 loaded successfully after compilation.

외형 보존: OK
Unity Humanoid: OK
Blender 연결: OK
검증: OK
