# Bàn giao – Ngày 7 – 16/10/2026

> Nhánh (đã tách theo `feature/<Sxx>_<mã>`): `feature/S09_A14` (S9 + tài liệu), `feature/S02_A05` (A-05), `feature/S02_A06` (A-06, xếp chồng trên A-05), `docs/agent-tools`. Nhánh cũ `feature_a05_a06_s9` đã merge `main`, giữ backup.

## 1. Việc bị chặn (A chờ người khác)

| Việc A | Mã | Chờ ai | Vì sao | Dùng stub gì để tiếp | Ngày cần |
|---|---|---|---|---|---|
| Playtest trận hoàn chỉnh | A-04…A-13 | **B** (S5 `GameManager`, S4 đặt tướng) | S5/S4 chưa có | Test Sandbox `A_Enemies`, `A_Combat` | 20 (T-16) |
| Prefab quái Bay/boss | A-05, A-06 | **C** | Prefab do C làm (`Enemy_Base` + Variant) | Asset `BossSummonSettings` + gắn `EnemyManager` **đã xong**; chỉ còn prefab | 18–19 |
| Tick A-05/A-06/A-14/A-15 trong `TRACEABILITY.md` | — | **cả nhóm** (PR 3 người) | Quy tắc vùng khóa | — | 8–19 |

## 2. A giao cho người khác (API theo TEAM_ASSIGNMENT mục 3.1)

### S9 Lưu trữ → **B** (S8)
| Nội dung | Cách dùng |
|---|---|
| `new SaveService(directory, IStarterSaveFactory)` | `directory` = `Application.persistentDataPath`; `starterFactory` truyền `StarterSettings` của S8 (hoặc null tạm) |
| `service.Load()` → `SaveLoadResult` | `.Data` là `SaveData` đã đọc; `.Kind` ∈ `Loaded` / `RecoveredFromBackup` / `CreatedNew`; `.Error` là lý do fallback (null nếu clean). **Không bao giờ throw** cho file hỏng |
| `service.Save()` / `service.Save(data)` | Ghi an toàn: `save.tmp` → `File.Replace`(json,bak) nếu json đã có, `File.Move` nếu lần đầu. **Không bao giờ ghi đè trực tiếp save.json** |
| `service.Delete()` | Xoá `save.json` + `save.bak` + `save.tmp` (Bảng Debug F1 – B-16 gọi) |
| `service.Exists`, `SaveFilePath`, `BackupFilePath` | Cho UI/Bảng Debug |
| `SaveSanitizer.Sanitize(data, validHeroIds, starterDeck)` | Gọi sau `Load()` với danh sách `HeroData.id` hợp lệ; trả `List<string>` cảnh báo (vd "Bỏ qua tướng không còn tồn tại…") để báo người chơi. Nếu không truyền roster thì không làm gì |

**Thời điểm lưu (S9):** sau quay/ghép/đổi deck (S8), khi màn hình kết quả hiện, khi đổi cài đặt, và trong `OnApplicationQuit` – **B là người gọi**; A chỉ cung cấp `SaveService`.

### A-05 / A-06 → đã lắp sẵn, còn 1 việc cho **C**

Đã xong (không cần ai làm lại):
1. **Asset `BossSummonSettings`:** có ở `Assets/_Project/Data/Settings/BossSummonSettings.asset` — `Summon Enemy` = `enemy_nhanh`, `Count` = `3`, `Interval Seconds` = `12`.
2. **Đã gắn vào `EnemyManager`** trong `A_Enemies.unity` qua `_bossSummonSettings` (cách khác lúc runtime: `_enemyManager.SetBossSummonSettings(asset)`).
3. **Quái Bay:** `enemy_bay.asset` có `flying = 1`; Melee (`CanHitFlying=false`) bỏ qua có test. Đường bay chọn bằng `pathIndex = 1` của `LevelPaths`.

Còn lại:
4. **C — prefab quái Bay/boss:** prefab cần gắn `EnemyController`, rồi A trỏ `EnemyData.prefab` của `enemy_bay` / `enemy_boss` vào prefab đó.
5. `.meta` cho file mới: Unity tự sinh khi mở Editor — nhớ commit kèm.

### `IStarterSaveFactory` là STUB
`Meta/Save/IStarterSaveFactory.cs` chỉ khai báo giao diện. Khi **S8/B-11** có `StarterSettings`, viết class implements `IStarterSaveFactory` (đọc 5 tướng khởi đầu + 1000 Ngọc + deck mặc định) và truyền vào constructor `SaveService`. Trong lúc chờ, `starterFactory = null` → `SaveData` rỗng (gems 0) – **chưa đủ 5 tướng/ngọc để vào trận**, đúng phạm vi A không tự nối S8.

## 3. Việc cần người dùng (chủ repo) làm

1. **Chạy lại test S2** sau khi hoàn tất việc đổi tên asset đang dở (`enemy_thuong` → `enemy_normal`, …) — hiện chỉ còn 1 test đỏ vì lý do này, không phải lỗi code ngày 7. Editor: `D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe` (theo `ProjectVersion.txt`).
2. **Newtonsoft: đã ổn** — S9 compile và 286 EM pass; **không cần sửa `FateBastion.Meta.asmdef`**.
3. **Mở PR** cho 4 nhánh (`docs/agent-tools`, `feature/S02_A05`, `feature/S02_A06`, `feature/S09_A14`). A-05/A-06 chạm `TRACEABILITY.md`/vùng dùng chung → cần PR 3 người.
4. **Bảng Debug F1 (B-16)** sẽ gọi `SaveService.Delete()` / `Save()` – A đã để sẵn API.

## 4. Rủi ro mới trong ngày
| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| ~~NewtonSoft chưa được reference trong `FateBastion.Meta`~~ | — | **Đã hết:** S9 compile + 286 EM pass |
| ~~Chưa mở Unity nên chưa biết code có compile/test xanh~~ | — | **Đã kiểm:** compile 0 lỗi; S9 286/286 EM; S2 297/298 EM + 50/50 PM |
| Có thay đổi đổi tên asset chưa commit (`enemy_thuong` → `enemy_normal`, …) làm 1 test S2 đỏ | Trung bình | Hoàn tất đổi tên rồi cập nhật đường dẫn trong `LevelBalanceDataTests`; không trộn vào PR ngày 7 |
| `IStarterSaveFactory` là stub → save mới chưa có tướng/ngọc | Trung bình | Gỡ khi B-11 (StarterSettings) xong |
| Boss cần prefab Bay (C) mới hiện hình | Thấp | Asset + code đã xong; chờ C |
