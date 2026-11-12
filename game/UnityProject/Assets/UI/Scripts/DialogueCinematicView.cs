using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.UI
{
    /// <summary>
    /// Dialogue-only cinematic composition over the CURRENT world-camera image:
    /// blurred map, player illustration left, NPC illustration right, dialogue low.
    /// A missing player illustration remains empty rather than impersonating an NPC.
    /// Does not replace the existing theme frame, portrait sprites, or fonts.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DialoguePanel))]
    public sealed class DialogueCinematicView : MonoBehaviour
    {
        [Header("Optional actual player art (do not substitute an NPC)")]
        [SerializeField] private Sprite playerIllustration;
        [Header("World camera (auto-detect Main Camera if empty)")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] [Range(0.5f, 4f)] private float blurStrength = 2.0f;

        private DialoguePanel dialogue;
        private CanvasGroup visibility;
        private RectTransform rootRect;
        private RawImage mapBackdrop;
        private GameObject backdropRoot;
        private Image playerImage;
        private Image npcImage;
        private RenderTexture blurredMap;
        private Material blurMaterial;
        private Coroutine captureRoutine;
        private bool visible;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private DialogueSpeaker lastSpeaker;
        private DialogueTheme lastTheme;
        private static readonly Color Frost = new Color32(213, 223, 228, 255);

        public bool IsConversationVisible => visible;

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            dialogue = GetComponent<DialoguePanel>();
            rootRect = GetComponent<RectTransform>();
            if (dialogue == null || rootRect == null) return;

            visibility = GetComponent<CanvasGroup>();
            if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
            dialogue.UseExternalPortraits = true;
            dialogue.ApplyPresentation();

            BuildBackdrop();
            BuildPortraits();
            ConfigureAdaptiveFonts();
            ApplyLayout();
            lastSpeaker = dialogue.Speaker;
            lastTheme = dialogue.Theme;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            SetConversationVisible(visibleOnStart);
        }

        private void OnDisable()
        {
            if (captureRoutine != null) StopCoroutine(captureRoutine);
            captureRoutine = null;
            if (dialogue != null)
            {
                dialogue.UseExternalPortraits = false;
                dialogue.ApplyPresentation();
            }
            DisposeMapCapture();
            if (Application.IsPlaying(gameObject))
            {
                if (backdropRoot != null) Destroy(backdropRoot);
                if (playerImage != null) Destroy(playerImage.gameObject);
                if (npcImage != null) Destroy(npcImage.gameObject);
            }
            backdropRoot = null;
            playerImage = null;
            npcImage = null;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || dialogue == null) return;
            if (lastSpeaker != dialogue.Speaker || lastTheme != dialogue.Theme)
            {
                lastSpeaker = dialogue.Speaker;
                lastTheme = dialogue.Theme;
                RefreshPortraits();
            }
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                ApplyLayout();
                if (visible) RequestMapCapture();
            }
        }

        /// <summary>Called by the future map/NPC interaction controller.</summary>
        public void SetConversationVisible(bool show)
        {
            visible = show;
            if (visibility != null)
            {
                visibility.alpha = show ? 1f : 0f;
                visibility.interactable = show;
                visibility.blocksRaycasts = show;
            }
            if (backdropRoot != null) backdropRoot.SetActive(show);
            if (show)
            {
                RefreshPortraits();
                RequestMapCapture();
            }
        }

        public void OpenWithNpc(DialogueSpeaker who)
        {
            if (dialogue != null) dialogue.Speaker = who;
            SetConversationVisible(true);
        }

        public void CloseConversation()
        {
            SetConversationVisible(false);
        }

        public void SetPlayerIllustration(Sprite sprite)
        {
            playerIllustration = sprite;
            RefreshPortraits();
        }

        public void SetWorldCamera(Camera camera)
        {
            worldCamera = camera;
            if (visible) RequestMapCapture();
        }

        private void BuildBackdrop()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var target = canvas.rootCanvas.transform;
            var obj = new GameObject("CinematicMapBackdrop", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            obj.layer = target.gameObject.layer;
            obj.transform.SetParent(target, false);
            obj.transform.SetAsFirstSibling();
            backdropRoot = obj;
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            mapBackdrop = obj.GetComponent<RawImage>();
            mapBackdrop.raycastTarget = false;
            mapBackdrop.color = new Color(0.08f, 0.11f, 0.14f, 0.54f);
        }

        private static Image NewIllustration(string name, Transform parent, float x)
        {
            var obj = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(440f, 470f);
            rect.anchoredPosition = new Vector2(x, 545f);
            var image = obj.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private void BuildPortraits()
        {
            playerImage = NewIllustration("PlayerIllustrationLeft", transform, -565f);
            npcImage = NewIllustration("NpcIllustrationRight", transform, 565f);
            RefreshPortraits();
            // They are behind input / interactive panels, but above map and frame.
            var frame = transform.Find("Frame");
            if (frame != null)
            {
                playerImage.transform.SetSiblingIndex(frame.GetSiblingIndex() + 1);
                npcImage.transform.SetSiblingIndex(playerImage.transform.GetSiblingIndex() + 1);
            }
        }

        private void RefreshPortraits()
        {
            if (dialogue == null) return;
            if (playerImage != null)
            {
                playerImage.sprite = playerIllustration;
                playerImage.enabled = playerIllustration != null;
            }
            if (npcImage != null)
            {
                var sprite = dialogue.GetPortraitFor(dialogue.Speaker);
                npcImage.sprite = sprite;
                npcImage.enabled = sprite != null;
            }
        }

        private static void SetAnchored(RectTransform rect, Vector2 pos, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private void ApplyLayout()
        {
            if (rootRect == null) return;
            var frame = transform.Find("Frame") as RectTransform;
            if (frame != null)
            {
                // The original 2048:682 theme frame stays at native aspect ratio.
                SetAnchored(frame, new Vector2(0, 370), new Vector2(1800, 599.414f));
            }
            if (dialogue != null)
            {
                // Original serialized references are preserved, moved only at runtime.
                var nameLabel = dialogue.SpeakerNameLabel;
                var textLabel = dialogue.NpcDialogueLabel;
                if (nameLabel != null)
                {
                    var rect = nameLabel.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    // Nameplate stays on the LEFT, below the portrait edges.
                    rect.anchoredPosition = new Vector2(-615, 164);
                    rect.sizeDelta = new Vector2(405, 74);
                }
                if (textLabel != null)
                {
                    var rect = textLabel.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    // Dialogue text sits between the nameplate and input row.
                    rect.anchoredPosition = new Vector2(0, -10);
                    rect.sizeDelta = new Vector2(1480, 210);
                }
            }

            SetAnchored(transform.Find("DialogueHistory") as RectTransform,
                new Vector2(0, 795), new Vector2(760, 202));
            SetAnchored(transform.Find("HistoryTitle") as RectTransform,
                new Vector2(0, 930), new Vector2(520, 42));

            var history = transform.Find("DialogueHistory") as RectTransform;
            if (history != null)
            {
                var viewport = history.Find("Viewport") as RectTransform;
                if (viewport != null)
                {
                    SetAnchored(viewport, new Vector2(0, 0), new Vector2(736, 182));
                    var content = viewport.Find("Content") as RectTransform;
                    if (content != null)
                    {
                        content.anchorMin = new Vector2(0, 1);
                        content.anchorMax = new Vector2(1, 1);
                        content.pivot = new Vector2(.5f, 1);
                        content.anchoredPosition = Vector2.zero;
                        content.sizeDelta = new Vector2(0, 190);
                        var historyText = content.Find("HistoryText") as RectTransform;
                        if (historyText != null)
                        {
                            historyText.anchorMin = Vector2.zero;
                            historyText.anchorMax = Vector2.one;
                            historyText.pivot = new Vector2(.5f, .5f);
                            historyText.anchoredPosition = Vector2.zero;
                            historyText.sizeDelta = new Vector2(-20, -6);
                        }
                    }
                }
                var bar = history.Find("Scrollbar") as RectTransform;
                if (bar != null) SetAnchored(bar, new Vector2(364, 0), new Vector2(12, 180));
            }

            // Frame bottom is y~=70. The question row is BELOW it (y=8..62):
            // neither the placeholder nor the send button covers its gold trim.
            SetAnchored(transform.Find("QuestionInput") as RectTransform,
                new Vector2(-105, 35), new Vector2(1538, 54));
            SetAnchored(transform.Find("SendButton") as RectTransform,
                new Vector2(790, 35), new Vector2(180, 54));
            // Center the text and placeholder inside the shortened input.
            var inputRoot = transform.Find("QuestionInput");
            var textArea = inputRoot != null ? inputRoot.Find("Text Area") as RectTransform : null;
            if (textArea != null)
            {
                textArea.sizeDelta = new Vector2(1492, 46);
                foreach (string childName in new[] { "Text", "Placeholder" })
                {
                    var child = textArea.Find(childName) as RectTransform;
                    if (child != null) child.sizeDelta = new Vector2(1492, 46);
                }
            }
            var sendLabel = transform.Find("SendButton/Label") as RectTransform;
            if (sendLabel != null) sendLabel.sizeDelta = new Vector2(174, 44);
            SetAnchored(transform.Find("Status") as RectTransform,
                new Vector2(0, 143), new Vector2(1500, 29));

            if (playerImage != null)
                SetAnchored(playerImage.rectTransform, new Vector2(-555, 810),
                    new Vector2(410, 430));
            if (npcImage != null)
                SetAnchored(npcImage.rectTransform, new Vector2(555, 810),
                    new Vector2(410, 430));
            // Portraits visually overlap only the upper frame edge, not its text.
            // The history panel fits into the free space between both portraits.
            if (dialogue != null) dialogue.ReflowHistory();
        }

        private void ConfigureAdaptiveFonts()
        {
            if (dialogue == null) return;
            Configure(dialogue.SpeakerNameLabel, 40, 23, 42);
            Configure(dialogue.NpcDialogueLabel, 33, 18, 36);
            Configure(dialogue.HistoryLabel, 23, 16, 25);
            if (dialogue.HistoryLabel != null)
                dialogue.HistoryLabel.overflowMode = TextOverflowModes.Overflow;
            Configure(dialogue.HistoryHeadingLabel, 25, 17, 29);
            Configure(dialogue.StatusLabel, 19, 14, 22);
            Configure(dialogue.InputField != null ? dialogue.InputField.textComponent : null,
                26, 18, 29);
            Configure(dialogue.InputField != null ? dialogue.InputField.placeholder as TMP_Text : null,
                25, 17, 28);
            var buttonLabel = transform.Find("SendButton/Label")?.GetComponent<TMP_Text>();
            Configure(buttonLabel, 25, 16, 28);
        }

        private static void Configure(TMP_Text label, float preferred, float min, float max)
        {
            if (label == null) return;
            label.enableAutoSizing = true;
            label.fontStyle |= FontStyles.Bold;
            label.fontSize = preferred;
            label.fontSizeMin = min;
            label.fontSizeMax = max;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(8, 5, 8, 5);
        }

        private void RequestMapCapture()
        {
            if (captureRoutine != null) StopCoroutine(captureRoutine);
            captureRoutine = StartCoroutine(CaptureMapAtEndOfFrame());
        }

        private IEnumerator CaptureMapAtEndOfFrame()
        {
            // Wait for the original map camera to finish rendering. Screen-space
            // overlay UI is not drawn into Camera.Render's target texture.
            yield return new WaitForEndOfFrame();
            captureRoutine = null;
            if (!visible || mapBackdrop == null) yield break;

            var camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
            {
                mapBackdrop.texture = null;
                mapBackdrop.color = new Color(0.08f, .11f, .14f, .54f);
                yield break;
            }

            int width = Mathf.Clamp(Screen.width / 3, 320, 720);
            int height = Mathf.Clamp(Screen.height / 3, 180, 480);
            var source = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var intermediate = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            bool rendered = false;
            try
            {
                if (blurredMap == null || blurredMap.width != width || blurredMap.height != height)
                {
                    DisposeMapCapture();
                    blurredMap = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                    blurredMap.filterMode = FilterMode.Bilinear;
                    blurredMap.wrapMode = TextureWrapMode.Clamp;
                    blurredMap.Create();
                }
                camera.targetTexture = source;
                camera.Render();
                if (blurMaterial == null)
                {
                    var shader = Shader.Find("Mystery/DialogueMapBlur");
                    if (shader != null) blurMaterial = new Material(shader)
                        { hideFlags = HideFlags.DontSave };
                }
                if (blurMaterial != null)
                {
                    blurMaterial.SetFloat("_BlurStrength", blurStrength);
                    Graphics.Blit(source, intermediate, blurMaterial, 0);
                    Graphics.Blit(intermediate, blurredMap, blurMaterial, 1);
                }
                else
                    Graphics.Blit(source, blurredMap);
                rendered = true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[mystery dialogue] Could not capture the map: " + error.Message
                    + ". Keeping the existing world visible under a soft dim layer.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(source);
                RenderTexture.ReleaseTemporary(intermediate);
            }
            mapBackdrop.texture = rendered ? blurredMap : null;
            mapBackdrop.color = rendered ? Frost : new Color(.08f, .11f, .14f, .54f);
        }

        private void DisposeMapCapture()
        {
            if (blurMaterial != null) Destroy(blurMaterial);
            blurMaterial = null;
            if (blurredMap != null)
            {
                blurredMap.Release();
                Destroy(blurredMap);
                blurredMap = null;
            }
        }
    }
}
