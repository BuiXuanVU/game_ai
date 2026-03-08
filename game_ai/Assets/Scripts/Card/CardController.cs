using TMPro;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public CardData cardData;

    public TextMeshProUGUI nameCard;
    public TextMeshProUGUI cost;
    public TextMeshProUGUI value;

    private void Reset()
    {
        nameCard = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        cost = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        value = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
    }
    public void updateCard(CardData data)
    {
        cardData = data;

        cost.text = "-" + cardData.staminaCost.ToString();
        nameCard.text = cardData.cardName.ToString();
        value.text = cardData.value.ToString();

        gameObject.SetActive(true);
    }

    public void useCard()
    {
        gameObject.SetActive (false);
    }
}
