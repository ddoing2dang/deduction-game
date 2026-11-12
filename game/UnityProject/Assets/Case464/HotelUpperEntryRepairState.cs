using System;
using UnityEngine;

namespace Case464.Hotel
{
    [Serializable] public sealed class HotelEntryMove
    {
        public string roomNumber,path;
        public Transform furniture;
        public Vector3 beforeLocal,afterLocal,beforeWorld,afterWorld,scale;
        public Quaternion rotation;
    }
    [Serializable] public sealed class HotelEntryRoomProof
    {
        public HotelOverviewRoomArea room;
        public Vector3 position,scale;
        public Quaternion rotation;
        public string digest,lights;
    }
    public sealed class HotelUpperEntryRepairState:MonoBehaviour
    {
        public string sourceScene,sourceHash;
        public HotelMergeDependency[] dependencies;
        public HotelEntryMove[] moves;
        public HotelEntryRoomProof[] rooms;
        public Vector3 arrivalBefore,arrivalAfter;
        public float corridorEdgeZ,distanceBefore,distanceAfter,lowerLandingHeight,upperFloorHeight;
        public Transform[] connectionTreads;
        public MeshRenderer[] newPlatform;
        public MeshRenderer foundation;
        public Bounds foundationBefore;
        public string[] vestibuleNotes;
    }
}
