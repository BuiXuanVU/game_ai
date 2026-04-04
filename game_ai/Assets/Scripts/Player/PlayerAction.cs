public class PlayerAction
{
    public PlayerActionType Type;
    public int RaiseAmount;

    public PlayerAction(PlayerActionType type, int raiseAmount = 0)
    {
        Type = type;
        RaiseAmount = raiseAmount;
    }
}
