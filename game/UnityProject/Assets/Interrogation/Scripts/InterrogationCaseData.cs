using System;
using System.Collections.Generic;
using System.Text;
using Mystery.UI;
using UnityEngine;

namespace Mystery.Interrogation
{
    [Serializable] public sealed class CaseFile
    {
        public string caseId;
        public int theme;
        public bool approved;
        public int culpritSpeaker = -1;
        public NpcProfile[] profiles = Array.Empty<NpcProfile>();
        public EvidenceDefinition[] evidence = Array.Empty<EvidenceDefinition>();
        public CaseTopic[] topics = Array.Empty<CaseTopic>();
    }
    [Serializable] public sealed class NpcProfile
    {
        public int speaker;
        public string personalityNotes;
        public string alibiNotes;
        public string knowledgeNotes;
    }
    [Serializable] public sealed class EvidenceDefinition { public string id; public string title; }
    [Serializable] public sealed class CaseTopic
    {
        public string id;
        public string[] aliases = Array.Empty<string>();
        public string[] requiredEvidenceIds = Array.Empty<string>();
        public NpcStatement[] statements = Array.Empty<NpcStatement>();
    }
    [Serializable] public sealed class NpcStatement
    {
        public int speaker;
        // The author must check this against the actual truth of the case.
        public string truthfulText;
        // Only the selected culprit may have a deceptive text.
        public string deceptiveText;
        public string deceptionBrokenByEvidenceId;
    }
    public readonly struct InterrogationOutcome
    {
        public readonly string Text;
        public readonly bool IsNpcSpeech;
        public readonly bool UsedDeception; // Internal only, never shown to the player.
        public InterrogationOutcome(string text, bool npc, bool lie)
        { Text = text; IsNpcSpeech = npc; UsedDeception = lie; }
        public static InterrogationOutcome Notice(string text)
        { return new InterrogationOutcome(text, false, false); }
    }

    /// <summary>
    /// Deterministic rule-based Korean question matcher, NOT an AI language model.
    /// Refuses to invent case facts or attribute missing statements to an NPC.
    /// </summary>
    public static class InterrogationResolver
    {
        public static InterrogationOutcome Answer(CaseFile file, DialogueTheme theme,
            DialogueSpeaker speaker, string question, ISet<string> ownedEvidence)
        {
            if (file == null) return InterrogationOutcome.Notice("이 장소의 사건 자료가 연결되지 않았습니다.");
            if (!file.approved) return InterrogationOutcome.Notice("사건 설정이 아직 확정되지 않아 진술을 제공할 수 없습니다.");
            var issue = Validate(file, theme);
            if (issue != null) return InterrogationOutcome.Notice("사건 데이터 확인 필요: " + issue);
            var normalized = Normalize(question);
            if (normalized.Length < 2) return InterrogationOutcome.Notice("질문을 더 구체적으로 입력해 주세요.");
            CaseTopic matched = null;
            int highest = 0;
            bool ambiguous = false;
            foreach (var topic in file.topics)
            {
                int score = 0;
                foreach (var alias in topic.aliases)
                {
                    var key = Normalize(alias);
                    if (key.Length >= 2 && normalized.Contains(key)) score = Math.Max(score, key.Length);
                }
                if (score > highest) { matched = topic; highest = score; ambiguous = false; }
                else if (score > 0 && score == highest) ambiguous = true;
            }
            if (matched == null)
                return InterrogationOutcome.Notice("이 질문에 해당하는 승인된 진술이 없습니다. 다른 표현으로 질문해 주세요.");
            if (ambiguous)
                return InterrogationOutcome.Notice("여러 주제와 겹치는 질문입니다. 한 번에 한 가지씩 물어봐 주세요.");
            if (matched.requiredEvidenceIds != null)
                foreach (var id in matched.requiredEvidenceIds)
                    if (ownedEvidence == null || !ownedEvidence.Contains(id))
                        return InterrogationOutcome.Notice("관련 증거를 확보한 후에 질문할 수 있습니다.");

            NpcStatement statement = null;
            foreach (var entry in matched.statements)
                if (entry.speaker == (int)speaker) { statement = entry; break; }
            if (statement == null || string.IsNullOrWhiteSpace(statement.truthfulText))
                return InterrogationOutcome.Notice("이 NPC에 대해 확인된 진술이 아직 없습니다.");

            bool canLie = (int)speaker == file.culpritSpeaker
                && !string.IsNullOrWhiteSpace(statement.deceptiveText);
            bool broken = !string.IsNullOrWhiteSpace(statement.deceptionBrokenByEvidenceId)
                && ownedEvidence != null
                && ownedEvidence.Contains(statement.deceptionBrokenByEvidenceId);
            bool lie = canLie && !broken;
            return new InterrogationOutcome(
                (lie ? statement.deceptiveText : statement.truthfulText).Trim(), true, lie);
        }

        public static string Validate(CaseFile file, DialogueTheme theme)
        {
            if (file == null || file.theme != (int)theme || string.IsNullOrWhiteSpace(file.caseId))
                return "사건 ID 또는 테마가 올바르지 않습니다.";
            if (file.culpritSpeaker < 0 || file.culpritSpeaker > 5)
                return "범인이 확정되지 않았습니다.";
            if (file.profiles == null || file.profiles.Length != 6)
                return "NPC 프로필 6개가 필요합니다.";
            var speakers = new HashSet<int>();
            foreach (var profile in file.profiles)
                if (profile == null || profile.speaker < 0 || profile.speaker > 5
                    || !speakers.Add(profile.speaker)) return "NPC 프로필이 중복되거나 잘못되었습니다.";

            if (file.evidence == null) return "증거 목록이 없습니다.";
            var evidenceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var clue in file.evidence)
                if (clue == null || string.IsNullOrWhiteSpace(clue.id)
                    || !evidenceIds.Add(clue.id)) return "증거 ID가 비어 있거나 중복되었습니다.";

            if (file.topics == null || file.topics.Length == 0)
                return "질문 주제가 없습니다.";
            var topics = new HashSet<string>(StringComparer.Ordinal);
            foreach (var topic in file.topics)
            {
                if (topic == null || string.IsNullOrWhiteSpace(topic.id) || !topics.Add(topic.id))
                    return "주제 ID가 중복되거나 비어 있습니다.";
                if (topic.aliases == null || topic.aliases.Length == 0)
                    return "주제 검색어가 없습니다: " + topic.id;
                foreach (var alias in topic.aliases)
                    if (Normalize(alias).Length < 2) return "검색어가 너무 짧습니다: " + topic.id;
                if (topic.requiredEvidenceIds != null)
                    foreach (var id in topic.requiredEvidenceIds)
                        if (!evidenceIds.Contains(id))
                            return "등록되지 않은 필요 증거: " + topic.id;
                if (topic.statements == null) return "진술 배열이 없습니다: " + topic.id;
                var answered = new HashSet<int>();
                foreach (var statement in topic.statements)
                {
                    if (statement == null || statement.speaker < 0 || statement.speaker > 5
                        || !answered.Add(statement.speaker))
                        return "진술 NPC 인덱스 오류: " + topic.id;
                    if (string.IsNullOrWhiteSpace(statement.truthfulText))
                        return "사실 진술이 비어 있습니다: " + topic.id;
                    if (!string.IsNullOrWhiteSpace(statement.deceptiveText)
                        && statement.speaker != file.culpritSpeaker)
                        return "범인이 아닌 NPC에게 거짓말이 설정됨: " + topic.id;
                    if (!string.IsNullOrWhiteSpace(statement.deceptionBrokenByEvidenceId)
                        && (!evidenceIds.Contains(statement.deceptionBrokenByEvidenceId)
                            || string.IsNullOrWhiteSpace(statement.deceptiveText)))
                        return "거짓 진술 해제 증거 오류: " + topic.id;
                }
            }
            return null;
        }

        public static bool KnownEvidence(CaseFile file, string id)
        {
            if (file == null || file.evidence == null || string.IsNullOrWhiteSpace(id)) return false;
            foreach (var clue in file.evidence)
                if (clue != null && string.Equals(clue.id, id, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
                if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
            return builder.ToString();
        }
    }
}
