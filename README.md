# Fate Bastion (Thủ Thành Vận Mệnh)

Tower Defense 3D góc nhìn thứ ba, PC, Unity 6.3 LTS + URP. Đồ án 3 người, 4 tuần.

Người chơi điều khiển **Triệu Hồi Sư** đi trên map, trả Vàng để đặt và nâng cấp tướng chặn quái đi từ Cổng tới Thành. Tướng lấy từ gacha ngoài trận. Trong trận không có yếu tố ngẫu nhiên.

---

## Cài đặt máy mới (làm đúng thứ tự, mỗi người làm một lần)

> Bỏ qua một bước là nguồn lỗi phổ biến nhất: ảnh/model thành file 1 KB (thiếu LFS), merge scene hỏng (thiếu UnityYAMLMerge), project mở sai phiên bản.

### Bước 1 – Cài phần mềm

| Phần mềm | Ghi chú |
|---|---|
| **Unity Hub** + **Unity `6000.3.25f1`** | Unity Hub → Installs → Install Editor → Archive → chọn đúng `6000.3.25f1`. Tick module **Windows Build Support (IL2CPP)**. **Không** dùng bản 6.3 khác: Unity sẽ sửa `ProjectSettings/ProjectVersion.txt` và gây xung đột |
| **Git for Windows** | Có sẵn Git Bash và Git LFS. Kiểm tra: `git --version`, `git lfs version` |
| Visual Studio 2022 hoặc Rider | Unity Hub tự gợi ý khi cài Editor |

### Bước 2 – Cấu hình Git (chạy trong **Git Bash**, một lần mỗi máy)

```bash
# 2.1 Bật Git LFS – PHẢI chạy trước khi clone
git lfs install

# 2.2 Tên và email sẽ hiện trong commit (dùng tài khoản GitHub của chính bạn – hội đồng xem lịch sử commit)
git config --global user.name  "Ten Cua Ban"
git config --global user.email "email-github-cua-ban@example.com"
```

### Bước 3 – Clone repo

```bash
git clone https://github.com/2312765-spec/Fate-Bastion.git
cd Fate-Bastion
git lfs pull          # chắc chắn đã tải đủ file ảnh, model, xlsx
```

Kiểm tra: mở `Docs/` – file `.docx`/`.xlsx` phải mở được. Nếu chỉ vài trăm byte và mở ra là chữ `version https://git-lfs...` thì bước 2.1 chưa chạy → chạy `git lfs install` rồi `git lfs pull`.

### Bước 4 – Cấu hình UnityYAMLMerge (chạy **trong thư mục repo**, sau khi clone)

Giúp git merge được file `.unity`, `.prefab`, `.asset`. Cấu hình này nằm trong `.git/config` nên **clone lại là phải chạy lại**.

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
git config --get merge.unityyamlmerge.driver   # phải in ra đúng đường dẫn ở trên
```

Nếu Unity cài ở chỗ khác: Unity Hub → Installs → bánh răng của `6000.3.25f1` → **Show in Explorer**, rồi thay phần trước `/Editor/Data/...` cho đúng. Đường dẫn dùng dấu `/`, giữ nguyên cặp nháy `'...'`.

### Bước 5 – Mở project

1. Unity Hub → **Add → Add project from disk** → chọn **thư mục gốc repo** (nơi có `Assets/`, `Packages/`, `ProjectSettings/`).
2. Lần đầu import khoảng 3–5 phút; Unity tự tải package theo `Packages/manifest.json`.
3. Cảnh báo **"Unity can't verify this package because it doesn't have a signature"** cho **PrimeTween** là bình thường (gói lấy từ OpenUPM, nhóm đã duyệt). Gói nào khác hiện cảnh báo này → báo nhóm.
4. **Window → General → Console**: không có dòng đỏ.

### Bước 6 – Kiểm tra cuối

- **Window → General → Test Runner → EditMode → Run All**: tất cả xanh.
- Mở `Docs/TRACEABILITY.md` mục 3, tick phần của bạn ở dòng **S0-09** và **S0-14**.

### Mỗi ngày làm việc

```bash
git checkout main && git pull            # lấy bản mới nhất
git checkout -b feature/s<số>-<tên>      # mỗi hệ thống một nhánh, vd feature/s1-combat
# ... làm việc, commit nhỏ, message tiếng Anh: "S1: add DamageCalculator"
git push -u origin feature/s<số>-<tên>   # rồi mở Pull Request trên GitHub
```

Trước khi pull/đổi nhánh: **lưu scene và đóng các scene không phải của mình**. Unity đánh dấu thay đổi ở scene/prefab không phải của bạn → `git restore <file>` (discard), không commit.

## Cấu trúc repo

| Đường dẫn | Nội dung |
|---|---|
| `Assets/_Project/Scripts/` | Code, mỗi thư mục một Assembly Definition `FateBastion.*` |
| `Assets/_Project/Data/` | ScriptableObject – `Heroes/`, `Enemies/`, `Waves/`, `Levels/`, `Skills/` do công cụ import sinh ra |
| `Assets/_Project/Tests/` | Test EditMode và PlayMode, một thư mục con mỗi hệ thống |
| `Data/Balance/*.csv` | Số liệu cân bằng xuất từ Excel (ngoài `Assets/`) |
| `Docs/` | GDD, đặc tả `Specs/S0–S11`, phân công, lộ trình, truy vết |

## Dữ liệu cân bằng

Không sửa tay asset trong `Assets/_Project/Data/Heroes|Enemies|Waves|Levels|Skills`.

1. Sửa số trong `Docs/Can bang - Fate Bastion.xlsx`.
2. Save As → **CSV UTF-8** vào `Data/Balance/` (`heroes.csv`, `enemies.csv`, `waves.csv`).
3. Trong Unity: **Tools → Import Balance CSV** → **Import All**.
4. Kết quả phải là `9 HeroData / 9 SkillData / 5 EnemyData / 45 WaveData / 3 LevelData`, `Errors: 0`.

Chạy từ dòng lệnh (đóng Editor trước):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath . -executeMethod FateBastion.Editor.Import.BalanceImportCli.ImportDefault -quit -logFile -
```

Các field không có trong CSV (`prefab`, `icon`, Ultimate của Long Vương, `hpMultiplier`/`displayName`/`enemyTypesPreview` của LevelData) **gắn tay** – ai gắn field nào xem `CLAUDE.md` mục 8 trong Inspector; importer không ghi đè chúng.

## Chạy test

```bash
# Dong Unity Editor truoc. KHONG dung -quit chung voi -runTests.
"C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -

"C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile -
```

## Làm việc trong nhóm — đọc trước khi commit

| File | Đọc để biết |
|---|---|
| `CLAUDE.md` | Quy ước code, quy tắc bắt buộc, hướng phụ thuộc asmdef, event hub dùng chung |
| `Docs/TEAM_ASSIGNMENT.md` | **Ai sở hữu thư mục nào**, hợp đồng event (mục 3), **API giữa các hệ thống (mục 3.1)**, thế nào là "đã bàn giao" (mục 4.1) |
| `Docs/ROADMAP.md` | Lịch 28 ngày, mốc M0–M4, hạn bàn giao |
| `Docs/TRACEABILITY.md` | Việc của mình (A-xx / B-xx / C-xx), checklist PR |
| `Docs/Specs/S<số>.md` | Đặc tả hệ thống đang làm – **đọc trước khi code** |

Luật ngắn gọn:

- Chỉ commit vào thư mục mình sở hữu. Cần sửa vùng người khác → mở issue `[Cần <A/B/C>]`, không tự sửa.
- Vùng dùng chung (`Scripts/Core/**`, các ScriptableObject do importer ghi, mọi `*.asmdef`, `ProjectSettings/`, `Packages/`, `CLAUDE.md`, `Docs/Specs/`) chỉ sửa qua PR cả 3 người duyệt.
- Mỗi hệ thống một nhánh `feature/s<số>-<tên>`; commit message tiếng Anh.
- Không commit file `.unity` của người khác. Unity tự đánh dấu scene không phải của mình → **discard**.
- Luôn commit file `.meta` đi kèm.

## Dùng Claude Code

Mở Claude Code ở gốc repo và nói rõ mình là ai ở đầu phiên:

> Tôi là B. Chỉ sửa file trong vùng của B theo `Docs/TEAM_ASSIGNMENT.md` mục 2. Nếu cần sửa vùng khác, dừng lại và soạn nội dung issue cho tôi.

`CLAUDE.md` được Claude đọc tự động mỗi phiên.
