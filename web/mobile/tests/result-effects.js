/* Result effects and automatic next-action scroll regression.
 * Run: node web/mobile/tests/result-effects.js
 * Pure JS DOM/time mocks: never touch the user's browser save.
 */
'use strict';
const fs=require('node:fs'),path=require('node:path');
const root=path.join(__dirname,'..');
const source=fs.readFileSync(path.join(root,'game.js'),'utf8');
const BRWorld=new Function(fs.readFileSync(path.join(root,'world.js'),'utf8')+';return BRWorld;')();
const assert=(v,message)=>{if(!v)throw Error(message)};
function fresh(round=2){
 const s={started:true,ended:false,won:false,playerCharacter:0,answer:{person:2,room:3,item:4},phase:'investigate',round,room:0,spent:0,fail:0,lastAccuseRound:0,marked:{person:[],room:[],item:[]},history:[]};
 BRWorld.setup(s);s.world.npcs[0].room=0;s.world.npcs[0].trait='helpful';s.world.event=null;s.world.key.appears=8;return s;
}
function ui(state){
 let html='',handlers={},focus=[],data={'blackrose.web.solo.v2':JSON.stringify(state)},clock=0,next=0,timers=new Map(),spoken=[];
 const app={get innerHTML(){return html},set innerHTML(v){html=v},addEventListener(k,f){handlers[k]=f},querySelector(sel){return {scrollIntoView:()=>focus.push(sel)}}};
 const win={scrollTo(){},setTimeout(fn,ms){const k=++next;timers.set(k,{at:clock+ms,fn});return k},clearTimeout(k){timers.delete(k)},speechSynthesis:{cancel(){},getVoices(){return []},speak(u){spoken.push(u);u.onstart?.()}},SpeechSynthesisUtterance:class{constructor(t){this.text=t}}};
 const local={getItem:k=>data[k]||null,setItem:(k,v)=>data[k]=v};
 new Function('document','localStorage','window','confirm','BRWorld',source)({getElementById:()=>app},local,win,()=>true,BRWorld);
 function tick(ms){const end=clock+ms;let steps=0;for(;;){let id=null,item=null;for(const [k,v] of timers)if(v.at<=end&&(!item||v.at<item.at)){item=v;id=k}if(!item)break;timers.delete(id);clock=item.at;item.fn();if(++steps>200)throw Error('Timer cycle')}clock=end}
 return {html:()=>html,focus,spoken,tick,click:act=>handlers.click({target:{closest:q=>q==='[data-act]'?{dataset:{act}}:null}}),pick:(k,v)=>handlers.change({target:{closest:q=>q==='[data-pick]'?{dataset:{pick:k},value:String(v)}:null}}),state:()=>JSON.parse(data['blackrose.web.solo.v2'])};
}
let passed=0;
for(let match=0;match<=3;match++){
 const a=ui(fresh());a.click('open-probe');a.pick('person',match>=1?2:1);a.pick('room',match>=2?3:2);a.pick('item',match>=3?4:3);a.click('submit-probe');
 assert(a.html().includes('fx-count-'+match),'No match effect: '+match);
 assert(!a.html().includes('host-auto-progress')&&!a.html().includes('host-disclaimer'),'Narrator timing text leaked');
 assert(a.focus.length===0,'Focused before result closed');
 a.tick(3999);assert(a.focus.length===0,'Focused before 4-second result');
 a.tick(1);assert(a.focus.length===1&&a.focus[0].includes('next-round'),'No next-round focus');
 assert(a.state().round===2,'Round advanced without button tap');
 passed++;
}
for(const [person,fx,finished] of [[2,'fx-victory',true],[1,'fx-defeat',false]]){
 const a=ui(fresh(6));a.click('tab:accuse');a.pick('person',person);a.pick('room',3);a.pick('item',4);a.click('submit');
 assert(a.html().includes(fx),'Missing verdict FX '+fx);a.tick(4000);
 assert(a.state().ended===finished,'Wrong verdict state');assert(a.focus.length===0,'Verdict improperly scrolled to next turn');
 passed++;
}
{
 const a=ui(fresh(12));a.click('pass-turn');a.tick(3000);
 assert(a.focus.length===1&&a.focus[0].includes('open-accuse'),'R12 must scroll to final accusation');
 assert(a.state().round===12,'R12 incorrectly auto advanced');
 passed++;
}
{
 const a=ui(fresh());a.click('cooperate:'+a.state().world.npcs[0].id);a.tick(4000);
 assert(a.focus.length===1&&a.focus[0].includes('next-round'),'Cooperation must scroll to next round');
 passed++;
}
console.log('Result effects / focus regression PASSED:',passed,'cases');
