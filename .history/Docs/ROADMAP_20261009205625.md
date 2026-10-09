# Lộ trình 4 tuần – Fate Bastion

> Phiên bản 1.0 · khớp GDD v1.3 mục 14. Ngày bắt đầu (Ngày 1): `_10_/_10_/2026` → khóa tính năng cuối Ngày 21: `__/__` → nộp cuối Ngày 28: `__/__`.
> Ai sở hữu gì: `Docs/TEAM_ASSIGNMENT.md`. Từng việc nhỏ và tiêu chí kiểm tra: `Docs/TRACEABILITY.md` (cột **Ngày** ở đó khớp bảng mục 2 dưới đây; mã việc A-xx/B-xx/C-xx ghi trong ngoặc).

---

## 1. Các mốc

| Mốc | Ngày | Điều kiện đạt (kiểm tra trên nhánh `main`) |
|---|---|---|
| **M0 – Nền chung** | Cuối Ngày 2 | S0 merge: enum, ScriptableObject, 4 event hub ở Core, InputRouter (khai báo), Import CSV tạo đúng 9 HeroData / 9 SkillData / 5 EnemyData / 45 WaveData / 3 LevelData; test xanh |
| **M1 – Graybox chơi được** | Cuối Ngày 7 | Chơi trọn màn 1 bằng khối hộp: quái đi theo Spline, đặt/bán tướng, tướng tự bắn, Vàng, máu Thành, thắng/thua; Triệu Hồi Sư đi + camera. Đo FPS trên GPU tích hợp |
| **M2 – Trận hoàn chỉnh** | Cuối Ngày 14 | Nâng cấp 3 cấp, 3 hệ, 4 loại quái + boss, Hào quang, Thiên thạch, Ultimate Long Vương, HUD trận; 3 màn + Vô tận có dữ liệu; model thật cho ít nhất 5 tướng khởi đầu |
| **M3 – Khóa tính năng** | Cuối Ngày 21 | Gacha, Túi, Ghép, Deck, lưu/tải, sảnh, hướng dẫn màn 1, map thật, âm thanh. Từ đây **không thêm tính năng** |
| **M4 – Nộp bài** | Cuối Ngày 28 | Build Release IL2CPP + build Development cho buổi bảo vệ, video demo, báo cáo, số liệu Profiler |

---

## 2. Lịch theo ngày

Ký hiệu: **→ giao** = bàn giao cho người khác dùng (cần xong đúng hạn để không chặn ai).

### Tuần 1 – Graybox

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 1 | S0 cùng nhóm: enum, Data SO, Core event | S0 cùng nhóm: dựng repo, Import Balance CSV | S0 cùng nhóm: InputRouter (khai báo), Tags/Layers, scene Boot/Game/Sandbox, cài package |
| 2 | S0: test, review | S0: test bộ đọc CSV, import thử | S0: review · **→ giao M0** |
| 3 | S1 Chiến đấu: `DamageCalculator` + test | S4 Đặt tướng: `PlacementRules`, kiểm tra hợp lệ (class thuần) + test | S0b HeroVisual + Validate Hero Prefabs · S3: `PlayerController` |
| 4 | S1: status effect (Bỏng, Chậm, Nảy), `IDamageable` · **→ giao S1** | S4: `PlacementSystem`, `GhostPreview`, `Wallet` (dùng stub InputRouter) | S3: `CameraRig`, `CursorController`, cài đặt `InputRouter` · **→ giao S3** |
| 5 | S2 Quái và Wave: `EnemyController` trên Spline, `WaveManager`, pool | S4: nối InputRouter thật, bán tướng · **→ giao S4** | Map graybox thành prefab `Map_Level1`: mặt đất, đường chính + đường bay (Spline) gắn vào `LevelPaths`, Cổng, Thành (C-06) · **→ giao map** |
| 6 | S2: boss gọi quái con, quái Bay, test PlayMode · **→ giao S2** | S5 Luồng trận: state machine (class thuần) + test, `TimeController` | Chọn và tải asset pack (Quaternius/Kenney), thử import 1 tướng + 1 quái |
| 7 | `HeroController`, `TargetingModule`, `AttackModule` cơ bản (A-07, bắt đầu ngày 6 nếu kịp); ghép S1+S2 với S4/S5 | S5: `Castle`, thắng/thua, tính sao · **→ giao S5** | Đo FPS GPU tích hợp, sửa camera theo phản hồi · **M1** |

### Tuần 2 – Nội dung trận

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 8 | S6 Kỹ năng: `SkillData`, `SkillRunner`, MeleeHit/Projectile/AreaAtPoint | Nâng cấp 3 cấp (`HeroLevel`) + test giá/hoàn tiền | Import model 5 tướng khởi đầu, Humanoid rig, gắn `HeroVisual` |
| 9 | S6: Cone, Line, Zone, Buff + quy tắc cộng buff | S10: `UIManager`, `ConfirmDialog`, HUD thanh trên (Wave, Thành, Vàng) | Animation Mixamo (Idle/Attack/Spawn) + Override Controller cho 5 tướng; font TMP tiếng Việt (C-10); bố cục prefab HUD trận (C-11) · **→ giao HUD cho B** |
| 10 | S6: Nộ + Ultimate Long Vương · **→ giao S6** | S10: 5 ô deck, bảng thông tin tướng | Model 5 loại quái, animation đi/chết |
| 11 | S7: `CommandAura` + test buff vào/ra | S10: `OffscreenWarning`, `WorldHealthBar` | VFX cơ bản: đạn, nổ, bỏng, băng, sét; Hào quang độ hiếm |
| 12 | S7: `MeteorAbility`, `MeteorAimIndicator` · **→ giao S7** | S10: ô Thiên thạch (`CooldownSlot`), tạm dừng | Model + animation 4 tướng còn lại (Rare/Epic/Legendary) |
| 13 | Dữ liệu wave 3 màn + Vô tận (kiểm tra sau import), boss | S10: màn Kết quả trận | VFX Thiên thạch, Ultimate; dựng khung báo cáo phần Art |
| 14 | Ghép, sửa lỗi, cân bằng nhanh | Ghép HUD với mọi event, sửa lỗi | Render Hero Icons, chụp ảnh tiến độ · **M2** |

### Tuần 3 – Meta và hình ảnh (khóa tính năng cuối Ngày 21)

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 15 | S9 Lưu trữ: `SaveData`, ghi an toàn tmp/Replace/bak + test | S8 Gacha: `GachaService` + test 100.000 lượt, pity, x10 | Dựng map thật: địa hình, cây, đá |
| 16 | S9: đọc lỗi → bak → tạo mới, lần lưu đầu · **→ giao S9** | S8: `CollectionService` (ghép), `DeckService`, `StarterSettings` + test | Ánh sáng, post-processing nhẹ, LOD cho quái |
| 17 | Hướng dẫn màn 1 (4 bước) | S8 nối S9: lưu sau quay/ghép/đổi deck | Bố cục UI sảnh: Sảnh, Chọn màn, Gacha, Túi & Deck |
| 18 | Hướng dẫn màn 1: nối event, lưu `tutorialDone` | Màn hình sảnh: gắn script vào prefab của C | S11: `AudioManager`, `MusicPlayer`, AudioMixer |
| 19 | Cân bằng lần 1 (sửa Excel → import) | Hiệu ứng ra tướng gacha, ConfirmDialog cho Ghép | S11: âm thanh tướng/quái/UI, `FeedbackService` (rung, số sát thương) |
| 20 | Sửa lỗi gameplay | Cài đặt (âm lượng, rung, hỏi khi bỏ qua) nối S9 | Tối ưu: Profiler, GC Alloc, FPS GPU tích hợp |
| 21 | Ghép toàn bộ, chơi thử 3 màn | Ghép toàn bộ | Ghép toàn bộ · **M3 – khóa tính năng** |

### Tuần 4 – Cân bằng, build, báo cáo

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 22 | Playtest có người ngoài, ghi log | Bảng Debug (`#if DEVELOPMENT_BUILD`) | Build Development đầu tiên |
| 23 | Cân bằng lần 2 | Sửa lỗi UI ở 1920×1080 và 1366×768 | Sửa lỗi hình ảnh, kiểm tra font tiếng Việt |
| 24 | Sửa lỗi | Sửa lỗi | Build Release IL2CPP, chạy trên máy sạch |
| 25 | Báo cáo: thiết kế gameplay, cân bằng | Báo cáo: kiến trúc, test | Báo cáo: art, hiệu năng (ảnh Profiler) |
| 26 | Slide bảo vệ (phần mình) | Slide bảo vệ (phần mình) | Quay video demo |
| 27 | Chạy thử buổi bảo vệ | Chạy thử buổi bảo vệ | Chạy thử buổi bảo vệ |
| 28 | Nộp | Nộp | Nộp · **M4** |

---

## 3. Phụ thuộc và bàn giao

Việc ở cột trái **chặn** việc ở cột phải. Nếu trễ, người bị chặn dùng stub và báo trong buổi họp sáng.

| Bàn giao | Hạn | Từ | Người dùng | Nếu trễ |
|---|---|---|---|---|
| S0 (Core, Data, Import) | Ngày 2 | Cả nhóm | Mọi người | Không được trễ – cả nhóm cùng làm |
| S3 InputRouter (cài đặt) | Ngày 4 | C | B (S4), A (S7) | B dùng stub bàn phím trong Sandbox |
| S1 Chiến đấu | Ngày 4 | A | A (S2, S6), C (FX) | — |
| S4 Đặt tướng | Ngày 5 | B | B (S5), A (Hào quang) | A đặt tướng bằng tay trong Sandbox |
| S2 Quái và Wave | Ngày 6 | A | B (S5) | B dùng `WaveManager` stub phát event giả |
| S0b HeroVisual | Ngày 3 | C | A (S6 bắn đạn) | A dùng vị trí tướng + offset tạm |
| Prefab map graybox + `LevelPaths` | Ngày 5 | C (prefab) + A (script) | A (S2), B (S4 đặt tướng) | A/B dựng 1 Spline thẳng trong Sandbox |
| Prefab HUD trận + font (C-10, C-11) | Ngày 10 | C | B (S10) | B dựng HUD bằng prefab xám, C thay sau |
| S5 Luồng trận | Ngày 7 | B | Mọi người | — |
| S6 Kỹ năng | Ngày 10 | A | A (S7) | — |
| S7 Thiên thạch | Ngày 12 | A | B (ô Thiên thạch HUD) | B dùng event giả |
| Prefab UI sảnh | Ngày 17 | C | B (gắn script) | B làm bằng prefab xám, C thay sau |
| S9 Lưu trữ | Ngày 16 | A | B (S8) | B lưu tạm vào bộ nhớ, chưa ghi file |

**Đường găng (critical path):** S0 → S1 → S2 → S5 → M1 → S6 → S7 → M2 → S9 → S8 → M3. Trễ ở đây là trễ cả dự án.

---

## 4. Phao cứu sinh

Nếu đến **Ngày 18** mà M3 có nguy cơ trễ, cắt theo thứ tự (quyết định trong buổi họp sáng, ghi vào `CLAUDE.md` mục 1):

1. Ultimate Long Vương
2. Ghép trong Túi
3. Chế độ Vô tận
4. Tốc độ ×2

**Không được cắt:** gacha + deck · đặt và nâng tướng · Triệu Hồi Sư với Hào quang và Thiên thạch.

---

## 5. Trạng thái mốc (cập nhật cuối mỗi tuần)

| Mốc | Trạng thái | Ghi chú |
|---|---|---|
| M0 | ☐ | |
| M1 | ☐ | |
| M2 | ☐ | |
| M3 | ☐ | |
| M4 | ☐ | |
