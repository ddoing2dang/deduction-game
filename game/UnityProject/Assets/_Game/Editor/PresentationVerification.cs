#if UNITY_EDITOR
using System;
using System.Collections;
using DeductionGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class PresentationVerification {
 public static void Begin()=>UnityEngine.Object.FindFirstObjectByType<RuntimeBoardView>().StartCoroutine(Run());
 static void Assert(bool ok,string name){if(!ok)throw new Exception("Presentation check: "+name);Debug.Log("[Presentation verified] "+name);}
 static IEnumerator Run(){
 var gm=GameManager.Instance;var ui=UnityEngine.Object.FindFirstObjectByType<RuntimeGameUI>();bool failed=false;
 try {
 Assert(ui!=null && ui.Ready,"Canvas UI ready");ui.Refresh();
 Assert(ui.GetComponent<CanvasScaler>().referenceResolution==new Vector2(1920,1080),"Responsive reference resolution");
 Assert(ui.transform.Find("Result").gameObject.activeSelf && ui.transform.Find("Result/ResultText").GetComponent<Text>().text==gm.Result,"Win Lose result screen");
 CharacterPresentation.Play(0,CharacterState.Suspect,1);
 var actors=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);
 var a=Array.Find(actors,x=>x.PlayerId==0);var b=Array.Find(actors,x=>x.PlayerId==1);
 Assert(Vector3.Distance(GameplayPresentation.Focus,(a.transform.position+b.transform.position)*0.5f)<0.01f,"Pair focus midpoint");
 foreach(var state in new[]{CharacterState.Investigate,CharacterState.Suspect,CharacterState.Cooperate,CharacterState.Deduction,CharacterState.UseAbility,CharacterState.Win,CharacterState.Lose}){
 CharacterPresentation.Play(0,state,state==CharacterState.Suspect || state==CharacterState.Cooperate ? 1:-1);
 Assert(GameplayPresentation.Active,"Focus cue "+state);
 }
 }catch(Exception e){Debug.LogException(e);failed=true;}
 if(failed){EditorApplication.Exit(1);yield break;}
 yield return new WaitForSecondsRealtime(2f);
 try {
 Assert(!GameplayPresentation.Active,"Automatically returned to map camera");
 Assert(Vector3.Distance(Camera.main.transform.position,MapData.GetRoom(gm.Human.RoomId).Center+new Vector3(0,28,-24))<0.5f,"Physical camera returns to map tracking");
 Assert(Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(42,0,0))<0.1f,"Fixed isometric rotation preserved");
 gm.StartNewGame();ui.Refresh();
 gm.Players[1].PrivateNotes.Add("PRIVATE_SENTINEL");ui.Refresh();
 var notes=ui.transform.Find("PrivateInfo/Viewport/Notes").GetComponent<Text>().text;
 Assert(notes.Contains(gm.Human.LocationCard) && !notes.Contains("PRIVATE_SENTINEL"),"Only human private information");
 var status=ui.transform.Find("Actions/Viewport/Content/Status").GetComponent<Text>().text;
 Assert(status.Contains(gm.Human.Name) && status.Contains("역할") && status.Contains("능력") && status.Contains("P"),"Status round turn room points role ability");
 var setup=ui.transform.Find("Actions/Viewport/Content/Setup").GetComponent<Button>();setup.onClick.Invoke();
 Assert(gm.Phase==GamePhase.Round,"Canvas setup invokes existing action");
 int round=gm.Round;
 ui.transform.Find("Actions/Viewport/Content/EndTurn").GetComponent<Button>().onClick.Invoke();
 Assert(gm.Round>round || gm.Phase==GamePhase.GameOver,"Canvas end turn advances real game");
 Assert(ui.transform.Find("PublicLog/Viewport/Log").GetComponent<Text>().text.Contains(gm.PublicLog[gm.PublicLog.Count-1]),"Public log refreshes");
 foreach(var player in gm.Players)player.RoomId=gm.Human.RoomId;
 gm.Human.InfoActionUsed=false;
 int logs=gm.PublicLog.Count;
 ui.transform.Find("Actions/Viewport/Content/SuspectAction").GetComponent<Button>().onClick.Invoke();
 Assert(gm.Human.InfoActionUsed,"Canvas suspicion executes");
 gm.Human.InfoActionUsed=false;
 ui.transform.Find("Actions/Viewport/Content/Cooperate").GetComponent<Button>().onClick.Invoke();
 Assert(gm.Human.InfoActionUsed,"Canvas cooperation executes");
 typeof(GameManager).GetField("<Round>k__BackingField",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(gm,6);
 ui.transform.Find("Actions/Viewport/Content/Deduction").GetComponent<Button>().onClick.Invoke();
 Assert(gm.Human.IsCulprit ? gm.Human.FakeDeductionUsed : gm.Human.DeductionAttempts>0,"Canvas deduction executes");
 Debug.Log("GAMEPLAY_PRESENTATION_VERIFICATION: OK "+MapData.Theme);UsabilityVerification.Begin();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
#endif
