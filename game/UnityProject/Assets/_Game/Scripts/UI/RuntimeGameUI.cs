using System;
using System.Collections.Generic;
using DeductionGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DeductionGame {
 public sealed class RuntimeGameUI : MonoBehaviour {
 GameManager gm;
 Text status,log,notes,result,choices;
 GameObject resultPanel;
 readonly List<Button> turnButtons=new();
 Button setup;
 int weapon,suspect,location,room=1,target=1;
 Font font;
 public bool Ready => status!=null;
 public void ResetSelections(){weapon=suspect=location=0;room=target=1;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(FindFirstObjectByType<RuntimeGameUI>()==null)new GameObject("RuntimeGameUI").AddComponent<RuntimeGameUI>();}
 void Start(){
 gm=GameManager.Instance;if(gm==null)return;
 font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
 var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
 var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0.5f;
 gameObject.AddComponent<GraphicRaycaster>();
 if(EventSystem.current==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
 var left=Panel("Actions",new Vector2(0,0),new Vector2(0.26f,1));
 var scroll=left.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
 var viewport=Node("Viewport",left);Stretch(viewport);viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
 var content=Node("Content",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0.5f,1);content.offsetMin=Vector2.zero;content.offsetMax=Vector2.zero;
 var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(12,12,12,12);layout.spacing=5;layout.childControlHeight=true;layout.childForceExpandHeight=false;
 content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;
 status=Label("Status",content,180);choices=Label("Choices",content,130);
 ButtonAt("Weapon", "무기 선택 +",content,()=>weapon=(weapon+1)%CardRules.Weapons.Length);
 ButtonAt("Suspect", "범인 선택 +",content,()=>suspect=(suspect+1)%6);
 ButtonAt("Location", "추리 장소 +",content,()=>location=(location+1)%9);
 ButtonAt("Room", "이동/능력 대상 방 +",content,()=>room=room%9+1);
 ButtonAt("Target", "의심/협조/교란 대상 +",content,()=>target=target%5+1);
 ButtonAt("Move", "선택한 방으로 이동",content,()=>gm.TryMoveHuman(room));
 setup=ButtonAt("Setup", "초기 배치 완료",content,()=>gm.CompleteHumanSetup());
 Turn("Investigate","과학수사 확인",content,()=>gm.HumanCheckForensics());
 Turn("SuspectAction","의심",content,()=>gm.HumanSuspicion(target,CardRules.Weapons[weapon]));
 Turn("Cooperate","협조",content,()=>gm.HumanCooperation(target));
 Turn("Deduction","추리 실행",content,()=>{if(gm.Human.IsCulprit)gm.HumanFakeDeduction();else gm.HumanDeduction();});
 Turn("Ability","능력 사용/준비",content,()=>gm.HumanUseAbilityPrimary());
 Turn("AnalyzeLocation","정보분석: 장소",content,()=>gm.HumanAnalyzeLocation());
 Turn("AnalyzeWeapon","정보분석: 무기",content,()=>gm.HumanAnalyzeWeapon());
 Turn("Lock","범인: 잠금",content,()=>gm.HumanCulpritLock());
 Turn("Disrupt","범인: 교란",content,()=>gm.HumanCulpritDisrupt());
 Turn("Block","범인: 경로차단",content,()=>gm.HumanCulpritRouteBlock());
 Turn("Smoke","범인: 연막",content,()=>gm.HumanCulpritSmoke());
 Turn("EndTurn","턴 종료",content,()=>gm.EndHumanTurn());
 ButtonAt("Fallback","개발 OnGUI 전환 (F10)",content,()=>gm.DebugUI=!gm.DebugUI);
 var right=Panel("PublicLog",new Vector2(0.77f,0.52f),Vector2.one);log=ScrollableText("Log",right);
 var lower=Panel("PrivateInfo",new Vector2(0.77f,0),new Vector2(1,0.51f));notes=ScrollableText("Notes",lower);
 resultPanel=Panel("Result",new Vector2(0.3f,0.3f),new Vector2(0.73f,0.7f)).gameObject;
 var resultLayout=resultPanel.AddComponent<VerticalLayoutGroup>();resultLayout.childForceExpandHeight=false;
 result=Label("ResultText",resultPanel.transform,190);
 ButtonAt("Restart","새 게임",resultPanel.transform,()=>gm.StartNewGame());
 gm.DebugUI=false;
 Refresh();
 }
 Text ScrollableText(string id,RectTransform panel) {
 var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
 var viewport=Node("Viewport",panel);Stretch(viewport);viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
 var text=Label(id,viewport,0);var rect=text.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(0.5f,1);rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
 text.verticalOverflow=VerticalWrapMode.Overflow;text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=rect;return text;
 }
 void Turn(string id,string title,Transform root,Action action){turnButtons.Add(ButtonAt(id,title,root,action));}
 Button ButtonAt(string id,string title,Transform root,Action action){
 var rect=Node(id,root);rect.gameObject.AddComponent<Image>().color=new Color(0.25f,0.25f,0.25f,1);
 var button=rect.gameObject.AddComponent<Button>();rect.gameObject.AddComponent<LayoutElement>().preferredHeight=36;
 var caption=Label("Caption",rect,0);caption.text=title;caption.alignment=TextAnchor.MiddleCenter;Stretch(caption.rectTransform);
 button.onClick.AddListener(()=>{gm.SetUISelection(weapon,suspect,location,room,target);action();Refresh();});return button;
 }
 RectTransform Panel(string id,Vector2 min,Vector2 max){var rect=Node(id,transform);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);rect.gameObject.AddComponent<Image>().color=new Color(0.12f,0.12f,0.12f,0.95f);return rect;}
 RectTransform Node(string id,Transform parent){var go=new GameObject(id,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
 Text Label(string id,Transform parent,float height){var rect=Node(id,parent);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.fontSize=20;text.color=Color.white;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
 if(height>0)rect.gameObject.AddComponent<LayoutElement>().preferredHeight=height;return text;}
 static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);}
 void Update(){if(gm!=null && Input.GetKeyDown(KeyCode.F10))gm.DebugUI=!gm.DebugUI;Refresh();}
 public void Refresh(){if(gm==null || gm.Human==null || status==null)return;var h=gm.Human;
 status.text=$"라운드 {gm.Round} / {gm.Phase}\\n턴: {(gm.TurnIndex<gm.Players.Count?gm.Players[gm.TurnIndex].Name:"-")}\\n내 위치: {MapData.GetRoom(h.RoomId).Name} / {h.MovementPoints}P\\n역할: {(h.IsCulprit?"범인":"수사자")}\\n능력: {GameDefinitions.AbilityNames[(int)h.Ability]} / {h.AbilityUsesLeft}회".Replace("\\n","\n");
 choices.text=$"대상: {GameDefinitions.Names[target]}\\n무기: {CardRules.Weapons[weapon]}\\n추리: {GameDefinitions.Names[suspect]} / {CardRules.Locations[location]}\\n이동/능력 방: {room} {MapData.GetRoom(room).Name}".Replace("\\n","\n");
 log.text="공개 로그\n"+string.Join("\n",gm.PublicLog);
 notes.text="내 비공개 정보\n"+h.LocationCard+" / "+h.WeaponCard+"\n"+string.Join("\n",h.PrivateNotes);
 bool turn=gm.Phase==GamePhase.Round && gm.TurnIndex==0 && !h.Spectating;
 foreach(var button in turnButtons)button.interactable=turn;
 setup.interactable=gm.Phase==GamePhase.SetupMove && gm.TurnIndex==0;
 resultPanel.SetActive(gm.Phase==GamePhase.GameOver);result.text=gm.Result;
 }
 }
}
