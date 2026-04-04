using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Components")]
    [SerializeField] private ActionButtonsUI buttonsUI;
    [SerializeField] private RaiseSliderUI sliderUI;

    public System.Action<PlayerAction> OnActionSelected;

    private PlayerHand currentPlayer;
    private int highestBet;

    private void Reset()
    {
        buttonsUI = GetComponentInChildren<ActionButtonsUI>();
        sliderUI = GetComponentInChildren<RaiseSliderUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Init();
    }

    private void Init()
    {
        buttonsUI.OnFold += () => SendAction(PlayerActionType.Fold);
        buttonsUI.OnCall += () => SendAction(PlayerActionType.Call);
        buttonsUI.OnCheck += () => SendAction(PlayerActionType.Check);
        buttonsUI.OnRaise += ShowRaiseSlider;

        sliderUI.OnConfirm += OnRaiseConfirmed;
        sliderUI.OnCancel += OnRaiseCanceled;
    }

    // ==========================================
    // SHOW / HIDE
    // ==========================================

    public void Show(PlayerHand player, int highestBet)
    {
        this.currentPlayer = player;
        this.highestBet = highestBet;

        gameObject.SetActive(true);

        RefreshUI();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        sliderUI.Hide();
        buttonsUI.gameObject.SetActive(true);
    }

    // ==========================================
    // UI LOGIC
    // ==========================================

    private void RefreshUI()
    {
        int callAmount = highestBet - currentPlayer.CurrentBet;

        bool canCheck = callAmount == 0;
        bool canCall = callAmount > 0 && currentPlayer.Wallet > 0;
        bool canRaise = currentPlayer.Wallet > callAmount;
        bool canFold = true;

        buttonsUI.Refresh(canFold, canCall, canCheck, canRaise);
        sliderUI.Hide();
        buttonsUI.gameObject.SetActive(true);
    }

    // ==========================================
    // BUTTON ACTIONS
    // ==========================================

    private void SendAction(PlayerActionType type)
    {
        OnActionSelected?.Invoke(new PlayerAction(type));
    }

    private void ShowRaiseSlider()
    {
        int callAmount = highestBet - currentPlayer.CurrentBet;

        int minRaise = callAmount + 1;
        int maxRaise = currentPlayer.Wallet;
        buttonsUI.gameObject.SetActive(false);
        sliderUI.Setup(minRaise, maxRaise);
    }

    private void OnRaiseConfirmed(int amount)
    {
        sliderUI.Hide();
        buttonsUI.gameObject.SetActive(true);
        OnActionSelected?.Invoke(
            new PlayerAction(PlayerActionType.Raise, amount)
        );
    }

    private void OnRaiseCanceled()
    {
        sliderUI.Hide();
        buttonsUI.gameObject.SetActive(true);
    }
}