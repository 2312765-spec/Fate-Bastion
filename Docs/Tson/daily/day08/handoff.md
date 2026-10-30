# Bàn giao – Ngày 8 – 17/10/2026

> Nhánh: `feature/S09_A14`. Tài liệu Ngày 8 chỉ nằm trên nhánh này (tài liệu Ngày 7 vẫn giống nhau trên cả 4 nhánh).
> Nội dung dưới đây là **bộ hồ sơ bàn giao S9 cho B**, đã được đưa vào mô tả [PR #23](https://github.com/2312765-spec/Fate-Bastion/pull/23) (điều kiện 4.1 #2/#3).

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
| 1 | PR merge `main` + test xanh | ◐ test **xanh hết**: `feature/S09_A14` 299/299 EM + 45/45 PM; `feature/S02_A06` 298/298 EM + 50/50 PM. Lượt toàn Editor 1912 test có 3 fail đều là test package `com.besty.unity-skills`, không có test `FateBastion.*` đỏ. **PR #23 đã mở, chưa merge** _(cập nhật 30/10/2026: đã merge `main` qua `7f02219`)_ |
| 2 | Mục "Cách dùng" | ☑ mục 2.1 (dán vào PR) |
| 3 | "Hướng dẫn lắp trong Unity" | ☑ mục 2.2 (dán vào PR) |
| 4 | Scene `Sandbox/A_*` chạy được | ☑ `A_Save.unity` + `SaveSandboxDemo.cs`: đã mở trong Editor (3 root đúng), Play ~4 s không exception, tạo `persistentDataPath/SandboxSave` |
| 5 | B xác nhận "Đã thử, dùng được" | ☐ chờ B |

## 3. Việc cần người dùng (chủ repo) làm

1. PR #23 cho `feature/S09_A14` và PR #21 (`docs/agent-tools`), #22 (`feature/S02_A06`), #24 (`feature/S02_A05`) đã mở; **chưa PR nào được merge**.
2. Đề xuất merge theo thứ tự #21 → #24 → #22 → #23 vì các nhánh kế thừa commits chung; sau đó kiểm tra lại diff/checks.
3. **B xác nhận** trong PR #23 (4.1 #5) — thiếu bước này dòng S9 chưa được tính "đã bàn giao".
4. **Test:** đã xanh hết theo kết quả ở mục 5 — không cần chạy lại trừ khi sửa code.

## 4. Rủi ro của ngày

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Scene `A_Save` soạn tay chưa qua Editor | Trung bình | Mở trong Unity, Play thử; sửa bằng Editor nếu cần |
| ~~Test chưa chạy lại sau tích hợp rename~~ | — | **Đã hết:** `S09_A14` 299/299 EM + 45/45 PM; `S02_A06` 298/298 EM + 50/50 PM (mục 5) |
| B chưa xác nhận / chưa nối S8 | Cao (chặn 4.1 #5) | API + Sandbox sẵn; B comment "Đã thử, dùng được" là xong |
| `IStarterSaveFactory` còn stub | Trung bình | Thay bằng `StarterSettings` khi B-11 xong; Sandbox có demo 1000 Ngọc + 5 tướng |

## 5. Chạy tiếp khi Unity rảnh (MCP timeout – luật 9 `README.md`)

> Ghi theo luật **MCP timeout**: bỏ lượt chờ, làm việc khác, để đây chạy tiếp. **Không** coi các mục dưới là đã xong.

| Việc | Trạng thái cuối | Cách chạy tiếp | Xong khi |
|---|---|---|---|
| EditMode lọc `FateBastion.Tests.EditMode` trên `feature/S09_A14` – job `114c9205c6cb4f7bb1c8026d079ce1b6` | **Xong:** 299/299 pass, 0 fail, 0 skip (93,5 s) — lấy được kết quả ở lần hỏi thứ 3 | Không cần chạy lại (chỉ chạy lại nếu sửa code) | ✅ |
| PlayMode lọc `FateBastion.Tests.PlayMode` trên `feature/S09_A14` – job `d0b86b63452c4ae684406d3279ad40a9` | **Xong:** 45/45 pass, 0 fail, 0 skip (9,2 s) | Không cần chạy lại (chỉ chạy lại nếu sửa code) | ✅ |
| EditMode + PlayMode `feature/S02_A06` sau tích hợp `refactor/enemy-asset-names` | **Xong:** EditMode 298/298 + PlayMode 50/50 pass (0 fail) — job `7e3ee7cea45e4cb3a9b6f4d290ed8cdb` / `f9c0653071824babab00f5e38cba3321` | Không cần chạy lại | ✅ |
| EditMode toàn Editor (đã xong) | `1912` test: **1840 pass**, `3` fail (đều `UnitySkills.Tests.Core.*` của package `com.besty.unity-skills`), `69` skip, 751 s | Không cần chạy lại; 3 fail thuộc tool, ngoài repo | — |
