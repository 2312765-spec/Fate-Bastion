# Handoff – Slot 20 – T-16 / Tích hợp và validation

## Phạm vi đã hoàn thành

- Tạo `feature/A_T16_integration` từ `origin/main`, dùng làm nhánh validation riêng; không merge vào `main` và không thay đổi trạng thái PR nguồn.
- Tích hợp theo thứ tự dependency: A #38 → #39 → #40 → #41 → #42 → #43; B #31 → #33 và #32 → #35; C #30.
- Đồng bộ nhánh với PR head và merge `origin/main` (`944a525`) trước lượt kiểm thử mới; resolve xung đột code/test và handoff, giữ bản sửa fixture Meteor trên nhánh tích hợp.
- Tạo settings asset A theo số liệu đã có trong S7, bao gồm SkillData riêng cho Thiên thạch. GUID tham chiếu đã được Unity MCP gán cho `meteor_settings`.

## Kết quả kiểm thử tổng hợp sau merge `main`

- Unity MCP refresh/compile hoàn tất; hai assembly được chạy toàn bộ trên nhánh tích hợp đã merge `main`.
- EditMode `FateBastion.Tests.EditMode`: **717/717 pass, 0 fail, 0 skip**; job `b64e5b6d634f417f842260fc9959be1a` (171.3 giây).
- PlayMode `FateBastion.Tests.PlayMode`: **154 pass / 157 total, 0 fail, 3 skip**; job `d6a119afa40a46c48588c34e6e9354ce` (19.3 giây). Ba test GC Alloc bị skip vì không hỗ trợ trên WindowsEditor.
- Tổng cộng **871 pass, 0 fail, 3 skip / 874 test cases**. Suite gồm các test của những A-xx đã tích hợp (bao gồm A-06/A-08/A-09/A-10/A-11/A-12/A-13); các con số là aggregate, không phải lượt chạy riêng hay số đếm riêng cho từng A-xx.
- Sau lượt T-16, TVA tạo scene `A_Skills.unity` trên `feature/S06_A08`; scene chưa có trong snapshot T-16 và chưa được Playtest.
- Lượt EditMode đầu tiên sau merge có lỗi AssetDatabase ghi `.meta`; test đó chạy riêng pass, sau đó full suite cuối cùng pass. Bản sửa `MeteorAbilityTests` không còn dùng fixture `MeteorTestInputRouter`.
- Lượt PlayMode đầu tiên sau merge phát hiện listener của `CommandAuraPlayModeTests` được đăng ký muộn; đã đăng ký trước khi bật aura, test riêng và full PlayMode cuối cùng đều pass.
- `HeroRageSettings.asset` đã được gán vào `_rageSettings` trên `Hero_Base.prefab` qua Unity MCP.
- **Chưa kiểm tra tay/playtest ba màn.** Validation tại đây chỉ gồm test tự động qua Unity MCP trong Unity Editor (EditMode và PlayMode); không bao gồm kiểm tra hoặc chỉnh tay lại prefab, asset, scene hay cân bằng.

## Cách tiếp tục

1. Sau khi PR được cập nhật, C mở đúng project và tiếp tục kiểm tra/chỉnh prefab Player theo ownership.
2. Gửi issue cho C để wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, ray provider `IMeteorAimRaySource` và các settings vào `Player.prefab`; C sở hữu prefab Player và phần input/camera. Không tự sửa prefab của C.
3. Hoàn tất kiểm tra tay/playtest ba màn cùng C; không mở hoặc lưu `Game.unity` khi chưa được C thực hiện.

## Issue draft cho C

**Tiêu đề:** `[Cần C] Wire S7 abilities và aim-ray provider vào Player`

**Nội dung:** Nhánh validation T-16 đã merge `main`; suite tự động qua Unity MCP có 717/717 EditMode pass và 154 PlayMode pass, 0 fail, 3 skip do WindowsEditor. Chưa có kiểm tra tay/playtest ba màn. Nhờ C triển khai `IMeteorAimRaySource` theo contract của A-13; bảo đảm không phát ray khi con trỏ ở trên UI hoặc Action Map không phải `Summoner`. Gắn/wire `CommandAura`, `MeteorAbility`, `MeteorAimIndicator`, `CommandAuraSettings.asset` và `meteor_settings.asset` lên Player; nối VFX/feedback phù hợp. Không thay đổi contract hay input actions nếu chưa trao đổi. Sau khi hoàn tất, phối hợp kiểm tra tay/playtest.

## Ranh giới tích hợp

- Nhánh này chứa các merge commit phục vụ validation. Không merge nhánh validation vào `main`.
- `Docs/TRACEABILITY.md` là vùng dùng chung; chưa sửa trạng thái các việc A/B/C vì PR đang mở và validation chưa hoàn thành.
