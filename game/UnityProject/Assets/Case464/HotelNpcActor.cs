using System;
using Case464.Hotel;
using Mystery.UI;
using UnityEngine;

namespace Case464.NPC
{
    // Eight distinct still poses from the user-provided sprite archive.
    // These are NOT walking or running animation frames.
    public enum HotelNpcFacing { N, NE, E, SE, S, SW, W, NW }

    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HotelNpcActor : MonoBehaviour
    {
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private string roomId;
        [SerializeField] private int floor;
        [SerializeField] private int dialogueSpeakerIndex;
        [SerializeField] private HotelNpcFacing facing = HotelNpcFacing.S;
        // 90-degree overhead maps cannot see a fully vertical 2D sprite.
        // Limit camera-facing lean so the NPC reads as standing, not painted flat on the floor.
        [SerializeField, Range(25f, 60f)] private float maximumLeanFromVertical = 45f;
        [SerializeField] private Sprite[] stillDirections = Array.Empty<Sprite>();
        [SerializeField] private SpriteRenderer physical;
        [SerializeField] private SpriteRenderer splitDisplay;
        [SerializeField] private BoxCollider physicalHitbox;
        [SerializeField] private BoxCollider splitHitbox;
        [SerializeField] private HotelMapCameraController hotelCamera;

        public string NpcId => npcId;
        public string RoomId => roomId;
        public int Floor => floor;

        public void Configure(string id, string name, string room, int level, int speaker,
            Sprite[] poses, SpriteRenderer worldRenderer, SpriteRenderer splitRenderer,
            BoxCollider worldHitbox, BoxCollider splitCollider,
            HotelMapCameraController controller)
        {
            npcId = id;
            displayName = name;
            roomId = room;
            floor = level;
            dialogueSpeakerIndex = speaker;
            stillDirections = poses;
            physical = worldRenderer;
            splitDisplay = splitRenderer;
            physicalHitbox = worldHitbox;
            splitHitbox = splitCollider;
            hotelCamera = controller;
            ApplyPose();
            SyncView();
        }

        public void Face(HotelNpcFacing direction)
        {
            facing = direction;
            ApplyPose();
        }

        private void OnEnable()
        {
            ApplyPose();
            SyncView();
        }

        private void OnValidate()
        {
            ApplyPose();
        }

        private void LateUpdate()
        {
            SyncView();
        }

        private void ApplyPose()
        {
            int index = (int)facing;
            if (stillDirections == null || stillDirections.Length != 8 ||
                index < 0 || index >= stillDirections.Length)
                return;
            if (physical != null) physical.sprite = stillDirections[index];
            if (splitDisplay != null) splitDisplay.sprite = stillDirections[index];
        }

        private void SyncView()
        {
            if (hotelCamera == null) return;
            HotelMapView view = hotelCamera.Mode;
            bool split = view == HotelMapView.SplitFloors;
            bool visible = true;
            switch (view)
            {
                case HotelMapView.FirstFloor: visible = floor == 1; break;
                case HotelMapView.SecondFloor: visible = floor == 2; break;
                case HotelMapView.Room:
                case HotelMapView.Object:
                    visible = hotelCamera.SelectedRoom == roomId;
                    break;
                case HotelMapView.Region:
                    visible = false;
                    var regions = hotelCamera.Regions;
                    if (regions != null)
                        foreach (var region in regions)
                            if (region != null && region.key == hotelCamera.SelectedRegion
                                && region.groupKeys != null)
                                foreach (var key in region.groupKeys)
                                    if (key == roomId) visible = true;
                    break;
            }

            if (physical != null) physical.enabled = visible && !split;
            if (splitDisplay != null) splitDisplay.enabled = visible && split;
            if (physicalHitbox != null)
                physicalHitbox.enabled = Application.isPlaying && visible && !split;
            if (splitHitbox != null)
                splitHitbox.enabled = Application.isPlaying && visible && split;

            // Keep sprite feet in place and its body mostly vertical.
            // The previous full camera billboard turned the whole XY sprite
            // parallel to the floor when the hotel camera looked straight down.
            Camera cam = hotelCamera.ReviewCamera;
            if (cam == null) return;
            Quaternion standing = StandingBillboard(cam, maximumLeanFromVertical);
            if (physical != null) physical.transform.rotation = standing;
            if (splitDisplay != null) splitDisplay.transform.rotation = standing;
        }

        private static Quaternion StandingBillboard(Camera camera, float maxLeanDegrees)
        {
            // Sprite local +Y must point toward screen-up while retaining a
            // substantial world-up component. Never rotate the feet anchor.
            Vector3 screenUpOnFloor = Vector3.ProjectOnPlane(
                camera.transform.up, Vector3.up);
            Vector3 screenRightOnFloor = Vector3.ProjectOnPlane(
                camera.transform.right, Vector3.up);

            // A horizontal camera already sees an upright sprite normally.
            if (screenUpOnFloor.sqrMagnitude < 0.000001f ||
                screenRightOnFloor.sqrMagnitude < 0.000001f)
                return Quaternion.LookRotation(-camera.transform.forward,
                    camera.transform.up);

            float cameraPitch = Mathf.Asin(Mathf.Clamp(
                Mathf.Abs(Vector3.Dot(camera.transform.forward, Vector3.up)), 0f, 1f))
                * Mathf.Rad2Deg;
            float leanRadians = Mathf.Min(cameraPitch,
                Mathf.Clamp(maxLeanDegrees, 25f, 60f)) * Mathf.Deg2Rad;
            Vector3 spriteUp = (Vector3.up * Mathf.Cos(leanRadians) +
                screenUpOnFloor.normalized * Mathf.Sin(leanRadians)).normalized;

            // Like the original billboard, use the camera-facing side of the
            // original PNG. Do not mirror images, alter UVs, or invert sprite up.
            Vector3 spriteNormal = Vector3.Cross(
                -screenRightOnFloor.normalized, spriteUp).normalized;
            return Quaternion.LookRotation(spriteNormal, spriteUp);
        }

        public void Interact()
        {
            if (!Application.isPlaying) return;
            // If the live dialogue scene is not present yet, do not pretend an
            // investigation has begun or reveal any unapproved testimony.
            var cinematic = FindFirstObjectByType<DialogueCinematicView>(
                FindObjectsInactive.Include);
            if (cinematic == null)
            {
                Debug.Log("[Case464] NPC " + displayName +
                    " selected; no dialogue presenter is loaded yet.");
                return;
            }
            var panel = cinematic.GetComponent<DialoguePanel>();
            if (panel == null) return;
            panel.Theme = DialogueTheme.MANSION;
            cinematic.OpenWithNpc((DialogueSpeaker)dialogueSpeakerIndex);
        }
    }

}
