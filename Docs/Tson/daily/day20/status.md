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

- Sau khi buộc refresh toàn bộ, Unity biên dịch được các assembly test; EditMode và PlayMode hoàn tất.
- EditMode `FateBastion.Tests.EditMode`: **675/675 pass**, 0 fail, 0 skip; job `cb4aede713514d83b261bbeff260396a`, 329.7 giây.
- PlayMode `FateBastion.Tests.PlayMode`: **116/116 pass**, 0 fail, 0 skip; job `87b378136e49444aa9d5c386d4ebc169`, 41.1 giây.
- Các lượt đầu lộ ra 5 test Meteor dùng MonoBehaviour helper trong Editor assembly và 3 lỗi PlayMode. Đã sửa helper EditMode, cập nhật `HeroController` để không báo thiếu rage settings với hero không Legendary, đăng ký test listener trước khi bật CommandAura, rồi chạy lại hai suite đầy đủ.
- Gán `HeroRageSettings.asset` vào `_rageSettings` của `Hero_Base.prefab` qua Unity MCP.
- Chưa kiểm tra tay ba màn trong `Game.unity`; scene thuộc C và cần C thao tác.

## Blocker và bàn giao

- `Player.prefab` thuộc C: chưa được gắn/wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, `CommandAuraSettings`, `MeteorSettings` và provider cho `IMeteorAimRaySource`.
- Cần C cung cấp aim-ray provider, gắn các component/asset vào Player và nối FX. Cần B nối HUD/event nếu còn thiếu. Không sửa vùng của C/B trong nhánh này.
- PlayMode test log có warning dự kiến về HeroVisual giả trong fixture; không test nào fail.
- Các lỗi compile về file S6 thiếu xuất hiện trong log cũ trước khi nhánh tích hợp được refresh; lượt suite cuối pass đầy đủ.
- Issue draft cho C nằm trong `handoff.md`; test tay ba màn vẫn cần C/TVA phối hợp trên Editor.
