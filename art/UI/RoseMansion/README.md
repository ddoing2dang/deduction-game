# Case464 · Rose Mansion visual asset library

The UI art created in this conversation is categorized and stored by purpose. **Approved/active images do not overwrite existing Unity assets.** Keep the older images as references without automatically connecting them to live UI.

## Existing official image (not moved)
- `game/UnityProject/Assets/UI/Art/Intro/RoseMansion_Intro_Title.png` — approved Rose Mansion intro artwork. Preserve its original Unity `.meta` GUID.

## Active / Unity import locations
- `game/UnityProject/Assets/UI/Art/RoseMansion/Investigation/` — colored + disabled magnifying-glass icons.
- `game/UnityProject/Assets/UI/Art/RoseMansion/Suspicion/` — empty dialogue frame, three-row parchment question panel, suspicion title.
- `game/UnityProject/Assets/UI/Art/RoseMansion/Navigation/` — back button.

## References / non-runtime
- `art/UI/RoseMansion/References/` — previous versions and alternate designs, kept out of the runtime Unity Assets folder to prevent accidental use.

## Rules
- Only the three approved questions: alibi, relationship to victim, suspected-person hint. Render question labels with Unity TMP over the blank paper art, not baked into PNG.
- A regular visit permits one question; the extra-question item permits one additional selection. Back before answer confirmation must not spend a question.
- Dialogue name and sentence are runtime UI text; `Dialogue_Frame_Empty.png` intentionally contains neither.
- All images are PNG RGBA with transparent outer canvas. The parchment body is semi-transparent.
- This commit stores source images only. No Unity Play Mode, functionality test, rebuild, or scene/prefab modification is claimed.
- The old dialogue art in `art/UI/DialogueFrames/Translucent/` is preserved as-is.

## Catalog
See `art/UI/RoseMansion/asset_manifest.json` for image dimensions, SHA-256 checksums, sizes, and classification.