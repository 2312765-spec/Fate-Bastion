# Bàn giao – Slot 14 – T-10 / A-10

## 1. Công việc

- Thêm `RageMeter` thuần để tích lũy đến ngưỡng, chặn vượt trần, reset và chỉ tiêu thụ khi đầy.
- Thêm `HeroRageSettings` với giá trị khởi tạo 5 Nộ/đòn và 100 Nộ tối đa; dự kiến lưu asset trong `Assets/_Project/Data/Settings/`.
- Thêm event instance `HeroController.OnRageChanged(float current, float max)` và API `TryActivateUltimate()`. Kích hoạt chỉ thành công với Legendary, meter đầy, Ultimate được cấu hình và `SkillRunner.TryCastAuto` chấp nhận cast; nếu thất bại thì giữ Nộ.
- `AttackModule` cộng Nộ đúng một lần cho đòn được `SkillRunner` chấp nhận, không phụ thuộc số quái trúng.
- Đã viết EditMode và PlayMode tests; tất cả vẫn nằm trong hàng chờ sau T-16.

## 2. Bằng chứng / validation để chạy ở T-16

- Unity MCP standalone standard validation: không có diagnostic cho các script/test mới hoặc thay đổi, ngoại trừ heuristic cảnh báo string concatenation trong `HeroController.Update()` (đã có logic target refresh, không phải kết quả compiler).
- Theo lựa chọn của TVA, dời thao tác Unity/compile sang T-16. Lần refresh cưỡng bức trước timeout sau 60 giây; Console lúc đó báo CS0246 trước khi Editor import xong scripts mới. Chưa có kết quả compile cuối.
- T-16 cần tạo `HeroRageSettings.asset` (5/100), gán `_rageSettings` vào `Hero_Base.prefab` bằng Editor, refresh/compile và xác nhận Console sạch. Không sửa YAML prefab/asset bằng tay.
- Không chạy EditMode/PlayMode test, không coi test là pass.
- UI hiển thị thanh Nộ và thông báo khi từ chối Ultimate thuộc B; B dùng event #5 và hiện thông báo khi `TryActivateUltimate()` trả `false`. A không đọc input trực tiếp, không sửa UI của B.

## 3. Nhánh / phụ thuộc

- Nhánh hiện tại: `feature/S06_A10`, từ `feature/S06_A08` tại `decda92`.
- PR #38 còn OPEN/draft; A-10 cần phụ thuộc API S6 trong nhánh đó. A-09 PR #39 không được gộp vào nhánh A-10.
- Commit `5689996` đã push; draft PR #40 nhắm vào `feature/S06_A08`. PR #38 còn OPEN/draft; A-09 PR #39 không được gộp vào nhánh A-10.
- Chưa coi T-10 hoàn chỉnh hoặc merge trước khi T-16 hoàn thành compile, cấu hình asset/prefab và validation.
