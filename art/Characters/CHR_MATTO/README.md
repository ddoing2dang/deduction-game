# CHR_MATTO pilot — technical preparation

Status: **incomplete; locked design reference required**. This folder currently
contains a technical skeleton, not a finished character. No other character has
been started. The PresentationCatalog still contains the existing placeholders.

AGENTS.md and ART_GUIDE.md were reread on 2026-10-24. The selected Matto drawing
was not found in the repository, available Downloads/Pictures, or the accessible
game conversation. A reference-path question is pending. The original drawing
is needed to verify the locked face, hair, outfit and accessories before modeling.

## Implemented and checked

- Blender MCP connection: Blender 5.0.1 / addon 1.8.
- `CHR_MATTO_RigDevelopment.blend`: isolated technical scene written through MCP.
- 57 bones: 54 Unity human mappings, Root, Hair_Back_01 and Hair_Back_02.
- Continuous finger chains; no zero-length bones.
- Rig development FBX exported through Blender MCP.
- The running Unity Editor imported it as Humanoid using explicit HumanDescription.
- Imported Avatar `isValid=True`, `isHuman=True`; required mappings present.
- No gameplay, Mansion geometry, UI, AI or multiplayer changes.

Evidence: `rig-blender-verification.json`, `rig-unity-verification.txt`.

## Shared draft specification

The hierarchy and exact rest positions are in `humanoid_v1.json`. The draft
must pass skinned-mesh deformation and actual gameplay tests before being frozen
for the other five characters.

| Item | Draft specification |
| --- | --- |
| Body height / proportion | 1.568 m excluding beret; approximately 5.6 heads |
| Units | 1 Blender unit = 1 metre; object scale 1 |
| Blender orientation | +Z up, -Y forward |
| Root hierarchy | CHR_MATTO → Armature; CHR_MATTO → Mesh |
| Core bones | Root → Hips → Spine → Chest → UpperChest → Neck → Head |
| Arm chains | UpperChest → Left/RightShoulder → UpperArm → LowerArm → Hand |
| Leg chains | Hips → Left/RightUpperLeg → LowerLeg → Foot → Toes |
| Fingers | Left/Right + Thumb/Index/Middle/Ring/Little + Proximal/Intermediate/Distal |
| Eyes | Head → LeftEye / RightEye |
| Hair | Head → Hair_Back_01 → Hair_Back_02 |
| LOD0 target | 20–30K tris; no character mesh exists yet |
| LOD1 / LOD2 target | 50–60% / 20–30% of actual LOD0 |
| Material target | Toon skin, hair and outfit regions; shared atlas where suitable |
| Texture target | One <=2048² base atlas; separate face only if needed |
| Runtime deformation | Maximum four weights per vertex; no root motion |

Object names will follow ART_GUIDE: Body, Face, Hair_Main, Hair_Sub, Outfit,
Shoes, Accessory and ACC_MATTO_Beret. No renderers, materials, texture atlas,
LODs or facial meshes have been authored in this technical preparation.

### Facial shape contract

Implement the user-required Blink_L, Blink_R, Smile, Angry, Sad, Surprise,
Mouth_A, Mouth_I, Mouth_U, Mouth_E, Mouth_O. Preserve the guide's shared
Neutral, Happy, Suspicious, Surprised, Worried, Thinking, Smirk, EyeLook_L,
EyeLook_R, EyeLook_Up, EyeLook_Down, Brow_Up, Brow_Down, Mouth_Open names.
Surprise and Surprised should share the same authored expression. Neutral is
the base face. All active shapes require actual visible deformation checks.

### FBX and Unity settings checked on the technical rig

FBX: selected Root/Armature hierarchy only, 1.0 scale, apply unit scale,
FBX_SCALE_UNITS, -Z forward / Y up, no leaf bones, no baked animations,
Armature node type NULL. Final export must also include skinned Mesh objects.

Unity: globalScale=1, Rig=Humanoid, CreateFromThisModel, explicit HumanDescription,
importAnimation=false, optimizeGameObjects=false during development. Final
normal/tangent import, renderer/material configuration, LODGroup and mesh bounds
are untested because the character meshes do not yet exist.

`MattoRigDevelopment.Verify` runs from an Editor menu or the project-local
`matto-rig.request` endpoint. It verifies the technical rig without starting a
second Unity instance, sending input, or replacing any gameplay models.

### Remaining completion gates

1. Obtain and inspect the selected Matto design; model and visually compare it.
2. Skin Body/Face/Hair/Beret/Outfit/Shoes/Accessories; implement facial shapes.
3. Create LODs and textures; measure actual tris, renderer/material/bone counts.
4. Validate Idle/Walk/Investigate/Suspect/Deduction deformation and intersections.
5. Export CHR_MATTO.fbx, create prefab/controller/LODGroup and replace catalog index 3.
6. Test the actual Matto CharacterActor in Mansion, stairs/doors, both floors,
   action camera, cutaway and all three zooms; inspect actual captures.
7. Run regression checks and final self-review. Until then: verification incomplete.
