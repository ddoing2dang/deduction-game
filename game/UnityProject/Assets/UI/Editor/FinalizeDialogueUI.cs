using System;
using System.IO;
using Mystery.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.UI.Editor
{
    // One-time, additive prefab edit; does not create or touch a game map or a scene.
    // Does not run Play Mode, Unity tests, or a player build.
    [InitializeOnLoad]
    public static class FinalizeDialogueUI
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private static bool attempted;

        static FinalizeDialogueUI()
        {
            EditorApplication.delayCall += TryFinalizeAfterImport;
        }

        private static void TryFinalizeAfterImport()
        {
            if (attempted) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryFinalizeAfterImport;
                return;
            }
            attempted = true;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // No-op if a prefab is not yet imported, or if its portrait reference is in place.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            try { FinalizePrefab(); }
            catch (Exception error)
            {
                Debug.LogWarning("[mystery UI] Optional portrait slot was not applied automatically: "
                    + error.Message + "\nRun Mystery/UI/Finalize Dialogue Panel (Edit Mode) to retry.");
            }
        }

        [MenuItem("Mystery/UI/Finalize Dialogue Panel (Edit Mode)")]
        public static void FinalizePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Only allowed in Edit Mode.");

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null) throw new FileNotFoundException(PrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            bool modified = false;
            try
            {
                var panel = root.GetComponent<DialoguePanel>();
                if (panel == null)
                    throw new InvalidOperationException("Existing DialoguePanel script not found.");

                Transform frame = root.transform.Find("Frame");
                if (frame == null)
                    throw new InvalidOperationException("Original Frame child not found. Prefab left unchanged.");

                Transform portrait = frame.Find("NpcPortrait");
                Image portraitImage;
                if (portrait == null)
                {
                    var child = new GameObject("NpcPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    child.transform.SetParent(frame, false);
                    var rect = child.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition = new Vector2(685, -40);
                    rect.sizeDelta = new Vector2(200, 200);
                    portraitImage = child.GetComponent<Image>();
                    portraitImage.color = Color.white;
                    portraitImage.type = Image.Type.Simple;
                    portraitImage.preserveAspect = true;
                    portraitImage.raycastTarget = false;
                    portraitImage.enabled = false;
                    modified = true;
                }
                else
                {
                    portraitImage = portrait.GetComponent<Image>();
                    if (portraitImage == null)
                        throw new InvalidOperationException("NpcPortrait exists but is not an Image; prefab unchanged.");
                }

                var serialized = new SerializedObject(panel);
                var slot = serialized.FindProperty("npcPortrait");
                var bindings = serialized.FindProperty("speakerPortraits");
                if (slot == null || bindings == null)
                    throw new InvalidOperationException("Updated DialoguePanel.cs must compile first.");

                if (slot.objectReferenceValue != portraitImage)
                {
                    slot.objectReferenceValue = portraitImage;
                    modified = true;
                }

                if (bindings.arraySize == 0)
                {
                    bindings.arraySize = Enum.GetValues(typeof(DialogueSpeaker)).Length;
                    for (int i = 0; i < bindings.arraySize; i++)
                    {
                        var element = bindings.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("speaker").enumValueIndex = i;
                        element.FindPropertyRelative("portrait").objectReferenceValue = null;
                    }
                    modified = true;
                }
                if (modified)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    panel.ApplyPresentation();
                    if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                        throw new IOException("Unable to save the prefab.");
                    AssetDatabase.SaveAssets();
                    Debug.Log("[mystery UI] DialoguePanel portrait slot and six NPC bindings added. "
                        + "Original frame and all interactive UI elements preserved. No Play Mode/tests/builds.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
