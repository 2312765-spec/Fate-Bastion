# Bàn giao – Ngày 10 – 19/10/2026

> Tạo bổ sung ngày 27/10/2026. Ghi các phụ thuộc liên thành viên phát sinh khi thực thi T-06; đây không phải xác nhận đã bàn giao xong.

## 1. Cần C cung cấp prefab Projectile

**Lý do:** `AttackModule` của A dùng pool prewarm và cần một prefab có component `FateBastion.Combat.Projectile`; `Prefabs/FX/` do C sở hữu. Không tìm thấy prefab đạn hiện có. A không tạo/sửa asset trong vùng của C.

**Issue draft để gửi:** `[Cần C] Tạo prefab Projectile dùng cho đòn thường A-07`

- File đề xuất: `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` (hoặc tên theo quy ước asset của C).
- Root có component `FateBastion.Combat.Projectile`; hình ảnh/trail nằm ở child để code projectile giữ root transform.
- Không cần Animator contract #11; `Projectile` nhận start position/tốc độ khi `Launch`.
- A sẽ gán prefab đó vào field `_projectilePrefab` của `AttackModule` trên `Hero_Base`; pool được prewarm theo `_projectilePrewarmCount` trước trận.
- Không sửa prefab `Hero_Base` hay code của A từ phía C; C bàn giao prefab/variant qua PR theo quy trình nhóm.

**Trạng thái:** BLOCKED cho tích hợp bắn Projectile trong Sandbox đến khi có prefab C. Có thể tiếp tục xác minh logic timer/Melee, build và test fixture trong code.

## 2. Animator contract #11

Hợp đồng Trigger `Attack`/`Ultimate`/`Spawn`, Float `AttackSpeed` và `Animator.Rebind()` vẫn là đề xuất chờ A duyệt trong `TEAM_ASSIGNMENT.md` 3.1 #11. Không nối Animator trong T-06 khi chưa có quyết định; timer gây sát thương không phụ thuộc Animation Event.

## 3. Quyết định phạm vi đã nhận

Người dùng chọn T-06 chỉ triển khai timer và Melee/Projectile; Area/Cone cùng passive để T-07/T-08 khi làm `SkillRunner`. Không giả lập Area/Cone thành Projectile/Melee, không suy ra bán kính hoặc góc tấn công.
