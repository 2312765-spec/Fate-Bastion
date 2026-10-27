# Contact – API và cách dùng dành cho B/C

> Cập nhật bổ sung ngày **27/10/2026**. Đây là chỉ mục bàn giao từ A; trạng thái PR dưới đây được kiểm tra cùng ngày. Tài liệu này không thay hợp đồng tại `Docs/TEAM_ASSIGNMENT.md` mục 3.1 hoặc checklist bàn giao 4.1.
> “Có API” không đồng nghĩa “đã bàn giao xong”: theo 4.1, cần PR merge vào `main`, test xanh, cách dùng, hướng dẫn Unity, Sandbox chạy được và người nhận xác nhận.

## Trạng thái nhanh

| Hệ thống / người nhận | API A cung cấp | Trạng thái |
|---|---|---|
| S2 → B (S5), C (S10) | `EnemyManager` / `WaveManager`, quái tới Thành, vòng đời quái và tiến độ wave | Đã có; xem mục 1 |
| S9 → B (S8, S10) | `SaveService`, `SaveData`, `SaveSanitizer` | PR [#23](https://github.com/2312765-spec/Fate-Bastion/pull/23) đang mở, chưa merge; B còn cần xác nhận 4.1 #5 |
| A-07 / S6 → B (S4), C (model/animation) | `HeroController`, `HeroStats`, `Hero_Base` | PR [#25](https://github.com/2312765-spec/Fate-Bastion/pull/25) đang mở, chưa merge; API chỉ sẵn trên nhánh PR, chưa có trên `main` |

## 1. S2 – Quái và Wave

**Dành cho:** B nối `Castle`/`GameManager`; C nối UI thế giới. Hợp đồng: `TEAM_ASSIGNMENT.md` 3.1 #1, #2, #9.

- B đăng ký `EnemyManager.OnEnemyReachedCastle(int castleDamage)` để xử lý Thành; A không gọi `Castle` hoặc tự trừ máu Thành.
- B gọi `WaveManager.Init(level, endless)`, `StartWave(wave)`, `StopAndClear()`; đăng ký `OnWaveFinished(int wave)`. Chỉ B phát `GameEvents.OnWaveStarted/OnWaveCleared`.
- C đăng ký `OnEnemySpawned` / `OnEnemyDespawned` để gắn/gỡ UI; khi spawn thì đăng ký `EnemyController.OnHealthChanged`, khi despawn thì hủy đăng ký. Không quét danh sách mỗi frame và không tự sửa trạng thái/máu quái.
- `EnemyManager.ActiveEnemies` chỉ đọc; mỗi `EnemyController` cung cấp `CurrentHP`, `MaxHP`, `IsBoss`, `IsFlying`, `Distance`, `Progress`, `HealthBarAnchor` và `OnHealthChanged`.
- Vàng khi hạ quái lấy từ `CombatEvents.OnEnemyKilled`; chỉ `Wallet` của B cộng Vàng. Chi tiết payload nằm trong `FateBastion.Core`.

Ví dụ đăng ký event (hủy đăng ký tương ứng trong `OnDisable`/`OnDestroy`):

```csharp
enemyManager.OnEnemyReachedCastle += HandleEnemyReachedCastle;
waveManager.OnWaveFinished += HandleWaveFinished;
enemyManager.OnEnemySpawned += HandleEnemySpawned;
enemyManager.OnEnemyDespawned += HandleEnemyDespawned;
```

Nguồn chi tiết: [S2](../Specs/S2.md), [hợp đồng giữa các thành viên](../TEAM_ASSIGNMENT.md), [bàn giao tổng thể của A](./handoff.md).

## 2. S9 – Lưu trữ cho B

**Vị trí:** `Assets/_Project/Scripts/Meta/Save/`, namespace `FateBastion.Meta`. `SaveService` là class C# thuần, không phải component Unity.

```csharp
var save = new SaveService(Application.persistentDataPath, starterFactory);
SaveLoadResult result = save.Load();
List<string> warnings = SaveSanitizer.Sanitize(result.Data, validHeroIds, starterDeck);
foreach (string warning in warnings) Debug.LogWarning(warning);
if (warnings.Count > 0) save.Save();
// Sau mỗi thay đổi Save.Data, gọi Save.Save().
```

- `Load()` trả `SaveLoadResult`; kiểm tra `Kind`/`Error` để biết dữ liệu được đọc, phục hồi từ backup hay tạo mới.
- Dùng chung một instance trong suốt phiên; không tạo một `SaveService` mới ở từng scene.
- Gọi `Save.Save()` sau gacha, ghép, đổi deck, kết quả trận, cài đặt và khi thoát ứng dụng. `Delete()` xóa save/backup/temp, dành cho debug.
- `starterFactory` phải nối `StarterSettings` thật của B khi có; `null` hiện tạo `SaveData` rỗng, không phải dữ liệu khởi đầu chính thức.
- Bàn giao chưa hoàn tất: PR #23 chưa merge và B chưa xác nhận đã thử dùng. Sandbox `A_Save` cùng cách dùng đầy đủ ở [handoff Ngày 8](./daily/day08/handoff.md).

## 3. A-07 / S6 – Vòng đời và chỉ số tướng

**Dành cho:** B trong S4. Code dưới đây nằm trong PR [#25](https://github.com/2312765-spec/Fate-Bastion/pull/25), chưa có trên `main` tại lần cập nhật này. B cần chờ merge hoặc làm việc trên đúng nhánh PR; không chép API thành một implementation khác.

```csharp
hero.SetEnemyManager(enemyManager);
hero.Init(heroData); // Init tự reset state trước khi khởi tạo dữ liệu và chỉ số.

hero.Stats.SetLevelMultipliers(1.4f, 1.1f, 1.1f); // ví dụ hệ số cấp 2

hero.OnDespawn(); // gọi trước khi trả instance về pool
```

- `HeroController.Init(HeroData)`, `ResetState()`, `OnDespawn()` là điểm nối 3.1 #3. `Init` hiện gọi `ResetState()`; `OnDespawn()` xóa dữ liệu/chỉ số và mục tiêu.
- `HeroStats.SetLevelMultipliers(float damage, float attackSpeed, float range)` là điểm nối 3.1 #4. B đọc hệ số từ `HeroLevelTable`; A chỉ áp dụng hệ số và tính lại chỉ số.
- `HeroController.Stats` cho phép đọc `Damage`, `AttacksPerSecond`, `Range`. `CurrentTarget` là mục tiêu hiện tại; `Muzzle` là điểm bắn, lấy từ `HeroVisual` và fallback về root nếu visual chưa gắn.
- `HeroController` cần `EnemyManager` được gán bằng `SetEnemyManager(...)` khi acquire; quét mục tiêu 5 lần/giây. B vẫn sở hữu pool tướng.
- PR #25 có `Hero_Base`, `HeroStatsSettings`, Sandbox `A_Heroes`, EditMode 8/8 và PlayMode 8/8 theo kết quả ngày 9. Đây là harness kiểm tra của A, chưa phải trận hoàn chỉnh của B.
- T-06 bổ sung `AttackModule`/`AttackTimer` cho PR #25: đòn Melee gọi `IDamageable` và tốc độ đánh dùng scaled-time; Projectile lấy tốc độ từ `HeroData.passiveSkill.projectileSpeed`, bắt đầu tại `HeroController.Muzzle`, dùng pool prewarm. `HeroController.ResetState()` reset timer và trả đạn đang bay về pool.
- `Hero_Base` hiện có `AttackModule`, `_projectilePrewarmCount = 8`, nhưng field `_projectilePrefab` còn trống do asset thuộc C chưa được bàn giao. Test dùng fixture Projectile tạo trong test; chưa xác nhận prefab thực trong trận. Chi tiết ở [handoff Ngày 10](./daily/day10/handoff.md).
- Kết quả kiểm thử T-06 trong Unity Editor: timer EditMode **5/5 pass**; AttackModule + targeting PlayMode **9/9 pass**. Chi tiết run/job nằm trong [status Ngày 10](./daily/day10/status.md).
- Target scan dùng `HeroStats.Range` sau nâng cấp; overload cũ `TargetingModule.FindTarget(position, enemies)` vẫn còn.
- Phạm vi T-06 đã chốt là Melee/Projectile. Area/Cone và passive được để T-07/T-08; module hiện cảnh báo rõ nếu nhận `AttackType.Area`/`Cone` cho tới khi SkillRunner nối xong.

**Dành cho C:** prefab Variant/model gắn `HeroVisual` ở gốc model; gameplay chỉ đọc `Muzzle`, `Overhead`, `AuraAnchor`, `Animator` từ `HeroVisual`, không tìm xương theo tên. `Hero_Base` và code A-07 hiện ở PR #25, vì vậy hãy phối hợp/đợi merge trước khi dựa vào asset trên `main`.

**Chưa chốt:** `TEAM_ASSIGNMENT.md` 3.1 #11 (Animator Trigger `Attack`/`Ultimate`/`Spawn`, Float `AttackSpeed`, `Animator.Rebind`) vẫn là đề xuất chờ A duyệt. Không coi các parameter này là hợp đồng đã duyệt. Dù animation được nối thế nào, sát thương đòn đánh vẫn phải chạy bằng timer logic, không dựa Animation Event.

Nguồn chi tiết: [S1](../Specs/S1.md), [S6](../Specs/S6.md), [S0b](../Specs/S0b.md), [bàn giao Ngày 9](./daily/day09/handoff.md).

### Kết quả hồi quy T-06

- EditMode S6: **13/13 pass**, job `a45cc5d6fe8b47d3abac31924e989baf`.
- PlayMode A-07 (`AttackModule`, targeting, `HeroController`): **12/12 pass**, job `b3c15e3aa26441b0818aa5effdcb9f5d`.
- Prefab Projectile thực vẫn chưa được gán hoặc xác minh trong Sandbox; xem [handoff Ngày 10](./daily/day10/handoff.md).

## 4. Việc cần xác nhận khi nhận bàn giao

- **B / S9:** thử Bootstrap/Save flow và xác nhận “Đã thử, dùng được” theo `TEAM_ASSIGNMENT.md` 4.1 #5; nối starter data thật và gọi Save sau các thay đổi S8.
- **B / S4:** sau khi PR #25 merge, nối `HeroPool` với lifecycle/chỉ số nêu trên; không tự thay chữ ký #3/#4.
- **C:** đợi `Hero_Base` trên `main` hoặc phối hợp trực tiếp trên PR #25; duyệt riêng Animator contract #11 với A trước khi tạo animation controller theo các parameter đó.
- Nếu cần đổi tên/chữ ký hoặc event tại điểm nối, tạo issue `[Cần A]` theo quy trình nhóm trước khi sửa hợp đồng.
