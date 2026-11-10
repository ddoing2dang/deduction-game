using Mystery.UI;
using UnityEngine;

namespace Mystery.Interrogation
{
    /// <summary>
    /// For future 2D clue buttons / world clue interaction. Only a trusted
    /// interaction calls Collect(); player question text cannot award clues.
    /// No map or dummy clue is placed in the existing preview scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EvidencePickup : MonoBehaviour
    {
        [SerializeField] private DialogueInterrogationAdapter interrogation;
        [SerializeField] private string evidenceId;
        [SerializeField] private bool hideAfterCollected = true;

        public void Collect()
        {
            if (!Application.IsPlaying(gameObject)) return;
            if (interrogation == null) return;
            // The adapter verifies registered ID, approved case and duplicates.
            if (!interrogation.UnlockEvidence(evidenceId)) return;
            if (hideAfterCollected) gameObject.SetActive(false);
        }

        public void Assign(DialogueInterrogationAdapter source, string id)
        {
            interrogation = source;
            evidenceId = id;
        }
    }
}
