from dataclasses import dataclass, field
from itertools import permutations
from functools import lru_cache

PLAYERS=range(6); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))
# All legal ownership rows for one category. UNDEALT occupies two slots, so
# deduplicate permutations once at module load. 9!/2 = 181,440 rows.
_SLOTS=(0,1,2,3,4,5,ANSWER,UNDEALT,UNDEALT)
ALL_ASSIGNMENTS=tuple(set(permutations(_SLOTS)))

@dataclass(frozen=True)
class Card:
    kind:str
    idx:int

@lru_cache(maxsize=32768)
def _solve(mask_key):
    # mask_key: nine owner bitmasks. Filter precomputed legal rows only.
    out=[]
    for row in ALL_ASSIGNMENTS:
        ok=True
        for i,o in enumerate(row):
            if not (mask_key[i] & (1<<o)):
                ok=False; break
        if ok: out.append(row)
    return tuple(out)

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

    def _valid_assignments(self,kind):
        return _solve(self._key(kind))

    def propagate(self):
        for kind in 'LW':
            sols=self._valid_assignments(kind)
            if not sols: raise ValueError('inconsistent knowledge state')
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
    for p,i in enumerate(range(6)): s.know(Card('L',i),p)
    s.know(Card('L',6),UNDEALT); s.know(Card('L',7),UNDEALT)
    s.propagate(); assert s.domains[Card('L',8)]=={ANSWER}
    assert len(ALL_ASSIGNMENTS)==181440
    print('PASS',len(ALL_ASSIGNMENTS),_solve.cache_info())

if __name__=='__main__': _sanity()
