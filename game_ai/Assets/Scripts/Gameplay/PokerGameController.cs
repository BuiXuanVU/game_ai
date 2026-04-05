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

    private List<ActionRecord> currentRoundHistory = new List<ActionRecord>();
    private List<HandHistory> matchHistory = new List<HandHistory>();

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

            if (!IsGameOverEarly())
            {
                yield return StartCoroutine(ShowdownPhase());
            }
            else
            {
                ResolveEarlyWinner();
            }

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
                ai.DecideAction(this,player, bettingManager.HighestBet, (a) => action = a)
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
        currentRoundHistory.Add(new ActionRecord
        {
            phase = currentPhase,
            playerName = player.name,
            action = action.Type,
            amount = (action.Type == PlayerActionType.Raise) ? action.RaiseAmount : 0
        });

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

    private void ResolveEarlyWinner()
    {
        var winner = players.FirstOrDefault(p => !p.IsFolded);

        if (winner != null)
        {
            winner.AddMoney(potController.Pot);
            Debug.Log($"🏆 {winner.name} thắng (mọi người fold)");
            SaveMatchResult(winner.name, potController.Pot);
        }
    }

    private void ResolveWinner()
    {
        var activePlayers = players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count == 0)
        {
            Debug.LogError("ResolveWinner: No active players!");
            return;
        }

        var results = new Dictionary<PlayerHand, HandResult>();
        foreach (var p in activePlayers)
        {
            var cards = new List<CardData>();

            if (p.handCards != null)
            {
                cards.AddRange(
                    p.handCards
                        .Where(c => c != null && c.data != null)
                        .Select(c => c.data)
                );
            }

            var community = cardDealer.CommunityCards();
            if (community != null)
            {
                cards.AddRange(community.Where(c => c != null));
            }

            if (cards.Count < 5)
            {
                Debug.LogError($"Player {p.name} không đủ bài để evaluate ({cards.Count})");
                continue;
            }

            var result = PokerHandEvaluator.EvaluateHand(cards);

            if (result == null)
            {
                Debug.LogError($"EvaluateHand trả null cho {p.name}");
                continue;
            }

            results[p] = result;
        }

        if (results.Count == 0)
        {
            Debug.LogError("ResolveWinner: No valid results!");
            return;
        }

        PlayerHand bestPlayer = results.Keys.First();
        HandResult bestHand = results[bestPlayer];

        foreach (var kvp in results)
        {
            var p = kvp.Key;
            var current = kvp.Value;

            if (PokerHandEvaluator.CompareHands(current, bestHand) > 0)
            {
                bestHand = current;
                bestPlayer = p;
            }
        }

        var winners = results
            .Where(kvp => PokerHandEvaluator.CompareHands(kvp.Value, bestHand) == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        if (winners.Count == 0)
        {
            Debug.LogError("No winners found!");
            return;
        }

        int share = potController.Pot / winners.Count;

        foreach (var w in winners)
        {
            w.AddMoney(share);
            Debug.Log($"🏆 {w.name} thắng với {results[w].Rank}");
            SaveMatchResult(w.name, potController.Pot);
        }

        
    }

    public GameStateSnapshot GetCurrentState(PlayerHand aiPlayer)
    {
        var snapshot = new GameStateSnapshot();
        snapshot.phase = currentPhase;
        snapshot.potSize = potController.Pot;
        snapshot.highestBet = bettingManager.HighestBet;

        snapshot.communityCards = cardDealer.CommunityCards()
            .Select(c => c.ToDTO())
            .ToList();

        snapshot.me = CreatePlayerSnapshot(aiPlayer, true);
        snapshot.opponents = players
            .Where(p => p != aiPlayer)
            .Select(p => CreatePlayerSnapshot(p, false))
            .ToList();

        snapshot.currentRoundHistory = currentRoundHistory;

        return snapshot;
    }

    private PlayerSnapshot CreatePlayerSnapshot(PlayerHand p, bool includeCards)
    {
        return new PlayerSnapshot
        {
            name = p.name,
            stack = p.Wallet,
            currentBet = p.CurrentBet,
            isFolded = p.IsFolded,
            isAllIn = p.Wallet <= 0,
            hand = includeCards ? p.handCards.Select(c => c.data.ToDTO()).ToList() : null
        };
    }

    private void SaveMatchResult(string winnerName, int finalPot)
    {
        HandHistory history = new HandHistory
        {
            roundNumber = matchHistory.Count + 1,
            allActions = new List<ActionRecord>(currentRoundHistory),
            winnerName = winnerName,
            finalPot = finalPot,
            summary = $"Winner: {winnerName} won {finalPot} chips."
        };

        matchHistory.Add(history);

        currentRoundHistory.Clear();
    }
}