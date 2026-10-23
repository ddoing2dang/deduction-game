from pathlib import Path
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--target',type=Path,required=True)
a=parser.parse_args()
p=a.source/'Assets/_Game/Editor'
out=a.target/'Assets/_Verification'
out.mkdir(exist_ok=True)
s=(p/'SinglePlayerE2E.cs').read_text(encoding='utf-8').replace('#if UNITY_EDITOR','#if DEVELOPMENT_BUILD && !UNITY_EDITOR').replace('using UnityEditor;','').replace('[InitializeOnLoad]','').replace('SinglePlayerE2E','PlayerLoopVerification')
start=s.index(' static PlayerLoopVerification()');end=s.index(' static object Get(',start)
s=s[:start]+''' [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--verify-loop")>=0)new GameObject("PlayerLoopVerification").AddComponent<PlayerLoopVerificationHost>();}
 public static void Execute(){frames=30;Run();}
'''+s[end:]
s=s.replace('EditorApplication.update-=Run;SessionState.SetBool("SingleE2E",false);','').replace('EditorApplication.Exit(','Application.Quit(')
s+='''\n#if DEVELOPMENT_BUILD && !UNITY_EDITOR
public sealed class PlayerLoopVerificationHost : UnityEngine.MonoBehaviour {
 System.Collections.IEnumerator Start(){yield return null;yield return null;PlayerLoopVerification.Execute();}
}
#endif
'''
(out/'PlayerLoopVerification.cs').write_text(s,encoding='utf-8')
for name in ['CharacterActorVerification','PresentationVerification','UsabilityVerification']:
 s=(p/(name+'.cs')).read_text(encoding='utf-8').replace('#if UNITY_EDITOR','#if DEVELOPMENT_BUILD && !UNITY_EDITOR').replace('using UnityEditor;','').replace('EditorApplication.Exit(','Application.Quit(')
 s=s.replace('EditorApplication.isPaused=false;','').replace('EditorApplication.QueuePlayerLoopUpdate();','').replace('EditorApplication.update += Pump;','')
 if name=='UsabilityVerification':
  capture=s.index(' static void Capture(');capture_end=s.index(' static IEnumerator Run()',capture)
  s=s[:capture]+' static void Capture(string label){}\n'+s[capture_end:]
  start=s.index('public static void Begin()');end=s.index(' static Button Button(',start)
  s=s[:start]+'''public static void Begin()=>UnityEngine.Object.FindFirstObjectByType<RuntimeBoardView>().StartCoroutine(Run());
'''+s[end:]
 (out/(name+'.cs')).write_text(s,encoding='utf-8')
(out/'PlayerDisplayVerification.cs').write_text((a.source/'Tests/Windows/PlayerDisplayVerification.cs').read_text(encoding='utf-8'),encoding='utf-8')
(out/'PlayerUIVerificationBoot.cs').write_text('''#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using UnityEngine;
public sealed class PlayerUIVerificationBoot : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--verify-ui")>=0)new GameObject("PlayerUIVerification").AddComponent<PlayerUIVerificationBoot>();}
 System.Collections.IEnumerator Start(){yield return null;yield return null;CharacterActorVerification.Begin();}
}
#endif
''',encoding='utf-8')
