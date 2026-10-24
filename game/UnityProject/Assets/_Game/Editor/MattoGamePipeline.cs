#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DeductionGame;

[InitializeOnLoad]
public static class MattoGamePipeline
{
 public const string ApiVersion="ApprovedVRoidGame_v12";
 static MattoGamePipeline(){EditorApplication.update+=Poll;Directory.CreateDirectory(Root);File.WriteAllText(Path.Combine(Root,"editor-api-version.txt"),ApiVersion);}
 static void Poll(){
  string request=Path.GetFullPath("../../../matto-game.request");
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
  string action=File.ReadAllText(request).Trim();File.Delete(request);
  if(action=="Setup")RunBatchSetup();
  else if(action=="Refresh")AssetDatabase.Refresh();
  else if(action=="Audit")MattoGameAudit.Begin("Game");
 }
 public const string Folder="Assets/_Game/Characters/CHR_MATTO";
 public const string Model=Folder+"/Model/CHR_MATTO.fbx";
 public static string Root=>Path.GetFullPath("../../art/Characters/CHR_MATTO/Game");
 [Serializable] class Materials {public Mat[] materials;public Image[] images;}
 [Serializable] class Image {public string name,file;public int[] size;}
 [Serializable] class Mat {public string name,texture,normal,emission,matcap;public float[] baseColor,shadeColor,emissionColor,matcapColor;public float cutoff,metallic;public bool linear;}
 [Serializable] class Spec {public Bone[] bones;}
 [Serializable] class Bone {public string name,human;}
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 static Color C(float[] a)=>new Color(a[0],a[1],a[2],a.Length>3?a[3]:1);
 static HumanDescription Description(GameObject model,HumanBone[] mapping)=>new HumanDescription {
  human=mapping,skeleton=model.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
  upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
 static HumanBone H(string bone,string human)=>new HumanBone{boneName=bone,humanName=human,limit=new HumanLimit{useDefaultValues=true}};
 static void Rig(ModelImporter importer,HumanBone[] mapping,bool animation)
 {
  importer.bakeAxisConversion=false;importer.globalScale=1;importer.importAnimation=animation;
  importer.animationType=ModelImporterAnimationType.Generic;importer.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
  Need(model.GetComponentsInChildren<Transform>(true).Select(t=>t.name).Distinct().Count()==model.GetComponentsInChildren<Transform>(true).Length,"Duplicate transform names");
  importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  importer.sourceAvatar=null;importer.humanDescription=Description(model,mapping);importer.optimizeGameObjects=false;
  importer.importBlendShapes=true;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
  importer.skinWeights=ModelImporterSkinWeights.Standard;importer.meshCompression=ModelImporterMeshCompression.Off;
  importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).OfType<Avatar>().Single();
  Need(avatar.isHuman&&avatar.isValid,"Invalid Humanoid Avatar: "+importer.assetPath);
 }
 public static void RunBatchSetup()
 {
  Directory.CreateDirectory(Root);
  try {Setup();File.WriteAllText(Path.Combine(Root,"unity-setup-status.txt"),"SETUP COMPLETE; PLAY AND VISUAL REVIEW REQUIRED");if(Application.isBatchMode)EditorApplication.Exit(0);}
  catch(Exception e){File.WriteAllText(Path.Combine(Root,"unity-setup-status.txt"),"FAILED\n"+e);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
 }
 public static void Setup()
 {
  AssetDatabase.Refresh();Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Prefabs");
  var desc=JsonUtility.FromJson<Materials>(File.ReadAllText(Path.Combine(Root,"unity-materials.json")));
  var shader=Shader.Find("DeductionGame/Approved VRoid Toon");Need(shader!=null,"Approved toon shader missing");
  var normals=desc.materials.Select(m=>m.normal).Where(s=>!string.IsNullOrEmpty(s)).ToHashSet();
  var linear=desc.materials.Where(m=>m.linear).Select(m=>m.texture).ToHashSet();
  foreach(var image in desc.images.GroupBy(i=>i.file).Select(g=>g.First())) {
   string path=Folder+"/Textures/"+image.file;var ti=(TextureImporter)AssetImporter.GetAtPath(path);Need(ti!=null,"Texture absent "+path);
   ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.sRGBTexture=!linear.Contains(image.file)&&!normals.Contains(image.file);
   ti.textureType=normals.Contains(image.file)?TextureImporterType.NormalMap:TextureImporterType.Default;
   ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;
   // Alpha masks and face ink stay uncompressed until visual preservation passes.
   ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=!normals.Contains(image.file);ti.SaveAndReimport();
   var android=ti.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;
   android.maxTextureSize=2048;android.format=linear.Contains(image.file)?TextureImporterFormat.RGBA32:TextureImporterFormat.ASTC_4x4;
   android.compressionQuality=100;ti.SetPlatformTextureSettings(android);ti.SaveAndReimport();
  }
  var mats=new Dictionary<string,Material>();
  foreach(var d in desc.materials) {
   string path=Folder+"/Materials/"+d.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
   m.SetColor("_Color",C(d.baseColor));m.SetColor("_ShadeColor",C(d.shadeColor));m.SetColor("_EmissionColor",C(d.emissionColor));m.SetColor("_MatcapColor",C(d.matcapColor));
   m.SetFloat("_Cutoff",d.cutoff);m.SetFloat("_Metallic",d.metallic);
   bool face=d.name.StartsWith("MAT_MATTO_Face"),skin=d.name=="MAT_MATTO_Body";
   m.SetFloat("_FaceAtlas",face?1:0);
   m.SetFloat("_ShadeFloor",face||skin?.65f:d.name=="MAT_MATTO_Hair"?.35f:d.metallic>.1f?.65f:0);
   m.SetFloat("_NormalStrength",face?.25f:skin?.35f:1);
   m.SetFloat("_MatcapStrength",face||skin?.03f:.12f);
   foreach(var pair in new[]{("_MainTex",d.texture),("_BumpMap",d.normal),("_EmissionTex",d.emission),("_MatcapTex",d.matcap)})
    m.SetTexture(pair.Item1,string.IsNullOrEmpty(pair.Item2)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+pair.Item2));
   EditorUtility.SetDirty(m);mats[d.name]=m;
  }
  var map=new List<HumanBone>();
  foreach(Match match in Regex.Matches(File.ReadAllText(Path.Combine(Root,"vroid-human-map.json")),"\"([a-z_]+)\"\\s*:\\s*\"([^\"]+)\"")) {
   string key=match.Groups[1].Value;
   if(key.Contains("thumb_metacarpal"))key=key.Replace("thumb_metacarpal","thumb_proximal");
   else if(key.Contains("thumb_proximal"))key=key.Replace("thumb_proximal","thumb_intermediate");
   string human=string.Concat(key.Split('_').Select(s=>char.ToUpperInvariant(s[0])+s.Substring(1)));
   map.Add(H(match.Groups[2].Value,human));
  }
  Need(map.Count==54,"Expected 54 VRoid Humanoid mappings; found "+map.Count);
  var importer=(ModelImporter)AssetImporter.GetAtPath(Model);Need(importer!=null,"Production FBX missing");Rig(importer,map.ToArray(),false);
  foreach(var pair in mats)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
  importer.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();
  // Five representative roles are authored against the current VRoid bind skeleton.
  // The model Avatar is shared by these clips; the retired model is never instantiated.
  string animation=Folder+"/Animations/MATTO_VRoid_Actions.fbx";
  var ai=(ModelImporter)AssetImporter.GetAtPath(animation);Need(ai!=null,"VRoid representative motion FBX missing");
  ai.bakeAxisConversion=false;ai.globalScale=1;ai.animationType=ModelImporterAnimationType.Human;
  ai.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;ai.sourceAvatar=avatar;ai.importAnimation=true;
  ai.materialImportMode=ModelImporterMaterialImportMode.None;ai.SaveAndReimport();
  var clips=ai.defaultClipAnimations;
  foreach(var c in clips){c.loopTime=true;c.loopPose=false;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.keepOriginalPositionY=true;}
  ai.clipAnimations=clips;ai.SaveAndReimport();
  var motions=AssetDatabase.LoadAllAssetsAtPath(animation).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
  foreach(var role in new[]{"Idle","Walk","Investigate","Suspect","Deduction"})Need(motions.Count(c=>c.name.Contains("MATTO_"+role))==1,"Missing representative "+role);
  string controllerPath=Folder+"/Animations/MattoVroid.controller";
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
  controller.parameters=new[]{new AnimatorControllerParameter{name="CharacterState",type=AnimatorControllerParameterType.Int}};
  var layers=controller.layers;layers[0].iKPass=true;layers[0].defaultWeight=1;controller.layers=layers;
  var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);foreach(var transition in sm.anyStateTransitions)sm.RemoveAnyStateTransition(transition);
  foreach(CharacterState state in Enum.GetValues(typeof(CharacterState))) {
   string role=state==CharacterState.Move?"Walk":state==CharacterState.Investigate||state==CharacterState.Cooperate?"Investigate":state==CharacterState.Suspect?"Suspect":state==CharacterState.Deduction||state==CharacterState.UseAbility?"Deduction":"Idle";
   var s=sm.AddState(state.ToString());s.motion=motions.Single(c=>c.name.Contains("MATTO_"+role));s.writeDefaultValues=true;
   var t=sm.AddAnyStateTransition(s);t.hasExitTime=false;t.duration=.1f;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.Equals,(int)state,"CharacterState");if(state==CharacterState.Idle)sm.defaultState=s;
  }
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));instance.name="CHR_MATTO";
  try {
   var animator=instance.GetComponent<Animator>();animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
   var all=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);Need(all.Length==21,"Expected seven parts at three LODs; got "+all.Length);
   foreach(var r in all){r.localBounds=new Bounds(Vector3.up*.9f,new Vector3(2.2f,2.3f,1.5f));r.updateWhenOffscreen=false;r.gameObject.SetActive(true);for(int i=0;i<r.sharedMesh.blendShapeCount;i++)r.SetBlendShapeWeight(i,0);}
   var lods=new LOD[3];float[] thresholds={.075f,.033f,.005f};
   for(int i=0;i<3;i++)lods[i]=new LOD(thresholds[i],all.Where(r=>r.name.EndsWith("_LOD"+i)).ToArray());
   var group=instance.GetComponent<LODGroup>()??instance.AddComponent<LODGroup>();group.SetLODs(lods);group.localReferencePoint=Vector3.up*.9f;group.size=1.80f;
   instance.AddComponent<MattoVroidPresentation>();
   var prefab=PrefabUtility.SaveAsPrefabAsset(instance,Folder+"/Prefabs/CHR_MATTO.prefab");
   var catalog=PresentationCatalog.Load();catalog.CharacterPrefabs[3]=prefab;EditorUtility.SetDirty(catalog);
   File.WriteAllLines(Path.Combine(Root,"unity-import-results.txt"),new[]{"API="+ApiVersion,"Avatar valid="+avatar.isValid+" human="+avatar.isHuman,"Mappings="+map.Count,"SMRs="+all.Length+" selected per LOD=7","Materials="+all.SelectMany(r=>r.sharedMaterials).Distinct().Count(),"Textures="+desc.images.Select(i=>i.file).Distinct().Count()+" max=2048; Android ASTC4x4 except the small linear palette","Animation source="+animation,"Source=approved VRoid; no discarded pilot mesh"}.Concat(lods.Select((l,i)=>"LOD"+i+" tris="+l.renderers.Cast<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3))).Concat(all.Select(r=>r.name+" bone palette="+r.bones.Length)).Concat(motions.Select(c=>c.name+" human="+c.humanMotion+" length="+c.length)));
  } finally {UnityEngine.Object.DestroyImmediate(instance);}
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
 }
}
#endif
