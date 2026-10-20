# Plan 22 ngày – Thành viên A (Gameplay)

> **Ngày 1 = 10/10/2026 · Ngày 22 = 31/10/2026.** Đây là **lịch làm việc cá nhân của Thành viên A** cho phạm vi việc của A, **không** phải lịch của nhóm.
> **Không dùng plan này để sửa `Docs/ROADMAP.md` hay `TRACEABILITY.md`.** Thành viên khác cứ theo `ROADMAP.md` hiện tại; A làm sớm hơn (hoặc trễ hơn) một số việc theo lịch riêng. Việc A phải chờ tính năng của người khác → ghi vào `handoff.md` của A, không đổi mốc/bàn giao chung và không ảnh hưởng tới công việc của thành viên khác.
> Mã việc **T-xx** là mã mới (đánh lại theo ưu tiên và thứ tự làm); cột **TRACEABILITY** trỏ về mã gốc A-xx để đối chiếu tiêu chí kiểm tra.
> Ưu tiên: **P1** không cần thao tác/tạo asset Unity · **P2** cần thao tác Unity · **P3** mâu thuẫn/bàn giao giữa thành viên. Luật đầy đủ ở `README.md`.
> Ký hiệu trạng thái: ☐ chưa · ◐ đang · ☑ xong.

---

## 1. Ngày 1–6 (10/10 – 15/10) — đã hoàn thành

A bận, **C (nhóm trưởng)** làm hộ. Từ Ngày 7 A ghi nhật ký.

| Ngày | Ngày thực | Việc | Mã gốc | Trạng thái | Ghi chú |
|---|---|---|---|---|---|
| 1 | 10/10 | S0: repo, Git LFS, tags/layers, scene Boot/Lobby/Game/Sandbox, package | S0-01, S0-06, S0-07, S0-08 | ☑ | Cả nhóm |
| 2 | 11/10 | S0: enum + Data SO + Core event hub, Import Balance CSV, test | S0-03…S0-05, S0-10, S0-12, S0-13 | ◐ | S0-15 (API contract) cần PR 3 người duyệt; S0-09/11/14 dọn nhỏ |
| 3 | 12/10 | S1: `DamageCalculator` + test | A-01 | ☑ | C làm hộ, 33 test EM |
| 4 | 13/10 | S1: `StatusEffectController`; chết một lần/đạn tới vị trí cuối/XZ | A-02, A-03 | ☑ | C làm hộ |
| 5 | 14/10 | S2: `EnemyController`, `EnemyManager`, `WaveManager`, pool | A-04 | ☑ | C làm hộ; Vô tận là STUB |
| 6 | 15/10 | (dự kiến) quái Bay + boss gọi con; máu theo `hpMultiplier`; bắt đầu `HeroController` | A-05, A-06, A-07 | ☐ → **dời** | Chuyển thành T-14 (A-05), T-15 (A-06), T-05 (A-07). **T-14/T-15 đã xong Ngày 7**; T-05 để Nguyên Ngày 9 |

**Nợ từ ngày 6 chuyển sang plan mới:** A-05 → T-14, A-06 → T-15, A-07 → T-05/T-06.

---

## 2. Plan Ngày 7–22 (16/10 – 31/10)

| Mã | Ngày | Ngày thực | Việc | TRACEABILITY | Ưu tiên | Phụ thuộc / Handoff | Nhánh | Kiểm tra | TT |
|---|---|---|---|---|---|---|---|---|---|
| **T-01** | 7 | 16/10 | Dựng khu `Docs/Tson` (README, plan, status, handoff, day07) + SRS toàn game `Docs/SRS.md` | — | P1 | — | `docs_tson_setup` | Tay | ☑ |
| **T-02** | 7 | 16/10 | `SaveData`, `SaveService` ghi an toàn (tmp → Replace/Move → bak) | A-14 | **P1** | — | `feature/S09_A14` | EM | ☑ |
| **T-03** | 7 | 16/10 | Đọc lỗi → `save.bak` → tạo mới từ `IStarterSaveFactory`; test lần lưu đầu | A-15 | **P1** | — | `feature/S09_A14` | EM | ☑ |
| **T-04** | 8 | 17/10 | Bàn giao API S9 cho B (`SaveService.Load/Save/Delete`, `SaveData`, `SaveSanitizer`) theo TEAM 4.1 | A-14, A-15 | P3 | **B chờ** (S8 lưu) | — | PR | ◐ |
| **T-05** | 9 | 18/10 | `HeroController` + `HeroStats` + `TargetingModule` (Đầu đoàn, DPS đơn ưu tiên boss, Melee không chọn Bay) | A-07 | P2 | S0b `HeroVisual` ☑ (C) | `feature_a07_hero_ctrl` | EM/PM | ☐ |
| **T-06** | 10 | 19/10 | `AttackModule` (timer, bắn từ `Muzzle`); PM Sandbox; **bàn giao API #3, #4 cho B** | A-07 | P2/P3 | **B chờ** (S4) | `feature_a07_attack` | PM | ☐ |
| **T-07** | 11 | 20/10 | `SkillRunner` + kind MeleeHit, Projectile, AreaAtPoint | A-08 | P1 | T-05 | `feature_a08_skills_core` | EM/PM | ☐ |
| **T-08** | 12 | 21/10 | kind Cone, Line, Zone, Buff + kỹ năng mẫu; **bàn giao S6** | A-08 | P2/P3 | A-10/A-11 chờ | `feature_a08_skills_kinds` | PM | ☐ |
| **T-09** | 13 | 22/10 | Cộng buff: cùng loại lấy mạnh nhất, khác nguồn cộng, trần **+60%** (2 Thánh Nữ = +30%; Thánh Nữ + Hào quang = +55%) | A-09 | **P1** | T-05 (`HeroStats`) | `feature_a09_buff_stack` | EM | ☐ |
| **T-10** | 14 | 23/10 | Nộ +5/đòn, Ultimate Long Vương kích hoạt tay; **bàn giao API #5** | A-10 | P2/P3 | **B chờ** (thanh Nộ) | `feature_a10_rage_ultimate` | EM | ☐ |
| **T-11** | 15 | 24/10 | `CommandAura` (6 m, +25%, 5 lần/s, chỉ tính lại khi vào/ra) + event #10 | A-11 | P2 | **B chờ** (Tutorial bước 3) | `feature_a11_command_aura` | PM | ☐ |
| **T-12** | 16 | 25/10 | `MeteorAbility` state machine (hủy không tốn hồi chiêu, hồi 45 s) + `CancelStack` + nghe `DeckSlot` | A-12 | **P1** | `CancelStack` ☑ (C) | `feature_a12_meteor` | EM | ☐ |
| **T-13** | 17 | 26/10 | `MeteorAimIndicator` + sát thương theo wave × `k_màn`, trúng quái Bay; **bàn giao S7** | A-13 | P2/P3 | **B chờ** (ô Thiên thạch) | `feature_a13_meteor_aim` | EM/PM | ☐ |
| **T-14** | 18 | 27/10 | Quái Bay (đường riêng, cao 2 m) + boss gọi 3 quái Nhanh mỗi 12 s | A-05 | P2 | — | `feature/S02_A05` | PM | ☑ |
| **T-15** | 19 | 28/10 | Máu theo `hpMultiplier`; dữ liệu 3 màn + Vô tận; kiểm tra sau import khớp sheet Wave | A-06 | P2 | Import CSV ☑ (B) | `feature/S02_A06` | EM | ☑ |
| **T-16** | 20 | 29/10 | Ghép gameplay A với S4/S5 (B) + FX (C); sửa lỗi tích hợp; playtest 3 màn | A-04…A-13 | P2/P3 | **B, C** | `feature_a_integration` | Tay | ☐ |
| **T-17** | 21 | 30/10 | Cân bằng lần 1 (Excel → CSV → import), chơi 3 màn — **M3 khóa tính năng** | A-17 | P3 | cần game chơi được (B, C) | `feature_a17_balance_1` | Tay | ☐ |
| **T-18** | 22 | 31/10 | Cân bằng lần 2 + rà soát tổng thể (skill `tson-project-audit`) + tick `TRACEABILITY.md`, đóng plan | A-17 | P3 | — | `feature_a17_balance_2` | Tay | ☐ |

> **Cập nhật 16/10 (Ngày 7):** làm sớm T-02, T-03, T-14, T-15 trong ngày 7 (đều P1/P2, độc lập, B đang review PR tài liệu). **Sau khi PR #14 merge:** tách thành các nhánh đúng quy ước `feature/<Sxx>_<mã>` — `feature/S09_A14` (T-02+T-03), `feature/S02_A05` (T-14), `feature/S02_A06` (T-15, xếp chồng trên A-05), thêm `docs/agent-tools` (công cụ agent, phát sinh T-19); nhánh gộp cũ `feature_a05_a06_s9` đã merge `main` và giữ làm backup (không PR). **T-04 ◐** (API S9 đã có, chờ B nối S8). **A-07/T-05 vẫn để nguyên Ngày 9** — không làm sớm. 30 test mới (25 EM + 5 PM), **chưa compile** (Editor đang mở giữ project → chạy Test Runner trong Editor, xem `handoff.md`). **A-07 (T-05/T-06) nhớ 4 parameter Animator** để `HeroController` gọi qua `HeroVisual.Animator`: Trigger `Attack`, `Ultimate`, `Spawn` + Float `AttackSpeed` (đề xuất 3.1 #11 của C, `AC_Hero_Base.controller` — xem `handoff.md` mục 2).

---

## 3. Tóm tắt theo ưu tiên

| Ưu tiên | Việc | Ngày |
|---|---|---|
| **P1 – không cần Unity** | T-01, T-02, T-03, T-07, T-09, T-12 | 7, 7, 8, 11, 13, 16 |
| **P2 – cần thao tác Unity** | T-05, T-06, T-08, T-10, T-11, T-13, T-14, T-15, T-16 | 9–20 |
| **P3 – bàn giao/mâu thuẫn** | T-04, T-06, T-08, T-10, T-11, T-13, T-16, T-17, T-18 | rải rác, gom về cuối |

**Nguyên tắc:** P1 làm trước; P2 sau; P3 (bàn giao) khi có điều kiện và gom về cuối. Nhưng phụ thuộc kỹ thuật thắng ưu tiên — ví dụ T-09 (P1) làm sau T-05 (P2) vì cần `HeroStats`.

---

## 4. Đối chiếu mốc nhóm (`ROADMAP.md`)

> Bảng này chỉ để **A đối chiếu** tiến độ của mình với mốc chung — **không sửa `ROADMAP.md`**. Mốc, ngày và bàn giao của nhóm vẫn theo `ROADMAP.md` hiện tại; ai cần biết khi nào A xong `Hero_Base`/`HeroController` thì xem cột "Ngày thực" của T-05/T-06 dưới đây hoặc `handoff.md` của A.

| Mốc | Điều kiện | Liên quan đến A |
|---|---|---|
| M1 – Graybox chơi được (cuối Ngày 7 = 16/10) | Đã có từ trước; **S5 (B) chưa xong** nên chưa chơi trọn màn | T-05/T-06 đóng góp `HeroController` |
| M2 – Trận hoàn chỉnh (cuối Ngày 14 = 23/10) | Cần Hào quang, Thiên thạch, Ultimate, 3 hệ, boss | Trong plan A: T-08…T-13 xong khoảng Ngày 17 → **trễ hơn ROADMAP ~3 ngày**, bù bằng T-14/T-15 |
| M3 – Khóa tính năng (cuối Ngày 21 = 30/10) | Không thêm tính năng | T-17 trùng ngày; mọi tính năng A phải xong trước đó (T-01…T-16) |
| M4 – Nộp (06/11) | Ngoài plan 22 ngày của A | Sau T-18 là phần nhóm (báo cáo/build) |

---

## 5. Việc thường trực (không gắn một ngày)

- **Review PR của B** (A review B theo `TEAM_ASSIGNMENT.md` mục 1) — làm khi B mở PR.
- **Nhận bàn giao từ B/C** và gỡ stub trong vùng A khi hệ thống thật merge.
- **Giữ file cân bằng** (`Docs/root/Can bang - Fate Bastion.xlsx` → `Data/Balance/*.csv`): chỉ A sửa số; người khác đề xuất qua issue.
