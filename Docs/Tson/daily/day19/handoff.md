# Bàn giao – Slot 19 – T-15 / A-06

## Queue kiểm thử T-16

- Hồi quy `EnemyHealthFormulaTests`, `LevelBalanceDataTests` và `Endless_WavesBeyondLast_RepeatLoopBlockWithRealWaveNumbers`.
- Kiểm tra HP wave 1/5/15 khớp công thức cho cả 3 màn, gồm màn 2 ×1.15 và màn 3/Vô tận ×1.3.
- Kiểm tra Vô tận lặp block wave 6–15 nhưng wave thật tiếp tục tăng trong hệ số `hpGrowth`.
- Xác nhận dữ liệu level 1/2/3 vẫn đúng; không sửa asset do importer quản lý.
- PR #22 đã merge; 298 EM + 50 PM là bằng chứng lịch sử, không thay cho lượt test tổng hợp T-16.
