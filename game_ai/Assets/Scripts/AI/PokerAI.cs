using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PokerAI
{
    // Danh sách ký ức của riêng AI
    private List<HandHistory> memory = new List<HandHistory>();

    public IEnumerator DecideAction(PokerGameController controller,PlayerHand aiPlayer, int highestBet, Action<PlayerAction> callback)
    {
        // 2. Chụp ảnh trạng thái
        GameStateSnapshot snapshot = controller.GetCurrentState(aiPlayer);

        // 3. Xây dựng Prompt
        string prompt = PromptBuilder.Generate(snapshot, memory);

        Debug.Log($"<color=yellow>[AI Prompt Generated]</color>\n{prompt}");

        // 4. (TƯƠNG LAI) Gửi prompt tới API và đợi phản hồi
        // Hiện tại ta giả lập một kết quả JSON từ LLM
        yield return new WaitForSeconds(2f); // Giả lập thời gian suy nghĩ

        // Giả sử LLM trả về: {"action": "Call", "amount": 0, "reason": "I have a pair of Jacks and the pot is worth it."}
        PlayerAction resultAction = new PlayerAction(PlayerActionType.Call);

        // Cập nhật ký ức sau khi ván đấu kết thúc (sẽ được controller gọi)
        callback?.Invoke(resultAction);
    }

    // Hàm để Controller "dạy" AI sau mỗi ván
    public void LearnFromRound(HandHistory history)
    {
        memory.Add(history);
        if (memory.Count > 10) memory.RemoveAt(0); // Giới hạn bộ nhớ 10 ván gần nhất
    }
}