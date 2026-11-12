using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Case464.Hotel
{
    public enum HotelMapView { SplitFloors, FirstFloor, SecondFloor, PhysicalBuilding, Room, Region, Object }

    [Serializable]
    public sealed class HotelVisualGroup
    {
        public string key;
        public string roomId;
        public int floor;
        public bool viewOnly;
        public Renderer[] renderers;
        public Renderer[] nearWalls;
        public Renderer[] sharedWalls;
        public Light[] lights;
        public Renderer[] displayRenderers;
        public Renderer[] displayNearWalls;
        public Light[] displayLights;
        public Bounds displayBounds;
    }

    [Serializable]
    public sealed class HotelMapFocusRegion
    {
        public string key;
        public Bounds bounds;
        public string[] groupKeys;
        public Vector3 angles = new Vector3(60, 225, 0);
    }

    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HotelMapCameraController : MonoBehaviour
    {
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private HotelVisualGroup[] groups;
        [SerializeField] private HotelMapFocusRegion[] regions;
        [SerializeField] private bool[] rendererBaseline;
        [SerializeField] private bool[] lightBaseline;
        [SerializeField] private HotelMapView mode;
        [SerializeField] private string selectedRoom = "LOBBY";
        [SerializeField] private string selectedRegion = "Lobby";
        [SerializeField] private bool allWalls;
        [SerializeField] private bool animate = true;
        [SerializeField] private float transitionSeconds = .18f;
        [SerializeField] private Vector3 presentationAngles = new Vector3(60, 225, 0);
        private Renderer[] inspected;
        private bool perspectiveObject;
        private HotelMapView returnMode;
        private string returnRoom;
        private string returnRegion;
        private Vector3 startPosition, targetPosition;
        private Quaternion startRotation, targetRotation;
        private float startSize, targetSize, transitionStart;
        private bool transitioning;

        public HotelMapView Mode => mode;
        public string SelectedRoom => selectedRoom;
        public string SelectedRegion => selectedRegion;
        public bool AllWalls => allWalls;
        public Camera ReviewCamera => reviewCamera;
        public HotelVisualGroup[] Groups => groups;
        public HotelMapFocusRegion[] Regions => regions;
        public Vector3 PresentationAngles => presentationAngles;

        public void SetPresentationAngles(Vector3 angles)
        {
            if (float.IsNaN(angles.x) || float.IsNaN(angles.y) || float.IsNaN(angles.z) ||
                float.IsInfinity(angles.x) || float.IsInfinity(angles.y) || float.IsInfinity(angles.z) || angles.x < 1 || angles.x > 90)
                throw new ArgumentException("Invalid hotel review camera angles.");
            presentationAngles = angles;
        }

        public void Configure(Camera camera, HotelVisualGroup[] visualGroups, HotelMapFocusRegion[] focusRegions)
        {
            reviewCamera = camera;
            groups = visualGroups;
            regions = focusRegions;
            rendererBaseline = groups.SelectMany(group => group.renderers).Select(item => item.forceRenderingOff).ToArray();
            lightBaseline = groups.SelectMany(group => group.lights).Select(item => item.enabled).ToArray();
        }

        public void Show(HotelMapView requested)
        {
            if (requested == HotelMapView.Object)
                throw new ArgumentException("Choose a rendered object before opening object inspection.");
            mode = requested;
            inspected = null;
            Apply();
        }

        public void FocusRoom(string roomId)
        {
            if (!groups.Any(group => group.key == roomId && !group.viewOnly))
                throw new ArgumentException("The requested room is not in this hotel.");
            selectedRoom = roomId;
            mode = HotelMapView.Room;
            inspected = null;
            Apply();
        }

        public void FocusRegion(string key)
        {
            if (!regions.Any(region => region.key == key))
                throw new ArgumentException("The requested H00 focus region does not exist.");
            selectedRegion = key;
            mode = HotelMapView.Region;
            inspected = null;
            Apply();
        }

        public void Inspect(Renderer[] selection, bool perspective)
        {
            var candidates = selection.Where(item => item != null && groups.Any(group => group.renderers.Contains(item))).Distinct().ToArray();
            if (candidates.Length == 0)
                throw new ArgumentException("Select a rendered physical hotel object in the Hierarchy.");
            HotelVisualGroup owner = groups.First(group => group.renderers.Contains(candidates[0]));
            if (candidates.Any(item => !owner.renderers.Contains(item)))
                throw new ArgumentException("Inspect objects from one room or public area at a time.");
            if (mode != HotelMapView.Object)
            {
                returnMode = mode;
                returnRoom = selectedRoom;
                returnRegion = selectedRegion;
            }
            selectedRoom = owner.key;
            inspected = candidates;
            perspectiveObject = perspective;
            mode = HotelMapView.Object;
            Apply();
        }

        public void ReturnFromInspection()
        {
            if (mode != HotelMapView.Object)
                return;
            mode = returnMode;
            selectedRoom = returnRoom;
            selectedRegion = returnRegion;
            inspected = null;
            Apply();
        }

        public void SetWalls(bool visible)
        {
            allWalls = visible;
            Apply();
        }

        public string RoomAt(Ray ray)
        {
            string result = null;
            float nearest = float.PositiveInfinity;
            foreach (var group in groups.Where(group => !string.IsNullOrEmpty(group.roomId) && !group.viewOnly))
            {
                if (!Visible(group))
                    continue;
                if (mode == HotelMapView.FirstFloor && group.floor != 1 ||
                    mode == HotelMapView.SecondFloor && group.floor != 2)
                    continue;
                Renderer[] renderers = mode == HotelMapView.SplitFloors ? group.displayRenderers : group.renderers;
                if (renderers.Length == 0)
                    continue;
                Bounds bounds = BoundsFor(renderers);
                if (bounds.IntersectRay(ray, out float distance) && distance < nearest)
                {
                    result = group.roomId;
                    nearest = distance;
                }
            }
            return result;
        }

        private bool Visible(HotelVisualGroup group)
        {
            if (group.viewOnly)
                return mode == HotelMapView.SplitFloors || mode == HotelMapView.SecondFloor;
            switch (mode)
            {
                case HotelMapView.SplitFloors:
                case HotelMapView.PhysicalBuilding: return true;
                case HotelMapView.FirstFloor: return group.floor == 1;
                case HotelMapView.SecondFloor: return group.floor == 2;
                case HotelMapView.Room: return group.key == selectedRoom;
                case HotelMapView.Region: return regions.Single(region => region.key == selectedRegion).groupKeys.Contains(group.key);
                case HotelMapView.Object: return group.key == selectedRoom;
                default: return false;
            }
        }

        private void Apply()
        {
            if (reviewCamera == null || groups == null)
                throw new InvalidOperationException("The hotel camera configuration is incomplete.");
            bool split = mode == HotelMapView.SplitFloors;
            int rendererIndex = 0, lightIndex = 0;
            bool directionalUsed = false;
            var frame = new List<Renderer>();
            foreach (var group in groups)
            {
                bool visible = Visible(group);
                for (int i = 0; i < group.renderers.Length; i++)
                {
                    Renderer physical = group.renderers[i];
                    bool baseline = rendererBaseline[rendererIndex++];
                    bool cut = !allWalls && group.nearWalls.Contains(physical);
                    bool shared = group.sharedWalls != null && group.sharedWalls.Contains(physical) &&
                        mode != HotelMapView.Room && mode != HotelMapView.Object;
                    cut |= shared;
                    physical.forceRenderingOff = baseline || split || !visible || cut;
                    Renderer display = group.displayRenderers[i];
                    display.forceRenderingOff = baseline || !split || !visible || cut;
                    if (!baseline && visible && !cut)
                        frame.Add(split ? display : physical);
                }
                for (int i = 0; i < group.lights.Length; i++)
                {
                    bool baseline = lightBaseline[lightIndex++];
                    Light physical = group.lights[i], display = group.displayLights[i];
                    bool enabled = baseline && visible;
                    if (enabled && physical.type == LightType.Directional)
                    {
                        enabled = !directionalUsed;
                        directionalUsed = true;
                    }
                    physical.enabled = enabled && !split;
                    display.enabled = enabled && split;
                }
            }
            if (frame.Count == 0)
                throw new InvalidOperationException("No visible geometry for this hotel view.");
            Quaternion rotation = Quaternion.Euler(presentationAngles);
            Bounds? explicitBounds = null;
            if (mode == HotelMapView.Region)
            {
                var region = regions.Single(item => item.key == selectedRegion);
                rotation = Quaternion.Euler(region.angles);
                explicitBounds = region.bounds;
            }
            if (mode == HotelMapView.Object && inspected != null)
                frame = inspected.ToList();
            Frame(frame.ToArray(), rotation, explicitBounds, mode == HotelMapView.Object && perspectiveObject);
        }

        private void Frame(Renderer[] renderers, Quaternion rotation, Bounds? explicitBounds, bool perspective)
        {
            Quaternion inverse = Quaternion.Inverse(rotation);
            Bounds projected = new Bounds(inverse * renderers[0].bounds.center, Vector3.zero);
            void Include(Vector3 point) => projected.Encapsulate(inverse * point);
            if (explicitBounds.HasValue)
            {
                Bounds b = explicitBounds.Value;
                projected = new Bounds(inverse * b.center, Vector3.zero);
                for (int i = 0; i < 8; i++)
                    Include(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z));
            }
            else
                foreach (var renderer in renderers)
                {
                    Bounds b = renderer.localBounds;
                    for (int i = 0; i < 8; i++)
                        Include(renderer.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                            (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)));
                }
            float size = Mathf.Max(.12f, 1.045f * Mathf.Max(projected.extents.y, projected.extents.x / (16f / 9f)));
            float distance = perspective ? size / Mathf.Tan(21 * Mathf.Deg2Rad) + projected.extents.z + .12f :
                Mathf.Max(12, projected.extents.z + 3);
            reviewCamera.aspect = 16f / 9f;
            reviewCamera.orthographic = !perspective;
            reviewCamera.fieldOfView = 42;
            reviewCamera.nearClipPlane = .02f;
            reviewCamera.farClipPlane = distance + projected.extents.z + 25;
            startPosition = reviewCamera.transform.position;
            startRotation = reviewCamera.transform.rotation;
            startSize = reviewCamera.orthographicSize;
            targetPosition = rotation * (projected.center - Vector3.forward * distance);
            targetRotation = rotation;
            targetSize = size;
            transitionStart = Time.realtimeSinceStartup;
            transitioning = animate && isActiveAndEnabled && !Application.isPlaying;
            if (!transitioning)
                FinishTransition();
        }

        public void FinishTransition()
        {
            reviewCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            reviewCamera.orthographicSize = targetSize;
            transitioning = false;
        }

        private void Update()
        {
            if (!transitioning || reviewCamera == null)
                return;
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - transitionStart) / Mathf.Max(.01f, transitionSeconds));
            t = t * t * (3 - 2 * t);
            reviewCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            reviewCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            if (t >= 1)
                transitioning = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        private void OnEnable()
        {
            if (reviewCamera != null && groups != null && groups.Length > 0 && rendererBaseline != null && lightBaseline != null)
            {
                Apply();
                FinishTransition();
            }
        }

        private void OnDisable()
        {
            if (groups == null || rendererBaseline == null || lightBaseline == null)
                return;
            int r = 0, l = 0;
            foreach (var group in groups)
            {
                foreach (var renderer in group.renderers)
                {
                    if (renderer != null && r < rendererBaseline.Length)
                        renderer.forceRenderingOff = rendererBaseline[r];
                    r++;
                }
                foreach (var light in group.lights)
                {
                    if (light != null && l < lightBaseline.Length)
                        light.enabled = lightBaseline[l];
                    l++;
                }
                foreach (var display in group.displayRenderers)
                    if (display != null)
                        display.forceRenderingOff = true;
                foreach (var light in group.displayLights)
                    if (light != null)
                        light.enabled = false;
            }
        }

        private static Bounds BoundsFor(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
