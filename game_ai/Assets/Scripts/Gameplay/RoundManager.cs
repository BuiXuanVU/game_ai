using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RoundManager
{
    private CardDealer dealer;
    private PotController pot;
    private List<PlayerHand> players;

    private BettingManager betting = new BettingManager();
    private TurnManager turn = new TurnManager();

    public bool IsEarlyWin => players.Count(p => !p.IsFolded) <= 1;

    public RoundManager(CardDealer d, PotController p, List<PlayerHand> pl)
    {
        dealer = d;
        pot = p;
        players = pl;
    }

    public IEnumerator PlayRound()
    {
        Setup();

        yield return Blinds();
        yield return dealer.PlayerDraw(players);

        yield return BettingPhase();
        if (IsEarlyWin) yield break;

        yield return Flop();
        if (IsEarlyWin) yield break;

        yield return Turn();
        if (IsEarlyWin) yield break;

        yield return River();
    }

    private void Setup()
    {
        pot.ClearPot();
        betting.SetHighestBet(0);

        foreach (var p in players)
            p.ResetHand();
    }

    private IEnumerator Blinds()
    {
        yield return null;
    }

    private IEnumerator BettingPhase()
    {
        while (!betting.IsRoundFinished(players))
        {
            var p = turn.GetCurrent(players);

            if (!p.IsFolded)
                yield return PlayerTurn(p);

            turn.MoveNext(players);
        }
    }

    private IEnumerator PlayerTurn(PlayerHand player)
    {
        yield return null;
    }

    private IEnumerator Flop()
    {
        yield return dealer.DealFlop();
        yield return BettingPhase();
    }

    private IEnumerator Turn()
    {
        yield return dealer.DealNextCommunityCard(3);
        yield return BettingPhase();
    }

    private IEnumerator River()
    {
        yield return dealer.DealNextCommunityCard(4);
        yield return BettingPhase();
    }
}