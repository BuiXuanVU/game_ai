using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PokerGameController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private BettingHandler bettingHandler;
    [SerializeField] private PotController potController;
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private PokerAI ai;

    [Header("Players")]
    [SerializeField] private List<PlayerHand> players;

    [Header("Game Config")]
    [SerializeField] private int smallBlind = 10;
    [SerializeField] private int bigBlind = 20;

    private TurnManager turnManager = new TurnManager();
    private GamePhase currentPhase;
    private List<ActionRecord> currentRoundHistory = new List<ActionRecord>();
    private List<HandHistory> matchHistory = new List<HandHistory>();

    private void Reset()
    {
        roundManager = GetComponent<RoundManager>();
        bettingHandler = GetComponent<BettingHandler>();
        potController = GetComponentInChildren<PotController>();
        cardDealer = GetComponent<CardDealer>();
        ai = GetComponent<PokerAI>();
        players.AddRange(GetComponentsInChildren<PlayerHand>());
    }

    private void Start()
    {
        matchHistory = PokerLocalStore.LoadHandHistory();
        ai?.LoadMemory(matchHistory);
        Debug.Log($"[PokerGameController] Loaded {matchHistory.Count} hands from {PokerLocalStore.HandHistoryPath}");
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            currentRoundHistory.Clear();
            potController.ClearPot();
            cardDealer.SetUpData(players);
            roundManager.SetupNewRound(players);
            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(bettingHandler.SetupBlinds(players, roundManager.DealerIndex, smallBlind, bigBlind));

            yield return StartCoroutine(cardDealer.PlayerDraw(players));

            yield return StartCoroutine(RunPhase(GamePhase.PreFlop));

            if (!IsGameOverEarly()) 
                yield return StartCoroutine(RunPhase(GamePhase.Flop));
            if (!IsGameOverEarly()) 
                yield return StartCoroutine(RunPhase(GamePhase.Turn));
            if (!IsGameOverEarly()) 
                yield return StartCoroutine(RunPhase(GamePhase.River));
            if (!IsGameOverEarly()) 
                yield return StartCoroutine(RunPhase(GamePhase.Showdown));
            else ResolveEarlyWinner();

            yield return new WaitForSeconds(3f);
        }
    }

    private IEnumerator RunPhase(GamePhase phase)
    {
        currentPhase = phase;
        if (phase == GamePhase.Showdown)
        {
            yield return new WaitForSeconds(1f);
            foreach (var p in players.Where(x => !x.IsFolded)) p.FlipCard();
            ResolveWinner();
            yield return new WaitForSeconds(3f);
        }
        else
        {
            PrepareNextBettingRound(phase);
            if (phase == GamePhase.Flop) yield return StartCoroutine(cardDealer.DealFlop());
            if (phase == GamePhase.Turn) yield return StartCoroutine(cardDealer.DealNextCommunityCard(3));
            if (phase == GamePhase.River) yield return StartCoroutine(cardDealer.DealNextCommunityCard(4));
            yield return StartCoroutine(bettingHandler.RunBettingPhase(players, currentPhase, turnManager, ai, currentRoundHistory));
        }
    }

    private void PrepareNextBettingRound(GamePhase phase)
    {
        if (phase == GamePhase.PreFlop)
        {
            turnManager.SetStartIndex(GetNextActivePlayerIndex((roundManager.DealerIndex + (players.Count > 2 ? 3 : 1)) % players.Count));
            return;
        }

        bettingHandler.ResetRound(players);
        turnManager.SetStartIndex(GetNextActivePlayerIndex((roundManager.DealerIndex + 1) % players.Count));
    }

    private bool IsGameOverEarly() => players.Count(p => !p.IsFolded) <= 1;

    private int GetNextActivePlayerIndex(int startIndex)
    {
        if (players.Count == 0) return 0;

        int index = startIndex;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[index];
            if (!player.IsFolded && player.Wallet > 0)
                return index;

            index = (index + 1) % players.Count;
        }

        return startIndex;
    }

    private void ResolveEarlyWinner()
    {
        var winner = players.FirstOrDefault(p => !p.IsFolded);
        if (winner != null)
        {
            int totalPot = potController.Pot;
            winner.AddMoney(totalPot);
            Debug.Log($"🏆 {winner.name} thắng (mọi người fold)");
            SaveMatchResult(new List<PlayerHand> { winner }, totalPot);
            potController.ClearPot();
        }
    }

    private void ResolveWinner()
    {
        var activePlayers = players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count == 0) { Debug.LogError("No active players!"); return; }

        var results = new Dictionary<PlayerHand, HandResult>();
        foreach (var p in activePlayers)
        {
            var cards = new List<CardData>();
            if (p.handCards != null) cards.AddRange(p.handCards.Where(c => c != null && c.data != null).Select(c => c.data));
            var community = cardDealer.CommunityCards();
            if (community != null) cards.AddRange(community.Where(c => c != null));
            if (cards.Count < 5) continue;
            var result = PokerHandEvaluator.EvaluateHand(cards);
            if (result != null) results[p] = result;
        }

        if (results.Count == 0) { Debug.LogError("No valid results!"); return; }

        HandResult bestHand = results.First().Value;

        foreach (var kvp in results)
        {
            if (PokerHandEvaluator.CompareHands(kvp.Value, bestHand) > 0)
            {
                bestHand = kvp.Value;
            }
        }

        var winners = activePlayers
            .Where(player => results.ContainsKey(player) && PokerHandEvaluator.CompareHands(results[player], bestHand) == 0)
            .ToList();
        int totalPot = potController.Pot;
        int share = totalPot / winners.Count;
        int remainder = totalPot % winners.Count;

        for (int i = 0; i < winners.Count; i++)
        {
            var w = winners[i];
            int payout = share + (i < remainder ? 1 : 0);

            w.AddMoney(payout);
            Debug.Log($"🏆 {w.name} thắng với {results[w].Rank}");
        }

        SaveMatchResult(winners, totalPot);
        potController.ClearPot();
    }

    private void SaveMatchResult(List<PlayerHand> winners, int finalPot)
    {
        string winnerNames = string.Join(", ", winners.Select(w => w.name));
        HandHistory handHistory = new HandHistory
        {
            roundNumber = matchHistory.Count + 1,
            allActions = new List<ActionRecord>(currentRoundHistory),
            winnerName = winnerNames,
            finalPot = finalPot,
            summary = winners.Count == 1
                ? $"Winner: {winnerNames} won {finalPot} chips."
                : $"Split pot: {winnerNames} shared {finalPot} chips."
        };
        matchHistory.Add(handHistory);
        PokerLocalStore.SaveHandHistory(matchHistory);
        ai?.LearnFromRound(handHistory);
        currentRoundHistory.Clear();
    }

    public GameStateSnapshot GetCurrentState(PlayerHand aiPlayer)
    {
        var snapshot = new GameStateSnapshot
        {
            phase = currentPhase,
            potSize = potController.Pot,
            highestBet = bettingHandler.HighestBet,
            communityCards = cardDealer.CommunityCards().Select(c => c.ToDTO()).ToList(),
            me = CreatePlayerSnapshot(aiPlayer, true),
            opponents = players.Where(p => p != aiPlayer).Select(p => CreatePlayerSnapshot(p, false)).ToList(),
            currentRoundHistory = currentRoundHistory
        };

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
            hand = includeCards
                ? p.handCards
                    .Where(c => c != null && c.data != null)
                    .Select(c => c.data.ToDTO())
                    .ToList()
                : null
        };
    }
}
