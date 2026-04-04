using System.Collections.Generic;
using System.Linq;

public class BettingManager
{
    public int HighestBet { get; private set; }

    public void ResetRound(List<PlayerHand> players)
    {
        HighestBet = 0;

        foreach (var p in players)
        {
            p.CurrentBet = 0;
            p.HasActed = false;
        }
    }

    public void SetHighestBet(int value)
    {
        HighestBet = value;
    }

    public bool IsRoundFinished(List<PlayerHand> players)
    {
        var activePlayers = players.Where(p => !p.IsFolded).ToList();

        if (activePlayers.Count <= 1) return true;

        return activePlayers.All(p =>
            p.HasActed &&
            (p.CurrentBet == HighestBet || p.Wallet == 0));
    }
}