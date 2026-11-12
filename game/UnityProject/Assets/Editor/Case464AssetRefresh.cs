using System;
using System.IO;
using System.Linq;
using Case464.NPC;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Case464.EditorTools
{
    /// <summary>
    /// Explicit, local authoring queue. File import alone never saves a scene.
    /// Submit one request AFTER all external file writes have completed.
    /// No Play Mode, test, build, arbitrary eval or menu execution is exposed.
    /// </summary>
    public static class Case464AssetRefresh
    {
        private const string StateKey = "Case464.AssetRefresh.Active";
        private const string SessionKey = "Case464.AssetRefresh.Session";
        public const string BootstrapKey = "Case464.AssetRefresh.BootstrapId";
        private static readonly string Folder = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/Case464AssetRefresh"));
        private static string session;
        private static Work work;
        private static double nextTick, idleSince;
        private static bool inTick;

        [Serializable] public sealed class AssetCheck
        {
            public string path;
            public string type; // Sprite, SceneAsset or Object
        }
        [Serializable] public sealed class Request
        {
            public string id, session, project, scene;
            public bool scriptsChanged, saveScene = true, checkHotelNpcs = true;
            public int timeoutSeconds = 300;
            public AssetCheck[] assets = Array.Empty<AssetCheck>();
        }
        [Serializable] private sealed class NpcPose
        {
            public string id, room;
            public Vector3 anchor, localPosition, localScale, displayPosition, displayScale;
            public string[] sprites;
        }
        [Serializable] private sealed class Work
        {
            public Request request;
            public string phase, error, createdUtc;
            public int refreshCount, reloadCount;
            public bool compileObserved, compileFinished, compileFailed;
            public NpcPose[] before;
        }
        [Serializable] private sealed class Status
        {
            public string session, project, utc, id, phase, error, scene, unityVersion;
            public int editorProcessId, refreshCount, reloadCount, spritesLoaded;
            public bool compiling, updating, playing, saved, npcAnchorsPreserved;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Asset reads happen on a later idle update, not during assembly initialization.
            session = SessionState.GetString(SessionKey, "");
            if (string.IsNullOrEmpty(session))
            {
                session = Guid.NewGuid().ToString("N");
                SessionState.SetString(SessionKey, session);
            }
            string json = SessionState.GetString(StateKey, "");
            work = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Work>(json);
            if (work != null && !Terminal(work.phase))
            {
                work.reloadCount++;
                if (work.phase == "ReloadRequested") work.phase = "Validate";
                Persist();
            }
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += Persist;
        }

        private static bool Terminal(string phase) =>
            phase == "Complete" || phase == "Failed";

        private static void OnCompilationStarted(object context)
        {
            if (work == null || Terminal(work.phase)) return;
            work.compileObserved = true;
            work.compileFinished = false;
            Persist();
        }
        private static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages)
        {
            if (work == null || Terminal(work.phase)) return;
            if (messages.Any(m => m.type == CompilerMessageType.Error))
            {
                work.compileFailed = true;
                work.error = string.Join("\n", messages.Where(m =>
                    m.type == CompilerMessageType.Error).Select(m => m.message));
                Persist();
            }
        }
        private static void OnCompilationFinished(object context)
        {
            if (work == null || Terminal(work.phase)) return;
            work.compileFinished = true;
            Persist();
        }
        private static void Persist()
        {
            if (work == null) return;
            SessionState.SetString(StateKey, JsonUtility.ToJson(work));
        }

        private static void Tick()
        {
            if (inTick || EditorApplication.timeSinceStartup < nextTick) return;
            nextTick = EditorApplication.timeSinceStartup + .25;
            inTick = true;
            try
            {
                Directory.CreateDirectory(Folder);
                WriteStatus("heartbeat.json");
                if (work == null || Terminal(work.phase))
                {
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                    AcceptRequest();
                }
                if (work == null || Terminal(work.phase)) return;
                if ((DateTime.UtcNow - DateTime.Parse(work.createdUtc,
                        null, System.Globalization.DateTimeStyles.RoundtripKind)).TotalSeconds >
                    work.request.timeoutSeconds)
                    throw new TimeoutException("Editor refresh timed out in " + work.phase +
                        ". No retry or scene deletion was performed.");
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Edit Mode is required.");
                // isCompiling can remain true while Unity defers a completed compilation's
                // domain reload in the background. The compilationFinished event is the
                // authority for requesting that reload; do not wait forever for focus.
                if (work.phase == "Waiting" && work.compileFinished &&
                    !work.compileFailed && !EditorApplication.isUpdating && work.reloadCount == 0)
                {
                    work.phase = "ReloadRequested";
                    Persist();
                    EditorUtility.RequestScriptReload();
                    return;
                }
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    idleSince = 0;
                    return;
                }
                // A new corrected batch must be allowed to clear an earlier compile failure.
                // Check the result of THIS refresh, not stale errors before importing fixes.
                if (work.phase != "Refresh" &&
                    (work.compileFailed || EditorUtility.scriptCompilationFailed))
                    throw new InvalidOperationException("C# compilation failed. " + work.error);
                if (idleSince == 0) idleSince = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - idleSince < 1.5) return;

                if (work.phase == "Refresh")
                {
                    RequireScene();
                    work.before = CaptureNpcs();
                    // Persist BEFORE Refresh: compilation/reload may interrupt this stack.
                    work.phase = "Waiting";
                    work.refreshCount = 1;
                    Persist();
                    idleSince = 0;
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    return;
                }
                if (work.phase == "Waiting")
                {
                    if (work.compileObserved && !work.compileFinished) return;
                    if ((work.request.scriptsChanged || work.compileObserved) && work.reloadCount == 0)
                    {
                        work.phase = "ReloadRequested";
                        Persist();
                        EditorUtility.RequestScriptReload();
                        return;
                    }
                    work.phase = "Validate";
                    Persist();
                }
                if (work.phase == "Validate")
                {
                    ValidateAndSave();
                    work.phase = "Complete";
                    Persist();
                    WriteStatus("result_" + work.request.id + ".json");
                }
            }
            catch (Exception error)
            {
                if (work != null && !Terminal(work.phase))
                {
                    work.phase = "Failed";
                    work.error = error.ToString();
                    Persist();
                    WriteStatus("result_" + work.request.id + ".json");
                }
                Debug.LogError("[Case464 Asset Refresh] " + error.Message);
                // No automatic retry: the same technical failure is never looped.
            }
            finally { inTick = false; }
        }

        private static void AcceptRequest()
        {
            string file = Path.Combine(Folder, "request.json");
            if (!File.Exists(file)) return;
            var request = JsonUtility.FromJson<Request>(File.ReadAllText(file));
            if (request == null || !Guid.TryParseExact(request.id, "N", out _)) return;
            if (File.Exists(Path.Combine(Folder, "result_" + request.id + ".json"))) return;
            // SessionState is the authority: a disk request is not replayed in a new Editor.
            bool bootstrap = SessionState.GetString(BootstrapKey, "") == request.id;
            if (!bootstrap && request.session != session) return;
            work = new Work { request = request, phase = bootstrap ? "Validate" : "Refresh",
                createdUtc = DateTime.UtcNow.ToString("O"), refreshCount = bootstrap ? 1 : 0,
                reloadCount = bootstrap ? 1 : 0 };
            Persist();
            SessionState.EraseString(BootstrapKey);
            if (!Path.GetFullPath(request.project).TrimEnd('/', '\\').Equals(
                    Path.GetFullPath(Application.dataPath + "/..").TrimEnd('/', '\\'),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Request targets a different Unity project.");
            request.timeoutSeconds = Mathf.Clamp(request.timeoutSeconds, 30, 900);
            RequireScene();
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException(
                    "Unowned unsaved scene changes exist. Nothing was discarded or saved.");
            work.before = CaptureNpcs();
            Persist();
            idleSince = 0;
        }

        private static void RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path) ||
                scene.path != work.request.scene)
                throw new InvalidOperationException("Expected active saved scene: " +
                    work.request.scene + "; actual: " + scene.path);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                throw new InvalidOperationException("Active scene asset did not load.");
        }
        private static NpcPose[] CaptureNpcs()
        {
            if (!work.request.checkHotelNpcs) return Array.Empty<NpcPose>();
            var actors = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<HotelNpcActor>(true)).ToArray();
            string[] ids = { "MATTO", "MARIHI", "NAZ", "BOKI", "JJIRO", "MOONIRE" };
            if (actors.Length != 6 || ids.Any(id => actors.Count(a => a.NpcId == id) != 1))
                throw new InvalidOperationException("Expected the existing six unique hotel NPCs.");
            return actors.OrderBy(a => a.NpcId, StringComparer.Ordinal).Select(actor =>
            {
                var data = new SerializedObject(actor);
                var poses = data.FindProperty("stillDirections");
                if (poses.arraySize != 8)
                    throw new InvalidOperationException("Eight source sprites required: " + actor.NpcId);
                var sprites = Enumerable.Range(0, 8).Select(i =>
                {
                    var sprite = poses.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    if (sprite == null) throw new InvalidOperationException("Missing NPC Sprite reference.");
                    string path = AssetDatabase.GetAssetPath(sprite);
                    if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                        throw new InvalidOperationException("Sprite import unresolved: " + path);
                    return AssetDatabase.AssetPathToGUID(path);
                }).ToArray();
                var world = data.FindProperty("physical").objectReferenceValue as SpriteRenderer;
                var display = data.FindProperty("splitDisplay").objectReferenceValue as SpriteRenderer;
                if (world == null || display == null || world.sprite == null || display.sprite == null)
                    throw new InvalidOperationException("NPC renderer reference missing.");
                return new NpcPose { id = actor.NpcId, room = actor.RoomId,
                    anchor = actor.transform.position, sprites = sprites,
                    localPosition = world.transform.localPosition, localScale = world.transform.localScale,
                    displayPosition = display.transform.localPosition, displayScale = display.transform.localScale };
            }).ToArray();
        }
        private static void ValidateAndSave()
        {
            RequireScene();
            foreach (var asset in work.request.assets ?? Array.Empty<AssetCheck>())
            {
                if (asset == null || string.IsNullOrEmpty(asset.path) ||
                    !asset.path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    asset.path.Split('/').Contains(".."))
                    throw new InvalidOperationException("Invalid asset verification path.");
                Type type = asset.type == "Sprite" ? typeof(Sprite) :
                    asset.type == "SceneAsset" ? typeof(SceneAsset) : typeof(UnityEngine.Object);
                if (AssetDatabase.LoadAssetAtPath(asset.path, type) == null)
                    throw new InvalidOperationException("Asset did not load: " + asset.path);
            }
            var after = CaptureNpcs();
            if (JsonUtility.ToJson(new PoseList { items = work.before }) !=
                JsonUtility.ToJson(new PoseList { items = after }))
                throw new InvalidOperationException("NPC anchors, scales or original sprite references changed.");
            var scene = SceneManager.GetActiveScene();
            if (scene.GetRootGameObjects().SelectMany(g =>
                    g.GetComponentsInChildren<Transform>(true)).Any(t =>
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0))
                throw new InvalidOperationException("Scene contains missing script references.");
            if (!work.request.saveScene) return;
            string backup = Path.Combine(Folder, work.request.id + "_before_save.unity");
            if (!File.Exists(backup)) File.Copy(scene.path, backup, false);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Saving the validated active scene failed.");
            // Do not SaveAssets globally: unrelated dirty fonts/assets belong to the user.
        }
        [Serializable] private sealed class PoseList { public NpcPose[] items; }
        private static void WriteStatus(string name)
        {
            var status = new Status { session = session,
                project = Path.GetFullPath(Application.dataPath + "/.."),
                utc = DateTime.UtcNow.ToString("O"),
                editorProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                unityVersion = Application.unityVersion,
                compiling = EditorApplication.isCompiling, updating = EditorApplication.isUpdating,
                playing = EditorApplication.isPlayingOrWillChangePlaymode,
                scene = SceneManager.GetActiveScene().path,
                id = work?.request.id, phase = work?.phase ?? "Idle", error = work?.error,
                refreshCount = work?.refreshCount ?? 0, reloadCount = work?.reloadCount ?? 0,
                spritesLoaded = work?.phase == "Complete" && work.request.checkHotelNpcs ? 48 : 0,
                saved = work?.phase == "Complete" && work.request.saveScene,
                npcAnchorsPreserved = work?.phase == "Complete" };
            string destination = Path.Combine(Folder, name), temporary = destination + ".pending";
            File.WriteAllText(temporary, JsonUtility.ToJson(status, true));
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(temporary, destination);
        }

        /// <summary>For future Editor authoring tools; never hold editing across reloads.</summary>
        public static void RunAssetBatch(Action writeAssets)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for idle Edit Mode before writing assets.");
            AssetDatabase.StartAssetEditing();
            try { writeAssets(); }
            finally { AssetDatabase.StopAssetEditing(); }
            // Caller submits ONE request only after every file write has completed.
        }
    }
}
