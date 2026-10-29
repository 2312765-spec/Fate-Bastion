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
- Hướng dẫn lắp trong Unity tại T-16: tạo `MeteorSettings.asset` trong `Assets/_Project/Data/Settings`; gắn `MeteorAimIndicator` vào Player prefab; assign đúng các references/layer ở trên; ring meshes cần hình tròn đơn vị đường kính 1 để scale theo mét.

## Kiểm thử hoãn đến T-16

- EditMode: công thức meteor tại wave 1/5/15 và kiểm tra tầm XZ, kể cả chênh cao.
- PlayMode: quái Bay cao 2 m trong AoE bị True damage; nhiều collider không gây hit lặp; kiểm tra click ngoài tầm không commit, trong tầm commit đúng một lần, impact sau castDelay, match end trước impact không gây sát thương.
- Chưa có kết quả compile/test. Fire trong `DamageInfo.Element` là nhãn event theo giả định đã ghi trong code; damage vẫn `True`, status null.

## Nhánh

- `feature/S07_A13` dựa trên `feature/S07_A12`; draft PR #43 nhắm PR #42, do TVA chọn tiếp tục khi A-12 còn draft.
