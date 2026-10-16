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
| A-14/A-15 Save phục vụ S8 lưu | A-14, A-15 | **B** (S8 gọi `SaveService`) | B cần API S9 để lưu sau gacha/ghép/deck | A cung cấp API trước (T-04) | 8 (T-04) |
| A-05/A-06 prefab quái (Bay, boss) | A-05, A-06 | **C/A** (prefab `Enemy_Base` + Variant) | Prefab gắn tay; `EnemyData.prefab` do A gắn | Dùng prefab hiện có, gắn tay sau | 18–19 |
| A-17 cân bằng | A-17 | **B, C** (game chơi được + FX) | Cần trận chạy thật | — | 21–22 |

---

## 2. A GIAO cho người khác (A cung cấp API / hệ thống)

Theo hợp đồng `TEAM_ASSIGNMENT.md` mục 3.1. Trạng thái = thực tế code hiện có.

| # | Điểm nối | Nội dung | Cho | Trạng thái | Ngày |
|---|---|---|---|---|---|
| 1 | Quái tới Thành | `OnEnemyReachedCastle` | B (Thành, S5) | ☑ (A-04) | xong |
| 2 | Wave | `Init`, `StartWave`, `StopAndClear`, `OnWaveFinished` | B (S5) | ☑ (A-04) | xong |
| 3 | Tướng | `Init(HeroData)`, `ResetState()`, `OnDespawn()`, `HeroStats.SetLevelMultipliers` | B (S4 đặt/nâng tướng) | ☐ | 10 (T-06) |
| 4 | Tướng ra đòn / chết | API #4 (sự kiện sát thương, chết) | B (HUD, kinh tế) | ☐ | 10 (T-06) |
| 5 | Nộ / Ultimate | `OnRageChanged`, `TryActivateUltimate()` | B (nút R, HUD Nộ) | ☐ | 14 (T-10) |
| 6 | Hủy ngắm Thiên thạch | `CancelStack.Push/Remove` | C (InputRouter, Esc) | ◐ (`CancelStack` có; A-12 nối nghe `DeckSlot`) | 16 (T-12) |
| 7 | Chọn tướng hủy ngắm | nghe `DeckSlot` | C | ☐ | 16 (T-12) |
| 8 | Vàng | `Wallet` | (A-04 **không** đụng; B sở hữu) | ☑ (B) | — |
| 9 | UI thế giới | `OnEnemySpawned/Despawned`, `ActiveEnemies`, `CurrentHP/MaxHP/IsBoss/Progress/HealthBarAnchor`, `OnHealthChanged` | C (thanh máu, cảnh báo) | ☑ (A-04) | xong |
| 10 | Hào quang | `OnHeroEnteredAura` | B (`TutorialController` bước 3) | ☐ | 15 (T-11) |
| 11 | Thiên thạch | `OnMeteorCooldownChanged`, `OnMeteorExploded(Vector3, radius)` | B (HUD), C (FX) | ☐ | 17 (T-13) |
| 12 | Sát thương / quái chết | `OnDamageDealt(DamageDealtInfo)`, `OnEnemyKilled(EnemyKilledInfo)` | B (kinh tế, số sát thương), C (nháy trắng) | ☑ (S1) | xong |
| 13 | Lưu trữ | `SaveService.Save`, `SaveData` (JSON Newtonsoft) | B (S8 gọi) | ☐ | 8 (T-04) |

---

## 3. Rủi ro bàn giao

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| S5 (B) chưa xong → A không playtest được màn | Cao | Test trong Sandbox A; giữ stub đúng tên 3.1 |
| S6/S7/S9 chưa bắt đầu, M2 (23/10) cần trận hoàn chỉnh | Cao | Dồn T-07…T-13; chấp nhận trễ ~3 ngày, bù ở T-16 |
| `CancelStack`/`InputRouter` (C) đổi signature | Trung bình | Chốt sớm ở T-12; theo dõi PR của C |
| HUD B gọi sai tên event | Trung bình | Dùng đúng tên 3.1; phát bằng `Raise…()` của hub |
| File cân bằng Excel bị sửa ngoài A | Trung bình | Chỉ A sửa số; người khác đề xuất qua issue |

---

## 4. Lịch sử cập nhật

| Ngày | Nội dung |
|---|---|
| 7 – 16/10 | Tạo file; 7 điểm A chờ người khác; 13 điểm nối A cung cấp (☑ 6, ◐ 1, ☐ 6); 5 rủi ro |
