using System;
using System.Collections.Generic;
using UnityEngine;
using DeductionGame;

// Visual adaptation for the Marihi candidate on the common VRoid technology; navigation and rules stay on CharacterActor.
public sealed class MarihiVroidPresentation : MonoBehaviour
{
    Animator animator;
    CharacterActor actor;
    SkinnedMeshRenderer[] faces;
    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly List<Transform> hair = new();
    readonly List<Quaternion> hairRest = new();
    readonly Transform[,] skirt = new Transform[2,2];
    readonly Quaternion[,] skirtRest = new Quaternion[2,2];
    readonly float[] bend = new float[2];
    float clock, turn;
    Quaternion previousRotation;
    void Start()
    {
        animator=GetComponent<Animator>();actor=GetComponentInParent<CharacterActor>();
        faces=Array.FindAll(GetComponentsInChildren<SkinnedMeshRenderer>(true),r=>r.sharedMesh.blendShapeCount>0);
        if(actor!=null&&actor.Agent!=null)actor.Agent.height=1.70f;
        previousRotation=transform.rotation;
        foreach(var t in GetComponentsInChildren<Transform>(true)) {
            if(t.name.StartsWith("MARIHI_Tail_")) {hair.Add(t);hairRest.Add(t.localRotation);}
            for(int side=0;side<2;side++)for(int face=0;face<2;face++)
                if(t.name=="MARIHI_Skirt_"+(face==0?"Front":"Back")+"_"+(side==0?"L":"R")) {skirt[side,face]=t;skirtRest[side,face]=t.localRotation;}
        }
    }
    void LateUpdate()
    {
        if(animator==null||!animator.isHuman)return;
        bool moving=actor!=null&&actor.State==CharacterState.Move;
        if(actor!=null&&!moving)actor.AlignAtStand(Quaternion.Euler(0,180,0));
        animator.speed=moving&&actor.Agent!=null?Mathf.Clamp(actor.Agent.velocity.magnitude/1.65f,.65f,2.6f):1;
        clock+=Time.deltaTime;float phase=clock%4.1f;
        float blink=phase<.14f?Mathf.Sin(phase/.14f*Mathf.PI)*100:0;
        foreach(var face in faces) {
            void Shape(string name,float weight){int i=face.sharedMesh.GetBlendShapeIndex(name);if(i>=0)face.SetBlendShapeWeight(i,weight);}
            Shape("Blink_L",blink);Shape("Blink_R",blink);
            Shape("Suspicious",actor!=null&&actor.State==CharacterState.Suspect?55:0);
            Shape("Thinking",actor!=null&&actor.State==CharacterState.Deduction?50:0);
            Shape("Smile",actor!=null&&actor.State==CharacterState.Win?65:0);
            Shape("Sad",actor!=null&&actor.State==CharacterState.Lose?65:0);
        }
        // The waist pivot prevents a sitting thigh from pulling the skirt through the torso.
        for(int side=0;side<2;side++) {
            var upper=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
            var lower=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
            Vector3 d=(lower.position-upper.position).normalized;
            float angle=Mathf.Atan2(Vector3.Dot(d,transform.forward),-Vector3.Dot(d,transform.up))*Mathf.Rad2Deg;
            bend[side]=Mathf.Lerp(bend[side],Mathf.Clamp(angle,-15,95),1-Mathf.Exp(-20*Time.deltaTime));
            for(int face=0;face<2;face++)if(skirt[side,face]!=null)
                skirt[side,face].rotation=Quaternion.AngleAxis(-bend[side]*(face==0?.88f:.30f),transform.right)*skirt[side,face].parent.rotation*skirtRest[side,face];
        }
        float angular=Mathf.DeltaAngle(previousRotation.eulerAngles.y,transform.eulerAngles.y);
        previousRotation=transform.rotation;
        turn=Mathf.Lerp(turn,Mathf.Clamp(angular/Mathf.Max(.001f,Time.deltaTime)*.006f,-1.5f,1.5f),1-Mathf.Exp(-6*Time.deltaTime));
        for(int i=0;i<hair.Count;i++) {
            float sway=moving?Mathf.Sin(clock*5+i*.4f)*.65f:Mathf.Sin(clock*1.2f+i*.4f)*.12f;
            hair[i].localRotation=hairRest[i]*Quaternion.Euler(sway,0,turn*.5f);
        }
    }
    void OnAnimatorIK(int layer)
    {
        if(animator==null||!animator.isHuman||actor==null)return;
        Ground(AvatarIKGoal.LeftFoot,HumanBodyBones.LeftFoot);Ground(AvatarIKGoal.RightFoot,HumanBodyBones.RightFoot);
    }
    void Ground(AvatarIKGoal goal,HumanBodyBones bone)
    {
        var foot=animator.GetBoneTransform(bone);if(foot==null)return;
        int count=Physics.RaycastNonAlloc(foot.position+Vector3.up*.32f,Vector3.down,hits,.75f,~0,QueryTriggerInteraction.Ignore);
        float best=float.PositiveInfinity;RaycastHit ground=default;bool found=false;
        for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(actor.transform)&&hits[i].normal.y>.5f&&hits[i].distance<best)
            {best=hits[i].distance;ground=hits[i];found=true;}
        if(!found)return;
        bool swing=actor.State==CharacterState.Move&&foot.position.y-actor.transform.position.y>.16f;
        animator.SetIKPositionWeight(goal,swing?.15f:.9f);animator.SetIKRotationWeight(goal,swing?.08f:.54f);
        animator.SetIKPosition(goal,new Vector3(foot.position.x,ground.point.y+.1007f,foot.position.z));
        Vector3 facing=Vector3.ProjectOnPlane(actor.transform.forward,ground.normal);
        if(facing.sqrMagnitude>.001f)animator.SetIKRotation(goal,Quaternion.LookRotation(facing,ground.normal));
    }
}
