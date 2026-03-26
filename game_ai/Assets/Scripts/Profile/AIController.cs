using System.Collections;
using UnityEngine;

public class AIController : InfoAbstract
{
    public DeckManager deck;
    private AiConnect connect;

    protected override void Reset()
    {
        base.Reset();
        deck = GetComponentInChildren<DeckManager>();
    }

    private void Awake()
    {
        connect = new AiConnect(this);
    }

    public void StartTurnAI()
    {
        StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(1f);

        deck.StartTurn();
        yield return new WaitForSeconds(0.5f);

        int actionCount = 0;
        int maxActions = 10;

        while (actionCount < maxActions)
        {
            if (GameManager.Instance.currentState != GameState.AITurn)
                yield break;

            var hand = deck.GetHandData();

            if (hand.Count == 0)
                break;

            bool hasPlayable = false;
            foreach (var c in hand)
            {
                if (c.cardData.staminaCost <= stamina)
                {
                    hasPlayable = true;
                    break;
                }
            }

            if (!hasPlayable)
                break;

            CardController chosenCard = null;

            yield return connect.Call(hand ,(card) =>
            {
                chosenCard = card;
            });

            if (chosenCard == null)
                break;

            if (stamina < chosenCard.cardData.staminaCost)
                break;

            chosenCard.OnAiClicked();

            actionCount++;

            yield return new WaitForSeconds(1f);
        }

        GameManager.Instance.EndTurn();
    }
}
