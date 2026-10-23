using UnityEngine;

namespace DeductionGame
{
    public sealed class PcCameraController : MonoBehaviour
    {
        private static readonly float[] ZoomLevels = { 9f, 19f, 32f };

        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 28f, -24f);
        [SerializeField] private float followSmoothTime = 0.22f;
        [SerializeField] private float zoomSmoothTime = 0.18f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private float zoomVelocity;
        private int zoomIndex = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<PcCameraController>() == null)
                cam.gameObject.AddComponent<PcCameraController>();
        }

        private void Start()
        {
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = ZoomLevels[zoomIndex];
            }

            transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            focus = ResolveTarget();
            transform.position = focus + cameraOffset;
        }

        private void LateUpdate()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) return;

            HandleZoomInput();

            Vector3 target = GameplayPresentation.Active ? GameplayPresentation.Focus : ResolveTarget();
            focus = Vector3.SmoothDamp(
                focus,
                target,
                ref focusVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            transform.position = focus + cameraOffset;
            transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                GameplayPresentation.Active ? GameplayPresentation.Zoom : ZoomLevels[zoomIndex],
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        private void HandleZoomInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) zoomIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) zoomIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) zoomIndex = 2;

            float scroll = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() ? 0f : Input.mouseScrollDelta.y;
            if (scroll > 0.05f)
                zoomIndex = Mathf.Max(0, zoomIndex - 1);
            else if (scroll < -0.05f)
                zoomIndex = Mathf.Min(ZoomLevels.Length - 1, zoomIndex + 1);

            if (Input.GetKeyDown(KeyCode.F))
                zoomIndex = 2;
        }

        private static Vector3 ResolveTarget()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Human == null)
                return MapData.GetRoom(1).Center;

            return MapData.GetRoom(gm.Human.RoomId).Center;
        }
    }
}
