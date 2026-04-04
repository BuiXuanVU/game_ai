using System.Collections.Generic;
using UnityEngine;

public class TurnManager
{
    private int currentIndex;

    public void SetStartIndex(int index)
    {
        currentIndex = index;
    }

    public PlayerHand GetCurrent(List<PlayerHand> players)
    {
        return players[currentIndex];
    }

    public void MoveNext(List<PlayerHand> players)
    {
        currentIndex = (currentIndex + 1) % players.Count;
    }

    public int GetIndex() => currentIndex;
}