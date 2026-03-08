using System.Collections.Generic;
using UnityEngine;

public class CardStack : MonoBehaviour
{
    public List<int> cards;

    public bool isGameDeck;

    public event CardRemoveEventHandler cardRemove;

    public bool HasCard
    {
        get { return cards != null && cards.Count > 0; }
    }    
    
    public int cardCount
    {
        get { if (cards == null) return 0; else return cards.Count; }
    }

    private void Awake()
    {
        cards = new List<int>();
        if (isGameDeck) CreateDeck();
    }
    public IEnumerable<int> GetCard()
    {
        foreach (int card in cards) {
            yield return card;
        }
    }

    public int Pop()
    {
        int temp = cards[0];
        cards.RemoveAt(0);
        if (cardRemove != null)
        {
            cardRemove(this,new CardRemoveEventAgr(temp));
        }
        return temp;
    }

    public void Push(int card)
    {
        cards.Add(card);
    }

    public int HandValue()
    {
        int total = 0;
        int aces = 0;

        foreach (int card in GetCard())
        {
            int cardRank = card % 13;

            if (cardRank <= 8) {
                cardRank += 2;
                total = total + cardRank;
            }
            else if(cardRank >8  && cardRank < 12) 
            {
                cardRank = 10;
                total = total + cardRank;
            }
            else
            {
               aces++;
            }
            
           
        }

        for(int i = 0;i<aces; i++)
        {
            if(total + 11 <= 21)
            {
                total += 11;
            }
            else
            {
                total++;
            }
        }


        return total;
    }
    
    public void CreateDeck()
    {
        cards.Clear();

        for (int i = 0; i < 52; i++)
        {
            cards.Add(i);
        }
        int n = cards.Count;

        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            int temp = cards[k];
            cards[k] = cards[n];
            cards[n] = temp;
        }
    }

}
