from dataclasses import dataclass, field
from functools import lru_cache

PLAYERS=range(6); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))
CAPS=(1,1,1,1,1,1,1,2)

@dataclass(frozen=True)
class Card:
    kind:str
    idx:int

@lru_cache(maxsize=131072)
def _possible_masks(mask_key):
    # Exact capacity solver without materializing 181,440 complete assignments.
    # Returns, for each card, the union of owners occurring in any legal solution.
    possible=[0]*9
    found=False
    counts=[0]*8
    assigned=[-1]*9

    def rec(left):
        nonlocal found
        if not left:
            found=True
            for i,o in enumerate(assigned):
                possible[i] |= 1<<o
            return
        # MRV using only owners with remaining capacity.
        best=None; opts=None
        for i in left:
            cur=[o for o in range(8) if (mask_key[i]&(1<<o)) and counts[o]<CAPS[o]]
            if not cur: return
            if opts is None or len(cur)<len(opts):
                best,opts=i,cur
                if len(opts)==1: break
        nxt=left-{best}
        for o in opts:
            assigned[best]=o; counts[o]+=1
            rec(nxt)
            counts[o]-=1; assigned[best]=-1
            # If every domain bit for every card is already witnessed, more
            # solutions cannot improve propagation.
            if all((possible[j] & mask_key[j]) == mask_key[j] for j in range(9)):
                return

    rec(set(range(9)))
    return tuple(possible) if found else None

@dataclass
class KnowledgeState:
    viewer:int
    domains:dict=field(default_factory=dict)

    @classmethod
    def fresh(cls,v):
        return cls(v,{Card(k,i):set(OWNERS) for k in 'LW' for i in range(9)})

    def know(self,c,o): self.domains[c]={o}
    def exclude(self,c,o): self.domains[c].discard(o)
    def own(self,l,w):
        self.know(Card('L',l),self.viewer); self.know(Card('W',w),self.viewer)
    def seen_player_card(self,kind,idx,owner): self.know(Card(kind,idx),owner)
    def seen_undealt(self,l,w):
        self.know(Card('L',l),UNDEALT); self.know(Card('W',w),UNDEALT)

    def _key(self,kind):
        return tuple(sum(1<<o for o in self.domains[Card(kind,i)]) for i in range(9))

    def propagate(self):
        for kind in 'LW':
            masks=_possible_masks(self._key(kind))
            if masks is None: raise ValueError('inconsistent knowledge state')
            for i,m in enumerate(masks):
                self.domains[Card(kind,i)] &= {o for o in range(8) if m&(1<<o)}
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
    # Capacity propagation: a player already fixed to one card cannot own another.
    assert all(0 not in s.domains[Card('L',i)] for i in range(1,9))
    print('PASS',_possible_masks.cache_info())

if __name__=='__main__': _sanity()
