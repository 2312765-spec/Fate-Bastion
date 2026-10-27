# Hiện trạng Ngày 10 – 19/10/2026

> Cập nhật bổ sung ngày 27/10/2026. Đây là tiến độ thực thi bổ sung theo mốc lịch sử, không phải báo cáo công việc đã làm vào 19/10.

## Đã triển khai / kiểm tra

- **P1:** `AttackTimer` và `AttackModule` triển khai nhịp đánh theo scaled time, Melee trực tiếp, Projectile qua pool prewarm và điểm bắn `HeroController.Muzzle`. `HeroController.ResetState()` reset timer và trả đạn đang bay về pool.
- Sửa targeting để đọc `HeroStats.Range` hiệu lực sau nâng cấp; overload cũ vẫn còn.
- **P2:** `AttackModule` đã gắn lên `Hero_Base` trong Sandbox A. `_projectilePrewarmCount = 8`; chưa gán prefab do chờ C.
- **EditMode:** `AttackTimerTests` – **5/5 pass**, job `419f752182d14c5fb70ff66b6a116e1e`.
- **PlayMode:** `AttackModulePlayModeTests` + `HeroTargetingPlayModeTests` – **9/9 pass**, job `128b44820d2d4fe5ade5398b047a529d`.
- Lượt PlayMode đầu `96f5fb0a6b4f410cad71550bde99a7b3` không khởi chạy test trong 120 giây, Editor ở `InitTestScene`; thoát Play Mode, tải lại `A_Heroes`, chạy lại thành công. Lượt đầu của bộ test sau đó chạy 2/3: test bay thất bại do fixture đổi cờ bay sau khi địch đã spawn/đã bị đánh; sửa fixture để spawn sau khi cấu hình và xác nhận lại ở job 9/9 pass. Không còn job test treo.
- Phạm vi chốt theo trả lời của người dùng: T-06 chỉ Melee/Projectile; Area/Cone và passive để T-07/T-08.

## Hồi quy cuối

- EditMode S6: **13/13 pass**, job `a45cc5d6fe8b47d3abac31924e989baf`.
- PlayMode A-07 (`AttackModule`, `HeroTargeting`, `HeroController`): **12/12 pass**, job `b3c15e3aa26441b0818aa5effdcb9f5d`.

## Còn lại

- Chưa có prefab Projectile để gán vào `Hero_Base`; `Prefabs/FX/` thuộc C. Xem [handoff Ngày 10](./handoff.md).
- Chưa xác minh hình ảnh/projectile prefab thật trong Sandbox; phần này chờ C cung cấp asset và gán Inspector.
- PR #25 là PR hiện hữu của A-07; chưa merge. Việc T-06 cần được thêm vào nhánh PR đó sau khi xác minh trên Unity.
