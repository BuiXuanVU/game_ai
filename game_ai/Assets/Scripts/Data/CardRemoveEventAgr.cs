using System;

public class CardRemoveEventAgr: EventArgs
{
    public int CardIndex { get; private set; }

    public CardRemoveEventAgr(int cardIndex)
    {
        CardIndex = cardIndex;
    }
}