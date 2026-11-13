using System.Linq;
using UnityEngine;

namespace Case464.Hotel
{
    // Owns ONLY newly added seasonal renderers/lights. Existing camera groups stay intact.
    [ExecuteAlways]
    public sealed class HotelYearEndVisibility : MonoBehaviour
    {
        [SerializeField] private HotelMapCameraController cameraController;
        [SerializeField] private string room;
        [SerializeField] private int floor;
        [SerializeField] private bool entrance;
        [SerializeField] private Renderer[] additions;
        [SerializeField] private Light[] additionalLights;
        public void Configure(HotelMapCameraController camera, string roomId, int level, bool publicEntry)
        {
            cameraController = camera; room = roomId; floor = level; entrance = publicEntry;
            additions = GetComponentsInChildren<Renderer>(true);
            additionalLights = GetComponentsInChildren<Light>(true);
            Apply();
        }
        private void OnEnable() => Apply();
        private void Update() => Apply();
        public void Apply()
        {
            if (cameraController == null || additions == null) return;
            var mode = cameraController.Mode;
            bool visible = mode == HotelMapView.PhysicalBuilding || mode == HotelMapView.SplitFloors;
            if (entrance) visible |= mode == HotelMapView.SecondFloor;
            else
            {
                visible |= mode == HotelMapView.FirstFloor && floor == 1 ||
                           mode == HotelMapView.SecondFloor && floor == 2 ||
                           (mode == HotelMapView.Room || mode == HotelMapView.Object) &&
                               cameraController.SelectedRoom == room;
                if (mode == HotelMapView.Region && cameraController.Regions != null)
                    visible |= cameraController.Regions.Any(r => r.key == cameraController.SelectedRegion &&
                        r.groupKeys != null && r.groupKeys.Contains(room));
            }
            foreach (var renderer in additions) if (renderer != null) renderer.forceRenderingOff = !visible;
            foreach (var light in additionalLights) if (light != null) light.enabled = visible;
        }
    }
}
