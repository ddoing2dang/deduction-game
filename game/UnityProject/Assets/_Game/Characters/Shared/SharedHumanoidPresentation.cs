using UnityEngine;
using DeductionGame;

// Visual-only adaptation shared by the pilot skeleton. No input or game rules.
public sealed class SharedHumanoidPresentation : MonoBehaviour
{
    Animator animator;
    CharacterActor actor;
    SkinnedMeshRenderer[] faces;
    float blinkClock;
    readonly RaycastHit[] groundHits=new RaycastHit[32];
    void Start()
    {
        animator=GetComponent<Animator>();actor=GetComponentInParent<CharacterActor>();
        faces=System.Array.FindAll(GetComponentsInChildren<SkinnedMeshRenderer>(true),r=>r.sharedMesh.blendShapeCount>0);
        if(actor!=null&&actor.Agent!=null)actor.Agent.height=1.65f;
    }
    void LateUpdate()
    {
        if(animator==null)return;
        bool moving=actor!=null&&actor.State==CharacterState.Move;
        if(actor!=null&&!moving)actor.AlignAtStand(Quaternion.Euler(0,180,0));
        animator.speed=moving&&actor.Agent!=null?Mathf.Clamp(actor.Agent.velocity.magnitude/1.65f,.65f,2.6f):1;
        blinkClock+=Time.deltaTime;
        float phase=blinkClock%4.1f;
        float blink=phase<.14f?Mathf.Sin(phase/.14f*Mathf.PI)*100:0;
        foreach(var face in faces)
        {
            var mesh=face.sharedMesh;
            void Shape(string name,float weight){int index=mesh.GetBlendShapeIndex(name);if(index>=0)face.SetBlendShapeWeight(index,weight);}
            Shape("Blink_L",blink);Shape("Blink_R",blink);
            Shape("Suspicious",actor!=null&&actor.State==CharacterState.Suspect?55:0);
            Shape("Thinking",actor!=null&&actor.State==CharacterState.Deduction?50:0);
            Shape("Smile",actor!=null&&actor.State==CharacterState.Win?65:0);
            Shape("Sad",actor!=null&&actor.State==CharacterState.Lose?65:0);
        }
    }
    void OnAnimatorIK(int layer)
    {
        if(animator==null||!animator.isHuman||actor==null)return;
        Ground(AvatarIKGoal.LeftFoot,HumanBodyBones.LeftFoot);
        Ground(AvatarIKGoal.RightFoot,HumanBodyBones.RightFoot);
    }
    void Ground(AvatarIKGoal goal,HumanBodyBones bone)
    {
        var foot=animator.GetBoneTransform(bone);if(foot==null)return;
        Vector3 start=foot.position+Vector3.up*.3f;
        int hitCount=Physics.RaycastNonAlloc(start,Vector3.down,groundHits,.65f,~0,QueryTriggerInteraction.Ignore);
        float best=float.PositiveInfinity;RaycastHit ground=default;bool found=false;
        for(int i=0;i<hitCount;i++)
        {
            var hit=groundHits[i];
            if(!hit.transform.IsChildOf(actor.transform)&&hit.normal.y>.5f&&hit.distance<best)
            {best=hit.distance;ground=hit;found=true;}
        }
        if(!found)return;
        bool swing=actor.State==CharacterState.Move&&foot.position.y-actor.transform.position.y>.15f;
        float weight=swing?.15f:.9f;
        animator.SetIKPositionWeight(goal,weight);animator.SetIKRotationWeight(goal,weight*.6f);
        animator.SetIKPosition(goal,new Vector3(foot.position.x,ground.point.y+.085f,foot.position.z));
        Vector3 facing=Vector3.ProjectOnPlane(actor.transform.forward,ground.normal);
        if(facing.sqrMagnitude>.001f)animator.SetIKRotation(goal,Quaternion.LookRotation(facing,ground.normal));
    }
}
