# mystery dialogue typography — MaruBuri + Pretendard

## Roles applied to the existing `Assets/UI/Prefabs/DialoguePanel.prefab`
- `NpcName`: **MaruBuri SemiBold**
- `NpcDialogue`: **MaruBuri Regular**
- `Text` (TMP_InputField), `Placeholder`, `HistoryText`, `Status`: **Pretendard Regular**
- `HistoryTitle` and SendButton's `Label`: **Pretendard SemiBold**

The original themed PNGs, Canvas, positioning, colors, events and Input System settings are unchanged.

## Applying on an already-open Unity project
The GitHub branch contains `Assets/UI/Editor/ApplyDialogueTypography.cs` and the **actual original OTF font bytes for MaruBuri Regular + SemiBold**. After updating the `mystery` project with this branch, Unity's Editor imports the script and **tries the one-time font migration automatically in Edit Mode**.

If network access prevents the migration, run **Mystery > UI > Apply MaruBuri + Pretendard Fonts** once after Internet is available. The script downloads **Pretendard Regular + SemiBold directly from the original author**, pinned to commit `7aeb0698819be2b4097dae8ec8fe6a795e5cf3ae`, verifies their Git blob hashes, creates four dynamic TMP SDF assets under this folder, and saves the existing prefab with all eight text components assigned to their new fonts.

Pretendard OTF files are **not yet GitHub-committed** because GitHub's connected text-file interface does not retrieve original OTF binary files larger than 1 MB; the editor installer downloads them instead. **After Unity runs the installer, commit the generated Pretendard OTFs, TMP SDF assets and their `.meta` files from the local `mystery` project to make the branch entirely self-contained.**

This change was prepared against the GitHub files. It was not executed in the user's local Unity editor from this conversation. No Play Mode, function testing, or executable build was run.

## Sources and licensing
- **MaruBuri**, copyright NAVER Corporation and NAVER Cultural Foundation: https://hangeul.naver.com/font and https://help.naver.com/service/30016/contents/18088?osType=PC
  - The fonts may be embedded and redistributed under NAVER's open font license. The font files must not be sold alone.
  - Source font mirror, file hashes verified against repository blobs: https://github.com/fonts-archive/MaruBuri
- **Pretendard**, copyright Kil Hyung-jin and contributors, SIL Open Font License 1.1: https://github.com/orioncactus/pretendard
  - Exact license text is included as `Pretendard-OFL.txt`.

## Verification boundaries
The GitHub code and font import assets can be reviewed statically. **Actual TMP font generation and prefab reserialization take place inside Unity Editor** when this change is imported; this response does not claim they already ran on your PC.
