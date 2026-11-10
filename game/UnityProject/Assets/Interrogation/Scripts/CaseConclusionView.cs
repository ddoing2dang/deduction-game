using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.Interrogation
{
    /// <summary>
    /// Shown only after an approved case has been solved. The culprit and
    /// closing narrative are never exposed for an unapproved or unsolved case.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DialogueInterrogationAdapter))]
    public sealed class CaseConclusionView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset headingFont;

        private DialogueInterrogationAdapter adapter;
        private GameObject tab;
        private GameObject window;
        private Button openButton;
        private TMP_Text story;
        private TMP_Text caseHeading;
        private bool lastSolved;
        private int lastTheme = -1;

        private static readonly Color Background = new Color32(27, 43, 55, 252);
        private static readonly Color Surface = new Color32(104, 85, 68, 255);
        private static readonly Color Light = new Color32(249, 237, 214, 255);

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            adapter = GetComponent<DialogueInterrogationAdapter>();
            if (adapter == null) return;
            adapter.VerdictChanged += OnVerdict;
            Build();
            lastTheme = (int)adapter.CurrentTheme;
            lastSolved = adapter.CaseReady && adapter.CaseSolved;
            Refresh();
        }

        private void OnDisable()
        {
            if (adapter != null) adapter.VerdictChanged -= OnVerdict;
            if (Application.IsPlaying(gameObject))
            {
                if (tab != null) Destroy(tab);
                if (window != null) Destroy(window);
            }
            tab = null;
            window = null;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || adapter == null) return;
            int theme = (int)adapter.CurrentTheme;
            bool solved = adapter.CaseReady && adapter.CaseSolved;
            if (theme == lastTheme && solved == lastSolved) return;
            if (theme != lastTheme) HideWindow();
            lastTheme = theme;
            lastSolved = solved;
            Refresh();
        }

        private void OnVerdict(VerdictOutcome result)
        {
            if (result.State != VerdictState.Solved || adapter == null
                || !adapter.CaseReady || !adapter.CaseSolved) return;
            ShowWindow();
        }

        private static RectTransform Rect(string id, Transform parent,
            Vector2 size, Vector2 pos, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(id, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private TMP_Text Text(Transform parent, string id, string value,
            Vector2 pos, Vector2 size, int fontSize, bool heading = false)
        {
            var rect = Rect(id, parent, size, pos, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = (heading ? headingFont : regularFont) ?? TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = fontSize;
            text.color = Light;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button Button(Transform parent, string id, string label,
            Vector2 pos, Vector2 size, Action callback)
        {
            var rect = Rect(id, parent, size, pos, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = Surface;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            Text(rect, "Label", label, Vector2.zero,
                new Vector2(size.x - 10, size.y - 4), 20, true);
            button.onClick.AddListener(() => callback());
            return button;
        }

        private void Build()
        {
            openButton = Button(transform, "OpenCaseConclusion", "사건 결말",
                Vector2.zero, new Vector2(170, 46), ShowWindow);
            tab = openButton.gameObject;
            var tabRect = openButton.GetComponent<RectTransform>();
            tabRect.anchorMin = tabRect.anchorMax = Vector2.one;
            tabRect.pivot = Vector2.one;
            tabRect.anchoredPosition = new Vector2(-558, -14);

            var root = Rect("CaseConclusionWindow", transform,
                new Vector2(710, 440), new Vector2(-18, -75), Vector2.one, Vector2.one);
            var image = root.gameObject.AddComponent<Image>();
            image.color = Background;
            window = root.gameObject;
            caseHeading = Text(root, "ConclusionHeading", "사건 해결",
                new Vector2(0, 158), new Vector2(650, 62), 34, true);
            story = Text(root, "ConclusionNarrative", "",
                new Vector2(0, 21), new Vector2(650, 200), 23);
            Text(root, "ConclusionFooter", "증거와 진술을 통해 진상을 밝혀냈습니다.",
                new Vector2(0, -110), new Vector2(650, 48), 18);
            Button(root, "CloseConclusion", "수사로 돌아가기",
                new Vector2(0, -175), new Vector2(310, 55), HideWindow);
            window.SetActive(false);
        }

        private void Refresh()
        {
            if (adapter == null || openButton == null) return;
            bool show = adapter.CaseReady && adapter.CaseSolved;
            openButton.interactable = show;
            if (!show)
            {
                HideWindow();
                return;
            }
            var file = adapter.CurrentCase;
            caseHeading.text = "사건 해결";
            story.text = file != null && file.verdict != null
                ? file.verdict.successText : "";
        }

        private void ShowWindow()
        {
            if (window == null || adapter == null
                || !adapter.CaseReady || !adapter.CaseSolved) return;
            var notebook = GetComponent<InvestigationNotebookView>();
            if (notebook != null) notebook.HideWindow();
            var investigation = GetComponent<InvestigationSceneView>();
            if (investigation != null) investigation.HideWindow();
            var saveMenu = GetComponent<CaseProgressSaveController>();
            if (saveMenu != null) saveMenu.HideWindow();
            Refresh();
            window.SetActive(true);
            window.transform.SetAsLastSibling();
        }

        public void HideWindow()
        {
            if (window != null) window.SetActive(false);
        }
    }
}
