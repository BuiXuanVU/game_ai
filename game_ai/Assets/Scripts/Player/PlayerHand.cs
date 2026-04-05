using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHand : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI walletText;
    [SerializeField] private int _wallet = 1000;
    [SerializeField] private Image dealerIcon;

    public List<PlayerAction> currentRoundActions = new List<PlayerAction>();
    public List<HandHistory> history = new List<HandHistory>();

    public List<CardModel> handCards;
    public ChipController chipController;

    public int CurrentBet = 0;
    public bool IsFolded = false;
    public bool HasActed = false;
    public bool IsHuman;
    public int Wallet
    {
        get => _wallet;
        private set
        {
            _wallet = value;
            walletText.text = _wallet.ToString() + "$";
            if (chipController != null)
            {
                chipController.RenderChips(_wallet);
            }
        }
    }

    private void Reset()
    {
        handCards.AddRange(GetComponentsInChildren<CardModel>());
        chipController = GetComponentInChildren<ChipController>();
        walletText = GetComponentInChildren<TextMeshProUGUI>();
        dealerIcon = transform.GetChild(0).GetComponent<Image>();
    }

    public void AddMoney(int amount)
    {
        if (amount < 0) return;
        Wallet += amount;
    }

    public bool SpendMoney(int amount)
    {
        if (amount > _wallet) return false;
        Wallet -= amount;
        return true;
    }

    public int RequestMoney(int amount)
    {
        int actualAmount = Mathf.Min(amount, Wallet);
        Wallet -= actualAmount;
        return actualAmount;
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
        IsFolded = false;
        HasActed = false;
        CurrentBet = 0;
        foreach (var c in handCards) c.ShowCard(false);
    }

    public void SetDealerActive(bool isActive) => dealerIcon.enabled = isActive;
}