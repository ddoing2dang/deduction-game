# Shared character specification v1 — Matto pilot

Acceptance is recorded in `Verification/final-review.md`. Do not start the
remaining five characters before the pilot passes its full visual/play review.

## Appearance source

User-provided `Matto_Turnaround.png` was directly inspected. The upper turnaround
governs approximately 5.6-head proportions; the lower SD examples do not. Retain
white long hair, red eyes, black beret, black/white/deep red outfit, neck bow,
puff sleeves, pleated skirt, heart pendant, thigh harness and shoe buckles.

## Measured asset specification

| Item | Specification |
| --- | --- |
| Height | Body 1.568 m; beret/hair silhouette approximately 1.65 m |
| Source orientation | Blender +Z up, -Y forward, 1 BU = 1 metre |
| Runtime orientation | Unity +Y up, +Z forward; root rotation identity, scale 1 |
| Root hierarchy | CHR_MATTO / Armature; CHR_MATTO / Mesh / named parts |
| LOD0 | 25,610 tris |
| LOD1 | 15,261 tris, 59.59% |
| LOD2 | 6,280 tris, 24.52% |
| Renderers | Seven parts per LOD; 21 SMRs stored, seven selected per LOD |
| Material / texture | One shared toon material; one 1024×1024 RGB atlas |
| Texture import | Compressed, mipmapped, sRGB, no unused alpha |
| Bones | 57 authored; 54 explicit Unity Humanoid mappings |
| Skin influence limit | Four maximum; authored weights use one or two |
| Root motion | Disabled; CharacterActor/NavMeshAgent owns movement |
| Runtime bounds | Animation-safe 1.8 × 2 × 1 m SMR bounds, centre y=.83 |
| LOD screen thresholds | .075 / .033 / .005 for existing zooms 9 / 19 / 32 |

Parts: Body, Face, Hair_Main, Outfit, ACC_MATTO_Beret, Shoes, Accessory. Hair uses
a fixed small number of broad locks, not individual strands. Face topology and
shape names are retained at each LOD. LOD2 reduces non-face parts with weighted
decimation. Fine fabric painting and dynamic hair physics remain later polish;
this is a pilot mesh/rig/Avatar asset, not the final 18-animation library.

## Skeleton and bone names

Exact hierarchy/rest positions: `humanoid_v1.json`.

```text
Root
└ Hips
  ├ Spine → Chest → UpperChest → Neck → Head
  │                         │             ├ LeftEye / RightEye
  │                         │             └ Hair_Back_01 → Hair_Back_02
  │                         ├ LeftShoulder → LeftUpperArm → LeftLowerArm → LeftHand
  │                         └ RightShoulder → RightUpperArm → RightLowerArm → RightHand
  ├ LeftUpperLeg → LeftLowerLeg → LeftFoot → LeftToes
  └ RightUpperLeg → RightLowerLeg → RightFoot → RightToes
```

Each Hand has Thumb, Index, Middle, Ring, Little chains, each named
`Left/Right + finger + Proximal/Intermediate/Distal`. Root and the two hair
bones are outside the Humanoid map. Names/hierarchy must not be renamed for
later characters. Character-specific hair helper extensions must leave the
base and all Humanoid names intact. Reuse body/face topology and weight rules;
retain each character's locked proportions and identity.

## Facial shapes

25 shared names: Neutral, Smile, Happy, Suspicious, Angry, Surprised, Worried,
Thinking, Smirk, Sad, Blink_L, Blink_R, EyeLook_L, EyeLook_R, EyeLook_Up,
EyeLook_Down, Brow_Up, Brow_Down, Mouth_Open, Surprise, Mouth_A, Mouth_I,
Mouth_U, Mouth_E, Mouth_O. Neutral is the unchanged base. Surprise and Surprised
share one authored expression for user/guide naming compatibility. All eleven
requested active shapes contain actual vertex deltas at every LOD.

## FBX export and Humanoid import

All bpy operations must run through Blender MCP. Never use OS input or GUI
automation. The editable file is Z-up; export-only copies are converted to Y-up.

Export Armature/Mesh only; the filename supplies the Unity CHR_MATTO root.
Use scale 1, apply unit scale, FBX_SCALE_UNITS, -Z forward / Y up, no leaf bones,
no cameras/lights, no baked mesh modifiers, no animations in the model FBX.
The five animation takes are in separate `MATTO_PilotActions.fbx`.

**Bake axis conversion into temporary mesh/armature data with cancelling object
transforms.** Transform every shape key with its mesh. Restore editable Z-up
original data in a `finally` block. A correction left on the Animator root can
produce a valid Avatar while visibly collapsing the animated model. Verify
identity root rotation and the actual upright skinned-mesh height after animation.

Unity: globalScale=1, bakeAxisConversion=false (data already converted),
Humanoid/CreateFromThisModel, explicit HumanDescription, unique transform names,
importAnimation=false for model, Import normals, CalculateMikk tangents, four
bone skinning, no mesh compression, optimizeGameObjects=false. Animation FBX
copies the production Avatar and imports only the five authored takes.

## Runtime contract

Only PresentationCatalog.CharacterPrefabs[3] is replaced. Existing rules, AI,
UI, CharacterActor and Mansion geometry/data remain unchanged.

Animator integer `CharacterState` uses the existing enum. Idle, Move, Investigate,
Suspect and Deduction use the corresponding five pilot clips (Move uses Walk).
Cooperate temporarily reuses Investigate, UseAbility reuses Deduction, Win/Lose
reuse Idle with facial expression. Walk is .8 s; the other four are 2 s loops.

SharedHumanoidPresentation adapts visual walk speed, blinking/expressions and
foot IK. Navigation stays on the actor root. Matto's agent height is 1.65 m;
radius, speed, paths, costs and rules are unchanged. Idle facing uses the existing
AlignAtStand API to face the fixed camera. No input interception, cursor change,
camera redesign or UI changes are involved.

## Reproduction and evidence

- `build_matto.py`: author meshes, weights, atlas, facial shapes and three LODs.
- `animate_export_matto.py`: author actions and export model/animation FBX.
- `capture_blender.py`: real pose/face renders and finite skin checks.
- `make_review_sheets.py`: contact sheets from actual render evidence.
- `unity-import-results.txt`: imported tris, valid Avatar, human clips, catalog slot.
- `blender-pose-results.json`: sampled skin stability and render inventory.
- `Verification/play-results.txt`: actual Matto traversal, foot/mesh clearance,
  all 11 routes in both directions, shapes, regression and Restart.
- `Verification/runtime-axis-diagnostics.txt`: runtime root/bone positions.
- `Verification/Review`: directly inspected real Blender/Unity contact sheets.

Raw PNGs and recovery sessions are local and Git-ignored; regenerate them with
Blender MCP and the project-local Editor request API. Recovery files preserve
the user's previous Blender session. The Mansion source was not overwritten.
