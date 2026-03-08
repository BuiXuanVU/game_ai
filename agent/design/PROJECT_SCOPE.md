# PROJECT SCOPE
## 1. Project Title
Blackjack 4-Player Table with Autonomous NPC AI (International Rules)

## 2. Problem Statement
Mục tiêu của đồ án là xây dựng một game blackjack tối giản nhưng tập trung vào AI hành vi.  
NPC phải tự quyết định hành động trong ván bài mà không dùng luật cứng trong gameplay UI.

## 3. Core Objectives
1. Xây hệ thống blackjack 4 người chơi theo luật quốc tế.
2. Thiết kế AI toàn quyền cho NPC: tự quyết định Bet/Hit/Stand.
3. Tạo sự khác biệt hành vi giữa nhiều NPC dựa trên personality và trạng thái trận.
4. Thu thập số liệu để đánh giá chất lượng AI.

## 4. Locked MVP Scope
1. 1 bàn blackjack, 4 ghế chơi.
2. Cấu hình mặc định: 1 Human + 3 NPC AI.
3. 1 dealer chung.
4. Luật quốc tế chuẩn:
   - Blackjack payout 3:2
   - Push khi bằng điểm
   - Dealer Stand on Soft 17 (S17)
   - Không insurance
   - Không surrender
5. Mỗi lượt NPC hành động qua NpcBrain:
   - Quyết định mức cược đầu ván
   - Quyết định Hit/Stand trong lượt chơi
6. Có panel hiển thị log lý do quyết định AI.
7. Có thống kê trận phục vụ báo cáo.

## 5. Out of Scope
1. Multiplayer online/network.
2. Open world/RPG.
3. LLM hội thoại tự do.
4. Animation/phần nhìn phức tạp.
5. Minigame ngoài blackjack.

## 6. Success Criteria (Definition of Done)
1. Chạy ổn định 20 ván liên tục không lỗi luật.
2. 4 người chơi tham gia đầy đủ mỗi ván.
3. 3 NPC có hành vi khác biệt đo được bằng số liệu.
4. Có log quyết định AI theo từng lượt.
5. Có dashboard hoặc file thống kê cho báo cáo.

## 7. Technical Deliverables
1. Playable Unity build.
2. Tài liệu AI architecture.
3. Kết quả benchmark (win rate, bust rate, avg bet, avg return).
4. Video demo 3-5 phút.

## 8. Timeline Constraint
Thời gian thực hiện: 2 tuần (buổi tối), ưu tiên hoàn thiện gameplay ổn định trước polish.
