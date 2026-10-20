from __future__ import annotations
import argparse, random, statistics
from collections import Counter, defaultdict
from dataclasses import dataclass
from inference_engine import Card, Investigation, KnowledgeState, InferenceEngine, ANSWER, UNDEALT

NPLAYERS=6; NCARDS=9; ROUNDS=12
INV_ABILITIES=('gatekeeper','sprint','jump','tracking','interrogation','observation','reinvestigation','return','stockpile','bypass')
CULPRIT_ABILITIES=('lock','disrupt','route_block','smoke')
# Provisional simulation-only graphs. They preserve the fixed 7-8-9 lower chain and two distinct upper links.
MAPS={
 'mansion':{0:{1,2,3},1:{0,2,4},2:{0,1,3,4,5},3:{0,2,5},4:{1,2,5,6},5:{2,3,4,8},6:{4,7},7:{6,8},8:{7,5}},
 'cruise':{0:{1,3},1:{0,2,4},2:{1,5},3:{0,4,6},4:{1,3,5},5:{2,4,8},6:{3,7},7:{6,8},8:{7,5}},
 'museum':{0:{1,5,3},1:{0,2,4},2:{1,3,5},3:{0,2,4},4:{1,3,5,6},5:{0,2,4,8},6:{4,7},7:{6,8},8:{7,5}}
}

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
    return provider, shown

def candidates(state):
    # Fast simulation path: safe answer candidates from direct domain exclusions.
    # The exact solver remains authoritative for validation, but this avoids
    # exponential backtracking on every AI query during bulk Monte Carlo runs.
    ls=[i for i in range(9) if ANSWER in state.domains[Card('L',i)]]
    ws=[i for i in range(9) if ANSWER in state.domains[Card('W',i)]]
    return (ls or list(range(9)), ws or list(range(9)))

def choose_query(state,r):
    # Information-seeking approximation: prefer answer candidates, with occasional bluff/known-card probe.
    ls,ws=candidates(state)
    loc=r.choice(ls if ls else range(9)); weap=r.choice(ws if ws else range(9))
    if r.random()<.15: # human-like non-optimal/bluff probe
        weap=r.randrange(9)
    return loc,weap

def _move(pos, points, graph, r, route_block_edge=None, surcharge=None, free_door=False, bypass_available=False):
    surcharge=surcharge or set(); cur=pos; spent=0; block=route_block_edge; bypass_used=False; free_used=False
    while points>0:
        opts=[]
        for n in graph[cur]:
            edge=tuple(sorted((cur,n)))
            # Route Block consumes itself on the first attempted traversal and that attempt fails.
            if block is not None and edge==block:
                block=None
                continue
            extra=1 if edge in surcharge else 0
            use_bypass=bypass_available and extra and not bypass_used
            cost=1+extra-(1 if use_bypass else 0)
            use_free=free_door and not free_used
            if use_free: cost=max(0,cost-1)
            if cost<=points: opts.append((n,cost,use_bypass,use_free))
        if not opts: break
        n,cost,ub,uf=r.choice(opts); cur=n; points-=cost; spent+=cost
        bypass_used|=ub; free_used|=uf
        if r.random()<.30: break
    return cur,spent,block,bypass_used,free_used

def run_game(seed, chest=True):
    r=random.Random(seed); w=deal(r)
    states=[KnowledgeState.fresh(i) for i in range(6)]
    for i,(l,wp) in enumerate(w.hands): states[i].add_own_hand(l,wp)
    # Culprit legitimately knows answer, but this knowledge is never copied to investigator states.
    deduction_used=[0]*6; active=[True]*6
    map_name=r.choice(tuple(MAPS)); graph=MAPS[map_name]; pos=[r.randrange(6) for _ in range(6)]
    inv_ability=[r.choice(INV_ABILITIES) for _ in range(6)]
    culprit_ability=r.choice(CULPRIT_ABILITIES); uses=[0]*6
    limits={'gatekeeper':3,'sprint':2,'jump':1,'tracking':2,'interrogation':1,'observation':1,'reinvestigation':1,'return':3,'stockpile':3,'bypass':2}
    culprit_left={'lock':2,'disrupt':2,'route_block':1,'smoke':1}[culprit_ability]
    event=r.choice(('blackout','broken_door','secret_passage')); event_round=r.randint(6,11)
    culprit_rounds=set(r.sample(range(1,12),culprit_left))
    prev_start=pos[:]; carry=[0]*6; fake_deduction_done=False; route_block_edge=None; blackout_room=None
    chest_round=r.randint(4,9) if chest else None
    chest_owner=r.randrange(6) if chest else None
    for rnd in range(1,ROUNDS+1):
        if chest and rnd==chest_round:
            states[chest_owner].add_chest(r.choice(tuple(w.undealt_l)),r.choice(tuple(w.undealt_w)))
        blackout=set(); surcharge=set(); blocked=set(); secret=None; disrupted=None
        if blackout_room is not None and event_round <= rnd <= event_round+1: blackout.add(blackout_room)
        if rnd==event_round:
            if event=='blackout': blackout_room=r.randrange(9); blackout.add(blackout_room)
            elif event=='broken_door':
                edges=[tuple(sorted((a,b))) for a in graph for b in graph[a] if a<b]; surcharge.add(r.choice(edges))
            else:
                pairs=[(a,b) for a in range(9) for b in range(a+1,9) if b not in graph[a]]
                if pairs: secret=r.choice(pairs)
        if rnd in culprit_rounds:
            if culprit_ability=='lock':
                edges=[tuple(sorted((a,b))) for a in graph for b in graph[a] if a<b]; surcharge.add(r.choice(edges))
            elif culprit_ability=='disrupt': disrupted=r.choice([x for x in range(6) if x!=w.culprit])
            elif culprit_ability=='route_block':
                edges=[tuple(sorted((a,b))) for a in graph for b in graph[a] if a<b]; route_block_edge=r.choice(edges); blocked.add(route_block_edge)
            elif culprit_ability=='smoke':
                # Simulation-only assumption until exact Smoke effect is finalized: two rooms lose non-movement action this round.
                blackout.update(r.sample([x for x in range(9) if x not in blackout],2))
        round_graph={k:set(v) for k,v in MAPS[map_name].items()}
        if secret:
            a,b=secret; round_graph[a].add(b); round_graph[b].add(a)
        graph=round_graph
        if route_block_edge is not None: blocked.add(route_block_edge)
        for actor in range(6):
            if not active[actor]: continue
            # Movement + mobility abilities. Ability use is heuristic and simulation-only.
            ab=inv_ability[actor]; pts=(3 if rnd==1 else 2)+carry[actor]; carry[actor]=0; turn_start=pos[actor]; spent=0
            if actor==disrupted: pts=max(1,pts-1)
            if ab=='sprint' and uses[actor]<limits[ab] and r.random()<.35: pts+=1; uses[actor]+=1
            if ab=='jump' and uses[actor]<1 and r.random()<.12: pos[actor]=r.randrange(9); uses[actor]+=1
            elif ab=='return' and uses[actor]<limits[ab] and r.random()<.18: pos[actor]=prev_start[actor]; uses[actor]+=1
            else:
                free=(ab in ('gatekeeper','tracking') and uses[actor]<limits[ab] and r.random()<.25)
                by=(ab=='bypass' and uses[actor]<limits[ab])
                pos[actor],spent,route_block_edge,bypass_used,free_used=_move(pos[actor],pts,graph,r,route_block_edge,surcharge,free,by)
                if free_used: uses[actor]+=1
                if bypass_used: uses[actor]+=1
            if ab=='stockpile' and uses[actor]<limits[ab]:
                # Carry exactly one genuinely unused movement point.
                base_available=(3 if rnd==1 else 2)
                if actor==disrupted: base_available=max(1,base_available-1)
                unused=max(0,base_available-spent)
                if unused>=1: carry[actor]=1; uses[actor]+=1
            prev_start[actor]=turn_start
            action_blocked=pos[actor] in blackout
            if action_blocked and rnd!=12: continue
            loc=pos[actor]; _,weaps=choose_query(states[actor],r); weap=weaps
            provider,shown=investigate(w,actor,loc,weap,states,r)
            # Observation reacts to another player's successful investigation.
            if provider is not None and shown is not None:
                for observer in range(6):
                    if observer!=actor and active[observer] and inv_ability[observer]=='observation' and uses[observer]<1 and r.random()<.25:
                        states[observer].observe_investigation(Investigation(actor,loc,weap,order_after(actor),provider,observed_kind=shown.kind)); uses[observer]+=1
            if provider is not None and ab=='interrogation' and uses[actor]<1 and r.random()<.55:
                both=(w.hands[provider][0]==loc and w.hands[provider][1]==weap)
                states[actor].observe_investigation(Investigation(actor,loc,weap,order_after(actor),provider,interrogation_both=both)); uses[actor]+=1
            reinvestigated=False
            if ab=='reinvestigation' and rnd>=7 and uses[actor]<1 and r.random()<.30:
                _,w2=choose_query(states[actor],r); investigate(w,actor,pos[actor],w2,states,r); uses[actor]+=1; reinvestigated=True
            if actor==w.culprit:
                if rnd>=6 and not fake_deduction_done and (rnd==12 or r.random()<.12): fake_deduction_done=True
                continue
            ls,ws=candidates(states[actor])
            must=(rnd==12)
            confident=(len(ls)==1 and len(ws)==1)
            # Character identity is not derivable from card ownership. Model suspicion separately:
            # absent behavioral evidence, investigator has all six character candidates.
            char_guess=r.randrange(6)
            if rnd>=6 and deduction_used[actor]<2 and not reinvestigated and (must or confident):
                deduction_used[actor]+=1
                lguess=ls[0] if len(ls)==1 else r.choice(ls)
                wguess=ws[0] if len(ws)==1 else r.choice(ws)
                if char_guess==w.culprit and lguess==w.ans_l and wguess==w.ans_w:
                    return {'winner':'investigators','round':rnd,'ability':inv_ability[actor],'culprit_ability':culprit_ability,'map':map_name,'event':event}
                if deduction_used[actor]>=2: active[actor]=False
    return {'winner':'culprit','round':12,'ability':None,'culprit_ability':culprit_ability,'map':map_name,'event':event}

def simulate(n,seed):
    wins=Counter(); rounds=[]; dims=defaultdict(Counter)
    for i in range(n):
        x=run_game(seed+i)
        wins[x['winner']]+=1; rounds.append(x['round']); [dims[k].update([x[k]]) for k in ('map','event','culprit_ability')]
    return {'games':n,'investigator_wins':wins['investigators'],'culprit_wins':wins['culprit'],
            'investigator_rate':wins['investigators']/n,'culprit_rate':wins['culprit']/n,
            'avg_end_round':statistics.mean(rounds),'rounds':dict(Counter(rounds)),'dimensions':{k:dict(v) for k,v in dims.items()}}

if __name__=='__main__':
    ap=argparse.ArgumentParser(); ap.add_argument('-n','--games',type=int,default=1000); ap.add_argument('--seed',type=int,default=20261021)
    a=ap.parse_args(); print(simulate(a.games,a.seed))
