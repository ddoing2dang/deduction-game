#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
using DeductionGame;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.AI;

[InitializeOnLoad]
public static class MansionRepairVerification
{
 static readonly string Out=Path.GetFullPath("../../art/Mansion/Pass01/UnityVerification");
 static string Request=>Path.GetFullPath("../../../mansion-repair.request");
 static readonly List<string> Results=new();
 static NavMeshAgent actor; static int test=-1,point; static float deadline; static bool started;
 static Vector3[] targets; static string snapshot; static int waitFrames; static float previousMaximumDelta;
 static readonly int[,] Edges={{3,5},{5,3},{4,7},{7,4},{5,8},{8,5}};
 static MansionRepairVerification(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Deduction Game/Verify Mansion Structural Repair")]
 public static void Run(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before repair verification");
  Directory.CreateDirectory(Out);Results.Clear();
  MansionStructuralRepair.ApplyToScene();
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  snapshot=MansionStructuralRepair.Snapshot(GameObject.Find(MapData.RootName).transform,GameObject.Find(MapData.ConnectionsName).transform);
  SessionState.SetString("MansionRepair.Snapshot",snapshot);SessionState.SetBool("MansionRepair.Running",true);
  EditorApplication.isPlaying=true;
 }
 static void Poll(){
  if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlaying&&File.Exists(Request)){
   SessionState.SetBool("MansionRepair.KitchenOnly",File.ReadAllText(Request).Trim()=="kitchen");
   File.Delete(Request);try{Run();}catch(Exception ex){Fail(ex);}
  }
 }
 static void State(PlayModeStateChange s){
  if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MansionRepair.Running",false)){
   Results.Clear();test=SessionState.GetBool("MansionRepair.KitchenOnly",false)?3:-1;point=0;waitFrames=0;started=false;EditorApplication.update+=Tick;
  }
  if(s==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
 }
 static void Require(bool b,string s){if(!b)throw new Exception(s);Results.Add("OK "+s);if(started)Save();}
 static void Tick(){
  if(!EditorApplication.isPlaying)return;
  try{
   if(++waitFrames<50)return;
   if(!started){
    started=true;
    previousMaximumDelta=Time.maximumDeltaTime;
    Time.maximumDeltaTime=1f/60f;
    Application.runInBackground=true;
    var rooms=GameObject.Find(MapData.RootName).transform;var doors=GameObject.Find(MapData.ConnectionsName).transform;
    Require(MansionStructuralRepair.Snapshot(rooms,doors)==SessionState.GetString("MansionRepair.Snapshot",""),"RoomNode/Entry/StandPoint/ActionPoint/Door anchor transform snapshot unchanged");
    Require(UnityEngine.Object.FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None).Length==9,"9 RoomNodes");
    Require(doors.Cast<Transform>().Count(t=>t.name.StartsWith("DOOR_")&&!t.name.Contains("SECRET"))==11,"11 locked DoorIDs");
    Require(UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Length==6,"6 existing CharacterActors");
    Require(rooms.Find("Room_01_중앙홀/DecorativeGallery").GetComponentsInChildren<Collider>(true).Length==0,"Decorative gallery excluded from NavMesh collision sources");
    Require(Camera.main.GetComponent<MansionCutawayView>()!=null&&Camera.main.GetComponent<PcCameraController>()!=null,"Camera/cutaway attached");
    foreach(var i in Enumerable.Range(1,9)){
     var room=MapData.GetRoom(i);Require(NavMesh.SamplePosition(room.Center,out _,1,NavMesh.AllAreas),"Room floor baked "+i);
     for(int slot=0;slot<6;slot++){
      Vector3 stand=RoomPresentation.Stand(i,slot);
      Require(NavMesh.SamplePosition(stand,out var standHit,.35f,NavMesh.AllAreas) && Vector3.Distance(stand,standHit.position)<.35f,"StandPoint baked and preserved "+i+"/"+(slot+1));
     }
    }
    foreach(Transform door in doors){if(door.name.Contains("SECRET"))continue;
     Require(door.Find("EventMarkers/Locked")!=null&&door.Find("EventMarkers/RouteBlocked")!=null&&door.Find("EventMarkers/BrokenDoor")!=null,"Existing event hierarchy "+door.name);
    }
    actor=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Single(a=>a.PlayerId==0).Agent;
    var diagnostics=new List<string>();
    foreach(int flight in new[]{4,5}){
     float x=flight==4?-7.1f:-1.3f;float top=flight==4?-6.02f:-6.82f;float bottom=flight==4?-12:-12.8f;float drop=flight==4?3:3.4f;
     Vector3? prev=null;
     for(int n=0;n<=10;n++){
      float t=n/10f;Vector3 pos=new Vector3(x,-drop*t,Mathf.Lerp(top,bottom,t));
      bool found=NavMesh.SamplePosition(pos,out var h,.8f,NavMesh.AllAreas);
      diagnostics.Add(flight+" sample "+pos+" -> "+found+" "+h.position+" physics "+string.Join(";",Physics.RaycastAll(pos+Vector3.up*1.5f,Vector3.down,3).Select(z=>z.collider.name+":"+z.point.y)));
      if(found&&prev.HasValue){var pp=new NavMeshPath();NavMesh.CalculatePath(prev.Value,h.position,NavMesh.AllAreas,pp);diagnostics.Add(" corners "+string.Join(";",pp.corners.Select(v=>v.ToString())));}
      prev=found?h.position:(Vector3?)null;
     }
    }
    File.WriteAllLines(Path.Combine(Out,"stair-nav-diagnostics.txt"),diagnostics);
    StartNext();return;
   }
   if(actor.isOnOffMeshLink)throw new Exception("Fallback off-mesh link used for repaired route "+Label());
   if(Time.realtimeSinceStartup>deadline)throw new Exception("Actor movement timeout "+Label()+" waypoint "+point+" at "+actor.transform.position+" target "+targets[point]+" path "+actor.pathStatus+" destination "+actor.destination+" remaining "+actor.remainingDistance+" velocity "+actor.velocity+" desired "+actor.desiredVelocity+" stopped "+actor.isStopped+" corners "+string.Join(";",actor.path.corners.Select(v=>v.ToString())));
   if(actor.pathPending)return;
   if(actor.pathStatus!=NavMeshPathStatus.PathComplete)throw new Exception("Incomplete NavMesh path "+Label()+" waypoint "+point);
   if(Vector3.Distance(actor.transform.position,targets[point])<.22f){
    CheckBody();
    point++;
    if(point==targets.Length){Require(true,"CharacterActor physically traversed "+Label()+" without off-mesh links");Capture(Edges[test,1]);StartNext();}
    else SetTarget();
   }else CheckBody();
  }catch(Exception ex){Fail(ex);}
 }
 static string Label()=>Edges[test,0]+"->"+Edges[test,1];
 static void StartNext(){
  test++;
  if(test>=Edges.GetLength(0)){Finish();return;}
  int a=Edges[test,0],b=Edges[test,1];
  var rooms=GameObject.Find(MapData.RootName).transform;
  Transform Room(int id)=>rooms.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
  var start=Room(a).Find($"NavigationPoints/Entry_To_{b:00}").position;
  var end=Room(b).Find($"NavigationPoints/Entry_To_{a:00}").position;
  var mids=new List<Vector3>();
  int min=Math.Min(a,b),max=Math.Max(a,b);
  if(min==3)mids.AddRange(new[]{new Vector3(8.66f,0,-.4f),new Vector3(8.66f,0,-4.6f),new Vector3(5.4f,0,-4.6f)});
  if(min==4)mids.AddRange(new[]{new Vector3(-7.1f,0,-5.7f),new Vector3(-7.1f,-3,-12),new Vector3(-13.3f,-4,-12),new Vector3(-13.3f,-4,-9.5f)});
  if(min==5&&max==8)mids.AddRange(new[]{new Vector3(-1.3f,0,-6.5f),new Vector3(-1.3f,-3.4f,-12.8f),new Vector3(5,-4,-12.8f),new Vector3(5,-4,-11)});
  if(a>b)mids.Reverse();mids.Add(end);
  Require(NavMesh.SamplePosition(start,out var begin,.7f,NavMesh.AllAreas),"Start Entry sampled "+Label());
  actor.ResetPath();Require(actor.Warp(begin.position),"Existing actor warped to source Entry "+Label());
  targets=mids.Select(p=>{if(!NavMesh.SamplePosition(p,out var hit,.85f,NavMesh.AllAreas)){
   NavMesh.SamplePosition(p,out var near,4,NavMesh.AllAreas);
   var phys=Physics.RaycastAll(p+Vector3.up*2,Vector3.down,6).Select(h=>h.collider.name+" at "+h.point);
   throw new Exception("Route waypoint not baked "+p+" nearest "+near.position+" physics "+string.Join("; ",phys));
  }return hit.position;}).ToArray();
  GameManager.Instance.Human.RoomId=a;
  point=0;SetTarget();
 }
 static void SetTarget(){
  var path=new NavMeshPath();
  Require(NavMesh.CalculatePath(actor.transform.position,targets[point],NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Complete path "+Label()+" segment "+point);
  float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
  Require(length<Vector3.Distance(actor.transform.position,targets[point])*2.5f+1f,"Route follows repaired corridor without other-room detour "+Label()+" segment "+point+" length="+length.ToString("F2")+" corners="+string.Join(";",path.corners.Select(v=>v.ToString())));
  actor.SetPath(path);deadline=Time.realtimeSinceStartup+45;
 }
 static void CheckBody(){
  Vector3 p=actor.transform.position;
  foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*.8f,.3f,~0,QueryTriggerInteraction.Ignore)){
   if(c.transform.IsChildOf(actor.transform)||c.name=="Model"||c.bounds.max.y<p.y+.25f)continue;
   // Physics overlap checks actual wall/rail/overhead geometry, not only path status.
   if(c is MeshCollider meshCollider && !meshCollider.convex){
    var cp=c.ClosestPointOnBounds(p+Vector3.up*.6f);
    if(Vector3.Distance(cp,p+Vector3.up*.6f)>.32f)continue;
   }
   if(c.name.Contains("Event")||c.name.Contains("Hint"))continue;
   throw new Exception("Character body obstruction "+Label()+" "+c.name+" at "+p);
  }
 }
 static void Capture(int room){
  GameManager.Instance.Human.RoomId=room;
  var cam=Camera.main;
  cam.GetComponent<PcCameraController>().ResetView();
  cam.orthographicSize=9f;
  typeof(MansionCutawayView).GetMethod("ApplyCutaway",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cam.GetComponent<MansionCutawayView>(),new object[]{room});
  var floor=GameObject.Find(MapData.RootName).transform.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{room:00}_")).Find("Floor");
  Require(floor.GetComponent<Collider>().enabled,"Cutaway retains physical floor "+room);
  foreach(var c in GameObject.Find(MapData.RootName).GetComponentsInChildren<Collider>(true))RequireColliderEnabled(c);
  var rt=new RenderTexture(1280,720,24);var prior=cam.targetTexture;var active=RenderTexture.active;
  cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
  File.WriteAllBytes(Path.Combine(Out,"Room_"+room+".png"),image.EncodeToPNG());
  cam.targetTexture=prior;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
 static void RequireColliderEnabled(Collider c){if(!c.enabled)throw new Exception("Cutaway disabled collision: "+c.name);}
 static void Finish(){
  Capture(1);
  var links=MapData.Links;
  for(int i=0;i<links.GetLength(0);i++){
   NavMesh.SamplePosition(MapData.GetRoom(links[i,0]).Center,out var a,1,NavMesh.AllAreas);
   NavMesh.SamplePosition(MapData.GetRoom(links[i,1]).Center,out var b,1,NavMesh.AllAreas);
   var path=new NavMeshPath();Require(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Locked connection regression "+links[i,0]+"-"+links[i,1]);
  }
  Results.Add("MANSION_STRUCTURAL_PLAYMODE: OK");Save();Stop();
 }
 static void Fail(Exception ex){Directory.CreateDirectory(Out);Results.Add("FAILED "+ex);Save();Debug.LogException(ex);Stop();}
 static void Save()=>File.WriteAllLines(Path.Combine(Out,"playmode-results.txt"),Results);
 static void Stop(){if(previousMaximumDelta>0)Time.maximumDeltaTime=previousMaximumDelta;SessionState.SetBool("MansionRepair.Running",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}
#endif
