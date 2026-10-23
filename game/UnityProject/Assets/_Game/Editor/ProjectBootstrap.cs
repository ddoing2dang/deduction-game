#if UNITY_EDITOR
using System.IO;
using DeductionGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ProjectBootstrap
{
    private const string ScenePath = "Assets/_Game/Scenes/Game.unity";
    private const int SceneLayoutVersion = 2;
    private const string SceneVersionKey = "DeductionGame.SceneLayoutVersion";

    private static readonly string[] RoomNames =
    {
        "", "중앙홀", "서재", "응접실", "식당", "주방",
        "온실", "보일러실", "와인저장고", "사용인실"
    };

    static ProjectBootstrap()
    {
        EditorApplication.delayCall += EnsureProject;
    }

    private static void EnsureProject()
    {
        if (Application.isPlaying || EditorApplication.isCompiling) return;

        ConfigurePcProject();
        Directory.CreateDirectory("Assets/_Game/Scenes");

        bool sceneExists = File.Exists(ScenePath);
        int appliedVersion = EditorPrefs.GetInt(SceneVersionKey, 0);
        if (sceneExists && appliedVersion >= SceneLayoutVersion)
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();

        CreateCamera();
        CreateLight();
        CreateRooms();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetInt(SceneVersionKey, SceneLayoutVersion);
        EditorSceneManager.OpenScene(ScenePath);

        Debug.Log($"[DeductionGame] Game scene layout v{SceneLayoutVersion} 자동 갱신 완료.");
    }

    private static void ConfigurePcProject()
    {
        PlayerSettings.productName = "Deduction Game";
        PlayerSettings.companyName = "ddoing2dang";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera");
        var camera = go.AddComponent<Camera>();
        camera.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 28f, -24f);
        go.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 19f;
        camera.clearFlags = CameraClearFlags.Skybox;
    }

    private static void CreateLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateRooms()
    {
        var root = new GameObject("Mansion_Graybox").transform;

        foreach (var layout in MansionLayout.Rooms)
        {
            var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.transform.SetParent(root);
            room.transform.position = layout.Center + Vector3.down * 0.15f;
            room.transform.localScale = new Vector3(layout.Size.x, 0.3f, layout.Size.y);

            var view = room.AddComponent<RoomNodeView>();
            view.Configure(layout.Id, layout.Name);

            var stands = new GameObject($"Room_{layout.Id:00}_StandPoints").transform;
            stands.SetParent(root);
            for (int slot = 0; slot < 6; slot++)
            {
                var point = new GameObject($"Stand_{slot + 1:00}").transform;
                point.SetParent(stands);
                point.position = MansionLayout.GetStandPoint(layout.Id, slot);
            }
        }
    }
}
#endif
