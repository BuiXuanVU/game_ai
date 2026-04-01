using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private TableManager tableManager;

    private void Reset()
    {
        deckManager = GetComponentInChildren<DeckManager>();
        tableManager = GetComponentInChildren<TableManager>();
    }

    public void SetUpData(List<PlayerHand> players)
    {
        deckManager.PrepareDeck();
        tableManager.ResetTable();
        foreach (var p in players) p.ResetHand();
    }

    /// <summary>
    /// Control Table
    /// </summary>
    /// <returns></returns>

    public IEnumerator DealFlop()
    {
        deckManager.Burn();
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < 3; i++)
        {
            tableManager.SetCard(i, deckManager.Draw());
            yield return new WaitForSeconds(0.15f);
            tableManager.TableFlip(i);
        }
    }

    public IEnumerator DealNextCommunityCard(int index)
    {
        yield return new WaitForSeconds(0.5f);
        tableManager.SetCard(index, deckManager.Draw());
        tableManager.TableFlip(index);
    }

    /// <summary>
    ///  Control player 
    /// </summary>
    /// <returns></returns>

    public IEnumerator PlayerDraw(List<PlayerHand> players)
    {
        List<Coroutine> playerRoutines = new List<Coroutine>();
        foreach (var p in players)
        {
            playerRoutines.Add(StartCoroutine(DealInitialCards(p)));
        }

        foreach (var routine in playerRoutines)
        {
            yield return routine;
        }

        foreach (var p in players)
        {
            p.FlipCard();
        }
    }

    public IEnumerator DealInitialCards(List<PlayerHand> players)
    {
        for (int round = 0; round < 2; round++)
        {
            foreach (var p in players)
            {
                if (p.IsFolded) continue;
                var cardData = deckManager.Draw();
                yield return p.ReceiveCard(cardData, round);
            }
        }
    }

    private IEnumerator DealInitialCards(PlayerHand p)
    {
        for (int round = 0; round < 2; round++)
        {
            var cardData = deckManager.Draw();
            yield return p.ReceiveCard(cardData, round);

            if (round < 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
}
