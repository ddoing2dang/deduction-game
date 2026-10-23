using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeductionGame {
 public enum CharacterState { Idle, Move, Investigate, Suspect, Cooperate, Deduction, UseAbility, Win, Lose }
 // Stateless cue boundary: receives only already-public actions, never cards or roles.
 public static class CharacterPresentation {
 public static void Play(int id,CharacterState state,int target=-1) {
 GameplayPresentation.Cue(id,state,target);
 foreach(var actor in Object.FindObjectsByType<CharacterActor>(FindObjectsSortMode.None))
 if(id<0 || actor.PlayerId==id) actor.Play(state,target);
 }
 }
 public sealed class CharacterActor : MonoBehaviour {
 public int PlayerId {get;private set;}
 public CharacterState State {get;private set;}
 public Animator Animator {get;private set;}
 public NavMeshAgent Agent {get;private set;}
 private float actionUntil;
 private Transform facingTarget;
 private Quaternion standRotation=Quaternion.identity;
 public void Initialize(int id,GameObject model) {
 PlayerId=id;
 var visual=model!=null ? Instantiate(model,transform) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
 visual.name="Model";
 visual.transform.SetParent(transform,false);
 if(model==null) {visual.transform.localPosition=Vector3.up*0.55f;visual.transform.localScale=new Vector3(0.45f,0.55f,0.45f);}
 foreach(var collider in visual.GetComponentsInChildren<Collider>()) {collider.enabled=false;Destroy(collider);}
 // Navigation belongs exclusively to the actor root, including imported models.
 foreach(var nested in visual.GetComponentsInChildren<NavMeshAgent>()) {nested.enabled=false;Destroy(nested);}
 Animator=visual.GetComponentInChildren<Animator>();
 if(Animator!=null) Animator.applyRootMotion=false;
 Agent=GetComponent<NavMeshAgent>(); Agent.updateRotation=false;
 SetState(CharacterState.Idle);
 }
 public void AlignAtStand(Quaternion rotation) {standRotation=rotation;}
 public void Play(CharacterState state,int target=-1) {
 facingTarget=null;
 if(target>=0) foreach(var actor in FindObjectsByType<CharacterActor>(FindObjectsSortMode.None))
 if(actor.PlayerId==target) facingTarget=actor.transform;
 if(Agent!=null && Agent.isOnNavMesh) Agent.isStopped=state!=CharacterState.Idle && state!=CharacterState.Move;
 actionUntil=Time.time+1.2f;
 SetState(state);
 }
 private void SetState(CharacterState state) {
 State=state;
 if(Animator==null || Animator.runtimeAnimatorController==null)return;
 foreach(var parameter in Animator.parameters)
 if(parameter.name=="CharacterState" && parameter.type==AnimatorControllerParameterType.Int)
 Animator.SetInteger(parameter.nameHash,(int)state);
 }
 private void Update() {
 bool terminal=State==CharacterState.Win || State==CharacterState.Lose;
 bool action=terminal || Time.time<actionUntil;
 if(!action && Agent!=null && Agent.isOnNavMesh) Agent.isStopped=false;
 bool moving=Agent!=null && Agent.isOnNavMesh && !Agent.pathPending &&
 Agent.hasPath && Agent.remainingDistance>Agent.stoppingDistance+0.12f;
 if(!moving && Agent!=null && Agent.isOnNavMesh && !Agent.pathPending && Agent.hasPath) Agent.ResetPath();
 if(!action) SetState(moving?CharacterState.Move:CharacterState.Idle);
 Vector3 direction=action && facingTarget!=null ? facingTarget.position-transform.position :
 moving ? Agent.desiredVelocity : Vector3.zero;
 direction.y=0;
 Quaternion rotation=direction.sqrMagnitude>0.001f ? Quaternion.LookRotation(direction) : standRotation;
 if(!terminal) transform.rotation=Quaternion.RotateTowards(transform.rotation,rotation,720f*Time.deltaTime);
 }
 }
}


