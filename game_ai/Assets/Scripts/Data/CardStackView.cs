
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(CardStack))]
public class CardStackView : MonoBehaviour
{
    CardStack deck;
    Dictionary<int,GameObject> fetchedCards;
    int lastCount;

    public Vector3 start;
    public bool faceUp;
    public float cardOffset;
    public GameObject cardPrefab;

    private void Start()
    {
        fetchedCards = new Dictionary<int, GameObject>();
        deck = GetComponent<CardStack>();
        ShowCards();
        lastCount = deck.cardCount;

        deck.cardRemove += DeckCardRemoved;
    }

    void DeckCardRemoved(object sender, CardRemoveEventAgr agr)
    {
        if (fetchedCards.ContainsKey(agr.CardIndex))
        {
            Destroy(fetchedCards[agr.CardIndex]);
            fetchedCards.Remove(agr.CardIndex);
        }
    }

    private void Update()
    {
        if(lastCount != deck.cardCount)
        {
            lastCount = deck.cardCount;
            ShowCards();
        }
    }

    public void ShowCards()
    {
        int cardCount = 0;

        if (!deck.HasCard) return; 

        foreach(int card in deck.GetCard()) {
            float co = cardOffset * cardCount;
            Vector3 temp = start + new Vector3(co, 0f, 0f);
            AddCard(temp, card, cardCount);
            cardCount++;
        }
    }


    void AddCard(Vector3 position, int cardIndex, int positionIndex)
    {
        if(fetchedCards.ContainsKey(cardIndex)) return;

        GameObject cardCopy = Instantiate(cardPrefab);
        cardCopy.transform.position = position;

        CardModel cardModel = cardCopy.GetComponent<CardModel>();
        cardModel.index = cardIndex;
        cardModel.ToggleFace(faceUp);

        SpriteRenderer spriteRenderer = cardCopy.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = positionIndex;

        fetchedCards.Add(cardIndex,cardCopy);

        Debug.Log(deck.HandValue());
    }
}
