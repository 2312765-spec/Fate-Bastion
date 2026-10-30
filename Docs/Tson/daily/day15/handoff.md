# Bàn giao – Slot 15 – T-11 / A-11

## 1. Kết quả code

- `CommandAuraSettings` giữ các giá trị cân bằng dưới dạng SO fields/properties: radius, AttackSpeed bonus và scaled check interval.
- `CommandAura` quét `HeroController` active roster mỗi interval, kiểm tra XZ, chỉ thêm/gỡ `HeroStats` buff khi membership đổi, và gỡ mọi buff trên `OnDisable`.
- Nguồn buff là instance `CommandAura`, source type là `CommandAuraSettings`, loại buff `AttackSpeed`; cùng loại aura không cộng trùng khi có nhiều nguồn cùng type theo logic A-09.
- `OnHeroEnteredAura(GameObject)` là event instance; phát một lần khi vào vùng, không phát liên tục khi ở trong.
- Có version khởi tạo trên HeroController để nhận biết pooled hero despawn/respawn kể cả khi cùng component được tái sử dụng.

## 2. Bằng chứng và kiểm thử

- Viết `CommandAuraPlayModeTests` bao gồm vào/ra, event entry, despawn/respawn và gỡ buff khi aura disable.
- Unity MCP standalone standard validation: 0 errors; hai cảnh báo là heuristic string concatenation trong `Update()` và cần được đánh giá lại sau khi compile trong Unity.
- Thao tác Unity/compile được dời sang T-16; kết quả nằm ở mục 5. `main` hiện chưa có `CommandAuraSettings.asset` và bản sửa test của T-16.

## 3. Bàn giao chéo chủ sở hữu

- **Issue draft `[Cần C] Gắn CommandAura vào Player prefab`:** gắn component `FateBastion.Player.CommandAura` lên root Summoner/Player prefab do C sở hữu; kéo `CommandAuraSettings.asset` vào `_settings`; không sửa scene Game hoặc prefab của C từ nhánh A.
- **B cần nối Tutorial bước 3:** subscribe `CommandAura.OnHeroEnteredAura(GameObject hero)` và unsubscribe theo vòng đời view/tutorial. Event chỉ phát khi tướng vừa vào vùng.
- **Asset tại T-16:** `Assets/_Project/Data/Settings/CommandAuraSettings.asset` đã được tạo trên nhánh tích hợp với radius 6, bonus 0.25, check interval 0.2; sau đó C kéo tham chiếu vào prefab. Asset này **chưa có trên `main`**.

## 4. Nhánh và phụ thuộc

- Nhánh `feature/S07_A11` được tạo từ `feature/S06_A10`; PR #41 đã **merge `main`** (`ce578cd`) ngày 30/10/2026.
- **Trạng thái 30/10/2026:** #46 (`15b8bf2`), #40 (`d1e653f`), #41 (`ce578cd`) đều đã merge `main`; phần asset và bản sửa test của T-16 vẫn chỉ nằm trên `feature/A_T16_integration`.
- Không tick `TRACEABILITY.md` cho A-11 khi C chưa gắn `CommandAura` vào Player prefab và B chưa nối tutorial bước 3.

## 5. T-16 – kết quả tích hợp (30/10/2026)

- Trên `feature/A_T16_integration`, Unity MCP đã tạo `CommandAuraSettings.asset` (bán kính 6 m, +25% AttackSpeed, chu kỳ 0.2 s). Asset chưa được gắn vào Player prefab vì prefab thuộc C.
- `CommandAuraPlayModeTests` đã chạy trong suite tích hợp: PlayMode **116/116 pass**, EditMode **675/675 pass**, 0 fail/0 skip (job `87b378136e49444aa9d5c386d4ebc169` và `cb4aede713514d83b261bbeff260396a`).
- Lượt PlayMode đầu tiên fail 1 test vì test đăng ký listener `OnHeroEnteredAura` sau khi aura đã bật. Bản sửa trên nhánh tích hợp đăng ký listener trong `SetUp` trước khi `SetActive(true)`; chạy lại pass.
- **Chưa có trên `main`:** `CommandAuraSettings.asset` và bản sửa `CommandAuraPlayModeTests`. Cần một PR follow-up port từ `feature/A_T16_integration`.
