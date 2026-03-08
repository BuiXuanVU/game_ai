using UnityEngine;

public class DebugDealer : MonoBehaviour
{
    public CardStack player;
    public CardStack dealer;

    int count = 0;
    public int[] card = new int[] { 9, 12};
    private void OnGUI()
    {
        //if(GUI.Button(new Rect(10,10,256,28),"HIT ME"))
        //{
        //    player.Push(dealer.Pop());
        //}
        
        if(GUI.Button(new Rect(10,10,256,28),"HIT ME"))
        {
            player.Push(card[count++]);
        }
    }
}
