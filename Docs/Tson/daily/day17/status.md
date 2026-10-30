# Hiện trạng – Slot 17 – T-13 / A-13

## Kết quả

- Thêm `MeteorSettings`, lấy range/query padding riêng và tham chiếu `SkillData` + `EnemyData` Thường; radius/cooldown/cast delay/damage multiplier/type lấy từ `SkillData`.
- Thêm `MeteorDamageCalculator` theo `baseHP × hpGrowth^(wave−1) × hpMultiplier × damageMultiplier`; `MeteorDamageApplier` áp True damage, dùng `OverlapSphereNonAlloc` và bounded active-roster fallback, lọc vùng trên XZ và khử hit trùng.
- Thêm `IMeteorAimRaySource` và `MeteorAimIndicator`: raycast mặt đất, màu xanh/đỏ theo tầm, xác nhận bằng `InputRouter.Select`, marker khi đang rơi, sự kiện cooldown/explosion, bỏ sát thương nếu trận kết thúc trong lúc rơi.
- Đã viết EditMode tests cho damage wave 1/5/15 và range XZ; PlayMode test cho quái Bay + nhiều collider chỉ nhận một hit. Chưa chạy tests.
- Không sửa `InputRouter`/file C; tạo contract interface A để C cung cấp ray camera/input.

## Xác minh

- Chưa chạy Unity compile, Editor, EditMode hay PlayMode tests; validation được giữ lại cho T-16.
- T-16 cần tạo `MeteorSettings.asset`, lắp component/provider/visuals trong prefab do C sở hữu, compile và chạy tests.
- Chưa tick `TRACEABILITY.md`; A-13 chưa được xem là bàn giao xong.

## Nhánh

- `feature/S07_A13` tạo từ `feature/S07_A12` theo lựa chọn tiếp tục của TVA; commit `b0cddcd` (code), `731c0b5` và `91f87fd` (docs) đã push; PR #43 đang mở ở trạng thái **Ready**, base `feature/S07_A12`, `MERGEABLE/CLEAN`.
- **30/10/2026:** #38 đã merge `main`; nhánh A-13 đã merge doc mới nhất của A-12. Thứ tự merge: **#42 → #43**.
- Kế thừa nợ của A-12: fixture `MeteorTestInputRouter` + fix `10d804e` chưa áp; phải xử lý ở #42 trước khi #43 vào `main`.
