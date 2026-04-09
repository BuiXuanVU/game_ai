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


class OpponentStats(BaseModel):
    name: str
    handsObserved: int = 0
    foldRate: float = 0.0
    callRate: float = 0.0
    raiseRate: float = 0.0
    showdownWinRate: float = 0.0
    aggressionScore: float = 0.0


class BotProfile(BaseModel):
    name: str
    styleNotes: str = ""
    historyWindow: int = 0
    useOpponentStats: bool = False
    modelName: Optional[str] = None


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
    opponentStats: List[OpponentStats] = Field(default_factory=list)
    botProfile: Optional[BotProfile] = None


class DecisionResponse(BaseModel):
    action: str
    amount: int = 0
    reason: str = ""
    rawResponse: Optional[str] = None
    usedFallback: bool = False
