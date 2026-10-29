# Handoff – Slot 20 – T-16 / Tích hợp và validation

## Phạm vi đã hoàn thành

- Tạo `feature/A_T16_integration` từ `origin/main`, dùng làm nhánh validation riêng; không merge vào `main` và không thay đổi trạng thái PR nguồn.
- Tích hợp theo thứ tự dependency: A #38 → #39 → #40 → #41 → #42 → #43; B #31 → #33 và #32 → #35; C #30.
- Xung đột duy nhất ở `Docs/Tson/handoff.md` khi nhập #40; đã giữ cả phần A-09 lẫn A-10.
- Tạo settings asset A theo số liệu đã có trong S7, bao gồm SkillData riêng cho Thiên thạch. GUID tham chiếu đã được Unity MCP gán cho `meteor_settings`.

## Kết quả kiểm thử hiện tại

- Unity refresh + compile đã được yêu cầu sau tích hợp. Khi đọc Console, không có lỗi C# mới; Console có lỗi môi trường Unity Assistant/MCP. Editor log còn các lỗi cũ từ giai đoạn chưa tích hợp S6; do Unity MCP bị ngắt giữa lượt kiểm thử nên compile cuối không được xác nhận bằng kết quả hoàn tất.
- EditMode job `a96bbb6b6d5e4aa5bfb06a9722575be7` đã bắt đầu. Unity log có sự kiện `RunStarted`, nhưng Unity MCP ngắt kết nối khi truy vấn job và không trả kết quả cuối; không có XML. Không tuyên bố test pass.
- Chưa chạy PlayMode hay kiểm thử gameplay bằng tay.

## Cách tiếp tục

1. Kết nối lại Unity Editor đang mở đúng project, yêu cầu refresh/compile và đọc Console để xác nhận không còn lỗi.
2. Gán `HeroRageSettings.asset` vào `_rageSettings` của `Hero_Base.prefab` bằng Unity Editor/MCP. Prefab này thuộc A.
3. Gửi issue cho C để wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, ray provider `IMeteorAimRaySource` và các settings vào `Player.prefab`; C sở hữu prefab Player và phần input/camera. Không tự sửa prefab của C.
4. Chạy lại EditMode và PlayMode trên nhánh validation, lưu job ID, pass/fail/skip và thời gian thực tế.
5. Hoàn tất các bước test tay của ba màn trong Game scene cùng C; không mở hoặc lưu `Game.unity` khi chưa được C thực hiện.

## Issue draft cho C

**Tiêu đề:** `[Cần C] Wire S7 abilities và aim-ray provider vào Player`

**Nội dung:** Nhánh validation T-16 đã tích hợp các PR A #38–#43. Nhờ C triển khai `IMeteorAimRaySource` theo contract của A-13; bảo đảm không phát ray khi con trỏ ở trên UI hoặc Action Map không phải `Summoner`. Gắn/wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, `CommandAuraSettings.asset` và `meteor_settings.asset` lên Player; nối VFX/feedback phù hợp. Không thay đổi contract hay input actions nếu chưa trao đổi. Sau khi hoàn tất, cho A biết để chạy lại compile và test tích hợp.

## Ranh giới tích hợp

- Nhánh này chứa các merge commit phục vụ validation. Không merge nhánh validation vào `main`.
- `Docs/TRACEABILITY.md` là vùng dùng chung; chưa sửa trạng thái các việc A/B/C vì PR đang mở và validation chưa hoàn thành.
