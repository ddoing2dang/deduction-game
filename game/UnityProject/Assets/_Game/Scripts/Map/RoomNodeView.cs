using UnityEngine;

namespace DeductionGame
{
    public sealed class RoomNodeView : MonoBehaviour
    {
        [SerializeField] private int roomId;
        [SerializeField] private string roomName;

        public int RoomId => roomId;
        public string RoomName => roomName;

        public void Configure(int id, string displayName)
        {
            roomId = id;
            roomName = displayName;

            // New production graybox keeps a named room root and a stable Floor child.
            // Preserve the Floor name so camera/cutaway/navigation systems can find it.
            if (transform.parent == null)
                gameObject.name = $"Room_{id}_{displayName}";
        }

        private void OnMouseDown()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TryMoveHuman(roomId);
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
            if (screen.z <= 0) return;

            var rect = new Rect(screen.x - 60, Screen.height - screen.y - 15, 120, 24);
            GUI.Label(rect, $"{roomId}. {roomName}");
        }
    }
}
