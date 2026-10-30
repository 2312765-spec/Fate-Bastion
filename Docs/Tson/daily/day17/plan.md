# Kế hoạch – Slot 17 – T-13 / A-13

## Mục tiêu

Hoàn thiện phần A của Thiên thạch: dữ liệu MeteorSettings, aim indicator có kiểm tra tầm, damage theo wave/level, và trúng quái Bay; cung cấp API/handoff để C nối nguồn ray chuột và B/C nối HUD/FX.

## Phạm vi

- Tạo `MeteorSettings` tham chiếu `SkillData` AreaAtPoint/GroundPoint và `EnemyData` Thường; lấy cooldown, thời gian rơi, radius, damage type và hệ số từ `SkillData`, không hardcode cân bằng.
- Thêm công thức thuần `MeteorDamageCalculator` và helper kiểm tra tầm trên XZ; viết EditMode tests wave 1/5/15 và biên tầm.
- Tạo `IMeteorAimRaySource` trong vùng A; `MeteorAimIndicator` nhận ray qua interface, raycast mặt đất, hiển thị vùng tầm/vòng nổ, chỉ xác nhận khi điểm hợp lệ và gọi `MeteorAbility.ConfirmAim`.
- Áp sát thương True qua `DamageInfo`, kiểm tra vùng trên XZ; dùng `OverlapSphereNonAlloc` cho collider và active roster fallback cho quái Bay/đối tượng không có collider; deduplicate tối đa 100 quái.
- Theo dõi wave bằng `GameEvents.OnWaveStarted`; khi trận kết thúc trong lúc rơi thì bỏ impact.
- Không sửa `InputRouter`, scene, prefab hay file vùng C/B. Cần C cung cấp implementation của `IMeteorAimRaySource` và nối vào Player prefab; ghi issue draft/handoff.

## Giả định cần nêu rõ

- `DamageInfo` bắt buộc có `Element`, dù S7 chỉ định True damage và không quy định hệ; dùng Fire làm nhãn event, không gắn status.
- Asset `MeteorSettings`, component/prefab references, ray provider và visual rings được lắp ở Unity trong T-16; tests chỉ được viết, chưa chạy.

## Nhánh và phụ thuộc

- Tiếp tục sau lựa chọn của TVA: `feature/S07_A13` tạo từ `feature/S07_A12`; PR #43 nhắm A-12 để dùng API `MeteorAbility`. **Cập nhật 30/10/2026:** PR #43 đang **Ready**, base `feature/S07_A12`, `CLEAN`; thứ tự merge **#42 → #43**.
- Không tick `TRACEABILITY.md` trước compile/test tích hợp.
