using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mystery.UI
{
    public enum DialogueTheme { MANSION, CRUISE, MUSIC_BROADCAST }
    public enum DialogueSpeaker { 마도연, 마루희, 나주은, 이복희, 진루나, 문일혜 }

    /// <summary>Shared presentation only. No case data or generated NPC answers.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class DialoguePanel : MonoBehaviour
    {
        [Header("Inspector presentation")]
        [SerializeField] private DialogueTheme theme = DialogueTheme.MANSION;
        [SerializeField] private DialogueSpeaker speaker = DialogueSpeaker.마도연;
        [TextArea(2, 5)] [SerializeField] private string previewText = "대사 미리보기입니다.\n실제 NPC 대사는 별도로 연결합니다.";
        [Header("Original frames — no slicing or tint")]
        [SerializeField] private Sprite mansionFrame;
        [SerializeField] private Sprite cruiseFrame;
        [SerializeField] private Sprite musicBroadcastFrame;
        [Header("Shared UI references")]
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text speakerName;
        [SerializeField] private TMP_Text dialogueBody;
        [SerializeField] private TMP_InputField questionInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect historyScroll;
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private TMP_Text statusText;
        [Header("Optional portraits (never synthesize missing NPC art)")]
        [SerializeField] private Image npcPortrait;
        [Serializable]
        public struct SpeakerPortrait
        {
            public DialogueSpeaker speaker;
            public Sprite portrait;
        }
        [SerializeField] private SpeakerPortrait[] speakerPortraits = Array.Empty<SpeakerPortrait>();
        // HOTEL / Rose Mansion uses its own approved portraits. Never overwrite
        // the shared portraits belonging to other themes.
        [SerializeField] private SpeakerPortrait[] mansionPortraits = Array.Empty<SpeakerPortrait>();
        [Header("Optional approved dialogue adapter")]
        public UnityEvent<string> questionSubmitted = new UnityEvent<string>();

        [Serializable]
        public sealed class TranscriptEntry { public string who; public string text; }
        [Serializable]
        public sealed class SpeakerTranscript
        {
            public int speaker;
            public TranscriptEntry[] entries = Array.Empty<TranscriptEntry>();
            public string latestReply;
        }

        public event Action DialogueChanged;

        private sealed class Entry { public string who, text; }
        // Histories and last replies are scoped to the CASE THEME and NPC.
        private readonly struct ContextKey : IEquatable<ContextKey>
        {
            public readonly DialogueTheme theme;
            public readonly DialogueSpeaker speaker;
            public ContextKey(DialogueTheme theme, DialogueSpeaker speaker)
            { this.theme = theme; this.speaker = speaker; }
            public bool Equals(ContextKey other) => theme == other.theme && speaker == other.speaker;
            public override bool Equals(object obj) => obj is ContextKey other && Equals(other);
            public override int GetHashCode() => unchecked(((int)theme * 397) ^ (int)speaker);
        }
        private ContextKey CurrentContext => new ContextKey(theme, speaker);
        private readonly Dictionary<ContextKey, List<Entry>> histories
            = new Dictionary<ContextKey, List<Entry>>();
        private readonly Dictionary<ContextKey, string> latestReplies
            = new Dictionary<ContextKey, string>();
        private bool presentationDirty;
        private DialogueSpeaker lastSpeaker;
        private DialogueTheme lastTheme;
        // The player question stays visible briefly before the synchronous
        // rule-based adapter's reply is revealed. This makes BOTH turns visible.
        private bool hasQueuedReply;
        private string queuedWho;
        private string queuedText;
        private float revealAt;
        private ContextKey queuedContext;

        public DialogueTheme Theme { get => theme; set { theme = value; ApplyPresentation(); } }
        public DialogueSpeaker Speaker { get => speaker; set { speaker = value; ApplyPresentation(); } }

        // The cinematic presenter reads existing serialized UI references,
        // without changing any NPC sprite, font or original dialogue frame.
        public TMP_Text SpeakerNameLabel => speakerName;
        public TMP_Text NpcDialogueLabel => dialogueBody;
        public TMP_Text HistoryLabel => historyText;
        public TMP_Text HistoryHeadingLabel =>
            transform.Find("HistoryTitle")?.GetComponent<TMP_Text>();
        public TMP_Text StatusLabel => statusText;
        public TMP_InputField InputField => questionInput;

        public bool UseExternalPortraits { get; set; }

        public Sprite GetPortraitFor(DialogueSpeaker target)
        {
            // Missing HOTEL artwork stays blank rather than showing a different
            // theme's portrait for the same logical NPC ID.
            var entries = theme == DialogueTheme.MANSION
                ? mansionPortraits : speakerPortraits;
            if (entries == null) return null;
            foreach (var item in entries)
                if (item.speaker == target) return item.portrait;
            return null;
        }

        // UI-only display names; DialogueSpeaker numeric values stay unchanged
        // so previously serialized scenes, saves and case data remain compatible.
        public static string DisplayNameFor(DialogueSpeaker target)
        {
            return (int)target switch
            {
                0 => "마도은",
                1 => "마연희",
                2 => "나지수",
                3 => "김보희",
                4 => "진소율",
                5 => "문이현",
                _ => target.ToString()
            };
        }

        private void OnEnable()
        {
            if (Application.IsPlaying(gameObject))
            {
                sendButton.onClick.AddListener(SubmitQuestion);
                questionInput.onSubmit.AddListener(OnInputSubmit);
            }
            ApplyPresentation();
        }
        private void OnDisable()
        {
            if (sendButton != null) sendButton.onClick.RemoveListener(SubmitQuestion);
            if (questionInput != null) questionInput.onSubmit.RemoveListener(OnInputSubmit);
        }
        private void OnValidate() => presentationDirty = true;
        private void Update()
        {
            if (presentationDirty)
            {
                presentationDirty = false;
                ApplyPresentation();
            }
            if (Application.IsPlaying(gameObject) && hasQueuedReply
                && Time.unscaledTime >= revealAt)
                RevealQueuedReply();
        }

        public void ApplyPresentation()
        {
            if (frame == null || speakerName == null || dialogueBody == null) return;
            frame.sprite = theme switch
            {
                DialogueTheme.CRUISE => cruiseFrame,
                DialogueTheme.MUSIC_BROADCAST => musicBroadcastFrame,
                _ => mansionFrame
            };
            frame.type = Image.Type.Simple;
            frame.color = Color.white;
            frame.preserveAspect = true;
            RefreshPortrait();
            bool playing = Application.IsPlaying(gameObject);
            bool contextChanged = lastSpeaker != speaker || lastTheme != theme;
            if (contextChanged)
            {
                hasQueuedReply = false;
                if (questionInput != null)
                    questionInput.SetTextWithoutNotify("");
            }

            if (!playing)
                SetCurrentLine(DisplayNameFor(speaker), previewText);
            else if (hasQueuedReply && queuedContext.Equals(CurrentContext))
            {
                // Keep the player's last question visible until its reply's turn.
                if (histories.TryGetValue(CurrentContext, out var pending)
                    && pending.Count >= 2)
                    SetCurrentLine(pending[pending.Count - 2].who,
                        pending[pending.Count - 2].text);
                else
                    ShowLastLine();
            }
            else
                ShowLastLine();

            lastSpeaker = speaker;
            lastTheme = theme;
            RefreshHistory();
        }

        // Portrait art is assigned explicitly per NPC in the Inspector or by a later asset importer.
        // The slot stays invisible if its actual user-approved sprite is not present.
        public void SetSpeakerPortrait(DialogueSpeaker npc, Sprite sprite)
        {
            if (speakerPortraits == null) speakerPortraits = Array.Empty<SpeakerPortrait>();
            for (int i = 0; i < speakerPortraits.Length; i++)
            {
                if (speakerPortraits[i].speaker != npc) continue;
                speakerPortraits[i].portrait = sprite;
                ApplyPresentation();
                return;
            }
            int count = speakerPortraits.Length;
            Array.Resize(ref speakerPortraits, count + 1);
            speakerPortraits[count] = new SpeakerPortrait { speaker = npc, portrait = sprite };
            ApplyPresentation();
        }

        private void RefreshPortrait()
        {
            if (npcPortrait == null || dialogueBody == null) return;
            Sprite selected = GetPortraitFor(speaker);

            npcPortrait.sprite = selected;
            npcPortrait.preserveAspect = true;
            npcPortrait.raycastTarget = false;
            npcPortrait.enabled = selected != null && !UseExternalPortraits;

            // Cinematic mode handles text layout and portraits separately.
            // Switching NPCs must not undo its responsive layout.
            if (UseExternalPortraits) return;

            // Legacy layout remains available when cinematic mode is disabled.
            var rect = dialogueBody.rectTransform;
            rect.anchoredPosition = selected != null ? new Vector2(-105, -38) : new Vector2(0, -38);
            rect.sizeDelta = selected != null ? new Vector2(1320, 224) : new Vector2(1560, 224);
        }

        private void SetCurrentLine(string who, string line)
        {
            if (speakerName != null) speakerName.text = who ?? "";
            if (dialogueBody != null) dialogueBody.text = line ?? "";
        }

        private void ShowLastLine()
        {
            if (histories.TryGetValue(CurrentContext, out var entries)
                && entries.Count > 0)
            {
                var last = entries[entries.Count - 1];
                SetCurrentLine(last.who, last.text);
            }
            else if (latestReplies.TryGetValue(CurrentContext, out var lastReply))
                SetCurrentLine(DisplayNameFor(speaker), lastReply);
            else
                SetCurrentLine(DisplayNameFor(speaker), "");
        }

        private void QueueOrShowReply(string who, string line)
        {
            if (hasQueuedReply && queuedContext.Equals(CurrentContext))
            {
                queuedWho = who;
                queuedText = line;
                // Preserve the player's full line for at least a moment
                // even when the adapter answers within the same frame.
                revealAt = Mathf.Max(revealAt, Time.unscaledTime + 0.8f);
            }
            else
                SetCurrentLine(who, line);
        }

        private void RevealQueuedReply()
        {
            if (!hasQueuedReply) return;
            hasQueuedReply = false;
            if (queuedContext.Equals(CurrentContext) && !string.IsNullOrEmpty(queuedWho))
                SetCurrentLine(queuedWho, queuedText);
            // If no adapter answered, leave the player's question on screen.
            queuedWho = queuedText = null;
        }

        private void OnInputSubmit(string _) => SubmitQuestion();
        public void SubmitQuestion()
        {
            if (!Application.IsPlaying(gameObject) || questionInput == null) return;
            var input = EventSystem.current != null ? EventSystem.current.currentInputModule?.input : null;
            if (input != null && !string.IsNullOrEmpty(input.compositionString)) return;
            string question = questionInput.text.Trim();
            if (question.Length == 0) return;
            hasQueuedReply = true;
            queuedContext = CurrentContext;
            queuedWho = queuedText = null;
            revealAt = Time.unscaledTime + 0.8f;
            SetCurrentLine("탐정", question);
            AddEntry("탐정", question);
            questionInput.SetTextWithoutNotify("");
            if (statusText != null) statusText.text = "질문을 기록했습니다.";
            questionSubmitted.Invoke(question);
            // The current interrogation resolver is synchronous. If no adapter
            // handled this event, never leave the player's question stalled.
            if (hasQueuedReply && string.IsNullOrEmpty(queuedWho))
            {
                var adapter = GetComponent<Mystery.Interrogation.DialogueInterrogationAdapter>();
                if (adapter == null || !adapter.CaseReady)
                    ShowInterrogationNotice("사건 설정이 아직 확정되지 않아 진술을 제공할 수 없습니다.");
                else
                    ShowInterrogationNotice("응답 연결이 확인되지 않았습니다. 심문 설정을 점검해 주세요.");
            }
            questionInput.ActivateInputField();
        }

        // This is a rendering boundary. A future case adapter must enforce author approvals.
        public void ShowNpcReply(string approvedText)
        {
            if (!Application.IsPlaying(gameObject) || string.IsNullOrWhiteSpace(approvedText)) return;
            latestReplies[CurrentContext] = approvedText;
            if (statusText != null) statusText.text = "";
            AddEntry(DisplayNameFor(speaker), approvedText);
            QueueOrShowReply(DisplayNameFor(speaker), approvedText);
        }

        // For unapproved/missing case data: never impersonate an NPC.
        public void ShowInterrogationNotice(string notice)
        {
            if (!Application.IsPlaying(gameObject) || string.IsNullOrWhiteSpace(notice)) return;
            string message = notice.Trim();
            if (statusText != null) statusText.text = "승인된 진술 없음 · 대화 기록을 확인하세요.";
            AddEntry("안내", message);
            QueueOrShowReply("안내", message);
        }

        private void AddEntry(string who, string text)
        {
            var context = CurrentContext;
            if (!histories.TryGetValue(context, out var entries))
                histories[context] = entries = new List<Entry>();
            entries.Add(new Entry { who = who, text = text });
            RefreshHistory();
            DialogueChanged?.Invoke();
        }

        // Snapshot only a single case theme. Every NPC remains in its own thread.
        public SpeakerTranscript[] ExportTranscripts(DialogueTheme target)
        {
            var result = new List<SpeakerTranscript>();
            for (int i = 0; i < 6; i++)
            {
                var key = new ContextKey(target, (DialogueSpeaker)i);
                histories.TryGetValue(key, out var entries);
                latestReplies.TryGetValue(key, out var reply);
                if ((entries == null || entries.Count == 0) && string.IsNullOrEmpty(reply))
                    continue;
                var copy = new List<TranscriptEntry>();
                if (entries != null)
                    foreach (var entry in entries)
                        copy.Add(new TranscriptEntry { who = entry.who, text = entry.text });
                result.Add(new SpeakerTranscript
                {
                    speaker = i,
                    entries = copy.ToArray(),
                    latestReply = reply ?? ""
                });
            }
            return result.ToArray();
        }

        // Validation happens before either dialogue or evidence state is changed.
        public static bool ValidateTranscripts(SpeakerTranscript[] threads)
        {
            if (threads == null || threads.Length > 6) return false;
            var unique = new HashSet<int>();
            foreach (var thread in threads)
            {
                if (thread == null || thread.speaker < 0 || thread.speaker > 5
                    || !unique.Add(thread.speaker)
                    || thread.entries == null || thread.entries.Length > 500
                    || (thread.latestReply != null && thread.latestReply.Length > 5000))
                    return false;
                foreach (var item in thread.entries)
                    if (item == null || string.IsNullOrWhiteSpace(item.who)
                        || item.who.Length > 100 || item.text == null
                        || item.text.Length > 5000) return false;
            }
            return true;
        }

        public bool RestoreTranscripts(DialogueTheme target, SpeakerTranscript[] threads)
        {
            if (!Application.IsPlaying(gameObject) || !ValidateTranscripts(threads))
                return false;
            for (int i = 0; i < 6; i++)
            {
                var key = new ContextKey(target, (DialogueSpeaker)i);
                histories.Remove(key);
                latestReplies.Remove(key);
            }
            foreach (var thread in threads)
            {
                var key = new ContextKey(target, (DialogueSpeaker)thread.speaker);
                var entries = new List<Entry>();
                foreach (var item in thread.entries)
                    entries.Add(new Entry { who = item.who, text = item.text });
                if (entries.Count != 0) histories[key] = entries;
                if (!string.IsNullOrEmpty(thread.latestReply))
                    latestReplies[key] = thread.latestReply;
            }
            hasQueuedReply = false;
            queuedWho = queuedText = null;
            if (theme == target) ApplyPresentation();
            DialogueChanged?.Invoke();
            return true;
        }

        public void ReflowHistory()
        {
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            if (historyText == null) return;
            var result = new StringBuilder();
            if (histories.TryGetValue(CurrentContext, out var entries))
                foreach (var entry in entries)
                    result.Append(entry.who).Append(" · ").AppendLine(entry.text);
            historyText.text = result.Length == 0
                ? "아직 대화 기록이 없습니다." : result.ToString();

            if (!Application.IsPlaying(gameObject) || historyScroll == null) return;
            // Keep all messages scrollable instead of squeezing long transcripts
            // into the fixed-height log panel.
            var content = historyScroll.content;
            if (content != null)
            {
                float width = Mathf.Max(120f, historyText.rectTransform.rect.width);
                var preferred = historyText.GetPreferredValues(historyText.text, width, 32768f);
                float viewportHeight = historyScroll.viewport != null
                    ? historyScroll.viewport.rect.height : 180f;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(viewportHeight, preferred.y + 20f));
            }
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0f;
        }
    }
}
