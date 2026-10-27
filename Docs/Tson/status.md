# Hiện trạng tổng thể – Thành viên A (Gameplay)

> Cập nhật: **Ngày 7 – 16/10/2026** (cuối ngày, sau khi tách nhánh ngày 7 thành `feature/S09_A14`, `feature/S02_A05`, `feature/S02_A06`, `docs/agent-tools`). Đây là hiện trạng **tổng thể**; hiện trạng cuối mỗi ngày nằm ở `daily/dayNN/status.md`.
> Ký hiệu: ☐ chưa · ◐ đang · ☑ xong. Bằng chứng = file code/test thật đã kiểm.

---

## 1. Tóm tắt nhanh

| Hệ thống | Mã việc A | Trạng thái | Ghi chú |
|---|---|---|---|
| S0 – Dùng chung | S0-xx | ◐ | Tool import + test xong; **S0-15 (hợp đồng API) chưa có PR 3 người duyệt**; S0-09/11/14 dọn nhỏ còn lại |
| S1 – Chiến đấu & sát thương | A-01, A-02, A-03 | ☑ | Xong (C làm hộ), 33 EM + 93 EM + 5 PM |
| S2 – Quái và Wave | A-04, A-05, A-06 | ☑ | A-04 xong; **A-05 (Bay + boss gọi con)** và **A-06 (hpMultiplier + Vô tận)** xong ngày 7 — **đo lại sau tích hợp rename: 298/298 EM + 50/50 PM** |
| S6 – Kỹ năng & Ultimate | A-08, A-09, A-10 | ☐ | Chỉ có `SkillData` + `PassiveTrigger`; `Heroes/Runtime` trống; `Skills/` chưa có `SkillRunner` |
| S7 – Hào quang & Thiên thạch | A-11, A-12, A-13 | ☐ | `Player/Abilities/` trống; `CancelStack` (C, Core) đã có sẵn để dùng |
| S9 – Lưu trữ | A-14, A-15 | ☑ | Xong ngày 7 (compile OK; **299/299 EM** toàn nhánh, đo lại sau tích hợp rename): `SaveData`, `SaveService`, `SaveSanitizer`; `IStarterSaveFactory` là stub chờ B-11 |
| Cân bằng | A-17 | ☐ | Chưa bắt đầu (cần game chơi được) |

**Tổng:** A còn nợ **A-07, A-08…A-13, A-17** (8 việc). S1/S2/S9 xong (đã compile + test xanh); S6/S7 chưa bắt đầu.

---

## 2. Trạng thái chi tiết theo hệ thống (có bằng chứng)

### S0 – Interface & dữ liệu dùng chung (vùng khóa, cả nhóm)
- ☑ Tool `Tools → Import Balance CSV` + bộ đọc CSV + test; 9 `HeroData`, 9 `SkillData`, 5 `EnemyData`, 45 `WaveData`, 3 `LevelData`.
- ☑ Core event hub (`CombatEvents`, `GameEvents`, `HeroEvents`, `AbilityEvents`), enum, payload struct, `CancelStack`, `InputRouter` — đã có file.
- ◐ **S0-15 chưa tick** trong `TRACEABILITY.md` dù code đã merge (PR #5) → **cần PR 3 người duyệt để đóng**.
- ◐ S0-09 (tag/layer), S0-11 (ProjectSettings), S0-14 (scene) còn dọn nhỏ.

### S1 – Chiến đấu và sát thương (A, làm hộ bởi C)
- ☑ `Combat/DamageCalculator.cs` — một chỗ tính sát thương, kháng kẹp [0;0.8], True bỏ qua kháng, tối thiểu 1.
- ☑ `Combat/StatusEffectController.cs`, `ChainLightning.cs`, `CombatSettings.cs` — Bỏng mạnh nhất, Chậm ≤60% (boss 50%), Nảy không lặp.
- ☑ `Combat/Health.cs`, `ProjectileFlight.cs`, `Projectile.cs`, `CombatMath.cs` — chết một lần/đạn tới vị trí cuối/XZ.
- **Bằng chứng:** `33` EM (A-01) · `93` EM + `5` PM (A-03). Sandbox `A_Combat`.

### S2 – Quái và Wave (A)
- ☑ A-04: `EnemyController`, `EnemyManager` (≤100 quái), `WaveManager` (nhóm song song, lệch 2 s), pool, `WaveSchedule`, `SpawnQueue`, `SplineEnemyPath`, `LevelPaths` (`MainPathIndex=0`, `FlyingPathIndex=1`). API 3.1 #1/#2/#9 đủ.
  - **Bằng chứng:** `49` EM + `27` PM. Sandbox `A_Enemies` (`WaveSandboxDemo`).
- ☑ **A-05:** quái Bay (đường riêng `pathIndex=1`, cao 2 m; Melee bỏ qua có test) + boss gọi 3 quái Nhanh/12 s — `Enemies/BossSummonSettings.cs` (SO), `Enemies/BossSummonAbility.cs`; nối `EnemyController.Initialize/Spawn/Tick` + `EnemyManager` buffer sinh sau vòng di chuyển.
  - **Bằng chứng:** `8` EM (`BossSummonAbilityTests`) + `4` PM (`BossSummonPlayModeTests`) — **đã chạy, pass**. Asset `Data/Settings/BossSummonSettings.asset` đã tạo + gắn `_bossSummonSettings` trong `A_Enemies.unity`.
- ☑ **A-06:** máu theo `hpMultiplier` (dữ liệu `level_1/2/3` = 1.0/1.15/1.3 đã đúng từ trước) + Vô tận lặp `loopFromWave..loopToWave` (màn 3 = 6…15) trong `WaveManager.ResolveWave`, giữ **wave thật** cho `hpGrowth`; `EndlessSettings.Configure`.
  - **Bằng chứng:** `4` EM (`LevelBalanceDataTests` đọc asset thật) + `1` PM (`Endless_WavesBeyondLast_RepeatLoopBlockWithRealWaveNumbers`). **Đã chạy lại sau tích hợp rename trên `feature/S02_A06`: EditMode 298/298 + PlayMode 50/50 pass** (0 fail), gồm 4 EM + 1 PM này.
- ⚠️ Đã lắp sẵn trong Unity: `Data/Settings/BossSummonSettings.asset` + `_bossSummonSettings` trong `A_Enemies.unity`. Chỉ còn **prefab quái Bay/boss** chờ C.

### S6 – Kỹ năng và Ultimate (A)
- ◐ `Skills/SkillData.cs`, `Skills/PassiveTrigger.cs` — mới có data.
- ☐ **A-07** `Heroes/Runtime/` **trống hoàn toàn** (chưa có `HeroController`, `HeroStats`, `TargetingModule`, `AttackModule`).
- ☐ **A-08** chưa có `SkillRunner` và 7 kind.
- ☐ **A-09** cộng buff/trần +60% chưa có.
- ☐ **A-10** Nộ/Ultimate Long Vương chưa có.

### S7 – Hào quang & Thiên thạch (A)
- ☐ **A-11/A-12/A-13**: thư mục `Player/Abilities/` **trống**. `Core/CancelStack.cs` đã có (C) → dùng được cho T-12.
- Ghi chú: `Player/Movement/` (S3, C) đã xong — `PlayerController`, `CameraRig`, `SummonerInputRouter`, `CancelPressRouter`…, làm nền cho A-12 nghe `DeckSlot`.

### S9 – Lưu trữ (A)
- ☑ **A-14/A-15:** `Meta/Save/` có `SaveData`, `SaveSettings`, `SaveService`, `SaveSanitizer`, `IStarterSaveFactory` (STUB chờ B-11).
- Ghi an toàn `save.tmp` → `File.Replace`(json,bak) / `File.Move` (lần đầu); đọc `save.json` → `save.bak` → tạo mới; `Load()` trả `SaveLoadResult{Data,Kind,Error}` (không throw).
  - **Bằng chứng:** `13` EM (`SaveServiceTests`, `SaveSanitizerTests`) — **đã chạy, pass**; toàn bộ EditMode của nhánh `feature/S09_A14` **299/299** (chạy lại sau tích hợp rename).
- ◐ **T-04 (bàn giao S9 cho B):** hồ sơ "Cách dùng" + "Hướng dẫn lắp trong Unity" ở `daily/day08/handoff.md`; Sandbox `Scripts/Meta/Sandbox/SaveSandboxDemo.cs` + `Scenes/Sandbox/A_Save.unity` đã mở trong Editor, Play ~4 s không exception, tạo `persistentDataPath/SandboxSave` → **4.1 #4 ☑**. Chờ B xác nhận (4.1 #5) và PR.

### A-17 – Cân bằng
- ☐ Chưa bắt đầu. File nguồn: `Docs/root/Can bang - Fate Bastion.xlsx` → `Data/Balance/{heroes,enemies,waves}.csv`.

---

## 3. Điểm lệch & cảnh báo (cần theo dõi)

| # | Lệch | Ảnh hưởng | Xử lý |
|---|---|---|---|
| 1 | S0-15 code đã merge nhưng TRACEABILITY ☐ | "Hợp đồng API" chưa được đóng chính thức | Mở PR 3 người duyệt (T-01/T-02) rồi tick |
| 2 | ~~S2 chưa có quái Bay + boss gọi con (A-05)~~ | Đã xử lý ngày 7 (T-14) | ☑ code xong; tick TRACEABILITY khi PR 3 người duyệt |
| 3 | ~~Máu 3 màn + Vô tận chưa có (A-06)~~ | Đã xử lý ngày 7 (T-15) | ☑ code xong; tick TRACEABILITY khi PR 3 người duyệt |
| 4 | S6/S7 chưa bắt đầu, trong khi M2 (23/10) yêu cầu trận hoàn chỉnh | Nguy cơ trễ M2 ~3 ngày | Dồn T-07…T-13, bù ở T-16 |
| 5 | ~~`EndlessSettings` là STUB~~ | Đã xử lý ngày 7 (T-15) | ☑ `ResolveWave` lặp wave + `Configure` |
| 6 | Đường dẫn tài liệu cũ (`Docs/GDD…docx`) đã chuyển sang `Docs/root/` | `CLAUDE.md`/`README.md` còn trỏ đường cũ | Cập nhật ở T-01 |
| 7 | ~~Chưa compile/test trong Unity~~ | — | **Đã hết:** Editor mở đúng project, compile 0 lỗi; S9 **299/299 EM + 45/45 PM**, S2 **298/298 EM + 50/50 PM** (đo lại sau tích hợp rename) |
| 8 | ~~`FateBastion.Meta` có thể chưa reference Newtonsoft~~ | — | **Đã hết:** S9 compile OK, không sửa asmdef |
| 9 | `IStarterSaveFactory` là STUB chờ B-11 (`StarterSettings`) | Save mới chưa có 5 tướng/1000 Ngọc | Gỡ khi S8/B-11 xong |
| 10 | ~~Đổi tên asset đang dở, chưa commit (`enemy_thuong` → `enemy_normal`, …)~~ | — | **Đã xong:** nhánh `refactor/enemy-asset-names` (2 commit, GUID giữ nguyên) merge vào 3 nhánh gameplay; test dùng id mới; còn **chạy lại test** để xác nhận |
| 11 | ~~Scene `Sandbox/A_Save` (T-04) soạn tay, chưa mở trong Unity~~ | — | **Đã hết:** mở trong Editor, hierarchy đúng 3 root, Play ~4 s không exception, tạo `persistentDataPath/SandboxSave` (4.1 #4 ☑) |
| 12 | ~~Lượt test sau tích hợp rename chưa có số liệu~~ | — | **Đã xanh hết:** `feature/S09_A14` 299/299 EM + 45/45 PM; `feature/S02_A06` 298/298 EM + 50/50 PM (0 fail). Lượt toàn Editor 1912 test có 3 fail đều của package `com.besty.unity-skills` |

---

## 4. Lịch sử cập nhật

| Ngày | Nội dung |
|---|---|
| 7 – 16/10 | Tạo file; ghi nhận S0 ◐, S1 ☑, S2 ◐, S6/S7/S9 ☐; liệt kê 6 điểm lệch |
| 7 – 16/10 | Ngày 7: S9 (A-14/A-15) xong; A-05 (Bay + boss gọi con) xong; A-06 (hpMultiplier + Vô tận) xong. 30 test mới (25 EM + 5 PM). S2/S9 → ☑. Lệch 2/3/5 gỡ. Sau PR #14: tách 4 nhánh `feature/S09_A14`, `feature/S02_A05`, `feature/S02_A06`, `docs/agent-tools` (bỏ nhánh gộp cũ). Kiểm chứng trong Unity: compile 0 lỗi, S9 286/286 EM, S2 297/298 EM + 50/50 PM; lệch 7/8 gỡ, thêm lệch 10 (đổi tên asset dở). Sau đó: nhánh riêng `refactor/enemy-asset-names` (đổi id quái sang tiếng Anh, GUID giữ nguyên) merge vào 3 nhánh gameplay, sửa test theo id mới → gỡ lệch 10; **chưa chạy lại** test sau tích hợp. Ngày 8: T-04 ◐ — hồ sơ bàn giao S9 (Cách dùng + Hướng dẫn lắp) + Sandbox `A_Save` (đã mở Editor + Play thử) → gỡ lệch 11; EditMode toàn Editor 1912 test: 1840 pass, 3 fail đều của package `com.besty.unity-skills`, 69 skip; **EditMode `FateBastion.Tests.EditMode` 299/299 pass** (lấy được job sau 2 lần MCP timeout — luật 9), **PlayMode 45/45 pass**; `S02_A06`: **298/298 EM + 50/50 PM**; chờ B xác nhận |
