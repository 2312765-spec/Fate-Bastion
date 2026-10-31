# Kế hoạch – Slot 20 – T-16 / Tích hợp và validation

## Mục tiêu

Tạo một nhánh validation độc lập từ `origin/main`, tích hợp tạm các PR A/B/C đang mở theo thứ tự dependency để compile/test trong Unity; không merge PR nguồn vào `main`.

## Phạm vi

- Đã có xác nhận TVA cho phép tích hợp draft PR vào nhánh validation riêng; giữ các PR gốc open/draft.
- Merge thứ tự: A-08 #38 → A-09 #39 → A-10/A-11 #40/#41 → A-12/A-13 #42/#43; B-S4 #31 → #33; B-S5 rewards #32 → GameManager #35; C feedback/FX #30.
- Resolve chỉ xung đột tích hợp có bằng chứng; không sửa scene/prefab thuộc C hoặc UI thuộc B.
- Unity refresh/compile; chạy suites đã hoãn: S6/S7, regression S2 và các suite S4/S5/S11 liên quan.
- Tạo các settings asset thuộc A nếu Editor hoạt động; ghi handoff để C gắn component/asset vào prefab Player/Hero do C sở hữu.
- Chuẩn bị checklist thao tác tay 3 màn; không tuyên bố playtest nếu không có scene Game hoàn chỉnh và C xác nhận.

## Quy tắc branch

- Nhánh `feature/A_T16_integration` dựa trên `origin/main`, chỉ dùng để xác nhận tương thích.
- Không đóng, sửa base hoặc merge các PR nguồn vào `main`; ghi rõ test run nào được chạy trên nhánh tích hợp này.
- Nếu merge/test thất bại, giữ nguyên branch sources và ghi blocker; không reset hoặc xóa dữ liệu.
