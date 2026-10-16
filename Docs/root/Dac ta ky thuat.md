# **Đặc tả kỹ thuật** 

Phiên bản v1.3 (05/10/2026), khớp GDD v1.3. Tab này mô tả từng hệ thống ở mức "làm trong Unity ra sao" cho **bản rút gọn 1 tháng** , sắp theo thứ tự Claude nên triển khai. Mỗi hệ thống viết theo cùng khung: Mục tiêu · Dữ liệu · Thành phần · Luồng hoạt động · Quy tắc và công thức · Trường hợp đặc biệt · Tiêu chí hoàn thành · Prompt gợi ý cho Claude. 

## **Thứ tự triển khai** 

Hệ thống sau dùng lại hệ thống trước, nhưng **3 người làm song song** : ngày 1–2 cả nhóm chốt interface và dữ liệu dùng chung (S0), sau đó mỗi người code trên stub của người khác. Một hệ thống chỉ được merge vào main khi đạt hết Tiêu chí hoàn thành. 

|Thứ tự|Hệ thống|Tuần|Cần có trước|Phụ<br>trách|
|---|---|---|---|---|
|S0|Interface và dữ liệu dùng chung|Ngày 1–2|—|Cả nhóm|
|S1|Chiến đấu và sát thương|1|S0|A|
|S2|Quái và Wave|1|S1|A|
|S3|Triệu Hồi Sư và Camera|1|S0 (song song S1)|C|
|S4|Đặt tướng và nâng cấp trong trận|1 → 2|S0, S3|B|
|S5|Luồng trận (GameManager)|1 → 2|S2, S4|B|
|S6|Kỹ năng và Ultimate|2|S1|A|
|S7|Hào quang Chỉ huy và Thiên thạch|2|S3, S6|A|
|S8|Gacha, Túi, Ghép, Deck|3|S0|B|
|S9|Lưu trữ|3|S8|A, B|
|S10|HUD và UI|2 → 3 (làm<br>dần)|Event của S1–S9|B, C|
|S11|Âm thanh và phản hồi|3|S1–S10|C|



Phụ trách theo phân vai ở mục 10 tab chính: A Gameplay, B Hệ thống và UI, C Art và kỹ thuật hình ảnh. **Feature freeze cuối ngày 21** : sau mốc này chỉ sửa lỗi và cân bằng. 

**S0 – Interface và dữ liệu dùng chung (ngày 1–2):** <mark>HeroData</mark> , <mark>EnemyData</mark> , <mark>WaveData</mark> , <mark>LevelData</mark> , <mark>SkillData</mark> , enum <mark>Rarity</mark> / <mark>Element</mark> / <mark>Role</mark> / <mark>DamageType</mark> , <mark>IDamageable</mark> , <mark>GameEvents</mark> , <mark>CombatEvents</mark> , <mark>InputRouter</mark> (chỉ khai báo sự kiện), và công cụ Editor **Import Balance CSV** đọc <mark>Data/Balance/heroes.csv</mark> , <mark>enemies.csv</mark> , <mark>waves.csv</mark> (xuất từ file Can bang - Fate Bastion.xlsx) để tạo 

hoặc cập nhật asset theo id. Mỗi người có stub tối thiểu để chạy được phần của mình mà không chờ người khác. 

**Mã enum dùng chung** (CSV và code dùng đúng các tên này, không dấu): 

|Enum|Giá trị|
|---|---|
|Rarity|Common, Rare, Epic, Legendary|
|Element|Fire (Lửa), Ice (Băng), Lightning (Lôi)|
|Role|SingleDps (DPS đơn), Mage (Pháp sư), Control (Khống chế), Support (Hỗ trợ)|
|DamageType|Physical, Magic, True|
|AttackType|Melee, Projectile, Area, Cone|
|BuffType|None, Damage, AttackSpeed|



**Ánh xạ CSV → asset** (công cụ Tools → Import Balance CSV): 

|File|Asset tạo ra|Quy tắc|
|---|---|---|
|heroes.csv|HeroDatatên<br><id>và<br>SkillDatatên<br><id>_passive|Cột id … range ghi vào HeroData. areaRadius ghi<br>vào passive.radius; buffType ≠ None thì passive<br>có kind Buff với buffValue, buffRadius. Cột notes<br>và calc_* bỏ qua.|
|enemies.csv|EnemyDatatên<br><id>|Ghi đủ 10 cột; flying và boss đọc TRUE/FALSE,<br>không phân biệt hoa thường.|
|waves.csv|WaveDatatên<br>L<level>_W<wave>;<br>LevelDatatên<br>level_<level>|Mỗi loại quái có số lượng > 0 thành 1<br>SpawnGroup, theo thứ tự normal → fast →<br>armored → flying → boss; startDelay = thứ tự<br>group × 2 giây; interval = spawnInterval; flying<br>dùng pathIndex 1, còn lại pathIndex 0;<br>isBossWave = boss > 0. LevelData nhận 15<br>WaveData theo thứ tự wave.|



Quy tắc chung của công cụ import: 

- Hàng 1 là tên cột. Hàng có ô đầu tiên bắt đầu bằng # bị bỏ qua (hàng mô tả tiếng Việt). Cột không có trong bảng trên bị bỏ qua. 

- Asset đã có cùng tên thì cập nhật, không tạo trùng. Asset có trong project nhưng không có trong CSV thì không xóa, chỉ ghi cảnh báo. 

- File CSV lưu dạng **UTF-8 có BOM** (Excel: Save As → "CSV UTF-8"), để Excel mở không lỗi font tiếng Việt. Công cụ import đọc bằng <mark>File.ReadAllText(path, Encoding.UTF8)</mark> và bỏ ký tự BOM ở đầu nếu có. 

- Đọc số bằng <mark>CultureInfo.InvariantCulture</mark> (dấu chấm thập phân). Chấp nhận cả dấu phân cách , và ; (xác định từ hàng 1, vì hàng tên cột không có ô nào chứa dấu phân cách), vì Excel trên 

Windows tiếng Việt có thể lưu CSV bằng ;. Phải dùng bộ đọc CSV hiểu ô trong ngoặc kép, không dùng <mark>Split(',')</mark> : cột notes có dấu phẩy và chấm phẩy nên được Excel bọc trong <mark>"..."</mark> . Trong Excel nên tắt "Use system separators" (File → Options → Advanced) và đặt dấu thập phân là dấu chấm. 

- Giá trị enum sai hoặc số không đọc được: ghi lỗi kèm tên file và số dòng, bỏ qua dòng đó, không dừng cả lần import. 

- Tiêu chí hoàn thành S0: unit test bộ đọc CSV với ô có dấu phẩy trong ngoặc kép, file có BOM, file dùng dấu ;; import 3 file mẫu tạo đúng 9 HeroData, 9 SkillData bị động, 5 EnemyData, 45 WaveData, 3 LevelData; chạy import lần 2 không tạo thêm asset nào. 

Cách dùng tab này với Claude Code: 

1. Mỗi hệ thống một nhánh <mark>feature/s<số>-<tên></mark> . 

2. Dán phần đặc tả của hệ thống đó vào <mark>Docs/Specs/S<số>.md</mark> trong repo, rồi dùng Prompt gợi ý ở cuối mục. 

3. Yêu cầu Claude lập kế hoạch trước (plan mode), duyệt rồi mới cho code. 

4. Viết test theo Tiêu chí hoàn thành trước hoặc cùng lúc với code; chạy Unity Test Runner. 

5. Mở Pull Request, người khác review và chơi thử trước khi merge. 

6. Thay đổi thiết kế trong lúc làm thì cập nhật lại đặc tả này trước. 

## **S1. Chiến đấu và sát thương** 

**Mục tiêu:** một chỗ duy nhất tính sát thương cho mọi nguồn (đòn thường, kỹ năng, Ultimate, Thiên thạch, hiệu ứng theo thời gian), để cân bằng và kiểm thử dễ. Trong trận không có yếu tố ngẫu nhiên: không có chí mạng. 

### **Dữ liệu:** 

- <mark>DamageInfo</mark> (struct, không tạo rác): <mark>amount</mark> , <mark>type</mark> (Physical / Magic / True), <mark>element</mark> , <mark>source</mark> , <mark>canHitFlying</mark> , <mark>status (StatusEfectData</mark> f , có thể null), <mark>hitPoint</mark> . 

- <mark>StatusEfectData</mark> f (SO): id, <mark>element</mark> , <mark>duration</mark> , <mark>tickInterval</mark> , <mark>value</mark> , <mark>stackRule</mark> (Làm mới thời gian / Lấy mạnh nhất), <mark>vfx</mark> . 

- Phòng thủ của quái (trong <mark>EnemyData</mark> ): <mark>armor</mark> (giảm sát thương vật lý, %), <mark>magicResist</mark> (%), cờ <mark>Flying</mark> , <mark>Boss</mark> . 

### **Thành phần:** 

- <mark>IDamageable</mark> với <mark>TakeDamage(in DamageInfo)</mark> . 

- <mark>DamageCalculator</mark> : class tĩnh, hàm thuần (không phụ thuộc scene) để viết unit test. 

- • <mark>StatusEfectController</mark> f gắn trên quái: giữ danh sách hiệu ứng đang có (mảng cố định, không new mỗi frame). 

- <mark>DamageNumberSpawner</mark> (pool) và <mark>CombatEvents</mark> : <mark>OnDamageDealt</mark> , <mark>OnEnemyKilled</mark> . 

### **Công thức:** 

<mark>Sát\,thương = D_{thực} \cdot k_{kỹ\,năng} \cdot (1 - kháng)</mark> 

- <mark>D_thực</mark> theo công thức chỉ số ở mục 12 tab chính (độ hiếm, cấp); <mark>k_kỹ năng</mark> là hệ số của đòn hoặc kỹ năng (đòn thường = 1). 

- <mark>kháng</mark> = giáp (vật lý) hoặc kháng phép (phép), kẹp trong [0; 0.8]. Sát thương True bỏ qua kháng. 

- Kết quả làm tròn xuống, tối thiểu 1. 

Hiệu ứng trạng thái theo hệ: 

|Hệ|Hiệu ứng|Giá trị mặc định|Quy tắc cộng dồn|
|---|---|---|---|
|Lửa|Bỏng|20% sát thương đòn mỗi giây, 3 giây|Lấy mạnh nhất, làm mới thời<br>gian|
|Băng|Làm chậm|−30% tốc độ, 2 giây|Lấy mạnh nhất; tổng tối đa<br>−60%; boss chịu 50% hiệu lực|
|Lôi|Nảy|Sét sang tối đa 2 quái trong 3 m, mỗi<br>lần −30%|Không nảy lại quái đã trúng|



### **Trường hợp đặc biệt:** 

- Quái chết khi đạn đang bay: đạn bay tới vị trí cuối rồi tắt, không gây sát thương. 

- Nhiều nguồn giết cùng frame: chỉ phát <mark>OnEnemyKilled</mark> một lần (dùng cờ <mark>IsAlive</mark> ). 

- Tướng cận chiến không chọn được quái Bay; Thiên thạch đánh trúng quái Bay. 

- Tầm đánh và vùng nổ tính trên mặt phẳng ngang (XZ), vì quái Bay bay cao 2 m. 

- Sát thương thừa (overkill) không chuyển sang quái khác. 

### **Tiêu chí hoàn thành:** 

- ☐ Unit test <mark>DamageCalculator</mark> : kháng 0% / 50% / 80%, True damage, hệ số kỹ năng. 

- ☐ Test cộng dồn: Bỏng lấy mạnh nhất, Chậm tối đa −60%, boss chịu 50%. 

- ☐ Test chỉ một sự kiện chết khi 3 đòn giết cùng frame. 

   - Profiler: GC Alloc 0 B/frame khi 100 quái cùng bị Bỏng. 

- ☐ 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md và Docs/Specs/S1.md. Lập kế hoạch triển khai hệ thống chiến đấu trong namespace FateBastion.Combat: DamageInfo, IDamageable, DamageCalculator, StatusEffectData, StatusEffectController, CombatEvents. Không có chí mạng. Viết unit test EditMode cho mọi tiêu chí hoàn thành. Không tạo rác bộ nhớ trong Update._ 

## **S2. Quái và Wave** 

**Mục tiêu:** sinh quái theo lịch, cho quái đi theo đường từ Cổng tới Thành, xử lý quái Bay và boss; mọi con số nằm trong dữ liệu. 

### **Dữ liệu:** 

- <mark>EnemyData</mark> (SO): id, <mark>displayName</mark> , <mark>baseHP</mark> , <mark>armor</mark> , <mark>magicResist</mark> , <mark>moveSpeed</mark> (m/s), <mark>goldReward</mark> , <mark>castleDamage</mark> (1 / 2 / 10), cờ <mark>Flying</mark> , <mark>Boss</mark> , <mark>prefab</mark> , <mark>animations</mark> . 

- 4 loại quái + boss: Thường, Nhanh (tốc độ ×1.8, máu ×0.5), Giáp nặng (giáp 50%, máu ×1.5), Bay (đường bay riêng, máu ×0.8), Boss (máu ×15). 

- <mark>BossSummonAbility</mark> : cứ 12 giây gọi 3 quái Nhanh tại vị trí boss. 

- <mark>WaveData</mark> (SO): danh sách <mark>SpawnGroup { enemy, count, interval, startDelay, pathIndex }</mark> , <mark>isBossWave</mark> . 

- <mark>LevelData</mark> (SO): 15 <mark>WaveData</mark> , các Spline đường đi (đường chính + đường bay), <mark>hpGrowth</mark> = 1.12, <mark>hpMultiplier</mark> (màn 1 = 1.0, màn 2 = 1.15, màn 3 = 1.3), Vàng khởi đầu 300, máu Thành 20, phần thưởng, danh sách loại quái hiển thị ở màn Chọn màn. 

- <mark>EndlessSettings</mark> (SO): dùng <mark>LevelData</mark> của màn 3; sau wave 15 lặp lại wave 6–15 của màn 3, boss mỗi 5 wave, <mark>hpGrowth</mark> tiếp tục, dùng <mark>hpMultiplier</mark> của màn 3. 

### **Thành phần:** 

- <mark>WaveManager</mark> : đọc <mark>WaveData</mark> , đẩy yêu cầu sinh quái vào hàng đợi theo thời gian có tỉ lệ (nên ×2 tự nhanh hơn). 

- <mark>EnemyManager</mark> : giữ danh sách quái đang sống, gọi cập nhật tập trung cho tất cả, giới hạn 100 quái cùng lúc (vượt thì chờ trong hàng đợi). 

- <mark>EnemyController</mark> : đi theo Spline bằng quãng đường <mark>distance</mark> ; <mark>progress = distance / pathLength</mark> dùng cho ưu tiên "Đầu đoàn"; thanh máu nổi trên đầu (ẩn khi đầy máu). 

- Pool riêng cho từng loại quái. 

### **Luồng hoạt động:** 

1. Wave bắt đầu: các <mark>SpawnGroup</mark> chạy song song theo <mark>startDelay</mark> và <mark>interval</mark> . 

2. Sinh quái: lấy từ pool, máu = <mark>baseHP × hpGrowth^(wave−1) × hpMultiplier</mark> , <mark>ResetState()</mark> . 

3. Quái tới cuối đường: Thành − <mark>castleDamage</mark> , trả về pool (không thưởng Vàng). 

4. Quái bị hạ: +Vàng, phát <mark>OnEnemyKilled</mark> , trả về pool. 

5. Wave kết thúc khi đã sinh hết và không còn quái sống (kể cả quái do boss gọi) → báo S5. 

### **Quy tắc:** 

- Bay: đi Spline riêng ngắn hơn, bay cao 2 m; tướng cận chiến không đánh được. 

- Boss: thanh máu to nổi trên đầu, âm thanh báo khi xuất hiện, gọi quái con theo chu kỳ; tới Thành trừ 10. 

- Bỏ qua wave: wave sau bắt đầu ngay, quái cũ vẫn tiếp tục đi. 

- Đường chính dài khoảng 90 m, quái thường đi 2.5 m/s (khoảng 35 giây). 

### **Trường hợp đặc biệt:** 

- Quái vừa tới Thành vừa bị hạ cùng frame: xử lý một lần duy nhất, ưu tiên "bị hạ". 

- Ở ×2, bước di chuyển lớn: kiểm tra tới cuối đường bằng <mark>distance >= pathLength</mark> , không bằng va chạm. 

- Hàng đợi quá 100 vẫn phải tính vào điều kiện kết thúc wave. 

- Máu quái lưu bằng float để Vô tận không tràn số. 

### **Tiêu chí hoàn thành:** 

- ☐ Test PlayMode: quái tới Thành trừ đúng máu, đúng một lần, ở cả ×1 và ×2. 

- ☐ Test wave kết thúc đúng khi boss gọi quái con và khi vượt 100 quái. 

- ☐ Test máu quái wave 1, 5, 15 của cả 3 màn khớp công thức và khớp cột calc_totalHP của sheet Wave. 

- ☐ Chỉnh wave hoàn toàn bằng Inspector, không sửa code. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S1.md và S2.md. Triển khai FateBastion.Enemies: EnemyData, WaveData, LevelData, EndlessSettings, WaveManager, EnemyManager, EnemyController đi theo Unity Splines, quái Bay và boss gọi quái con. Dùng ObjectPool, không Instantiate/Destroy trong trận. Tạo scene thử Sandbox với 1 đường và 3 wave mẫu, viết test PlayMode theo tiêu chí._ 

## **S3. Triệu Hồi Sư và Camera** 

**Mục tiêu:** nhân vật người chơi đi lại mượt ở góc nhìn thứ 3, camera xoay/zoom thoải mái, input tách rõ giữa gameplay và UI. 

### **Dữ liệu:** 

- <mark>PlayerMovementSettings</mark> (SO): tốc độ đi 6 m/s, chạy nhanh 9 m/s, trọng lực −20 m/s², độ mượt khi xoay người. Không có nhảy. 

- <mark>CameraSettings</mark> (SO): khoảng cách zoom 4–25 m, góc ngẩng 10–60°, tốc độ xoay. 

- Input Actions asset với 2 Action Map: 

|Action Map|Actions|
|---|---|
|Summoner|Move (WASD), Sprint (Shift), Look (giữ chuột phải), Zoom, Select<br>(chuột trái), Cancel (bấm nhanh chuột phải / Esc), Deck1–Deck5,|
||Meteor (Q), Upgrade (U), Sell (X), Ultimate (R), SkipWave (N), Debug<br>(F1)|
|UI|Navigate, Submit, Cancel, Point, Click|



### **Thành phần:** 

- <mark>PlayerController</mark> : <mark>CharacterController</mark> , di chuyển theo hướng camera; animation Idle / Walk / Run / Cast. 

- <mark>InputRouter</mark> : bật đúng một Action Map tại một thời điểm; phát sự kiện cho các hệ thống khác (không để hệ thống khác đọc phím trực tiếp). 

- <mark>CameraRig</mark> : Cinemachine Camera + Third Person Follow + Deoccluder (tránh xuyên vật cản) + Impulse Listener (rung camera). 

- <mark>CursorController</mark> : luôn hiện chuột; giữ chuột phải thì ẩn để xoay camera. 

### **Luồng hoạt động:** 

1. Vào trận: Triệu Hồi Sư xuất hiện cạnh Thành, bật map Summoner. 

2. Mở bảng UI đầy màn hình (Tạm dừng, Kết quả): tắt Summoner, bật UI; đóng thì khôi phục. 

### **Trường hợp đặc biệt:** 

- Bấm chuột lên UI không được xuyên xuống thế giới (kiểm tra <mark>EventSystem</mark> trước khi xử lý Select). 

- Rơi khỏi map: dịch chuyển về điểm xuất hiện. 

- Camera bị kẹt trong tường hoặc cây: Deoccluder kéo camera lại gần. 

- Alt-Tab: tự tạm dừng (S5) và trả chuột. 

- Triệu Hồi Sư và camera dùng <mark>Time.unscaledDeltaTime</mark> (không chia cho <mark>timeScale</mark> , vì khi tạm dừng <mark>timeScale</mark> = 0). Ở ×2 nhân vật vẫn đi 6 m/s theo thời gian thực; khi tạm dừng Action Map Summoner bị tắt nên nhân vật đứng yên. Cinemachine Brain bật Ignore Time Scale. 

- Chuột phải: action Look dùng interaction Hold, action Cancel dùng interaction Tap (nhả trong 0,2 giây, chuột di chuyển dưới 5 pixel). Giữ để xoay camera không bao giờ kích hoạt hủy. 

- Thứ tự xử lý Esc ( <mark>Cancel</mark> ): hủy ngắm Thiên thạch hoặc hủy đặt tướng → đóng bảng thông tin tướng → mở Tạm dừng. Chỉ một bước được thực hiện cho mỗi lần bấm. 

### **Tiêu chí hoàn thành:** 

- ☐ Đi, chạy nhanh, xoay, zoom hoạt động ở 60 FPS và 30 FPS như nhau (dùng <mark>unscaledDeltaTime</mark> cho nhân vật và camera). 

- ☐ Ở ×2, Triệu Hồi Sư vẫn đi 6 m/s theo thời gian thực; tạm dừng rồi tiếp tục không làm nhân vật nhảy vị trí. 

- ☐ Esc xử lý đúng thứ tự ưu tiên trong cả 3 tình huống. 

   - Chỉ một Action Map được bật tại một thời điểm (test tự động). 

- ☐ 

- ☐ Bấm lên nút UI không kích hoạt Select trong thế giới. 

- ☐ Camera không xuyên qua vật cản trên map thử. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md và Docs/Specs/S3.md. Tạo Input Actions asset với 2 Action Map như bảng, InputRouter, PlayerController dùng CharacterController (không có nhảy), CameraRig dùng Cinemachine 3 (Third Person Follow, Deoccluder, Impulse) và CursorController. Mọi thông số đặt trong ScriptableObject. Viết test cho việc chuyển Action Map._ 

## **S4. Đặt tướng và nâng cấp trong trận** 

**Mục tiêu:** cho người chơi chọn tướng từ 5 ô deck, đặt xuống chỗ hợp lệ bằng Vàng, nâng cấp cấp 1→3 và bán; đây là toàn bộ kinh tế trong trận. 

### **Dữ liệu:** 

- <mark>PlacementRules</mark> (SO): giá đặt và số con tối đa theo độ hiếm (Common 100/6, Rare 200/5, Epic 350/4, Legendary 600/2), quân số tối đa 15, bán kính chiếm chỗ 0.8 m, tỉ lệ hoàn tiền 0.5. 

- <mark>HeroLevelTable</mark> (SO): cấp 2 = 75% giá đặt, +40% sát thương, +10% tốc đánh, +10% tầm; cấp 3 = 150% giá đặt, +100% sát thương, +20% tốc đánh, +15% tầm. 

- Dữ liệu trong trận: <mark>DeckSlot { heroData, placedCount }</mark> ×5; <mark>PlacedHero { heroData, level, goldSpent }</mark> . 

- <mark>Wallet</mark> : số Vàng hiện tại, sự kiện <mark>OnGoldChanged</mark> . 

### **Thành phần:** 

- <mark>PlacementSystem</mark> : trạng thái Rảnh / Đang xem trước; xử lý đặt, nâng cấp, bán. 

- <mark>GhostPreview</mark> : model của tướng với vật liệu trong suốt và vòng tầm đánh; xanh là hợp lệ, đỏ là không. 

- <mark>HeroLevel</mark> (component trên tướng): cấp hiện tại, tính lại chỉ số khi lên cấp, bật hào quang nhẹ ở cấp 3. 

- Layer vật lý: Placeable (đất), Path và Obstacle (không đặt), Hero. 

### **Luồng hoạt động:** 

1. Chọn ô deck (bấm hoặc phím 1–5) → vào trạng thái xem trước. 

2. Mỗi frame: raycast từ chuột xuống đất (bản NonAlloc), đặt bóng mờ tại điểm trúng, kiểm tra hợp lệ. 

3. Chuột trái khi hợp lệ: trừ Vàng → lấy tướng từ pool → <mark>Init(heroData)</mark> ở cấp 1 → <mark>placedCount+</mark> + → phát <mark>OnHeroPlaced</mark> . Nếu vẫn đủ Vàng, chưa đạt số con tối đa và quân số chưa đầy thì **giữ nguyên chế độ xem trước** để đặt tiếp; ngược lại tự thoát. (Không dùng Shift vì Shift là chạy nhanh.) 

4. Chuột phải hoặc Esc: hủy xem trước. 

5. Nâng cấp: giá = <mark>foor(giá đặt × % của cấp kế)</mark> l ; trừ Vàng, <mark>level++</mark> , tính lại chỉ số, <mark>goldSpent += giá</mark> . 

6. Bán: hoàn <mark>foor(goldSpent × 0.5)</mark> l , trả tướng về pool, <mark>placedCount--</mark> . 

**Kiểm tra hợp lệ khi đặt** (theo thứ tự, dừng ở lỗi đầu tiên và hiện lý do): 

1. Đủ Vàng 

2. <mark>placedCount</mark> < số con tối đa của độ hiếm 

3. Tổng quân số < 15 

4. Raycast trúng layer Placeable 

5. <mark>Physics.CheckSphere</mark> bán kính 0.8 m không chạm Hero, Path, Obstacle 

6. Không đang ngắm Thiên thạch (S7) 

### **Trường hợp đặc biệt:** 

- Vàng thay đổi trong lúc xem trước: kiểm tra lại mỗi frame, ô deck và bóng mờ đổi màu ngay. 

- Bấm liên tục rất nhanh: mỗi lần đặt kiểm tra lại toàn bộ, không dựa vào kết quả frame trước. 

- Đang xem trước mà bấm Q: hủy xem trước, chuyển sang ngắm Thiên thạch. 

- Kết thúc trận: trả mọi tướng về pool, Vàng về 0. 

### **Tiêu chí hoàn thành:** 

- ☐ Unit test: giới hạn số con theo từng độ hiếm, quân số 15, thiếu Vàng. 

- ☐ Unit test: giá nâng và chỉ số cấp 1–3 khớp <mark>HeroLevelTable</mark> ; hoàn tiền đúng khi bán (kể cả làm tròn, ví dụ Epic cấp 2 bán được 306). 

- ☐ PlayMode: không đặt được trên đường, nước, vách đá, hoặc chồng lên tướng khác. 

- ☐ HUD cập nhật qua sự kiện, không đọc trong Update. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S1.md, S3.md và S4.md. Triển khai PlacementRules, HeroLevelTable, Wallet, PlacementSystem, GhostPreview và HeroLevel (3 cấp). Tách_ 

_phần kiểm tra hợp lệ và tính giá thành class thuần để viết unit test. Nhận input qua InputRouter của S3._ 

## **S5. Luồng trận (GameManager)** 

**Mục tiêu:** điều phối một trận từ lúc vào đến lúc nhận thưởng: trạng thái, tốc độ, tạm dừng, bỏ qua wave, thắng/thua, tính sao và trao Ngọc. 

**Dữ liệu:** <mark>LevelData</mark> (S2) thêm: thời gian chuẩn bị 15 giây, nghỉ giữa wave 8 giây, ngưỡng sao (3 sao: máu Thành ≥ 15; 2 sao: ≥ 8; 1 sao: còn sống), Ngọc thưởng (lần đầu qua màn +300, mỗi sao mới +50; Vô tận +10 mỗi 5 wave). 

**Thành phần:** <mark>GameManager</mark> (state machine), <mark>TimeController</mark> (×1/×2 và tạm dừng, là nơi duy nhất được sửa <mark>Time.timeScale</mark> ), <mark>Castle</mark> (máu Thành), <mark>MatchResult</mark> , <mark>GameEvents</mark> : <mark>OnStateChanged</mark> , <mark>OnWaveStarted(n)</mark> , <mark>OnWaveCleared(n)</mark> , <mark>OnCastleDamaged</mark> , <mark>OnMatchEnded(result)</mark> . 

Trạng thái và chuyển trạng thái: 

|Từ|Điều kiện|Sang|
|---|---|---|
|Loading|Scene, pool và deck đã sẵn sàng|Chuẩn bị|
|Chuẩn bị (đặt tướng trước wave<br>1)|Hết 15 giây hoặc Bỏ qua|Đang wave|
|Đang wave|WaveManager báo wave sạch quái, chưa<br>phải wave cuối|Nghỉ|
|Đang wave|Wave cuối sạch quái và Thành còn máu<br>(không áp dụng cho Vô tận)|Thắng|
|Nghỉ|Hết 8 giây hoặc Bỏ qua (+50 Vàng)|Đang wave|
|Bất kỳ trạng thái chơi nào|Máu Thành ≤ 0|Thua|
|Thắng / Thua|Hiện màn hình kết quả, lưu (S9)|Về sảnh hoặc chơi<br>lại|



Tạm dừng là lớp phủ lên mọi trạng thái chơi: <mark>timeScale = 0</mark> , bật Action Map UI; tiếp tục thì khôi phục <mark>timeScale</mark> và Action Map trước đó. 

### **Quy tắc:** 

- Tốc độ ×1/×2 = <mark>Time.timeScale</mark> , chỉ đổi qua <mark>TimeController</mark> . 

- Bỏ qua: hộp xác nhận dùng chung (S10), lưu lựa chọn "Không hỏi lại" trong cài đặt (S9). 

- Ngọc chơi lại: chỉ trả phần sao cao hơn lần trước. 

- Vô tận: thua là kết thúc; kết quả ghi số wave đạt được và cập nhật kỷ lục. 

### **Trường hợp đặc biệt:** 

- Thua khi đang ngắm Thiên thạch: hủy ngắm trước, rồi mới hiện màn hình Thua. 

- Wave cuối sạch quái và Thành về 0 cùng frame: xử lý Thua trước. 

- Alt-Tab hoặc cửa sổ mất focus: tự tạm dừng. 

- Thoát giữa trận: không thưởng, không lưu trạng thái trận. 

### **Tiêu chí hoàn thành:** 

- ☐ Unit test mọi dòng trong bảng chuyển trạng thái. 

- ☐ Unit test tính sao ở các mốc 20, 15, 14, 8, 7, 1 máu. 

   - Tạm dừng rồi tiếp tục ở ×2 thì vẫn là ×2. 

- ☐ 

- ☐ Unit test Ngọc chơi lại: lần đầu 2 sao, lần sau 3 sao chỉ nhận thêm 50. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S2.md, S4.md và S5.md. Triển khai GameManager theo đúng bảng chuyển trạng thái, TimeController, Castle và GameEvents. State machine viết dạng class thuần có thể unit test; MonoBehaviour chỉ nối vào Unity._ 

## **S6. Kỹ năng và Ultimate** 

**Mục tiêu:** một hệ thống kỹ năng dùng chung cho bị động của tướng, Ultimate của tướng Legendary và Thiên thạch của người chơi (S7); thêm kỹ năng mới chỉ bằng dữ liệu khi cùng kiểu. 

**Dữ liệu – SkillData (SO):** id, <mark>displayName</mark> , <mark>icon</mark> , <mark>kind</mark> , <mark>aimMode</mark> , <mark>damageMultiplier</mark> (× sát thương tướng), <mark>damageType</mark> , <mark>status</mark> , <mark>radius</mark> / <mark>length</mark> / <mark>width</mark> , <mark>projectileSpeed</mark> , <mark>duration</mark> (cho vùng), <mark>cooldown</mark> , <mark>castDelay</mark> , <mark>animTrigger</mark> , <mark>vfx</mark> , <mark>sfx</mark> , <mark>cameraShake</mark> . Bị động thêm <mark>trigger</mark> (Mỗi đòn / Mỗi đòn thứ N / Hào quang). 

Các kiểu kỹ năng (kind): 

|Kiểu|Mô tả|Ví dụ|
|---|---|---|
|MeleeHit|Đánh trực tiếp mục tiêu trong tầm, không có đạn;<br>không chọn được quái Bay|Chém của Chiến binh Sấm|
|Projectile|Đạn bay tới mục tiêu|Mũi tên Cung thủ, ném băng|
|AreaAtPoint|Vùng nổ tại một điểm|Cầu lửa, Thiên thạch|
|Cone|Hình nón trước mặt|Thở lửa Long Vương|
|Line|Vùng dài dọc đường đi trong một khoảng thời<br>gian|Ultimate Rồng Thiêng Giáng<br>Thế|
|Zone|Vùng tồn tại theo thời gian, gây hiệu ứng liên tục|Vùng tuyết Người Tuyết|
|Buff|Tăng chỉ số cho tướng trong bán kính|Tu sĩ Làng, Thánh Nữ, Hào<br>quang Chỉ huy|



**Kiểu ngắm (aimMode):** <mark>Auto</mark> (theo ưu tiên "Đầu đoàn" khi tự đánh, riêng vai trò DPS đơn luôn ưu tiên boss nếu boss trong tầm; Ultimate chọn điểm có nhiều quái nhất trong tầm), <mark>GroundPoint</mark> (điểm trên mặt đất dưới con trỏ, chỉ dùng cho Thiên thạch). 

**Thành phần:** <mark>SkillRunner</mark> trên tướng (giữ hồi chiêu, kiểm tra dùng được), mỗi kind một class thực thi (strategy pattern), dùng <mark>DamageCalculator</mark> (S1) và truy vấn vật lý NonAlloc. Thời điểm gây sát thương do timer logic quyết định ( <mark>castDelay</mark> ), không phụ thuộc Animation Event. 

### **Nộ và Ultimate (chỉ Legendary):** 

- +5 Nộ mỗi lần tướng ra đòn (không tính theo số quái trúng), tối đa 100; Ultimate tiêu hết 100. 

- Kích hoạt thủ công: nút trong bảng thông tin tướng hoặc phím R khi đang chọn tướng. 

- Không có cắt cảnh: chỉ VFX lớn, rung camera (Cinemachine Impulse) và âm thanh riêng. 

### **Trường hợp đặc biệt:** 

- Mục tiêu chết trong lúc vận chiêu: AreaAtPoint vẫn nổ tại vị trí cuối; Projectile bay tới vị trí cuối rồi tắt. 

- Hồi chiêu và thời gian vùng dùng thời gian có tỉ lệ (×2 hồi nhanh gấp đôi theo thời gian thực). 

- Bấm Ultimate khi không có quái trong tầm: không dùng, giữ nguyên Nộ, hiện thông báo nhỏ. 

- Nhiều Buff cùng tác động lên một tướng: buff từ **cùng một loại tướng** (ví dụ 2 Thánh Nữ) chỉ lấy mức mạnh nhất; buff từ nguồn khác nhau thì cộng; tổng tốc đánh tối đa +60%. 

### **Tiêu chí hoàn thành:** 

- ☐ Scene thử có mỗi kind một kỹ năng; test PlayMode sát thương và số quái trúng đúng. 

- ☐ Test hồi chiêu ở ×1 và ×2. 

- ☐ Test Nộ: +5 mỗi đòn kể cả khi đòn trúng nhiều quái; Ultimate không dùng được khi chưa đủ 100. 

- ☐ Test cộng dồn Buff: 2 Thánh Nữ chỉ cho +30%; Thánh Nữ + Hào quang = +55%; không bao giờ vượt +60% tốc đánh. 

- ☐ Test DPS đơn chuyển sang bắn boss ngay khi boss vào tầm, kể cả khi quái con đi trước. 

- ☐ Thêm một kỹ năng mới cùng kiểu mà không sửa code. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S1.md và S6.md. Triển khai SkillData, SkillRunner và một class cho mỗi kiểu kỹ năng (MeleeHit, Projectile, AreaAtPoint, Cone, Line, Zone, Buff), thanh Nộ và Ultimate kích hoạt thủ công. Tạo kỹ năng mẫu cho 9 tướng trong GDD và scene thử, viết test PlayMode theo tiêu chí._ 

## **S7. Hào quang Chỉ huy và Thiên thạch** 

**Mục tiêu:** cho Triệu Hồi Sư hai cách tác động trực tiếp lên trận đấu, để việc di chuyển trên map có ý nghĩa: buff tướng đứng gần và ném Thiên thạch. 

### **Dữ liệu:** 

- <mark>CommandAuraSettings</mark> (SO): bán kính 6 m, +25% tốc đánh, tần suất kiểm tra 5 lần/giây, VFX vòng sáng và tia buff. 

- <mark>MeteorSettings</mark> (SO, dùng <mark>SkillData</mark> kiểu AreaAtPoint): tầm 20 m, bán kính nổ 4 m, rơi sau 0.8 giây, hồi chiêu 45 giây, sát thương True = 3 × máu quái Thường của wave hiện tại, rung camera, VFX bóng báo trước và vụ nổ. 

### **Thành phần:** 

- <mark>CommandAura</mark> (trên Triệu Hồi Sư): mỗi 0.2 giây so khoảng cách bình phương tới các tướng trên sân; tướng vào/ra vùng thì bật/tắt buff qua <mark>HeroStats</mark> (chỉ tính lại chỉ số khi trạng thái đổi). 

- <mark>MeteorAbility</mark> : trạng thái Sẵn sàng / Đang ngắm / Đang rơi / Hồi chiêu; phát <mark>OnMeteorCooldownChanged</mark> cho HUD. 

- <mark>MeteorAimIndicator</mark> : vòng tầm 20 m quanh Triệu Hồi Sư và vòng nổ 4 m dưới con trỏ (xanh trong tầm, đỏ ngoài tầm). 

### **Luồng hoạt động của Thiên thạch:** 

1. Bấm Q khi Sẵn sàng → Đang ngắm: hiện hai vòng, hủy xem trước đặt tướng (S4) nếu đang có. 

2. Mỗi frame: raycast từ chuột xuống mặt đất; kiểm tra khoảng cách tới Triệu Hồi Sư ≤ 20 m. 

3. Chuột trái khi trong tầm: chốt điểm rơi, bắt đầu hồi chiêu ngay, hiện bóng đỏ trên mặt đất. 

4. Sau 0.8 giây: <mark>OverlapSphereNonAlloc</mark> bán kính 4 m, gây sát thương True cho mọi quái (kể cả quái Bay), rung camera, VFX nổ. 

5. Chuột phải, Esc hoặc bấm Q lần nữa: hủy ngắm, không tốn hồi chiêu. 

### **Quy tắc và công thức:** 

<mark>Sát\,thương_{Thiên\,thạch} = 3 \cdot HP_{0}^{Thường} \cdot 1.12^{\,n-1} \cdot k_{màn}</mark> 

- Mọi quái không phải boss trong vùng nổ chết ngay (máu cao nhất là Giáp nặng, ×1.5 quái Thường); boss mất khoảng 20% máu (3 / 15). Nếu chơi thử thấy quá mạnh, giảm <mark>meteorMult</mark> từ 3 xuống 2 trong file cân bằng. 

- Hồi chiêu dùng thời gian có tỉ lệ. 

- Hào quang luôn bật, kể cả trong lúc nghỉ giữa wave. 

### **Trường hợp đặc biệt:** 

- Triệu Hồi Sư đi ra ngoài tầm trong lúc đang ngắm: vòng chuyển đỏ, không thả được, không tự hủy. 

- Thiên thạch đang rơi thì trận kết thúc: hủy, không gây sát thương. 

- Tạm dừng trong lúc Thiên thạch đang rơi: thời gian rơi dừng theo <mark>timeScale</mark> . 

- Tướng bị bán khi đang trong Hào quang: gỡ buff trước khi trả về pool. 

### **Tiêu chí hoàn thành:** 

- ☐ Unit test sát thương Thiên thạch ở wave 1, 5, 15 khớp công thức. 

- ☐ Unit test trạng thái <mark>MeteorAbility</mark> : hủy ngắm không tốn hồi chiêu; thả rồi thì hồi đúng 45 giây (×1) và 22.5 giây thực (×2). 

- ☐ PlayMode: tướng vào/ra vòng 6 m thì tốc đánh đổi đúng; bán tướng không để lại buff. 

- ☐ Thiên thạch trúng quái Bay; không thả được ngoài 20 m. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S3.md, S4.md, S6.md và S7.md. Triển khai FateBastion.Player: CommandAuraSettings, CommandAura, MeteorSettings, MeteorAbility (state machine dạng class thuần), MeteorAimIndicator. Dùng SkillData_ 

_kiểu AreaAtPoint của S6 và DamageCalculator của S1. Nhận input qua InputRouter. Viết unit test và test PlayMode theo tiêu chí._ 

## **S8. Gacha, Túi, Ghép, Deck** 

**Mục tiêu:** toàn bộ tiến trình ngoài trận: quay tướng bằng Ngọc, quản lý bản sao trong Túi, ghép bản thừa và chọn deck. Logic viết dạng class thuần (không cần scene) để test kỹ. 

### **Dữ liệu:** 

- <mark>GachaTable</mark> (SO): trọng số 4 độ hiếm (65 / 25 / 8.5 / 1.5), giá x1 = 100 và x10 = 900 Ngọc, pity = 25, danh sách tướng theo từng độ hiếm (tự gom từ mọi <mark>HeroData</mark> ). 

- <mark>StarterSettings</mark> (SO): 5 tướng khởi đầu (Cung thủ Làng, Phù thủy Lửa, Lính Băng, Tu sĩ Làng, Người Tuyết), 1.000 Ngọc, deck mặc định. Khi tạo save mới, 5 tướng này được ghi sẵn vào <mark>ownedEver</mark> nên không nhận +50 Ngọc "lần đầu sở hữu". 

- Dữ liệu lưu (S9): <mark>gems</mark> , <mark>pityCounter</mark> , <mark>collection: { heroId → copies }</mark> , <mark>deck: heroId[5]</mark> , <mark>ownedEver</mark> (để thưởng +50 Ngọc lần đầu sở hữu). 

### **Thuật toán quay một lượt:** 

1. Nếu <mark>pityCounter</mark> = 24: kết quả là Legendary. Ngược lại quay theo cả bảng trọng số. 

2. Kết quả là Legendary: <mark>pityCounter</mark> = 0; ngược lại <mark>pityCounter++</mark> . 

3. Chọn đều ngẫu nhiên một tướng trong danh sách của độ hiếm đó; <mark>copies++</mark> . 

4. Lần đầu sở hữu: thêm vào <mark>ownedEver</mark> , +50 Ngọc. 

**Quay x10:** chạy 10 lượt liên tiếp. Nếu không có kết quả nào từ Epic trở lên thì **thay lượt cuối** bằng một lượt quay trong nhóm Epic + Legendary (chuẩn hóa lại trọng số). Khi thay, phải hoàn tác thay đổi của lượt cuối cũ (copies, <mark>pityCounter</mark> , <mark>ownedEver</mark> , Ngọc thưởng) trước khi áp lượt mới. Cách đơn giản: tính trước kết quả cả 10 lượt trong bộ nhớ, rồi mới áp vào dữ liệu một lần. 

### **Ghép:** 

- Bản thừa của một tướng = <mark>copies − 1</mark> . Ghép cần tổng 3 bản thừa cùng độ hiếm (Common, Rare hoặc Epic), có thể từ nhiều tướng khác nhau. 

- Thực hiện: người chơi chọn độ hiếm và 3 bản thừa (hoặc bấm "Tự chọn" để lấy từ tướng có nhiều bản thừa nhất) → trừ <mark>copies</mark> tương ứng → quay đều ngẫu nhiên một tướng thuộc độ hiếm kế tiếp → <mark>copies++</mark> . 

- Mỗi tướng luôn còn ít nhất 1 bản, nên ghép không làm thay đổi deck. 

- Legendary không ghép được. 

**Deck:** đúng 5 tướng khác nhau, mỗi tướng có ít nhất 1 bản; kiểm tra trước khi vào trận. 

### **Trường hợp đặc biệt:** 

- Thiếu Ngọc: nút quay bị khóa, không trừ dở dang. 

- Không đủ 3 bản thừa cùng độ hiếm: nút Ghép bị khóa và ghi rõ còn thiếu bao nhiêu. 

- Sau mỗi lần quay, ghép, đổi deck: lưu ngay (S9). Tắt game giữa chừng không được mất Ngọc mà không có tướng. 

- RNG dùng <mark>System.Random</mark> truyền vào từ ngoài, để test có thể cố định seed. 

### **Tiêu chí hoàn thành:** 

- ☐ Test 100.000 lượt quay (tắt pity): tỉ lệ mỗi độ hiếm lệch không quá 0.5 điểm %. 

- ☐ Test pity kích hoạt đúng lượt thứ 25 và được giữ qua lưu/tải. 

- ☐ Test x10 luôn có ít nhất 1 Epic trở lên, và <mark>pityCounter</mark> đúng sau khi thay lượt cuối. 

- ☐ Test ghép: cần đúng 3 bản thừa cùng độ hiếm, không ghép được Legendary, không bao giờ làm tướng về 0 bản. 

- ☐ Test người chơi mới có đủ 5 tướng khởi đầu, 1.000 Ngọc, <mark>ownedEver</mark> chứa đủ 5 tướng đó và vào trận được ngay. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md và Docs/Specs/S8.md. Triển khai FateBastion.Meta: GachaTable, StarterSettings, GachaService, CollectionService (ghép bản thừa) và DeckService dạng class thuần nhận System.Random và dữ liệu lưu từ ngoài. Viết unit test EditMode cho mọi tiêu chí, kể cả test 100.000 lượt._ 

## **S9. Lưu trữ** 

**Mục tiêu:** lưu toàn bộ tiến trình ngoài trận an toàn, không mất dữ liệu khi tắt game hoặc máy sập giữa chừng. 

### **Dữ liệu – SaveData:** 

{ "version": 1, "gems": 1200, "pityCounter": 17, "collection": { "hero_cung_thu": 3, "hero_long_vuong": 1 }, "deck": ["hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_thanh_nu", "hero_long_vuong"], "ownedEver": ["hero_cung_thu", "hero_long_vuong"], "levelProgress": { "level_1": 3, "level_2": 2 }, "endlessBest": 23, "tutorialDone": true, "settings": { "master": 0.8, "music": 0.6, "sfx": 0.8, "cameraShake": true, "skipConfirm": true } } **Thành phần:** <mark>SaveService</mark> (đọc/ghi bằng Newtonsoft Json tại <mark>Application.persistentDataPath/save.json</mark> ). Các service khác chỉ làm việc với <mark>SaveData</mark> trong bộ nhớ và gọi <mark>SaveService.Save()</mark> . Bản 1 tháng chỉ có <mark>version</mark> = 1, chưa cần SaveMigrator. 

**Ghi an toàn:** ghi ra <mark>save.tmp</mark> → nếu <mark>save.json</mark> đã tồn tại thì <mark>File.Replace(tmp, json, bak)</mark> , nếu chưa (lần lưu đầu) thì <mark>File.Move(tmp, json)</mark> . Không bao giờ ghi đè trực tiếp lên <mark>save.json</mark> . 

**Thời điểm lưu:** sau mỗi thao tác của S8 (quay, ghép, đổi deck); khi màn hình kết quả trận hiện ra; khi đổi cài đặt; trong <mark>OnApplicationQuit</mark> . Không lưu trạng thái giữa trận. 

**Đọc khi mở game:** đọc <mark>save.json</mark> → lỗi thì thử <mark>save.bak</mark> → vẫn lỗi thì tạo dữ liệu mới từ <mark>StarterSettings</mark> và báo cho người chơi. 

### **Trường hợp đặc biệt:** 

- <mark>heroId</mark> trong file không còn tồn tại trong game (đã xóa tướng): bỏ qua và ghi log, không làm hỏng cả file. 

- Deck chứa tướng không hợp lệ: thay bằng tướng khởi đầu còn trống, báo người chơi kiểm tra lại deck. 

- Bảng Debug (F1): xóa save, cộng Ngọc, nhận một tướng bất kỳ, nhảy wave, bất tử Thành, tốc độ ×5. Toàn bộ code bọc trong <mark>#if UNITY_EDITOR || DEVELOPMENT_BUILD</mark> . Bản nộp là build Release (không có Debug); buổi bảo vệ chạy bản Development build. 

### **Tiêu chí hoàn thành:** 

- ☐ Test lưu rồi đọc lại cho kết quả giống hệt. 

   - Test lần lưu đầu tiên (chưa có <mark>save.json</mark> ) không lỗi. 

- ☐ 

   - Test file hỏng → dùng 

- ☐ 

   - <mark>save.bak</mark> ; cả hai hỏng → tạo mới, không crash. 

- ☐ Tắt game ngay sau khi quay: mở lại có đủ tướng vừa quay và Ngọc đã trừ. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S8.md và S9.md. Triển khai SaveData, SaveService dùng Newtonsoft Json với ghi an toàn (tmp, Replace hoặc Move, bak) và Bảng Debug. Nối S8 và cài đặt vào SaveService. Viết test theo tiêu chí, dùng thư mục tạm thay cho persistentDataPath khi test._ 

## **S10. HUD và UI** 

**Mục tiêu:** toàn bộ giao diện trong và ngoài trận, nhận dữ liệu qua sự kiện, không chứa logic game. Làm dần theo từng hệ thống (S4 xong thì làm 5 ô deck, S5 xong thì làm thanh Wave, S7 xong thì làm ô Thiên thạch…). 

### **Danh sách màn hình:** 

|Màn hình|Nội dung chính|Cần hệ thống|
|---|---|---|
|HUD trong trận|Wave, máu Thành, Bỏ qua, Tốc độ ×1/×2, Cài đặt, cảnh<br>báo, Vàng, Quân số, ô Thiên thạch, 5 ô deck|S4, S5, S7|
|Bảng thông tin tướng|Chỉ số, cấp 1–3, Nâng cấp, Bán, Ultimate (Legendary)|S4, S6|
|Kết quả trận|Thắng/Thua, số sao, Ngọc nhận, Chơi lại / Về sảnh|S5|
|Sảnh, Chọn màn|Nút điều hướng, sao đã đạt mỗi màn, loại quái sẽ xuất<br>hiện, kỷ lục Vô tận|S2, S9|
|Gacha|Quay x1 / x10, Ngọc, pity, bảng tỉ lệ, hiệu ứng ra tướng|S8|
|Túi & Deck|Lưới tướng, bản sao, Ghép, 5 ô deck|S8|



|Màn hình|Nội dung chính|Cần hệ thống|
|---|---|---|
|Cài đặt, Tạm dừng|Âm lượng, rung camera, hỏi khi bỏ qua wave|S5, S9, S11|
|Hướng dẫn màn 1|4 hộp gợi ý theo sự kiện: đặt tướng đầu tiên, nâng cấp,<br>Hào quang, Thiên thạch; nút Bỏ qua; chỉ hiện lần đầu|S4, S7, S9|
|Bảng Debug|Các nút debug (chỉ Editor và Development)|S9|



### **Thành phần dùng chung:** 

- <mark>UIManager</mark> : điều hướng dạng ngăn xếp (Push / Pop), Esc đóng màn hình trên cùng; trong trận, Esc đi theo thứ tự ưu tiên ở S3. 

- <mark>ConfrmDialog</mark> i : tiêu đề, nội dung, Xác nhận / Hủy, tùy chọn ô "Không hỏi lại"; dùng cho Bỏ qua wave, Ghép, Bán tướng cấp 3. 

- <mark>Of</mark> f <mark>screenWarning</mark> : chiếu vị trí quái gần Thành nhất (tiến độ ≥ 80% đường) lên màn hình; ngoài màn hình thì hiện mũi tên đỏ ở rìa. 

- <mark>WorldHealthBar</mark> : thanh máu nổi trên đầu quái (pool, luôn quay về camera, ẩn khi đầy máu). 

- <mark>CooldownSlot</mark> : ô kỹ năng có lớp phủ hồi chiêu và số giây, dùng cho Thiên thạch. 

### **Quy tắc:** 

- View chỉ đăng ký sự kiện ( <mark>OnGoldChanged</mark> , <mark>OnCastleDamaged</mark> , <mark>OnWaveStarted</mark> , <mark>OnMeteorCooldownChanged</mark> , <mark>OnHeroSelected</mark> …); không đọc dữ liệu trong Update; chỉ đổi text khi giá trị đổi. 

- Tách Canvas tĩnh (HUD) và Canvas động (số sát thương, thanh máu) để tránh vẽ lại toàn bộ. 

- EventSystem dùng <mark>InputSystemUIInputModule</mark> ; bấm lên UI không xuyên xuống thế giới. 

- Font TextMeshPro dạng Dynamic có đủ ký tự tiếng Việt; kiểm tra ngay tuần 1. 

- Animation UI (nút nảy, số Vàng nhảy, bảng trượt) dùng PrimeTween với thời gian không tỉ lệ để vẫn chạy khi tạm dừng. 

### **Tiêu chí hoàn thành:** 

- ☐ Mọi màn hình mở/đóng đúng, Esc luôn đóng màn hình trên cùng. 

- ☐ Giá trị trên HUD khớp sự kiện (test bằng cách phát sự kiện giả). 

- ☐ Profiler: HUD không tạo rác bộ nhớ khi Vàng thay đổi liên tục. 

- ☐ Chữ tiếng Việt hiển thị đủ dấu trên mọi màn hình. 

- ☐ Chạy đúng ở 1920×1080 và 1366×768. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md, Docs/Specs/S10.md và đặc tả của hệ thống liên quan. Triển khai UIManager, ConfirmDialog, OffscreenWarning, WorldHealthBar, CooldownSlot, rồi [tên màn hình] theo bảng. View chỉ nhận dữ liệu qua sự kiện; dùng uGUI, TextMeshPro, PrimeTween._ 

## **S11. Âm thanh và phản hồi** 

**Mục tiêu:** làm mọi hành động có cảm giác (âm thanh, số sát thương, rung, nháy sáng) mà không ồn ào hay gây lag khi đông quái. 

**Dữ liệu:** <mark>SoundData</mark> (SO): danh sách clip (chọn ngẫu nhiên), âm lượng, dao động cao độ ±10%, nhóm mixer, số bản phát cùng lúc tối đa (mặc định 4). 

**Thành phần:** <mark>AudioManager</mark> với AudioMixer (Master, Music, SFX) và pool 24 AudioSource; <mark>MusicPlayer</mark> (nhạc sảnh, nhạc trận, nhạc boss, chuyển mượt); <mark>FeedbackService</mark> (số sát thương, rung camera bằng Cinemachine Impulse, nháy trắng quái bằng MaterialPropertyBlock). 

Danh sách âm thanh cần có: 

|Nhóm|Âm thanh|
|---|---|
|Tướng|Đặt, nâng cấp, lên cấp 3, bán, đòn đánh theo vai trò, Ultimate|
|Triệu Hồi Sư|Bước chân, Thiên thạch rơi và nổ, Thiên thạch hồi xong|
|Quái|Trúng đòn, chết, lọt vào Thành (cảnh báo), boss xuất hiện|
|Trận|Wave bắt đầu, wave kết thúc, thắng, thua|
|Ngoài trận|Quay gacha theo từng độ hiếm, ghép|
|UI|Bấm nút, mở/đóng bảng, lỗi (thiếu Vàng, chỗ đặt không hợp lệ, ngoài<br>tầm)|



### **Quy tắc phản hồi:** 

- Số sát thương: màu theo hệ, Thiên thạch to hơn; khi có hơn 60 số trên màn hình thì bỏ bớt số nhỏ. 

- Rung camera: boss xuất hiện, Thiên thạch nổ, Ultimate, Thành mất máu; có tùy chọn tắt trong Cài đặt. 

- Âm thanh trùng trong cùng một lúc (nhiều quái chết cùng frame): vượt giới hạn thì bỏ qua. 

- Âm thanh lấy từ nguồn miễn phí (Kenney, freesound); ghi nguồn và giấy phép vào <mark>Assets/ThirdParty/CREDITS.md</mark> . 

### **Tiêu chí hoàn thành:** 

- ☐ Âm lượng từng nhóm chỉnh được và được lưu (S9). 

- ☐ 100 quái chết cùng lúc không phát quá 4 âm thanh "quái chết" và không tạo rác bộ nhớ. 

- ☐ Nhạc chuyển mượt giữa sảnh, trận và boss. 

- ☐ Tắt rung camera trong Cài đặt có tác dụng ngay. 

### **Prompt gợi ý cho Claude:** 

_Đọc CLAUDE.md và Docs/Specs/S11.md. Triển khai SoundData, AudioManager với AudioMixer và pool AudioSource, MusicPlayer, FeedbackService. Nối vào các sự kiện đã có của S1–S10, không sửa logic của các hệ thống đó._ 

