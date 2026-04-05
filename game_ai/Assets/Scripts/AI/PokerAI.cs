using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

public class PokerAI : MonoBehaviour
{
    [Header("LLM Agent")]
    [SerializeField] private string agentUrl = "http://127.0.0.1:8000/v1/decide";
    [SerializeField] private int timeoutSeconds = 20;
    [SerializeField] private int memoryWindow = 5;
    [SerializeField] private bool logPromptPreview = true;
    [SerializeField] private bool logAgentResponse = true;

    private readonly List<HandHistory> memory = new List<HandHistory>();

    public IEnumerator DecideActionCoroutine(BettingHandler bettingHandler, PlayerHand aiPlayer, int highestBet, GamePhase phase, List<ActionRecord> roundHistory, Action<PlayerAction> callback)
    {
        PokerGameController controller = bettingHandler.GetComponent<PokerGameController>();
        GameStateSnapshot snapshot = controller.GetCurrentState(aiPlayer);
        LlmDecisionRequest request = BuildRequest(snapshot, aiPlayer);
        request.promptPreview = PromptBuilder.Generate(request);

        if (logPromptPreview)
            Debug.Log($"<color=yellow>[AI Prompt Preview]</color>\n{request.promptPreview}");

        string payload = JsonUtility.ToJson(request);
        using UnityWebRequest webRequest = new UnityWebRequest(agentUrl, UnityWebRequest.kHttpVerbPOST);
        webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.timeout = timeoutSeconds;
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        PlayerAction fallbackAction = BuildFallbackAction(request);
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[PokerAI] Agent request failed: {webRequest.error}. Using fallback action.");
            callback?.Invoke(fallbackAction);
            yield break;
        }

        LlmDecisionResponse response = ParseResponse(webRequest.downloadHandler.text);
        if (response == null)
        {
            Debug.LogWarning("[PokerAI] Agent returned invalid JSON. Using fallback action.");
            callback?.Invoke(fallbackAction);
            yield break;
        }

        if (logAgentResponse)
            Debug.Log($"<color=cyan>[AI Agent Response]</color> {webRequest.downloadHandler.text}");

        callback?.Invoke(ConvertToPlayerAction(request, response));
    }

    public void LearnFromRound(HandHistory history)
    {
        memory.Add(history);
        if (memory.Count > 10)
            memory.RemoveAt(0);
    }

    private LlmDecisionRequest BuildRequest(GameStateSnapshot snapshot, PlayerHand aiPlayer)
    {
        int callAmount = Mathf.Max(0, snapshot.highestBet - aiPlayer.CurrentBet);
        int maxRaiseAmount = Mathf.Max(0, aiPlayer.Wallet - callAmount);
        bool canCheck = callAmount == 0;
        bool canCall = callAmount > 0 && aiPlayer.Wallet > 0;
        bool canRaise = maxRaiseAmount > 0;

        return new LlmDecisionRequest
        {
            phase = snapshot.phase.ToString(),
            potSize = snapshot.potSize,
            highestBet = snapshot.highestBet,
            callAmount = callAmount,
            minRaiseAmount = canRaise ? 1 : 0,
            maxRaiseAmount = canRaise ? maxRaiseAmount : 0,
            canFold = true,
            canCheck = canCheck,
            canCall = canCall,
            canRaise = canRaise,
            me = CreatePlayerState(snapshot.me),
            opponents = snapshot.opponents.Select(CreatePlayerState).ToList(),
            communityCards = snapshot.communityCards ?? new List<CardDataDTO>(),
            currentRoundHistory = (snapshot.currentRoundHistory ?? new List<ActionRecord>())
                .Select(record => new LlmActionRecord
                {
                    phase = record.phase.ToString(),
                    playerName = record.playerName,
                    action = record.action.ToString(),
                    amount = record.amount
                })
                .ToList(),
            memory = memory
                .Skip(Mathf.Max(0, memory.Count - memoryWindow))
                .Select(hand => new LlmHandMemory
                {
                    roundNumber = hand.roundNumber,
                    winnerName = hand.winnerName,
                    finalPot = hand.finalPot,
                    summary = hand.summary
                })
                .ToList()
        };
    }

    private LlmPlayerState CreatePlayerState(PlayerSnapshot snapshot)
    {
        return new LlmPlayerState
        {
            name = snapshot.name,
            stack = snapshot.stack,
            currentBet = snapshot.currentBet,
            isFolded = snapshot.isFolded,
            isAllIn = snapshot.isAllIn,
            hand = snapshot.hand ?? new List<CardDataDTO>()
        };
    }

    private LlmDecisionResponse ParseResponse(string json)
    {
        try
        {
            return JsonUtility.FromJson<LlmDecisionResponse>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PokerAI] Failed to parse agent response: {ex.Message}");
            return null;
        }
    }

    private PlayerAction ConvertToPlayerAction(LlmDecisionRequest request, LlmDecisionResponse response)
    {
        if (response == null || string.IsNullOrWhiteSpace(response.action))
            return BuildFallbackAction(request);

        string action = response.action.Trim().ToLowerInvariant();
        switch (action)
        {
            case "fold":
                return new PlayerAction(PlayerActionType.Fold);
            case "check":
                return request.canCheck ? new PlayerAction(PlayerActionType.Check) : BuildFallbackAction(request);
            case "call":
                return request.canCall ? new PlayerAction(PlayerActionType.Call) : BuildFallbackAction(request);
            case "raise":
                if (!request.canRaise)
                    return BuildFallbackAction(request);

                int clampedRaise = Mathf.Clamp(response.amount, request.minRaiseAmount, request.maxRaiseAmount);
                return new PlayerAction(PlayerActionType.Raise, clampedRaise);
            default:
                return BuildFallbackAction(request);
        }
    }

    private PlayerAction BuildFallbackAction(LlmDecisionRequest request)
    {
        if (request.canCheck)
            return new PlayerAction(PlayerActionType.Check);

        if (request.canCall)
            return new PlayerAction(PlayerActionType.Call);

        return new PlayerAction(PlayerActionType.Fold);
    }
}
