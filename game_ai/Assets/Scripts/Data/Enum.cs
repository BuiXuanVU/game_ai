public enum GamePhase
{
    PreFlop,
    Flop,
    Turn,
    River,
    Showdown
}

public enum Suit { Clubs, Diamonds, Hearts, Spades }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }


public enum PlayerActionType
{
    Fold,
    Check,
    Call,
    Raise
}

public enum HandRank
{
    HighCard = 1,
    OnePair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush
}