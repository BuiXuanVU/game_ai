using UnityEngine;

public class testcard : MonoBehaviour
{
    public CardFlipper card;
    public CardModel cardModel;
    public int cardindex = 0;
    public void onSwap()
    {
        if(cardindex > cardModel.faces.Count)
        {
            cardindex = 0;
            card.FlipCard(cardModel.faces[cardindex], cardModel.back, cardindex);
        }
        else
        {
            if(cardindex > 0)
            {
                card.FlipCard(cardModel.faces[cardindex - 1], cardModel.back, cardindex);
            }
            else
            {
                card.FlipCard(cardModel.back, cardModel.faces[cardindex], cardindex);
            }
            cardindex++;
        }
        
    }
}
