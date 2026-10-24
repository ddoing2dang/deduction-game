# CHR_MATTO — approved VRoid production character

The user-approved appearance source is VRoidVisual/CHR_MATTO_VRoid_Visual.blend,
including the attached beret emblem. Never overwrite it or redesign its face,
eyes, hair, beret, outfit, colours or proportions. Its hash is in Game/approved-source.sha256.

The separate editable game source is Game/CHR_MATTO_GAME.blend.
The production FBX/prefab are in Assets/_Game/Characters/CHR_MATTO in game/UnityProject.
Only PresentationCatalog character slot 3 uses this model.

Read CHARACTER_STANDARD.md and Game/Verification/final-review.md for current
specifications and evidence. Raw captures are local; curated sheets are in
Game/Verification/Review.

The root-level CHR_MATTO.blend, build_matto.py, 57-bone humanoid_v1.json,
old pilot animation FBX and root Verification folder are retired history.
They are not the approved character. Do not regenerate or apply the old model.
The legacy Editor setup entry point redirects to the approved VRoid pipeline.

Blender work uses MCP; Unity uses project-local Editor/runtime APIs.
No OS input, cursor changes or GUI automation.