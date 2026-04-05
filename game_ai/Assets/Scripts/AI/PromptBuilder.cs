using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PromptBuilder
{
    public static string Generate(LlmDecisionRequest request)
    {
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine("You are a disciplined Texas Hold'em poker agent.");
        prompt.AppendLine("Choose exactly one legal action.");
        prompt.AppendLine($"Phase: {request.phase}");
        prompt.AppendLine($"Pot: {request.potSize}");
        prompt.AppendLine($"HighestBet: {request.highestBet}");
        prompt.AppendLine($"CallAmount: {request.callAmount}");
        prompt.AppendLine($"CanCheck: {request.canCheck}");
        prompt.AppendLine($"CanCall: {request.canCall}");
        prompt.AppendLine($"CanRaise: {request.canRaise}");
        prompt.AppendLine($"RaiseRange: {request.minRaiseAmount}..{request.maxRaiseAmount}");

        if (request.me != null)
        {
            string myCards = request.me.hand != null && request.me.hand.Count > 0
                ? string.Join(", ", request.me.hand.Select(c => c.displayName))
                : "Unknown";

            prompt.AppendLine("[Me]");
            prompt.AppendLine($"{request.me.name} | stack={request.me.stack} | currentBet={request.me.currentBet} | cards=[{myCards}]");
        }

        if (request.communityCards != null && request.communityCards.Count > 0)
        {
            prompt.AppendLine("[Board]");
            prompt.AppendLine(string.Join(", ", request.communityCards.Select(c => c.displayName)));
        }

        if (request.opponents != null && request.opponents.Count > 0)
        {
            prompt.AppendLine("[Opponents]");
            foreach (var opponent in request.opponents)
            {
                prompt.AppendLine($"{opponent.name} | stack={opponent.stack} | bet={opponent.currentBet} | folded={opponent.isFolded} | allIn={opponent.isAllIn}");
            }
        }

        if (request.currentRoundHistory != null && request.currentRoundHistory.Count > 0)
        {
            prompt.AppendLine("[RoundHistory]");
            foreach (var action in request.currentRoundHistory)
            {
                prompt.AppendLine($"{action.phase}: {action.playerName} -> {action.action} ({action.amount})");
            }
        }

        if (request.memory != null && request.memory.Count > 0)
        {
            prompt.AppendLine("[RecentMemory]");
            foreach (var hand in request.memory)
            {
                prompt.AppendLine($"Round {hand.roundNumber} | winner={hand.winnerName} | pot={hand.finalPot} | {hand.summary}");
            }
        }

        prompt.AppendLine("[Output]");
        prompt.AppendLine("Return JSON only: {\"action\":\"Fold|Check|Call|Raise\",\"amount\":0,\"reason\":\"short reason\"}");

        return prompt.ToString();
    }
}
