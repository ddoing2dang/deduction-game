using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deduction.Dialogue
{
    public sealed class DialoguePrototypeView : MonoBehaviour
    {
        [Serializable] public sealed class ThemeFrame { public string themeId; public Sprite frame; }
        public Image frameImage;
        public TMP_Text npcName, body, historyText, statusText;
        public TMP_InputField questionInput;
        public ScrollRect historyScroll;
        public Button sendButton;
        public Button[] npcButtons, themeButtons;
        public ThemeFrame[] themes;
        public TextAsset npcProfilesJson;
        public string initialThemeId = "HOTEL";
        private NpcProfiles profiles;
        private DialogueJournal journal = new DialogueJournal();
        private IApprovedDialogueSource source;
        private string activeNpc = "MATTO", activeCase = "UI_PROTOTYPE";
        private string SavePath => Path.Combine(Application.persistentDataPath, "dialogue-journal.v1.json");

        private void Awake()
        {
            profiles = JsonUtility.FromJson<NpcProfiles>(npcProfilesJson.text);
            try
            {
                if (File.Exists(SavePath)) journal = JsonUtility.FromJson<DialogueJournal>(File.ReadAllText(SavePath));
                if (journal == null || journal.schemaVersion != 1 || journal.entries == null) journal = new DialogueJournal();
                journal.entries.RemoveAll(e => e == null || !NpcDirectory.Contains(e.npcId));
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            { journal = new DialogueJournal(); statusText.text = "기록을 읽지 못했습니다. 이번 대화는 새 기록으로 시작합니다."; }
            for (int i = 0; i < npcButtons.Length; i++)
            {
                string id = profiles.characters[i].npcId;
                npcButtons[i].onClick.AddListener(() => SelectNpc(id));
            }
            for (int i = 0; i < themeButtons.Length; i++)
            {
                string id = themes[i].themeId;
                themeButtons[i].onClick.AddListener(() => SetTheme(id));
            }
            sendButton.onClick.AddListener(SubmitQuestion);
            questionInput.onSubmit.AddListener(_ => { if (!Input.compositionString.Any()) SubmitQuestion(); });
            SetTheme(initialThemeId);
            SelectNpc(activeNpc);
        }

        // Case adapters are intentionally not supplied with this UI-only prototype.
        public void BindValidatedCase(IApprovedDialogueSource authority)
        {
            if (authority == null || !authority.CaseValidated || !NpcDirectory.Contains(authority.CulpritNpcId)
                || string.IsNullOrWhiteSpace(authority.CaseId)) throw new ArgumentException("Validated case authority required.");
            source = authority;
            activeCase = authority.CaseId;
            SelectNpc(activeNpc);
        }

        public void SetTheme(string themeId)
        {
            var selected = Array.Find(themes, t => t.themeId == themeId);
            if (selected == null || selected.frame == null) return;
            frameImage.sprite = selected.frame;
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
            // Only the visual frame changes. Case and journal remain authoritative.
        }

        public void SelectNpc(string npcId)
        {
            var selected = Array.Find(profiles.characters, p => p.npcId == npcId);
            if (selected == null) return;
            activeNpc = npcId;
            npcName.text = selected.displayName;
            body.text = journal.entries.LastOrDefault(e => e.caseId == activeCase && e.npcId == activeNpc && e.role == "NPC")?.text ?? "";
            statusText.text = source == null ? "질문과 대화 기록을 연결했습니다. 승인된 사건 대사 연결은 준비 중입니다." : "자유롭게 질문해 주세요.";
            RefreshHistory();
        }

        public void SubmitQuestion()
        {
            string question = questionInput.text.Trim();
            if (question.Length == 0 || !string.IsNullOrEmpty(Input.compositionString)) return;
            Append("PLAYER", question);
            questionInput.text = "";
            var context = journal.entries.Where(e => e.caseId == activeCase && e.npcId == activeNpc).ToArray();
            if (source != null && source.TrySelect(activeNpc, question, context, out var line)
                && DialoguePolicy.Allows(source, activeNpc, line))
            {
                body.text = line.text;
                Append("NPC", line.text, line.lineId);
                statusText.text = "";
            }
            else
            {
                const string message = "승인된 응답을 연결하지 못했습니다. 질문은 기록에 보관했습니다.";
                statusText.text = message;
                Append("SYSTEM", message);
            }
            Save();
            RefreshHistory();
            questionInput.ActivateInputField();
        }

        private void Append(string role, string text, string lineId = "") => journal.entries.Add(new DialogueEntry
        { caseId = activeCase, npcId = activeNpc, role = role, text = text, lineId = lineId, utc = DateTime.UtcNow.ToString("O") });

        private void RefreshHistory()
        {
            var text = new StringBuilder();
            foreach (var entry in journal.entries.Where(e => e.caseId == activeCase && e.npcId == activeNpc))
                text.Append(entry.role == "PLAYER" ? "탐정" : entry.role == "NPC" ? npcName.text : "안내").Append(" · ").AppendLine(entry.text);
            historyText.text = text.Length == 0 ? "아직 대화 기록이 없습니다." : text.ToString();
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0;
        }

        private void Save()
        {
            try
            {
                string staging = SavePath + ".tmp";
                File.WriteAllText(staging, JsonUtility.ToJson(journal, true));
                if (File.Exists(SavePath)) File.Replace(staging, SavePath, SavePath + ".bak");
                else File.Move(staging, SavePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { statusText.text = "기록을 파일에 저장하지 못했습니다. 현재 세션 기록은 유지됩니다."; }
        }
    }
}
