/* Mobile solo mystery: game-world rules, AI residents and events. */
const BRWorld = (function buildWorld(){
 "use strict";
 const EDGES=[[1,2,3],[0,5,6],[0,6,7],[0,4,5],[3,8],[1,3,6],[1,2,5,7],[2,6,8],[4,7]];
 const ABILITIES=[
  {id:"dash",name:"질주",charges:3,desc:"이번 라운드 이동력 +1P (3회)"},
  {id:"jump",name:"도약",charges:1,desc:"원하는 방으로 즉시 이동 (1회)"},
  {id:"trace",name:"추적",charges:3,desc:"다른 NPC가 있는 방으로 연결 이동할 때 기본 통행비 1P 면제 (3회). 비밀 카드는 열람하지 않음"},
  {id:"interrogate",name:"심문",charges:1,desc:"같은 방 인물의 현재 장소·선택 흉기 카드 YES/NO 확인 (1회, 정보행동 소비)"},
  {id:"analysis",name:"정보분석",charges:2,desc:"지정한 장소·흉기 카드의 배분 여부 확인 (2회, 정보행동 소비)"},
  {id:"return",name:"회귀",charges:2,desc:"바로 전에 방문한 방으로 순간 복귀 (2회)"},
  {id:"detour",name:"우회",charges:2,desc:"최대 두 연결을 1P에 통과 (2회)"},
  {id:"gate",name:"문지기",charges:3,desc:"방 이동 시 기본 통행비 1P 면제 (3회). 잠금·고장난 문의 추가 비용은 유지"}
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
  state.world={npcs,ownHand:owner,unassigned:{room:roomCards.slice(6),item:itemCards.slice(6)},ability:{type:ability.id,left:ability.charges},moveLeft:2,prevRoom:0,prevRoomRound:-1,dashUsedRound:0,investigated:0,suspicions:0,cooperations:0,awaiting:null,event:null,eventLog:[],sabotageUses:{smoke:1,lock:2,delay:2},key:{appears:2+rnd(4),room:pick(candidateRooms),taken:false,owner:null},box:{appears:null,room:null,opened:false,owner:null},special,forensic:{arrived:false,read:false,cards:{room:pick(roomCards.slice(6)),item:pick(itemCards.slice(6))}},clueRead:[],released:null};
  return state.world;
 }
 function migrate(state){
  const w=setup(state);
  if(!Number.isFinite(w.moveLeft))w.moveLeft=2;
  if(!Number.isInteger(w.prevRoomRound))w.prevRoomRound=-1;
  if(!Number.isInteger(w.dashUsedRound))w.dashUsedRound=0;
  if(!Array.isArray(w.clueRead))w.clueRead=[];
  // Remove unsupported location-based eyewitness claims from old browser saves.
  delete w.witnesses;
  state.history=Array.isArray(state.history)?state.history.filter(v=>!/목격자 진술|범인은 .* 중 한 곳|특별 단서 ·/.test(v)):[];
  w.clueRead=w.clueRead.filter(c=>c.via!=="evidence");
  delete w.hotspots;
  if(!Array.isArray(w.eventLog))w.eventLog=[];
  if(!w.sabotageUses)w.sabotageUses={smoke:1,lock:2,delay:2};
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
 function routeInfo(state,to,mode="walk"){
 const w=migrate(state);
 if(state.ended)return {ok:false,reason:"ended",message:"사건이 종료되었습니다."};
 if(state.phase!=="investigate")return {ok:false,reason:"phase",message:"이번 라운드 정보행동을 마쳤어요. 다음 라운드부터 다시 이동할 수 있어요."};
 if(!Number.isInteger(to)||to<0||to>=EDGES.length)return {ok:false,reason:"invalid",message:"유효하지 않은 방입니다."};
 if(to===state.room)return {ok:false,reason:"same",path:[state.room],message:"현재 위치입니다."};
 const ability=w.ability;
 if(mode==="jump"){
  if(ability.type!=="jump"||ability.left<=0)return {ok:false,reason:"ability",message:"도약 능력이 없거나 사용 횟수를 다 썼어요."};
  return {ok:true,path:[state.room,to],cost:0,hops:0,special:"jump",extra:0,message:"도약 능력으로 연결 없이 이동 (0P)"};
 }
 if(mode!=="walk"&&mode!=="detour"&&mode!=="trace")return {ok:false,reason:"ability",message:"이동 모드를 확인해주세요."};
 if(mode==="detour"&&(ability.type!=="detour"||ability.left<=0))return {ok:false,reason:"ability",message:"우회 능력을 사용할 수 없어요."};
 if(mode==="trace"&&(ability.type!=="trace"||ability.left<=0||!w.npcs.some(n=>n.room===to)))return {ok:false,reason:"ability",message:"추적은 다른 NPC가 있는 목적지 방으로 연결 이동할 때만 사용할 수 있어요."};
 const passage=w.special?.activated&&w.special.type==="secret_passage"&&state.round===w.special.round?w.special:null;
 const isExtra=(a,b)=>{
  const segment=edge(a,b);
  const locked=w.event?.kind==="lock"&&w.event.edge===segment;
  const broken=w.special?.activated&&w.special.type==="broken_door"&&state.round===w.special.round&&w.special.edge===segment;
  return locked||broken?1:0;
 };
 let best=null;
 function visit(room,path,visited,extra){
  if(room===to){
   const hops=path.length-1;
   const base=mode==="detour"&&hops===2?1:hops;
   const waived=(ability.type==="gate"&&ability.left>0||mode==="trace")?1:0;
   const cost=Math.max(0,base+extra-waived);
   const candidate={path:path.slice(),cost,hops,extra,special:mode==="trace"?"trace":mode==="detour"&&hops===2?"detour":null,gateUsed:ability.type==="gate"&&waived>0};
   if(!best||candidate.cost<best.cost||candidate.cost===best.cost&&candidate.hops<best.hops)best=candidate;
   return;
  }
  if(path.length>=EDGES.length)return;
  const neighbors=EDGES[room].slice();
  if(passage){if(room===passage.from)neighbors.push(passage.to);else if(room===passage.to)neighbors.push(passage.from);}
  for(const next of neighbors){
   if(visited.has(next))continue;
   visited.add(next);path.push(next);
   visit(next,path,visited,extra+isExtra(room,next));
   path.pop();visited.delete(next);
  }
 }
 visit(state.room,[state.room],new Set([state.room]),0);
 if(!best)return {ok:false,reason:"disconnected",message:"연결된 이동 경로가 없습니다."};
 return {...best,ok:best.cost<=w.moveLeft,
   reason:best.cost<=w.moveLeft?null:"points",
   message:best.cost<=w.moveLeft?"연결된 이동 경로 확인":`이 경로는 ${best.cost}P가 필요해요. 현재 ${w.moveLeft}P 남았습니다.`};
}
 function travel(state,to,mode="walk"){
 const info=routeInfo(state,to,mode);
 if(!info.ok)return {ok:false,message:info.message,route:info};
 const w=migrate(state),ability=w.ability;
 if(info.special==="jump")ability.left--;
 else{
  if(info.special==="detour"||info.special==="trace")ability.left--;
  if(info.gateUsed)ability.left--;
  w.moveLeft-=info.cost;
 }
 for(const step of info.path.slice(1)){
  w.prevRoom=state.room;w.prevRoomRound=state.round;state.room=step;arrive(state);
 }
 const names=["현관 홀","응접실","서재","식당","주방","침실","음악실","온실","지하 와인창고"];
 const route=info.path.map(id=>names[id]).join(" → ");
 state.history.unshift(`R${state.round} 이동 · ${route} (${info.cost}P)`);
 return {ok:true,route:info,message:`이동 완료: ${route} · ${info.cost}P 사용, 남은 ${w.moveLeft}P.`};
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
   if(state.phase!=="investigate"||state.ended)return {ok:false,message:"이동 능력은 정보행동 전에만 사용할 수 있어요."};
   if(w.dashUsedRound===state.round)return {ok:false,message:"질주는 한 라운드에 한 번만 사용할 수 있어요."};
   w.moveLeft++;ability.left--;w.dashUsedRound=state.round;
   return {ok:true,message:"질주! 이번 라운드 이동력 +1P."};
  }
  if(ability.type==="return"){
   if(state.phase!=="investigate"||state.ended)return {ok:false,message:"이동 능력은 정보행동 전에만 사용할 수 있어요."};
   if(w.prevRoomRound<state.round-2||w.prevRoomRound<1||state.room===w.prevRoom)return {ok:false,message:"최근 두 라운드 내에 방문한 복귀 가능한 이전 방이 없어요."};
   ability.left--;
   const here=state.room;state.room=w.prevRoom;w.prevRoom=here;w.prevRoomRound=state.round;
   return {ok:true,message:"회귀 발동! 이전 방으로 순간 이동. "+arrive(state)};
  }
  if(ability.type==="interrogate"){
   const reason=canInfo(state);if(reason)return {ok:false,message:reason};
   const npc=w.npcs.find(n=>n.id===target&&n.room===state.room);
   if(!npc)return {ok:false,message:"심문은 같은 방의 NPC에게만 가능합니다."};
   if(w.suspicions>=3)return {ok:false,message:"의심 횟수가 부족해 심문할 수 없어요."};
   ability.left--;w.suspicions++;
   if(!Number.isInteger(itemId)||itemId<0||itemId>=9)return {ok:false,message:"심문할 흉기를 선택하세요."};
   const roomYes=npc.hand.room===state.room,itemYes=npc.hand.item===itemId;
   const found=roomYes?{kind:"room",id:npc.hand.room}:itemYes?{kind:"item",id:npc.hand.item}:null;
   const res=consumeInfo(state,`심문 결과 · 현재 장소 카드 ${roomYes?"YES":"NO"} / 선택 도구 카드 ${itemYes?"YES":"NO"}`);
   if(found)w.clueRead.push({...found,via:"interrogate",person:npc.id});
   state.history.unshift(`R${state.round} · 심문 · 현재 장소 ${roomYes?"YES":"NO"}, 도구 #${itemId+1} ${itemYes?"YES":"NO"} ${found?"(카드 1장 확인)":""}`);
   return {...res,found};
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
  const delayed=w.awaiting;
  if(w.event?.kind==="delay"&&!delayed)w.sabotageUses.delay=Math.min(2,w.sabotageUses.delay+1);
  w.released=delayed;
  w.awaiting=null;
  w.moveLeft=2;w.event=null;
  const announce=[];
  if(w.released){
   const line="지난 R"+w.released.round+" 조사 결과: "+w.released.match+"개 일치";
   announce.push(line);
   state.history.unshift("R"+w.released.round+" · 지연된 조사 판정 공개: "+w.released.match+"개 일치");
  }
  if(state.round>=7&&!w.forensic.arrived){
   w.forensic.arrived=true;
   announce.push("과학수사 결과가 도착했습니다. 현관 홀(로비)에서 일반 정보행동 1회를 사용해 개인적으로 확인하세요.");
  }
  if(!w.special.activated&&state.round===w.special.round){
   const sp=w.special;
   sp.activated=true;
   const names=["현관 홀","응접실","서재","식당","주방","침실","음악실","온실","지하 와인창고"];
   const title={blackout:"정전",broken_door:"고장난 문",secret_passage:"비밀통로"}[sp.type];
   const detail=sp.type==="blackout"?names[sp.room]+"에서 2라운드(R"+state.round+"~R"+(state.round+1)+") 이동 외 행동 불가":
   sp.type==="broken_door"?names[Number(sp.edge.split("-")[0])]+" ↔ "+names[Number(sp.edge.split("-")[1])]+" 연결 1라운드 동안 통과비 +1P":
   names[sp.from]+" ↔ "+names[sp.to]+" 1라운드 임시 연결(1P)";
   const line="확정 특별이벤트: "+title+"! "+detail;
   w.eventLog.unshift("R"+state.round+" · "+line);
   announce.push(line);
  }
  for(const npc of w.npcs){
   let target=null;
   // An NPC may collect the key if they physically reach its room.
   if(!w.key.taken&&state.round>=Math.max(6,w.key.appears)&&npc===w.npcs[0])target=w.key.room;
   if(w.key.owner===npc.id&&w.box.appears!==null&&state.round>=w.box.appears&&!w.box.opened)target=w.box.room;
   if(target!==null){
    const path=pathBetween(npc.room,target);
    if(path)for(let j=1;j<Math.min(path.length,3);j++){npc.prevRoom=npc.room;npc.room=path[j];}
   }else{
    const skill=npc.ability;
    if(skill.type==="jump"&&skill.left>0&&Math.random()<.22){
     npc.prevRoom=npc.room;npc.room=rnd(9);skill.left--;
    }else if(skill.type==="return"&&skill.left>0&&Number.isInteger(npc.prevRoom)&&Math.random()<.20){
     const from=npc.room;npc.room=npc.prevRoom;npc.prevRoom=from;skill.left--;
    }else{
     let steps=npc.trait==="swift"?3:npc.trait==="stationary"?rnd(2):1+rnd(2);
     if((skill.type==="dash"||skill.type==="detour")&&skill.left>0&&Math.random()<.36){steps++;skill.left--;}
     if(["analysis","interrogate"].includes(skill.type)&&skill.left>0&&Math.random()<.36){
      const others=w.npcs.filter(o=>o.id!==npc.id);
      if(others.length){const victim=pick(others),kind=Math.random()<.5?"room":"item";npc.intel.push({kind,id:victim.hand[kind]});skill.left--;}
     }
     for(let j=0;j<steps;j++){
      if(npc.trait==="stationary"&&Math.random()<.35)continue;
      const from=npc.room;
      const options=npc.trait==="roamer"?EDGES[from].filter(room=>room!==npc.prevRoom):EDGES[from];
      npc.room=pick(options.length?options:EDGES[from]);npc.prevRoom=from;
     }
    }
   }
   if(!w.key.taken&&state.round>=w.key.appears&&npc.room===w.key.room)claimKey(state,npc.id);
   if(w.key.owner===npc.id&&w.box.appears!==null&&state.round>=w.box.appears&&npc.room===w.box.room&&!w.box.opened){
    w.box.opened=true;w.box.owner=npc.id;
    w.eventLog.unshift("R"+state.round+" · 누군가 비밀상자를 개봉했습니다. 열람한 카드는 소유자에게만 공개됩니다.");
   }
  }
  // The culprit's abilities are independent of the single extra event from EVENTS.md.
  if(state.round>=2&&state.round<=11&&Math.random()<.62&&Object.values(w.sabotageUses).some(n=>n>0)){
   const types=["smoke","lock","delay"].filter(k=>w.sabotageUses[k]>0);
   const kind=pick(types);w.sabotageUses[kind]--;
   if(kind==="smoke"){
    const blocked=w.special.type==="blackout"&&w.special.activated&&state.round>=w.special.round&&state.round<=w.special.round+1?[w.special.room]:[];
    const places=shuffle(Array.from({length:9},(_,i)=>i).filter(i=>!blocked.includes(i)));
    w.event={kind,rooms:places.slice(0,2)};
   }
   if(kind==="lock"){const a=rnd(9),b=pick(EDGES[a]);w.event={kind,edge:edge(a,b)};}
   if(kind==="delay")w.event={kind};
   w.eventLog.unshift("R"+state.round+" · 범인 능력: "+{smoke:"연막",lock:"잠금",delay:"교란"}[kind]);
  }
  if(state.round===w.key.appears)announce.push("낡은 열쇠가 나타났습니다. 아직 발견되지 않았다면 지도에서 위치를 확인할 수 있습니다. 획득자 신원은 비공개입니다.");
  if(w.box.appears!==null&&state.round===w.box.appears)announce.push("비밀상자가 나타났습니다. 열쇠 보유자만 열 수 있으며 카드 내용은 개인 정보입니다.");
  const seen=arrive(state);
  if(seen)announce.push(seen);
  return announce;
 }

 return {EDGES,ABILITIES,TRAITS,setup,migrate,pathBetween,routeInfo,canInfo,consumeInfo,npcHere,travel,arrive,checkForensic,interaction,useAbility,nextRound};
})();
