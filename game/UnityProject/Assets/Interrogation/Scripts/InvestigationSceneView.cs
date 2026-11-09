using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.Interrogation
{
    /// <summary>
    /// Optional pre-map investigation window: location -> hotspot -> examine
    /// -> collect. It changes NO authored dialogue frame or NPC portrait assets.
    /// It does not unlock facts unless the source case is author-approved.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DialogueInterrogationAdapter))]
    public sealed class InvestigationSceneView : MonoBehaviour
    {
        [Header("Location catalogs (one per case theme)")]
        [SerializeField] private TextAsset mansionInvestigation;
        [SerializeField] private TextAsset cruiseInvestigation;
        [SerializeField] private TextAsset musicBroadcastInvestigation;
        [Header("Already imported TMP font assets")]
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset headingFont;

        private static readonly Color Back = new Color32(27, 42, 53, 250);
        private static readonly Color Surface = new Color32(53, 70, 79, 255);
        private static readonly Color Accent = new Color32(153, 116, 72, 255);
        private static readonly Color Foreground = new Color32(247, 236, 215, 255);
        private static readonly Color Muted = new Color32(205, 203, 195, 255);

        private readonly Dictionary<int, InvestigationCatalog> catalogs
            = new Dictionary<int, InvestigationCatalog>();
        private readonly List<GameObject> locationRows = new List<GameObject>();
        private readonly List<GameObject> hotspotRows = new List<GameObject>();
        private DialogueInterrogationAdapter adapter;
        private GameObject openTab;
        private GameObject window;
        private RectTransform locationContent;
        private RectTransform hotspotContent;
        private TMP_Text progress;
        private TMP_Text roomTitle;
        private TMP_Text roomDescription;
        private TMP_Text observationText;
        private TMP_Text feedbackText;
        private Button examineButton;
        private Button collectButton;
        private TMP_Text collectLabel;
        private int selectedLocation = -1;
        private int selectedHotspot = -1;
        private int lastTheme = -1;
        private bool wasReady;
        private string examinedSpotId;
        private string statusMessage = "";

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            adapter = GetComponent<DialogueInterrogationAdapter>();
            if (adapter == null || GetComponent<RectTransform>() == null) return;
            Read(0, mansionInvestigation);
            Read(1, cruiseInvestigation);
            Read(2, musicBroadcastInvestigation);
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
                if (openTab != null) Destroy(openTab);
                if (window != null) Destroy(window);
            }
            openTab = null;
            window = null;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || adapter == null) return;
            int theme = (int)adapter.CurrentTheme;
            bool ready = adapter.CaseReady;
            if (theme == lastTheme && ready == wasReady) return;
            lastTheme = theme;
            wasReady = ready;
            selectedLocation = selectedHotspot = -1;
            examinedSpotId = null;
            statusMessage = "";
            Refresh();
        }

        private void Read(int theme, TextAsset json)
        {
            catalogs.Remove(theme);
            if (json == null) return;
            try
            {
                var parsed = JsonUtility.FromJson<InvestigationCatalog>(json.text);
                if (parsed == null || parsed.theme != theme)
                {
                    Debug.LogWarning("[mystery] Investigation catalog theme mismatch: " + json.name);
                    return;
                }
                catalogs[theme] = parsed;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[mystery] Invalid investigation JSON " + json.name + ": " + e.Message);
            }
        }

        private bool TryGetCatalog(out InvestigationCatalog catalog)
        {
            catalog = null;
            if (adapter == null || !adapter.CaseReady) return false;
            if (!catalogs.TryGetValue((int)adapter.CurrentTheme, out catalog))
                return false;
            return InvestigationCatalogRules.Validate(catalog, adapter.CurrentCase) == null;
        }

        private static RectTransform Rect(string name, Transform parent,
            Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image SurfaceImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private TextMeshProUGUI Label(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, bool heading = false)
        {
            var rect = Rect(name, parent, size, position,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = (heading ? headingFont : regularFont) ?? TMP_Settings.defaultFontAsset;
            label.color = heading ? Foreground : Muted;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private Button Action(Transform parent, string id, string title,
            Vector2 position, Vector2 size, Action click, out Image color,
            Vector2? anchor = null)
        {
            var rect = Rect(id, parent, size, position,
                anchor ?? new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            color = SurfaceImage(rect, Surface);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = color;
            var colors = button.colors;
            colors.highlightedColor = new Color32(186, 163, 128, 255);
            colors.pressedColor = new Color32(107, 91, 67, 255);
            button.colors = colors;
            Label(rect, "Caption", title, Vector2.zero,
                new Vector2(size.x - 12, size.y - 4), 18, true);
            if (click != null) button.onClick.AddListener(() => click());
            return button;
        }

        private RectTransform ScrollColumn(Transform parent, string id,
            Vector2 size, Vector2 center, out RectTransform content)
        {
            var outer = Rect(id, parent, size, center,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            SurfaceImage(outer, Surface);
            var scroll = outer.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", outer, size - new Vector2(10, 10),
                Vector2.zero, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Items", viewport, new Vector2(size.x - 18, size.y - 10),
                new Vector2(0, -4), new Vector2(.5f, 1), new Vector2(.5f, 1));
            scroll.viewport = viewport;
            scroll.content = content;
            return outer;
        }

        private void Build()
        {
            Image unused;
            var tab = Action(transform, "OpenInvestigation", "현장 조사",
                new Vector2(-284, -37), new Vector2(170, 46),
                ToggleWindow, out unused, Vector2.one);
            var tabRect = tab.GetComponent<RectTransform>();
            tabRect.pivot = Vector2.one;
            tabRect.anchoredPosition = new Vector2(-198, -14);
            openTab = tab.gameObject;

            var root = Rect("InvestigationBoard", transform,
                new Vector2(900, 705), new Vector2(-18, -74), Vector2.one, Vector2.one);
            SurfaceImage(root, Back);
            window = root.gameObject;

            Label(root, "Title", "현장 조사 · 단서 발견",
                new Vector2(0, 317), new Vector2(690, 43), 29, true);
            Action(root, "CloseBoard", "닫기",
                new Vector2(396, 318), new Vector2(80, 42),
                HideWindow, out unused);
            progress = Label(root, "Progress", "", new Vector2(0, 265),
                new Vector2(805, 34), 21);
            Label(root, "LocationsLabel", "조사 장소",
                new Vector2(-292, 219), new Vector2(270, 34), 22, true);
            ScrollColumn(root, "LocationScroll", new Vector2(274, 464),
                new Vector2(-292, -31), out locationContent);

            roomTitle = Label(root, "RoomTitle", "",
                new Vector2(153, 223), new Vector2(530, 38), 24, true);
            roomDescription = Label(root, "RoomDescription", "",
                new Vector2(153, 170), new Vector2(525, 68), 18);
            Label(root, "HotspotTitle", "조사할 곳",
                new Vector2(153, 120), new Vector2(520, 33), 20, true);
            ScrollColumn(root, "HotspotScroll", new Vector2(526, 172),
                new Vector2(153, 22), out hotspotContent);
            observationText = Label(root, "Observation", "",
                new Vector2(153, -130), new Vector2(518, 96), 18);
            examineButton = Action(root, "ExamineButton", "자세히 조사",
                new Vector2(30, -236), new Vector2(240, 50),
                ExamineSelected, out unused);
            collectButton = Action(root, "CollectButton", "수첩에 기록",
                new Vector2(283, -236), new Vector2(240, 50),
                CollectSelected, out unused);
            collectLabel = collectButton.GetComponentInChildren<TextMeshProUGUI>();
            feedbackText = Label(root, "Feedback", "",
                new Vector2(120, -308), new Vector2(745, 67), 18);
            window.SetActive(false);
        }

        private void ToggleWindow()
        {
            if (window == null) return;
            bool open = !window.activeSelf;
            if (open)
            {
                var notebook = GetComponent<InvestigationNotebookView>();
                if (notebook != null) notebook.HideWindow();
                var saveMenu = GetComponent<CaseProgressSaveController>();
                if (saveMenu != null) saveMenu.HideWindow();
                var conclusion = GetComponent<CaseConclusionView>();
                if (conclusion != null) conclusion.HideWindow();
            }
            window.SetActive(open);
            if (open) Refresh();
        }

        public void HideWindow()
        {
            if (window != null) window.SetActive(false);
        }

        private InvestigationLocation SelectedLocation(InvestigationCatalog catalog)
        {
            return catalog != null && catalog.locations != null
                && selectedLocation >= 0 && selectedLocation < catalog.locations.Length
                    ? catalog.locations[selectedLocation] : null;
        }

        private InvestigationHotspot SelectedHotspot(InvestigationCatalog catalog)
        {
            var loc = SelectedLocation(catalog);
            return loc != null && loc.spots != null
                && selectedHotspot >= 0 && selectedHotspot < loc.spots.Length
                    ? loc.spots[selectedHotspot] : null;
        }

        private void SelectLocation(int index)
        {
            selectedLocation = index;
            selectedHotspot = -1;
            examinedSpotId = null;
            statusMessage = "";
            Refresh();
        }

        private void SelectHotspot(int index)
        {
            selectedHotspot = index;
            examinedSpotId = null;
            statusMessage = "";
            Refresh();
        }

        private void ExamineSelected()
        {
            if (!TryGetCatalog(out var catalog)) return;
            var spot = SelectedHotspot(catalog);
            if (spot == null) return;
            if (spot.requiresEvidenceIds != null)
                foreach (var id in spot.requiresEvidenceIds)
                    if (!adapter.HasEvidence(id))
                    {
                        examinedSpotId = null;
                        statusMessage = "관련 단서를 조금 더 살펴봐야 조사할 수 있습니다.";
                        Refresh();
                        return;
                    }
            examinedSpotId = spot.id;
            statusMessage = string.IsNullOrWhiteSpace(spot.evidenceId)
                ? "조사 기록을 확인했습니다."
                : adapter.HasEvidence(spot.evidenceId)
                    ? "이미 확보한 증거입니다."
                    : "조사를 마쳤습니다. 증거를 수첩에 기록할 수 있습니다.";
            Refresh();
        }

        private void CollectSelected()
        {
            if (!TryGetCatalog(out var catalog)) return;
            var spot = SelectedHotspot(catalog);
            if (spot == null || examinedSpotId != spot.id
                || string.IsNullOrWhiteSpace(spot.evidenceId)) return;
            if (spot.requiresEvidenceIds != null)
                foreach (var id in spot.requiresEvidenceIds)
                    if (!adapter.HasEvidence(id)) return;
            bool collected = adapter.UnlockEvidence(spot.evidenceId);
            statusMessage = collected
                ? "증거를 수사 수첩에 기록했습니다."
                : "이미 확보한 증거이거나 현재 획득할 수 없습니다.";
            Refresh();
        }

        private void ClearRows(List<GameObject> rows)
        {
            foreach (var row in rows) if (row != null) Destroy(row);
            rows.Clear();
        }

        private void Refresh()
        {
            if (window == null || adapter == null) return;
            ClearRows(locationRows);
            ClearRows(hotspotRows);

            if (!TryGetCatalog(out var catalog))
            {
                progress.text = adapter.CaseReady
                    ? "이 사건의 조사 장소 자료가 아직 완성되지 않았습니다."
                    : "사건 승인 전 · 조사 및 증거 획득 잠금";
                roomTitle.text = "조사 대기";
                roomDescription.text = "사건 내용을 확정한 후에 조사할 수 있습니다.";
                observationText.text = "아직 공개된 조사 지점이 없습니다.";
                feedbackText.text = "미승인 사건은 단서를 얻거나 범인을 지목할 수 없습니다.";
                examineButton.interactable = false;
                collectButton.interactable = false;
                return;
            }

            var evidence = adapter.DiscoveredEvidence();
            progress.text = "수사 수첩에 기록한 증거 " + evidence.Count
                + " / " + adapter.EvidenceTotal + "개";

            if (selectedLocation >= catalog.locations.Length) selectedLocation = -1;
            locationContent.sizeDelta = new Vector2(256,
                Mathf.Max(454, catalog.locations.Length * 48 + 10));
            for (int i = 0; i < catalog.locations.Length; i++)
            {
                int captured = i;
                var loc = catalog.locations[i];
                Image bg;
                var button = Action(locationContent, "Location_" + loc.id,
                    loc.title, new Vector2(0, -25 - i * 48), new Vector2(246, 42),
                    () => SelectLocation(captured), out bg, new Vector2(.5f, 1));
                bg.color = i == selectedLocation ? Accent : Surface;
                locationRows.Add(button.gameObject);
            }

            var selected = SelectedLocation(catalog);
            roomTitle.text = selected == null ? "장소를 선택하세요" : selected.title;
            roomDescription.text = selected == null
                ? "왼쪽 목록에서 조사할 장소를 선택하세요." : selected.description;
            if (selected == null)
            {
                selectedHotspot = -1;
                examinedSpotId = null;
            }
            else
            {
                if (selectedHotspot >= selected.spots.Length) selectedHotspot = -1;
                hotspotContent.sizeDelta = new Vector2(508,
                    Mathf.Max(162, selected.spots.Length * 47 + 8));
                for (int i = 0; i < selected.spots.Length; i++)
                {
                    int captured = i;
                    var spot = selected.spots[i];
                    bool acquired = !string.IsNullOrWhiteSpace(spot.evidenceId)
                        && adapter.HasEvidence(spot.evidenceId);
                    Image bg;
                    var button = Action(hotspotContent, "Spot_" + spot.id,
                        spot.label + (acquired ? "  ✓" : ""),
                        new Vector2(0, -24 - i * 47), new Vector2(498, 41),
                        () => SelectHotspot(captured), out bg, new Vector2(.5f, 1));
                    bg.color = i == selectedHotspot ? Accent : Surface;
                    hotspotRows.Add(button.gameObject);
                }
            }

            var current = SelectedHotspot(catalog);
            bool examined = current != null && examinedSpotId == current.id;
            observationText.text = current == null
                ? "조사할 지점을 선택해 주세요."
                : examined ? current.observation : "선택한 곳을 자세히 조사해 보세요.";
            examineButton.interactable = current != null;
            bool acquiredCurrent = current != null
                && !string.IsNullOrWhiteSpace(current.evidenceId)
                && adapter.HasEvidence(current.evidenceId);
            collectButton.interactable = examined && current != null
                && !string.IsNullOrWhiteSpace(current.evidenceId) && !acquiredCurrent;
            if (collectLabel != null)
                collectLabel.text = acquiredCurrent ? "확보 완료" : "수첩에 기록";
            feedbackText.text = statusMessage;
        }
    }
}
