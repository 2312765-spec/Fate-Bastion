# Kế hoạch Ngày 7 – 16/10/2026

> Mã việc theo `Docs/Tson/plan.md`. Ưu tiên: P1 không cần Unity · P2 cần Unity · P3 bàn giao.

| Mã | Việc | Mã TRACEABILITY | Ưu tiên | Nhánh | Kiểm tra | Trạng thái |
|---|---|---|---|---|---|---|
| T-01 | Dựng khu `Docs/Tson` + SRS toàn game `Docs/SRS.md` | — | P1 | `docs_tson_setup` | Tay | ☑ |
| T-02 | `SaveData`, `SaveService` ghi an toàn (tmp → `File.Replace`/`Move` → bak), test lưu/đọc giống hệt + lần lưu đầu | A-14 | P1 | `feature/S09_A14` | EM | ☑ |
| T-03 | Đọc lỗi → `save.bak` → tạo mới từ `IStarterSaveFactory`; test file hỏng/hai file hỏng/rỗng | A-15 | P1 | `feature/S09_A14` | EM | ☑ |
| T-14 | Quái Bay (đường riêng) + boss gọi 3 quái Nhanh mỗi 12 s (`BossSummonSettings`, `BossSummonAbility`) | A-05 | P2 | `feature/S02_A05` | EM/PM | ☑ |
| T-15 | Máu theo `hpMultiplier`; Vô tận lặp wave `loopFromWave..loopToWave`; test dữ liệu 3 màn | A-06 | P2 | `feature/S02_A06` | EM/PM | ☑ |
| T-19 | (phát sinh) Ghi chú công cụ agent (unityMCP/unitySkills) + dependency package + ignore `.vscode` | — | P1 | `docs/agent-tools` | Tay | ☑ |
| T-04 | Bàn giao API S9 cho B (`SaveService.Load/Save`, `SaveData`, `SaveSanitizer`) | A-14, A-15 | P3 | — | PR | ◐ |

## Ghi chú đầu ngày
- Hôm nay là **Ngày 7** – ngày đầu A tự ghi nhật ký; Ngày 1–6 do C làm hộ (xem `plan.md` mục 1).
- **Kéo việc của ngày 8/18/19 lên làm hôm nay:** vì A-05/A-06 và S9 đều P1/P2, độc lập nhau, và B đang review PR `docs_tson_setup` nên A gộp làm sớm.
- **Tách nhánh theo quy ước mới của `main`** (`feature/<Sxx>_<mã>`, **bỏ nhánh cha/con**, mỗi việc PR thẳng `main` – `CLAUDE.md` mục 12 sau PR #14):
  - `feature/S02_A05` ← A-05; `feature/S02_A06` ← A-06 (**xếp chồng** lên `feature/S02_A05` để tránh đụng `EnemyTestRig.cs`; sau khi A-05 merge thì rebase lên `main`).
  - `feature/S09_A14` ← A-14 + A-15 (cùng nằm trong `SaveService`, không tách sạch được) + cập nhật tài liệu ngày 7.
  - `docs/agent-tools` ← công cụ agent (ngoài gameplay).
  - Nhánh cũ `feature_a05_a06_s9` **đã merge `main`** rồi giữ làm backup, không PR.
- **A-07 (`HeroController`/`AttackModule`) KHÔNG làm hôm nay** – để nguyên T-05/T-06 (day 9/10) theo quyết định.
- **Số liệu A-06 đã đúng từ trước:** `level_1 hpMultiplier=1`, `level_2=1.15`, `level_3=1.3`; `EndlessSettings` loop 6–15. Việc còn lại là code Vô tận + test xác minh.
- Newtonsoft: `FateBastion.Meta.asmdef` có `overrideReferences:false` nên DLL của `com.unity.nuget.newtonsoft-json` được auto-referenced – **không sửa asmdef chung**; đã compile thử: **OK** (S9 286/286 EM pass, xem `status.md`).
- **Tài liệu ngày 7 nằm trên cả 4 nhánh** (cùng nội dung) để mỗi PR tự mô tả đủ ngày 7; nhánh chứa việc gì xem cột `Nhánh` ở bảng trên.
- **A-05 đã lắp sẵn trong Unity:** `Data/Settings/BossSummonSettings.asset` (Fast, 3 con, 12 s) + `_bossSummonSettings` trong `A_Enemies.unity` – không cần tạo tay. Chỉ còn prefab Bay/boss của C.
- **Việc phát sinh ngoài phạm vi ngày 7:** đổi tên asset/prefab quái sang id tiếng Anh ở nhánh riêng `refactor/enemy-asset-names` (từ `main`), đã merge vào 3 nhánh gameplay trước khi mở PR; `LevelBalanceDataTests` đã dùng `enemy_normal.asset`. Chi tiết + danh sách file ở `status.md` mục "Việc phát sinh".
