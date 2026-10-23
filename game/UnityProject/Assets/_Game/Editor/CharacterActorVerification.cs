#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
public static class CharacterActorVerification {
 private static void Pump() { EditorApplication.isPaused=false; EditorApplication.QueuePlayerLoopUpdate(); } public static void Begin() {EditorApplication.update += Pump; UnityEngine.Object.FindFirstObjectByType<RuntimeBoardView>().StartCoroutine(Run());}
 static void Assert(bool ok,string text){if(!ok)throw new Exception("Actor check: "+text);Debug.Log("[Actor verified] "+text);}
 static void Invoke(GameManager gm,string method,params object[] args)=>typeof(GameManager).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gm,args);
 static IEnumerator Run() {
 var gm=GameManager.Instance;
 var actors=UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);
 bool failed=false;
 try {
 Assert(actors.Length==6,"Six actors");
 for(int id=0;id<6;id++) {
 var actor=Array.Find(actors,x=>x.PlayerId==id);
 Assert(actor!=null && actor.GetComponent<TokenLabel>().DisplayName==GameDefinitions.Names[id],"Identity "+id);
 Assert(actor.transform.Find("Model")!=null && actor.Agent!=null,"Replaceable model and root agent");
 }
 gm.StartNewGame(); foreach(var actor in actors) { var seat=actor.Agent.destination; actor.Agent.Warp(seat); actor.Agent.ResetPath(); }
 }catch(Exception e){Debug.LogException(e);failed=true;}
 if(failed){EditorApplication.Exit(1);yield break;}
 yield return new WaitForSeconds(1f);
 var mover=Array.Find(actors,x=>x.PlayerId==0);
 for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)
 Assert(Vector3.Distance(actors[a].transform.position,actors[b].transform.position)>0.6f,"Six occupied stands remain separate");
 Vector3 start=mover.transform.position;
 Vector3 destination=MapData.GetRoom(1).Center+Vector3.forward*2.8f;
 mover.Agent.SetDestination(destination);
 bool sawMove=false; bool sawFacing=false;
 float deadline=Time.time+5f;
 while(Time.time<deadline){if(mover.State==CharacterState.Move) { sawMove=true; if(mover.Agent.desiredVelocity.sqrMagnitude>0.01f && Vector3.Dot(mover.transform.forward,mover.Agent.desiredVelocity.normalized)>0.8f) sawFacing=true; }yield return null;}
 try {
 Assert(sawMove && sawFacing,"Move state and facing from actual NavMesh travel");
 Assert(Vector3.Distance(mover.transform.position,destination)<0.25f,"Arrives at destination");
 Assert(mover.State==CharacterState.Idle,"Returns to Idle");
 foreach(var actor in actors) Assert(actor.Agent.isOnNavMesh,"On NavMesh "+actor.PlayerId);
 for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)
 Assert(Vector3.Distance(actors[a].transform.position,actors[b].transform.position)>0.6f,"Six occupied stands remain separate");
 Invoke(gm,"ResolveSuspicion",gm.Players[0],gm.Players[1],gm.Players[0].LocationCard,gm.Players[0].WeaponCard,false);
 Assert(mover.State==CharacterState.Suspect,"Real suspicion cue");
 Invoke(gm,"ResolveCooperation",gm.Players[0],gm.Players[1]);
 Assert(mover.State==CharacterState.Cooperate,"Real cooperation cue");
 }catch(Exception e){Debug.LogException(e);failed=true;}
 if(failed){EditorApplication.Exit(1);yield break;}
 yield return new WaitForSeconds(0.6f);
 try {
 var other=Array.Find(actors,x=>x.PlayerId==1);
 Vector3 toward=other.transform.position-mover.transform.position;toward.y=0;
 Assert(Vector3.Dot(mover.transform.forward,toward.normalized)>0.95f,"Faces interaction partner in place");
 Invoke(gm,"CheckForensics",gm.Players[0]);Assert(mover.State==CharacterState.Investigate,"Real investigation cue");
 Invoke(gm,"ConsumeAbility",gm.Players[0]);Assert(mover.State==CharacterState.UseAbility,"Real ability cue");
 Invoke(gm,"ResolveDeduction",gm.Players[0],0,"invalid","invalid",false);Assert(mover.State==CharacterState.Deduction,"Real deduction cue");
 Invoke(gm,"FinishGame","test",0);Assert(mover.State==CharacterState.Win && other.State==CharacterState.Lose,"Win and Lose cues");
 Debug.Log("CHARACTER_ACTOR_VERIFICATION: OK "+MapData.Theme);
 PresentationVerification.Begin();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
#endif
