# Hiện trạng Ngày 8 – 17/10/2026 (cuối ngày)

> Nhánh: `feature/S09_A14`. Tài liệu Ngày 7 trên 4 nhánh vẫn giống nhau; Ngày 8 chỉ nằm ở nhánh này.

## Đã xong

- **T-04a (☑) – "Cách dùng":** bảng chữ ký public + 2 ví dụ code (`SaveBootstrap`, `StarterSaveFactory`) viết sẵn trong `daily/day08/handoff.md` mục 2.1, khớp `TEAM_ASSIGNMENT.md` 4.1 #2.
- **T-04b (☑) – "Hướng dẫn lắp trong Unity":** mục 2.2 — không cần prefab/component/SO/layer, tạo 1 instance ở Boot/Lobby, đường dẫn `persistentDataPath`, asmdef/Newtonsoft, thời điểm gọi `Sanitize`, nút debug B-16, cách mở Sandbox.
- **T-04c (☑) – Sandbox S9:**
  - `Assets/_Project/Scripts/Meta/Sandbox/SaveSandboxDemo.cs` (+ `.meta` GUID `9145a2bcbd024df4b695ea7a3e5ea26c`), namespace `FateBastion.Meta.Sandbox`, bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD`; OnGUI: `Load`, `Save`, `+100 gems`, `Sanitize`, `Delete`, `Corrupt save.json`; ghi vào `persistentDataPath/SandboxSave`.
  - `Assets/_Project/Scenes/Sandbox/A_Save.unity` (+ `.meta`): Camera + Directional Light + `Save Sandbox` (gắn `SaveSandboxDemo`).
  - **Đã mở trong Editor:** scene load OK, hierarchy đúng 3 root (`Main Camera`, `Directional Light`, `Save Sandbox` với component `SaveSandboxDemo`); compile sạch (`last_compile_finished`, domain reload xong), Console không có error.
  - **Đã Play thử:** chạy ~4 s không exception; demo tạo thư mục `C:\Users\admin\AppData\LocalLow\DefaultCompany\My project\SandboxSave` (tức `Load()` → `CreatedNew` chạy đúng, ghi vào `persistentDataPath/SandboxSave`, không đụng save thật).
- **T-04e (☑) – chạy lại test sau tích hợp rename:** `feature/S09_A14` **299/299 EM + 45/45 PM**; `feature/S02_A06` **298/298 EM + 50/50 PM** (chi tiết ở mục Test).
- Cập nhật `Docs/Tson/{plan,status,handoff}.md` cho T-04 (trạng thái bàn giao + checklist 4.1) và thêm **luật 9 – MCP timeout** vào `README.md`.

## Test

- **EditMode `FateBastion.Tests.EditMode` trên `feature/S09_A14` – chạy lại sau tích hợp `refactor/enemy-asset-names`: 299/299 pass, 0 fail, 0 skip** (93,5 s) — gồm 13 EM của S9 (`SaveServiceTests`, `SaveSanitizerTests`). Job `114c9205c6cb4f7bb1c8026d079ce1b6` (lấy kết quả được ở lần hỏi thứ 3).
- **PlayMode `FateBastion.Tests.PlayMode` trên `feature/S09_A14`: 45/45 pass**, 0 fail, 0 skip (9,2 s) — job `d0b86b63452c4ae684406d3279ad40a9`.
- **EditMode toàn Editor (gồm cả test của package):** `1912` test, **1840 pass**, `3` fail, `69` skip (751 s).
  - **Cả 3 test fail đều thuộc package `com.besty.unity-skills`** (`UnitySkills.Tests.Core.*`: `DryRunPolicyBatchTests`, `EditorUndoRedoTests`, `SkillValidationTests`) — không có test `FateBastion.*` nào đỏ; 3 fail kia là của công cụ, không sửa trong repo.
- **EditMode + PlayMode `FateBastion.Tests` trên `feature/S02_A06` (sau tích hợp `refactor/enemy-asset-names`): EditMode 298/298 pass** (96,4 s, job `7e3ee7cea45e4cb3a9b6f4d290ed8cdb`) **+ PlayMode 50/50 pass** (10,4 s, job `f9c0653071824babab00f5e38cba3321`) — 0 fail, 0 skip.
- Số liệu cũ (đo trước tích hợp, ghi ở `daily/day07/status.md`): S9 286/286 EM, S2 297/298 EM + 50/50 PM. Số hôm nay khác vì đã gộp nhánh đổi tên + test phát sinh; PM 45 là của riêng nhánh S9 (5 PM mới của A-05/A-06 nằm trên nhánh S02).
- **Không còn lượt test nào bị treo:** cả 4 lượt EM/PM của 2 nhánh đều có kết quả thật.

## Chưa xong / dời sang bước sau

1. **T-04d:** B xác nhận "Đã thử, dùng được" (4.1 #5) — vẫn đang chờ B.
2. **Đã mở PR #23** cho `feature/S09_A14`; mục 2.1 + 2.2 của `handoff.md` nằm trong mô tả PR. PR chưa merge.
3. **Đã mở PR #21, #22 và #24** cho `docs/agent-tools`, `feature/S02_A06` và `feature/S02_A05`. PR #22 chứa lịch sử A-05; thứ tự merge đề xuất để tránh lặp diff là #21 → #24 → #22 → #23.
4. **Tick `TRACEABILITY.md`** A-14/A-15 và A-05/A-06 sau khi PR tương ứng merge; vùng dùng chung cần đủ 3 người duyệt.

## Điểm lệch / ghi chú

- `SaveSandboxDemo` là script demo, không phải API bàn giao; nằm trong assembly `FateBastion.Meta` nhưng bị lọc bởi `#if UNITY_EDITOR || DEVELOPMENT_BUILD` nên không vào bản release.
- `IStarterSaveFactory` vẫn là stub; Sandbox dùng `DemoStarterSaveFactory` (1000 Ngọc + 5 tướng) để minh hoạ, **không** phải giá trị khởi đầu chính thức (B-11 quyết định).
- Chưa có consumer nào gọi `SaveService` ngoài test/Sandbox → API mới chỉ được xác nhận qua test, chưa qua luồng S8 thật.
