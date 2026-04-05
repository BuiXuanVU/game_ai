using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class GameStateSnapshot
{
    // Thông tin chung của ván bài
    public GamePhase phase;
    public int potSize;
    public int highestBet;
    public List<CardDataDTO> communityCards; // Dùng DTO để dễ Serialize

    // Thông tin người chơi (đối thủ)
    public List<PlayerSnapshot> opponents;

    // Thông tin của chính AI
    public PlayerSnapshot me;

    // Lịch sử hành động trong vòng cược hiện tại (Rất quan trọng để AI nhận biết độ "gấu" của đối thủ)
    public List<ActionRecord> currentRoundHistory;
}

[System.Serializable]
public class PlayerSnapshot
{
    public string name;
    public int stack;          // Tiền còn lại
    public int currentBet;    // Tiền đã bỏ ra trong vòng này
    public bool isFolded;
    public bool isAllIn;
    public List<CardDataDTO> hand; // Chỉ hiện khi là "me" hoặc lúc Showdown
}

[System.Serializable]
public class CardDataDTO
{
    public Rank rank;
    public Suit suit;
    public string displayName; // VD: "Ace of Spades"
}

[System.Serializable]
public class HandHistory
{
    public int roundNumber;
    public List<ActionRecord> allActions; // Lưu từ Pre-flop đến River
    public List<ShowdownResult> showdowns;
    public string winnerName;
    public int finalPot;
    public string summary; // "AI thua vì đối thủ bluff ở River"
}

[System.Serializable]
public class ActionRecord
{
    public GamePhase phase;
    public string playerName;
    public PlayerActionType action;
    public int amount;
}

[System.Serializable]
public class ShowdownResult
{
    public string playerName;
    public HandRank handRank;
    public List<CardDataDTO> cards;
}

[System.Serializable]
public class LlmDecisionRequest
{
    public string phase;
    public int potSize;
    public int highestBet;
    public int callAmount;
    public int minRaiseAmount;
    public int maxRaiseAmount;
    public bool canFold;
    public bool canCheck;
    public bool canCall;
    public bool canRaise;
    public string promptPreview;
    public LlmPlayerState me;
    public List<LlmPlayerState> opponents;
    public List<CardDataDTO> communityCards;
    public List<LlmActionRecord> currentRoundHistory;
    public List<LlmHandMemory> memory;
}

[System.Serializable]
public class LlmPlayerState
{
    public string name;
    public int stack;
    public int currentBet;
    public bool isFolded;
    public bool isAllIn;
    public List<CardDataDTO> hand;
}

[System.Serializable]
public class LlmActionRecord
{
    public string phase;
    public string playerName;
    public string action;
    public int amount;
}

[System.Serializable]
public class LlmHandMemory
{
    public int roundNumber;
    public string winnerName;
    public int finalPot;
    public string summary;
}

[System.Serializable]
public class LlmDecisionResponse
{
    public string action;
    public int amount;
    public string reason;
    public string rawResponse;
}

[System.Serializable]
public class HandHistoryCollection
{
    public List<HandHistory> items = new List<HandHistory>();
}

[System.Serializable]
public class DecisionAuditEntry
{
    public string timestampUtc;
    public string playerName;
    public string phase;
    public int potSize;
    public int callAmount;
    public bool usedFallback;
    public string chosenAction;
    public int chosenAmount;
    public string reason;
    public string error;
    public string rawResponse;
    public string promptPreview;
}

[System.Serializable]
public class DecisionAuditCollection
{
    public List<DecisionAuditEntry> items = new List<DecisionAuditEntry>();
}
