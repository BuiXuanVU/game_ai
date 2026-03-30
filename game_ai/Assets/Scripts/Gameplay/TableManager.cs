using System;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    public List<CardModel> communityCards;
    private int CurrentCard = 0;
    private void Reset()
    {
        communityCards.AddRange(GetComponentsInChildren<CardModel>());
    }

    public void FillNextCard(CardData data)
    {
        if (CurrentCard < communityCards.Count)
        {
            var card = communityCards[CurrentCard];
            card.SetData(data);
            card.ShowCard(true);
            CurrentCard++;
        }
    }
    public void SetCard(int index, CardData data)
    {
        if (index >= communityCards.Count) return;

        communityCards[index].SetData(data);
        communityCards[index].ShowCard(true);

        CurrentCard = index;
    }

    public void TableFlip(int index)
    {
        communityCards[index].Flip();
    }

    public void ResetTable()
    {
        foreach (var c in communityCards) c.ShowCard(false);
    }

    public int getCurrentCard()=> CurrentCard;
}