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
- Năm test Meteor được chạy trong suite tổng hợp T-16 sau merge `main`: EditMode **717/717 pass**; PlayMode **154 pass / 157 total, 0 fail, 3 skip** (job `b64e5b6d634f417f842260fc9959be1a` và `d6a119afa40a46c48588c34e6e9354ce`). Đây là aggregate, không phải lượt riêng của A-12.
- `feature/A_T16_integration` đã merge `main` và bỏ fixture cũ không được dùng; `main` vẫn giữ fixture cho đến khi PR #47 được merge. Ba test skip là các bài GC Alloc không hỗ trợ trên WindowsEditor.
- Chưa kiểm tra tay/playtest Meteor trong trận; validation trên chỉ là test tự động qua Unity MCP trong Editor, không bao gồm kiểm tra/chỉnh tay lại prefab hoặc asset.
