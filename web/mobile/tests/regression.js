/* Mobile solo deduction game regression smoke tests.
 * Run from repository root: node web/mobile/tests/regression.js
 * No browser, dependencies, or changes to real player saves required.
 */
'use strict';
const fs=require('node:fs');
const path=require('node:path');
function regressionCore(gameSource,worldSource){
 const W=new Function(worldSource+";return BRWorld;")();
 const check=(ok,message)=>{if(!ok)throw Error(message)};
 let assertions=0;
 function seed(player=0,round=1){
  const s={started:true,ended:false,won:false,playerCharacter:player,answer:{person:(player+1)%6,room:3,item:4},round,room:0,phase:"investigate",spent:0,fail:0,lastAccuseRound:0,marked:{person:[],room:[],item:[]},history:[],lastInvestigation:null};
  W.setup(s);s.world.npcs[0].room=0;s.world.npcs[0].trait="helpful";s.world.event=null;s.world.special.activated=false;s.world.ability={type:"dash",left:3};s.world.key.appears=8;return s;
 }
 function open(initial,{mode="normal",world=W}={}){
  let clock=0,next=0,html="",callbacks={},spoken=[],timers=new Map();
  let storage=initial?{"blackrose.web.solo.v2":JSON.stringify(initial)}:{};
  const synth={cancel(){if(mode==="cancel-error"&&spoken.length)spoken.at(-1).onerror?.()},getVoices(){return [{lang:"ko-KR"}]},speak(v){spoken.push(v);if(mode==="error")v.onerror?.();if(mode==="throw")throw Error("autoplay prohibited");if(mode==="normal")v.onstart?.()}};
  const win={scrollTo(){},setTimeout(fn,ms){const id=++next;timers.set(id,{at:clock+ms,fn});return id},clearTimeout(id){timers.delete(id)},speechSynthesis:mode==="none"?null:synth,SpeechSynthesisUtterance:mode==="none"?null:class{constructor(t){this.text=t}}};
  const app={get innerHTML(){return html},set innerHTML(s){html=s},addEventListener(k,fn){callbacks[k]=fn}};
  const localStorage={getItem:k=>storage[k]||null,setItem:(k,v)=>{storage[k]=v}};
  new Function("document","localStorage","window","confirm","BRWorld",gameSource)({getElementById:()=>app},localStorage,win,()=>true,world);
  function tick(ms){
   const limit=clock+ms;let count=0;
   for(;;){
    let chosen=null,id=null;
    for(const [k,t] of timers)if(t.at<=limit&&(!chosen||t.at<chosen.at)){chosen=t;id=k}
    if(!chosen)break;
    timers.delete(id);clock=chosen.at;chosen.fn();
    if(++count>200)throw Error("runaway narrator timer");
   }
   clock=limit;
  }
  const update=(type,kind,name,value)=>callbacks[type]({target:{closest:q=>q==="[data-"+kind+"]"?{dataset:{[kind.replace(/-([a-z])/g,(_,c)=>c.toUpperCase())]:name},value:String(value)}:null}});
  return {html:()=>html,state:()=>JSON.parse(storage["blackrose.web.solo.v2"]||"null"),spoken,tick,click:a=>callbacks.click({target:{closest:q=>q==="[data-act]"?{dataset:{act:a}}:null}}),pick:(k,v)=>update("change","pick",k,v),susp:(k,v)=>update("input","suspicion-pick",k,v),finish(){spoken.at(-1)?.onend?.();tick(1000)}};
 }
 for(let player=0;player<6;player++){
  const s=seed(player),npc=s.world.npcs[0],a=open(s);
  a.click("suspect:"+npc.id);
  check(a.html().includes("인물 의심 · 장소 + 흉기")&&!a.html().includes("host-backdrop"),"suspicion screen blocked");
  a.susp("room",npc.hand.room);a.susp("item",npc.hand.item);a.click("submit-suspicion");
  check(a.state().phase==="review"&&a.state().world.suspicions===1&&a.spoken.length===1,"suspicion failed");
  a.tick(4000);
  check(!a.html().includes("host-backdrop")&&a.html().includes('data-act="next-round"'),"result screen not dismissed");
  a.click("next-round");a.tick(3000);
  check(a.state().round===2&&!a.html().includes("host-backdrop"),"next round stuck");
  assertions++;
 }
 for(const mode of ["normal","none","no-events","throw","error","cancel-error"]){
  const s=seed(),a=open(s,{mode});
  a.click("cooperate:"+s.world.npcs[0].id);check(a.state().phase==="review","cooperation failed");
  a.tick(4000);check(!a.html().includes("host-backdrop"),"TTS deadlock: "+mode);
  assertions++;
 }
 {
  const a=open(seed());a.click("open-probe");
  check(a.html().includes("가설 직접 조사")&&!a.html().includes("host-backdrop"),"investigation input blocked");
  a.pick("person",1);a.pick("room",3);a.pick("item",4);a.click("submit-probe");
  check(a.state().spent===1&&a.spoken.length===1,"investigation result missing");
  a.tick(4000);check(!a.html().includes("host-backdrop"),"investigation result stuck");
  assertions++;
 }
 {
  const s=seed();s.world.event={kind:"smoke",rooms:[0,2]};
  const a=open(s);a.click("suspect:"+s.world.npcs[0].id);
  check(!a.html().includes("인물 의심 · 장소 + 흉기")&&a.state().world.suspicions===0,"smoke enabled suspicion");
  a.click("cooperate:"+s.world.npcs[0].id);
  check(a.state().world.cooperations===0,"smoke charged cooperation");
  a.click("pass-turn");a.tick(3000);a.click("next-round");a.tick(3000);
  check(a.state().round===2,"smoke blocked progress");
  assertions++;
 }
 {
  const a=open(seed()),npc=a.state().world.npcs[0];
  a.click("suspect:"+npc.id);a.click("submit-suspicion");
  check(a.state().spent===0,"empty suspicion consumed attempt");
  a.click("cancel-suspicion");a.click("room:1");a.click("cancel-route");
  check(a.state().room===0&&a.state().world.moveLeft===2,"travel preview consumed points");
  assertions++;
 }
 for(let player=0;player<6;player++){
  const a=open(seed(player));
  for(let round=1;round<=11;round++){
   a.click("pass-turn");a.tick(3000);
   check(a.state().phase==="review","round pass failed");
   a.click("next-round");a.tick(3000);
   check(a.state().round===round+1&&!a.html().includes("host-backdrop"),"round "+round+" stuck");
  }
  assertions++;
 }
 for(let from=0;from<9;from++)for(let to=0;to<9;to++){
  if(from===to)continue;
  const s=seed();s.room=from;const p=W.routeInfo(s,to),copy=JSON.parse(JSON.stringify(s)),out=W.travel(copy,to);
  if(p.ok)check(out.ok&&copy.room===to&&copy.world.moveLeft===s.world.moveLeft-p.cost,"route discrepancy");
  else check(!out.ok&&copy.room===from,"illegal teleport");
  assertions++;
 }
 return {passed:true,cases:assertions,details:"6 character suspicion, 6 TTS modes, direct investigation, sabotage, cancellation, 6×12 rounds, 72 room routes"};
}
const root=path.resolve(__dirname,'..');
const result=regressionCore(fs.readFileSync(path.join(root,'game.js'),'utf8'),fs.readFileSync(path.join(root,'world.js'),'utf8'));
console.log('Mobile deduction smoke test PASSED:',result.cases,'checks');
