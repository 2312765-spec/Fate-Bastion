# Issue #64 — Hợp đồng animation tướng

## Yêu cầu

`HeroController` cần thực thi hợp đồng animation tướng tại mục 3.1 #11 của `TEAM_ASSIGNMENT.md`:

- Trigger `Attack` sau mỗi đòn đánh thực thi thành công.
- Trigger `Ultimate` khi `TryActivateUltimate()` thành công.
- Trigger `Spawn` trong `Init(HeroData)`.
- Float `AttackSpeed` bằng tốc độ đánh hiện tại chia cho tốc độ đánh gốc.
- Gọi `Animator.Rebind()` khi reset trạng thái tướng.

## Phạm vi triển khai

- `HeroController` cache `HeroVisual.Animator`, điều khiển các trigger theo hợp đồng và cập nhật `AttackSpeed`.
- `HeroStats` phát `StatsChanged` khi chỉ số thay đổi; `HeroController` đăng ký để cập nhật animation mà không polling mỗi frame.
- PlayMode test kiểm tra trigger đánh cơ bản và cập nhật `AttackSpeed` khi đổi hệ số chỉ số.

## Kiểm thử

- `dotnet build FateBastion.Tests.PlayMode.csproj --no-restore`: thành công, 0 lỗi, 0 cảnh báo.
- `dotnet build Fate-Bastion.slnx --no-restore`: chưa thành công do lỗi ngoài phạm vi tại `MeteorAbilityTests.cs` (S7), không tìm thấy `MeteorTestInputRouter`.
- Chưa xác nhận được PlayMode test: VS Code không khớp test đã chỉ định; Unity MCP không kết nối được.
- `git diff --check`: thành công.

## Hướng dẫn lắp trong Unity

Không cần tạo asset hoặc chỉnh prefab. Trên `HeroVisual`, bảo đảm field `Animator` tham chiếu Animator dùng `AC_Hero_Base.controller` hoặc Override Controller tương thích, với các parameter `Attack`, `Ultimate`, `Spawn` (Trigger) và `AttackSpeed` (Float).

## Trạng thái

Đã triển khai trên nhánh `fix/issue-64`; chờ review và xác nhận PlayMode test trước khi đóng issue.
