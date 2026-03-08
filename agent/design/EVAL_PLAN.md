# AI EVALUATION PLAN (External LLM/API)

## 1. Mục tiêu
Đánh giá chất lượng NPC AI khi dùng mô hình bên ngoài (LLM/GPT API) trong game blackjack 4 người chơi.

## 2. Cấu hình test
1. Bàn 4 người: 1 Human + 3 NPC AI.
2. Luật quốc tế:
   - Blackjack 3:2
   - Dealer đứng soft 17 (S17)
   - Push khi bằng điểm
   - Không insurance/surrender
3. Số ván:
   - Quick test: 100 ván
   - Main test: 500 ván

## 3. Chỉ số bắt buộc
1. Win rate mỗi NPC
2. Bust rate mỗi NPC
3. Average bet mỗi NPC
4. Latency trung bình mỗi quyết định AI (ms)
5. Tỉ lệ lỗi API (timeout/invalid response)

## 4. Chỉ số AI cho báo cáo
1. Decision consistency: cùng context, AI trả action hợp lệ bao nhiêu %
2. Persona consistency: NPC có giữ đúng tính cách đã set không
3. Command validity: % action hợp lệ (`Bet/Hit/Stand`) theo phase

## 5. Log tối thiểu mỗi lượt
- matchId, roundId, npcId
- handValue, dealerUpCard, bankroll
- promptVersion
- aiRawResponse
- parsedAction
- reason
- latencyMs
- success/fail

## 6. Quy trình đánh giá
1. Chạy 100 ván kiểm tra ổn định parse/action.
2. Chạy 500 ván benchmark chính.
3. Tổng hợp metric theo từng NPC.
4. So sánh sự khác biệt hành vi giữa 3 NPC.
5. Kết luận: AI dùng được cho gameplay hay chưa.

## 7. Tiêu chí đạt
1. Chạy 500 ván không crash.
2. API error rate < 5%.
3. Command validity >= 95%.
4. 3 NPC có hành vi khác nhau rõ (ít nhất ở avg bet + hit tendency).

## 8. Bảng kết quả mẫu
| NPC | Win Rate | Bust Rate | Avg Bet | Avg Latency (ms) | API Error % |
|-----|----------|-----------|---------|------------------|-------------|
| NPC_A |  |  |  |  |  |
| NPC_B |  |  |  |  |  |
| NPC_C |  |  |  |  |  |
