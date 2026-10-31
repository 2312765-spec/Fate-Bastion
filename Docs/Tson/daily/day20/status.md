# Trạng thái – Slot 20 – T-16

## Nhánh validation

- Nhánh hiện tại: `feature/A_T16_integration`, tạo từ `origin/main`.
- Đã đồng bộ nhánh PR với `origin/feature/A_T16_integration` và merge `origin/main` mới nhất (`944a525`) để chạy lại suite trên snapshot đã cập nhật.
- Đã tích hợp tạm các PR đang mở theo dependency: A #38–#43, B #31/#33 và #32/#35, C #30.
- Resolve xung đột tích hợp, giữ các cập nhật handoff mới hơn từ `main` và giữ bản sửa test S7 trên nhánh tích hợp. Không merge PR nguồn vào `main`.

## Unity assets đã tạo qua Unity MCP

- `HeroRageSettings.asset`: +5 nộ mỗi đòn, ngưỡng 100.
- `CommandAuraSettings.asset`: bán kính 6 m, +25% tốc đánh, kiểm tra mỗi 0.2 giây.
- `meteor.asset`: `AreaAtPoint`, `GroundPoint`, True damage ×3, bán kính 4 m, hồi chiêu 45 giây, cast delay 0.8 giây.
- `meteor_settings.asset`: tham chiếu `meteor` và `enemy_normal`, tầm 20 m, vertical query padding 2 m.

## Validation

- Unity MCP refresh/compile hoàn tất; chạy lại toàn bộ hai assembly sau khi merge `main`.
- EditMode `FateBastion.Tests.EditMode`: **717/717 pass**, 0 fail, 0 skip; job `b64e5b6d634f417f842260fc9959be1a`, 171.3 giây.
- PlayMode `FateBastion.Tests.PlayMode`: **154 pass / 157 total**, 0 fail, 3 skip; job `d6a119afa40a46c48588c34e6e9354ce`, 19.3 giây. Ba test GC Alloc bị skip vì không hỗ trợ trên WindowsEditor.
- Tổng hợp: **871 pass, 0 fail, 3 skip / 874 test cases**; đây là số liệu của cả suite tích hợp, không phải số test riêng của từng A-xx.
- Các lượt đầu lộ ra 5 test Meteor dùng MonoBehaviour helper trong Editor assembly và 3 lỗi PlayMode. Đã sửa helper EditMode, cập nhật `HeroController` để không báo thiếu rage settings với hero không Legendary, đăng ký test listener trước khi bật CommandAura, rồi chạy lại hai suite đầy đủ.
- Lượt chạy toàn bộ ngay sau merge có một lỗi tạm thời khi AssetDatabase ghi `.meta`; test riêng sau đó pass và lượt EditMode toàn bộ cuối cùng pass. Test CommandAura cũng được chạy lại riêng sau khi sửa thứ tự đăng ký listener rồi toàn bộ PlayMode pass.
- Gán `HeroRageSettings.asset` vào `_rageSettings` của `Hero_Base.prefab` qua Unity MCP.
- Chưa kiểm tra tay hoặc chơi thử ba màn trong `Game.unity`; scene thuộc C và cần C thao tác. PlayMode ở trên là test tự động qua Unity MCP trong Unity Editor, không bao gồm kiểm tra/chỉnh tay lại prefab, asset hoặc scene.

## Blocker và bàn giao

- `Player.prefab` thuộc C: chưa được gắn/wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, `CommandAuraSettings`, `MeteorSettings` và provider cho `IMeteorAimRaySource`.
- Cần C cung cấp aim-ray provider, gắn các component/asset vào Player và nối FX. Cần B nối HUD/event nếu còn thiếu. Không sửa vùng của C/B trong nhánh này.
- PlayMode test log có warning dự kiến về HeroVisual giả trong fixture; không test nào fail.
- Các lỗi compile về file S6 thiếu xuất hiện trong log cũ trước khi nhánh tích hợp được refresh; lượt suite cuối pass đầy đủ.
- Issue draft cho C nằm trong `handoff.md`; test tay ba màn vẫn cần C/TVA phối hợp trên Editor.
