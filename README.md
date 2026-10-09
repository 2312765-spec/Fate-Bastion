# Fate Bastion (Thủ Thành Vận Mệnh)

Tower Defense 3D góc nhìn thứ ba, PC, Unity 6.3 LTS + URP. Đồ án 3 người, 4 tuần.

Người chơi điều khiển **Triệu Hồi Sư** đi trên map, trả Vàng để đặt và nâng cấp tướng chặn quái đi từ Cổng tới Thành. Tướng lấy từ gacha ngoài trận. Trong trận không có yếu tố ngẫu nhiên.

---

## Cài đặt máy mới (làm đúng thứ tự)

1. **Unity `6000.3.25f1`** (Unity 6.3 LTS) qua Unity Hub, kèm module **Windows Build Support (IL2CPP)**.
   Dùng đúng bản này; bản khác sẽ sửa `ProjectSettings/ProjectVersion.txt` và gây xung đột.
2. **Git LFS** – chạy một lần trên mỗi máy, trước khi clone:
   ```bash
   git lfs install
   ```
3. Clone repo, rồi mở project bằng Unity Hub → **Add project from disk** → chọn **thư mục gốc của repo** (nơi có `Assets/`, `Packages/`, `ProjectSettings/`). Không có thư mục con nào là project.
4. Lần mở đầu Unity import khoảng 3–5 phút.
5. Kiểm tra nhanh: **Window → General → Test Runner → EditMode → Run All**. Hiện tại phải xanh **39/39**.

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

Các field không có trong CSV (`prefab`, `icon`, Ultimate của Long Vương, `hpMultiplier` của màn 2–3) **gắn tay** trong Inspector; importer không ghi đè chúng.

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
| `Docs/TEAM_ASSIGNMENT.md` | **Ai sở hữu thư mục nào** và hợp đồng event giữa A, B, C |
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
