using UnityEngine;
namespace DeductionGame {
 // Short, non-blocking focus override. Camera rotation and game clock are untouched.
 public static class GameplayPresentation {
 static Transform actor,target;
 static float until;
 static bool winning;
 public static bool Active => actor!=null && Time.unscaledTime<until;
 public static Vector3 Focus => target!=null ? (actor.position+target.position)*0.5f : actor.position;
 public static float Zoom => target!=null ? Mathf.Max(9f,Vector3.Distance(actor.position,target.position)*0.7f+4f) : 9f;
 public static void Cue(int id,CharacterState state,int targetId) {
 if(id<0 && state==CharacterState.Idle){actor=null;target=null;winning=false;return;}
 if(state==CharacterState.Idle || state==CharacterState.Move)return;
 if(state==CharacterState.Lose && winning && Active)return;
 Transform source=null,partner=null;
 foreach(var candidate in Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None)) {
 if(candidate.PlayerId==id)source=candidate.transform;
 if(candidate.PlayerId==targetId)partner=candidate.transform;
 }
 if(source==null)return;
 actor=source;target=partner;winning=state==CharacterState.Win;until=Time.unscaledTime+1.2f;
 }
 }
}
