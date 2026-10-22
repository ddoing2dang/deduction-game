using UnityEngine;

namespace DeductionGame
{
    public sealed class PcCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float rotateSpeed = 70f;
        [SerializeField] private float zoomSpeed = 12f;
        [SerializeField] private float minHeight = 7f;
        [SerializeField] private float maxHeight = 22f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<PcCameraController>() == null)
                cam.gameObject.AddComponent<PcCameraController>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 delta = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) delta += forward;
            if (Input.GetKey(KeyCode.S)) delta -= forward;
            if (Input.GetKey(KeyCode.D)) delta += right;
            if (Input.GetKey(KeyCode.A)) delta -= right;

            transform.position += delta * moveSpeed * dt;

            if (Input.GetMouseButton(2))
            {
                float yaw = Input.GetAxis("Mouse X") * rotateSpeed * dt;
                transform.RotateAround(Vector3.zero, Vector3.up, yaw);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 pos = transform.position + transform.forward * (scroll * zoomSpeed * dt * 8f);
                pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
                transform.position = pos;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                transform.position = new Vector3(0f, 13f, -12f);
                transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            }
        }
    }
}
