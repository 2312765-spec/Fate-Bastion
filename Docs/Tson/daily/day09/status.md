# Hiện trạng Ngày 9 – 18/10/2026

> Được cập nhật bổ sung ngày 27/10/2026 theo yêu cầu. Đây là nhật ký triển khai, không phải ghi nhận giả rằng công việc đã hoàn thành ngày 18/10.
> Thành viên A – T-05/A-07.

## Tiến độ

- **Đầu việc:** `HeroController`, `HeroStats`, `TargetingModule` theo `Docs/Specs/S1.md`, `S6.md`, `S0b.md` và API `TEAM_ASSIGNMENT.md` 3.1 #3/#4.
- **Thứ tự bắt buộc:** logic thuần + EditMode tests trước; thao tác Unity/Play Mode sau; testing dài chạy cuối.
- **Timeout:** quy tắc `README.md` mục 2 #9 đã được làm rõ: cùng job, khoảng 3 lần timeout và tổng chờ >180 giây thì dừng poll/retry, ghi job + tiến độ vào handoff và chuyển sang việc không cần Unity.
- **Tình trạng lúc bắt đầu:** chưa có code A-07 trong `Heroes/Runtime/`; chỉ có `.gitkeep`.
- **Quyết định dữ liệu:** người dùng đã chọn `HeroStatsSettings` ScriptableObject do A sở hữu làm nguồn runtime cho hệ số độ hiếm; không sửa `HeroData`, CSV hoặc importer.

## Thay đổi và xác minh

- Cập nhật quy tắc timeout tại `Docs/Tson/README.md` mục 2 #9.
- P1: đã tạo `HeroStatsSettings`, `HeroStats`, `TargetingModule`, `HeroController` và ba test fixture cho EditMode/PlayMode.
- `dotnet build Fate-Bastion.slnx --no-restore`: thành công, 0 warning, 0 error.
- P2 qua Unity MCP: đã tạo/gán `HeroStatsSettings.asset`, tạo `Hero_Base` với `HeroStats`, `HeroController`, `CapsuleCollider`, `ModelRoot`, và tạo scene `A_Heroes`.
- EditMode: **8/8 pass**. Lượt đầu 7/8 do fixture chưa expect log validation `OnValidate`; đã bổ sung `LogAssert.Expect` và chạy lại thành công.
- PlayMode: **8/8 pass**; prewarm test enemies để lượt xác minh cuối không phát cảnh báo pool-growth.
- Không có test timeout. Unity Console chỉ ghi cảnh báo WebSocket của MCP (`WebSocket is not initialised`), không có lỗi biên dịch.
- Tạo nhánh `feature/S06_A07` từ `origin/main` trong worktree riêng; các file A-07 đã được cô lập, chưa commit/push/mở PR.

## Còn lại

1. Rà soát diff cuối, commit/push nhánh `feature/S06_A07` và mở PR; không đưa thay đổi đang dirty khác của `feature/S09_A14` vào PR.
2. Sau khi PR được merge/review, cập nhật trạng thái A-07 và tick TRACEABILITY theo quy trình nhóm.
