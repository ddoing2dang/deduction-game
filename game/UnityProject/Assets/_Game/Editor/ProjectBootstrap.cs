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

        Directory.CreateDirectory("Assets/_Game/Scenes");

        if (File.Exists(ScenePath))
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
        EditorSceneManager.OpenScene(ScenePath);

        Debug.Log("[DeductionGame] Game scene 자동 생성 완료. Play를 누르면 바로 시작됩니다.");
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera");
        var camera = go.AddComponent<Camera>();
        camera.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 13f, -12f);
        go.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
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
        var root = new GameObject("Rooms").transform;
        var positions = new[]
        {
            Vector3.zero,
            new Vector3(0, 0, 0),
            new Vector3(-5, 0, 2.5f),
            new Vector3(5, 0, 2.5f),
            new Vector3(-5, 0, -2.5f),
            new Vector3(0, 0, -3.5f),
            new Vector3(0, 0, 5f),
            new Vector3(-5, 0, -7f),
            new Vector3(0, 0, -7f),
            new Vector3(5, 0, -7f)
        };

        for (int id = 1; id <= 9; id++)
        {
            var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.transform.SetParent(root);
            room.transform.position = positions[id];
            room.transform.localScale = new Vector3(3.6f, 0.35f, 2.2f);

            var view = room.AddComponent<RoomNodeView>();
            view.Configure(id, RoomNames[id]);
        }
    }
}
#endif
