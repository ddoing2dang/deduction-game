import random, argparse, statistics
from collections import Counter
from inference_engine import *
MAPS={'mansion':{0:{1,2,3},1:{0,2,4},2:{0,1,3,4,5},3:{0,2,5},4:{1,2,5,6},5:{2,3,4,8},6:{4,7},7:{6,8},8:{7,5}}}
LOBBY=0
ABILITIES=('gatekeeper','sprint','jump','tracking','interrogation','analysis','resuspicion','return','stockpile','bypass')
# 12 fixed profile fields. Every value is shared by exactly two of the six characters.
# R3/R6 use different fields but the same pairing, preventing an early singleton.
# R9 changes the pairing and can logically isolate the culprit; R12 is confirmation.
PROFILE_PAIRINGS=[
 ((0,1),(2,3),(4,5)), ((0,1),(2,3),(4,5)), ((0,1),(2,3),(4,5)),
 ((0,1),(2,3),(4,5)), ((0,1),(2,3),(4,5)), ((0,1),(2,3),(4,5)),
 ((0,2),(1,4),(3,5)), ((0,2),(1,4),(3,5)), ((0,2),(1,4),(3,5)),
 ((0,3),(1,5),(2,4)), ((0,3),(1,5),(2,4)), ((0,3),(1,5),(2,4))]
CLUE_FIELDS={3:(0,1,2),6:(3,4,5),9:(6,7,8),12:(9,10,11)}
def witness_candidates(culprit, revealed_fields):
 candidates=set(range(6))
 for field in revealed_fields:
  pair=next(p for p in PROFILE_PAIRINGS[field] if culprit in p)
  candidates &= set(pair)
 return candidates
def deal(r):
 c=r.randrange(6); al=r.randrange(9); aw=r.randrange(9); ls=[i for i in range(9) if i!=al]; ws=[i for i in range(9) if i!=aw]; r.shuffle(ls);r.shuffle(ws)
 return c,al,aw,[(ls[i],ws[i]) for i in range(6)],set(ls[6:]),set(ws[6:])
def move(pos,pts,g,r):
 while pts and r.random()<.82: pos=r.choice(tuple(g[pos]));pts-=1
 return pos
def choose_weapon(state,target):
 ranked=[]
 for w in range(9):
  d=state.domains[Card('W',w)]
  if target not in d: continue
  score=(10 if ANSWER in d else 0)+len(d)
  ranked.append((score,w))
 return max(ranked)[1] if ranked else 0
def suspicion(r,hands,states,a,t,loc,weapon,metrics,interrogate=False):
 matches=[]
 if hands[t][0]==loc: matches.append(('L',loc))
 if hands[t][1]==weapon: matches.append(('W',weapon))
 if matches:
  k,i=r.choice(matches); states[a].seen_player_card(k,i,t); metrics['suspicion_hits']+=1
 else:
  states[a].exclude(Card('L',loc),t); states[a].exclude(Card('W',weapon),t); metrics['suspicion_misses']+=1
 if interrogate:
  both=(hands[t][0]==loc and hands[t][1]==weapon); metrics['interrogation_yes' if both else 'interrogation_no']+=1
  if both: states[a].seen_player_card('L',loc,t); states[a].seen_player_card('W',weapon,t)
def run(seed):
 r=random.Random(seed); culprit,al,aw,hands,ul,uw=deal(r); states=[KnowledgeState.fresh(i) for i in range(6)]
 for i,(l,w) in enumerate(hands): states[i].own(l,w)
 pos=[r.randrange(6) for _ in range(6)]; active=[True]*6; ded=[0]*6; forensic=[False]*6
 abilities=[r.choice(ABILITIES) for _ in range(6)]; used=[False]*6
 fl=r.choice(tuple(ul)); fw=r.choice(tuple(uw)); chest_owner=r.randrange(6); chest_round=r.randint(4,9)
 metrics=Counter(); coop_reject=0; revealed_fields=[]; culprit_candidates=set(range(6))
 for rnd in range(1,13):
  if rnd in CLUE_FIELDS:
   revealed_fields.extend(CLUE_FIELDS[rnd]); culprit_candidates=witness_candidates(culprit,revealed_fields)
   metrics['witness_clues']+=1; metrics['culprit_candidates_r'+str(rnd)]+=len(culprit_candidates)
  if rnd==chest_round: states[chest_owner].seen_undealt(r.choice(tuple(ul)),r.choice(tuple(uw))); metrics['chest_open']+=1
  for a in range(6):
   if not active[a]: continue
   pos[a]=move(pos[a],3 if rnd==1 else 2,MAPS['mansion'],r); action_done=False; no_immediate_deduction=False
   if rnd>=7 and not forensic[a] and pos[a]==LOBBY and r.random()<.62:
    states[a].seen_undealt(fl,fw); forensic[a]=True; metrics['forensic_reads']+=1; metrics['forensic_turns_spent']+=1; action_done=True
   if not action_done and abilities[a]=='analysis' and not used[a] and r.random()<.18:
    kind=r.choice(('L','W')); idx=r.randrange(9); held=any((h[0] if kind=='L' else h[1])==idx for h in hands)
    metrics['analysis_yes' if held else 'analysis_no']+=1; metrics['analysis_uses']+=1; used[a]=True; action_done=True
    if not held:
     for p in range(6): states[a].exclude(Card(kind,idx),p)
   if not action_done:
    mates=[p for p in range(6) if p!=a and active[p] and pos[p]==pos[a]]
    if mates:
     t=r.choice(mates)
     if r.random()<.38:
      accept=r.random()<(.42 if t==culprit else .68)
      if accept:
       ca=r.choice(('L','W')); cb=r.choice(('L','W')); ia=hands[a][0 if ca=='L' else 1]; ib=hands[t][0 if cb=='L' else 1]
       states[t].seen_player_card(ca,ia,a); states[a].seen_player_card(cb,ib,t); metrics['cooperations']+=1; action_done=True
      else: coop_reject+=1
     if not action_done:
      weapon=choose_weapon(states[a],t); do_int=(abilities[a]=='interrogation' and not used[a])
      suspicion(r,hands,states,a,t,pos[a],weapon,metrics,do_int)
      if do_int: used[a]=True; metrics['interrogation_uses']+=1
      if abilities[a]=='resuspicion' and rnd>=7 and not used[a]:
       w2=choose_weapon(states[a],t);\n       if w2==weapon: w2=(weapon+1)%9\n       suspicion(r,hands,states,a,t,pos[a],w2,metrics,False); used[a]=True; metrics['resuspicion_uses']+=1; no_immediate_deduction=True
      action_done=True
   if a==culprit: continue
   ls,ws=states[a].candidates(); confident=len(ls)==1 and len(ws)==1
   if rnd>=6 and ded[a]<2 and not no_immediate_deduction and (rnd==12 or confident):
    ded[a]+=1; lg=ls[0] if len(ls)==1 else r.choice(ls); wg=ws[0] if len(ws)==1 else r.choice(ws); cg=r.choice(tuple(culprit_candidates))
    if (cg,lg,wg)==(culprit,al,aw): return 'investigators',rnd,metrics,forensic,coop_reject,abilities
    if ded[a]>=2: active[a]=False
 return 'culprit',12,metrics,forensic,coop_reject,abilities
def sim(n,seed=20261021):
 wins=Counter(); rounds=[]; m=Counter(); fr=[]; cr=0; ability_games=Counter()
 for i in range(n):
  w,rd,x,f,b,abs_=run(seed+i); wins[w]+=1; rounds.append(rd); m.update(x); fr.append(sum(f)); cr+=b; ability_games.update(abs_)
 return {'games':n,'investigator_wins':wins['investigators'],'culprit_wins':wins['culprit'],'investigator_rate':wins['investigators']/n,'avg_end_round':round(statistics.mean(rounds),3),'avg_forensic_readers':round(statistics.mean(fr),3),'forensic_reads':m['forensic_reads'],'cooperations':m['cooperations'],'coop_rejects':cr,'suspicion_hits':m['suspicion_hits'],'suspicion_misses':m['suspicion_misses'],'analysis_uses':m['analysis_uses'],'analysis_yes':m['analysis_yes'],'analysis_no':m['analysis_no'],'interrogation_uses':m['interrogation_uses'],'interrogation_yes':m['interrogation_yes'],'interrogation_no':m['interrogation_no'],'resuspicion_uses':m['resuspicion_uses'],'ability_assignments':dict(ability_games),'witness_clues':m['witness_clues'],'avg_culprit_candidates_R3':m['culprit_candidates_r3']/n,'avg_culprit_candidates_R6':m['culprit_candidates_r6']/n,'avg_culprit_candidates_R9':m['culprit_candidates_r9']/n,'avg_culprit_candidates_R12':m['culprit_candidates_r12']/n}
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('-n',type=int,default=1000);a=ap.parse_args();print(sim(a.n))
