# Hiện trạng Ngày 8 – 17/10/2026 (cuối ngày)

> Nhánh: `feature/S09_A14`. Tài liệu Ngày 7 trên 4 nhánh vẫn giống nhau; Ngày 8 chỉ nằm ở nhánh này.

## Đã xong

- **T-04a (☑) – "Cách dùng":** bảng chữ ký public + 2 ví dụ code (`SaveBootstrap`, `StarterSaveFactory`) viết sẵn trong `daily/day08/handoff.md` mục 2.1, khớp `TEAM_ASSIGNMENT.md` 4.1 #2.
- **T-04b (☑) – "Hướng dẫn lắp trong Unity":** mục 2.2 — không cần prefab/component/SO/layer, tạo 1 instance ở Boot/Lobby, đường dẫn `persistentDataPath`, asmdef/Newtonsoft, thời điểm gọi `Sanitize`, nút debug B-16, cách mở Sandbox.
- **T-04c (◐) – Sandbox S9:**
  - `Assets/_Project/Scripts/Meta/Sandbox/SaveSandboxDemo.cs` (+ `.meta` GUID `9145a2bcbd024df4b695ea7a3e5ea26c`), namespace `FateBastion.Meta.Sandbox`, bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD`; OnGUI: `Load`, `Save`, `+100 gems`, `Sanitize`, `Delete`, `Corrupt save.json`; ghi vào `persistentDataPath/SandboxSave`.
  - `Assets/_Project/Scenes/Sandbox/A_Save.unity` (+ `.meta`): Camera + Directional Light + `Save Sandbox` (gắn `SaveSandboxDemo`).
  - **Chưa mở trong Unity** — file soạn tay, cần Play thử để xác nhận.
- Cập nhật `Docs/Tson/{plan,status,handoff}.md` cho T-04 (trạng thái bàn giao + checklist 4.1).

## Test

- **Không có lượt chạy mới trong ngày:** cầu nối agent ↔ Unity Editor rớt khi chuyển nhánh/domain reload, nên chưa chạy lại EditMode/PlayMode sau tích hợp `refactor/enemy-asset-names`.
- Số liệu gần nhất (đo trước tích hợp, ghi ở `daily/day07/status.md`):
  - `feature/S09_A14`: **EditMode 286/286 pass**, trong đó **13 EM của S9** (`SaveServiceTests`, `SaveSanitizerTests`).
  - `feature/S02_A06`: EditMode 297/298 (1 test do tên asset cũ, **đã sửa**), PlayMode 50/50.
- Sau khi sửa, dự kiến: S9 286/286 EM, S2 298/298 EM — **chưa xác nhận bằng lượt chạy thật**.

## Chưa xong / dời sang bước sau

1. **T-04e:** chạy lại EditMode + PlayMode sau tích hợp rename (2 nhánh).
2. **T-04c:** mở `A_Save.unity` trong Editor, Play, xác nhận scene dùng được; sửa YAML/`.meta` nếu Unity báo lỗi.
3. **T-04d:** B xác nhận "Đã thử, dùng được" (4.1 #5) — hiện là điều kiện duy nhất còn thiếu ngoài PR.
4. **Mở PR** `feature/S09_A14`; dán mục 2.1 + 2.2 của `handoff.md` vào mô tả.
5. **Tick `TRACEABILITY.md`** A-14/A-15 (vùng khóa, PR 3 người).

## Điểm lệch / ghi chú

- `SaveSandboxDemo` là script demo, không phải API bàn giao; nằm trong assembly `FateBastion.Meta` nhưng bị lọc bởi `#if UNITY_EDITOR || DEVELOPMENT_BUILD` nên không vào bản release.
- `IStarterSaveFactory` vẫn là stub; Sandbox dùng `DemoStarterSaveFactory` (1000 Ngọc + 5 tướng) để minh hoạ, **không** phải giá trị khởi đầu chính thức (B-11 quyết định).
- Chưa có consumer nào gọi `SaveService` ngoài test/Sandbox → API mới chỉ được xác nhận qua test, chưa qua luồng S8 thật.
