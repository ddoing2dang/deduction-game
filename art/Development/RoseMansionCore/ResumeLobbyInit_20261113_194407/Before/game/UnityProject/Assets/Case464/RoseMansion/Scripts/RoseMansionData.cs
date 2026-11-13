using System;
using System.Collections.Generic;
using System.Linq;

namespace Case464.RoseMansion
{
    public enum RosePhase { Opening, Lobby, Room, LobbyReview, Accusation, Ending, Report }
    public enum RoseEndingStep { AccusedDialogue, Reenactment, Complete }
    [Serializable] public sealed class RoseTerm
    {
        public string id, name;
        public string[] aliases=Array.Empty<string>();
        public string[] compatibleMethodIds=Array.Empty<string>();
    }
    [Serializable] public sealed class RoseSlot
    {
        // Structural slot IDs are not evidence IDs. Empty evidence IDs stay empty.
        public string id, key, label, kind, body, evidenceId, sourceRevision;
        public bool meaningful, approved;
        public bool Ready => approved&&!string.IsNullOrWhiteSpace(body)&&!string.IsNullOrWhiteSpace(sourceRevision);
    }
    [Serializable] public sealed class RoseRoom
    {
        public string id, number, npcId, name;
        public int floor;
        public string[] aliases=Array.Empty<string>();
        public RoseSlot greeting;
        public RoseSlot[] slots=Array.Empty<RoseSlot>();
        public string Caption => string.IsNullOrEmpty(number)?name:number+"호 · "+name;
    }
    [Serializable] public sealed class RoseBranch
    {
        public string culpritNpcId, methodId, weaponId, accusedDialogueAsset, reenactmentAsset;
        public bool truthApproved, consistencyApproved, endingAssetsApproved;
        public RoseSlot dialogueOverride, q1Override;
        public RoseSlot[] evidenceOverrides=Array.Empty<RoseSlot>();
    }
    [Serializable] public sealed class RoseReward
    {
        public string id, kind; // Search / Question / Record; no key/unlock reward exists.
        public bool approved;
        public RoseSlot record;
    }
    [Serializable] public sealed class RoseCatalog
    {
        public int schemaVersion=1;
        public string contentRevision, source;
        public bool contentApproved;
        public RoseRoom[] rooms=Array.Empty<RoseRoom>();
        public RoseTerm[] methods=Array.Empty<RoseTerm>(), weapons=Array.Empty<RoseTerm>();
        public RoseBranch[] branches=Array.Empty<RoseBranch>();
        public RoseReward[] specialEventRewards=Array.Empty<RoseReward>(), emptySearchRewards=Array.Empty<RoseReward>();
        public RoseRoom Room(string id)=>rooms.SingleOrDefault(r=>r.id==id);
        public RoseBranch Branch(string id)=>branches.SingleOrDefault(b=>b.culpritNpcId==id);
        public IEnumerable<RoseRoom> Investigable=>rooms.Where(r=>r.id!="LOBBY");
        public bool TruthReady(RoseBranch b)=>contentApproved&&b!=null&&b.truthApproved&&b.consistencyApproved&&
            methods.Any(m=>m.id==b.methodId)&&weapons.Any(w=>w.id==b.weaponId&&w.compatibleMethodIds.Contains(b.methodId));
        // Do not reveal the selected culprit through a partially connected Q1/greeting.
        // All six branches and common records must be ready before investigation text is released.
        public bool InvestigationReady=>contentApproved&&branches.All(b=>TruthReady(b)&&b.dialogueOverride.Ready&&b.q1Override.Ready&&b.evidenceOverrides.All(s=>s.Ready))&&
            Investigable.All(r=>r.slots.All(s=>s.Ready))&&rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).All(r=>r.greeting!=null&&r.greeting.Ready);
        public string ValidateStructure()
        {
            var ids=new[]{"LOBBY","ROOM_MATTO","ROOM_JIRO","ROOM_BOKI","ROOM_MARIHI","ROOM_NAZ","ROOM_MOONIRE","CRIME_SCENE"};
            if(rooms==null||rooms.Length!=8||ids.Any(id=>rooms.Count(r=>r!=null&&r.id==id)!=1))return "기존 8개 공간 구성이 필요합니다.";
            if(methods==null||methods.Length!=4||weapons==null||weapons.Length!=12||weapons.Select(w=>w.id).Distinct().Count()!=12)return "확정된 4개 수법·12개 흉기가 필요합니다.";
            if(branches==null||branches.Length!=6||rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).Any(r=>branches.Count(b=>b!=null&&b.culpritNpcId==r.npcId)!=1))return "6명의 범인 분기 구조가 필요합니다.";
            foreach(var r in Investigable)
            {
                if(r.slots==null||r.slots.Length!=6||r.slots.Select(s=>s.id).Distinct().Count()!=6)return "객실별 6개 정보 슬롯이 필요합니다.";
                string[] keys=r.id=="CRIME_SCENE"?new[]{"M1","M2","M3","V1","V2","V3"}:new[]{"M1","M2","M3","Q1","Q2","Q3"};
                if(keys.Any(k=>r.slots.Count(s=>s.key==k)!=1)||r.slots.Count(s=>s.kind=="Search"&&s.meaningful)!=2||r.slots.Count(s=>s.kind=="Search"&&!s.meaningful)!=1)return "돋보기 2+1 및 질문/시신 정보 3개 구조가 필요합니다.";
            }
            foreach(var b in branches)
            {
                var r=rooms.Single(x=>x.npcId==b.culpritNpcId);
                if(b.q1Override==null||b.q1Override.key!="Q1"||b.q1Override.id!=r.id+":Q1"||b.dialogueOverride==null||b.dialogueOverride.key!="GREETING")return "범인 전용 인사 1개·Q1 1개 구조를 확인하세요.";
                if(b.evidenceOverrides==null||b.evidenceOverrides.Any(s=>s==null||!(s.key.StartsWith("M")||s.key.StartsWith("V"))||!Investigable.SelectMany(x=>x.slots).Any(x=>x.id==s.id&&x.key==s.key)))return "증거 분기는 등록된 돋보기/시신 정보에만 연결할 수 있습니다.";
                if(b.evidenceOverrides.Select(s=>s.id).Distinct().Count()!=b.evidenceOverrides.Length)return "증거 분기 중복입니다.";
            }
            return null;
        }
    }
    [Serializable] public sealed class RoseRoomProgress
    {
        public string roomId;
        public int visitedDay;
        public List<string> used=new List<string>();
        public bool Exhausted=>used.Count(s=>s.StartsWith("M"))>=3||used.Count(s=>s.StartsWith("Q")||s.StartsWith("V"))>=3;
    }
    [Serializable] public sealed class RoseRecord
    {
        public string id, slotId, evidenceId, roomId, npcId, title, body, note="";
        public int day, period;
        public bool important, suspicious, recheck;
    }
    [Serializable] public sealed class RoseTimelineRow { public string recordId, timeLabel=""; public int order; }
    [Serializable] public sealed class RoseDailyEvent { public int day; public bool won, pendingContent; public string rewardId; }
    [Serializable] public sealed class RoseItem { public string instanceId, definitionId, kind; public bool used; }
    [Serializable] public sealed class RoseAccusation
    {
        public string selectedNpcId, culpritText, methodText, weaponText;
        public string normalizedNpcId, normalizedMethodId, normalizedWeaponId;
    }
    [Serializable] public sealed class RoseProgress
    {
        public string runId, contentHash, selectedCulprit, truthMethodId, truthWeaponId;
        public bool truthReady;
        public int seed, randomDraws, turnIndex, preparedTurn=-1, visits, skippedTurns;
        public RosePhase phase;
        public string currentRoom="LOBBY", lockedRoom="", notebookMemo="";
        public bool searchedThisVisit, questionedThisVisit;
        public int extraSearchCredits, extraQuestionCredits;
        public List<RoseRoomProgress> rooms=new List<RoseRoomProgress>();
        public List<RoseRecord> records=new List<RoseRecord>();
        public List<RoseTimelineRow> timeline=new List<RoseTimelineRow>();
        public List<RoseDailyEvent> dailyEvents=new List<RoseDailyEvent>();
        public List<RoseItem> items=new List<RoseItem>();
        public RoseAccusation submitted;
        public string outcome;
        public RoseEndingStep endingStep;
        public int Day=>Math.Min(5,turnIndex/2+1);
        public int Period=>turnIndex%2;
        public RoseRoomProgress Room(string id)=>rooms.Single(r=>r.roomId==id);
    }
    [Serializable] public sealed class RoseEndingEntry
    {
        public string culpritNpcId, outcome, firstCompletedUtc;
        public bool unlocked;
    }
    [Serializable] public sealed class RoseEndingArchive { public int schemaVersion=1; public List<RoseEndingEntry> entries=new List<RoseEndingEntry>(); }
}
