using System;
using Mystery.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.UI.Editor
{
    /// <summary>
    /// Edit Mode only: connect the six user-approved HOTEL portraits without
    /// changing the legacy/shared portrait set used by the other themes.
    /// No scene, case logic, Play Mode, tests or builds are touched here.
    /// </summary>
    [InitializeOnLoad]
    public static class BindRoseMansionPortraits
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private const string Folder = "Assets/UI/Art/NPCPortraits/RoseMansion/";

        // The old DialogueSpeaker enum's numeric order is a serialized contract.
        // MATTO(0), MARIHI(1), NAZ(2), BOKI(3), JJIRO(4), MOONIRE(5).
        private static readonly string[] Filenames =
        {
            "MATTO.png", "MARIHI.png", "NAZ.png",
            "BOKI.png", "JJIRO.png", "MOONIRE.png"
        };

        static BindRoseMansionPortraits()
        {
            EditorApplication.delayCall += TryAutoBind;
        }

        internal static void TryAutoBind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBind;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            foreach (string filename in Filenames)
                if (!(AssetImporter.GetAtPath(Folder + filename) is TextureImporter))
                    return;

            try { Bind(); }
            catch (Exception e)
            {
                Debug.LogWarning("[RoseMansion portraits] Binding skipped: " + e.Message);
            }
        }

        [MenuItem("Mystery/UI/Bind Rose Mansion Six Portraits (Edit Mode)")]
        public static void Bind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Rose Mansion binding requires Edit Mode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("DialoguePanel prefab missing: " + PrefabPath);

            var sprites = new Sprite[Filenames.Length];
            for (int i = 0; i < Filenames.Length; i++)
            {
                string path = Folder + Filenames[i];
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException("Missing user-approved portrait: " + path);

                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                int resolution = Mathf.Max(2048,
                    Mathf.NextPowerOfTwo(Mathf.Max(width, height)));
                bool mustReimport = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.alphaIsTransparency || importer.mipmapEnabled
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.maxTextureSize < resolution;
                if (mustReimport)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.crunchedCompression = false;
                    importer.maxTextureSize = Mathf.Max(resolution, importer.maxTextureSize);
                    importer.SaveAndReimport();
                }
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null)
                    throw new InvalidOperationException("Sprite import failed: " + path);
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = root.GetComponent<DialoguePanel>();
                if (panel == null) throw new InvalidOperationException("DialoguePanel component missing.");
                var data = new SerializedObject(panel);
                var entries = data.FindProperty("mansionPortraits");
                if (entries == null)
                    throw new InvalidOperationException("DialoguePanel needs mansionPortraits field.");

                bool dirty = entries.arraySize != Filenames.Length;
                if (dirty) entries.arraySize = Filenames.Length;

                for (int i = 0; i < Filenames.Length; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var speaker = entry.FindPropertyRelative("speaker");
                    var portrait = entry.FindPropertyRelative("portrait");
                    if (speaker.enumValueIndex == i && portrait.objectReferenceValue == sprites[i])
                        continue;
                    speaker.enumValueIndex = i;
                    portrait.objectReferenceValue = sprites[i];
                    dirty = true;
                }

                if (dirty)
                {
                    data.ApplyModifiedPropertiesWithoutUndo();
                    if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                        throw new InvalidOperationException("Failed to save DialoguePanel prefab.");
                    AssetDatabase.SaveAssets();
                    Debug.Log("[RoseMansion portraits] Six approved HOTEL sprites bound. "
                        + "Other themes remain unchanged; no Play Mode, tests, or builds.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    public sealed class RoseMansionPortraitImportHook : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!path.StartsWith("Assets/UI/Art/NPCPortraits/RoseMansion/",
                        StringComparison.Ordinal)
                    || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;
                EditorApplication.delayCall += BindRoseMansionPortraits.TryAutoBind;
                return;
            }
        }
    }
}
