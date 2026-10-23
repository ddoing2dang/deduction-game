#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;
using DeductionGame;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.AI;

[InitializeOnLoad]
public static class MansionStructureAudit
{
 static string Root=>Path.GetFullPath("../../art/Mansion/Pass01/StructureAudit");
 static string Request=>Path.GetFullPath("../../../mansion-structure.request");
 static int frames;
 static readonly List<string> Results=new();static NavMeshAgent actor;static int routeIndex;static bool moving;static float deadline,oldMaximumDelta;static int lastFrame;static Quaternion lastRotation;static Vector3 targetPosition;
 static MansionStructureAudit(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=State;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
  string stage=File.ReadAllText(Request).Trim();File.Delete(Request);
  SessionState.SetString("MansionStructure.Stage",stage);
  EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
  if(stage=="After"){MansionEnvironmentCleanup.Apply();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
  EditorApplication.isPlaying=true;
 }
 static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetString("MansionStructure.Stage","")!=""){frames=0;moving=false;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;}
 static void Tick(){
  if(moving){MoveTick();return;}
  if(++frames<90)return;EditorApplication.update-=Tick;
  string stage=SessionState.GetString("MansionStructure.Stage","");
  try{
   Directory.CreateDirectory(Path.Combine(Root,stage));
   if(stage=="Visual"){
    foreach(int room in Enumerable.Range(1,9)){
     Capture(room,stage);var cam=Camera.main;var focus=MapData.GetRoom(room).Center;
     foreach(var angle in new[]{("Front",new Vector3(0,0,-30)),("Side",new Vector3(30,0,0)),("Iso",new Vector3(24,24,-28))}){
      cam.transform.position=focus+angle.Item2;cam.transform.LookAt(focus);cam.orthographicSize=10;Save(cam,Path.Combine(Root,stage,$"Room_{room}_{angle.Item1}.png"));
     }
    }
    GameManager.Instance.Human.RoomId=1;Camera.main.GetComponent<PcCameraController>().ResetView();
    Camera.main.transform.rotation=Quaternion.Euler(42,0,0);
    var ui=UnityEngine.Object.FindFirstObjectByType<RuntimeGameUI>();ui.Refresh();
    var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
    var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
    try{foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera.main;c.planeDistance=1;}Canvas.ForceUpdateCanvases();Save(Camera.main,Path.Combine(Root,stage,"Play_UI.png"));}
    finally{for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}}
    File.WriteAllText(Path.Combine(Root,stage,"capture-status.txt"),"63 actual Unity Play camera captures; nine rooms x (3 zooms, top/front/side/iso); UI rendered through existing Canvas with modes restored.");
    SessionState.SetString("MansionStructure.Stage","");EditorApplication.isPlaying=false;return;
   }
   if(stage=="Diagnostics"){
    var lines=new List<string>();var r=MansionEnvironmentCleanup.Read().layout.routes.Single(r=>r.door=="DOOR_03_05");
    foreach(float x in new[]{6.5f,7.5f,8.5f,9.5f})foreach(float z in new[]{0f,-1f,-2f,-3f,-4f,-5.4f}){
     var p=new Vector3(x,0,z);bool found=NavMesh.SamplePosition(p,out var hit,.25f,NavMesh.AllAreas);
     lines.Add(p+" nav="+found+" sample="+hit.position+" ground="+string.Join(";",Physics.RaycastAll(p+Vector3.up*.3f,Vector3.down,.8f).Select(h=>h.collider.name+":"+h.point.y+":"+h.normal)));
    }
    var checkpoints=new[]{new Vector3(9.5f,0,1.1f),new Vector3(9.5f,0,-1),new Vector3(9.5f,0,-4),new Vector3(9.5f,0,-5.4f),new Vector3(7.5f,0,-5.4f),new Vector3(5.25f,0,-4.9f)};
    for(int i=0;i<checkpoints.Length-1;i++){var path=new NavMeshPath();NavMesh.CalculatePath(checkpoints[i],checkpoints[i+1],NavMesh.AllAreas,path);lines.Add("SEGMENT "+i+" "+path.status+" "+string.Join(";",path.corners.Select(v=>v.ToString())));}
    var corridor=GameObject.Find(MapData.ConnectionsName).transform.Find("DOOR_03_05");
    lines.AddRange(corridor.GetComponentsInChildren<Collider>().Select(c=>c.name+" "+c.bounds));
    foreach(var service in MansionEnvironmentCleanup.Read().layout.routes.Where(t=>t.b>=7&&t.a<7)){
     var samples=new List<Vector3>();for(int k=0;k<service.points.Length-1;k++)for(int n=0;n<5;n++)samples.Add(Vector3.Lerp(service.points[k],service.points[k+1],n/5f));samples.Add(service.points[^1]);
     foreach(var p in samples){bool found=NavMesh.SamplePosition(p,out var hit,.3f,NavMesh.AllAreas);lines.Add(service.door+" "+p+" nav="+found+" "+hit.position+" ground="+string.Join(";",Physics.RaycastAll(p+Vector3.up*.3f,Vector3.down,.8f).Select(h=>h.collider.name+":"+h.point+":"+h.normal)));}
     lines.AddRange(GameObject.Find(MapData.ConnectionsName).transform.Find(service.door).GetComponentsInChildren<Collider>().Where(c=>c.name.Contains("Vestibule")).Select(c=>c.name+" "+c.bounds));
     for(int k=0;k<service.points.Length-1;k++){var path=new NavMeshPath();NavMesh.CalculatePath(service.points[k],service.points[k+1],NavMesh.AllAreas,path);lines.Add(service.door+" SEG "+k+" "+path.status+" "+string.Join(";",path.corners.Select(v=>v.ToString())));}
    }
    File.WriteAllLines(Path.Combine(Root,stage,"nav.txt"),lines);SessionState.SetString("MansionStructure.Stage","");EditorApplication.isPlaying=false;return;
   }
   var rows=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
    .Where(t=>t.gameObject.scene.name=="Game").Select(t=>new Row{path=PathOf(t),position=t.position,scale=t.lossyScale,components=t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray(),mesh=MeshName(t)}).ToArray();
   File.WriteAllText(Path.Combine(Root,stage,"unity-inventory.json"),JsonUtility.ToJson(new Inventory{objects=rows},true));
   foreach(int room in Enumerable.Range(1,9))Capture(room,stage);
   File.WriteAllText(Path.Combine(Root,stage,"capture-status.txt"),"COMPLETE: 9 rooms; Camera.main play zooms 9/19/32 and top views");
   if(stage=="After"){
    Results.Clear();oldMaximumDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=1f/60f;
    Validate();actor=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Single(a=>a.PlayerId==0).Agent;
    routeIndex=-1;moving=true;EditorApplication.update+=Tick;NextRoute();return;
   }
  }catch(Exception ex){File.WriteAllText(Path.Combine(Root,stage,"FAILED.txt"),ex.ToString());Debug.LogException(ex);}
  SessionState.SetString("MansionStructure.Stage","");EditorApplication.isPlaying=false;
 }
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static void Require(bool value,string message){if(!value)throw new Exception(message);Results.Add("OK "+message);File.WriteAllLines(Path.Combine(Root,"After","play-results.txt"),Results);}
 static Transform Room(int id)=>GameObject.Find(MapData.RootName).transform.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
 static void Validate(){
  Require(UnityEngine.Object.FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None).Length==9,"9 RoomNodes");
  Require(GameObject.Find(MapData.ConnectionsName).transform.Cast<Transform>().Count(t=>t.name.StartsWith("DOOR_")&&!t.name.Contains("SECRET"))==11,"11 LOCK DoorIDs");
  Require(UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Length==6,"6 CharacterActors");
  Require(Camera.main.GetComponent<PcCameraController>()!=null&&Camera.main.GetComponent<MansionCutawayView>()!=null,"existing camera and cutaway");
  foreach(int id in Enumerable.Range(1,9)){
   var points=Room(id).Find("NavigationPoints");
   Require(Vector3.Distance(points.Find("Action_Point").position,MapData.GetRoom(id).Center+Vector3.up*.05f)<.00001f,"ActionPoint unchanged "+id);
   for(int slot=0;slot<6;slot++){
    Require(Vector3.Distance(points.Find($"Stand_{slot+1:00}").position,MapData.GetStandPoint(id,slot))<.00001f,"StandPoint unchanged "+id+"/"+slot);
    Require(NavMesh.SamplePosition(RoomPresentation.Stand(id,slot),out var hit,.3f,NavMesh.AllAreas)&&Vector3.Distance(hit.position,RoomPresentation.Stand(id,slot))<.3f,"StandPoint baked "+id+"/"+slot);
   }
   Require(Room(id).Find("Floor").GetComponent<Collider>().enabled,"physical room floor enabled "+id);
  }
  foreach(var e in MansionEnvironmentCleanup.Read().layout.entries)Require(Vector3.Distance(Room(e.room).Find($"NavigationPoints/Entry_To_{e.to:00}").position,e.position)<.00001f,"Blender/Unity Entry alignment "+e.room+"->"+e.to);
  Require(Room(1).Find("DecorativeGallery").GetComponentsInChildren<Collider>(true).Length==0,"Gallery is decorative, no extra movement node");
 }
 static string Label(){var r=MansionEnvironmentCleanup.Read().layout.routes[routeIndex/2];return routeIndex%2==0?r.a+"->"+r.b:r.b+"->"+r.a;}
 static void NextRoute(){
  routeIndex++;
  var routes=MansionEnvironmentCleanup.Read().layout.routes;
  if(routeIndex>=routes.Length*2){Finish();return;}
  var r=routes[routeIndex/2];int a=routeIndex%2==0?r.a:r.b,b=routeIndex%2==0?r.b:r.a;
  var start=Room(a).Find($"NavigationPoints/Entry_To_{b:00}").position;var end=Room(b).Find($"NavigationPoints/Entry_To_{a:00}").position;
  Require(NavMesh.SamplePosition(start,out var begin,.6f,NavMesh.AllAreas),"start Entry sampled "+Label());
  Require(NavMesh.SamplePosition(end,out var finish,.6f,NavMesh.AllAreas),"end Entry sampled "+Label());
  actor.ResetPath();Require(actor.Warp(begin.position),"actual CharacterActor positioned "+Label());
  var path=new NavMeshPath();Require(NavMesh.CalculatePath(begin.position,finish.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"complete single NavMesh path "+Label());
  float length=0,planned=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);for(int i=1;i<r.points.Length;i++)planned+=Vector3.Distance(r.points[i-1],r.points[i]);
  Require(length<planned+5,"no long detour "+Label()+" length="+length.ToString("F2")+" corners="+string.Join(";",path.corners.Select(p=>p.ToString())));
  foreach(Vector3 p in path.corners){
   bool Inside(int id){var room=MapData.GetRoom(id);return Mathf.Abs(p.x-room.Center.x)<room.Size.x/2+.15f&&Mathf.Abs(p.z-room.Center.z)<room.Size.y/2+.15f&&Mathf.Abs(p.y-room.Center.y)<.6f;}
   float near=float.PositiveInfinity;for(int i=1;i<r.points.Length;i++){Vector3 d=r.points[i]-r.points[i-1];float t=Mathf.Clamp01(Vector3.Dot(p-r.points[i-1],d)/d.sqrMagnitude);near=Mathf.Min(near,Vector3.Distance(p,r.points[i-1]+d*t));}
   Require(Inside(a)||Inside(b)||near<1.4f,"path stays in own rooms/corridor "+Label()+" "+p);
  }
  GameManager.Instance.Human.RoomId=a;targetPosition=finish.position;Require(actor.SetPath(path),"actual agent accepted path "+Label());deadline=Time.realtimeSinceStartup+45;lastFrame=Time.frameCount;lastRotation=actor.transform.rotation;
 }
 static void MoveTick(){try{
  if(Time.realtimeSinceStartup>deadline)throw new Exception("movement timeout "+Label()+" at "+actor.transform.position);
  RequireFrame();
  if(actor.pathPending)return;
  if(actor.isOnOffMeshLink)throw new Exception("off-mesh fallback used "+Label());
  if(Vector3.Distance(actor.transform.position,targetPosition)<.22f){Require(true,"CharacterActor physically traversed "+Label()+" without wall penetration, unsupported steps or fallback");NextRoute();}
  else if(!actor.hasPath)throw new Exception("agent stopped before reaching actual destination "+Label()+" "+actor.transform.position);
 }catch(Exception ex){Fail(ex);}}
 static void RequireFrame(){
  Vector3 p=actor.transform.position;
  if(lastFrame!=Time.frameCount){if(Quaternion.Angle(lastRotation,actor.transform.rotation)>720*Time.deltaTime+1)throw new Exception("rotation jump "+Label());lastRotation=actor.transform.rotation;lastFrame=Time.frameCount;}
  var floorHits=Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,.65f,~0,QueryTriggerInteraction.Ignore);
  if(!floorHits.Any(h=>!h.collider.transform.IsChildOf(actor.transform)&&h.normal.y>.5f))throw new Exception("unsupported actor/air movement "+Label()+" "+p);
  var probe=new GameObject("StructureCapsuleProbe");var capsule=probe.AddComponent<CapsuleCollider>();capsule.radius=.3f;capsule.height=1;capsule.center=Vector3.up*.6f;capsule.enabled=false;
  try{foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*.8f,.3f,~0,QueryTriggerInteraction.Ignore)){
   if(collider.transform.IsChildOf(actor.transform)||collider.bounds.max.y<p.y+.23f)continue;
   if(Physics.ComputePenetration(capsule,p,Quaternion.identity,collider,collider.transform.position,collider.transform.rotation,out _,out float depth)&&depth>.015f)throw new Exception("wall/door/railing penetration "+Label()+" "+collider.name+" depth="+depth);
  }}finally{UnityEngine.Object.DestroyImmediate(probe);}
 }
 static void Set(GameManager gm,string field,object value)=>typeof(GameManager).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gm,value);
 static void Finish(){try{
  var gm=GameManager.Instance;
  Set(gm,"<Round>k__BackingField",6);Set(gm,"secretPassageA",9);Set(gm,"secretPassageB",6);Set(gm,"secretPassageUntilRound",6);
  var view=UnityEngine.Object.FindFirstObjectByType<MansionEventView>();
  void Refresh()=>typeof(MansionEventView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
  Refresh();Require(gm.SecretPassageActive,"secret event active");
  foreach(var pair in new[]{(9,6),(6,9)}){
   gm.Human.RoomId=pair.Item1;gm.Human.MovementPoints=2;RuntimeBoardView.RefreshAll();
   var entry=MansionEnvironmentCleanup.Read().layout.secret.Single(s=>s.room==pair.Item1).position;
   NavMesh.SamplePosition(entry,out var begin,.6f,NavMesh.AllAreas);actor.ResetPath();actor.Warp(begin.position);
   bool moved=(bool)typeof(GameManager).GetMethod("TryMove",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gm,new object[]{gm.Human,pair.Item2,false});
   Require(moved&&gm.Human.RoomId==pair.Item2&&gm.Human.MovementPoints==1,"secret logical 1P transition "+pair.Item1+"->"+pair.Item2);
   Require(Vector3.Distance(actor.transform.position,MapData.GetRoom(pair.Item2).Center)<3.5f,"actual CharacterActor secret transition, no airborne tunnel "+pair.Item1+"->"+pair.Item2);
  }
  foreach(var panel in UnityEngine.Object.FindObjectsByType<MansionSecretPanel>(FindObjectsSortMode.None)){
   typeof(MansionSecretPanel).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,null);
   Require(!panel.GetComponent<Collider>().enabled,"active secret wall opening clears panel collider");
  }
  Set(gm,"culpritLockEdge","1-2");Set(gm,"culpritLockUntilRound",6);Set(gm,"routeBlockedEdge","1-2");Set(gm,"brokenDoorEdge","1-2");Set(gm,"extraEventRound",6);
  Set(gm,"blackoutRoom",1);Set(gm,"blackoutUntilRound",6);Set(gm,"smokeRoomA",2);Set(gm,"smokeRoomB",3);Set(gm,"smokeUntilRound",6);Refresh();
  var events=GameObject.Find(MapData.ConnectionsName).transform.Find("DOOR_01_02/EventMarkers");
  Require(events.Find("Locked").gameObject.activeSelf&&events.Find("RouteBlocked").gameObject.activeSelf&&events.Find("BrokenDoor").gameObject.activeSelf,"door event state visuals preserved");
  Require(view.transform.Find("Room_02_EventVisuals/Smoke").gameObject.activeSelf&&view.transform.Find("Room_03_EventVisuals/Smoke").gameObject.activeSelf,"smoke event preserved");
  gm.StartNewGame();Refresh();Require(!gm.SecretPassageActive&&gm.ActiveLockedEdge==null&&gm.ActiveBlackoutRoom==0,"restart clears all public effects");
  var flags=BindingFlags.Static|BindingFlags.NonPublic;typeof(SinglePlayerE2E).GetField("gm",flags).SetValue(null,gm);
  typeof(SinglePlayerE2E).GetMethod("FullGames",flags).Invoke(null,null);typeof(SinglePlayerE2E).GetMethod("Focused",flags).Invoke(null,null);
  Require(true,"existing single-player E2E assertions="+typeof(SinglePlayerE2E).GetField("assertions",flags).GetValue(null));
  gm.StartNewGame();Refresh();foreach(int room in Enumerable.Range(1,9))Capture(room,"After");
  Require(true,"actual play camera final 27 zoom captures and 9 top views");
  Results.Add("MANSION_ENVIRONMENT_STRUCTURE_FINAL: OK");File.WriteAllLines(Path.Combine(Root,"After","play-results.txt"),Results);Stop();
 }catch(Exception ex){Fail(ex);}}
 static void Fail(Exception ex){Results.Add("FAILED "+ex);File.WriteAllLines(Path.Combine(Root,"After","play-results.txt"),Results);Debug.LogException(ex);Stop();}
 static void Stop(){moving=false;EditorApplication.update-=Tick;if(oldMaximumDelta>0)Time.maximumDeltaTime=oldMaximumDelta;SessionState.SetString("MansionStructure.Stage","");EditorApplication.isPlaying=false;}
 static string MeshName(Transform t){var filter=t.GetComponent<MeshFilter>();return filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.name:"";}
 [Serializable] class Inventory{public Row[] objects;}
 [Serializable] class Row{public string path,mesh;public Vector3 position,scale;public string[] components;}
 static void Capture(int room,string stage){
  GameManager.Instance.Human.RoomId=room;var cam=Camera.main;cam.GetComponent<PcCameraController>().ResetView();cam.transform.rotation=Quaternion.Euler(42,0,0);
  typeof(MansionCutawayView).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam.GetComponent<MansionCutawayView>(),null);
  foreach(float zoom in new[]{9f,19f,32f}){cam.orthographicSize=zoom;Save(cam,Path.Combine(Root,stage,$"Room_{room}_Zoom_{zoom}.png"));}
  Vector3 pos=cam.transform.position;Quaternion rotation=cam.transform.rotation;
  cam.transform.position=MapData.GetRoom(room).Center+new Vector3(0,40,.001f);cam.transform.LookAt(MapData.GetRoom(room).Center);cam.orthographicSize=10;
  Save(cam,Path.Combine(Root,stage,$"Room_{room}_Top.png"));cam.transform.SetPositionAndRotation(pos,rotation);
 }
 static void Save(Camera cam,string path){
  var rt=new RenderTexture(1440,900,24);var prior=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
  cam.targetTexture=prior;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
 }
}
#endif
