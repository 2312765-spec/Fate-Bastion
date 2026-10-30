# Bàn giao – Slot 13 – T-09 / A-09

## 1. Kết quả

- Buff được aggregate theo `(sourceType, BuffType)`: trong cùng nhóm chỉ giữ giá trị mạnh nhất; nhóm khác nhau cộng dồn.
- Trần AttackSpeed được đọc từ `HeroStatsSettings.AttackSpeedBuffCap`; asset hiện có được lưu và xác nhận giá trị `0.6`.
- Damage buff không dùng trần AttackSpeed. Set/replacement và Remove đều tính lại tổng.
- Đã thêm sáu EditMode tests cho stacking, thay contribution trên cùng nguồn, Support + aura, giới hạn +60%, gỡ nguồn và Damage.

## 2. Bằng chứng và giới hạn

- Unity MCP standard script validation: 0 lỗi, 0 cảnh báo trên `HeroStats.cs`, `HeroStatsSettings.cs`, `HeroStatsTests.cs`.
- Unity refresh đã hoàn tất ở trạng thái idle; Console không có lỗi biên dịch. Các log còn lại thuộc môi trường Unity/MCP.
- Đọc lại qua Unity Editor xác nhận asset cap là `0.6`.
- Chưa chạy EditMode/PlayMode tests, chưa thực hiện Play test. Không coi các test là pass.
- `TRACEABILITY.md` chưa đổi vì là file dùng chung cần duyệt; chỉ tick A-09 sau khi đủ điều kiện hoàn thành và có phê duyệt phù hợp.

## 3. Phụ thuộc PR

- Nhánh `feature/S06_A09` được tạo theo ngoại lệ TVA đã duyệt, từ head `decda92` của A-08.
- **Trạng thái 30/10/2026:** A-08 PR #38 đã merge `main` (`623c820`); PR #39 đã merge vào `feature/S06_A08` (`55feec2`). PR #46 (base `main`) đang mở để đưa A-08 + A-09 vào `main`; **#46 phải merge trước #40** và trước các PR S7.
- A-09 vẫn phụ thuộc API/source-type plumbing của A-08 nên không tách riêng vào `main`; nội dung đi kèm A-08 trong #46.
- Nếu A-08 bị thay đổi/review yêu cầu chỉnh sửa, cần tích hợp các thay đổi cần thiết và xác nhận lại API trước khi merge #46.

## 4. Kiểm thử để chạy sau T-16

- EditMode `HeroStatsTests`: sáu test mới cùng các test hiện có về rarity và level multiplier.
- Regression S6: xác nhận buff từ hai Support cùng loại chỉ lấy contribution mạnh nhất; Support + Hào quang đạt +55%; tổng AttackSpeed tối đa +60%; gỡ nguồn cập nhật đúng; Damage không bị trần.
- Ghi job ID, thời lượng, số pass/fail/skip và log thực tế khi chạy; không khởi chạy lại cả suite nếu job bị timeout theo quy định trong `Docs/Tson/README.md`.

## 5. Nhánh và PR

- Commit `5c2e634` (+ `2ac03d2`) đã push trên `feature/S06_A09`.
- PR #39: buff stacking/cap, base `feature/S06_A08` — **đã merge** (`55feec2`, 30/10/2026).
- PR #46 (base `main`, head `feature/S06_A08`) đang mở để đưa A-08 + A-09 vào `main`; merge trước #40.
