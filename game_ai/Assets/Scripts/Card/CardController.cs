using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    public CardData cardData;
    [Header("UI References")]
    public TextMeshProUGUI nameCard;
    public TextMeshProUGUI cost;
    public TextMeshProUGUI value;
    public Button btn;

    private void Start()
    {
        if (btn != null)
            btn.onClick.AddListener(OnCardClicked);
    }
    private void Reset()
    {
        nameCard = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        cost = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        value = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        btn = transform.GetComponentInChildren<Button>();
    }


    public void UpdateCard(CardData data)
    {
        cardData = data;

        nameCard.text = data.cardName;
        cost.text = "-" + data.staminaCost;
        value.text = data.value.ToString();

        gameObject.SetActive(true);
    }

    public void OnCardClicked()
    {
        bool success = GameManager.Instance.HandleCardPlayed(cardData);

        if (success)
        {
            Hide();
        }
    }

    public void OnAiClicked()
    {
        bool success = GameManager.Instance.HandleCardAi(cardData);

        if (success)
        {
            Hide();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
