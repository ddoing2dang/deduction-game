using System;
using System.Collections.Generic;
using System.IO;
using Mystery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mystery.Interrogation
{
    /// <summary>
    /// Save/load only author-approved cases. Auto-saves after new dialogue,
    /// collected evidence, or verdict decisions. Windows persistentDataPath.
    /// Does not alter existing prefab art or editor assets.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DialogueInterrogationAdapter))]
    public sealed class CaseProgressSaveController : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset headingFont;

        private readonly HashSet<string> attemptedAutoLoads =
            new HashSet<string>(StringComparer.Ordinal);
        private DialogueInterrogationAdapter adapter;
        private DialoguePanel dialogue;
        private GameObject openTab;
        private GameObject menu;
        private Button saveButton;
        private Button loadButton;
        private TMP_Text progressText;
        private TMP_Text statusText;
        private bool restoring;
        private bool started;
        private string lastKey = "";
        private bool lastReady;

        private static readonly Color Background = new Color32(31, 44, 53, 249);
        private static readonly Color Surface = new Color32(60, 79, 88, 255);
        private static readonly Color Light = new Color32(248, 237, 217, 255);

        private string StorageDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, "mystery-case-saves"); }
        }

        private string CurrentKey()
        {
            var file = adapter != null ? adapter.CurrentCase : null;
            return file == null ? "" : file.theme + ":" + file.caseId;
        }

        // Filename depends ONLY on the known enum value, not untrusted JSON strings.
        private string CurrentSaveFile()
        {
            return Path.Combine(StorageDirectory, "case_" + (int)adapter.CurrentTheme + ".json");
        }

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            adapter = GetComponent<DialogueInterrogationAdapter>();
            dialogue = GetComponent<DialoguePanel>();
            if (adapter == null || dialogue == null) return;
            adapter.ProgressChanged += HandleProgressChanged;
            dialogue.DialogueChanged += HandleProgressChanged;
            Build();
        }

        private void OnDisable()
        {
            if (adapter != null) adapter.ProgressChanged -= HandleProgressChanged;
            if (dialogue != null) dialogue.DialogueChanged -= HandleProgressChanged;
            if (Application.IsPlaying(gameObject))
            {
                if (openTab != null) Destroy(openTab);
                if (menu != null) Destroy(menu);
            }
            openTab = null;
            menu = null;
            started = false;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || adapter == null) return;
            if (!started)
            {
                started = true;
                lastKey = CurrentKey();
                lastReady = adapter.CaseReady;
                if (lastReady) LoadCurrent(true);
                Refresh();
                return;
            }
            string key = CurrentKey();
            bool ready = adapter.CaseReady;
            if (key == lastKey && ready == lastReady) return;
            lastKey = key;
            lastReady = ready;
            if (ready) LoadCurrent(true);
            else SetStatus("사건 승인 전에는 진행 데이터를 저장하거나 불러오지 않습니다.");
            Refresh();
        }

        private void HandleProgressChanged()
        {
            if (!started || restoring) return;
            SaveCurrent(true);
            Refresh();
        }

        // Manual Save/Load is also callable from other UI workflows.
        public bool SaveCurrent(bool automatic = false)
        {
            if (!Application.IsPlaying(gameObject) || adapter == null || dialogue == null
                || !adapter.CaseReady || restoring)
            {
                if (!automatic) SetStatus("승인된 사건에서만 저장할 수 있습니다.");
                return false;
            }
            var record = adapter.ExportProgress();
            if (record == null) return false;
            record.conversations = dialogue.ExportTranscripts(adapter.CurrentTheme);
            string path = CurrentSaveFile();
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(StorageDirectory);
                File.WriteAllText(temp, JsonUtility.ToJson(record, true));
                // On Windows this replaces the destination atomically and keeps
                // the previous file as a backup in case of corruption.
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
                else File.Move(temp, path);
                SetStatus((automatic ? "자동 저장" : "저장 완료")
                    + " · " + DateTime.Now.ToString("HH:mm:ss"));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[mystery saves] Save failed: " + ex.Message);
                SetStatus("저장에 실패했습니다. 게임 저장 폴더에 쓸 수 있는지 확인해 주세요.");
                return false;
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { /* keep the user's original save intact */ }
            }
        }

        public bool LoadCurrent(bool automatic = false)
        {
            if (!Application.IsPlaying(gameObject) || adapter == null || dialogue == null
                || !adapter.CaseReady || restoring)
            {
                if (!automatic) SetStatus("승인된 사건에서만 불러올 수 있습니다.");
                return false;
            }
            string key = CurrentKey();
            if (automatic && !attemptedAutoLoads.Add(key)) return false;
            string path = CurrentSaveFile();
            if (!File.Exists(path))
            {
                if (!automatic) SetStatus("현재 사건에 저장된 진행 기록이 없습니다.");
                return false;
            }
            try
            {
                var data = JsonUtility.FromJson<CaseProgressSave>(File.ReadAllText(path));
                if (data == null || !adapter.ValidateProgress(data)
                    || !DialoguePanel.ValidateTranscripts(data.conversations))
                {
                    SetStatus("저장된 사건 정보가 현재 사건과 달라 불러오지 않았습니다.");
                    return false;
                }

                restoring = true;
                try
                {
                    // Both payloads were validated before any live state mutation.
                    if (!adapter.RestoreProgress(data)
                        || !dialogue.RestoreTranscripts(adapter.CurrentTheme, data.conversations))
                        throw new InvalidOperationException("Validated save could not be restored.");
                }
                finally { restoring = false; }

                attemptedAutoLoads.Add(key);
                SetStatus("진행 기록을 불러왔습니다.");
                Refresh();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[mystery saves] Load failed: " + ex.Message);
                SetStatus("저장 파일을 읽을 수 없습니다. 기존 진행 상태를 유지해 주세요.");
                return false;
            }
        }

        private static RectTransform Rect(string id, Transform parent,
            Vector2 size, Vector2 pos, Vector2 anchor, Vector2 pivot)
        {
            var obj = new GameObject(id, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
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
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = (heading ? headingFont : regularFont) ?? TMP_Settings.defaultFontAsset;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = fontSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.color = Light;
            label.text = value;
            return label;
        }

        private Button Button(Transform parent, string id, string caption,
            Vector2 pos, Vector2 size, Action callback)
        {
            var rect = Rect(id, parent, size, pos, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Surface;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text(rect, "Label", caption, Vector2.zero,
                new Vector2(size.x - 10, size.y - 4), 20, true);
            button.onClick.AddListener(() => callback());
            return button;
        }

        private void Build()
        {
            var tab = Button(transform, "SaveMenuTab", "진행 저장",
                Vector2.zero, new Vector2(170, 46), ToggleMenu);
            var tabRect = tab.GetComponent<RectTransform>();
            tabRect.anchorMin = tabRect.anchorMax = Vector2.one;
            tabRect.pivot = Vector2.one;
            tabRect.anchoredPosition = new Vector2(-378, -14);
            openTab = tab.gameObject;

            var root = Rect("ProgressSaveMenu", transform, new Vector2(478, 330),
                new Vector2(-18, -74), Vector2.one, Vector2.one);
            var background = root.gameObject.AddComponent<Image>();
            background.color = Background;
            menu = root.gameObject;
            Text(root, "Heading", "진행 기록", new Vector2(0, 128),
                new Vector2(440, 43), 28, true);
            progressText = Text(root, "ProgressInfo", "",
                new Vector2(0, 65), new Vector2(430, 76), 20);
            saveButton = Button(root, "SaveNow", "지금 저장",
                new Vector2(-113, -25), new Vector2(216, 52),
                () => SaveCurrent());
            loadButton = Button(root, "LoadNow", "불러오기",
                new Vector2(113, -25), new Vector2(216, 52),
                () => LoadCurrent());
            statusText = Text(root, "SaveStatus", "",
                new Vector2(0, -100), new Vector2(434, 70), 17);
            Button(root, "Close", "닫기", new Vector2(195, 129),
                new Vector2(78, 40), HideWindow);
            menu.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (menu == null) return;
            bool opening = !menu.activeSelf;
            if (opening)
            {
                var notebook = GetComponent<InvestigationNotebookView>();
                if (notebook != null) notebook.HideWindow();
                var investigation = GetComponent<InvestigationSceneView>();
                if (investigation != null) investigation.HideWindow();
                var conclusion = GetComponent<CaseConclusionView>();
                if (conclusion != null) conclusion.HideWindow();
            }
            menu.SetActive(opening);
            if (opening) Refresh();
        }

        public void HideWindow()
        {
            if (menu != null) menu.SetActive(false);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void Refresh()
        {
            if (adapter == null || menu == null) return;
            bool ready = adapter.CaseReady;
            progressText.text = ready
                ? "증거 " + adapter.DiscoveredEvidence().Count + "/" + adapter.EvidenceTotal
                    + " · 남은 지목 " + adapter.RemainingAccusations
                    + "회 · " + (adapter.CaseSolved ? "사건 해결" : "수사 중")
                : "사건 초안 · 승인 전";
            saveButton.interactable = ready;
            loadButton.interactable = ready;
        }
    }
}
