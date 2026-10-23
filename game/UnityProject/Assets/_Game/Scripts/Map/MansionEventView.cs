using System.Collections.Generic;
using UnityEngine;

namespace DeductionGame
{
    // Reads public state only. Markers have no colliders and never decide movement costs.
    public sealed class MansionEventView : MonoBehaviour
    {
        private sealed class DoorVisual
        {
            public string Edge;
            public GameObject Locked, Blocked, Broken;
        }

        private sealed class RoomVisual
        {
            public int Id;
            public Renderer Floor;
            public MaterialPropertyBlock Original;
            public GameObject Smoke;
            public Light[] Lights;
            public bool[] LightEnabled;
        }

        private readonly List<DoorVisual> doors = new();
        private readonly List<RoomVisual> rooms = new();
        private readonly List<Material> materials = new();
        private Transform secret;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Attach()
        {
            if (FindFirstObjectByType<MansionEventView>() == null)
                new GameObject("MansionEventView").AddComponent<MansionEventView>();
        }

        private void LateUpdate()
        {
            if (!initialized && !Build()) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            foreach (var door in doors)
            {
                door.Locked.SetActive(door.Edge == gm.ActiveLockedEdge);
                door.Blocked.SetActive(door.Edge == gm.ActiveRouteBlockedEdge);
                door.Broken.SetActive(door.Edge == gm.ActiveBrokenDoorEdge);
            }
            foreach (var room in rooms)
            {
                bool dark = room.Id == gm.ActiveBlackoutRoom;
                if (room.Floor != null)
                {
                    if (dark)
                    {
                        var block = new MaterialPropertyBlock();
                        room.Floor.GetPropertyBlock(block);
                        block.SetColor("_Color", new Color(0.12f, 0.14f, 0.2f));
                        block.SetColor("_BaseColor", new Color(0.12f, 0.14f, 0.2f));
                        room.Floor.SetPropertyBlock(block);
                    }
                    else room.Floor.SetPropertyBlock(room.Original);
                }
                for (int i = 0; i < room.Lights.Length; i++)
                    if (room.Lights[i] != null) room.Lights[i].enabled = !dark && room.LightEnabled[i];
                room.Smoke.SetActive(room.Id == gm.ActiveSmokeRoomA || room.Id == gm.ActiveSmokeRoomB);
            }
            if (secret != null) secret.gameObject.SetActive(gm.SecretPassageActive);
        }

        private bool Build()
        {
            var connections = GameObject.Find(MapData.ConnectionsName);
            var mansion = GameObject.Find(MapData.RootName);
            if (connections == null || mansion == null) return false;
            for (int i = 0; i < connections.transform.childCount; i++)
            {
                var door = connections.transform.GetChild(i);
                if (door.name == MapData.SecretName)
                {
                    secret = door;
                    foreach (Transform anchor in door)
                        Marker(anchor, "SecretEntrance", Vector3.up, new Vector3(1f, 2f, 0.15f), Color.cyan);
                    continue;
                }
                string[] parts = door.name.Split('_');
                if (parts.Length != 3 || !int.TryParse(parts[1], out int a) || !int.TryParse(parts[2], out int b)) continue;
                var route = door.Find("Passage") ?? door.Find("ServiceStair");
                if (route == null) continue;
                // Use an unscaled root so markers retain meter dimensions.
                var markers = new GameObject("EventMarkers").transform;
                markers.SetParent(door, false);
                markers.position = route.position;
                markers.rotation = route.rotation;
                doors.Add(new DoorVisual
                {
                    Edge = a < b ? $"{a}-{b}" : $"{b}-{a}",
                    Locked = Marker(markers, "Locked", new Vector3(0f, 1.7f, 0f), new Vector3(0.5f, 0.5f, 0.25f), Color.yellow),
                    Blocked = Marker(markers, "RouteBlocked", Vector3.up * 0.6f, new Vector3(1.8f, 1.2f, 0.3f), new Color(1f, 0.3f, 0.1f)),
                    Broken = Marker(markers, "BrokenDoor", new Vector3(0.6f, 1.5f, 0f), new Vector3(0.2f, 1.6f, 0.25f), Color.red)
                });
            }
            foreach (var node in mansion.GetComponentsInChildren<RoomNodeView>())
            {
                var floor = node.GetComponent<Renderer>();
                var original = new MaterialPropertyBlock();
                if (floor != null) floor.GetPropertyBlock(original);
                var lights = node.transform.parent.GetComponentsInChildren<Light>(true);
                var enabled = new bool[lights.Length];
                for (int i = 0; i < lights.Length; i++) enabled[i] = lights[i].enabled;
                var smokeRoot = new GameObject($"Room_{node.RoomId:00}_EventVisuals").transform;
                smokeRoot.SetParent(transform, false);
                smokeRoot.position = MapData.GetRoom(node.RoomId).Center;
                rooms.Add(new RoomVisual
                {
                    Id = node.RoomId, Floor = floor, Original = original, Lights = lights, LightEnabled = enabled,
                    Smoke = Marker(smokeRoot, "Smoke", Vector3.up * 0.65f, new Vector3(4f, 0.7f, 3f), new Color(0.65f, 0.7f, 0.8f, 0.3f))
                });
            }
            initialized = true;
            return true;
        }

        private GameObject Marker(Transform parent, string label, Vector3 position, Vector3 scale, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = label;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
            marker.transform.localScale = scale;
            var collider = marker.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            // Built-in lightweight graybox shader; replace the marker hierarchy for final art.
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            materials.Add(material);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            marker.SetActive(false);
            // Secret entrance visibility is controlled by the parent passage.
            if (label == "SecretEntrance") marker.SetActive(true);
            return marker;
        }

        private void OnDisable()
        {
            foreach (var door in doors)
            {
                if (door.Locked != null) door.Locked.SetActive(false); if (door.Blocked != null) door.Blocked.SetActive(false); if (door.Broken != null) door.Broken.SetActive(false);
            }
            foreach (var room in rooms)
            {
                if (room.Floor != null) room.Floor.SetPropertyBlock(room.Original);
                for (int i = 0; i < room.Lights.Length; i++)
                    if (room.Lights[i] != null) room.Lights[i].enabled = room.LightEnabled[i];
                if (room.Smoke != null) room.Smoke.SetActive(false);
            }
            if (secret != null) secret.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
