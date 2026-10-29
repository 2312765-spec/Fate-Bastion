# Bàn giao – Slot 12 – T-08 / A-08

## 1. Đã làm

- Mở rộng `SkillRunner` cho Cone, Line, Zone và Buff bằng executor strategy riêng theo kind; lưu origin/hướng ngay khi cast bắt đầu, gồm cả cast có delay.
- Cone dùng tầm từ `HeroData.range` và `SkillData.radius` làm nửa bề rộng ở mép xa. Line/Zone ghi nhận tối đa một hit cho mỗi enemy trong vòng đời field; status theo `StatusEffectData`.
- Zone làm mới status trong khi enemy còn ở trong vùng nhưng không lặp sát thương gốc: dùng `StatusEffectData.tickInterval` nếu > 0, còn Slow không định kỳ (`tickInterval = 0`) dùng nhịp kỹ thuật 5 Hz. Khi ra khỏi vùng, status tự hết theo `StatusEffectData.duration`.
- Buff kiểm tra vùng ở 5 Hz, độc lập nhịp đánh; gỡ nguồn buff khi mục tiêu rời vùng hoặc source reset/despawn.
- Gán `status_burn` cho passive Phù thủy Lửa; gán `status_slow` cho passive Lính Băng và Người Tuyết bằng Unity MCP.
- Tạo `hero_long_vuong_ultimate` kiểu Line (`length=5`, `width=5`, `damageMultiplier=1`, `duration=5`) và gán vào `hero_long_vuong.ultimateSkill`. Tạo `hero_nguoi_tuyet_zone` (radius 3, multiplier 1, duration 5, status Slow). Các giá trị theo TVA là tạm; cooldown/castDelay còn mặc định 0 vì tài liệu chưa cho số. TVA cần kiểm tra/chỉnh tay trước khi chốt cân bằng.
- Chưa có VFX gắn cho hai sample này; cần C bàn giao FX prefab. Projectile prefab thật của C cũng còn thiếu.
- Bổ sung PlayMode cases cho Cone, Line, Zone, Buff và cooldown. Chưa chạy test theo yêu cầu dời validation đến sau T-16.

## 2. Bằng chứng hiện có

- Unity MCP standard script validation: 0 lỗi cho scripts/tests đã đổi. Heuristic báo cảnh báo string concatenation trong `Update()` ở `SkillRunner` và `HeroController`; đọc trực tiếp hai thân `Update()` không thấy nối chuỗi.
- Unity Console không có lỗi biên dịch; các log còn lại thuộc Unity Assistant/MCP (AssistantPerf sharing violation, WebSocket chưa khởi tạo và account API timeout).
- `git diff --check` chỉ báo trailing whitespace ở trường `m_Name:` do Unity ghi vào YAML của `Hero_Base.prefab`; không sửa YAML prefab bằng tay.
- Chưa có EditMode/PlayMode run, Play thử, hoặc scene thử A-Skills. Không coi các bước này là pass.

## 3. Cần người khác

### Quyết định TVA ngày 29/10/2026 – prefab projectile draft

- Vì C chưa phản hồi, TVA muốn tiếp tục bằng prefab draft tại `Assets/_Project/Prefabs/FX/Projectile_Base.prefab`; sau này C có thể thay nội dung prefab mà không đổi contract của `SkillRunner`.
- Root cần giữ component `FateBastion.Combat.Projectile`; model/VFX có thể được C thay hoặc thêm dưới dạng child.
- Khi draft được C bàn giao/đặt vào vùng làm việc được phép, A sẽ gán prefab vào `_projectilePrefab` trên `Hero_Base.prefab` và kiểm tra projectile trong Sandbox.
- Nếu C không đồng ý với draft hoặc cần projectile khác, C chủ động liên hệ A để thống nhất thay thế/bàn giao, tránh để A-08 đứng chờ không có hướng tiếp tục.
- `Assets/_Project/Prefabs/FX/` do C sở hữu. Prefab chưa được tạo trong PR A-08; cần C tạo hoặc cập nhật phân công qua PR được duyệt trước khi A thay đổi vùng này. Không coi yêu cầu draft là đã hoàn tất.

**Issue draft để TVA gửi C nếu cần:** `[Cần C] Tạo projectile draft để A-08 tiếp tục`

> Nhờ C tạo `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` với `FateBastion.Combat.Projectile` trên root. Đây là prefab draft để A-08 tiếp tục kiểm thử; C có thể thay model/VFX hoặc sửa prefab sau review, miễn component contract được giữ. Nếu C muốn dùng projectile khác, vui lòng liên hệ A để bàn giao và thống nhất cách gán vào `SkillRunner`.

## 4. Kiểm thử để chạy sau T-16

- PlayMode `AttackModulePlayModeTests`: Melee + status/multiplier, projectile Muzzle, AreaAtPoint XZ/flying, Cone geometry, Line/Zone bounds and one-hit-per-field, Line cast-delay origin, Buff enter/exit và cooldown với scaled delta.
- Kiểm tra ×1/×2 cooldown trong runtime; kiểm tra status tick/duration theo `StatusEffectData`.
- Tạo/xác nhận `A_Skills` scene có cấu hình kiểm thử các kind; projectile runtime cần prefab của C.
- Sau mỗi lượt ghi job id, thời lượng, số pass/fail/skip và các thao tác Unity Editor thực tế.

## 5. Nhánh và PR

- Commit `e21358e` đã push lên `feature/S06_A08`.
- Draft PR #38 đã mở; TVA yêu cầu dừng ở ranh giới task để chờ review/merge trước khi bắt đầu A-09.
