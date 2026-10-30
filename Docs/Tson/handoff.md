# Bàn giao tổng thể – Thành viên A (Gameplay)

> Cập nhật: **30/10/2026**. Bàn giao **tổng thể**; bàn giao phát sinh trong ngày ở `daily/dayNN/handoff.md`.
> "Đã bàn giao" theo `TEAM_ASSIGNMENT.md` mục 4.1: (1) merge `main` + test xanh, (2) mục "Cách dùng" (API khớp mục 3.1), (3) "Hướng dẫn lắp trong Unity", (4) scene Sandbox chạy được, (5) người nhận xác nhận.
> Ký hiệu: ☐ chưa · ◐ đang · ☑ xong.

---

## 1. A ĐANG CHỜ người khác (việc của A bị chặn)

| Việc A | Mã | Chờ ai | Vì sao | Trong lúc chờ dùng | Ngày cần |
|---|---|---|---|---|---|
| Ghép trận hoàn chỉnh / playtest màn | A-04…A-13 | **B** (S5 `GameManager`, S4 đặt tướng) | Cần luồng trận + đặt tướng để chơi thật | Stub theo 3.1; test trong Sandbox `A_Enemies`, `A_Combat` | 20 (T-16) |
| A-10 Ultimate hiển thị thanh Nộ | A-10 | **B** (HUD Nộ) | HUD do B; A chỉ phát `OnRageChanged` (#5) | Phát event đúng tên, B nối sau | 14 (T-10) |
| A-11 Hào quang bước 3 Tutorial | A-11 | **B** (`TutorialController`) | Tutorial do B; A phát `OnHeroEnteredAura` (#10) | Phát event đúng tên | 15 (T-11) |
| A-13 ô Thiên thạch trên HUD | A-13 | **B** (HUD hồi chiêu) | Ô/hồi chiêu do B; A phát `OnMeteorCooldownChanged`/`OnMeteorExploded` | Phát event đúng tên | 17 (T-13) |
| A-14/A-15 Save phục vụ S8 lưu | A-14, A-15 | **B** (S8 gọi `SaveService`) | A đã cung cấp API S9; B cần gọi sau gacha/ghép/deck/kết quả trận/cài đặt/Thoát | API đã có (`SaveService.Load/Save/Delete`, `SaveData`, `SaveSanitizer`) | 8 (T-04) |
| Prefab quái Bay/boss | A-05, A-06 | **C** (prefab) | Prefab do C làm; A chỉ trỏ `EnemyData.prefab` | Asset `BossSummonSettings` + gắn `EnemyManager` **đã xong** trong `A_Enemies.unity` | 18–19 |
| A-17 cân bằng | A-17 | **B, C** (game chơi được + FX) | Cần trận chạy thật | — | 21–22 |

---

## 2. A GIAO cho người khác (A cung cấp API / hệ thống)

Theo hợp đồng `TEAM_ASSIGNMENT.md` mục 3.1. Trạng thái = thực tế code hiện có.
Cột **3.1 #** dùng đúng số trong mục 3.1 để trao đổi giữa các người. Dòng **ngoài 3.1** là API bàn giao không có số hợp đồng → tham chiếu bảng mục 3 (event hub) hoặc Spec hệ thống tương ứng.

| 3.1 # | Điểm nối | Nội dung | Cho | Trạng thái | Ngày |
|---|---|---|---|---|---|
| #1 | Quái tới Thành | `OnEnemyReachedCastle` | B (Thành, S5) | ☑ (A-04) | xong |
| #2 | Wave | `Init`, `StartWave`, `StopAndClear`, `OnWaveFinished` | B (S5) | ☑ (A-04) | xong |
| #3 | Đặt tướng → tướng hoạt động | `Init(HeroData)`, `ResetState()`, `OnDespawn()` | B (S4 đặt/nâng tướng) | ☑ code (`main`, PR #25 merged; chờ B xác nhận) | 10 (T-06) |
| #4 | Nâng cấp → chỉ số | `HeroStats.SetLevelMultipliers` | B (S4 nâng cấp) | ☑ code (`main`, PR #25 merged; chờ B xác nhận) | 10 (T-06) |
| #5 | Nộ / Ultimate | `OnRageChanged`, `TryActivateUltimate()` | B (nút R, HUD Nộ) | ◐ code có, PR #40 mở (base `main`), chờ B nối HUD | 14 (T-10) |
| #6 | Hủy ngắm Thiên thạch | `CancelStack.Push/Remove` | C (InputRouter, Esc) | ◐ (`CancelStack` có; A-12 đã nối, PR #42 mở) | 16 (T-12) |
| #7 | Chọn tướng hủy ngắm | nghe `DeckSlot` | C | ◐ code có, PR #42 mở | 16 (T-12) |
| #8 | Vàng | `Wallet` | (A-04 **không** đụng; B sở hữu) | ☑ (B) | — |
| #9 | UI thế giới | `OnEnemySpawned/Despawned`, `ActiveEnemies`, `CurrentHP/MaxHP/IsBoss/Progress/HealthBarAnchor`, `OnHealthChanged` | C (thanh máu, cảnh báo) | ☑ (A-04) | xong |
| #10 | Hào quang | `OnHeroEnteredAura` | B (`TutorialController` bước 3) | ◐ code có, PR #41 mở (base `feature/S06_A10`) | 15 (T-11) |
| ngoài 3.1 | Thiên thạch (HUD/FX) | `OnMeteorCooldownChanged`, `OnMeteorExploded(Vector3, radius)` — theo `AbilityEvents` (mục 3) | B (HUD), C (FX) | ◐ code có, PR #42/#43 mở | 17 (T-13) |
| ngoài 3.1 | Sát thương / quái chết | `OnDamageDealt(DamageDealtInfo)`, `OnEnemyKilled(EnemyKilledInfo)` — `CombatEvents` (mục 3), cũng là nguồn Vàng của 3.1 #8 | B (kinh tế, số sát thương), C (nháy trắng) | ☑ (S1) | xong |
| ngoài 3.1 | Lưu trữ (S9) | `SaveService.Load/Save/Delete`, `SaveData`, `SaveSanitizer` (JSON Newtonsoft) | B (S8 gọi) | ☑ code · ◐ bàn giao: "Cách dùng" + "Hướng dẫn lắp" ở `daily/day08/handoff.md`, Sandbox `A_Save` đã chạy được trong Editor, chờ B xác nhận | 9 (T-04) |
| ngoài 3.1 | Quái Bay / boss gọi con | `EnemyController.Spawn(...,pathIndex,...)`, `BossSummonSettings` (SO) | C (prefab Bay); asset đã gắn sẵn | ☑ code (298/298 EM + 50/50 PM, đo lại sau tích hợp rename) | 8 (T-14) |

> **Ghi chú A-07 (T-05/T-06):** `HeroController` điều khiển `HeroVisual.Animator` bằng đúng 4 parameter — Trigger `Attack`, `Ultimate`, `Spawn` và Float `AttackSpeed` (đề xuất 3.1 #11 của C, Controller `Art/Animations/Heroes/AC_Hero_Base.controller`; A duyệt/nhận xét trên nhánh `docs/animator-contract`). Sát thương vẫn theo timer, không chờ animation.

---

## 3. Rủi ro bàn giao

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| S5 (B) chưa xong → A không playtest được màn | Cao | Test trong Sandbox A; giữ stub đúng tên 3.1 |
| S6/S7 code xong nhưng chưa lên `main`/chưa playtest | Cao | #46 → #40 → #41, #42 → #43; chạy lại EditMode/PlayMode + playtest `A_Skills` sau khi merge |
| PR #39 lỡ merge vào `feature/S06_A08` thay vì `main` | Trung bình | #46 đang mở để đưa A-08 + A-09 vào `main`; phải merge #46 trước #40 |
| Snapshot T-16 (675 EM / 116 PM) thiếu `010c558` (#33) và `4e42dc0` (#35) | Trung bình | Không dùng làm bằng chứng cho `main`; chạy lại test trên `main` sau #38 |
| PR #42 còn fixture `MeteorTestInputRouter` | Trung bình | Áp fix T-16 `10d804e` trước khi merge #42 |
| `CancelStack`/`InputRouter` (C) đổi signature | Trung bình | Chốt sớm ở T-12; theo dõi PR của C |
| HUD B gọi sai tên event | Trung bình | Dùng đúng tên 3.1; phát bằng `Raise…()` của hub |
| File cân bằng Excel bị sửa ngoài A | Trung bình | Chỉ A sửa số; người khác đề xuất qua issue |
| ~~Chưa compile S9/A-05/A-06~~ | — | **Đã hết:** compile 0 lỗi trong Editor; S9 299/299 EM + 45/45 PM, S2 298/298 EM + 50/50 PM (đo lại sau tích hợp rename) |
| ~~`FateBastion.Meta` có thể thiếu reference Newtonsoft~~ | — | **Đã hết:** S9 compile OK, không sửa asmdef |
| ~~Việc đổi tên asset đang dở (chưa commit) làm 1 test S2 đỏ~~ | — | **Đã hết:** nhánh `refactor/enemy-asset-names` (GUID giữ nguyên) merge vào 3 nhánh gameplay; test dùng id mới; còn chạy lại test để xác nhận |
| ~~Lượt chạy test bị MCP timeout, chưa có số liệu xác nhận sau tích hợp rename~~ | — | **Đã hết:** chạy lại đủ 4 lượt — `S09_A14` 299/299 EM + 45/45 PM, `S02_A06` 298/298 EM + 50/50 PM (mục 5) |

### Đề xuất hướng phát triển S6 (chưa triển khai)

- Hiện `HeroData` chỉ có một `passiveSkill`; hướng mở rộng có thể cho phép một tướng có nhiều passive độc lập, ví dụ một passive điều chỉnh đòn cơ bản (như hệ số sát thương) và một passive bổ sung hiệu ứng riêng (như độc gây sát thương theo thời gian).
- T-07 giữ nguyên schema hiện tại; `damageMultiplier` điều chỉnh sát thương đòn, còn `status` là hiệu ứng gắn thêm khi đòn trúng. Không thêm nhiều passive hoặc hiệu ứng Độc trong lượt này.
- Nếu nhóm muốn phát triển tiếp, cần chốt cách biểu diễn nhiều skill trong `HeroData`, định dạng/import CSV, trigger/cooldown và phạm vi hiệu ứng trong S6 trước khi sửa schema dùng chung hoặc importer.

### A-09 – đã merge vào nhánh A-08, đang chờ vào `main`

- `feature/S06_A09` implement buff stacking theo `(sourceType, BuffType)` và trần AttackSpeed lấy từ `HeroStatsSettings`; asset hiện có đặt ở `0.6`.
- PR #39 đã merge vào `feature/S06_A08` (`55feec2`, 30/10/2026). PR #46 (base `main`) đang mở để đưa A-08 + A-09 vào `main`; **phải merge #46 trước #40**.
- Sáu EditMode cases đã được viết; theo quyết định của TVA, vẫn chưa chạy EditMode/PlayMode riêng cho A-09 — bằng chứng hiện có là snapshot tích hợp T-16 và Unity MCP script validation.
- Handoff chi tiết và queue kiểm thử nằm ở `daily/day13/handoff.md`; `TRACEABILITY.md` chưa sửa do là tài liệu dùng chung cần duyệt.

### S6/S7 – trạng thái PR (30/10/2026)

| Việc | Nhánh | PR | Base | Trạng thái |
|---|---|---|---|---|
| A-08 | `feature/S06_A08` | #38 | `main` | ☑ merged `main` (`623c820`) |
| A-09 | `feature/S06_A09` | #39 | `feature/S06_A08` | ☑ merged vào A-08 (`55feec2`) |
| A-08 + A-09 | `feature/S06_A08` | #46 | `main` | ◐ open, `MERGEABLE/CLEAN` |
| A-10 | `feature/S06_A10` | #40 | `main` | ◐ open, Ready, `CLEAN` |
| A-11 | `feature/S07_A11` | #41 | `feature/S06_A10` | ◐ open, Ready, `CLEAN` |
| A-12 | `feature/S07_A12` | #42 | `main` | ◐ open, Ready, `CLEAN` |
| A-13 | `feature/S07_A13` | #43 | `feature/S07_A12` | ◐ open, Ready, `CLEAN` |

- Thứ tự merge đề xuất: **#46 → #40 → #41**, và **#42 → #43**.
- C còn phải gắn `CommandAura`, `IMeteorAimRaySource`, indicator và các reference vào Player prefab; B nối HUD Nộ/meteor và tutorial bước 3.
- PR #42 vẫn còn fixture `MeteorTestInputRouter`; fix T-16 `10d804e` chưa được áp.

---

## 4. Lịch sử cập nhật

| Ngày | Nội dung |
|---|---|
| 7 – 16/10 | Tạo file; 7 điểm A chờ người khác; 13 điểm nối A cung cấp (☑ 6, ◐ 1, ☐ 6); 5 rủi ro |
| 7 – 16/10 | Nhánh `feature_a05_a06_s9`: điểm nối #13 (S9 Lưu trữ) ☑, thêm #14 (quái Bay/boss gọi con); cập nhật 2 dòng chờ (S9 API đã có, cần tạo asset Unity); thêm 2 rủi ro (chưa compile, Newtonsoft) |
| 16/10 | PR #14 review (C): đánh lại bảng mục 2 theo đúng số `TEAM_ASSIGNMENT` 3.1 — cột `3.1 #` (#1–#10), gộp dòng cũ #4 "Tướng ra đòn/chết" trùng lặp vào `CombatEvents`, 4 dòng còn lại ghi **ngoài 3.1** kèm tham chiếu; thêm ghi chú 4 parameter Animator cho A-07 |
| 16/10 | Sau khi PR #14 merge: tách day07 thành 4 nhánh theo quy ước `feature/<Sxx>_<mã>` (bỏ nhánh cha/con) — `feature/S09_A14` (A-14/A-15), `feature/S02_A05` (A-05), `feature/S02_A06` (A-06), `docs/agent-tools`; nhánh cũ `feature_a05_a06_s9` merge `main` làm backup. Sau đó: nhánh `refactor/enemy-asset-names` (đổi id quái sang tiếng Anh, GUID giữ nguyên) merge vào 3 nhánh gameplay; API S9 giữ nguyên tên, không đổi bàn giao. Ngày 8: thêm hồ sơ bàn giao S9 ("Cách dùng", "Hướng dẫn lắp") + Sandbox `A_Save` (mở Editor + Play thử OK); dòng S9 chuyển ◐ bàn giao, chờ B xác nhận. Chạy lại test sau tích hợp rename: **`S09_A14` 299/299 EM + 45/45 PM, `S02_A06` 298/298 EM + 50/50 PM** (0 fail); thêm luật 9 MCP timeout vào `README.md` |
| T-07 | Ghi nhận đề xuất tương lai hỗ trợ nhiều passive độc lập; giữ nguyên schema hiện tại cho T-07, chờ chốt S6/importer trước khi mở rộng |
| T-08 | Bổ sung Cone/Line/Zone/Buff, chốt hình học theo quyết định TVA, nối status Lửa/Băng vào SkillData; PR #38 draft trên `feature/S06_A08`; test S6 pass trong T-16 integrated suite (675 EM, 116 PM), scene A_Skills đã tạo nhưng chưa Playtest; Projectile draft do TVA tạo |
| T-09 | A-09 commit đã push; PR #39 merge vào `feature/S06_A08` (`55feec2`); PR #46 mở để đưa A-08 + A-09 vào `main` |
| 30/10/2026 | Đồng bộ lại bàn giao theo topology PR mới: #38 merged `main`, #39 merged A-08, #46 open; #40–#43 Ready/`CLEAN`; cập nhật #3–#7, #10, thiên thạch, thêm bảng trạng thái PR và 3 rủi ro mới |

---

## 5. Test sau tích hợp rename (MCP timeout – luật 9 `README.md`)

> Ghi theo luật **MCP timeout**: khi timeout ≥ 3 lần thì bỏ lượt chờ, ghi trạng thái vào đây + `daily/day08/handoff.md`, làm việc khác rồi quay lại. **Hiện tất cả đã chạy xong** — bảng dưới giữ lại job id để đối chiếu, không phải việc còn dở.

| Việc | Trạng thái cuối | Cách chạy tiếp | Xong khi |
|---|---|---|---|
| PlayMode `FateBastion.Tests.PlayMode` trên `feature/S09_A14` | **Xong:** 45/45 pass, 0 fail, 0 skip (9,2 s) — job `d0b86b63452c4ae684406d3279ad40a9` | Không cần chạy lại | ✅ |
| EditMode + PlayMode trên `feature/S02_A06` sau tích hợp `refactor/enemy-asset-names` | **Xong:** EditMode 298/298 + PlayMode 50/50 pass (0 fail) — job `7e3ee7cea45e4cb3a9b6f4d290ed8cdb` / `f9c0653071824babab00f5e38cba3321` | Không cần chạy lại | ✅ |
| EditMode `FateBastion.Tests.EditMode` trên `feature/S09_A14` | **Xong:** 299/299 pass, 0 fail, 0 skip (93,5 s) — job `114c9205c6cb4f7bb1c8026d079ce1b6` | Không cần chạy lại | ✅ |
| EditMode toàn Editor trên `feature/S09_A14` | **Xong:** 1912 test — 1840 pass, 3 fail, 69 skip (751 s). 3 fail đều là `UnitySkills.Tests.Core.*` của package `com.besty.unity-skills`, **không có test `FateBastion.*` nào đỏ** | Không cần chạy lại | — |

## 6. T-07/T-08 / A-08 — đã merge `main`, test tích hợp T-16 là snapshot cũ

- PlayMode cases trong `Assets/_Project/Tests/PlayMode/S6/AttackModulePlayModeTests.cs` hiện bao gồm Melee + multiplier/status, pooled Projectile launch từ Muzzle, AreaAtPoint bounds/flying, Cone geometry, Line/Zone hit bounds, Line origin khi cast delay, Zone Slow refresh không gây lặp sát thương gốc, Buff vào/ra bán kính và cooldown theo scaled delta.
- T-16 integrated branch có các test S6/A-08 (`AttackModulePlayModeTests`, `HeroControllerPlayModeTests`, `HeroTargetingPlayModeTests` và ba bộ EditMode); lượt test toàn bộ ghi nhận EditMode **675/675 pass** và PlayMode **116/116 pass**, không fail/skip. Job: `cb4aede713514d83b261bbeff260396a` (EditMode, 329.7 s), `87b378136e49444aa9d5c386d4ebc169` (PlayMode, 41.1 s). Đây là snapshot **lịch sử** (thiếu `010c558` và `4e42dc0`) nên không thay thế lượt test trên `main` sau #38.
- `Assets/_Project/Scenes/Sandbox/A_Skills.unity` đã tạo trên A-08 với ground, camera và directional light. Chưa Playtest, chưa wiring để tự khởi tạo hero/enemy; scene này chưa được tính là nghiệm thu tay hoặc demo đầy đủ.
- PR #38 (`feature/S06_A08`) đã merge vào `main` (`623c820`, 30/10/2026). Snapshot T-16 nêu trên **thiếu** hai commit cuối `010c558` (#33) và `4e42dc0` (#35), nên chỉ là bằng chứng lịch sử; cần chạy lại test trên `main` hiện tại.
- `hero_phu_thuy_lua_passive` đã nối `status_burn`; `hero_linh_bang_passive` và `hero_nguoi_tuyet_passive` đã nối `status_slow`. Đây là gán tham chiếu thủ công trong Unity, không sửa CSV/importer.
- Đã tạo `hero_long_vuong_ultimate` kiểu Line (length 5 m, width 5 m, x1, duration 5 s) và gán vào `hero_long_vuong.ultimateSkill`; đã tạo `hero_nguoi_tuyet_zone` (radius 3 m, x1, duration 5 s, status Slow). Cooldown/castDelay còn mặc định 0 vì tài liệu chưa cho số. Các giá trị TVA cung cấp tạm thời cho T-08; **TVA cần kiểm tra/chỉnh tay trước khi chốt cân bằng**. Line/Zone chỉ gây sát thương gốc một lần mỗi enemy trong field; status được làm mới khi enemy còn ở trong field, không lặp sát thương gốc.
- `Hero_Base.prefab` có `SkillRunner`, pool prewarm mặc định 8; `_projectilePrefab` đã trỏ tới `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` (draft do TVA tạo, root có `FateBastion.Combat.Projectile`, child hình cầu đơn giản).
- Chưa có VFX gắn cho hai sample Ultimate/Zone; cần C bàn giao FX prefab để A gán các tham chiếu `SkillData.vfx`.
- Unity MCP standard validation báo 0 lỗi cho các script đã đổi; hai cảnh báo heuristic về string trong `Update()` xuất hiện ở `SkillRunner`/`HeroController`, nhưng hai thân `Update()` không nối chuỗi. Console chỉ có log môi trường Unity Assistant/MCP (AssistantPerf sharing violation, WebSocket chưa khởi tạo, account API timeout), không có lỗi biên dịch.
- Projectile draft đã được TVA tạo để unblock A-08; C có thể review, thay model/VFX hoặc thay prefab, giữ tương thích với `FateBastion.Combat.Projectile`. Vẫn cần C bàn giao FX cho Long Vương Line và Người Tuyết Zone để A gán tham chiếu `SkillData.vfx`.
- Còn kiểm tra tay trong Unity: Playtest scene A_Skills, xác nhận behavior/visuals và rà soát tiêu chí S6 chưa được covered bởi suite (cooldown ×1/×2, Nộ/Ultimate, buff stacking, boss targeting, thêm skill chỉ bằng data). Không coi suite pass là bằng chứng playtest.

## 7. A-08 – quyết định projectile draft (29/10/2026)

- Theo yêu cầu của TVA, TVA (A) đã tạo `Assets/_Project/Prefabs/FX/Projectile_Base.prefab` bản draft và gán vào `_projectilePrefab` của `Hero_Base.prefab`, không chờ C bàn giao prefab hoàn thiện. Root giữ contract `FateBastion.Combat.Projectile`; child hiện là hình cầu đơn giản, có thể thay.
- Nếu C không đồng ý hoặc muốn bổ sung projectile khác, C chủ động liên hệ A để thay thế/bàn giao, tránh block việc A-08.
- C có thể review, sửa/thay prefab và phối hợp với A nếu cần; phần draft được ghi nhận cho TVA, không ghi công cho C. Runtime/Sandbox chưa được kiểm tra; validation vẫn chờ sau T-16.
- `TRACEABILITY.md` ghi chú A-08 và C-09 về người tạo draft và quyền C chỉnh sửa/thay thế. Sau khi TVA xác nhận PR chung đủ 3 người duyệt, quy tắc takeover đã được bổ sung ở `TEAM_ASSIGNMENT.md` mục 5 và đồng bộ trong `CLAUDE.md`.
