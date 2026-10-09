# Truy vết yêu cầu và checklist công việc – Fate Bastion

> Phiên bản 1.0 · khớp GDD v1.3, `Docs/Specs/S0–S11`, `Docs/TEAM_ASSIGNMENT.md`, `Docs/ROADMAP.md`.
> **Cách dùng:** mỗi người chỉ tick ☐ → ☑ ở **phần của mình** (mục 4 = A, 5 = B, 6 = C; mục 3 tick chung trong PR S0) trong PR của việc đó. Không sửa phần của người khác → không xung đột khi merge. Bảng mục 2 do trưởng nhóm kỹ thuật cập nhật cuối tuần.
> Cột **Ngày** khớp lịch `ROADMAP.md` mục 2 (dời ngày ở đây thì dời cả ở ROADMAP và ngược lại). Cột **Vùng file** khớp `TEAM_ASSIGNMENT.md` mục 2.
> Ký hiệu cột **Kiểm tra**: `EM` = unit test EditMode, `PM` = test PlayMode, `Tay` = kiểm tra tay trong Unity (ghi người kiểm và ngày vào cột Ghi chú).

---

## 1. Danh sách yêu cầu (từ GDD v1.3)

| Mã | Yêu cầu | Nguồn | Spec | Chủ |
|---|---|---|---|---|
| R01 | Gacha x1 (100 Ngọc) / x10 (900, ≥ 1 Epic+), tỉ lệ 65/25/8.5/1.5 công khai, pity 25 | GDD 5 | S8 | B |
| R02 | Ghép 3 bản thừa cùng độ hiếm → 1 tướng ngẫu nhiên bậc kế, luôn giữ ≥ 1 bản | GDD 5 | S8 | B |
| R03 | Deck đúng 5 tướng khác nhau; người chơi mới có 5 tướng khởi đầu + 1.000 Ngọc | GDD 5 | S8 | B |
| R04 | Đặt tướng tự do, kiểm tra hợp lệ, quân số 15, giới hạn số con theo độ hiếm | GDD 3, 8 | S4 | B |
| R05 | Nâng cấp 3 cấp (75% / 150% giá đặt), bán hoàn 50% | GDD 8 | S4 | B |
| R06 | Một chỗ tính sát thương; Vật lý/Phép/True; kháng kẹp [0; 0.8]; không chí mạng | GDD 2 | S1 | A |
| R07 | 3 hệ: Bỏng, Làm chậm (boss 50%), Sét nảy | GDD 6 | S1 | A |
| R08 | Đòn đánh/bị động của 9 tướng; quy tắc cộng buff, trần +60% | GDD 6 | S6 | A |
| R09 | Ultimate Long Vương: Nộ +5 mỗi đòn, kích hoạt tay | GDD 6 | S6 | A |
| R10 | 4 loại quái + boss; quái Bay đường riêng; boss gọi 3 quái Nhanh mỗi 12 s | GDD 7 | S2 | A |
| R11 | 15 wave mỗi màn; máu = HP₀ × 1.12^(n−1) × k_màn; boss wave 5/10/15 | GDD 7 | S2 | A |
| R12 | 3 màn chiến dịch + Vô tận (dùng màn 3) | GDD 9 | S2, S5 | A, B |
| R13 | Thành 20 máu; thắng/thua; sao ≥15/≥8/còn sống | GDD 7 | S5 | B |
| R14 | Vàng trong trận: khởi đầu 300, hạ quái, thưởng cuối wave, bỏ qua +50 | GDD 8 | S4, S5 | B |
| R15 | Ngọc ngoài trận: qua màn +300, sao mới +50, Vô tận +10/5 wave, tướng mới +50 | GDD 8 | S5, S8 | B |
| R16 | Triệu Hồi Sư đi bằng WASD, Shift chạy; camera góc nhìn thứ 3 xoay/zoom | GDD 4 | S3 | C |
| R17 | Hào quang Chỉ huy: 6 m, +25% tốc đánh | GDD 4 | S7 | A |
| R18 | Thiên thạch: Q, tầm 20 m, nổ 4 m, rơi 0.8 s, hồi 45 s, True damage theo wave | GDD 4 | S7 | A |
| R19 | Phím tắt đầy đủ; Esc theo thứ tự ưu tiên; chuột phải giữ = xoay, bấm nhanh = hủy | GDD 4 | S3 | C |
| R20 | Tốc độ ×1/×2; tạm dừng; Alt-Tab tự tạm dừng | GDD 9 | S5 | B |
| R21 | HUD trận + bảng thông tin tướng + cảnh báo quái sắp tới Thành | GDD 9 | S10 | B, C |
| R22 | Màn hình ngoài trận: Sảnh, Chọn màn, Gacha, Túi & Deck, Kết quả, Cài đặt | GDD 9 | S10 | B, C |
| R23 | Hướng dẫn màn 1 (4 bước, chỉ lần đầu) | GDD 9 | S10 | A |
| R24 | Bảng Debug F1, chỉ Editor/Development | GDD 9 | S9, S10 | B |
| R25 | Lưu tiến trình an toàn (tmp → Replace/Move → bak) | GDD 11 | S9 | A |
| R26 | Âm thanh, rung camera, số sát thương | GDD 9 | S11 | C |
| R27 | Hiệu năng: ≥ 60 FPS (RTX 3050), ≥ 30 FPS (GPU tích hợp), GC ≈ 0 B/frame, 100 quái | GDD 10, 13 | S1–S11 | C (đo), mọi người (giữ) |
| R28 | Dữ liệu cân bằng import từ CSV, không hardcode | GDD 12 | S0 | B (công cụ), A (số liệu) |
| R29 | Đổi ngoại hình tướng không cần sửa code (HeroVisual) | GDD 12 | S0b | C |
| R30 | Build Release IL2CPP + Development cho buổi bảo vệ | GDD 9, 10 | — | C |
| R31 | Chữ tiếng Việt hiển thị đủ dấu | GDD 10 | S10 | C |

---

## 2. Ma trận truy vết (yêu cầu → việc)

| Yêu cầu | Việc thực hiện | Trạng thái |
|---|---|---|
| R01 | B-08, B-09 | ☐ |
| R02 | B-10 | ☐ |
| R03 | B-11, B-12 | ☐ |
| R04 | B-01, B-02, B-03 | ☐ |
| R05 | B-04, B-05 | ☐ |
| R06 | A-01 | ☐ |
| R07 | A-02 | ☐ |
| R08 | A-07, A-08, A-09 | ☐ |
| R09 | A-10 | ☐ |
| R10 | A-04, A-05 | ☐ |
| R11 | A-04, A-06 | ☐ |
| R12 | A-06, B-07 | ☐ |
| R13 | B-06 | ☐ |
| R14 | B-03, B-06 | ☐ |
| R15 | B-07, B-11 | ☐ |
| R16 | C-03, C-04 | ☐ |
| R17 | A-11 | ☐ |
| R18 | A-12, A-13 | ☐ |
| R19 | C-05 | ☐ |
| R20 | B-06 | ☐ |
| R21 | B-13, B-14, C-11 | ☐ |
| R22 | B-15, C-12 | ☐ |
| R23 | A-16 | ☐ |
| R24 | B-16 | ☐ |
| R25 | A-14, A-15 | ☐ |
| R26 | C-13, C-14 | ☐ |
| R27 | C-15, mọi PR (mục 6) | ☐ |
| R28 | S0-04, S0-05, A-06, A-17 | ☑ công cụ · ☐ số liệu |
| R29 | C-01, C-02, C-08 | ☐ |
| R30 | C-16 | ☐ |
| R31 | C-10 | ☐ |

---

Việc nền không gắn với một yêu cầu đơn lẻ (vẫn bắt buộc, kiểm tra ở mục 6 và mục 8):

| Việc | Vì sao không có mã R | Chủ |
|---|---|---|
| C-06 Map graybox, C-17 map thật, ánh sáng, LOD | Là nền cho R10–R16 và R27, không phải một yêu cầu riêng | C |
| C-07 Chọn asset pack + `CREDITS.md` | Nghĩa vụ giấy phép, kiểm ở mục 8 | C |
| C-09 VFX đạn, nổ, hệ, hào quang, Thiên thạch, Ultimate | Phần hình ảnh của R07, R08, R09, R18 | C |
| A-17 Cân bằng lần 1 và 2 | Thuộc R28 (dữ liệu từ CSV), không phải tính năng mới | A |

---

## 3. S0 – Việc chung (Ngày 1–2)

| ID | Ngày | Việc | Vùng file | Kiểm tra | Xong | Ghi chú |
|---|---|---|---|---|---|---|
| S0-01 | 1 | Repo, Git LFS, `.gitignore`, `.gitattributes`, Force Text, project Unity ở gốc repo | gốc repo, `ProjectSettings/` | Tay: clone mới trên máy khác mở được | ☑ | LFS 3.7.0, `m_SerializationMode: 2` |
| S0-02 | 1 | Cấu trúc thư mục + 13 asmdef theo `TEAM_ASSIGNMENT.md` mục 2 | `Assets/_Project/Scripts/*` | Tay: compile không lỗi, không phụ thuộc vòng | ☑ | 0 lỗi, 0 warning CS |
| S0-03 | 1–2 | Enum, `HeroData`, `EnemyData`, `WaveData`, `SpawnGroup`, `LevelData` (+`hpMultiplier`), `SkillData`, `StatusEffectData`, `DamageInfo`, `IDamageable`, 4 event hub (`GameEvents`, `CombatEvents`, `HeroEvents`, `AbilityEvents`), `InputRouter` (khai báo) | `Core/`, `Combat/`, `Heroes/Data/`, `Enemies/`, `Skills/` | Tay: compile | ☑ | |
| S0-04 | 1–2 | Bộ đọc CSV: BOM, ngoặc kép, `,`/`;`, InvariantCulture | `Editor/Import/` | EM: 3 trường hợp file mẫu | ☑ | 19 test `CsvReaderTests` |
| S0-05 | 2 | Tools → Import Balance CSV | `Editor/Import/` | Tay: 9 HeroData, 9 SkillData, 5 EnemyData, 45 WaveData, 3 LevelData; chạy lại không tạo thêm | ☑ | 20 test `BalanceImporterTests` |
| S0-06 | 1 | Tags & Layers: Placeable, Path, Obstacle, Hero, Enemy | `ProjectSettings/` | Tay | ☐ | |
| S0-07 | 1 | Scene Boot, Lobby, Game, Sandbox/A_, B_, C_ (trống) | `Scenes/` | Tay | ☐ | |
| S0-08 | 1 | Package: Splines, Cinemachine 3.x, TextMeshPro, Newtonsoft Json; PrimeTween vào `ThirdParty/` | `Packages/manifest.json` | Tay: compile sau khi thêm | ☐ | Chặn S2, S3, S9, S10 |
| S0-09 | 1 | UnityYAMLMerge làm merge tool cho `.unity`/`.prefab` | `.git/config` hoặc `~/.gitconfig` (mỗi máy) | Tay: tạo xung đột scene thử rồi merge | ☐ | Mỗi người tự chạy trên máy mình |
| S0-10 | 2 | Gắn tay (người làm: A) field `LevelData` không có trong CSV: `hpMultiplier` (1.0 / 1.15 / 1.3), `displayName`, `enemyTypesPreview` | `Data/Levels/` | Tay: đối chiếu cột `calc_totalHP` sheet Wave | ☐ | **Chặn A-06** – importer không ghi cột này |
| S0-11 | 1 | Xóa rác template URP (`Assets/Scenes/`, `TutorialInfo/`, `Readme.asset`, `InputSystem_Actions.inputactions`); giữ `Assets/Settings/` | `Assets/` | Tay: project vẫn mở và chạy được | ☐ | `Assets/Scenes/` đụng tên `_Project/Scenes/` |
| S0-12 | 2 | Bổ sung tham chiếu asmdef theo `S0.md` mục "Assembly và hướng phụ thuộc" (Enemies → Splines; Skills/Heroes/Player → Enemies; Game → Meta; UI → Game, Meta, TMP, PrimeTween; asmdef test đủ assembly) | `*.asmdef` | Tay: compile 0 lỗi **sau** S0-08 | ☐ | File đã sửa, chờ compile |
| S0-13 | 2 | Chốt tài liệu: ghi tên trưởng nhóm kỹ thuật (`TEAM_ASSIGNMENT.md` mục 1), điền ngày thật vào `ROADMAP.md` | `Docs/` | Tay | ☐ | |
| S0-14 | 2 | Mỗi người: `git lfs install`, clone `main` sau khi merge S0, mở Unity compile được | máy từng người | Tay: A ☐ · B ☐ · C ☐ | ☐ | Ghép với S0-09 |

---

## 4. Người A – Gameplay

| ID | Ngày | Việc | Spec | Vùng file | Kiểm tra | Xong | Ghi chú |
|---|---|---|---|---|---|---|---|
| A-01 | 3 | `DamageCalculator` (`DamageInfo` đã có từ S0, vùng khóa) (kháng 0/50/80%, True, hệ số kỹ năng, làm tròn, tối thiểu 1) | S1 | `Combat/` | EM | ☐ | |
| A-02 | 4 | `StatusEffectController`: Bỏng lấy mạnh nhất, Chậm ≤ 60% và boss 50%, Nảy không lặp | S1 | `Combat/` | EM | ☐ | |
| A-03 | 4 | Chết một lần khi 3 đòn cùng frame; đạn bay tới vị trí cuối khi mục tiêu chết; XZ | S1 | `Combat/`, `Heroes/Runtime/` | EM + PM | ☐ | |
| A-04 | 5 | `EnemyController` trên Spline (lấy từ `LevelPaths`), `EnemyManager` (≤ 100 quái), `WaveManager` (nhóm song song, lệch 2 s), pool | S2 | `Enemies/` | PM: tới Thành trừ đúng 1 lần ở ×1 và ×2 | ☐ | |
| A-05 | 6 | Quái Bay (đường riêng, cao 2 m), boss gọi 3 quái Nhanh mỗi 12 s | S2 | `Enemies/` | PM: wave kết thúc đúng khi có quái con | ☐ | |
| A-06 | 6, 13 | Máu theo công thức có `hpMultiplier`; dữ liệu 3 màn + Vô tận kiểm tra sau import | S2 | `Enemies/`, `Data/Balance/` | EM: wave 1/5/15 cả 3 màn khớp sheet Wave | ☐ | |
| A-07 | 6–7 | `HeroController`, `TargetingModule` (Đầu đoàn, DPS đơn ưu tiên boss, Melee không chọn Bay), `AttackModule` (timer, bắn từ `HeroVisual.muzzle`) | S1, S6 | `Heroes/Runtime/` | PM | ☐ | |
| A-08 | 8–9 | `SkillData`, `SkillRunner`, 7 kind: MeleeHit, Projectile, AreaAtPoint, Cone, Line, Zone, Buff | S6 | `Skills/` | PM: mỗi kind một kỹ năng mẫu | ☐ | |
| A-09 | 9 | Cộng buff: cùng loại lấy mạnh nhất, khác nguồn cộng, trần +60% | S6 | `Heroes/Runtime/`, `Skills/` | EM: 2 Thánh Nữ = +30%, Thánh Nữ + Hào quang = +55% | ☐ | |
| A-10 | 10 | Nộ +5 mỗi đòn, Ultimate Long Vương kích hoạt tay | S6 | `Skills/` | EM | ☐ | |
| A-11 | 11 | `CommandAura` (6 m, +25%, 5 lần/s, chỉ tính lại khi vào/ra) | S7 | `Player/Abilities/` | PM: vào/ra đổi tốc đánh; bán tướng gỡ buff | ☐ | |
| A-12 | 12 | `MeteorAbility` (state machine thuần): hủy không tốn hồi chiêu, hồi 45 s game time | S7 | `Player/Abilities/` | EM: 45 s (×1), 22.5 s thực (×2) | ☐ | |
| A-13 | 12 | `MeteorAimIndicator`, tầm 20 m, sát thương theo wave × k_màn, trúng quái Bay | S7 | `Player/Abilities/` | EM: sát thương wave 1/5/15 | ☐ | |
| A-14 | 15 | `SaveData`, `SaveService` ghi an toàn | S9 | `Meta/Save/` | EM: lưu/đọc giống hệt; lần lưu đầu | ☐ | |
| A-15 | 16 | Đọc lỗi → bak → tạo mới từ `StarterSettings` | S9 | `Meta/Save/` | EM: file hỏng, cả hai hỏng | ☐ | |
| A-16 | 17–18 | Hướng dẫn màn 1: 4 bước theo event, nút Bỏ qua, `tutorialDone` | S10 | `UI/Tutorial/` | Tay | ☐ | |
| A-17 | 19, 23 | Cân bằng lần 1 và 2 (Excel → CSV → import), ghi lý do vào cột ghi chú | — | `Docs/Can bang…xlsx`, `Data/Balance/` | Tay: chơi 3 màn | ☐ | |

---

## 5. Người B – Hệ thống và UI

| ID | Ngày | Việc | Spec | Vùng file | Kiểm tra | Xong | Ghi chú |
|---|---|---|---|---|---|---|---|
| B-01 | 3 | `PlacementRules`, kiểm tra hợp lệ theo thứ tự 6 bước (class thuần) | S4 | `Heroes/Placement/` | EM: giới hạn số con, quân số 15, thiếu Vàng | ☐ | |
| B-02 | 4–5 | `PlacementSystem`, `GhostPreview`; giữ chế độ đặt nếu còn đặt được; Q hủy xem trước | S4 | `Heroes/Placement/` | PM: không đặt trên đường, nước, vách, chồng tướng | ☐ | |
| B-03 | 4 | `Wallet` + `OnGoldChanged` | S4 | `Heroes/Placement/` | EM | ☐ | |
| B-04 | 8 | `HeroLevel`, `HeroLevelTable` 3 cấp, hiệu ứng cấp 3 qua `HeroVisual.auraAnchor` | S4 | `Heroes/Placement/` | EM: giá và chỉ số khớp bảng | ☐ | |
| B-05 | 5 | Bán: `floor(goldSpent × 0.5)` | S4 | `Heroes/Placement/` | EM: Epic cấp 2 bán 306 | ☐ | |
| B-06 | 6–7 | `GameManager` (state machine thuần), `TimeController`, `Castle`, sao, Alt-Tab tạm dừng | S5 | `Game/` | EM: mọi dòng bảng chuyển trạng thái; sao ở 20/15/14/8/7/1 | ☐ | |
| B-07 | 7, 17 | Ngọc thưởng sau trận, chỉ trả phần sao cao hơn; Vô tận ghi kỷ lục | S5 | `Game/` | EM: lần đầu 2 sao, lần sau 3 sao +50 | ☐ | |
| B-08 | 15 | `GachaService`: x1, x10 tính trước trong bộ nhớ, bảo đảm Epic+ | S8 | `Meta/Gacha/` | EM: 100.000 lượt lệch ≤ 0.5 điểm % | ☐ | |
| B-09 | 15 | Pity 25, giữ qua lưu/tải | S8 | `Meta/Gacha/` | EM | ☐ | |
| B-10 | 16 | `CollectionService`: ghép 3 bản thừa cùng độ hiếm, không ghép Legendary | S8 | `Meta/Collection/` | EM: không bao giờ về 0 bản | ☐ | |
| B-11 | 16 | `StarterSettings`: 5 tướng + 1.000 Ngọc, `ownedEver` có sẵn 5 tướng | S8 | `Meta/Collection/` | EM | ☐ | |
| B-12 | 16 | `DeckService`: 5 tướng khác nhau, kiểm tra trước khi vào trận | S8 | `Meta/Collection/` | EM | ☐ | |
| B-13 | 9 | `UIManager` (Push/Pop, Esc đóng màn trên cùng), `ConfirmDialog` | S10 | `UI/` | PM | ☐ | |
| B-14 | 9–12 | HUD trận: thanh trên, 5 ô deck, ô Thiên thạch, bảng tướng, `OffscreenWarning`, `WorldHealthBar` – chỉ nhận event | S10 | `UI/` | EM: phát event giả, giá trị khớp | ☐ | |
| B-15 | 18 | Script cho màn hình sảnh: Chọn màn, Gacha, Túi & Deck, Kết quả, Cài đặt | S10 | `UI/` | Tay | ☐ | |
| B-16 | 22 | Bảng Debug F1 (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`) | S9, S10 | `UI/Debug/`, `Editor/Debug/` | Tay: không có trong build Release | ☐ | |

---

## 6. Người C – Art và kỹ thuật hình ảnh

| ID | Ngày | Việc | Spec | Vùng file | Kiểm tra | Xong | Ghi chú |
|---|---|---|---|---|---|---|---|
| C-01 | 3 | `HeroVisual` (`muzzle`, `overhead`, `auraAnchor`, `animator`) | S0b | `Heroes/Visual/` | Tay | ☐ | |
| C-02 | 3, 14 | Tools → Validate Hero Prefabs, Tools → Render Hero Icons | S0b | `Editor/Heroes/` | Tay: báo thiếu đúng; icon 256×256 | ☐ | |
| C-03 | 3 | `PlayerController` (6 / 9 m/s, `unscaledDeltaTime`, không nhảy) | S3 | `Player/Movement/` | PM: ở ×2 vẫn 6 m/s thực | ☐ | |
| C-04 | 4 | `CameraRig` Cinemachine 3 (Third Person Follow, Deoccluder, Impulse), Brain Ignore Time Scale | S3 | `Player/Movement/`, `Prefabs/Player/` | Tay: không xuyên vật cản | ☐ | |
| C-05 | 4 | `InputRouter` + Input Actions: Hold/Tap chuột phải, thứ tự Esc, 1 Action Map bật tại một thời điểm, click UI không xuyên | S3 | `Player/Movement/`, `Input/` | EM/PM: chuyển Action Map; Esc 3 tình huống | ☐ | |
| C-06 | 5 | Map graybox thành prefab `Map_Level1`: mặt đất, Spline đường chính (~90 m) + đường bay gắn vào `LevelPaths`, Cổng, Thành, layer Placeable/Path/Obstacle | GDD 3, S2 | `Prefabs/Map/`, `Scenes/Game.unity` | Tay | ☐ | |
| C-07 | 6 | Chọn asset pack, ghi `ThirdParty/CREDITS.md` | — | `Assets/ThirdParty/` | Tay | ☐ | |
| C-08 | 8–12 | Model + animation 9 tướng, 5 loại quái, Triệu Hồi Sư; Prefab Variant + `HeroVisual`; gắn prefab + icon vào `HeroData` | GDD 12 | `Art/`, `Prefabs/Heroes/`, `Prefabs/Enemies/` | Tay: Validate Hero Prefabs không báo lỗi | ☐ | |
| C-09 | 11, 13 | VFX: đạn, nổ, Bỏng, Băng, Sét, Hào quang độ hiếm, Thiên thạch, Ultimate | GDD 6, 12 | `Prefabs/FX/` | Tay | ☐ | |
| C-10 | 9 | Font TextMeshPro Dynamic có đủ dấu tiếng Việt | S10 | `Art/Fonts/` | Tay: mọi màn hình | ☐ | |
| C-11 | 9–10 | Bố cục prefab HUD trận (theo hình HUD trong GDD mục 9) | S10 | `Prefabs/UI/` | Tay: 1920×1080 và 1366×768 | ☐ | |
| C-12 | 17 | Bố cục prefab màn hình sảnh | S10 | `Prefabs/UI/` | Tay | ☐ | |
| C-13 | 18 | `AudioManager` (Mixer Master/Music/SFX, pool 24, tối đa 4 bản cùng lúc), `MusicPlayer` | S11 | `Audio/` | PM: 100 quái chết không quá 4 âm thanh | ☐ | |
| C-14 | 19 | `FeedbackService`: số sát thương (≤ 60), rung camera (tắt được), nháy trắng | S11 | `Audio/` | Tay | ☐ | |
| C-15 | 7, 20 | Đo hiệu năng: FPS 2 cấu hình, GC Alloc, Memory sau 50 wave Vô tận ×2 | GDD 13 | — | Tay: ghi số liệu vào báo cáo | ☐ | |
| C-16 | 22, 24 | Build Release IL2CPP + Development | GDD 9 | `Builds/` (không commit) | Tay: chạy trên máy sạch | ☐ | |
| C-17 | 15–16 | Dựng map thật, ánh sáng, LOD | GDD 3 | `Scenes/Game.unity`, `Art/` | Tay: FPS vẫn đạt mục tiêu | ☐ | |

---

## 7. Checklist cho mọi PR (người mở PR tự tick trong mô tả PR)

- [ ] Chỉ sửa file trong vùng của mình (`TEAM_ASSIGNMENT.md` mục 2), hoặc có issue `cross-owner` được chủ vùng đồng ý.
- [ ] Không hardcode số cân bằng; số mới nằm trong SO hoặc CSV.
- [ ] Không `Instantiate`/`Destroy` trong trận; không tạo rác trong `Update`.
- [ ] Không đọc input ngoài `InputRouter`; không sửa `Time.timeScale` ngoài `TimeController`.
- [ ] Test mới chạy xanh trong Test Runner; test cũ không đỏ.
- [ ] Đã tick dòng tương ứng ở mục 3–6 của file này.
- [ ] Nếu đổi luật, event, asmdef, chủ sở hữu hoặc lịch: đã cập nhật đủ các file theo bảng "Khi thay đổi một thứ" ở `CLAUDE.md` mục 1.
- [ ] Có "Hướng dẫn lắp trong Unity" nếu cần lắp component/prefab.
- [ ] Liệt kê `// STUB:` và `// ASSUMPTION:` còn lại.

---

## 8. Kiểm tra trước khi nộp (Ngày 27)

- [ ] Mọi dòng R01–R31 ở mục 2 đã ☑.
- [ ] Chơi trọn 3 màn và 15 wave Vô tận không lỗi trên build Release.
- [ ] Mở game lần đầu: có 5 tướng, 1.000 Ngọc, hướng dẫn màn 1 hiện.
- [ ] Tắt game ngay sau khi quay gacha, mở lại: còn tướng vừa quay, Ngọc đã trừ.
- [ ] Bảng Debug có trong bản Development, không có trong bản Release.
- [ ] FPS và GC đạt mục tiêu (C-15), có ảnh Profiler cho báo cáo.
- [ ] `ThirdParty/CREDITS.md` đủ nguồn và giấy phép.
