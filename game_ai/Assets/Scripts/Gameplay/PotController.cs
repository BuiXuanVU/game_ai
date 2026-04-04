using TMPro;
using UnityEngine;

public class PotController : MonoBehaviour
{ 
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private int _pot = 0;
    public ChipController chipController;
    public int Pot
    {
        get => _pot;
        private set
        {
            _pot = value;
            textMeshProUGUI.text = _pot.ToString() + "$";
            if (chipController != null)
            {
                chipController.RenderChips(_pot);
            }
        }
    }

    private void Reset()
    {
        chipController = GetComponentInChildren<ChipController>();
        textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }
    public void AddToPot(int amount)
    {
        if (amount <= 0) return;

        Pot += amount;
    }
    public void ClearPot()
    {
        Pot = 0;
    }
}
