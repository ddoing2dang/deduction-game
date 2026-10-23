using System;
namespace DeductionGame {
 public sealed partial class GameManager {
 public string LastFeedback {get;private set;} = "";
 public int LastAITurnCount {get;private set;}
 public event Action<string> PublicMessage;
 public string HumanAbilityStatus => Human.IsCulprit ? $"잠금 {culpritPowerUses.Lock} / 교란 {culpritPowerUses.Disrupt} / 차단 {culpritPowerUses.RouteBlock} / 연막 {culpritPowerUses.Smoke}" : $"{GameDefinitions.AbilityNames[(int)Human.Ability]} / {Human.AbilityUsesLeft}회";
 public string MoveReason(int destination) {
 if(Human==null || Phase==GamePhase.GameOver)return "게임이 종료되었습니다.";
 if(destination<1 || destination>9)return "올바른 방을 선택하세요.";
 if(destination==Human.RoomId)return "현재 있는 방입니다.";
 if(TurnIndex!=0)return "AI 턴 진행 중입니다.";
 if(Human.Spectating)return "관전 중에는 이동할 수 없습니다.";
 if(Phase!=GamePhase.SetupMove && Phase!=GamePhase.Round)return "이동 단계가 아닙니다.";
 if(!MapGraph.IsConnected(Human.RoomId,destination) && !IsSecretPassage(Human.RoomId,destination))return "현재 방과 연결되어 있지 않습니다.";
 bool setup=Phase==GamePhase.SetupMove;
 if(!setup && ActiveRouteBlockedEdge==EdgeKey(Human.RoomId,destination))return "경로차단: 다음 통과 시도 1회가 막힌 뒤 해제됩니다.";
 int cost=MovementCost(Human,destination,setup,out _,out _,out _);
 if(Human.MovementPoints<cost)return $"이동 P 부족: {cost}P 필요 / 현재 {Human.MovementPoints}P (잠금·고장문 추가비용 포함)";
 return "";
 }
 public int PreviewMovementCost(int destination)=>MovementCost(Human,destination,Phase==GamePhase.SetupMove,out _,out _,out _);
 public string ActionReason(string action,int target,int destination,int weapon) {
 if(action=="Setup")return Phase==GamePhase.SetupMove ? "" : "초기 배치에서만 사용";
 if(action=="EndTurn" && Phase==GamePhase.Round && TurnIndex==0 && Human.Spectating)return "";
 if(!HumanCanAct)return Phase==GamePhase.SetupMove ? "초기 배치 완료 후 사용" : Human!=null && Human.Spectating ? "관전 중" : "내 정상 턴에 사용";
 bool final=Round==12 && (Human.IsCulprit ? !Human.FakeDeductionUsed : Human.LastDeductionRound!=12);
 if(action=="EndTurn")return final ? "R12 최종 추리를 먼저 실행하세요" : "";
 if(action=="Deduction") {
 if(Round<6)return "R6부터 추리 가능";
 if(Human.IsCulprit)return Human.FakeDeductionUsed ? "가짜추리 1회 완료" : Round!=12 && !CanActInRoom(Human.RoomId) ? "정전/연막으로 행동 불가" : "";
 if(Human.DeductionAttempts>=2)return "추리 2회 소진";
 if(Human.LastDeductionRound==Round)return "이번 라운드 추리 완료";
 return Round!=12 && (!CanActInRoom(Human.RoomId)||reSuspicionUsedThisTurn) ? "정전·연막 또는 재의심 직후 추리 불가" : "";
 }
 if(!CanActInRoom(Human.RoomId))return "정전/연막으로 방 행동 불가";
 if(action=="SuspectAction" || action=="Cooperate")return CanHumanInfoAction(target)?"":Human.InfoActionUsed?"이번 턴 정보행동 사용 완료":"같은 방의 활성 대상을 선택하세요";
 if(action=="Investigate")return !forensicsReady?"R7 과학수사 도착 후 사용":Human.RoomId!=1?"1번 방에서 확인":Human.ForensicsChecked?"이미 확인 완료":Human.InfoActionUsed?"이번 턴 정보행동 사용 완료":"";
 if(action=="Ability" || action=="AnalyzeLocation" || action=="AnalyzeWeapon") {
 if(Human.IsCulprit)return "수사자 능력 전용";
 if(Human.AbilityUsesLeft<=0)return "남은 능력 횟수 없음";
 if(action!="Ability")return Human.Ability==InvestigatorAbility.Analysis ? "" : "정보분석 능력 전용";
 if(Human.Ability==InvestigatorAbility.Analysis)return "아래 정보분석 버튼 사용";
 if(Human.Ability==InvestigatorAbility.ReSuspicion && (Round<7 || lastSuspicionTargetId<0 || players[lastSuspicionTargetId].RoomId!=Human.RoomId || lastSuspicionWeapon==CardRules.Weapons[weapon]))return "R7 이후 같은 방의 직전 의심 대상 / 다른 무기 선택";
 return "";
 }
 if(!Human.IsCulprit)return "범인 능력 전용";
 if(Round>11)return "범인 능력은 R11까지";
 if(action=="Lock" || action=="Block") {
 if(!MapGraph.IsConnected(Human.RoomId,destination))return "연결된 방을 능력 대상으로 선택하세요";
 return (action=="Lock"?culpritPowerUses.Lock:culpritPowerUses.RouteBlock)>0?"":"남은 횟수 없음";
 }
 if(action=="Disrupt")return culpritPowerUses.Disrupt>0 && target>0 && target<6 && !players[target].IsCulprit ? "" : "대상 또는 남은 횟수 확인";
 if(action=="Smoke")return culpritPowerUses.Smoke<=0?"남은 횟수 없음":Human.RoomId==ActiveBlackoutRoom || (destination==Human.RoomId?Human.RoomId%9+1:destination)==ActiveBlackoutRoom?"정전 방과 연막 중첩 불가":"";
 return "";
 }
 private int MovementCost(PlayerState player,int targetRoom,bool setupMove,out bool useGatekeeper,out bool useTracking,out bool useDetour) {
 string edge=EdgeKey(player.RoomId,targetRoom);
            int baseCost = 1;
            bool zeroBase = false;
            useGatekeeper = useTracking = useDetour = false;

            if (!setupMove && gatekeeperArmed && player == Human && Human.Ability == InvestigatorAbility.Gatekeeper)
            {
                zeroBase = true;
                useGatekeeper = true;
            }
            else if (!setupMove && trackingArmed && player == Human && Human.Ability == InvestigatorAbility.Tracking && RoomHasOtherPlayer(targetRoom, player.Id))
            {
                zeroBase = true;
                useTracking = true;
            }

            int extra = 0;
            if (!setupMove && brokenDoorEdge == edge && Round == extraEventRound)
                extra++;
            if (!setupMove && culpritLockEdge == edge && Round <= culpritLockUntilRound)
                extra++;

            if (!setupMove && detourArmed && player == Human && extra > 0)
            {
                extra--;
                useDetour = true;
            }

            return (zeroBase ? 0 : baseCost) + extra;

 }
 }
}


