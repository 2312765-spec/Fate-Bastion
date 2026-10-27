# Kế hoạch Ngày 10 – 19/10/2026

> Lập bổ sung ngày **27/10/2026** theo lịch cá nhân trong `Docs/Tson/plan.md`; không phải xác nhận các việc đã hoàn thành ngày 10. T-06/A-07 chưa có trạng thái hoàn tất được xác nhận.
> T-05 đã được triển khai trên PR #25 nhưng PR vẫn mở, chưa merge. Kế hoạch này tiếp tục đúng A-07, không tạo nhánh cha/con.
> Ưu tiên: P1 không cần thao tác Unity · P2 thao tác Unity/Sandbox · kiểm thử dài đặt cuối · P3 bàn giao sau khi có kết quả kiểm thử.
> **Phạm vi đã chốt:** T-06 làm timer và đòn Melee/Projectile. Area/Cone cùng passive được để cho T-07/T-08 (SkillRunner); không thay thế chúng bằng Melee/Projectile.

## Việc trong ngày

| Mã | Việc | TRACEABILITY | Ưu tiên | Nhánh khi triển khai | Kiểm tra | Trạng thái |
|---|---|---|---|---|---|---|
| T-06 | `AttackModule`: đánh thường theo timer, bắt đầu đòn từ `HeroController.Muzzle`; kiểm tra trong Sandbox; bàn giao API #3/#4 cho B | A-07 | P1 → P2 → kiểm thử cuối → P3 | Tiếp tục PR/nhánh `feature/S06_A07` (PR #25) | EditMode/PlayMode | ◐ |

## Phạm vi và thứ tự

1. **P1 – Rà soát hợp đồng và hoàn thiện logic không cần Unity**
   - Đọc lại [S1](../../../Specs/S1.md), [S6](../../../Specs/S6.md), [S0b](../../../Specs/S0b.md), `HeroData`, `DamageInfo`, `Projectile` và code A-07 trên đúng nhánh PR #25.
   - Tạo `AttackModule` thuộc `FateBastion.Heroes/Runtime`; dùng `HeroController.CurrentTarget`, `HeroController.Stats` và `HeroController.Muzzle`. Tốc độ đánh lấy từ `AttacksPerSecond`; timer dùng thời gian game có tỉ lệ (`Time.deltaTime`), không phụ thuộc Animation Event.
   - Đòn hợp lệ phải đi qua `DamageInfo`/`IDamageable`; không tính sát thương ở một nơi thứ hai thay cho `DamageCalculator`. Nếu là đạn, dùng `Projectile` hiện có và điểm xuất phát từ `Muzzle`.
   - Kiểm tra mục tiêu còn sống/hợp lệ lúc ra đòn; không cấp phát mỗi frame, không gọi `Instantiate`/`Destroy` trong trận. `AttackModule` tự reset timer/tham chiếu khi tướng được lấy/trả pool.
   - Viết các test EditMode thuần cho nhịp timer nếu logic có thể tách không phụ thuộc scene; giữ test tích hợp PlayMode ở bước cuối.

2. **P2 – Tích hợp trong Unity, sau khi P1 ổn định**
   - Gắn `AttackModule` lên `Hero_Base` trong nhánh PR #25; chỉ sửa prefab/scene thuộc A.
   - Nối cấu hình cần thiết với data tướng và prefab đạn pool. Thành phần `Projectile` đã có API `Launch(...)`/`Finished`; chưa có prefab đạn trong nhánh và thư mục `Prefabs/FX/` do C sở hữu, nên ghi handoff cho C thay vì tự tạo asset thuộc C. Tuyệt đối không tạo/huỷ đạn cho từng lần bắn trong trận.
   - Dùng Sandbox `A_Heroes` để kiểm tra tướng có mục tiêu thì phát đòn từ `Muzzle`, không đánh khi ngoài tầm/target chết, và mục tiêu Melee không gồm quái Bay.
   - Không sửa `Game.unity`, scene của B/C hoặc vùng dùng chung. `Hero_Base`/`HeroStatsSettings` đang thuộc PR #25; không tạo PR phụ thuộc/nhánh con.

3. **Kiểm thử dài – cuối cùng**
   - Chạy test tích hợp liên quan A-07 và test PlayMode trong Sandbox sau khi code/prefab đã ổn định; đối chiếu timer theo `attacksPerSecond`, sát thương qua đúng đường `IDamageable`, và vòng đời pool.
   - Ghi rõ lệnh, job id, thời gian từng lượt, số test pass/fail/skip. Nếu cùng job timeout khoảng 3 lần và tổng chờ >180 giây, dừng poll/retry, không khởi chạy test từ đầu; ghi handoff theo luật 9 trong `README.md` và tiếp tục việc P1 khác.

4. **P3 – Bàn giao sau kiểm thử**
   - Cập nhật `contact.md` và hồ sơ ngày với cách nối `HeroPool`, chữ ký API thực tế, hướng dẫn Unity, kết quả kiểm tra và trạng thái PR.
   - Bàn giao #3/#4 cho B theo `TEAM_ASSIGNMENT.md` 3.1; chỉ đánh dấu hoàn tất khi đủ checklist 4.1, bao gồm PR merge và B xác nhận.

## File dự kiến

- Tạo `Assets/_Project/Scripts/Heroes/Runtime/AttackModule.cs`.
- Nếu cần bộ đếm thời gian thuần để kiểm thử, tạo một class logic nhỏ trong cùng vùng `Heroes/Runtime`; chỉ tách khi cần thiết.
- Cập nhật `Assets/_Project/Prefabs/Heroes/Hero_Base.prefab` trên đúng nhánh A-07 để gắn module và tham chiếu cần thiết.
- Tạo/cập nhật EditMode tests cho timer/điều kiện đánh và PlayMode tests trong `Assets/_Project/Tests/PlayMode/S6/`; dùng `Assets/_Project/Scenes/Sandbox/A_Heroes.unity`.
- Cập nhật hồ sơ bàn giao trong `Docs/Tson/contact.md` và file ngày; không sửa Spec/hợp đồng dùng chung nếu chưa có duyệt theo quy định.

## API dự kiến và điểm cần chốt

| Thành phần | Trách nhiệm / API |
|---|---|
| `AttackModule` | Thành phần runtime của tướng, tự dùng controller/stats/target hiện tại; API nội bộ, không thêm điểm nối công khai giữa A/B nếu chưa cần |
| `HeroController` | Dùng API sẵn có trên PR #25: `CurrentTarget`, `Stats`, `Muzzle`, `Init`, `ResetState`, `OnDespawn` |
| `HeroStats` | Đọc `Damage`, `AttacksPerSecond`, `Range`; B tiếp tục gọi `SetLevelMultipliers(damage, attackSpeed, range)` |
| `Projectile` | Nếu cần đạn, gọi `Launch(start, target, targetPoint, in damage, speed)`; gắn vào pool và chỉ trả pool qua `Finished` |

1. **Kiểu đánh:** đã hỏi và chọn chỉ làm Melee/Projectile trong T-06; Area/Cone và passive để T-07/T-08. Không tự suy ra hình dạng vùng hay góc đánh.
2. **Prefab đạn:** code pool prewarm của A nhận prefab `Projectile` qua Inspector; asset thực tế nằm ở `Prefabs/FX/` của C. Đã tạo handoff cho C, T-06 phần tích hợp Unity chờ prefab này. Không thêm component vào prefab/scene của C.
3. **Animation:** 3.1 #11 vẫn là đề xuất chờ A duyệt. Chưa wire Animator Trigger/Float cho đến khi A và C xác nhận hợp đồng; logic/timer ra đòn không được chặn bởi điểm này.
4. **Nối với API B:** S4 của B còn là phụ thuộc; bàn giao API #3/#4 ở dạng hướng dẫn và Sandbox trước. Không sửa `Heroes/Placement/` thay B.

## Hướng dẫn lắp trong Unity (dự kiến)

- Mở Sandbox thuộc A: `Assets/_Project/Scenes/Sandbox/A_Heroes.unity`.
- Trên prefab `Hero_Base`, thêm `AttackModule`; gán đúng tham chiếu module/pool được xác định khi triển khai. Không sửa model của C; `HeroVisual.Muzzle` là nguồn điểm bắn duy nhất.
- Với đạn, chờ prefab do C cung cấp ở vùng `Prefabs/FX/`, có component `FateBastion.Combat.Projectile` trên root; gán vào `_projectilePrefab`. Pool prewarm theo `_projectilePrewarmCount` trên `Hero_Base` trước khi vào trận.
- Sau khi gắn, kiểm tra không có lỗi Inspector/Console trước khi chạy PlayMode.
