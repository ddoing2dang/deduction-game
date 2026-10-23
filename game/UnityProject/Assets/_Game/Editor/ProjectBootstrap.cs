#if UNITY_EDITOR
using System.Collections.Generic;
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
    private const int SceneLayoutVersion = 9;
    // A validation copy or another checkout must not suppress this project's upgrade.
    private static readonly string SceneVersionKey = "DeductionGame.SceneLayoutVersion:" + Application.dataPath;

    private const float FloorThickness = 0.3f;
    private const float WallThickness = 0.22f;
    private const float DoorHeight = 2.6f;
    private const float PassageWidth = 2.0f;

    [MenuItem("Deduction Game/Map/Mansion")] public static void Mansion() => Select(MapTheme.Mansion);
    [MenuItem("Deduction Game/Map/Cruise")] public static void Cruise() => Select(MapTheme.Cruise);
    [MenuItem("Deduction Game/Map/Museum")] public static void Museum() => Select(MapTheme.Museum);
    private static void Select(MapTheme theme) {
        if (Application.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorPrefs.SetInt(SceneVersionKey + ".Theme", (int)theme);
        EditorPrefs.SetInt(SceneVersionKey, 0);
        EnsureProject();
    }

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

        if (sceneExists && appliedVersion >= 5 && EditorPrefs.GetInt(SceneVersionKey + ".Theme", 0) == 0)
        {
            var existing = EditorSceneManager.OpenScene(ScenePath);
            MansionStructuralRepair.ApplyToScene();
            EditorSceneManager.SaveScene(existing);
            EditorPrefs.SetInt(SceneVersionKey, SceneLayoutVersion);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var selection = new GameObject("MapTheme").AddComponent<MapThemeSettings>();
        selection.Theme = (MapTheme)EditorPrefs.GetInt(SceneVersionKey + ".Theme", 0);

        var manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();

        CreateCamera();
        CreateLight();
        CreateRooms();
        CreateConnections();
        MansionStructuralRepair.ApplyToScene();

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
        PlayerSettings.resizableWindow = true;
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
        var root = new GameObject(MapData.RootName).transform;

        foreach (var layout in MapData.Rooms)
        {
            var roomRoot = new GameObject($"Room_{layout.Id:00}_{layout.Name}").transform;
            roomRoot.SetParent(root);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(roomRoot);
            floor.transform.position = layout.Center + Vector3.down * (FloorThickness * 0.5f);
            floor.transform.localScale = new Vector3(layout.Size.x, FloorThickness, layout.Size.y);

            var view = floor.AddComponent<RoomNodeView>();
            view.Configure(layout.Id, layout.Name);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(roomRoot);
            CreateRoomWalls(layout, walls);

            var points = new GameObject("NavigationPoints").transform;
            points.SetParent(roomRoot);

            for (int slot = 0; slot < 6; slot++)
            {
                var point = new GameObject($"Stand_{slot + 1:00}").transform;
                point.SetParent(points);
                point.position = MapData.GetStandPoint(layout.Id, slot);
            }

            var actionPoint = new GameObject("Action_Point").transform;
            actionPoint.SetParent(points);
            actionPoint.position = layout.Center + Vector3.up * 0.05f;

            int[] neighbors = MapGraph.GetConnections(layout.Id);
            for (int i = 0; i < neighbors.Length; i++)
            {
                int neighborId = neighbors[i];
                var neighbor = MapData.GetRoom(neighborId);
                Vector3 anchor = GetDoorAnchor(layout, neighbor);
                Vector3 outward = FlatDirection(layout.Center, neighbor.Center);

                var entry = new GameObject($"Entry_To_{neighborId:00}").transform;
                entry.SetParent(points);
                entry.position = anchor - outward * 1.25f + Vector3.up * 0.05f;
                if (outward.sqrMagnitude > 0.001f)
                    entry.rotation = Quaternion.LookRotation(outward, Vector3.up);
            }
        }
    }

    private static void CreateRoomWalls(MansionRoomLayout room, Transform parent)
    {
        float halfX = room.Size.x * 0.5f;
        float halfZ = room.Size.y * 0.5f;

        CreateWallSide(parent, room, true,  1f, halfX, halfZ, "East");
        CreateWallSide(parent, room, true, -1f, halfX, halfZ, "West");
        CreateWallSide(parent, room, false,  1f, halfZ, halfX, "North");
        CreateWallSide(parent, room, false, -1f, halfZ, halfX, "South");
    }

    private static void CreateWallSide(
        Transform parent,
        MansionRoomLayout room,
        bool constantX,
        float sideSign,
        float constantHalfExtent,
        float spanHalfExtent,
        string sideName)
    {
        var openings = new List<Vector2>();
        int[] neighbors = MapGraph.GetConnections(room.Id);

        for (int i = 0; i < neighbors.Length; i++)
        {
            int neighborId = neighbors[i];
            var neighbor = MapData.GetRoom(neighborId);
            Vector3 anchor = GetDoorAnchor(room, neighbor);
            Vector3 local = anchor - room.Center;

            bool belongsToSide = constantX
                ? Mathf.Abs(local.x - sideSign * constantHalfExtent) < 0.05f
                : Mathf.Abs(local.z - sideSign * constantHalfExtent) < 0.05f;

            if (!belongsToSide) continue;

            float center = constantX ? local.z : local.x;
            float width = GetDoorWidth(room.Id, neighborId) + 0.2f;
            openings.Add(new Vector2(center - width * 0.5f, center + width * 0.5f));
        }

        openings.Sort((a, b) => a.x.CompareTo(b.x));

        var merged = new List<Vector2>();
        for (int i = 0; i < openings.Count; i++)
        {
            Vector2 current = openings[i];
            current.x = Mathf.Clamp(current.x, -spanHalfExtent, spanHalfExtent);
            current.y = Mathf.Clamp(current.y, -spanHalfExtent, spanHalfExtent);
            if (current.y <= current.x) continue;

            if (merged.Count == 0 || current.x > merged[merged.Count - 1].y + 0.05f)
            {
                merged.Add(current);
            }
            else
            {
                Vector2 last = merged[merged.Count - 1];
                last.y = Mathf.Max(last.y, current.y);
                merged[merged.Count - 1] = last;
            }
        }

        float wallHeight = GetWallHeight(room);
        float cursor = -spanHalfExtent;
        int segment = 0;

        for (int i = 0; i < merged.Count; i++)
        {
            Vector2 opening = merged[i];

            if (opening.x - cursor > 0.08f)
            {
                CreateWallBlock(
                    parent,
                    $"{sideName}_Wall_{segment++:00}",
                    room,
                    constantX,
                    sideSign * constantHalfExtent,
                    (cursor + opening.x) * 0.5f,
                    opening.x - cursor,
                    0f,
                    wallHeight);
            }

            float lintelHeight = wallHeight - DoorHeight;
            if (lintelHeight > 0.08f)
            {
                CreateWallBlock(
                    parent,
                    $"{sideName}_Lintel_{i + 1:00}",
                    room,
                    constantX,
                    sideSign * constantHalfExtent,
                    (opening.x + opening.y) * 0.5f,
                    opening.y - opening.x,
                    DoorHeight,
                    lintelHeight);
            }

            cursor = Mathf.Max(cursor, opening.y);
        }

        if (spanHalfExtent - cursor > 0.08f)
        {
            CreateWallBlock(
                parent,
                $"{sideName}_Wall_{segment:00}",
                room,
                constantX,
                sideSign * constantHalfExtent,
                (cursor + spanHalfExtent) * 0.5f,
                spanHalfExtent - cursor,
                0f,
                wallHeight);
        }
    }

    private static void CreateWallBlock(
        Transform parent,
        string name,
        MansionRoomLayout room,
        bool constantX,
        float constantCoordinate,
        float spanCenter,
        float spanLength,
        float bottomHeight,
        float height)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);

        float y = room.Center.y + bottomHeight + height * 0.5f;
        if (constantX)
        {
            wall.transform.position = room.Center + new Vector3(constantCoordinate, y - room.Center.y, spanCenter);
            wall.transform.localScale = new Vector3(WallThickness, height, spanLength);
        }
        else
        {
            wall.transform.position = room.Center + new Vector3(spanCenter, y - room.Center.y, constantCoordinate);
            wall.transform.localScale = new Vector3(spanLength, height, WallThickness);
        }
    }

    private static float GetWallHeight(MansionRoomLayout room)
    {
        if (room.Id == 1) return 8f;
        return room.IsBasement ? 3.5f : 4f;
    }

    private static void CreateConnections()
    {
        var root = new GameObject(MapData.ConnectionsName).transform;

        for (int i = 0; i < MapData.Links.GetLength(0); i++)
        {
            int a = MapData.Links[i, 0];
            int b = MapData.Links[i, 1];
            var roomA = MapData.GetRoom(a);
            var roomB = MapData.GetRoom(b);

            Vector3 from = GetDoorAnchor(roomA, roomB);
            Vector3 to = GetDoorAnchor(roomB, roomA);
            CreateConnection(root, a, b, from, to);
        }

        CreateSecretPassagePlaceholder(root);
    }

    private static void CreateConnection(Transform root, int a, int b, Vector3 from, Vector3 to)
    {
        var group = new GameObject($"DOOR_{a:00}_{b:00}").transform;
        group.SetParent(root);

        Vector3 delta = to - from;
        bool changesFloor = Mathf.Abs(delta.y) > 1f;

        var route = GameObject.CreatePrimitive(PrimitiveType.Cube);
        route.name = changesFloor ? "ServiceStair" : "Passage";
        route.transform.SetParent(group);

        if (changesFloor)
        {
            route.transform.position = (from + to) * 0.5f;
            route.transform.localScale = new Vector3(PassageWidth, 0.18f, Mathf.Max(0.5f, delta.magnitude));
            route.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }
        else
        {
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float length = Mathf.Max(0.5f, flat.magnitude);
            route.transform.position = (from + to) * 0.5f + Vector3.down * 0.09f;
            route.transform.localScale = new Vector3(PassageWidth, 0.18f, length);
            route.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, 0f);
        }

        CreateDoorAnchor(group, $"Door_{a:00}_Anchor", from, FlatDirection(from, to), GetDoorWidth(a, b));
        CreateDoorAnchor(group, $"Door_{b:00}_Anchor", to, FlatDirection(to, from), GetDoorWidth(a, b));
    }

    private static void CreateDoorAnchor(Transform parent, string name, Vector3 position, Vector3 facing, float width)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(parent);
        anchor.position = position + Vector3.up * 0.05f;
        anchor.localScale = new Vector3(width, 1f, 1f);
        if (facing.sqrMagnitude > 0.001f)
            anchor.rotation = Quaternion.LookRotation(facing, Vector3.up);
    }

    private static void CreateSecretPassagePlaceholder(Transform root)
    {
        var room9 = MapData.GetRoom(MapData.SecretA);
        var room6 = MapData.GetRoom(MapData.SecretB);

        var secret = new GameObject(MapData.SecretName);
        secret.transform.SetParent(root);

        var from = GetDoorAnchor(room9, room6);
        var to = GetDoorAnchor(room6, room9);

        var a = new GameObject("HiddenPanel_09_Anchor").transform;
        a.SetParent(secret.transform);
        a.position = from + Vector3.up * 0.05f;

        var b = new GameObject("GreenhouseExit_06_Anchor").transform;
        b.SetParent(secret.transform);
        b.position = to + Vector3.up * 0.05f;

        secret.SetActive(false);
    }

    private static Vector3 GetDoorAnchor(MansionRoomLayout from, MansionRoomLayout to)
    {
        Vector3 delta = new Vector3(to.Center.x - from.Center.x, 0f, to.Center.z - from.Center.z);
        if (delta.sqrMagnitude < 0.001f)
            return from.Center;

        float halfX = from.Size.x * 0.5f;
        float halfZ = from.Size.y * 0.5f;

        float tx = Mathf.Abs(delta.x) > 0.001f ? halfX / Mathf.Abs(delta.x) : float.PositiveInfinity;
        float tz = Mathf.Abs(delta.z) > 0.001f ? halfZ / Mathf.Abs(delta.z) : float.PositiveInfinity;
        float t = Mathf.Min(tx, tz);

        return new Vector3(
            from.Center.x + delta.x * t,
            from.Center.y,
            from.Center.z + delta.z * t);
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    private static float GetDoorWidth(int a, int b)
    {
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);

        if (min == 1 && max == 2) return 1.4f;
        if (min == 1 && max == 3) return 1.8f;
        if (min == 1 && max == 5) return 1.3f;
        if (min == 1 && max == 6) return 2.2f;
        if (min == 2 && max == 4) return 2.0f;
        if (min == 3 && max == 5) return 1.6f;
        if (min == 4 && max == 5) return 1.6f;
        if (min == 4 && max == 7) return 2.0f;
        if (min == 5 && max == 8) return 2.0f;
        if (min == 7 && max == 8) return 2.0f;
        if (min == 8 && max == 9) return 1.6f;
        return 1.6f;
    }
}
#endif
