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