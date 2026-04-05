using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameStateSnapshot
{
    public string phase;          
    public int potSize;         
    public int currentHighestBet; 
    public List<string> communityCards;
    public List<PlayerData> players;
    public MyData me;
}