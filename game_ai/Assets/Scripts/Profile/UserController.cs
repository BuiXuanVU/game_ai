using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UserController : InfoAbstract
{
    public DeckManager deck;

    protected override void Reset()
    {
        base.Reset();
        deck = GetComponentInChildren<DeckManager>();
    }
}
