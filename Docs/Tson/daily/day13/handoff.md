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
- Sáu EditMode tests của A-09 được thực thi trong suite aggregate T-16 sau merge `main`: 717/717 EditMode pass, 0 fail/skip (job `b64e5b6d634f417f842260fc9959be1a`). Không có lượt riêng cho A-09.
- `TRACEABILITY.md` chưa đổi vì là file dùng chung cần duyệt; chỉ tick A-09 sau khi đủ điều kiện hoàn thành và có phê duyệt phù hợp.

## 3. Phụ thuộc PR

- Nhánh `feature/S06_A09` được tạo theo ngoại lệ TVA đã duyệt, từ head `decda92` của A-08.
- **Trạng thái 30/10/2026:** A-08 PR #38 đã merge `main` (`623c820`); PR #39 đã merge vào `feature/S06_A08` (`55feec2`), sau đó PR #46 đưa A-08 + A-09 vào `main` (`15b8bf2`). Các PR A-10…A-13 cũng đã merge `main`.
- A-09 vẫn phụ thuộc API/source-type plumbing của A-08 nên không tách riêng vào `main`; nội dung đi kèm A-08 trong #46.
- Nếu A-08 bị thay đổi/review yêu cầu chỉnh sửa, cần xác nhận lại API; thay đổi mới không thuộc các PR đã merge này.

## 4. Kiểm thử và bằng chứng T-16

- EditMode `HeroStatsTests`: sáu test mới cùng các test hiện có về rarity và level multiplier.
- Regression S6: xác nhận buff từ hai Support cùng loại chỉ lấy contribution mạnh nhất; Support + Hào quang đạt +55%; tổng AttackSpeed tối đa +60%; gỡ nguồn cập nhật đúng; Damage không bị trần.
- T-16 aggregate sau merge `main`: EditMode **717/717 pass**, PlayMode **154 pass / 157 total, 0 fail, 3 skip** (ba test GC Alloc không hỗ trợ trên WindowsEditor). Job `b64e5b6d634f417f842260fc9959be1a` / `d6a119afa40a46c48588c34e6e9354ce`.
- Đây là bằng chứng cho test code A-09 có mặt trong suite tổng hợp, không phải lượt chạy riêng hoặc kiểm tra tay buff stacking. Chưa playtest/chỉnh tay.

## 5. Nhánh và PR

- Commit `5c2e634` (+ `2ac03d2`) đã push trên `feature/S06_A09`.
- PR #39: buff stacking/cap, base `feature/S06_A08` — **đã merge** (`55feec2`, 30/10/2026).
- PR #46 (base `main`, head `feature/S06_A08`) đã merge (`15b8bf2`); A-08 + A-09 đã có trên `main`.
