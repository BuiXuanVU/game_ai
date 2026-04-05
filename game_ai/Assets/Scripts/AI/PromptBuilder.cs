using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PromptBuilder
{
    public static string Generate(GameStateSnapshot state, List<HandHistory> memory)
    {
        StringBuilder prompt = new StringBuilder();

        // 1. Vai trò và bối cảnh
        prompt.AppendLine("You are a professional Poker AI player in a Texas Hold'em game.");
        prompt.AppendLine($"Current Game Phase: {state.phase}");
        prompt.AppendLine($"Pot Size: {state.potSize} chips. Current Highest Bet to call: {state.highestBet} chips.");

        // 2. Thông tin bài của AI
        string myCards = string.Join(", ", state.me.hand.Select(c => c.displayName));
        prompt.AppendLine($"--- YOUR INFO ---");
        prompt.AppendLine($"Your Name: {state.me.name}");
        prompt.AppendLine($"Your Cards: [{myCards}]");
        prompt.AppendLine($"Your Stack: {state.me.stack} chips. Your Bet this round: {state.me.currentBet}.");

        // 3. Thông tin bài chung
        if (state.communityCards.Count > 0)
        {
            string community = string.Join(", ", state.communityCards.Select(c => c.displayName));
            prompt.AppendLine($"Community Cards on table: [{community}]");
        }

        // 4. Thông tin đối thủ
        prompt.AppendLine("--- OPPONENTS ---");
        foreach (var opp in state.opponents)
        {
            string status = opp.isFolded ? "Folded" : (opp.isAllIn ? "All-In" : "Active");
            prompt.AppendLine($"- {opp.name}: Stack {opp.stack}, Bet {opp.currentBet}, Status: {status}");
        }

        // 5. Diễn biến vòng cược hiện tại (Giúp AI nhận diện độ hung hãn của đối thủ)
        prompt.AppendLine("--- ROUND HISTORY ---");
        if (state.currentRoundHistory.Count > 0)
        {
            foreach (var record in state.currentRoundHistory)
                prompt.AppendLine($"{record.playerName} performed {record.action} ({record.amount} chips)");
        }
        else prompt.AppendLine("No actions yet this round.");

        // 6. TRÍ NHỚ (Học từ các ván trước)
        prompt.AppendLine("--- MEMORY (Past Rounds) ---");
        if (memory.Count > 0)
        {
            // Chỉ gửi 3 ván gần nhất để tránh quá tải Token
            var recentMemory = memory.Skip(Mathf.Max(0, memory.Count - 3)).ToList();
            foreach (var h in recentMemory)
                prompt.AppendLine($"- Round {h.roundNumber}: {h.summary}. Winner: {h.winnerName}");
        }
        else prompt.AppendLine("First round. No memory yet.");

        // 7. Yêu cầu đầu ra (Output format)
        prompt.AppendLine("--- TASK ---");
        prompt.AppendLine("Analyze the board, your hand strength, and opponents' behavior.");
        prompt.AppendLine("Respond ONLY in JSON format with: 'action' (Fold, Check, Call, Raise), 'amount' (if raising), and 'reason' (briefly explain why).");

        return prompt.ToString();
    }
}