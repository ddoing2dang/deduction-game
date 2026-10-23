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
                new Vector3(0f, 4f, 0f),
                new Vector3(120f, 40f, 120f));

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
            if (built) {
                var links = MapData.Links;
                for(int i=0;i<links.GetLength(0);i++) {
                    int a=links[i,0], b=links[i,1];
                    var root=GameObject.Find(MapData.RootName);
                    var from=root.transform.Find($"Room_{a:00}_{MapData.GetRoom(a).Name}/NavigationPoints/Entry_To_{b:00}");
                    var to=root.transform.Find($"Room_{b:00}_{MapData.GetRoom(b).Name}/NavigationPoints/Entry_To_{a:00}");
                    if(from!=null && to!=null && NavMesh.SamplePosition(MapData.GetRoom(a).Center,out var ah,1f,NavMesh.AllAreas) &&
                        NavMesh.SamplePosition(MapData.GetRoom(b).Center,out var bh,1f,NavMesh.AllAreas)) {
                        var path=new NavMeshPath();
                        if (MapData.Theme != MapTheme.Mansion || !NavMesh.CalculatePath(ah.position,bh.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)
                            NavMesh.AddLink(new NavMeshLinkData {startPosition=ah.position,endPosition=bh.position,width=0.6f,bidirectional=true,agentTypeID=settings.agentTypeID,costModifier=-1});
                    }
                }
            }

            if (built)
                Debug.Log($"[DeductionGame] Runtime NavMesh ready · sources {sources.Count}");

            return built;
        }
    }
}
