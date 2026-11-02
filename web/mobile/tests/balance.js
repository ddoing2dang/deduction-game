/* Mobile-only balance and economy regression tests. 
 * Run from repository root: node web/mobile/tests/balance.js
 * No external packages or stored user data modified.
 */
'use strict';
const fs=require('node:fs');
const path=require('node:path');
function balanceSuite(worldSource,gameSource) {
 const W=new Function(worldSource+";return BRWorld;")(),fail=(v,m)=>{if(!v)throw Error(m)};
 const records=[];
 function mk(seed=0,round=1){
  const st={started:true,ended:false,won:false,playerCharacter:seed%6,answer:{person:(seed+1)%6,room:3,item:4},phase:"investigate",round,room:0,spent:0,fail:0,lastAccuseRound:0,marked:{person:[],room:[],item:[]},history:[]};
  W.setup(st);st.world.event=null;st.world.key.appears=8;return st;
 }
 for(const type of W.ABILITIES.map(a=>a.id)){
  const st=mk(),w=st.world,wdef=W.ABILITIES.find(a=>a.id===type);w.ability={type,left:wdef.charges};const before=w.ability.left;
  if(type==="dash"){
   fail(W.useAbility(st).ok&&w.moveLeft===3,"dash +P");
   fail(!W.useAbility(st).ok&&w.moveLeft===3&&w.ability.left===before-1,"dash repeat");
   st.phase="review";fail(!W.useAbility(st).ok&&!W.routeInfo(st,1).ok,"post-action move");
  }else if(type==="jump"){
   fail(W.travel(st,8,"jump").ok&&st.room===8&&w.moveLeft===2&&w.ability.left===0,"jump");
   fail(!W.travel(st,0,"jump").ok,"repeat jump");
  }else if(type==="trace"){
   w.npcs.forEach(n=>n.room=8);w.npcs[0].room=1;
   fail(W.routeInfo(st,1,"trace").cost===0,"trace base free");
   const n=w.clueRead.length;
   fail(W.travel(st,1,"trace").ok&&w.ability.left===before-1&&w.clueRead.length===n,"trace card leak");
   fail(!W.routeInfo(st,2,"trace").ok,"trace empty room");
  }else if(type==="interrogate"){
   w.npcs[0].room=0;
   fail(!W.useAbility(st,w.npcs[0].id,"item",-1).ok&&w.ability.left===before&&w.suspicions===0,"invalid interrogate charged");
   const npc=w.npcs[0];const r=W.useAbility(st,npc.id,"item",npc.hand.item);
   fail(r.ok&&r.found&&w.clueRead.length===1&&st.phase==="review"&&w.suspicions===1,"interrogation info/card");
  }else if(type==="analysis"){
   let card=w.ownHand.room,r=W.useAbility(st,-1,"room",card);
   fail(r.ok&&w.ability.left===before-1&&st.phase==="review"&&r.message.includes("YES"),"analysis");
  }else if(type==="return"){
   fail(!W.useAbility(st).ok,"return without movement");
   W.travel(st,1);st.round=4;
   fail(!W.useAbility(st).ok&&w.ability.left===before,"return ancient");
   st.round=2;
   fail(W.useAbility(st).ok&&st.room===0&&w.ability.left===before-1,"return recent");
  }else if(type==="detour"){
   let r=W.routeInfo(st,5,"detour");
   fail(r.ok&&r.cost===1&&r.hops===2,"detour cost");
   fail(W.travel(st,5,"detour").ok&&w.moveLeft===1&&w.ability.left===before-1,"detour travel");
  }else if(type==="gate"){
   w.special={type:"broken_door",round:6,room:7,activated:true,edge:"0-1",from:0,to:8};st.round=6;
   const r=W.routeInfo(st,1);
   fail(r.cost===1&&r.ok,"gate extra surcharge preserved");
   fail(W.travel(st,1).ok&&w.moveLeft===1&&w.ability.left===before-1,"gate charge");
  }
  records.push(type);
 }
 const smoke=mk();smoke.world.event={kind:"smoke",rooms:[0,3]};smoke.world.npcs[0].room=0;
 fail(!!W.canInfo(smoke),"smoke prevents info");
 fail(!W.interaction(smoke,smoke.world.npcs[0].id,"suspect",0,0).ok&&smoke.spent===0,"smoke suspicion");
 const coop=mk();coop.world.npcs[0].room=0;coop.world.npcs[0].trait="helpful";
 const c=W.interaction(coop,coop.world.npcs[0].id,"cooperate",0);
 fail(c.ok&&coop.spent===1&&coop.world.cooperations===1,"coop spends");
 fail(!W.routeInfo(coop,1).ok,"cooperation post-turn move");
 const forensic=mk(2,7),wr=forensic.world;let f=W.checkForensic(forensic);
 fail(f.ok&&wr.forensic.read&&wr.clueRead.length===2&&forensic.spent===1,"forensics");
 fail(wr.clueRead.every(x=>x.id!==forensic.answer[x.kind]),"forensic answer leak");
 const tokens={special:{"blackout":0,"broken_door":0,"secret_passage":0},playerKeys:0,npcKeys:0,boxMissing:0};
 for(let i=0;i<2500;i++){
  const st=mk(i),w=st.world;
  w.key.appears=2+(i%4);
  tokens.special[w.special.type]++;
  let extraSeen=0;
  for(let round=1;round<=12;round++){
   if(round>1){st.round=round;st.phase="investigate";const events=W.nextRound(st);extraSeen+=events.filter(e=>e.startsWith("확정 특별이벤트:")).length;}
   // A non-cheating player can navigate toward the displayed key or box.
   let to=null;
   if(!w.key.taken&&round>=w.key.appears)to=w.key.room;
   else if(w.key.owner==="player"&&w.box.appears!==null&&round>=w.box.appears&&!w.box.opened)to=w.box.room;
   if(to!==null&&to!==st.room){
    const info=W.routeInfo(st,to);
    if(info.ok)W.travel(st,to);
    else if(info.path&&info.path.length>1){
     const partial=W.routeInfo(st,info.path[1]);
     if(partial.ok)W.travel(st,info.path[1]);
    }
   }
   fail(w.moveLeft>=0&&w.ability.left>=0&&w.suspicions<=3&&w.cooperations<=1,"counter underrun");
   if(st.round>=12)fail(w.event===null,"R12 sabotage");
  }
  fail(extraSeen===1,"special event count");
  fail(w.key.taken,"key never found");
  fail(w.box.appears!==null&&w.box.appears>=4&&w.box.appears<=8,"box never spawned");
  fail(w.box.opened,"box never opened by someone");
  if(w.key.owner==="player")tokens.playerKeys++;else tokens.npcKeys++;
 }
 fail(tokens.playerKeys>250&&tokens.npcKeys>250,"key acquisition too lopsided");
 const st=mk();st.world.npcs[0].room=0;const old=JSON.parse(JSON.stringify(st));old.world.prevRoomRound=undefined;old.world.dashUsedRound=undefined;W.migrate(old);
 fail(old.world.prevRoomRound===-1&&old.world.dashUsedRound===0,"legacy defaults");
 let domChecks={};
 const ev={},save={"blackrose.web.solo.v2":JSON.stringify(mk())};let html="";
 const app={get innerHTML(){return html},set innerHTML(x){html=x},addEventListener(k,fn){ev[k]=fn}};
 const win={scrollTo(){},speechSynthesis:{cancel(){},getVoices:()=>[],speak(){}},SpeechSynthesisUtterance:class{},setTimeout() {return 1},clearTimeout(){}};
 new Function("document","localStorage","window","confirm","BRWorld",gameSource)({getElementById:()=>app},{getItem:k=>save[k]||null,setItem:(k,v)=>save[k]=v},win,()=>true,W);
 const click=a=>ev.click({target:{closest:q=>q==="[data-act]"?{dataset:{act:a}}:null}});
 let x=JSON.parse(save["blackrose.web.solo.v2"]);x.world.ability={type:"trace",left:3};
 save["blackrose.web.solo.v2"]=JSON.stringify(x); // Fresh app needed? source state already loaded.
 domChecks.staticUI=["world-ability-head","mansion-graph"].every(t=>html.includes(t));
 fail(domChecks.staticUI,"UI not rendering");
 return {checks:records.length+2500+6,abilities:records,events:tokens,domChecks};
}
const dir=path.resolve(__dirname,'..');
const result=balanceSuite(fs.readFileSync(path.join(dir,'world.js'),'utf8'),fs.readFileSync(path.join(dir,'game.js'),'utf8'));
console.log('Mobile balance suite PASSED',JSON.stringify(result));
