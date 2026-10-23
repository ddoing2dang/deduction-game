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
                token.transform.position = MansionLayout.GetStandPoint(p.RoomId, p.Id % 6);

                var collider = token.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;

                var agent = token.AddComponent<NavMeshAgent>();
                agent.radius = 0.3f;
                agent.height = 1.1f;
                agent.baseOffset = 0.55f;
                agent.speed = 4.2f;
                agent.acceleration = 14f;
                agent.angularSpeed = 720f;
                agent.stoppingDistance = 0.05f;
                agent.autoBraking = true;
                agent.autoRepath = true;
                agent.avoidancePriority = Mathf.Clamp(30 + p.Id * 5, 0, 99);

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
                Vector3 target = MansionLayout.GetStandPoint(p.RoomId, slot);

                var agent = token.GetComponent<NavMeshAgent>();
                if (agent != null &&
                    NavMesh.SamplePosition(target, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(hit.position);
                    }
                    else
                    {
                        agent.Warp(hit.position);
                    }
                }
                else
                {
                    token.transform.position = target;
                }

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
