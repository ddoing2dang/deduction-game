using System;
using System.Linq;
using Case464.Hotel;
using Case464.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Case464.Editor
{
    // Explicit authoring action. Does not overwrite the approved final map,
    // enter Play Mode, execute tests, or build a player.
    public static class HotelNpcPlacementBuilder
    {
        private const string SourceScene =
            "Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map.unity";
        private const string OutputScene =
            "Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Characters.unity";
        private const string SpriteRoot = "Assets/Case464/NPC/MapIdle/";

        private static readonly string[] Directions =
            { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        // The earlier Unity import pack used numbered source folders.
        // Reuse it when already imported instead of forcing duplicate PNGs.
        private static readonly string[] ExistingImportFolders =
            { "01_silver", "02_blonde", "03_brown",
              "04_red", "05_green", "06_purple" };

        // Order is the EXISTING DialogueSpeaker numeric ID; it is not
        // a license to rename enum constants or alter saved case data.
        private static readonly Entry[] People =
        {
            new Entry("MATTO", "마도은", "ROOM_MATTO", "101", 1, 0),
            new Entry("MARIHI", "마혜인", "ROOM_MARIHI", "201", 2, 1),
            new Entry("NAZ", "나지수", "ROOM_NAZ", "202", 2, 2),
            new Entry("BOKI", "김보희", "ROOM_BOKI", "103", 1, 3),
            new Entry("JJIRO", "진초록", "ROOM_JIRO", "102", 1, 4),
            new Entry("MOONIRE", "문이현", "ROOM_MOONIRE", "203", 2, 5)
        };

        private readonly struct Entry
        {
            public readonly string id, name, room, number;
            public readonly int floor, speaker;
            public Entry(string id, string name, string room, string number,
                int floor, int speaker)
            {
                this.id = id; this.name = name; this.room = room;
                this.number = number; this.floor = floor; this.speaker = speaker;
            }
        }

        [MenuItem("Case464/Hotel NPC/Build 6 NPCs in Separate Scene (Edit Mode)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode is required.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException(
                    "Current scene has unsaved changes. Save it before generating the NPC scene.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScene) != null)
                throw new InvalidOperationException(
                    "NPC placement scene already exists. It will not be overwritten: " + OutputScene);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene) == null)
                throw new InvalidOperationException("Approved hotel scene was not found.");

            // Validate all 48 genuine user-provided files BEFORE opening another scene.
            var allSprites = new Sprite[People.Length][];
            for (int i = 0; i < People.Length; i++)
            {
                var sprites = new Sprite[8];
                for (int d = 0; d < 8; d++)
                {
                    string asset = SpriteRoot + People[i].id + "/MapIdle_" + Directions[d] + ".png";
                    sprites[d] = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
                    if (sprites[d] == null)
                    {
                        string existing = "Assets/MattoMystery/Art/NPC/" +
                            ExistingImportFolders[i] + "/MapIdle_" + Directions[d] + ".png";
                        sprites[d] = AssetDatabase.LoadAssetAtPath<Sprite>(existing);
                    }
                    if (sprites[d] == null)
                        throw new InvalidOperationException(
                            "Missing original Sprite (install supplied asset pack): " + asset);
                }
                allSprites[i] = sprites;
            }

            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            var map = UnityEngine.Object.FindFirstObjectByType<HotelFinalPolishLayout>();
            var camera = UnityEngine.Object.FindFirstObjectByType<HotelMapCameraController>();
            var areas = UnityEngine.Object.FindObjectsByType<HotelOverviewRoomArea>(
                FindObjectsSortMode.None);
            if (map == null || map.rooms == null || camera == null)
                throw new InvalidOperationException(
                    "The approved scene must contain HotelFinalPolishLayout and the hotel camera.");

            var positions = new Vector3[People.Length];
            var offsets = new Vector3[People.Length];
            var scales = new float[People.Length];

            for (int i = 0; i < People.Length; i++)
            {
                Entry item = People[i];
                HotelPolishRoomMetric metric = map.rooms.FirstOrDefault(m =>
                    m != null && (m.roomId == item.room || m.number == item.number || m.number == item.number + "호"));
                HotelOverviewRoomArea area = areas.FirstOrDefault(a =>
                    a.RoomId == item.room || a.DisplayNumber == item.number || a.DisplayNumber == item.number + "호");
                if (metric == null || area == null)
                    throw new InvalidOperationException(
                        "The approved scene is missing room metrics/area for " + item.room +
                        ". No approximate room placement will be made.");

                // Stay away from the door and in the authored room footprint.
                Vector3 center = metric.floorBounds.center;
                Vector3 start = metric.inside != null ? metric.inside.position : center;
                Vector3 point = Vector3.Lerp(start, center, 0.50f);
                point.x = Mathf.Clamp(point.x, metric.floorBounds.min.x + .25f,
                    metric.floorBounds.max.x - .25f);
                point.z = Mathf.Clamp(point.z, metric.floorBounds.min.z + .25f,
                    metric.floorBounds.max.z - .25f);
                point.y = metric.floorBounds.max.y + 0.035f;

                if (metric.floorBounds.size.x <= .5f || metric.floorBounds.size.z <= .5f)
                    throw new InvalidOperationException("Invalid room bounds for " + item.room);

                positions[i] = point;
                offsets[i] = area.DisplayPosition - area.PhysicalPosition;
                float artHeight = allSprites[i][4].bounds.size.y;
                float desiredHeight = Mathf.Clamp(
                    Mathf.Min(metric.floorBounds.size.x, metric.floorBounds.size.z) * .20f,
                    1.1f, 1.9f);
                scales[i] = desiredHeight / Mathf.Max(.01f, artHeight);
            }

            var group = new GameObject("NPC Placement - 6 suspects (authored original stills)");
            for (int i = 0; i < People.Length; i++)
            {
                Entry item = People[i];
                var actorObj = new GameObject(item.number + " " + item.name + " (" + item.id + ")");
                actorObj.transform.SetParent(group.transform, false);
                actorObj.transform.position = positions[i];
                var actor = actorObj.AddComponent<HotelNpcActor>();

                var physical = CreateImage("World Sprite", actorObj.transform,
                    Vector3.zero, scales[i], out var physicalBox);
                var display = CreateImage("Split-Floor Display Sprite", actorObj.transform,
                    offsets[i], scales[i], out var displayBox);
                physical.sprite = allSprites[i][4];
                display.sprite = allSprites[i][4];
                physical.gameObject.AddComponent<HotelNpcClickProxy>().Configure(actor);
                display.gameObject.AddComponent<HotelNpcClickProxy>().Configure(actor);
                actor.Configure(item.id, item.name, item.room, item.floor,
                    item.speaker, allSprites[i], physical, display,
                    physicalBox, displayBox, camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            // Save under a NEW path. Never save to or overwrite SourceScene.
            if (!EditorSceneManager.SaveScene(scene, OutputScene, false))
                throw new InvalidOperationException("Saving separate NPC scene failed.");
            AssetDatabase.SaveAssets();
            Debug.Log("[Case464 NPC] Six original 8-way still NPCs placed into: " +
                OutputScene + ". Approved base map is intact. No Play Mode/tests/build.");
        }

        /// <summary>
        /// Apply user-provided still NPCs to the CURRENTLY OPEN hotel scene.
        /// Supports newer authored hotel scene paths: we inspect its room anchors,
        /// not its filename. A separate backup .unity is created before saving.
        /// This is an explicit Edit Mode menu command, never an auto-import.
        /// </summary>
        [MenuItem("Case464/Hotel NPC/Apply 6 NPCs to OPEN Latest Map (Edit Mode)")]
        public static void ApplyToOpenLatestMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying NPCs.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path) ||
                !scene.path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !scene.path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Open your latest saved hotel .unity scene in Edit Mode.");
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "Save the CURRENT hotel scene first (Ctrl+S). NPC placement has not started.");

            // Discover map layout and camera only within the ACTIVE scene,
            // so a preview/additive scene cannot silently influence placement.
            var layouts = UnityEngine.Object.FindObjectsByType<HotelFinalPolishLayout>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray();
            var cameras = UnityEngine.Object.FindObjectsByType<HotelMapCameraController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray();
            var areas = UnityEngine.Object.FindObjectsByType<HotelOverviewRoomArea>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray();
            if (layouts.Length != 1 || cameras.Length != 1 || layouts[0].rooms == null)
                throw new InvalidOperationException(
                    "This is not a uniquely identified final hotel map. Expected one HotelFinalPolishLayout and one HotelMapCameraController.");

            if (UnityEngine.Object.FindObjectsByType<HotelNpcActor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(x => x.gameObject.scene == scene))
                throw new InvalidOperationException(
                    "This scene already has NPC actors. Aborted to prevent duplicate placement.");

            HotelFinalPolishLayout layout = layouts[0];
            HotelMapCameraController camera = cameras[0];
            var poses = new Sprite[People.Length][];
            var positions = new Vector3[People.Length];
            var offsets = new Vector3[People.Length];
            var scales = new float[People.Length];

            // Fully resolve the user's 48 original sprites and room anchors
            // BEFORE creating the backup or modifying anything in the scene.
            for (int i = 0; i < People.Length; i++)
            {
                Entry item = People[i];
                poses[i] = new Sprite[Directions.Length];
                for (int d = 0; d < Directions.Length; d++)
                {
                    string file = "MapIdle_" + Directions[d] + ".png";
                    string preferred = SpriteRoot + item.id + "/" + file;
                    poses[i][d] = AssetDatabase.LoadAssetAtPath<Sprite>(preferred);
                    if (poses[i][d] == null)
                    {
                        string earlier = "Assets/MattoMystery/Art/NPC/" +
                            ExistingImportFolders[i] + "/" + file;
                        poses[i][d] = AssetDatabase.LoadAssetAtPath<Sprite>(earlier);
                    }
                    if (poses[i][d] == null)
                        throw new InvalidOperationException(
                            "Missing original NPC sprite: " + preferred +
                            " — import the supplied six-person map sprite ZIP into Assets first.");
                }

                var matchingMetrics = layout.rooms.Where(m => m != null &&
                    (m.roomId == item.room || m.number == item.number ||
                     m.number == item.number + "호")).ToArray();
                var matchingAreas = areas.Where(a =>
                    a.RoomId == item.room || a.DisplayNumber == item.number ||
                    a.DisplayNumber == item.number + "호").ToArray();
                if (matchingMetrics.Length != 1 || matchingAreas.Length != 1)
                    throw new InvalidOperationException(
                        "Latest map room anchors are not unique for " +
                        item.number + " " + item.name + ". No guessed coordinates used.");

                var room = matchingMetrics[0];
                var area = matchingAreas[0];
                if (room.floor != item.floor || area.Floor != item.floor ||
                    room.floorBounds.size.x <= .5f || room.floorBounds.size.z <= .5f)
                    throw new InvalidOperationException(
                        "Floor/bounds mismatch for " + item.number +
                        ". Use an approved latest hotel map with accurate room anchors.");

                Vector3 boundsCenter = room.floorBounds.center;
                Vector3 entryInside = room.inside != null ? room.inside.position : boundsCenter;
                Vector3 point = Vector3.Lerp(entryInside, boundsCenter, .50f);
                point.x = Mathf.Clamp(point.x, room.floorBounds.min.x + .25f,
                    room.floorBounds.max.x - .25f);
                point.z = Mathf.Clamp(point.z, room.floorBounds.min.z + .25f,
                    room.floorBounds.max.z - .25f);
                point.y = room.floorBounds.max.y + .035f;

                positions[i] = point;
                offsets[i] = area.DisplayPosition - area.PhysicalPosition;
                float desiredHeight = Mathf.Clamp(
                    Mathf.Min(room.floorBounds.size.x, room.floorBounds.size.z) * .20f,
                    1.1f, 1.9f);
                scales[i] = desiredHeight /
                    Mathf.Max(.01f, poses[i][(int)HotelNpcFacing.S].bounds.size.y);
            }

            // Keep an unmodified copy BEFORE directly updating the open map.
            string backupPath = AssetDatabase.GenerateUniqueAssetPath(
                scene.path.Substring(0, scene.path.Length - ".unity".Length) +
                "_Before_NPC_Placement.unity");
            if (!AssetDatabase.CopyAsset(scene.path, backupPath))
                throw new InvalidOperationException(
                    "Could not create a backup of the current hotel map. No NPCs were placed.");

            var group = new GameObject("NPC Placement - 6 suspects (user originals)");
            SceneManager.MoveGameObjectToScene(group, scene);
            Undo.RegisterCreatedObjectUndo(group, "Place 6 hotel NPCs");

            for (int i = 0; i < People.Length; i++)
            {
                Entry item = People[i];
                var obj = new GameObject(item.number + " " + item.name + " (" + item.id + ")");
                obj.transform.SetParent(group.transform, false);
                obj.transform.position = positions[i];
                var actor = obj.AddComponent<HotelNpcActor>();
                var physical = CreateImage("World Sprite", obj.transform,
                    Vector3.zero, scales[i], out var physicalBox);
                var split = CreateImage("Split-Floor Display Sprite", obj.transform,
                    offsets[i], scales[i], out var splitBox);
                physical.sprite = poses[i][(int)HotelNpcFacing.S];
                split.sprite = poses[i][(int)HotelNpcFacing.S];
                physical.gameObject.AddComponent<HotelNpcClickProxy>().Configure(actor);
                split.gameObject.AddComponent<HotelNpcClickProxy>().Configure(actor);
                actor.Configure(item.id, item.name, item.room, item.floor, item.speaker,
                    poses[i], physical, split, physicalBox, splitBox, camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "The original scene backup exists, but saving the NPC scene failed: " +
                    backupPath);
            Selection.activeGameObject = group;
            Debug.Log("[Case464 NPC] Added six original 8-way idle NPCs to OPEN latest hotel map: " +
                scene.path + " (backup: " + backupPath +
                "). No Play Mode/tests/build executed.");
            EditorUtility.DisplayDialog("대저택 NPC 배치 완료",
                "현재 열린 맵에 NPC 6명을 배치하고 저장했습니다.\\n" +
                "원본 맵 백업: " + backupPath +
                "\\nUnity Editor에서 배치 위치를 직접 확인하세요.\\n" +
                "(Play Mode/기능 테스트/빌드 미실시)", "확인");
        }

        private static SpriteRenderer CreateImage(string name, Transform parent,
            Vector3 localPosition, float scale, out BoxCollider hitbox)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = Vector3.one * scale;
            var sprite = obj.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 120;
            hitbox = obj.AddComponent<BoxCollider>();
            hitbox.center = new Vector3(0f, .9f, 0f);
            hitbox.size = new Vector3(1.6f, 1.8f, .20f);
            hitbox.enabled = false;
            return sprite;
        }
    }
}
