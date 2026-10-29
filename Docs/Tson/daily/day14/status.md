# Hiện trạng – Slot 14 – T-10 / A-10

## Đã triển khai ở mức code

- Thêm `RageMeter` thuần với tích lũy, giới hạn, reset và chỉ tiêu thụ khi đầy.
- Thêm `HeroRageSettings` để cấu hình Nộ mỗi đòn và ngưỡng Ultimate.
- `HeroController` có `OnRageChanged(float current, float max)`, giá trị Nộ, reset khi pooled, và `TryActivateUltimate()` dùng `SkillRunner.TryCastAuto`.
- `AttackModule` cộng Nộ một lần khi `SkillRunner` chấp nhận đòn; chỉ Legendary tích Nộ.
- Viết EditMode tests cho RageMeter và PlayMode cases cho tăng Nộ/Ultimate; chưa chạy tests theo kế hoạch.

## Hạng mục dời sang T-16

- Theo lựa chọn của TVA, dời toàn bộ thao tác Unity và compile sang T-16. Lần refresh trước timeout sau 60 giây; Console lúc đó báo CS0246 trước khi Editor import xong các script mới, nên chưa có kết quả compile cuối.
- T-16 phải tạo `HeroRageSettings.asset` (5/100), gán vào `Hero_Base.prefab` qua Editor, force import/compile và ghi kết quả. Không sửa YAML prefab/asset bằng tay.
- Chưa xác nhận compile; EditMode/PlayMode tests hoãn theo kế hoạch.

## Người nhận

- B dùng event #5 để cập nhật HUD. UI cần thông báo nhỏ nếu `TryActivateUltimate()` trả `false`; logic input/nút vẫn do B gọi, A không đọc phím trực tiếp.

## Nhánh

- `feature/S06_A10` được tạo từ `origin/feature/S06_A08` tại `decda92`; phụ thuộc PR #38. Nhánh A-09 không được đưa vào diff.
