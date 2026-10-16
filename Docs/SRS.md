# SRS – Fate Bastion (Thành Trì Vận Mệnh)

> **Đặc tả yêu cầu phần mềm** theo cấu trúc IEEE 830. Bản này tách **Yêu cầu chức năng (FR)** và **Yêu cầu phi chức năng (NFR)** rõ ràng, thay cho cách trình bày lẫn lộn trong bản đặc tả kỹ thuật cũ (`Docs/root/Dac ta ky thuat.md`).
> **Phiên bản:** 1.0 · **Ngày:** 16/10/2026 · **Trạng thái:** dự thảo để so sánh với đặc tả cũ · **Người lập:** A (Gameplay)
> **Phạm vi:** toàn bộ game (S0–S11), mọi yêu cầu R01–R31.

**Quan hệ giữa các tài liệu:**

| Tài liệu | Trả lời câu hỏi | Vai trò |
|---|---|---|
| GDD (`Docs/root/GDD - Tower Defense RNG.md`) | Chơi như thế nào? | Luật thiết kế (nguồn cao nhất về luật) |
| **SRS này (`Docs/SRS.md`)** | **Hệ thống phải làm được gì?** | Yêu cầu ở mức "cái gì" (FR/NFR), có mã để truy vết |
| Specs (`Docs/Specs/S*.md`) | Làm thế nào trong Unity? | Đặc tả kỹ thuật ở mức "như thế nào" |
| `Docs/TRACEABILITY.md` | Ai làm việc nào, ngày nào? | Checklist công việc A-xx/B-xx/C-xx |

Khi SRS và GDD khác nhau về **luật**: GDD thắng, và phải sửa SRS. Khi SRS và Specs khác nhau: Specs thắng về **chi tiết kỹ thuật**, SRS thắng về **hành vi bắt buộc**; ghi lại chênh lệch để đồng bộ.

---

## 1. Giới thiệu

### 1.1 Mục đích

Tài liệu này đặc tả đầy đủ yêu cầu chức năng và phi chức năng của **Fate Bastion** để: (1) là cơ sở để lập kế hoạch và kiểm thử, (2) là nguồn truy vết cho `TRACEABILITY.md`, (3) thay thế dần bản đặc tả kỹ thuật trộn lẫn yêu cầu. Đối tượng đọc: 3 thành viên nhóm (A gameplay, B hệ thống/UI, C art/kỹ thuật hình ảnh), giảng viên hướng dẫn, và agent hỗ trợ code.

### 1.2 Phạm vi

Fate Bastion là game **Tower Defense 3D góc nhìn thứ ba** trên PC (bàn phím + chuột), làm bằng Unity 6.3 LTS + URP. Người chơi điều khiển **Triệu Hồi Sư** trên map, trả **Vàng** để đặt tướng (tối đa 15) chặn quái đi từ Cổng tới Thành, và nâng cấp tướng (cấp 1→3). Tướng lấy từ **gacha** bằng **Ngọc** ở ngoài trận, ghép bản thừa, chọn **deck 5 tướng**. Trong trận **không có yếu tố ngẫu nhiên** (không chí mạng, không tỉ lệ kích hoạt); mọi RNG nằm ở gacha và ghép. Sản phẩm gồm 3 màn chiến dịch và chế độ Vô tận.

### 1.3 Định nghĩa, thuật ngữ, viết tắt

| Thuật ngữ | Nghĩa |
|---|---|
| Triệu Hồi Sư | Nhân vật người chơi; không máu; có Hào quang Chỉ huy và Thiên thạch |
| Vàng / Ngọc | Tiền tệ trong trận / ngoài trận |
| Độ hiếm (Rarity) | Common, Rare, Epic, Legendary — tỉ lệ 65/25/8.5/1.5 |
| Hệ (Element) | Fire (Lửa), Ice (Băng), Lightning (Lôi) |
| Vai trò (Role) | SingleDps, Mage, Control, Support |
| Bản thừa | `copies − 1` của một tướng; dùng để ghép |
| Đầu đoàn | Quái đi xa nhất trên Spline (so `distance`), ưu tiên mục tiêu |
| Hào quang Chỉ huy | Buff bị động của Triệu Hồi Sư: 6 m, +25% tốc đánh |
| Thiên thạch | Năng lực chủ động (Q) của Triệu Hồi Sư |
| Nộ | Tài nguyên Ultimate của tướng Legendary, đầy ở 100 |
| FR / NFR | Yêu cầu chức năng / Yêu cầu phi chức năng |
| EM / PM | Test EditMode / Test PlayMode |
| M / S / C | MoSCoW: Must / Should / Could |

### 1.4 Tài liệu tham khảo

- `Docs/root/GDD - Tower Defense RNG.docx` / `.md` — GDD v1.3
- `Docs/root/Dac ta ky thuat.docx` / `.md` — Đặc tả kỹ thuật v1.3 (bản cũ, giữ để so sánh)
- `Docs/root/Can bang - Fate Bastion.xlsx` và `Docs/root/CanBang/*.md` — file cân bằng
- `Docs/Specs/S0.md`, `S0b.md`, `S1.md` … `S11.md` — đặc tả từng hệ thống
- `Docs/TEAM_ASSIGNMENT.md`, `Docs/ROADMAP.md`, `Docs/TRACEABILITY.md`, `CLAUDE.md`
- Chuẩn: IEEE 830 (cấu trúc SRS)

### 1.5 Tổng quan tài liệu

Mục 2 mô tả tổng quan sản phẩm. Mục 3 là yêu cầu chi tiết: giao diện (3.1), chức năng theo miền (3.2), hiệu năng (3.3), ràng buộc thiết kế (3.4), thuộc tính hệ thống (3.5), yêu cầu khác (3.6). Mục 4 là phụ lục: thuật ngữ, ma trận truy vết, số liệu thiết kế, lịch sử phiên bản.

---

## 2. Mô tả tổng quan

### 2.1 Bối cảnh sản phẩm

#### 2.1.1 Ngữ cảnh hệ thống

Game đứng độc lập (single-player, offline), không có backend. Vòng chơi: **Lobby** (gacha, túi, ghép, deck) → **Chọn màn** → **Trận** (đặt/nâng/bán tướng, chặn quái, thắng/thua) → **Kết quả** (sao, Ngọc thưởng) → về Lobby. Tiến trình lưu cục bộ.

#### 2.1.2 Giao diện người dùng (tóm tắt)

- **Trong trận:** HUD tĩnh (Canvas) — thanh trên (Wave, máu Thành, Bỏ qua, tốc độ, cài đặt), Vàng, Quân số, ô Thiên thạch, 5 ô deck; bảng thông tin tướng (chỉ số, nâng cấp, bán, Ultimate); Canvas động (thanh máu quái, số sát thương, cảnh báo quái gần Thành).
- **Ngoài trận:** Sảnh, Chọn màn, Gacha, Túi & Deck, Kết quả, Cài đặt, Hướng dẫn màn 1, Bảng Debug (chỉ Editor/Development).
- Ngôn ngữ: tiếng Việt, font TMP Dynamic đủ dấu. Độ phân giải 1920×1080 và 1366×768.

#### 2.1.3 Giao diện phần cứng

PC Windows x64, bàn phím + chuột. Không hỗ trợ tay cầm. Mục tiêu GPU: ≥ 60 FPS trên RTX 3050, ≥ 30 FPS trên Intel UHD ở 1080p.

#### 2.1.4 Giao diện phần mềm

Unity 6.3 LTS + URP; Input System; Cinemachine 3.x; Unity Splines; uGUI + TextMeshPro; PrimeTween; Newtonsoft Json; Particle System; Unity Test Framework. Xem mục 3.4 (CON).

#### 2.1.5 Ràng buộc bộ nhớ / hiệu năng

Không `Instantiate`/`Destroy` trong trận; dùng object pool (prewarm khi load màn). Không tạo rác mỗi frame. Tối đa 100 quái cùng lúc.

### 2.2 Chức năng sản phẩm (tóm tắt)

1. **Meta ngoài trận:** gacha x1/x10, túi (bản sao), ghép bản thừa, deck 5 tướng, lưu tiến trình.
2. **Trận:** luồng trạng thái, đặt/nâng/bán tướng, kinh tế Vàng, 15 wave + boss.
3. **Chiến đấu:** tính sát thương tập trung, 3 hệ hiệu ứng, kỹ năng 7 kiểu, Nộ/Ultimate.
4. **Triệu Hồi Sư:** di chuyển + camera, Hào quang Chỉ huy, Thiên thạch.
5. **Trình bày:** HUD/UI, UI thế giới, âm thanh, phản hồi, hướng dẫn, bảng Debug.
6. **Nội dung:** 9 tướng, 4 loại quái + boss, 3 màn + Vô tận, dữ liệu cân bằng từ CSV.

### 2.3 Đặc điểm người dùng

3 thành viên sinh viên (đồ án 3 người/4 tuần), và người chơi cuối là người quen với game tower defense trên PC. Không yêu cầu kỹ năng đặc biệt; hướng dẫn màn 1 dạy 4 thao tác chính.

### 2.4 Ràng buộc

- Thời gian: 4 tuần; **feature freeze** sau ngày 21 (theo `ROADMAP.md`); mục tiêu hoàn thành theo plan 22 ngày của A (`Docs/Tson/plan.md`).
- Nhân lực: 3 người, mỗi người sở hữu vùng file riêng (`TEAM_ASSIGNMENT.md` mục 2).
- Kỹ thuật: xem CON ở mục 3.4.
- Phạm vi: xem CON-SCOPE-01 (mục 3.4).

### 2.5 Giả định và phụ thuộc

- **Giả định:** người chơi có chuột + bàn phím; máy có GPU rời (kiểm thử chính) và GPU tích hợp (kiểm thử tối thiểu).
- **Phụ thuộc nội bộ:** S0 (Interface/Data/Core event) phải xong trước mọi hệ thống; S4/S5 phụ thuộc S2, S3; S6 phụ thuộc S1/S0b; S7 phụ thuộc S3, S6; S9 phụ thuộc S8; S10 phụ thuộc S1–S9; S11 phụ thuộc S1–S10.
- **Phụ thuộc ngoài:** các package Unity và asset pack miễn phí (ghi nguồn ở `Assets/ThirdParty/CREDITS.md`).

---

## 3. Yêu cầu chi tiết

Quy ước: **phải** = bắt buộc (M); **nên** = quan trọng (S); **có thể** = tùy chọn (C). Cột "Nguồn" trỏ GDD/Spec; cột "Chủ" là A/B/C.

### 3.1 Yêu cầu giao diện ngoài

#### 3.1.1 Giao diện người dùng

| ID | Yêu cầu | Mức |
|---|---|---|
| UI-INT-01 | Mọi màn hình phải mở/đóng đúng, Esc đóng màn hình trên cùng (ngăn xếp) | M |
| UI-INT-02 | Click lên UI không được xuyên xuống thế giới (`EventSystem.current.IsPointerOverGameObject()`) | M |
| UI-INT-03 | Chỉ một Action Map (`Summoner` hoặc `UI`) bật tại một thời điểm | M |
| UI-INT-04 | HUD chỉ đổi giá trị khi dữ liệu đổi; không đọc dữ liệu gameplay trong `Update` | M |
| UI-INT-05 | Chữ tiếng Việt hiển thị đủ dấu trên mọi màn hình, ở 1920×1080 và 1366×768 | M |

#### 3.1.2 Giao diện phần mềm (API nội bộ giữa các hệ thống)

Hợp đồng chốt ở `TEAM_ASSIGNMENT.md` mục 3.1. SRS xác nhận các điểm nối bắt buộc tồn tại: `IDamageable.TakeDamage`, `CombatEvents`, `GameEvents`, `HeroEvents`, `AbilityEvents`, `InputRouter`, `Wallet.OnGoldChanged`, `HeroVisual`, `SaveService`, `LevelPaths`, `CancelStack`. (Chi tiết "cách dùng" ghi trong mô tả PR.)

#### 3.1.3 Giao diện truyền thông

Không có (offline).

### 3.2 Yêu cầu chức năng

#### 3.2.1 Luồng trận (FR-GAM)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-GAM-01 | Hệ thống phải có trạng thái `Loading → Preparing → InWave → Resting → Won/Lost` và chuyển trạng thái đúng bảng ở S5 | M | GDD 7 | S5 | B |
| FR-GAM-02 | Mỗi màn có 15 wave; chuẩn bị 15 s; nghỉ 8 s; boss ở wave 5/10/15 | M | GDD 7 | S5 | B |
| FR-GAM-03 | Thành có 20 máu; quái thường −1, quái đặc biệt −2, boss −10 | M | GDD 7 | S5 | B |
| FR-GAM-04 | Thắng khi wave cuối sạch quái và Thành còn máu; thua khi máu Thành ≤ 0 (Thua ưu tiên nếu cùng frame) | M | GDD 7 | S5 | B |
| FR-GAM-05 | Tính sao: máu Thành ≥ 15 → 3★; ≥ 8 → 2★; còn sống → 1★ | M | GDD 7 | S5 | B |
| FR-GAM-06 | Tốc độ ×1/×2 và tạm dừng, chỉ `TimeController` sửa `Time.timeScale`; Alt-Tab/mất focus tự tạm dừng | M | GDD 9 | S5 | B |
| FR-GAM-07 | Bỏ qua chờ wave (phím N) cộng +50 Vàng, có hộp xác nhận và tùy chọn "không hỏi lại" | S | GDD 8 | S5 | B |
| FR-GAM-08 | Có 3 màn chiến dịch + Vô tận (dùng màn 3, sau wave 15 lặp wave 6–15, boss mỗi 5 wave, `hpGrowth` tiếp tục) | S | GDD 9 | S2, S5 | A, B |
| FR-GAM-09 | Esc theo thứ tự ưu tiên qua `CancelStack`, mỗi lần bấm một bước: hủy ngắm Thiên thạch/hủy đặt tướng → đóng bảng tướng → mở Tạm dừng | M | GDD 4 | S3 | C |

#### 3.2.2 Chiến đấu và sát thương (FR-CBT)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-CBT-01 | Chỉ một nơi tính sát thương (`DamageCalculator`, hàm thuần): `ST = D_thực × k_kỹ năng × (1 − kháng)`; kháng kẹp [0; 0.8]; True bỏ kháng; làm tròn xuống; tối thiểu 1; không chí mạng | M | GDD 2 | S1 | A |
| FR-CBT-02 | Ba hệ hiệu ứng: **Lửa** Bỏng 20%/s trong 3 s (lấy mạnh nhất, làm mới thời gian); **Băng** chậm −30% trong 2 s (tổng ≤ −60%, boss chịu 50% hiệu lực); **Lôi** nảy tối đa 2 quái trong 3 m, mỗi lần −30%, không nảy lại quái đã trúng | M | GDD 6 | S1 | A |
| FR-CBT-03 | Quái chết khi đạn đang bay: đạn bay tới vị trí cuối rồi tắt, không gây sát thương; nhiều nguồn giết cùng frame chỉ phát `OnEnemyKilled` một lần (cờ `IsAlive`) | M | GDD 2 | S1 | A |
| FR-CBT-04 | Tầm đánh và vùng nổ tính trên mặt phẳng ngang XZ; tướng Melee không chọn được quái Bay; Thiên thạch trúng quái Bay; sát thương thừa không chuyển sang quái khác | M | GDD 2 | S1 | A |
| FR-CBT-05 | Phát `CombatEvents.OnDamageDealt` và `OnEnemyKilled` (payload struct ở Core) cho HUD/UI thế giới/âm thanh | M | GDD 2 | S1 | A |

#### 3.2.3 Quái và wave (FR-ENM)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-ENM-01 | Có 4 loại quái + boss: Thường (40 HP, 2.5 m/s), Nhanh (×0.5 HP, ×1.8 tốc độ), Giáp nặng (×1.5 HP, giáp 50%), Bay (×0.8 HP, đường riêng cao 2 m), Boss (×15 HP, giáp/kháng 20%, gọi 3 quái Nhanh mỗi 12 s) | M | GDD 7 | S2 | A |
| FR-ENM-02 | Máu khi sinh: `baseHP × hpGrowth^(wave−1) × hpMultiplier` (hpGrowth = 1.12; hpMultiplier màn 1/2/3 = 1.0/1.15/1.3); lưu `float` | M | GDD 7 | S2 | A |
| FR-ENM-03 | Quái đi theo Spline; tới Thành khi `distance ≥ pathLength` (không dùng va chạm) | M | GDD 7 | S2 | A |
| FR-ENM-04 | Tối đa 100 quái sống; vượt thì chờ hàng đợi; hàng đợi vẫn tính vào điều kiện kết thúc wave | M | GDD 7 | S2 | A |
| FR-ENM-05 | Wave: các nhóm quái chạy song song theo `startDelay` và `interval`; wave kết thúc khi đã sinh hết và không còn quái sống (kể cả quái con do boss gọi) | M | GDD 7 | S2 | A |
| FR-ENM-06 | Cung cấp dữ liệu cho UI thế giới: `OnEnemySpawned/OnEnemyDespawned`, `ActiveEnemies`; `EnemyController` có `CurrentHP`, `MaxHP`, `IsBoss`, `Progress` (0–1), `HealthBarAnchor`, event `OnHealthChanged` | M | GDD 9 | S2, S10 | A |
| FR-ENM-07 | Quái tới Thành phát `OnEnemyReachedCastle(castleDamage)`; chỉ `Castle` (B) trừ máu rồi phát `GameEvents.OnCastleDamaged` | M | GDD 7 | S2, S5 | A |
| FR-ENM-08 | Quái bị hạ phát `OnEnemyKilled` có `GoldReward`; chỉ `Wallet` (B) cộng Vàng | M | GDD 8 | S2, S4 | A |

#### 3.2.4 Đặt tướng, nâng cấp, kinh tế trong trận (FR-PLC)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-PLC-01 | Chọn tướng từ 5 ô deck (phím 1–5 hoặc bấm) → vào chế độ xem trước (ghost xanh hợp lệ / đỏ không hợp lệ) | M | GDD 3 | S4 | B |
| FR-PLC-02 | Kiểm tra hợp lệ theo **đúng thứ tự 6 bước**, dừng ở lỗi đầu và hiện lý do: (1) đủ Vàng, (2) `placedCount` < số con tối đa của độ hiếm, (3) tổng quân số < 15, (4) raycast trúng layer Placeable, (5) `Physics.CheckSphere` bán kính 0.8 m không chạm Hero/Path/Obstacle, (6) không đang ngắm Thiên thạch | M | GDD 3 | S4 | B |
| FR-PLC-03 | Giới hạn đặt theo độ hiếm: Common 100/6, Rare 200/5, Epic 350/4, Legendary 600/2; quân số tối đa 15 | M | GDD 3, 8 | S4 | B |
| FR-PLC-04 | Nâng cấp cấp 2 = `floor(75% giá đặt)`, +40% sát thương, +10% tốc đánh, +10% tầm; cấp 3 = `floor(150%)`, +100%, +20%, +15%; hiệu ứng cấp 3 qua `HeroVisual.AuraAnchor` | M | GDD 8 | S4 | B |
| FR-PLC-05 | Bán tướng hoàn `floor(goldSpent × 0.5)` (vd Epic cấp 2 bán 306) | M | GDD 8 | S4 | B |
| FR-PLC-06 | Giữ chế độ đặt nếu vẫn đủ Vàng, chưa đạt số con tối đa và quân số chưa đầy; Q hủy xem trước; chuột phải/Esc hủy xem trước | S | GDD 3 | S4 | B |
| FR-PLC-07 | Vàng trong trận: khởi đầu 300; hạ quái +5 (thường) / +10 (đặc biệt) / +150 (boss); cuối wave +50 + 10×n; bỏ qua chờ +50 | M | GDD 8 | S4, S5 | B |
| FR-PLC-08 | API đặt tướng: `HeroController.Init(HeroData)`, `ResetState()`, `OnDespawn()`; `HeroStats.SetLevelMultipliers(damage, attackSpeed, range)`; phát `HeroEvents.OnHeroPlaced/Sold/Upgraded/Selected` | M | GDD 3 | S4, S6 | A, B |

#### 3.2.5 Tướng, kỹ năng và Ultimate (FR-HRO)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-HRO-01 | `HeroController` + `TargetingModule` (mặc định "Đầu đoàn"; vai trò `SingleDps` luôn ưu tiên boss nếu boss trong tầm; Melee không chọn Bay) + `AttackModule` chạy theo **timer logic**, không dựa Animation Event; đòn đánh xuất phát từ `HeroVisual.Muzzle` | M | GDD 6 | S1, S6 | A |
| FR-HRO-02 | `SkillData` + `SkillRunner` hỗ trợ 7 kiểu: MeleeHit, Projectile, AreaAtPoint, Cone, Line, Zone, Buff; thêm kỹ năng cùng kiểu bằng dữ liệu, không sửa code | M | GDD 6 | S6 | A |
| FR-HRO-03 | `aimMode`: `Auto` (theo ưu tiên mục tiêu) hoặc `GroundPoint` (chỉ Thiên thạch); thời điểm gây sát thương do `castDelay` | M | GDD 6 | S6 | A |
| FR-HRO-04 | Cộng dồn buff: cùng một **loại tướng** chỉ lấy mức mạnh nhất; khác nguồn thì cộng; tổng tốc đánh tối đa **+60%** | M | GDD 6 | S6 | A |
| FR-HRO-05 | Nộ (chỉ Legendary): +5 mỗi lần ra đòn (không theo số quái trúng), tối đa 100; Ultimate tiêu hết 100, **kích hoạt tay** (phím R hoặc nút), tự ngắm điểm đông quái nhất; nếu không có quái trong tầm thì không dùng, giữ Nộ | S | GDD 6 | S6 | A |
| FR-HRO-06 | Phát `HeroController.OnRageChanged(current, max)` và `TryActivateUltimate()` trả `bool`; HUD đăng ký khi nhận `OnHeroSelected`, hủy khi bỏ chọn | S | GDD 9 | S6, S10 | A |
| FR-HRO-07 | 9 tướng có đòn đánh/bị động đúng GDD mục 6 (đòn thường theo vai trò; bị động theo hệ/vai trò) | M | GDD 6 | S6 | A |

#### 3.2.6 Triệu Hồi Sư và Camera (FR-SUM) — chủ C

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-SUM-01 | Triệu Hồi Sư đi bằng WASD 6 m/s, Shift chạy 9 m/s, không nhảy; dùng `Time.unscaledDeltaTime`, không chia `timeScale` | M | GDD 4 | S3 | C |
| FR-SUM-02 | Camera góc nhìn thứ ba (Cinemachine: ThirdPersonFollow, Deoccluder, Impulse), zoom 4–25 m, góc ngẩng 10–60°, giữ chuột phải để xoay; Brain bật Ignore Time Scale | M | GDD 4 | S3 | C |
| FR-SUM-03 | Chỉ `InputRouter` đọc Input System; hệ thống khác không đọc phím trực tiếp; chuột phải giữ = xoay, bấm nhanh (Tap < 0.2 s, < 5 px) = hủy | M | GDD 4 | S3 | C |
| FR-SUM-04 | Rơi khỏi map thì dịch chuyển về điểm xuất hiện; camera bị kẹt thì Deoccluder kéo lại gần | C | GDD 4 | S3 | C |

#### 3.2.7 Năng lực Triệu Hồi Sư: Hào quang và Thiên thạch (FR-ABL) — chủ A

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-ABL-01 | Hào quang Chỉ huy (bị động, luôn bật, kể cả lúc nghỉ): bán kính 6 m, +25% tốc đánh; kiểm tra 5 lần/s; chỉ tính lại chỉ số khi tướng vào/ra vùng; bán tướng thì gỡ buff trước khi trả pool | M | GDD 4 | S7 | A |
| FR-ABL-02 | Thiên thạch: phím Q; tầm 20 m; bán kính nổ 4 m; rơi sau 0.8 s; hồi chiêu 45 s (thời gian game); sát thương `True = 3 × HP₀(Thường) × 1.12^(n−1) × k_màn` | M | GDD 4 | S7 | A |
| FR-ABL-03 | Trạng thái Thiên thạch: Sẵn sàng / Đang ngắm / Đang rơi / Hồi chiêu; hủy ngắm không tốn hồi chiêu; vào chế độ thì `CancelStack.Push(this)`, nghe `InputRouter.DeckSlot` để hủy (hai chế độ loại trừ với đặt tướng) | M | GDD 4 | S7 | A |
| FR-ABL-04 | Phát `AbilityEvents.OnMeteorCooldownChanged` và `OnMeteorExploded`; `CommandAura.OnHeroEnteredAura` cho Hướng dẫn; Thiên thạch trúng quái Bay; không thả được ngoài 20 m | M | GDD 4 | S7, S10 | A |
| FR-ABL-05 | Thiên thạch đang rơi mà trận kết thúc thì hủy, không gây sát thương; tạm dừng thì thời gian rơi dừng theo `timeScale` | S | GDD 4 | S7 | A |

#### 3.2.8 Gacha, Túi, Ghép, Deck (FR-MTA) — chủ B

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-MTA-01 | Gacha x1 = 100 Ngọc, x10 = 900 Ngọc (bảo đảm ≥ 1 Epic+); tỉ lệ công khai 65/25/8.5/1.5; pity 25 (lượt thứ 25 chắc Legendary) | M | GDD 5 | S8 | B |
| FR-MTA-02 | x10 tính trước cả 10 kết quả trong bộ nhớ (kể cả pity và bảo đảm Epic) rồi áp vào SaveData một lần; lần đầu sở hữu một tướng +50 Ngọc | M | GDD 5 | S8 | B |
| FR-MTA-03 | Ghép 3 bản thừa cùng độ hiếm (Common/Rare/Epic, không Legendary) → 1 tướng ngẫu nhiên bậc kế; mỗi tướng luôn giữ ≥ 1 bản nên deck không đổi | M | GDD 5 | S8 | B |
| FR-MTA-04 | Deck đúng 5 tướng khác nhau, mỗi tướng ≥ 1 bản; người chơi mới có 5 tướng khởi đầu + 1.000 Ngọc, 5 tướng nằm sẵn trong `ownedEver` | M | GDD 5 | S8 | B |
| FR-MTA-05 | Ngọc thưởng: lần đầu qua màn +300; mỗi sao mới +50; tướng mới sở hữu +50; Vô tận +10 mỗi 5 wave; chơi lại chỉ trả phần sao cao hơn | M | GDD 8 | S5, S8 | B |
| FR-MTA-06 | RNG dùng `System.Random` truyền từ ngoài (test cố định seed); không dùng `UnityEngine.Random` trong Meta | M | GDD 5 | S8 | B |

#### 3.2.9 Lưu trữ (FR-SAV) — chủ A

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-SAV-01 | `SaveService` dùng Newtonsoft Json tại `Application.persistentDataPath/save.json`; `SaveData` gồm `version`, `gems`, `pityCounter`, `collection`, `deck`, `ownedEver`, `levelProgress`, `endlessBest`, `tutorialDone`, `settings` | M | GDD 11 | S9 | A |
| FR-SAV-02 | Ghi an toàn: ghi `save.tmp` → `File.Replace(tmp, json, bak)` nếu đã có `save.json`, ngược lại `File.Move(tmp, json)`; không bao giờ ghi đè trực tiếp | M | GDD 11 | S9 | A |
| FR-SAV-03 | Đọc: `save.json` → lỗi thì `save.bak` → lỗi nữa thì tạo mới từ `StarterSettings` và báo người chơi; không crash | M | GDD 11 | S9 | A |
| FR-SAV-04 | Lưu ngay sau: quay gacha, ghép, đổi deck, kết quả trận, đổi cài đặt, `OnApplicationQuit`; **không** lưu trạng thái giữa trận | M | GDD 11 | S9 | A |
| FR-SAV-05 | `heroId` không còn tồn tại → bỏ qua + log; deck chứa tướng không hợp lệ → thay bằng tướng khởi đầu còn trống + báo người chơi | S | GDD 11 | S9 | A |

#### 3.2.10 HUD và UI (FR-UI) — chủ B, C

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-UI-01 | `UIManager` điều hướng ngăn xếp (Push/Pop), Esc đóng màn trên cùng; `ConfirmDialog` (tiêu đề, nội dung, Xác nhận/Hủy, tùy chọn "không hỏi lại") dùng cho Bỏ qua/Ghép/Bán tướng cấp 3 | M | GDD 9 | S10 | B |
| FR-UI-02 | HUD trận: Wave, máu Thành, Bỏ qua, Tốc độ ×1/×2, Cài đặt, cảnh báo, Vàng, Quân số, ô Thiên thạch (`CooldownSlot`), 5 ô deck | M | GDD 9 | S10 | B |
| FR-UI-03 | Bảng thông tin tướng: chỉ số, cấp 1–3, Nâng cấp, Bán, Ultimate + thanh Nộ (Legendary); dùng `CancelStack` để Esc đóng | M | GDD 9 | S10 | B |
| FR-UI-04 | Màn hình ngoài trận: Sảnh, Chọn màn, Gacha, Túi & Deck, Kết quả, Cài đặt, Tạm dừng | M | GDD 9 | S10 | B |
| FR-UI-05 | UI thế giới (Canvas động): `WorldHealthBar` (pool, gắn/gỡ theo `OnEnemySpawned/Despawned`, ẩn khi đầy máu, thanh to cho boss), `OffscreenWarning` (quái `Progress` ≥ 0.8, mũi tên rìa màn hình), `DamageNumberSpawner` (pool, ≤ 60 số), `HitFlash` (nháy trắng bằng MaterialPropertyBlock) | M | GDD 9 | S10, S11 | C |
| FR-UI-06 | View chỉ nhận dữ liệu qua event và đọc field SO; không chứa logic game; tách Canvas tĩnh (HUD) và động (số sát thương/thanh máu) | M | GDD 9 | S10 | B, C |
| FR-UI-07 | Animation UI dùng PrimeTween với `useUnscaledTime: true` để chạy khi tạm dừng | M | GDD 9 | S10 | B |

#### 3.2.11 Hướng dẫn (FR-TUT) — chủ B

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-TUT-01 | Hướng dẫn màn 1: 4 bước theo event (đặt tướng `OnHeroPlaced`, nâng cấp `OnHeroUpgraded`, Hào quang `OnHeroEnteredAura`, Thiên thạch `OnMeteorExploded`), có nút Bỏ qua, chỉ hiện lần đầu (lưu `tutorialDone`) | S | GDD 9 | S10 | B |

#### 3.2.12 Âm thanh và phản hồi (FR-AUD) — chủ C

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-AUD-01 | `AudioManager` với AudioMixer (Master/Music/SFX) và pool 24 `AudioSource`; cùng một âm thanh không phát quá 4 bản cùng lúc, vượt thì bỏ qua | S | GDD 9 | S11 | C |
| FR-AUD-02 | `MusicPlayer` chuyển mượt nhạc sảnh / trận / boss | S | GDD 9 | S11 | C |
| FR-AUD-03 | `FeedbackService`: rung camera (Cinemachine Impulse) khi Thiên thạch nổ, boss xuất hiện, Thành mất máu, Ultimate; tắt được trong Cài đặt | S | GDD 9 | S11 | C |
| FR-AUD-04 | `SoundData` (SO): danh sách clip, âm lượng, dao động cao độ ±10%, nhóm mixer, số bản phát đồng thời tối đa | S | GDD 9 | S11 | C |
| FR-AUD-05 | Số sát thương: màu theo hệ, Thiên thạch to hơn; khi có hơn 60 số trên màn hình thì bỏ bớt số nhỏ | S | GDD 9 | S11 | C |

#### 3.2.13 Bảng Debug (FR-DBG) — chủ B

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-DBG-01 | Bảng Debug (F1): xóa save, cộng Ngọc, nhận một tướng bất kỳ, nhảy wave, bất tử Thành, tốc độ ×5; toàn bộ code bọc trong `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`, không có trong build Release | C | GDD 9 | S9, S10 | B |

#### 3.2.14 Dữ liệu cân bằng và import (FR-DAT) — chủ B (công cụ), A (số liệu)

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-DAT-01 | Công cụ `Tools → Import Balance CSV` từ `Data/Balance/*.csv` tạo/cập nhật đúng 9 `HeroData`, 9 `SkillData`, 5 `EnemyData`, 45 `WaveData`, 3 `LevelData`; chạy lần 2 không tạo thêm | M | GDD 12 | S0 | B, A |
| FR-DAT-02 | Bộ đọc CSV: UTF-8 có BOM, hiểu ô trong ngoặc kép, chấp nhận `,` và `;`, đọc số bằng `CultureInfo.InvariantCulture`; lỗi một dòng thì log kèm file + số dòng, bỏ qua dòng đó, không dừng import | M | GDD 12 | S0 | B |
| FR-DAT-03 | **Không hardcode số cân bằng**: mọi con số (giá, máu, tầm, hồi chiêu, %) nằm trong SO hoặc CSV | M | GDD 12 | S0 | mọi người |
| FR-DAT-04 | Field gắn tay (prefab/icon `HeroData`; `hpMultiplier`/`displayName`/`enemyTypesPreview` `LevelData`; prefab `EnemyData`; Ultimate Long Vương + VFX `SkillData`) không bị importer ghi đè | M | GDD 12 | S0 | A, C |

#### 3.2.15 Hình ảnh tướng và pipeline (FR-ART) — chủ C

| ID | Yêu cầu | Mức | Nguồn | Spec | Chủ |
|---|---|---|---|---|---|
| FR-ART-01 | Mỗi model tướng có `HeroVisual` ở gốc giữ 4 property chỉ đọc: `Muzzle`, `Overhead`, `AuraAnchor`, `Animator`; gameplay không tìm xương theo tên | M | GDD 12 | S0b | C |
| FR-ART-02 | Đổi ngoại hình tướng = thay model dưới `ModelRoot` + gắn lại `HeroVisual`, không sửa code/HeroData | M | GDD 12 | S0b | C |

### 3.3 Yêu cầu hiệu năng (NFR-PERF)

| ID | Yêu cầu | Mức | Nguồn | Chủ |
|---|---|---|---|---|
| NFR-PERF-01 | Đạt ≥ 60 FPS trên RTX 3050 và ≥ 30 FPS trên GPU tích hợp (Intel UHD), 1080p, với 100 quái | M | GDD 10, 13 | C (đo), mọi người (giữ) |
| NFR-PERF-02 | GC Alloc ≈ 0 B/frame trong trận (có ảnh Profiler cho báo cáo) | M | GDD 10, 13 | mọi người |
| NFR-PERF-03 | Không `Instantiate`/`Destroy` trong trận; dùng `UnityEngine.Pool.ObjectPool<T>`, prewarm khi load màn, `ResetState()` khi lấy từ pool | M | GDD 13 | mọi người |
| NFR-PERF-04 | Không LINQ / `new List` / nối string / `GetComponent` trong `Update`; dùng `Physics.OverlapSphereNonAlloc`/`RaycastNonAlloc` với mảng dựng sẵn; cache component trong `Awake` | M | GDD 13 | mọi người |
| NFR-PERF-05 | Một `EnemyManager` cập nhật tất cả quái (không mỗi quái một `Update`) | M | GDD 13 | A |
| NFR-PERF-06 | Tìm mục tiêu 5–10 lần/giây, Hào quang kiểm tra 5 lần/giây (không mỗi frame) | M | GDD 13 | A |
| NFR-PERF-07 | Text HUD chỉ đổi khi giá trị đổi; tách Canvas tĩnh và động | M | GDD 13 | B, C |

### 3.4 Ràng buộc thiết kế (CON)

| ID | Ràng buộc | Loại |
|---|---|---|
| CON-TECH-01 | Unity 6.3 LTS + URP; bật SRP Batcher và GPU Instancing | Kỹ thuật |
| CON-TECH-02 | Không dùng Addressables, Netcode, VFX Graph, DOTS/ECS, UniTask, LINQ trong code chạy mỗi frame, `Resources.Load` cho dữ liệu game, Singleton tràn lan | Kỹ thuật |
| CON-TECH-03 | PrimeTween là thư viện bên thứ ba **duy nhất** cho tween UI | Kỹ thuật |
| CON-TECH-04 | Cinemachine 3 dùng namespace `Unity.Cinemachine` (`CinemachineCamera`, `CinemachineThirdPersonFollow`, `CinemachineDeoccluder`, `CinemachineImpulseSource`) | Kỹ thuật |
| CON-TECH-05 | Chỉ `TimeController` sửa `Time.timeScale`; gameplay dùng `Time.deltaTime`, Triệu Hồi Sư/camera dùng `Time.unscaledDeltaTime` | Kỹ thuật |
| CON-SCOPE-01 | **Ngoài phạm vi** (không code, không hook "để sau"): Nhập Hồn, Mythic/Secret, lên sao, mảnh, Bùa May mắn, Index, Thử thách ngày, Co-op, mạng, bảng xếp hạng online, tốc độ ×3, cắt cảnh | Phạm vi |
| CON-ARCH-01 | Phụ thuộc asmdef một chiều: `Core ← Combat ← Enemies ← Skills ← Heroes ← Player`; `Core ← Meta`; `Game → Heroes, Enemies, Player, Meta`; `UI → Core, Combat, Enemies, Skills, Heroes, Game, Meta`; `Audio → Core`. Không tạo phụ thuộc vòng | Kiến trúc |
| CON-ARCH-02 | Giao tiếp giữa hệ thống bằng event (C# `event Action<T>` hoặc SO event channel); UI không đọc trạng thái gameplay trong `Update` | Kiến trúc |
| CON-ARCH-03 | Logic thuần tách khỏi MonoBehaviour (tính sát thương, gacha, ghép, kinh tế, state machine, Meteor state) để unit test EditMode | Kiến trúc |

### 3.5 Thuộc tính hệ thống

| ID | Thuộc tính | Yêu cầu | Mức |
|---|---|---|---|
| NFR-RELI-01 | Độ tin cậy | Lưu an toàn (FR-SAV-02/03), không mất dữ liệu khi tắt game; không lưu trạng thái giữa trận | M |
| NFR-RELI-02 | Độ tin cậy | RNG tách khỏi `UnityEngine.Random` trong Meta để test cố định seed | M |
| NFR-USA-01 | Khả dụng | Esc nhất quán theo thứ tự ưu tiên; click UI không xuyên; hiện lý do khi đặt tướng không hợp lệ | M |
| NFR-LOC-01 | Bản địa hóa | Font TMP Dynamic đủ dấu tiếng Việt trên mọi màn hình | M |
| NFR-MAINT-01 | Bảo trì | Mỗi file một class public; logic thuần có unit test EditMode; test PlayMode cho hành vi scene | M |
| NFR-MAINT-02 | Bảo trì | Enum/event hub dùng chung đúng định nghĩa ở Core; đổi thì cập nhật đồng bộ SRS/Specs/TRACEABILITY | M |
| NFR-PORT-01 | Khả chuyển | Windows x64; Mono khi dev, IL2CPP cho bản nộp | M |
| NFR-AVAIL-01 | Sẵn sàng | Game offline, không phụ thuộc mạng | M |

### 3.6 Yêu cầu khác

| ID | Yêu cầu | Mức |
|---|---|---|
| NFR-BLD-01 | Có build Release IL2CPP (bản nộp, không có Debug) và build Development (buổi bảo vệ) | M |
| NFR-DOC-01 | Tài liệu đồng bộ: `SRS.md`, `Specs/S*.md`, `TRACEABILITY.md`, `ROADMAP.md`, `TEAM_ASSIGNMENT.md`, `CLAUDE.md`, `Docs/Tson/*` | M |
| NFR-DATA-01 | Nguồn asset/âm thanh miễn phí có ghi nguồn và giấy phép ở `Assets/ThirdParty/CREDITS.md` | M |

---

## 4. Phụ lục

### 4.1 Phụ lục A – Ma trận truy vết (R-xx → FR/NFR → Spec → chủ)

| Mã R | Yêu cầu (tóm tắt) | FR/NFR | Spec | Chủ |
|---|---|---|---|---|
| R01 | Gacha x1/x10, tỉ lệ, pity 25 | FR-MTA-01 | S8 | B |
| R02 | Ghép 3 bản thừa cùng độ hiếm | FR-MTA-03 | S8 | B |
| R03 | Deck 5 tướng; người mới 5 tướng + 1.000 Ngọc | FR-MTA-04 | S8 | B |
| R04 | Đặt tướng, kiểm tra hợp lệ, quân số 15, giới hạn theo hiếm | FR-PLC-01, FR-PLC-02, FR-PLC-03 | S4 | B |
| R05 | Nâng cấp 3 cấp, bán hoàn 50% | FR-PLC-04, FR-PLC-05 | S4 | B |
| R06 | Một chỗ tính sát thương; kháng kẹp; không chí mạng | FR-CBT-01 | S1 | A |
| R07 | 3 hệ: Bỏng, Làm chậm, Sét nảy | FR-CBT-02 | S1 | A |
| R08 | Đòn đánh/bị động 9 tướng; cộng buff trần +60% | FR-HRO-01, FR-HRO-02, FR-HRO-04, FR-HRO-07 | S6 | A |
| R09 | Ultimate Long Vương: Nộ +5, kích hoạt tay | FR-HRO-05, FR-HRO-06 | S6 | A |
| R10 | 4 loại quái + boss; Bay đường riêng; boss gọi con | FR-ENM-01 | S2 | A |
| R11 | 15 wave; máu theo công thức; boss wave 5/10/15 | FR-ENM-02, FR-ENM-05 | S2 | A |
| R12 | 3 màn + Vô tận | FR-GAM-08 | S2, S5 | A, B |
| R13 | Thành 20 máu; thắng/thua; sao | FR-GAM-03, FR-GAM-04, FR-GAM-05 | S5 | B |
| R14 | Vàng trong trận | FR-PLC-07 | S4, S5 | B |
| R15 | Ngọc ngoài trận | FR-MTA-05 | S5, S8 | B |
| R16 | Triệu Hồi Sư WASD, camera thứ ba | FR-SUM-01, FR-SUM-02 | S3 | C |
| R17 | Hào quang Chỉ huy | FR-ABL-01 | S7 | A |
| R18 | Thiên thạch | FR-ABL-02 | S7 | A |
| R19 | Phím tắt; Esc ưu tiên; chuột phải giữ/bấm | FR-GAM-09, FR-SUM-02, FR-SUM-03 | S3 | C |
| R20 | Tốc độ ×1/×2; tạm dừng; Alt-Tab | FR-GAM-06 | S5 | B |
| R21 | HUD trận + bảng tướng + cảnh báo | FR-UI-02, FR-UI-03, FR-UI-05 | S10 | B, C |
| R22 | Màn hình ngoài trận | FR-UI-04 | S10 | B, C |
| R23 | Hướng dẫn màn 1 (4 bước) | FR-TUT-01 | S10 | B |
| R24 | Bảng Debug F1 (Editor/Development) | FR-DBG-01 | S9, S10 | B |
| R25 | Lưu tiến trình an toàn (tmp→Replace/Move→bak) | FR-SAV-01, FR-SAV-02, FR-SAV-03 | S9 | A |
| R26 | Âm thanh, rung camera, số sát thương | FR-AUD-01…05, FR-UI-05 | S11 | C |
| R27 | Hiệu năng, GC ≈ 0 B/frame, 100 quái | NFR-PERF-01…07 | S1–S11 | C (đo), mọi người |
| R28 | Dữ liệu cân bằng từ CSV, không hardcode | FR-DAT-01…04 | S0 | B (công cụ), A (số liệu) |
| R29 | Đổi ngoại hình tướng không sửa code | FR-ART-01, FR-ART-02 | S0b | C |
| R30 | Build Release IL2CPP + Development | NFR-BLD-01 | — | C |
| R31 | Chữ tiếng Việt đủ dấu | NFR-LOC-01, UI-INT-05 | S10 | C |

### 4.2 Phụ lục B – Thuật ngữ bổ sung

| Thuật ngữ | Nghĩa |
|---|---|
| `hpGrowth` | Hệ số tăng máu mỗi wave, mặc định 1.12 |
| `hpMultiplier` | Hệ số máu theo màn: màn 1 = 1.0, màn 2 = 1.15, màn 3 = 1.3 |
| `k_kỹ năng` | Hệ số sát thương của đòn/kỹ năng (đòn thường = 1) |
| `D_thực` | Sát thương thực theo độ hiếm và cấp (GDD mục 12) |
| `k_màn` | Hệ số màn (dùng trong công thức Thiên thạch) |
| `ownedEver` | Danh sách tướng đã từng sở hữu (để thưởng +50 Ngọc lần đầu) |
| `T-xx` | Mã việc trong plan 22 ngày của A (`Docs/Tson/plan.md`) |

### 4.3 Phụ lục C – Số liệu thiết kế chính (để đối chiếu, không hardcode)

| Hạng mục | Giá trị |
|---|---|
| Độ hiếm (tỉ lệ / hệ số / giá đặt / tối đa mỗi tướng) | Common 65% ×1 100 6 · Rare 25% ×1.6 200 5 · Epic 8.5% ×2.5 350 4 · Legendary 1.5% ×4 600 2 |
| Quân số tối đa / bán kính chiếm chỗ | 15 tướng / 0.8 m |
| Nâng cấp | Cấp 2: 75% giá, +40% ST, +10% tốc đánh, +10% tầm · Cấp 3: 150%, +100%, +20%, +15% (làm tròn xuống) |
| Bán | hoàn `floor(goldSpent × 0.5)` |
| Vàng | bắt đầu 300; quái thường +5, đặc biệt +10, boss +150; cuối wave 50 + 10×n; bỏ qua +50 |
| Thành | 20 máu; thường −1, đặc biệt −2, boss −10; sao ≥15/≥8/còn sống |
| Màn | 15 wave; chuẩn bị 15 s; nghỉ 8 s; boss wave 5/10/15; đường chính ~90 m |
| Quái | Thường 40 HP 2.5 m/s · Nhanh ×0.5 HP ×1.8 tốc · Giáp ×1.5 HP, giáp 50% · Bay ×0.8 HP đường riêng · Boss ×15 HP, giáp/kháng 20%, 3 Nhanh/12 s |
| Hệ | Lửa Bỏng 20%/s 3 s · Băng chậm 30% 2 s (≤60%, boss 50%) · Lôi nảy 2 quái trong 3 m, −30%/lần |
| Hào quang Chỉ huy | 6 m, +25% tốc đánh, 5 lần/s |
| Thiên thạch | tầm 20 m, nổ 4 m, rơi 0.8 s, hồi 45 s, True = 3 × HP Thường wave hiện tại |
| Ultimate (Legendary) | +5 Nộ mỗi đòn, 100 = đầy, kích hoạt tay |
| Gacha | x1 = 100 Ngọc, x10 = 900 (≥ 1 Epic+); pity 25; ghép 3 → 1 bậc kế |
| Người chơi mới | 1.000 Ngọc + 5 tướng khởi đầu (đã trong `ownedEver`) |
| Ngọc thưởng | lần đầu qua màn +300; sao mới +50; Vô tận +10/5 wave; tướng mới +50 |

### 4.4 Lịch sử phiên bản

| Phiên bản | Ngày | Thay đổi |
|---|---|---|
| 1.0 | 16/10/2026 | Bản đầu: tách FR/NFR từ GDD v1.3 + đặc tả kỹ thuật v1.3; gắn mã FR/NFR, ma trận truy vết R01–R31 |

> **Ghi chú dự thảo:** bản này giữ để so sánh với `Docs/root/Dac ta ky thuat.md`. Nếu được chọn làm nguồn chính, cần (1) trỏ Specs về mã FR tương ứng, (2) đồng bộ cách đánh mã việc với `TRACEABILITY.md`, (3) cập nhật `CLAUDE.md` mục 1.
