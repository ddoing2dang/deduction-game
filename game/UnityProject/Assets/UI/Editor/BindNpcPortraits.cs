using System;
using System.Linq;
using Mystery.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.UI.Editor
{
    // Applies user-supplied transparent PNGs to the existing DialoguePanel prefab.
    // Edit Mode only. No Play Mode, test, scene generation, or build.
    [InitializeOnLoad]
    public static class BindNpcPortraits
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private const string PortraitFolder = "Assets/UI/Art/NPCPortraits/";
        // DialogueSpeaker indices are stable: 0 마도연, 1 마루희, 2 나주은,
        // 3 이복희, 4 진루나, 5 문일혜.
        private static readonly string[] PortraitFiles =
        {
            "Madoyeon.png", "Maruhee.png", "Najueun.png",
            "Ibokhee.png", "Jinruna.png", "Munilhye.png"
        };

        static BindNpcPortraits() { EditorApplication.delayCall += TryAutoBind; }

        private static void TryAutoBind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBind;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            if (PortraitFiles.Any(f => AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolder + f) == null))
                return;
            try { Bind(); }
            catch (Exception e) { Debug.LogWarning("[mystery portraits] " + e.Message); }
        }

        [MenuItem("Mystery/UI/Bind Six NPC Portraits")]
        public static void Bind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Only Unity Edit Mode is allowed.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("Current mystery DialoguePanel prefab is missing.");

            var sprites = new Sprite[PortraitFiles.Length];
            for (int i = 0; i < PortraitFiles.Length; i++)
            {
                string path = PortraitFolder + PortraitFiles[i];
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing portrait: " + path);

                bool adjust = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.alphaIsTransparency || importer.mipmapEnabled
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.maxTextureSize < 2048;
                if (adjust)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null) throw new InvalidOperationException("Sprite could not be imported: " + path);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            bool changed = false;
            try
            {
                var panel = root.GetComponent<DialoguePanel>();
                if (panel == null)
                    throw new InvalidOperationException("Import the current DialoguePanel.cs before binding.");
                var frame = root.transform.Find("Frame");
                if (frame == null) throw new InvalidOperationException("Original frame missing.");

                var holder = frame.Find("NpcPortrait");
                Image image;
                if (holder == null)
                {
                    var go = new GameObject("NpcPortrait",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(frame, false);
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition = new Vector2(685, -40);
                    rect.sizeDelta = new Vector2(200, 200);
                    image = go.GetComponent<Image>();
                    image.enabled = false;
                    changed = true;
                }
                else
                {
                    image = holder.GetComponent<Image>();
                    if (image == null)
                        throw new InvalidOperationException("Existing NpcPortrait is not a UI Image.");
                }
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;

                var serialized = new SerializedObject(panel);
                var imageField = serialized.FindProperty("npcPortrait");
                var entries = serialized.FindProperty("speakerPortraits");
                if (imageField == null || entries == null)
                    throw new InvalidOperationException("Prefab code lacks six portrait mappings.");
                if (imageField.objectReferenceValue != image)
                {
                    imageField.objectReferenceValue = image;
                    changed = true;
                }
                if (entries.arraySize != PortraitFiles.Length)
                {
                    entries.arraySize = PortraitFiles.Length;
                    changed = true;
                }
                for (int i = 0; i < PortraitFiles.Length; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var speaker = entry.FindPropertyRelative("speaker");
                    var portrait = entry.FindPropertyRelative("portrait");
                    if (speaker.enumValueIndex != i || portrait.objectReferenceValue != sprites[i])
                    {
                        speaker.enumValueIndex = i;
                        portrait.objectReferenceValue = sprites[i];
                        changed = true;
                    }
                }
                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    panel.ApplyPresentation();
                    if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                        throw new InvalidOperationException("Could not save DialoguePanel.");
                    AssetDatabase.SaveAssets();
                    Debug.Log("[mystery] All six original portraits bound in Edit Mode. No tests or builds.");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    public sealed class NpcPortraitImportHook : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!importedAssets.Any(p => p.StartsWith("Assets/UI/Art/NPCPortraits/",
                    StringComparison.Ordinal) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                return;
            EditorApplication.delayCall += BindWhenReady;
        }

        private static void BindWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = "Assets/UI/Art/NPCPortraits/";
            foreach (string file in new[]
            {
                "Madoyeon.png", "Maruhee.png", "Najueun.png",
                "Ibokhee.png", "Jinruna.png", "Munilhye.png"
            })
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(root + file) == null) return;
            }
            try { BindNpcPortraits.Bind(); }
            catch (Exception e) { Debug.LogWarning("[mystery portraits] " + e.Message); }
        }
    }
}
