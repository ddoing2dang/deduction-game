using System;
using System.Collections.Generic;
using DeductionGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DeductionGame {
 public sealed class RuntimeGameUI : MonoBehaviour {
 GameManager gm;
 Text status,log,notes,result,choices,hud,feedback;
 readonly Dictionary<Button,string> titles=new();
 readonly List<string> history=new();
 readonly List<string> important=new();
 string notice="",lastEvents=""; float noticeUntil;
 int readNotes;
 public int SelectedRoom => room;
 public void SelectRoom(int id){room=Mathf.Clamp(id,1,9);Refresh();}
 void ReceivePublic(string message){history.Add(message);if(message.Contains("목격정보") || message.Contains("추가 이벤트") || message.Contains("비밀상자") || message.Contains("과학수사 결과가 도착")){important.Add(message);if(important.Count>3)important.RemoveAt(0);}}
 void OnDestroy(){if(gm!=null)gm.PublicMessage-=ReceivePublic;}
 void Notice(string message){notice=message;noticeUntil=Time.unscaledTime+4f;}
 string Events(){var parts=new List<string>();
 if(gm.ActiveBlackoutRoom>0)parts.Add($"정전: {MapData.GetRoom(gm.ActiveBlackoutRoom).Name} (이동 외 행동 제한)");
 if(gm.ActiveSmokeRoomA>0)parts.Add($"연막: {gm.ActiveSmokeRoomA}, {gm.ActiveSmokeRoomB} (행동 제한)");
 if(gm.ActiveLockedEdge!=null)parts.Add($"잠금 {gm.ActiveLockedEdge}: 통과 +1P");
 if(gm.ActiveBrokenDoorEdge!=null)parts.Add($"고장문 {gm.ActiveBrokenDoorEdge}: 통과 +1P");
 if(gm.ActiveRouteBlockedEdge!=null)parts.Add($"경로차단 {gm.ActiveRouteBlockedEdge}: 통과 시도 1회 차단");
 if(gm.SecretPassageActive)parts.Add($"비밀통로 {MapData.SecretA}↔{MapData.SecretB}: 임시 연결");
 return string.Join(" / ",parts);
 }
 GameObject resultPanel;
 readonly List<Button> turnButtons=new();
 Button setup;
 int weapon,suspect,location,room=1,target=1;
 Font font;
 public bool Ready => status!=null;
 public void ResetSelections(){weapon=suspect=location=0;room=target=1;history.Clear();important.Clear();readNotes=0;notice="";noticeUntil=0;lastEvents="";if(Ready){transform.Find("Actions").GetComponent<ScrollRect>().verticalNormalizedPosition=1;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(FindFirstObjectByType<RuntimeGameUI>()==null)new GameObject("RuntimeGameUI").AddComponent<RuntimeGameUI>();}
 void Start(){
 gm=GameManager.Instance;if(gm==null)return;
 history.AddRange(gm.PublicLog);gm.PublicMessage+=ReceivePublic;
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
 status=Label("Status",content,230);choices=Label("Choices",content,180);
 hud=Label("Guide",Panel("GuidePanel",new Vector2(0.27f,0.81f),new Vector2(0.76f,1)),0);Stretch(hud.rectTransform);
 var guidePanel=transform.Find("GuidePanel");
 Turn("EndTurn","턴 종료",guidePanel,()=>gm.EndHumanTurn());
 Turn("Deduction","추리 실행",guidePanel,()=>{if(gm.Human.IsCulprit)gm.HumanFakeDeduction();else gm.HumanDeduction();});
 var endRect=guidePanel.Find("EndTurn").GetComponent<RectTransform>();endRect.anchorMin=new Vector2(0,0);endRect.anchorMax=new Vector2(0.49f,0);endRect.offsetMin=new Vector2(8,8);endRect.offsetMax=new Vector2(-8,72);
 var deductRect=guidePanel.Find("Deduction").GetComponent<RectTransform>();deductRect.anchorMin=new Vector2(0.5f,0);deductRect.anchorMax=new Vector2(1,0);deductRect.offsetMin=new Vector2(8,8);deductRect.offsetMax=new Vector2(-8,72);
 hud.rectTransform.offsetMin=new Vector2(8,80);
 feedback=Label("Feedback",Panel("FeedbackPanel",new Vector2(0.27f,0),new Vector2(0.76f,0.23f)),0);Stretch(feedback.rectTransform);
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
 ButtonAt("ReadInfo","비공개 새 정보 확인",content,()=>readNotes=gm.Human.PrivateNotes.Count);
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
 string CardPlace(int index)=>CardRules.Locations[index]+(MapData.Theme==MapTheme.Mansion?"":" ("+MapData.GetRoom(index+1).Name+")");
 void Turn(string id,string title,Transform root,Action action){turnButtons.Add(ButtonAt(id,title,root,action));}
 Button ButtonAt(string id,string title,Transform root,Action action){
 var rect=Node(id,root);rect.gameObject.AddComponent<Image>().color=new Color(0.25f,0.25f,0.25f,1);
 var button=rect.gameObject.AddComponent<Button>();rect.gameObject.AddComponent<LayoutElement>().preferredHeight=70;
 var caption=Label("Caption",rect,0);caption.text=title;caption.fontSize=18;caption.alignment=TextAnchor.MiddleCenter;Stretch(caption.rectTransform);
 titles[button]=title;
 button.onClick.AddListener(()=>{
 gm.SetUISelection(weapon,suspect,location,room,target);
 string reason=turnButtons.Contains(button)?gm.ActionReason(id,target,room,weapon):"";
 if(reason!=""){Notice(reason);Refresh();return;}
 action();
 Notice(id=="Move" ? gm.LastFeedback : id=="EndTurn" ? $"AI {gm.LastAITurnCount}명 턴 처리 완료 · {gm.Round}R / {gm.Players[gm.TurnIndex].Name} 차례" : title+" 선택/처리 완료 — 현재 상태와 정보를 확인하세요.");Refresh();});return button;
 }
 RectTransform Panel(string id,Vector2 min,Vector2 max){var rect=Node(id,transform);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);rect.gameObject.AddComponent<Image>().color=new Color(0.12f,0.12f,0.12f,0.95f);return rect;}
 RectTransform Node(string id,Transform parent){var go=new GameObject(id,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
 Text Label(string id,Transform parent,float height){var rect=Node(id,parent);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.fontSize=20;text.color=Color.white;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
 if(height>0)rect.gameObject.AddComponent<LayoutElement>().preferredHeight=height;return text;}
 static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);}
 void Update(){if(gm!=null && Input.GetKeyDown(KeyCode.F10))gm.DebugUI=!gm.DebugUI;Refresh();}
 public void Refresh(){if(gm==null || gm.Human==null || status==null)return;var h=gm.Human;
 status.text=$"{(gm.Phase==GamePhase.SetupMove?"초기 배치 (라운드 전)":gm.Phase==GamePhase.GameOver?"게임 종료":$"라운드 {gm.Round}")}\\n턴: {(gm.TurnIndex<gm.Players.Count?gm.Players[gm.TurnIndex].Name:"-")}\\n내 위치: {MapData.GetRoom(h.RoomId).Name} [카드명: {CardRules.Locations[h.RoomId-1]}] / {h.MovementPoints}P\\n역할: {(h.IsCulprit?"범인":"수사자")}\\n능력: {gm.HumanAbilityStatus}".Replace("\\n","\n");
 choices.text=$"대상: {GameDefinitions.Names[target]}\\n무기: {CardRules.Weapons[weapon]}\\n추리: {GameDefinitions.Names[suspect]} / {CardPlace(location)}\\n이동/능력 방: {room} {MapData.GetRoom(room).Name}".Replace("\\n","\n");
 log.text="공개 정보 · 이번 게임 전체 로그 (스크롤)\n"+string.Join("\n",history);
 notes.text=$"내 비공개 정보 · 새 정보 {Mathf.Max(0,h.PrivateNotes.Count-readNotes)}개 (확인 버튼)\n"+CardPlace(Array.IndexOf(CardRules.Locations,h.LocationCard))+" / "+h.WeaponCard+"\n"+string.Join("\n",h.PrivateNotes);
 bool turn=gm.Phase==GamePhase.Round && gm.TurnIndex==0 && !h.Spectating;
 foreach(var button in turnButtons){
 string reason=gm.ActionReason(button.name,target,room,weapon);
 button.interactable=reason=="";
 button.GetComponentInChildren<Text>().text=titles[button]+(reason!="" ? "\n"+reason : "");
 }
 string events=Events();
 if(lastEvents!="" && events!=lastEvents)Notice("지도 이벤트 상태 변경/종료: "+(events==""?"모든 임시 효과 해제":events));
 lastEvents=events;
 feedback.text=(Time.unscaledTime<noticeUntil?notice:gm.LastFeedback)+"\n"+events+"\n청록=현재 방 / 초록=이동 가능 / 노랑=선택 방\n공개 기록은 오른쪽 위, 새 비공개 정보는 오른쪽 아래에서 확인하세요.";
 string guide=gm.Phase==GamePhase.SetupMove?"초기 배치 · 연결 방으로 최대 3P 이동 → [초기 배치 완료]\n일반 행동은 R1부터 가능합니다. 왼쪽 행동 목록은 휠/드래그로 스크롤합니다.":gm.Phase==GamePhase.GameOver?"게임 종료 · 결과 창에서 [새 게임]":gm.TurnIndex!=0?"AI 턴 처리 중":h.Spectating?"관전 · [턴 종료]로 남은 게임을 진행합니다.":"내 턴 · 이동 → 정보행동 선택 → [턴 종료] (왼쪽 목록 스크롤)";
 if(gm.Round==12 && gm.Phase==GamePhase.Round)guide=(h.IsCulprit?h.FakeDeductionUsed:h.LastDeductionRound==12)?"R12 최종 추리 완료 · 턴 종료 가능":"R12 최종 추리 필수 · 추리를 완료한 뒤 턴을 종료하세요.";
 hud.text=$"{guide}\n{(gm.Round==0?"초기 배치":$"R{gm.Round}")} · 현재 {gm.Players[Mathf.Min(gm.TurnIndex,5)].Name} · {MapData.GetRoom(h.RoomId).Name} · {h.MovementPoints}P · 정보행동 {(h.InfoActionUsed?"사용 완료":"미사용")}\n추리: {(gm.Round<6?"R6부터 가능":h.IsCulprit?(h.FakeDeductionUsed?"가짜추리 완료":"가짜추리 1회 필요"):$"남은 {2-h.DeductionAttempts}회")} / 카메라 1·2·3 = 근접·기본·전체 / F10 개발 화면";
 choices.text+=$"\n확인: {GameDefinitions.Names[suspect]} + {CardPlace(location)} + {CardRules.Weapons[weapon]}\n이동 선택: {gm.MoveReason(room)}";
 status.text+=$"\n정보행동: {(h.InfoActionUsed?"사용":"미사용")} / 내 추리 잔여 {(h.IsCulprit?(h.FakeDeductionUsed?0:1):2-h.DeductionAttempts)}회";
 setup.interactable=gm.Phase==GamePhase.SetupMove && gm.TurnIndex==0;
 resultPanel.SetActive(gm.Phase==GamePhase.GameOver);result.text=gm.Result;
 }
 }
}
