# Kế hoạch – Slot 15 – T-11 / A-11

## Mục tiêu

Triển khai Hào quang Chỉ huy: kiểm tra tướng trong bán kính cấu hình theo chu kỳ 5 Hz, áp buff AttackSpeed khi vào vùng, gỡ khi ra vùng/despawn/disable, và phát event cho hướng dẫn.

## Phạm vi

- Thêm `CommandAuraSettings` (bán kính, bonus AttackSpeed, chu kỳ kiểm tra) và `CommandAura`.
- Expose accessors giới hạn cho registry tướng đang hoạt động từ `HeroController`, không quét scene hoặc tìm GameObject mỗi frame.
- Dùng `HeroStats.SetBuff/RemoveBuff` với nguồn `CommandAura` và loại nguồn `CommandAuraSettings`.
- Phát instance event `OnHeroEnteredAura(GameObject hero)` chỉ khi tướng vừa vào vùng.
- Viết PlayMode tests cho vào/ra và teardown buff; không chạy tests trước T-16.
- Ghi bàn giao cho C gắn component vào Player prefab của C, B nghe event trong tutorial; thao tác Unity và compile dời sang T-16.

## Phụ thuộc / nhánh

- `feature/S07_A11` dựa trên `feature/S06_A10` vì cần HeroController runtime và registry; PR #41 (Ready) nhắm vào nhánh đó, phụ thuộc #40 rồi #46.
- **Cập nhật 30/10/2026:** #38 đã merge `main`, #40 đã rebase về `main`; thứ tự merge là **#46 → #40 → #41**.
- Không sửa prefab/scene của C, UI/tutorial của B, Core, Spec hoặc `TRACEABILITY.md`.
