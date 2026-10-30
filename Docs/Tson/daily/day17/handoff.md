# Bàn giao – Slot 17 – T-13 / A-13

## API và cách lắp

- `MeteorSettings` tham chiếu một `SkillData` (`AreaAtPoint`, `GroundPoint`, `True`, radius 4, cooldown 45, castDelay 0.8, multiplier 3), `EnemyData` Thường, range 20 m và vertical query padding 2 m.
- `MeteorAimIndicator` cần `WaveManager`, `EnemyManager`, Transform Triệu Hồi Sư, layer mặt đất, `IMeteorAimRaySource`, range ring, blast preview renderer và impact marker renderer.
- `IMeteorAimRaySource.TryGetAimRay(out Ray)` là contract A; provider ở Player/Movement do C sở hữu nên trả ray camera theo con trỏ, không đọc input trực tiếp trong gameplay A. Provider nên trả `false` khi con trỏ ở trên UI hoặc không ở Action Map Summoner.
- `MeteorAimIndicator` subscribe `InputRouter.Select`, `GameEvents.OnWaveStarted/OnMatchEnded`, phát `AbilityEvents.OnMeteorCooldownChanged` và `OnMeteorExploded`.
- Tướng Bay được kiểm tra bởi active roster và XZ sau NonAlloc collider query; nhiều collider trên cùng enemy chỉ nhận sát thương một lần.

## Handoff cần chủ sở hữu khác

- **Issue draft `[Cần C] Triển khai IMeteorAimRaySource cho Thiên thạch`:** implement provider trong vùng `Player/Movement` sở hữu bởi C, cung cấp world ray từ camera theo pointer, không sửa `Core/InputRouter` trong PR này; gắn provider cùng `MeteorAimIndicator` vào Player prefab của C.
- **B cần nối HUD** với `OnMeteorCooldownChanged`; **C cần nối FX/rung/âm thanh** với `OnMeteorExploded`. Không sửa file B/C trong nhánh A.
- Hướng dẫn lắp trong Unity: T-16 đã tạo `meteor.asset` và `meteor_settings.asset` trên nhánh tích hợp (mục "T-16"); vẫn cần gắn `MeteorAimIndicator` vào Player prefab, assign đúng các references/layer ở trên. Ring meshes cần hình tròn đơn vị đường kính 1 để scale theo mét.

## Kiểm thử

- EditMode: công thức meteor tại wave 1/5/15 và kiểm tra tầm XZ, kể cả chênh cao.
- PlayMode: quái Bay cao 2 m trong AoE bị True damage; nhiều collider không gây hit lặp; kiểm tra click ngoài tầm không commit, trong tầm commit đúng một lần, impact sau castDelay, match end trước impact không gây sát thương.
- Kết quả trên `feature/A_T16_integration` ở mục "T-16"; kết quả này không thay thế lượt chạy trên `main`.
- Fire trong `DamageInfo.Element` là nhãn event theo giả định đã ghi trong code; damage vẫn `True`, status null.

## Nhánh

- `feature/S07_A13` dựa trên `feature/S07_A12`; PR #43 đã **merge `main`** (`d374a6c`) ngày 30/10/2026.
- Kế thừa nợ của A-12: fixture `MeteorTestInputRouter` vẫn còn trên `main` sau #42; fix `10d804e` chỉ có trên `feature/A_T16_integration` và cần được port.

## T-16 – kết quả tích hợp (30/10/2026)

- Unity MCP trên `feature/A_T16_integration` đã tạo:
  - `Assets/_Project/Data/Skills/meteor.asset`: `AreaAtPoint`, `GroundPoint`, True damage ×3, bán kính 4 m, hồi chiêu 45 s, castDelay 0.8 s.
  - `Assets/_Project/Data/Settings/meteor_settings.asset`: tham chiếu `meteor` + `enemy_normal`, tầm 20 m, vertical query padding 2 m.
- `MeteorAimMathTests` và `MeteorDamageCalculatorTests` (EditMode) cùng `MeteorDamageApplierPlayModeTests` đã chạy trong suite tích hợp: EditMode **675/675 pass**, PlayMode **116/116 pass**, 0 fail/0 skip (job `cb4aede713514d83b261bbeff260396a` và `87b378136e49444aa9d5c386d4ebc169`).
- **Chưa có trên `main`:** hai asset trên, wiring `CommandAura`/`MeteorAbility`/`MeteorAimIndicator`/`IMeteorAimRaySource` trong Player prefab (C sở hữu), và fixture fix `10d804e`. Cần một PR follow-up port từ nhánh tích hợp.
- Chưa playtest tay ba màn; `Game.unity` thuộc C. Issue draft cho C nằm ở `daily/day20/handoff.md` trên nhánh tích hợp.
