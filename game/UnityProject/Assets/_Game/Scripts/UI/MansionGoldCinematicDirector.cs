using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace DeductionGame {
 // Presentation only: consumes public cues/events, never moves an actor or changes game state.
 [DefaultExecutionOrder(500)]
 public sealed class MansionGoldCinematicDirector:MonoBehaviour {
  [Serializable] public struct IllustrationFrame {
   public int RoomId;public Vector3 CameraPosition;public Quaternion CameraRotation;public float OrthographicSize,Aspect;public Sprite ExistingIllustration;
  }
  public static event Action<IllustrationFrame> IllustrationReady;
  public static IllustrationFrame LastIllustrationFrame {get;private set;}
  public int ViewRoomId {get;private set;}
  public bool SideViewActive=>blend>.95f;
  public int IllustrationDispatchCount {get;private set;}
  Camera cameraView;float blend,elapsed,eventUntil;int serial=-1,eventRoom,lastViewRoom;bool dispatched;Vector3 eventFocus,lastFocus;string events;
  CharacterActor[] actors;RoomNodeView[] rooms;Light[] roomLights;MansionCutawayView cutaway;MansionVisualGoldState marker;Renderer ground;MaterialPropertyBlock groundProperties;readonly Vector4[] stairMin=new Vector4[2],stairMax=new Vector4[2];int stairBoxes;Renderer[] exterior;Transform connections;readonly List<Renderer> sideHidden=new(),occlusionHidden=new();
  public static Vector3 SidePosition(Vector3 focus)=>focus+new Vector3(4.5f,2.6f,-5.8f);
  public static Quaternion SideRotation(Vector3 focus)=>Quaternion.LookRotation(focus+Vector3.up*.9f-SidePosition(focus));
  void Awake(){cameraView=GetComponent<Camera>();}
  void Start(){RefreshCaches();}
  void OnDisable(){RestoreOcclusionVisibility();RestoreSideVisibility();if(ground!=null)ground.SetPropertyBlock(null);}
  public void RefreshCaches(){ground=GameObject.Find("VG_EstateGround_WithGameplayVoids")?.GetComponent<Renderer>();groundProperties=new MaterialPropertyBlock();actors=FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);rooms=FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None);marker=FindFirstObjectByType<MansionVisualGoldState>();exterior=marker!=null?marker.transform.Find("EstateExterior").GetComponentsInChildren<Renderer>(true):Array.Empty<Renderer>();connections=GameObject.Find(MapData.ConnectionsName)?.transform;RefreshStairBounds();cutaway=GetComponent<MansionCutawayView>();roomLights=FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name.StartsWith("Gold_Room_")).ToArray();}
  public void PreviewPublicEvent(int roomId){eventRoom=roomId;eventFocus=MapData.GetRoom(roomId).Center;eventUntil=Time.unscaledTime+1.2f;elapsed=0;dispatched=false;}
  void LateUpdate(){
   if(MapData.Theme!=MapTheme.Mansion||marker==null)return;
   var gm=GameManager.Instance;if(gm==null||gm.Human==null)return;
   RestoreOcclusionVisibility();RestoreSideVisibility();
   string now=gm.ActiveBlackoutRoom+"/"+gm.ActiveSmokeRoomA+"/"+gm.ActiveSmokeRoomB+"/"+gm.ActiveLockedEdge+"/"+gm.ActiveRouteBlockedEdge+"/"+gm.SecretPassageActive;
   if(events!=null&&events!=now){int room=gm.ActiveBlackoutRoom>0?gm.ActiveBlackoutRoom:gm.ActiveSmokeRoomA>0?gm.ActiveSmokeRoomA:gm.Human.RoomId;PreviewPublicEvent(room);}events=now;
   if(serial!=GameplayPresentation.CueSerial){serial=GameplayPresentation.CueSerial;elapsed=0;dispatched=false;}
   bool publicEvent=Time.unscaledTime<eventUntil,active=GameplayPresentation.Active||publicEvent;
   blend=Mathf.MoveTowards(blend,active?1:0,Time.unscaledDeltaTime/.23f);
   int viewRoom=gm.Human.RoomId;
   if(active){lastFocus=publicEvent?eventFocus:GameplayPresentation.Focus;lastViewRoom=publicEvent?eventRoom:GameplayPresentation.SourceId>=0?gm.Players[GameplayPresentation.SourceId].RoomId:gm.Human.RoomId;}
   if(active||blend>0){
    Vector3 focus=lastFocus;viewRoom=lastViewRoom;
    Vector3 position=SidePosition(focus);Quaternion rotation=SideRotation(focus);
    transform.position=Vector3.Lerp(transform.position,position,Mathf.SmoothStep(0,1,blend));transform.rotation=Quaternion.Slerp(transform.rotation,rotation,Mathf.SmoothStep(0,1,blend));
    cameraView.orthographicSize=Mathf.Lerp(cameraView.orthographicSize,publicEvent?4.4f:Mathf.Clamp(GameplayPresentation.Zoom*.35f,2.9f,6.5f),Mathf.SmoothStep(0,1,blend));
    if(active)elapsed+=Time.unscaledDeltaTime;
    if(active&&elapsed>.60f&&!dispatched){
     dispatched=true;var catalog=PresentationCatalog.Load();int room=viewRoom;
     LastIllustrationFrame=new IllustrationFrame{RoomId=room,CameraPosition=transform.position,CameraRotation=transform.rotation,OrthographicSize=cameraView.orthographicSize,Aspect=cameraView.aspect,ExistingIllustration=catalog!=null&&catalog.MansionRoomBackgrounds!=null?catalog.MansionRoomBackgrounds[room-1]:null};
     IllustrationDispatchCount++;IllustrationReady?.Invoke(LastIllustrationFrame);
    }
   }
   ViewRoomId=viewRoom;
   // The original cutaway system is rerun for the real side camera; wall-linked mouldings follow its renderer state.
   cutaway?.RefreshForCamera(viewRoom);
   RefreshGroundCutaway(viewRoom);
   RefreshActorOcclusion(viewRoom);
   if(SideViewActive)ApplySideVisibility(viewRoom);
   if(roomLights!=null)foreach(var light in roomLights){if(light==null)continue;int id;if(!int.TryParse(light.name.Substring("Gold_Room_".Length),out id))continue;var room=rooms.FirstOrDefault(r=>r.RoomId==id);var floor=room!=null?room.GetComponent<Renderer>():null;light.enabled=id!=gm.ActiveBlackoutRoom&&(floor==null||floor.enabled);}
  }
  void RestoreOcclusionVisibility(){foreach(var r in occlusionHidden)if(r!=null)r.enabled=true;occlusionHidden.Clear();}
  void HideOcclusion(Renderer r){if(r!=null&&r.enabled){occlusionHidden.Add(r);r.enabled=false;}}
  public void RestoreSideVisibility(){foreach(var r in sideHidden)if(r!=null)r.enabled=true;sideHidden.Clear();}
  void HideForSide(Renderer r){if(r!=null&&r.enabled){sideHidden.Add(r);r.enabled=false;}}
  public void ApplySideVisibility(int id){
   if(rooms==null)RefreshCaches();
   foreach(var room in rooms)if(room.RoomId!=id)foreach(var r in room.transform.parent.GetComponentsInChildren<Renderer>(true))HideForSide(r);
   foreach(var r in exterior)HideForSide(r);
   var gm=GameManager.Instance;if(gm!=null)foreach(var a in actors)if(gm.Players[a.PlayerId].RoomId!=id)foreach(var r in a.GetComponentsInChildren<Renderer>(true))HideForSide(r);
   if(connections!=null)foreach(Transform door in connections)if(door.name.StartsWith("DOOR_")&&!door.name.Contains($"_{id:00}"))foreach(var r in door.GetComponentsInChildren<Renderer>(true))HideForSide(r);
  }
  void RefreshStairBounds(){
   stairBoxes=0;if(connections==null)return;
   foreach(string id in new[]{"DOOR_04_07","DOOR_05_08"}){
    var group=connections.Find(id);if(group==null)continue;
    var parts=group.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith(id+"_")&&!r.name.Contains("_COLLISION_")).ToArray();if(parts.Length==0)continue;
    var bounds=parts[0].bounds;foreach(var r in parts.Skip(1))bounds.Encapsulate(r.bounds);bounds.Expand(new Vector3(.6f,.2f,.6f));
    stairMin[stairBoxes]=bounds.min;stairMax[stairBoxes]=bounds.max;stairBoxes++;
   }
  }
  public void RefreshGroundCutaway(int roomId){
   if(ground==null)return;var room=MapData.GetRoom(roomId);ground.GetPropertyBlock(groundProperties);
   groundProperties.SetFloat("_GroundCutaway",room.Center.y< -1?1:0);groundProperties.SetFloat("_CutawayFloor",room.Center.y);
   groundProperties.SetVector("_CutawayRoom",new Vector4(room.Center.x,room.Center.z,room.Size.x*.5f+.5f,room.Size.y*.5f+.5f));groundProperties.SetVector("_CutawayDirection",transform.forward);
   bool movingBelow=actors!=null&&actors.Any(a=>a!=null&&a.transform.position.y<-.2f&&a.Agent!=null&&a.Agent.hasPath);
   groundProperties.SetInteger("_StairHoleCount",room.IsBasement||movingBelow?stairBoxes:0);
   groundProperties.SetVectorArray("_StairHoleMin",stairMin);groundProperties.SetVectorArray("_StairHoleMax",stairMax);ground.SetPropertyBlock(groundProperties);
  }
  public void RefreshActorOcclusion(int viewRoom=0){
   var gm=GameManager.Instance;if(gm==null||gm.Human==null)return;if(viewRoom==0)viewRoom=gm.Human.RoomId;
   if(actors==null||actors.Any(a=>a==null))RefreshCaches();
   if(actors!=null)foreach(var actor in actors){
    if(gm.Players[actor.PlayerId].RoomId!=viewRoom)continue;
    foreach(Vector3 sample in new[]{Vector3.up*.85f,Vector3.up*1.15f,Vector3.up*1.45f,Vector3.up*1.6f,Vector3.up*1.45f-transform.right*.16f,Vector3.up*1.45f+transform.right*.16f}){
     Vector3 point=actor.transform.position+sample;Ray ray=cameraView.ViewportPointToRay(cameraView.WorldToViewportPoint(point));float distance=Vector3.Dot(point-ray.origin,ray.direction);
     foreach(var hit in Physics.RaycastAll(ray,distance-.05f,~0,QueryTriggerInteraction.Ignore)){
      var art=hit.collider.GetComponentInParent<MansionArtOccluder>();if(art!=null)foreach(var r in art.GetComponentsInChildren<Renderer>())HideOcclusion(r);
      else if(!hit.transform.name.Contains("SECRET")&&hit.transform.parent!=null&&(hit.transform.parent.name=="Walls"||hit.transform.name.Contains("Wall")||hit.transform.name.Contains("Railing")||hit.transform.name.StartsWith("DOOR_"))){var wall=hit.collider.GetComponent<Renderer>();if(wall!=null)HideOcclusion(wall);}
     }
    }
   }

  }
 }
}
