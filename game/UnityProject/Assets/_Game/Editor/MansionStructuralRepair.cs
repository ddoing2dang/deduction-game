#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeductionGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MansionStructuralRepair
{
    const string Folder = "Assets/_Game/Art/Mansion";
    [Serializable] class Manifest { public Part[] meshes; }
    [Serializable] class Part { public string name; public bool decorative; }

    public static void ApplyToScene()
    {
        if (MapData.Theme != MapTheme.Mansion) return;
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Mansion_Environment_Pass01.fbx");
        if(model==null) throw new InvalidOperationException("Repaired Mansion FBX is missing.");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/structural_patch.json"));
        var source=model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(m=>m.name);
        var rooms=GameObject.Find(MapData.RootName).transform;
        var connections=GameObject.Find(MapData.ConnectionsName).transform;
        MansionStairArchitecture.Load().ApplyEntries(rooms);
        var navigation=Snapshot(rooms,connections);
        Directory.CreateDirectory(Folder+"/RepairMeshes");
        var rotate=Matrix4x4.Rotate(Quaternion.Euler(0,180,0));
        foreach(var edge in new[]{"03_05","04_07","05_08"}){
            var door=connections.Find("DOOR_"+edge);
            var route=door.Find(edge=="03_05"?"Passage":"ServiceStair");
            // Retain the existing route Transform for event markers.
            foreach(var component in route.GetComponents<Component>())
                if(component is Renderer || component is Collider || component is MeshFilter)
                    UnityEngine.Object.DestroyImmediate(component);
            foreach(Transform child in door.Cast<Transform>().ToArray())
                if(child.name.StartsWith("DOOR_")) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        // Replace only the wall faces adjoining the two repaired openings.
        foreach(var pair in new[]{(4,"North"),(4,"West"),(5,"North"),(5,"East"),(7,"South"),(7,"West"),(8,"South"),(8,"East")}){
            var walls=Room(rooms,pair.Item1).Find("Walls");
            foreach(Transform wall in walls.Cast<Transform>().ToArray())
                if(wall.name.StartsWith(pair.Item2+"_")) UnityEngine.Object.DestroyImmediate(wall.gameObject);
        }
        var hall=Room(rooms,1);
        var oldGallery=hall.Find("DecorativeGallery");
        if(oldGallery!=null)UnityEngine.Object.DestroyImmediate(oldGallery.gameObject);
        var gallery=new GameObject("DecorativeGallery").transform;gallery.SetParent(hall,false);
        foreach(var part in manifest.meshes){
            if(!source.TryGetValue(part.name,out var src)) throw new InvalidOperationException("FBX mesh missing: "+part.name);
            if(src.sharedMesh.vertexCount==0)continue;
            Transform target;
            if(part.name=="R04_Floor" || part.name=="R05_Floor"){
                target=Room(rooms,int.Parse(part.name.Substring(1,2))).Find("Floor");
            } else {
                Transform parent;string displayName=part.name;
                if(part.name.StartsWith("DOOR_"))parent=connections.Find(part.name.Substring(0,10));
                else if(part.name.StartsWith("R01_")) parent=gallery;
                else {
                    int id=int.Parse(part.name.Substring(1,2));
                    parent=Room(rooms,id).Find("Walls");
                    displayName=part.name.Substring(4); // South_ prefix retained for cutaway.
                }
                target=new GameObject(displayName).transform;target.SetParent(parent,false);
            }
            // Bake the imported FBX transform, corrected to the project's explicit
            // Blender(X,Y,Z-up)->Unity(X,Z,Y) convention confirmed by the probe.
            var matrix=target.worldToLocalMatrix*rotate*src.transform.localToWorldMatrix;
            var baked=new Mesh { name=part.name, indexFormat=IndexFormat.UInt32 };
            baked.CombineMeshes(new[]{new CombineInstance{mesh=src.sharedMesh,transform=matrix}},true,true);
            baked.RecalculateNormals();baked.RecalculateBounds();
            string path=Folder+"/RepairMeshes/"+part.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null){
                // Rebuild native vertex/index buffers too; CopySerialized alone can leave
                // rendered geometry and collision cooking on the previous mesh revision.
                existing.Clear();existing.indexFormat=baked.indexFormat;
                existing.vertices=baked.vertices;existing.normals=baked.normals;existing.uv=baked.uv;
                existing.subMeshCount=baked.subMeshCount;
                for(int sub=0;sub<baked.subMeshCount;sub++)existing.SetTriangles(baked.GetTriangles(sub),sub);
                existing.RecalculateBounds();existing.UploadMeshData(false);
                UnityEngine.Object.DestroyImmediate(baked);baked=existing;EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(baked,path);
            var mf=target.GetComponent<MeshFilter>();if(mf==null)mf=target.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=baked;
            var mr=target.GetComponent<MeshRenderer>();if(mr==null)mr=target.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=src.GetComponent<MeshRenderer>().sharedMaterials;
            if(part.name.Contains("_COLLISION_"))mr.enabled=false;
            foreach(var old in target.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(old);
            // Decorative gallery never contributes walkable surfaces or upper-level exits.
            if(!part.decorative && !part.name.Contains("_Step_")){var collider=target.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=baked;}
            if(part.name.StartsWith("DOOR_") && new[]{"Rail","_Post","_Jamb","_Header","Skirt","UnderBeam","Support","GuardWall","OuterWall"}.Any(part.name.Contains))
                target.gameObject.AddComponent<MansionRepairObstacle>();
        }
        if(Snapshot(rooms,connections)!=navigation)throw new InvalidOperationException("Navigation reference regression");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(rooms.gameObject.scene);
        Debug.Log("MANSION_REPAIR_APPLIED: navigation references unchanged; FBX geometry installed");
    }
    static Transform Room(Transform root,int id)=>root.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
    public static string Snapshot(Transform rooms,Transform doors)
    {
        var entries=rooms.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="NavigationPoints"||t.name.StartsWith("Entry_")||t.name.StartsWith("Stand_")||t.name=="Action_Point")
            .Concat(doors.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("_Anchor")));
        return string.Join("\n",entries.OrderBy(t=>t.parent.name+"/"+t.name).Select(t=>t.parent.name+"/"+t.name+":"+t.position.ToString("F5")+":"+t.rotation.ToString("F5")+":"+t.localScale.ToString("F5")));
    }
}
#endif
