#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using DeductionGame;

[InitializeOnLoad]
public static class MansionVisualGoldPipeline
{
 public const string Folder="Assets/_Game/Art/MansionVisualGold";
 public const string ApiVersion="MansionVisualGold_v2";
 public static string Root=>Path.GetFullPath("../../art/Mansion/VisualGold");
 [Serializable] public class Document {public string modelFile;public Part[] parts;public Mat[] materials;public Image[] images;public string[] retired;public Lamp[] lights;}
 [Serializable] public class Part {public string name,role,door,side,parent,assembly,meshKey,asset;public int room;public bool collision,hidden;public string[] materials;public Vector3 min,max;}
 [Serializable] public class Mat {public string name,albedo,normal,metalGloss;public float[] color,emission;public float cutoff,normalStrength,metallic,smoothness;public bool transparent;}
 [Serializable] public class Image {public string file;public bool linear,normal;public int[] size;}
 [Serializable] public class Lamp {public string name;public int room;public Vector3 position;public Color color;public float intensity,range;}
 public static Document Read()=>JsonUtility.FromJson<Document>(File.ReadAllText(Folder+"/gold-manifest.json"));
 static MansionVisualGoldPipeline(){
  Directory.CreateDirectory(Root);EditorApplication.update+=Poll;
  try{
   File.WriteAllText(Path.Combine(Root,"editor-api-version.txt"),ApiVersion);
   File.WriteAllText(Path.Combine(Root,"editor-state.txt"),"play="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused+" updating="+EditorApplication.isUpdating+" compiling="+EditorApplication.isCompiling);
  }catch(IOException){ /* Diagnostic files must never disable the request API. */ }
  if(!SessionState.GetBool("MansionGoldRecovery_v2",false)){
   SessionState.SetBool("MansionGoldRecovery_v2",true);
   EditorApplication.delayCall+=()=>{
    if(SessionState.GetString("MansionVisualGoldAudit","")!=""&&EditorApplication.isPlaying){
     SessionState.SetString("MansionVisualGoldAudit","");EditorApplication.isPlaying=false;EditorApplication.isPaused=false;
    }
   };
  }
 }
 static void Poll(){
  string path=Path.GetFullPath("../../../mansion-gold.request");
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(path))return;
  string action=File.ReadAllText(path).Trim();File.Delete(path);
  try{
   if(action=="Refresh")AssetDatabase.Refresh();
   else if(action=="RestoreRelease")UnityEditor.Compilation.CompilationPipeline.codeOptimization=UnityEditor.Compilation.CodeOptimization.Release;
   else if(action=="Setup"){EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");Setup();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());File.WriteAllText(Path.Combine(Root,"unity-setup-status.txt"),"SETUP COMPLETE; PLAY AND DIRECT VISUAL REVIEW REQUIRED");}
   else if(action=="PruneGenerated"){
    var d=Read();var liveMeshes=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.sharedMesh!=null).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).ToHashSet();
    foreach(string f in Directory.GetFiles(Folder+"/Meshes","*.asset"))if(!liveMeshes.Contains(f.Replace('\\','/')))AssetDatabase.DeleteAsset(f);
    var liveMats=d.materials.Select(m=>Folder+"/Materials/"+m.name+".mat").ToHashSet();foreach(string f in Directory.GetFiles(Folder+"/Materials","GoldMat_*.mat"))if(!liveMats.Contains(f.Replace('\\','/')))AssetDatabase.DeleteAsset(f);
    var liveImages=d.images.Select(im=>Folder+"/Textures/"+im.file).ToHashSet();foreach(string f in Directory.GetFiles(Folder+"/Textures","Gold_*.png"))if(!liveImages.Contains(f.Replace('\\','/')))AssetDatabase.DeleteAsset(f);
    AssetDatabase.SaveAssets();
   }
   else if(action=="SceneRender"){
    var cam=Camera.main;var oldP=cam.transform.position;var oldR=cam.transform.rotation;float oldZ=cam.orthographicSize;
    cam.transform.position=new Vector3(34,42,-43);cam.transform.LookAt(new Vector3(0,.5f,1));cam.orthographicSize=31.5f;
    var rt=new RenderTexture(1500,1150,24);var prev=RenderTexture.active;
    try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var im=new Texture2D(1500,1150,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1500,1150),0,0);im.Apply();File.WriteAllBytes(Path.Combine(Root,"Unity_Edit_Estate.png"),im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);}
    finally{cam.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(rt);cam.transform.SetPositionAndRotation(oldP,oldR);cam.orthographicSize=oldZ;}
   }
   else if(action=="Diagnose"){
    File.WriteAllLines(Path.Combine(Root,"mesh-counts.txt"),UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(f=>f.name+" vertices="+f.sharedMesh.vertexCount+" tris="+f.sharedMesh.triangles.Length/3+" materials="+string.Join(",",f.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name))));
    var d=Read();var expected=d.parts.Select(p=>p.name).ToHashSet();var b=MansionEnvironmentCleanup.Read();foreach(var p in b.parts)if(p.role=="Wall")expected.Add(p.side+"_"+p.name);
    File.WriteAllLines(Path.Combine(Root,"unmanaged-scene-meshes.txt"),UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>!expected.Contains(f.name)).Select(f=>f.name+" parent="+f.transform.parent?.name+" bounds="+f.GetComponent<Renderer>()?.bounds));
   }
   else if(action=="Audit")MansionVisualGoldAudit.Begin("Gold");
  }catch(Exception e){File.WriteAllText(Path.Combine(Root,"unity-setup-status.txt"),"FAILED\n"+e);Debug.LogException(e);}
 }
 static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
 public static bool ApplyIfReady(){if(MapData.Theme!=MapTheme.Mansion||!File.Exists(Folder+"/gold-manifest.json")||!Directory.Exists(Folder+"/Materials"))return false;Apply();return true;}
 public static void Setup(){
  AssetDatabase.Refresh();var d=Read();Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
  var shader=Shader.Find("DeductionGame/Mansion Visual Gold");Need(shader!=null,"Gold shader not compiled");Need(!ShaderUtil.ShaderHasError(shader),"Gold shader has compiler errors");
  foreach(var im in d.images){
   var ti=AssetImporter.GetAtPath(Folder+"/Textures/"+im.file)as TextureImporter;Need(ti!=null,"Missing texture "+im.file);
   ti.maxTextureSize=2048;ti.textureType=im.normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=!im.linear&&!im.normal;ti.mipmapEnabled=true;
   ti.wrapMode=TextureWrapMode.Repeat;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=!im.normal&&!im.linear;ti.textureCompression=TextureImporterCompression.CompressedHQ;
   var android=ti.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=90;ti.SetPlatformTextureSettings(android);ti.SaveAndReimport();
  }
  foreach(var m in d.materials){
   string path=Folder+"/Materials/"+m.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=m.transparent?Shader.Find("Standard"):shader;mat.enableInstancing=true;
   mat.SetColor("_Color",new Color(m.color[0],m.color[1],m.color[2],m.color[3]));mat.SetFloat("_Metallic",m.metallic);mat.SetFloat("_Glossiness",m.smoothness);mat.SetFloat("_Cutoff",m.cutoff);mat.SetFloat("_BumpScale",m.normalStrength);
   mat.SetColor("_EmissionColor",new Color(m.emission[0],m.emission[1],m.emission[2]));mat.SetFloat("_LightFill",.025f);
   foreach(var p in new[]{("_MainTex",m.albedo),("_BumpMap",m.normal),("_MetallicGlossMap",m.metalGloss)})mat.SetTexture(p.Item1,string.IsNullOrEmpty(p.Item2)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+p.Item2));
   if(!string.IsNullOrEmpty(m.normal))mat.EnableKeyword("_NORMALMAP");else mat.DisableKeyword("_NORMALMAP");if(!string.IsNullOrEmpty(m.metalGloss))mat.EnableKeyword("_METALLICGLOSSMAP");else mat.DisableKeyword("_METALLICGLOSSMAP");
   if(m.transparent){mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHABLEND_ON");mat.renderQueue=3000;}
   EditorUtility.SetDirty(mat);
  }
  var importer=AssetImporter.GetAtPath(Folder+"/"+d.modelFile)as ModelImporter;Need(importer!=null,"Gold FBX missing");
  importer.importAnimation=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.meshCompression=ModelImporterMeshCompression.Off;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
  Apply();AssetDatabase.SaveAssets();
 }
 static Transform Room(int id)=>GameObject.Find(MapData.RootName).transform.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{id:00}_"));
 public static void Apply(){
  if(MapData.Theme!=MapTheme.Mansion)return;var d=Read();
  var protectedPoints=GameObject.Find(MapData.RootName).GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Stand_")||t.name=="Action_Point"||t.name.StartsWith("Entry_To_")).ToDictionary(t=>t.name+"/"+t.parent.parent.name,t=>t.position);
  var old=GameObject.Find("MansionVisualGold");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
  // The current Pass02 topology/collider assets are authoritative. Replace only Gold visuals.
  // Reapplying the old cleanup here would unnecessarily rewrite serialized collision meshes.
  foreach(var generated in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene.name=="Game"&&t.name.StartsWith("VG_")).ToArray())if(generated!=null)UnityEngine.Object.DestroyImmediate(generated.gameObject);
  var baseline=MansionEnvironmentCleanup.Read();
  var baselineParts=baseline.parts.ToDictionary(p=>p.name);
  var all=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.gameObject.scene.name=="Game").ToArray();
  var targets=new Dictionary<string,Transform>();
  foreach(var p in baseline.parts){
   string label=p.role=="Wall"?p.side+"_"+p.name:p.name;
   var source=p.role=="Floor"&&p.room>0?Room(p.room).Find("Floor"):all.FirstOrDefault(f=>f!=null&&f.name==label)?.transform;
   if(source!=null)targets[p.name]=source;
   else Need(d.retired.Contains(p.name),"Authoritative Pass02 object missing "+p.name);
  }
  foreach(string name in d.retired)if(targets.TryGetValue(name,out var target)){UnityEngine.Object.DestroyImmediate(target.gameObject);targets.Remove(name);}
  var marker=new GameObject("MansionVisualGold");marker.AddComponent<MansionVisualGoldState>();var outside=new GameObject("EstateExterior").transform;outside.SetParent(marker.transform,false);
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+d.modelFile);Need(model!=null,"Gold model absent");var src=model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name);
  var mats=d.materials.ToDictionary(m=>m.name,m=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+m.name+".mat"));var meshes=new Dictionary<string,Mesh>();var axis=Matrix4x4.Rotate(Quaternion.Euler(0,180,0));var boundsErrors=new List<string>();
  foreach(var p in d.parts){
   Need(src.ContainsKey(p.name),"Missing exported object "+p.name);var f=src[p.name];bool existing=targets.TryGetValue(p.name,out var t);
   if(!existing){
    Transform parent=!string.IsNullOrEmpty(p.parent)&&targets.ContainsKey(p.parent)?targets[p.parent]:p.room>0?Room(p.room).Find("EnvironmentArt"):outside;
    t=new GameObject(p.name).transform;t.SetParent(parent,false);
   }
   if(!existing){t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;}
   string key="Baked_"+p.name;Mesh mesh;
   if(!meshes.TryGetValue(key,out mesh)){
    mesh=UnityEngine.Object.Instantiate(f.sharedMesh);mesh.name=key;
    {
     var baked=new Mesh{indexFormat=IndexFormat.UInt32};var matrix=t.worldToLocalMatrix*axis*f.transform.localToWorldMatrix;
     baked.CombineMeshes(new[]{new CombineInstance{mesh=mesh,transform=matrix}},false,true);UnityEngine.Object.DestroyImmediate(mesh);mesh=baked;mesh.name=key;
     // CombineMeshes with mergeSubMeshes=false needs a separate source per submesh.
     if(f.sharedMesh.subMeshCount>1){UnityEngine.Object.DestroyImmediate(mesh);mesh=new Mesh{name=key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(Enumerable.Range(0,f.sharedMesh.subMeshCount).Select(i=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=matrix}).ToArray(),false,true);}
    }
    mesh.RecalculateBounds();if(mesh.uv.Length==mesh.vertexCount)mesh.RecalculateTangents();string path=Folder+"/Meshes/"+key+".asset";
    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{saved.Clear();saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.uv=mesh.uv;if(mesh.uv2.Length>0)saved.uv2=mesh.uv2;saved.colors32=mesh.colors32;saved.subMeshCount=mesh.subMeshCount;for(int sub=0;sub<mesh.subMeshCount;sub++)saved.SetTriangles(mesh.GetTriangles(sub),sub,false);saved.RecalculateBounds();saved.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}meshes[key]=mesh;
   }
   // Full source matrices are baked, including reflection/shear; no lossy TRS decomposition.
   var filter=t.GetComponent<MeshFilter>();if(filter==null)filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
   var renderer=t.GetComponent<MeshRenderer>();if(renderer==null)renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=f.GetComponent<Renderer>().sharedMaterials.Select(m=>{
 string key="GoldMat_"+System.Text.RegularExpressions.Regex.Replace(m.name,"[^A-Za-z0-9_-]","_");
 Need(mats.ContainsKey(key),"Unmapped FBX material "+m.name+" on "+p.name);return mats[key];}).ToArray();renderer.enabled=!p.hidden;
   if(existing&&baselineParts.TryGetValue(p.name,out var original)&&original.collision){
    var collider=t.GetComponent<MeshCollider>();Need(collider!=null,"Original collider component missing "+p.name);
    string originalPath="Assets/_Game/Art/Mansion/EnvironmentMeshes/"+(original.shared?"Shared_"+original.meshKey:original.name)+".asset";
    var collisionMesh=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);Need(collisionMesh!=null,"Authoritative collision mesh missing "+originalPath);collider.sharedMesh=collisionMesh;
   }
   Need(renderer.sharedMaterials.Length==mesh.subMeshCount,"Material/submesh mismatch "+p.name+" materials="+renderer.sharedMaterials.Length+" submeshes="+mesh.subMeshCount);
   if(!existing&&!string.IsNullOrEmpty(p.parent)){
    var follow=t.gameObject.AddComponent<MansionGoldRendererFollower>();follow.Source=targets[p.parent].GetComponent<Renderer>();
   }else if(!existing&&p.room>0){
    t.gameObject.AddComponent<MansionArtOccluder>();
    if(p.collision||!string.IsNullOrEmpty(p.asset)){var box=t.gameObject.AddComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;t.gameObject.AddComponent<MansionRepairObstacle>();}
   }
   var b=renderer.bounds;if(Vector3.Distance(b.min,p.min)>.025f||Vector3.Distance(b.max,p.max)>.025f)boundsErrors.Add(p.name+" actual="+b+" expected="+p.min+".."+p.max);
  }
  foreach(var l in d.lights){var go=new GameObject(l.name);go.transform.SetParent(Room(l.room).Find("EnvironmentArt"),false);go.transform.position=l.position;var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=l.color;light.intensity=l.intensity;light.range=l.range;light.shadows=LightShadows.None;}
  var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);if(sun!=null){sun.intensity=.9f;sun.color=new Color(1,.92f,.81f);sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.40f,.46f,.55f);RenderSettings.ambientEquatorColor=new Color(.26f,.25f,.23f);RenderSettings.ambientGroundColor=new Color(.12f,.13f,.14f);RenderSettings.reflectionIntensity=.65f;
  var camera=Camera.main;if(camera!=null){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.20f);if(camera.GetComponent<MansionGoldCinematicDirector>()==null)camera.gameObject.AddComponent<MansionGoldCinematicDirector>();}
  Physics.SyncTransforms();
  foreach(var t in GameObject.Find(MapData.RootName).GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Stand_")||t.name=="Action_Point"||t.name.StartsWith("Entry_To_")){string key=t.name+"/"+t.parent.parent.name;Need(protectedPoints.ContainsKey(key)&&Vector3.Distance(protectedPoints[key],t.position)<1e-6f,"Protected point moved "+key);}
  File.WriteAllLines(Path.Combine(Root,"unity-bounds-errors.txt"),boundsErrors);Need(boundsErrors.Count==0,"Blender/Unity bounds mismatch; see unity-bounds-errors.txt");
  File.WriteAllText(Path.Combine(Root,"unity-install-results.txt"),"Gold parts="+d.parts.Length+"; mesh assets="+meshes.Count+"; materials="+mats.Count+"; textures="+d.images.Length+"; protected points="+protectedPoints.Count+"; all visible bounds aligned; original graph retained");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
}
#endif
