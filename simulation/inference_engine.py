from __future__ import annotations
from dataclasses import dataclass, field
from typing import Dict, List, Set, Tuple, Optional

PLAYERS=tuple(range(6)); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))

@dataclass(frozen=True)
class Card:
    kind:str
    idx:int

@dataclass
class Investigation:
    investigator:int
    location:int
    weapon:int
    order_checked:List[int]
    provider:Optional[int]
    shown:Optional[Card]=None
    observed_kind:Optional[str]=None
    interrogation_both:Optional[bool]=None

@dataclass
class KnowledgeState:
    viewer:int
    domains:Dict[Card,Set[int]]=field(default_factory=dict)
    clauses:List[Tuple[Tuple[Card,int],...]]=field(default_factory=list)

    @classmethod
    def fresh(cls,viewer:int):
        return cls(viewer,{Card(k,i):set(OWNERS) for k in ('L','W') for i in range(9)},[])

    def know_owner(self,c,o): self.domains[c]={o}
    def exclude_owner(self,c,o): self.domains[c].discard(o)
    def add_or(self,*lits): self.clauses.append(tuple(lits))
    def add_own_hand(self,l,w):
        self.know_owner(Card('L',l),self.viewer); self.know_owner(Card('W',w),self.viewer)
    def add_chest(self,l,w):
        self.know_owner(Card('L',l),UNDEALT); self.know_owner(Card('W',w),UNDEALT)

    def observe_investigation(self,inv:Investigation):
        L,W=Card('L',inv.location),Card('W',inv.weapon)
        if inv.provider is None:
            for p in inv.order_checked:
                self.exclude_owner(L,p); self.exclude_owner(W,p)
            return
        for p in inv.order_checked:
            if p==inv.provider: break
            self.exclude_owner(L,p); self.exclude_owner(W,p)
        self.add_or((L,inv.provider),(W,inv.provider))
        if self.viewer==inv.investigator and inv.shown is not None:
            self.know_owner(inv.shown,inv.provider)
        if inv.observed_kind in ('L','W'):
            self.know_owner(L if inv.observed_kind=='L' else W,inv.provider)
        if inv.interrogation_both is True:
            self.know_owner(L,inv.provider); self.know_owner(W,inv.provider)
        elif inv.interrogation_both is False:
            self.clauses.append(((L,-inv.provider-1),(W,-inv.provider-1)))

class InferenceEngine:
    """Exact constraint solver for one player's legitimate knowledge.
    Each category has capacities: six players x1, answer x1, undealt x2.
    Positive literal=(card==owner); negative literal uses owner=-player-1.
    """
    def __init__(self,state): self.s=state

    def _lit_status(self,lit,assign):
        c,o=lit; neg=o<0; owner=(-o-1) if neg else o
        if c in assign:
            v=(assign[c]!=owner) if neg else (assign[c]==owner)
            return 1 if v else -1
        d=self.s.domains[c]
        if neg:
            if owner not in d:return 1
            if d=={owner}:return -1
        else:
            if owner not in d:return -1
            if d=={owner}:return 1
        return 0

    def _clauses_possible(self,assign):
        return all(not all(self._lit_status(x,assign)==-1 for x in clause) for clause in self.s.clauses)

    def feasible(self,forced=None):
        forced=forced or {}
        cards=[Card(k,i) for k in ('L','W') for i in range(9)]
        caps={(k,o):(2 if o==UNDEALT else 1) for k in ('L','W') for o in OWNERS}
        assign={}; used={x:0 for x in caps}
        for c in cards:
            d=set(self.s.domains[c])
            if c in forced:d&={forced[c]}
            if not d:return False
            if len(d)==1:
                o=next(iter(d)); assign[c]=o; used[(c.kind,o)]+=1
                if used[(c.kind,o)]>caps[(c.kind,o)]:return False
        un=[c for c in cards if c not in assign]
        un.sort(key=lambda c:len(self.s.domains[c]&({forced[c]} if c in forced else OWNERS)))
        def bt(i):
            if not self._clauses_possible(assign):return False
            if i==len(un):return all(used[x]==caps[x] for x in caps)
            c=un[i]; d=self.s.domains[c]&({forced[c]} if c in forced else OWNERS)
            for o in d:
                key=(c.kind,o)
                if used[key]<caps[key]:
                    assign[c]=o; used[key]+=1
                    if bt(i+1):return True
                    used[key]-=1; del assign[c]
            return False
        return bt(0)

    def possible_owners(self,c):
        return {o for o in self.s.domains[c] if self.feasible({c:o})}

    def answer_candidates(self):
        return ([i for i in range(9) if self.feasible({Card('L',i):ANSWER})],
                [i for i in range(9) if self.feasible({Card('W',i):ANSWER})])

    def infer_fixed(self):
        out={}
        for c in self.s.domains:
            p=self.possible_owners(c)
            if len(p)==1:out[c]=next(iter(p))
        return out

if __name__=='__main__':
    s=KnowledgeState.fresh(0); s.add_own_hand(0,0)
    s.observe_investigation(Investigation(0,1,1,[1,2,3,4,5],2,shown=Card('W',1)))
    e=InferenceEngine(s)
    assert e.possible_owners(Card('W',1))=={2}
    assert 1 not in e.possible_owners(Card('L',1))
    assert 1 not in e.possible_owners(Card('W',1))
    s.add_chest(2,2); e=InferenceEngine(s)
    assert e.possible_owners(Card('L',2))=={UNDEALT}
    assert e.possible_owners(Card('W',2))=={UNDEALT}
    print('PASS')
