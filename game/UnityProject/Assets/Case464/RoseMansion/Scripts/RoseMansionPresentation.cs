using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Case464.RoseMansion
{
    [Serializable] public sealed class RoseCinematicBinding
    {
        public string key;
        public bool approved;
        public PlayableAsset asset;
    }
    [DisallowMultipleComponent]
    public sealed class RoseMansionPresentation : MonoBehaviour
    {
        [SerializeField] private RoseMansionSession session;
        [SerializeField] private PlayableDirector director;
        [SerializeField] private RoseCinematicBinding[] bindings=Array.Empty<RoseCinematicBinding>();
        [SerializeField] private UnityEvent openingRequested=new UnityEvent();
        private string running;
        public string Status { get; private set; }="컷신·재연 자료 연결 대기";
        public void Configure(RoseMansionSession owner,PlayableDirector player){session=owner;director=player;}
        public void RequestOpening(){openingRequested.Invoke();Play("Opening");}
        public void RequestTravel(string room)=>Play("Travel:"+room);
        public void RequestAccusedDialogue(string npc)=>Play("AccusedDialogue:"+npc);
        public void RequestReenactment(string actualCulprit)=>Play("Reenactment:"+actualCulprit);
        private void Play(string key)
        {
            if(!Application.isPlaying)return;
            var item=bindings.SingleOrDefault(b=>b.key==key&&b.approved&&b.asset!=null);
            if(item==null||director==null){Status="연출 자료 연결 대기";return;}
            if((key.StartsWith("AccusedDialogue:")||key.StartsWith("Reenactment:"))&&
                (session.State==null||!session.Catalog.Branch(session.State.selectedCulprit).endingAssetsApproved))
            {Status="엔딩 연출 승인 대기";return;}
            director.playableAsset=item.asset;director.extrapolationMode=DirectorWrapMode.Hold;
            if(double.IsInfinity(director.duration)||director.duration<=0){Status="연출 길이 확인 필요";return;}
            running=key;Status="연출 재생 중";director.time=0;director.Play();
        }
        private void Update()
        {
            if(!Application.isPlaying||string.IsNullOrEmpty(running)||director==null||director.playableAsset==null)return;
            if(director.time<director.duration-.02)return;
            string completed=running;running=null;Status="연출 종료";
            if(completed=="Opening")session.EnterLobbyAfterOpening();
            else if(completed.StartsWith("AccusedDialogue:"))session.CompleteEndingPresentation(RoseEndingStep.AccusedDialogue);
            else if(completed.StartsWith("Reenactment:"))session.CompleteEndingPresentation(RoseEndingStep.Reenactment);
        }
    }
}
