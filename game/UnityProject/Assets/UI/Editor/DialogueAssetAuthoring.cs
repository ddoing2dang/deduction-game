using System;
using System.IO;
using System.Linq;
using Mystery.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Mystery.UI.Editor
{
    /// <summary>Edit Mode asset authoring. Never enters Play Mode or invokes tests/builds.</summary>
    public static class DialogueAssetAuthoring
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private const string ScenePath = "Assets/Scenes/DialoguePreview.unity";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color32(47, 36, 45, 255);
        private static readonly Color Light = new Color32(246, 238, 220, 255);

        [MenuItem("Mystery/UI/Create Dialogue Preview Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            if (File.Exists(PrefabPath) || File.Exists(ScenePath))
                throw new InvalidOperationException("Output already exists; refusing to overwrite authored assets.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (TMP_Settings.instance == null) throw new InvalidOperationException("TMP Essential Resources must be imported first.");
            string[] filenames = { "Mansion", "Cruise", "MusicBroadcast" };
            var frames = new Sprite[3];
            for (int i = 0; i < filenames.Length; i++)
            {
                string path = "Assets/UI/Art/DialogueFrames/" + filenames[i] + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing original image: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                standalone.overridden = true;
                standalone.maxTextureSize = 2048;
                standalone.format = TextureImporterFormat.RGBA32;
                standalone.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(standalone);
                importer.spriteBorder = Vector4.zero;
                importer.SaveAndReimport();
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null || frames[i].texture.width != 2048 || frames[i].texture.height != 682)
                    throw new InvalidOperationException("Original frame dimensions were not retained.");
            }
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/NotoSansKR-Regular.otf");
            string fontPath = "Assets/UI/Fonts/NotoSansKR SDF.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(sourceFont, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (font == null) throw new InvalidOperationException("Korean TMP font creation failed.");
                font.name = "NotoSansKR Dynamic SDF";
                AssetDatabase.CreateAsset(font, fontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            }

            Scene original = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(25, 49, 58, 255);
                camera.orthographic = true;
                cameraObject.AddComponent<UniversalAdditionalCameraData>();

                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                var root = Rect("DialoguePanel", canvasObject.transform, new Vector2(0, 24), new Vector2(1800, 1032));
                root.anchorMin = root.anchorMax = new Vector2(.5f, 0);
                root.pivot = new Vector2(.5f, 0);
                var panel = root.gameObject.AddComponent<DialoguePanel>();

                // Original 2048:682 rectangle. No opaque image is placed behind this frame.
                var frameRect = Rect("Frame", root, new Vector2(0, 410), new Vector2(1800, 599.4140625f));
                BottomCoordinates(frameRect);
                var frame = frameRect.gameObject.AddComponent<Image>();
                frame.sprite = frames[0]; frame.color = Color.white;
                frame.type = Image.Type.Simple; frame.preserveAspect = true; frame.raycastTarget = false;
                var name = Text("NpcName", frameRect, new Vector2(-590, 150), new Vector2(390, 68), "마도연", 36, Ink);
                name.alignment = TextAlignmentOptions.Center;
                var body = Text("NpcDialogue", frameRect, new Vector2(0, -38), new Vector2(1560, 224), "", 32, Ink);
                body.alignment = TextAlignmentOptions.TopLeft;

                var inputRect = Rect("QuestionInput", root, new Vector2(-105, 50), new Vector2(1540, 64));
                BottomCoordinates(inputRect);
                inputRect.gameObject.AddComponent<Image>().color = new Color32(248, 243, 234, 245);
                var textArea = Rect("Text Area", inputRect, Vector2.zero, new Vector2(1492, 58));
                textArea.gameObject.AddComponent<RectMask2D>();
                var text = Text("Text", textArea, Vector2.zero, new Vector2(1492, 58), "", 26, Ink);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                var placeholder = Text("Placeholder", textArea, Vector2.zero, new Vector2(1492, 58), "궁금한 내용을 한국어로 자유롭게 입력해 주세요.", 26, new Color32(111, 103, 108, 255));
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
                var input = inputRect.gameObject.AddComponent<TMP_InputField>();
                input.textViewport = textArea; input.textComponent = text; input.placeholder = placeholder;
                input.lineType = TMP_InputField.LineType.SingleLine;
                input.characterLimit = 1000;
                input.selectionColor = new Color32(184, 150, 111, 130);
                var sendRect = Rect("SendButton", root, new Vector2(790, 50), new Vector2(180, 64));
                BottomCoordinates(sendRect);
                sendRect.gameObject.AddComponent<Image>().color = new Color32(92, 58, 66, 255);
                var send = sendRect.gameObject.AddComponent<Button>();
                var sendText = Text("Label", sendRect, Vector2.zero, new Vector2(174, 58), "전송", 26, Light);
                sendText.alignment = TextAlignmentOptions.Center;
                var status = Text("Status", root, new Vector2(0, 100), new Vector2(1730, 30), "질문을 입력한 뒤 전송해 주세요.", 20, Light);
                BottomCoordinates(status.rectTransform);

                var historyTitle = Text("HistoryTitle", root, new Vector2(-700, 970), new Vector2(360, 40), "이전 대화 기록", 26, Light);
                BottomCoordinates(historyTitle.rectTransform);
                var historyRect = Rect("DialogueHistory", root, new Vector2(0, 838), new Vector2(1760, 212));
                BottomCoordinates(historyRect);
                historyRect.gameObject.AddComponent<Image>().color = new Color32(12, 24, 31, 145);
                var scroll = historyRect.gameObject.AddComponent<ScrollRect>();
                var viewport = Rect("Viewport", historyRect, new Vector2(-8, 0), new Vector2(1706, 190));
                viewport.gameObject.AddComponent<RectMask2D>();
                var content = Rect("Content", viewport, Vector2.zero, new Vector2(1706, 190));
                content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 190); content.anchoredPosition = Vector2.zero;
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlHeight = layout.childControlWidth = true;
                layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var history = Text("HistoryText", content, Vector2.zero, new Vector2(1706, 190), "아직 대화 기록이 없습니다.", 24, Light);
                history.alignment = TextAlignmentOptions.TopLeft;
                scroll.viewport = viewport; scroll.content = content;
                scroll.horizontal = false; scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
                var barRect = Rect("Scrollbar", historyRect, new Vector2(858, 0), new Vector2(12, 184));
                barRect.gameObject.AddComponent<Image>().color = new Color32(255, 255, 255, 25);
                var bar = barRect.gameObject.AddComponent<Scrollbar>();
                bar.direction = Scrollbar.Direction.BottomToTop;
                var handle = Rect("Handle", barRect, Vector2.zero, new Vector2(12, 30));
                var handleImage = handle.gameObject.AddComponent<Image>();
                handleImage.color = new Color32(211, 184, 140, 200);
                bar.handleRect = handle; bar.targetGraphic = handleImage;
                scroll.verticalScrollbar = bar;

                var fields = new SerializedObject(panel);
                Set(fields, "mansionFrame", frames[0]); Set(fields, "cruiseFrame", frames[1]); Set(fields, "musicBroadcastFrame", frames[2]);
                Set(fields, "frame", frame); Set(fields, "speakerName", name); Set(fields, "dialogueBody", body);
                Set(fields, "questionInput", input); Set(fields, "sendButton", send); Set(fields, "historyScroll", scroll);
                Set(fields, "historyText", history); Set(fields, "statusText", status);
                fields.ApplyModifiedPropertiesWithoutUndo();
                panel.ApplyPresentation();
                string glyphs = string.Concat(root.GetComponentsInChildren<TMP_Text>().Select(t => t.text))
                    + string.Concat(Enum.GetNames(typeof(DialogueSpeaker))) + "탐정 · 질문을 기록했습니다.";
                font.TryAddCharacters(glyphs, out string missing);
                if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("Missing UI glyphs: " + missing);
                EditorUtility.SetDirty(font);
                if (PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath) == null) throw new IOException("Prefab save failed.");
                UnityEngine.Object.DestroyImmediate(root.gameObject);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvasObject.transform);

                var events = new GameObject("EventSystem", typeof(EventSystem));
                var ime = events.AddComponent<DialogueImeInput>();
                var module = events.AddComponent<InputSystemUIInputModule>();
                module.inputOverride = ime;
                module.AssignDefaultActions();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Preview scene save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("MYSTERY_DIALOGUE_ASSETS_CREATED — Edit Mode authoring only; no tests, Play Mode, or builds.");
            }
            finally
            {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Set(SerializedObject target, string name, UnityEngine.Object value) => target.FindProperty(name).objectReferenceValue = value;
        private static void BottomCoordinates(RectTransform rect) => rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float pointSize, Color color)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = pointSize; label.color = color; label.text = value;
            label.richText = false; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
