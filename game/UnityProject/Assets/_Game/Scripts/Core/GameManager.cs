using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeductionGame
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<PlayerState> players = new();
        private readonly List<string> publicLog = new();
        private readonly List<string> undealtLocations = new();
        private readonly List<string> undealtWeapons = new();
        private readonly List<string> publicWitnessClues = new();
        private readonly System.Random rng = new();

        private CaseAnswer answer;

        private int selectedWeaponIndex;
        private int selectedSuspectIndex;
        private int selectedLocationIndex;
        private int selectedAbilityRoom = 1;
        private int selectedTargetPlayer = 1;

        private bool gatekeeperArmed;
        private bool trackingArmed;
        private bool detourArmed;
        private bool reserveArmed;
        private bool interrogationArmed;

        private int lastSuspicionTargetId = -1;
        private string lastSuspicionWeapon;

        private int keySpawnRound;
        private int keyRoom;
        private bool keySpawned;
        private bool keyTaken;

        private int chestSpawnRound = -1;
        private int chestRoom;
        private bool chestSpawned;
        private bool chestOpened;

        private ExtraEventType extraEvent;
        private int extraEventRound;
        private int blackoutRoom;
        private int blackoutUntilRound;
        private string brokenDoorEdge;
        private string culpritLockEdge;
        private int culpritLockUntilRound;
        private string routeBlockedEdge;
        private int smokeRoomA;
        private int smokeRoomB;
        private int smokeUntilRound;
        private int secretPassageA;
        private int secretPassageB;
        private int secretPassageUntilRound;

        private string forensicsLocation;
        private string forensicsWeapon;
        private bool forensicsReady;

        private readonly CulpritPowerUses culpritPowerUses = new();
        private readonly Dictionary<string, int> aiCulpritPowerRounds = new();

        private int witnessStage;
        private bool gameFinished;
        private string gameResult = "";

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public int Round { get; private set; }
        public int TurnIndex { get; private set; }
        public PlayerState Human => players.Count > 0 ? players[0] : null;

        private static readonly string[] RoomNames = CardRules.Locations;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartNewGame();

        public void StartNewGame()
        {
            players.Clear();
            publicLog.Clear();
            undealtLocations.Clear();
            undealtWeapons.Clear();
            publicWitnessClues.Clear();
            aiCulpritPowerRounds.Clear();

            gatekeeperArmed = trackingArmed = detourArmed = reserveArmed = interrogationArmed = false;
            lastSuspicionTargetId = -1;
            lastSuspicionWeapon = null;

            keySpawned = keyTaken = chestSpawned = chestOpened = false;
            chestSpawnRound = -1;
            forensicsReady = false;
            witnessStage = 0;
            gameFinished = false;
            gameResult = "";

            culpritPowerUses.Lock = 2;
            culpritPowerUses.Disrupt = 2;
            culpritPowerUses.RouteBlock = 1;
            culpritPowerUses.Smoke = 1;

            for (int i = 0; i < 6; i++)
                players.Add(new PlayerState(i, GameDefinitions.Names[i], i == 0, 1));

            CreateCaseAndDeal();
            AssignAbilities();
            PrepareEvents();

            Round = 0;
            TurnIndex = 0;
            Phase = GamePhase.SetupMove;
            Human.MovementPoints = 3;

            AddPublic("초기 배치 시작 · 전원 3P 이동 후 R1 시작");
        }

        private void CreateCaseAndDeal()
        {
            int culpritId = rng.Next(players.Count);
            string answerLocation = CardRules.Locations[rng.Next(CardRules.Locations.Length)];
            string answerWeapon = CardRules.Weapons[rng.Next(CardRules.Weapons.Length)];

            answer = new CaseAnswer
            {
                CulpritPlayerId = culpritId,
                Location = answerLocation,
                Weapon = answerWeapon
            };

            players[culpritId].IsCulprit = true;

            var locations = new List<string>();
            foreach (string card in CardRules.Locations)
                if (card != answerLocation) locations.Add(card);

            var weapons = new List<string>();
            foreach (string card in CardRules.Weapons)
                if (card != answerWeapon) weapons.Add(card);

            CardRules.Shuffle(locations, rng);
            CardRules.Shuffle(weapons, rng);

            for (int i = 0; i < players.Count; i++)
            {
                PlayerState p = players[i];
                p.LocationCard = locations[i];
                p.WeaponCard = weapons[i];
                p.PrivateNotes.Clear();
                p.KnownNotLocations.Clear();
                p.KnownNotWeapons.Clear();
                p.PrivateNotes.Add($"내 장소 카드: {p.LocationCard}");
                p.PrivateNotes.Add($"내 무기 카드: {p.WeaponCard}");
                p.PrivateNotes.Add(p.IsCulprit
                    ? $"내 역할: 범인 · 정답 {answerLocation} / {answerWeapon}"
                    : "내 역할: 수사자");
            }

            for (int i = players.Count; i < locations.Count; i++)
                undealtLocations.Add(locations[i]);

            for (int i = players.Count; i < weapons.Count; i++)
                undealtWeapons.Add(weapons[i]);
        }

        private void AssignAbilities()
        {
            Array values = Enum.GetValues(typeof(InvestigatorAbility));

            foreach (PlayerState p in players)
            {
                if (p.IsCulprit)
                {
                    p.Ability = InvestigatorAbility.None;
                    p.AbilityUsesLeft = 0;
                    continue;
                }

                p.Ability = (InvestigatorAbility)values.GetValue(rng.Next(1, values.Length));
                p.AbilityUsesLeft = GameDefinitions.AbilityBaseUses[p.Ability];
                p.PrivateNotes.Add($"능력: {GameDefinitions.AbilityNames[(int)p.Ability]} × {p.AbilityUsesLeft}");
            }

            PlayerState culprit = players[answer.CulpritPlayerId];
            if (!culprit.IsHuman)
            {
                aiCulpritPowerRounds["Lock"] = rng.Next(6, 12);
                aiCulpritPowerRounds["Disrupt"] = rng.Next(6, 12);
                aiCulpritPowerRounds["RouteBlock"] = rng.Next(6, 12);
                aiCulpritPowerRounds["Smoke"] = rng.Next(6, 12);
            }
        }

        private void PrepareEvents()
        {
            keySpawnRound = rng.Next(2, 6);
            keyRoom = rng.Next(1, 10);

            Array e = Enum.GetValues(typeof(ExtraEventType));
            extraEvent = (ExtraEventType)e.GetValue(rng.Next(1, e.Length));
            extraEventRound = rng.Next(6, 12);

            if (undealtLocations.Count > 0)
                forensicsLocation = undealtLocations[rng.Next(undealtLocations.Count)];
            if (undealtWeapons.Count > 0)
                forensicsWeapon = undealtWeapons[rng.Next(undealtWeapons.Count)];
        }

        public bool TryMoveHuman(int targetRoom)
        {
            if (Human == null || gameFinished) return false;
            if (Phase == GamePhase.Round && TurnIndex != 0) return false;

            if (Phase == GamePhase.SetupMove)
                return TryMove(Human, targetRoom, true);

            if (Phase != GamePhase.Round || Human.Spectating) return false;
            return TryMove(Human, targetRoom, false);
        }

        private bool TryMove(PlayerState player, int targetRoom, bool setupMove)
        {
            if (player.MovementPoints < 0 || targetRoom < 1 || targetRoom > 9) return false;

            bool secretPassage = IsSecretPassage(player.RoomId, targetRoom);
            if (!MapGraph.IsConnected(player.RoomId, targetRoom) && !secretPassage) return false;

            string edge = EdgeKey(player.RoomId, targetRoom);

            if (!setupMove && routeBlockedEdge == edge)
            {
                routeBlockedEdge = null;
                AddPublic($"{player.Name}의 통과 시도 차단 · 경로차단 해제");
                return false;
            }

            int baseCost = 1;
            bool zeroBase = false;

            if (!setupMove && gatekeeperArmed && player == Human && Human.Ability == InvestigatorAbility.Gatekeeper)
            {
                zeroBase = true;
                gatekeeperArmed = false;
                ConsumeAbility(Human);
            }
            else if (!setupMove && trackingArmed && player == Human && Human.Ability == InvestigatorAbility.Tracking && RoomHasOtherPlayer(targetRoom, player.Id))
            {
                zeroBase = true;
                trackingArmed = false;
                ConsumeAbility(Human);
            }

            int extra = 0;
            if (!setupMove && brokenDoorEdge == edge && Round == extraEventRound)
                extra++;
            if (!setupMove && culpritLockEdge == edge && Round <= culpritLockUntilRound)
                extra++;

            if (!setupMove && detourArmed && player == Human && extra > 0)
            {
                extra--;
                detourArmed = false;
                ConsumeAbility(Human);
            }

            int cost = (zeroBase ? 0 : baseCost) + extra;
            if (player.MovementPoints < cost) return false;

            player.MovementPoints -= cost;
            player.RoomId = targetRoom;

            AddPublic($"{player.Name} 이동 → {RoomName(targetRoom)} · {cost}P / 남은 {player.MovementPoints}P");
            CheckArrival(player);
            RuntimeBoardView.RefreshAll();
            return true;
        }

        public void CompleteHumanSetup()
        {
            if (Phase != GamePhase.SetupMove || Human.SetupComplete) return;

            Human.SetupComplete = true;
            AddPublic($"나즈 초기 배치 완료 · {RoomName(Human.RoomId)}");

            for (int i = 1; i < players.Count; i++)
                AutoSetup(players[i]);

            StartRoundOne();
        }

        private void AutoSetup(PlayerState player)
        {
            player.MovementPoints = 3;
            int moves = rng.Next(0, 4);

            for (int i = 0; i < moves && player.MovementPoints > 0; i++)
            {
                int[] options = MapGraph.GetConnections(player.RoomId);
                if (options.Length == 0) break;
                TryMove(player, options[rng.Next(options.Length)], true);
            }

            player.SetupComplete = true;
            AddPublic($"{player.Name} 초기 배치 완료 · {RoomName(player.RoomId)}");
        }

        private void StartRoundOne()
        {
            Phase = GamePhase.Round;
            Round = 1;
            TurnIndex = 0;
            BeginRound();
            BeginTurn();
            AddPublic("전원 초기 배치 완료 → R1 시작");
            RuntimeBoardView.RefreshAll();
        }

        private void BeginRound()
        {
            ExpireRoundEffects();

            if (Round == keySpawnRound && !keySpawned)
            {
                keySpawned = true;
                AddPublic($"R{Round} · 어딘가에 낡은 열쇠가 등장했다.");
            }

            if (chestSpawnRound == Round && !chestSpawned)
            {
                chestSpawned = true;
                AddPublic($"비밀상자 출현 · {RoomName(chestRoom)}");
            }

            if (Round == 3 || Round == 6 || Round == 9 || Round == 12)
                RevealWitnessClue();

            if (Round == 7)
            {
                forensicsReady = true;
                AddPublic("과학수사 결과가 도착했습니다. 중앙홀에서 행동 1회를 사용해 확인할 수 있습니다.");
            }

            if (Round == extraEventRound)
                TriggerExtraEvent();

            TriggerAiCulpritPowersIfScheduled();
        }

        private void BeginTurn()
        {
            if (Phase != GamePhase.Round || gameFinished) return;

            PlayerState p = players[TurnIndex];

            if (!p.Active || p.Spectating)
            {
                AdvanceTurn();
                return;
            }

            p.PreviousTurnStartRoomId = p.RoomId;
            p.InfoActionUsed = false;

            int baseMove = 2 + p.ReservedPoint;
            p.ReservedPoint = 0;

            if (p.DisruptionPenaltyNextTurn > 0)
            {
                baseMove = Mathf.Max(1, baseMove - p.DisruptionPenaltyNextTurn);
                p.DisruptionPenaltyNextTurn = 0;
            }

            p.MovementPoints = baseMove;

            gatekeeperArmed = trackingArmed = detourArmed = reserveArmed = interrogationArmed = false;
            lastSuspicionTargetId = -1;
            lastSuspicionWeapon = null;

            AddPublic($"R{Round} · {p.Name} 턴 시작 · {p.MovementPoints}P");
        }

        private void EndTurn(PlayerState p)
        {
            if (p.Ability == InvestigatorAbility.Reserve && reserveArmed && p.AbilityUsesLeft > 0 && p.MovementPoints > 0)
            {
                p.ReservedPoint = 1;
                ConsumeAbility(p);
                p.PrivateNotes.Add("비축: 다음 턴 +1P");
            }

            if (Round == 12 && !p.IsCulprit && p.Active && p.LastDeductionRound != 12)
            {
                if (p.IsHuman)
                {
                    p.PrivateNotes.Add("R12 강제 추리를 완료해야 턴을 종료할 수 있습니다.");
                    return;
                }

                RunAiDeduction(p, true);
                if (gameFinished) return;
            }

            if (p.IsCulprit && !p.FakeDeductionUsed && Round >= 6 && !p.IsHuman && Round >= 10)
                RunAiFakeDeduction(p);

            AdvanceTurn();
        }

        public void EndHumanTurn()
        {
            if (Phase != GamePhase.Round || TurnIndex != 0 || gameFinished) return;
            EndTurn(Human);

            while (!gameFinished && Phase == GamePhase.Round && TurnIndex != 0)
            {
                PlayerState ai = players[TurnIndex];
                RunAiTurn(ai);
                if (!gameFinished && TurnIndex != 0)
                    EndTurn(ai);
            }
        }

        private void AdvanceTurn()
        {
            if (gameFinished) return;

            TurnIndex++;

            if (TurnIndex >= players.Count)
            {
                TurnIndex = 0;

                if (Round >= 12)
                {
                    FinishGame($"R12 종료 · 범인 {players[answer.CulpritPlayerId].Name} 승리");
                    return;
                }

                Round++;
                BeginRound();
            }

            BeginTurn();
        }

        private void RunAiTurn(PlayerState p)
        {
            if (!p.Active || p.Spectating) return;

            UseAiAbilityBeforeMove(p);

            int moveBudget = rng.Next(0, p.MovementPoints + 1);
            for (int step = 0; step < moveBudget && p.MovementPoints > 0; step++)
            {
                int[] options = MapGraph.GetConnections(p.RoomId);
                if (options.Length == 0) break;
                int target = options[rng.Next(options.Length)];
                if (!TryMove(p, target, false)) break;
            }

            if (CanActInRoom(p.RoomId))
            {
                if (forensicsReady && !p.ForensicsChecked && p.RoomId == 1 && rng.NextDouble() < 0.55)
                    CheckForensics(p);
                else
                    RunAiInfoAction(p);
            }

            if (!p.IsCulprit && Round >= 6 && p.DeductionAttempts < 2 && p.LastDeductionRound != Round)
            {
                double chance = Round >= 10 ? 0.45 : 0.12;
                if (rng.NextDouble() < chance)
                    RunAiDeduction(p, false);
            }

            AddPublic($"{p.Name} 턴 행동 완료 · {RoomName(p.RoomId)}");
        }

        private void UseAiAbilityBeforeMove(PlayerState p)
        {
            if (p.IsCulprit || p.AbilityUsesLeft <= 0) return;

            if (p.Ability == InvestigatorAbility.Sprint && rng.NextDouble() < 0.35)
            {
                p.MovementPoints++;
                ConsumeAbility(p);
                p.PrivateNotes.Add("질주 사용 · +1P");
            }
            else if (p.Ability == InvestigatorAbility.Return && rng.NextDouble() < 0.1)
            {
                p.RoomId = p.PreviousTurnStartRoomId;
                ConsumeAbility(p);
            }
            else if (p.Ability == InvestigatorAbility.Reserve && p.MovementPoints > 0 && rng.NextDouble() < 0.25)
            {
                p.ReservedPoint = 1;
                ConsumeAbility(p);
            }
        }

        private void RunAiInfoAction(PlayerState p)
        {
            if (p.InfoActionUsed) return;

            var sameRoom = new List<PlayerState>();
            foreach (PlayerState other in players)
                if (other.Id != p.Id && other.Active && !other.Spectating && other.RoomId == p.RoomId)
                    sameRoom.Add(other);

            if (sameRoom.Count == 0) return;
            PlayerState target = sameRoom[rng.Next(sameRoom.Count)];

            if (rng.NextDouble() < 0.68)
            {
                string location = RoomName(p.RoomId);
                string weapon = CardRules.Weapons[rng.Next(CardRules.Weapons.Length)];
                ResolveSuspicion(p, target, location, weapon, false);
            }
            else
            {
                ResolveCooperation(p, target);
            }
        }

        public void HumanSuspicion(int targetId, string weapon)
        {
            if (!CanHumanInfoAction(targetId)) return;
            if (!CanActInRoom(Human.RoomId)) return;

            ResolveSuspicion(Human, players[targetId], RoomName(Human.RoomId), weapon, interrogationArmed);
            interrogationArmed = false;
        }

        private void ResolveSuspicion(PlayerState requester, PlayerState target, string location, string weapon, bool interrogation)
        {
            string shownCard = null;
            if (target.LocationCard == location) shownCard = target.LocationCard;
            else if (target.WeaponCard == weapon) shownCard = target.WeaponCard;

            bool success = shownCard != null;

            AddPublic($"{requester.Name} → {target.Name} 의심 · 장소 {location} / 무기 {weapon} · 결과 {(success ? "성공" : "실패")}" +
                      (success ? " · 공개 카드 비공개" : ""));

            if (success)
            {
                requester.PrivateNotes.Add($"의심 성공 · {target.Name} → 확인 카드: {shownCard}");
                target.PrivateNotes.Add($"의심 응답 · {requester.Name}에게 공개: {shownCard}");
            }
            else
            {
                requester.PrivateNotes.Add($"의심 실패 · {target.Name}은 {location}, {weapon} 둘 다 미보유");
                AddUnique(requester.KnownNotLocations, location);
                AddUnique(requester.KnownNotWeapons, weapon);
            }

            if (interrogation && requester.Ability == InvestigatorAbility.Interrogation && requester.AbilityUsesLeft > 0)
            {
                bool both = target.LocationCard == location && target.WeaponCard == weapon;
                requester.PrivateNotes.Add($"심문 추가 결과 · 두 카드 모두 보유: {(both ? "YES" : "NO")}");
                ConsumeAbility(requester);
            }

            requester.InfoActionUsed = true;
            lastSuspicionTargetId = target.Id;
            lastSuspicionWeapon = weapon;
        }

        public void HumanCooperation(int targetId)
        {
            if (!CanHumanInfoAction(targetId)) return;
            if (!CanActInRoom(Human.RoomId)) return;
            ResolveCooperation(Human, players[targetId]);
        }

        private void ResolveCooperation(PlayerState a, PlayerState b)
        {
            string shownToA = rng.Next(2) == 0 ? b.LocationCard : b.WeaponCard;
            string shownToB = rng.Next(2) == 0 ? a.LocationCard : a.WeaponCard;

            a.PrivateNotes.Add($"협조 · {b.Name}에게서 확인: {shownToA}");
            b.PrivateNotes.Add($"협조 · {a.Name}에게서 확인: {shownToB}");
            a.InfoActionUsed = true;

            AddPublic($"{a.Name} ↔ {b.Name} 협조 성공 · 공개된 카드 내용은 당사자만 확인");
        }

        private bool CanHumanInfoAction(int targetId)
        {
            if (Phase != GamePhase.Round || TurnIndex != 0 || Human.InfoActionUsed || Human.Spectating) return false;
            if (targetId <= 0 || targetId >= players.Count) return false;
            PlayerState target = players[targetId];
            return target.Active && !target.Spectating && target.RoomId == Human.RoomId;
        }

        public void HumanCheckForensics()
        {
            if (TurnIndex != 0 || !forensicsReady || Human.ForensicsChecked || Human.RoomId != 1 || Human.InfoActionUsed) return;
            if (!CanActInRoom(Human.RoomId)) return;
            CheckForensics(Human);
        }

        private void CheckForensics(PlayerState p)
        {
            p.ForensicsChecked = true;
            p.InfoActionUsed = true;
            p.PrivateNotes.Add($"과학수사 결과 · 미배분 장소: {forensicsLocation}");
            p.PrivateNotes.Add($"과학수사 결과 · 미배분 무기: {forensicsWeapon}");
            AddPublic($"{p.Name}이(가) 중앙홀에서 과학수사 결과를 확인했다.");
        }

        public void HumanDeduction()
        {
            if (TurnIndex != 0 || Human.IsCulprit || Human.Spectating || Round < 6) return;
            if (Human.DeductionAttempts >= 2 || Human.LastDeductionRound == Round) return;
            if (!CanActInRoom(Human.RoomId)) return;

            ResolveDeduction(Human,
                selectedSuspectIndex,
                CardRules.Locations[selectedLocationIndex],
                CardRules.Weapons[selectedWeaponIndex],
                false);
        }

        public void HumanFakeDeduction()
        {
            if (TurnIndex != 0 || !Human.IsCulprit || Human.FakeDeductionUsed || Round < 6) return;
            Human.FakeDeductionUsed = true;
            AddPublic($"{Human.Name}이(가) 추리를 선언했다 · 결과 실패");
            Human.PrivateNotes.Add("가짜추리 1회 사용 완료");
        }

        private void ResolveDeduction(PlayerState p, int suspectId, string location, string weapon, bool forced)
        {
            p.LastDeductionRound = Round;
            p.DeductionAttempts++;

            bool correct =
                suspectId == answer.CulpritPlayerId &&
                location == answer.Location &&
                weapon == answer.Weapon;

            AddPublic($"{p.Name} 추리 · {GameDefinitions.Names[suspectId]} / {location} / {weapon} → {(correct ? "정답" : "실패")}");

            if (correct)
            {
                FinishGame($"{p.Name} 정답 성공 · 수사자 승리");
                return;
            }

            p.PrivateNotes.Add($"추리 실패 {p.DeductionAttempts}/2");

            if (p.DeductionAttempts >= 2)
            {
                p.Spectating = true;
                p.Active = false;
                AddPublic($"{p.Name} 추리 2회 실패 · 관전 전환");
            }

            if (forced)
                p.PrivateNotes.Add("R12 강제 추리 완료");
        }

        private void RunAiDeduction(PlayerState p, bool forced)
        {
            int suspect = PickAiSuspect(p);
            string location = PickAiLocation(p);
            string weapon = PickAiWeapon(p);
            ResolveDeduction(p, suspect, location, weapon, forced);
        }

        private int PickAiSuspect(PlayerState p)
        {
            var candidates = new List<int>();
            for (int i = 0; i < players.Count; i++)
                if (!ContradictsWitness(i))
                    candidates.Add(i);

            if (candidates.Count == 0)
                for (int i = 0; i < players.Count; i++) candidates.Add(i);

            return candidates[rng.Next(candidates.Count)];
        }

        private string PickAiLocation(PlayerState p)
        {
            var candidates = new List<string>();
            foreach (string x in CardRules.Locations)
                if (x != p.LocationCard && !p.KnownNotLocations.Contains(x))
                    candidates.Add(x);

            return candidates.Count == 0 ? CardRules.Locations[rng.Next(CardRules.Locations.Length)] : candidates[rng.Next(candidates.Count)];
        }

        private string PickAiWeapon(PlayerState p)
        {
            var candidates = new List<string>();
            foreach (string x in CardRules.Weapons)
                if (x != p.WeaponCard && !p.KnownNotWeapons.Contains(x))
                    candidates.Add(x);

            return candidates.Count == 0 ? CardRules.Weapons[rng.Next(CardRules.Weapons.Length)] : candidates[rng.Next(candidates.Count)];
        }

        private bool ContradictsWitness(int id)
        {
            CharacterProfile p = GameDefinitions.Profiles[id];
            CharacterProfile culprit = GameDefinitions.Profiles[answer.CulpritPlayerId];

            if (witnessStage >= 1 && p.Hair != culprit.Hair) return true;
            if (witnessStage >= 2 && p.Ornament != culprit.Ornament) return true;
            if (witnessStage >= 3 && p.Tone != culprit.Tone) return true;
            if (witnessStage >= 4 && p.Style != culprit.Style) return true;
            return false;
        }

        private void RunAiFakeDeduction(PlayerState culprit)
        {
            culprit.FakeDeductionUsed = true;
            AddPublic($"{culprit.Name}이(가) 추리를 선언했다 · 결과 실패");
            culprit.PrivateNotes.Add("가짜추리 1회 사용 완료");
        }

        private void RevealWitnessClue()
        {
            CharacterProfile p = GameDefinitions.Profiles[answer.CulpritPlayerId];
            string clue;

            if (witnessStage == 0) clue = $"목격정보 · 머리 실루엣: {p.Hair}";
            else if (witnessStage == 1) clue = $"목격정보 · 대표 머리장식: {p.Ornament}";
            else if (witnessStage == 2) clue = $"목격정보 · 대표 색감: {p.Tone}";
            else clue = $"목격정보 · 의상 실루엣: {p.Style}";

            witnessStage = Mathf.Min(4, witnessStage + 1);
            publicWitnessClues.Add(clue);
            AddPublic(clue);
        }

        private void TriggerExtraEvent()
        {
            if (extraEvent == ExtraEventType.Blackout)
            {
                var allowed = new List<int>();
                for (int room = 1; room <= 9; room++)
                    if (room != smokeRoomA && room != smokeRoomB) allowed.Add(room);

                blackoutRoom = allowed[rng.Next(allowed.Count)];
                blackoutUntilRound = Round + 1;
                AddPublic($"추가 이벤트 · 정전: {RoomName(blackoutRoom)} · 2R 행동 불가");
            }
            else if (extraEvent == ExtraEventType.BrokenDoor)
            {
                brokenDoorEdge = RandomEdge();
                AddPublic($"추가 이벤트 · 고장난 문: {brokenDoorEdge} · 이번 라운드 +1P");
            }
            else if (extraEvent == ExtraEventType.SecretPassage)
            {
                secretPassageA = 6;
                secretPassageB = 9;
                secretPassageUntilRound = Round;
                AddPublic($"추가 이벤트 · 비밀통로: {RoomName(secretPassageA)} ↔ {RoomName(secretPassageB)} · 이번 라운드");
            }
        }

        private void ExpireRoundEffects()
        {
            if (Round > blackoutUntilRound) blackoutRoom = 0;
            if (Round > smokeUntilRound) { smokeRoomA = 0; smokeRoomB = 0; }
            if (Round > culpritLockUntilRound) culpritLockEdge = null;
            if (Round > secretPassageUntilRound) { secretPassageA = 0; secretPassageB = 0; }
            if (Round != extraEventRound) brokenDoorEdge = null;
        }

        private void TriggerAiCulpritPowersIfScheduled()
        {
            PlayerState culprit = players[answer.CulpritPlayerId];
            if (culprit.IsHuman || Round > 11) return;

            if (culpritPowerUses.Lock > 0 && aiCulpritPowerRounds.TryGetValue("Lock", out int lr) && lr == Round)
            {
                culpritLockEdge = RandomEdge();
                culpritLockUntilRound = Round;
                culpritPowerUses.Lock--;
                AddPublic($"라운드 사건 · 특정 연결 잠금 발생 · 이번 라운드 +1P");
            }

            if (culpritPowerUses.Disrupt > 0 && aiCulpritPowerRounds.TryGetValue("Disrupt", out int dr) && dr == Round)
            {
                var investigators = ActiveInvestigators();
                if (investigators.Count > 0)
                {
                    PlayerState target = investigators[rng.Next(investigators.Count)];
                    target.DisruptionPenaltyNextTurn = 1;
                    culpritPowerUses.Disrupt--;
                    AddPublic("라운드 사건 · 한 수사자의 다음 이동력이 감소합니다.");
                }
            }

            if (culpritPowerUses.RouteBlock > 0 && aiCulpritPowerRounds.TryGetValue("RouteBlock", out int rr) && rr == Round)
            {
                routeBlockedEdge = RandomEdge();
                culpritPowerUses.RouteBlock--;
                AddPublic("라운드 사건 · 특정 연결의 다음 통과 시도 1회가 차단됩니다.");
            }

            if (culpritPowerUses.Smoke > 0 && aiCulpritPowerRounds.TryGetValue("Smoke", out int sr) && sr == Round)
            {
                SetSmokeRandom();
                culpritPowerUses.Smoke--;
                AddPublic($"라운드 사건 · 연막 발생: {RoomName(smokeRoomA)}, {RoomName(smokeRoomB)}");
            }
        }

        public void HumanCulpritLock()
        {
            if (!CanHumanUseCulpritPower() || culpritPowerUses.Lock <= 0) return;
            int[] next = MapGraph.GetConnections(Human.RoomId);
            if (next.Length == 0) return;
            culpritLockEdge = EdgeKey(Human.RoomId, next[0]);
            culpritLockUntilRound = Round;
            culpritPowerUses.Lock--;
            AddPublic("라운드 사건 · 특정 연결 잠금 발생 · 이번 라운드 +1P");
        }

        public void HumanCulpritDisrupt()
        {
            if (!CanHumanUseCulpritPower() || culpritPowerUses.Disrupt <= 0) return;
            int id = Mathf.Clamp(selectedTargetPlayer, 1, 5);
            if (players[id].IsCulprit) return;
            players[id].DisruptionPenaltyNextTurn = 1;
            culpritPowerUses.Disrupt--;
            AddPublic("라운드 사건 · 한 수사자의 다음 이동력이 감소합니다.");
        }

        public void HumanCulpritRouteBlock()
        {
            if (!CanHumanUseCulpritPower() || culpritPowerUses.RouteBlock <= 0) return;
            int[] next = MapGraph.GetConnections(Human.RoomId);
            if (next.Length == 0) return;
            routeBlockedEdge = EdgeKey(Human.RoomId, next[0]);
            culpritPowerUses.RouteBlock--;
            AddPublic("라운드 사건 · 특정 연결의 다음 통과 시도 1회가 차단됩니다.");
        }

        public void HumanCulpritSmoke()
        {
            if (!CanHumanUseCulpritPower() || culpritPowerUses.Smoke <= 0) return;
            smokeRoomA = Human.RoomId;
            smokeRoomB = selectedAbilityRoom == Human.RoomId ? ((Human.RoomId % 9) + 1) : selectedAbilityRoom;
            if (smokeRoomA == blackoutRoom || smokeRoomB == blackoutRoom) return;
            smokeUntilRound = Round;
            culpritPowerUses.Smoke--;
            AddPublic($"라운드 사건 · 연막 발생: {RoomName(smokeRoomA)}, {RoomName(smokeRoomB)}");
        }

        private bool CanHumanUseCulpritPower()
        {
            return TurnIndex == 0 && Human.IsCulprit && Round >= 1 && Round <= 11 && !gameFinished;
        }

        private void SetSmokeRandom()
        {
            var rooms = new List<int>();
            for (int i = 1; i <= 9; i++)
                if (i != blackoutRoom) rooms.Add(i);

            smokeRoomA = rooms[rng.Next(rooms.Count)];
            do { smokeRoomB = rooms[rng.Next(rooms.Count)]; } while (smokeRoomB == smokeRoomA);
            smokeUntilRound = Round;
        }

        public void HumanUseAbilityPrimary()
        {
            if (TurnIndex != 0 || Human.IsCulprit || Human.AbilityUsesLeft <= 0 || !CanActInRoom(Human.RoomId)) return;

            switch (Human.Ability)
            {
                case InvestigatorAbility.Gatekeeper:
                    gatekeeperArmed = true;
                    Human.PrivateNotes.Add("문지기 준비 · 다음 통과 문의 기본 비용 0P");
                    break;
                case InvestigatorAbility.Sprint:
                    Human.MovementPoints++;
                    ConsumeAbility(Human);
                    Human.PrivateNotes.Add("질주 사용 · +1P");
                    break;
                case InvestigatorAbility.Leap:
                    Human.RoomId = Mathf.Clamp(selectedAbilityRoom, 1, 9);
                    ConsumeAbility(Human);
                    CheckArrival(Human);
                    RuntimeBoardView.RefreshAll();
                    Human.PrivateNotes.Add($"도약 사용 → {RoomName(Human.RoomId)}");
                    break;
                case InvestigatorAbility.Tracking:
                    trackingArmed = true;
                    Human.PrivateNotes.Add("추적 준비 · 다른 플레이어가 있는 방으로 이동 시 기본 비용 0P");
                    break;
                case InvestigatorAbility.Interrogation:
                    interrogationArmed = true;
                    Human.PrivateNotes.Add("심문 준비 · 다음 의심에 추가 YES/NO 확인");
                    break;
                case InvestigatorAbility.Analysis:
                    AnalyzeCard(Human, CardRules.Weapons[selectedWeaponIndex]);
                    ConsumeAbility(Human);
                    break;
                case InvestigatorAbility.ReSuspicion:
                    HumanReSuspicion();
                    break;
                case InvestigatorAbility.Return:
                    Human.RoomId = Human.PreviousTurnStartRoomId;
                    ConsumeAbility(Human);
                    CheckArrival(Human);
                    RuntimeBoardView.RefreshAll();
                    Human.PrivateNotes.Add($"회귀 사용 → {RoomName(Human.RoomId)}");
                    break;
                case InvestigatorAbility.Reserve:
                    reserveArmed = true;
                    Human.PrivateNotes.Add("비축 예약 · 턴 종료 시 남은 P가 있으면 1P 이월");
                    break;
                case InvestigatorAbility.Detour:
                    detourArmed = true;
                    Human.PrivateNotes.Add("우회 준비 · 다음 추가 +1P 비용 1회 무시");
                    break;
            }
        }

        private void AnalyzeCard(PlayerState p, string card)
        {
            bool held = false;
            foreach (PlayerState x in players)
                if (x.Holds(card)) { held = true; break; }

            p.PrivateNotes.Add($"정보분석 · {card} · 6명 중 보유자 존재: {(held ? "YES" : "NO")}");
        }

        private void HumanReSuspicion()
        {
            if (Human.Ability != InvestigatorAbility.ReSuspicion || Human.AbilityUsesLeft <= 0 || Round < 7) return;
            if (lastSuspicionTargetId < 0 || lastSuspicionTargetId >= players.Count) return;

            string weapon = CardRules.Weapons[selectedWeaponIndex];
            if (weapon == lastSuspicionWeapon) return;

            PlayerState target = players[lastSuspicionTargetId];
            if (target.RoomId != Human.RoomId) return;

            bool previousAction = Human.InfoActionUsed;
            Human.InfoActionUsed = false;
            ResolveSuspicion(Human, target, RoomName(Human.RoomId), weapon, false);
            Human.InfoActionUsed = previousAction || Human.InfoActionUsed;
            ConsumeAbility(Human);
            Human.PrivateNotes.Add("재의심 사용 완료");
        }

        private void ConsumeAbility(PlayerState p)
        {
            p.AbilityUsesLeft = Mathf.Max(0, p.AbilityUsesLeft - 1);
        }

        private bool CanActInRoom(int room)
        {
            if (blackoutRoom == room && Round <= blackoutUntilRound) return false;
            if ((smokeRoomA == room || smokeRoomB == room) && Round <= smokeUntilRound) return false;
            return true;
        }

        private void CheckArrival(PlayerState p)
        {
            if (keySpawned && !keyTaken && p.RoomId == keyRoom)
            {
                keyTaken = true;
                p.HasKey = true;
                p.PrivateNotes.Add("낡은 열쇠를 비공개로 획득했습니다.");

                int minRound = Mathf.Max(4, Round + 1);
                int maxRound = 8;
                chestSpawnRound = minRound <= maxRound ? rng.Next(minRound, maxRound + 1) : Round;
                chestRoom = rng.Next(1, 10);

                AddPublic($"{p.Name}이(가) 이동 중 무언가를 발견했다.");
            }

            if (p.HasKey && chestSpawned && !chestOpened && p.RoomId == chestRoom)
            {
                chestOpened = true;
                p.HasKey = false;

                if (undealtLocations.Count > 0)
                    p.PrivateNotes.Add($"비밀상자 · 미배분 장소: {undealtLocations[rng.Next(undealtLocations.Count)]}");
                if (undealtWeapons.Count > 0)
                    p.PrivateNotes.Add($"비밀상자 · 미배분 무기: {undealtWeapons[rng.Next(undealtWeapons.Count)]}");

                AddPublic($"{p.Name}이(가) 비밀상자를 열었다. 내용은 비공개.");
            }
        }

        private bool IsSecretPassage(int a, int b)
        {
            if (Round > secretPassageUntilRound) return false;
            return (a == secretPassageA && b == secretPassageB) || (a == secretPassageB && b == secretPassageA);
        }

        private bool RoomHasOtherPlayer(int room, int exceptId)
        {
            foreach (PlayerState p in players)
                if (p.Id != exceptId && p.Active && !p.Spectating && p.RoomId == room) return true;
            return false;
        }

        private List<PlayerState> ActiveInvestigators()
        {
            var list = new List<PlayerState>();
            foreach (PlayerState p in players)
                if (!p.IsCulprit && p.Active && !p.Spectating) list.Add(p);
            return list;
        }

        private string RandomEdge()
        {
            var edges = new List<string>();
            for (int a = 1; a <= 9; a++)
            {
                foreach (int b in MapGraph.GetConnections(a))
                    if (a < b) edges.Add(EdgeKey(a, b));
            }

            return edges[rng.Next(edges.Count)];
        }

        private static string EdgeKey(int a, int b)
        {
            return a < b ? $"{a}-{b}" : $"{b}-{a}";
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (!list.Contains(value)) list.Add(value);
        }

        private static string RoomName(int id)
        {
            return RoomNames[Mathf.Clamp(id - 1, 0, RoomNames.Length - 1)];
        }

        private void FinishGame(string result)
        {
            gameFinished = true;
            gameResult = result;
            Phase = GamePhase.GameOver;
            AddPublic($"게임 종료 · {result}");
        }

        private void AddPublic(string message)
        {
            publicLog.Add(message);
            if (publicLog.Count > 18) publicLog.RemoveAt(0);
            Debug.Log(message);
        }

        private void OnGUI()
        {
            float panelWidth = Mathf.Min(520, Screen.width * 0.42f);
            GUILayout.BeginArea(new Rect(14, 14, panelWidth, Screen.height - 28), GUI.skin.box);

            GUILayout.Label("<b>Deduction Game · Production Core</b>", RichLabel());

            if (Human != null)
            {
                GUILayout.Label($"상태: {Phase}");
                GUILayout.Label($"라운드: {(Round == 0 ? "초기 배치" : $"R{Round}")}");
                GUILayout.Label($"턴: {(players.Count > TurnIndex ? players[TurnIndex].Name : "-")}");
                GUILayout.Label($"내 위치: {RoomName(Human.RoomId)} · {Human.MovementPoints}P");
                GUILayout.Label($"역할: {(Human.IsCulprit ? "범인" : "수사자")}");
                GUILayout.Label($"내 카드: {Human.LocationCard} / {Human.WeaponCard}");

                if (!Human.IsCulprit)
                    GUILayout.Label($"능력: {GameDefinitions.AbilityNames[(int)Human.Ability]} · {Human.AbilityUsesLeft}회");

                if (Phase == GamePhase.SetupMove)
                    DrawSetupUi();
                else if (Phase == GamePhase.Round && TurnIndex == 0 && !Human.Spectating)
                    DrawHumanTurnUi();
                else if (Phase == GamePhase.GameOver)
                {
                    GUILayout.Space(8);
                    GUILayout.Label($"<b>{gameResult}</b>", RichLabel());
                    if (GUILayout.Button("새 게임", GUILayout.Height(34)))
                        StartNewGame();
                }

                GUILayout.Space(8);
                GUILayout.Label("<b>공개 목격정보</b>", RichLabel());
                foreach (string clue in publicWitnessClues)
                    GUILayout.Label(clue);

                GUILayout.Space(8);
                GUILayout.Label("<b>내 비공개 정보</b>", RichLabel());
                int noteStart = Mathf.Max(0, Human.PrivateNotes.Count - 8);
                for (int i = noteStart; i < Human.PrivateNotes.Count; i++)
                    GUILayout.Label(Human.PrivateNotes[i]);
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>공개 로그</b>", RichLabel());
            int logStart = Mathf.Max(0, publicLog.Count - 9);
            for (int i = logStart; i < publicLog.Count; i++)
                GUILayout.Label(publicLog[i]);

            GUILayout.EndArea();
        }

        private void DrawSetupUi()
        {
            GUILayout.Space(8);
            GUILayout.Label("방 클릭으로 최대 3P 이동 → 배치 완료");
            if (GUILayout.Button("초기 배치 완료", GUILayout.Height(34)))
                CompleteHumanSetup();
        }

        private void DrawHumanTurnUi()
        {
            GUILayout.Space(6);

            selectedWeaponIndex = Mathf.Clamp(selectedWeaponIndex, 0, CardRules.Weapons.Length - 1);
            selectedSuspectIndex = Mathf.Clamp(selectedSuspectIndex, 0, players.Count - 1);
            selectedLocationIndex = Mathf.Clamp(selectedLocationIndex, 0, CardRules.Locations.Length - 1);
            selectedAbilityRoom = Mathf.Clamp(selectedAbilityRoom, 1, 9);
            selectedTargetPlayer = Mathf.Clamp(selectedTargetPlayer, 1, 5);

            GUILayout.Label("<b>정보 행동</b>", RichLabel());
            CycleRow("의심 무기", CardRules.Weapons, ref selectedWeaponIndex);

            for (int i = 1; i < players.Count; i++)
            {
                PlayerState target = players[i];
                if (!target.Active || target.Spectating || target.RoomId != Human.RoomId) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(target.Name, GUILayout.Width(100));
                GUI.enabled = !Human.InfoActionUsed && CanActInRoom(Human.RoomId);
                if (GUILayout.Button("의심", GUILayout.Width(80)))
                    HumanSuspicion(target.Id, CardRules.Weapons[selectedWeaponIndex]);
                if (GUILayout.Button("협조", GUILayout.Width(80)))
                    HumanCooperation(target.Id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (forensicsReady && !Human.ForensicsChecked && Human.RoomId == 1)
            {
                GUI.enabled = !Human.InfoActionUsed && CanActInRoom(Human.RoomId);
                if (GUILayout.Button("과학수사 결과 확인", GUILayout.Height(30)))
                    HumanCheckForensics();
                GUI.enabled = true;
            }

            if (!Human.IsCulprit)
                DrawInvestigatorAbilityUi();
            else
                DrawCulpritPowerUi();

            GUILayout.Space(6);
            GUILayout.Label("<b>추리</b>", RichLabel());

            if (!Human.IsCulprit)
            {
                CycleRow("범인", GameDefinitions.Names, ref selectedSuspectIndex);
                CycleRow("장소", CardRules.Locations, ref selectedLocationIndex);
                GUILayout.Label($"무기: {CardRules.Weapons[selectedWeaponIndex]}");

                GUI.enabled = Round >= 6 && Human.DeductionAttempts < 2 && Human.LastDeductionRound != Round && CanActInRoom(Human.RoomId);
                if (GUILayout.Button(Round == 12 ? "강제 추리 실행" : "추리 실행", GUILayout.Height(32)))
                    HumanDeduction();
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = Round >= 6 && !Human.FakeDeductionUsed;
                if (GUILayout.Button("가짜 추리", GUILayout.Height(32)))
                    HumanFakeDeduction();
                GUI.enabled = true;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("턴 종료", GUILayout.Height(36)))
                EndHumanTurn();
        }

        private void DrawInvestigatorAbilityUi()
        {
            GUILayout.Space(6);
            GUILayout.Label("<b>능력</b>", RichLabel());

            if (Human.Ability == InvestigatorAbility.Leap ||
                Human.Ability == InvestigatorAbility.Smoke)
            {
                // reserved
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("방 -", GUILayout.Width(70))) selectedAbilityRoom = selectedAbilityRoom == 1 ? 9 : selectedAbilityRoom - 1;
            GUILayout.Label($"대상 방: {RoomName(selectedAbilityRoom)}", GUILayout.Width(210));
            if (GUILayout.Button("방 +", GUILayout.Width(70))) selectedAbilityRoom = selectedAbilityRoom == 9 ? 1 : selectedAbilityRoom + 1;
            GUILayout.EndHorizontal();

            GUI.enabled = Human.AbilityUsesLeft > 0 && CanActInRoom(Human.RoomId);
            if (GUILayout.Button($"{GameDefinitions.AbilityNames[(int)Human.Ability]} 사용/준비", GUILayout.Height(30)))
                HumanUseAbilityPrimary();
            GUI.enabled = true;
        }

        private void DrawCulpritPowerUi()
        {
            GUILayout.Space(6);
            GUILayout.Label("<b>범인 능력 · R11까지</b>", RichLabel());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("대상 -", GUILayout.Width(70))) selectedTargetPlayer = selectedTargetPlayer <= 1 ? 5 : selectedTargetPlayer - 1;
            GUILayout.Label($"교란 대상: {players[selectedTargetPlayer].Name}", GUILayout.Width(210));
            if (GUILayout.Button("대상 +", GUILayout.Width(70))) selectedTargetPlayer = selectedTargetPlayer >= 5 ? 1 : selectedTargetPlayer + 1;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("연막방 -", GUILayout.Width(70))) selectedAbilityRoom = selectedAbilityRoom == 1 ? 9 : selectedAbilityRoom - 1;
            GUILayout.Label($"연막 2번방: {RoomName(selectedAbilityRoom)}", GUILayout.Width(210));
            if (GUILayout.Button("연막방 +", GUILayout.Width(70))) selectedAbilityRoom = selectedAbilityRoom == 9 ? 1 : selectedAbilityRoom + 1;
            GUILayout.EndHorizontal();

            GUI.enabled = Round <= 11;
            if (GUILayout.Button($"잠금 ({culpritPowerUses.Lock})")) HumanCulpritLock();
            if (GUILayout.Button($"교란 ({culpritPowerUses.Disrupt})")) HumanCulpritDisrupt();
            if (GUILayout.Button($"경로차단 ({culpritPowerUses.RouteBlock})")) HumanCulpritRouteBlock();
            if (GUILayout.Button($"연막 ({culpritPowerUses.Smoke})")) HumanCulpritSmoke();
            GUI.enabled = true;
        }

        private static void CycleRow(string label, string[] values, ref int index)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(38))) index = (index + values.Length - 1) % values.Length;
            GUILayout.Label($"{label}: {values[index]}", GUILayout.Width(270));
            if (GUILayout.Button("▶", GUILayout.Width(38))) index = (index + 1) % values.Length;
            GUILayout.EndHorizontal();
        }

        private static GUIStyle RichLabel()
        {
            return new GUIStyle(GUI.skin.label) { richText = true };
        }
    }
}
