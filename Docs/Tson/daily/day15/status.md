# Hiện trạng – Slot 15 – T-11 / A-11

## Đã triển khai ở mức code

- Thêm `CommandAuraSettings` cấu hình bán kính, tốc độ buff và chu kỳ kiểm tra.
- Thêm `CommandAura`: quét roster tướng đã khởi tạo mỗi 0.2 giây scaled-time, áp buff AttackSpeed khi vào, gỡ khi ra/không còn active/disable; phát `OnHeroEnteredAura(GameObject)` khi có transition vào.
- Expose `HeroController.ActiveHeroCount`, `GetActiveHero(index)` và version khởi tạo để tránh scene scan, phân biệt despawn/respawn giữa các lượt quét.
- Viết PlayMode case cho vào/ra, despawn/respawn và cleanup khi aura disable; chưa chạy test theo kế hoạch.
- Unity MCP standalone standard validation: không có error cho `CommandAuraSettings.cs` hoặc test; heuristic cảnh báo ở `CommandAura.Update`/`HeroController.Update` về log string ở nhánh capacity/error, không phải kết quả compile.

## Dời sang T-16

- Theo lựa chọn TVA, chưa dùng Unity Editor/MCP. Chưa tạo `CommandAuraSettings.asset`, chưa gắn component vào Summoner/Player prefab của C và chưa xác nhận compile.
- T-16 cần tạo asset với bán kính 6 m, +25% AttackSpeed, chu kỳ 0.2 s; C gắn component vào prefab Player của C và tham chiếu asset. B nghe `OnHeroEnteredAura` cho tutorial.
- Chưa chạy EditMode/PlayMode tests.

## Nhánh

- `feature/S07_A11` được tạo từ `feature/S06_A10`; draft PR sẽ phụ thuộc PR #40 và gián tiếp phụ thuộc #38. Không đưa PR #39 vào nhánh.
