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
    private const int SceneLayoutVersion = 3;
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
        CreateConnections();

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

    private static void CreateConnections()
    {
        var root = new GameObject("Mansion_Connections").transform;
        int[,] links =
        {
            {1,2},{1,3},{1,5},{1,6},{2,4},{3,5},
            {4,5},{4,7},{5,8},{7,8},{8,9}
        };

        for (int i = 0; i < links.GetLength(0); i++)
        {
            int a = links[i,0], b = links[i,1];
            Vector3 from = MansionLayout.GetRoom(a).Center;
            Vector3 to = MansionLayout.GetRoom(b).Center;
            CreateConnection(root, a, b, from, to);
        }

        var secret = new GameObject("DOOR_09_06_SECRET");
        secret.transform.SetParent(root);
        secret.transform.position = (MansionLayout.GetRoom(9).Center + MansionLayout.GetRoom(6).Center) * 0.5f;
        secret.SetActive(false);
    }

    private static void CreateConnection(Transform root, int a, int b, Vector3 from, Vector3 to)
    {
        var group = new GameObject($"DOOR_{a:00}_{b:00}").transform;
        group.SetParent(root);

        Vector3 delta = to - from;
        bool changesFloor = Mathf.Abs(delta.y) > 1f;

        var passage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        passage.name = changesFloor ? "ServiceStair" : "Passage";
        passage.transform.SetParent(group);
        passage.transform.position = (from + to) * 0.5f + Vector3.up * 0.08f;
        passage.transform.localScale = new Vector3(2f, 0.16f, Mathf.Max(2f, new Vector2(delta.x, delta.z).magnitude));
        passage.transform.rotation = Quaternion.Euler(
            changesFloor ? Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg : 0f,
            Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg,
            0f);

        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "DoorMarker";
        door.transform.SetParent(group);
        door.transform.position = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 1.1f;
        door.transform.localScale = new Vector3(1.6f, 2.2f, 0.18f);
        door.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0f);
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
