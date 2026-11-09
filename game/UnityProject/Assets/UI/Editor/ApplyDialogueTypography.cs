using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Mystery.UI.Editor
{
    /// <summary>
    /// Applies the approved MaruBuri + Pretendard typography to the EXISTING DialoguePanel prefab.
    /// Runs in Edit Mode only. Does not enter Play Mode, test, create scenes, or build.
    /// A one-time import hook supports GitHub updates to an already-open mystery Unity project.
    /// </summary>
    [InitializeOnLoad]
    public static class ApplyDialogueTypography
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private const string FontsDir = "Assets/UI/Fonts/";
        private const string PretendardCommit = "7aeb0698819be2b4097dae8ec8fe6a795e5cf3ae";
        private const string PretendardBaseUrl = "https://raw.githubusercontent.com/orioncactus/pretendard/"
            + PretendardCommit + "/packages/pretendard/dist/public/static/";
        private const string MaruRegularBlob = "3b96955a9fc5b84f0ac04fddea7c4c93ae0b388a";
        private const string MaruSemiBoldBlob = "28871c1c55fe259f40e769cbbb6ae15a012bd5dd";
        private const string PretendardRegularBlob = "08bf4cfc2164a0bff74a4bf844128d3b843bbf87";
        private const string PretendardSemiBoldBlob = "e7e36abc474796c8b6c383380ddd9f87655682ec";

        private const string MaruRegularAsset = FontsDir + "MaruBuri-Regular SDF.asset";
        private const string MaruSemiBoldAsset = FontsDir + "MaruBuri-SemiBold SDF.asset";
        private const string PretendardRegularAsset = FontsDir + "Pretendard-Regular SDF.asset";
        private const string PretendardSemiBoldAsset = FontsDir + "Pretendard-SemiBold SDF.asset";

        private static bool attempted;

        static ApplyDialogueTypography()
        {
            // When the new C# script is imported by Unity, finish the font migration once
            // in the Editor (not on scene load or during game execution).
            EditorApplication.delayCall += ApplyOnImportOnce;
        }

        private static void ApplyOnImportOnce()
        {
            if (attempted) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ApplyOnImportOnce;
                return;
            }
            attempted = true;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            if (IsApplied()) return;
            try { ApplyInternal(); }
            catch (Exception ex)
            {
                Debug.LogWarning("[mystery fonts] Automatic Edit Mode font update could not finish: "
                    + ex.Message
                    + "\nRetry from Mystery/UI/Apply MaruBuri + Pretendard Fonts. No prefab was intentionally overwritten on failure.");
            }
        }

        [MenuItem("Mystery/UI/Apply MaruBuri + Pretendard Fonts")]
        public static void ApplyFromMenu()
        {
            ApplyInternal();
        }

        private static bool IsApplied()
        {
            var maruName = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MaruSemiBoldAsset);
            var maruBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MaruRegularAsset);
            var pretRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardRegularAsset);
            var pretSemi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardSemiBoldAsset);
            if (maruName == null || maruBody == null || pretRegular == null || pretSemi == null) return false;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return false;
            foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                TMP_FontAsset expected = RoleFont(text.gameObject.name, maruName, maruBody, pretRegular, pretSemi);
                if (expected != null && text.font != expected) return false;
            }
            return true;
        }

        private static void ApplyInternal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Typography can only be applied in Unity Edit Mode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new FileNotFoundException("Existing dialogue prefab not found", PrefabPath);

            // Never overwrite an altered font silently. Sources are pinned to immutable upstream Git blobs.
            EnsureLocalSource("MaruBuri-Regular.otf", MaruRegularBlob);
            EnsureLocalSource("MaruBuri-SemiBold.otf", MaruSemiBoldBlob);
            EnsurePretendardSource("Pretendard-Regular.otf", PretendardRegularBlob);
            EnsurePretendardSource("Pretendard-SemiBold.otf", PretendardSemiBoldBlob);

            TMP_FontAsset maruRegular = GetOrCreateFont("MaruBuri-Regular.otf", MaruRegularAsset);
            TMP_FontAsset maruSemi = GetOrCreateFont("MaruBuri-SemiBold.otf", MaruSemiBoldAsset);
            TMP_FontAsset pretendardRegular = GetOrCreateFont("Pretendard-Regular.otf", PretendardRegularAsset);
            TMP_FontAsset pretendardSemi = GetOrCreateFont("Pretendard-SemiBold.otf", PretendardSemiBoldAsset);

            // Prewarm visible Korean labels; Dynamic font assets remain capable of rendering user input.
            Warm(maruSemi, "마도연마리예나주은이복희진루나문일혜");
            Warm(maruRegular, "대사 미리보기입니다. 실제 NPC 대사는 별도로 연결합니다.");
            Warm(pretendardRegular, "궁금한 내용을 한국어로 자유롭게 입력해 주세요. 아직 대화 기록이 없습니다.");
            Warm(pretendardSemi, "이전 대화 기록 전송");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int changes = 0;
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    TMP_FontAsset desired = RoleFont(text.gameObject.name, maruSemi, maruRegular,
                        pretendardRegular, pretendardSemi);
                    if (desired == null) continue;
                    text.font = desired;
                    text.fontSharedMaterial = desired.material;
                    EditorUtility.SetDirty(text);
                    changes++;
                }

                // The prefab should retain its existing Image, alpha, RectTransforms, colors, and events.
                if (changes != 8)
                    throw new InvalidOperationException("Unexpected prefab text inventory: " + changes
                        + " (expected 8). Prefab left unchanged; inspect hierarchy.");

                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new IOException("Prefab save failed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            AssetDatabase.SaveAssets();
            Debug.Log("[mystery fonts] MaruBuri (NPC name + dialogue) and Pretendard "
                + "(input + history + button + status) assigned to existing prefab. Edit Mode only.");
        }

        private static TMP_FontAsset RoleFont(string objectName,
            TMP_FontAsset maruSemiBold, TMP_FontAsset maruRegular,
            TMP_FontAsset pretendardRegular, TMP_FontAsset pretendardSemiBold)
        {
            switch (objectName)
            {
                case "NpcName": return maruSemiBold;
                case "NpcDialogue": return maruRegular;
                case "Text":
                case "Placeholder":
                case "HistoryText":
                case "Status": return pretendardRegular;
                case "HistoryTitle":
                case "Label": return pretendardSemiBold;
                default: return null;
            }
        }

        private static void EnsureLocalSource(string filename, string expectedBlobSha)
        {
            string fullPath = Path.Combine(Application.dataPath, "UI", "Fonts", filename);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Required MaruBuri font was not imported from GitHub.", fullPath);
            VerifyGitBlob(File.ReadAllBytes(fullPath), expectedBlobSha, filename);
        }

        private static void EnsurePretendardSource(string filename, string expectedBlobSha)
        {
            string target = Path.Combine(Application.dataPath, "UI", "Fonts", filename);
            if (File.Exists(target))
            {
                VerifyGitBlob(File.ReadAllBytes(target), expectedBlobSha, filename);
                return;
            }

            byte[] data;
            using (var web = new WebClient())
            {
                web.Headers[HttpRequestHeader.UserAgent] = "mystery-unity-typography/1.0";
                // Upstream is the original Pretendard repository, pinned to a specific commit.
                data = web.DownloadData(PretendardBaseUrl + filename);
            }
            VerifyGitBlob(data, expectedBlobSha, filename);
            string temporary = target + ".download";
            try
            {
                File.WriteAllBytes(temporary, data);
                File.Move(temporary, target);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            AssetDatabase.ImportAsset(FontsDir + filename, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void VerifyGitBlob(byte[] content, string expected, string filename)
        {
            byte[] prefix = Encoding.ASCII.GetBytes("blob " + content.Length + "\0");
            byte[] payload = new byte[prefix.Length + content.Length];
            Buffer.BlockCopy(prefix, 0, payload, 0, prefix.Length);
            Buffer.BlockCopy(content, 0, payload, prefix.Length, content.Length);
            using (var sha = SHA1.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Font source hash mismatch; not replacing prefab: " + filename);
            }
        }

        private static TMP_FontAsset GetOrCreateFont(string filename, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontsDir + filename);
            if (source == null) throw new InvalidOperationException("Unity font importer not ready: " + filename);
            var created = TMP_FontAsset.CreateFontAsset(
                source, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            if (created == null) throw new InvalidOperationException("TMP font creation failed: " + filename);

            created.name = Path.GetFileNameWithoutExtension(filename) + " Dynamic SDF";
            AssetDatabase.CreateAsset(created, assetPath);
            AssetDatabase.AddObjectToAsset(created.material, created);
            foreach (var texture in created.atlasTextures)
                AssetDatabase.AddObjectToAsset(texture, created);
            EditorUtility.SetDirty(created);
            return created;
        }

        private static void Warm(TMP_FontAsset asset, string sample)
        {
            if (!asset.TryAddCharacters(sample, out string missing) && !string.IsNullOrEmpty(missing))
                Debug.LogWarning("[mystery fonts] Some sample glyphs were not available in "
                    + asset.name + ": " + missing);
            EditorUtility.SetDirty(asset);
        }
    }
}
