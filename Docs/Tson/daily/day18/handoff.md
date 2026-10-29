# Bàn giao – Slot 18 – T-14 / A-05

## Queue kiểm thử T-16

- Hồi quy `BossSummonAbilityTests` và `BossSummonPlayModeTests`.
- Kiểm tra wave chỉ kết thúc sau khi quái boss gọi ra đã chết.
- Kiểm tra quái Bay dùng `pathIndex=1`, tướng Melee không đánh được, Thiên thạch vẫn trúng.
- Kiểm tra prefab Bay/Boss do C sở hữu nếu đã được cung cấp; chưa xác nhận trong phiên này.
- PR #24 đã merge; kết quả 8 EM + 4 PM là bằng chứng lịch sử, không thay cho lượt test tổng hợp T-16.
