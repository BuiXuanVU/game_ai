CARDS_CONFIG = {
    "Attack": {"cost": 2, "value": 4},
    "StrongAttack": {"cost": 5, "value": 9},
    "Defend": {"cost": 2, "value": 3},
    "StrongDefend":  {"cost": 4, "value": 7},
    "Heal":  {"cost": 3, "value": 5},
    "StaminaUp": {"cost": 0, "value": 2}
}

def generate_ai_prompt(player_hp, ai_hp, ai_energy, hand, history, memory):
    prompt = f"""
    [LUẬT CHƠI]
    - Thể lực tối đa: 10.
    - Bạn đang có {ai_energy} Thể lực.
    - Bài trên tay: {hand} (Chi tiết: { {k: CARDS_CONFIG[k] for k in hand} })
    
    [BỐI CẢNH]
    - AI HP: {ai_hp}, Người chơi HP: {player_hp}
    - Ký ức về người chơi: {memory}
    
    [NHIỆM VỤ]
    Dựa trên việc người chơi có xu hướng tấn công hay phòng thủ ở lượt này, hãy chọn các lá bài tối ưu. 
    Tổng Cost không được vượt quá {ai_energy} (Lưu ý: Surge hồi thêm 2 Thể lực).
    
    Trả về JSON: {{"play": ["Card1", "Card2"], "thought": "Tại sao chọn vậy?"}}
    """
    return prompt