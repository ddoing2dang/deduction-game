/* Mobile solo mystery: game-world rules, AI residents and events. */
const BRWorld = (function buildWorld(){
 "use strict";
 const EDGES=[[1,2,3],[0,5,6],[0,6,7],[0,4,5],[3,8],[1,3,6],[1,2,5,7],[2,6,8],[4,7]];
 const ABILITIES=[
  {id:"dash",name:"질주",charges:3,desc:"이번 라운드 이동력 +1P (3회)"},
  {id:"jump",name:"도약",charges:1,desc:"원하는 방으로 즉시 이동 (1회)"},
  {id:"trace",name:"추적",charges:2,desc:"다른 방의 NPC 보유 장소카드 1장 확인 (2회, 정보행동 소비)"},
  {id:"interrogate",name:"심문",charges:1,desc:"같은 방 인물의 현재 장소·선택 흉기 카드 YES/NO 확인 (1회, 정보행동 소비)"},
  {id:"analysis",name:"정보분석",charges:2,desc:"지정한 장소·흉기 카드의 배분 여부 확인 (2회, 정보행동 소비)"},
  {id:"return",name:"회귀",charges:2,desc:"바로 전에 방문한 방으로 순간 복귀 (2회)"},
  {id:"detour",name:"우회",charges:2,desc:"최대 두 연결을 1P에 통과 (2회)"},
  {id:"gate",name:"문지기",charges:3,desc:"잠긴 연결의 추가 이동력 1P 무시 (3회)"}
 ];
 const TRAITS=[
  {id:"swift",name:"신속",desc:"다음 라운드에 최대 세 칸 이동"},
  {id:"helpful",name:"협조적",desc:"협조 요청을 반드시 수락"},
  {id:"careful",name:"신중",desc:"협조 요청 수락 확률 낮음"},
  {id:"stationary",name:"신중한 이동",desc:"자주 현재 방에 머무름"},
  {id:"ordinary",name:"일반 이동",desc:"한두 칸 이동"},
  {id:"roamer",name:"배회",desc:"새로운 방을 찾아 이동"}
 ];
 const rnd=n=>Math.floor(Math.random()*n);
 const pick=arr=>arr[rnd(arr.length)];
 const shuffle=arr=>{for(let i=arr.length-1;i>0;i--){let j=rnd(i+1);[arr[i],arr[j]]=[arr[j],arr[i]];}return arr;};
 const edge=(a,b)=>a<b?`${a}-${b}`:`${b}-${a}`;
 function pathBetween(from,to,limit=8,passage=null){
  if(from===to)return [from];
  const queue=[[from]],visited=new Set([from]);
  while(queue.length){
   const path=queue.shift(),last=path[path.length-1];
   const choices=EDGES[last].slice();
   if(passage&&passage.type==="secret_passage"&&passage.activated){if(last===passage.from)choices.push(passage.to);if(last===passage.to)choices.push(passage.from)}
   for(const next of choices){
    if(visited.has(next))continue;
    const np=path.concat(next);
    if(next===to)return np;
    if(np.length<=limit){visited.add(next);queue.push(np);}
   }
  }
  return null;
 }
 function setup(state){
  if(state.world)return state.world;
  const notAnswer=(kind,total)=>shuffle(Array.from({length:total},(_,i)=>i).filter(i=>i!==state.answer[kind]));
  const roomCards=notAnswer("room",9),itemCards=notAnswer("item",9);
  const all=Array.from({length:6},(_,i)=>{
   const a=pick(ABILITIES);
   return {id:i,room:i===state.playerCharacter?0:rnd(9),trait:pick(TRAITS).id,ability:{type:a.id,left:a.charges},intel:[],hand:{room:roomCards[i],item:itemCards[i]}};
  });
  const own=all.find(x=>x.id===state.playerCharacter);
  const npcs=all.filter(x=>x.id!==state.playerCharacter);
  // Legacy save may have no selected player. Support sentinel -1 by assigning a temporary card.
  const owner=own?own.hand:{room:roomCards[0],item:itemCards[0]};
  const candidateRooms=shuffle([1,2,3,4,5,6,7,8]);
  // EVENTS.md: only one additional event per case, once in R6–R11.
  const extraType=pick(["blackout","broken_door","secret_passage"]);
  const extraRound=6+rnd(6);
  const extraLocation=pick(candidateRooms);
  const a=rnd(9),b=pick(EDGES[a]);
  const extraEdge=edge(a,b);
  const pairs=[];for(let i=0;i<9;i++)for(let j=i+1;j<9;j++)if(!EDGES[i].includes(j))pairs.push([i,j]);
  const passage=pick(pairs);
  const special={type:extraType,round:extraRound,room:extraLocation,edge:extraEdge,from:passage[0],to:passage[1],activated:false};
  const ability=pick(ABILITIES);
  state.world={npcs,ownHand:owner,unassigned:{room:roomCards.slice(6),item:itemCards.slice(6)},ability:{type:ability.id,left:ability.charges},moveLeft:2,prevRoom:0,investigated:0,suspicions:0,cooperations:0,awaiting:null,event:null,eventLog:[],sabotageUses:{smoke:2,lock:2,delay:2},key:{appears:2+rnd(4),room:pick(candidateRooms),taken:false,owner:null},box:{appears:null,room:null,opened:false,owner:null},special,forensic:{arrived:false,read:false,cards:{room:pick(roomCards.slice(6)),item:pick(itemCards.slice(6))}},clueRead:[],released:null};
  return state.world;
 }
 function migrate(state){
  const w=setup(state);
  if(!Number.isFinite(w.moveLeft))w.moveLeft=2;
  if(!Array.isArray(w.clueRead))w.clueRead=[];
  // Remove unsupported location-based eyewitness claims from old browser saves.
  delete w.witnesses;
  state.history=Array.isArray(state.history)?state.history.filter(v=>!/목격자 진술|범인은 .* 중 한 곳|특별 단서 ·/.test(v)):[];
  w.clueRead=w.clueRead.filter(c=>c.via!=="evidence");
  delete w.hotspots;
  if(!Array.isArray(w.eventLog))w.eventLog=[];
  if(!w.sabotageUses)w.sabotageUses={smoke:2,lock:2,delay:2};
  if(!w.special){const a=rnd(9),b=pick(EDGES[a]);const pairs=[];for(let i=0;i<9;i++)for(let j=i+1;j<9;j++)if(!EDGES[i].includes(j))pairs.push([i,j]);const t=pick(["blackout","broken_door","secret_passage"]);const chosen=pick(pairs);w.special={type:t,round:Math.min(11,Math.max(6,state.round<11?state.round+1:11)),room:rnd(9),edge:edge(a,b),from:chosen[0],to:chosen[1],activated:false};}
  if(!w.forensic)w.forensic={arrived:state.round>=7,read:false,cards:{room:w.unassigned.room[rnd(w.unassigned.room.length)],item:w.unassigned.item[rnd(w.unassigned.item.length)]}};
  if(w.box.appears!=null&&w.key.taken&&w.box.appears<=w.key.acquiredRound&&state.round<w.box.appears)w.box.appears=Math.min(8,w.key.acquiredRound+1);
  if(w.key.owner===undefined)w.key.owner=w.key.taken?"player":null;
  if(w.box.owner===undefined)w.box.owner=null;
  for(const n of w.npcs){
   if(!n.ability){const a=pick(ABILITIES);n.ability={type:a.id,left:a.charges}}
   if(!Array.isArray(n.intel))n.intel=[];
  }
  return w;
 }
 function canInfo(state){
  const w=migrate(state);
  if(state.phase!=="investigate"||state.ended)return "이번 라운드 정보행동은 이미 끝났어요.";
  if(w.event?.kind==="smoke"&&w.event.rooms.includes(state.room))return "연막이 가득한 방입니다. 다른 방으로 이동해야 정보행동을 할 수 있어요.";
  if(w.special.type==="blackout"&&w.special.activated&&state.round>=w.special.round&&state.round<=w.special.round+1&&state.room===w.special.room)return "정전 중인 방입니다. 2라운드 동안 이동 외 행동을 할 수 없어요.";
  return "";
 }
 function consumeInfo(state,summary){
  const reason=canInfo(state);if(reason)return {ok:false,message:reason};
  state.spent++;
  state.phase="review";
  state.world.eventLog.unshift(`R${state.round} · ${summary}`);
  return {ok:true,message:summary};
 }
 function npcHere(state){return migrate(state).npcs.filter(n=>n.room===state.room)}
 function travel(state,to,mode="walk"){
  const w=migrate(state);
  if(state.ended)return {ok:false,message:"사건이 종료되었습니다."};
  if(to===state.room)return {ok:false,message:"이미 그 방에 있어요."};
  if(!Number.isInteger(to)||to<0||to>8)return {ok:false,message:"유효하지 않은 방입니다."};
  const special=w.special?.activated&&w.special.type==="secret_passage"&&state.round===w.special.round?w.special:null;
  const path=pathBetween(state.room,to,8,special);
  if(!path)return {ok:false,message:"이동 경로가 없어요."};
  const ability=w.ability;
  if(mode==="jump"&&ability.type==="jump"&&ability.left>0){
   ability.left--;w.prevRoom=state.room;state.room=to;
   const found=arrive(state);
   return {ok:true,message:`도약 능력 사용 · 목적지로 이동! ${found}`};
  }
  const hops=path.length-1;
  const event=w.event;
  let extra=0;
  for(let i=1;i<path.length;i++){
   const passageEdge=edge(path[i-1],path[i]);
   const villainLock=event?.kind==="lock"&&event.edge===passageEdge;
   const broken=w.special?.activated&&w.special.type==="broken_door"&&state.round===w.special.round&&w.special.edge===passageEdge;
   if(villainLock||broken)extra++; // Never stack two +1P surcharges on the same edge.
  }
  let price=hops+extra;
  if(mode==="detour"&&ability.type==="detour"&&ability.left>0&&hops===2)price=1+extra;
  if(extra>0&&ability.type==="gate"&&ability.left>=extra){price-=extra;}
  if(price>w.moveLeft)return {ok:false,message:`이 방까지 ${price}P 필요해요. 현재 ${w.moveLeft}P 남았습니다. 연결된 가까운 방부터 이동하세요.`};
  if(extra>0&&ability.type==="gate"&&ability.left>=extra)ability.left-=extra;
  if(mode==="detour"&&hops===2&&ability.type==="detour"&&ability.left>0)ability.left--;
  w.moveLeft-=price;
  w.prevRoom=state.room;state.room=to;
  const found=arrive(state);
  return {ok:true,message:`${hops}개 연결 이동 · ${price}P 사용, 남은 ${w.moveLeft}P. ${found}`};
 }
 function claimKey(state,owner){
  const w=migrate(state);
  if(w.key.taken||state.round<w.key.appears)return "";
  if(owner==="player"&&state.room!==w.key.room)return "";
  if(owner!=="player"&&(!w.npcs.find(n=>n.id===owner&&n.room===w.key.room)))return "";
  w.key.taken=true;w.key.owner=owner;w.key.acquiredRound=state.round;
  if(w.box.appears===null){
   const first=Math.max(4,state.round+1);
   if(first<=8){w.box.appears=first+rnd(9-first);w.box.room=rnd(9);}
  }
  if(owner==="player"){
   state.history.unshift("R"+state.round+" · 낡은 열쇠를 비공개로 획득했습니다.");
   return "낡은 열쇠를 획득했습니다. 소유 정보는 비공개입니다.";
  }
  return "";
 }
 function arrive(state){
  const w=migrate(state),texts=[];
  const key=claimKey(state,"player");
  if(key)texts.push(key);
  if(w.box.appears!==null&&state.round>=w.box.appears&&!w.box.opened&&state.room===w.box.room&&w.key.owner==="player"){
   w.box.opened=true;w.box.owner="player";
   const room=pick(w.unassigned.room),item=pick(w.unassigned.item);
   w.clueRead.push({kind:"room",id:room,via:"secret"},{kind:"item",id:item,via:"secret"});
   state.history.unshift("R"+state.round+" · 비밀상자 개봉(개인): 미배분 장소 카드와 흉기 카드 각각 한 장 확인");
   texts.push("비밀상자를 열었습니다. 정답이 아닌 장소·흉기 카드 각 한 장을 확인했습니다.");
  }
  return texts.join(" ");
 }
 function checkForensic(state){
  const w=migrate(state);
  if(state.round<7)return {ok:false,message:"R7에 과학수사 결과가 도착합니다."};
  if(state.room!==0)return {ok:false,message:"현관 홀(로비)에서만 과학수사 결과를 확인할 수 있어요."};
  if(w.forensic.read)return {ok:false,message:"이 과학수사 결과는 이미 확인했습니다. 수첩에서 다시 볼 수 있어요."};
  const reason=canInfo(state);if(reason)return {ok:false,message:reason};
  const result=consumeInfo(state,"과학수사 결과를 개인적으로 확인했습니다.");
  if(!result.ok)return result;
  w.forensic.read=true;
  w.clueRead.push({kind:"room",id:w.forensic.cards.room,via:"forensic"},{kind:"item",id:w.forensic.cards.item,via:"forensic"});
  state.history.unshift("R"+state.round+" · 과학수사(개인): 미배분 장소/흉기 카드 1장씩 확인");
  return {...result,cards:w.forensic.cards};
 }
 function searchEvidence(){return {ok:false,message:"확정 EVENTS.md에 없는 방별 임의 특별 단서 이벤트는 제거되었습니다."};}
 function interaction(state,npcId,action,weapon,suspectedRoom=state.room){
  const w=migrate(state),npc=w.npcs.find(x=>x.id===npcId);
  if(!npc||npc.room!==state.room)return {ok:false,message:"그 인물은 지금 같은 방에 없습니다. 지도에서 위치를 찾아 이동하세요."};
  const reason=canInfo(state);if(reason)return {ok:false,message:reason};
  if(action==="suspect"){
   if(w.suspicions>=3)return {ok:false,message:"이번 사건의 의심 기회 3회를 모두 사용했어요."};
   if(!Number.isInteger(suspectedRoom)||suspectedRoom<0||suspectedRoom>=9||!Number.isInteger(weapon)||weapon<0||weapon>=9)return {ok:false,message:"의심할 장소와 흉기를 각각 선택하세요."};
   const cards=[];
   if(npc.hand.room===suspectedRoom)cards.push({kind:"room",id:npc.hand.room});
   if(npc.hand.item===weapon)cards.push({kind:"item",id:npc.hand.item});
   const found=cards.length?pick(cards):null;
   w.suspicions++;
   const res=consumeInfo(state,found?"의심 성공 · 상대가 보유한 장소/흉기 카드 중 1장을 확인했습니다.":"의심 결과 · 상대에게 선택한 장소/흉기 카드가 없었습니다.");
   if(found)w.clueRead.push({...found,via:"suspect",person:npc.id});
   state.history.unshift(`R${state.round} · 인물 #${npc.id+1} 의심 [장소 #${suspectedRoom+1} / 흉기 #${weapon+1}] · ${found?found.kind+" 카드 #"+(found.id+1)+" 공개":"두 카드 모두 보유하지 않음"}`);
   return {...res,found,npc:npc.id};
  }
  if(action==="cooperate"){
   if(w.cooperations>=1)return {ok:false,message:"이번 사건의 협조 기회 1회를 모두 사용했어요."};
   const trait=npc.trait;
   const accept=trait==="helpful"||Math.random()<(trait==="careful"?.55:.83);
   if(!accept)return {ok:false,message:"상대가 협조를 거절했어요. 행동권은 소모되지 않았습니다."};
   w.cooperations++;
   const found=Math.random()<.5?{kind:"room",id:npc.hand.room}:{kind:"item",id:npc.hand.item};
   w.clueRead.push({...found,via:"cooperate",person:npc.id});
   const res=consumeInfo(state,"협조 수락 · 상대가 보유한 카드 한 장을 보여주었습니다.");
   state.history.unshift(`R${state.round} · 같은 방 협조 · ${found.kind} 카드 #${found.id+1} 확인`);
   return {...res,found,npc:npc.id};
  }
  return {ok:false,message:"알 수 없는 행동입니다."};
 }
 function useAbility(state,target=-1,itemType="item",itemId=0){
  const w=migrate(state),ability=w.ability;
  if(ability.left<=0)return {ok:false,message:"능력 횟수를 모두 사용했습니다."};
  if(ability.type==="dash"){
   w.moveLeft++;ability.left--;return {ok:true,message:"질주! 이번 라운드 이동력 +1P."};
  }
  if(ability.type==="return"){
   if(state.room===w.prevRoom)return {ok:false,message:"돌아갈 이전 방이 없어요."};
   ability.left--;
   const here=state.room;state.room=w.prevRoom;w.prevRoom=here;
   return {ok:true,message:"회귀 발동! 이전 방으로 순간 이동. "+arrive(state)};
  }
  if(ability.type==="trace"){
   const reason=canInfo(state);if(reason)return {ok:false,message:reason};
   const npc=w.npcs.find(n=>n.id===target);
   if(!npc)return {ok:false,message:"추적할 NPC를 선택하세요."};
   ability.left--;const res=consumeInfo(state,"추적 · 지정한 인물의 장소 카드를 확인했습니다.");
   const found={kind:"room",id:npc.hand.room,via:"trace",person:npc.id};
   w.clueRead.push(found);state.history.unshift(`R${state.round} · 추적 · 장소 카드 #${found.id+1} 확인`);
   return {...res,found};
  }
  if(ability.type==="interrogate"){
   const reason=canInfo(state);if(reason)return {ok:false,message:reason};
   const npc=w.npcs.find(n=>n.id===target&&n.room===state.room);
   if(!npc)return {ok:false,message:"심문은 같은 방의 NPC에게만 가능합니다."};
   if(w.suspicions>=3)return {ok:false,message:"의심 횟수가 부족해 심문할 수 없어요."};
   ability.left--;w.suspicions++;
   const roomYes=npc.hand.room===state.room,itemYes=npc.hand.item===itemId;
   const res=consumeInfo(state,`심문 결과 · 현재 장소 카드 ${roomYes?"YES":"NO"} / 선택 도구 카드 ${itemYes?"YES":"NO"}`);
   state.history.unshift(`R${state.round} · 심문 · 현재 장소 ${roomYes?"YES":"NO"}, 도구 #${itemId+1} ${itemYes?"YES":"NO"}`);
   return res;
  }
  if(ability.type==="analysis"){
   const reason=canInfo(state);if(reason)return {ok:false,message:reason};
   if(!["room","item"].includes(itemType)||!Number.isInteger(itemId)||itemId<0||itemId>8)return {ok:false,message:"분석할 카드를 고르세요."};
   const distributed=w.npcs.some(n=>n.hand[itemType]===itemId)||w.ownHand[itemType]===itemId;
   ability.left--;
   const res=consumeInfo(state,`정보분석 · 해당 카드가 6인 손패에 배분되었는가? ${distributed?"YES":"NO"}`);
   state.history.unshift(`R${state.round} · 정보분석 · ${itemType} 카드 #${itemId+1} 배분 ${distributed?"YES":"NO"}`);
   return res;
  }
  return {ok:false,message:"이 능력은 이동할 방을 선택할 때 발동해요."};
 }
 function nextRound(state){
  const w=migrate(state);
  w.released=w.awaiting;
  w.awaiting=null;
  w.moveLeft=2;
  w.event=null;
  for(const npc of w.npcs){
   const skill=npc.ability;
   if(skill.type==="jump"&&skill.left>0&&Math.random()<.22){
    npc.prevRoom=npc.room;npc.room=rnd(9);skill.left--;continue;
   }
   if(skill.type==="return"&&skill.left>0&&Number.isInteger(npc.prevRoom)&&Math.random()<.20){
    const from=npc.room;npc.room=npc.prevRoom;npc.prevRoom=from;skill.left--;continue;
   }
   let steps=npc.trait==="swift"?3:npc.trait==="stationary"?rnd(2):1+rnd(2);
   if((skill.type==="dash"||skill.type==="detour")&&skill.left>0&&Math.random()<.36){steps++;skill.left--}
   if(["trace","analysis","interrogate"].includes(skill.type)&&skill.left>0&&Math.random()<.36){
    const others=w.npcs.filter(o=>o.id!==npc.id);
    if(others.length){const victim=pick(others),kind=Math.random()<.5?"room":"item";npc.intel.push({kind,id:victim.hand[kind]});skill.left--}
   }
   for(let j=0;j<steps;j++){
    if(npc.trait==="stationary"&&Math.random()<.35)continue;
    const from=npc.room;const options=npc.trait==="roamer"?EDGES[from].filter(room=>room!==npc.prevRoom):EDGES[from];npc.room=pick(options.length?options:EDGES[from]);npc.prevRoom=from;
   }
  }
  // Actual culprit can disrupt with hidden origin. Never identify the actor in UI.
  if(state.round>=2&&state.round<=11&&Math.random()<.62&&Object.values(w.sabotageUses).some(n=>n>0)){
   const types=["smoke","lock","delay"].filter(k=>w.sabotageUses[k]>0);
   const kind=pick(types);
   w.sabotageUses[kind]--;
   if(kind==="smoke")w.event={kind,rooms:[pick(w.npcs).room]};
   if(kind==="lock"){const a=rnd(9),b=pick(EDGES[a]);w.event={kind,edge:edge(a,b)};}
   if(kind==="delay")w.event={kind};
   w.eventLog.unshift(`R${state.round} · 출처 불명의 사건: ${kind==="smoke"?"연막":kind==="lock"?"문 잠금":"조사 결과 지연"}`);
  }
  const announce=[];
  if(w.released){
   announce.push(`지난 R${w.released.round} 조사 결과가 도착했습니다: ${w.released.match}개 일치`);
   state.history.unshift(`R${w.released.round} 지연된 조사 · [${w.released.chosen.join(" / ")}] → ${w.released.match}개 일치`);
  }
  if(w.key.taken&&!w.box.opened&&state.round===w.box.appears)announce.push("비밀상자 출현! 지도에서 해당 방을 확인하세요.");
  if(state.round===w.key.appears&&!w.key.taken)announce.push("낡은 열쇠가 저택 어딘가에 출현했어요.");
  if(state.round===3||state.round===6||state.round===9||state.round===12){
   const culprit=w.npcs.find(n=>n.id===state.answer.person);
   if(culprit){
    const others=shuffle(w.npcs.filter(n=>n.id!==culprit.id).map(n=>n.room));
    const candidates=[culprit.room];
    for(const room of others)if(!candidates.includes(room)&&candidates.length<4)candidates.push(room);
    for(const room of shuffle(Array.from({length:9},(_,i)=>i)))if(!candidates.includes(room)&&candidates.length<4)candidates.push(room);
    const regions=shuffle(candidates);
    const names=["현관 홀","응접실","서재","식당","주방","침실","음악실","온실","지하 와인창고"];
    w.witnesses.push({round:state.round,rooms:regions});
    const clue="목격자 진술 · 이 라운드 시작 시 범인은 "+regions.map(i=>names[i]).join(" / ")+" 중 한 곳에 있었습니다.";
    state.history.unshift("R"+state.round+" "+clue);
    announce.push(clue+" 어느 인물인지는 알려주지 않습니다.");
   }
  }
  announce.push(arrive(state));
  return announce.filter(Boolean);
 }
 return {EDGES,ABILITIES,TRAITS,setup,migrate,pathBetween,canInfo,consumeInfo,npcHere,travel,arrive,searchEvidence,interaction,useAbility,nextRound};
})();
