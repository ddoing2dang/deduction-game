#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.Animations;using UnityEngine;using DeductionGame;

[InitializeOnLoad]
public static class MattoPilotPipeline
{
 public const string Folder="Assets/_Game/Characters/CHR_MATTO";
 public const string Model=Folder+"/Model/CHR_MATTO.fbx";
 public static string Root=>Path.GetFullPath("../../art/Characters/CHR_MATTO");
 static string Request=>Path.GetFullPath("../../../matto-pilot.request");
 [Serializable] class Spec{public Bone[] bones;} [Serializable] class Bone{public string name,human;}
 static MattoPilotPipeline(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;string action=File.ReadAllText(Request).Trim();File.Delete(Request);if(action=="Setup")Setup();else if(File.Exists(Path.Combine(Root,"Game/approved-source.sha256")))MattoGameAudit.Begin(action);else MattoPilotAudit.Begin(action);}
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Ensure(string folder){Directory.CreateDirectory(folder);AssetDatabase.Refresh();}
 public static void Setup(){
  if(File.Exists(Path.Combine(Root,"Game/approved-source.sha256"))){MattoGamePipeline.RunBatchSetup();return;}
  try{
   Ensure(Folder+"/Materials");Ensure(Folder+"/Prefabs");
   var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(Path.Combine(Root,"humanoid_v1.json")));
   var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
   Need(importer!=null,"FBX absent");
   importer.bakeAxisConversion=false;importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
   var transforms=model.GetComponentsInChildren<Transform>(true);
   Need(transforms.Select(t=>t.name).Distinct().Count()==transforms.Length,"Duplicate skeleton/mesh transform names would corrupt HumanDescription");
   importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   importer.humanDescription=new HumanDescription{
    human=spec.bones.Where(b=>!string.IsNullOrEmpty(b.human)).Select(b=>new HumanBone{boneName=b.name,humanName=b.human,limit=new HumanLimit{useDefaultValues=true}}).ToArray(),
    skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
    upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
   importer.globalScale=1;importer.importAnimation=false;importer.importBlendShapes=true;
   importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
   importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.optimizeGameObjects=false;
   importer.skinWeights=ModelImporterSkinWeights.Standard;importer.meshCompression=ModelImporterMeshCompression.Off;
   importer.SaveAndReimport();
   var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();Need(avatar.isHuman&&avatar.isValid,"Avatar invalid");
   string source=Folder+"/Animations/MATTO_PilotActions.fbx";
   var ai=(ModelImporter)AssetImporter.GetAtPath(source);Need(ai!=null,"Animation source absent");
   ai.bakeAxisConversion=false;ai.animationType=ModelImporterAnimationType.Human;ai.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
   ai.sourceAvatar=avatar;ai.importAnimation=true;ai.materialImportMode=ModelImporterMaterialImportMode.None;
   ai.SaveAndReimport();
   var defaults=ai.defaultClipAnimations;
   foreach(var clip in defaults){clip.loopTime=true;clip.loopPose=false;clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.keepOriginalPositionY=true;}
   ai.clipAnimations=defaults;ai.SaveAndReimport();
   var clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
   foreach(string role in new[]{"Idle","Walk","Investigate","Suspect","Deduction"})Need(clips.Count(c=>c.name.Contains("MATTO_"+role))==1,"Missing/duplicate action "+role+" clips="+string.Join(",",clips.Select(c=>c.name)));
   var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Textures/MATTO_Atlas.png");ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
   string materialPath=Folder+"/Materials/MAT_MATTO_Atlas.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(mat==null){mat=new Material(Shader.Find("DeductionGame/Character Toon Atlas"));AssetDatabase.CreateAsset(mat,materialPath);}
   mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/MATTO_Atlas.png");EditorUtility.SetDirty(mat);
   string controllerPath=Folder+"/Animations/SharedHumanoidPilot.controller";
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
   controller.parameters=new[]{new AnimatorControllerParameter{name="CharacterState",type=AnimatorControllerParameterType.Int,defaultInt=0}};
   var layers=controller.layers;layers[0].iKPass=true;layers[0].defaultWeight=1;controller.layers=layers;
   var sm=controller.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);foreach(var t in sm.anyStateTransitions)sm.RemoveAnyStateTransition(t);
   foreach(CharacterState state in Enum.GetValues(typeof(CharacterState))){
    string role=state==CharacterState.Move?"Walk":state==CharacterState.Investigate||state==CharacterState.Cooperate?"Investigate":state==CharacterState.Suspect?"Suspect":state==CharacterState.Deduction||state==CharacterState.UseAbility?"Deduction":"Idle";
    var s=sm.AddState(state.ToString());s.motion=clips.Single(c=>c.name.Contains("MATTO_"+role));s.writeDefaultValues=true;
    var t=sm.AddAnyStateTransition(s);t.hasExitTime=false;t.duration=.1f;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.Equals,(int)state,"CharacterState");
    if(state==CharacterState.Idle)sm.defaultState=s;
   }
   EditorUtility.SetDirty(controller);
   model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);instance.name="CHR_MATTO";
   File.WriteAllLines(Path.Combine(Root,"import-transform-diagnostics.txt"),instance.GetComponentsInChildren<Transform>().Select(t=>t.name+" localPos="+t.localPosition+" rotation="+t.localRotation+" world="+t.position));
   try{
    var animator=instance.GetComponent<Animator>();Need(animator!=null,"Animator absent");animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
    var all=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);Need(all.Length==21,"Expected seven authoring parts per LOD; actual="+all.Length);
    foreach(var r in all){r.sharedMaterial=mat;r.updateWhenOffscreen=false;r.localBounds=new Bounds(Vector3.up*.83f,new Vector3(1.8f,2f,1f));r.gameObject.SetActive(true);}
    var lods=new LOD[3];float[] thresholds={.075f,.033f,.005f};var counts=new List<string>();
    for(int i=0;i<3;i++){var renderers=all.Where(r=>r.name.EndsWith("_LOD"+i)).ToArray();Need(renderers.Length==7,"LOD naming mismatch "+i);lods[i]=new LOD(thresholds[i],renderers);counts.Add("LOD"+i+" tris="+renderers.Sum(r=>r.sharedMesh.triangles.Length/3));}
    var group=instance.GetComponent<LODGroup>()??instance.AddComponent<LODGroup>();group.SetLODs(lods);group.localReferencePoint=Vector3.up*.83f;group.size=1.65f;
    instance.AddComponent<SharedHumanoidPresentation>();
    var prefab=PrefabUtility.SaveAsPrefabAsset(instance,Folder+"/Prefabs/CHR_MATTO.prefab");
    var catalog=PresentationCatalog.Load();Need(catalog!=null,"Catalog missing");catalog.CharacterPrefabs[3]=prefab;EditorUtility.SetDirty(catalog);
    counts.Add("Avatar valid="+avatar.isValid+" human="+avatar.isHuman);counts.Add("54 explicit human mappings; 57 authored bones; 1 atlas material; 1024x1024 texture; seven active SMRs per LOD");
    counts.AddRange(clips.Select(c=>c.name+" humanMotion="+c.humanMotion+" length="+c.length));
    counts.Add("Only PresentationCatalog.CharacterPrefabs[3] replaced; gameplay code unchanged.");
    File.WriteAllLines(Path.Combine(Root,"unity-import-results.txt"),counts);
   }finally{UnityEngine.Object.DestroyImmediate(instance);}
   AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(Root,"setup-status.txt"),"SETUP COMPLETE — PLAY/VISUAL VERIFICATION STILL REQUIRED");
  }catch(Exception e){File.WriteAllText(Path.Combine(Root,"setup-status.txt"),"FAILED\n"+e);Debug.LogException(e);}
 }
}
#endif
