using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    private int dealerIndex = -1;

    public void SetupNewRound(List<PlayerHand> players)
    {
        dealerIndex = GetNextValidPlayerIndex(players, dealerIndex);
        foreach (var p in players)
        {
            p.ResetHand();
            p.SetDealerActive(p == players[dealerIndex]);
            if (p.Wallet <= 0) p.IsFolded = true;
        }
    }

    private int GetNextValidPlayerIndex(List<PlayerHand> players, int currentIndex)
    {
        int next = (currentIndex + 1) % players.Count;
        int checks = 0;
        while (players[next].Wallet <= 0 && checks < players.Count)
        {
            next = (next + 1) % players.Count;
            checks++;
        }
        return next;
    }

    public int DealerIndex => dealerIndex;
}