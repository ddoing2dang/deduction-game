#if UNITY_EDITOR
using System.IO;
using DeductionGame;
using UnityEditor;
using UnityEngine;

public static class PresentationSetup
{
    [MenuItem("Deduction Game/Create Presentation Slots")]
    public static void CreateSlots()
    {
        const string root = "Assets/_Game/Art";
        string[] codes = { "NAZ", "JIRO", "MARIHI", "MATTO", "BOKI", "MUNIRE" };
        foreach (string code in codes)
            foreach (string folder in new[] { "Model", "Materials", "Textures", "Animations", "Prefabs" })
                Directory.CreateDirectory($"{root}/Characters/{code}/{folder}");
        Directory.CreateDirectory("Assets/_Game/Resources");
        AssetDatabase.Refresh();
        const string path = "Assets/_Game/Resources/PresentationCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }
        Selection.activeObject = catalog;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Deduction Game/Add Selected Room Anchors")]
    public static void AddRoomAnchors()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) return;
        var room = selected.GetComponent<RoomNodeView>();
        if (room == null || selected.GetComponentInChildren<RoomPresentation>() != null) return;
        var root = new GameObject("PresentationAnchors");
        Undo.RegisterCreatedObjectUndo(root, "Add room anchors");
        root.transform.SetParent(selected.transform, false);
        var anchors = root.AddComponent<RoomPresentation>();
        for (int slot = 0; slot < 6; slot++)
        {
            var point = new GameObject($"Stand_{slot + 1:00}").transform;
            point.SetParent(root.transform);
            point.position = MansionLayout.GetStandPoint(room.RoomId, slot) - Vector3.up * 0.55f;
            anchors.StandPoints[slot] = point;
        }
        anchors.ActionPoint = new GameObject("Action_Point").transform;
        anchors.ActionPoint.SetParent(root.transform);
        anchors.ActionPoint.position = MansionLayout.GetRoom(room.RoomId).Center;
        EditorUtility.SetDirty(anchors);
    }
}
#endif
