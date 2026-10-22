using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeductionGame
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly List<PlayerState> players = new();
        private readonly System.Random rng = new();
        private readonly List<string> publicLog = new();
        private readonly List<string> undealtLocations = new();
        private readonly List<string> undealtWeapons = new();

        private static readonly string[] Names =
        {
            "나즈", "찌로", "마리히", "마또", "보키", "문이레"
        };

        private CaseAnswer answer;
        private int selectedWeaponIndex;

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public int Round { get; private set; }
        public int TurnIndex { get; private set; }

        public PlayerState Human => players.Count > 0 ? players[0] : null;

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

            for (int i = 0; i < 6; i++)
                players.Add(new PlayerState(i, Names[i], i == 0, 1));

            CreateCaseAndDeal();

            Round = 0;
            TurnIndex = 0;
            Phase = GamePhase.SetupMove;
            Human.MovementPoints = 3;

            AddPublic("초기 배치 시작: 전원 3P 이동 후 R1 시작");
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
            foreach (var card in CardRules.Locations)
                if (card != answerLocation) locations.Add(card);

            var weapons = new List<string>();
            foreach (var card in CardRules.Weapons)
                if (card != answerWeapon) weapons.Add(card);

            CardRules.Shuffle(locations, rng);
            CardRules.Shuffle(weapons, rng);

            for (int i = 0; i < players.Count; i++)
            {
                players[i].LocationCard = locations[i];
                players[i].WeaponCard = weapons[i];
                players[i].PrivateNotes.Clear();
                players[i].PrivateNotes.Add($"내 장소 카드: {players[i].LocationCard}");
                players[i].PrivateNotes.Add($"내 무기 카드: {players[i].WeaponCard}");
                players[i].PrivateNotes.Add(players[i].IsCulprit
                    ? $"내 역할: 범인 / 정답 장소 {answerLocation} / 정답 무기 {answerWeapon}"
                    : "내 역할: 수사관");
            }

            for (int i = players.Count; i < locations.Count; i++)
                undealtLocations.Add(locations[i]);

            for (int i = players.Count; i < weapons.Count; i++)
                undealtWeapons.Add(weapons[i]);
        }

        public bool TryMoveHuman(int targetRoom)
        {
            if (Human == null || Phase == GamePhase.GameOver) return false;
            if (Phase == GamePhase.Round && TurnIndex != 0) return false;
            if (Human.MovementPoints <= 0) return false;
            if (!MapGraph.IsConnected(Human.RoomId, targetRoom)) return false;

            Human.RoomId = targetRoom;
            Human.MovementPoints -= 1;
            AddPublic($"나즈 이동 → {targetRoom}번 방 / 남은 {Human.MovementPoints}P");
            return true;
        }

        public void CompleteHumanSetup()
        {
            if (Phase != GamePhase.SetupMove || Human.SetupComplete) return;

            Human.SetupComplete = true;
            AddPublic($"나즈 초기 배치 완료: {Human.RoomId}번 방");

            for (int i = 1; i < players.Count; i++)
                AutoSetup(players[i]);

            StartRoundOne();
        }

        private void AutoSetup(PlayerState player)
        {
            player.MovementPoints = 3;
            int steps = rng.Next(1, 4);

            for (int step = 0; step < steps && player.MovementPoints > 0; step++)
            {
                var options = MapGraph.GetConnections(player.RoomId);
                if (options.Length == 0) break;
                player.RoomId = options[rng.Next(options.Length)];
                player.MovementPoints--;
            }

            player.SetupComplete = true;
            AddPublic($"{player.Name} 초기 배치 완료: {player.RoomId}번 방");
        }

        private void StartRoundOne()
        {
            Phase = GamePhase.Round;
            Round = 1;
            TurnIndex = 0;
            BeginTurn();
            AddPublic("모든 플레이어 배치 완료 → R1 시작");
        }

        private void BeginTurn()
        {
            if (Phase != GamePhase.Round) return;

            var player = players[TurnIndex];
            player.MovementPoints = 2;
            AddPublic($"R{Round} · {player.Name} 턴 시작 · 2P");
        }

        public void HumanSuspicion(int targetPlayerId, string weapon)
        {
            if (!CanHumanActWith(targetPlayerId)) return;

            var target = players[targetPlayerId];
            string location = CardRules.Locations[Mathf.Clamp(Human.RoomId - 1, 0, CardRules.Locations.Length - 1)];
            ResolveSuspicion(Human, target, location, weapon);
        }

        public void HumanCooperation(int targetPlayerId)
        {
            if (!CanHumanActWith(targetPlayerId)) return;

            var target = players[targetPlayerId];

            string shownToHuman = rng.Next(2) == 0 ? target.LocationCard : target.WeaponCard;
            string shownToTarget = rng.Next(2) == 0 ? Human.LocationCard : Human.WeaponCard;

            Human.PrivateNotes.Add($"협조 · {target.Name}에게서 확인: {shownToHuman}");
            target.PrivateNotes.Add($"협조 · {Human.Name}에게서 확인: {shownToTarget}");

            AddPublic($"{Human.Name} ↔ {target.Name} 협조 성공 · 공개된 카드 내용은 당사자만 확인");
        }

        private bool CanHumanActWith(int targetPlayerId)
        {
            if (Phase != GamePhase.Round || TurnIndex != 0) return false;
            if (targetPlayerId <= 0 || targetPlayerId >= players.Count) return false;
            return players[targetPlayerId].RoomId == Human.RoomId && players[targetPlayerId].Active;
        }

        private void ResolveSuspicion(PlayerState requester, PlayerState target, string location, string weapon)
        {
            string shownCard = null;

            if (target.LocationCard == location)
                shownCard = target.LocationCard;
            else if (target.WeaponCard == weapon)
                shownCard = target.WeaponCard;

            bool success = shownCard != null;

            AddPublic($"{requester.Name} → {target.Name} 의심 · 장소 {location} / 무기 {weapon} · 결과 {(success ? "성공" : "실패")}" +
                      (success ? " · 공개 카드 비공개" : ""));

            if (success)
            {
                requester.PrivateNotes.Add($"의심 성공 · {target.Name} / {location} / {weapon} → 확인 카드: {shownCard}");
                target.PrivateNotes.Add($"의심 응답 · {requester.Name}에게 공개한 카드: {shownCard}");
            }
            else
            {
                requester.PrivateNotes.Add($"의심 실패 · {target.Name}은 {location}, {weapon} 둘 다 보유하지 않음");
            }
        }

        public void EndHumanTurn()
        {
            if (Phase != GamePhase.Round || TurnIndex != 0) return;

            AdvanceTurn();

            while (Phase == GamePhase.Round && TurnIndex != 0)
            {
                RunAiTurn(players[TurnIndex]);
                AdvanceTurn();
            }
        }

        private void RunAiTurn(PlayerState player)
        {
            int moves = rng.Next(0, 3);

            for (int i = 0; i < moves && player.MovementPoints > 0; i++)
            {
                var options = MapGraph.GetConnections(player.RoomId);
                if (options.Length == 0) break;
                player.RoomId = options[rng.Next(options.Length)];
                player.MovementPoints--;
            }

            var sameRoom = new List<PlayerState>();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Id != player.Id &&
                    players[i].Active &&
                    players[i].RoomId == player.RoomId)
                    sameRoom.Add(players[i]);
            }

            if (sameRoom.Count > 0)
            {
                var target = sameRoom[rng.Next(sameRoom.Count)];

                if (rng.NextDouble() < 0.65)
                {
                    string location = CardRules.Locations[Mathf.Clamp(player.RoomId - 1, 0, CardRules.Locations.Length - 1)];
                    string weapon = CardRules.Weapons[rng.Next(CardRules.Weapons.Length)];
                    ResolveSuspicion(player, target, location, weapon);
                }
                else
                {
                    string shownToPlayer = rng.Next(2) == 0 ? target.LocationCard : target.WeaponCard;
                    string shownToTarget = rng.Next(2) == 0 ? player.LocationCard : player.WeaponCard;
                    player.PrivateNotes.Add($"협조 · {target.Name}에게서 확인: {shownToPlayer}");
                    target.PrivateNotes.Add($"협조 · {player.Name}에게서 확인: {shownToTarget}");
                    AddPublic($"{player.Name} ↔ {target.Name} 협조 성공 · 공개된 카드 내용은 당사자만 확인");
                }
            }

            AddPublic($"{player.Name} 턴 종료: {player.RoomId}번 방");
        }

        private void AdvanceTurn()
        {
            TurnIndex++;

            if (TurnIndex >= players.Count)
            {
                TurnIndex = 0;

                if (Round >= 12)
                {
                    Phase = GamePhase.GameOver;
                    AddPublic("R12 종료");
                    return;
                }

                Round++;
            }

            BeginTurn();
        }

        private void AddPublic(string message)
        {
            publicLog.Add(message);
            if (publicLog.Count > 14) publicLog.RemoveAt(0);
            Debug.Log(message);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 460, Screen.height - 32), GUI.skin.box);
            GUILayout.Label("<b>Deduction Game - Production Core</b>", RichLabel());

            if (Human != null)
            {
                GUILayout.Label($"상태: {Phase}");
                GUILayout.Label($"라운드: {(Round == 0 ? "초기 배치" : $"R{Round}")}");
                GUILayout.Label($"턴: {(players.Count > TurnIndex ? players[TurnIndex].Name : "-")}");
                GUILayout.Label($"내 위치: {Human.RoomId}번 방");
                GUILayout.Label($"이동력: {Human.MovementPoints}P");
                GUILayout.Label($"역할: {(Human.IsCulprit ? "범인" : "수사관")}");
                GUILayout.Label($"내 카드: {Human.LocationCard} / {Human.WeaponCard}");

                if (Phase == GamePhase.SetupMove)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("방을 클릭해서 최대 3P 이동한 뒤 배치를 완료하세요.");
                    if (GUILayout.Button("초기 배치 완료", GUILayout.Height(34)))
                        CompleteHumanSetup();
                }
                else if (Phase == GamePhase.Round && TurnIndex == 0)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("<b>같은 방 플레이어에게 정보 행동</b>", RichLabel());

                    selectedWeaponIndex = Mathf.Clamp(selectedWeaponIndex, 0, CardRules.Weapons.Length - 1);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("◀", GUILayout.Width(40))) selectedWeaponIndex = (selectedWeaponIndex + CardRules.Weapons.Length - 1) % CardRules.Weapons.Length;
                    GUILayout.Label($"의심 무기: {CardRules.Weapons[selectedWeaponIndex]}", GUILayout.Width(230));
                    if (GUILayout.Button("▶", GUILayout.Width(40))) selectedWeaponIndex = (selectedWeaponIndex + 1) % CardRules.Weapons.Length;
                    GUILayout.EndHorizontal();

                    for (int i = 1; i < players.Count; i++)
                    {
                        var target = players[i];
                        if (!target.Active || target.RoomId != Human.RoomId) continue;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(target.Name, GUILayout.Width(90));
                        if (GUILayout.Button("의심", GUILayout.Width(90)))
                            HumanSuspicion(target.Id, CardRules.Weapons[selectedWeaponIndex]);
                        if (GUILayout.Button("협조", GUILayout.Width(90)))
                            HumanCooperation(target.Id);
                        GUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("턴 종료", GUILayout.Height(34)))
                        EndHumanTurn();
                }
                else if (Phase == GamePhase.GameOver)
                {
                    if (GUILayout.Button("새 게임", GUILayout.Height(34)))
                        StartNewGame();
                }

                GUILayout.Space(10);
                GUILayout.Label("<b>내 비공개 정보</b>", RichLabel());
                int start = Mathf.Max(0, Human.PrivateNotes.Count - 7);
                for (int i = start; i < Human.PrivateNotes.Count; i++)
                    GUILayout.Label(Human.PrivateNotes[i]);
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>공개 로그</b>", RichLabel());
            for (int i = 0; i < publicLog.Count; i++)
                GUILayout.Label(publicLog[i]);

            GUILayout.EndArea();
        }

        private static GUIStyle RichLabel()
        {
            return new GUIStyle(GUI.skin.label) { richText = true };
        }
    }
}
