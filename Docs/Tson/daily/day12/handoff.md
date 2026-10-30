# Bàn giao – Slot 12 – T-08 / A-08

## 1. Đã làm

- Mở rộng `SkillRunner` cho Cone, Line, Zone và Buff bằng executor strategy riêng theo kind; lưu origin/hướng ngay khi cast bắt đầu, gồm cả cast có delay.
- Cone dùng tầm từ `HeroData.range` và `SkillData.radius` làm nửa bề rộng ở mép xa. Line/Zone ghi nhận tối đa một hit cho mỗi enemy trong vòng đời field; status theo `StatusEffectData`.
- Zone làm mới status trong khi enemy còn ở trong vùng nhưng không lặp sát thương gốc: dùng `StatusEffectData.tickInterval` nếu > 0, còn Slow không định kỳ (`tickInterval = 0`) dùng nhịp kỹ thuật 5 Hz. Khi ra khỏi vùng, status tự hết theo `StatusEffectData.duration`.
- Buff kiểm tra vùng ở 5 Hz, độc lập nhịp đánh; gỡ nguồn buff khi mục tiêu rời vùng hoặc source reset/despawn.
- Gán `status_burn` cho passive Phù thủy Lửa; gán `status_slow` cho passive Lính Băng và Người Tuyết bằng Unity MCP.
- Tạo `hero_long_vuong_ultimate` kiểu Line (`length=5`, `width=5`, `damageMultiplier=1`, `duration=5`) và gán vào `hero_long_vuong.ultimateSkill`. Tạo `hero_nguoi_tuyet_zone` (radius 3, multiplier 1, duration 5, status Slow). Các giá trị theo TVA là tạm; cooldown/castDelay còn mặc định 0 vì tài liệu chưa cho số. TVA cần kiểm tra/chỉnh tay trước khi chốt cân bằng.
- Chưa có VFX gắn cho hai sample này; cần C bàn giao FX prefab. Projectile hiện dùng prefab hình cầu đơn giản dạng draft do TVA (A) tạo để unblock A-08; C có thể thay model/VFX về sau.
- Bổ sung PlayMode cases cho Cone, Line, Zone, Buff và cooldown. Các test này đã được chạy trong suite tích hợp T-16 (bằng chứng mục 2).

## 2. Bằng chứng hiện có

- T-16 integrated branch có các test S6/A-08 (`AttackModulePlayModeTests`, `HeroControllerPlayModeTests`, `HeroTargetingPlayModeTests` và ba bộ EditMode); lượt test toàn bộ ghi nhận EditMode 675/675 pass và PlayMode 116/116 pass, không fail/skip. Job: `cb4aede713514d83b261bbeff260396a` (EditMode, 329.7 s), `87b378136e49444aa9d5c386d4ebc169` (PlayMode, 41.1 s).
- Đây là kết quả suite tích hợp T-16, không phải lượt chạy riêng trên A-08. A-08 được coi là có test pass vì các test S6 hiện diện trong snapshot T-16 đã chạy trong suite và toàn bộ suite pass.
- Scene `A_Skills.unity` đã được tạo với ground, camera và directional light làm sandbox nền. Scene chưa được Playtest, chưa có wiring để tự khởi tạo hero/enemy và chưa thay thế các bài test PlayMode.
- Chưa có bằng chứng playtest ba màn; không coi test suite là nghiệm thu runtime/S6 hoàn chỉnh.

## 3. Cần người khác

### Quyết định TVA ngày 29/10/2026 – tiếp quản projectile draft

- Theo yêu cầu của TVA, TVA (A) đã tạo `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` dạng draft để unblock A-08; root giữ component `FateBastion.Combat.Projectile`, child `ProjectileVisual` hiện là hình cầu đơn giản.
- Đã gán prefab draft vào `_projectilePrefab` trên `Hero_Base.prefab`. C có thể review và thay model/VFX hoặc thay prefab sau này, giữ tương thích với component contract; phần draft được ghi nhận cho TVA, không ghi công cho C.
- Chưa chạy test runtime hoặc kiểm tra trong Sandbox; các bước này vẫn nằm trong lượt validation sau T-16.
- TVA xác nhận PR cập nhật quy tắc dùng chung đã đủ 3 người duyệt; ngoại lệ tiếp quản khi owner không phản hồi/block đã được ghi vào `TEAM_ASSIGNMENT.md` mục 5 và đồng bộ trong `CLAUDE.md`.

## 4. Kiểm thử và việc còn lại

- PlayMode `AttackModulePlayModeTests`: Melee + status/multiplier, projectile Muzzle, AreaAtPoint XZ/flying, Cone geometry, Line/Zone bounds and one-hit-per-field, Line cast-delay origin, Buff enter/exit và cooldown với scaled delta.
- Không cần chạy lại A-08 suite chỉ để xác nhận test code: T-16 aggregate EditMode/PlayMode đã pass, bao gồm các test S6 liệt kê ở trên.
- Nếu A-08 code/test thay đổi sau snapshot T-16, chạy lại các test S6 liên quan và ghi kết quả/job mới.
- Còn kiểm tra tay trong Unity: Playtest scene A_Skills, xác nhận behavior/visuals và rà soát các tiêu chí S6 chưa được covered bởi suite (đặc biệt cooldown ×1/×2, Nộ/Ultimate, buff stacking, boss targeting và thêm kỹ năng chỉ bằng data).
- Ghi job id, thời lượng, pass/fail/skip và thao tác Unity thực tế nếu phát sinh lượt validation mới.

## 5. Nhánh và PR

- Commit `e21358e` đã push lên `feature/S06_A08`.
- Draft PR #38 đã mở; TVA yêu cầu dừng ở ranh giới task để chờ review/merge trước khi bắt đầu A-09.
