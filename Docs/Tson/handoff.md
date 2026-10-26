# Bàn giao tổng thể – Thành viên A (Gameplay)

> Cập nhật: **Ngày 7 – 16/10/2026**. Bàn giao **tổng thể**; bàn giao phát sinh trong ngày ở `daily/dayNN/handoff.md`.
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
| #3 | Đặt tướng → tướng hoạt động | `Init(HeroData)`, `ResetState()`, `OnDespawn()` | B (S4 đặt/nâng tướng) | ☐ | 10 (T-06) |
| #4 | Nâng cấp → chỉ số | `HeroStats.SetLevelMultipliers` | B (S4 nâng cấp) | ☐ | 10 (T-06) |
| #5 | Nộ / Ultimate | `OnRageChanged`, `TryActivateUltimate()` | B (nút R, HUD Nộ) | ☐ | 14 (T-10) |
| #6 | Hủy ngắm Thiên thạch | `CancelStack.Push/Remove` | C (InputRouter, Esc) | ◐ (`CancelStack` có; A-12 nối nghe `DeckSlot`) | 16 (T-12) |
| #7 | Chọn tướng hủy ngắm | nghe `DeckSlot` | C | ☐ | 16 (T-12) |
| #8 | Vàng | `Wallet` | (A-04 **không** đụng; B sở hữu) | ☑ (B) | — |
| #9 | UI thế giới | `OnEnemySpawned/Despawned`, `ActiveEnemies`, `CurrentHP/MaxHP/IsBoss/Progress/HealthBarAnchor`, `OnHealthChanged` | C (thanh máu, cảnh báo) | ☑ (A-04) | xong |
| #10 | Hào quang | `OnHeroEnteredAura` | B (`TutorialController` bước 3) | ☐ | 15 (T-11) |
| ngoài 3.1 | Thiên thạch (HUD/FX) | `OnMeteorCooldownChanged`, `OnMeteorExploded(Vector3, radius)` — theo `AbilityEvents` (mục 3) | B (HUD), C (FX) | ☐ | 17 (T-13) |
| ngoài 3.1 | Sát thương / quái chết | `OnDamageDealt(DamageDealtInfo)`, `OnEnemyKilled(EnemyKilledInfo)` — `CombatEvents` (mục 3), cũng là nguồn Vàng của 3.1 #8 | B (kinh tế, số sát thương), C (nháy trắng) | ☑ (S1) | xong |
| ngoài 3.1 | Lưu trữ (S9) | `SaveService.Load/Save/Delete`, `SaveData`, `SaveSanitizer` (JSON Newtonsoft) | B (S8 gọi) | ☑ (S9, compile OK · 286/286 EM) | 8 (T-04) |
| ngoài 3.1 | Quái Bay / boss gọi con | `EnemyController.Spawn(...,pathIndex,...)`, `BossSummonSettings` (SO) | C (prefab Bay); asset đã gắn sẵn | ☑ code (compile OK · 297/298 EM, 50/50 PM) | 8 (T-14) |

> **Ghi chú A-07 (T-05/T-06):** `HeroController` điều khiển `HeroVisual.Animator` bằng đúng 4 parameter — Trigger `Attack`, `Ultimate`, `Spawn` và Float `AttackSpeed` (đề xuất 3.1 #11 của C, Controller `Art/Animations/Heroes/AC_Hero_Base.controller`; A duyệt/nhận xét trên nhánh `docs/animator-contract`). Sát thương vẫn theo timer, không chờ animation.

---

## 3. Rủi ro bàn giao

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| S5 (B) chưa xong → A không playtest được màn | Cao | Test trong Sandbox A; giữ stub đúng tên 3.1 |
| S6/S7 chưa bắt đầu, M2 (23/10) cần trận hoàn chỉnh | Cao | Dồn T-07…T-13; chấp nhận trễ ~3 ngày, bù ở T-16 |
| `CancelStack`/`InputRouter` (C) đổi signature | Trung bình | Chốt sớm ở T-12; theo dõi PR của C |
| HUD B gọi sai tên event | Trung bình | Dùng đúng tên 3.1; phát bằng `Raise…()` của hub |
| File cân bằng Excel bị sửa ngoài A | Trung bình | Chỉ A sửa số; người khác đề xuất qua issue |
| ~~Chưa compile S9/A-05/A-06~~ | — | **Đã hết:** compile 0 lỗi trong Editor; S9 286/286 EM, S2 297/298 EM + 50/50 PM |
| ~~`FateBastion.Meta` có thể thiếu reference Newtonsoft~~ | — | **Đã hết:** S9 compile OK, không sửa asmdef |
| Việc đổi tên asset đang dở (chưa commit) làm 1 test S2 đỏ | Trung bình | Hoàn tất đổi tên + cập nhật đường dẫn test; không trộn vào PR ngày 7 |

---

## 4. Lịch sử cập nhật

| Ngày | Nội dung |
|---|---|
| 7 – 16/10 | Tạo file; 7 điểm A chờ người khác; 13 điểm nối A cung cấp (☑ 6, ◐ 1, ☐ 6); 5 rủi ro |
| 7 – 16/10 | Nhánh `feature_a05_a06_s9`: điểm nối #13 (S9 Lưu trữ) ☑, thêm #14 (quái Bay/boss gọi con); cập nhật 2 dòng chờ (S9 API đã có, cần tạo asset Unity); thêm 2 rủi ro (chưa compile, Newtonsoft) |
| 16/10 | PR #14 review (C): đánh lại bảng mục 2 theo đúng số `TEAM_ASSIGNMENT` 3.1 — cột `3.1 #` (#1–#10), gộp dòng cũ #4 "Tướng ra đòn/chết" trùng lặp vào `CombatEvents`, 4 dòng còn lại ghi **ngoài 3.1** kèm tham chiếu; thêm ghi chú 4 parameter Animator cho A-07 |
| 16/10 | Sau khi PR #14 merge: tách day07 thành 4 nhánh theo quy ước `feature/<Sxx>_<mã>` (bỏ nhánh cha/con) — `feature/S09_A14` (A-14/A-15), `feature/S02_A05` (A-05), `feature/S02_A06` (A-06), `docs/agent-tools`; nhánh cũ `feature_a05_a06_s9` merge `main` làm backup |
