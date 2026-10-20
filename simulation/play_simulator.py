import random, argparse, statistics
from collections import Counter
from inference_engine import *
MAPS={'mansion':{0:{1,2,3},1:{0,2,4},2:{0,1,3,4,5},3:{0,2,5},4:{1,2,5,6},5:{2,3,4,8},6:{4,7},7:{6,8},8:{7,5}}}
LOBBY=0
def deal(r):
 c=r.randrange(6); al=r.randrange(9); aw=r.randrange(9); ls=[i for i in range(9) if i!=al]; ws=[i for i in range(9) if i!=aw]; r.shuffle(ls);r.shuffle(ws)
 return c,al,aw,[(ls[i],ws[i]) for i in range(6)],set(ls[6:]),set(ws[6:])
def move(pos,pts,g,r):
 while pts and r.random()<.82: pos=r.choice(tuple(g[pos]));pts-=1
 return pos
def run(seed):
 r=random.Random(seed); culprit,al,aw,hands,ul,uw=deal(r); states=[KnowledgeState.fresh(i) for i in range(6)]
 for i,(l,w) in enumerate(hands): states[i].own(l,w)
 pos=[r.randrange(6) for _ in range(6)]; active=[True]*6; ded=[0]*6; forensic=[False]*6; abilities=[r.choice(('gatekeeper','sprint','jump','tracking','interrogation','analysis','resuspicion','return','stockpile','bypass')) for _ in range(6)]; used=[False]*6
 fl=r.choice(tuple(ul)); fw=r.choice(tuple(uw)); chest_owner=r.randrange(6); chest_round=r.randint(4,9)
 metrics=Counter(); coop_accept=coop_reject=0
 for rnd in range(1,13):
  if rnd==chest_round: states[chest_owner].seen_undealt(r.choice(tuple(ul)),r.choice(tuple(uw))); metrics['chest_open']+=1
  for a in range(6):
   if not active[a]: continue
   pos[a]=move(pos[a],3 if rnd==1 else 2,MAPS['mansion'],r); action_done=False
   if rnd>=7 and not forensic[a] and pos[a]==LOBBY and r.random()<.62:
    states[a].seen_undealt(fl,fw); forensic[a]=True; metrics['forensic_reads']+=1;metrics['forensic_turns_spent']+=1;action_done=True
   if not action_done:
    mates=[p for p in range(6) if p!=a and active[p] and pos[p]==pos[a]]
    if mates:
     t=r.choice(mates)
     if r.random()<.38:
      accept=r.random()<(.42 if t==culprit else .68)
      if accept:
       coop_accept+=1; ca=r.choice(('L','W')); cb=r.choice(('L','W')); ia=hands[a][0 if ca=='L' else 1]; ib=hands[t][0 if cb=='L' else 1]
       states[t].seen_player_card(ca,ia,a); states[a].seen_player_card(cb,ib,t); metrics['cooperations']+=1;action_done=True
      else: coop_reject+=1
     if not action_done:
      weapon=r.randrange(9); matches=[]
      if hands[t][0]==pos[a]: matches.append(('L',pos[a]))
      if hands[t][1]==weapon: matches.append(('W',weapon))
      if matches:
       k,i=r.choice(matches); states[a].seen_player_card(k,i,t); metrics['suspicion_hits']+=1
      else:
       metrics['suspicion_misses']+=1; states[a].exclude(Card('L',pos[a]),t); states[a].exclude(Card('W',weapon),t)
      action_done=True
   if a==culprit: continue
   ls,ws=states[a].candidates(); confident=len(ls)==1 and len(ws)==1
   if rnd>=6 and ded[a]<2 and (rnd==12 or confident):
    ded[a]+=1; lg=ls[0] if len(ls)==1 else r.choice(ls); wg=ws[0] if len(ws)==1 else r.choice(ws); cg=r.randrange(6)
    if (cg,lg,wg)==(culprit,al,aw): return 'investigators',rnd,metrics,forensic,coop_accept,coop_reject
    if ded[a]>=2: active[a]=False
 return 'culprit',12,metrics,forensic,coop_accept,coop_reject
def sim(n,seed=20261021):
 wins=Counter(); rounds=[]; m=Counter(); fr=[];ca=cr=0
 for i in range(n):
  w,rd,x,f,a,b=run(seed+i);wins[w]+=1;rounds.append(rd);m.update(x);fr.append(sum(f));ca+=a;cr+=b
 return {'games':n,'investigator_wins':wins['investigators'],'culprit_wins':wins['culprit'],'investigator_rate':wins['investigators']/n,'avg_end_round':round(statistics.mean(rounds),3),'avg_forensic_readers':round(statistics.mean(fr),3),'forensic_reads':m['forensic_reads'],'forensic_turns_spent':m['forensic_turns_spent'],'cooperations':m['cooperations'],'coop_rejects':cr,'suspicion_hits':m['suspicion_hits'],'suspicion_misses':m['suspicion_misses'],'analysis_uses':m['analysis_uses'],'interrogation_yes':m['interrogation_yes'],'interrogation_no':m['interrogation_no'],'resuspicion_hits':m['resuspicion_hits'],'resuspicion_misses':m['resuspicion_misses']}
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('-n',type=int,default=1000);a=ap.parse_args();print(sim(a.n))
