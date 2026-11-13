using System;
using Mystery.UI;

namespace Mystery.Interrogation
{
    /// <summary>
    /// One case per save file, with a strict schema and matching theme/case ID.
    /// Serialized with Unity JsonUtility; no UnityEngine.Object references.
    /// </summary>
    [Serializable]
    public sealed class CaseProgressSave
    {
        // Optional final HOTEL flow. Legacy saves keep their original fields/format.
        public Case464.RoseMansion.RoseProgress roseMansion;
        public int schemaVersion = 1;
        public string caseId;
        public int theme;
        public string[] evidenceIds = Array.Empty<string>();
        public int wrongAccusations;
        public bool solved;
        public DialoguePanel.SpeakerTranscript[] conversations
            = Array.Empty<DialoguePanel.SpeakerTranscript>();
    }
}
