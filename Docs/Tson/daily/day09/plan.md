# Kế hoạch Ngày 9 – 18/10/2026

> Đây là kế hoạch theo mốc lịch sử trong `Docs/Tson/plan.md`. File được lập bổ sung ngày 27/10/2026 theo yêu cầu; không phải xác nhận các việc đã hoàn thành trong ngày 9.
> Phạm vi: Thành viên A – Gameplay. T-05/A-07 được triển khai bổ sung sau mốc lịch sử này.
> Ưu tiên: P1 không cần thao tác Unity · P2 cần Unity · P3 bàn giao.

## Việc trong ngày

| Mã | Việc | TRACEABILITY | Ưu tiên | Nhánh khi triển khai | Kiểm tra | Trạng thái |
|---|---|---|---|---|---|---|
| T-05 | `HeroController` + `HeroStats` + `TargetingModule`: khởi tạo/lifecycle tướng, chỉ số, chọn mục tiêu Đầu đoàn; DPS đơn ưu tiên boss, Melee bỏ qua quái Bay | A-07 | P1 → P2 | `feature/S06_A07` (tạo từ `main` mới nhất) | EM + PM | ☐ |

### Phạm vi và thứ tự

1. **P1 – `HeroStats` và `TargetingModule`**
   - Đọc chỉ số gốc từ `HeroData`; áp dụng các hệ số nâng cấp qua đúng API `SetLevelMultipliers`.
   - Chọn mục tiêu có `EnemyController.Distance` lớn nhất trong tầm; `Role.SingleDps` ưu tiên boss đang trong tầm; `AttackType.Melee` không chọn quái Bay.
   - Tầm và khoảng cách kiểm tra trên mặt phẳng XZ. Làm mới mục tiêu 5 lần/giây; không quét mỗi frame, không LINQ, không cấp phát danh sách trong vòng lặp.
2. **P2 – `HeroController`**
   - Cung cấp `Init(HeroData)`, `ResetState()` và `OnDespawn()` theo hợp đồng `TEAM_ASSIGNMENT.md` 3.1 #3; cache `HeroVisual` một lần trong `Awake`.
   - Thiếu `HeroVisual`: log lỗi rõ ràng, dùng vị trí gốc tướng làm điểm neo dự phòng, không crash.
   - Chỉ nối `HeroStats` và mục tiêu hiện tại; `AttackModule`, timer ra đòn và bắn từ `Muzzle` để T-06/Ngày 10.
   - Dựng Sandbox `A_Heroes` và chạy PlayMode kiểm tra khởi tạo, reset/despawn, chọn mục tiêu.

## File triển khai

- Tạo `Assets/_Project/Scripts/Heroes/Runtime/HeroController.cs`.
- Tạo `Assets/_Project/Scripts/Heroes/Runtime/HeroStats.cs`.
- Tạo `Assets/_Project/Scripts/Heroes/Runtime/TargetingModule.cs`.
- Tạo `Assets/_Project/Scripts/Heroes/Runtime/HeroStatsSettings.cs` và asset `Assets/_Project/Data/Settings/HeroStatsSettings.asset`.
- Tạo prefab `Assets/_Project/Prefabs/Heroes/Hero_Base.prefab` và scene `Assets/_Project/Scenes/Sandbox/A_Heroes.unity`.
- Tạo EditMode tests dưới `Assets/_Project/Tests/EditMode/S6/` cho chỉ số.
- Tạo PlayMode tests dưới `Assets/_Project/Tests/PlayMode/S6/` cho quy tắc chọn mục tiêu và lifecycle `HeroController`; scene `Assets/_Project/Scenes/Sandbox/A_Heroes.unity` để kiểm tra thủ công nếu cần.

## API dự kiến

| Thành phần | API / trách nhiệm |
|---|---|
| `HeroController` | `Init(HeroData data)`, `ResetState()`, `OnDespawn()` — chữ ký bàn giao đã chốt |
| `HeroStats` | Component trên `Hero_Base`; `Initialize(HeroData data)`; `SetLevelMultipliers(float damage, float attackSpeed, float range)` — chữ ký bàn giao đã chốt; tham chiếu settings gắn trong Inspector |
| `TargetingModule` | Nhận dữ liệu tướng và danh sách quái đang hoạt động; trả mục tiêu hợp lệ theo các quy tắc trên. Chữ ký cụ thể chưa được Spec quy định |

## Tiêu chí kiểm tra

- EditMode: chỉ số gốc, hệ số rarity từ settings và hệ số cấp được tính đúng; `SetLevelMultipliers` cập nhật sát thương/tốc đánh/tầm.
- PlayMode: DPS đơn đổi sang boss khi boss vào tầm; boss ngoài tầm thì chọn mục tiêu thường ở đầu đoàn; Melee không chọn Bay; tầm đo XZ; `Init` gán dữ liệu, `ResetState` xóa hệ số cấp, `OnDespawn` xóa dữ liệu; thiếu `HeroVisual` không làm crash.
- Kiểm tra thủ công trong Sandbox: đúng một mục tiêu được chọn theo từng tình huống; ngoài tầm không được chọn; mục tiêu được chọn vẫn nằm trong tầm ngang XZ.
- Không triển khai `AttackModule`, kỹ năng, buff hoặc Ultimate trong T-05; các phần đó thuộc T-06 trở đi.

## Điểm cần chốt trước khi code

1. `HeroData.damage` được mô tả là sát thương gốc trước hệ số độ hiếm. Bảng nguồn `Docs/root/CanBang/Tuong_Sheet.md` có `calc_rarityMult`, nhưng cột `calc_*` không được import vào `HeroData` và `heroes.csv` hiện không chứa hệ số runtime. **Đã chốt với người dùng:** dùng `HeroStatsSettings` ScriptableObject do A sở hữu làm nguồn runtime; không sửa `HeroData`, CSV hoặc importer.
2. `TEAM_ASSIGNMENT.md` 3.1 #11 (Trigger `Attack`/`Ultimate`/`Spawn`, Float `AttackSpeed`) còn ghi là đề xuất chờ A duyệt. Chưa wire các parameter Animator cho đến khi hợp đồng được xác nhận; không sửa file dùng chung trong T-05.
3. Tên getter cho chỉ số hiệu lực và chữ ký nội bộ của `TargetingModule` chưa được quy định. Chốt trong PR trước khi B nối hệ thống; không đổi chữ ký bàn giao #3/#4.

## Lắp và bàn giao Unity

- Sandbox thuộc A: `Assets/_Project/Scenes/Sandbox/A_Heroes.unity`; không sửa `Game.unity` hoặc prefab/scene của B/C.
- `Hero_Base` thuộc A; prefab Variant/model và `HeroVisual` thuộc C. Dùng `HeroVisual` làm ranh giới duy nhất để đọc Muzzle/Overhead/AuraAnchor/Animator, không tìm xương theo tên.
- Đã tạo `HeroStatsSettings.asset` với hệ số theo Rarity: Common 1, Rare 1.6, Epic 2.5, Legendary 4.
- Đã tạo `Hero_Base` gồm `HeroStats`, `HeroController`, `CapsuleCollider` và con rỗng `ModelRoot`; settings được gán vào `HeroStats`.
- Đã tạo scene Sandbox `A_Heroes`; chưa thêm model/`HeroVisual` vì phần đó thuộc C.
