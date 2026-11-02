/* 검은 장미의 비밀 — independent mobile web edition. No Unity or Godot runtime required. */
(()=>{'use strict';
const KEY='blackrose.web.solo.v2';
const CHARACTERS=[
 {id:'MARIHI',name:'마리히',hint:'장미 장식을 좋아하는 금발의 손님',portrait:'CHR_MARIHI.webp',tone:'gold'},
 {id:'JIRO',name:'찌로',hint:'온실에서 꽃을 돌보던 손님',portrait:'CHR_JIRO.webp',tone:'green'},
 {id:'NAZ',name:'나즈',hint:'수첩에 무언가를 적고 있던 손님',portrait:'CHR_NAZ.webp',tone:'pink'},
 {id:'MATTO',name:'마또',hint:'조용히 저택을 둘러보던 손님',portrait:'CHR_MATTO.webp',tone:'white'},
 {id:'BOKI',name:'보키',hint:'복도에서 발걸음 소리를 들었다는 손님',portrait:'CHR_BOKI.webp',tone:'red'},
 {id:'MUNIRE',name:'문이레',hint:'밤늦게까지 음악을 듣던 손님',portrait:'CHR_MUNIRE.webp',tone:'violet'},
];
const ROOMS=[
 ['현관 홀','⌂','사람들이 처음 모인 거대한 홀'], ['응접실','♜','벨벳 소파가 놓인 조용한 공간'],
 ['서재','▤','오래된 책과 비밀이 가득한 방'],['식당','♧','긴 식탁과 촛대가 놓인 방'],
 ['주방','♨','후면 통로와 이어진 서비스 공간'],['침실','☾','커튼이 드리운 손님의 침실'],
 ['음악실','♫','피아노 선율이 머무는 방'],['온실','❀','달빛이 비치는 장미 온실'],
 ['지하 와인창고','♢','묵은 오크통이 줄지어 있는 지하']
];
// Exclusive mobile-demo sample items; NOT finalized PC weapons.
const ITEMS=['황동 촛대','장식용 편지칼','은제 열쇠','유리 화병','오래된 시계추','장미 조각상','묵직한 장부','금속 북엔드','철제 문고리'];
const QUOTES=['오늘 밤은 이상할 만큼 조용했어요.','저택에는 누구에게나 감추고 싶은 이야기가 있죠.','장미 향이 진해지는 순간을 기억해요.','어딘가에서 문이 닫히는 소리를 들었어요.','소문만으로는 누구도 의심하고 싶지 않아요.','진실을 알고 싶다면 기록을 살펴보세요.'];
let state=readState()||freshState(false);
let currentTab=state.started?(state.ended?'end':(state.phase==='review'&&state.lastInvestigation?'probeResult':'map')):'home';
let activePerson=0;
let message='';
let picks={person:-1,room:-1,item:-1};
let probePicks={person:-1,room:-1,item:-1};
let hostScenes=[];
let hostScene=null;
let hostVoiceToken=0;
let hostActiveUtterance=null;
let hostTimers=[];
let personDialog=-1;
let selectedAvatar=-1;
let worldChoice={npc:-1,weapon:0,kind:'item',item:0,mode:'walk'};
let selectedDestination=-1;
let suspectTarget=-1;
let suspectGuess={room:-1,item:-1};
const app=document.getElementById('app');
function freshState(started=true,playerCharacter=-1){const suspects=CHARACTERS.map((_,i)=>i).filter(i=>i!==playerCharacter);const caseState={started,ended:false,won:false,playerCharacter,answer:{person:suspects[Math.floor(Math.random()*suspects.length)],room:Math.floor(Math.random()*9),item:Math.floor(Math.random()*9)},spent:0,fail:0,room:0,round:1,phase:'investigate',lastAccuseRound:0,marked:{person:[],room:[],item:[]},history:[],lastInvestigation:null,seed:Date.now()};BRWorld.setup(caseState);return caseState;}
function save(){try{localStorage.setItem(KEY,JSON.stringify(state))}catch(e){console.warn('저장 불가',e)}}
function readState(){try{const raw=JSON.parse(localStorage.getItem(KEY)||'null');if(raw&&raw.answer&&raw.marked&&raw.history&&typeof raw.spent==='number'&&raw.answer.person>=0&&raw.answer.person<6){
 // Migrate existing v2 solo progress without losing clues or restarting the case.
 if(!Number.isInteger(raw.round)){raw.round=Math.max(1,Math.min(12,raw.spent||1));raw.phase=raw.spent>0?'review':'investigate'}
 if(raw.phase!=='review'&&raw.phase!=='investigate')raw.phase=raw.spent>=raw.round?'review':'investigate';
 if(!Number.isInteger(raw.lastAccuseRound))raw.lastAccuseRound=0;
 if(!Number.isInteger(raw.playerCharacter)||raw.playerCharacter<0||raw.playerCharacter>=6)raw.playerCharacter=-1;
 BRWorld.migrate(raw);
 return raw}}catch(e){}return null}
function esc(s){return String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]))}
const portrait=c=>`assets/characters/${c.portrait}`;
function img(c,klass='hero-photo'){return `<img class="${klass}" src="${portrait(c)}" alt="${esc(c.name)} 캐릭터 확정 원본 이미지" loading="lazy" onerror="this.style.display='none';this.insertAdjacentHTML('afterend','<div class=&quot;muted&quot;>이미지 준비 중</div>')">`}
function btn(txt,fn,cls='button',off=false){return `<button type="button" class="${cls}" data-act="${fn}" ${off?'disabled':''}>${txt}</button>`}
function header(){return `<header class="topbar"><div><div class="smallcaps">BLACK ROSE · MOBILE</div><div class="brand">검은 장미의 비밀</div></div><div class="chip">${state.started?`R${state.round}/12`:'2D 추리 게임'}</div></header>`}
function panel(title,body){return `<section class="panel"><div class="eyebrow">BLACK ROSE MANSION</div><h2 class="section-title">${title}</h2>${body}</section>`}
function intro(){return `<section class="panel hero"><div class="badge-float">싱글플레이</div><div class="eyebrow">어둠 속에 숨겨진 진실</div>${img(CHARACTERS[0])}<h1 class="hero-title">장미가 지는 밤</h1><p class="hero-desc">저택에서 벌어진 수상한 사건.<br>여섯 캐릭터 중 나의 역할을 고르고, 아홉 개의 방과 도구로 사건을 추리하세요.<br>단서를 수집해 숨겨진 조합을 밝혀내세요.</p>${btn('캐릭터 선택하고 시작','new','button primary full')}<p class="lead">${state.started&&!state.ended?'이전 사건이 저장되어 있어요.':''}</p>${state.started&&!state.ended?btn('이어서 플레이','resume','button secondary full'):''}</section>${panel('플레이 방법',`<p class="lead">① 캐릭터를 선택하면 랜덤 능력을 받아요.<br>② 2P로 방을 이동해 NPC를 만나 의심·협조할 수 있어요.<br>③ 가설 조사는 사건당 최대 3회. 나머지 정보는 의심·협조·특별 단서로 얻어요.<br>④ 범인의 방해와 이벤트를 넘어 R6부터 최종 추리에 도전해요! 같은 라운드에 2번 연속 지목할 수 없어요.</p><div class="evidence">모바일 독립 규칙 · 12라운드 · 가설 조사 최대 3회 · 라운드당 정보행동 1회 · 최종 지목 2회 · 휴대폰에 자동 저장</div>`)}<div class="footer">기존 PC Unity 게임과 분리된 독립 게임입니다.</div>`}

function chooseCharacter(){
 const cards=CHARACTERS.map((c,i)=>`<button type="button" class="avatar-card ${selectedAvatar===i?'selected':''}" data-act="choose:${i}" aria-pressed="${selectedAvatar===i?'true':'false'}" aria-label="${esc(c.name)} 선택">${img(c,'avatar-portrait')}<span class="avatar-name">${esc(c.name)}</span><span class="avatar-selected">${selectedAvatar===i?'✓ 선택됨':'선택하기'}</span></button>`).join('');
 const chosen=selectedAvatar>=0?CHARACTERS[selectedAvatar]:null;
 return `<section class="panel choose-panel"><div class="eyebrow">CHOOSE YOUR INVESTIGATOR · 나의 캐릭터</div><h1 class="choose-title">누구로 수사할까?</h1><p class="lead">플레이할 캐릭터를 골라주세요. 선택한 인물은 <b>내 조사자</b>가 되고, 나머지 5명 중 범인이 정해져요.</p><div class="avatar-grid">${cards}</div><div class="avatar-preview">${chosen?img(chosen,'preview-portrait'):'<div class="preview-placeholder">❀</div>'}<div class="avatar-info"><span>PLAYER CHARACTER</span><strong>${chosen?esc(chosen.name):'캐릭터 선택 대기'}</strong><p>${chosen?'이 인물로 검은 장미 저택의 사건을 조사합니다.':'위 초상화를 눌러 캐릭터를 선택하세요.'}</p></div></div>${btn(chosen?chosen.name+'(으)로 사건 시작':'캐릭터를 먼저 선택하세요','confirm-character','button primary full',!chosen)}<div style="height:9px"></div>${btn('뒤로 가기','choose-back','button secondary full')}</section>`;
}

function roundBar(){
 if(!state.started||state.ended)return '';
 const review=state.phase==='review';
 const mode=review?'정보행동 완료 · 라운드 정산':('조사 진행 중 · '+(state.round>=6?'최종 추리 가능':'R6부터 최종 추리 가능'));
 const dots=Array.from({length:12},(_,i)=>`<span class="round-dot ${i+1<state.round?'done':i+1===state.round?'current':''}" title="R${i+1}"></span>`).join('');
 const next=review?(state.round===12?'마지막 라운드 — 최종 추리로 사건을 마무리하세요.':'조사 결과 확인 후 ‘다음 라운드 시작’을 눌러주세요.'):'방을 고르고 범인·장소·도구 조합을 제출하면 이번 라운드의 조사가 끝나요.';
 return `<section class="round-banner" aria-label="라운드 진행 상태"><div class="round-head"><div><span class="round-kicker">현재 라운드</span><div class="round-big">R${state.round}<small> / 12</small></div></div><div class="round-state">${mode}</div></div><div class="round-track" role="img" aria-label="전체 12라운드 중 ${state.round}라운드">${dots}</div><p class="round-guidance">${next}</p></section>`;
}
function phaseAction(){
 if(state.ended||state.phase!=='review')return '';
 if(state.round>=12)return btn('R12 종료 · 마지막 최종 추리하기','open-accuse','button primary full');
 return btn(`R${state.round} 종료 → R${state.round+1} 시작`,'next-round','button primary full');
}
function nextRound(){
 if(state.ended||state.phase!=='review'||state.round>=12)return;
 state.round++;
 state.phase='investigate';
 state.lastInvestigation=null;
 probePicks={person:-1,room:-1,item:-1};
 worldChoice.mode='walk';selectedDestination=-1;
 const announcements=BRWorld.nextRound(state);
 save();show('map');
 message=`R${state.round} 시작! NPC가 이동하고 사건이 갱신되었습니다.`;render();
 const scene=hostRoundScene(state.round);
 if(announcements.length){
  scene.body+=' '+announcements.join(' ');
  scene.sub='새 라운드의 NPC 위치와 이벤트를 확인하세요. 이 안내는 추리 단서가 아닙니다.';
 }
 showHost(scene);
}

function hostRoundScene(n) {
 const lines=['장미 장식의 문이 닫힙니다. 첫 번째 기록을 시작하죠.','촛불이 흔들립니다. 여러분의 추측은 얼마나 단단할까요?','세 번째 종이 울렸습니다. 성급한 판단은 금물입니다.','복도 끝에서 발소리가 사라집니다. 기록을 다시 살펴보세요.','그림자가 길어집니다. 이제 다음 장막을 준비하죠.','여섯 번째 종이 울렸습니다. 이제 최종 추리가 열립니다.','장미꽃잎 하나가 떨어집니다. 판단은 신중하게.','서재의 시계가 느리게 움직입니다. 남은 시간을 살펴보세요.','아홉 번째 종이 울렸습니다. 조합을 비교할 때입니다.','촛불의 마지막 불꽃이 흔들립니다. 진실이 가까워집니다.','닫혀 있던 문의 틈으로 새벽빛이 스며듭니다.','마지막 종입니다. 이제 사건을 끝내야 합니다.'];
 return {kind:n===12?'danger':n>=6?'reveal':'round',icon:n===12?'⌛':'✦',eyebrow:'CHAPTER '+String(n).padStart(2,'0')+' / 12',title:n===12?'최후의 라운드':n===6?'최종 추리 개방':('라운드 '+n+' 시작'),body:lines[n-1],sub:n===12?'마지막 조사 후 최종 지목으로 마무리하세요.':n>=6?'최종 추리 가능 · 한 라운드에 지목은 한 번뿐입니다.':'범인·장소·도구를 고른 뒤 조사하세요. 이 대사는 추리 단서가 아닙니다.',action:'수사 계속하기'};
}

function clearHostTimers(){
 for(const id of hostTimers)window.clearTimeout(id);
 hostTimers=[];
}
function scheduleHost(fn,delay){
 const id=window.setTimeout(fn,delay);
 hostTimers.push(id);
 return id;
}
function stopHostPlayback(){
 hostVoiceToken++; // invalidate canceled speech callbacks and stale timers
 clearHostTimers();
 hostActiveUtterance=null;
 if(window.speechSynthesis){try{window.speechSynthesis.cancel()}catch(_){}}
}
const NARRATED_SCOPES=new Set(["deduction","investigation","suspicion","cooperation"]);
function shouldNarrate(sc){return !!sc&&sc.voicePhase==="result"&&NARRATED_SCOPES.has(sc.voiceScope)}
function showHost(scenes){
 stopHostPlayback();
 hostScenes=Array.isArray(scenes)?scenes.slice():[scenes];
 hostScene=hostScenes.shift()||null;
 render();
 if(hostScene)playHostAutomatically(); // run inside the original tap's call stack
}
function advanceHost(){
 if(!hostScene)return;
 stopHostPlayback();
 hostScene=hostScenes.shift()||null;
 render();
 if(hostScene)playHostAutomatically();
}
function shortResultVoice(sc){
 if(sc.voiceScope==="investigation"){
  if(sc.title==="발표 지연")return "조사 결과가 지연됐습니다.";
  const matched=String(sc.title||"").match(/[0-3]개/);
  return matched?`조사 결과, ${matched[0]} 일치.`:"조사 결과를 확인하세요.";
 }
 if(sc.voiceScope==="suspicion"){
  if(/획득한|카드.*확인|의심 성공/.test(sc.body||""))return "의심 결과, 카드 한 장 확인.";
  return "의심 결과, 일치 카드 없음.";
 }
 if(sc.voiceScope==="cooperation"){
  if(/거절/.test(sc.body||""))return "협조 요청을 거절했습니다.";
  return "협조 성공, 카드 한 장 확인.";
 }
 if(sc.voiceScope==="deduction"){
  if(sc.kind==="verdict")return "정답입니다. 사건 해결!";
  if(sc.kind==="danger")return "추리 실패. 사건 종료.";
  return "오답입니다. 다시 추리하세요.";
 }
 return "";
}
function playHostAutomatically(){
 if(!hostScene)return;
 const token=hostVoiceToken;
 if(!shouldNarrate(hostScene)){
  scheduleHost(()=>{if(token===hostVoiceToken)advanceHost()},3000);
  return;
 }
 let settled=false;
 function closeAfter(delay){
  if(settled||token!==hostVoiceToken)return;
  settled=true;clearHostTimers();
  scheduleHost(()=>{if(token===hostVoiceToken)advanceHost()},delay);
 }
 // Arm this timer independently of speech callbacks. Some Android TTS
 // engines never fire onstart/onend, or fire them in an unexpected order.
 const deadline=scheduleHost(()=>{
  if(settled||token!==hostVoiceToken)return;
  closeAfter(1000);
  try{window.speechSynthesis?.cancel()}catch(_){}
 },3000);
 if(!window.speechSynthesis||!window.SpeechSynthesisUtterance)return;
 try{
  const utterance=new window.SpeechSynthesisUtterance(shortResultVoice(hostScene));
  utterance.lang='ko-KR';
  utterance.rate=1.07;
  utterance.pitch=.92;
  utterance.volume=1;
  const voices=typeof window.speechSynthesis.getVoices==='function'?window.speechSynthesis.getVoices():[];
  const korean=voices.find(v=>/^ko(?:-|_)/i.test(v.lang||''));
  if(korean)utterance.voice=korean;
  hostActiveUtterance=utterance;
  utterance.onend=()=>closeAfter(1000);
  // An error means no speech; retain the original 3-second caption deadline.
  utterance.onerror=()=>{};
  window.speechSynthesis.speak(utterance);
 }catch(_){/* Caption deadline remains armed. */}
}

function hostDialog() {
 if(!hostScene)return '';
 const sc=hostScene;
 return `<div class="host-backdrop" role="dialog" aria-modal="true" aria-label="사회자 진행 연출"><div class="host-stage host-${esc(sc.kind||'round')}"><div class="host-stage-orbit" aria-hidden="true">✦</div><div class="host-orb" aria-hidden="true"><span class="host-emblem">${esc(sc.icon||'✦')}</span></div><div class="host-eyebrow">${esc(sc.eyebrow||'BLACK ROSE')}</div><div class="host-title">${esc(sc.title||'저택의 기록')}</div><div class="host-speaker"><span class="speaker-dot"></span>장미 저택의 사회자</div><p class="host-words">${esc(sc.body||'')}</p><p class="host-sub">${esc(sc.sub||'')}</p><div class="host-auto-progress" role="status"><span class="host-auto-pulse" aria-hidden="true"></span><span>${shouldNarrate(sc)?'짧은 결과 음성 · 최대 3초 · 종료 1초 후 자동 진행':'무음 연출 · 3초 후 자동 진행'}</span></div><div class="host-disclaimer">음성 재생이 차단된 기기에서는 자막을 보여준 뒤 자동으로 진행합니다.</div></div></div>`;
}

function hostStrip() {
 if(!state.started||state.ended)return '';
 const phase=state.phase==='review'?'정보행동 완료. 수첩에서 결과를 비교하고 다음 라운드로 진행하세요.':state.round>=6?'이제 최종 추리도 가능해요. 같은 라운드에서 두 번 지목할 수 없습니다.':'범인·장소·도구를 고르고 조사하세요. 일치 개수만 알려드립니다.';
 return `<div class="host-inline"><div class="host-mini-icon" aria-hidden="true">✿</div><div><b>장미 저택의 사회자</b><p>${phase}</p></div></div>`;
}

function currentHostScene() {
 return {kind:'round',icon:'✿',eyebrow:'사회자의 안내',title:'R'+state.round+' · 진행 안내',body:state.phase==='review'?'이번 라운드의 조사는 끝났습니다. 수첩을 확인하고 다음 라운드로 진행하세요.':'범인과 장소, 도구에 관한 가설을 세워보세요. 맞힌 항목 개수만 알려드립니다.',sub:'사회자의 연출 대사는 정답 단서를 추가로 공개하지 않습니다.',action:'확인'};
}

function personDialogView() {
 if(personDialog<0 || !CHARACTERS[personDialog])return '';
 const c=CHARACTERS[personDialog];
 return `<div class="person-backdrop" role="dialog" aria-modal="true" aria-label="${esc(c.name)} 대화 장면"><div class="person-stage"><div class="person-upper"><div class="person-chapter">PERSONNEL FILE ${String(personDialog+1).padStart(2,'0')} · BLACK ROSE</div><button type="button" class="person-close" data-act="person-close" aria-label="대화 닫기">✕</button></div><div class="person-figure">${img(c,'character-dialog-portrait')}</div><div class="person-name">${esc(c.name)}</div><div class="person-quote"><span class="quote-mark">“</span><p>${esc(QUOTES[personDialog])}</p><span class="quote-mark">”</span></div><p class="person-info">${esc(c.hint)} · 캐릭터의 일반 대화이며 범인·장소·도구의 정답을 알리는 단서는 아닙니다.</p><div class="person-dialog-actions">${btn('대화 마치기','person-close','button secondary full')}${btn('이 인물로 가설 세우기','person-probe','button primary full',state.phase!=='investigate'||state.ended||personDialog===state.playerCharacter)}</div></div></div>`;
}
function myCharacterTag(){if(!Number.isInteger(state.playerCharacter)||state.playerCharacter<0)return '';const c=CHARACTERS[state.playerCharacter];return `<div class="my-character"><img src="${portrait(c)}" alt="${esc(c.name)}" loading="lazy"><div><small>나의 캐릭터 · 수사자</small><b>${esc(c.name)}</b></div><span>MY ROLE</span></div>`;}
function summary(){const w=BRWorld.migrate(state);return myCharacterTag()+`<div class="case-stat"><div class="stat"><b>${3-w.investigated}</b><span>남은 가설 조사</span></div><div class="stat"><b>${2-state.fail}</b><span>지목 기회</span></div><div class="stat"><b>${state.marked.person.length+state.marked.room.length+state.marked.item.length}</b><span>수동 표시</span></div></div>`}
function clueTitle(kind,id){return kind==='room'?ROOMS[id][0]:ITEMS[id];}

function abilityDef(){const w=BRWorld.migrate(state);return BRWorld.ABILITIES.find(a=>a.id===w.ability.type);}

function NPCName(id){return CHARACTERS[id]?.name||'알 수 없는 인물';}

function worldSelect(name,values,selected){return '<select data-world-pick="'+name+'">'+values.map((v,i)=>'<option value="'+i+'" '+(selected===i?'selected':'')+'>'+esc(v)+'</option>').join('')+'</select>';}

function worldNotice(){
 const w=BRWorld.migrate(state);
 if(!w.event)return '<div class="world-safe">현재 라운드의 공개된 방해 사건 없음</div>';
 const e=w.event;
 if(e.kind==='smoke')return '<div class="world-alert">🌫 연막 발생 · '+e.rooms.map(id=>esc(ROOMS[id][0])).join(', ')+'에서 정보행동 불가 (이동 가능)</div>';
 if(e.kind==='lock')return '<div class="world-alert">🔒 통로 잠금 · '+e.edge.split('-').map(i=>esc(ROOMS[Number(i)][0])).join(' ↔ ')+' 추가 이동력 1P</div>';
 return '<div class="world-alert">⌛ 통신 교란 · 이번 라운드에 가설 조사 시 판정 발표가 다음 라운드로 지연</div>';
}

function worldAbility(){
 const w=BRWorld.migrate(state),ab=abilityDef(),c=w.ability;
 const base='<div class="world-ability-head"><span>✦ 랜덤 능력</span><b>'+esc(ab.name)+'</b><span>남은 '+c.left+'회</span></div><p class="hint">'+esc(ab.desc)+'</p>';
 let controls='';
 if(ab.id==='jump'||ab.id==='detour'){
  controls='<p class="hint">능력 이동 모드를 켜고 지도에서 목적지 방을 눌러주세요.</p>'+btn(worldChoice.mode===ab.id?'능력 이동 선택 중 · 끄기':'능력 이동 모드 켜기','ability-mode','button secondary full',c.left<=0);
 }else if(ab.id==='gate')controls='<p class="hint">이동 확정 시 기본 통행비 1P만 자동 면제됩니다. 잠금·고장난 문의 추가 통과 비용은 그대로 적용돼요.</p>';
 else if(ab.id==='trace'||ab.id==='interrogate'){
  const candidates=ab.id==='interrogate'?BRWorld.npcHere(state):w.npcs;
  controls='<label class="segtitle">능력 대상 인물</label><select data-world-pick="npc"><option value="-1">인물을 선택하세요</option>'+candidates.map(n=>'<option value="'+n.id+'" '+(worldChoice.npc===n.id?'selected':'')+'>'+esc(NPCName(n.id))+'</option>').join('')+'</select>';
  if(ab.id==='interrogate')controls+='<label class="segtitle">심문할 도구</label>'+worldSelect('weapon',ITEMS,worldChoice.weapon);
  controls+=btn('「'+ab.name+'」 능력 사용','ability-use','button secondary full',c.left<=0||state.phase!=='investigate');
 }else if(ab.id==='analysis'){
  controls='<label class="segtitle">분석 카드 종류</label><select data-world-pick="kind"><option value="room" '+(worldChoice.kind==='room'?'selected':'')+'>장소 카드</option><option value="item" '+(worldChoice.kind==='item'?'selected':'')+'>도구 카드</option></select>';
  controls+='<label class="segtitle">분석할 카드</label>'+worldSelect('item',worldChoice.kind==='room'?ROOMS.map(x=>x[0]):ITEMS,worldChoice.item);
  controls+=btn('배분 여부 분석하기','ability-use','button secondary full',c.left<=0||state.phase!=='investigate');
 }else controls=btn('「'+ab.name+'」 능력 사용','ability-use','button secondary full',c.left<=0||ab.id==='return'&&state.room===w.prevRoom);
 return panel('나의 특별 능력',base+controls);
}

function roomPlayers(room){
 const w=BRWorld.migrate(state);
 return w.npcs.filter(n=>n.room===room).map(n=>'<img src="'+portrait(CHARACTERS[n.id])+'" alt="'+esc(NPCName(n.id))+'" title="'+esc(NPCName(n.id))+'" class="npc-mini">').join('');
}

function roomTokens(i){
 const w=BRWorld.migrate(state);
 const box=w.box.appears!==null&&state.round>=w.box.appears&&!w.box.opened&&w.box.room===i;
 return '<div class="world-room-tokens">'+(box?'📦':'')+'</div>';
}
function officialEventPanel(){
 const w=BRWorld.migrate(state),sp=w.special;
 const roomNames=ROOMS.map(r=>r[0]);
 const here=state.room===0;
 let active='';
 if(sp.activated){
  if(sp.type==='blackout'&&state.round>=sp.round&&state.round<=sp.round+1)active='💡 정전 · '+roomNames[sp.room]+' (R'+sp.round+'~R'+(sp.round+1)+') 이동 외 행동 금지';
  if(sp.type==='broken_door'&&state.round===sp.round)active='🚪 고장난 문 · '+sp.edge.split('-').map(x=>roomNames[Number(x)]).join(' ↔ ')+' 통과비 +1P';
  if(sp.type==='secret_passage'&&state.round===sp.round)active='🕳️ 비밀통로 · '+roomNames[sp.from]+' ↔ '+roomNames[sp.to]+' 1P 임시 이동';
 }
 const keyText=w.key.owner==='player'?'내가 열쇠 보유 중':w.key.taken?'누군가 발견함 (소유 비공개)':state.round>=w.key.appears?'열쇠 수색 중':'R'+w.key.appears+'에 등장';
 const boxText=w.box.opened?'개봉됨 (열람자는 비공개)':w.box.appears!==null&&state.round>=w.box.appears?roomNames[w.box.room]+'에 등장':w.box.appears!==null?'R'+w.box.appears+'에 등장':'열쇠를 찾은 뒤 후속 라운드에 등장';
 const science=w.forensic;
 const scienceText=science.read?'결과 확인 완료 · 수첩에 개인 기록됨':state.round>=7?'도착 · 현관 홀(로비)에서 확인 가능':'R7 시작에 결과 도착';
 const scienceButton='';
 const extraText=sp.activated?(active||'추가 이벤트 종료 · '+{blackout:'정전',broken_door:'고장난 문',secret_passage:'비밀통로'}[sp.type]):'R6~R11에 정전 / 고장난 문 / 비밀통로 중 단 한 번 발생';
 return panel('확정 특별이벤트 · EVENTS.md',`<div class="world-events"><div>🗝️ 열쇠 → 📦 비밀상자<br>${esc(keyText)} / ${esc(boxText)}</div><div>🔬 과학수사<br>${esc(scienceText)}</div><div>✦ 추가 이벤트 (1종 1회)<br>${esc(extraText)}</div></div>${scienceButton}<p class="hint">열쇠와 과학수사 카드 내용은 각자 획득·확인해야 합니다. 정식 이벤트에 없는 단서 생성은 사용하지 않습니다.</p>`);
}
function worldMap(){
 const w=BRWorld.migrate(state);
 const roomInfo=i=>BRWorld.routeInfo(state,i,worldChoice.mode);
 const pending=selectedDestination>=0?roomInfo(selectedDestination):null;
 const planned=pending?.ok?pending:null;
 const current=state.room;
 const coords=Array.from({length:9},(_,i)=>({x:50+(i%3)*100,y:50+Math.floor(i/3)*100}));
 const pathEdges=new Set();
 if(planned&&planned.special!=="jump"){
  for(let j=1;j<planned.path.length;j++)pathEdges.add([planned.path[j-1],planned.path[j]].sort((a,b)=>a-b).join("-"));
 }
 const linkSvg=BRWorld.EDGES.flatMap((connections,i)=>connections.filter(j=>i<j).map(j=>{
  const key=i+"-"+j,from=coords[i],to=coords[j];
  const isBlocked=(w.event?.kind==="lock"&&w.event.edge===key)||(w.special?.activated&&w.special.type==="broken_door"&&state.round===w.special.round&&w.special.edge===key);
  const isSelected=pathEdges.has(key);
  const adjacent=i===current||j===current;
  return `<line x1="${from.x}" y1="${from.y}" x2="${to.x}" y2="${to.y}" class="graph-link ${isBlocked?'graph-locked':isSelected?'graph-selected':adjacent?'graph-adjacent':''}" />`;
 })).join('');
 const sp=w.special;
 const passageActive=sp?.activated&&sp.type==="secret_passage"&&state.round===sp.round;
 const passageSvg=passageActive?`<line x1="${coords[sp.from].x}" y1="${coords[sp.from].y}" x2="${coords[sp.to].x}" y2="${coords[sp.to].y}" class="graph-link graph-secret ${pathEdges.has([sp.from,sp.to].sort((a,b)=>a-b).join('-'))?'graph-selected':''}"/>`:'';
 const jumpLine=planned?.special==="jump"?`<line x1="${coords[current].x}" y1="${coords[current].y}" x2="${coords[selectedDestination].x}" y2="${coords[selectedDestination].y}" class="graph-link graph-jump"/>`:'';
 const grid=ROOMS.map(([name,emoji],i)=>{
  const isCurrent=i===current,route=isCurrent?null:roomInfo(i);
  const canGo=route?.ok===true;
  const choice=selectedDestination===i&&canGo;
  const css=isCurrent?'active':choice?'route-selected':canGo?'reachable':'not-reachable';
  const cost=isCurrent?'● 현재':canGo?route.cost+'P 이동':'이동 불가';
  const accessible=isCurrent?'내 위치':canGo?'경로 연결됨 · '+route.cost+'P':'이동력 부족 / 연결 없음';
  return `<button type="button" class="room world-room graph-node ${css}" data-act="room:${i}" aria-label="${esc(name)} · ${accessible}" aria-pressed="${choice?'true':'false'}" ${!canGo?'disabled':''}>
   <span class="symbol">${emoji}</span><span class="room-name">${esc(name)}</span>
   <span class="graph-room-cost">${cost}</span><div class="npc-row">${roomPlayers(i)}</div>${roomTokens(i)}
  </button>`;
 }).join('');
 const routePreview=planned?`<div class="route-preview" role="status"><div class="route-preview-title"><span>선택한 이동 경로</span><strong>${planned.cost}P 사용</strong></div><p>${planned.path.map(i=>esc(ROOMS[i][0])).join(' → ')}</p><div class="route-preview-actions">${btn('이 경로로 이동하기','confirm-route','button primary full')}${btn('취소','cancel-route','button secondary full')}</div>${planned.special==='jump'?'<p class="hint">예외: 도약 능력 사용 시에만 복도를 거치지 않고 이동할 수 있습니다.</p>':''}</div>`:'<p class="map-instruction">밝은 방을 터치하면 <b>실제 경로와 필요한 이동력</b>을 먼저 볼 수 있어요. 확인하기 전까지 캐릭터가 이동하지 않습니다.</p>';
 const possible=ROOMS.map(([name],i)=>i!==current&&roomInfo(i).ok?esc(name):null).filter(Boolean);
 const routeHint='<p class="world-route">현재 위치에서 연결 경로로 이동 가능한 방: '+(possible.length?possible.join(' · '):'없음 · 다음 라운드 또는 능력 사용')+'</p>';
 const people=BRWorld.npcHere(state);
 const infoBlocked=BRWorld.canInfo(state);
 const encounter=people.length?'<div class="encounter-grid">'+people.map(n=>'<div class="encounter-card"><img src="'+portrait(CHARACTERS[n.id])+'" alt="'+esc(NPCName(n.id))+'"><div class="encounter-meta"><strong>'+esc(NPCName(n.id))+'</strong><span>'+esc((BRWorld.ABILITIES.find(a=>a.id===n.ability?.type)||{}).name||'기본 능력')+' 능력 · '+esc((BRWorld.TRAITS.find(t=>t.id===n.trait)||{}).name||'일반 이동')+'</span></div>'+btn('의심','suspect:'+n.id,'button secondary',!!infoBlocked||w.suspicions>=3)+btn('협조','cooperate:'+n.id,'button secondary',!!infoBlocked||w.cooperations>=1)+'</div>').join('')+'</div><p class="hint">의심은 장소와 흉기를 각각 선택해 제출해요.</p>'+(infoBlocked?'<p class="world-alert">'+esc(infoBlocked)+' · 이 방에서 의심·협조를 사용할 수 없습니다.</p>':w.suspicions>=3&&w.cooperations>=1?'<p class="hint">이번 사건의 의심·협조 횟수를 모두 사용했어요.</p>':''):'<p class="hint">같은 방에 NPC가 없어요. 지도에서 초상화가 표시된 방으로 연결 경로를 따라 이동하세요.</p>';
 const specialButton=state.round>=7&&state.room===0&&!w.forensic.read?btn('🔬 과학수사 결과 확인 · 정보행동 1회','check-forensic','button secondary full',state.phase!=='investigate'):'<p class="hint">NPC를 만나 의심·협조하거나 확정 이벤트 장소로 이동해 정보를 얻으세요.</p>';
 const primary=state.phase==='review'?'<p class="hint">이번 라운드 정보행동이 끝났어요. 지도 위의 다음 라운드 버튼으로 진행할 수 있어요.</p>':(btn('가설 조사 · 3항목 직접 선택','open-probe','button primary full',w.investigated>=3)+(w.investigated>=3?'<p class="hint">가설 조사 3회 소진. NPC 의심·협조, 특별 능력, R7 과학수사로 정보를 더 모으세요.</p>':'')+'<div style="height:9px"></div>'+btn('이번 라운드 행동 없이 종료','pass-turn','button ghost full'));
 const mode=worldChoice.mode!=='walk'?'<div class="notice">✦ '+(worldChoice.mode==='jump'?'도약':'우회')+' 능력 선택됨 · 목적지를 고르고 이동을 확정하세요.</div>':'';
 return summary()+(state.phase==='review'?'<div class="round-next-quick">'+phaseAction()+'</div>':'')+worldNotice()+panel(`저택 이동 경로 · 남은 ${w.moveLeft}P`,
 `<p class="hint">금색 연결선: 현재 방과 연결 · 밝은 방: 이동 가능 · 붉은 선: 잠금/고장난 문 · 청록 점선: 비밀통로 · 방 하나 통과 시 기본 1P.</p>${mode}<div class="mansion-graph"><svg class="map-connections" viewBox="0 0 300 300" preserveAspectRatio="none" aria-hidden="true">${linkSvg}${passageSvg}${jumpLine}</svg><div class="graph-grid">${grid}</div></div>${routePreview}${routeHint}`)+
 panel(ROOMS[state.room][0]+' · 방 행동',`<p class="lead">같은 방 NPC ${people.length}명 · 의심 ${w.suspicions}/3 · 협조 ${w.cooperations}/1</p>${encounter}<div class="sep"></div>${specialButton}<div class="sep"></div>${primary}`)+
 worldAbility()+officialEventPanel();
}


function suspicionSelect(kind, values){
 const label=kind==='room'?'의심할 장소':'의심할 흉기';
 return `<label class="segtitle" for="suspicion-${kind}">${label}</label><select id="suspicion-${kind}" data-suspicion-pick="${kind}"><option value="-1" ${suspectGuess[kind]===-1?'selected':''}>선택하세요</option>${values.map((v,i)=>`<option value="${i}" ${suspectGuess[kind]===i?'selected':''}>${esc(v)}</option>`).join('')}</select>`;
}
function suspicionScreen(){
 const w=BRWorld.migrate(state),npc=w.npcs.find(n=>n.id===suspectTarget);
 const blocked=BRWorld.canInfo(state);
 if(!npc||npc.room!==state.room)return `${summary()}${panel('의심할 인물을 찾을 수 없어요','<p class="lead">이 인물이 같은 방에 없어요. 저택 지도로 돌아가세요.</p>')}${btn('지도 돌아가기','cancel-suspicion','button secondary full')}`;
 const c=CHARACTERS[npc.id];
 return `${summary()}${panel('인물 의심 · 장소 + 흉기',`<div class="suspicion-person">${img(c,'suspicion-portrait')}<div><span class="eyebrow">SUSPECT</span><strong>${esc(c.name)}</strong><p>현재 ${esc(ROOMS[state.room][0])}에서 만났습니다.</p></div></div><p class="lead">이 인물이 보유한 장소 카드와 흉기 카드를 추측하세요. <b>의심 장소는 지금 서 있는 방이 아니어도 됩니다.</b></p>${suspicionSelect('room',ROOMS.map(r=>r[0]))}${suspicionSelect('item',ITEMS)}<p class="hint">두 카드 중 한 장 이상 맞으면, 상대가 보유한 카드 한 장만 공개합니다. 둘 다 틀리면 해당 카드가 없다고 알려줍니다. 성공·실패 모두 의심 1회와 이번 라운드 정보행동 1회를 사용합니다.</p>${blocked?'<p class="world-alert">'+esc(blocked)+'</p>':''}${btn('이 장소와 흉기로 의심하기','submit-suspicion','button primary full',!!blocked||w.suspicions>=3)}`)}${btn('취소 · 방으로 돌아가기','cancel-suspicion','button secondary full')}`;
}
function submitSuspicion(){
 if(suspectGuess.room<0||suspectGuess.item<0){
  message='의심할 장소와 흉기를 모두 선택해주세요.';render();return;
 }
 const npcId=suspectTarget, chosen={...suspectGuess};
 if(BRWorld.canInfo(state)){message=BRWorld.canInfo(state);render();return;}
 const result=BRWorld.interaction(state,npcId,'suspect',chosen.item,chosen.room);
 if(result.ok){
  const card=result.found?clueTitle(result.found.kind,result.found.id):'두 카드 모두 없음';
  state.history[0]=`R${state.round} · ${NPCName(npcId)} 의심 [${ROOMS[chosen.room][0]} / ${ITEMS[chosen.item]}] → ${card}`;
  suspectTarget=-1;suspectGuess={room:-1,item:-1};
 }
 worldResult(result,'suspicion');
}

function worldResult(result,voiceScope=null){
 if(!result.ok){message=result.message;render();if(result.message.includes('협조를 거절'))showHost({voiceScope:'cooperation',voicePhase:'result',kind:'warning',icon:'⚖',eyebrow:`R${state.round} · 협조 요청`,title:'협조 거절',body:result.message,sub:'협조 거절 시 정보행동과 협조 횟수는 소모되지 않습니다.'});return;}
 let desc=result.message;
 if(result.found)desc+=' 획득한 '+(result.found.kind==='room'?'장소':'도구')+' 카드: '+clueTitle(result.found.kind,result.found.id);
 if(result.cards)desc+=' 미배분 장소: '+clueTitle('room',result.cards.room)+' / 미배분 흉기: '+clueTitle('item',result.cards.item);
 save();currentTab='map';message=desc;
 showHost({voiceScope,voicePhase:voiceScope?'result':null,kind:'evidence',icon:'✦',eyebrow:`R${state.round} · 행동 판정`,title:state.phase==='review'?'정보 판정':'행동 완료',body:desc,sub:state.phase==='review'?'획득한 정보는 추리 수첩에 기록됩니다.':'이동과 정보행동을 계속 진행할 수 있습니다.'});
}

function map(){return worldMap();}
function people(){return `${panel('인물 도감',`<p class="lead">확정된 캐릭터 6명. 내 캐릭터는 범인 후보에서 제외돼요. 터치하면 프로필과 대화를 확인할 수 있어요. 가벼운 대화는 조사 기회를 소모하지 않습니다.</p><div class="grid2">${CHARACTERS.map((c,i)=>`<button class="card-person ${state.marked.person.includes(i)?'eliminated':''}" data-act="person:${i}">${img(c,'')}<strong>${c.name}</strong><small>${i===state.playerCharacter?'★ 내 캐릭터':state.marked.person.includes(i)?'⊖ 내가 표시한 후보':'프로필 보기'}</small></button>`).join('')}</div>`)}${panel(CHARACTERS[activePerson].name,`${img(CHARACTERS[activePerson],'scene-person')}<p class="lead">${CHARACTERS[activePerson].hint}</p><div class="evidence">“${QUOTES[activePerson]}”</div><p class="hint">이 대화는 분위기용이며 정답을 확정해 주는 단서는 아닙니다.</p>`)}`}
function noteChips(kind,names){return `<div class="chips">${names.map((name,i)=>kind==='person'&&i===state.playerCharacter?`<span class="choice my-choice">★ ${esc(name)} · 내 캐릭터</span>`:`<button type="button" class="choice ${state.marked[kind].includes(i)?'struck':''}" data-act="mark:${kind}:${i}">${state.marked[kind].includes(i)?'⊖ ': '◇ '}${esc(name)}</button>`).join('')}</div>`}
function notebook(){const w=BRWorld.migrate(state);return `${summary()}${panel('내 손패 · 정답이 아닌 카드',`<div class="evidence">장소: ${clueTitle('room',w.ownHand.room)}<br>도구: ${clueTitle('item',w.ownHand.item)}</div>`)}${panel('추리 수첩',`<p class="lead">후보를 터치하면 내가 직접 제외 표시할 수 있어요. 표시는 메모일 뿐 자동 판정이 아니며, 실제 정답은 감춰져 있어요.</p><div class="segtitle">범인 후보 · 수동 표시 ${state.marked.person.length}명</div>${noteChips('person',CHARACTERS.map(c=>c.name))}<div class="segtitle">장소 후보 · 수동 표시 ${state.marked.room.length}곳</div>${noteChips('room',ROOMS.map(r=>r[0]))}<div class="segtitle">도구 후보 · 수동 표시 ${state.marked.item.length}종</div>${noteChips('item',ITEMS)}`)}${panel('저택 사건 기록',w.eventLog.length?`<ul class="notes">${w.eventLog.slice(0,8).map(v=>`<li>${esc(v)}</li>`).join('')}</ul>`:'<p class="hint">아직 기록된 방해 사건이 없습니다.</p>')}${panel('확보한 다른 카드',w.clueRead.length?`<ul class="notes">${w.clueRead.map(k=>`<li>${k.kind&&k.id!=null?clueTitle(k.kind,k.id):k.room!=null?'장소 '+clueTitle('room',k.room)+' / 도구 '+clueTitle('item',k.item):'기록 확인'} (${k.via})</li>`).join('')}</ul>`:'<p class="hint">NPC를 만나거나 특별 증거를 조사하면 카드가 쌓여요.</p>')}${panel('발견한 증거',state.history.length?`<ol class="notes">${state.history.map(v=>`<li>${esc(v)}</li>`).join('')}</ol>`:`<p class="lead">아직 조사 기록이 없습니다. NPC와 만나거나 로비에서 과학수사를 확인해보세요.</p>`)}`}
function selectField(label,kind,values,choices=picks){return `<label class="segtitle" for="pick-${kind}">${label}</label><select id="pick-${kind}" data-pick="${kind}"><option value="-1">선택해주세요</option>${values.map((v,i)=>`<option value="${i}" ${kind==='person'&&i===state.playerCharacter?'disabled':''} ${choices[kind]===i?'selected':''}>${kind==='person'&&i===state.playerCharacter?'★ 내 캐릭터 · 선택 불가 · ':state.marked[kind].includes(i)?'✕ ':''}${esc(v)}</option>`).join('')}</select>`}

function investigationForm(){if(BRWorld.migrate(state).investigated>=3)return `${summary()}${panel('가설 조사 3회 사용 완료','<p class="lead">NPC를 만나 의심·협조하거나 방의 특별 증거를 조사해서 정보를 더 모으세요.</p>')}${btn('저택 지도 보기','back-map','button primary full')}`;if(state.phase==='review')return `${summary()}${panel(`R${state.round} 조사 완료`,'<p class="lead">이번 라운드 조사는 이미 끝났어요.</p>')}${phaseAction()}`;return `${summary()}${panel(`R${state.round} · 가설 직접 조사`,`<p class="lead">범인 · 장소 · 도구 세 항목을 직접 고르고 제출해요. 총 몇 개가 맞았는지만 알려주며, 어떤 항목인지는 알려주지 않아요.</p>${selectField('범인은 누구일까?','person',CHARACTERS.map(c=>c.name),probePicks)}${selectField('어디에서 벌어졌을까?','room',ROOMS.map(r=>r[0]),probePicks)}${selectField('무엇을 사용했을까?','item',ITEMS,probePicks)}${btn('이 조합으로 조사하기','submit-probe','button primary full',state.spent>=12||state.phase!=='investigate')}<p class="hint">현재 R${state.round} · 남은 가설 조사 ${3-BRWorld.migrate(state).investigated}회 · 제출한 조합과 결과는 내 수첩에 기록됩니다.</p>`)}${btn('지도로 돌아가기','back-map','button secondary full')}`;}
function investigationResult(){
 const last=state.lastInvestigation;
 if(!last)return `${panel('조사 결과','<p>기록이 없습니다.</p>')}${btn('지도 보기','back-map','button primary full')}`;
 const combination=[CHARACTERS[last.chosen.person].name,ROOMS[last.chosen.room][0],ITEMS[last.chosen.item]].map(esc).join(' / ');
 return `${summary()}${panel(`R${state.round} · 조사 종료`, `<div class="result" style="font-size:29px;font-weight:800">${last.pending?'발표 지연':last.match+'개 일치!'}</div><p class="lead">${last.pending?'통신 교란으로 결과가 다음 라운드에 공개됩니다.':'세 항목 중 '+last.match+'개가 정답과 같습니다. 어느 항목인지는 공개되지 않습니다.'}</p><div class="evidence">내가 제출한 조합<br><b>${combination}</b></div><p class="hint">자동으로 후보를 지우지 않습니다. 결과를 비교해 수첩에 직접 표시하세요.</p>`)}${btn('추리 수첩 확인','open-notes','button primary full')}<div style="height:9px"></div>${phaseAction()}`;
}
function markCandidate(kind,i){if(!['person','room','item'].includes(kind)||kind==='person'&&i===state.playerCharacter)return;const max=kind==='person'?6:9;if(!Number.isInteger(i)||i<0||i>=max)return;const marks=state.marked[kind];const index=marks.indexOf(i);if(index<0)marks.push(i);else marks.splice(index,1);save();render();}
function submitProbe(){
 if(state.ended||state.spent>=12||state.phase!=='investigate')return;
 if(BRWorld.migrate(state).investigated>=3){message='이번 사건의 가설 조사는 최대 3회입니다. NPC·방별 단서·능력을 활용하세요.';show('map');return;}
 const forbidden=BRWorld.canInfo(state);if(forbidden){message=forbidden;render();return;}
 if(probePicks.person===state.playerCharacter||['person','room','item'].some(k=>!Number.isInteger(probePicks[k])||probePicks[k]<0||probePicks[k]>=(k==='person'?6:9))){
  message='범인, 장소, 도구를 모두 선택한 뒤 조사해주세요.';render();return;
 }
 const chosen={...probePicks};
 const match=['person','room','item'].filter(k=>chosen[k]===state.answer[k]).length;
 const info=BRWorld.consumeInfo(state,'범인·장소·도구 가설 제출');
 if(!info.ok){message=info.message;render();return}
 state.world.investigated++;
 const delayed=state.world.event?.kind==='delay';
 state.lastInvestigation={chosen,match:delayed?null:match,location:state.room,number:state.spent,pending:delayed};
 if(delayed){state.world.awaiting={round:state.round,match,chosen:[CHARACTERS[chosen.person].name,ROOMS[chosen.room][0],ITEMS[chosen.item]]};state.history.unshift(`R${state.round} 조사 접수 · 판정 결과는 다음 라운드 발표`);}
 else state.history.unshift(`R${state.round} 조사 · ${ROOMS[state.room][0]} · [${CHARACTERS[chosen.person].name} / ${ROOMS[chosen.room][0]} / ${ITEMS[chosen.item]}] → ${match}개 일치`);
 save();probePicks={person:-1,room:-1,item:-1};show('probeResult');
 showHost({voiceScope:'investigation',voicePhase:'result',kind:delayed?'warning':match===3?'reveal':'evidence',icon:delayed?'⌛':'◇',eyebrow:`R${state.round} · 조사 판정`,title:delayed?'발표 지연':match+'개 일치',body:delayed?'정체불명의 통신 교란으로 조사 판정 발표가 다음 라운드로 늦춰졌습니다. 입력한 조합은 보존됩니다.':`제출한 세 항목 중 정답과 일치하는 것은 ${match}개입니다. 어느 항목인지는 알려드릴 수 없습니다.`,sub:'발표가 지연되어도 정답 값은 바뀌지 않습니다.',action:'조사 결과 확인'});
}

function accuse(){const locked=state.round<6;const attempted=state.lastAccuseRound===state.round;return `${summary()}${panel(`R${state.round} · 최종 추리`, `<p class="lead">R6부터 최종 지목할 수 있어요. 전체 최대 2번, 한 라운드에 1번만 가능하며 실패해도 다음 라운드에는 다시 도전할 수 있어요.</p>${selectField('누가?','person',CHARACTERS.map(c=>c.name))}${selectField('어디서?','room',ROOMS.map(r=>r[0]))}${selectField('무엇으로?','item',ITEMS)}${btn('세 항목으로 최종 지목','submit','button primary full',locked||attempted||state.fail>=2)}${locked?'<p class="hint">R1~R5에는 공식 최종 추리가 잠겨 있어요. R6부터 열려요.</p>':''}${attempted?'<p class="hint">이번 라운드에는 이미 추리했어요. 다음 라운드부터 다시 도전 가능해요.</p>':''}`)}<div class="notice">어떤 항목이 맞았는지는 알려주지 않아요. R12에는 반드시 최종 추리로 마무리해요.</div>${state.phase==='review'&&state.round<12?phaseAction():''}`}
function ending(){const p=CHARACTERS[state.answer.person],rn=ROOMS[state.answer.room][0],it=ITEMS[state.answer.item];return `<section class="panel hero"><div class="eyebrow">CASE CLOSED</div><h1 class="hero-title">${state.won?'진실을 밝혀냈다':'사건은 미궁 속으로'}</h1>${img(p)}<p class="hero-desc">${state.won?'모든 조각이 하나의 진실을 가리킨다.':'조사 기록은 남았지만, 마지막 기회를 잃었다.'}</p><div class="evidence" style="text-align:left"><b>범인</b> ${p.name}<br><b>장소</b> ${rn}<br><b>도구</b> ${it}</div><div class="sep"></div>${btn('새로운 사건 시작','new','button primary full')}<div style="height:9px"></div>${btn('조사 기록 보기','log','button secondary full')}</section>`}
function navbar(){return `<nav class="bottomnav" aria-label="게임 메뉴">${[['map','⌂','저택'],['people','♙','인물'],['notes','▤','수첩'],['accuse','◆','추리']].map(([id,ico,txt])=>`<button class="navitem ${currentTab===id?'active':''}" data-act="tab:${id}"><span class="navico">${ico}</span>${txt}</button>`).join('')}</nav>`}
function render(){if(!app)return;const title=header()+roundBar()+hostStrip();let body='';if(currentTab==='home')body=intro();else if(currentTab==='choose')body=chooseCharacter();else if(currentTab==='map')body=map();else if(currentTab==='suspect')body=suspicionScreen();else if(currentTab==='people')body=people();else if(currentTab==='notes')body=notebook();else if(currentTab==='accuse')body=accuse();else if(currentTab==='probe')body=investigationForm();else if(currentTab==='probeResult')body=investigationResult();else if(currentTab==='end')body=ending();app.innerHTML=title+(message?`<div class="notice" role="status">${esc(message)}</div>`:'')+body+(state.started&&!state.ended?navbar():'')+hostDialog()+personDialogView();}
function show(tab){currentTab=tab;message='';render();window.scrollTo({top:0,behavior:'instant'})}
function announceOpening(){
 const w=BRWorld.migrate(state);const ability=BRWorld.ABILITIES.find(a=>a.id===w.ability.type);
 showHost([{kind:'opening',icon:'❀',eyebrow:'PROLOGUE · 저택의 밤',title:'검은 장미의 비밀',body:`한밤중, 검은 장미 저택에서 수상한 사건이 일어났습니다. ${state.playerCharacter>=0?CHARACTERS[state.playerCharacter].name+'(이)가 수사자로 나섭니다.':'당신이 수사자로 나섭니다.'} 남은 다섯 인물 사이에 진실이 숨어 있죠.`,sub:'이 이야기는 모바일 독립판의 사건 도입입니다. 특정 인물을 범인으로 지목하지 않습니다.',action:'저택 안으로 들어가기'},{kind:'reveal',icon:'✦',eyebrow:'RANDOM ABILITY',title:'랜덤 능력 부여',body:`${CHARACTERS[state.playerCharacter]?.name||'수사자'}의 능력은 「${ability.name}」입니다. ${ability.desc}`,sub:'다른 NPC들도 개별 랜덤 능력을 부여받았으며, 범인은 남몰래 방해 능력을 발동합니다.',action:'능력 확인'},hostRoundScene(1)]);
}
function start(){selectedDestination=-1;suspectTarget=-1;suspectGuess={room:-1,item:-1};worldChoice={npc:-1,weapon:0,kind:'item',item:0,mode:'walk'};if(!Number.isInteger(selectedAvatar)||selectedAvatar<0||selectedAvatar>=CHARACTERS.length){message='먼저 캐릭터를 선택해주세요.';render();return}personDialog=-1;state=freshState(true,selectedAvatar);picks={person:-1,room:-1,item:-1};probePicks={person:-1,room:-1,item:-1};save();show('map');message=`${CHARACTERS[selectedAvatar].name}(으)로 사건을 시작합니다.`;render();announceOpening()}
function submit(){if(state.round<6||state.lastAccuseRound===state.round||state.fail>=2||state.ended)return;if(picks.person===state.playerCharacter||Object.values(picks).some(n=>n<0)){message='범인·장소·도구를 모두 골라주세요.';render();return}const match=['person','room','item'].filter(k=>picks[k]===state.answer[k]).length;state.lastAccuseRound=state.round;
if(match===3){state.won=true;state.ended=true;state.history.unshift(`R${state.round} 최종 지목: 3개 일치, 사건 해결`);save();show('end');showHost({voiceScope:'deduction',voicePhase:'result',kind:'verdict',icon:'❀',eyebrow:`R${state.round} · 최종 판정`,title:'진실이 밝혀졌다',body:'세 항목이 모두 일치합니다. 장미의 밤을 뒤덮은 수수께끼가 풀렸습니다.',sub:'사건 해결 · 결말 화면에서 정답을 확인하세요.',action:'사건의 결말 보기'});return}state.fail++;state.history.unshift(`R${state.round} 최종 지목 실패 · 총 ${match}개 일치`);if(state.fail>=2||state.round===12){state.won=false;state.ended=true;save();show('end');showHost({voiceScope:'deduction',voicePhase:'result',kind:'danger',icon:'⌛',eyebrow:'최종 판정 · 실패',title:'진실은 어둠 속에',body:`최종 지목은 ${match}개만 일치했습니다. 더 이상 기회가 남지 않았습니다.`,sub:'수사 종료 · 결말 화면에서 정답을 확인하세요.',action:'결말 보기'});}else{message=`지목이 틀렸어요. 세 항목 중 ${match}개 일치했어요. 남은 기회는 1회입니다. 같은 라운드 재추리는 불가하며 다음 라운드부터 가능해요.`;save();render();showHost({voiceScope:'deduction',voicePhase:'result',kind:'warning',icon:'⚖',eyebrow:`R${state.round} · 최종 추리 판정`,title:'아직 답이 아니다',body:`세 항목 중 정답과 일치한 것은 ${match}개. 하지만 어떤 항목이 틀렸는지는 공개되지 않습니다.`,sub:'다음 라운드부터 두 번째 최종 추리가 가능합니다.',action:'수사 계속하기'});}}
app.addEventListener('click',(e)=>{const el=e.target.closest('[data-act]');if(!el)return;const act=el.dataset.act;if(personDialog>=0){if(act==='person-close'){personDialog=-1;render();return}if(act==='person-probe'&&state.phase==='investigate'&&!state.ended&&personDialog!==state.playerCharacter){probePicks.person=personDialog;personDialog=-1;show('probe');return}return}if(hostScene)return;if(act==='new'){if(state.started&&!state.ended&&!confirm('진행 중인 사건을 지우고 새로 시작할까요?'))return;selectedAvatar=-1;show('choose');return}if(act==='choose-back'){show('home');return}if(act==='confirm-character'){start();return}if(act==='resume'){show('map');return}if(act==='submit'){submit();return}if(act==='open-probe'){if(state.phase==='investigate'){if(BRWorld.migrate(state).investigated>=3){message='가설 조사 최대 3회를 사용했어요. NPC 만남, 증거 조사, 특별 능력을 이용하세요.';render();return}const why=BRWorld.canInfo(state);if(why){message=why;render();return}show('probe')}else show('probeResult');return}
 if(act==='cancel-route'){selectedDestination=-1;render();return}
 if(act==='confirm-route'){
  if(selectedDestination<0)return;
  const destination=selectedDestination;
  const result=BRWorld.travel(state,destination,worldChoice.mode);
  if(result.ok){selectedDestination=-1;worldChoice.mode='walk';save();currentTab='map';message=result.message;showHost({kind:'round',icon:'⌂',eyebrow:`R${state.round} · 방 이동`,title:'이동 완료',body:result.message,sub:'연결된 경로를 따라 이동했습니다.'});}
  else{selectedDestination=-1;message=result.message;render();}
  return;
 }
 if(act==='cancel-suspicion'){suspectTarget=-1;suspectGuess={room:-1,item:-1};show('map');return}
 if(act==='submit-suspicion'){submitSuspicion();return}
 if(act==='check-forensic'){worldResult(BRWorld.checkForensic(state));return}
 if(act==='pass-turn'){
  if(state.ended||state.phase!=='investigate')return;
  state.phase='review';state.lastInvestigation=null;
  state.history.unshift(`R${state.round} · 정보행동 없이 턴 종료`);
  save();currentTab='map';message='이번 라운드는 행동을 하지 않고 넘겼습니다. 다음 라운드로 진행할 수 있습니다.';showHost({kind:'round',icon:'◇',eyebrow:`R${state.round} · 라운드 정산`,title:'이번 라운드 종료',body:'정보행동 없이 이번 라운드를 종료했습니다. 수첩을 확인한 뒤 다음 라운드로 넘어가세요.'});return;
 }
 if(act==='ability-mode'){const ab=abilityDef();if(state.world.ability.left>0){worldChoice.mode=worldChoice.mode===ab.id?'walk':ab.id;selectedDestination=-1;render()}return}
 if(act==='ability-use'){worldResult(BRWorld.useAbility(state,worldChoice.npc,worldChoice.kind,abilityDef().id==='interrogate'?worldChoice.weapon:worldChoice.item));return}if(act==='next-round'){nextRound();return}if(act==='open-accuse'){show('accuse');return}if(act==='submit-probe'){submitProbe();return}if(act==='back-map'){show('map');return}if(act==='open-notes'){show('notes');return}if(act==='log'){show('notes');return}const [type,val,extra]=act.split(':');if(type==='choose'){const n=Number(val);if(currentTab==='choose'&&Number.isInteger(n)&&n>=0&&n<6){selectedAvatar=n;message='';render()}return}if(type==='room'){
  const destination=Number(val);
  const route=BRWorld.routeInfo(state,destination,worldChoice.mode);
  if(!route.ok){selectedDestination=-1;message=route.message;render();return;}
  selectedDestination=destination;message='';show('map');return;
 }
 if(type==='suspect'){
  const npcId=Number(val);
  const why=BRWorld.canInfo(state);
  if(why){message=why;render();return}
  if(state.ended||BRWorld.migrate(state).suspicions>=3||!BRWorld.npcHere(state).some(n=>n.id===npcId)){message='의심 기회를 사용했거나 그 인물이 같은 방에 없어요.';render();return}
  suspectTarget=npcId;suspectGuess={room:-1,item:-1};show('suspect');return;
 }
 if(type==='cooperate'){const reason=BRWorld.canInfo(state);if(reason){message=reason;render();return}worldResult(BRWorld.interaction(state,Number(val),'cooperate',worldChoice.weapon),'cooperation');return}
 if(type==='tab'){show(val);return}if(type==='person'){activePerson=+val;personDialog=activePerson;render();return}if(type==='mark'){markCandidate(val,Number(extra));return}});
function updateChoices(e){const el=e.target.closest('[data-pick]');if(el){const target=currentTab==='probe'?probePicks:picks;target[el.dataset.pick]=Number(el.value)}
 const sp=e.target.closest('[data-suspicion-pick]');
 if(sp){suspectGuess[sp.dataset.suspicionPick]=Number(sp.value)}
 const w=e.target.closest('[data-world-pick]');
 if(w){const k=w.dataset.worldPick;worldChoice[k]=k==='kind'?w.value:Number(w.value);if(k==='kind'){worldChoice.item=0;render()}}
}
app.addEventListener('change',updateChoices);
app.addEventListener('input',updateChoices);
render();
})();
