using UnityEngine;

namespace Case464.Hotel
{
    [DisallowMultipleComponent]
    public sealed class HotelOverviewRoomArea : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayNumber;
        [SerializeField] private int floor;
        [SerializeField] private Bounds selectionBounds;
        [SerializeField] private Vector3 physicalPosition;
        [SerializeField] private Vector3 displayPosition;
        [SerializeField] private Light[] authoredDirectionalLights;

        public string RoomId => roomId;
        public string DisplayNumber => displayNumber;
        public int Floor => floor;
        public Bounds SelectionBounds => selectionBounds;
        public Vector3 PhysicalPosition => physicalPosition;
        public Vector3 DisplayPosition => displayPosition;
        public Light[] AuthoredDirectionalLights => authoredDirectionalLights;

        public void Configure(string id, string number, int level, Bounds area, Vector3 physical, Vector3 display,
            Light[] directionalLights)
        {
            roomId = id;
            displayNumber = number;
            floor = level;
            selectionBounds = area;
            physicalPosition = physical;
            displayPosition = display;
            authoredDirectionalLights = directionalLights;
        }

        public bool Contains(Vector3 worldPosition) =>
            selectionBounds.Contains(transform.InverseTransformPoint(worldPosition));

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(.88f, .69f, .36f, .9f);
            Gizmos.DrawWireCube(selectionBounds.center, selectionBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
