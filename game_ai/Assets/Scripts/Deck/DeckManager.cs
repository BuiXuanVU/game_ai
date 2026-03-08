using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public CardDatabase database;
    public GameObject prefab;

    private List<CardData> currentHand = new List<CardData>();

    public int maxStamina = 10;
    public int currentStamina;

    private void Start()
    {
        StartTurn();
    }
    public void StartTurn()
    {
        currentStamina = maxStamina;

        currentHand.Clear();
        currentHand = database.getRandom(5);

        DisplayHand();
    }

    void DisplayHand()
    {
        foreach (CardData card in currentHand)
        {
            GameObject data = Instantiate(prefab, transform);
            data.GetComponent<CardController>().updateCard(card);
        }
        
    }
}
