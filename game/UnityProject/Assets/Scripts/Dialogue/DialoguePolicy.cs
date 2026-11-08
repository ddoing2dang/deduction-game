using System;
using System.Collections.Generic;

namespace Deduction.Dialogue
{
    // Only an authoritative, validated case adapter may implement this interface.
    // An intent classifier supplies IDs, never text, approval flags or case truth.
    public interface IApprovedDialogueSource
    {
        bool TrySelect(string npcId, string question, IReadOnlyList<DialogueEntry> history,
            out AuthoredLine line);
        bool IsApprovedForCurrentCase(AuthoredLine line);
        bool SpeakerKnows(string npcId, string factId);
        bool IsFactVerified(string factId);
        bool IsEvidenceDiscoveredAndPresented(string evidenceId);
        bool IsContradictionEvidenceReachable(string evidenceId);
        bool WasStatementMade(string npcId, string lineId);
        bool WasFalseStatementMade(string npcId, string lineId);
        string CaseId { get; }
        string CulpritNpcId { get; }
        bool CaseValidated { get; }
    }

    public enum TruthStatus { Unverified, VerifiedTrue, AuthorApprovedFalse, UnknownNoAssertion, DeclinedNoAssertion }

    [Serializable]
    public sealed class AuthoredLine
    {
        public string lineId, npcId, text;
        public TruthStatus truthStatus;
        public string[] requiredFactIds = Array.Empty<string>();
        public string[] requiredEvidenceIds = Array.Empty<string>();
        public string[] priorStatementIds = Array.Empty<string>();
        public string contradictoryFactId, reachableCounterEvidenceId;
        public bool retractsFalseStatement;
    }

    public static class DialoguePolicy
    {
        public static bool Allows(IApprovedDialogueSource source, string npcId, AuthoredLine line)
        {
            if (source == null || !source.CaseValidated || !NpcDirectory.Contains(source.CulpritNpcId)
                || string.IsNullOrWhiteSpace(source.CaseId) || !NpcDirectory.Contains(npcId)
                || line == null || line.npcId != npcId || string.IsNullOrWhiteSpace(line.lineId)
                || string.IsNullOrWhiteSpace(line.text) || !source.IsApprovedForCurrentCase(line)) return false;
            if (line.requiredFactIds == null || line.requiredEvidenceIds == null || line.priorStatementIds == null) return false;
            foreach (var fact in line.requiredFactIds)
                if (string.IsNullOrWhiteSpace(fact) || !source.IsFactVerified(fact) || !source.SpeakerKnows(npcId, fact)) return false;
            foreach (var evidence in line.requiredEvidenceIds)
                if (string.IsNullOrWhiteSpace(evidence) || !source.IsEvidenceDiscoveredAndPresented(evidence)) return false;
            foreach (var previous in line.priorStatementIds)
                if (string.IsNullOrWhiteSpace(previous) || !source.WasStatementMade(npcId, previous)) return false;
            if (line.retractsFalseStatement)
            {
                if (npcId != source.CulpritNpcId) return false;
                bool hasFalsePrior = false;
                foreach (var previous in line.priorStatementIds)
                    hasFalsePrior |= source.WasFalseStatementMade(npcId, previous);
                if (!hasFalsePrior) return false;
            }
            switch (line.truthStatus)
            {
                case TruthStatus.AuthorApprovedFalse:
                    return npcId == source.CulpritNpcId
                        && !string.IsNullOrWhiteSpace(line.contradictoryFactId)
                        && !string.IsNullOrWhiteSpace(line.reachableCounterEvidenceId)
                        && source.IsFactVerified(line.contradictoryFactId)
                        && source.SpeakerKnows(npcId, line.contradictoryFactId)
                        && source.IsContradictionEvidenceReachable(line.reachableCounterEvidenceId);
                case TruthStatus.VerifiedTrue: return line.requiredFactIds.Length > 0;
                case TruthStatus.UnknownNoAssertion:
                case TruthStatus.DeclinedNoAssertion: return true;
                default: return false;
            }
        }
    }

    [Serializable] public sealed class NpcProfile { public string npcId, displayName; }
    [Serializable] public sealed class NpcProfiles { public NpcProfile[] characters; }
    public static class NpcDirectory
    {
        public static readonly string[] Ids = { "MATTO", "MARIHI", "NAZ", "BOKI", "JJIRO", "MOONIRE" };
        public static bool Contains(string id) => Array.IndexOf(Ids, id) >= 0;
    }

    [Serializable]
    public sealed class DialogueEntry
    {
        public string caseId, npcId, role, text, lineId, utc;
    }
    [Serializable]
    public sealed class DialogueJournal
    {
        public int schemaVersion = 1;
        public List<DialogueEntry> entries = new List<DialogueEntry>();
    }
}
