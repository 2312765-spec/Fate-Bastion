# Kế hoạch – Slot 14 – T-10 / A-10

## Mục tiêu

Thêm Nộ cho tướng Legendary: mỗi đòn tấn công hợp lệ cộng 5, tối đa 100; Ultimate kích hoạt thủ công, chỉ tiêu Nộ khi kỹ năng thực sự nhận được lệnh thi triển.

## Phạm vi dự kiến

- Thêm `RageMeter` thuần để cộng, giới hạn, reset và tiêu Nộ.
- Mở rộng `HeroStatsSettings` với giá trị cấu hình cho Nộ mỗi đòn và ngưỡng đầy; lưu asset qua Unity Editor.
- Thêm vào `HeroController` event instance `OnRageChanged(float current, float max)` và API `TryActivateUltimate()`.
- Tăng Nộ đúng một lần mỗi đòn hợp lệ từ `AttackModule`, không theo số mục tiêu trúng; chỉ tướng Legendary tích Nộ.
- Dùng `SkillRunner.TryCastAuto` để chọn điểm đông quái trong tầm; nếu thiếu Legendary, chưa đủ Nộ, không có mục tiêu hoặc không cast được thì giữ nguyên Nộ.
- Viết EditMode tests cho `RageMeter` và PlayMode cases cho API/event/Ultimate; không chạy test trước T-16.
- Ghi handoff cho B về event và trạng thái “không thể kích hoạt” để UI hiển thị thông báo nhỏ theo S6.

## Phụ thuộc / nhánh

- T-10 cần `HeroController` và `SkillRunner` từ A-08 PR #38. Để không đưa A-09 vào diff T-10, nhánh riêng sẽ dựa trên `feature/S06_A08`; draft PR T-10 sẽ nhắm vào nhánh đó và phụ thuộc PR #38.
- A-09 PR #39 độc lập với Nộ/Ultimate; không gộp vào nhánh này.
- Không sửa `Core/`, Specs, `TEAM_ASSIGNMENT.md`, prefab/scene của người khác hoặc `TRACEABILITY.md`.

## Điểm kiểm chứng

- Mỗi đòn hợp lệ cộng một lần dù đòn vùng trúng nhiều quái.
- Nộ chỉ đổi khi giá trị đổi, bị chặn ở ngưỡng tối đa và được reset khi dùng thành công / khi pooled hero reset.
- Hero không Legendary hoặc chưa đầy Nộ không được dùng Ultimate; không có quái trong tầm thì trả `false` và giữ Nộ.
- Unity script validation/compile; tests được ghi vào queue để chạy sau T-16.
