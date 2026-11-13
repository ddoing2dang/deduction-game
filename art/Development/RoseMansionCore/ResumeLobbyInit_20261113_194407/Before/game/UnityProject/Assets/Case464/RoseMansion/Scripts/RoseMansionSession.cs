using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Case464.Hotel;
using Mystery.Interrogation;
using UnityEngine;

namespace Case464.RoseMansion
{
    [DisallowMultipleComponent]
    public sealed class RoseMansionSession : MonoBehaviour
    {
        [SerializeField] private TextAsset content;
        [SerializeField] private HotelMapCameraController mapCamera;
        [SerializeField] private RoseMansionPresentation presentation;
        private RoseCatalog catalog;
        private RoseProgress state, checkpoint;
        private RoseMansionStore store;
        private System.Random random;
        private RoseAccusation pendingAccusation;
        private string hash;
        public event Action Changed;
        public RoseProgress State=>state;
        public RoseCatalog Catalog { get { LoadCatalog();return catalog; } }
        public string Status { get; private set; }="승인되지 않은 사건 정보는 표시·소비하지 않습니다.";
        public bool HasContinue=>Application.isPlaying&&Store.HasCheckpoint;
        public bool MainMenu { get; private set; }=true;
        private RoseMansionStore Store=>store??(store=new RoseMansionStore());
        public void Configure(TextAsset data,HotelMapCameraController camera,RoseMansionPresentation bridge)
        { content=data;mapCamera=camera;presentation=bridge; }
        private void Awake(){ if(Application.isPlaying)LoadCatalog(); }
        private void LoadCatalog()
        {
            if(catalog!=null)return;
            if(content==null)throw new InvalidOperationException("장미대저택 진행 자료가 연결되지 않았습니다.");
            catalog=JsonUtility.FromJson<RoseCatalog>(content.text);
            string error=catalog?.ValidateStructure()??(catalog==null?"진행 자료를 읽을 수 없습니다.":null);
            if(error!=null){catalog=null;throw new InvalidDataException(error);}
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(content.text))).Replace("-","");
        }
        private void Notify(string text=null){ if(text!=null)Status=text;Changed?.Invoke(); }
        private int Draw(int count){state.randomDraws++;return random.Next(count);}
        public void NewGame()
        {
            if(!Application.isPlaying)return;
            LoadCatalog();pendingAccusation=null;
            state=new RoseProgress{runId=Guid.NewGuid().ToString("N"),contentHash=hash,seed=Guid.NewGuid().GetHashCode(),phase=RosePhase.Opening};
            random=new System.Random(state.seed);
            var people=catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).ToArray();
            state.selectedCulprit=people[Draw(people.Length)].npcId;
            var branch=catalog.Branch(state.selectedCulprit);state.truthReady=catalog.InvestigationReady;
            state.truthMethodId=state.truthReady?branch.methodId:"";state.truthWeaponId=state.truthReady?branch.weaponId:"";
            state.rooms=catalog.Investigable.Select(r=>new RoseRoomProgress{roomId=r.id}).ToList();
            checkpoint=null;MainMenu=false;
            presentation?.RequestOpening();Notify("새 회차를 시작했습니다. 오프닝 연출은 연결된 자료만 사용합니다.");
        }
        // Characterless fallback is a screen transition, never a pretend walking cutscene.
        public void EnterLobbyAfterOpening()
        {
            if(state==null||state.phase!=RosePhase.Opening)return;
            state.phase=RosePhase.Lobby;PrepareTurn();SaveCheckpoint();ShowMap();Notify();
        }
        public void OpenMainMenu(){MainMenu=true;pendingAccusation=null;Notify();}
        public void ContinueGame()
        {
            if(!Application.isPlaying)return;
            try
            {
                LoadCatalog();var saved=Store.ReadCheckpoint();var candidate=saved?.roseMansion;
                string error=ValidateSave(saved);
                if(error!=null){Notify(error);return;}
                state=RoseMansionStore.Copy(candidate);checkpoint=RoseMansionStore.Copy(candidate);
                random=new System.Random(state.seed);for(int i=0;i<state.randomDraws;i++)random.Next();
                MainMenu=false;pendingAccusation=null;ShowMap();Notify("마지막 로비 자동 저장을 복원했습니다.");
            }
            catch(Exception e){Debug.LogWarning("[Rose save] "+e.Message);Notify("자동 저장을 불러오지 못했습니다. 원본 저장 파일은 보존됩니다.");}
        }
        private string ValidateSave(CaseProgressSave saved)
        {
            var s=saved?.roseMansion;
            if(saved==null||saved.schemaVersion!=2||s==null||s.contentHash!=hash||string.IsNullOrWhiteSpace(s.runId))return "현재 콘텐츠와 맞는 로비 자동 저장이 아닙니다.";
            if(s.phase==RosePhase.Room||s.phase==RosePhase.Opening||s.turnIndex<0||s.turnIndex>9||s.currentRoom!="LOBBY"||s.randomDraws<1||s.randomDraws>10000)return "로비 저장 상태의 확인이 필요합니다.";
            var b=catalog.Branch(s.selectedCulprit);
            if(b==null||s.truthReady!=catalog.InvestigationReady||s.truthReady&&(s.truthMethodId!=b.methodId||s.truthWeaponId!=b.weaponId)||!s.truthReady&&(!string.IsNullOrEmpty(s.truthMethodId)||!string.IsNullOrEmpty(s.truthWeaponId)))return "저장된 사건 정답 세트가 현재 승인 자료와 다릅니다.";
            if(s.rooms==null||s.rooms.Count!=7||catalog.Investigable.Any(r=>s.rooms.Count(p=>p!=null&&p.roomId==r.id)!=1))return "객실 조사 저장이 손상되었습니다.";
            foreach(var room in s.rooms)
                if(room.used==null||room.used.Distinct().Count()!=room.used.Count||room.used.Any(k=>!catalog.Room(room.roomId).slots.Any(slot=>slot.key==k))||room.visitedDay<0||room.visitedDay>5)return "확인 정보 이력이 올바르지 않습니다.";
            if(s.records==null||s.timeline==null||s.items==null||s.dailyEvents==null||s.records.Select(r=>r.id).Distinct().Count()!=s.records.Count||s.dailyEvents.Select(e=>e.day).Distinct().Count()!=s.dailyEvents.Count)return "수첩 또는 이벤트 저장의 확인이 필요합니다.";
            if(s.records.Any(r=>catalog.Room(r.roomId)==null)||s.timeline.Any(t=>!s.records.Any(r=>r.id==t.recordId)))return "수첩 출처 연결이 올바르지 않습니다.";
            if(s.lockedRoom=="CRIME_SCENE"||!string.IsNullOrEmpty(s.lockedRoom)&&!catalog.Investigable.Any(r=>r.id==s.lockedRoom))return "시간대 잠금 저장이 올바르지 않습니다.";
            return null;
        }
        public int PermanentRemaining=>state==null?7:state.rooms.Count(r=>!r.Exhausted);
        private IEnumerable<RoseRoom> EligibleBeforeLock()=>catalog.Investigable.Where(r=>!state.Room(r.id).Exhausted&&state.Room(r.id).visitedDay!=state.Day);
        public string RoomStatus(string id)
        {
            if(id=="LOBBY")return "현재 장소";
            if(state==null)return "새 게임을 시작하세요";
            var r=state.Room(id);
            if(r.Exhausted)return "영구 조사 완료";
            if(r.visitedDay==state.Day)return "당일 방문 완료";
            if(state.lockedRoom==id)return "이번 시간대 임시 폐쇄";
            return "방문 가능";
        }
        public bool CanVisit(string id)=>state!=null&&!MainMenu&&state.phase==RosePhase.Lobby&&id!="LOBBY"&&RoomStatus(id)=="방문 가능";
        private void PrepareTurn()
        {
            for(int guard=0;guard<11;guard++)
            {
                if(PermanentRemaining==0){state.phase=RosePhase.Accusation;state.currentRoom="LOBBY";return;}
                if(state.preparedTurn==state.turnIndex)return;
                if(state.Period==0&&!state.dailyEvents.Any(e=>e.day==state.Day))
                {
                    var daily=new RoseDailyEvent{day=state.Day,won=Draw(5)==0};state.dailyEvents.Add(daily);
                    if(daily.won){daily.rewardId=GrantReward(catalog.specialEventRewards);daily.pendingContent=string.IsNullOrEmpty(daily.rewardId);}
                }
                state.lockedRoom="";var available=EligibleBeforeLock().ToArray();
                if(available.Length==0)
                {
                    state.skippedTurns++;
                    if(state.turnIndex==9){state.phase=RosePhase.Accusation;return;}
                    state.turnIndex++;continue;
                }
                if(available.Length>3)
                {
                    var candidates=available.Where(r=>r.id!="CRIME_SCENE").ToArray();
                    if(candidates.Length>0)state.lockedRoom=candidates[Draw(candidates.Length)].id;
                }
                state.preparedTurn=state.turnIndex;return;
            }
            throw new InvalidOperationException("Turn preparation could not settle.");
        }
        public void Visit(string roomId)
        {
            if(!CanVisit(roomId)){Notify("현재 시간대에는 방문할 수 없습니다. 다른 객실을 선택하세요.");return;}
            state.currentRoom=roomId;state.phase=RosePhase.Room;state.Room(roomId).visitedDay=state.Day;state.visits++;
            state.searchedThisVisit=state.questionedThisVisit=false;state.extraQuestionCredits=state.extraSearchCredits=0;
            presentation?.RequestTravel(roomId);ShowMap();Notify(GetGreeting(roomId)??"인사 대사 연결 대기 · 임의 대사를 생성하지 않습니다.");
        }
        private string GetGreeting(string id)
        {
            var r=catalog.Room(id);if(r==null||string.IsNullOrEmpty(r.npcId))return null;
            var slot=r.npcId==state.selectedCulprit?catalog.Branch(state.selectedCulprit).dialogueOverride:r.greeting;
            return catalog.InvestigationReady&&slot!=null&&slot.Ready?slot.body:null;
        }
        private RoseSlot Resolve(RoseRoom room,RoseSlot slot)
        {
            var branch=catalog.Branch(state.selectedCulprit);
            if(!state.truthReady||!catalog.TruthReady(branch))return null;
            if(room.npcId==state.selectedCulprit&&slot.key=="Q1")slot=branch.q1Override;
            else if(slot.kind=="Search"||slot.kind=="Victim")slot=branch.evidenceOverrides.SingleOrDefault(s=>s.id==slot.id)??slot;
            return slot!=null&&slot.Ready?slot:null;
        }
        public bool SlotAvailable(RoseSlot slot)
        {
            if(state==null||state.phase!=RosePhase.Room||state.Room(state.currentRoom).used.Contains(slot.key))return false;
            return slot.kind=="Search"?!state.searchedThisVisit||state.extraSearchCredits>0:!state.questionedThisVisit||state.extraQuestionCredits>0;
        }
        public bool SlotContentReady(RoseSlot slot)=>state!=null&&Resolve(catalog.Room(state.currentRoom),slot)!=null;
        public void InspectSlot(string key)
        {
            if(state==null||state.phase!=RosePhase.Room)return;
            var room=catalog.Room(state.currentRoom);var slot=room.slots.SingleOrDefault(s=>s.key==key);
            if(slot==null||!SlotAvailable(slot))return;
            var resolved=Resolve(room,slot);
            if(resolved==null){Notify("데이터 연결 대기 · 조사/질문 횟수와 정보는 소모되지 않았습니다.");return;}
            if(slot.kind=="Search"){if(state.searchedThisVisit)state.extraSearchCredits--;else state.searchedThisVisit=true;}
            else{if(state.questionedThisVisit)state.extraQuestionCredits--;else state.questionedThisVisit=true;}
            state.Room(room.id).used.Add(slot.key);
            state.records.Add(new RoseRecord{id=Guid.NewGuid().ToString("N"),slotId=slot.id,evidenceId=resolved.evidenceId??"",roomId=room.id,npcId=room.npcId,day=state.Day,period=state.Period,title=slot.label,body=resolved.body});
            if(slot.kind=="Search"&&!slot.meaningful&&Draw(2)==0)GrantReward(catalog.emptySearchRewards);
            Notify(resolved.body);
        }
        public bool CanLeave=>state!=null&&state.phase==RosePhase.Room&&state.searchedThisVisit&&state.questionedThisVisit;
        public void LeaveRoom()
        {
            if(!CanLeave)return;
            state.currentRoom="LOBBY";state.phase=RosePhase.LobbyReview;state.extraSearchCredits=state.extraQuestionCredits=0;
            ShowMap();SaveCheckpoint();Notify("수집한 기록을 확인하고 생각 정리를 마치세요.");
        }
        public void FinishReview()
        {
            if(state==null||state.phase!=RosePhase.LobbyReview)return;
            if(state.turnIndex==9||PermanentRemaining==0)state.phase=RosePhase.Accusation;
            else{state.turnIndex++;state.phase=RosePhase.Lobby;PrepareTurn();}
            ShowMap();SaveCheckpoint();Notify(state.phase==RosePhase.Accusation?"최종 고발 단계입니다.":state.Day+"일차 · "+(state.Period==0?"오전":"오후"));
        }
        private string GrantReward(RoseReward[] table)
        {
            // Distribution and named items are unapproved: an empty table grants nothing.
            var approved=(table??Array.Empty<RoseReward>()).Where(r=>r.approved&&(r.kind=="Search"||r.kind=="Question"||r.kind=="Record"&&r.record!=null&&r.record.Ready)).ToArray();
            if(approved.Length==0)return "";
            var reward=approved[Draw(approved.Length)];
            if(reward.kind=="Record")
            {
                if(!state.truthReady)return "";
                state.records.Add(new RoseRecord{id=Guid.NewGuid().ToString("N"),slotId=reward.record.id,evidenceId=reward.record.evidenceId??"",roomId="LOBBY",npcId="",day=state.Day,period=state.Period,title=reward.record.label,body=reward.record.body});
            }
            else state.items.Add(new RoseItem{instanceId=Guid.NewGuid().ToString("N"),definitionId=reward.id,kind=reward.kind});
            return reward.id;
        }
        public void UseItem(string id)
        {
            if(state==null||state.phase!=RosePhase.Room)return;
            var item=state.items.SingleOrDefault(i=>i.instanceId==id&&!i.used);if(item==null)return;
            var room=catalog.Room(state.currentRoom);
            if(!room.slots.Any(s=>(item.kind=="Search"?s.kind=="Search":s.kind!="Search")&&!state.Room(room.id).used.Contains(s.key)&&Resolve(room,s)!=null))
            {Notify("추가로 확인할 승인된 정보가 없어 소모하지 않았습니다.");return;}
            item.used=true;if(item.kind=="Search")state.extraSearchCredits++;else state.extraQuestionCredits++;
            Notify("추가 행동 1회를 사용할 수 있습니다.");
        }
        private bool SaveCheckpoint()
        {
            if(state==null||state.phase==RosePhase.Room||state.phase==RosePhase.Opening)return false;
            try{Store.WriteCheckpoint(state);checkpoint=RoseMansionStore.Copy(state);return true;}
            catch(Exception e){Debug.LogWarning("[Rose save] "+e.Message);Status="자동 저장 실패 · 이전 저장은 보존됩니다.";return false;}
        }
        private void SaveNotebook()
        {
            if(state==null)return;
            if(state.phase!=RosePhase.Room){SaveCheckpoint();return;}
            // Never turn a room-in-progress into a checkpoint. Only personal edits to
            // previously saved records/memo/time rows may update the last lobby save.
            if(checkpoint==null)return;
            var copy=RoseMansionStore.Copy(checkpoint);copy.notebookMemo=state.notebookMemo;
            foreach(var r in copy.records)
            {var current=state.records.Single(x=>x.id==r.id);r.important=current.important;r.suspicious=current.suspicious;r.recheck=current.recheck;r.note=current.note;}
            copy.timeline=state.timeline.Where(t=>copy.records.Any(r=>r.id==t.recordId)).Select(RoseMansionStore.Copy).ToList();
            try{Store.WriteCheckpoint(copy);checkpoint=copy;}catch(Exception e){Debug.LogWarning("[Rose notebook] "+e.Message);Status="수첩 저장 실패 · 기존 저장을 보존합니다.";}
        }
        public void SetMemo(string text){if(state==null)return;state.notebookMemo=text??"";SaveNotebook();}
        public void SetRecordNote(string id,string text){var r=state?.records.SingleOrDefault(x=>x.id==id);if(r==null)return;r.note=text??"";SaveNotebook();}
        public void ToggleMark(string id,string mark)
        {
            var r=state?.records.SingleOrDefault(x=>x.id==id);if(r==null)return;
            if(mark=="important")r.important=!r.important;else if(mark=="suspicious")r.suspicious=!r.suspicious;else if(mark=="recheck")r.recheck=!r.recheck;
            SaveNotebook();Notify();
        }
        public void AddTimeline(string recordId)
        {
            if(state==null||!state.records.Any(r=>r.id==recordId)||state.timeline.Any(t=>t.recordId==recordId))return;
            state.timeline.Add(new RoseTimelineRow{recordId=recordId,order=state.timeline.Count});SaveNotebook();Notify();
        }
        public void SetTimelineTime(string id,string label){var row=state?.timeline.SingleOrDefault(r=>r.recordId==id);if(row==null)return;row.timeLabel=label??"";SaveNotebook();}
        public void MoveTimeline(string id,int delta)
        {
            if(state==null)return;var rows=state.timeline.OrderBy(r=>r.order).ToList();int at=rows.FindIndex(r=>r.recordId==id),to=at+delta;
            if(at<0||to<0||to>=rows.Count)return;var value=rows[at];rows[at]=rows[to];rows[to]=value;
            for(int i=0;i<rows.Count;i++)rows[i].order=i;SaveNotebook();Notify();
        }
        public void RemoveTimeline(string id){if(state==null)return;state.timeline.RemoveAll(t=>t.recordId==id);SaveNotebook();Notify();}
        public bool PrepareAccusation(string target,string who,string method,string weapon,out string error)
        {
            error=null;if(state==null||state.phase!=RosePhase.Accusation){error="최종 고발 단계가 아닙니다.";return false;}
            string npc=ResolveText(who,catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).Select(r=>new RoseTerm{id=r.npcId,name=r.name,aliases=r.aliases}).ToArray());
            string m=ResolveText(method,catalog.methods),w=ResolveText(weapon,catalog.weapons);
            if(string.IsNullOrEmpty(npc)||string.IsNullOrEmpty(m)||string.IsNullOrEmpty(w)||npc!=target){error="인물·수법·흉기를 명확히 적고, 지목한 인물과 입력한 이름이 같은지 확인하세요.";return false;}
            pendingAccusation=new RoseAccusation{selectedNpcId=target,culpritText=who,methodText=method,weaponText=weapon,normalizedNpcId=npc,normalizedMethodId=m,normalizedWeaponId=w};return true;
        }
        public void CancelAccusation(){pendingAccusation=null;}
        public void ConfirmAccusation()
        {
            if(state==null||state.phase!=RosePhase.Accusation||pendingAccusation==null)return;
            if(!state.truthReady){pendingAccusation=null;Notify("사건 정답·물적 증거가 아직 승인되지 않아 판정하지 않습니다. 입력 내용은 수정할 수 있습니다.");return;}
            state.submitted=pendingAccusation;pendingAccusation=null;
            state.outcome=AccusationVerdictRules.EvaluateFinalAxes(state.submitted.normalizedNpcId,state.submitted.normalizedMethodId,state.submitted.normalizedWeaponId,state.selectedCulprit,state.truthMethodId,state.truthWeaponId);
            state.phase=RosePhase.Ending;state.endingStep=RoseEndingStep.AccusedDialogue;SaveCheckpoint();
            presentation?.RequestAccusedDialogue(state.submitted.selectedNpcId);Notify("지목한 인물의 대화 연출 연결을 기다립니다.");
        }
        // Called by the presentation bridge only after an assigned approved asset completes.
        public void CompleteEndingPresentation(RoseEndingStep completed)
        {
            if(state==null||state.phase!=RosePhase.Ending||state.endingStep!=completed)return;
            if(completed==RoseEndingStep.AccusedDialogue)
            {state.endingStep=RoseEndingStep.Reenactment;presentation?.RequestReenactment(state.selectedCulprit);SaveCheckpoint();Notify("범행 재연 연출 연결을 기다립니다.");}
            else if(completed==RoseEndingStep.Reenactment)
            {
                try
                {
                    var archive=LoadArchive();var entry=archive.entries.Single(e=>e.culpritNpcId==state.selectedCulprit&&e.outcome==state.outcome);
                    if(!entry.unlocked){entry.unlocked=true;entry.firstCompletedUtc=DateTime.UtcNow.ToString("O");Store.WriteArchive(archive);}
                    state.endingStep=RoseEndingStep.Complete;state.phase=RosePhase.Report;SaveCheckpoint();Notify("사건 종결 보고서");
                }
                catch(Exception e){Debug.LogWarning("[Rose archive] "+e.Message);Notify("기록도감 저장 실패 · 기존 기록을 보존합니다.");}
            }
        }
        public RoseEndingArchive LoadArchive()
        {
            LoadCatalog();var people=catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId));var archive=Store.ReadArchive();
            if(archive==null)return new RoseEndingArchive{entries=people.SelectMany(r=>new[]{"Complete","Partial","Misjudged"}.Select(o=>new RoseEndingEntry{culpritNpcId=r.npcId,outcome=o})).ToList()};
            if(archive.schemaVersion!=1||archive.entries==null||archive.entries.Count!=18||people.Any(r=>new[]{"Complete","Partial","Misjudged"}.Any(o=>archive.entries.Count(e=>e.culpritNpcId==r.npcId&&e.outcome==o)!=1)))throw new InvalidDataException("18칸 기록도감 형식 확인이 필요합니다.");
            return archive;
        }
        private static string Normalize(string text)=>new string((text??"").Normalize(NormalizationForm.FormKC).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        private static string ResolveText(string raw,RoseTerm[] terms)
        {
            if(string.IsNullOrWhiteSpace(raw)||raw.Length>256)return null;
            var text=Normalize(raw);
            if(new[]{"아니","않","또는","혹은","모르"}.Any(text.Contains))return null;
            var exact=terms.Where(t=>new[]{t.id,t.name}.Concat(t.aliases??Array.Empty<string>()).Any(a=>Normalize(a)==text)).ToArray();
            if(exact.Length==1)return exact[0].id;
            var matches=terms.Where(t=>new[]{t.name}.Concat(t.aliases??Array.Empty<string>()).Where(a=>Normalize(a).Length>=2).Any(a=>text.Contains(Normalize(a)))).ToArray();
            return matches.Length==1?matches[0].id:null;
        }
        private void ShowMap()
        {
            if(mapCamera==null||state==null)return;
            if(state.phase==RosePhase.Room)mapCamera.FocusRoom(state.currentRoom);else mapCamera.Show(HotelMapView.SplitFloors);
        }
    }
}
