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

- **T-13/A-13:** đã cung cấp `MeteorSettings`, aim indicator, kiểm tra tầm, damage theo wave/level, `OverlapSphereNonAlloc` và xử lý quái Bay trong #43; xem `daily/day17/handoff.md`.
- **T-16:** trên `feature/A_T16_integration` đã sửa fixture và chạy `MeteorAbilityTests` cùng toàn bộ suite; kết quả ở mục "T-16".
- Trên `main`, fixture `MeteorTestInputRouter` vẫn còn (xem mục "Nhánh"), nên chưa coi A-12 pass trên `main` cho đến khi port fix.
- Không cần B/C sửa file trong phạm vi A-12. Event `AbilityEvents` sẵn có tiếp tục là điểm nối cho HUD/FX ở T-13.

## Nhánh

- `feature/S07_A12` bắt đầu từ `origin/main`; PR #42 đã **merge `main`** (`277d784`) ngày 30/10/2026.
- **Nợ còn lại trên `main`:** fixture `Assets/_Project/Tests/EditMode/S7/MeteorTestInputRouter.cs` vẫn nằm trong #42 đã merge. Fix T-16 `10d804e` (xóa fixture, dùng `InputRouter.Instance` + reflection `RaiseMeteor`/`RaiseDeckSlot`) chỉ có trên `feature/A_T16_integration`, **chưa được port vào `main`**.

## T-16 – kết quả tích hợp (30/10/2026)

- Trên `feature/A_T16_integration`, commit `10d804e` xóa `MeteorTestInputRouter`, chuyển `MeteorAbilityTests` sang `InputRouter.Instance` và gọi `RaiseMeteor`/`RaiseDeckSlot` qua reflection.
- Năm test Meteor chạy lại pass; toàn bộ EditMode **675/675 pass** (job `cb4aede713514d83b261bbeff260396a`, 329.7 s) và PlayMode **116/116 pass** (job `87b378136e49444aa9d5c386d4ebc169`, 41.1 s), 0 fail/0 skip.
- Lượt chạy này diễn ra trên nhánh tích hợp, không phải trên `main`. Trên `main` sau #42, fixture cũ vẫn có thể làm 5 test Meteor fail khi chạy EditMode; cần port `10d804e` (hoặc merge `feature/A_T16_integration`) trước khi coi A-12 pass trên `main`.
