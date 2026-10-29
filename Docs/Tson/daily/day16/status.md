# Hiện trạng – Slot 16 – T-12 / A-12

## Kết quả

- Đã tạo `MeteorAbility` dạng class C# thường, nhận `InputRouter`, thời gian rơi và hồi chiêu qua constructor.
- State machine có Ready/Aiming/Falling/Cooldown; xác nhận ngắm bắt đầu hồi chiêu ngay, thời gian rơi và hồi chiêu chỉ tiến khi `Tick` được gọi với scaled delta time.
- Q mở/hủy ngắm; `CancelStack` xử lý Esc/right-click; chọn `DeckSlot` hủy ngắm. Hủy không tiêu hồi chiêu.
- Phát `AbilityEvents.OnMeteorCooldownChanged` và `OnImpactReady(Vector3)` cho T-13 nối xử lý điểm va chạm.
- Đã viết năm EditMode tests cho Q toggle, deck slot, CancelStack, impact/cooldown ×1 và cooldown ×2. Chưa chạy theo quyết định hoãn validation đến T-16.

## Xác minh

- `git diff --cached --check`: pass sau khi stage toàn bộ file của T-12.
- Không chạy Unity compile, Unity Editor, EditMode hoặc PlayMode tests. Không có Unity setup trong phạm vi slot này.
- Kết quả chưa đủ để đánh dấu A-12 hoàn tất hoặc tick `TRACEABILITY.md`.

## Nhánh

- `feature/S07_A12`, tạo từ `origin/main`, giữ độc lập với các PR task chưa review; commit `3665024` đã push và draft PR #42 mở vào `main`.
- T-11 PR #41 đã được push riêng; nhánh A-12 không chứa thay đổi A-10/A-11.
