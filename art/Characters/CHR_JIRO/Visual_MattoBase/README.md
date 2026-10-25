# CHR_JIRO — Matto clean-base appearance candidate

Status: Blender visual review complete; **user appearance approval pending**.
Active file: `CHR_JIRO.blend` in this folder.
Reference: `../CHR_JIRO_Reference.png` (green twin tails / gold eyes only).

## Source and boundaries
Approved `CHR_MATTO/Game/CHR_MATTO_GAME.blend` supplies only clean body/hand/foot topology, basic face topology and expression structure, Humanoid skeleton, metric scale, and toon/material structure. Source hashes are recorded in `source-contract.json` and `comparison-source-audit.json`; approved files remain unchanged.
Rejected Jiro Visual/Archive files and scripts were not read or reused. Existing rejected files remain rejected backups. The latest user instruction supersedes the former request to wait for a new VRoid base.
No Unity work, production FBX export, LOD creation, other characters, or game-code changes were performed.

## Appearance construction
- Face: independently reshaped cheek/jaw contour, shorter lower face, rounder/livelier eyes, lifted brows, reduced nose and small upturned mouth; all existing expression keys transformed consistently. Own gold iris, pupil, highlights and brow paint. Clean 4,280-vertex face topology retained. Redundant disabled highlight/eyelash overlay polygons removed; no decimation.
- Hair: scalp-sampled crown; six uneven bangs; temple locks and part sweeps; ahoge; ten new flattened, bevelled anime leaf clumps forming two curved twin tails. Roots are narrow, middle sections broaden, pointed ends follow distinct S curves. No circular tube tails, subdivision or rejected hair data. White five-petal flowers and dark ribbons have fitted mounting pins.
- Outfit: new fitted white blouse/puff sleeves, green neck bow, dropped gathered charcoal sleeves/cardigan, green/black pleated skirt, waist/front and rear ribbons, thigh strap, asymmetric socks and dark buckle loafers. Collar front/back thickness was rebuilt; socks were fitted to actual leg skin. A face/neck boundary gap found in close-up was closed by fitting 13 neck boundary vertices, with a maximum 6.42 mm adjustment.

## Counts and compatibility
Raw and evaluated geometry: **35,922 tris**. Body 3,900; face 7,090; hair 12,354; outfit 11,214; accessories 732; shoes 632.
One armature, 108 bones (approved 102-bone structure plus six authored tail bones); all 54 source Humanoid mappings exist. This is a Blender structure check, not a new Unity Avatar validation.
25 expression aliases plus Basis; required 11 keys are nonzero and rendered. 111 mesh objects remain separate for appearance editing; merge/LOD/performance finalization belongs to the later approved game preparation stage.
Units: 1 Blender unit = 1 m; Z up / -Y front. Body and rig uniformly 0.96 of the approved Matto base; final object scale is 1.
Hair root-to-skin maximum measured distance: 1.5001 mm. No SUBSURF or DECIMATE modifiers.

## Directly reviewed images
1. [Front](Review/01_Front.png)
2. [Three-quarter](Review/02_ThreeQuarter.png)
3. [Exact 90-degree side](Review/03_Side90.png)
4. [Back](Review/04_Back.png)
5. [Front face](Review/05_FaceFront.png)
6. [Three-quarter face](Review/06_FaceThreeQuarter.png)
7. [Twin-tail root close-up](Review/07_TwinRoot_Close.png)
8. [Head side close-up](Review/08_Head_Side.png)
9. [Reference comparison](Review/09_Reference_Comparison.png)
10. [Actual Matto / Naz / Jiro side by side](Review/10_MATTO_NAZ_JIRO.png)

[Eight-view sheet](Review/00_Review_ContactSheet.png), [expression sheet](Review/11_Expression_ContactSheet.png), [three faces](Review/12_Faces_MATTO_NAZ_JIRO.png).
Expression sheet order, left to right/top to bottom: Neutral, Blink_L, Blink_R, Smile; Angry, Sad, Surprise, Mouth_A; Mouth_I, Mouth_U, Mouth_E, Mouth_O.
Comparison characters are temporary read-only imports. They were discarded by reopening this Jiro file after rendering, and their source hashes match.

`final-audit.json` records structural checks and the direct visual review scope. The requested OK labels describe the agent's Blender review only. **They do not record user appearance approval.** Stop here until the user approves the new candidate.
