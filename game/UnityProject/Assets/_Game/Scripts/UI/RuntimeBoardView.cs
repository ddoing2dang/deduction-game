using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeductionGame
{
    public sealed class RuntimeBoardView : MonoBehaviour
    {
        private static RuntimeBoardView instance;
        private GameManager manager;
        private readonly Dictionary<int, GameObject> tokens = new();
        private readonly Dictionary<int, int> tokenRooms = new();
        private readonly Dictionary<int, int> tokenSlots = new();
        private readonly Dictionary<int, float> pivotOffsets = new();
        private readonly Dictionary<int, int> displayedRooms = new();

        public static void Ensure(GameManager gm)
        {
            RuntimeNavMesh.EnsureBuilt();
            if (instance == null)
            {
                var go = new GameObject("RuntimeBoardView");
                instance = go.AddComponent<RuntimeBoardView>();
            }

            instance.manager = gm;
            instance.BuildTokens();
            instance.Refresh();
        }

        public static void ResetMatch() {
            if (instance == null) return;
            instance.tokenRooms.Clear(); instance.tokenSlots.Clear(); instance.displayedRooms.Clear();
            foreach (var pair in instance.tokens) {
                var agent = pair.Value.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh) {agent.ResetPath(); agent.Warp(RoomPresentation.Stand(1,pair.Key));}
            }
        }

        public static void RefreshAll()
        {
            if (instance != null)
                instance.Refresh();
        }

        private void BuildTokens()
        {
            if (manager == null) return;

            foreach (var p in manager.Players)
            {
                if (tokens.ContainsKey(p.Id)) continue;

                var catalog = PresentationCatalog.Load();
                var prefab = catalog != null ? catalog.Character(p.Id) : null;
                var token = new GameObject($"Token_{p.Id}_{p.Name}");
                token.transform.SetParent(transform);
                token.transform.position = RoomPresentation.Stand(p.RoomId, p.Id % 6);
                var agent = token.AddComponent<NavMeshAgent>();
                agent.radius = 0.3f;
                agent.height = 1.1f;
                agent.baseOffset = 0f;
                agent.speed = 4.2f;
                agent.acceleration = 14f;
                agent.angularSpeed = 720f;
                agent.stoppingDistance = 0.05f;

                var actor = token.AddComponent<CharacterActor>();
                actor.Initialize(p.Id, prefab);
                var label = token.AddComponent<TokenLabel>();
                label.PlayerId = p.Id;
                label.DisplayName = p.Name;

                tokens[p.Id] = token;
                pivotOffsets[p.Id] = 0f;
            }
        }

        private void Refresh()
        {
            if (manager == null) return;
            BuildTokens();

            // Release only departed seats; other occupants keep their current stand point.
            foreach (var p in manager.Players)
                if (tokenRooms.TryGetValue(p.Id, out int oldRoom) && oldRoom != p.RoomId)
                {
                    tokenRooms.Remove(p.Id);
                    tokenSlots.Remove(p.Id);
                }

            foreach (var p in manager.Players)
            {
                if (!tokens.TryGetValue(p.Id, out var token)) continue;

                if (!tokenSlots.TryGetValue(p.Id, out int slot))
                {
                    float bestDistance = float.PositiveInfinity;
                    slot = 0;
                    for (int candidate = 0; candidate < 6; candidate++)
                    {
                        bool occupied = false;
                        foreach (var seat in tokenRooms)
                            if (seat.Value == p.RoomId && tokenSlots[seat.Key] == candidate) occupied = true;
                        if (occupied) continue;
                        float distance = (token.transform.position - RoomPresentation.Stand(p.RoomId, candidate)).sqrMagnitude;
                        if (distance >= bestDistance) continue;
                        bestDistance = distance;
                        slot = candidate;
                    }
                    tokenRooms[p.Id] = p.RoomId;
                    tokenSlots[p.Id] = slot;
                }
                Vector3 target = RoomPresentation.Stand(p.RoomId, slot);
                token.GetComponent<CharacterActor>().AlignAtStand(Quaternion.identity);
                var agent = token.GetComponent<NavMeshAgent>();
                if (agent != null && NavMesh.SamplePosition(target, out var hit, 1f, NavMesh.AllAreas))
                {
                    if (!agent.isOnNavMesh) agent.Warp(hit.position);
                    // Secret passage is a transition, not a walk through intervening rooms.
                    if (displayedRooms.TryGetValue(p.Id, out int fromRoom) && manager.SecretPassageActive &&
                        MapData.IsSecretPair(fromRoom, p.RoomId))
                        agent.Warp(hit.position);
                    else if (agent.isOnNavMesh) agent.SetDestination(hit.position);
                }
                else token.transform.position = target + Vector3.up * pivotOffsets[p.Id];
                displayedRooms[p.Id] = p.RoomId;
                token.SetActive(true);
            }
        }
    }

    public sealed class TokenLabel : MonoBehaviour
    {
        public int PlayerId;
        public string DisplayName;

        private void OnGUI()
        {
            if (!gameObject.activeInHierarchy) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.3f);
            if (screen.z <= 0) return;

            GUI.Label(
                new Rect(screen.x - 45, Screen.height - screen.y - 12, 90, 22),
                DisplayName
            );
        }
    }
}
