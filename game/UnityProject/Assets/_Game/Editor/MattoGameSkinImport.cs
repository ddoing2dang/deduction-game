#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// FBX exports include empty clusters for the whole armature. Strip only those
// renderer bindings; retain every skeleton transform, vertex and blendshape.
public sealed class MattoGameSkinImport : AssetPostprocessor
{
    void OnPostprocessModel(GameObject root)
    {
        if(assetPath!=MattoGamePipeline.Model)return;
        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=renderer.sharedMesh;
            var weights=mesh.boneWeights;
            var used=new HashSet<int>();
            foreach(var w in weights) {
                if(w.weight0>0)used.Add(w.boneIndex0);if(w.weight1>0)used.Add(w.boneIndex1);
                if(w.weight2>0)used.Add(w.boneIndex2);if(w.weight3>0)used.Add(w.boneIndex3);
            }
            var indices=used.OrderBy(i=>i).ToArray();
            if(indices.Length==0||indices.Length==renderer.bones.Length)continue;
            var remap=indices.Select((old,index)=>(old,index)).ToDictionary(x=>x.old,x=>x.index);
            for(int i=0;i<weights.Length;i++) {
                var w=weights[i];
                w.boneIndex0=w.weight0>0?remap[w.boneIndex0]:0;
                w.boneIndex1=w.weight1>0?remap[w.boneIndex1]:0;
                w.boneIndex2=w.weight2>0?remap[w.boneIndex2]:0;
                w.boneIndex3=w.weight3>0?remap[w.boneIndex3]:0;
                weights[i]=w;
            }
            var poses=mesh.bindposes;var bones=renderer.bones;
            mesh.boneWeights=weights;mesh.bindposes=indices.Select(i=>poses[i]).ToArray();
            renderer.bones=indices.Select(i=>bones[i]).ToArray();
        }
    }
}
#endif
