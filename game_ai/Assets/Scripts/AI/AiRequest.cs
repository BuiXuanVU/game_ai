using System;
using System.Collections.Generic;

[System.Serializable]
public class AIRequest
{
    public int currentHp;
    public int stamina;
    public int enemyHp;
    public List<CardDataSend> hand;
}

[System.Serializable]
public class CardDataSend
{
    public string type;
    public int value;
    public int staminaCost;
}

[Serializable]
public class UserTurnActive
{
    public List<CardType> active = new List<CardType>();
    public int turnIndex = 0;
    public int currentHp = 0;
}
