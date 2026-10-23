using UnityEngine;

namespace DeductionGame
{
    // Optional anchors inside a replacement room prefab. All coordinates are Unity meters.
    public sealed class RoomPresentation : MonoBehaviour
    {
        public Transform[] StandPoints = new Transform[6];
        public Transform ActionPoint;

        public static Vector3 Stand(int roomId, int slot)
        {
            foreach (var room in FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None))
            {
                if (room.RoomId != roomId) continue;
                var anchors = room.GetComponentInChildren<RoomPresentation>();
                if (anchors != null && anchors.StandPoints != null && slot >= 0 &&
                    slot < anchors.StandPoints.Length && anchors.StandPoints[slot] != null)
                    return anchors.StandPoints[slot].position;
                var point = room.transform.parent != null
                    ? room.transform.parent.Find($"NavigationPoints/Stand_{slot + 1:00}") : null;
                if (point != null) return point.position - Vector3.up * 0.55f;
                break;
            }
            return MapData.GetStandPoint(roomId, slot) - Vector3.up * 0.55f;
        }

        public static void Install(GameObject room, int roomId, PresentationCatalog catalog)
        {
            var prefab = catalog != null ? catalog.Room(roomId) : null;
            if (prefab == null) return;
            var visual = Instantiate(prefab, room.transform);
            // Floor object is scaled for selection. Imported art must retain meter scale.
            visual.transform.localScale = new Vector3(
                1f / room.transform.lossyScale.x,
                1f / room.transform.lossyScale.y,
                1f / room.transform.lossyScale.z);
            visual.transform.position = MapData.GetRoom(roomId).Center;
            var floor = room.GetComponent<Renderer>();
            if (floor != null) floor.enabled = false;
        }
    }
}
