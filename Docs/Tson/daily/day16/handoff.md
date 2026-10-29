# Bàn giao – Slot 16 – T-12 / A-12

## API để T-13 tích hợp

- Tạo `MeteorAbility(InputRouter inputRouter, float cooldownDuration, float fallDuration)`.
- Gọi `Tick(Time.deltaTime)` mỗi frame để dùng thời gian game có tỉ lệ; không truyền `unscaledDeltaTime`.
- Gọi `BeginAiming()` khi cần bật ngắm từ service; phím Q đã được nối sẵn: bắt đầu nếu Ready, hủy nếu Aiming.
- Gọi `ConfirmAim(Vector3 point)` sau khi T-13 xác nhận điểm nằm trong tầm; hồi chiêu bắt đầu ngay tại thời điểm commit.
- Subscribe `OnImpactReady(Vector3)` để gây sát thương/VFX sau thời gian rơi; unsubscribe theo vòng đời consumer.
- Q lần nữa, `CancelStack.TryCancelTop()` và `InputRouter.DeckSlot` hủy ngắm mà không tiêu hồi chiêu.
- Gọi `Dispose()` khi chủ thể sở hữu ability bị hủy để gỡ subscriptions và entry khỏi `CancelStack`.

## Việc còn chờ

- **T-13/A-13:** cung cấp `MeteorSettings`, aim indicator, kiểm tra tầm, damage theo wave/level, OverlapSphereNonAlloc và xử lý quái Bay; chưa tích hợp trong nhánh này.
- **T-16:** import/compile trong Unity, chạy `MeteorAbilityTests`, kiểm tra hồi chiêu 45 giây game ở ×1/×2, cancellation không mất cooldown và commit/impact một lần.
- Chưa có kết quả Unity compile/test; không được mô tả các tests là pass.
- Không cần B/C sửa file trong phạm vi A-12. Event `AbilityEvents` sẵn có tiếp tục là điểm nối cho HUD/FX ở T-13.

## Nhánh

- `feature/S07_A12` bắt đầu từ `origin/main`; PR sẽ ghi rõ trạng thái code-only và validation còn hoãn.
