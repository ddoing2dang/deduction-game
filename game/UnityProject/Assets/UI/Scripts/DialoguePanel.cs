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
        [Header("Optional approved dialogue adapter")]
        public UnityEvent<string> questionSubmitted = new UnityEvent<string>();

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

        public DialogueTheme Theme { get => theme; set { theme = value; ApplyPresentation(); } }
        public DialogueSpeaker Speaker { get => speaker; set { speaker = value; ApplyPresentation(); } }

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
            if (!presentationDirty) return;
            presentationDirty = false;
            ApplyPresentation();
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
            speakerName.text = speaker.ToString();
            RefreshPortrait();
            bool playing = Application.IsPlaying(gameObject);
            dialogueBody.text = playing
                ? (latestReplies.TryGetValue(CurrentContext, out var reply) ? reply : "")
                : previewText;
            if ((lastSpeaker != speaker || lastTheme != theme) && questionInput != null)
                questionInput.SetTextWithoutNotify("");
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
            Sprite selected = null;
            if (speakerPortraits != null)
            {
                for (int i = 0; i < speakerPortraits.Length; i++)
                {
                    if (speakerPortraits[i].speaker != speaker) continue;
                    selected = speakerPortraits[i].portrait;
                    break;
                }
            }

            npcPortrait.sprite = selected;
            npcPortrait.preserveAspect = true;
            npcPortrait.raycastTarget = false;
            npcPortrait.enabled = selected != null;

            // Reserve dialogue-body space only when an actual portrait has been assigned.
            // With no portrait, preserve the exact established theme-frame layout.
            var rect = dialogueBody.rectTransform;
            rect.anchoredPosition = selected != null ? new Vector2(-105, -38) : new Vector2(0, -38);
            rect.sizeDelta = selected != null ? new Vector2(1320, 224) : new Vector2(1560, 224);
        }

        private void OnInputSubmit(string _) => SubmitQuestion();
        public void SubmitQuestion()
        {
            if (!Application.IsPlaying(gameObject) || questionInput == null) return;
            var input = EventSystem.current != null ? EventSystem.current.currentInputModule?.input : null;
            if (input != null && !string.IsNullOrEmpty(input.compositionString)) return;
            string question = questionInput.text.Trim();
            if (question.Length == 0) return;
            AddEntry("탐정", question);
            questionInput.SetTextWithoutNotify("");
            statusText.text = "질문을 기록했습니다.";
            questionSubmitted.Invoke(question);
            questionInput.ActivateInputField();
        }

        // This is a rendering boundary. A future case adapter must enforce author approvals.
        public void ShowNpcReply(string approvedText)
        {
            if (!Application.IsPlaying(gameObject) || string.IsNullOrWhiteSpace(approvedText)) return;
            latestReplies[CurrentContext] = approvedText;
            dialogueBody.text = approvedText;
            if (statusText != null) statusText.text = "";
            AddEntry(speaker.ToString(), approvedText);
        }

        // For unapproved/missing case data: never impersonate an NPC.
        public void ShowInterrogationNotice(string notice)
        {
            if (!Application.IsPlaying(gameObject) || string.IsNullOrWhiteSpace(notice)) return;
            if (statusText != null) statusText.text = "승인된 진술 없음 · 대화 기록을 확인하세요.";
            AddEntry("안내", notice.Trim());
        }

        private void AddEntry(string who, string text)
        {
            var context = CurrentContext;
            if (!histories.TryGetValue(context, out var entries))
                histories[context] = entries = new List<Entry>();
            entries.Add(new Entry { who = who, text = text });
            RefreshHistory();
        }
        private void RefreshHistory()
        {
            if (historyText == null) return;
            var result = new StringBuilder();
            if (histories.TryGetValue(CurrentContext, out var entries))
                foreach (var entry in entries) result.Append(entry.who).Append(" · ").AppendLine(entry.text);
            historyText.text = result.Length == 0 ? "아직 대화 기록이 없습니다." : result.ToString();
            if (Application.IsPlaying(gameObject) && historyScroll != null)
            { Canvas.ForceUpdateCanvases(); historyScroll.verticalNormalizedPosition = 0; }
        }
    }
}
