using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.Interrogation
{
    /// <summary>
    /// Compact runtime notebook and accusation sheet on the existing DialoguePanel.
    /// Its UI is created at runtime only; the original three dialogue frames and
    /// existing serialized RectTransforms are never replaced or moved.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DialogueInterrogationAdapter))]
    public sealed class InvestigationNotebookView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset headingFont;

        private readonly HashSet<string> presentedEvidence = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<GameObject> clueRows = new List<GameObject>();
        private readonly List<Image> suspectImages = new List<Image>();
        private DialogueInterrogationAdapter adapter;
        private GameObject notebookTab;
        private GameObject notebookWindow;
        private RectTransform cluesContent;
        private TMP_Text clueCounter;
        private TMP_Text selectedCounter;
        private TMP_Text feedbackText;
        private Button accuseButton;
        private int selectedSuspect = -1;
        private int lastTheme = -1;
        private bool wasReady;

        private static readonly string[] Names =
            { "마도연", "마루희", "나주은", "이복희", "진루나", "문일혜" };

        private static readonly Color Background = new Color32(30, 44, 54, 248);
        private static readonly Color Surface = new Color32(51, 70, 81, 255);
        private static readonly Color Selected = new Color32(147, 113, 72, 255);
        private static readonly Color Light = new Color32(246, 238, 221, 255);
        private static readonly Color Muted = new Color32(198, 197, 191, 255);

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            adapter = GetComponent<DialogueInterrogationAdapter>();
            if (adapter == null || GetComponent<RectTransform>() == null) return;
            adapter.EvidenceChanged += Refresh;
            Build();
            lastTheme = (int)adapter.CurrentTheme;
            wasReady = adapter.CaseReady;
            Refresh();
        }

        private void OnDisable()
        {
            if (adapter != null) adapter.EvidenceChanged -= Refresh;
            if (Application.IsPlaying(gameObject))
            {
                if (notebookTab != null) Destroy(notebookTab);
                if (notebookWindow != null) Destroy(notebookWindow);
            }
            notebookTab = null;
            notebookWindow = null;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || adapter == null) return;
            int current = (int)adapter.CurrentTheme;
            bool ready = adapter.CaseReady;
            if (current == lastTheme && ready == wasReady) return;
            lastTheme = current;
            wasReady = ready;
            selectedSuspect = -1;
            presentedEvidence.Clear();
            Refresh();
        }

        private static RectTransform Rect(string name, Transform parent,
            Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private TextMeshProUGUI Text(Transform parent, string name, string value,
            Vector2 position, Vector2 size, float fontSize, bool heading = false)
        {
            var rect = Rect(name, parent, size, position, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = (heading ? headingFont : regularFont) ?? TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = Light;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button Button(Transform parent, string name, string caption,
            Vector2 position, Vector2 size, Action clicked, out Image background)
        {
            var rect = Rect(name, parent, size, position, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            background = Fill(rect, Surface);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color32(185, 165, 139, 255);
            colors.pressedColor = new Color32(110, 95, 82, 255);
            button.colors = colors;
            var label = Text(rect, "Caption", caption, Vector2.zero,
                new Vector2(size.x - 10, size.y - 3), 20, true);
            label.raycastTarget = false;
            if (clicked != null) button.onClick.AddListener(() => clicked());
            return button;
        }

        private void Build()
        {
            var root = transform;
            Image tabImage;
            var tab = Button(root, "OpenInvestigationNotebook", "수사 수첩",
                new Vector2(-18, -14), new Vector2(170, 46), ToggleWindow, out tabImage);
            var tabRect = tab.GetComponent<RectTransform>();
            tabRect.anchorMin = tabRect.anchorMax = Vector2.one;
            tabRect.pivot = Vector2.one;
            notebookTab = tab.gameObject;

            var window = Rect("InvestigationNotebook", root,
                new Vector2(490, 688), new Vector2(-18, -72),
                Vector2.one, Vector2.one);
            Fill(window, Background);
            notebookWindow = window.gameObject;

            Text(window, "Heading", "수사 수첩 · 범인 지목",
                new Vector2(0, 300), new Vector2(440, 43), 27, true);
            clueCounter = Text(window, "EvidenceCount", "",
                new Vector2(0, 251), new Vector2(440, 38), 21);
            Text(window, "EvidenceHint", "확보한 증거를 눌러 지목 근거로 선택하세요.",
                new Vector2(0, 214), new Vector2(440, 30), 17);

            var scrolling = Rect("EvidenceScroll", window,
                new Vector2(446, 215), new Vector2(0, 75),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            Fill(scrolling, Surface);
            var scroll = scrolling.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", scrolling,
                new Vector2(438, 207), Vector2.zero, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            viewport.gameObject.AddComponent<RectMask2D>();
            cluesContent = Rect("FoundEvidence", viewport,
                new Vector2(432, 200), new Vector2(0, -4),
                new Vector2(.5f, 1), new Vector2(.5f, 1));
            scroll.viewport = viewport;
            scroll.content = cluesContent;

            Text(window, "SuspectHeading", "의심 인물 선택",
                new Vector2(0, -69), new Vector2(440, 31), 22, true);
            for (int i = 0; i < Names.Length; i++)
            {
                int captured = i;
                Image background;
                Button(window, "Suspect" + i, Names[i],
                    new Vector2(-149 + (i % 3) * 149, -114 - (i / 3) * 50),
                    new Vector2(139, 42),
                    () => SelectSuspect(captured), out background);
                suspectImages.Add(background);
            }
            selectedCounter = Text(window, "SelectedClues", "",
                new Vector2(0, -208), new Vector2(440, 32), 18);
            Image accuseImage;
            accuseButton = Button(window, "SubmitAccusation", "증거와 함께 지목하기",
                new Vector2(0, -256), new Vector2(432, 52),
                SubmitAccusation, out accuseImage);
            feedbackText = Text(window, "InvestigationFeedback", "",
                new Vector2(0, -314), new Vector2(448, 57), 17);
            notebookWindow.SetActive(false);
        }

        private void ToggleWindow()
        {
            if (notebookWindow == null) return;
            notebookWindow.SetActive(!notebookWindow.activeSelf);
            if (notebookWindow.activeSelf) Refresh();
        }

        private void SelectSuspect(int suspect)
        {
            selectedSuspect = suspect;
            Refresh();
        }

        private void Refresh()
        {
            if (adapter == null || notebookWindow == null || cluesContent == null) return;
            var discovered = adapter.DiscoveredEvidence();
            var available = new HashSet<string>(StringComparer.Ordinal);
            foreach (var clue in discovered) available.Add(clue.id);
            presentedEvidence.RemoveWhere(id => !available.Contains(id));

            foreach (var row in clueRows)
                if (row != null) Destroy(row);
            clueRows.Clear();

            clueCounter.text = adapter.CaseReady
                ? "확보한 단서: " + discovered.Count + " / " + adapter.EvidenceTotal
                : "아직 승인된 사건 자료가 없습니다.";

            cluesContent.sizeDelta = new Vector2(432, Math.Max(207, discovered.Count * 46 + 12));
            for (int i = 0; i < discovered.Count; i++)
            {
                var clue = discovered[i];
                string id = clue.id;
                Image image;
                Button button = Button(cluesContent, "Clue_" + i,
                    (presentedEvidence.Contains(id) ? "■ " : "□ ") + clue.title,
                    new Vector2(0, -29 - i * 46), new Vector2(422, 40),
                    () => ToggleEvidence(id), out image);
                image.color = presentedEvidence.Contains(id) ? Selected : Surface;
                clueRows.Add(button.gameObject);
            }
            selectedCounter.text = "제출할 증거 " + presentedEvidence.Count + "개 · 남은 지목 "
                + (adapter.CaseReady ? adapter.RemainingAccusations.ToString() : "—") + "회";
            for (int i = 0; i < suspectImages.Count; i++)
                suspectImages[i].color = i == selectedSuspect ? Selected : Surface;

            accuseButton.interactable = adapter.CaseReady && !adapter.CaseSolved
                && adapter.RemainingAccusations > 0 && selectedSuspect >= 0;
            if (!adapter.CaseReady)
                feedbackText.text = "사건 내용을 승인하기 전에는 증거 수집과 지목이 잠겨 있습니다.";
            else if (adapter.CaseSolved)
                feedbackText.text = "해결한 사건입니다.";
            else if (adapter.RemainingAccusations == 0)
                feedbackText.text = "지목 기회를 모두 사용했습니다.";
            else if (string.IsNullOrWhiteSpace(feedbackText.text)
                || feedbackText.text.Contains("승인하기 전에는"))
                feedbackText.text = "근거를 충분히 확보한 뒤 범인을 지목해 주세요.";
        }

        private void ToggleEvidence(string id)
        {
            if (presentedEvidence.Contains(id)) presentedEvidence.Remove(id);
            else if (adapter != null && adapter.HasEvidence(id)) presentedEvidence.Add(id);
            Refresh();
        }

        private void SubmitAccusation()
        {
            if (adapter == null) return;
            var result = adapter.Accuse(selectedSuspect, presentedEvidence);
            feedbackText.text = result.Message;
            // Refresh updates counters/button state without exposing the true culprit.
            Refresh();
            feedbackText.text = result.Message;
        }
    }
}
