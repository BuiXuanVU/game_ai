using System.Collections;
using UnityEngine;

public class PokerAI
{
    public IEnumerator DecideAction(PlayerHand player, int highestBet, System.Action<PlayerAction> callback)
    {
        yield return new WaitForSeconds(0.5f);

        PlayerAction action;

        if (player.CurrentBet < highestBet)
        {
            int rand = Random.Range(0, 10);

            if (rand < 2)
                action = new PlayerAction(PlayerActionType.Fold);
            else if (rand < 8)
                action = new PlayerAction(PlayerActionType.Call);
            else
                action = new PlayerAction(PlayerActionType.Raise, 50);
        }
        else
        {
            if (Random.value < 0.7f)
                action = new PlayerAction(PlayerActionType.Check);
            else
                action = new PlayerAction(PlayerActionType.Raise, 50);
        }

        callback?.Invoke(action);
    }
}