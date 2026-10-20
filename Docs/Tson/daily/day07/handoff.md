# Bàn giao – Ngày 7 – 16/10/2026

> Nhánh (đã tách theo `feature/<Sxx>_<mã>`): `feature/S09_A14` (S9 + tài liệu), `feature/S02_A05` (A-05), `feature/S02_A06` (A-06, xếp chồng trên A-05), `docs/agent-tools`. Nhánh cũ `feature_a05_a06_s9` đã merge `main`, giữ backup.

## 1. Việc bị chặn (A chờ người khác)

| Việc A | Mã | Chờ ai | Vì sao | Dùng stub gì để tiếp | Ngày cần |
|---|---|---|---|---|---|
| Playtest trận hoàn chỉnh | A-04…A-13 | **B** (S5 `GameManager`, S4 đặt tướng) | S5/S4 chưa có | Test Sandbox `A_Enemies`, `A_Combat` | 20 (T-16) |
| Tạo tài nguyên Unity (các mục 2) | A-05, A-06 | **người dùng** (mở Unity) | Claude không mở được Editor, thư mục `Library` trống | Xem mục 3 | — |
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

### A-05 / A-06 → **người dùng** (đây là "Hướng dẫn lắp trong Unity")

1. **Tạo asset `BossSummonSettings`:**
   `Assets > Create > Fate Bastion > Settings > Boss Summon` → lưu thành `Assets/_Project/Data/Settings/BossSummonSettings.asset`.
2. **Cấu hình asset:** `Summon Enemy` = `enemy_nhanh`, `Count` = `3`, `Interval Seconds` = `12` (mặc định đã đúng luật GDD).
3. **Gắn vào `EnemyManager`** (trong prefab map `Enemy_Base` hoặc GameObject trong scene `Game`): kéo `BossSummonSettings` vào ô **Boss Summon Settings**. (Cách khác lúc runtime: `_enemyManager.SetBossSummonSettings(asset)`.)
4. **Quái Bay:** `enemy_bay.asset` đã `flying = 1`. Đảm bảo **prefab quái Bay** có gắn `EnemyController` và `EnemyData.prefab` được chỉ tới prefab đó (C làm prefab, A gắn `EnemyData.prefab`). Nếu muốn, kéo `LevelPaths` và đặt `Flying Path Index = 1` cho đường bay.
5. **A-06:** không cần thao tác – `level_1/2/3` đã có `hpMultiplier` 1.0/1.15/1.3; `EndlessSettings.asset` đã loop 6–15. `WaveManager.Init(level_3, endlessSettings)` là đủ để chơi Vô tận.
6. **Tạo các file `.meta` mới bằng cách mở Unity** rồi **commit cả `.meta`** (Unity sinh GUID).

### `IStarterSaveFactory` là STUB
`Meta/Save/IStarterSaveFactory.cs` chỉ khai báo giao diện. Khi **S8/B-11** có `StarterSettings`, viết class implements `IStarterSaveFactory` (đọc 5 tướng khởi đầu + 1000 Ngọc + deck mặc định) và truyền vào constructor `SaveService`. Trong lúc chờ, `starterFactory = null` → `SaveData` rỗng (gems 0) – **chưa đủ 5 tướng/ngọc để vào trận**, đúng phạm vi A không tự nối S8.

## 3. Việc cần người dùng (chủ repo) làm

1. **Compile + chạy test trong Unity.** Unity Editor đang mở giữ project (không chạy `-batchmode` cùng project được) → chạy bằng Test Runner trong Editor. Khi đã đóng Editor thì dùng lệnh (Editor theo `ProjectVersion.txt` là `D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe`):
   ```
   "D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
   "D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile -
   ```
2. **Xác nhận Newtonsoft** của `FateBastion.Meta`: asmdef có `overrideReferences:false` → DLL của `com.unity.nuget.newtonsoft-json` được auto-referenced. Nếu **không** compile được, sửa `FateBastion.Meta.asmdef`: `"overrideReferences": true` + `"precompiledReferences": ["Newtonsoft.Json.dll"]` – **asmdef là vùng dùng chung, cần PR 3 người duyệt**. *Quyết định này đã ghi lại để xem xét kỹ hơn.*
3. **Push + mở PR** cho 4 nhánh (`docs/agent-tools`, `feature/S02_A05`, `feature/S02_A06`, `feature/S09_A14`) khi test xanh. A-05 và A-06 chạm `TRACEABILITY.md`/vùng dùng chung → cần PR 3 người.
4. **Bảng Debug F1 (B-16)** sẽ gọi `SaveService.Delete()` / `Save()` – A đã để sẵn API.

## 4. Rủi ro mới trong ngày
| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| NewtonSoft có thể chưa được reference trong `FateBastion.Meta` | Trung bình | Compile thử; sửa asmdef (PR 3 người) |
| Chưa mở Unity nên chưa biết code có compile/test xanh | Cao | Chạy batchmode ở mục 3.1 trước khi merge |
| `IStarterSaveFactory` là stub → save mới chưa có tướng/ngọc | Trung bình | Gỡ khi B-11 (StarterSettings) xong |
| Boss chưa gọi quái cho tới khi tạo `BossSummonSettings` asset | Thấp | Hướng dẫn mục 2 |
