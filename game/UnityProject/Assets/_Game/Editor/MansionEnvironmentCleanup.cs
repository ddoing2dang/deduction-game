#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using DeductionGame;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;

public static class MansionEnvironmentCleanup
{
 const string Folder="Assets/_Game/Art/Mansion";
 [Serializable] public class Manifest{public Part[] parts;public Layout layout;}
 [Serializable] public class Part{public string name,role,door,side;public int room;public bool collision,hidden;public Color color;}
 [Serializable] public class Layout{public Route[] routes;public Entry[] entries;public Secret[] secret;}
 [Serializable] public class Route{public string door;public int a,b;public Vector3[] points;}
 [Serializable] public class Entry{public int room,to;public Vector3 position;}
 [Serializable] public class Secret{public int room;public Vector3 position;}
 public static Manifest Read()=>JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/environment_manifest.json"));
 static Transform Room(Transform rooms,int id)=>rooms.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
 public static void Apply(){
  if(MapData.Theme!=MapTheme.Mansion)return;
  var manifest=Read();var rooms=GameObject.Find(MapData.RootName).transform;var connections=GameObject.Find(MapData.ConnectionsName).transform;
  var stable=rooms.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Stand_")||t.name=="Action_Point").ToDictionary(t=>t,t=>t.position);
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Mansion_Environment_Pass01.fbx");
  var source=model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(t=>t.name);
  foreach(Transform room in rooms){
   foreach(string name in new[]{"Walls","DecorativeGallery","EnvironmentArt","SecretPanels"}){
    var old=room.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   }
   foreach(string name in new[]{"Walls","DecorativeGallery","EnvironmentArt","SecretPanels"})new GameObject(name).transform.SetParent(room,false);
  }
  foreach(var route in manifest.layout.routes){
   var door=connections.Find(route.door);var placeholder=door.Find(route.a<7&&route.b>=7?"ServiceStair":"Passage");
   foreach(var c in placeholder.GetComponents<Component>())if(c is Renderer||c is Collider||c is MeshFilter)UnityEngine.Object.DestroyImmediate(c);
   foreach(Transform child in door.Cast<Transform>().ToArray())if(child!=placeholder&&!child.name.EndsWith("_Anchor")&&child.name!="EventMarkers")UnityEngine.Object.DestroyImmediate(child.gameObject);
   // Event markers follow the actual entrance rather than the obsolete straight-line midpoint.
   placeholder.position=route.points[0];placeholder.rotation=Quaternion.identity;
   door.Find($"Door_{route.a:00}_Anchor").position=route.points[0]+Vector3.up*.05f;
   door.Find($"Door_{route.b:00}_Anchor").position=route.points[^1]+Vector3.up*.05f;
  }
  foreach(var e in manifest.layout.entries)Room(rooms,e.room).Find($"NavigationPoints/Entry_To_{e.to:00}").position=e.position;
  Directory.CreateDirectory(Folder+"/EnvironmentMeshes");Directory.CreateDirectory(Folder+"/EnvironmentMaterials");
  var axis=Matrix4x4.Rotate(Quaternion.Euler(0,180,0));
  foreach(var p in manifest.parts){
   if(!source.TryGetValue(p.name,out var src))throw new Exception("Full environment FBX part missing: "+p.name);
   Transform target;
   if(p.role=="Floor"&&p.room>0)target=Room(rooms,p.room).Find("Floor");
   else{
    Transform parent;
    if(p.door=="DOOR_09_06_SECRET")parent=Room(rooms,p.room).Find("SecretPanels");
    else if(!string.IsNullOrEmpty(p.door))parent=connections.Find(p.door);
    else parent=Room(rooms,p.room).Find(p.role=="Wall"?"Walls":p.role=="Gallery"?"DecorativeGallery":"EnvironmentArt");
    string label=p.role=="Wall"?p.side+"_"+p.name:p.name;
    target=new GameObject(label).transform;target.SetParent(parent,false);
   }
   var baked=new Mesh{name=p.name,indexFormat=IndexFormat.UInt32};
   baked.CombineMeshes(new[]{new CombineInstance{mesh=src.sharedMesh,transform=target.worldToLocalMatrix*axis*src.transform.localToWorldMatrix}},true,true);
   baked.RecalculateNormals();baked.RecalculateBounds();
   string path=Folder+"/EnvironmentMeshes/"+p.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(mesh==null){mesh=baked;AssetDatabase.CreateAsset(mesh,path);}else{
    mesh.Clear();mesh.indexFormat=baked.indexFormat;mesh.vertices=baked.vertices;mesh.normals=baked.normals;mesh.uv=baked.uv;mesh.subMeshCount=baked.subMeshCount;
    for(int sub=0;sub<baked.subMeshCount;sub++)mesh.SetTriangles(baked.GetTriangles(sub),sub);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);UnityEngine.Object.DestroyImmediate(baked);
   }
   var filter=target.GetComponent<MeshFilter>();if(filter==null)filter=target.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
   var renderer=target.GetComponent<MeshRenderer>();if(renderer==null)renderer=target.gameObject.AddComponent<MeshRenderer>();
   string matPath=Folder+"/EnvironmentMaterials/Palette_"+ColorUtility.ToHtmlStringRGB(p.color)+".mat";
   var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(material==null){material=new Material(Shader.Find("Standard")){color=p.color};material.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(material,matPath);}
   renderer.sharedMaterial=material;renderer.enabled=!p.hidden;
   foreach(var collider in target.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(collider);
   if(p.collision){var collider=target.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;}
   if(p.role is "Railing" or "Door" or "Support" or "Wall")target.gameObject.AddComponent<MansionRepairObstacle>();
   if(p.door=="DOOR_09_06_SECRET")target.gameObject.AddComponent<MansionSecretPanel>();
  }
  var secret=connections.Find(MapData.SecretName);
  foreach(var s in manifest.layout.secret){
   var anchor=secret.Find(s.room==9?"HiddenPanel_09_Anchor":"GreenhouseExit_06_Anchor");anchor.position=s.position;
  }
  foreach(var pair in stable)if(pair.Key.position!=pair.Value)throw new Exception("Protected navigation point changed: "+pair.Key.name);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(rooms.gameObject.scene);Debug.Log("MANSION_FULL_ENVIRONMENT_APPLIED parts="+manifest.parts.Length);
 }
}
#endif
