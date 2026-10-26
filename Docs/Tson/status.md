# Hiện trạng tổng thể – Thành viên A (Gameplay)

> Cập nhật: **Ngày 7 – 16/10/2026**. Đây là hiện trạng **tổng thể**; hiện trạng cuối mỗi ngày nằm ở `daily/dayNN/status.md`.
> Ký hiệu: ☐ chưa · ◐ đang · ☑ xong. Bằng chứng = file code/test thật đã kiểm.

---

## 1. Tóm tắt nhanh

| Hệ thống | Mã việc A | Trạng thái | Ghi chú |
|---|---|---|---|
| S0 – Dùng chung | S0-xx | ◐ | Tool import + test xong; **S0-15 (hợp đồng API) chưa có PR 3 người duyệt**; S0-09/11/14 dọn nhỏ còn lại |
| S1 – Chiến đấu & sát thương | A-01, A-02, A-03 | ☑ | Xong (C làm hộ), 33 EM + 93 EM + 5 PM |
| S2 – Quái và Wave | A-04 ☑ · A-05 ☐ · A-06 ☐ | ◐ | A-04 xong (49 EM + 27 PM); **quái Bay + boss gọi con chưa**; **máu `hpMultiplier` + 3 màn + Vô tận chưa** |
| S6 – Kỹ năng & Ultimate | A-08, A-09, A-10 | ☐ | Chỉ có `SkillData` + `PassiveTrigger`; `Heroes/Runtime` trống; `Skills/` chưa có `SkillRunner` |
| S7 – Hào quang & Thiên thạch | A-11, A-12, A-13 | ☐ | `Player/Abilities/` trống; `CancelStack` (C, Core) đã có sẵn để dùng |
| S9 – Lưu trữ | A-14, A-15 | ☐ | `Meta/` trống |
| Cân bằng | A-17 | ☐ | Chưa bắt đầu (cần game chơi được) |

**Tổng:** A còn nợ **A-05, A-06, A-07…A-15, A-17** (12 việc). S1 xong; S2 gần xong; S6/S7/S9 chưa bắt đầu.

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
- ☐ **A-05:** quái Bay (đường riêng, cao 2 m) + boss gọi 3 quái Nhanh/12 s — **chưa có** (`BossSummonAbility` không tồn tại).
- ☐ **A-06:** máu theo `hpMultiplier` — có `EnemyHealthFormula` nhưng **chưa** gắn dữ liệu 3 màn + Vô tận; `EndlessSettings` còn là **STUB**.
- ⚠️ Test `PlayMode/S2` có tham chiếu tạm cho `BossSummonAbility` — khi làm T-14 phải đối chiếu lại.

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
- ☐ **A-14/A-15**: `Meta/Save/` **trống**. Chưa có `SaveData`, `SaveService`.

### A-17 – Cân bằng
- ☐ Chưa bắt đầu. File nguồn: `Docs/root/Can bang - Fate Bastion.xlsx` → `Data/Balance/{heroes,enemies,waves}.csv`.

---

## 3. Điểm lệch & cảnh báo (cần theo dõi)

| # | Lệch | Ảnh hưởng | Xử lý |
|---|---|---|---|
| 1 | S0-15 code đã merge nhưng TRACEABILITY ☐ | "Hợp đồng API" chưa được đóng chính thức | Mở PR 3 người duyệt (T-01/T-02) rồi tick |
| 2 | S2 chưa có quái Bay + boss gọi con (A-05) | Wave có boss 5/10/15 chưa kết thúc đúng; R10 chưa đạt | T-14 (day18) |
| 3 | Máu 3 màn + Vô tận chưa có (A-06) | R11/R12/R28 chưa đạt; RNG/S2 chưa "xong" | T-15 (day19) |
| 4 | S6/S7/S9 chưa bắt đầu, trong khi M2 (23/10) yêu cầu trận hoàn chỉnh | Nguy cơ trễ M2 ~3 ngày | Dồn T-07…T-13, bù ở T-16 |
| 5 | `EndlessSettings` là STUB | Vô tận chưa chạy thật | Gỡ ở T-15 |
| 6 | Đường dẫn tài liệu cũ (`Docs/GDD…docx`) đã chuyển sang `Docs/root/` | `CLAUDE.md`/`README.md` còn trỏ đường cũ | Cập nhật ở T-01 |

---

## 4. Lịch sử cập nhật

| Ngày | Nội dung |
|---|---|
| 7 – 16/10 | Tạo file; ghi nhận S0 ◐, S1 ☑, S2 ◐, S6/S7/S9 ☐; liệt kê 6 điểm lệch |
