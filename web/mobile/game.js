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
let currentTab=state.started?(state.ended?'end':'map'):'home';
let activePerson=0;
let message='';
let picks={person:-1,room:-1,item:-1};
let probePicks={person:-1,room:-1,item:-1};
const app=document.getElementById('app');
function freshState(started=true){return{started,ended:false,won:false,answer:{person:Math.floor(Math.random()*6),room:Math.floor(Math.random()*9),item:Math.floor(Math.random()*9)},spent:0,fail:0,room:0,marked:{person:[],room:[],item:[]},history:[],lastInvestigation:null,seed:Date.now()};}
function save(){try{localStorage.setItem(KEY,JSON.stringify(state))}catch(e){console.warn('저장 불가',e)}}
function readState(){try{const raw=JSON.parse(localStorage.getItem(KEY)||'null');if(raw&&raw.answer&&raw.marked&&raw.history&&typeof raw.spent==='number'&&raw.answer.person>=0&&raw.answer.person<6)return raw}catch(e){}return null}
function esc(s){return String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]))}
const portrait=c=>`assets/characters/${c.portrait}`;
function img(c,klass='hero-photo'){return `<img class="${klass}" src="${portrait(c)}" alt="${esc(c.name)} 캐릭터 확정 원본 이미지" loading="lazy" onerror="this.style.display='none';this.insertAdjacentHTML('afterend','<div class=&quot;muted&quot;>이미지 준비 중</div>')">`}
function btn(txt,fn,cls='button',off=false){return `<button type="button" class="${cls}" data-act="${fn}" ${off?'disabled':''}>${txt}</button>`}
function header(){return `<header class="topbar"><div><div class="smallcaps">BLACK ROSE · MOBILE</div><div class="brand">검은 장미의 비밀</div></div><div class="chip">${state.started?`단서 ${state.spent}/12`:'2D 추리 게임'}</div></header>`}
function panel(title,body){return `<section class="panel"><div class="eyebrow">BLACK ROSE MANSION</div><h2 class="section-title">${title}</h2>${body}</section>`}
function intro(){return `<section class="panel hero"><div class="badge-float">싱글플레이</div><div class="eyebrow">어둠 속에 숨겨진 진실</div>${img(CHARACTERS[0])}<h1 class="hero-title">장미가 지는 밤</h1><p class="hero-desc">저택에서 벌어진 수상한 사건.<br>여섯 인물, 아홉 개의 방, 아홉 개의 도구.<br>단서를 수집해 숨겨진 조합을 밝혀내세요.</p>${btn('새로운 사건 시작','new','button primary full')}<p class="lead">${state.started&&!state.ended?'이전 사건이 저장되어 있어요.':''}</p>${state.started&&!state.ended?btn('이어서 플레이','resume','button secondary full'):''}</section>${panel('플레이 방법',`<p class="lead">① 방을 고른 뒤 조사할 세 항목을 직접 선택해요.<br>② 범인·장소·도구 조사할 범인·장소·도구를 직접 골라 일치 개수를 확인해요.<br>③ 남은 후보를 조합해 최종 지목하세요.</p><div class="evidence">모바일 전용 간편 규칙 · 선택형 조사 최대 12회 · 최종 지목 기회 2회 · 휴대폰에 자동 저장</div>`)}<div class="footer">기존 PC Unity 게임과 분리된 독립 게임입니다.</div>`}
function summary(){return `<div class="case-stat"><div class="stat"><b>${12-state.spent}</b><span>남은 조사</span></div><div class="stat"><b>${2-state.fail}</b><span>지목 기회</span></div><div class="stat"><b>${state.marked.person.length+state.marked.room.length+state.marked.item.length}</b><span>수동 표시</span></div></div>`}
function map(){let cells=ROOMS.map(([name,emoji],i)=>`<button class="room ${state.room===i?'active':''}" data-act="room:${i}"><span class="symbol">${emoji}</span><span class="room-name">${name}</span></button>`).join('');return `${summary()}${panel('저택 지도',`<p class="lead">방을 터치해 이동하세요. 이동은 조사 횟수를 소모하지 않아요.</p><div class="grid3">${cells}</div>`)}${panel(ROOMS[state.room][0],`<p class="narrative">${ROOMS[state.room][2]}. 발걸음 소리만 희미하게 울린다.</p><div class="sep"></div><div class="eyebrow">가설 직접 조사</div><p class="hint">범인 1명 + 장소 1곳 + 도구 1종을 직접 선택하고 제출하세요. 맞은 항목의 개수만 알려줘요.</p>${btn('조사하기 · 직접 3항목 선택','open-probe','button primary full',state.spent>=12)}`)}${state.history.length?panel('최근 증거',`<div class="evidence">${esc(state.history[0])}</div>`):''}`}
function people(){return `${panel('인물 도감',`<p class="lead">확정된 캐릭터 6명. 터치하면 프로필과 대화를 확인할 수 있어요. 가벼운 대화는 조사 기회를 소모하지 않습니다.</p><div class="grid2">${CHARACTERS.map((c,i)=>`<button class="card-person ${state.marked.person.includes(i)?'eliminated':''}" data-act="person:${i}">${img(c,'')}<strong>${c.name}</strong><small>${state.marked.person.includes(i)?'⊖ 내가 표시한 후보':'프로필 보기'}</small></button>`).join('')}</div>`)}${panel(CHARACTERS[activePerson].name,`${img(CHARACTERS[activePerson],'scene-person')}<p class="lead">${CHARACTERS[activePerson].hint}</p><div class="evidence">“${QUOTES[activePerson]}”</div><p class="hint">이 대화는 분위기용이며 정답을 확정해 주는 단서는 아닙니다.</p>`)}`}
function noteChips(kind,names){return `<div class="chips">${names.map((name,i)=>`<button type="button" class="choice ${state.marked[kind].includes(i)?'struck':''}" data-act="mark:${kind}:${i}">${state.marked[kind].includes(i)?'⊖ ': '◇ '}${esc(name)}</button>`).join('')}</div>`}
function notebook(){return `${summary()}${panel('추리 수첩',`<p class="lead">후보를 터치하면 내가 직접 제외 표시할 수 있어요. 표시는 메모일 뿐 자동 판정이 아니며, 실제 정답은 감춰져 있어요.</p><div class="segtitle">범인 후보 · 수동 표시 ${state.marked.person.length}명</div>${noteChips('person',CHARACTERS.map(c=>c.name))}<div class="segtitle">장소 후보 · 수동 표시 ${state.marked.room.length}곳</div>${noteChips('room',ROOMS.map(r=>r[0]))}<div class="segtitle">도구 후보 · 수동 표시 ${state.marked.item.length}종</div>${noteChips('item',ITEMS)}`)}${panel('발견한 증거',state.history.length?`<ol class="notes">${state.history.map(v=>`<li>${esc(v)}</li>`).join('')}</ol>`:`<p class="lead">아직 조사 기록이 없습니다. 지도에서 세 항목을 직접 골라 조사해보세요.</p>`)}`}
function selectField(label,kind,values,choices=picks){return `<label class="segtitle" for="pick-${kind}">${label}</label><select id="pick-${kind}" data-pick="${kind}"><option value="-1">선택해주세요</option>${values.map((v,i)=>`<option value="${i}" ${choices[kind]===i?'selected':''}>${state.marked[kind].includes(i)?'✕ ':''}${esc(v)}</option>`).join('')}</select>`}

function investigationForm(){return `${summary()}${panel('가설 직접 조사',`<p class="lead">범인 · 장소 · 도구 세 항목을 직접 고르고 제출해요. 총 몇 개가 맞았는지만 알려주며, 어떤 항목인지는 알려주지 않아요.</p>${selectField('범인은 누구일까?','person',CHARACTERS.map(c=>c.name),probePicks)}${selectField('어디에서 벌어졌을까?','room',ROOMS.map(r=>r[0]),probePicks)}${selectField('무엇을 사용했을까?','item',ITEMS,probePicks)}${btn('이 조합으로 조사하기','submit-probe','button primary full',state.spent>=12)}<p class="hint">조사 가능 횟수: ${12-state.spent}회 · 제출한 조합과 결과는 내 수첩에 기록됩니다.</p>`)}${btn('지도로 돌아가기','back-map','button secondary full')}`;}
function investigationResult(){
 const last=state.lastInvestigation;
 if(!last)return `${panel('조사 결과','<p>기록이 없습니다.</p>')}${btn('지도 보기','back-map','button primary full')}`;
 const combination=[CHARACTERS[last.chosen.person].name,ROOMS[last.chosen.room][0],ITEMS[last.chosen.item]].map(esc).join(' / ');
 return `${summary()}${panel('조사 결과',`<div class="result" style="font-size:29px;font-weight:800">${last.match}개 일치!</div><p class="lead">세 항목 중 ${last.match}개가 정답과 같습니다. 어느 항목인지는 공개되지 않습니다.</p><div class="evidence">내가 제출한 조합<br><b>${combination}</b></div><p class="hint">자동으로 후보를 지우지 않습니다. 결과를 비교해 수첩에 직접 표시하세요.</p>`)}${btn('추리 수첩 확인','open-notes','button primary full')}<div style="height:9px"></div>${btn('다음 조사하러 가기','back-map','button secondary full')}`;
}
function markCandidate(kind,i){if(!['person','room','item'].includes(kind))return;const max=kind==='person'?6:9;if(!Number.isInteger(i)||i<0||i>=max)return;const marks=state.marked[kind];const index=marks.indexOf(i);if(index<0)marks.push(i);else marks.splice(index,1);save();render();}
function submitProbe(){
 if(state.ended||state.spent>=12)return;
 if(['person','room','item'].some(k=>!Number.isInteger(probePicks[k])||probePicks[k]<0||probePicks[k]>=(k==='person'?6:9))){
  message='범인, 장소, 도구를 모두 선택한 뒤 조사해주세요.';render();return;
 }
 const chosen={...probePicks};
 const match=['person','room','item'].filter(k=>chosen[k]===state.answer[k]).length;
 state.spent++;
 state.lastInvestigation={chosen,match,location:state.room,number:state.spent};
 state.history.unshift(`${state.spent}차 조사 · ${ROOMS[state.room][0]} · [${CHARACTERS[chosen.person].name} / ${ROOMS[chosen.room][0]} / ${ITEMS[chosen.item]}] → ${match}개 일치`);
 save();probePicks={person:-1,room:-1,item:-1};show('probeResult');
}

function accuse(){return `${summary()}${panel('최종 추리',`<p class="lead">조사를 세 번 이상 진행하면 최종 지목할 수 있어요. 틀리면 일치 항목 개수만 알려줍니다.</p>${selectField('누가?','person',CHARACTERS.map(c=>c.name))}${selectField('어디서?','room',ROOMS.map(r=>r[0]))}${selectField('무엇으로?','item',ITEMS)}${btn('세 항목으로 최종 지목','submit','button primary full',state.spent<3||state.fail>=2)}${state.spent<3?'<p class="hint">조사를 최소 3회 진행해야 지목할 수 있어요.</p>':''}`)}<div class="notice">정답과 각 항목의 판정은 마지막까지 비공개입니다. 지목 후에는 총 일치 개수만 확인해요.</div>`}
function ending(){const p=CHARACTERS[state.answer.person],rn=ROOMS[state.answer.room][0],it=ITEMS[state.answer.item];return `<section class="panel hero"><div class="eyebrow">CASE CLOSED</div><h1 class="hero-title">${state.won?'진실을 밝혀냈다':'사건은 미궁 속으로'}</h1>${img(p)}<p class="hero-desc">${state.won?'모든 조각이 하나의 진실을 가리킨다.':'조사 기록은 남았지만, 마지막 기회를 잃었다.'}</p><div class="evidence" style="text-align:left"><b>범인</b> ${p.name}<br><b>장소</b> ${rn}<br><b>도구</b> ${it}</div><div class="sep"></div>${btn('새로운 사건 시작','new','button primary full')}<div style="height:9px"></div>${btn('조사 기록 보기','log','button secondary full')}</section>`}
function navbar(){return `<nav class="bottomnav" aria-label="게임 메뉴">${[['map','⌂','저택'],['people','♙','인물'],['notes','▤','수첩'],['accuse','◆','추리']].map(([id,ico,txt])=>`<button class="navitem ${currentTab===id?'active':''}" data-act="tab:${id}"><span class="navico">${ico}</span>${txt}</button>`).join('')}</nav>`}
function render(){if(!app)return;const title=header();let body='';if(currentTab==='home')body=intro();else if(currentTab==='map')body=map();else if(currentTab==='people')body=people();else if(currentTab==='notes')body=notebook();else if(currentTab==='accuse')body=accuse();else if(currentTab==='probe')body=investigationForm();else if(currentTab==='probeResult')body=investigationResult();else if(currentTab==='end')body=ending();app.innerHTML=title+(message?`<div class="notice" role="status">${esc(message)}</div>`:'')+body+(state.started&&!state.ended?navbar():'');}
function show(tab){currentTab=tab;message='';render();window.scrollTo({top:0,behavior:'instant'})}
function start(){state=freshState();picks={person:-1,room:-1,item:-1};probePicks={person:-1,room:-1,item:-1};save();show('map');message='새 사건이 시작됐습니다. 저택을 조사해보세요.';render()}
function submit(){if(state.spent<3||state.fail>=2||state.ended)return;if(Object.values(picks).some(n=>n<0)){message='범인·장소·도구를 모두 골라주세요.';render();return}const match=['person','room','item'].filter(k=>picks[k]===state.answer[k]).length;if(match===3){state.won=true;state.ended=true;state.history.unshift('최종 지목: 3개 일치, 사건 해결');save();show('end');return}state.fail++;state.history.unshift(`최종 지목 실패 · 총 ${match}개 일치`);if(state.fail>=2){state.won=false;state.ended=true;save();show('end')}else{message=`지목이 틀렸어요. 세 항목 중 ${match}개 일치했어요. 남은 기회는 1회입니다.`;save();render()}}
app.addEventListener('click',(e)=>{const el=e.target.closest('[data-act]');if(!el)return;const act=el.dataset.act;if(act==='new'){if(state.started&&!state.ended&&!confirm('진행 중인 사건을 지우고 새로 시작할까요?'))return;start();return}if(act==='resume'){show('map');return}if(act==='submit'){submit();return}if(act==='open-probe'){show('probe');return}if(act==='submit-probe'){submitProbe();return}if(act==='back-map'){show('map');return}if(act==='open-notes'){show('notes');return}if(act==='log'){show('notes');return}const [type,val,extra]=act.split(':');if(type==='tab'){show(val);return}if(type==='room'){state.room=+val;save();show('map');return}if(type==='person'){activePerson=+val;show('people');return}if(type==='mark'){markCandidate(val,Number(extra));return}});
app.addEventListener('change',(e)=>{const el=e.target.closest('[data-pick]');if(el){const target=currentTab==='probe'?probePicks:picks;target[el.dataset.pick]=Number(el.value)}});
render();
})();
