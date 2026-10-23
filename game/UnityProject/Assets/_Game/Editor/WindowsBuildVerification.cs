#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class WindowsBuildVerification {
 public static void BuildDevelopment(){Build(true);}
 public static void BuildRelease(){Build(false);}
 static void Build(bool development){
 string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../Builds/Windows",development?"Development":"Release"));
 foreach(string arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--build-output="))output=arg.Substring(15);
 foreach(var theme in new[]{"Mansion","Cruise","Museum"}) {
 if(theme=="Mansion")ProjectBootstrap.Mansion();else if(theme=="Cruise")ProjectBootstrap.Cruise();else ProjectBootstrap.Museum();
 string destination=Path.Combine(output,theme,"DeductionGame.exe");Directory.CreateDirectory(Path.GetDirectoryName(destination));
 var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/_Game/Scenes/Game.unity"},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=development?BuildOptions.Development:BuildOptions.None});
 if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+theme+" "+report.summary.result);
 Debug.Log($"WINDOWS_BUILD: OK {theme} bytes={report.summary.totalSize} path={destination}");
 }
 }
}
#endif
