# Bàn giao – Slot 15 – T-11 / A-11

## 1. Kết quả code

- `CommandAuraSettings` giữ các giá trị cân bằng dưới dạng SO fields/properties: radius, AttackSpeed bonus và scaled check interval.
- `CommandAura` quét `HeroController` active roster mỗi interval, kiểm tra XZ, chỉ thêm/gỡ `HeroStats` buff khi membership đổi, và gỡ mọi buff trên `OnDisable`.
- Nguồn buff là instance `CommandAura`, source type là `CommandAuraSettings`, loại buff `AttackSpeed`; cùng loại aura không cộng trùng khi có nhiều nguồn cùng type theo logic A-09.
- `OnHeroEnteredAura(GameObject)` là event instance; phát một lần khi vào vùng, không phát liên tục khi ở trong.
- Có version khởi tạo trên HeroController để nhận biết pooled hero despawn/respawn kể cả khi cùng component được tái sử dụng.

## 2. Bằng chứng và kiểm thử

- Viết `CommandAuraPlayModeTests` bao gồm vào/ra, event entry, despawn/respawn và gỡ buff khi aura disable.
- Unity MCP standalone standard validation: 0 errors; hai cảnh báo là heuristic string concatenation trong `Update()` và cần được đánh giá lại sau khi compile trong Unity. Chưa có Editor compile hoặc test run.
- Không chạy EditMode/PlayMode tests và không xác nhận compile theo lựa chọn code-only; toàn bộ validation Unity dời sang T-16.
- T-16 cần xác nhận compile sau khi A-10/A-11 được import cùng A-08/A-09. Kiểm tra asset setting, component/prefab reference và thao tác thử in/out trong Sandbox.

## 3. Bàn giao chéo chủ sở hữu

- **Issue draft `[Cần C] Gắn CommandAura vào Player prefab`:** gắn component `FateBastion.Player.CommandAura` lên root Summoner/Player prefab do C sở hữu; kéo `CommandAuraSettings.asset` vào `_settings`; không sửa scene Game hoặc prefab của C từ nhánh A.
- **B cần nối Tutorial bước 3:** subscribe `CommandAura.OnHeroEnteredAura(GameObject hero)` và unsubscribe theo vòng đời view/tutorial. Event chỉ phát khi tướng vừa vào vùng.
- **Cần tạo asset ở T-16:** `Assets/_Project/Data/Settings/CommandAuraSettings.asset` với radius 6, bonus 0.25, check interval 0.2; sau đó C kéo tham chiếu vào prefab.

## 4. Nhánh và phụ thuộc

- Nhánh `feature/S07_A11` được tạo từ `feature/S06_A10`; PR #41 đang **Ready**, base `feature/S06_A10`, `MERGEABLE/CLEAN`.
- **Trạng thái 30/10/2026:** #38 đã merge `main`; #40 đã rebase về `main`; A-09 đi vào `main` qua #46. Thứ tự merge: **#46 → #40 → #41**.
- Tests hoãn; không tick `TRACEABILITY.md` khi chưa compile/test và chưa có C/B handoff.
