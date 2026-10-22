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

        private static readonly string[] Names =
        {
            "나즈", "찌로", "마리히", "마또", "보키", "문이레"
        };

        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public int Round { get; private set; }
        public int TurnIndex { get; private set; }

        public PlayerState Human => players.Count > 0 ? players[0] : null;

        private readonly List<string> log = new();

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
            log.Clear();

            for (int i = 0; i < 6; i++)
                players.Add(new PlayerState(i, Names[i], i == 0, 1));

            Round = 0;
            TurnIndex = 0;
            Phase = GamePhase.SetupMove;

            Human.MovementPoints = 3;
            AddLog("초기 배치 시작: 전원 3P 이동 후 R1 시작");
        }

        public bool TryMoveHuman(int targetRoom)
        {
            if (Human == null || Phase == GamePhase.GameOver) return false;
            if (Phase == GamePhase.Round && TurnIndex != 0) return false;
            if (Human.MovementPoints <= 0) return false;
            if (!MapGraph.IsConnected(Human.RoomId, targetRoom)) return false;

            Human.RoomId = targetRoom;
            Human.MovementPoints -= 1;
            AddLog($"나즈 이동 → {targetRoom}번 방 / 남은 {Human.MovementPoints}P");
            return true;
        }

        public void CompleteHumanSetup()
        {
            if (Phase != GamePhase.SetupMove || Human.SetupComplete) return;

            Human.SetupComplete = true;
            AddLog($"나즈 초기 배치 완료: {Human.RoomId}번 방");

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
            AddLog($"{player.Name} 초기 배치 완료: {player.RoomId}번 방");
        }

        private void StartRoundOne()
        {
            Phase = GamePhase.Round;
            Round = 1;
            TurnIndex = 0;
            BeginTurn();
            AddLog("모든 플레이어 배치 완료 → R1 시작");
        }

        private void BeginTurn()
        {
            if (Phase != GamePhase.Round) return;

            var player = players[TurnIndex];
            player.MovementPoints = 2;
            AddLog($"R{Round} · {player.Name} 턴 시작 · 2P");
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

            AddLog($"{player.Name} 턴 종료: {player.RoomId}번 방");
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
                    AddLog("R12 종료");
                    return;
                }

                Round++;
            }

            BeginTurn();
        }

        private void AddLog(string message)
        {
            log.Add(message);
            if (log.Count > 12) log.RemoveAt(0);
            Debug.Log(message);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 390, 430), GUI.skin.box);
            GUILayout.Label("<b>Deduction Game - Production Core</b>", RichLabel());

            if (Human != null)
            {
                GUILayout.Label($"상태: {Phase}");
                GUILayout.Label($"라운드: {(Round == 0 ? "초기 배치" : $"R{Round}")}");
                GUILayout.Label($"턴: {(players.Count > TurnIndex ? players[TurnIndex].Name : "-")}");
                GUILayout.Label($"내 위치: {Human.RoomId}번 방");
                GUILayout.Label($"이동력: {Human.MovementPoints}P");

                if (Phase == GamePhase.SetupMove)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("방을 클릭해서 이동한 뒤 배치를 완료하세요.");
                    if (GUILayout.Button("초기 배치 완료", GUILayout.Height(36)))
                        CompleteHumanSetup();
                }
                else if (Phase == GamePhase.Round && TurnIndex == 0)
                {
                    GUILayout.Space(8);
                    if (GUILayout.Button("턴 종료", GUILayout.Height(36)))
                        EndHumanTurn();
                }
                else if (Phase == GamePhase.GameOver)
                {
                    if (GUILayout.Button("새 게임", GUILayout.Height(36)))
                        StartNewGame();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>로그</b>", RichLabel());
            for (int i = 0; i < log.Count; i++)
                GUILayout.Label(log[i]);

            GUILayout.EndArea();
        }

        private static GUIStyle RichLabel()
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true };
            return style;
        }
    }
}
