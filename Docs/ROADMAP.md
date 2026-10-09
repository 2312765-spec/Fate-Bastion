# Lộ trình 4 tuần – Fate Bastion

> Phiên bản 1.1 (09/10/2026: mã việc trong mọi ô lịch, UI thế giới sang C, Hướng dẫn sang B) · khớp GDD v1.3 mục 14. Ngày bắt đầu (Ngày 1): `10/10/2026` → khóa tính năng cuối Ngày 21: `30/10/2026` → nộp cuối Ngày 28: `06/11/2026`.
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
| 3 | S1 Chiến đấu: `DamageCalculator` + test (A-01) | S4 Đặt tướng: `PlacementRules`, kiểm tra hợp lệ (class thuần) + test (B-01) | S0b HeroVisual + Validate Hero Prefabs (C-01, C-02) · S3: `PlayerController` (C-03) |
| 4 | S1: status effect Bỏng, Chậm, Nảy; chết một lần, đạn tới vị trí cuối (A-02, A-03) · **→ giao S1** | S4: `PlacementSystem`, `GhostPreview`, `Wallet` (dùng stub InputRouter) (B-02, B-03) | S3: `CameraRig`, `CursorController`, `SummonerInputRouter` + `CancelStack` (C-04, C-05) · **→ giao S3** |
| 5 | S2 Quái và Wave: `EnemyController` trên Spline, `WaveManager`, pool; API 3.1 #1, #2, #9 (A-04) | S4: nối InputRouter thật, `HeroPool`, `CancelStack`, bán tướng (B-02, B-05) · **→ giao S4** | Map graybox thành prefab `Map_Level1`: mặt đất, đường chính + đường bay (Spline) gắn vào `LevelPaths`, Cổng, Thành (C-06) · **→ giao map** |
| 6 | S2: boss gọi quái con, quái Bay, máu theo `hpMultiplier`, test PlayMode (A-05, A-06) · **→ giao S2** · nếu kịp: bắt đầu `HeroController` (A-07) | S5 Luồng trận: state machine (class thuần) + test, `TimeController` (B-06) | Chọn và tải asset pack (Quaternius/Kenney), thử import 1 tướng + 1 quái (C-07) |
| 7 | `HeroController`, `TargetingModule`, `AttackModule` cơ bản (A-07, bắt đầu ngày 6 nếu kịp); ghép S1+S2 với S4/S5 | S5: `Castle`, thắng/thua, tính sao, Ngọc thưởng (logic) (B-06, B-07) · **→ giao S5** | Đo mốc hiệu năng ban đầu trên GPU tích hợp (C-15), sửa camera theo phản hồi · **M1** |

### Tuần 2 – Nội dung trận

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 8 | S6 Kỹ năng: `SkillData`, `SkillRunner`, MeleeHit/Projectile/AreaAtPoint (A-08) | Nâng cấp 3 cấp (`HeroLevel`) + test giá/hoàn tiền (B-04) | Import model 5 tướng khởi đầu, Humanoid rig, gắn `HeroVisual` (C-08) |
| 9 | S6: Cone, Line, Zone, Buff + quy tắc cộng buff (A-08, A-09) | S10: `UIManager`, `ConfirmDialog`, HUD thanh trên (Wave, Thành, Vàng) (B-13, B-14) | Animation Mixamo (Idle/Attack/Spawn) + Override Controller cho 5 tướng (C-08); font TMP tiếng Việt (C-10); bắt đầu bố cục prefab HUD trận (C-11) |
| 10 | S6: Nộ + Ultimate Long Vương (A-10) · **→ giao S6** | S10: 5 ô deck, bảng thông tin tướng (B-14) | Model 5 loại quái, animation đi/chết (C-08); xong prefab HUD trận (C-11) · **→ giao HUD cho B** |
| 11 | S7: `CommandAura` + test buff vào/ra, event 3.1 #10 (A-11) | Hướng dẫn màn 1: `TutorialController` + bước 1–2 (đặt tướng, nâng cấp) (B-17) | VFX cơ bản: đạn, nổ, bỏng, băng, sét; Hào quang độ hiếm (C-09) |
| 12 | S7: `MeteorAbility`, `MeteorAimIndicator` (A-12, A-13) · **→ giao S7** | S10: ô Thiên thạch (`CooldownSlot`), màn Tạm dừng (B-14) | Model + animation 4 tướng còn lại (Rare/Epic/Legendary) (C-08) |
| 13 | Dữ liệu wave 3 màn + Vô tận (kiểm tra sau import), boss (A-06) | S10: màn Kết quả trận (B-15) | VFX Thiên thạch, Ultimate (C-09); dựng khung báo cáo phần Art |
| 14 | Ghép, sửa lỗi, cân bằng nhanh | Ghép HUD với mọi event, sửa lỗi | UI thế giới: `WorldHealthBar`, `OffscreenWarning` (C-14) · **M2** |

### Tuần 3 – Meta và hình ảnh (khóa tính năng cuối Ngày 21)

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 15 | S9 Lưu trữ: `SaveData`, ghi an toàn tmp/Replace/bak + test (A-14) | S8 Gacha: `GachaService` + test 100.000 lượt, pity, x10 (B-08, B-09) | Dựng map thật: địa hình, cây, đá (C-17) |
| 16 | S9: đọc lỗi → bak → tạo mới, lần lưu đầu (A-15) · **→ giao S9** | S8: `CollectionService` (ghép), `DeckService`, `StarterSettings` + test (B-10, B-11, B-12) | Ánh sáng, post-processing nhẹ, LOD cho quái (C-17); Render Hero Icons (C-02) · **→ giao icon cho B** |
| 17 | Dự phòng: sửa lỗi S1–S7 tồn đọng, kiểm Vô tận và boss; hỗ trợ B nối S8 ↔ S9 (review) | S8 nối S9: lưu sau quay/ghép/đổi deck; Ngọc thưởng sau trận ghi vào save (B-07); Hướng dẫn màn 1 bước 3–4 + lưu `tutorialDone` (B-17) | Bố cục UI sảnh: Sảnh, Chọn màn, Gacha, Túi & Deck (C-12) |
| 18 | Cân bằng lần 1 (sửa Excel → import) (A-17) | Màn hình sảnh: gắn script vào prefab của C (B-15) | S11: `AudioManager`, `MusicPlayer`, AudioMixer (C-13) |
| 19 | Cân bằng lần 1: chơi thử 3 màn, chỉnh tiếp (A-17) | Hiệu ứng ra tướng gacha, ConfirmDialog cho Ghép (B-15) | S11: âm thanh tướng/quái/UI (C-13); số sát thương (C-14); `FeedbackService` rung, nháy trắng (C-18) |
| 20 | Sửa lỗi gameplay | Cài đặt (âm lượng, rung, hỏi khi bỏ qua) nối S9 (B-15) | **Tối ưu hiệu năng** (C-15): GPU Instancing, LOD, draw call, Canvas, GC Alloc – ghi số liệu trước/sau |
| 21 | Ghép toàn bộ, chơi thử 3 màn | Ghép toàn bộ | Ghép toàn bộ · **M3 – khóa tính năng** |

### Tuần 4 – Cân bằng, build, báo cáo

| Ngày | A – Gameplay | B – Hệ thống và UI | C – Art và kỹ thuật |
|---|---|---|---|
| 22 | Playtest có người ngoài, ghi log | Bảng Debug (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`) (B-16) | Build Development đầu tiên (C-16) |
| 23 | Cân bằng lần 2 (A-17) | Sửa lỗi UI ở 1920×1080 và 1366×768 | Sửa lỗi hình ảnh, kiểm tra font tiếng Việt; đo lại hiệu năng sau tối ưu (C-15) |
| 24 | Sửa lỗi | Sửa lỗi | Build Release IL2CPP, chạy trên máy sạch (C-16) |
| 25 | Báo cáo: thiết kế gameplay, cân bằng | Báo cáo: kiến trúc, test | Báo cáo: art, hiệu năng (ảnh Profiler) |
| 26 | Slide bảo vệ (phần mình) | Slide bảo vệ (phần mình) | Quay video demo |
| 27 | Chạy thử buổi bảo vệ | Chạy thử buổi bảo vệ | Chạy thử buổi bảo vệ |
| 28 | Nộp | Nộp | Nộp · **M4** |

---

## 3. Phụ thuộc và bàn giao

Việc ở cột trái **chặn** việc ở cột phải. Nếu trễ, người bị chặn dùng stub (đúng tên API ở `TEAM_ASSIGNMENT.md` mục 3.1) và báo trong buổi họp sáng.

**"Giao" nghĩa là đủ 5 điều ở `TEAM_ASSIGNMENT.md` mục 4.1** (merge + test xanh, mục "Cách dùng", hướng dẫn lắp, scene Sandbox, người nhận xác nhận) – không chỉ là "đã push code".

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
| API UI thế giới (3.1 #9) trong `EnemyManager`/`EnemyController` | Ngày 6 (cùng S2) | A | C (C-14) | C dùng stub `EnemyController` giả trong Sandbox |
| Hero Icons (C-02) | Ngày 16 | C | B (Gacha, Túi, ô deck) | B dùng ô màu theo độ hiếm |
| S5 Luồng trận | Ngày 7 | B | Mọi người | — |
| S6 Kỹ năng | Ngày 10 | A | A (S7) | — |
| S7 Thiên thạch | Ngày 12 | A | B (ô Thiên thạch HUD) | B dùng event giả |
| Prefab UI sảnh | Ngày 17 | C | B (gắn script) | B làm bằng prefab xám, C thay sau |
| S9 Lưu trữ | Ngày 16 | A | B (S8) | B lưu tạm vào bộ nhớ, chưa ghi file |

**Đường găng (critical path):** S0 → S1 → S2 → S5 → M1 → S6 → S7 → M2 → S9 → S8 → M3. Trễ ở đây là trễ cả dự án.

---

## 4. Phao cứu sinh

**Kiểm tra sớm Ngày 10:** nếu S6 chưa xong phần bị động của 9 tướng, cắt Ultimate Long Vương ngay (không đợi Ngày 18) để A kịp S7 đúng Ngày 12.

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
| M0 | ☑ | Merge PR #1. Còn việc dọn nhỏ không chặn ai: S0-09, S0-10, S0-11, S0-14 (xem TRACEABILITY mục 3) |
| M1 | ☐ | |
| M2 | ☐ | |
| M3 | ☐ | |
| M4 | ☐ | |
