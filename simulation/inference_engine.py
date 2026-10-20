from dataclasses import dataclass, field

PLAYERS=range(6); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))
CAPS={0:1,1:1,2:1,3:1,4:1,5:1,ANSWER:1,UNDEALT:2}

@dataclass(frozen=True)
class Card:
    kind:str
    idx:int

@dataclass
class KnowledgeState:
    viewer:int
    domains:dict=field(default_factory=dict)
    dirty:set=field(default_factory=lambda:set('LW'))

    @classmethod
    def fresh(cls,v):
        return cls(v,{Card(k,i):set(OWNERS) for k in 'LW' for i in range(9)})

    def _touch(self,c): self.dirty.add(c.kind)
    def know(self,c,o):
        if self.domains[c]!={o}: self.domains[c]={o}; self._touch(c)
    def exclude(self,c,o):
        if o in self.domains[c]: self.domains[c].discard(o); self._touch(c)
    def own(self,l,w):
        self.know(Card('L',l),self.viewer); self.know(Card('W',w),self.viewer)
    def seen_player_card(self,kind,idx,owner): self.know(Card(kind,idx),owner)
    def seen_undealt(self,l,w):
        self.know(Card('L',l),UNDEALT); self.know(Card('W',w),UNDEALT)

    def _propagate_kind(self,kind):
        cards=[Card(kind,i) for i in range(9)]
        changed=True
        while changed:
            changed=False
            # Fixed cards consume owner capacity.
            fixed={o:0 for o in OWNERS}
            for c in cards:
                if not self.domains[c]: raise ValueError('inconsistent knowledge state')
                if len(self.domains[c])==1: fixed[next(iter(self.domains[c]))]+=1
            for o,n in fixed.items():
                if n>CAPS[o]: raise ValueError('inconsistent knowledge state')
                if n==CAPS[o]:
                    for c in cards:
                        if len(self.domains[c])>1 and o in self.domains[c]:
                            self.domains[c].remove(o); changed=True
                            if not self.domains[c]: raise ValueError('inconsistent knowledge state')
            # If exactly remaining-capacity cards can take an owner, they must.
            for o in OWNERS:
                remaining=CAPS[o]-sum(1 for c in cards if self.domains[c]=={o})
                if remaining<=0: continue
                possible=[c for c in cards if len(self.domains[c])>1 and o in self.domains[c]]
                if len(possible)<remaining: raise ValueError('inconsistent knowledge state')
                if len(possible)==remaining:
                    for c in possible:
                        if self.domains[c]!={o}:
                            self.domains[c]={o}; changed=True
        self.dirty.discard(kind)

    def propagate(self):
        for kind in tuple(self.dirty): self._propagate_kind(kind)
        return self

    def candidates(self):
        self.propagate()
        return ([i for i in range(9) if ANSWER in self.domains[Card('L',i)]],
                [i for i in range(9) if ANSWER in self.domains[Card('W',i)]])

def _sanity():
    s=KnowledgeState.fresh(0)
    for p,i in enumerate(range(6)): s.know(Card('L',i),p)
    s.know(Card('L',6),UNDEALT); s.know(Card('L',7),UNDEALT)
    s.propagate()
    assert s.domains[Card('L',8)]=={ANSWER}
    print('PASS')

if __name__=='__main__': _sanity()
