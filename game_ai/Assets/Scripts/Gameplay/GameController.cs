using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PokerGameController : MonoBehaviour
{
    private TurnManager turnManager = new TurnManager();
    private BettingManager bettingManager = new BettingManager();
    private PokerAI ai = new PokerAI();

    [Header("References")]
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private PotController potController;
    [SerializeField] private List<PlayerHand> players;
    [SerializeField] private ChipSender chipSender;

    [Header("Game Config")]
    [SerializeField] private int smallBlind = 10;
    [SerializeField] private int bigBlind = 20;

    private int dealerIndex = -1;
    private GamePhase currentPhase;

    private void Reset()
    {
        cardDealer = GetComponentInChildren<CardDealer>();
        potController = GetComponentInChildren<PotController>();
        players.AddRange(GetComponentsInChildren<PlayerHand>());
    }

    private void Start()
    {
        StartCoroutine(GameLoop());
    }

    // ==========================================
    // GAME LOOP
    // ==========================================
    private IEnumerator GameLoop()
    {
        while (true)
        {
            yield return StartCoroutine(StartNewRound());
            yield return StartCoroutine(PreFlopPhase());

            if (!IsGameOverEarly())
                yield return StartCoroutine(FlopPhase());

            if (!IsGameOverEarly())
                yield return StartCoroutine(TurnPhase());

            if (!IsGameOverEarly())
                yield return StartCoroutine(RiverPhase());

            yield return StartCoroutine(ShowdownPhase());

            yield return new WaitForSeconds(3f);
        }
    }

    // ==========================================
    // PLAYER TURN
    // ==========================================
    private IEnumerator PlayerTurn(PlayerHand player)
    {
        PlayerAction action = null;

        if (player.IsHuman)
        {
            yield return StartCoroutine(WaitForHumanAction(player, (a) => action = a));
        }
        else
        {
            yield return StartCoroutine(
                ai.DecideAction(player, bettingManager.HighestBet, (a) => action = a)
            );
        }

        yield return StartCoroutine(ExecuteAction(player, action));
    }

    private IEnumerator WaitForHumanAction(PlayerHand player, System.Action<PlayerAction> callback)
    {
        UIManager.Instance.Show(player, bettingManager.HighestBet);

        PlayerAction action = null;

        System.Action<PlayerAction> handler = null;
        handler = (a) =>
        {
            action = a;
            UIManager.Instance.OnActionSelected -= handler;
        };

        UIManager.Instance.OnActionSelected += handler;

        yield return new WaitUntil(() => action != null);

        UIManager.Instance.Hide();

        callback?.Invoke(action);
    }

    private IEnumerator ExecuteAction(PlayerHand player, PlayerAction action)
    {
        switch (action.Type)
        {
            case PlayerActionType.Fold:
                Fold(player);
                break;

            case PlayerActionType.Check:
                Check(player);
                break;

            case PlayerActionType.Call:
                yield return StartCoroutine(Call(player));
                break;

            case PlayerActionType.Raise:
                yield return StartCoroutine(Raise(player, action.RaiseAmount));

                // 🔥 RESET ROUND WHEN RAISE
                foreach (var p in players)
                    p.HasActed = false;

                player.HasActed = true;
                break;
        }
    }

    // ==========================================
    // BETTING PHASE
    // ==========================================
    private IEnumerator RunBettingPhase(bool reset = true)
    {
        Debug.Log($"--- BETTING: {currentPhase} ---");

        if (reset)
            bettingManager.ResetRound(players);

        while (!bettingManager.IsRoundFinished(players))
        {
            var player = turnManager.GetCurrent(players);

            if (!player.IsFolded && player.Wallet > 0)
            {
                yield return StartCoroutine(PlayerTurn(player));
                player.HasActed = true;
            }

            turnManager.MoveNext(players);
        }

        Debug.Log($"Pot: {potController.Pot}");
    }

    private void PrepareNextBettingRound()
    {
        bettingManager.ResetRound(players);
        turnManager.SetStartIndex((dealerIndex + 1) % players.Count);
    }

    private bool IsGameOverEarly() => players.Count(p => !p.IsFolded) <= 1;

    // ==========================================
    // ACTIONS
    // ==========================================
    public void Fold(PlayerHand player)
    {
        player.IsFolded = true;
        Debug.Log($"<color=red>{player.name} FOLD</color>");
    }

    public void Check(PlayerHand player)
    {
        Debug.Log($"{player.name} CHECK");
    }

    public IEnumerator Call(PlayerHand player)
    {
        int amount = bettingManager.HighestBet - player.CurrentBet;

        yield return StartCoroutine(ExecuteBet(player, amount));

        Debug.Log($"{player.name} CALL {amount}");
    }

    public IEnumerator Raise(PlayerHand player, int raiseAmount)
    {
        int callAmount = bettingManager.HighestBet - player.CurrentBet;
        int total = callAmount + raiseAmount;

        yield return StartCoroutine(ExecuteBet(player, total));

        bettingManager.SetHighestBet(player.CurrentBet);

        Debug.Log($"{player.name} RAISE → {player.CurrentBet}");
    }

    private IEnumerator ExecuteBet(PlayerHand player, int amount)
    {
        int actualBet = player.RequestMoney(amount);

        player.CurrentBet += actualBet;

        if (actualBet > 0)
        {
            yield return StartCoroutine(
                chipSender.SendRoutine(player.chipController,
                    potController.chipController,
                    actualBet
                )
            );

            potController.AddToPot(actualBet);
        }
    }

    // ==========================================
    // ROUND FLOW
    // ==========================================
    private IEnumerator StartNewRound()
    {
        SetupNewRound();
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(SetupBlinds());
        yield return StartCoroutine(cardDealer.PlayerDraw(players));
    }

    private IEnumerator PreFlopPhase()
    {
        currentPhase = GamePhase.PreFlop;
        yield return StartCoroutine(RunBettingPhase());
    }

    private IEnumerator FlopPhase()
    {
        currentPhase = GamePhase.Flop;
        PrepareNextBettingRound();

        yield return StartCoroutine(cardDealer.DealFlop());
        yield return StartCoroutine(RunBettingPhase());
    }

    private IEnumerator TurnPhase()
    {
        currentPhase = GamePhase.Turn;
        PrepareNextBettingRound();

        yield return StartCoroutine(cardDealer.DealNextCommunityCard(3));
        yield return StartCoroutine(RunBettingPhase());
    }

    private IEnumerator RiverPhase()
    {
        currentPhase = GamePhase.River;
        PrepareNextBettingRound();

        yield return StartCoroutine(cardDealer.DealNextCommunityCard(4));
        yield return StartCoroutine(RunBettingPhase());
    }

    private IEnumerator ShowdownPhase()
    {
        currentPhase = GamePhase.Showdown;

        yield return new WaitForSeconds(1f);

        if (!IsGameOverEarly())
        {
            foreach (var p in players.Where(x => !x.IsFolded))
                p.FlipCard();
        }

        ResolveWinner();

        yield return new WaitForSeconds(3f);
    }

    // ==========================================
    // SETUP
    // ==========================================
    private void SetupNewRound()
    {
        cardDealer.SetUpData(players);
        dealerIndex = GetNextValidPlayerIndex(dealerIndex);

        potController.ClearPot();
        bettingManager.SetHighestBet(0);

        foreach (var p in players)
        {
            p.ResetHand();
            p.SetDealerActive(p == players[dealerIndex]);

            if (p.Wallet <= 0)
                p.IsFolded = true;
        }
    }

    private IEnumerator SetupBlinds()
    {
        int sbIdx = (dealerIndex + 1) % players.Count;
        int bbIdx = (dealerIndex + 2) % players.Count;

        yield return StartCoroutine(ExecuteBet(players[sbIdx], smallBlind));
        yield return StartCoroutine(ExecuteBet(players[bbIdx], bigBlind));

        bettingManager.SetHighestBet(bigBlind);

        turnManager.SetStartIndex((bbIdx + 1) % players.Count);
    }

    private int GetNextValidPlayerIndex(int currentIndex)
    {
        int next = (currentIndex + 1) % players.Count;
        int checks = 0;

        while (players[next].Wallet <= 0 && checks < players.Count)
        {
            next = (next + 1) % players.Count;
            checks++;
        }

        return next;
    }

    private void ResolveWinner()
    {
        var activePlayers = players.Where(p => !p.IsFolded).ToList();

        var results = new Dictionary<PlayerHand, HandResult>();

        // 1. Evaluate tất cả player
        foreach (var p in activePlayers)
        {
            var cards = new List<CardData>();

            cards.AddRange(p.handCards.Select(c => c.data));
            cards.AddRange(cardDealer.CommunityCards());

            results[p] = PokerHandEvaluator.EvaluateHand(cards);
        }

        PlayerHand bestPlayer = activePlayers[0];
        HandResult bestHand = results[bestPlayer];

        foreach (var p in activePlayers)
        {
            var current = results[p];

            if (PokerHandEvaluator.CompareHands(current, bestHand) > 0)
            {
                bestHand = current;
                bestPlayer = p;
            }
        }

        var winners = activePlayers
            .Where(p => PokerHandEvaluator.CompareHands(results[p], bestHand) == 0)
            .ToList();

        int share = potController.Pot / winners.Count;

        foreach (var w in winners)
        {
            w.AddMoney(share);
            Debug.Log($"🏆 {w.name} thắng với {results[w].Rank}");
        }
    }
}