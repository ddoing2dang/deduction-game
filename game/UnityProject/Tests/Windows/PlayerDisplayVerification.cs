#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DeductionGame;
public sealed class PlayerDisplayVerification : MonoBehaviour {
 int errors;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--verify-display")>=0){var go=new GameObject("PlayerDisplayVerification");DontDestroyOnLoad(go);go.AddComponent<PlayerDisplayVerification>();}}
 void Awake(){Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert)errors++;};}
 void Check(bool ok,string label){if(!ok)throw new Exception("Player display: "+label);Debug.Log("[Player verified] "+label);}
 IEnumerator Start(){yield return null;yield return null;
 var scrolling=FindFirstObjectByType<RuntimeGameUI>().transform.Find("Actions").GetComponent<ScrollRect>();
 float before=scrolling.content.anchoredPosition.y;
 scrolling.OnScroll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){scrollDelta=new Vector2(0,-1)});
 yield return null;
 try{Check(scrolling.content.anchoredPosition.y>before+5,"Usable wheel scroll distance");scrolling.verticalNormalizedPosition=1;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}

 foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(800,600)}){
 Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(0.5f);
 try {Check(Screen.width==size.x && Screen.height==size.y,"Window resolution "+size);var ui=FindFirstObjectByType<RuntimeGameUI>();Canvas.ForceUpdateCanvases();
 foreach(string id in new[]{"GuidePanel","Actions","PrivateInfo","PublicLog","FeedbackPanel"}){
 var corners=new Vector3[4];ui.transform.Find(id).GetComponent<RectTransform>().GetWorldCorners(corners);
 Check(corners[0].x>=-1 && corners[0].y>=-1 && corners[2].x<=Screen.width+1 && corners[2].y<=Screen.height+1,"UI bounds "+size+" "+id);}
 }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
 }
 Screen.SetResolution(1920,1080,FullScreenMode.FullScreenWindow);yield return new WaitForSecondsRealtime(1);
 try{Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Borderless fullscreen");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
 float sum=0,max=0;for(int i=0;i<120;i++){yield return null;float dt=Time.unscaledDeltaTime;sum+=dt;max=Mathf.Max(max,dt);}
 try{Check(120/sum>15 && max<1,"Stable warmed frames fps="+(120/sum)+" max="+max);}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
 int baseline=FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
 for(int i=0;i<10;i++){GameManager.Instance.StartNewGame();yield return null;yield return null;
 try {Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1,"One GameManager after restart");Check(FindObjectsByType<RuntimeGameUI>(FindObjectsSortMode.None).Length==1,"One UI");Check(FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Length==6,"Six actors");Check(FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"One camera");Check(FindObjectsByType<MansionEventView>(FindObjectsSortMode.None).Length==1,"One event view");Check(FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length==baseline,"Stable object count "+baseline);}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
 }
 SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);yield return null;yield return null;yield return null;
 try{Check(FindFirstObjectByType<RuntimeGameUI>()!=null,"UI restored after scene reload");Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1,"One DontDestroy manager after scene reload");Check(FindObjectsByType<CharacterActor>(FindObjectsSortMode.None).Length==6,"Six actors after scene reload");Check(GameManager.Instance.Round==0,"Fresh scene restart state");Check(errors==0,"No runtime exceptions/errors");Debug.Log("PLAYER_DISPLAY_VERIFICATION: OK "+MapData.Theme);Application.Quit(0);}catch(Exception e){Debug.LogException(e);Application.Quit(1);}
 }
}
#endif
