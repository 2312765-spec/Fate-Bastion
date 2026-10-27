# Docs/Tson – Khu làm việc của Thành viên A (Gameplay)

> Đây là **khu tài liệu riêng của A** (Gameplay): kế hoạch 22 ngày, hiện trạng, bàn giao và nhật ký từng ngày. Đọc file này trước mỗi phiên làm việc; mọi luật dưới đây **bắt buộc**.
> **Plan 22 ngày là lịch làm việc cá nhân của A, không phải lịch của nhóm:** nó **không** thay thế và **không** làm thay đổi `Docs/ROADMAP.md`. Thành viên B và C vẫn theo `ROADMAP.md` hiện tại; A làm sớm hơn (hoặc trễ hơn) một số việc theo lịch riêng của mình. Việc A phải chờ tính năng từ người khác thì ghi vào `handoff.md` của A — không đổi mốc/bàn giao chung, không ảnh hưởng công việc của thành viên khác.
> A phụ trách: **S1 Chiến đấu · S2 Quái và Wave · S6 Kỹ năng và Ultimate · S7 Hào quang & Thiên thạch · S9 Lưu trữ · nội dung file cân bằng** (xem `Docs/TEAM_ASSIGNMENT.md` mục 1–2).
> Ngày 1 = 10/10/2026 · Ngày 7 = 16/10/2026 (bắt đầu ghi nhật ký) · **hết plan Ngày 22 = 31/10/2026**.

---

## 1. Cấu trúc khu này

```
Docs/Tson/
├── README.md        ← file này: luật và cách dùng
├── plan.md          ← plan 22 ngày, mã việc T-xx (ánh xạ A-xx của TRACEABILITY)
├── status.md        ← HIỆN TRẠNG TỔNG THỂ (khác file hiện trạng trong ngày)
├── handoff.md       ← BÀN GIAO TỔNG THỂ (khác file handoff trong ngày)
└── daily/
    ├── day07/       ← mỗi ngày một folder: day07, day08, … day22
    │   ├── plan.md      kế hoạch của đúng ngày đó
    │   ├── status.md    hiện trạng cuối ngày đó
    │   └── handoff.md   việc bị chặn / bàn giao phát sinh trong ngày đó
    └── day08/ …
```

**Phân biệt rõ hai cấp tài liệu:**

| Cấp | File | Trả lời câu hỏi |
|---|---|---|
| Tổng thể (`Docs/Tson/*.md`) | `status.md`, `handoff.md` | A đang ở đâu so với cả plan? Còn chặn ai / ai chặn A? |
| Trong ngày (`daily/dayNN/*.md`) | `plan.md`, `status.md`, `handoff.md` | Ngày đó làm gì, xong đến đâu, vướng gì? |

---

## 2. Luật bắt buộc

1. **Mỗi ngày tạo một folder mới** `daily/dayNN` (NN có 2 chữ số: `day07`, `day08`, …). Ngày có làm hay không vẫn tạo, vì đó là mốc để đối chiếu plan.
   - Tạo folder của ngày **vào chính ngày đó** (không dồn); nếu làm trễ, ghi lý do vào `status.md` của ngày.
2. **Công việc không nhất thiết phải xong đúng ngày**, nhưng mọi việc phải có dòng trong `plan.md`/`status.md` để chia nhỏ và rà soát lại dễ dàng. Việc dồn sang ngày khác thì ghi rõ "dời từ dayNN".
3. **Mỗi chức năng làm trên một nhánh riêng**, tên `feature/<Sxx>_<mã>` lấy mã trong `TRACEABILITY.md` (`Sxx` = hệ, mã = `A-xx`/`B-xx`/`C-xx`). Ví dụ: `feature/S02_A05`, `feature/S07_A11`. Việc phát sinh không gắn S đặt tên khác được nhưng phải ngắn gọn, dễ tìm (vd `rebalance`); nội dung để trong mô tả PR, không tràn vào tên nhánh/tiêu đề.
   - **Không commit trực tiếp lên `main`.** Muốn sửa (kể cả sửa nhỏ) thì tạo nhánh mới rồi mở PR.
   - Nhánh phụ trợ tài liệu đặt tên `docs/<tên>` (vd `docs_tson_setup` là nhánh cũ, giữ nguyên), nhánh sửa lỗi `fix/<mô tả>`.
   - Việc thuộc 2 hệ (vd A-07 = S1 + S6): đặt theo hệ chính, vd A-07 → `feature/S06_A07`; plan trong ngày tách việc rõ để biết phần nào làm trước. Chỉ tách thành 2 nhánh khi hai phần thật sự độc lập. **Không dùng nhánh cha/nhánh con** — mỗi việc PR thẳng vào `main`.
   - Đây là **quy ước chung** ghi ở `CLAUDE.md` mục 12 (chốt trong PR #14); nhánh cũ sai chuẩn giữ nguyên đến khi mở lại việc đó.
   - **Trần nhánh mỗi ngày: 3 nhánh.** Nếu một ngày cần nhiều việc rời hơn (việc phát sinh, tài liệu, công cụ) mà không gộp được vào 3 nhánh đó, **tách thêm một folder ngày riêng** (`daily/dayNN`) để giữ mỗi ngày gọn — không nhồi quá nhiều nhánh vào một ngày.
   - **Cập nhật tài liệu theo nhánh:** sửa `daily/dayNN`/tổng thể **trên chính nhánh đang làm**, rồi **làm tươi (refresh) lúc mở PR**. Tài liệu **tổng thể** (`Docs/Tson/status.md`, `handoff.md`) chỉ chốt khi nhánh đó **merge vào `main`** (không còn tầng nhánh cha nên mỗi PR là một lần chốt).
4. **Thứ tự ưu tiên khi chọn việc** (áp dụng trong ngày và trong cả plan):
   - **P1 – Không cần thao tác/tạo asset trên Unity:** class thuần, logic, test EditMode, đọc/ghi file, đọc dữ liệu. Làm trước.
   - **P2 – Cần thao tác Unity:** gắn component, prefab, dựng/thử scene Sandbox, test PlayMode, import asset, chỉnh Inspector.
   - **P3 – Mâu thuẫn/bàn giao giữa các thành viên:** việc cần người khác xong hoặc cần duyệt chéo; làm sau cùng và phải mở handoff.
   - **Ngoại lệ:** phụ thuộc kỹ thuật thắng ưu tiên. Việc P1 mà cần một class P2 chưa có thì phải làm P2 đó trước; ghi lý do vào `plan.md`.
5. **Mỗi hệ thống chỉ "xong" khi đạt hết Tiêu chí hoàn thành trong Spec** (`Docs/Specs/S*.md`), có test hoặc hướng dẫn kiểm tra tay, và tick dòng tương ứng ở `TRACEABILITY.md` mục 4.
6. **Handoff chỉ tính xong khi đủ 5 điều** ở `TEAM_ASSIGNMENT.md` mục 4.1 (merge main + test xanh, mục "Cách dùng", "Hướng dẫn lắp trong Unity", scene Sandbox chạy được, người nhận xác nhận). Việc chờ người khác phải nằm trong `handoff.md`.
7. **Không sửa vùng của B/C** (`TEAM_ASSIGNMENT.md` mục 2). Cần thì mở issue `[Cần <A/B/C>]`, đồng thời dùng stub đúng tên API ở `TEAM_ASSIGNMENT.md` mục 3.1, đánh dấu `// STUB:`.
8. **Cập nhật đủ 3 chỗ khi có thay đổi:** file trong ngày (`daily/dayNN`), file tổng thể (`status.md`/`handoff.md`), và tick `TRACEABILITY.md` mục 4 (dòng của A).
9. **MCP timeout – ngưỡng chờ và handoff:** thao tác Unity thông thường (ví dụ thêm component hoặc vào Play Mode) có thể thực hiện trực tiếp; riêng tác vụ lâu như chạy test/build/import phải theo dõi thời gian chờ cho cùng một job.
   - Nếu cùng một job timeout khoảng **3 lần** và tổng thời gian chờ đã vượt **180 giây** (ví dụ các lượt chờ 60 s + 90 s + lượt timeout tiếp theo), dừng poll/retry job đó; không khởi chạy lại test từ đầu. Chuyển sang việc P1 không cần Unity.
   - Trước khi dừng, ghi lại **job id**, thời lượng từng lượt và tổng thời gian chờ, trạng thái/kết quả cuối nhận được (số test xong/tổng, test đang chạy nếu có), lệnh hoặc cách tiếp tục chính xác, và điều kiện xác nhận hoàn tất. Nếu chưa có job id hoặc tiến độ thì ghi rõ "chưa nhận được", không tự suy đoán.
   - Nếu phần còn lại trong ngày chỉ là test hoặc tác vụ Unity đang timeout, ghi handoff vào **`daily/dayNN/handoff.md`** và **`Docs/Tson/handoff.md`**, rồi làm việc không cần Unity khác. Khi Unity/job sẵn sàng mới tiếp tục đúng job đã ghi; cập nhật `daily/dayNN/status.md` sau đó. Timeout không phải test fail và không được ghi là đã chạy xong.

---

## 3. Quy trình một ngày (làm đúng thứ tự)

1. Mở `plan.md` — xem việc dự kiến của ngày hôm nay (mã T-xx).
2. Tạo `daily/dayNN/plan.md` — chốt việc thực tế sẽ làm hôm nay (có thể khác plan nếu dời), ghi ưu tiên P1/P2/P3.
3. Tạo nhánh `feature/<Sxx>_<mã>` từ `main` mới nhất cho từng việc; làm; commit nhỏ (message tiếng Anh, vd `S6: add SkillRunner for MeleeHit`).
4. Cuối ngày, cập nhật `daily/dayNN/status.md` (đã xong gì, còn gì, số test) và `daily/dayNN/handoff.md` (việc bị chặn, cần ai).
5. Cập nhật `status.md` và `handoff.md` (tổng thể).
6. Chạy lượt **rà soát** (mục 5) trước khi kết thúc ngày.

---

## 4. Ký hiệu dùng thống nhất

| Ký hiệu | Nghĩa |
|---|---|
| ☐ / ◐ / ☑ | Chưa làm / đang làm / xong |
| P1 / P2 / P3 | Ưu tiên: không Unity / cần Unity / bàn giao–mâu thuẫn |
| T-xx | Mã việc trong `plan.md` (đánh lại) |
| A-xx | Mã việc gốc trong `Docs/TRACEABILITY.md` mục 4 |
| BLOCKED | Việc không tiến được vì chờ người/việc khác |
| `// STUB:` | Đoạn chờ thay bằng hệ thống thật |
| `// ASSUMPTION:` | Giả định khi Spec chưa rõ |

---

## 5. Rà soát (bắt buộc, dùng skill của agent)

Hai skill nằm ở `.opencode/skills/` (**local, không commit** — mỗi máy tự có):

| Skill | Dùng khi | Kiểm tra |
|---|---|---|
| `tson-member-audit` | Muốn rà soát công việc của **một thành viên** (A/B/C) | Tiến độ theo `TRACEABILITY.md`, hiện trạng trong `Docs/Tson`, code/test thực tế, việc trễ/bị chặn |
| `tson-project-audit` | Rà soát **toàn dự án** | Đồng bộ `Docs/Tson` ↔ `TRACEABILITY.md` ↔ `ROADMAP.md` ↔ code; file ngày còn thiếu; stub/giả định còn lại; đường dẫn tài liệu sai |

Cách gọi trong phiên: nói rõ, ví dụ *"dùng skill tson-member-audit rà soát thành viên A"* hoặc *"dùng skill tson-project-audit rà soát toàn dự án"*. Kết quả rà soát phải dẫn tới cập nhật `status.md`/`handoff.md` nếu có sai lệch.

---

## 6. Cấu trúc một file trong ngày

**`daily/dayNN/plan.md`** — bảng việc dự kiến:

| Mã | Việc | Mã TRACEABILITY | Ưu tiên | Nhánh | Kiểm tra | Trạng thái |
|---|---|---|---|---|---|---|
| T-02 | `SaveData`, `SaveService` ghi an toàn | A-14 | P1 | `feature_a14_save` | EM | ◐ |

**`daily/dayNN/status.md`** — kết thúc ngày: việc đã xong, việc dở (dời sang ngày nào), số test thêm, file đã tạo/sửa, ghi chú.

**`daily/dayNN/handoff.md`** — việc bị chặn: chờ ai, vì sao, dùng stub gì để tiếp, và việc A đã giao cho ai trong ngày.

---

## 7. Liên kết

| File | Dùng để |
|---|---|
| `Docs/Tson/plan.md` | Plan 22 ngày, mã T-xx |
| `Docs/Tson/status.md` | Hiện trạng tổng thể |
| `Docs/Tson/handoff.md` | Bàn giao tổng thể |
| `Docs/TRACEABILITY.md` | Mã việc gốc A-xx, tiêu chí kiểm tra |
| `Docs/Specs/S1|S2|S6|S7|S9.md` | Đặc tả kỹ thuật hệ thống A làm |
| `Docs/SRS.md` | Yêu cầu FR/NFR toàn game (mã FR-xx) |
| `Docs/TEAM_ASSIGNMENT.md` | Vùng sở hữu, hợp đồng API (mục 3.1), định nghĩa "đã bàn giao" (4.1) |
| `CLAUDE.md` | Quy ước code và quy tắc bắt buộc |

---

## 8. Việc của ngày 1–6 (đã xong trước khi bắt đầu nhật ký)

A được phân công từ Ngày 1 (10/10) nhưng bận, đã nhờ **C (nhóm trưởng)** làm hộ phần đầu. Từ **Ngày 7 (16/10)** A tự ghi nhật ký. Trạng thái ngày 1–6 ghi trong `plan.md` (bảng ngày 1–6) và `status.md`.

- S0 dùng chung (Cả nhóm): enum, Data SO, Core event hub, Import Balance CSV — ☑
- A-01 `DamageCalculator` (Ngày 3) — ☑
- A-02 `StatusEffectController` (Ngày 4) — ☑
- A-03 chết một lần/đạn tới vị trí cuối/XZ (Ngày 4) — ☑
- A-04 `EnemyController`, `EnemyManager`, `WaveManager` (Ngày 5) — ☑
- Việc A còn nợ từ Ngày 6: A-05 (quái Bay, boss gọi con), A-06 (máu theo `hpMultiplier`, dữ liệu 3 màn + Vô tận) → đưa vào plan 22 ngày (day18–19).
