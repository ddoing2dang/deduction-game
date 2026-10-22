using System.Collections.Generic;
using UnityEngine;

namespace DeductionGame
{
    public sealed class RuntimeBoardView : MonoBehaviour
    {
        private static RuntimeBoardView instance;
        private GameManager manager;
        private readonly Dictionary<int, GameObject> tokens = new();

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

                var token = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                token.name = $"Token_{p.Id}_{p.Name}";
                token.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);

                var label = token.AddComponent<TokenLabel>();
                label.PlayerId = p.Id;
                label.DisplayName = p.Name;

                tokens[p.Id] = token;
            }
        }

        private void Refresh()
        {
            if (manager == null) return;
            BuildTokens();

            var roomCounts = new Dictionary<int, int>();

            foreach (var p in manager.Players)
            {
                if (!tokens.TryGetValue(p.Id, out var token)) continue;

                if (!roomCounts.ContainsKey(p.RoomId))
                    roomCounts[p.RoomId] = 0;

                int slot = roomCounts[p.RoomId]++;
                token.transform.position = MansionLayout.GetStandPoint(p.RoomId, slot);
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
