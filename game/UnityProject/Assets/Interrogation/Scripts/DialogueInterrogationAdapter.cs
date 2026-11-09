using System;
using System.Collections.Generic;
using Mystery.UI;
using UnityEngine;

namespace Mystery.Interrogation
{
    [DisallowMultipleComponent, RequireComponent(typeof(DialoguePanel))]
    public sealed class DialogueInterrogationAdapter : MonoBehaviour
    {
        [Header("Author-approved JSON case files, by theme")]
        [SerializeField] private TextAsset mansionCase;
        [SerializeField] private TextAsset cruiseCase;
        [SerializeField] private TextAsset musicBroadcastCase;

        public event Action EvidenceChanged;
        public event Action<VerdictOutcome> VerdictChanged;
        public event Action ProgressChanged;

        private DialoguePanel panel;
        private readonly Dictionary<DialogueTheme, CaseFile> files
            = new Dictionary<DialogueTheme, CaseFile>();
        private readonly Dictionary<string, HashSet<string>> collected
            = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> failedAccusations
            = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> solvedCases
            = new HashSet<string>(StringComparer.Ordinal);

        public DialogueTheme CurrentTheme => panel != null ? panel.Theme : DialogueTheme.MANSION;
        public CaseFile CurrentCase
        {
            get
            {
                files.TryGetValue(CurrentTheme, out var file);
                return file;
            }
        }
        public bool CaseReady
        {
            get
            {
                var file = CurrentCase;
                return file != null && file.approved
                    && InterrogationResolver.Validate(file, CurrentTheme) == null
                    && AccusationVerdictRules.ValidateVerdict(file) == null;
            }
        }
        public int EvidenceTotal => CurrentCase?.evidence?.Length ?? 0;
        public bool CaseSolved
        {
            get
            {
                var file = CurrentCase;
                return file != null && solvedCases.Contains(Key(file));
            }
        }
        public int RemainingAccusations
        {
            get
            {
                var file = CurrentCase;
                if (file == null || file.verdict == null) return 0;
                failedAccusations.TryGetValue(Key(file), out int wrong);
                return Math.Max(0, file.verdict.maxWrongAccusations - wrong);
            }
        }

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
            var file = CurrentCase;
            var reply = InterrogationResolver.Answer(
                file, panel.Theme, panel.Speaker, question, GetEvidence(file));
            if (reply.IsNpcSpeech) panel.ShowNpcReply(reply.Text);
            else panel.ShowInterrogationNotice(reply.Text);
        }

        // Evidence is granted only by approved interactive clue sources.
        // Text typed by a player is NOT an evidence source.
        public bool UnlockEvidence(string id)
        {
            if (!Application.IsPlaying(gameObject) || !CaseReady) return false;
            var file = CurrentCase;
            if (!InterrogationResolver.KnownEvidence(file, id)) return false;
            if (!GetOrCreate(Key(file)).Add(id)) return false;
            EvidenceChanged?.Invoke();
            ProgressChanged?.Invoke();
            return true;
        }

        public bool HasEvidence(string id)
        {
            var file = CurrentCase;
            return file != null && GetEvidence(file).Contains(id);
        }

        // Reveal only evidence the player actually acquired, never every case clue.
        public List<EvidenceDefinition> DiscoveredEvidence()
        {
            var result = new List<EvidenceDefinition>();
            var file = CurrentCase;
            if (file?.evidence == null) return result;
            var owned = GetEvidence(file);
            foreach (var clue in file.evidence)
                if (clue != null && owned.Contains(clue.id))
                    result.Add(clue);
            return result;
        }

        public VerdictOutcome Accuse(int suspectIndex, ICollection<string> presentedIds)
        {
            if (!Application.IsPlaying(gameObject) || !CaseReady)
                return new VerdictOutcome(VerdictState.NotReady,
                    "확정된 사건과 증거가 준비되기 전에는 범인을 지목할 수 없습니다.");
            var file = CurrentCase;
            string key = Key(file);
            if (solvedCases.Contains(key))
                return new VerdictOutcome(VerdictState.AlreadySolved, "이 사건은 이미 해결되었습니다.");

            failedAccusations.TryGetValue(key, out int wrong);
            if (wrong >= file.verdict.maxWrongAccusations)
                return new VerdictOutcome(VerdictState.Locked,
                    "지목 기회를 모두 사용했습니다. 사건을 처음부터 다시 진행해야 합니다.");

            var outcome = AccusationVerdictRules.Evaluate(
                file, CurrentTheme, suspectIndex, GetEvidence(file), presentedIds);
            if (outcome.State == VerdictState.Incorrect)
                failedAccusations[key] = wrong + 1;
            else if (outcome.State == VerdictState.Solved)
                solvedCases.Add(key);
            if (outcome.State == VerdictState.Incorrect || outcome.State == VerdictState.Solved)
            {
                ProgressChanged?.Invoke();
                VerdictChanged?.Invoke(outcome);
            }
            return outcome;
        }

        public void ClearSessionEvidence()
        {
            if (!Application.IsPlaying(gameObject)) return;
            collected.Clear();
            failedAccusations.Clear();
            solvedCases.Clear();
            EvidenceChanged?.Invoke();
            ProgressChanged?.Invoke();
        }

        // Saving and restoring never bypass case approval or evidence-ID validation.
        public CaseProgressSave ExportProgress()
        {
            if (!CaseReady) return null;
            var file = CurrentCase;
            failedAccusations.TryGetValue(Key(file), out var wrong);
            var owned = GetEvidence(file);
            var ordered = new List<string>();
            foreach (var clue in file.evidence)
                if (clue != null && owned.Contains(clue.id)) ordered.Add(clue.id);
            return new CaseProgressSave
            {
                schemaVersion = 1,
                caseId = file.caseId,
                theme = file.theme,
                evidenceIds = ordered.ToArray(),
                wrongAccusations = wrong,
                solved = solvedCases.Contains(Key(file))
            };
        }

        public bool ValidateProgress(CaseProgressSave state)
        {
            if (!CaseReady || state == null || state.schemaVersion != 1) return false;
            var file = CurrentCase;
            if (state.theme != file.theme
                || !string.Equals(state.caseId, file.caseId, StringComparison.Ordinal)
                || state.evidenceIds == null || state.evidenceIds.Length > file.evidence.Length
                || state.wrongAccusations < 0
                || state.wrongAccusations > file.verdict.maxWrongAccusations) return false;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in state.evidenceIds)
                if (!InterrogationResolver.KnownEvidence(file, id) || !unique.Add(id))
                    return false;
            if (state.solved)
                foreach (var id in file.verdict.requiredEvidenceIds)
                    if (!unique.Contains(id)) return false;
            return true;
        }

        public bool RestoreProgress(CaseProgressSave state)
        {
            if (!Application.IsPlaying(gameObject) || !ValidateProgress(state)) return false;
            var file = CurrentCase;
            string key = Key(file);
            var inventory = GetOrCreate(key);
            inventory.Clear();
            foreach (var id in state.evidenceIds) inventory.Add(id);
            failedAccusations[key] = state.wrongAccusations;
            if (state.solved) solvedCases.Add(key);
            else solvedCases.Remove(key);
            EvidenceChanged?.Invoke();
            ProgressChanged?.Invoke();
            return true;
        }

        private ISet<string> GetEvidence(CaseFile file)
        {
            return file == null || string.IsNullOrWhiteSpace(file.caseId)
                ? new HashSet<string>(StringComparer.Ordinal)
                : GetOrCreate(Key(file));
        }
        private static string Key(CaseFile file)
        {
            return file.theme + ":" + file.caseId;
        }
        private HashSet<string> GetOrCreate(string key)
        {
            if (!collected.TryGetValue(key, out var found))
                collected[key] = found = new HashSet<string>(StringComparer.Ordinal);
            return found;
        }
    }
}
