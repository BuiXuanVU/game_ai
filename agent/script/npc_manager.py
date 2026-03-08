import 
class NpcManager:
    npc_1 = GameNPC("lao_nam")
    res1 = npc_1.get_response("Lão đang làm gì đó?")
    print(f"Lão Nam: {res1['speech']}")

    # Tạo đối tượng Cô Thắm
    npc_2 = GameNPC("co_thắm")
    res2 = npc_2.get_response("Chào cô nương!")
    print(f"Cô Thắm: {res2['speech']}")