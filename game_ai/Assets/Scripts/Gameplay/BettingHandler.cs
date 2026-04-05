using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BettingHandler : MonoBehaviour
{
    [SerializeField] private PotController potController;
    [SerializeField] private ChipSender chipSender;
    private BettingManager bettingManager = new BettingManager();

    private void Reset()
    {
        potController = GetComponentInChildren<PotController>();
        chipSender = GetComponentInChildren<ChipSender>();
    }

    public void ResetRound(List<PlayerHand> players) => bettingManager.ResetRound(players);

    public IEnumerator SetupBlinds(List<PlayerHand> players, int dealerIndex, int smallBlind, int bigBlind)
    {
        int sbIdx = (dealerIndex + 1) % players.Count;
        int bbIdx = (dealerIndex + 2) % players.Count;

        yield return ExecuteBet(players[sbIdx], smallBlind);
        yield return ExecuteBet(players[bbIdx], bigBlind);

        bettingManager.SetHighestBet(bigBlind);
    }

    public IEnumerator RunBettingPhase(List<PlayerHand> players, GamePhase phase, TurnManager turnManager, PokerAI ai, List<ActionRecord> roundHistory)
    {
        while (!bettingManager.IsRoundFinished(players))
        {
            var player = turnManager.GetCurrent(players);
            if (!player.IsFolded && player.Wallet > 0)
            {
                PlayerAction action = null;
                if (player.IsHuman) yield return WaitForHumanAction(player, phase, roundHistory, a => action = a);
                else yield return ai.DecideActionCoroutine(this, player, bettingManager.HighestBet, phase, roundHistory, a => action = a);

                yield return ExecuteAction(player, action, phase, roundHistory);
                player.HasActed = true;
            }
            turnManager.MoveNext(players);
        }
    }

    private IEnumerator WaitForHumanAction(PlayerHand player, GamePhase phase, List<ActionRecord> roundHistory, System.Action<PlayerAction> callback)
    {
        UIManager.Instance.Show(player, bettingManager.HighestBet);
        PlayerAction action = null;
        System.Action<PlayerAction> handler = null;
        handler = (a) => { action = a; UIManager.Instance.OnActionSelected -= handler; };
        UIManager.Instance.OnActionSelected += handler;
        yield return new WaitUntil(() => action != null);
        UIManager.Instance.Hide();
        callback?.Invoke(action);
    }

    private IEnumerator ExecuteAction(PlayerHand player, PlayerAction action, GamePhase phase, List<ActionRecord> roundHistory)
    {
        roundHistory.Add(new ActionRecord
        {
            phase = phase,
            playerName = player.name,
            action = action.Type,
            amount = (action.Type == PlayerActionType.Raise) ? action.RaiseAmount : 0
        });

        switch (action.Type)
        {
            case PlayerActionType.Fold: player.IsFolded = true; break;
            case PlayerActionType.Check: break;
            case PlayerActionType.Call: yield return ExecuteBet(player, bettingManager.HighestBet - player.CurrentBet); break;
            case PlayerActionType.Raise:
                int total = (bettingManager.HighestBet - player.CurrentBet) + action.RaiseAmount;
                yield return ExecuteBet(player, total);
                bettingManager.SetHighestBet(player.CurrentBet);
                break;
        }
    }

    public IEnumerator ExecuteBet(PlayerHand player, int amount)
    {
        int actualBet = player.RequestMoney(amount);
        player.CurrentBet += actualBet;

        if (actualBet > 0)
        {
            yield return StartCoroutine(chipSender.SendRoutine(player.chipController, potController.chipController));
            potController.AddToPot(actualBet);
        }
    }

    public int HighestBet => bettingManager.HighestBet;
}
