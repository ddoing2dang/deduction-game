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
        // Set only by an explicitly requested local authoring operation.
        // Normal interactive menu use retains its completion dialog.
        public static bool SuppressCompletionDialog { get; set; }
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
            new Entry("MARIHI", "마연희", "ROOM_MARIHI", "201", 2, 1),
            new Entry("NAZ", "나지수", "ROOM_NAZ", "202", 2, 2),
            new Entry("BOKI", "김보희", "ROOM_BOKI", "103", 1, 3),
            new Entry("JJIRO", "진소율", "ROOM_JIRO", "102", 1, 4),
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
            if (layouts.Length > 1 || cameras.Length != 1)
                throw new InvalidOperationException(
                    "This is not a uniquely identified final hotel map. Expected one HotelMapCameraController and unambiguous actual room areas.");

            if (UnityEngine.Object.FindObjectsByType<HotelNpcActor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(x => x.gameObject.scene == scene))
                throw new InvalidOperationException(
                    "This scene already has NPC actors. Aborted to prevent duplicate placement.");

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

                var matchingAreas = areas.Where(a =>
                    a.RoomId == item.room || a.DisplayNumber == item.number ||
                    a.DisplayNumber == item.number + "호").ToArray();
                if (matchingAreas.Length != 1)
                    throw new InvalidOperationException(
                        "Latest map room anchors are not unique for " +
                        item.number + " " + item.name + ". No guessed coordinates used.");

                var area = matchingAreas[0];
                if (area.Floor != item.floor || area.RoomId != item.room)
                    throw new InvalidOperationException(
                        "Floor/bounds mismatch for " + item.number +
                        ". Use an approved latest hotel map with accurate room anchors.");

                // Rows/signs/aisle authoring moves whole room roots. Historical
                // layout.floorBounds are stale after those operations (and some
                // final scenes intentionally omit that authoring-only component).
                // Use the actual current floor mesh and entrance, never old world coordinates.
                positions[i] = FindActualRoomPoint(area, out float shorterSide);
                offsets[i] = area.DisplayPosition - area.PhysicalPosition;
                float desiredHeight = Mathf.Clamp(
                    shorterSide * .20f,
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

            EnsureExistingDialoguePresenter(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "The original scene backup exists, but saving the NPC scene failed: " +
                    backupPath);
            Selection.activeGameObject = group;
            Debug.Log("[Case464 NPC] Added six original 8-way idle NPCs to OPEN latest hotel map: " +
                scene.path + " (backup: " + backupPath +
                "). No Play Mode/tests/build executed.");
            if (!SuppressCompletionDialog) EditorUtility.DisplayDialog("대저택 NPC 배치 완료",
                "현재 열린 맵에 NPC 6명을 배치하고 저장했습니다.\\n" +
                "원본 맵 백업: " + backupPath +
                "\\nUnity Editor에서 배치 위치를 직접 확인하세요.\\n" +
                "(Play Mode/기능 테스트/빌드 미실시)", "확인");
        }

        private static Vector3 FindActualRoomPoint(HotelOverviewRoomArea area, out float shorterSide)
        {
            var floor = area.GetComponentsInChildren<MeshRenderer>(true)
                .SingleOrDefault(r => r.name == "Uniform floor substrate");
            var entry = area.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(t => t.name == "Uniform entrance" && t.gameObject.activeInHierarchy);
            var inside = entry != null ? entry.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(t => t.name == "Uniform inside approach") : null;
            var outside = entry != null ? entry.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(t => t.name == "Uniform outside approach") : null;
            var finish = area.GetComponentsInChildren<Transform>().Where(t =>
                t.name == "Rose hotel | complete octagonal marble floor" ||
                t.name == "Rose hotel | 202 matching standard floor").ToArray();
            if (floor == null || entry == null || inside == null || outside == null || finish.Length != 1)
                throw new InvalidOperationException("Current floor/entrance/finish geometry is not unique for " + area.DisplayNumber);
            Vector3 u = Vector3.ProjectOnPlane(area.transform.right, Vector3.up).normalized;
            Vector3 v = Vector3.ProjectOnPlane(area.transform.forward, Vector3.up).normalized;
            (float min, float max) Span(MeshRenderer r, Vector3 axis)
            {
                var b = r.GetComponent<MeshFilter>().sharedMesh.bounds;
                float low = float.PositiveInfinity, high = float.NegativeInfinity;
                for (int i = 0; i < 8; i++)
                {
                    var p = r.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                    float x = Vector3.Dot(p, axis); low = Mathf.Min(low, x); high = Mathf.Max(high, x);
                }
                return (low, high);
            }
            var a = Span(floor, u); var bSpan = Span(floor, v);
            shorterSide = Mathf.Min(a.max - a.min, bSpan.max - bSpan.min);
            if (shorterSide < 2) throw new InvalidOperationException("Actual floor dimensions are invalid for " + area.DisplayNumber);
            float level = finish[0].GetComponentsInChildren<MeshRenderer>().Max(r => Span(r, Vector3.up).max);
            var obstacles = area.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.enabled && r.GetComponent<MeshFilter>() != null)
                .Select(r => (x: Span(r, u), z: Span(r, v), y: Span(r, Vector3.up)))
                .Where(p => p.y.max > level + .005f && p.y.min < level + 1.65f).ToArray();
            Vector3 inward = Vector3.ProjectOnPlane(inside.position - outside.position, Vector3.up).normalized;
            Vector3 centre = u * ((a.min + a.max) / 2) + v * ((bSpan.min + bSpan.max) / 2) + Vector3.up * level;
            Vector3 preferred = Vector3.Lerp(inside.position, centre, .5f);
            var candidates = new System.Collections.Generic.List<Vector3>();
            for (float x = a.min + .50f; x <= a.max - .50f; x += .25f)
                for (float z = bSpan.min + .50f; z <= bSpan.max - .50f; z += .25f)
                {
                    Vector3 point = u * x + v * z + Vector3.up * (level + .025f);
                    if (Vector3.Dot(point - entry.position, inward) < 1.3f) continue;
                    // At the approved 90-degree view the billboard spans floor area.
                    // Clear its whole projected image, not only its foot anchor.
                    var review = UnityEngine.Object.FindFirstObjectByType<HotelMapCameraController>().ReviewCamera;
                    Vector3 imageUp = Vector3.ProjectOnPlane(review.transform.up, Vector3.up).normalized;
                    Vector3 imageRight = Vector3.ProjectOnPlane(review.transform.right, Vector3.up).normalized;
                    float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;
                    float minV = float.PositiveInfinity, maxV = float.NegativeInfinity;
                    foreach (float h in new[] { -.12f, 1.52f })
                        foreach (float w in new[] { -.48f, .48f })
                        {
                            Vector3 corner = point + imageUp * h + imageRight * w;
                            float cu = Vector3.Dot(corner,u), cv = Vector3.Dot(corner,v);
                            minU=Mathf.Min(minU,cu);maxU=Mathf.Max(maxU,cu);
                            minV=Mathf.Min(minV,cv);maxV=Mathf.Max(maxV,cv);
                        }
                    if (minU < a.min+.2f || maxU > a.max-.2f || minV < bSpan.min+.2f || maxV > bSpan.max-.2f) continue;
                    if (obstacles.Any(p => maxU > p.x.min && minU < p.x.max &&
                                           maxV > p.z.min && minV < p.z.max)) continue;
                    candidates.Add(point);
                }
            if (candidates.Count == 0)
                throw new InvalidOperationException("No clear in-room NPC footprint found for " + area.DisplayNumber + ". Furniture was not moved.");
            return candidates.OrderBy(p => (p - preferred).sqrMagnitude).First();
        }


        // Explicit repair of existing actors only; never creates duplicates or moves furniture.
        public static void RefitExistingNpcAuthoring()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
                throw new InvalidOperationException("Save the clean Edit Mode scene before refitting.");
            var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HotelNpcActor>(true)).ToArray();
            var areas=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HotelOverviewRoomArea>(true)).ToArray();
            if(actors.Length!=6)throw new InvalidOperationException("Expected six existing original actors.");
            var positions=actors.Select(actor=>FindActualRoomPoint(areas.Single(r=>r.RoomId==actor.RoomId),out _)).ToArray();
            EnsureExistingDialoguePresenter(scene);
            for(int i=0;i<actors.Length;i++){Undo.RecordObject(actors[i].transform,"Clear NPC image footprint");actors[i].transform.position=positions[i];}
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("NPC position save failed.");
        }



        public static void EnsureExistingDialoguePresenter(Scene scene)
        {
            var presenters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Mystery.UI.DialogueCinematicView>(true)).ToArray();
            if(presenters.Length>1)throw new InvalidOperationException("Ambiguous dialogue presenters; nothing will be replaced.");
            Mystery.UI.DialogueCinematicView presenter;
            if(presenters.Length==0)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/DialoguePanel.prefab");
                if(prefab==null || prefab.GetComponent<Mystery.UI.DialogueCinematicView>()==null)
                    throw new InvalidOperationException("Existing approved dialogue prefab is required.");
                var canvasObject=new GameObject("Hotel NPC Dialogue Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject,scene);
                Undo.RegisterCreatedObjectUndo(canvasObject,"Connect existing NPC dialogue");
                var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
                var panel=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvasObject.transform);
                presenter=panel.GetComponent<Mystery.UI.DialogueCinematicView>();
            }
            else presenter=presenters[0];
            var serialized=new SerializedObject(presenter);
            serialized.FindProperty("visibleOnStart").boolValue=false;
            serialized.FindProperty("worldCamera").objectReferenceValue=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HotelMapCameraController>(true)).Single().ReviewCamera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var visibility=presenter.GetComponent<CanvasGroup>();
            if(visibility==null)visibility=presenter.gameObject.AddComponent<CanvasGroup>();
            visibility.alpha=0;visibility.interactable=false;visibility.blocksRaycasts=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(presenter);
            if(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Any())
            {
                var events=new GameObject("Hotel NPC Dialogue EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(events,scene);Undo.RegisterCreatedObjectUndo(events,"Connect existing NPC dialogue input");
            }
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
