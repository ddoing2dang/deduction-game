using System;
using UnityEngine;

namespace Case464.Hotel
{
    [Serializable]
    public sealed class HotelPolishRoomMetric
    {
        public string roomId;
        public string number;
        public int floor;
        public Transform root;
        public Transform entrance;
        public Transform outside;
        public Transform inside;
        public Bounds floorBounds;
    }

    [Serializable]
    public sealed class HotelPolishRoute
    {
        public string label;
        public int floor;
        public float width;
        public Vector3[] points;
    }

    [DisallowMultipleComponent]
    public sealed class HotelFinalPolishLayout : MonoBehaviour
    {
        public Bounds lobbyBounds;
        public Bounds annexBounds;
        public Bounds[] lobbySubareas;
        public bool ContainsLobbyPoint(Vector3 point)
        {
            if(lobbySubareas==null)return false;
            foreach(var area in lobbySubareas)if(area.Contains(point))return true;
            return false;
        }
        public float originalLandingHeight;
        public float upperFloorHeight;
        public float hallWidth;
        public Transform lowerLift;
        public Transform upperLift;
        public Transform upperStairEnd;
        public HotelPolishRoomMetric[] rooms;
        public HotelPolishRoute[] routes;
        public string coordinateNote = "Canonical plan: camera 90/180; right=-X, down=+Z. Not a claim about an unavailable user screenshot.";
    }
}
