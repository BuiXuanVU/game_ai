using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    public CardDatabase database;
    public GameObject prefab;

    private List<CardController> cardSlots = new List<CardController>();
    public HorizontalLayoutGroup content;
    public int handSize = 5;

    private void Reset()
    {
        handSize = 5;
        content = transform.GetComponentInChildren<HorizontalLayoutGroup>();
    }
    public void StartTurn()
    {
        List<CardData> newHand = database.GetRandom(handSize);
        SetupHand(newHand);
    }

    public List<CardController> GetHandData()
    {
        return cardSlots.Where(c => c.gameObject.activeSelf).ToList();
    }

    void SetupHand(List<CardData> cards)
    {
        for (int i = 0; i < handSize; i++)
        {
            CardController card;

            if (i >= cardSlots.Count)
            {
                GameObject obj = Instantiate(prefab, content.transform);
                card = obj.GetComponent<CardController>();
                cardSlots.Add(card);
            }
            else
            {
                card = cardSlots[i];
            }

            card.gameObject.SetActive(true);
            card.UpdateCard(cards[i]);
        }

        for (int i = handSize; i < cardSlots.Count; i++)
        {
            cardSlots[i].Hide();
        }
    }

    public void ClearHand()
    {
        foreach (var card in cardSlots)
        {
            card.Hide();
        }
    }
}
