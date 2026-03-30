using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<CardModel> handCards;
    public ChipController chipController;
    public TextMeshProUGUI textMeshProUGUI;

    private void Reset()
    {
        handCards.AddRange(GetComponentsInChildren<CardModel>());
        chipController = GetComponentInChildren<ChipController>();
        textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        setPrice();
    }

    public void setPrice()
    {
        textMeshProUGUI.text = chipController.wallet.ToString();
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
