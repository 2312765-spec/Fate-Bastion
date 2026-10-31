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
- Tích hợp prefab và yêu cầu nối tiếp từ PR #59: gán VFX cho sáu `SkillData` và hai `StatusEffectData`; thêm `HeroStats.BuffsChanged` và `HeroStats.HasSkillBuff(BuffType)` để chỉ báo buff do `SkillData` cung cấp. `CommandAuraSettings` không được tính là skill buff.

## Tài nguyên và nối scene còn chờ

Các prefab theo Issue #56 hiện đã có trong branch PR #59 và được đưa vào parent của PR #57:

- `FX_Skill_Area_Fire`, `FX_Skill_Area_Lightning`, `FX_Skill_Area_Ice`, `FX_Skill_Cone_Fire`, `FX_Ult_DragonLine`, `FX_Skill_FrostZone`
- `FX_Status_Burn`, `FX_Status_Slow`
- `FX_Meteor_Fall`, `FX_Hero_Level3`

Các `SkillData` và `StatusEffectData` thuộc phần A đã được gán prefab tương ứng. Cần C gán `FX_Meteor_Fall` vào `MeteorAimIndicator._fallFxPrefab` trên `Player.prefab` và `FX_Hero_Level3` vào `HeroPool._topLevelAuraPrefab` trong scene Game. `HeroBuffIndicator` vẫn có STUB đăng ký event trong file vùng C; C cần nối subscriber sau khi API A có mặt.

## Hướng dẫn lắp trong Unity

Các tham chiếu asset do A sở hữu đã được gán; C cần hoàn tất các tham chiếu prefab/scene thuộc vùng C:

1. Gán `FX_Meteor_Fall` vào `_fallFxPrefab` trên `Player.prefab` (prefab root ParticleSystem, one-shot).
2. Gán `FX_Hero_Level3` vào **Top Level Aura Prefab** trên `HeroPool` trong scene Game; `HeroPool` prewarm và `HeroLevel` bật/tắt tại `HeroVisual.AuraAnchor`.
3. Nối `HeroBuffIndicator` tới `HeroStats.BuffsChanged` trong `OnEnable`/`OnDisable`, khởi tạo bằng `HasSkillBuff(Damage/AttackSpeed)`, rồi xác nhận ký hiệu ẩn khi buff hết và khi hero về pool.
4. Chạy wave thử: xác nhận VFX skill/status theo vị trí, status tắt khi hết hạn, VFX Thiên thạch tắt ở impact, aura cấp 3 chỉ bật tại level 3 và icon buff phản ánh đúng loại nguồn.

## Kiểm thử

- `dotnet build Fate-Bastion.slnx --no-restore`: pass, 0 warning, 0 error.
- Unity EditMode mục tiêu `HeroStatsTests` + `HeroBuffIndicatorTests`: 23/23 pass, gồm test event/idempotency, lọc skill buff khỏi Command Aura và clear buff.
- Lần chạy kết hợp thêm `FxPrefabBuilderTests`: 26/27 pass; test còn lại lỗi môi trường khi Unity không ghi được `Assets/_FxPrefabBuilderTest.meta`, không phải assertion gameplay.
- PlayMode trước khi tích hợp PR #59: các test mục tiêu Issue #56 4/4 pass. Toàn bộ PlayMode 173 pass, 1 lỗi CommandAura ngoài phạm vi và 3 test GC allocation bị skip trên Windows Editor.
- PlayMode mục tiêu sau khi tích hợp PR #59: 4/4 pass (status VFX, aura cấp 3, pool VFX kỹ năng và Meteor fall).
- Issue chưa hoàn tất tới khi C gắn ref scene/prefab, nối `HeroBuffIndicator`, và B review phần takeover.
