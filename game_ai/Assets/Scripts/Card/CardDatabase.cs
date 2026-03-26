using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Scriptable Objects/CardDatabase")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> allCards;

    public CardData GetCardByID(CardType type)
    {
        return allCards.Find(c => c.type == type);
    }

    public List<CardData> GetRandom(int count)
    {
        List<CardData> currentHand = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            currentHand.Add(allCards[Random.Range(0, allCards.Count)]);
        }
        return currentHand;
    }
}

[System.Serializable]
public class CardData
{
    public CardType type;
    public string cardName;
    public int staminaCost;
    public int value;
}