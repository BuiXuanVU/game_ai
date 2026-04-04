using System.Collections.Generic;
using UnityEngine;

public class HandResult
{
    public HandRank Rank;
    public List<int> Values;
    public List<CardData> WinningCards;

    public HandResult(HandRank rank, List<int> values, List<CardData> winningCards)
    {
        Rank = rank;
        Values = values;
        WinningCards = winningCards;
    }
}