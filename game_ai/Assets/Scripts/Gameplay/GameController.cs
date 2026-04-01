using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum GamePhase { PreFlop, Flop, Turn, River, Showdown }

public class PokerGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private PotController potController;
    [SerializeField] private List<PlayerHand> players;

    [Header("Game Config")]
    [SerializeField] private int smallBlind = 10;
    [SerializeField] private int bigBlind = 20;

    private int dealerIndex = -1;
    private int currentPlayerIndex;
    private int highestBet;

    private GamePhase currentPhase;

    private void Reset()
    {
        cardDealer = GetComponentInChildren<CardDealer>();
        potController = GetComponentInChildren<PotController>();
        players.AddRange(GetComponentsInChildren<PlayerHand>());
    }


    private void Start() => StartCoroutine(GameLoop());

    // ==========================================
    // 1. (MASTER LOOP)
    // ==========================================
    private IEnumerator GameLoop()
    {
        while (true)
        {
            SetupNewRound();
            yield return new WaitForSeconds(1f);

            // PRE-FLOP
            currentPhase = GamePhase.PreFlop;
            SetupBlinds();
            yield return StartCoroutine(cardDealer.PlayerDraw(players));
            yield return StartCoroutine(RunBettingPhase());

            // FLOP
            if (!IsGameOverEarly())
            {
                currentPhase = GamePhase.Flop;
                PrepareNextBettingRound();
                yield return StartCoroutine(cardDealer.DealFlop());
                yield return StartCoroutine(RunBettingPhase());
            }

            // TURN
            if (!IsGameOverEarly())
            {
                currentPhase = GamePhase.Turn;
                PrepareNextBettingRound();
                yield return StartCoroutine(cardDealer.DealNextCommunityCard(3));
                yield return StartCoroutine(RunBettingPhase());
            }

            // RIVER
            if (!IsGameOverEarly())
            {
                currentPhase = GamePhase.River;
                PrepareNextBettingRound();
                yield return StartCoroutine(cardDealer.DealNextCommunityCard(4));
                yield return StartCoroutine(RunBettingPhase());
            }

            // SHOWDOWN
            currentPhase = GamePhase.Showdown;
            if (!IsGameOverEarly())
            {
                foreach (var p in players.Where(x => !x.IsFolded)) p.FlipCard();
            }
            ResolveWinner();

            yield return new WaitForSeconds(5f);
        }
    }

    // ==========================================
    // 2. LOGIC
    // ==========================================
    private IEnumerator RunBettingPhase()
    {
        Debug.Log($"--- BẮT ĐẦU VÒNG CƯỢC: {currentPhase} ---");

        foreach (var p in players) p.HasActed = false;

        while (!IsBettingRoundFinished())
        {
            PlayerHand p = players[currentPlayerIndex];
            if (!p.IsFolded && p.Wallet > 0)
            {
                yield return StartCoroutine(PlayerTurn(p));
                p.HasActed = true;
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        }
        EndBettingRound();
    }

    private bool IsBettingRoundFinished()
    {
        var activePlayers = players.Where(p => !p.IsFolded).ToList();
        if (activePlayers.Count <= 1) return true;
        return activePlayers.All(p => p.HasActed && (p.CurrentBet == highestBet || p.Wallet == 0));
    }

    private void PrepareNextBettingRound()
    {
        highestBet = 0;
        foreach (var p in players)
        {
            p.CurrentBet = 0;
            p.HasActed = false;
        }
        currentPlayerIndex = (dealerIndex + 1) % players.Count;
    }

    private void EndBettingRound()
    {
        Debug.Log($"Vòng cược kết thúc. Tổng Pot: {potController.Pot}");
    }

    private bool IsGameOverEarly() => players.Count(p => !p.IsFolded) <= 1;

    // ==========================================
    // 3. PLAYER
    // ==========================================
    private IEnumerator PlayerTurn(PlayerHand player)
    {
        yield return new WaitForSeconds(0.5f);

        if (player.CurrentBet < highestBet)
        {
            int rand = Random.Range(0, 10);
            if (rand < 2) Fold(player);
            else if (rand < 8) Call(player);
            else Raise(player, 50);
        }
        else
        {
            int rand = Random.Range(0, 10);
            if (rand < 7) Check(player);
            else Raise(player, 50);
        }
    }

    public void Fold(PlayerHand player)
    {
        player.IsFolded = true;
        Debug.Log($"<color=red>{player.name} FOLD</color>");
    }

    public void Check(PlayerHand player)
    {
        Debug.Log($"{player.name} CHECK");
    }

    public void Call(PlayerHand player)
    {
        int amountNeeded = highestBet - player.CurrentBet;
        ExecuteBet(player, amountNeeded);
        Debug.Log($"{player.name} CALL {amountNeeded}");
    }

    public void Raise(PlayerHand player, int raiseAmount)
    {
        int callAmount = highestBet - player.CurrentBet;
        int totalToSub = callAmount + raiseAmount;

        ExecuteBet(player, totalToSub);
        highestBet = player.CurrentBet;
        Debug.Log($"{player.name} RAISE lên {highestBet}");
    }

    private void ExecuteBet(PlayerHand player, int amount)
    {
        int actualBet = player.RequestMoney(amount);

        player.CurrentBet += actualBet;
        potController.AddToPot(actualBet);
    }

    // ==========================================
    // 4. SETUP VÀ KẾT QUẢ
    // ==========================================
    private void SetupNewRound()
    {
        cardDealer.SetUpData(players);
        dealerIndex = GetNextValidPlayerIndex(dealerIndex);

        potController.ClearPot();
        highestBet = 0;

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            p.SetDealerActive(i == dealerIndex);
            p.ResetHand();
            if (p.Wallet <= 0) p.IsFolded = true;
        }
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

    private void SetupBlinds()
    {
        int sbIdx = (dealerIndex + 1) % players.Count;
        int bbIdx = (dealerIndex + 2) % players.Count;

        ExecuteBet(players[sbIdx], smallBlind);
        ExecuteBet(players[bbIdx], bigBlind);
        highestBet = bigBlind;

        currentPlayerIndex = (bbIdx + 1) % players.Count;
    }

    private void ResolveWinner()
    {
        var winners = players.Where(p => !p.IsFolded).ToList();
        if (winners.Count == 0) return;

        int share = potController.Pot / winners.Count;
        foreach (var w in winners)
        {
            w.AddMoney(share);
            Debug.Log($"Winner: {w.name} nhận {share}");
        }
    }
}