#if UNITY_EDITOR
using System;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
[InitializeOnLoad]
public static class MapThemeVerification {
 static int frames;
 static MapThemeVerification() {EditorApplication.playModeStateChanged += s=> {if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MapVerify",false)) EditorApplication.update+=Check;};}
 public static void Cruise(){ProjectBootstrap.Cruise();Begin();}
 public static void Museum(){ProjectBootstrap.Museum();Begin();}
 static void Begin(){SessionState.SetBool("MapVerify",true);EditorApplication.isPlaying=true;}
 static void Assert(bool ok,string name){if(!ok)throw new Exception(name);Debug.Log("[Map verified] "+name);}
 static void Check(){if(++frames<30)return;EditorApplication.update-=Check;SessionState.SetBool("MapVerify",false);
 try {
 var root=GameObject.Find(MapData.RootName); Assert(root!=null,"Theme root");
 Assert(root.GetComponentsInChildren<RoomNodeView>().Length==9,"Nine RoomNodes");
 var expected=MapData.Theme==MapTheme.Cruise ? "1-2,2-3,2-4,3-4,3-5,4-6,5-6,5-7,6-8,7-9,8-9" : "1-2,1-3,1-4,1-5,2-3,4-5,4-6,5-6,4-7,5-8,6-9,7-8,8-9";
 var actual=new System.Collections.Generic.List<string>();
 for(int a=1;a<=9;a++) {
 var room=root.transform.Find($"Room_{a:00}_{MapData.GetRoom(a).Name}");
 Assert(room.Find("Walls").childCount>0,"Room walls "+a);
 Assert(room.Find("NavigationPoints/Action_Point")!=null,"Action point "+a);
 for(int slot=1;slot<=6;slot++) Assert(room.Find($"NavigationPoints/Stand_{slot:00}")!=null,"Stand "+a+"/"+slot);
 foreach(int b in MapGraph.GetConnections(a)) {
 Assert(room.Find($"NavigationPoints/Entry_To_{b:00}")!=null,"Entry "+a+"/"+b);
 if(a>b)continue;actual.Add(a+"-"+b);
 var con=GameObject.Find(MapData.ConnectionsName).transform.Find($"DOOR_{a:00}_{b:00}");Assert(con!=null,"Door ID "+a+"/"+b);
 Assert(NavMesh.SamplePosition(MapData.GetRoom(a).Center,out var ah,1,NavMesh.AllAreas) && NavMesh.SamplePosition(MapData.GetRoom(b).Center,out var bh,1,NavMesh.AllAreas),"NavMesh rooms");
 NavMesh.SamplePosition(MapData.GetRoom(a).Center,out var startHit,1,NavMesh.AllAreas); NavMesh.SamplePosition(MapData.GetRoom(b).Center,out var endHit,1,NavMesh.AllAreas); var path=new NavMeshPath();NavMesh.CalculatePath(startHit.position,endHit.position,NavMesh.AllAreas,path);
 Assert(path.status==NavMeshPathStatus.PathComplete,"Traversable graph edge "+a+"/"+b);
 }
 }
 actual.Sort(); var expectedEdges=expected.Split(','); Array.Sort(expectedEdges); Assert(string.Join(",",actual)==string.Join(",",expectedEdges),"Exact LOCK graph");
 Assert(Camera.main.GetComponent<PcCameraController>()!=null && Camera.main.GetComponent<MansionCutawayView>()!=null,"Camera and cutaway");
 var gm=GameManager.Instance;gm.Human.MovementPoints=3;int next=MapGraph.GetConnections(1)[0];
 Assert(gm.TryMoveHuman(next) && gm.Human.RoomId==next,"Existing setup movement");
 Assert(UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None).Length==6,"Six agents");
 Debug.Log("MAP_THEME_VERIFICATION: OK "+MapData.Theme);CharacterActorVerification.Begin();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
#endif


