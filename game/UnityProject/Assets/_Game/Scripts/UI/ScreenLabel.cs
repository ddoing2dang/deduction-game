using UnityEngine;
using UnityEngine.UI;
namespace DeductionGame {
 // Use the same runtime font/render path as Canvas UI: player IMGUI box text can be blank.
 public sealed class ScreenLabel {
 Text text; RectTransform rect; Canvas canvas;
 public void Hide(){if(text!=null)rect.gameObject.SetActive(false);}
 public void Dispose(){if(rect!=null)Object.Destroy(rect.gameObject);}
 public void Draw(Vector3 screen,string value,float width,Vector2 offset){
 if(text==null){
 var ui=Object.FindFirstObjectByType<RuntimeGameUI>();if(ui==null || !ui.Ready)return;
 canvas=ui.GetComponent<Canvas>();
 var root=ui.transform.Find("WorldLabels");
 if(root==null){var go=new GameObject("WorldLabels",typeof(RectTransform));go.transform.SetParent(ui.transform,false);go.transform.SetAsFirstSibling();var r=go.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;root=go.transform;}
 var background=new GameObject("ScreenLabel",typeof(RectTransform),typeof(Image));background.transform.SetParent(root,false);rect=background.GetComponent<RectTransform>();
 var image=background.GetComponent<Image>();image.color=new Color(0,0,0,0.7f);image.raycastTarget=false;
 var label=new GameObject("Text",typeof(RectTransform),typeof(Text));label.transform.SetParent(rect,false);text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
 var tr=label.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
 }
 RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,screen,null,out var point);
 rect.anchoredPosition=point+offset/canvas.scaleFactor;rect.sizeDelta=new Vector2(width,26);text.text=value;rect.gameObject.SetActive(true);
 }
 }
}
