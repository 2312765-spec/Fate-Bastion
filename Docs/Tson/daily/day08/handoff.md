# Bàn giao – Ngày 8 – 17/10/2026

> Nhánh: `feature/S09_A14`. Tài liệu Ngày 8 chỉ nằm trên nhánh này (tài liệu Ngày 7 vẫn giống nhau trên cả 4 nhánh).
> Nội dung dưới đây là **bộ hồ sơ bàn giao S9 cho B**, viết sẵn để dán vào mô tả PR khi người dùng cho phép mở PR (điều kiện 4.1 #2/#3).

## 1. Việc bị chặn (A chờ người khác)

| Việc A | Mã | Chờ ai | Vì sao | Dùng stub gì để tiếp | Ngày cần |
|---|---|---|---|---|---|
| B xác nhận API S9 dùng được | A-14, A-15 | **B** (S8) | 4.1 #5 bắt buộc người nhận comment "Đã thử, dùng được" | API + Sandbox `A_Save` đã sẵn | 9 |
| `IStarterSaveFactory` có giá trị thật | A-15 | **B** (B-11 `StarterSettings`) | A không tự nối S8 | `starterFactory = null` → `SaveData` rỗng; Sandbox dùng `DemoStarterSaveFactory` (1000 Ngọc + 5 tướng) | khi B-11 xong |
| Xác nhận scene `A_Save` chạy được | A-14, A-15 | **A + Unity Editor** | Scene soạn tay khi chưa mở Editor | — | 9 |
| Tick A-14/A-15 trong `TRACEABILITY.md` | — | **cả nhóm** (PR 3 người) | Vùng khóa | — | 9–19 |

## 2. A giao cho B – S9 Lưu trữ (A-14 + A-15)

### 2.1 Cách dùng

**Vị trí code:** `Assets/_Project/Scripts/Meta/Save/`
**Namespace:** `FateBastion.Meta`
**Assembly:** `FateBastion.Meta` (asmdef, `overrideReferences: false` → Newtonsoft có sẵn, **không cần sửa asmdef**)

| API | Chữ ký / kiểu | Ghi chú |
|---|---|---|
| Tạo service | `SaveService(string directory, IStarterSaveFactory starterFactory = null)` | `directory` = `Application.persistentDataPath`; `starterFactory = null` → `SaveData` rỗng |
| Đọc | `SaveLoadResult Load()` | **Không bao giờ throw** với file thiếu/hỏng; xem `Kind`/`Error` |
| Ghi | `void Save()` · `void Save(SaveData data)` | An toàn: ghi `save.tmp` rồi `File.Replace` (json,bak) hoặc `File.Move` (lần đầu). **Không ghi đè trực tiếp `save.json`** |
| Xoá | `void Delete()` | Xoá `save.json`, `save.bak`, `save.tmp`; `Data = null` (B-16 dùng) |
| Trạng thái | `SaveData Data` · `bool Exists` · `string Directory` · `SaveFilePath` · `BackupFilePath` · `TempFilePath` | `Data` do `Load()`/`Save()` gán |
| Kết quả đọc | `SaveLoadResult { SaveData Data; SaveLoadKind Kind; string Error; bool Recovered }` | `Kind` ∈ `Loaded` / `RecoveredFromBackup` / `CreatedNew`; `Error` = lý do fallback (null nếu sạch) |
| Dọn dữ liệu | `SaveSanitizer.Sanitize(SaveData data, IReadOnlyCollection<string> validHeroIds, IReadOnlyList<string> starterDeck = null)` → `List<string>` | Sửa `collection`/`ownedEver`/`deck` tại chỗ, trả cảnh báo cho UI. `SaveSanitizer.DeckSize = 5` |
| Hằng số file | `SaveService.SaveFileName` / `BackupFileName` / `TempFileName` | `save.json` / `save.bak` / `save.tmp` |
| `SaveData` | `version`, `gems`, `pityCounter`, `collection` (`heroId → số bản`), `deck` (5 id), `ownedEver`, `levelProgress`, `endlessBest`, `tutorialDone`, `settings` | `SaveData.CurrentVersion = 1`; JSON dùng Newtonsoft, đọc thiếu field = mặc định |
| `SaveSettings` | `master`, `music`, `sfx` (float), `cameraShake`, `skipConfirm` (bool) | S10/S11 đọc-ghi trực tiếp trên `Save.Data.settings` |

**Ví dụ tối thiểu (Bootstrap/Lobby, một lần):**

```csharp
using System.Collections.Generic;
using System.Linq;
using FateBastion.Meta;
using UnityEngine;

public sealed class SaveBootstrap : MonoBehaviour
{
    public SaveService Save { get; private set; }

    private void Awake()
    {
        // starterFactory: class implements IStarterSaveFactory đọc StarterSettings (S8/B-11); null = SaveData rỗng.
        Save = new SaveService(Application.persistentDataPath, starterFactory);
        SaveLoadResult result = Save.Load();
        if (result.Recovered)
            Debug.LogWarning($"Save phải phục hồi: {result.Kind} – {result.Error}");

        List<string> warnings = SaveSanitizer.Sanitize(
            result.Data,
            heroRoster.Select(hero => hero.id).ToList(),   // roster HeroData thật của project
            starterDeck);                                   // 5 id deck mặc định (có thể null)
        foreach (string warning in warnings) Debug.LogWarning(warning);
        if (warnings.Count > 0) Save.Save();

        DontDestroyOnLoad(gameObject);
    }

    public void AddGems(int amount)
    {
        Save.Data.gems += amount;
        Save.Save();   // save.tmp -> save.json (+ save.bak)
    }
}
```

**Ví dụ implementation `IStarterSaveFactory` (thay bằng `StarterSettings` khi B-11 xong):**

```csharp
public sealed class StarterSaveFactory : IStarterSaveFactory
{
    private readonly StarterSettings _settings;   // S8/B-11 – tên field thật theo class của B

    public StarterSaveFactory(StarterSettings settings) => _settings = settings;

    public SaveData CreateNew()
    {
        var data = new SaveData { gems = _settings.startingGems };
        foreach (HeroData hero in _settings.starterHeroes)
        {
            data.collection[hero.id] = 1;
            data.deck.Add(hero.id);
            data.ownedEver.Add(hero.id);
        }
        return data;
    }
}
```

**Thời điểm lưu (B là người gọi):** sau quay/ghép/đổi deck (S8), khi màn hình kết quả hiện, khi đổi cài đặt, và trong `OnApplicationQuit`. A chỉ cung cấp `SaveService`, không tự lưu hộ.

### 2.2 Hướng dẫn lắp trong Unity

1. **Không cần prefab, component, Settings SO hay layer nào.** `SaveService` là class C# thuần (không phải `MonoBehaviour`), chỉ cần một chỗ giữ instance.
2. **Tạo một lần** ở scene Boot/Lobby (ví dụ `SaveBootstrap` ở mục trên) và `DontDestroyOnLoad`; **không tạo `SaveService` mới ở mỗi scene** (mỗi instance tự giữ `Data` riêng).
3. **Đường dẫn:** `Application.persistentDataPath` trên Windows = `C:\Users\<user>\AppData\LocalLow\<Company>\<Product>`; service tạo `save.json` / `save.bak` / `save.tmp` trong đó. Test/Sandbox có thể trỏ vào thư mục con để không đụng save thật.
4. **asmdef:** `FateBastion.Meta` đã `autoReferenced: true`; B gọi từ assembly khác chỉ cần thêm reference `FateBastion.Meta` nếu assembly đó `autoReferenced: false`. **Không sửa `overrideReferences`/`precompiledReferences`** — Newtonsoft của `com.unity.nuget.newtonsoft-json` đã tự tham chiếu.
5. **Sau `Load()`:** gọi `SaveSanitizer.Sanitize(...)` với roster `HeroData` thật + starter deck; nếu trả cảnh báo thì hiện cho người chơi (S10) và `Save.Save()` một lần.
6. **Debug (B-16):** nút xoá save gọi `SaveService.Delete()` rồi `Load()` để tạo lại từ starter.
7. **Sandbox để xem chạy:** mở `Assets/_Project/Scenes/Sandbox/A_Save.unity`, bấm Play. Các nút: `Load`, `Save`, `+100 gems`, `Sanitize`, `Delete`, `Corrupt save.json` — nút cuối ghi file hỏng để thấy `RecoveredFromBackup`/`CreatedNew` cùng `Error`. Demo ghi vào `persistentDataPath/SandboxSave`, **không đụng save thật**.

### 2.3 Checklist 4.1 cho dòng S9

| # | Điều kiện | Trạng thái |
|---|---|---|
| 1 | PR merge `main` + test xanh | ☐ nhánh chưa mở PR; test S9 gần nhất **13/13 EM pass** (toàn bộ EditMode nhánh: 286/286, đo trước tích hợp rename) |
| 2 | Mục "Cách dùng" | ☑ mục 2.1 (dán vào PR) |
| 3 | "Hướng dẫn lắp trong Unity" | ☑ mục 2.2 (dán vào PR) |
| 4 | Scene `Sandbox/A_*` chạy được | ◐ có `A_Save.unity` + `SaveSandboxDemo.cs` (soạn tay, **chưa mở trong Editor**) |
| 5 | B xác nhận "Đã thử, dùng được" | ☐ chờ B |

## 3. Việc cần người dùng (chủ repo) làm

1. **Mở Unity** kiểm scene `Assets/_Project/Scenes/Sandbox/A_Save.unity` (Play, bấm thử các nút) — nếu Unity báo lỗi YAML/thiếu script thì sửa bằng Editor rồi commit lại `.meta`.
2. **Chạy lại EditMode + PlayMode** trên `feature/S02_A06` và EditMode trên `feature/S09_A14` sau tích hợp `refactor/enemy-asset-names` (xem `daily/day07/status.md`).
3. **Mở PR** cho `feature/S09_A14` (và 3 nhánh ngày 7 còn lại) khi cho phép; dán mục 2.1 + 2.2 vào mô tả PR.
4. **B xác nhận** trong PR (4.1 #5) — thiếu bước này dòng S9 chưa được tính "đã bàn giao".

## 4. Rủi ro của ngày

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Scene `A_Save` soạn tay chưa qua Editor | Trung bình | Mở trong Unity, Play thử; sửa bằng Editor nếu cần |
| Test chưa chạy lại sau tích hợp rename | Trung bình | Chạy lại trước khi merge PR; test đã dùng id mới |
| B chưa xác nhận / chưa nối S8 | Cao (chặn 4.1 #5) | API + Sandbox sẵn; B comment "Đã thử, dùng được" là xong |
| `IStarterSaveFactory` còn stub | Trung bình | Thay bằng `StarterSettings` khi B-11 xong; Sandbox có demo 1000 Ngọc + 5 tướng |
