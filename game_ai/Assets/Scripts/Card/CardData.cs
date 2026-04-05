using UnityEngine;

public class CardData 
{
    public Suit suit;
    public Rank rank;
    public Sprite face;
    public Sprite back;

    public CardDataDTO ToDTO()
    {
        return new CardDataDTO
        {
            rank = this.rank,
            suit = this.suit,
            displayName = $"{rank} of {suit}"
        };
    }
}
