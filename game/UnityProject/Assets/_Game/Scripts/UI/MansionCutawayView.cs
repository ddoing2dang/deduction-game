using UnityEngine;

namespace DeductionGame
{
    public sealed class MansionCutawayView : MonoBehaviour
    {
        private Transform mansionRoot;
        private int lastRoomId = -1;
        private Vector3 lastCutawayCameraPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Attach()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MansionCutawayView>() == null)
                cam.gameObject.AddComponent<MansionCutawayView>();
        }

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Human == null) return;

            if (mansionRoot == null)
            {
                GameObject root = GameObject.Find(MapData.RootName);
                if (root == null) return;
                mansionRoot = root.transform;
                lastRoomId = -1;
            }

            int roomId = gm.Human.RoomId;
            if (roomId != lastRoomId || (MapData.Theme == MapTheme.Mansion &&
                (transform.position - lastCutawayCameraPosition).sqrMagnitude > .0025f))
            {
                ApplyCutaway(roomId);
                lastRoomId = roomId;
                lastCutawayCameraPosition = transform.position;
            }
        }

        private void ApplyCutaway(int activeRoomId)
        {
            var activeRoom = MapData.GetRoom(activeRoomId);

            foreach (var layout in MapData.Rooms)
            {
                Transform roomRoot = FindRoomRoot(layout.Id);
                if (roomRoot == null) continue;

                SetAllWallsVisible(roomRoot, true);
                SetFloorVisible(roomRoot, true);
            }

            Transform activeRoot = FindRoomRoot(activeRoomId);
            if (activeRoot != null)
                SetForegroundWallVisible(activeRoot, false);

            

            foreach (var layout in MapData.Rooms)
            {
                if (layout.Center.y <= activeRoom.Center.y) continue;
                if (!FootprintsOverlap(activeRoom, layout)) continue;

                Transform coveringRoom = FindRoomRoot(layout.Id);
                if (coveringRoom != null)
                {
                    SetFloorVisible(coveringRoom, false);
                    SetForegroundWallVisible(coveringRoom, false);
                }
            }
            if (MapData.Theme == MapTheme.Mansion)
                HideWallsOccludingRoom(activeRoom);
        }

        private void HideWallsOccludingRoom(MansionRoomLayout room)
        {
            Camera camera = GetComponent<Camera>();
            if (camera == null) return;
            // Orthographic rays match the fixed play camera. Other rooms' tall walls
            // can obscure the active room even when their floor footprints do not overlap.
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                Vector3 point = room.Center + new Vector3(x * room.Size.x * .35f, .1f, z * room.Size.y * .35f);
                Ray ray = camera.ScreenPointToRay(camera.WorldToScreenPoint(point));
                float distance = Vector3.Dot(point - ray.origin, ray.direction);
                foreach (RaycastHit hit in Physics.RaycastAll(ray, distance - .05f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Transform wall = hit.collider.transform;
                    if (wall.parent == null || wall.parent.name != "Walls") continue;
                    Renderer renderer = wall.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }
        }

        private Transform FindRoomRoot(int roomId)
        {
            if (mansionRoot == null) return null;

            string prefix = $"Room_{roomId:00}_";
            for (int i = 0; i < mansionRoot.childCount; i++)
            {
                Transform child = mansionRoot.GetChild(i);
                if (child.name.StartsWith(prefix))
                    return child;
            }

            return null;
        }

        private static void SetAllWallsVisible(Transform roomRoot, bool visible)
        {
            Transform walls = roomRoot.Find("Walls");
            if (walls == null) return;

            Renderer[] renderers = walls.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) {
                renderers[i].enabled = visible;
            }
        }

        private static void SetForegroundWallVisible(Transform roomRoot, bool visible)
        {
            Transform walls = roomRoot.Find("Walls");
            if (walls == null) return;

            for (int i = 0; i < walls.childCount; i++)
            {
                Transform wall = walls.GetChild(i);
                if (!wall.name.StartsWith("South_")) continue;

                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }

        private static void SetFloorVisible(Transform roomRoot, bool visible)
        {
            Transform floor = roomRoot.Find("Floor");
            if (floor == null) return;

            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = visible;
        }

        private static bool FootprintsOverlap(MansionRoomLayout a, MansionRoomLayout b)
        {
            float dx = Mathf.Abs(a.Center.x - b.Center.x);
            float dz = Mathf.Abs(a.Center.z - b.Center.z);
            float overlapX=(a.Size.x+b.Size.x)*.5f-dx;
            float overlapZ=(a.Size.y+b.Size.y)*.5f-dz;
            // A half-metre perimeter overlap must not erase the entire Hall floor
            // while viewing the cellar under the kitchen. Keep adjacent-room context.
            float minimumOverlap=MapData.Theme==MapTheme.Mansion?.75f:0f;
            return overlapX>minimumOverlap && overlapZ>minimumOverlap;
        }
    }
}
