# Bàn giao – Slot 12 – T-08 / A-08

## 1. Đã làm

- Mở rộng `SkillRunner` cho Cone, Line, Zone và Buff bằng executor strategy riêng theo kind; lưu origin/hướng ngay khi cast bắt đầu, gồm cả cast có delay.
- Cone dùng tầm từ `HeroData.range` và `SkillData.radius` làm nửa bề rộng ở mép xa. Line/Zone ghi nhận tối đa một hit cho mỗi enemy trong vòng đời field; status theo `StatusEffectData`.
- Zone làm mới status trong khi enemy còn ở trong vùng nhưng không lặp sát thương gốc: dùng `StatusEffectData.tickInterval` nếu > 0, còn Slow không định kỳ (`tickInterval = 0`) dùng nhịp kỹ thuật 5 Hz. Khi ra khỏi vùng, status tự hết theo `StatusEffectData.duration`.
- Buff kiểm tra vùng ở 5 Hz, độc lập nhịp đánh; gỡ nguồn buff khi mục tiêu rời vùng hoặc source reset/despawn.
- Gán `status_burn` cho passive Phù thủy Lửa; gán `status_slow` cho passive Lính Băng và Người Tuyết bằng Unity MCP.
- Tạo `hero_long_vuong_ultimate` kiểu Line (`length=5`, `width=5`, `damageMultiplier=1`, `duration=5`) và gán vào `hero_long_vuong.ultimateSkill`. Tạo `hero_nguoi_tuyet_zone` (radius 3, multiplier 1, duration 5, status Slow). Các giá trị theo TVA là tạm; cooldown/castDelay còn mặc định 0 vì tài liệu chưa cho số. TVA cần kiểm tra/chỉnh tay trước khi chốt cân bằng.
- Chưa có VFX gắn cho hai sample này; cần C bàn giao FX prefab. Projectile hiện dùng prefab hình cầu đơn giản dạng draft do TVA (A) tạo để unblock A-08; C có thể thay model/VFX về sau.
- Bổ sung PlayMode cases cho Cone, Line, Zone, Buff và cooldown. Chưa chạy test theo yêu cầu dời validation đến sau T-16.

## 2. Bằng chứng hiện có

- Unity MCP standard script validation: 0 lỗi cho scripts/tests đã đổi. Heuristic báo cảnh báo string concatenation trong `Update()` ở `SkillRunner` và `HeroController`; đọc trực tiếp hai thân `Update()` không thấy nối chuỗi.
- Unity Console không có lỗi biên dịch; các log còn lại thuộc Unity Assistant/MCP (AssistantPerf sharing violation, WebSocket chưa khởi tạo và account API timeout).
- `git diff --check` chỉ báo trailing whitespace ở trường `m_Name:` do Unity ghi vào YAML của `Hero_Base.prefab`; không sửa YAML prefab bằng tay.
- Chưa có EditMode/PlayMode run, Play thử, hoặc scene thử A-Skills. Không coi các bước này là pass.

## 3. Cần người khác

### Quyết định TVA ngày 29/10/2026 – tiếp quản projectile draft

- Theo yêu cầu của TVA, TVA (A) đã tạo `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` dạng draft để unblock A-08; root giữ component `FateBastion.Combat.Projectile`, child `ProjectileVisual` hiện là hình cầu đơn giản.
- Đã gán prefab draft vào `_projectilePrefab` trên `Hero_Base.prefab`. C có thể review và thay model/VFX hoặc thay prefab sau này, giữ tương thích với component contract; phần draft được ghi nhận cho TVA, không ghi công cho C.
- Chưa chạy test runtime hoặc kiểm tra trong Sandbox; các bước này vẫn nằm trong lượt validation sau T-16.
- TVA xác nhận PR cập nhật quy tắc dùng chung đã đủ 3 người duyệt; ngoại lệ tiếp quản khi owner không phản hồi/block đã được ghi vào `TEAM_ASSIGNMENT.md` mục 5 và đồng bộ trong `CLAUDE.md`.

## 4. Kiểm thử để chạy sau T-16

- PlayMode `AttackModulePlayModeTests`: Melee + status/multiplier, projectile Muzzle, AreaAtPoint XZ/flying, Cone geometry, Line/Zone bounds and one-hit-per-field, Line cast-delay origin, Buff enter/exit và cooldown với scaled delta.
- Kiểm tra ×1/×2 cooldown trong runtime; kiểm tra status tick/duration theo `StatusEffectData`.
- Tạo/xác nhận `A_Skills` scene có cấu hình kiểm thử các kind; kiểm tra projectile draft, pool và hình ảnh trong runtime. C có thể thay prefab trước hoặc sau lượt validation.
- Sau mỗi lượt ghi job id, thời lượng, số pass/fail/skip và các thao tác Unity Editor thực tế.

## 5. Nhánh và PR

- Commit `e21358e` đã push lên `feature/S06_A08`.
- Draft PR #38 đã mở; TVA yêu cầu dừng ở ranh giới task để chờ review/merge trước khi bắt đầu A-09.
