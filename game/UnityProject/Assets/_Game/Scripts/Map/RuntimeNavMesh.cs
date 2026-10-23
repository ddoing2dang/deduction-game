using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeductionGame
{
    public static class RuntimeNavMesh
    {
        private static NavMeshData navMeshData;
        private static NavMeshDataInstance navMeshInstance;
        private static bool built;

        public static bool EnsureBuilt()
        {
            if (built && navMeshInstance.valid)
                return true;

            if (NavMesh.GetSettingsCount() <= 0)
            {
                Debug.LogWarning("[DeductionGame] NavMesh agent settings not found.");
                return false;
            }

            var bounds = new Bounds(
                new Vector3(0f, -2f, 2f),
                new Vector3(80f, 20f, 80f));

            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();

            NavMeshBuilder.CollectSources(
                bounds,
                ~0,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                markups,
                sources);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            navMeshData = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity);

            if (navMeshData == null)
            {
                Debug.LogWarning("[DeductionGame] Runtime NavMesh build returned no data.");
                return false;
            }

            navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
            built = navMeshInstance.valid;

            if (built)
                Debug.Log($"[DeductionGame] Runtime NavMesh ready · sources {sources.Count}");

            return built;
        }
    }
}
