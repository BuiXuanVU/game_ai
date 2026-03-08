from openai import OpenAI
import json
import database as NPC_DATABASE

client = OpenAI(
    base_url="http://127.0.0.1:1234/v1", 
    api_key="lm-studio"
)

npc_role = """
[IDENTITY]
Name: Lão Nam (70 tuổi).
Setting: Làng cổ Việt Nam, thế kỷ 17.
Language: Tiếng Việt thời xưa. Tuyệt đối không dùng: "mình", "cảm ơn", "vấn đề", "hỗ trợ", "village".
Keywords: Phải dùng: "Dạ bẩm", "Quan lớn", "Lão", "Tiện dân", "Khẩn khoản", "Đại xá".

[WORLD DATA - CHỈ ĐƯỢC DÙNG CÁC GIÁ TRỊ NÀY]
Animations: CUI_CHAO, DI_BO, QUY_LAY, CHI_DUONG, LO_LANG.
Items: GAO, RUOU, CUOC, HAT_GIONG.
Locations: DAU_LANG, DINH_LANG, RUONG_LUA, NHA_LAO_NAM.
Global Events: TRO_NANG, TROI_MUA, SAU_BENH.

[LOGIC RULES]
Thought: Phải phân tích thái độ của Quan lớn trước khi nói.
Speech: Ngắn gọn (dưới 15 từ). Không nói "Chào mừng", hãy nói "Bái kiến".
Consistency: Nếu Quan lớn ra lệnh đi đâu, animation phải là DI_BO và parameter là tên địa điểm.

[STRICT OUTPUT FORMAT] Trả về JSON duy nhất. Không văn bản thừa. Cấu trúc:
{
  "thought": "Suy nghĩ logic",
  "speech": "Lời thoại NPC",
  "action": "ANIMATION_NAME",
  "cmd": "COMMAND_NAME",
  "param": "VALUE_STRING"
}
"""

def chat_with_ai(prompt):
    try:
        completion = client.chat.completions.create(
            model="meta-llama-3.1-8b-instruct", 
            messages=[
                {"role": "system", "content": npc_role},
                {"role": "user", "content": prompt}
            ],
            temperature=0.7,
        )
        return completion.choices[0].message.content
    except Exception as e:
        return f"Lỗi kết nối: {str(e)}"

user_input = "Ta là quan huyện đi ngang qua đây, Nhà ngươi có gì ngon mang ra đây"

print(f"User: {user_input}")
print(f"AI: {chat_with_ai(user_input)}")

import json

class GameNPC:
    def __init__(self, npc_id):
        info = NPC_DATABASE.get(npc_id)
        self.name = info["name"]
        # Tạo System Prompt động
        self.system_prompt = f"""
        [IDENTITY]
        Name: {info['name']}
        Setting: {info['identity']}
        Language: Tuyệt đối không dùng: {info['forbidden']}.
        Keywords: Phải dùng: {info['keywords']}.

        [WORLD DATA]
        Animations: CUI_CHAO, DI_BO, QUY_LAY, CHI_DUONG.
        Locations: DAU_LANG, DINH_LANG, RUONG_LUA.

        [STRICT OUTPUT FORMAT] 
        Trả về JSON duy nhất:
        {{
          "thought": "Suy nghĩ",
          "speech": "Lời thoại",
          "action": "ANIMATION",
          "param": "VALUE"
        }}
        """

    def get_response(self, user_text):
        completion = client.chat.completions.create(
            model="meta-llama-3.1-8b-instruct",
            messages=[
                {"role": "system", "content": self.system_prompt},
                {"role": "user", "content": user_text}
            ],
            temperature=0.3
        )
        return json.loads(completion.choices[0].message.content)