# Shared character standard v2 — approved Matto VRoid pilot

This supersedes the retired procedural 57-bone pilot. The immutable appearance
source is VRoidVisual/CHR_MATTO_VRoid_Visual.blend. Game edits belong only in
Game/CHR_MATTO_GAME.blend, through Blender MCP. Other five characters are outside this task.

## Asset contract

| Item | Specification |
| --- | --- |
| Scale | 1 BU = 1 metre; approximately 1.79 m silhouette |
| Axes | Editable Z up / -Y forward; Unity Y up / +Z forward |
| Unity root | CHR_MATTO; identity rotation, scale 1; Armature and Mesh children |
| LOD0 | 30,385 tris; approved face basis positions unchanged |
| LOD1 | 16,539 tris, 54.43% |
| LOD2 | 9,059 tris, 29.81% |
| Skeleton | 102 bones; current VRoid core/fingers/hair plus four skirt helpers |
| Humanoid | 54 explicit mappings from VRoid metadata |
| Parts | Body, Face, Hair_Main, Outfit, ACC_MATTO_Beret, Shoes, Accessory |
| Renderers | Seven selected per LOD, 21 stored |
| Materials | Nine; shared linear palette and face atlas |
| Textures | Ten referenced images including normals/emission/matcap; maximum 2048 |
| Skinning | Normalized weights, four influences maximum |
| Bone palettes | Face 3, body 54–56, hair 40, beret 1, outfit 15–17, accessories 5, shoes 4 |
| Movement | Root motion off; CharacterActor/NavMeshAgent remains authoritative |
| Agent | Height 1.80 m; radius, speed, routes and rules unchanged |
| LOD thresholds | .075 / .033 / .005 for the existing three zoom levels |

Exact hierarchy/rest positions: Game/vrm-humanoid-game.json.
Humanoid map: Game/vroid-human-map.json. Retain J_Bip_* and finger names;
do not force the approved VRoid onto the retired 57-bone skeleton. Remove only
unweighted, unused helper branches in the game copy.

## Facial expressions

25 common aliases: Neutral, Smile, Happy, Suspicious, Angry, Surprised, Worried,
Thinking, Smirk, Sad, Blink_L, Blink_R, EyeLook_L, EyeLook_R, EyeLook_Up,
EyeLook_Down, Brow_Up, Brow_Down, Mouth_Open, Surprise, Mouth_A, Mouth_I,
Mouth_U, Mouth_E, Mouth_O. Neutral has zero displacement from the approved basis.
Recipes reuse VRoid deltas and are recorded in Game/expression-map.json.
LOD faces retain every alias with interpolated deltas. Original VRoid facial
data remains in the immutable approved file and the hidden game-source archive.
The archive is never exported.

## Cloth, hair and visual adaptation

Four MATTO_Skirt_Front/Back_L/R bones pivot at the waist surfaces rather than
the thigh roots. Neutral coordinates stay unchanged. Front/back follow ratios
are .88/.30; the thigh strap follows the actual upper leg.

MattoVroidPresentation handles measured thigh flexion, foot IK with the .1007 m
ankle pivot, shared expression cues and gentle hair motion. Two selected joints
per visible hair chain move by less than two degrees. There is no expensive
full-strand simulation. Gameplay rules and CharacterActor are unchanged.

## Materials and export

Keep approved colour constants. Flat materials share a non-colour palette.
Face albedo and normals share corresponding 2K atlas layouts. Preserve source
mouth/iris/sclera backface rules and soft toon shading in Unity.
PC images stay uncompressed for visual preservation; Android uses ASTC 4x4,
except the tiny linear palette. All textures remain 2K or smaller.

Export Armature, Mesh container and the 21 game meshes only: no camera, light,
collider, archive or animation in the model FBX. Convert temporary data copies
to Y-up, including every shape key, with cancelling object transforms; restore
editable data in finally. Scale 1, apply unit scale, FBX_SCALE_UNITS,
-Z forward/Y up, no leaf bones. Import normals; calculate Mikk tangents.

Five representative clips use the current skeleton in
Animations/MATTO_VRoid_Actions.fbx and copy the production Avatar:
Idle, Walk, Investigate, Suspect, Deduction. Cooperate reuses Investigate,
UseAbility reuses Deduction, Win/Lose reuse Idle with expressions.
The nine-state CharacterState interface is unchanged.
This is not the final 18-animation library.

## Reproduction and verification

Game scripts: prepare_game_meshes.py, fix_cloth_weights.py,
optimize_game_materials.py, animate_game_vroid.py, preserve_lod2_layers.py, export_game_fbx.py,
finalize_skin_weights.py, capture_final_review.py. These are build stages; do not run them blindly on
an already-finalized file. Start a fresh game copy from the approved file.

Unity uses MattoGamePipeline for import, materials, Avatar, Animator, LODGroup
and catalog slot 3; MattoGameAudit tests real CharacterActor movement and captures
Camera.main. The local matto-game.request API accepts Refresh, Setup, Audit.
After compilation, wait for the current editor-api-version.txt before Setup.
Acceptance requires directly reviewing captures, not merely a valid Avatar/path.