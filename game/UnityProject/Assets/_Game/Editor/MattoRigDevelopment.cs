#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Technical pilot-rig validation. This never replaces a CharacterActor model.
[InitializeOnLoad]
public static class MattoRigDevelopment
{
    const string ModelPath = "Assets/_Game/Characters/CHR_MATTO/Model/CHR_MATTO_RigDevelopment.fbx";
    static string Root => Path.GetFullPath("../../art/Characters/CHR_MATTO");
    static string Request => Path.GetFullPath("../../../matto-rig.request");
    [Serializable] class RigSpec { public BoneSpec[] bones; }
    [Serializable] class BoneSpec { public string name, parent, human; }
    static MattoRigDevelopment() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        File.Delete(Request);
        Verify();
    }

    [MenuItem("Tools/Characters/Verify Matto Technical Rig")]
    public static void Verify()
    {
        Directory.CreateDirectory(Root);
        string report = Path.Combine(Root, "rig-unity-verification.txt");
        try
        {
            AssetDatabase.Refresh();
            var spec = JsonUtility.FromJson<RigSpec>(File.ReadAllText(Path.Combine(Root, "humanoid_v1.json")));
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new Exception("Technical FBX importer missing");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var transforms = model.GetComponentsInChildren<Transform>(true);
            foreach (var bone in spec.bones)
                if (transforms.Count(t => t.name == bone.name) != 1)
                    throw new Exception("Missing or duplicate bone " + bone.name);
            var human = spec.bones.Where(b => !string.IsNullOrEmpty(b.human)).Select(b => new HumanBone
            {
                boneName = b.name, humanName = b.human,
                limit = new HumanLimit { useDefaultValues = true }
            }).ToArray();
            foreach (var bone in human)
                if (!HumanTrait.BoneName.Contains(bone.humanName))
                    throw new Exception("Unknown Unity human name: " + bone.humanName);
            var description = new HumanDescription
            {
                human = human,
                skeleton = transforms.Select(t => new SkeletonBone
                {
                    name = t.name, position = t.localPosition,
                    rotation = t.localRotation, scale = t.localScale
                }).ToArray(),
                upperArmTwist = .5f, lowerArmTwist = .5f,
                upperLegTwist = .5f, lowerLegTwist = .5f,
                armStretch = .05f, legStretch = .05f,
                feetSpacing = 0, hasTranslationDoF = false
            };
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = description;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.globalScale = 1;
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
            if (!avatar.isValid || !avatar.isHuman) throw new Exception("Imported humanoid Avatar invalid");
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                var animator = instance.GetComponent<Animator>();
                if (animator == null) throw new Exception("Imported Animator missing");
                animator.avatar = avatar;
                animator.Rebind();
                var lines = new List<string>
                {
                    "TECHNICAL RIG ONLY — NOT A COMPLETED CHARACTER",
                    "Avatar.isValid: " + avatar.isValid,
                    "Avatar.isHuman: " + avatar.isHuman,
                    "Authored bones: " + spec.bones.Length,
                    "Explicit Humanoid mappings: " + human.Length,
                    "FBX scale: " + importer.globalScale,
                    "Animation import: " + importer.importAnimation,
                    "No presentation catalog/model replacement performed."
                };
                for (int n = 0; n < HumanTrait.BoneCount; n++)
                {
                    var bone = (HumanBodyBones)n;
                    if (bone == HumanBodyBones.LastBone) break;
                    var transform = animator.GetBoneTransform(bone);
                    if (HumanTrait.RequiredBone(n) && transform == null)
                        throw new Exception("Required Humanoid mapping absent: " + bone);
                    if (transform != null) lines.Add(bone + " = " + transform.name + " world=" + transform.position.ToString("F4"));
                }
                File.WriteAllLines(report, lines);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            Debug.Log("Matto technical rig verified; character production remains pending.");
        }
        catch (Exception e) { File.WriteAllText(report, "FAILED\n" + e); Debug.LogException(e); }
    }
}
#endif
