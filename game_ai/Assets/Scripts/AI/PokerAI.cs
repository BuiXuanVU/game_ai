using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PokerAI : MonoBehaviour
{
    // Danh sách ký ức của riêng AI
    private List<HandHistory> memory = new List<HandHistory>();

    // Coroutine quyết định hành động
    public IEnumerator DecideActionCoroutine(BettingHandler bettingHandler, PlayerHand aiPlayer, int highestBet, GamePhase phase, List<ActionRecord> roundHistory, Action<PlayerAction> callback)
    {
        // 1. Chụp ảnh trạng thái từ GameController
        GameStateSnapshot snapshot = bettingHandler.GetComponent<PokerGameController>().GetCurrentState(aiPlayer);

        // 2. Xây dựng prompt cho AI
        string prompt = PromptBuilder.Generate(snapshot, memory);
        Debug.Log($"<color=yellow>[AI Prompt Generated]</color>\n{prompt}");

        // 3. Giả lập thời gian suy nghĩ của AI
        yield return new WaitForSeconds(1f);

        // 4. Giả lập hành động AI (ở đây chỉ Call hoặc Check đơn giản)
        PlayerAction resultAction;
        if (aiPlayer.CurrentBet < highestBet)
            resultAction = new PlayerAction(PlayerActionType.Call);
        else
            resultAction = new PlayerAction(PlayerActionType.Check);

        // 5. Trả hành động cho BettingHandler
        callback?.Invoke(resultAction);
    }

    // Hàm để Controller "dạy" AI sau mỗi ván
    public void LearnFromRound(HandHistory history)
    {
        memory.Add(history);
        if (memory.Count > 10) memory.RemoveAt(0); // Giới hạn bộ nhớ 10 ván gần nhất
    }
}