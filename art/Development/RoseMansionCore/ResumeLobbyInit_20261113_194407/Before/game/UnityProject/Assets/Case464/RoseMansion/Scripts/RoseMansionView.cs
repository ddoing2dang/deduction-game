using System;
using System.Collections.Generic;
using System.Linq;
using Case464.Hotel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Case464.RoseMansion
{
    [DisallowMultipleComponent]
    public sealed class RoseMansionView : MonoBehaviour
    {
        [SerializeField] private RoseMansionSession session;
        [SerializeField] private HotelMapCameraController mapCamera;
        [SerializeField] private TMP_FontAsset regularFont, headingFont;
        private RectTransform generated;
        private string preview, window="", roomTab="", focusRecord="", compareA="", compareB="", notice="";
        private string accusationNpc="MATTO", who="", method="", weapon="";
        private int roomFilter, personFilter, dayFilter;
        private bool confirming, newGameConfirm, toolsOpen, rendering;
        private readonly Color surface=new Color(.065f,.067f,.082f,.98f), elevated=new Color(.105f,.106f,.126f,1);
        private readonly Color ink=new Color(.91f,.89f,.86f), muted=new Color(.60f,.62f,.67f), gold=new Color(.77f,.64f,.43f), burgundy=new Color(.36f,.12f,.18f);
        private RoseCatalog Catalog=>session.Catalog;
        private RoseProgress State=>session.State;
        public void Configure(RoseMansionSession owner,HotelMapCameraController camera,TMP_FontAsset regular,TMP_FontAsset heading)
        {session=owner;mapCamera=camera;regularFont=regular;headingFont=heading;}
        private void Start()
        {
            if(!Application.isPlaying)return;
            preview=null;session.Changed+=Render;Render();
        }
        private void OnDestroy(){if(session!=null)session.Changed-=Render;}
        // Editor authoring draws widgets only. It never starts a case or runs progression.
        public void BuildAuthoringPreview(string screen="Main"){preview=screen;Render();}
        private RectTransform Box(Transform parent,string id,float x,float y,float w,float h,Color? color=null)
        {
            var go=new GameObject(id,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            if(color.HasValue)go.AddComponent<Image>().color=color.Value;
            return r;
        }
        private TMP_Text Text(Transform parent,string value,float x,float y,float w,float h,int size=22,Color? color=null,bool heading=false)
        {
            var r=Box(parent,"Text",x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=(heading?headingFont:regularFont)??TMP_Settings.defaultFontAsset;t.text=value??"";
            t.fontSize=size;t.color=color??ink;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;t.richText=false;
            t.alignment=TextAlignmentOptions.TopLeft;return t;
        }
        private Button Button(Transform parent,string label,float x,float y,float w,float h,Action action,bool enabled=true,bool accent=false)
        {
            var r=Box(parent,"Button | "+label,x,y,w,h,accent?burgundy:elevated);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.14f,1.12f,1.10f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.5f,.5f,.5f,.6f);b.colors=colors;
            var t=Text(r,label,14,Mathf.Max(9,(h-26)/2),w-28,h-10,21,enabled?ink:muted);t.alignment=TextAlignmentOptions.TopLeft;
            if(action!=null)b.onClick.AddListener(()=>action());return b;
        }
        private TMP_InputField Input(Transform parent,string value,string placeholder,float x,float y,float w,float h,Action<string> changed,bool multiline=false)
        {
            var r=Box(parent,"Input",x,y,w,h,elevated);var field=r.gameObject.AddComponent<TMP_InputField>();
            var viewport=Box(r,"Viewport",12,8,w-24,h-16);viewport.gameObject.AddComponent<RectMask2D>();
            var text=Text(viewport,"",0,0,w-24,h-16,21);var prompt=Text(viewport,placeholder,0,0,w-24,h-16,21,muted);
            field.textViewport=viewport;field.textComponent=(TextMeshProUGUI)text;field.placeholder=prompt;
            field.lineType=multiline?TMP_InputField.LineType.MultiLineNewline:TMP_InputField.LineType.SingleLine;
            field.richText=false;field.characterLimit=multiline?4000:256;field.SetTextWithoutNotify(value??"");
            if(changed!=null)field.onEndEdit.AddListener(s=>changed(s));return field;
        }
        private RectTransform Scroll(Transform parent,float x,float y,float w,float h,float contentHeight)
        {
            var root=Box(parent,"Scroll",x,y,w,h);var scroll=root.gameObject.AddComponent<ScrollRect>();
            var viewport=Box(root,"Viewport",0,0,w,h);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Box(viewport,"Content",0,0,w,Mathf.Max(h,contentHeight));
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        private void Render()
        {
            if(rendering||session==null)return;rendering=true;
            try
            {
                if(generated==null)generated=transform.Find("Generated investigation UI") as RectTransform;
                if(generated!=null){generated.gameObject.SetActive(false);if(Application.isPlaying)Destroy(generated.gameObject);else DestroyImmediate(generated.gameObject);}
                generated=Box(transform,"Generated investigation UI",0,0,1920,1080);
                string screen=preview??(session.MainMenu?"Main":State==null?"Main":State.phase.ToString());
                Header(screen);
                if(screen=="Main")MainScreen();
                else if(screen=="Opening")OpeningScreen();
                else if(screen=="Lobby")LobbyScreen();
                else if(screen=="Room")RoomScreen();
                else if(screen=="LobbyReview")ReviewScreen();
                else if(screen=="Accusation")AccusationScreen();
                else if(screen=="Ending")EndingScreen();
                else if(screen=="Report")ReportScreen();
                else if(screen=="Notebook")NotebookScreen();
                if(screen!="Main")Tools();
                if(window=="Notebook")NotebookScreen();
                else if(window=="Archive")ArchiveScreen();
                else if(window=="Settings")SettingsScreen();
                else if(window=="Inventory")InventoryScreen();
                if(confirming)ConfirmScreen();
                if(newGameConfirm)NewGameConfirmation();
            }
            finally{rendering=false;}
        }
        private void Header(string screen)
        {
            var p=Box(generated,"Header",24,22,1872,90,surface);
            Text(p,"사건파일 No.464",24,16,600,32,27,gold,true);
            Text(p,"장미대저택  /  수사 기록",25,52,700,26,17,muted);
            string progress=screen=="Main"?"ROSE MANSION":State==null?"UI 미리보기 · 게임 미실행":State.Day+"일차 · "+(State.Period==0?"오전":"오후")+"   /   최대 10회 조사";
            Text(p,progress,1130,27,700,40,24,ink,true);
        }
        private RectTransform RightPanel(string title,string subtitle)
        {
            var p=Box(generated,title,1360,132,536,924,surface);
            Text(p,title,28,24,480,48,31,gold,true);Text(p,subtitle,28,79,480,72,19,muted);return p;
        }
        private void MainScreen()
        {
            var p=RightPanel("장미대저택","증언을 읽고, 기록을 비교하고, 당신의 결론을 남기세요.");
            string[] labels={"새 게임","이어하기","기록도감","환경설정","게임 종료"};
            Action[] actions={StartNew,()=>{window="";session.ContinueGame();},()=>{window="Archive";Render();},()=>{window="Settings";Render();},()=>Application.Quit()};
            for(int i=0;i<labels.Length;i++)Button(p,labels[i],28,198+i*82,480,64,actions[i],i!=1||session.HasContinue,i==0);
            Text(p,"5일 · 오전/오후 10회\n캐릭터 없이 객실과 기록으로 진행",28,648,480,86,22);
            Text(p,Catalog.InvestigationReady?"사전 승인된 사건 자료만 사용합니다.":"콘텐츠 연결 대기\n6개 사건 정답·증거·진술은 미승인 상태입니다.\n임의 증거 생성 및 미승인 정보 소모를 차단합니다.",28,766,480,130,18,muted);
        }
        private void StartNew()
        {if(session.HasContinue){newGameConfirm=true;Render();}else{window="";roomTab="";session.NewGame();}}
        private void NewGameConfirmation()
        {
            var p=Modal("새 회차 시작",710,370);
            Text(p,"기존 회차의 조사 진행은 새 로비 저장으로 바뀝니다.\n기록도감 18칸은 보존됩니다.",30,90,650,120,24);
            Button(p,"취소",30,265,300,62,()=>{newGameConfirm=false;Render();});
            Button(p,"새 게임 시작",365,265,310,62,()=>{newGameConfirm=false;window="";session.NewGame();},true,true);
        }
        private void OpeningScreen()
        {
            var p=RightPanel("수사 시작","오프닝 연출 연결");
            Text(p,"2층 끝에서 로비로 이어지는 오프닝 자료는 아직 연결되지 않았습니다.\n\n현재는 캐릭터 없는 로비 화면으로 진입합니다. 이동 컷신을 제작·재생한 것으로 처리하지 않습니다.",28,190,480,260,24);
            Button(p,"로비 화면으로",28,700,480,70,()=>session.EnterLobbyAfterOpening(),true,true);
        }
        private void MapControls()
        {
            Button(generated,"전체 맵",36,134,170,48,()=>mapCamera.Show(HotelMapView.SplitFloors));
            Button(generated,"1층",216,134,140,48,()=>mapCamera.Show(HotelMapView.FirstFloor));
            Button(generated,"2층",366,134,140,48,()=>mapCamera.Show(HotelMapView.SecondFloor));
        }
        private void LobbyScreen()
        {
            MapControls();var p=RightPanel("현재 장소 · 로비","어디로 이동하시겠습니까?");
            int row=0;
            foreach(var room in Catalog.rooms)
            {
                var r=room;string status=State==null?(r.id=="LOBBY"?"현재 장소":"방문 가능 · 미리보기"):session.RoomStatus(r.id);
                Button(p,r.Caption+"\n"+status,28,164+row*78,480,70,()=>{roomTab="";session.Visit(r.id);},State!=null&&session.CanVisit(r.id));row++;
            }
            Text(p,session.Status,28,810,480,90,18,muted);
        }
        private RoseRoom CurrentRoom=>State==null?Catalog.Room("ROOM_MATTO"):Catalog.Room(State.currentRoom);
        private void RoomScreen()
        {
            var room=CurrentRoom;var p=RightPanel(room.Caption,"현재 객실 · "+(room.id=="CRIME_SCENE"?"피해자 정보":"인물 의심"));
            bool search=State?.searchedThisVisit??false,question=State?.questionedThisVisit??false;
            Button(p,"방 조사"+(search?" · 기본 완료":""),28,160,230,60,()=>{roomTab="Search";Render();});
            Button(p,room.id=="CRIME_SCENE"?"피해자 시신 정보":"인물 의심"+(question?" · 기본 완료":""),270,160,238,60,()=>{roomTab="Question";Render();});
            if(string.IsNullOrEmpty(roomTab))
                Text(p,"방 조사에서 정보 1개, "+(room.id=="CRIME_SCENE"?"시신 정보":"질문")+"에서 정보 1개를 확인하세요.\n\n두 기본 행동을 모두 완료하면 로비로 돌아갈 수 있습니다.",28,258,480,230,24);
            else
            {
                var slots=room.slots.Where(s=>roomTab=="Search"?s.kind=="Search":s.kind!="Search").ToArray();
                for(int i=0;i<slots.Length;i++)
                {
                    var slot=slots[i];bool used=State!=null&&State.Room(room.id).used.Contains(slot.key);
                    bool ready=State!=null&&session.SlotContentReady(slot);
                    string label=slot.key+" · "+slot.label+"\n"+(used?"확인 완료":ready?"미확인":"데이터 연결 대기");
                    Button(p,label,28,250+i*98,480,86,()=>session.InspectSlot(slot.key),State!=null&&session.SlotAvailable(slot));
                }
            }
            Text(p,session.Status,28,566,480,220,20,muted);
            Button(p,"방 나가기",28,812,480,70,()=>{roomTab="";session.LeaveRoom();},session.CanLeave,true);
        }
        private void ReviewScreen()
        {
            var p=RightPanel("로비 · 생각 정리","오늘 실제로 획득한 정보만 정리합니다.");
            var records=(State?.records??new List<RoseRecord>()).Where(r=>r.day==(State?.Day??1)).ToArray();
            var list=Scroll(p,28,166,480,430,Mathf.Max(420,records.Length*100));
            for(int i=0;i<records.Length;i++){var r=records[i];Text(list,Source(r)+"\n"+r.title,8,i*100,455,90,21);}
            if(records.Length==0)Text(list,"아직 획득한 정보가 없습니다.",8,8,455,64,22,muted);
            Text(p,"기록을 다시 읽어 볼 시간입니다.",28,626,480,60,23,gold);
            Button(p,"수첩 펼치기",28,705,480,62,()=>{window="Notebook";Render();});
            Button(p,"생각 정리 마치기",28,802,480,76,()=>session.FinishReview(),State!=null,true);
        }
        private string Source(RoseRecord r)=>(Catalog.Room(r.roomId)?.Caption??r.roomId)+" · "+r.day+"일차 "+(r.period==0?"오전":"오후");
        private RectTransform Modal(string title,float w=1570,float h=870)
        {
            Box(generated,"Modal shade",0,0,1920,1080,new Color(0,0,0,.75f));
            var p=Box(generated,title,(1920-w)/2,(1080-h)/2,w,h,surface);Text(p,title,30,25,w-100,50,30,gold,true);return p;
        }
        private void CloseWindow(){window="";notice="";Render();}
        private void NotebookScreen()
        {
            var p=Modal("탐정의 수첩");Button(p,"닫기",1410,22,130,46,CloseWindow);
            var rooms=new[]{"전체 객실"}.Concat(Catalog.rooms.Select(r=>r.Caption)).ToArray();
            var people=new[]{"전체 인물"}.Concat(Catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).Select(r=>r.name)).ToArray();
            Button(p,"객실: "+rooms[roomFilter%rooms.Length],30,88,420,48,()=>{roomFilter=(roomFilter+1)%rooms.Length;Render();});
            Button(p,"인물: "+people[personFilter%people.Length],466,88,400,48,()=>{personFilter=(personFilter+1)%people.Length;Render();});
            Button(p,dayFilter==0?"전체 날짜":dayFilter+"일차",882,88,220,48,()=>{dayFilter=(dayFilter+1)%6;Render();});
            var records=(State?.records??new List<RoseRecord>()).Where(r=>roomFilter==0||Catalog.rooms[roomFilter-1].id==r.roomId)
                .Where(r=>personFilter==0||Catalog.rooms.Where(x=>!string.IsNullOrEmpty(x.npcId)).ElementAt(personFilter-1).npcId==r.npcId)
                .Where(r=>dayFilter==0||r.day==dayFilter).ToArray();
            var list=Scroll(p,30,160,390,450,records.Length*104);
            for(int i=0;i<records.Length;i++)
            {
                var r=records[i];Button(list,r.title+"\n"+Source(r),0,i*104,382,96,()=>{focusRecord=r.id;Render();});
            }
            if(records.Length==0)Text(list,"획득한 기록이 없습니다.\n연결 대기 항목을 증거로 만들지 않습니다.",8,8,370,120,21,muted);
            DrawRecord(p,compareA,444,160,"비교 A");DrawRecord(p,compareB,1000,160,"비교 B");
            var focused=State?.records.SingleOrDefault(r=>r.id==focusRecord);
            if(focused!=null)
            {
                Button(p,"비교 A로",30,628,180,46,()=>{if(compareB!=focused.id)compareA=focused.id;Render();});
                Button(p,"비교 B로",226,628,194,46,()=>{if(compareA!=focused.id)compareB=focused.id;Render();});
                Button(p,(focused.important?"✓ ":"")+"중요",444,628,150,46,()=>session.ToggleMark(focused.id,"important"));
                Button(p,(focused.suspicious?"✓ ":"")+"의심",602,628,150,46,()=>session.ToggleMark(focused.id,"suspicious"));
                Button(p,(focused.recheck?"✓ ":"")+"재확인",760,628,170,46,()=>session.ToggleMark(focused.id,"recheck"));
                Button(p,"시간표에 추가",946,628,245,46,()=>session.AddTimeline(focused.id));
                Input(p,focused.note,"선택한 기록에 개인 메모",444,688,746,80,s=>session.SetRecordNote(focused.id,s),true);
            }
            Input(p,State?.notebookMemo??"","개인 수사 메모",30,704,390,100,s=>session.SetMemo(s),true);
            Button(p,"수동 사건 시간표",1210,704,330,64,()=>{window="Timeline";RenderTimeline();});
            Text(p,"두 기록은 직접 선택해 비교합니다. 표시·시간표는 개인 정리이며 자동 추론하지 않습니다.",444,796,1080,46,18,muted);
        }
        private void DrawRecord(Transform parent,string id,float x,float y,string caption)
        {
            var p=Box(parent,caption,x,y,540,450,elevated);Text(p,caption,18,16,504,34,23,gold,true);
            var r=State?.records.SingleOrDefault(item=>item.id==id);
            if(r==null){Text(p,"기록을 고른 뒤 비교 칸에 놓으세요.",18,80,504,80,22,muted);return;}
            Text(p,Source(r)+"\n"+r.title,18,64,504,100,22);
            var scroll=Scroll(p,18,175,504,250,Mathf.Max(250,r.body.Length/24*29+100));Text(scroll,r.body,0,0,484,Mathf.Max(250,r.body.Length/24*29+100),22);
        }
        private void RenderTimeline()
        {
            // This view edits existing record references only; it never assigns inferred times.
            Render();var p=Modal("플레이어 사건 시간표");Button(p,"닫기",1410,22,130,46,CloseWindow);
            Text(p,"시각이 불명확하면 비워 두세요. 순서와 시간 표기는 직접 작성합니다.",30,86,1450,42,21,muted);
            var rows=(State?.timeline??new List<RoseTimelineRow>()).OrderBy(r=>r.order).ToArray();
            var list=Scroll(p,30,154,1510,650,rows.Length*112);
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];var record=State.records.Single(r=>r.id==row.recordId);float y=i*112;
                Text(list,record.title+"\n"+Source(record),12,y+8,700,90,23);
                Input(list,row.timeLabel,"직접 작성한 시각",736,y+12,350,62,s=>session.SetTimelineTime(row.recordId,s));
                Button(list,"↑",1105,y+12,70,62,()=>{session.MoveTimeline(row.recordId,-1);RenderTimeline();});
                Button(list,"↓",1188,y+12,70,62,()=>{session.MoveTimeline(row.recordId,1);RenderTimeline();});
                Button(list,"빼기",1272,y+12,160,62,()=>{session.RemoveTimeline(row.recordId);RenderTimeline();});
            }
            if(rows.Length==0)Text(list,"수첩에서 획득한 기록을 선택해 추가하세요.",12,16,1200,80,24,muted);
        }
        private void AccusationScreen()
        {
            var p=Modal("최종 고발",1280,880);
            Text(p,"고발할 인물 한 명을 선택하고 범인·수법·흉기를 한국어로 입력하세요.",30,90,1220,54,24);
            var people=Catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).ToArray();
            for(int i=0;i<people.Length;i++)
            {var r=people[i];Button(p,(accusationNpc==r.npcId?"● ":"")+r.name,30+(i%3)*412,164+(i/3)*72,396,60,()=>{accusationNpc=r.npcId;Render();},true,accusationNpc==r.npcId);}
            Text(p,"범인",30,330,150,40,24,gold);Input(p,who,"이름 또는 명확한 한국어 표현",200,320,1020,64,s=>who=s);
            Text(p,"범행수법",30,422,170,40,24,gold);Input(p,method,"상위 수법 분류",200,412,1020,64,s=>method=s);
            Text(p,"흉기",30,514,150,40,24,gold);Input(p,weapon,"흉기 이름",200,504,1020,64,s=>weapon=s);
            Text(p,notice+"\n"+session.Status,30,610,1190,116,21,muted);
            Button(p,"최종 제출",30,770,1190,68,()=>{if(session.PrepareAccusation(accusationNpc,who,method,weapon,out var error)){confirming=true;notice="";}else notice=error;Render();},State!=null,true);
        }
        private void ConfirmScreen()
        {
            var p=Modal("고발을 확정하시겠습니까?",850,600);
            Text(p,"확인하면 고발이 최종 제출됩니다.\n이 화면에서는 정답 여부나 힌트를 표시하지 않습니다.\n\n범인: "+who+"\n수법: "+method+"\n흉기: "+weapon,30,100,790,300,24);
            Button(p,"돌아가 수정",30,480,375,68,()=>{confirming=false;session.CancelAccusation();Render();});
            Button(p,"확정",435,480,385,68,()=>{confirming=false;session.ConfirmAccusation();Render();},true,true);
        }
        private void EndingScreen()
        {
            var p=RightPanel("엔딩 연결","지목 인물 대화 → 실제 범인 재연 → 보고서");
            Text(p,"승인된 엔딩 연출 자료 연결을 기다리고 있습니다.\n\n텍스트 화면을 재연 컷신이 재생된 것으로 처리하거나, 연결되지 않은 엔딩을 수집 완료로 기록하지 않습니다.",28,190,480,330,25);
            Text(p,session.Status,28,590,480,180,23,muted);
        }
        public static string EndingLabel(string outcome)=>outcome=="Complete"?"완전 해결":outcome=="Partial"?"부분 해결":outcome=="Misjudged"?"오판":"결과 연결 대기";
        private void ReportScreen()
        {
            var p=RightPanel("사건 종결 보고서",EndingLabel(State?.outcome));
            Text(p,State?.submitted==null?"승인된 사건 결과 연결 대기":"제출한 고발\n\n"+State.submitted.culpritText+"\n"+State.submitted.methodText+"\n"+State.submitted.weaponText,28,180,480,330,26);
            Text(p,"엔딩 기록은 실제 범인과 결과 종류로 18칸 도감에 보관됩니다. 새 회차의 수첩에는 복사하지 않습니다.",28,570,480,160,22,muted);
            Button(p,"메인 메뉴",28,806,480,72,()=>session.OpenMainMenu(),true,true);
        }
        private void ArchiveScreen()
        {
            var p=Modal("기록도감 · 장미대저택 18종",1400,820);Button(p,"닫기",1235,24,130,46,CloseWindow);
            RoseEndingArchive archive=null;
            if(Application.isPlaying)try{archive=session.LoadArchive();}catch(Exception){notice="기존 기록도감을 읽지 못했습니다. 파일은 보존됩니다.";}
            string[] outcomes={"Complete","Partial","Misjudged"};
            for(int c=0;c<3;c++)Text(p,EndingLabel(outcomes[c]),330+c*340,108,310,44,24,gold,true);
            var people=Catalog.rooms.Where(r=>!string.IsNullOrEmpty(r.npcId)).ToArray();
            for(int r=0;r<people.Length;r++)
            {
                Text(p,people[r].name,34,186+r*83,280,64,25);
                for(int c=0;c<3;c++)
                {bool unlocked=archive?.entries.Any(e=>e.culpritNpcId==people[r].npcId&&e.outcome==outcomes[c]&&e.unlocked)??false;Text(p,unlocked?"수집 완료":"미해금",330+c*340,186+r*83,310,64,23,unlocked?ink:muted);}
            }
            Text(p,notice,34,716,1300,64,20,muted);
        }
        private void Tools()
        {
            Button(generated,toolsOpen?"메뉴 닫기":"메뉴",1080,132,250,46,()=>{toolsOpen=!toolsOpen;Render();});
            if(!toolsOpen)return;
            Button(generated,"환경설정",1080,190,250,54,()=>{window="Settings";Render();});
            Button(generated,"수첩",1080,254,250,54,()=>{window="Notebook";Render();});
            Button(generated,"소지품",1080,318,250,54,()=>{window="Inventory";Render();});
        }
        private void SettingsScreen()
        {
            var p=Modal("환경설정",850,430);Button(p,"닫기",680,24,130,46,CloseWindow);
            Text(p,"환경설정의 세부 항목은 아직 확정되지 않았습니다.\n기존 카메라·조명·맵 설정을 자동 변경하지 않습니다.",30,115,790,150,25,muted);
            if(!session.MainMenu)Button(p,"메인 메뉴로",30,320,790,64,()=>{window="";session.OpenMainMenu();});
        }
        private void InventoryScreen()
        {
            var p=Modal("소지품",1100,760);Button(p,"닫기",930,24,130,46,CloseWindow);
            Text(p,"추가 행동 아이템은 객실 안에서 미확인 정보에만 사용합니다. 잠금 해제 기능은 없습니다.",30,96,1040,100,23,muted);
            var items=(State?.items??new List<RoseItem>()).Where(i=>!i.used).ToArray();var list=Scroll(p,30,218,1040,480,items.Length*86);
            for(int n=0;n<items.Length;n++)
            {var item=items[n];Button(list,(item.kind=="Search"?"추가 돋보기":"추가 질문권")+" · 사용",0,n*86,1015,72,()=>session.UseItem(item.instanceId),State!=null&&State.phase==RosePhase.Room);}
            if(items.Length==0)Text(list,"보유한 아이템이 없습니다.\n미확정 드롭 목록은 임의로 채우지 않습니다.",8,14,1000,120,25,muted);
        }
    }
}
