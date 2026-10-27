# Kế hoạch Ngày 8 – 17/10/2026

> Mã việc theo `Docs/Tson/plan.md`. Ưu tiên: P1 không cần Unity · P2 cần Unity · P3 bàn giao.
> **Nhánh của ngày:** `feature/S09_A14` (T-04 dùng đúng code S9 đang nằm trên nhánh này). Tài liệu Ngày 8 **chỉ đặt trên nhánh này**; tài liệu Ngày 7 vẫn giống nhau trên cả 4 nhánh.

| Mã | Việc | Mã TRACEABILITY | Ưu tiên | Nhánh | Kiểm tra | Trạng thái |
|---|---|---|---|---|---|---|
| T-04a | Viết **"Cách dùng"** API S9 (chữ ký public + ví dụ code) để dán vào mô tả PR | A-14, A-15 | P1 | `feature/S09_A14` | Tay | ☑ |
| T-04b | Viết **"Hướng dẫn lắp trong Unity"** (file, asmdef, đường dẫn save, thứ tự khởi động, không cần prefab/layer) | A-14, A-15 | P1 | `feature/S09_A14` | Tay | ☑ |
| T-04c | Scene Sandbox `Assets/_Project/Scenes/Sandbox/A_Save.unity` + `Scripts/Meta/Sandbox/SaveSandboxDemo.cs` minh hoạ Load/Save/Sanitize/Delete | A-14, A-15 | P2 | `feature/S09_A14` | Tay | ☑ (mở Editor + Play thử OK) |
| T-04d | B xác nhận **"Đã thử, dùng được"** (TEAM_ASSIGNMENT 4.1 #5) | A-14, A-15 | P3 | — | PR | ☐ (chờ B) |
| T-04e | Chạy lại EditMode/PlayMode sau khi tích hợp `refactor/enemy-asset-names` (mang từ Ngày 7) | A-14, A-15, A-05, A-06 | P2 | `feature/S09_A14`, `feature/S02_A06` | EM/PM | ☑ (`S09_A14`: **299/299 EM + 45/45 PM**; `S02_A06`: **298/298 EM + 50/50 PM**) |

## Ghi chú đầu ngày
- **T-04 là bàn giao, không viết thêm code S9:** code đã xong Ngày 7 trên `feature/S09_A14`. Hôm nay chỉ hoàn thiện 5 điều kiện của `TEAM_ASSIGNMENT.md` mục 4.1: (1) test xanh, (2) "Cách dùng", (3) "Hướng dẫn lắp trong Unity", (4) scene `Sandbox/A_*`, (5) B xác nhận.
- **Chưa mở PR** (người dùng chưa cho phép) → "Cách dùng" và "Hướng dẫn lắp trong Unity" viết sẵn ở `daily/day08/handoff.md` để dán nguyên vào PR khi mở.
- **Không sửa `Assets/_Project/Scripts/Meta/FateBastion.Meta.asmdef`:** `overrideReferences: false` nên Newtonsoft được auto-reference; đã kiểm bằng 286 EM của nhánh.
- **Không tạo nhánh mới trong ngày:** T-04 nằm trên `feature/S09_A14`; việc chạy lại test của A-05/A-06 làm trên `feature/S02_A06` (không tạo PR riêng, chỉ là xác minh sau tích hợp).
- `A_Save.unity` và `.meta` của scene/script được soạn tay khi chưa có Unity; **bắt buộc mở scene trong Editor** để xác nhận trước khi tính là xong 4.1 #4.
