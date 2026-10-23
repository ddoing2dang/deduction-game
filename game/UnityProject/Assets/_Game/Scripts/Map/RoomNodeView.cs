using UnityEngine;

namespace DeductionGame
{
    public sealed class RoomNodeView : MonoBehaviour
    {
        [SerializeField] private int roomId;
        [SerializeField] private string roomName;
        public int RoomId => roomId;

        private void Start()
        {
            RoomPresentation.Install(gameObject, roomId, PresentationCatalog.Load());
            RuntimeBoardView.RefreshAll();
        }

        public void Configure(int id, string displayName)
        {
            roomId = id;
            roomName = displayName;
            if (transform.parent == null)
                gameObject.name = $"Room_{id}_{displayName}";
        }

        private void OnMouseDown()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            foreach (var touch in Input.touches)
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
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
