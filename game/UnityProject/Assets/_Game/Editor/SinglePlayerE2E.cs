#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class SinglePlayerE2E {
 static int frames,assertions;
 static GameManager gm;
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static SinglePlayerE2E(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SingleE2E",false))EditorApplication.update+=Run;};}
 public static void Mansion(){ProjectBootstrap.Mansion();Begin();}
 public static void Cruise(){ProjectBootstrap.Cruise();Begin();}
 public static void Museum(){ProjectBootstrap.Museum();Begin();}
 static void Begin(){SessionState.SetBool("SingleE2E",true);EditorApplication.isPlaying=true;}
 static object Get(string name)=>typeof(GameManager).GetField(name,Flags).GetValue(gm);
 static void Set(string name,object value)=>typeof(GameManager).GetField(name,Flags).SetValue(gm,value);
 static object Call(string name,params object[] args)=>typeof(GameManager).GetMethod(name,Flags).Invoke(gm,args);
 static void Require(bool value,string name){assertions++;if(!value)throw new Exception("E2E: "+name);}
 static void Fresh(int seed){Set("rng",new System.Random(seed));gm.StartNewGame();}
 static void Round(int r){Set("<Round>k__BackingField",r);Set("<Phase>k__BackingField",GamePhase.Round);Set("<TurnIndex>k__BackingField",0);}
 static void Investigator(){for(int seed=0;seed<100;seed++){Fresh(seed);if(!gm.Human.IsCulprit)return;}throw new Exception("No fixture");}
 static void Culprit(){for(int seed=0;seed<100;seed++){Fresh(seed);if(gm.Human.IsCulprit)return;}throw new Exception("No fixture");}
 static void Run(){if(++frames<30)return;EditorApplication.update-=Run;SessionState.SetBool("SingleE2E",false);
 try {Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);gm=GameManager.Instance;FullGames();Focused();Debug.Log($"SINGLE_PLAYER_E2E: OK {MapData.Theme} assertions={assertions}");EditorApplication.Exit(0);}
 catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}}
 static void WrongDeduction(){gm.SetUISelection(0,0,Array.IndexOf(CardRules.Locations,gm.Human.LocationCard),1,1);gm.HumanDeduction();}
 static int NextTo(int start,int destination){var q=new Queue<int>();var parent=new Dictionary<int,int>();q.Enqueue(start);parent[start]=0;
 while(q.Count>0){int a=q.Dequeue();foreach(int b in MapGraph.GetConnections(a)){if(parent.ContainsKey(b))continue;parent[b]=a;q.Enqueue(b);}}
 if(!parent.ContainsKey(destination)||destination==start)return start;int x=destination;while(parent[x]!=start)x=parent[x];return x;}
 static void FullGames(){int r12=0,key=0,chest=0;var events=new HashSet<ExtraEventType>();
 for(int seed=0;seed<24;seed++){
 Fresh(seed);Require(gm.Players.Count==6 && gm.Human.IsHuman,"1 human + 5 AI");for(int i=1;i<6;i++)Require(!gm.Players[i].IsHuman,"AI seat");
 Require(gm.Round==0 && gm.Human.MovementPoints==3,"Pre-round 3P");
 int first=MapGraph.GetConnections(1)[0];Require(gm.TryMoveHuman(first)&&gm.Human.MovementPoints==2,"Setup edge 1P");gm.CompleteHumanSetup();Require(gm.Round==1 && gm.Human.MovementPoints==2,"R1 2P after all setup");
 foreach(var p in gm.Players)Require(p.SetupComplete,"All six setup complete");
 var aiTurns=new HashSet<string>();Application.LogCallback listener=(text,trace,type)=>{foreach(var p in gm.Players)if(!p.IsHuman && text.Contains(p.Name+" 턴 시작"))aiTurns.Add(p.Name);};Application.logMessageReceived+=listener;
 int steps=0,lastRound=0;
 try {while(gm.Phase!=GamePhase.GameOver && steps++<15){
 Require(gm.Round>lastRound,"Human round progresses, no deadlock");lastRound=gm.Round;
 Require(gm.TurnIndex==0,"Returns to human turn");
 Require(gm.Human.MovementPoints>=1 && gm.Human.MovementPoints<=2,"Normal 2P or disruption 1P");
 int expected=gm.Round>=12?4:gm.Round>=9?3:gm.Round>=6?2:gm.Round>=3?1:0;
 Require((int)Get("witnessStage")==expected,"Witness R3/R6/R9/R12");
 Require((bool)Get("forensicsReady")== (gm.Round>=7),"Forensics arrives R7");
 if(gm.Round==12)r12++;
 int destination=(bool)Get("keySpawned") && !(bool)Get("keyTaken") ? (int)Get("keyRoom") : gm.Human.HasKey && (bool)Get("chestSpawned") ? (int)Get("chestRoom") : 1;
 for(int move=0;move<2 && gm.Human.RoomId!=destination;move++) {int next=NextTo(gm.Human.RoomId,destination);int before=gm.Human.MovementPoints;if(!gm.TryMoveHuman(next))break;Require(gm.Human.MovementPoints<before,"Ordinary movement costs P");}
 if(gm.Human.HasKey)key++;
 if((bool)Get("chestOpened"))chest++;
 if(gm.Round>=7 && gm.Human.RoomId==1)gm.HumanCheckForensics();
 if(gm.Round>=6 && gm.Human.IsCulprit)gm.HumanFakeDeduction();
 if(gm.Round==12 && !gm.Human.IsCulprit){int r=gm.Round;gm.EndHumanTurn();Require(gm.Phase==GamePhase.Round && gm.Round==r,"R12 blocks end until human deduction");WrongDeduction();Require(gm.Human.LastDeductionRound==12,"R12 human final deduction");}
 events.Add((ExtraEventType)Get("extraEvent"));
 gm.EndHumanTurn();
 }
 Require(gm.Phase==GamePhase.GameOver && gm.Round<=12,"Normal GameOver within R12");Require(aiTurns.Count==5,"All five AI turn participation");
 var culprit=gm.Players.FindCulprit();if(gm.Round==12)Require(culprit.FakeDeductionUsed,"Culprit fake deduction requirement");
 Debug.Log($"[Full game] {MapData.Theme} seed={seed} end=R{gm.Round} {gm.Result}");
 Restart();
 }finally{Application.logMessageReceived-=listener;}
 }
 Require(r12>0,"At least one full R1-R12 run");Require(key>0 && chest>0,"Key and chest reached through legal movement");Require(events.Count==3,"All three random extra events covered");
 }
 static PlayerState FindCulprit(this IReadOnlyList<PlayerState> players){foreach(var p in players)if(p.IsCulprit)return p;throw new Exception();}
 static void Restart(){gm.StartNewGame();Require(gm.Phase==GamePhase.SetupMove && gm.Round==0 && gm.Result=="","Restart phase/result");Require(gm.ActiveBlackoutRoom==0 && gm.ActiveSmokeRoomA==0 && gm.ActiveLockedEdge==null && gm.ActiveRouteBlockedEdge==null && !gm.SecretPassageActive,"Restart events");
 foreach(var p in gm.Players)Require(p.DeductionAttempts==0 && !p.ForensicsChecked && !p.HasKey && !p.FakeDeductionUsed && p.Active && !p.Spectating,"Restart player state");
 foreach(var actor in UnityEngine.Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None))Require(actor.State==CharacterState.Idle,"Restart actor state");Require(!GameplayPresentation.Active,"Restart camera");var ui=UnityEngine.Object.FindFirstObjectByType<RuntimeGameUI>();ui.Refresh();Require(!ui.transform.Find("Result").gameObject.activeSelf,"Restart UI result hidden");}
 static PlayerState Ability(InvestigatorAbility ability){Investigator();gm.CompleteHumanSetup();Round(7);var h=gm.Human;h.RoomId=1;h.MovementPoints=2;h.InfoActionUsed=false;h.Ability=ability;h.AbilityUsesLeft=GameDefinitions.AbilityBaseUses[ability];return h;}
 static void Focused(){
 Investigator();var h=gm.Human;h.Ability=InvestigatorAbility.Sprint;h.AbilityUsesLeft=2;gm.HumanUseAbilityPrimary();Require(h.AbilityUsesLeft==2 && h.MovementPoints==3,"Abilities blocked during setup");
 gm.CompleteHumanSetup();Round(12);h.RoomId=1;Set("blackoutRoom",1);Set("blackoutUntilRound",12);WrongDeduction();Require(h.DeductionAttempts==1,"R12 forced deduction overrides blackout");
 Investigator();gm.CompleteHumanSetup();Round(5);WrongDeduction();Require(gm.Human.DeductionAttempts==0,"No deduction before R6");Round(6);WrongDeduction();WrongDeduction();Require(gm.Human.DeductionAttempts==1,"One deduction per round");Round(7);WrongDeduction();WrongDeduction();Require(gm.Human.DeductionAttempts==2 && gm.Human.Spectating,"Two failure limit and spectating");gm.EndHumanTurn();Require(gm.Phase==GamePhase.GameOver,"Spectator game continues to GameOver");
 Investigator();gm.CompleteHumanSetup();Round(6);var answer=(CaseAnswer)Get("answer");gm.SetUISelection(Array.IndexOf(CardRules.Weapons,answer.Weapon),answer.CulpritPlayerId,Array.IndexOf(CardRules.Locations,answer.Location),1,1);gm.HumanDeduction();Require(gm.Phase==GamePhase.GameOver && gm.Result.Contains(gm.Human.Name),"Correct answer immediate sole winner");int attempts=gm.Human.DeductionAttempts;Round(7);Set("<Phase>k__BackingField",GamePhase.GameOver);gm.HumanDeduction();Require(gm.Human.DeductionAttempts==attempts,"GameOver cannot execute actions");
 Culprit();gm.CompleteHumanSetup();Round(12);gm.EndHumanTurn();Require(gm.Phase==GamePhase.Round && !gm.Human.FakeDeductionUsed,"R12 culprit cannot skip fake");gm.HumanFakeDeduction();gm.HumanFakeDeduction();Require(gm.Human.FakeDeductionUsed && gm.Human.DeductionAttempts==0,"Fake exactly once without elimination");
 h=Ability(InvestigatorAbility.Gatekeeper);int next=MapGraph.GetConnections(1)[0];gm.HumanUseAbilityPrimary();Require(gm.TryMoveHuman(next) && h.MovementPoints==2 && h.AbilityUsesLeft==2,"Gatekeeper zero base one use");
 h=Ability(InvestigatorAbility.Tracking);next=MapGraph.GetConnections(1)[0];gm.Players[1].RoomId=next;gm.HumanUseAbilityPrimary();Require(gm.TryMoveHuman(next) && h.MovementPoints==2 && h.AbilityUsesLeft==1,"Tracking occupied room zero base");
 h=Ability(InvestigatorAbility.Sprint);gm.HumanUseAbilityPrimary();gm.HumanUseAbilityPrimary();gm.HumanUseAbilityPrimary();Require(h.MovementPoints==4 && h.AbilityUsesLeft==0,"Sprint two uses only");
 h=Ability(InvestigatorAbility.Leap);gm.SetUISelection(0,0,0,9,1);gm.HumanUseAbilityPrimary();Require(h.RoomId==9 && h.MovementPoints==2 && h.AbilityUsesLeft==0,"Leap one use no movement cost");
 h=Ability(InvestigatorAbility.Interrogation);gm.Players[1].RoomId=1;gm.HumanUseAbilityPrimary();gm.HumanSuspicion(1,gm.Players[1].WeaponCard);Require(h.InfoActionUsed && h.AbilityUsesLeft==0 && h.PrivateNotes.Exists(x=>x.Contains("심문 추가 결과")),"Interrogation private result consumes once");
 h=Ability(InvestigatorAbility.Analysis);gm.HumanAnalyzeLocation();gm.HumanAnalyzeWeapon();Require(h.AbilityUsesLeft==0 && h.PrivateNotes.Exists(x=>x.Contains("정보분석")),"Analysis single use");
 h=Ability(InvestigatorAbility.ReSuspicion);gm.Players[1].RoomId=1;gm.HumanSuspicion(1,CardRules.Weapons[0]);gm.SetUISelection(1,0,0,1,1);gm.HumanUseAbilityPrimary();Require(h.AbilityUsesLeft==0,"ReSuspicion different weapon consumes once");gm.HumanDeduction();Require(h.DeductionAttempts==0,"ReSuspicion blocks immediate deduction");
 h=Ability(InvestigatorAbility.Return);int old=MapGraph.GetConnections(1)[0];h.TurnStartRoomId=old;h.RoomId=1;Call("BeginTurn");Require(h.PreviousTurnStartRoomId==old,"Previous turn origin retained");gm.HumanUseAbilityPrimary();Require(h.RoomId==old && h.AbilityUsesLeft==2,"Return to previous turn origin");
 h=Ability(InvestigatorAbility.Reserve);gm.HumanUseAbilityPrimary();Call("EndTurn",h);Require(h.ReservedPoint==1 && h.AbilityUsesLeft==2,"Reserve at most one P");Set("<TurnIndex>k__BackingField",0);Call("BeginTurn");Require(h.MovementPoints==3 && h.ReservedPoint==0,"Reserve consumed next turn");
 h=Ability(InvestigatorAbility.Detour);next=MapGraph.GetConnections(1)[0];string edge="1-"+next;Set("culpritLockEdge",edge);Set("culpritLockUntilRound",7);gm.HumanUseAbilityPrimary();h.MovementPoints=0;Require(!gm.TryMoveHuman(next) && h.AbilityUsesLeft==2,"Unaffordable move does not consume armed ability");h.MovementPoints=2;Require(gm.TryMoveHuman(next) && h.MovementPoints==1 && h.AbilityUsesLeft==1,"Detour ignores exactly one surcharge");
 Investigator();gm.CompleteHumanSetup();Round(7);h=gm.Human;h.RoomId=1;gm.Players[1].RoomId=1;int notes=gm.Players[2].PrivateNotes.Count;gm.HumanSuspicion(1,gm.Players[1].WeaponCard);Require(h.InfoActionUsed && gm.Players[2].PrivateNotes.Count==notes,"Suspicion private and consumes action");int count=h.PrivateNotes.Count;gm.HumanCooperation(1);Require(h.PrivateNotes.Count==count,"No second info action");Call("BeginTurn");gm.HumanCooperation(1);Require(h.InfoActionUsed,"Cooperation consumes action");Call("BeginTurn");Set("forensicsReady",true);gm.HumanCheckForensics();Require(h.ForensicsChecked && h.InfoActionUsed,"Forensics consumes action");
 Culprit();gm.CompleteHumanSetup();Round(6);h=gm.Human;h.RoomId=1;next=MapGraph.GetConnections(1)[0];gm.SetUISelection(0,0,0,next,1);var powers=(CulpritPowerUses)Get("culpritPowerUses");gm.HumanCulpritLock();gm.HumanCulpritLock();gm.HumanCulpritLock();Require(powers.Lock==0,"Lock two uses");h.MovementPoints=3;Require(gm.TryMoveHuman(next)&&h.MovementPoints==1,"Lock +1P");
 gm.HumanCulpritDisrupt();gm.HumanCulpritDisrupt();Require(powers.Disrupt==0,"Disrupt two uses");Set("<TurnIndex>k__BackingField",1);Call("BeginTurn");Require(gm.Players[1].MovementPoints==1 && gm.Players[1].DisruptionPenaltyNextTurn==0,"Disruption minimum one, consumed once");Set("<TurnIndex>k__BackingField",0);
 h.RoomId=1;h.MovementPoints=3;gm.HumanCulpritRouteBlock();Require(powers.RouteBlock==0 && !gm.TryMoveHuman(next) && gm.ActiveRouteBlockedEdge==null && h.MovementPoints==3,"Route block consumes one failed attempt");
 Set("blackoutRoom",next);Set("blackoutUntilRound",7);gm.HumanCulpritSmoke();Require(powers.Smoke==1 && gm.ActiveSmokeRoomA==0 && gm.ActiveSmokeRoomB==0,"Rejected smoke leaves no state");Set("blackoutRoom",0);gm.HumanCulpritSmoke();Require(powers.Smoke==0 && gm.ActiveSmokeRoomA!=gm.ActiveSmokeRoomB,"Smoke two rooms one use");Round(8);Call("ExpireRoundEffects");Require(gm.ActiveSmokeRoomA==0 && gm.ActiveLockedEdge==null,"Power expiry");
 foreach(ExtraEventType type in new[]{ExtraEventType.Blackout,ExtraEventType.BrokenDoor,ExtraEventType.SecretPassage}){Investigator();gm.CompleteHumanSetup();Round(6);Set("extraEvent",type);Set("extraEventRound",6);Call("TriggerExtraEvent");Require(type==ExtraEventType.Blackout ? gm.ActiveBlackoutRoom>0 : type==ExtraEventType.BrokenDoor ? gm.ActiveBrokenDoorEdge!=null : gm.SecretPassageActive,"Event activates "+type);Round(8);Call("ExpireRoundEffects");Require(gm.ActiveBlackoutRoom==0 && gm.ActiveBrokenDoorEdge==null && !gm.SecretPassageActive,"Event expires "+type);}
 Investigator();gm.CompleteHumanSetup();Round(3);Set("keyRoom",gm.Human.RoomId);Set("keySpawned",true);Call("CheckArrival",gm.Human);Require(gm.Human.HasKey,"Key private pickup");int spawn=(int)Get("chestSpawnRound");Require(spawn>=4 && spawn<=8 && spawn>gm.Round,"Chest follows key within R4-R8");Round(spawn);Call("BeginRound");gm.Human.RoomId=(int)Get("chestRoom");Call("CheckArrival",gm.Human);Require((bool)Get("chestOpened") && !gm.Human.HasKey,"Chest consumes key");Restart();
 }
}
#endif

