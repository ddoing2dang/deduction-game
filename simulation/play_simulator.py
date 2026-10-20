from __future__ import annotations
import argparse, random, statistics
from collections import Counter, defaultdict
from dataclasses import dataclass
from inference_engine import Card, Investigation, KnowledgeState, InferenceEngine, ANSWER, UNDEALT

NPLAYERS=6; NCARDS=9; ROUNDS=12

@dataclass
class World:
    culprit:int; ans_l:int; ans_w:int; hands:list; undealt_l:set; undealt_w:set

def deal(r):
    culprit=r.randrange(6); al=r.randrange(9); aw=r.randrange(9)
    ls=[x for x in range(9) if x!=al]; ws=[x for x in range(9) if x!=aw]
    r.shuffle(ls); r.shuffle(ws)
    hands=[(ls[i],ws[i]) for i in range(6)]
    return World(culprit,al,aw,hands,set(ls[6:]),set(ws[6:]))

def order_after(p):
    return [(p+i)%6 for i in range(1,6)]

def investigate(world, actor, loc, weap, states, r):
    checked=[]; provider=None; shown=None
    for p in order_after(actor):
        checked.append(p); matches=[]
        if world.hands[p][0]==loc: matches.append(Card('L',loc))
        if world.hands[p][1]==weap: matches.append(Card('W',weap))
        if matches:
            provider=p; shown=r.choice(matches); break
    public=Investigation(actor,loc,weap,checked,provider)
    for s in states: s.observe_investigation(public)
    if provider is not None:
        states[actor].know_owner(shown,provider)
    return provider

def candidates(state):
    return InferenceEngine(state).answer_candidates()

def choose_query(state,r):
    # Information-seeking approximation: prefer answer candidates, with occasional bluff/known-card probe.
    ls,ws=candidates(state)
    loc=r.choice(ls if ls else range(9)); weap=r.choice(ws if ws else range(9))
    if r.random()<.15: # human-like non-optimal/bluff probe
        weap=r.randrange(9)
    return loc,weap

def run_game(seed, chest=True):
    r=random.Random(seed); w=deal(r)
    states=[KnowledgeState.fresh(i) for i in range(6)]
    for i,(l,wp) in enumerate(w.hands): states[i].add_own_hand(l,wp)
    # Culprit legitimately knows answer, but this knowledge is never copied to investigator states.
    deduction_used=[0]*6; active=[True]*6
    chest_round=r.randint(4,9) if chest else None
    chest_owner=r.randrange(6) if chest else None
    for rnd in range(1,ROUNDS+1):
        if chest and rnd==chest_round:
            states[chest_owner].add_chest(r.choice(tuple(w.undealt_l)),r.choice(tuple(w.undealt_w)))
        for actor in range(6):
            if not active[actor]: continue
            # Culprit participates publicly but never performs a real winning deduction.
            loc,weap=choose_query(states[actor],r)
            investigate(w,actor,loc,weap,states,r)
            if actor==w.culprit: continue
            ls,ws=candidates(states[actor])
            must=(rnd==12)
            confident=(len(ls)==1 and len(ws)==1)
            # Character identity is not derivable from card ownership. Model suspicion separately:
            # absent behavioral evidence, investigator has all six character candidates.
            char_guess=r.randrange(6)
            if rnd>=6 and deduction_used[actor]<2 and (must or confident):
                deduction_used[actor]+=1
                lguess=ls[0] if len(ls)==1 else r.choice(ls)
                wguess=ws[0] if len(ws)==1 else r.choice(ws)
                if char_guess==w.culprit and lguess==w.ans_l and wguess==w.ans_w:
                    return {'winner':'investigators','round':rnd,'ability':'baseline'}
                if deduction_used[actor]>=2: active[actor]=False
    return {'winner':'culprit','round':12,'ability':'baseline'}

def simulate(n,seed):
    wins=Counter(); rounds=[]; 
    for i in range(n):
        x=run_game(seed+i)
        wins[x['winner']]+=1; rounds.append(x['round'])
    return {'games':n,'investigator_wins':wins['investigators'],'culprit_wins':wins['culprit'],
            'investigator_rate':wins['investigators']/n,'culprit_rate':wins['culprit']/n,
            'avg_end_round':statistics.mean(rounds),'rounds':dict(Counter(rounds))}

if __name__=='__main__':
    ap=argparse.ArgumentParser(); ap.add_argument('-n','--games',type=int,default=1000); ap.add_argument('--seed',type=int,default=20261021)
    a=ap.parse_args(); print(simulate(a.games,a.seed))
