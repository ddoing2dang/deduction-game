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

            // The image always faces the room-review camera. Feet remain fixed
            // to their authored world/split anchors (no runtime NPC teleport).
            Camera cam = hotelCamera.ReviewCamera;
            if (cam == null) return;
            Quaternion billboarding = Quaternion.LookRotation(-cam.transform.forward,
                cam.transform.up);
            if (physical != null) physical.transform.rotation = billboarding;
            if (splitDisplay != null) splitDisplay.transform.rotation = billboarding;
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

    [DisallowMultipleComponent]
    public sealed class HotelNpcClickProxy : MonoBehaviour
    {
        [SerializeField] private HotelNpcActor actor;

        public void Configure(HotelNpcActor value) { actor = value; }

        private void OnMouseUpAsButton()
        {
            if (actor != null) actor.Interact();
        }
    }
}
