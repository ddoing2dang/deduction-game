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
    public enum DialogueSpeaker { 마도아, 마리예, 나주아, 복희, 지로아, 문이린 }

    /// <summary>Shared presentation only. No case data or generated NPC answers.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class DialoguePanel : MonoBehaviour
    {
        [Header("Inspector presentation")]
        [SerializeField] private DialogueTheme theme = DialogueTheme.MANSION;
        [SerializeField] private DialogueSpeaker speaker = DialogueSpeaker.마도아;
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
        [Header("Optional approved dialogue adapter")]
        public UnityEvent<string> questionSubmitted = new UnityEvent<string>();

        private sealed class Entry { public string who, text; }
        private readonly Dictionary<DialogueSpeaker, List<Entry>> histories = new Dictionary<DialogueSpeaker, List<Entry>>();
        private readonly Dictionary<DialogueSpeaker, string> latestReplies = new Dictionary<DialogueSpeaker, string>();
        private bool presentationDirty;
        private DialogueSpeaker lastSpeaker;

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
            bool playing = Application.IsPlaying(gameObject);
            dialogueBody.text = playing ? (latestReplies.TryGetValue(speaker, out var reply) ? reply : "") : previewText;
            if (lastSpeaker != speaker && questionInput != null) questionInput.SetTextWithoutNotify("");
            lastSpeaker = speaker;
            RefreshHistory();
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
            latestReplies[speaker] = approvedText;
            dialogueBody.text = approvedText;
            statusText.text = "";
            AddEntry(speaker.ToString(), approvedText);
        }
        private void AddEntry(string who, string text)
        {
            if (!histories.TryGetValue(speaker, out var entries)) histories[speaker] = entries = new List<Entry>();
            entries.Add(new Entry { who = who, text = text });
            RefreshHistory();
        }
        private void RefreshHistory()
        {
            if (historyText == null) return;
            var result = new StringBuilder();
            if (histories.TryGetValue(speaker, out var entries))
                foreach (var entry in entries) result.Append(entry.who).Append(" · ").AppendLine(entry.text);
            historyText.text = result.Length == 0 ? "아직 대화 기록이 없습니다." : result.ToString();
            if (Application.IsPlaying(gameObject) && historyScroll != null)
            { Canvas.ForceUpdateCanvases(); historyScroll.verticalNormalizedPosition = 0; }
        }
    }
}
