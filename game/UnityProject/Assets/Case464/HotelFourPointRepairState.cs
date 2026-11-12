using UnityEngine;

namespace Case464.Hotel
{
    [DisallowMultipleComponent]
    public sealed class HotelFourPointRepairState : MonoBehaviour
    {
        public string sourceScene;
        public string sourceSha256;
        public HotelMergedRoomRecord[] rooms;
        public HotelMergeDependency[] protectedAssets;
        public GameObject centralWall;
        public GameObject[] excludedLeftArchParts;
        public MeshRenderer leftClosure;
        public MeshRenderer[] stairCheeks;
        public MeshRenderer originalStairCore;
        public MeshRenderer originalLanding;
        public Transform normalWestArch;
        public Transform daybed;
        public Transform room201;
        public Transform entrance201;
        public Transform outside201;
        public Vector3 daybedBeforeLocal;
        public Vector3 daybedAfterLocal;
        public Vector3 daybedBeforeWorld;
        public Vector3 daybedAfterWorld;
        public float openingBufferBefore;
        public float openingBufferAfter;
        public string[] removedGeneratedPaths;
    }
}
