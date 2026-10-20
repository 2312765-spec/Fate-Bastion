# Hiện trạng Ngày 7 – 16/10/2026 (cuối ngày)

> **Nhánh (tách theo quy ước `feature/<Sxx>_<mã>`, bỏ nhánh cha/con):** `feature/S09_A14` (S9 + tài liệu ngày 7), `feature/S02_A05` (A-05), `feature/S02_A06` (A-06, xếp chồng trên A-05), `docs/agent-tools` (công cụ ngoài gameplay). Nhánh cũ `feature_a05_a06_s9` đã merge `main` (PR #14) làm backup, không PR.

## Đã xong
- **T-01 (☑):** tạo khu `Docs/Tson`:
  - `README.md` (luật làm việc: folder theo ngày, 3 file/ngày, nhánh `feature/<Sxx>_<mã>`, không commit thẳng `main`, 3 mức ưu tiên, rà soát bằng skill).
  - `plan.md` (22 ngày, mã T-01…T-18, cột ánh xạ TRACEABILITY A-xx; bảng ngày 1–6 đã xong).
  - `status.md`, `handoff.md` (tổng thể).
  - `daily/day07/{plan,status,handoff}.md`.
  - `Docs/SRS.md` – SRS toàn game v1.0 (16/10/2026, IEEE 830, FR/NFR + ma trận R01–R31).
- Cập nhật `CLAUDE.md` + `README.md`: đường dẫn tài liệu cũ → `Docs/root/`, thêm dòng SRS/Tson, thống nhất quy ước nhánh `feature/<Sxx>_<mã>` theo `TRACEABILITY.md` (mục 12 `CLAUDE.md`).

### T-01 – Khu tài liệu + SRS (từ `docs_tson_setup`, commit `f3ab8fa`)
- `Docs/Tson/**`, `Docs/SRS.md`, cập nhật `CLAUDE.md` + `README.md`.

### T-02 / T-03 – S9 Lưu trữ (A-14 + A-15), P1
File mới trong `Assets/_Project/Scripts/Meta/Save/`:
- `SaveData.cs` – `version/gems/pityCounter/collection/deck/ownedEver/levelProgress/endlessBest/tutorialDone/settings`, đúng schema của `Docs/Specs/S9.md`.
- `SaveSettings.cs` – master/music/sfx/cameraShake/skipConfirm.
- `IStarterSaveFactory.cs` – **STUB**, thay bằng `StarterSettings` (S8, B) khi B-11 xong.
- `SaveSanitizer.cs` – logic thuần: bỏ `heroId` không còn, sửa deck 5 tướng khác nhau, trả về cảnh báo.
- `SaveService.cs` – đọc/ghi bằng Newtonsoft, ghi an toàn `save.tmp` → `File.Replace`(json) hoặc `File.Move`(lần đầu), fallback `save.json` → `save.bak` → tạo mới.

Test: `Assets/_Project/Tests/EditMode/S9/{SaveServiceTests,SaveSanitizerTests}.cs` – **13 EM** (lưu/đọc giống hệt, lần lưu đầu, file hỏng→bak, hai file hỏng→mới, file rỗng, Delete, sanitizer).

### T-14 – A-05 Quái Bay + boss gọi con, P2
- `Enemies/BossSummonSettings.cs` (SO, `Data/Settings/BossSummonSettings` – A gắn tay trong Inspector).
- `Enemies/BossSummonAbility.cs` (class thuần, timer scaled time).
- `EnemyController`: `Initialize(..., bossSummonSettings)`, `Spawn(..., pathIndex, ...)`, `Tick(deltaTime, List<SpawnRequest>)` sinh quái gọi (đường/wave của boss, vị trí xuất phát = quái gọi); dừng khi boss tới Thành hoặc chết.
- `EnemyManager`: field `_bossSummonSettings` + `SetBossSummonSettings`, buffer `_summonBuffer` sinh sau vòng di chuyển để không sửa `_active` khi đang duyệt; `ClearAll` cũng dọn buffer.
- Quái Bay: `enemy_bay.asset` đã `flying=1`; Melee (`CanHitFlying=false`) bỏ qua – có test.

Test: `Tests/EditMode/S2/BossSummonAbilityTests.cs` (**8 EM**) · `Tests/PlayMode/S2/BossSummonPlayModeTests.cs` (**4 PM**: boss gọi theo chu kỳ, wave mở thêm khi còn quái gọi, boss chết thì dừng, Bay né đòn cận chiến).

### T-15 – A-06 Máu theo màn + Vô tận, P2
- `WaveManager.ResolveWave`: sau wave cuối, lặp `loopFromWave..loopToWave` (màn 3 = 6…15); **wave thật** vẫn được dùng cho `WaveSchedule`/`BeginWave` nên `hpGrowth` tiếp tục.
- `EndlessSettings.Configure(...)` – helper cho test/tools (giá trị thật vẫn set trong Inspector).
- Dữ liệu đã đúng từ trước, không sửa asset: `level_1=1.0`, `level_2=1.15`, `level_3=1.3`, loop 6–15.

Test: `Tests/EditMode/S2/LevelBalanceDataTests.cs` (**4 EM** qua `AssetDatabase`, kiểm tra 3 màn × 15 wave + hpMultiplier + công thức wave 15) · thêm `Endless_WavesBeyondLast_RepeatLoopBlockWithRealWaveNumbers` vào `WaveManagerPlayModeTests.cs` (**1 PM**).

## Tổng test mới: 30 (25 EM + 5 PM)

## Chưa xong / cần người dùng
1. **Compile + chạy test trong Unity.** Unity Editor đang mở giữ project (không chạy `-batchmode` cùng project được) → chạy bằng Test Runner trong Editor. Lệnh batchmode (khi đã đóng Editor):
   ```
   "<UnityEditorPath>/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
   ```
   Editor: `D:\Unity\Editor\6000.3.25f1\Editor\Unity.exe` (theo `ProjectVersion.txt`).
2. **Xác nhận Newtonsoft** của `FateBastion.Meta` compile được (xem handoff).
3. **A-07 KHÔNG làm hôm nay** – để nguyên T-05/T-06 (day 9/10), đúng quyết định.
4. T-04 (bàn giao API S9 cho B) – ◐, chờ B xem PR.

## Điểm lệch / gỡ stub còn lại
- `BossSummonSettings` asset chưa được tạo trong Inspector → boss **chưa** gọi quái cho tới khi người dùng tạo asset + kéo vào `EnemyManager`.
- Prefab quái Bay/boss (A-05/A-06 phần prefab) vẫn chờ C (`Enemy_Base` + Variant) – giữ trong handoff tổng thể.
- `TRACEABILITY.md` chưa tick A-05/A-06/A-14/A-15 (vùng dùng chung, cần PR 3 người).
