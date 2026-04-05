from typing import List, Optional, Union

from pydantic import BaseModel, Field


class CardData(BaseModel):
    rank: Union[str, int]
    suit: Union[str, int]
    displayName: str


class PlayerState(BaseModel):
    name: str
    stack: int
    currentBet: int
    isFolded: bool
    isAllIn: bool
    hand: List[CardData] = Field(default_factory=list)


class ActionRecord(BaseModel):
    phase: str
    playerName: str
    action: str
    amount: int = 0


class HandMemory(BaseModel):
    roundNumber: int
    winnerName: str
    finalPot: int
    summary: str


class DecisionRequest(BaseModel):
    phase: str
    potSize: int
    highestBet: int
    callAmount: int
    minRaiseAmount: int
    maxRaiseAmount: int
    canFold: bool
    canCheck: bool
    canCall: bool
    canRaise: bool
    promptPreview: Optional[str] = None
    me: PlayerState
    opponents: List[PlayerState] = Field(default_factory=list)
    communityCards: List[CardData] = Field(default_factory=list)
    currentRoundHistory: List[ActionRecord] = Field(default_factory=list)
    memory: List[HandMemory] = Field(default_factory=list)


class DecisionResponse(BaseModel):
    action: str
    amount: int = 0
    reason: str = ""
    rawResponse: Optional[str] = None
