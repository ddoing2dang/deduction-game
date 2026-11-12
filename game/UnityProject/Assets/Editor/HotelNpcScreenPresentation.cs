using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Case464.Hotel;
using Case464.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Case464.EditorTools
{
    /// <summary>Explicit Edit Mode authoring of the six EXISTING actors only.</summary>
    public static class HotelNpcScreenPresentation
    {
        private const string ScenePath = "Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity";
        private const string MaterialPath = "Assets/Case464/NPC/Materials/ScreenAlignedNPC.mat";
        private static string Output => Path.GetFullPath(Application.dataPath +
            "/../../../art/Environment/HotelNpcPlacement/ScreenAligned_20261112");
        private static readonly string[] Ids = { "MATTO", "JJIRO", "BOKI", "MARIHI", "NAZ", "MOONIRE" };

        [Serializable] private sealed class SourceMetric
        {
            public string npc, asset, guid, sha256;
            public int width, height;
            public float pixelsPerUnit;
            public Vector2 pivot, opaqueFeet;
            public Rect opaquePixels;
        }
        [Serializable] private sealed class ImageMetric
        {
            public string view, npc;
            public bool visible;
            public Vector3 root;
            public float screenUpDot, screenRightDot, projectedAspect, sourceAspect, footPixelError;
        }
        [Serializable] private sealed class Report
        {
            public string scene;
            public SourceMetric[] sources;
            public ImageMetric[] views;
            public bool rootCoordinatesPreserved, cameraPreserved, saved;
        }
        private static HotelNpcActor[] ExistingActors()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open the exact latest map in Edit Mode.");
            var actors = scene.GetRootGameObjects().SelectMany(g =>
                g.GetComponentsInChildren<HotelNpcActor>(true)).ToArray();
            if (actors.Length != 6 || Ids.Any(id => actors.Count(a => a.NpcId == id) != 1))
                throw new InvalidOperationException("Expected the original six unique NPC actors.");
            return Ids.Select(id => actors.Single(a => a.NpcId == id)).ToArray();
        }

        [MenuItem("Case464/Hotel NPC/Repair Screen-Aligned Presentation (Existing 6 Only)")]
        public static void ApplyAndCapture()
        {
            var actors = ExistingActors();
            var scene = SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("Unowned unsaved changes; nothing discarded.");
            var positions = actors.Select(a => a.transform.position).ToArray();
            var shader = Shader.Find("Case464/NPC/ScreenAlignedUnlit");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("The imported URP NPC shader is unavailable.");
            var measurements = new List<SourceMetric>();
            var bounds = new Rect[6][];
            var feet = new Vector2[6][];
            // Resolve and measure every original before modifying any scene object.
            for (int i = 0; i < actors.Length; i++)
            {
                var data = new SerializedObject(actors[i]);
                var sprites = data.FindProperty("stillDirections");
                if (sprites.arraySize != 8) throw new InvalidOperationException("Eight original poses required.");
                bounds[i] = new Rect[8]; feet[i] = new Vector2[8];
                for (int d = 0; d < 8; d++)
                {
                    var sprite = sprites.GetArrayElementAtIndex(d).objectReferenceValue as Sprite;
                    if (sprite == null) throw new InvalidOperationException("An original pose is missing.");
                    var metric = Measure(actors[i].NpcId, sprite);
                    measurements.Add(metric); bounds[i][d] = metric.opaquePixels; feet[i][d] = metric.opaqueFeet;
                }
            }
            Directory.CreateDirectory(Output);
            var backupFolder = Path.GetFullPath(Application.dataPath + "/../Library/Case464NpcScreenReview");
            Directory.CreateDirectory(backupFolder);
            File.Copy(ScenePath, Path.Combine(backupFolder,
                "BeforePresentation_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff") + ".unity"), false);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Case464/NPC/Materials"))
                    AssetDatabase.CreateFolder("Assets/Case464/NPC", "Materials");
                material = new Material(shader) { name = "ScreenAlignedNPC" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
                throw new InvalidOperationException("Existing material belongs to another shader; no overwrite.");
            foreach (var actor in actors)
                Undo.RecordObjects(new Object[] { actor }.Concat(actor.GetComponentsInChildren<Transform>(true)).ToArray(),
                    "Repair existing NPC screen presentation");
            for (int i = 0; i < actors.Length; i++)
            {
                actors[i].ConfigureScreenPresentation(bounds[i], feet[i], material);
                EditorUtility.SetDirty(actors[i]);
            }
            if (actors.Where((actor, i) => actor.transform.position != positions[i]).Any())
                throw new InvalidOperationException("NPC root moved; save refused.");
            var metrics = Capture(actors, out bool cameraPreserved);
            // Undo diffs are normally recorded at frame end. Flush them BEFORE
            // saving so this explicit authoring action does not leave a late dirty flag.
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("NPC presentation save failed.");
            File.WriteAllText(Path.Combine(Output, "SCREEN_PRESENTATION.json"),
                JsonUtility.ToJson(new Report { scene = ScenePath, sources = measurements.ToArray(),
                    views = metrics, rootCoordinatesPreserved = true, cameraPreserved = cameraPreserved,
                    saved = true }, true));
            Debug.Log("[Case464 NPC] Original six actors screen-aligned and saved; no Play Mode/tests/build.");
        }

        private static SourceMetric Measure(string id, Sprite sprite)
        {
            string path = AssetDatabase.GetAssetPath(sprite);
            byte[] bytes = File.ReadAllBytes(path);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(image, bytes, false))
                    throw new InvalidOperationException("Source PNG could not be decoded: " + path);
                var pixels = image.GetPixels32();
                int left = image.width, bottom = image.height, right = -1, top = -1;
                for (int y = 0; y < image.height; y++)
                    for (int x = 0; x < image.width; x++)
                        if (pixels[y * image.width + x].a > 8)
                        { left = Math.Min(left, x); right = Math.Max(right, x);
                          bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
                if (right < left || top - bottom < 16)
                    throw new InvalidOperationException("Original alpha footprint invalid: " + path);
                int footLeft = image.width, footRight = -1;
                for (int y = bottom; y <= Math.Min(top, bottom + 7); y++)
                    for (int x = left; x <= right; x++)
                        if (pixels[y * image.width + x].a > 8)
                        { footLeft = Math.Min(footLeft, x); footRight = Math.Max(footRight, x); }
                string hash;
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                return new SourceMetric { npc = id, asset = path, guid = AssetDatabase.AssetPathToGUID(path),
                    sha256 = hash, width = image.width, height = image.height,
                    pixelsPerUnit = sprite.pixelsPerUnit, pivot = sprite.pivot,
                    opaquePixels = new Rect(left, bottom, right - left + 1, top - bottom + 1),
                    opaqueFeet = new Vector2((footLeft + footRight + 1) * .5f, bottom) };
            }
            finally { Object.DestroyImmediate(image); }
        }

        private static ImageMetric[] Capture(HotelNpcActor[] actors, out bool cameraPreserved)
        {
            var controller = Object.FindFirstObjectByType<HotelMapCameraController>();
            var camera = controller.ReviewCamera;
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            float size = camera.orthographicSize;
            var mode = controller.Mode; string room = controller.SelectedRoom, region = controller.SelectedRegion;
            var jobs = new List<(string, Action)> {
                ("Full", () => controller.Show(HotelMapView.PhysicalBuilding)),
                ("FirstFloor", () => controller.Show(HotelMapView.FirstFloor)),
                ("SecondFloor", () => controller.Show(HotelMapView.SecondFloor)) };
            foreach (var actor in actors)
            { string id = actor.NpcId, roomId = actor.RoomId; jobs.Add((id, () => controller.FocusRoom(roomId))); }
            var metrics = new List<ImageMetric>();
            try
            {
                foreach (var job in jobs)
                {
                    job.Item2(); controller.FinishTransition();
                    foreach (var actor in actors) actor.SendMessage("LateUpdate");
                    var target = RenderTexture.GetTemporary(1280, 720, 24);
                    var active = RenderTexture.active; Texture2D image = null;
                    try
                    {
                        var request = new RenderPipeline.StandardRequest { destination = target };
                        if (!RenderPipeline.SupportsRenderRequest(camera, request))
                            throw new InvalidOperationException("URP render capture unavailable.");
                        RenderPipeline.SubmitRenderRequest(camera, request);
                        RenderTexture.active = target;
                        image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(Output, "After_" + job.Item1 + ".png"), image.EncodeToPNG());
                        foreach (var actor in actors) metrics.Add(InspectProjection(job.Item1, actor, camera));
                    }
                    finally
                    {
                        RenderTexture.active = active;
                        if (image != null) Object.DestroyImmediate(image);
                        RenderTexture.ReleaseTemporary(target);
                    }
                }
            }
            finally
            {
                var state = new SerializedObject(controller);
                state.FindProperty("selectedRoom").stringValue = room;
                state.FindProperty("selectedRegion").stringValue = region;
                state.ApplyModifiedPropertiesWithoutUndo(); controller.Show(mode); controller.FinishTransition();
                foreach (var actor in actors) actor.SendMessage("LateUpdate");
            }
            cameraPreserved = camera.transform.position == position &&
                Quaternion.Angle(camera.transform.rotation, rotation) < .001f &&
                Mathf.Abs(camera.orthographicSize - size) < .00001f;
            if (!cameraPreserved) throw new InvalidOperationException("Camera restoration failed; save refused.");
            return metrics.ToArray();
        }

        private static ImageMetric InspectProjection(string view, HotelNpcActor actor, Camera camera)
        {
            var data = new SerializedObject(actor);
            var renderer = data.FindProperty("physical").objectReferenceValue as SpriteRenderer;
            int index = data.FindProperty("facing").enumValueIndex;
            var sprite = renderer.sprite;
            var rect = data.FindProperty("opaquePixelBounds").GetArrayElementAtIndex(index).rectValue;
            var feet = data.FindProperty("footPixels").GetArrayElementAtIndex(index).vector2Value;
            var origin = sprite.rect.min + sprite.pivot;
            Vector3 Project(Vector2 pixel) => camera.WorldToScreenPoint(renderer.transform.TransformPoint(
                new Vector3((pixel.x - origin.x) / sprite.pixelsPerUnit,
                    (pixel.y - origin.y) / sprite.pixelsPerUnit, 0)));
            var low = Project(rect.min); var highX = Project(new Vector2(rect.xMax, rect.yMin));
            var highY = Project(new Vector2(rect.xMin, rect.yMax));
            float ratio = Vector3.Distance(low, highX) / Vector3.Distance(low, highY);
            float expected = rect.width / rect.height;
            float footError = Vector2.Distance(Project(feet), camera.WorldToScreenPoint(actor.transform.position));
            float up = Vector3.Dot(renderer.transform.up, camera.transform.up);
            float right = Vector3.Dot(renderer.transform.right, camera.transform.right);
            if (renderer.enabled && (Mathf.Abs(ratio - expected) > .001f || footError > .1f ||
                up < .9999f || right < .9999f))
                throw new InvalidOperationException("NPC projection/pivot mismatch: " + actor.NpcId);
            return new ImageMetric { view = view, npc = actor.NpcId, visible = renderer.enabled,
                root = actor.transform.position, projectedAspect = ratio, sourceAspect = expected,
                footPixelError = footError, screenUpDot = up, screenRightDot = right };
        }
    }
}
