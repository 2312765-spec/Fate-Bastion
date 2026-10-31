# Issue #56 — C-09 gameplay VFX integration

## Người thực hiện

- **A (Thành / Tson-dev):** triển khai các hook gameplay thuộc A.
- **Phần code thuộc B do A tiếp quản:** `HeroPool` prewarm và `HeroLevel` aura cấp 3. Việc tiếp quản được xác nhận trong phiên này vì B chưa phản hồi và đang chặn tiến độ; quyền sở hữu lâu dài không thay đổi. Code takeover vẫn là draft cho đến khi B review.
- **C:** bàn giao các prefab VFX theo Issue #56. Không tạo art thay C trong thay đổi này.

## Phạm vi đã triển khai

- `SkillRunner` prewarm VFX từ `SkillData.vfx` cùng lúc `HeroPool` tạo pool lúc tải trận. VFX vùng/đường được đặt và scale theo loại kỹ năng; Line/Zone được trả pool khi hết `duration`; one-shot được trả pool khi particle kết thúc.
- `StatusEffectController` phát sự kiện khi từng hiệu ứng trạng thái bắt đầu/kết thúc. `EnemyManager` nhận các status từ deck trước khi prewarm quái; `EnemyController` tạo trước VFX theo status cho từng quái và chỉ bật/tắt khi trạng thái đổi.
- `MeteorAimIndicator` có field tùy chọn `_fallFxPrefab`, tạo một instance lúc khởi tạo và bật tại điểm rơi trong trạng thái Falling; ẩn khi impact hoặc trận kết thúc.
- Phần B được A hoàn thiện: bỏ STUB ở `HeroLevel`; hook `HeroPool` vào prewarm skill/status VFX. Aura cấp 3 vẫn dùng field `_topLevelAuraPrefab` hiện có và neo tại `HeroVisual.AuraAnchor`.

## Tài nguyên còn chờ C

Đã kiểm tra `Assets/_Project/Prefabs/FX/`. Có `FX_Meteor_Explosion`, `MeteorAimFX`, các FX hit/death, projectile và aura độ hiếm; **chưa có** các prefab được Issue #56 yêu cầu:

- `FX_Skill_Fireball`, `FX_Skill_Cone`, `FX_Ult_DragonLine`, `FX_Skill_FrostZone`
- `FX_Status_Burn`, `FX_Status_Slow`
- `FX_Meteor_Fall`
- `FX_Hero_Level3`

`status_burn.asset` và `status_slow.asset` hiện có `vfx` để trống. Code xử lý null an toàn theo yêu cầu trong Issue #56; không tạo prefab thay C. Cần gán các prefab khi được bàn giao trước khi xác nhận hiệu ứng hình ảnh hoạt động hoàn chỉnh.

## Hướng dẫn lắp trong Unity

Sau khi C bàn giao prefab:

1. Gán prefab tương ứng vào `SkillData.vfx` cho Fireball, Cone, Ultimate Line và Frost Zone.
2. Gán `FX_Status_Burn` / `FX_Status_Slow` vào `StatusEffectData.vfx`. Prefab cần có `ParticleSystem` ở root và bật loop.
3. Gán `FX_Meteor_Fall` vào field `_fallFxPrefab` của `MeteorAimIndicator`. Prefab cần có `ParticleSystem` root không loop.
4. Gán `FX_Hero_Level3` vào field **Top Level Aura Prefab** trên `HeroPool` trong scene Game. `HeroPool` tạo một bản lúc prewarm; `HeroLevel` bật/tắt bản đó dưới `HeroVisual.AuraAnchor`.
5. Chạy một wave thử: xác nhận VFX skill/status theo vị trí, status tắt khi hết hạn, VFX Thiên thạch tắt ở impact, aura cấp 3 chỉ bật tại level 3.

## Kiểm thử

- `dotnet build Fate-Bastion.slnx --no-restore`: pass, 0 warning, 0 error.
- Unity EditMode: 157/157 pass.
- PlayMode mục tiêu Issue #56: 4/4 pass (status VFX, aura cấp 3, pool VFX kỹ năng, VFX rơi Thiên thạch).
- Toàn bộ PlayMode: 173 pass, 1 fail, 3 skipped. Test fail là `CommandAuraPlayModeTests.Aura_EnterExitAndDisable_UpdatesBuffAndPublishesOnlyEntryTransitions` (ngoài phạm vi, không thay đổi trong PR); 3 test GC allocation bị skip trên Windows Editor.
- PR được tạo dạng draft và không đóng Issue #56 cho đến khi C bàn giao/gán đủ prefab còn thiếu và code takeover được review.
