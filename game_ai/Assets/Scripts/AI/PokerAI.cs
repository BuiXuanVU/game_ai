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

    private void Awake()
    {
        Debug.Log($"[PokerAI] Local data path: {PokerLocalStore.RootPath}");
    }

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
            RecordDecisionAudit(request, null, fallbackAction, true, webRequest.error);
            callback?.Invoke(fallbackAction);
            yield break;
        }

        LlmDecisionResponse response = ParseResponse(webRequest.downloadHandler.text);
        if (response == null)
        {
            Debug.LogWarning("[PokerAI] Agent returned invalid JSON. Using fallback action.");
            RecordDecisionAudit(request, null, fallbackAction, true, "Invalid JSON response from local agent.");
            callback?.Invoke(fallbackAction);
            yield break;
        }

        if (logAgentResponse)
            Debug.Log($"<color=cyan>[AI Agent Response]</color> {webRequest.downloadHandler.text}");

        PlayerAction finalAction = ConvertToPlayerAction(request, response);
        bool usedFallback = IsFallbackDecision(request, response, finalAction);
        RecordDecisionAudit(request, response, finalAction, usedFallback, null);
        callback?.Invoke(finalAction);
    }

    public void LearnFromRound(HandHistory history)
    {
        memory.Add(history);
        if (memory.Count > memoryWindow)
            memory.RemoveAt(0);
    }

    public void LoadMemory(IEnumerable<HandHistory> history)
    {
        memory.Clear();
        if (history == null)
            return;

        foreach (HandHistory hand in history.Skip(Mathf.Max(0, history.Count() - memoryWindow)))
        {
            memory.Add(hand);
        }
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

    private bool IsFallbackDecision(LlmDecisionRequest request, LlmDecisionResponse response, PlayerAction finalAction)
    {
        if (response == null || string.IsNullOrWhiteSpace(response.action))
            return true;

        string requestedAction = response.action.Trim().ToLowerInvariant();
        string finalActionName = finalAction.Type.ToString().ToLowerInvariant();
        if (requestedAction != finalActionName)
            return true;

        if (finalAction.Type == PlayerActionType.Raise)
            return response.amount != finalAction.RaiseAmount;

        return false;
    }

    private void RecordDecisionAudit(LlmDecisionRequest request, LlmDecisionResponse response, PlayerAction finalAction, bool usedFallback, string error)
    {
        PokerLocalStore.AppendDecisionAudit(new DecisionAuditEntry
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            playerName = request.me?.name,
            phase = request.phase,
            potSize = request.potSize,
            callAmount = request.callAmount,
            usedFallback = usedFallback,
            chosenAction = finalAction.Type.ToString(),
            chosenAmount = finalAction.Type == PlayerActionType.Raise ? finalAction.RaiseAmount : 0,
            reason = response?.reason ?? string.Empty,
            error = error ?? string.Empty,
            rawResponse = response?.rawResponse ?? string.Empty,
            promptPreview = request.promptPreview ?? string.Empty
        });
    }
}
