# Kế hoạch – Slot 16 – T-12 / A-12

## Mục tiêu

Triển khai state machine thuần cho Thiên thạch, quản lý trạng thái ngắm/hủy, thời gian rơi và hồi chiêu theo thời gian game; nối Q, `CancelStack` và `InputRouter.DeckSlot`.

## Phạm vi

- Tạo `MeteorAbility` trong `Player/Abilities`, nhận `InputRouter`, thời gian rơi và hồi chiêu qua constructor để không hardcode cân bằng.
- Các trạng thái: Ready, Aiming, Falling, Cooldown; bắt đầu hồi chiêu ngay khi xác nhận điểm rơi.
- Q mở ngắm khi sẵn sàng, Q lần nữa hoặc `CancelStack.OnCancel()` hủy ngắm miễn phí; chọn deck slot cũng hủy ngắm.
- Phát `AbilityEvents.OnMeteorCooldownChanged` và sự kiện impact-ready để T-13 nối sát thương/VFX.
- Viết EditMode tests cho input/cancel, hủy không tốn hồi chiêu và hồi chiêu theo scaled delta time; hoãn chạy test đến T-16 theo kế hoạch TVA.

## Giới hạn và xác minh

- T-13 chịu trách nhiệm kiểm tra tầm, vùng nổ, sát thương, aim indicator và kết nối `MeteorSettings`.
- Không sửa Core/InputRouter, asset/prefab/scene hoặc file của B/C. Không dùng Unity Editor/MCP trong slot code-only.
- Nhánh riêng `feature/S07_A12` bắt đầu từ `origin/main`; PR #42 ghi rõ chưa compile/test trong Unity. **Cập nhật 30/10/2026:** PR #42 đang **Ready**, base `main`, `CLEAN`; còn nợ việc áp fix `10d804e` cho fixture EditMode trước khi merge.
