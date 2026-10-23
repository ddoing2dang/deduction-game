using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeductionGame {
 // RuntimeInitializeOnLoadMethod alone is not invoked by later scene reloads.
 public static class ScenePresentationBootstrap {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Register(){SceneManager.sceneLoaded-=Restore;SceneManager.sceneLoaded+=Restore;}
 static void Restore(Scene scene,LoadSceneMode mode){
 PcCameraController.Attach();MansionCutawayView.Attach();MansionEventView.Attach();RuntimeGameUI.Attach();
 if(GameManager.Instance!=null)GameManager.Instance.RestoreAfterSceneReload();
 }
 }
}
