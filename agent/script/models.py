from pydantic import BaseModel
from typing import List

class Card(BaseModel):
    type: str
    value: int
    staminaCost: int

class GameState(BaseModel):
    currentHp: int
    stamina: int
    enemyHp: int
    hand: List[Card]