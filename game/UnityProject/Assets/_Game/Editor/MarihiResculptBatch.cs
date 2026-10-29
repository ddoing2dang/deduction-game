#if UNITY_EDITOR
using System.IO;
using UnityEditor;

// Keep the resculpt review evidence separate from the earlier production candidate.
public static class MarihiResculptBatch
{
    static void SelectArtifacts()
    {
        var root = Path.GetFullPath("../../art/Characters/CHR_MARIHI/Resculpt/FinalPass");
        Directory.CreateDirectory(root);
        SessionState.SetString("MarihiArtifactRoot", root);
    }

    public static void Setup() { SelectArtifacts(); MarihiGamePipeline.RunBatchSetup(); }
    public static void Audit() { SelectArtifacts(); MarihiGameAudit.Begin("Game"); }
    public static void CameraAudit() { SelectArtifacts(); MarihiGameAudit.Begin("Camera"); }
}
#endif
