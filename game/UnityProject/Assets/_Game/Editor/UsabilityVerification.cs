#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class UsabilityVerification {
 static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Set(GameManager gm,string name,object value)=>typeof(GameManager).GetField(name,F).SetValue(gm,value);
 static void Check(bool ok,string name){if(!ok)throw new Exception("Usability: "+name);Debug.Log("[Usability verified] "+name);}
 public static void Begin(){
 var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
 var view=EditorWindow.GetWindow(type);view.position=new Rect(20,20,1920,1080);
 UnityEngine.Object.FindFirstObjectByType<RuntimeBoardView>().StartCoroutine(Run());
 }
 static Button Button(RuntimeGameUI ui,string id)=>ui.transform.Find("Actions/Viewport/Content/"+id).GetComponent<Button>();
 static void Capture(string label){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Usability-"+MapData.Theme+"-"+label+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
 static IEnumerator Run(){
 var gm=GameManager.Instance;var ui=UnityEngine.Object.FindFirstObjectByType<RuntimeGameUI>();bool failed=false;
 try {
 gm.StartNewGame();ui.Refresh();
 Check(ui.transform.Find("GuidePanel/Guide").GetComponent<Text>().text.Contains("초기 배치"),"Initial setup guidance pinned");
 Check(!Button(ui,"Deduction").interactable && !Button(ui,"Cooperate").interactable,"Normal actions disabled during setup");
 Check(Button(ui,"Setup").interactable,"Setup completion enabled");
 int neighbor=MapGraph.GetConnections(1)[0];Check(gm.MoveReason(neighbor)=="" && gm.PreviewMovementCost(neighbor)==1,"Connected move visible with 1P cost");
 int disconnected=1;for(int i=2;i<=9;i++)if(!MapGraph.IsConnected(1,i)){disconnected=i;break;}
 Check(!gm.TryMoveHuman(disconnected) && gm.LastFeedback.Contains("연결"),"Disconnected click reason");
 gm.Human.MovementPoints=0;Check(!gm.TryMoveHuman(neighbor) && gm.LastFeedback.Contains("부족"),"Insufficient P reason");gm.Human.MovementPoints=3;
 }catch(Exception e){Debug.LogException(e);failed=true;}
 if(failed){EditorApplication.Exit(1);yield break;}
 yield return null;Capture("setup");
 try {
 Button(ui,"Setup").onClick.Invoke();ui.Refresh();
 var mapRoot=GameObject.Find(MapData.RootName);
 var currentHint=mapRoot.transform.Find($"Room_{gm.Human.RoomId:00}_{MapData.GetRoom(gm.Human.RoomId).Name}/MovementHint").GetComponent<LineRenderer>();
 Check(currentHint.startColor==Color.cyan,"Current room outline");
 int reachable=MapGraph.GetConnections(gm.Human.RoomId)[0];
 var reachableHint=mapRoot.transform.Find($"Room_{reachable:00}_{MapData.GetRoom(reachable).Name}/MovementHint").GetComponent<LineRenderer>();
 Check(reachableHint.startColor==Color.green,"Reachable room outline");
 Check(gm.Round==1 && !Button(ui,"Deduction").interactable && Button(ui,"Deduction").GetComponentInChildren<Text>().text.Contains("R6"),"Pre-R6 deduction reason");
 gm.Players[1].RoomId=gm.Human.RoomId;gm.Human.InfoActionUsed=false;ui.Refresh();Check(Button(ui,"Cooperate").interactable,"Valid same-room cooperation enabled");
 Button(ui,"Cooperate").onClick.Invoke();ui.Refresh();Check(!Button(ui,"Cooperate").interactable,"Consumed info action disabled");
 int neighbor=MapGraph.GetConnections(gm.Human.RoomId)[0];gm.Human.MovementPoints=3;Set(gm,"routeBlockedEdge",gm.Human.RoomId<neighbor?$"{gm.Human.RoomId}-{neighbor}":$"{neighbor}-{gm.Human.RoomId}");
 Check(!gm.TryMoveHuman(neighbor) && gm.LastFeedback.Contains("경로차단") && gm.ActiveRouteBlockedEdge==null,"Route block attempt explains and consumes existing block");
 Set(gm,"<Round>k__BackingField",12);gm.Human.LastDeductionRound=0;gm.Human.FakeDeductionUsed=false;ui.Refresh();
 Check(ui.transform.Find("GuidePanel/Guide").GetComponent<Text>().text.Contains("R12 최종 추리 필수"),"R12 forced prompt pinned");
 Check(!Button(ui,"EndTurn").interactable && Button(ui,"Deduction").interactable,"R12 final required before end");
 Check(ui.transform.Find("Actions/Viewport/Content/Choices").GetComponent<Text>().text.Contains("확인:"),"Deduction triple preview");
 Set(gm,"blackoutRoom",gm.Human.RoomId);Set(gm,"blackoutUntilRound",12);ui.Refresh();
 Check(!Button(ui,"Cooperate").interactable && Button(ui,"Deduction").interactable,"R12 priority and blackout action reason");
 Check(ui.transform.Find("FeedbackPanel/Feedback").GetComponent<Text>().text.Contains("정전"),"Active event explained");
 Set(gm,"blackoutRoom",0);ui.Refresh();Check(!ui.transform.Find("FeedbackPanel/Feedback").GetComponent<Text>().text.Contains("이동 외 행동 제한"),"Expired event summary cleared");
 gm.Human.Spectating=true;gm.Human.Active=false;ui.Refresh();Check(Button(ui,"EndTurn").interactable,"Spectator can advance remaining AI game");gm.Human.Spectating=false;gm.Human.Active=true;ui.Refresh();
 }catch(Exception e){Debug.LogException(e);failed=true;}
 if(failed){EditorApplication.Exit(1);yield break;}
 yield return null;Capture("r12");
 try {
 typeof(GameManager).GetMethod("FinishGame",F).Invoke(gm,new object[]{"검증 결과",0});ui.Refresh();Check(ui.transform.Find("Result").gameObject.activeSelf,"GameOver result shown");
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}
 yield return null;Capture("result");
 try {
 ui.transform.Find("Result/Restart").GetComponent<Button>().onClick.Invoke();
 Check(gm.Round==0 && ui.SelectedRoom==1 && !ui.transform.Find("Result").gameObject.activeSelf,"Real Restart button clears UI choices/result");
 Check(Camera.main.orthographicSize==19 && !GameplayPresentation.Active,"Restart resets default zoom/focus");
 Check(gm.LastAITurnCount==0 && gm.ActiveBlackoutRoom==0,"Restart resets AI progress/event UI");
 Check(!gm.DebugUI,"Debug fallback hidden by default");
 var cut=Camera.main.GetComponent<MansionCutawayView>();
 typeof(MansionCutawayView).GetMethod("ApplyCutaway",F).Invoke(cut,new object[]{7});
 var root=GameObject.Find(MapData.RootName);
 foreach(var node in root.GetComponentsInChildren<RoomNodeView>())
 Check(node.GetComponent<Renderer>().enabled==node.GetComponent<Collider>().enabled,"Cutaway floor click collider matches visibility");
 foreach(var renderer in root.GetComponentsInChildren<Renderer>())
 if(renderer.transform.parent!=null && renderer.transform.parent.name=="Walls")Check(renderer.GetComponent<Collider>().enabled==renderer.enabled,"Cutaway wall cannot intercept hidden clicks");
 typeof(MansionCutawayView).GetMethod("ApplyCutaway",F).Invoke(cut,new object[]{1});
 // Complete an additional game exclusively through Canvas Button.onClick.
 typeof(GameManager).GetField("rng",F).SetValue(gm,new System.Random(0));
 ui.transform.Find("Result/Restart").GetComponent<Button>().onClick.Invoke();
 int wrongLocation=Array.IndexOf(CardRules.Locations,gm.Human.LocationCard);
 for(int i=0;i<wrongLocation;i++)Button(ui,"Location").onClick.Invoke();
 Button(ui,"Setup").onClick.Invoke();int turns=0;
 while(gm.Phase!=GamePhase.GameOver && turns++<14){
 ui.Refresh();
 if(gm.Round==12 && (gm.Human.IsCulprit?!gm.Human.FakeDeductionUsed:gm.Human.LastDeductionRound!=12))
 ui.transform.Find("GuidePanel/Deduction").GetComponent<Button>().onClick.Invoke();
 ui.Refresh();var end=ui.transform.Find("GuidePanel/EndTurn").GetComponent<Button>();Check(end.interactable,"Canvas full game end-turn available");end.onClick.Invoke();
 }
 Check(gm.Phase==GamePhase.GameOver,"Setup to GameOver using only Canvas buttons");
 Debug.Log("USABILITY_VERIFICATION: OK "+MapData.Theme);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
#endif


