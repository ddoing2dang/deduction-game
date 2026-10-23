using UnityEngine;

namespace DeductionGame
{
    public sealed class MansionCutawayView : MonoBehaviour
    {
        private Transform mansionRoot;
        private int lastRoomId = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
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
                GameObject root = GameObject.Find("Mansion_Graybox");
                if (root == null) return;
                mansionRoot = root.transform;
                lastRoomId = -1;
            }

            int roomId = gm.Human.RoomId;
            if (roomId != lastRoomId)
            {
                ApplyCutaway(roomId);
                lastRoomId = roomId;
            }
        }

        private void ApplyCutaway(int activeRoomId)
        {
            var activeRoom = MansionLayout.GetRoom(activeRoomId);

            foreach (var layout in MansionLayout.Rooms)
            {
                Transform roomRoot = FindRoomRoot(layout.Id);
                if (roomRoot == null) continue;

                SetAllWallsVisible(roomRoot, true);
                SetFloorVisible(roomRoot, true);
            }

            Transform activeRoot = FindRoomRoot(activeRoomId);
            if (activeRoot != null)
                SetForegroundWallVisible(activeRoot, false);

            if (!activeRoom.IsBasement) return;

            foreach (var layout in MansionLayout.Rooms)
            {
                if (layout.IsBasement) continue;
                if (!FootprintsOverlap(activeRoom, layout)) continue;

                Transform coveringRoom = FindRoomRoot(layout.Id);
                if (coveringRoom != null)
                    SetFloorVisible(coveringRoom, false);
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
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = visible;
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
            return dx < (a.Size.x + b.Size.x) * 0.5f &&
                   dz < (a.Size.y + b.Size.y) * 0.5f;
        }
    }
}
