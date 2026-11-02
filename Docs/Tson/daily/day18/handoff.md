# Bàn giao – Slot 18 – T-14 / A-05

## Queue kiểm thử T-16

- Hồi quy `BossSummonAbilityTests` và `BossSummonPlayModeTests`.
- Kiểm tra wave chỉ kết thúc sau khi quái boss gọi ra đã chết.
- Kiểm tra quái Bay dùng `pathIndex=1`, tướng Melee không đánh được, Thiên thạch vẫn trúng.
- Kiểm tra prefab Bay/Boss do C sở hữu nếu đã được cung cấp; chưa xác nhận trong phiên này.
- PR #24 đã merge; kết quả 8 EM + 4 PM là bằng chứng lịch sử, không thay cho lượt test tổng hợp T-16.

## Kết quả T-16 sau merge `main`

- `BossSummonAbilityTests` và `BossSummonPlayModeTests` có trong suite tổng hợp: EditMode **717/717 pass**, PlayMode **154 pass / 157 total, 0 fail, 3 skip** (job `b64e5b6d634f417f842260fc9959be1a` / `d6a119afa40a46c48588c34e6e9354ce`).
- Đây là aggregate của hai assembly, không phải số test riêng của A-05; ba PlayMode skip là test GC Alloc không hỗ trợ trên WindowsEditor.
- Chưa kiểm tra tay quái Bay/boss hoặc playtest ba màn; test tự động qua Unity MCP không bao gồm kiểm tra/chỉnh tay lại prefab hay asset.
