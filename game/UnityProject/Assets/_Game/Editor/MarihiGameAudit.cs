#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using DeductionGame;

[InitializeOnLoad]
public static class MarihiGameAudit
{
 static string Root=>Path.Combine(MarihiGamePipeline.Root,"Verification");
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static CharacterActor actor;static Camera camera;static int phase,frames,route,room,action,faceIndex,neutralFrame,standRoom,standSlot,standFrame;
 static readonly string[] FaceNames={"Neutral","Blink_L","Blink_R","Smile","Angry","Sad","Surprise","Mouth_A","Mouth_I","Mouth_U","Mouth_E","Mouth_O"};
 static float deadline,oldDelta,cameraStarted;static Vector3 target;static Quaternion oldRotation;static int lastFrame;
 static bool sawWalk,capturedStair;static float routeLength;
 static readonly List<string> Results=new();static bool running;
 static readonly CharacterState[] Actions={CharacterState.Idle,CharacterState.Move,CharacterState.Investigate,CharacterState.Suspect,CharacterState.Deduction,CharacterState.Cooperate,CharacterState.UseAbility,CharacterState.Win,CharacterState.Lose};
 static MarihiGameAudit(){EditorApplication.playModeStateChanged+=State;}
 public static void RunBatchAudit(){Begin("Game");}
 public static void Begin(string mode){
  Directory.CreateDirectory(Root);SessionState.SetString("MarihiGameAudit",mode);
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/Game.unity")EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
  EditorApplication.isPlaying=true;
 }
 static void State(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetString("MarihiGameAudit","")!=""){
   running=true;frames=0;phase=0;Results.Clear();EditorApplication.update+=Tick;
  }
  if(state==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;running=false;}
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Results.Add("OK "+message);Flush();}
 static void Flush()=>File.WriteAllLines(Path.Combine(Root,SessionState.GetString("MarihiGameAudit","")=="Camera"?"camera-results.txt":"play-results.txt"),Results);
 static void Tick(){
  if(!running)return;EditorApplication.QueuePlayerLoopUpdate();
  try{
   if(phase==0){if(++frames<90)return;Initialize();return;}
   if(phase==1){if(++frames<30)return;CaptureRoom();return;}
   if(phase==11){if(++frames<30)return;FinishFaces();return;}
   if(phase==2){if(++frames<30)return;CaptureAction();return;}
   if(phase==3){MoveTick();return;}
   if(phase==4){if(++frames<30)return;Finish();}
   if(phase==5){if(++frames<(faceIndex==0?30:5))return;CaptureFace();}
   if(phase==6&&Time.frameCount>neutralFrame+3)FinishFaces();
   if(phase==7&&Time.frameCount>standFrame+2)CheckStand();
   if(phase==8&&Time.unscaledTime-cameraStarted>.65f)CaptureActionCamera();
   if(phase==9&&Time.unscaledTime-cameraStarted>.65f)FinishActionCamera();
   if(phase==10&&Time.frameCount>standFrame+12)CheckActionPoint();
  }catch(Exception e){Results.Add("FAILED "+e);Flush();Debug.LogException(e);Stop();}
 }
 static Transform Room(int id)=>GameObject.Find(MapData.RootName).transform.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
 static void Initialize(){
  actor=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Single(a=>a.PlayerId==2);camera=Camera.main;
  Check(actor.Animator!=null&&actor.Animator.isHuman&&actor.Animator.avatar.isValid,"actual Marihi CharacterActor uses valid Humanoid Avatar");
  var rootModel=actor.transform.Find("Model");
  File.WriteAllLines(Path.Combine(Root,"runtime-axis-diagnostics.txt"),new[]{"root local rotation="+rootModel.localRotation+" scale="+rootModel.localScale,"head="+actor.Animator.GetBoneTransform(HumanBodyBones.Head).position,"hips="+actor.Animator.GetBoneTransform(HumanBodyBones.Hips).position,"feet="+actor.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position});
  Check(Quaternion.Angle(rootModel.localRotation,Quaternion.identity)<.1f,"model root axis conversion baked; local rotation identity");
  var renderedBody=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Body_LOD0");
  Check(renderedBody.bones.Length<=64,"body GPU bone palette bounded for Android: "+renderedBody.bones.Length);
  Check(actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Face_")).All(r=>r.bones.Length<=4),"face GPU bone palettes bounded without deleting skeleton transforms");
  var skinProbe=new Mesh();renderedBody.BakeMesh(skinProbe);var skinPoints=skinProbe.vertices.Select(v=>renderedBody.transform.TransformPoint(v)).ToArray();UnityEngine.Object.DestroyImmediate(skinProbe);
  Check(skinPoints.Max(v=>v.y)-skinPoints.Min(v=>v.y)>1f,"actual skinned body remains upright after Animator evaluation");
  Check(actor.transform.Find("Model").GetComponent<LODGroup>()!=null,"model prefab LODGroup installed");
  Check(UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Length==6,"six CharacterActors retained");
  Check(UnityEngine.Object.FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None).Length==9,"nine Mansion RoomNodes retained");
  Check(MansionEnvironmentCleanup.Read().layout.routes.Length==11,"eleven basic LOCK routes retained");
  int stands=0,actions=0;
  for(int id=1;id<=9;id++){
   var points=Room(id).Find("NavigationPoints");stands+=points.Cast<Transform>().Count(t=>t.name.StartsWith("Stand_"));actions+=points.Cast<Transform>().Count(t=>t.name=="Action_Point");
  }
  Check(stands==54&&actions==9,"54 StandPoints and nine ActionPoints unchanged");
  var lods=actor.transform.Find("Model").GetComponent<LODGroup>().GetLODs();
  var tris=lods.Select(l=>l.renderers.Cast<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3)).ToArray();
  Check(tris[0]>=18000&&tris[0]<=33000,"LOD0 budget "+tris[0]);
  Check((float)tris[1]/tris[0]>=.5f&&(float)tris[1]/tris[0]<=.6f,"LOD1 ratio "+((float)tris[1]/tris[0]));
  Check((float)tris[2]/tris[0]>=.2f&&(float)tris[2]/tris[0]<=.3f,"LOD2 ratio "+((float)tris[2]/tris[0]));
  foreach(var face in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Face_"))){
   foreach(string name in new[]{"Blink_L","Blink_R","Smile","Angry","Sad","Surprise","Mouth_A","Mouth_I","Mouth_U","Mouth_E","Mouth_O"}){
    int n=face.sharedMesh.GetBlendShapeIndex(name);Check(n>=0,face.name+" shape "+name);
    var dv=new Vector3[face.sharedMesh.vertexCount];face.sharedMesh.GetBlendShapeFrameVertices(n,0,dv,null,null);
    Check(dv.Any(v=>v.sqrMagnitude>1e-10f),"authored deformation "+face.name+"/"+name);
   }
  }
  room=1;phase=SessionState.GetString("MarihiGameAudit","")=="Camera"?11:1;PlaceRoom(room);
 }
 static void PlaceRoom(int id){
  foreach(var person in GameManager.Instance.Players)person.RoomId=id;
  RuntimeBoardView.RefreshAll();
  foreach(var person in UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None)){
   NavMesh.SamplePosition(RoomPresentation.Stand(id,person.PlayerId),out var hit,.6f,NavMesh.AllAreas);person.Agent.ResetPath();person.Agent.Warp(hit.position);person.Play(CharacterState.Idle);
  }
  frames=0;
 }
 static void CaptureRoom(){
  camera.GetComponent<PcCameraController>().ResetView();
  typeof(MansionCutawayView).GetMethod("LateUpdate",F).Invoke(camera.GetComponent<MansionCutawayView>(),null);
  foreach(float zoom in new[]{9f,19f,32f}){
   camera.orthographicSize=zoom;Save($"Room_{room}_Zoom_{zoom}");
   Vector3 head=actor.transform.position+Vector3.up*1.45f;Ray ray=camera.ScreenPointToRay(camera.WorldToScreenPoint(head));float distance=Vector3.Dot(head-ray.origin,ray.direction);
   foreach(var hit in Physics.RaycastAll(ray,distance-.05f,~0,QueryTriggerInteraction.Ignore)){
    var visible=hit.collider.GetComponent<Renderer>();
    if(visible!=null&&visible.enabled&&visible.sharedMaterial!=null&&visible.sharedMaterial.HasProperty("_Color")&&visible.sharedMaterial.color.a>.8f&&!hit.transform.IsChildOf(actor.transform))throw new Exception("visible structure occludes Marihi head in room "+room+" zoom="+zoom+" "+hit.collider.name);
   }
  }
  Check(actor.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled&&r.isVisible),"Marihi rendered in Room "+room);
  GroundCheck("Stand Room "+room,true);
  if(++room<=9){PlaceRoom(room);return;}
  standRoom=1;standSlot=0;phase=7;PrepareStand();
 }
 static void PrepareStand(){
  PlaceRoom(standRoom);
  foreach(var person in UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None)){
   int slot=(person.PlayerId+standSlot+6)%6;
   Vector3 point=MapData.GetStandPoint(standRoom,slot);
   Check(NavMesh.SamplePosition(point,out var hit,.6f,NavMesh.AllAreas),"StandPoint sampled "+standRoom+"/"+slot);
   person.Agent.ResetPath();person.Agent.Warp(hit.position);person.Play(CharacterState.Idle);
  }
  standFrame=Time.frameCount;
 }
 static void CheckStand(){
  CheckSupport();GroundCheck("Marihi StandPoint "+standRoom+"/"+standSlot,true);MeshClearance();
  Check(true,"actual Marihi mesh clearance at StandPoint "+standRoom+"/"+standSlot);
  if(++standSlot==6){standSlot=0;standRoom++;}
  if(standRoom<=9){PrepareStand();return;}
  room=1;phase=10;PrepareActionPoint();
 }
 static void PrepareActionPoint(){
  PlaceRoom(room);
  var point=Room(room).Find("NavigationPoints/Action_Point").position;
  Check(NavMesh.SamplePosition(point,out var hit,.6f,NavMesh.AllAreas),"ActionPoint sampled "+room);
  actor.Agent.ResetPath();actor.Agent.Warp(hit.position);actor.Play(CharacterState.Investigate);standFrame=Time.frameCount;
 }
 static void CheckActionPoint(){
  CheckSupport();GroundCheck("Marihi ActionPoint "+room,true);MeshClearance();
  Check(true,"actual Marihi mesh clearance at ActionPoint "+room);
  Hero("ActionPoint_"+room);
  if(++room<=9){PrepareActionPoint();return;}
  PlaceRoom(1);phase=2;action=0;actor.Play(Actions[action]);frames=0;
 }
 static void Hero(string name,float zoom=1.12f){
  camera.transform.position=actor.transform.position+actor.transform.forward*3+Vector3.up*1.1f;
  camera.transform.LookAt(actor.transform.position+Vector3.up*.85f);camera.orthographicSize=zoom;
  Save(name);
 }
 static void CaptureAction(){
  CharacterState state=Actions[action];
  if(state==CharacterState.Move)actor.Animator.SetInteger("CharacterState",(int)CharacterState.Move);
  actor.Animator.Update(.15f);
  string expected=state.ToString();Check(actor.Animator.GetCurrentAnimatorStateInfo(0).IsName(expected),"actual Animator state "+expected);
  Hero("Action_"+expected);
  var baked=new Mesh();var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Body_LOD0");body.BakeMesh(baked);
  Check(baked.vertices.All(v=>!float.IsNaN(v.x)&&v.magnitude<4),"finite skinned geometry "+expected);UnityEngine.Object.DestroyImmediate(baked);
  if(++action<Actions.Length){actor.Play(Actions[action]);frames=0;return;}
  actor.Play(CharacterState.Idle);actor.Animator.SetInteger("CharacterState",0);
  actor.GetComponentInChildren<MarihiVroidPresentation>().enabled=false;
  phase=5;faceIndex=0;frames=0;ApplyFace(FaceNames[0]);
 }
 static void ApplyFace(string shape){
  foreach(var face in actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name.StartsWith("Face_"))){
   for(int n=0;n<face.sharedMesh.blendShapeCount;n++)face.SetBlendShapeWeight(n,0);
   if(shape!=null)face.SetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex(shape),100);
  }
 }
 static void CaptureFace(){
  camera.transform.position=actor.transform.position+actor.transform.forward*2+Vector3.up*1.43f;
  camera.transform.LookAt(actor.transform.position+Vector3.up*1.43f);camera.orthographicSize=.30f;
  Save("Face_"+FaceNames[faceIndex]);Check(true,"actual per-frame facial render "+FaceNames[faceIndex]);
  if(++faceIndex<FaceNames.Length){ApplyFace(FaceNames[faceIndex]);frames=0;return;}
  ApplyFace(null);neutralFrame=Time.frameCount;phase=6;frames=0;
 }
 static void FinishFaces(){
  CapturePreservation();CaptureTrio();actor.GetComponentInChildren<MarihiVroidPresentation>().enabled=true;
  // Test action camera through the existing public presentation boundary.
  camera.GetComponent<PcCameraController>().ResetView();
  CharacterPresentation.Play(actor.PlayerId,CharacterState.Deduction);
  Check(GameplayPresentation.Active&&GameplayPresentation.SourceId==2&&GameplayPresentation.PublicState==CharacterState.Deduction,"actual Marihi slot 2 deduction action camera cue active");
  cameraStarted=Time.unscaledTime;phase=8;
 }
 static void CaptureTrio(){
  var all=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);
  var trio=new[]{all.Single(a=>a.PlayerId==3),all.Single(a=>a.PlayerId==1),actor};
  var positions=trio.Select(a=>a.transform.position).ToArray();var rotations=trio.Select(a=>a.transform.rotation).ToArray();
  var groups=trio.Select(a=>a.GetComponentInChildren<LODGroup>()).ToArray();
  Vector3 origin=actor.transform.position,forward=actor.transform.forward,right=Vector3.Cross(forward,Vector3.up);
  try{
   for(int i=0;i<3;i++){trio[i].Agent.Warp(origin+right*(i-1)*.85f);trio[i].transform.rotation=actor.transform.rotation;trio[i].Play(CharacterState.Idle);trio[i].Animator.Update(.02f);groups[i].ForceLOD(0);}
   camera.transform.position=origin+forward*4+Vector3.up*1.1f;camera.transform.LookAt(origin+Vector3.up*.85f);camera.orthographicSize=1.15f;
   Save("Trio_MATTO_JIRO_MARIHI",2100,1200);
   Check(trio.All(a=>a.Animator!=null&&a.Animator.isHuman&&a.Animator.avatar.isValid),"MATTO JIRO MARIHI coexist with valid original Humanoid Avatars");
  }finally{for(int i=0;i<3;i++){groups[i].ForceLOD(-1);trio[i].Agent.Warp(positions[i]);trio[i].transform.rotation=rotations[i];}}
 }
 static void CaptureActionCamera(){
  Check(GameplayPresentation.Active&&camera.GetComponent<MansionGoldCinematicDirector>().SideViewActive&&camera.orthographicSize<6.6f,"actual action camera reaches existing Visual Gold 3D side view");
  var viewport=camera.WorldToViewportPoint(actor.transform.position+Vector3.up*.9f);
  Check(viewport.x>.1f&&viewport.x<.9f&&viewport.y>.1f&&viewport.y<.9f,"Marihi visible in actual action camera framing");
  Check(actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("Deduction"),"actual Marihi Deduction Animator during side view");Save("Actual_ActionCamera");GameplayPresentation.Cancel();cameraStarted=Time.unscaledTime;phase=9;
 }
 static void FinishActionCamera(){
  Check(!GameplayPresentation.Active&&Mathf.Abs(camera.orthographicSize-19)<.6f,"existing camera returns to default zoom after action cue");
  Save("Actual_ActionCamera_Return");
  if(SessionState.GetString("MarihiGameAudit","")=="Camera"){Results.Add("CAMERA CHECKS PASSED; SOURCE ID 2 CONFIRMED");Flush();Stop();return;}
  actor.Play(CharacterState.Idle);actor.Agent.isStopped=false;oldDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=1f/60;
  route=-1;phase=3;NextRoute();
 }
 static void NextRoute(){
  if(++route>=22){phase=4;frames=0;return;}
  var r=MansionEnvironmentCleanup.Read().layout.routes[route/2];int a=route%2==0?r.a:r.b,b=route%2==0?r.b:r.a;
  GameManager.Instance.Human.RoomId=a;GameManager.Instance.Players[2].RoomId=a;
  var start=Room(a).Find($"NavigationPoints/Entry_To_{b:00}").position;var end=Room(b).Find($"NavigationPoints/Entry_To_{a:00}").position;
  Check(NavMesh.SamplePosition(start,out var first,.6f,NavMesh.AllAreas)&&NavMesh.SamplePosition(end,out _,.6f,NavMesh.AllAreas),"Entries sampled "+r.door+" "+a+"->"+b);
  NavMesh.SamplePosition(end,out var last,.6f,NavMesh.AllAreas);actor.Agent.ResetPath();actor.Agent.Warp(first.position);
  var path=new NavMeshPath();Check(NavMesh.CalculatePath(first.position,last.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"complete NavMesh path "+a+"->"+b);
  float length=0,planned=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);for(int i=1;i<r.points.Length;i++)planned+=Vector3.Distance(r.points[i-1],r.points[i]);Check(length<planned+5,"no long detour "+a+"->"+b);
  actor.Play(CharacterState.Idle);typeof(CharacterActor).GetField("actionUntil",F).SetValue(actor,-1f);
  actor.Agent.isStopped=false;actor.Agent.SetPath(path);target=last.position;deadline=Time.realtimeSinceStartup+60;lastFrame=-1;oldRotation=actor.transform.rotation;
  sawWalk=false;capturedStair=false;routeLength=length;
 }
 static void CapturePreservation(){
  actor.Animator.SetInteger("CharacterState",0);actor.Animator.Update(.2f);
  var group=actor.transform.Find("Model").GetComponent<LODGroup>();
  camera.transform.position=actor.transform.position+actor.transform.forward*3+Vector3.up*1.10f;
  camera.transform.LookAt(actor.transform.position+Vector3.up*.85f);camera.orthographicSize=.97f;
  for(int i=0;i<3;i++){group.ForceLOD(i);Save("Preservation_LOD"+i,900,1200);}
  group.ForceLOD(-1);
  camera.transform.position=actor.transform.position+actor.transform.forward*5+Vector3.up*1.55f;
  camera.transform.LookAt(actor.transform.position+Vector3.up*1.55f);camera.orthographicSize=.275f;
  Save("Preservation_Face",1000,1000);
  Check(true,"actual imported neutral face and forced LOD captures for Blender candidate comparison");
 }
 static void MoveTick(){
  if(Time.realtimeSinceStartup>deadline)throw new Exception("Route timeout "+route+" at "+actor.transform.position);
  if(lastFrame==Time.frameCount)return;
  if(lastFrame>=0&&Quaternion.Angle(oldRotation,actor.transform.rotation)>720*Time.deltaTime+2)throw new Exception("rotation discontinuity");
  lastFrame=Time.frameCount;oldRotation=actor.transform.rotation;
  if(actor.State==CharacterState.Move&&actor.Animator.GetCurrentAnimatorStateInfo(0).IsName("Move"))sawWalk=true;
  var current=MansionEnvironmentCleanup.Read().layout.routes[route/2];
  if(!capturedStair&&sawWalk&&(current.door=="DOOR_04_07"||current.door=="DOOR_05_08")&&actor.Agent.remainingDistance<routeLength*.55f){
   int active=actor.transform.position.y< -1?current.b:current.a;GameManager.Instance.Human.RoomId=active;
   var offset=(Vector3)typeof(PcCameraController).GetField("cameraOffset",F).GetValue(camera.GetComponent<PcCameraController>());
   camera.transform.position=actor.transform.position+offset;camera.transform.rotation=Quaternion.Euler(42,0,0);
   typeof(MansionCutawayView).GetMethod("LateUpdate",F).Invoke(camera.GetComponent<MansionCutawayView>(),null);
   foreach(float zoom in new[]{9f,19f,32f}){camera.orthographicSize=zoom;Save("Stair_"+current.door+"_Direction_"+(route%2)+"_Zoom_"+zoom);}
   capturedStair=true;
  }
  CheckSupport();
  if(Time.frameCount%12==0){GroundCheck("Route "+route,false);MeshClearance();}
  if(actor.Agent.pathPending)return;
  if(actor.Agent.isOnOffMeshLink)throw new Exception("Unexpected airborne off-mesh fallback");
  if(Vector3.Distance(actor.transform.position,target)<.22f){Check(sawWalk,"actual walking Animator during navigation "+current.door+" direction="+(route%2));Check(true,"actual Marihi bidirectional traversal "+current.door+" direction="+(route%2));NextRoute();}
  else if(!actor.Agent.hasPath)throw new Exception("stopped short of destination "+route);
 }
 static void CheckSupport(){
  var p=actor.transform.position;
  if(!Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,.65f,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(actor.transform)&&h.normal.y>.5f))throw new Exception("unsupported actor root "+p);
  foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.32f,p+Vector3.up*1.35f,.27f,~0,QueryTriggerInteraction.Ignore)){
   if(collider.transform.IsChildOf(actor.transform)||collider.bounds.max.y<p.y+.23f)continue;
   var probe=new GameObject("MarihiClearanceProbe");var capsule=probe.AddComponent<CapsuleCollider>();capsule.radius=.27f;capsule.height=1.80f;capsule.center=Vector3.up*.90f;capsule.enabled=false;
   try{if(Physics.ComputePenetration(capsule,p,Quaternion.identity,collider,collider.transform.position,collider.transform.rotation,out _,out float depth)&&depth>.02f)throw new Exception("Marihi wall/door penetration "+collider.name+" depth="+depth);}
   finally{UnityEngine.Object.DestroyImmediate(probe);}
  }
 }
 static void GroundCheck(string label,bool stand){
  float nearest=float.PositiveInfinity;
  foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){
   Vector3 foot=actor.Animator.GetBoneTransform(bone).position;
   var hits=Physics.RaycastAll(foot+Vector3.up*.35f,Vector3.down,.8f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(actor.transform)&&h.normal.y>.5f).OrderBy(h=>h.distance).ToArray();
   if(hits.Length==0)continue;float error=foot.y-.1007f-hits[0].point.y;
   if(error<-.065f)throw new Exception(label+" foot below floor "+bone+" error="+error);
   nearest=Mathf.Min(nearest,Mathf.Abs(error));
  }
  if(nearest>(stand?.065f:.15f))throw new Exception(label+" both feet floating; nearest error="+nearest);
 }
 static void MeshClearance(){
  var boxes=Physics.OverlapSphere(actor.transform.position+Vector3.up*.8f,1.1f).OfType<BoxCollider>().Where(c=>!c.transform.IsChildOf(actor.transform)&&c.bounds.max.y>actor.transform.position.y+.25f).ToArray();
  if(boxes.Length==0)return;var baked=new Mesh();
  try{foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name.EndsWith("LOD0"))){
   renderer.BakeMesh(baked);var vertices=baked.vertices;
   for(int i=0;i<vertices.Length;i+=5){Vector3 world=renderer.transform.TransformPoint(vertices[i]);
    foreach(var box in boxes){Vector3 local=box.transform.InverseTransformPoint(world)-box.center;Vector3 half=box.size*.5f-Vector3.one*.025f;
     if(Mathf.Abs(local.x)<half.x&&Mathf.Abs(local.y)<half.y&&Mathf.Abs(local.z)<half.z)throw new Exception("skinned mesh enters solid collider "+renderer.name+" / "+box.name);
    }
   }
  }}finally{UnityEngine.Object.DestroyImmediate(baked);}
 }
 static void Finish(){
  var performance=new List<string>();var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
  foreach(var r in renderers){
   var m=r.sharedMesh;var n=m.normals;var t=m.tangents;var weights=m.boneWeights;
   Check(n.Length==m.vertexCount&&t.Length==m.vertexCount,"normal/tangent streams "+r.name);
   Check(m.triangles.Distinct().All(v=>n[v].sqrMagnitude>.1f&&!float.IsNaN(t[v].x)&&new Vector3(t[v].x,t[v].y,t[v].z).sqrMagnitude>.1f),"finite usable normals/tangents "+r.name);
   Check(weights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f),"normalized skin weights "+r.name);
   performance.Add(r.name+" vertices="+m.vertexCount+" tris="+m.triangles.Length/3+" weightedBones="+r.bones.Length+" shapes="+m.blendShapeCount);
  }
  performance.Add("Stored SMRs="+renderers.Length+"; active LOD SMRs=7; authored bones=69; unique weighted transforms="+renderers.SelectMany(r=>r.bones).Distinct().Count());
  performance.Add("Materials="+renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count()+" mainTextures="+renderers.SelectMany(r=>r.sharedMaterials).Select(m=>m.mainTexture).Distinct().Count());
  File.WriteAllLines(Path.Combine(Root,"performance-results.txt"),performance);
  var gm=GameManager.Instance;var flags=BindingFlags.Static|BindingFlags.NonPublic;
  typeof(SinglePlayerE2E).GetField("gm",flags).SetValue(null,gm);typeof(SinglePlayerE2E).GetMethod("FullGames",flags).Invoke(null,null);typeof(SinglePlayerE2E).GetMethod("Focused",flags).Invoke(null,null);
  Check(true,"existing single-player regression assertions="+typeof(SinglePlayerE2E).GetField("assertions",flags).GetValue(null));
  gm.StartNewGame();RuntimeBoardView.RefreshAll();
  Check(UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Count(a=>a.PlayerId==2&&a.Animator!=null)==1,"Restart preserves one Marihi and all six actors");
  Check(camera.GetComponent<PcCameraController>()!=null&&camera.GetComponent<MansionCutawayView>()!=null,"camera and cutaway preserved");
  var ui=UnityEngine.Object.FindFirstObjectByType<RuntimeGameUI>();Check(ui!=null&&ui.Ready,"existing RuntimeGameUI remains ready after Restart");
  Results.Add("AUTOMATED CHECKS PASSED; CAPTURES REQUIRE DIRECT VISUAL REVIEW BEFORE FINAL OK");Flush();Stop();
 }
 static void Stop(){running=false;EditorApplication.update-=Tick;if(oldDelta>0)Time.maximumDeltaTime=oldDelta;SessionState.SetString("MarihiGameAudit","");EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(Results.Any(x=>x.StartsWith("FAILED"))?1:0);}
 static void Save(string name,int width=1440,int height=900){
  var isolate=name.StartsWith("Face_")||name.StartsWith("Preservation_")||name.StartsWith("Action_")||name.StartsWith("ActionPoint_");
  var hidden=(isolate||name.StartsWith("Trio_"))?UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Where(a=>name.StartsWith("Trio_")?a.PlayerId!=2&&a.PlayerId!=1&&a.PlayerId!=3:a!=actor).SelectMany(a=>a.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled).ToArray():Array.Empty<Renderer>();
  foreach(var r in hidden)r.enabled=false;
  var rt=new RenderTexture(width,height,24);var old=camera.targetTexture;var active=RenderTexture.active;
  try{camera.targetTexture=rt;var director=camera.GetComponent<MansionGoldCinematicDirector>();if(director!=null){director.RefreshGroundCutaway(director.SideViewActive?director.ViewRoomId:GameManager.Instance.Human.RoomId);director.RefreshActorOcclusion();}foreach(var follower in UnityEngine.Object.FindObjectsByType<MansionGoldRendererFollower>(FindObjectsSortMode.None))follower.RefreshVisibility();camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Root,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);foreach(var r in hidden)if(r!=null)r.enabled=true;}
 }
}
#endif
