using System.Collections.Generic;
using UnityEngine;

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

        public static void Ensure(GameManager gm)
        {
            if (instance == null)
            {
                var go = new GameObject("RuntimeBoardView");
                instance = go.AddComponent<RuntimeBoardView>();
            }

            instance.manager = gm;
            instance.BuildTokens();
            instance.Refresh();
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
                var token = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
                token.transform.SetParent(transform);
                token.name = $"Token_{p.Id}_{p.Name}";
                if (prefab == null)
                    token.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);

                var label = token.AddComponent<TokenLabel>();
                label.PlayerId = p.Id;
                label.DisplayName = p.Name;

                tokens[p.Id] = token;
                pivotOffsets[p.Id] = prefab == null ? 0.55f : 0f;
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
                token.transform.position = RoomPresentation.Stand(p.RoomId, slot) + Vector3.up * pivotOffsets[p.Id];
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
