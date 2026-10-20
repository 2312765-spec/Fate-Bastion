# Hiện trạng Ngày 7 – 16/10/2026 (cuối ngày)

## Đã xong
- **T-01 (◐ → ☑ sau khi commit):** tạo khu `Docs/Tson`:
  - `README.md` (luật làm việc: folder theo ngày, 3 file/ngày, nhánh `feature/<Sxx>_<mã>`, không commit thẳng `main`, 3 mức ưu tiên, rà soát bằng skill).
  - `plan.md` (22 ngày, mã T-01…T-18, cột ánh xạ TRACEABILITY A-xx; bảng ngày 1–6 đã xong).
  - `status.md`, `handoff.md` (tổng thể).
  - `daily/day07/{plan,status,handoff}.md`.
  - `Docs/SRS.md` – SRS toàn game v1.0 (16/10/2026, IEEE 830, FR/NFR + ma trận R01–R31).
- Cập nhật `CLAUDE.md` + `README.md`: đường dẫn tài liệu cũ → `Docs/root/`, thêm dòng SRS/Tson, thống nhất quy ước nhánh `feature/<Sxx>_<mã>` theo `TRACEABILITY.md` (mục 12 `CLAUDE.md`).

## Chưa xong / dời
- **T-02 (A-14 `SaveData`/`SaveService`):** chưa bắt đầu → **dời sang đầu ngày 8** (vẫn P1). Lý do: dành ngày 7 đóng T-01 cho gọn một PR tài liệu.

## Bằng chứng
- Số test: 0 test mới (ngày tài liệu).
- File tạo: `Docs/Tson/**`, `Docs/SRS.md`; file sửa: `CLAUDE.md`, `README.md`.
- Nhánh: `docs_tson_setup` (đã push).

## Ghi chú
- Kiểm chứng code phục vụ `status.md`: `Heroes/Runtime`, `Player/Abilities`, `Meta/Save` **trống**; S2 thiếu `BossSummonAbility`; `EndlessSettings` là STUB.
- Phát hiện lệch: S0-15 code đã merge (PR #5) nhưng `TRACEABILITY.md` vẫn ☐ → cần PR 3 người duyệt để tick.
