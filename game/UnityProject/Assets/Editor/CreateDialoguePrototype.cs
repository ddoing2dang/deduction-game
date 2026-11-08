using System;
using System.IO;
using System.Linq;
using Deduction.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class CreateDialoguePrototype
{
    private static TMP_FontAsset font;
    private static readonly Color Ink = new Color32(44, 31, 39, 255);
    private static readonly Color Cream = new Color32(244, 234, 214, 255);

    [MenuItem("Deduction/Create Dialogue Prototype")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        if (File.Exists("Assets/Scenes/DialoguePrototype.unity") || File.Exists("Assets/Prefabs/DialoguePanel.prefab"))
            throw new InvalidOperationException("Prototype already exists; refusing to overwrite edited assets.");
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            throw new InvalidOperationException("Import TMP Essential Resources before running this Edit Mode authoring command.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NotoSansKR.ttf");
        font = TMP_FontAsset.CreateFontAsset(sourceFont, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        if (font == null) throw new InvalidOperationException("Korean font import failed.");
        font.name = "NotoSansKR Dynamic SDF";
        AssetDatabase.CreateAsset(font, "Assets/Art/Fonts/NotoSansKR SDF.asset");
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);

        var profilesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/UI/NpcNames.json");
        var profiles = JsonUtility.FromJson<NpcProfiles>(profilesJson.text);
        var frames = new DialoguePrototypeView.ThemeFrame[3];
        string[] ids = { "HOTEL", "CRUISE", "MUSIC_BROADCAST" };
        string[] files = { "Mansion", "Cruise", "MusicBroadcast" };
        for (int i = 0; i < 3; i++)
        {
            string path = "Assets/Art/DialogueFrames/" + files[i] + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            frames[i] = new DialoguePrototypeView.ThemeFrame { themeId = ids[i], frame = AssetDatabase.LoadAssetAtPath<Sprite>(path) };
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 2, -10);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = new Color32(20, 17, 26, 255);
        camera.AddComponent<UniversalAdditionalCameraData>();

        var canvasGo = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = Rect("DialoguePanel", canvasGo.transform, Vector2.zero, new Vector2(1920, 1080));
        var view = root.gameObject.AddComponent<DialoguePrototypeView>();
        view.npcProfilesJson = profilesJson;
        view.themes = frames;

        Label("Title", root, new Vector2(-560, 490), new Vector2(660, 60), "대화 · 수사 기록", 32, Cream);
        view.themeButtons = new Button[3];
        string[] themeNames = { "대저택", "크루즈", "음악방송" };
        for (int i = 0; i < 3; i++) view.themeButtons[i] = Button("Theme_" + ids[i], root,
            new Vector2(380 + i * 190, 490), new Vector2(175, 52), themeNames[i]);
        view.npcButtons = new Button[6];
        for (int i = 0; i < profiles.characters.Length; i++) view.npcButtons[i] = Button("Npc_" + profiles.characters[i].npcId,
            root, new Vector2(-725 + i * 290, 420), new Vector2(270, 48), profiles.characters[i].displayName);

        // Frame stays 2048:682. All coordinates are overlays, never baked into the PNG.
        var art = Rect("OriginalFrame", root, new Vector2(0, 80), new Vector2(1800, 599.4141f));
        view.frameImage = art.gameObject.AddComponent<Image>();
        view.frameImage.sprite = frames[0].frame;
        view.frameImage.preserveAspect = true;
        view.frameImage.raycastTarget = false;
        view.npcName = Label("NpcName", art, new Vector2(-590, 160), new Vector2(390, 70), profiles.characters[0].displayName, 36, Ink);
        view.npcName.alignment = TextAlignmentOptions.Center;
        view.body = Label("DialogueBody", art, new Vector2(0, -35), new Vector2(1550, 215), "", 32, Ink);
        view.body.alignment = TextAlignmentOptions.TopLeft;

        var inputRoot = Rect("QuestionInput", root, new Vector2(-90, -235), new Vector2(1530, 64));
        inputRoot.gameObject.AddComponent<Image>().color = new Color32(249, 244, 234, 255);
        var inputViewport = Rect("Text Area", inputRoot, Vector2.zero, new Vector2(1480, 58));
        inputViewport.gameObject.AddComponent<RectMask2D>();
        var inputText = Label("Text", inputViewport, Vector2.zero, new Vector2(1480, 58), "", 26, Ink);
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label("Placeholder", inputViewport, Vector2.zero, new Vector2(1480, 58), "궁금한 내용을 자유롭게 입력해 주세요.", 26, new Color32(110, 98, 106, 255));
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        view.questionInput = inputRoot.gameObject.AddComponent<TMP_InputField>();
        view.questionInput.textViewport = inputViewport;
        view.questionInput.textComponent = inputText;
        view.questionInput.placeholder = placeholder;
        view.questionInput.lineType = TMP_InputField.LineType.SingleLine;
        view.questionInput.characterLimit = 1000;
        view.sendButton = Button("SendQuestion", root, new Vector2(780, -235), new Vector2(180, 64), "질문 보내기");
        view.statusText = Label("Status", root, new Vector2(0, -290), new Vector2(1720, 36), "질문과 대화 기록을 연결했습니다. 승인된 사건 대사 연결은 준비 중입니다.", 22, Cream);

        Label("HistoryHeading", root, new Vector2(-700, -335), new Vector2(340, 36), "이전 대화", 24, Cream);
        var history = Rect("HistoryScroll", root, new Vector2(0, -430), new Vector2(1740, 160));
        history.gameObject.AddComponent<Image>().color = new Color32(35, 30, 42, 255);
        view.historyScroll = history.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("Viewport", history, Vector2.zero, new Vector2(1700, 146));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport, Vector2.zero, new Vector2(1700, 146));
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 146); content.anchoredPosition = Vector2.zero;
        var vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.childControlHeight = true; vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false; vertical.childForceExpandWidth = true;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.historyText = Label("HistoryText", content, Vector2.zero, new Vector2(1680, 146), "아직 대화 기록이 없습니다.", 23, Cream);
        view.historyText.alignment = TextAlignmentOptions.TopLeft;
        view.historyScroll.viewport = viewport; view.historyScroll.content = content;
        view.historyScroll.horizontal = false; view.historyScroll.vertical = true;
        view.historyScroll.movementType = ScrollRect.MovementType.Clamped;
        view.historyScroll.scrollSensitivity = 35;

        string uiCharacters = string.Concat(root.GetComponentsInChildren<TMP_Text>().Select(t => t.text))
            + "탐정안내승인된응답을연결하지못했습니다질문은기록에보관현재세션파일저장읽새대화시작유지자롭게주세요";
        font.TryAddCharacters(uiCharacters, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Font missing glyphs: " + missing);
        EditorUtility.SetDirty(font);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/DialoguePanel.prefab");
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DialoguePanel.prefab"), canvasGo.transform);
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        PlayerSettings.companyName = "DeductionGame";
        PlayerSettings.productName = "Deduction Game";
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        // TMP's desktop IME composition polling uses UnityEngine.Input as well.
        playerSettings.FindProperty("activeInputHandler").intValue = 2;
        playerSettings.ApplyModifiedPropertiesWithoutUndo();
        var pcPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline = pcPipeline;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pcPipeline; }
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1);
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/DialoguePrototype.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/DialoguePrototype.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("DIALOGUE_ASSETS_CREATED: Edit Mode authoring only. No functional tests, Play Mode or builds executed.");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float sizePt, Color color)
    {
        var rect = Rect(name, parent, position, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = sizePt; label.color = color; label.text = text;
        label.richText = false; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
    private static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string title)
    {
        var rect = Rect(name, parent, position, size);
        rect.gameObject.AddComponent<Image>().color = new Color32(83, 53, 65, 255);
        var button = rect.gameObject.AddComponent<Button>();
        var text = Label("Label", rect, Vector2.zero, size, title, 24, Cream);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
