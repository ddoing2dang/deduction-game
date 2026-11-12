using System;
using UnityEngine;

namespace Case464.Hotel
{
    [Serializable]
    public sealed class HotelMergedRoomRecord
    {
        public HotelOverviewRoomArea room;
        public Vector3 expectedPosition;
        public Quaternion expectedRotation;
        public Vector3 preservedScale;
        public string interiorDigest;
        public int interiorLights;
    }

    [Serializable]
    public sealed class HotelMergeDependency
    {
        public string path;
        public string sha256;
    }

    [DisallowMultipleComponent]
    public sealed class HotelApprovedLayoutMerge : MonoBehaviour
    {
        public string architectureSource;
        public string interiorSource;
        public string architectureSha256;
        public string interiorSha256;
        public Bounds annexBounds;
        public HotelMergedRoomRecord[] rooms;
        public HotelMergeDependency[] protectedSources;
        public string limitation = "Red-rectangle reference image was not provided; unidentifiable structures were not removed.";
    }
}
