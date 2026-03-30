using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private TableManager tableManager;
    [SerializeField] private List<PlayerHand> players;

    private void Reset()
    {
        deckManager = GetComponentInChildren<DeckManager>();
        tableManager = GetComponentInChildren<TableManager>();
        players.AddRange(GetComponentsInChildren<PlayerHand>());
    }

    private void Start()
    {
        SetUpData();
        StartCoroutine(StartNewGame());
    }

    void SetUpData()
    {
        deckManager.PrepareDeck();
        tableManager.ResetTable();
        foreach (var p in players) p.ResetHand();
    }

    public IEnumerator StartNewGame()
    {
        //StartCoroutine(PlayerDraw());
        //yield return new WaitForSeconds(0.5f);

        StartCoroutine(TableDraw());

        yield return null;
    }

    /// <summary>
    /// Control Table
    /// </summary>
    /// <returns></returns>

    private IEnumerator TableDraw()
    {
        deckManager.Burn();
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < 3; i++)
        {
            tableManager.SetCard(i, deckManager.Draw());
            yield return new WaitForSeconds(0.15f);
            tableManager.TableFlip(i);
        }

        yield return new WaitForSeconds(2f);
        StartCoroutine(TableDrawMore());        
        yield return new WaitForSeconds(2f);
        StartCoroutine(TableDrawMore());
        yield return new WaitForSeconds(2f);
        StartCoroutine(TableDrawMore());
    }

    private IEnumerator TableDrawMore()
    {
        int nextIndex = tableManager.getCurrentCard() + 1;

        if (nextIndex < 5)
        {
            tableManager.SetCard(nextIndex, deckManager.Draw());
            yield return new WaitForSeconds(0.3f);
            tableManager.TableFlip(nextIndex);
        }
    }


    /// <summary>
    ///  Control player 
    /// </summary>
    /// <returns></returns>
    public IEnumerator PlayerDraw()
    {
        List<Coroutine> playerRoutines = new List<Coroutine>();
        foreach (var p in players)
        {
            playerRoutines.Add(StartCoroutine(SinglePlayerDrawRoutine(p)));
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

    private IEnumerator SinglePlayerDrawRoutine(PlayerHand p)
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
