# S0b. HeroVisual và công cụ kiểm tra hình ảnh tướng

> Bổ sung sau v1.3 để tách **hình ảnh** khỏi **hành vi** của tướng. Khi thiết kế thay đổi: sửa file này trước, rồi mới sửa code. Chủ: **C**. Cần có trước: S0. Hạn bàn giao: Ngày 3 (`Docs/ROADMAP.md` mục 3).

**Mục tiêu:** đổi ngoại hình một tướng (model, animation, điểm bắn, vị trí thanh máu) **không phải sửa dòng code nào và không sửa `HeroData`** (R29). Code gameplay của A chỉ biết đến một hợp đồng duy nhất là `HeroVisual`; nếu model thiếu điểm neo thì game vẫn chạy và báo lỗi rõ ràng, không crash.

Vấn đề cần tránh: A viết `transform.Find("RightHand")` hoặc tìm xương theo tên. Lúc đó C thay model của Mixamo bằng model Quaternius là đạn bắn ra từ gốc chân, hoặc `NullReferenceException`, và lỗi chỉ hiện khi chơi.

---

## Dữ liệu

Không có ScriptableObject mới. `HeroVisual` là **component**, sống trong prefab, không phải data asset.

| Property (chỉ đọc) | Kiểu | Ý nghĩa | Bắt buộc |
|---|---|---|---|
| `Muzzle` | `Transform` | Điểm sinh đạn và điểm bắt đầu hiệu ứng tung phép (đầu cung, bàn tay, miệng rồng) | Có |
| `Overhead` | `Transform` | Điểm neo thanh máu, nhãn tên, số sát thương – nằm trên đỉnh đầu | Có |
| `AuraAnchor` | `Transform` | Điểm neo vòng hào quang độ hiếm và hiệu ứng cấp 3 – nằm dưới chân, sát mặt đất | Có |
| `Animator` | `Animator` | Animator của model; `HeroController` đổi tốc độ clip theo tốc đánh | Có |

Mỗi property đọc một field `[SerializeField] private` (`_muzzle`, `_overhead`, `_auraAnchor`, `_animator`) theo quy ước `CLAUDE.md` mục 5: gắn trong Inspector, code khác chỉ đọc, không gán được.

`HeroData.icon` được công cụ **Render Hero Icons** ghi (xem dưới). Các field hình ảnh khác của `HeroData` (`prefab`) vẫn gắn tay.

---

## Thành phần

| Thành phần | Vùng file | Nội dung |
|---|---|---|
| `HeroVisual` | `Scripts/Heroes/Visual/` | MonoBehaviour giữ 4 tham chiếu trên; `OnValidate` cảnh báo field còn trống |
| Tools → Validate Hero Prefabs | `Scripts/Editor/Heroes/` | Quét mọi prefab tướng, báo prefab nào thiếu `HeroVisual` hoặc thiếu field |
| Tools → Render Hero Icons | `Scripts/Editor/Heroes/` | Render từng prefab tướng ra PNG 256×256 và gán vào `HeroData.icon` |
| `Prefabs/Heroes/Hero_Base.prefab` | chủ **A** | Component hành vi (`HeroController`, `HeroStats`, collider), có GameObject rỗng `ModelRoot` |
| `Prefabs/Heroes/Hero_<Tên>.prefab` | chủ **C** | Prefab Variant của `Hero_Base`: model đặt dưới `ModelRoot`, `HeroVisual` gắn ở gốc model |

---

## Luồng hoạt động

**Khi thêm hoặc đổi model một tướng (việc của C, không cần A):**

1. Tạo Prefab Variant của `Hero_Base`, đặt tên `Hero_<Tên>`.
2. Kéo model vào làm con của `ModelRoot`; xóa model cũ nếu có.
3. Gắn `HeroVisual` lên **gốc của model**, tạo 3 GameObject rỗng `Muzzle`, `Overhead`, `AuraAnchor` đặt đúng vị trí, kéo vào 3 field; kéo `Animator` của model vào field `animator`.
4. Chạy **Tools → Validate Hero Prefabs**, sửa đến khi không còn lỗi.
5. Gán prefab vào `HeroData.prefab` của tướng tương ứng.

**Khi chạy game (code của A):**

1. `HeroController.Awake()` gọi `GetComponentInChildren<HeroVisual>()` **một lần** và cache lại. Không gọi trong `Update`, không `transform.Find`.
2. Bắn đạn, tung phép → dùng `visual.Muzzle.position`.
3. Thanh máu, nhãn, số sát thương → neo vào `visual.Overhead`.
4. Hào quang độ hiếm, hiệu ứng cấp 3 → neo vào `visual.AuraAnchor`.
5. Animation → qua `visual.Animator`.

---

## Quy tắc

- **Code gameplay không bao giờ đi vào bên trong model:** không `transform.Find`, không tìm theo tên xương, không `GetChild(i)` theo chỉ số. Mọi truy cập đi qua `HeroVisual`.
- `HeroVisual` **chỉ giữ tham chiếu**, không có logic gameplay, không `Update`.
- Dùng cùng một hợp đồng cho quái (`Enemy_Base` + Variant) nếu cần điểm neo; S0b chỉ bắt buộc cho tướng.
- Công cụ Editor bọc trong asmdef `FateBastion.Editor` (chỉ Editor), không vào bản build.

**Validate Hero Prefabs** – với mỗi `HeroData` trong `Data/Heroes/`:

| Kiểm tra | Mức |
|---|---|
| `HeroData.prefab` chưa gán | Lỗi |
| Prefab không có `HeroVisual` trong cây con | Lỗi |
| `Muzzle` / `Overhead` / `AuraAnchor` / `Animator` còn trống | Lỗi, ghi rõ field nào |
| `HeroData.icon` chưa gán | Cảnh báo |
| Prefab tướng không nằm trong `Prefabs/Heroes/` | Cảnh báo |

Kết quả in ra một bảng tổng kết kèm số lỗi và số cảnh báo, mỗi dòng ghi `<id>` và lý do; click vào dòng chọn được asset trong Project.

**Render Hero Icons:**

- Render từng `HeroData.prefab` trên nền trong suốt, kích thước **256×256**, lưu `Art/Icons/<id>.png` (Sprite, không nén mất chất lượng).
- Gán ảnh vào `HeroData.icon`. Chạy lại thì **ghi đè đúng file cũ**, không tạo file thứ hai.
- Góc camera, khoảng cách và ánh sáng để trong một asset cài đặt ở `Data/Settings/` để 9 icon trông đồng bộ.

---

## Trường hợp đặc biệt

- **Thiếu `HeroVisual` lúc chạy:** `HeroController` log `LogError` kèm tên prefab, rồi dùng `transform.position` của tướng thay cho cả 3 điểm neo. Game không crash, nhưng Validate phải bắt được từ Editor.
- **Model có sẵn Animator ở gốc prefab thay vì gốc model:** vẫn hợp lệ, miễn ô `Animator` trỏ đúng.
- **Model lệch tỉ lệ hoặc lệch hướng:** chỉnh scale và rotation ở `ModelRoot`, không chỉnh ở gốc prefab (gốc prefab là hệ quy chiếu của gameplay: tầm đánh, bán kính chiếm chỗ).
- **Đổi model sau khi đã đặt tướng trong scene thử:** Prefab Variant tự cập nhật, không cần đặt lại.
- **Tướng Legendary có hiệu ứng riêng:** vẫn neo vào `AuraAnchor`, không thêm field mới vào `HeroVisual`.

---

## Tiêu chí hoàn thành

- ☐ `HeroVisual` có 4 field, `OnValidate` cảnh báo khi còn trống.
- ☐ `grep -rn "transform.Find" Assets/_Project/Scripts` không có kết quả nào trong code gameplay.
- ☐ Thay model của một tướng bằng model khác, chạy lại game: đạn ra từ đúng tay, thanh máu đúng trên đầu, **không sửa dòng code nào**.
- ☐ Tools → Validate Hero Prefabs: cố tình bỏ trống `Muzzle` của 1 prefab → báo đúng prefab và đúng tên field; gắn lại → báo 0 lỗi.
- ☐ Tools → Render Hero Icons: tạo đủ 9 PNG 256×256, gán vào `HeroData.icon`; chạy lần 2 không tạo thêm file.
- ☐ Xóa `HeroVisual` khỏi 1 prefab rồi chơi thử: có `LogError` rõ ràng, game không crash.

---

**Prompt gợi ý cho Claude:**

> Tôi là C. Đọc CLAUDE.md và Docs/Specs/S0b.md. Triển khai `HeroVisual` trong `Scripts/Heroes/Visual/` và hai công cụ Editor `Tools → Validate Hero Prefabs`, `Tools → Render Hero Icons` trong `Scripts/Editor/Heroes/`. Phần tách logic kiểm tra ra class thuần để viết test EditMode; phần render icon viết hướng dẫn kiểm tra tay. Chỉ sửa file trong vùng của C theo Docs/TEAM_ASSIGNMENT.md mục 2.
