#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StairArchitectureVerification
{
    static int frames;
    static string Root => Path.GetFullPath("../../art/Mansion/Pass01/ArchitectureVerification");
    static StairArchitectureVerification() { EditorApplication.playModeStateChanged += State; EditorApplication.update+=Poll; }
    static void Poll() {
        string request=Path.GetFullPath("../../../stair-architecture.request");
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
        string stage=File.ReadAllText(request).Trim();File.Delete(request);
        if(stage=="Refresh"){AssetDatabase.Refresh();return;}
        Begin(stage);
    }
    public static void Baseline() => Begin("Before");
    public static void Final() => Begin("After");
    static void Begin(string stage) {
        SessionState.SetString("StairArchitecture.Stage",stage);
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
        if(stage=="After") MansionStructuralRepair.ApplyToScene();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state) {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetString("StairArchitecture.Stage","")!="") {
            frames=0; EditorApplication.update+=Tick;
        }
    }
    static void Tick() {
        if(++frames<120)return;
        EditorApplication.update-=Tick;
        try {
            string stage=SessionState.GetString("StairArchitecture.Stage","");
            Directory.CreateDirectory(Path.Combine(Root,stage));
            var flight=GameObject.Find(MapData.ConnectionsName).transform.Find("DOOR_05_08/DOOR_05_08_A_COLLISION_Flight");
            var ramp=flight!=null?flight.GetComponent<Collider>():null;
            if(ramp!=null)File.WriteAllLines(Path.Combine(Root,stage,"kitchen-colliders.txt"),UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.bounds.Intersects(ramp.bounds)).Select(c=>c.transform.parent.name+"/"+c.name+" bounds="+c.bounds));
            if(stage=="Rules") {
                var flags=BindingFlags.NonPublic|BindingFlags.Static;
                typeof(SinglePlayerE2E).GetField("gm",flags).SetValue(null,GameManager.Instance);
                typeof(SinglePlayerE2E).GetMethod("FullGames",flags).Invoke(null,null);
                typeof(SinglePlayerE2E).GetMethod("Focused",flags).Invoke(null,null);
                File.WriteAllText(Path.Combine(Root,stage,"results.txt"),"SINGLE_PLAYER_E2E: OK assertions="+typeof(SinglePlayerE2E).GetField("assertions",flags).GetValue(null));
            } else foreach(int room in new[]{1,4,5,7,8}) Capture(room,stage);
            File.WriteAllText(Path.Combine(Root,stage,"captures.txt"),"Actual Play Mode Camera.main; original rotation and offset; zooms 9/19/32. Additional orthogonal diagnostic views.\n");
            SessionState.SetString("StairArchitecture.Stage","");
            EditorApplication.isPlaying=false;
            if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else EditorApplication.isPlaying=false; }
    }
    static void Capture(int room,string stage) {
        GameManager.Instance.Human.RoomId=room;
        var actor=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Single(a=>a.PlayerId==0).Agent;
        Vector3 placement=room==1?new Vector3(-1.3f,0,4.1f):MansionStairArchitecture.Load().entries.Single(e=>e.room==room).position;
        if(UnityEngine.AI.NavMesh.SamplePosition(placement,out var sample,.6f,UnityEngine.AI.NavMesh.AllAreas)){
            actor.ResetPath();actor.Warp(sample.position);
        }
        var cam=Camera.main; var controller=cam.GetComponent<PcCameraController>();
        controller.ResetView();
        var cutaway=cam.GetComponent<MansionCutawayView>();
        typeof(MansionCutawayView).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cutaway,null);
        if(room==8){
            var rooms=GameObject.Find(MapData.RootName).transform;
            Transform Room(int id)=>rooms.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
            if(!Room(1).Find("Floor").GetComponent<Renderer>().enabled || Room(5).Find("Floor").GetComponent<Renderer>().enabled)
                throw new Exception("Cellar cutaway must retain Hall floor and hide covering kitchen floor");
            if(!Room(5).Find("Floor").GetComponent<Collider>().enabled)throw new Exception("Cutaway disabled physical floor");
        }
        foreach(float zoom in new[]{9f,19f,32f}) {
            cam.orthographicSize=zoom; Save(cam,Path.Combine(Root,stage,$"Room_{room}_Play_Zoom_{zoom}.png"));
        }
        Vector3 pos=cam.transform.position;Quaternion rot=cam.transform.rotation;
        var center=MapData.GetRoom(room).Center;
        foreach(var view in new[]{("Top",new Vector3(0,30,0.001f)),("Front",new Vector3(0,2,-30)),("Side",new Vector3(30,2,0)),("Iso",new Vector3(24,24,-24))}) {
            cam.transform.position=center+view.Item2;cam.transform.LookAt(center);cam.orthographicSize=10;
            Save(cam,Path.Combine(Root,stage,$"Room_{room}_{view.Item1}.png"));
        }
        cam.transform.SetPositionAndRotation(pos,rot);
    }
    static void Save(Camera cam,string path) {
        var rt=new RenderTexture(1600,1000,24);var prior=cam.targetTexture;var active=RenderTexture.active;
        cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());cam.targetTexture=prior;RenderTexture.active=active;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
}
#endif
