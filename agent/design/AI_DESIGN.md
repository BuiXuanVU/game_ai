# AI DESIGN
## 1. Design Principle
AI toàn quyền quyết định hành vi NPC.  
Unity gameplay layer chỉ gửi trạng thái và thực thi command từ AI.

## 2. Architecture
Pipeline chuẩn cho mỗi NPC:

Perception -> Decision Brain -> Command -> Actuator -> Logger

1. Perception
   - Thu thập trạng thái ván hiện tại:
     - hand hiện tại của NPC
     - dealer up-card
     - số người còn trong ván
     - cược hiện tại
     - lịch sử thắng/thua gần đây
     - chỉ số quan hệ với Human (Respect/Trust/Rivalry)
2. Decision Brain
   - Tính toán xác suất/utility.
   - Trả hành động hợp lệ theo phase.
3. Command
   - Enum command:
     - BetSmall
     - BetMedium
     - BetLarge
     - Hit
     - Stand
4. Actuator
   - Thực thi command trong Unity.
   - Validate command theo phase để tránh lỗi.
5. Logger
   - Ghi lại context + action + reason + outcome.

## 3. AI Control Policy
NPC dùng policy lai:
1. Rule constraints
   - điểm <= 11: luôn Hit
   - điểm >= 17: ưu tiên Stand
2. Utility scoring cho vùng 12-16
   - hitUtility = f(riskTolerance, discipline, tilt, dealerThreat, potPressure)
   - standUtility = f(safetyBias, discipline, currentScoreValue)
   - chọn action có utility cao hơn
3. Bet policy
   - Quyết định BetSmall/Medium/Large theo:
     - personality
     - bankroll
     - chuỗi thắng/thua
     - rivalry với người chơi

## 4. NPC Personality Model
Mỗi NPC có profile:

- riskTolerance (0..1)
- discipline (0..1)
- aggression (0..1)
- tiltSensitivity (0..1)
- attitudeToHuman (-1..1)

Ảnh hưởng:
1. riskTolerance cao -> ưu tiên Hit ở vùng điểm trung bình.
2. discipline cao -> ít hành vi cảm tính.
3. tiltSensitivity cao -> dễ thay đổi chiến thuật khi thua liên tiếp.
4. attitudeToHuman ảnh hưởng mức cược khi đối đầu người chơi.

## 5. Adaptive Mechanism
AI cập nhật ngắn hạn theo lịch sử:
1. Nếu thua liên tiếp:
   - tăng hoặc giảm risk tùy personality.
2. Nếu người chơi hay cược lớn:
   - NPC aggressive tăng tần suất BetLarge.
3. Nếu người chơi quá an toàn:
   - NPC rivalry cao tăng áp lực bằng cược lớn.

Update giới hạn bởi clamp để tránh hành vi cực đoan.

## 6. Explainability
Mỗi quyết định lưu reason text ngắn, ví dụ:
- "Hit: score=14, dealerThreat=high, hitUtility=0.62 > standUtility=0.48"
- "Stand: score=17, bustRiskHigh"

Thông tin này hiển thị trong AI debug panel và lưu vào stats.

## 7. Data Contracts
## 7.1 NPCProfile
- id
- displayName
- riskTolerance
- discipline
- aggression
- tiltSensitivity
- attitudeToHuman
- respect
- trust
- rivalry

## 7.2 RoundContext
- phase
- npcSeat
- npcHandValue
- npcSoftHand
- dealerUpCardValue
- betAmount
- npcBankroll
- recentResults
- tableAliveCount

## 7.3 AICommand
- actionType (BetSmall/BetMedium/BetLarge/Hit/Stand)
- confidence (0..1)
- reason (string)

## 8. Evaluation Plan
Chạy mô phỏng tối thiểu 1000 ván:
1. Win rate mỗi NPC.
2. Bust rate mỗi NPC.
3. Average bet size.
4. Average return per round.
5. Tần suất Hit/Stand theo score band (<=11, 12-16, >=17).
6. Mức độ khác biệt hành vi giữa 3 NPC.

## 9. Risks and Mitigation
1. Risk: AI quá random.
   - Mitigation: utility + constraints + seed control.
2. Risk: NPC hành vi giống nhau.
   - Mitigation: tách personality mạnh hơn và benchmark score band.
3. Risk: khó debug.
   - Mitigation: bắt buộc log reason cho mọi command.
