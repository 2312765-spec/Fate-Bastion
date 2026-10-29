# Trạng thái – Slot 20 – T-16

## Nhánh validation

- Nhánh hiện tại: `feature/A_T16_integration`, tạo từ `origin/main`.
- Đã tích hợp tạm các PR đang mở theo dependency: A #38–#43, B #31/#33 và #32/#35, C #30.
- Chỉ resolve xung đột văn bản trong `Docs/Tson/handoff.md` khi nhập PR #40; giữ cả nội dung A-09 và A-10. Không merge PR nguồn vào `main`.

## Unity assets đã tạo qua Unity MCP

- `HeroRageSettings.asset`: +5 nộ mỗi đòn, ngưỡng 100.
- `CommandAuraSettings.asset`: bán kính 6 m, +25% tốc đánh, kiểm tra mỗi 0.2 giây.
- `meteor.asset`: `AreaAtPoint`, `GroundPoint`, True damage ×3, bán kính 4 m, hồi chiêu 45 giây, cast delay 0.8 giây.
- `meteor_settings.asset`: tham chiếu `meteor` và `enemy_normal`, tầm 20 m, vertical query padding 2 m.

## Validation

- Đã yêu cầu Unity refresh toàn bộ Assets và compile sau khi merge.
- Console không trả lỗi C# mới tại lần đọc; Editor log còn các lỗi biên dịch cũ trước đó về file S6 chưa được tích hợp. Không có xác nhận compile cuối cùng độc lập.
- Đã khởi chạy EditMode assembly `FateBastion.Tests.EditMode`, job `a96bbb6b6d5e4aa5bfb06a9722575be7`. Unity Test Runner bắt đầu; Unity MCP mất kết nối khi lấy trạng thái, không có summary/ XML. Không đánh dấu pass.
- Chưa chạy PlayMode hoặc kiểm tra tay trong Game scene.

## Blocker và bàn giao

- Unity MCP mất kết nối với Editor đang chạy sau khi khởi tạo Test Runner. Chưa thể lấy kết quả job, kiểm tra prefab hoặc gắn field.
- `HeroRageSettings.asset` chưa được gán vào `_rageSettings` trên `Hero_Base.prefab` (prefab do A sở hữu).
- `Player.prefab` thuộc C: chưa được gắn/wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, `CommandAuraSettings`, `MeteorSettings` và provider cho `IMeteorAimRaySource`.
- Cần C cung cấp aim-ray provider, gắn các component/asset vào Player và nối FX. Cần B nối HUD/event nếu còn thiếu. Không sửa vùng của C/B trong nhánh này.
- Kết quả EditMode/PlayMode và thao tác tay ba màn vẫn chờ Editor kết nối lại.
