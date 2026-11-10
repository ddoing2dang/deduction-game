#if UNITY_EDITOR
// 사건파일 No.464 - editor-only map workshop. No automatic low-quality fallback.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Case464.MapWorkshop
{
    public enum PlacementRole { Architecture, Hero, Furniture, Detail, EvidenceAnchor }

    [Serializable]
    public class ApprovedPlacement
    {
        public string label;
        public PlacementRole role;
        public GameObject approvedPrefab;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 localScale = Vector3.one;
    }

    [CreateAssetMenu(fileName = "RoomBlueprint", menuName = "Case464/Approved Room Blueprint")]
    public sealed class RoomBlueprint : ScriptableObject
    {
        public string themeId;
        public string designRoomId;
        public string gameplayRoomId;
        public string displayName;
        [TextArea(2, 5)] public string visualIdentity;
        public string sourceHeroAssetPath;
        public bool artDirectorApproved;
        public string artApprovalReference;
        public Vector3 cameraTarget = new Vector3(0, 1.4f, 0);
        [Range(45f, 75f)] public float cameraElevationDegrees = 60f; // Reference style: high-angle cutaway
        [Range(0f, 360f)] public float cameraAzimuthDegrees = 225f;
        public float cameraDistance = 20f; // Orthographic perspective; distance controls clipping, not scale.
        public float cameraOrthoSize = 7;
        public List<ApprovedPlacement> placements = new List<ApprovedPlacement>();
    }

    public sealed class MapWorkshopWindow : EditorWindow
    {
        const string PlanPath = "Assets/Case464/Data/room_profiles_24.v1.json";
        const string BlueprintFolder = "Assets/Case464/MapBlueprints";
        const string SceneFolder = "Assets/Case464/Scenes/Workshop";
        RoomBlueprint selected;

        [Serializable]
        class DesignRoom
        {
            public string theme, roomId, level, roomType, title, heroAsset, spatialSignature;
        }

        [Serializable]
        class RoomPlan
        {
            public DesignRoom[] rooms;
        }

        [MenuItem("Case464/Map Workshop/Open")]
        public static void Open() => GetWindow<MapWorkshopWindow>("Case464 Map Workshop");

        [MenuItem("Case464/Map Workshop/Create Blueprint Assets From Plan")]
        public static void MakeAllBlueprints()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(PlanPath);
            if (json == null)
            {
                Debug.LogError("Case464 STOP: 24-room JSON missing: " + PlanPath);
                return;
            }
            if (!AssetDatabase.IsValidFolder(BlueprintFolder))
            {
                Debug.LogError("Case464 STOP: make the folder first: " + BlueprintFolder);
                return;
            }

            var plan = JsonUtility.FromJson<RoomPlan>(json.text);
            if (plan == null || plan.rooms == null || plan.rooms.Length != 24)
            {
                Debug.LogError("Case464 STOP: 24-room art plan is missing or malformed.");
                return;
            }
            int created = 0, kept = 0;
            foreach (var item in plan.rooms)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.theme) || string.IsNullOrWhiteSpace(item.roomId))
                {
                    Debug.LogError("Case464 STOP: invalid room entry; no remaining items created.");
                    AssetDatabase.SaveAssets();
                    return;
                }
                var path = BlueprintFolder + "/" + item.theme + "_" + item.roomId + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    kept++;
                    continue; // Never overwrite the artist's existing placements.
                }

                var blueprint = CreateInstance<RoomBlueprint>();
                blueprint.themeId = item.theme;
                blueprint.designRoomId = item.roomId;
                blueprint.displayName = item.title;
                blueprint.visualIdentity = item.spatialSignature;
                blueprint.sourceHeroAssetPath = item.heroAsset;
                blueprint.gameplayRoomId = ""; // Must map to actual 8-room game IDs by an artist.
                blueprint.artDirectorApproved = false;
                AssetDatabase.CreateAsset(blueprint, path);
                created++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Case464: {created} blank design blueprints created, {kept} preserved. No scene or placeholder generated.");
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Production gate: only manually approved detailed prefabs and scene composition. " +
                "No cubes, no primitive filler, no auto replacement. No Play Mode or build.", MessageType.Info);
            selected = (RoomBlueprint)EditorGUILayout.ObjectField(
                "Room blueprint", selected, typeof(RoomBlueprint), false);
            if (GUILayout.Button("Create 24 empty blueprints from saved room plan"))
                MakeAllBlueprints();
            using (new EditorGUI.DisabledScope(selected == null))
            {
                if (GUILayout.Button("Check selected blueprint (no scene created)"))
                    Audit(selected, true);
                if (GUILayout.Button("Create review scene additively (never overwrite)"))
                    Build(selected);
            }
        }

        static readonly HashSet<string> Themes = new HashSet<string> {
            "HOTEL", "CRUISE", "MUSIC_BROADCAST"
        };
        static readonly HashSet<string> GameRoomIds = new HashSet<string> {
            "LOBBY", "ROOM_MATTO", "ROOM_MARIHI", "ROOM_NAZ",
            "ROOM_JIRO", "ROOM_BOKI", "ROOM_MOONIRE", "CRIME_SCENE"
        };

        public static bool Audit(RoomBlueprint b, bool verbose)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (b == null) { Debug.LogError("Case464 STOP: no blueprint selected."); return false; }
            if (!Themes.Contains(b.themeId)) errors.Add("themeId must be HOTEL, CRUISE or MUSIC_BROADCAST");
            if (string.IsNullOrWhiteSpace(b.designRoomId)) errors.Add("designRoomId is required");
            if (!GameRoomIds.Contains(b.gameplayRoomId)) errors.Add("map gameplayRoomId to one of the real eight game rooms");
            if (!b.artDirectorApproved || string.IsNullOrWhiteSpace(b.artApprovalReference))
                errors.Add("quality review is missing: artDirectorApproved + artApprovalReference required");

            int requiredCount = b.gameplayRoomId == "LOBBY" ? 12 : 9;
            if (b.placements == null || b.placements.Count < requiredCount)
                errors.Add($"at least {requiredCount} individually composed placements required");

            int hero = 0, architecture = 0, details = 0;
            var prefabGuids = new HashSet<string>();
            if (b.placements != null)
            {
                foreach (var p in b.placements)
                {
                    if (p == null || p.approvedPrefab == null)
                    {
                        errors.Add("missing approvedPrefab (no primitive fallback allowed)");
                        continue;
                    }
                    if (!EditorUtility.IsPersistent(p.approvedPrefab))
                        errors.Add("not a saved prefab or model asset: " + p.label);
                    if (p.localScale.x <= 0 || p.localScale.y <= 0 || p.localScale.z <= 0)
                        errors.Add("zero/negative placement scale: " + p.label);
                    if (p.role == PlacementRole.Hero) hero++;
                    if (p.role == PlacementRole.Architecture) architecture++;
                    if (p.role == PlacementRole.Detail) details++;
                    string path = AssetDatabase.GetAssetPath(p.approvedPrefab);
                    if (string.IsNullOrEmpty(path)) errors.Add("asset path missing: " + p.label);
                    else prefabGuids.Add(AssetDatabase.AssetPathToGUID(path));
                    var renderers = p.approvedPrefab.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) errors.Add("not renderable: " + path);
                    foreach (var renderer in renderers)
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat == null || mat.shader == null || mat.shader.name.Contains("InternalError"))
                            errors.Add("missing/broken material on " + path);
                }
            }
            if (hero != 1) errors.Add("exactly one approved visual hero placement required");
            if (architecture < 2) errors.Add("at least two authored architectural placements required");
            if (details < 3) errors.Add("at least three close-up detail placements required");
            if (prefabGuids.Count < 5) errors.Add("insufficient unique art assets (five minimum)");
            if (b.cameraOrthoSize < 1) errors.Add("cameraOrthoSize must be positive");
            if (b.cameraElevationDegrees < 45f || b.cameraElevationDegrees > 75f)
                errors.Add("cameraElevationDegrees must be between 45 and 75");
            if (b.cameraAzimuthDegrees < 0f || b.cameraAzimuthDegrees > 360f)
                errors.Add("cameraAzimuthDegrees must be between 0 and 360");
            if (b.cameraDistance <= 1f)
                errors.Add("cameraDistance must be positive and provide clipping clearance");
            warnings.Add("Geometry beauty, PBR realism, UVs, texel density, occlusion, lighting and composition require human visual review.");
            if (verbose || errors.Count > 0)
            {
                foreach (var e in errors) Debug.LogError("Case464 QUALITY STOP [" + b.name + "]: " + e);
                foreach (var w in warnings) Debug.LogWarning("Case464 REVIEW [" + b.name + "]: " + w);
                if (errors.Count == 0) Debug.Log("Case464 blueprint preflight PASS (visual quality not automatically certified): " + b.name);
            }
            return errors.Count == 0;
        }

        public static void Build(RoomBlueprint b)
        {
            if (!Audit(b, true)) return;
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                Debug.LogError("Case464 STOP: create directory before generating a scene: " + SceneFolder);
                return;
            }
            var path = SceneFolder + "/" + b.themeId + "_" + b.designRoomId + "_Review.unity";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || File.Exists(path))
            {
                Debug.LogError("Case464 STOP: review scene already exists. No overwrite: " + path);
                return;
            }

            // Additive to keep the artist's current open scene untouched.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Case464_" + b.themeId + "_" + b.designRoomId);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var p in b.placements)
            {
                var instance = PrefabUtility.InstantiatePrefab(p.approvedPrefab, scene) as GameObject;
                if (instance == null)
                {
                    Debug.LogError("Case464 STOP: prefab could not be instantiated: " + p.label);
                    return; // Leave the additive scene open for inspection; never mark complete.
                }
                instance.name = string.IsNullOrWhiteSpace(p.label) ? p.approvedPrefab.name : p.label;
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = p.localPosition;
                instance.transform.localEulerAngles = p.localEuler;
                instance.transform.localScale = p.localScale;
            }

            var cameraObject = new GameObject("MapReviewCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = b.cameraOrthoSize;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.012f, 0.017f, 1f);
            float elevation = b.cameraElevationDegrees * Mathf.Deg2Rad;
            float azimuth = b.cameraAzimuthDegrees * Mathf.Deg2Rad;
            float horizontalDistance = b.cameraDistance * Mathf.Cos(elevation);
            Vector3 offset = new Vector3(
                Mathf.Sin(azimuth) * horizontalDistance,
                Mathf.Sin(elevation) * b.cameraDistance,
                Mathf.Cos(azimuth) * horizontalDistance);
            cameraObject.transform.position = b.cameraTarget + offset;
            cameraObject.transform.LookAt(b.cameraTarget);

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                Debug.LogError("Case464 STOP: editor could not save scene: " + path);
                return;
            }
            Debug.Log("Case464 review scene saved, NOT visual-tested/built: " + path +
                      ". Add authored lighting, architecture finishes, decals and approved evidence anchors.");
        }
    }
}
#endif
