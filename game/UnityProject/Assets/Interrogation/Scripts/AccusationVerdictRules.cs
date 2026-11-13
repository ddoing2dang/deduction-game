using System;
using System.Collections.Generic;
using Mystery.UI;

namespace Mystery.Interrogation
{
    [Serializable]
    public sealed class CaseVerdict
    {
        public string[] requiredEvidenceIds = Array.Empty<string>();
        public int maxWrongAccusations = 2;
        public string successText;
    }

    public enum VerdictState
    {
        NotReady,
        InvalidSuspect,
        MissingEvidence,
        Incorrect,
        Solved,
        Locked,
        AlreadySolved
    }

    public readonly struct VerdictOutcome
    {
        public readonly VerdictState State;
        public readonly string Message;
        public bool IsSolved => State == VerdictState.Solved || State == VerdictState.AlreadySolved;

        public VerdictOutcome(VerdictState state, string message)
        {
            State = state;
            Message = message;
        }
    }

    /// <summary>
    /// Pure verdict logic; it never grants evidence or exposes the true culprit
    /// in failed attempts. The authored case file is the only authority.
    /// </summary>
    public static class AccusationVerdictRules
    {
        // Final HOTEL rules: no legacy wrong-attempt limit or required-evidence gate.
        public static string EvaluateFinalAxes(string suspect, string method, string weapon,
            string actualCulprit, string actualMethod, string actualWeapon)
        {
            if(string.IsNullOrWhiteSpace(actualCulprit)||string.IsNullOrWhiteSpace(actualMethod)||string.IsNullOrWhiteSpace(actualWeapon))
                throw new InvalidOperationException("Approved final truth is required.");
            if(suspect!=actualCulprit)return "Misjudged";
            return method==actualMethod&&weapon==actualWeapon?"Complete":"Partial";
        }
        public static string ValidateVerdict(CaseFile file)
        {
            if (file == null || file.verdict == null)
                return "범인 지목 기준이 작성되지 않았습니다.";
            if (file.verdict.requiredEvidenceIds == null || file.verdict.requiredEvidenceIds.Length == 0)
                return "범인 지목에 필요한 핵심 증거가 없습니다.";
            if (file.verdict.maxWrongAccusations < 1 || file.verdict.maxWrongAccusations > 6)
                return "허용 오답 횟수는 1~6이어야 합니다.";
            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in file.verdict.requiredEvidenceIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !required.Add(id)
                    || !InterrogationResolver.KnownEvidence(file, id))
                    return "범인 지목 증거 ID가 중복되거나 미등록 상태입니다.";
            }
            if (string.IsNullOrWhiteSpace(file.verdict.successText))
                return "사건 해결 후 출력할 결말 문구가 없습니다.";
            return null;
        }

        public static VerdictOutcome Evaluate(
            CaseFile file, DialogueTheme expectedTheme, int suspectIndex,
            ISet<string> collected, ICollection<string> presented)
        {
            if (file == null || !file.approved)
                return new VerdictOutcome(VerdictState.NotReady, "사건이 아직 확정되지 않았습니다.");
            if (InterrogationResolver.Validate(file, expectedTheme) != null
                || ValidateVerdict(file) != null)
                return new VerdictOutcome(VerdictState.NotReady, "승인된 사건 데이터의 확인이 필요합니다.");
            if (suspectIndex < 0 || suspectIndex > 5)
                return new VerdictOutcome(VerdictState.InvalidSuspect, "지목할 NPC를 먼저 선택해 주세요.");

            var selected = new HashSet<string>(StringComparer.Ordinal);
            if (presented != null)
                foreach (var id in presented)
                {
                    if (string.IsNullOrWhiteSpace(id) || !InterrogationResolver.KnownEvidence(file, id)
                        || collected == null || !collected.Contains(id))
                        return new VerdictOutcome(VerdictState.MissingEvidence, "획득하지 않은 단서는 제출할 수 없습니다.");
                    selected.Add(id);
                }
            foreach (var required in file.verdict.requiredEvidenceIds)
                if (!selected.Contains(required))
                    return new VerdictOutcome(VerdictState.MissingEvidence, "범인을 특정하기에는 제시한 근거가 충분하지 않습니다.");

            if (suspectIndex != file.culpritSpeaker)
                return new VerdictOutcome(VerdictState.Incorrect, "지목이 틀렸습니다. 증거와 진술을 다시 살펴보세요.");

            return new VerdictOutcome(VerdictState.Solved, file.verdict.successText.Trim());
        }
    }
}
