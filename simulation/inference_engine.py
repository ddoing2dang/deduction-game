from dataclasses import dataclass, field
PLAYERS=range(6); ANSWER=6; UNDEALT=7; OWNERS=set(range(8))
@dataclass(frozen=True)
class Card: kind:str; idx:int
@dataclass
class KnowledgeState:
    viewer:int
    domains:dict=field(default_factory=dict)
    @classmethod
    def fresh(cls,v): return cls(v,{Card(k,i):set(OWNERS) for k in 'LW' for i in range(9)})
    def know(self,c,o): self.domains[c]={o}
    def exclude(self,c,o): self.domains[c].discard(o)
    def own(self,l,w): self.know(Card('L',l),self.viewer); self.know(Card('W',w),self.viewer)
    def seen_player_card(self,kind,idx,owner): self.know(Card(kind,idx),owner)
    def seen_undealt(self,l,w): self.know(Card('L',l),UNDEALT); self.know(Card('W',w),UNDEALT)
    def candidates(self):
        return ([i for i in range(9) if ANSWER in self.domains[Card('L',i)]],[i for i in range(9) if ANSWER in self.domains[Card('W',i)]])
