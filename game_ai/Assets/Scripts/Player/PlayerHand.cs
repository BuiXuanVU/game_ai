using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<CardModel> handCards;

    private void Reset()
    {
        handCards.AddRange(GetComponentsInChildren<CardModel>());
    }
    public IEnumerator ReceiveCard(CardData data, int index)
    {
        var card = handCards[index];
        card.SetData(data);
        yield return card.ActiveAnimation();
    }

    public void FlipCard()
    {
        foreach(CardModel card in handCards)
            card.Flip();
    }

    public void ResetHand()
    {
        foreach (var c in handCards) c.ShowCard(false);
    }
}
