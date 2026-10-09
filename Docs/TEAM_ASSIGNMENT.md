# Phân công nhóm – Fate Bastion

> Phiên bản 1.0 · khớp GDD v1.3, Đặc tả v1.3, CLAUDE.md. Mục tiêu của file này: **mỗi người chỉ sửa vùng của mình**, mọi chỗ giao nhau đều đi qua "hợp đồng" (interface, event, asset dùng chung) đã chốt ở S0.
> Đi kèm: `Docs/ROADMAP.md` (lịch) và `Docs/TRACEABILITY.md` (checklist công việc, cột Vùng file lấy từ mục 2 file này). Đổi chủ sở hữu ở đây thì cập nhật cả `CLAUDE.md` mục 4 và cột Vùng file trong TRACEABILITY.

---

## 1. Vai trò

| Người | Vai trò | Hệ thống phụ trách | Review chéo |
|---|---|---|---|
| **A** | Gameplay | S1 Chiến đấu · S2 Quái và Wave · S6 Kỹ năng và Ultimate · S7 Hào quang và Thiên thạch · S9 Lưu trữ · Hướng dẫn màn 1 · nội dung file cân bằng | Review PR của **B** |
| **B** | Hệ thống và UI | S4 Đặt tướng và nâng cấp · S5 Luồng trận · S8 Gacha, Túi, Ghép, Deck · S10 code HUD/UI · công cụ Import Balance CSV · Bảng Debug | Review PR của **C** |
| **C** | Art và kỹ thuật hình ảnh | S0b HeroVisual · S3 Triệu Hồi Sư và Camera · S10 bố cục và prefab UI · S11 Âm thanh · map, model, animation, VFX · tối ưu hiệu năng · build | Review PR của **A** |

**Trưởng nhóm kỹ thuật (giữ "hợp đồng" chung):** ghi tên ở đây → **Thuận**. Người này duyệt mọi thay đổi ở vùng **Dùng chung** (mục 2.3).

---

## 2. Bản đồ sở hữu thư mục

Quy tắc vàng: **chỉ commit vào thư mục mình sở hữu.** Cần thay đổi ở vùng người khác → mở issue gắn tên chủ vùng (mục 5), không tự sửa.

### 2.1 Code (`Assets/_Project/Scripts/`)

| Thư mục | asmdef / namespace | Chủ | Nội dung |
|---|---|---|---|
| `Core/` | `FateBastion.Core` | **Dùng chung** (khóa sau S0) | enum, 4 event hub (`GameEvents`, `CombatEvents`, `HeroEvents`, `AbilityEvents`), payload struct (`DamageDealtInfo`, `EnemyKilledInfo`, `HeroEventInfo`, `MatchResult`), `InputRouter` (khai báo sự kiện), interface stub |
| `Combat/` | `FateBastion.Combat` | A | `DamageInfo`, `DamageCalculator`, `StatusEffectData`, `StatusEffectController` |
| `Enemies/` | `FateBastion.Enemies` | A | `EnemyData`, `WaveData`, `LevelData`, `EndlessSettings`, `WaveManager`, `EnemyManager`, `EnemyController`, `LevelPaths` (C đặt vào prefab map) |
| `Skills/` | `FateBastion.Skills` | A | `SkillData`, `SkillRunner`, các kind |
| `Heroes/Runtime/` | `FateBastion.Heroes` | A | `HeroController`, `HeroStats`, `TargetingModule`, `AttackModule` |
| `Heroes/Placement/` | `FateBastion.Heroes` | B | `PlacementRules`, `PlacementSystem`, `GhostPreview`, `HeroLevel`, `HeroLevelTable`, `Wallet` |
| `Heroes/Visual/` | `FateBastion.Heroes` | C | `HeroVisual` |
| `Heroes/Data/` | `FateBastion.Heroes` | **Dùng chung** (khóa sau S0) | `HeroData` |
| `Player/Movement/` | `FateBastion.Player` | C | `PlayerController`, `CameraRig`, `CursorController`, `SummonerInputRouter : InputRouter` (phần cài đặt) |
| `Player/Abilities/` | `FateBastion.Player` | A | `CommandAura`, `MeteorAbility`, `MeteorAimIndicator` |
| `Game/` | `FateBastion.Game` | B | `GameManager`, `TimeController`, `Castle` (`MatchResult` nằm ở Core vì `GameEvents.OnMatchEnded` dùng) |
| `Meta/Gacha/`, `Meta/Collection/` | `FateBastion.Meta` | B | `GachaService`, `CollectionService`, `DeckService`, `StarterSettings` |
| `Meta/Save/` | `FateBastion.Meta` | A | `SaveData`, `SaveService` |
| `UI/` | `FateBastion.UI` | B | View script, `UIManager`, `ConfirmDialog`, `OffscreenWarning`, `WorldHealthBar`, `CooldownSlot`, `UI/Debug/` (Bảng Debug) |
| `UI/Tutorial/` | `FateBastion.UI` | A | `TutorialController`, các bước hướng dẫn màn 1 |
| `Audio/` | `FateBastion.Audio` | C | `SoundData`, `AudioManager`, `MusicPlayer`, `FeedbackService` |
| `Editor/Import/` | `FateBastion.Editor` | B | Import Balance CSV, bộ đọc CSV |
| `Editor/Heroes/` | `FateBastion.Editor` | C | Validate Hero Prefabs, Render Hero Icons |
| `Editor/Debug/` | `FateBastion.Editor` | B | Menu debug Editor (Bảng Debug runtime nằm ở `UI/Debug/`, chủ B) |
| `Tests/EditMode/<Hệ thống>/` | — | Chủ hệ thống tương ứng | Mỗi người test trong thư mục con của mình |
| `Tests/PlayMode/<Hệ thống>/` | — | Chủ hệ thống tương ứng | |

> `CLAUDE.md` mục 4 dùng cấu trúc này. Hai người chung một asmdef (`Heroes`, `Player`, `Meta`, `UI`) nhưng **khác thư mục con** nên không đụng file của nhau.

### 2.2 Asset, prefab, scene

| Đường dẫn | Chủ | Ghi chú |
|---|---|---|
| `Data/Balance/*.csv`, `Docs/Can bang - Fate Bastion.xlsx` | A | Người khác đề xuất số qua issue; A sửa Excel → xuất CSV |
| `Assets/_Project/Data/Heroes/`, `Enemies/`, `Waves/`, `Levels/`, `Skills/*_passive` | **Sinh tự động** | Chỉ công cụ Import ghi. Không ai sửa tay các field có trong CSV |
| ↳ field gắn tay trong `HeroData` (prefab, icon) | C | Làm cùng Variant tướng và Render Hero Icons |
| ↳ field gắn tay trong `LevelData` (`hpMultiplier`, `displayName`, `enemyTypesPreview`) | A | TRACEABILITY S0-10 |
| ↳ field gắn tay trong `EnemyData` (prefab) | A | Trỏ tới Variant quái (model do C làm) |
| `Assets/_Project/Data/Skills/` (Ultimate Long Vương, SkillData không có trong CSV) | A | Kéo VFX prefab của C vào field VFX; không mở prefab FX để sửa |
| `Assets/_Project/Data/Settings/` | Chủ hệ thống của từng file (`PlacementRules` B, `MeteorSettings` A, `CameraSettings` C…) | Mỗi Settings SO một file riêng |
| `Prefabs/Heroes/Hero_Base.prefab` | A | Component hành vi. B thêm `HeroLevel` qua PR được A duyệt |
| `Prefabs/Heroes/Hero_<Tên>.prefab` (Variant) | C | Chỉ thay model dưới `ModelRoot` + `HeroVisual` |
| `Prefabs/Enemies/Enemy_Base.prefab` | A | Variant từng loại quái: C thay model |
| `Prefabs/Player/` | C | Triệu Hồi Sư, camera rig |
| `Prefabs/Systems/` (`_GameSystems`, `_UIRoot`, `_Audio`) | B / B / C | Mỗi nhóm manager một prefab, scene chỉ chứa instance |
| `Prefabs/UI/` | C (bố cục) | B gắn script view vào prefab của C qua PR được C duyệt |
| `Prefabs/FX/` | C | |
| `Prefabs/Map/Map_Level<n>.prefab` | C | Địa hình, Cổng, Thành, Spline đường chính + đường bay, component `LevelPaths` (script của A). A/B kéo prefab này vào Sandbox để thử, không sửa |
| `Art/`, `Audio/`, `Assets/ThirdParty/` | C | Ghi nguồn vào `ThirdParty/CREDITS.md` |
| `Scenes/Game.unity` | C | Chỉ chứa instance của `Map_Level<n>`, `_GameSystems`, `_UIRoot`, `_Audio`, `Player`. **Chỉ C mở và lưu** |
| `Scenes/Lobby.unity`, `Scenes/Boot.unity` | B | |
| `Scenes/Sandbox/A_*.unity`, `B_*`, `C_*` | Từng người | Thử nghiệm thoải mái, không ai khác mở |
| `Assets/_Project/Input/FateBastion.inputactions` | C | Người khác cần action mới → issue cho C |

### 2.3 Vùng dùng chung (khóa sau S0)

Các file này mọi người đều phụ thuộc. **Sau khi S0 merge, chỉ sửa qua PR có cả 3 người duyệt**, và cập nhật Spec trước khi sửa code.

- `Scripts/Core/**` (enum, 4 event hub, payload struct, `InputRouter` khai báo)
- **ScriptableObject do importer ghi và nhiều người đọc:** `Heroes/Data/HeroData.cs`, `Enemies/EnemyData.cs`, `Enemies/WaveData.cs`, `Enemies/SpawnGroup.cs`, `Enemies/LevelData.cs`, `Skills/SkillData.cs`, `Combat/StatusEffectData.cs`
- **Hợp đồng chiến đấu:** `Combat/DamageInfo.cs`, `Combat/IDamageable.cs`
- Mọi file `*.asmdef` (đổi tham chiếu assembly ảnh hưởng cả nhóm)
- `ProjectSettings/` (Tags & Layers, Physics layers, Input, Player, Graphics, Quality) và `Packages/manifest.json`
- `CLAUDE.md`, `Docs/Specs/*.md`, `Docs/TEAM_ASSIGNMENT.md`, `Docs/ROADMAP.md`
- `.gitignore`, `.gitattributes`

Ngoại lệ: `Docs/TRACEABILITY.md` – mỗi người tự tick dòng của mình (mỗi người một cột, xem file đó).

---

## 3. Hợp đồng giữa các người (event và interface)

Ai **phát** (publisher) thì sở hữu chữ ký event. Người nghe không sửa event của người khác.

| Event / Interface | Khai báo ở | Người phát | Người nghe |
|---|---|---|---|
| `IDamageable.TakeDamage(in DamageInfo)` | Core | A | A (quái), C (FX) |
| `CombatEvents.OnDamageDealt(DamageDealtInfo)`, `OnEnemyKilled(EnemyKilledInfo)` | Core | A | B (Vàng, HUD), C (số sát thương, âm thanh) |
| `GameEvents.OnStateChanged`, `OnWaveStarted(n)`, `OnWaveCleared(n)`, `OnCastleDamaged`, `OnMatchEnded(result)` | Core | B | A (wave), C (nhạc, rung) |
| `Wallet.OnGoldChanged` | Heroes/Placement | B | B (HUD) |
| `HeroEvents.OnHeroPlaced`, `OnHeroSold`, `OnHeroUpgraded`, `OnHeroSelected` (payload `HeroEventInfo`) | Core | B | A (buff Hào quang), C (âm thanh, FX) |
| `AbilityEvents.OnMeteorCooldownChanged(remaining, total)`, `OnMeteorExploded(point, radius)` | Core | A | B (HUD), C (rung, âm thanh) |
| `InputRouter`: `Move`, `SprintChanged`, `Look`, `Zoom`, `Select`, `Cancel`, `DeckSlot(int 0-4)`, `Meteor`, `Upgrade`, `Sell`, `Ultimate`, `SkipWave`, `DebugPanel`, `ActionMapChanged` | Core (khai báo) / Player/Movement (cài đặt) | C | A, B |
| `TimeController.SetSpeed`, `Pause`, `Resume` | Game | B | Mọi người gọi, **chỉ B sửa** |
| `HeroVisual.muzzle`, `overhead`, `auraAnchor`, `animator` | Heroes/Visual | C | A (bắn đạn), B (thanh/nhãn UI) |
| `SaveService.Save()`, `SaveData`, hàm xóa save cho Bảng Debug | Meta/Save | A | B (gacha, deck, cài đặt, Bảng Debug) |
| `LevelPaths` (`SplineContainer` theo `pathIndex` 0 = đường chính, 1 = đường bay) | Enemies | A khai báo, C gắn trong prefab map | A (`WaveManager`, `EnemyController`) |
| Hàm lệnh cho nút UI: `TimeController.SetSpeed/Pause/Resume`, `GachaService`, `CollectionService`, `DeckService`, bán/nâng tướng ở `Heroes/Placement` | Game, Meta, Heroes | B | B (UI) – chỉ gọi khi người chơi bấm; hiển thị vẫn qua event (`CLAUDE.md` mục 4) |

Thêm event mới vào Core = sửa vùng dùng chung → PR 3 người duyệt. Thêm event **chỉ dùng trong hệ thống mình** thì để trong thư mục của mình, không cần duyệt chung.

Hai lưu ý về `InputRouter`:

- `Core/InputRouter.cs` là MonoBehaviour chỉ khai báo event + `SetActionMap`, với các hàm `protected Raise…()`. C **không sửa file đó** (vùng khóa) mà viết `SummonerInputRouter : InputRouter` trong `Player/Movement/`, đọc Input System và gọi `Raise…()`. Các hệ thống khác luôn dùng qua `InputRouter.Instance`.
- **Không có action `Pause` riêng.** Esc đi qua `Cancel` và được xử lý theo thứ tự ưu tiên ở S3 (hủy ngắm/hủy đặt → đóng bảng tướng → mở Tạm dừng).

---

## 4. Làm việc khi phụ thuộc chưa xong

- Dùng **stub** theo interface ở Core. Đánh dấu `// STUB: thay bằng S<số>` để tìm lại (`grep -r "STUB:"`).
- Không chờ người khác để bắt đầu. Thứ tự phụ thuộc ở `ROADMAP.md` mục 3.
- Khi hệ thống thật merge, **người dùng stub** tự gỡ stub trong vùng của mình.

---

## 5. Khi cần sửa vùng của người khác

1. Mở GitHub issue, tiêu đề `[Cần <A/B/C>] <mô tả>`, gắn label `cross-owner`.
2. Ghi rõ: file/asset nào, vì sao, đề xuất thay đổi.
3. Chủ vùng làm, hoặc đồng ý bằng comment để người hỏi tự làm trong **một PR riêng** chỉ chứa thay đổi đó, chủ vùng duyệt.
4. Gấp (đang chặn việc) → nhắn trong nhóm, nhưng vẫn mở issue để lưu vết.

---

## 6. Git và Pull Request

- Nhánh: `feature/s<số>-<tên>` (vd `feature/s4-placement`), sửa lỗi `fix/<mô tả>`, prototype `prototype/<tên>`.
- **Một PR = một hệ thống hoặc một việc nhỏ**, chỉ chạm vùng của người mở PR. PR chạm vùng khác bị trả lại.
- Người review theo bảng mục 1 (A → B → C → A). PR chạm vùng dùng chung: cả 3 duyệt.
- Trước khi mở PR: pull `main`, chạy Test Runner, tick dòng tương ứng trong `TRACEABILITY.md`.
- Commit message tiếng Anh: `S4: add placement validity checks`.
- Không commit file `.unity` của người khác. Nếu Unity tự đánh dấu thay đổi scene không phải của mình: **discard**, không commit.

---

## 7. Nhịp làm việc

| Khi nào | Việc | Ai |
|---|---|---|
| Mỗi sáng (10 phút) | Hôm qua làm gì, hôm nay làm gì, đang bị chặn bởi ai | Cả nhóm |
| Mỗi tối | Một người chơi thử bản `main` 1 màn, ghi cảm giác vào issue `playtest` | Luân phiên A → B → C |
| Cuối tuần | Cập nhật `ROADMAP.md` (trạng thái mốc) và bảng trạng thái mục 10 của `CLAUDE.md` | Trưởng nhóm kỹ thuật |
| Cuối ngày 21 | Khóa tính năng | Cả nhóm |

---

## 8. Dùng Claude Code đúng vùng

Mỗi người mở Claude Code trong repo và **nói rõ mình là ai** ở đầu phiên:

> Tôi là B. Chỉ sửa file trong vùng của B theo Docs/TEAM_ASSIGNMENT.md mục 2. Nếu cần sửa vùng khác, dừng lại và soạn nội dung issue cho tôi.

`CLAUDE.md` đã có quy tắc này (mục "Vùng sở hữu"), nên Claude sẽ hỏi trước khi chạm vùng người khác.
