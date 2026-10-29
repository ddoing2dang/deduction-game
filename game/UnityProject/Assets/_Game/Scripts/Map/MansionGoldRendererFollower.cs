using UnityEngine;
namespace DeductionGame {
 [DefaultExecutionOrder(1200)]
 public sealed class MansionGoldRendererFollower:MonoBehaviour {
  public Renderer Source;
  Renderer[] own;
  void Awake(){own=GetComponentsInChildren<Renderer>(true);}
  public void RefreshVisibility(){
   if(own==null)own=GetComponentsInChildren<Renderer>(true);
   foreach(var r in own)r.enabled=Source!=null&&Source.enabled;
  }
  void LateUpdate(){RefreshVisibility();}
 }
}
