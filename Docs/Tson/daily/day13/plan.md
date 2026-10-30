# Kế hoạch – Slot 13 – T-09 / A-09

## Mục tiêu

Triển khai cộng dồn buff theo loại nguồn: cùng `(sourceType, BuffType)` chỉ lấy giá trị mạnh nhất, các loại nguồn khác nhau cộng dồn; tổng buff AttackSpeed không vượt mức cấu hình +60%.

## Phạm vi

- Sửa `HeroStats` để tính lại buff khi thêm/thay thế/gỡ nguồn.
- Thêm cấu hình trần AttackSpeed trong `HeroStatsSettings` và cập nhật asset hiện có qua Unity Editor.
- Viết EditMode tests cho same-type, Support + aura, trần +60%, gỡ nguồn mạnh nhất và Damage không bị trần AttackSpeed.
- Ghi handoff và kiểm chứng script/compile; không chạy EditMode/PlayMode tests trước T-16.

## Phụ thuộc và quy trình

- Theo quyết định của TVA, nhánh `feature/S06_A09` được tạo từ A-08 PR #38 dù PR đó vẫn draft/open; A-09 phải giữ riêng và phụ thuộc vào #38.
- Không sửa `TRACEABILITY.md` hoặc Spec dùng chung khi chưa có duyệt theo quy định.
- Kiểm thử sẽ được đưa vào lượt validation tổng hợp sau T-16.
