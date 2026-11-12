using System;
using Case464.Hotel;
using Mystery.UI;
using UnityEngine;
using UnityEngine.Rendering;

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
        // Source alpha geometry is authored without changing PNGs or importer GUIDs.
        [SerializeField] private Rect[] opaquePixelBounds = Array.Empty<Rect>();
        [SerializeField] private Vector2[] footPixels = Array.Empty<Vector2>();
        [SerializeField] private float visibleBodyHeight = 1.65f;
        [SerializeField] private bool screenPresentationConfigured;
        [SerializeField] private Vector3 physicalFootLocal;
        [SerializeField] private Vector3 displayFootLocal;
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
            SyncView();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRender;
            RenderPipelineManager.beginCameraRendering += BeforeCameraRender;
            ApplyPose();
            SyncView();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRender;
        }

        private void BeforeCameraRender(ScriptableRenderContext context, Camera camera)
        {
            if (hotelCamera != null && camera == hotelCamera.ReviewCamera) SyncView();
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

            Camera cam = hotelCamera.ReviewCamera;
            if (cam == null) return;
            AlignImage(physical, physicalFootLocal, cam);
            AlignImage(splitDisplay, displayFootLocal, cam);
        }

        private void AlignImage(SpriteRenderer renderer, Vector3 footLocal, Camera camera)
        {
            if (renderer == null) return;
            // Exact screen basis: +X = screen-right, +Y = screen-up.
            // There is no world-upright lean and therefore no cosine foreshortening.
            renderer.transform.rotation = camera.transform.rotation;
            renderer.flipX = false; renderer.flipY = false;
            int index = (int)facing;
            if (!screenPresentationConfigured || renderer.sprite == null ||
                opaquePixelBounds.Length != 8 || footPixels.Length != 8) return;
            var sprite = renderer.sprite;
            float pixelsHigh = opaquePixelBounds[index].height;
            if (pixelsHigh < 1) return;
            float parentScale = renderer.transform.parent != null ?
                renderer.transform.parent.lossyScale.y : 1f;
            float uniformScale = visibleBodyHeight * sprite.pixelsPerUnit /
                (pixelsHigh * Mathf.Max(.0001f, Mathf.Abs(parentScale)));
            renderer.transform.localScale = Vector3.one * uniformScale;
            Vector2 pixelOffset = footPixels[index] - sprite.rect.min - sprite.pivot;
            Vector3 offset = renderer.transform.TransformVector(new Vector3(
                pixelOffset.x / sprite.pixelsPerUnit, pixelOffset.y / sprite.pixelsPerUnit, 0));
            renderer.transform.position = transform.TransformPoint(footLocal) - offset;
        }

        // Explicit Editor authoring supplies measurements from the original alpha pixels.
        public void ConfigureScreenPresentation(Rect[] bounds, Vector2[] feet, Material material)
        {
            if (bounds == null || feet == null || bounds.Length != 8 || feet.Length != 8 || material == null)
                throw new ArgumentException("Eight original alpha bounds/feet and an NPC material are required.");
            if (!screenPresentationConfigured)
            {
                physicalFootLocal = physical.transform.localPosition;
                displayFootLocal = splitDisplay.transform.localPosition;
            }
            opaquePixelBounds = bounds; footPixels = feet; visibleBodyHeight = 1.65f;
            screenPresentationConfigured = true;
            physical.sharedMaterial = material; splitDisplay.sharedMaterial = material;
            ApplyPose(); SyncView();
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
