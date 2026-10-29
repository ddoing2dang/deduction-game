# CHR_MARIHI production candidate

Base game / Mansion commit: 0425d680bbb8802e7e46598900f19c69340e3b57.
Branch: codex/chr-marihi-production. User design approval is pending.

The authoritative reference is ../CHR_MARIHI_Reference.png, not a generated concept image. It specifies warm lightly tanned skin, blonde wavy twin tails, black bows, amber eyes, an ivory blouse, dropped charcoal sleeves, gold-accented pleats and asymmetric ankle socks.

Only clean body/face topology, expressions and Humanoid technology may come from the approved Matto. Finished Matto/Jiro hair, garments and their source files must remain unchanged. Naz Visual and Game Blender files exist locally and are protected with the other characters.

Blender authoring is through MCP only. Unity integration is restricted to PresentationCatalog slot 2. No Mansion geometry, navigation, rules, AI, UI or other character authoring is permitted. Final design approval and merging to main remain explicitly outside this pass.

`protected-files-before.json` records the existing source/runtime assets before authoring. `catalog-before.asset.txt` preserves the user's local catalog, including its uncommitted Naz slot. These records are not proof of completed model, visual or runtime validation.

Status: playable design-review candidate. **User appearance approval is pending; no final design approval or main merge is recorded.** See FINAL_REVIEW.md for the final runtime and direct visual results.

## Saved assets and appearance

Editable production source: `CHR_MARIHI.blend`; game copy: `Game/CHR_MARIHI_GAME.blend`. Both contain the final LOD meshes and hidden, individually editable construction meshes in `Approved_Source_Archive`. That inherited collection name denotes the pipeline role, not user approval of Marihi.

Runtime model: `game/UnityProject/Assets/_Game/Characters/CHR_MARIHI/Model/CHR_MARIHI.fbx`, relative to repository root; prefab is in the sibling Prefabs folder. Only Matto's clean body/hand/foot and basic face topology, skeleton technology and expression interface were reused. Face coordinates were independently reshaped across all expressions: tapered lower jaw, high cheek, more hooded/upturned eyes and a small confident mouth. Amber iris paint and a warm light tan distinguish Marihi from Matto. Finished Matto/Jiro hair or garment objects were not cloned.

New hair: fitted scalp grid, eight swept fringe clumps, layered blonde S-wave twin tails, cheek locks, one cowlick and two fitted black bows. No flowers. The final crown uses only the current body/face coordinate basis; redundant overlapping surfaces were removed and adjacent fringe clumps have distinct shallow layer offsets. New outfit: ivory blouse/collar, narrow black/gold tie, fitted black bodice side panels, gold piping, charcoal jacquard sleeves with ribbon accents, black/gold pleats, belt buckle, heart charm, thigh strap, asymmetric ankle socks and platform loafers. Pattern/loft utilities are shared technology; these are newly authored meshes, not recoloured finished character objects.

## Runtime contract

| Item | Value |
|---|---:|
| Unity LOD0 | 30,679 tris |
| Unity LOD1 | 18,094 tris / 58.98% |
| Unity LOD2 | 9,092 tris / 29.64% |
| Blender LOD0/1/2 | 30,703 / 18,118 / 9,116 tris |
| Skeleton | 69 bones; 54 explicit Humanoid mappings |
| Renderers | 21 stored; 7 active per LOD |
| Materials / textures | 9 / 6; maximum 2048 |

Unity removes 24/24/24 degenerate triangles on import. Initial construction was 37,389 tris before subsequent tailoring/crown corrections. No whole-character decimation: face and hands retain the clean LOD0 topology; lower face LODs interpolate all expression deltas. Small pleat and ornament detail simplifies in far LODs.

1 BU = 1 m; approximately 1.65 m including the small cowlick (measured rest geometry: 1.647 m). Blender Z up / -Y front; Unity Y up / +Z front, model rotation identity, scale one. Rest T-pose is retained. Core names remain J_Bip_*; six MARIHI_Tail_* and four MARIHI_Skirt_* helpers are character-specific. `Game/vroid-human-map.json` records all 54 mappings. Unused copied hair/cloth branches were pruned only in this character. Fingers remain.

Weights are normalized, maximum four influences. Unity removes unused renderer binding clusters without deleting skeleton transforms. Body binding palette <=56; face one Head bone, with facial/eye-look motion through blendshapes; hair seven. The production FBX imports no animation, cameras, lights or colliders.

25 shared expression aliases plus Basis exist on every face LOD. Required nonzero keys: Blink_L, Blink_R, Smile, Angry, Sad, Surprise, Mouth_A, Mouth_I, Mouth_U, Mouth_E, Mouth_O. Neutral is zero. Textures: face/body 2048², hair 512×1024, skirt 1024², sleeves 512², linear palette 128². Android uses ASTC4x4 except the palette; no device FPS benchmark is claimed.

## Integration and evidence

MarihiGamePipeline updates PresentationCatalog **slot 2 only**, creates a valid Humanoid Avatar, nine-state Animator, LODGroup and prefab. The user's pre-existing local Naz slot is preserved and is not introduced into this commit. `catalog-before.asset.txt` and `catalog-preservation.json` record that distinction.

The existing Matto Humanoid library is read without editing its files: Idle, Walk, Investigate, Suspect, Deduction. Cooperate reuses Investigate; UseAbility reuses Deduction; Win/Lose reuse Idle with expressions. These are representative compatible motions, not a new final eighteen-clip set. A Marihi-only visual adapter supplies conservative hair sway, waist cloth follow, blink/expressions and foot IK. Navigation and game rules remain on the unchanged CharacterActor.

Blender captures cover front/45°/exact side/back/face/root, expressions, T/idle/walk, arms raised/forward, knee/sitting bends and three LODs. Unity runtime evidence covers nine rooms × three zooms, 54 StandPoints, nine ActionPoints, nine Animator states, eleven routes in both directions, both stairs, support/mesh clearance, bounded turning, Restart, UI and 2,461 existing single-player assertions. A focused camera pass additionally checks GameplayPresentation.SourceId == 2, Marihi's actual Deduction Animator, the existing Visual Gold side view and return. The complete traversal log is retained separately.

`Game/Verification/play-results.txt` and `camera-results.txt` are runtime evidence. Terminal success alone is not visual approval. `Review/` contains contact sheets from actual Blender and Camera.main renders. All required single views use the same finished game model. The reference sheet is labelled separately; no AI concept is presented as a model render. The final review records which current images were directly inspected.

Existing Naz Visual/Game files were found and preserved. `protected-files-check.json` verifies 3,758 existing Mansion/character files against the starting hashes. The Mansion scene, graph, rules, AI, UI and other characters are not authored by this pass.

Blender uses MCP only; Unity uses project-local Editor/runtime APIs and `C:/UnityProjects/DeductionGame/marihi-game.request` (Refresh, Setup, Audit, CameraAudit). No OS input, cursor control or GUI automation. Construction scripts are sequential development checkpoints, not commands to replay blindly over the finalized file. Export and capture operate on the final game file. Local backups and .blend1 files are excluded from the deliverable commit.

Blender and Unity lighting differ. Far LODs simplify small hair/pleat/ornament detail; Android device performance and final user design approval are not claimed.
