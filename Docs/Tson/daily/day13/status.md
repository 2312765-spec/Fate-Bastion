# Hiện trạng – Slot 13 – T-09 / A-09

## Đã thực hiện

- `HeroStats` gộp contribution mạnh nhất theo `(sourceType, BuffType)`, cộng các nhóm khác nhau và giới hạn riêng tổng AttackSpeed bằng setting.
- `HeroStatsSettings` có cấu hình trần; asset `HeroStatsSettings.asset` đã được cập nhật bằng Unity Editor với giá trị `0.6`.
- Đã viết năm EditMode tests: cùng loại nguồn, Support + aura, vượt trần, gỡ contribution mạnh nhất, và Damage không bị trần AttackSpeed.
- Unity MCP standard validation: không có diagnostic cho hai script runtime và test file.

## Chưa hoàn tất

- Chưa chạy EditMode/PlayMode tests theo quyết định hoãn validation đến sau T-16.
- Chưa commit/push hoặc mở PR A-09.
- A-09 dựa trên `feature/S06_A08` tại `decda92`; PR #38 vẫn draft/open. Nhánh này chưa độc lập để merge vào `main`.
- Chưa cập nhật `TRACEABILITY.md`; đây là file dùng chung và cần quy trình duyệt trước khi sửa.
