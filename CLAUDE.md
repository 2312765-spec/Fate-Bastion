# CLAUDE.md – Fate Bastion (Thủ Thành Vận Mệnh)

File này Claude Code đọc mỗi lần làm việc trong repo. Nó tóm tắt game, quy ước code và các quy tắc bắt buộc. Chi tiết từng hệ thống nằm ở `Docs/Specs/S<số>.md`; nếu file này và Spec khác nhau, **Spec thắng**, và báo lại cho người dùng để sửa file này.

---

## 1. Game là gì (đọc trước tiên)

Tower Defense 3D góc nhìn thứ 3, PC (bàn phím + chuột), Unity 6.3 LTS + URP. Đồ án 3 người, 4 tuần.

- **Ngoài trận:** quay gacha bằng Ngọc → nhận tướng (4 độ hiếm) vào Túi → ghép 3 bản thừa cùng độ hiếm thành 1 tướng ngẫu nhiên bậc kế → chọn deck 5 tướng khác nhau.
- **Trong trận:** người chơi điều khiển **Triệu Hồi Sư** đi trên map. Trả Vàng để đặt tướng (tối đa 15) và nâng cấp (cấp 1→3). Tướng tự đánh quái đi theo một đường cố định từ Cổng tới Thành.
- **Triệu Hồi Sư** không có máu, có 2 năng lực: **Hào quang Chỉ huy** (bị động, tướng trong 6 m được +25% tốc đánh) và **Thiên thạch** (phím Q, tầm 20 m, nổ 4 m, hồi chiêu 45 giây).
- **Trong trận không có yếu tố ngẫu nhiên** (không chí mạng, không tỉ lệ kích hoạt). Mọi RNG nằm ở gacha và ghép.
- **Không có trong phạm vi** (đừng code, đừng thêm hook "để sau"): Nhập Hồn, Mythic/Secret, lên sao, mảnh, Bùa May mắn, Index, Thử thách ngày, Co-op, mạng, bảng xếp hạng online, tốc độ ×3, cắt cảnh.

Tài liệu gốc:

| File | Nội dung |
|---|---|
| `Docs/GDD - Tower Defense RNG.docx` | Thiết kế game v1.3 (luật, số liệu, HUD, lộ trình) |
| `Docs/Dac ta ky thuat.docx` | Đặc tả kỹ thuật S0–S11 (bản gốc của `Docs/Specs/`) |
| `Docs/Specs/S0.md`, `S0b.md` … `S11.md` | Đặc tả từng hệ thống – **đọc file tương ứng trước khi code** |
| `Docs/Can bang - Fate Bastion.xlsx` | File cân bằng; xuất ra CSV |
| `Docs/TEAM_ASSIGNMENT.md` | Ai sở hữu thư mục, asset, scene nào; hợp đồng event giữa các người |
| `Docs/ROADMAP.md` | Lịch 28 ngày, mốc, bàn giao |
| `Docs/TRACEABILITY.md` | Yêu cầu R01–R31 → việc A-xx / B-xx / C-xx, checklist PR |
| `Data/Balance/heroes.csv`, `enemies.csv`, `waves.csv` | Dữ liệu cân bằng, import vào asset bằng công cụ Editor |

### Các tài liệu nối với nhau thế nào

```
GDD (luật) → Specs/S<số>.md (làm thế nào) → TRACEABILITY (R-xx → việc A/B/C-xx, tick khi xong)
                                           ↘ TEAM_ASSIGNMENT (ai sở hữu file/asset, hợp đồng event)
                                           ↘ ROADMAP (ngày nào, bàn giao cho ai)
CLAUDE.md = quy tắc chung rút gọn từ tất cả các file trên
```

**Khi thay đổi một thứ, cập nhật cùng lúc trong cùng PR:**

| Thay đổi | Sửa cả |
|---|---|
| Luật / số thiết kế | `Specs/S<số>.md` → mục 9 file này (nếu có trong bảng) → Excel/CSV |
| Thêm/đổi event ở Core, interface, asmdef | `Specs/S0.md` hoặc Spec chủ event → mục 4, 6 file này → `TEAM_ASSIGNMENT.md` mục 3 |
| Đổi chủ sở hữu thư mục/asset | `TEAM_ASSIGNMENT.md` mục 2 → mục 4 file này → cột Vùng file trong `TRACEABILITY.md` |
| Thêm/bớt/dời việc | `TRACEABILITY.md` (mục 2–6) → `ROADMAP.md` (ngày, bàn giao) |
| Cắt tính năng (phao cứu sinh) | `ROADMAP.md` mục 4 → mục 1 file này → đánh dấu dòng R-xx trong `TRACEABILITY.md` là "cắt" |

---

## 2. Cách làm việc với Claude trong repo này

1. **Luôn đọc trước:** file này → `Docs/Specs/S<số>.md` của hệ thống đang làm → các Spec mà nó phụ thuộc (ghi ở cột "Cần có trước" trong S0).
2. **Lập kế hoạch trước, chờ duyệt rồi mới sửa code.** Kế hoạch gồm: file sẽ tạo/sửa, class và public API, test sẽ viết, những điểm Spec chưa rõ.
3. **Spec chưa rõ thì hỏi, không tự đoán.** Nếu buộc phải giả định, ghi rõ giả định trong kế hoạch và trong comment `// ASSUMPTION:`.
4. **Không mở rộng phạm vi.** Chỉ làm đúng hệ thống được giao. Thấy lỗi ở hệ thống khác thì báo, không tự sửa trừ khi được yêu cầu.
5. **Viết test cùng lúc với code**, theo mục "Tiêu chí hoàn thành" trong Spec. Một hệ thống chỉ "xong" khi mọi tiêu chí có test hoặc có hướng dẫn kiểm tra tay rõ ràng.
6. **Claude không mở được Unity Editor.** Phần kéo thả prefab, gắn component trong Inspector, dựng scene, chơi thử là việc của người dùng. Khi xong code, luôn kèm **"Hướng dẫn lắp trong Unity"**: tạo asset nào, gắn component nào lên GameObject nào, kéo tham chiếu nào, layer/tag nào.
7. **Thay đổi thiết kế:** sửa `Docs/Specs/*.md` trước (hoặc nhắc người dùng sửa), rồi mới sửa code.
8. Trả lời người dùng bằng **tiếng Việt**; tên class, biến, commit message bằng **tiếng Anh**.

---

## 3. Tech stack (cố định – không thêm/bớt package khi chưa được đồng ý)

| Mục | Lựa chọn |
|---|---|
| Engine | Unity 6.3 LTS, URP (bật SRP Batcher, GPU Instancing) |
| Nền tảng | Windows x64; Mono khi dev, IL2CPP cho bản nộp |
| Input | Input System (Action Map `Summoner` và `UI`) |
| Camera | Cinemachine 3.x (Third Person Follow, Deoccluder, Impulse) |
| Đường đi | Unity Splines |
| UI | uGUI + TextMeshPro (font Dynamic có đủ dấu tiếng Việt) |
| Tween UI | PrimeTween (thư viện bên thứ ba duy nhất) |
| Lưu dữ liệu | Newtonsoft Json (`com.unity.nuget.newtonsoft-json`) |
| Hiệu ứng | Particle System (Shuriken) |
| Test | Unity Test Framework (EditMode + PlayMode) |

**Không dùng:** Addressables, Netcode, VFX Graph, DOTS/ECS, UniTask, LINQ trong code chạy mỗi frame, `Resources.Load` cho dữ liệu game (trừ khi Spec cho phép), Singleton tràn lan.

**Cinemachine 3 dùng namespace `Unity.Cinemachine`** (không phải `Cinemachine` của bản 2.x). Component: `CinemachineCamera`, `CinemachineThirdPersonFollow`, `CinemachineDeoccluder`, `CinemachineImpulseSource`.

---

## 4. Cấu trúc thư mục

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/        FateBastion.Core      (enum, event, pool, time, input, save interface)
│   │   ├── Combat/      FateBastion.Combat    (DamageInfo, DamageCalculator, status effect)
│   │   ├── Heroes/      FateBastion.Heroes    Data/ (HeroData) · Runtime/ (A) · Placement/ (B) · Visual/ (C)
│   │   ├── Enemies/     FateBastion.Enemies   (EnemyData, WaveData, LevelData, WaveManager, LevelPaths)
│   │   ├── Skills/      FateBastion.Skills    (SkillData, SkillRunner, các kind)
│   │   ├── Player/      FateBastion.Player    Movement/ (C: PlayerController, CameraRig, SummonerInputRouter) · Abilities/ (A: CommandAura, Meteor)
│   │   ├── Game/        FateBastion.Game      (GameManager, TimeController, Castle)
│   │   ├── Meta/        FateBastion.Meta      Gacha/, Collection/ (B) · Save/ (A)
│   │   ├── UI/          FateBastion.UI        view, UIManager, HUD, Debug/ (B) · Tutorial/ (A)
│   │   ├── Audio/       FateBastion.Audio
│   │   └── Editor/      FateBastion.Editor    Import/, Debug/ (B) · Heroes/ (C) – asmdef chỉ Editor
│   ├── Data/            ScriptableObject asset (Heroes/, Enemies/, Waves/, Levels/, Skills/, Settings/)
│   ├── Input/           FateBastion.inputactions (chủ: C)
│   ├── Prefabs/         Heroes/, Enemies/, Player/, Map/, Systems/, UI/, FX/
│   ├── Art/  Audio/
│   ├── Scenes/          Boot, Lobby, Game, Sandbox/A_*, B_*, C_*
│   └── Tests/
│       ├── EditMode/    FateBastion.Tests.EditMode – một thư mục con mỗi hệ thống (S0/, S1/…)
│       └── PlayMode/    FateBastion.Tests.PlayMode – một thư mục con mỗi hệ thống
├── ThirdParty/          asset tải về + CREDITS.md
Data/Balance/            heroes.csv, enemies.csv, waves.csv (ngoài Assets/)
Docs/                    GDD, Đặc tả, file cân bằng, Specs/
```

**Assembly Definition:** mỗi thư mục trong `Scripts/` một asmdef cùng tên namespace. Hướng phụ thuộc chỉ đi một chiều:

```
Core ← Combat ← Enemies ← Skills ← Heroes ← Player     (chuỗi gameplay, một chiều)
Core ← Meta                                            (gacha, túi, deck, lưu)
Game  → Heroes, Enemies, Player, Meta                  (điều phối trận, trả Ngọc sau trận)
UI    → Core, Combat, Enemies, Skills, Heroes, Game, Meta
Audio → chỉ Core
Editor → được tham chiếu mọi asmdef, chỉ build cho Editor
```

Tham chiếu package: `Enemies` → `Unity.Splines`, `Unity.Mathematics` · `Player` → `Unity.InputSystem`, `Unity.Cinemachine` · `UI` → `UnityEngine.UI`, `Unity.TextMeshPro`, `PrimeTween.Runtime` · Newtonsoft là DLL tự tham chiếu (asmdef test khai báo trong `precompiledReferences`).

Không tạo phụ thuộc vòng. Nếu cần gọi ngược chiều (vd Enemies báo cho Heroes), dùng event trong `FateBastion.Core` hoặc interface khai báo ở Core.

Vì sao chuỗi gameplay như trên: tướng và kỹ năng chọn mục tiêu theo `distance` trên Spline, boss, quái Bay → cần đọc `EnemyController`; Thiên thạch (Player) tính máu theo `LevelData`/`EnemyData`.

**Quy tắc cho `UI`:**
- **Hiển thị**: chỉ qua event (Core hub hoặc event của hệ thống, vd `Wallet.OnGoldChanged`) và đọc field ScriptableObject (`HeroData`, `EnemyData`, `LevelData`, `SkillData`). Không đọc trạng thái gameplay trong `Update`.
- **Lệnh từ nút bấm**: được gọi hàm lệnh public của service khi người chơi bấm (`TimeController.SetSpeed/Pause/Resume`, `GachaService.Pull…`, `CollectionService.Merge…`, `DeckService.Set…`, bán/nâng tướng). Không sửa field, không gọi logic mỗi frame.

Ai sở hữu thư mục con nào: `Docs/TEAM_ASSIGNMENT.md` mục 2.

---

## 5. Quy ước code

- **Đặt tên:** `PascalCase` cho class, method, property, enum; `_camelCase` cho field private; `camelCase` cho biến cục bộ và tham số; hằng số `PascalCase`.
- **Field cho Inspector:** `[SerializeField] private`, không dùng public field. Property chỉ đọc khi cần expose: `public float Damage => _damage;`.
- **ScriptableObject data:** field có thể để `public` khi là data thuần được importer ghi (HeroData, EnemyData…), theo đúng tên cột CSV (`damage`, `attacksPerSecond`, `baseHP`…) để importer map 1-1.
- **Mỗi file một class public**, tên file trùng tên class.
- **Không hardcode số cân bằng.** Mọi con số (giá, máu, tầm, hồi chiêu, %) nằm trong ScriptableObject hoặc CSV. Hằng số kỹ thuật (kích thước mảng NonAlloc, số lần quét/giây) để trong Settings SO hoặc `const` có comment.
- **Logic thuần tách khỏi MonoBehaviour:** tính sát thương, gacha, ghép, kinh tế, state machine, Meteor state… viết thành class C# thường (không phụ thuộc scene) để unit test EditMode. MonoBehaviour chỉ nối với Unity (input, transform, hiển thị).
- **Giao tiếp giữa hệ thống bằng event** (C# `event Action<T>` hoặc SO event channel). UI nhận dữ liệu qua event, chỉ gọi hàm lệnh khi người chơi bấm nút (mục 4), không đọc dữ liệu trong `Update`.
- **Singleton:** chỉ cho vài Manager cấp cao (GameManager, TimeController, AudioManager, SaveService, InputRouter). Không dùng `FindObjectOfType` trong gameplay.
- **Comment** bằng tiếng Việt hoặc tiếng Anh đều được, ngắn gọn, giải thích *vì sao*. Mỗi class public có XML summary 1 dòng.
- **`#nullable`**: không bắt buộc, nhưng kiểm tra null cho tham chiếu Inspector trong `Awake`/`OnValidate` và log lỗi rõ ràng.

---

## 6. Mã enum dùng chung (đặt trong `FateBastion.Core`)

CSV và code dùng **đúng** các tên này.

```csharp
public enum Rarity     { Common, Rare, Epic, Legendary }
public enum Element    { Fire, Ice, Lightning }          // Lửa, Băng, Lôi
public enum Role       { SingleDps, Mage, Control, Support }
public enum DamageType { Physical, Magic, True }
public enum AttackType { Melee, Projectile, Area, Cone }
public enum BuffType   { None, Damage, AttackSpeed }
public enum SkillKind  { MeleeHit, Projectile, AreaAtPoint, Cone, Line, Zone, Buff }
public enum AimMode    { Auto, GroundPoint }
public enum GameState  { Loading, Preparing, InWave, Resting, Won, Lost }
```

### Event hub dùng chung (`FateBastion.Core`, khóa sau S0)

| Hub | Event | Người phát |
|---|---|---|
| `CombatEvents` | `OnDamageDealt(DamageDealtInfo)`, `OnEnemyKilled(EnemyKilledInfo)` | A |
| `GameEvents` | `OnStateChanged(GameState)`, `OnWaveStarted(int)`, `OnWaveCleared(int)`, `OnCastleDamaged(int current, int max)`, `OnMatchEnded(MatchResult)` | B |
| `HeroEvents` | `OnHeroPlaced`, `OnHeroSold`, `OnHeroUpgraded`, `OnHeroSelected` (`HeroEventInfo`) | B |
| `AbilityEvents` | `OnMeteorCooldownChanged(float remaining, float total)`, `OnMeteorExploded(Vector3, float radius)` | A |

- Phát bằng `Raise…()` của hub, **không** `Invoke` trực tiếp. Payload là struct trong Core nên UI/Audio không cần tham chiếu Combat.
- Event là `static` nên sống qua load scene và qua từng test: gọi `ResetAll()` khi khởi tạo trận và trong `[TearDown]`.
- Event chỉ dùng trong một hệ thống thì để trong thư mục của hệ thống đó, không đưa vào Core.

---

## 7. Quy tắc bắt buộc (các lỗi dễ mắc nhất)

### Thời gian
- **Chỉ `TimeController` được sửa `Time.timeScale`** (×1, ×2, tạm dừng = 0). Không hệ thống nào khác đổi timeScale.
- **Gameplay (quái, tướng, đạn, hồi chiêu, Thiên thạch, wave)** dùng `Time.deltaTime` (có tỉ lệ) → ×2 tự nhanh gấp đôi, tạm dừng tự dừng.
- **Triệu Hồi Sư và camera** dùng `Time.unscaledDeltaTime`. **Không bao giờ chia cho `Time.timeScale`** (bằng 0 khi tạm dừng). Khi tạm dừng, Action Map `Summoner` bị tắt nên nhân vật đứng yên. Cinemachine Brain bật Ignore Time Scale.
- **UI tween (PrimeTween)** dùng thời gian không tỉ lệ (`useUnscaledTime: true`) để chạy được khi tạm dừng.

### Input
- Chỉ `InputRouter` đọc Input System và phát event. Hệ thống khác **không** đọc phím trực tiếp.
- Đúng **một** Action Map bật tại một thời điểm (`Summoner` hoặc `UI`).
- Phím: WASD di chuyển, Shift chạy nhanh, 1–5 chọn tướng, Q Thiên thạch, chuột trái chọn/đặt/thả, **giữ** chuột phải xoay camera, **bấm nhanh** chuột phải hủy (interaction `Tap`, < 0.2 s, di chuyển < 5 px), U nâng cấp, X bán, R Ultimate, N bỏ qua chờ wave, Esc, F1 debug.
- **Esc theo thứ tự ưu tiên, mỗi lần bấm chỉ một bước:** hủy ngắm Thiên thạch / hủy đặt tướng → đóng bảng thông tin tướng → mở Tạm dừng.
- Click lên UI không được xuyên xuống thế giới: kiểm tra `EventSystem.current.IsPointerOverGameObject()` trước khi xử lý Select.

### Chiến đấu
- **Một chỗ tính sát thương duy nhất:** `DamageCalculator` (hàm thuần). `Sát thương = D_thực × k_kỹ năng × (1 − kháng)`, kháng kẹp [0; 0.8], True bỏ qua kháng, làm tròn xuống, tối thiểu 1.
- **Đòn đánh chạy theo timer logic, không dựa vào Animation Event.** Animation chỉ là hình ảnh (tăng tốc clip theo tốc đánh).
- **Tầm đánh và vùng nổ tính trên mặt phẳng ngang XZ** (quái Bay bay cao 2 m).
- **Tìm mục tiêu 5–10 lần/giây**, không mỗi frame. Mặc định "Đầu đoàn" (so `distance` trên Spline). **Vai trò `SingleDps` luôn ưu tiên boss nếu boss trong tầm.**
- Tướng `Melee` không chọn được quái Bay. Thiên thạch trúng cả quái Bay.
- Quái chết khi đạn đang bay: đạn bay tới vị trí cuối rồi tắt. Nhiều nguồn giết cùng frame: `OnEnemyKilled` chỉ phát **một lần** (cờ `IsAlive`).
- **Buff cộng dồn:** cùng một loại tướng (vd 2 Thánh Nữ) chỉ lấy mức mạnh nhất; khác nguồn thì cộng; tổng tốc đánh tối đa **+60%**.

### Quái và wave
- Máu khi sinh: `baseHP × hpGrowth^(wave−1) × hpMultiplier` (hpGrowth = 1.12; hpMultiplier màn 1/2/3 = 1.0/1.15/1.3; Vô tận dùng của màn 3). Máu lưu `float`.
- Kiểm tra tới Thành bằng `distance >= pathLength`, không dùng va chạm (ở ×2 bước di chuyển lớn).
- Tối đa **100 quái** cùng lúc; vượt thì chờ hàng đợi, hàng đợi vẫn tính vào điều kiện kết thúc wave.
- Wave kết thúc khi đã sinh hết và không còn quái sống, **kể cả quái con do boss gọi**.

### Bộ nhớ và hiệu năng
- **Không `Instantiate`/`Destroy` trong trận.** Dùng `UnityEngine.Pool.ObjectPool<T>`, prewarm khi load màn. Pool cho: từng loại quái, đạn, hiệu ứng, số sát thương, AudioSource, tướng.
- Lấy từ pool: luôn gọi `ResetState()`. Trả về pool: `StopAllCoroutines()`, hủy đăng ký event, gỡ buff.
- **Không tạo rác mỗi frame:** không LINQ, không `new List`, không nối string, không `GetComponent` trong `Update`; dùng `Physics.OverlapSphereNonAlloc`/`RaycastNonAlloc` với mảng dựng sẵn; cache component trong `Awake`.
- Một `EnemyManager` cập nhật tất cả quái (không mỗi quái một `Update`).
- Text HUD chỉ đổi khi giá trị đổi. Tách Canvas tĩnh (HUD) và động (số sát thương, thanh máu).
- Mục tiêu: GC Alloc ≈ 0 B/frame trong trận; ≥ 60 FPS (RTX 3050) và ≥ 30 FPS (Intel UHD) ở 1080p với 100 quái.

### Lưu dữ liệu
- `SaveService` dùng Newtonsoft Json tại `Application.persistentDataPath/save.json`.
- **Ghi an toàn:** ghi `save.tmp` → nếu `save.json` đã có thì `File.Replace(tmp, json, bak)`, nếu chưa thì `File.Move(tmp, json)`. Không ghi đè trực tiếp.
- Đọc: `save.json` → lỗi thì `save.bak` → lỗi nữa thì tạo mới từ `StarterSettings` và báo người chơi.
- Lưu ngay sau: quay gacha, ghép, đổi deck, kết quả trận, đổi cài đặt, `OnApplicationQuit`. **Không lưu trạng thái giữa trận.**
- Test dùng thư mục tạm, không dùng `persistentDataPath` thật.

### Gacha
- RNG là `System.Random` **truyền vào từ ngoài** (để test cố định seed). Không dùng `UnityEngine.Random` trong Meta.
- Quay x10: tính trước cả 10 kết quả trong bộ nhớ (kể cả pity và bảo đảm Epic), rồi mới áp vào SaveData một lần.

### Debug
- Toàn bộ code Bảng Debug (F1) bọc trong `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Bản nộp là build Release (không có Debug).

### Hình ảnh tướng (HeroVisual – S0b)
- Code gameplay **không** tìm vào bên trong model (không `transform.Find("RightHand")`, không tìm xương theo tên).
- Mỗi model tướng có component `HeroVisual` (`FateBastion.Heroes`, thư mục `Heroes/Visual/`) ở gốc model, giữ: `muzzle` (điểm bắn đạn/tung phép), `overhead` (thanh máu, nhãn), `auraAnchor` (hào quang độ hiếm, hiệu ứng cấp 3), `animator`.
- `HeroController` lấy `HeroVisual` bằng `GetComponentInChildren<HeroVisual>()` trong `Awake` và cache lại. Thiếu thì log lỗi rõ ràng và dùng vị trí tướng làm mặc định, không crash.
- Đổi ngoại hình = thay model dưới `ModelRoot` trong Prefab Variant + gắn lại `HeroVisual`. Không sửa code, không sửa HeroData.

### Vùng sở hữu (bắt buộc)
- Đầu mỗi phiên, người dùng sẽ nói mình là **A, B hay C**. Nếu chưa nói, hỏi trước khi sửa file.
- **Chỉ tạo/sửa file trong vùng của người đó** theo `Docs/TEAM_ASSIGNMENT.md` mục 2. Vùng **Dùng chung** (danh sách đầy đủ ở `TEAM_ASSIGNMENT.md` mục 2.3: `Core/`, các SO dữ liệu do importer ghi, `DamageInfo`/`IDamageable`, mọi `*.asmdef`, `ProjectSettings/`, `Packages/`, `CLAUDE.md`, `Docs/Specs/`, `TEAM_ASSIGNMENT.md`, `ROADMAP.md`, `.gitignore`/`.gitattributes`) chỉ sửa khi người dùng xác nhận đã có PR 3 người duyệt.
- Cần thay đổi ở vùng người khác: **dừng lại**, soạn sẵn nội dung GitHub issue `[Cần <A/B/C>] …` (file nào, vì sao, đề xuất) để người dùng gửi; trong lúc chờ dùng stub `// STUB:` trong vùng của mình.
- Không mở/sửa scene của người khác (`Game.unity` của C, `Lobby.unity`/`Boot.unity` của B, `Sandbox/<người khác>_*`).
- Khi xong việc, nhắc người dùng tick dòng tương ứng trong `Docs/TRACEABILITY.md`.

---

## 8. Dữ liệu cân bằng và công cụ import (S0)

**Không sửa tay asset HeroData/EnemyData/WaveData do importer tạo** – sửa Excel → xuất CSV → chạy `Tools → Import Balance CSV`.

Định dạng CSV:
- UTF-8 **có BOM**; đọc bằng `File.ReadAllText(path, Encoding.UTF8)` và bỏ ký tự `\uFEFF` đầu file nếu còn.
- Hàng 1 là tên cột (khớp tên field). Hàng có ô đầu bắt đầu bằng `#` bị bỏ qua (hàng 2 là mô tả tiếng Việt).
- Dấu phân cách `,` hoặc `;` – xác định từ hàng 1. **Phải dùng bộ đọc hiểu ô trong ngoặc kép** (cột `notes` có dấu phẩy) – không dùng `Split(',')`.
- Số đọc bằng `CultureInfo.InvariantCulture`. Bool: `TRUE`/`FALSE`, không phân biệt hoa thường. Enum: `Enum.TryParse(..., ignoreCase: true)`.
- Lỗi một dòng: log kèm tên file + số dòng, bỏ qua dòng đó, không dừng cả lần import. Cột lạ (vd `calc_*`) bỏ qua.

Ánh xạ:

| File | Asset | Quy tắc |
|---|---|---|
| `heroes.csv` | `HeroData` tên `<id>` + `SkillData` tên `<id>_passive` | `id…range` → HeroData; `areaRadius` → passive.radius; `buffType ≠ None` → passive kind `Buff` với `buffValue`, `buffRadius`; `notes` bỏ qua |
| `enemies.csv` | `EnemyData` tên `<id>` | 10 cột ghi thẳng |
| `waves.csv` | `WaveData` tên `L<level>_W<wave>`; `LevelData` tên `level_<level>` | Mỗi loại quái có số lượng > 0 → 1 `SpawnGroup`, thứ tự normal → fast → armored → flying → boss; `startDelay` = thứ tự × 2 s; `interval` = `spawnInterval`; flying `pathIndex` = 1, còn lại 0; `isBossWave` = boss > 0; LevelData nhận 15 WaveData theo thứ tự |

- Asset đã có cùng tên → cập nhật (giữ tham chiếu prefab/model/icon đã gắn tay). Không xóa asset thiếu trong CSV, chỉ cảnh báo.
- Ultimate của Long Vương, prefab, model, icon, VFX **không** có trong CSV – gắn tay trong Inspector; importer không được ghi đè các field này.
- **Ai gắn tay field nào** (chi tiết `TEAM_ASSIGNMENT.md` mục 2.2):

| Asset | Field gắn tay | Người gắn |
|---|---|---|
| `HeroData` | prefab, icon | C |
| `SkillData` | Ultimate Long Vương (cả asset), tham chiếu VFX prefab | A (VFX prefab do C làm) |
| `LevelData` | `hpMultiplier` (1.0 / 1.15 / 1.3), `displayName`, `enemyTypesPreview` | A (TRACEABILITY S0-10) |
| `EnemyData` | prefab | A (`Enemy_Base` + Variant của C) |

- **Đường đi không nằm trong `LevelData`** (SO không tham chiếu được object trong scene): component `LevelPaths` (A viết, `Enemies/`) giữ `SplineContainer` đường chính (`pathIndex` 0) và đường bay (`pathIndex` 1); C đặt nó trong prefab map và kéo Spline vào (S2).
- Tiêu chí: import ra đúng 9 HeroData, 9 SkillData bị động, 5 EnemyData, 45 WaveData, 3 LevelData; chạy lần 2 không tạo thêm asset.

---

## 9. Số liệu thiết kế chính (để đối chiếu, không hardcode)

| Hạng mục | Giá trị |
|---|---|
| Độ hiếm: tỉ lệ / hệ số / giá đặt / tối đa mỗi tướng | Common 65% ×1 100 6 · Rare 25% ×1.6 200 5 · Epic 8.5% ×2.5 350 4 · Legendary 1.5% ×4 600 2 |
| Quân số tối đa | 15 tướng; bán kính chiếm chỗ 0.8 m |
| Nâng cấp | Cấp 2: 75% giá đặt, +40% ST, +10% tốc đánh, +10% tầm · Cấp 3: 150%, +100%, +20%, +15% (giá làm tròn xuống) |
| Bán | hoàn `floor(goldSpent × 0.5)` (vd Epic cấp 2 → 306) |
| Vàng | bắt đầu 300; quái thường +5, đặc biệt +10, boss +150; cuối wave 50 + 10 × n; bỏ qua chờ +50 |
| Thành | 20 máu; thường −1, đặc biệt −2, boss −10; sao: ≥15 → 3★, ≥8 → 2★, còn sống → 1★ |
| Màn | 15 wave; chuẩn bị 15 s; nghỉ 8 s; boss wave 5/10/15; đường chính ~90 m |
| Quái | Thường 40 HP 2.5 m/s · Nhanh ×0.5 HP ×1.8 tốc độ · Giáp ×1.5 HP, giáp 50% · Bay ×0.8 HP, đường riêng · Boss ×15 HP, giáp/kháng 20%, gọi 3 Nhanh mỗi 12 s |
| Hệ | Lửa: Bỏng 20%/s 3 s · Băng: chậm 30% 2 s (tối đa 60%, boss 50% hiệu lực) · Lôi: nảy 2 quái trong 3 m, −30%/lần |
| Hào quang Chỉ huy | 6 m, +25% tốc đánh, kiểm tra 5 lần/s |
| Thiên thạch | tầm 20 m, nổ 4 m, rơi sau 0.8 s, hồi 45 s (thời gian game), True damage = 3 × máu quái Thường của wave hiện tại (đã nhân hpMultiplier) |
| Ultimate (Legendary) | +5 Nộ mỗi lần ra đòn (không theo số quái trúng), 100 = đầy, kích hoạt tay (nút/R), tự ngắm điểm đông quái nhất |
| Gacha | x1 = 100 Ngọc, x10 = 900 (≥ 1 Epic+); pity 25 (lượt 25 chắc Legendary); ghép 3 bản thừa cùng độ hiếm → 1 ngẫu nhiên bậc kế; luôn giữ ≥ 1 bản mỗi tướng |
| Người chơi mới | 1.000 Ngọc + 5 tướng: hero_cung_thu, hero_phu_thuy_lua, hero_linh_bang, hero_tu_si, hero_nguoi_tuyet (đã nằm trong `ownedEver`) |
| Ngọc thưởng | lần đầu qua màn +300; mỗi sao mới +50; Vô tận +10 mỗi 5 wave; lần đầu sở hữu tướng +50 |
| Tốc độ | ×1 / ×2 |

---

## 10. Danh sách hệ thống

| # | Hệ thống | Tuần | Phụ thuộc | Người | Trạng thái |
|---|---|---|---|---|---|
| S0 | Interface, data dùng chung, Import Balance CSV | Ngày 1–2 | — | Cả nhóm | ☑ |
| S0b | HeroVisual, Validate Hero Prefabs, Render Hero Icons | Ngày 3 | S0 | C | ☐ |
| S1 | Chiến đấu và sát thương | 1 | S0 | A | ☐ |
| S2 | Quái và Wave | 1 | S1 | A | ☐ |
| S3 | Triệu Hồi Sư và Camera | 1 | S0 | C | ☐ |
| S4 | Đặt tướng và nâng cấp | 1–2 | S0, S3 | B | ☐ |
| S5 | Luồng trận (GameManager) | 1–2 | S2, S4 | B | ☐ |
| S6 | Kỹ năng và Ultimate | 2 | S1, S0b | A | ☐ |
| S7 | Hào quang Chỉ huy và Thiên thạch | 2 | S3, S6 | A | ☐ |
| S8 | Gacha, Túi, Ghép, Deck | 3 | S0 | B | ☐ |
| S9 | Lưu trữ | 3 | S8 | A (B review; B nối S8 vào Save) | ☐ |
| S10 | HUD và UI | 2–3 | event S1–S9 | B, C | ☐ |
| S11 | Âm thanh và phản hồi | 3 | S1–S10 | C | ☐ |

Cập nhật cột Trạng thái (☐ → ◐ đang làm → ☑ xong) khi merge. **Feature freeze cuối ngày 21**: sau đó chỉ sửa lỗi và cân bằng.

Khi làm hệ thống mà hệ thống phụ thuộc chưa xong: dùng **stub** tối thiểu theo interface ở S0, đánh dấu `// STUB: thay bằng S<số>` để tìm lại.

---

## 11. Test

- **EditMode** (`Tests/EditMode`): mọi logic thuần – DamageCalculator, status effect, kinh tế (giá, hoàn tiền), state machine GameManager, MeteorAbility, Gacha (100.000 lượt, pity, x10), Collection (ghép), Save (lưu/đọc, file hỏng, lần lưu đầu), bộ đọc CSV.
- **PlayMode** (`Tests/PlayMode`): quái tới Thành ở ×1/×2, wave kết thúc đúng, đặt tướng hợp lệ, buff Hào quang vào/ra, chuyển Action Map.
- Tên test: `MethodOrScenario_Condition_ExpectedResult`, ví dụ `Sell_EpicLevel2_Refunds306`.
- Test dùng seed cố định và dữ liệu tạo trong test (`ScriptableObject.CreateInstance`), không phụ thuộc asset trong project trừ khi test import.
- Chạy test từ dòng lệnh (đóng Unity Editor trước):

```bash
"<UnityEditorPath>/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -
"<UnityEditorPath>/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile -
```

`<UnityEditorPath>` thường là `C:/Program Files/Unity/Hub/Editor/6000.3.x/Editor`. Nếu không chạy được dòng lệnh, hướng dẫn người dùng chạy Window → General → Test Runner.

---

## 12. Git

- `main` luôn chạy được. Mỗi hệ thống một nhánh `feature/s<số>-<tên>` (vd `feature/s1-combat`); sửa lỗi `fix/<mô tả>`.
- Commit nhỏ, message tiếng Anh dạng `S1: add DamageCalculator with armor clamp`.
- Merge qua Pull Request, ít nhất 1 người khác review và chơi thử.
- Asset Serialization = Force Text; Git LFS cho `.fbx .png .psd .wav .mp3 .tga`; luôn commit file `.meta` đi kèm.
- **Tránh xung đột scene:** làm mọi thứ thành Prefab; mỗi người test trong `Scenes/Sandbox/<tên>`; scene chính (`Game`, `Lobby`) chỉ một người sửa tại một thời điểm. Claude **không sửa file `.unity`/`.prefab` bằng tay** trừ khi được yêu cầu rõ – thay vào đó viết hướng dẫn lắp trong Unity.
- Không commit `Library/ Temp/ Logs/ UserSettings/ Builds/ TestResults/`.

---

## 13. Checklist trước khi báo "xong"

- [ ] Đọc lại Spec, đối chiếu từng mục Tiêu chí hoàn thành.
- [ ] Code compile, không warning mới, không phụ thuộc vòng giữa asmdef.
- [ ] Test EditMode/PlayMode viết đủ và (nếu chạy được) pass.
- [ ] Không hardcode số cân bằng; không `Instantiate`/`Destroy` trong trận; không alloc trong `Update`.
- [ ] Không đọc input trực tiếp ngoài `InputRouter`; không sửa `timeScale` ngoài `TimeController`.
- [ ] Có mục **"Hướng dẫn lắp trong Unity"** (asset cần tạo, component, tham chiếu, layer).
- [ ] Liệt kê giả định (`// ASSUMPTION:`) và stub (`// STUB:`) còn lại.
- [ ] Cập nhật bảng trạng thái ở mục 10 nếu được yêu cầu.

---

## 14. Khi gặp tình huống không có trong Spec

1. Tìm trong GDD (`Docs/GDD…docx`) hoặc các Spec liên quan.
2. Nếu vẫn không có: đề xuất 1–2 phương án đơn giản nhất phù hợp phạm vi 1 tháng, hỏi người dùng chọn.
3. Không thêm tính năng ngoài phạm vi (mục 1). Không "chuẩn bị sẵn" cho Nhập Hồn, mạng, Mythic…
