# CHR_JIRO — final integrated game asset

The latest user instruction authorizes the appearance correction, optimization and Unity application together. No further appearance-approval gate is pending for this pass.
The previous rejected Jiro Visual/Archive models were not read or reused. `../Visual_MattoBase/CHR_JIRO.blend` is the immutable pre-correction source; its recorded hash still matches.

## Appearance correction
Cheek width was reduced about 2.7% and jaw width about 3% with smooth regional influence. Eye contour is slightly less round with a clearer outer canthus; gold iris and the existing cheerful expression remain. Every expression key received the same basis morph.
Bang leaf ends taper progressively; the existing scalp-root row remains unchanged. Tail middle widths and outward volume are reduced, with thin non-circular clumps tapering into separate points. The downward S flow, both flowers, dark ribbons and green colour remain. Optimized LOD0 surface distance from refined root centres is below 0.10 mm; the original 1.5 mm scalp offset remains.
Cardigan sleeve radius was reduced 6% around the correct 28-vertex authored rings, with inner puff ends fitted underneath. The waist is clearer and skirt pleat normals are sharp. Only fully covered torso/calf skin faces were removed to prevent animation/LOD poke-through.

## Production contract
- Blender: `CHR_JIRO_GAME.blend`; 1 BU = 1 m, editable Z up / -Y front, approximately 1.68 m silhouette.
- Unity: `Assets/_Game/Characters/CHR_JIRO/Model/CHR_JIRO.fbx`; Y up / +Z front, root local rotation identity, scale 1.
- Original visual: 35,922 tris. Final Blender LOD0/1/2: 30,140 / 17,638 / 8,887 tris.
- Final Unity LOD0/1/2: **30,122 / 17,624 / 8,873 tris**. Unity removes 18/14/14 degenerate triangles during import. Ratios: 58.51% / 29.46%.
- LOD0 retains the complete corrected face topology and hands. Reduction is selected by part, not applied to the whole character. Lower face LODs interpolate all keys from the LOD0 surface.
- 69 bones: original Humanoid/fingers/eyes and ancestors, six JIRO_Tail_* helper joints and four JIRO_Skirt_* waist helpers. Unused old hair/skirt helper branches were removed only in the game copy. All 54 explicit Humanoid mappings remain; `vroid-human-map.json` documents them.
- Seven active SkinnedMeshRenderers per LOD, 21 stored. Body palettes 54, face palettes 3; maximum four normalized weights. See `skin-weight-audit.json` and runtime `Verification/performance-results.txt`.
- Seven used Unity materials (eight descriptors, one unused face overlay definition), seven referenced textures / five main textures, maximum 2048. A 128px linear palette consolidates flat colours; the green hair paint is also linear and must NOT be imported as sRGB. Android ASTC4x4 except the small linear palette; no Android device benchmark is claimed.
- LOD thresholds .075/.033/.005 match existing zooms 9/19/32. LOD2 is for overall zoom; small skirt folds and cap shading simplify when forcibly enlarged.
- 25 expression aliases plus Basis on each LOD. Required eleven keys are present, nonzero and rendered: Blink_L/R, Smile, Angry, Sad, Surprise, Mouth_A/I/U/E/O. Neutral remains zero.

## Unity integration
`JiroGamePipeline` creates a valid Humanoid Avatar from 54 mappings, remaps the toon materials, creates the Animator/LODGroup/prefab, and changes only PresentationCatalog slot 1. Matto slot 3 and Naz slot 0 are preserved in the working project. Existing Matto/Naz character files remain hash-identical (118 checked).
`JiroVroidPresentation` adapts expressions, conservative six-joint hair sway, waist cloth response and foot IK. CharacterActor/navigation/game logic are unchanged; root motion is disabled.
The five validated common MATTO_VRoid_Actions clips are retargeted without changing that library: Idle, Walk, Investigate, Suspect, Deduction. Cooperate reuses Investigate, UseAbility reuses Deduction, and Win/Lose reuse Idle with their expression cues. All nine requested Animator states were tested; this is not a new final 18-animation set.
The toon shader is a Jiro-named copy of the validated shared/Naz shader so this commit does not depend on modifying the completed Naz asset.

## Workflow and evidence
Blender edits/export use MCP only. Unity uses existing Editor/runtime code and file requests (`../../../jiro-game.request`: Refresh, Setup, Audit); no OS input, cursor control or GUI automation.
Construction scripts are sequential stages, not a one-button rerun over a finalized file: preserve Visual and save a game copy, refine_game_appearance, prepare_game_meshes, finalize_skin_weights, optimize_game_materials, fix_palette_uv (Blender 5 UV allocation safeguard), mask_hidden_skin, export_game_fbx. The source archive is hidden and excluded from export.

Actual Play Mode results: 22/22 bidirectional traversals; nine Animator states; 54 StandPoints; nine ActionPoints; support/foot contact/wall and door mesh clearance; both service stairs in both directions; three zooms in nine rooms; basement/cutaway; action camera and return; original single-player regression 2,461 assertions; Restart and existing UI readiness. Matto/Naz/Jiro coexist in a controlled actual-runtime comparison; other three placeholder renderers are temporarily hidden for that one comparison and restored. Full room/stair captures retain all six actors.

Directly reviewed: `Verification/Boards` and raw Camera.main frames; `Comparison` contains the pre-correction Blender frames and current optimized frames. All final review labels were written only after directly viewing the current captures.
[Final review](Verification/final-review.md), [before/refined/Unity](Verification/Boards/Before_Refined_Unity_LODs.png), [faces](Verification/Boards/Face_Before_Refined_Unity.png), [trio](Verification/Trio_MATTO_NAZ_JIRO.png).
Game rules, AI, Mansion geometry, UI and multiplayer files were not changed. No other character was authored.
