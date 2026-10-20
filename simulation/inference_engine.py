from dataclasses import dataclass, field
from itertools import permutations

PLAYERS=range(6); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))

@dataclass(frozen=True)
class Card:
    kind:str
    idx:int

@dataclass
class KnowledgeState:
    viewer:int
    domains:dict=field(default_factory=dict)

    @classmethod
    def fresh(cls,v):
        return cls(v,{Card(k,i):set(OWNERS) for k in 'LW' for i in range(9)})

    def know(self,c,o):
        self.domains[c]={o}

    def exclude(self,c,o):
        self.domains[c].discard(o)

    def own(self,l,w):
        self.know(Card('L',l),self.viewer)
        self.know(Card('W',w),self.viewer)

    def seen_player_card(self,kind,idx,owner):
        self.know(Card(kind,idx),owner)

    def seen_undealt(self,l,w):
        self.know(Card('L',l),UNDEALT)
        self.know(Card('W',w),UNDEALT)

    def _valid_assignments(self,kind):
        # Exact category capacities:
        # players 0..5 own exactly one card each, ANSWER exactly one,
        # UNDEALT exactly two. Every one of the 9 cards has one owner.
        cards=[Card(kind,i) for i in range(9)]
        slots=(0,1,2,3,4,5,ANSWER,UNDEALT,UNDEALT)
        # only 9! / 2! possibilities before domain pruning; recursive MRV keeps this small.
        result=[]
        counts={o:0 for o in OWNERS}
        caps={**{p:1 for p in PLAYERS},ANSWER:1,UNDEALT:2}
        assignment=[None]*9

        def rec(left):
            if not left:
                result.append(tuple(assignment))
                return
            i=min(left,key=lambda x:sum(counts[o]<caps[o] for o in self.domains[cards[x]]))
            nxt=left-{i}
            for o in self.domains[cards[i]]:
                if counts[o]>=caps[o]:
                    continue
                assignment[i]=o; counts[o]+=1
                rec(nxt)
                counts[o]-=1; assignment[i]=None
        rec(set(range(9)))
        return result

    def propagate(self):
        for kind in 'LW':
            sols=self._valid_assignments(kind)
            if not sols:
                raise ValueError('inconsistent knowledge state')
            for i in range(9):
                possible={s[i] for s in sols}
                self.domains[Card(kind,i)] &= possible
        return self

    def candidates(self):
        self.propagate()
        return ([i for i in range(9) if ANSWER in self.domains[Card('L',i)]],
                [i for i in range(9) if ANSWER in self.domains[Card('W',i)]])

def _sanity():
    s=KnowledgeState.fresh(0)
    s.own(0,0)
    # If six player-owned locations and two undealt locations are known,
    # the remaining location must be the answer.
    for p,i in enumerate(range(6)):
        s.know(Card('L',i),p)
    s.know(Card('L',6),UNDEALT); s.know(Card('L',7),UNDEALT)
    s.propagate()
    assert s.domains[Card('L',8)]=={ANSWER}
    print('PASS')

if __name__=='__main__':
    _sanity()
