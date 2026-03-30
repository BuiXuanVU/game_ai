using System.Collections.Generic;
using UnityEngine;
using System;
public class DeckManager : MonoBehaviour
{
    private CardDataManager dataManager;
    private List<CardData> deck = new List<CardData>();
    private int currentIndex = 0;

    private void Start()
    {
        dataManager = CardDataManager.Instance;
    }
    public void PrepareDeck()
    {
        
        deck.Clear();
        Sprite randomBack = dataManager.backs[UnityEngine.Random.Range(0, dataManager.backs.Count)];

        foreach (Suit s in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank r in Enum.GetValues(typeof(Rank)))
            {
                CardData newData = new CardData
                {
                    suit = s,
                    rank = r,
                    back = randomBack,
                    face = GetFaceSprite(s, r)
                };
                deck.Add(newData);
            }
        }
        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, deck.Count);
            (deck[i], deck[rnd]) = (deck[rnd], deck[i]);
        }
        currentIndex = 0;
    }

    public CardData Draw()
    {
        if (currentIndex >= deck.Count) return null;
        return deck[currentIndex++];
    }

    public void Burn() => currentIndex++;

    private Sprite GetFaceSprite(Suit suit, Rank rank)
    {
        int index = (int)rank - 2;
        return suit switch
        {
            Suit.Clubs => dataManager.facesClubs[index],
            Suit.Diamonds => dataManager.facesDiamonds[index],
            Suit.Hearts => dataManager.facesHearts[index],
            Suit.Spades => dataManager.facesSpades[index],
            _ => null
        };
    }
}