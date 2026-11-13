using System;
using System.IO;
using System.Linq;
using Case464.Hotel;
using Case464.RoseMansion;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Case464.EditorTools
{
    public static class RoseMansionCoreAuthoring
    {
        public const string Source="Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_YearEnd_EntrySnow_Work.unity";
        public const string Target="Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Gameplay_Work.unity";
        private const string Data="Assets/Case464/RoseMansion/Data/RoseMansionContent.json";
        private static string Snapshot(Scene scene)=>string.Join("\n",scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Component>(true))
            .Where(c=>c is MeshRenderer||c is MeshFilter||c is Camera||c is Light||c is HotelMapCameraController||c is HotelOverviewRoomArea)
            .OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c)+":"+EditorJsonUtility.ToJson(c.transform)));

        [MenuItem("Case464/Rose Mansion/Create Characterless Gameplay Scene")]
        public static void Create()
        {
            var source=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||source.path!=Source||
               Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty||s.isLoaded&&string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("Open the clean, saved NPC-free seasonal map. No changes were discarded.");
            if(File.Exists(Target)||File.Exists(Target+".meta"))throw new IOException("Gameplay scene already exists; no overwrite.");
            if(Object.FindObjectsByType<Case464.NPC.HotelNpcActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any())
                throw new InvalidOperationException("NPC map instances must remain absent.");
            var data=AssetDatabase.LoadAssetAtPath<TextAsset>(Data);
            var catalog=data==null?null:JsonUtility.FromJson<RoseCatalog>(data.text);
            if(catalog==null||catalog.ValidateStructure()!=null)throw new InvalidOperationException(catalog?.ValidateStructure()??"Gameplay catalog missing.");
            var regular=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Pretendard-Regular SDF.asset");
            var heading=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/MaruBuri-SemiBold SDF.asset");
            if(regular==null||heading==null)throw new InvalidOperationException("Existing Korean fonts are required.");
            string backup="Assets/Case464/Scenes/Hotel/Backups/Hotel_RoseBlack_CoreSource_"+DateTime.UtcNow.AddHours(9).ToString("yyyyMMdd_HHmmss")+".unity";
            if(File.Exists(backup)||!EditorSceneManager.SaveScene(source,backup,true))throw new IOException("Source backup failed.");
            AssetDatabase.ImportAsset(backup,ImportAssetOptions.ForceSynchronousImport);
            if(!AssetDatabase.CopyAsset(Source,Target))throw new IOException("Scene copy failed; source retained.");
            var scene=EditorSceneManager.OpenScene(Target,OpenSceneMode.Single);
            if(!scene.IsValid()||!scene.isLoaded||SceneManager.GetActiveScene()!=scene)throw new InvalidOperationException("Gameplay scene was not loaded as active.");
            string before=Snapshot(scene);
            var map=Object.FindFirstObjectByType<HotelMapCameraController>();
            if(map==null||map.ReviewCamera==null)throw new InvalidOperationException("Existing hotel camera controller missing.");
            // Keep the legacy UI in the derived scene, but never run a second save/verdict system.
            var legacy=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(c=>c!=null&&(c.GetType().Namespace=="Mystery.Interrogation"||c.GetType().Namespace=="Mystery.UI")).ToArray();
            foreach(var component in legacy)component.enabled=false;
            foreach(var canvas in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)))canvas.enabled=false;
            var root=new GameObject("Rose Mansion | final investigation systems");
            var session=root.AddComponent<RoseMansionSession>();var bridge=root.AddComponent<RoseMansionPresentation>();
            var director=root.AddComponent<PlayableDirector>();director.playOnAwake=false;
            bridge.Configure(session,director);session.Configure(data,map,bridge);
            var go=new GameObject("Rose Mansion | gameplay UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.layer=5;go.transform.SetParent(root.transform,false);
            var ui=go.GetComponent<Canvas>();ui.renderMode=RenderMode.ScreenSpaceOverlay;ui.sortingOrder=200;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            var view=go.AddComponent<RoseMansionView>();view.Configure(session,map,regular,heading);view.BuildAuthoringPreview();
            if(!Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any())
                new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            if(Snapshot(scene)!=before)throw new InvalidOperationException("Protected map component changed; source and partial scene retained.");
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Gameplay scene save failed.");
            var report=new{source=Source,backup,scene=Target,rooms=catalog.rooms.Length,pendingSlots=catalog.Investigable.Sum(r=>r.slots.Count(s=>!s.Ready)),
                pendingTruthBranches=catalog.branches.Count(b=>!catalog.TruthReady(b)),npcs=0,mapComponentsUnchanged=true,
                disabledLegacyComponents=legacy.Select(c=>c.GetType().FullName).ToArray(),playModeRun=false,functionalTestsRun=false,buildRun=false,
                dependencies=AssetDatabase.GetDependencies(Target,true)};
            string folder=Application.dataPath+"/../../../art/Development/RoseMansionCore";
            Directory.CreateDirectory(folder);File.WriteAllText(folder+"/ASSEMBLY.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        }

        // Authoring-only render. This does not call NewGame, inspect a slot, advance a turn,
        // submit an accusation, create a player save, or run a functional test.
        [MenuItem("Case464/Rose Mansion/Capture Current UI Preview")]
        public static void CapturePreview()=>Capture("Main");
        public static void Capture(string screen)
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=Target||scene.isDirty)throw new InvalidOperationException("Clean gameplay authoring scene required.");
            var view=Object.FindFirstObjectByType<RoseMansionView>();var canvas=view.GetComponent<Canvas>();
            var camera=Object.FindFirstObjectByType<HotelMapCameraController>().ReviewCamera;
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
            var rt=RenderTexture.GetTemporary(1920,1080,24);var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                view.BuildAuthoringPreview(screen);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;
                Canvas.ForceUpdateCanvases();UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt});
                RenderTexture.active=rt;image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                string folder=Application.dataPath+"/../../../art/Development/RoseMansionCore/UI";Directory.CreateDirectory(folder);
                File.WriteAllBytes(folder+"/"+screen+".png",image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
                RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(rt);
                view.BuildAuthoringPreview("Main");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
