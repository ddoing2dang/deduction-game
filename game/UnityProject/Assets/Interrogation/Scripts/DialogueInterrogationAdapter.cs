using System;
using System.Collections.Generic;
using Mystery.UI;
using UnityEngine;

namespace Mystery.Interrogation
{
    [DisallowMultipleComponent, RequireComponent(typeof(DialoguePanel))]
    public sealed class DialogueInterrogationAdapter : MonoBehaviour
    {
        [Header("Approved JSON case files, by theme")]
        [SerializeField] private TextAsset mansionCase;
        [SerializeField] private TextAsset cruiseCase;
        [SerializeField] private TextAsset musicBroadcastCase;

        private DialoguePanel panel;
        private readonly Dictionary<DialogueTheme, CaseFile> files = new Dictionary<DialogueTheme, CaseFile>();
        private readonly Dictionary<string, HashSet<string>> collected
            = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;
            panel = GetComponent<DialoguePanel>();
            Read(DialogueTheme.MANSION, mansionCase);
            Read(DialogueTheme.CRUISE, cruiseCase);
            Read(DialogueTheme.MUSIC_BROADCAST, musicBroadcastCase);
            if (panel != null) panel.questionSubmitted.AddListener(HandleQuestion);
        }

        private void OnDisable()
        {
            if (panel != null) panel.questionSubmitted.RemoveListener(HandleQuestion);
        }

        private void Read(DialogueTheme theme, TextAsset asset)
        {
            files.Remove(theme);
            if (asset == null) return;
            try
            {
                var parsed = JsonUtility.FromJson<CaseFile>(asset.text);
                if (parsed == null || parsed.theme != (int)theme)
                {
                    Debug.LogWarning("[mystery] Case theme mismatch: " + asset.name);
                    return;
                }
                files[theme] = parsed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[mystery] Invalid case JSON: " + asset.name + ": " + ex.Message);
            }
        }

        private void HandleQuestion(string question)
        {
            if (!Application.IsPlaying(gameObject) || panel == null) return;
            files.TryGetValue(panel.Theme, out var file);
            var reply = InterrogationResolver.Answer(file, panel.Theme, panel.Speaker,
                question, GetEvidence(file));
            if (reply.IsNpcSpeech) panel.ShowNpcReply(reply.Text);
            else panel.ShowInterrogationNotice(reply.Text);
        }

        // To be called by verified evidence discovery gameplay, not player text.
        public bool UnlockEvidence(string id)
        {
            if (!Application.IsPlaying(gameObject) || panel == null
                || !files.TryGetValue(panel.Theme, out var file) || !file.approved
                || !InterrogationResolver.KnownEvidence(file, id)) return false;
            return GetOrCreate(file.caseId).Add(id);
        }

        public bool HasEvidence(string id)
        {
            return panel != null && files.TryGetValue(panel.Theme, out var file)
                && GetEvidence(file).Contains(id);
        }

        public void ClearSessionEvidence()
        {
            if (Application.IsPlaying(gameObject)) collected.Clear();
        }

        private ISet<string> GetEvidence(CaseFile file)
        {
            return file == null || string.IsNullOrWhiteSpace(file.caseId)
                ? new HashSet<string>(StringComparer.Ordinal) : GetOrCreate(file.caseId);
        }

        private HashSet<string> GetOrCreate(string id)
        {
            if (!collected.TryGetValue(id, out var found))
                collected[id] = found = new HashSet<string>(StringComparer.Ordinal);
            return found;
        }
    }
}
