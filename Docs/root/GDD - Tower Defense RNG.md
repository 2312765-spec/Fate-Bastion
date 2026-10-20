# **GDD – Tower Defense RNG (Unity 3D)** 

Oct 5, 2026 · @Thuan · v1.3 · Bản rút gọn cho đồ án 1 tháng (lịch sử thay đổi ở mục 15) 

## **1. Tổng quan** 

Đây là game thủ thành 3D góc nhìn thứ 3. **Ngoài trận** , người chơi quay gacha nhận tướng thuộc 4 độ hiếm, ghép bản thừa trong Túi, rồi chọn 5 tướng làm deck. **Trong trận** , người chơi điều khiển Triệu Hồi Sư đi trên map, trả Vàng để đặt và nâng cấp tướng cạnh đường, đứng gần tướng để tăng sức đánh bằng Hào quang Chỉ huy, và tự tay ném Thiên thạch vào quái. Tên tạm: **Thủ Thành Vận Mệnh** (Fate Bastion). 

|Mục|Nội dung|
|---|---|
|Thể loại|Tower Defense + RNG/Gacha + Merge, người chơi là một nhân vật hoạt động<br>trên map|
|Engine|Unity 3D (URP), góc nhìn thứ 3 hoàn toàn, camera xoay và zoom được|
|Nền tảng|PC (bàn phím + chuột)|
|Đối tượng|Người chơi casual đến midcore, 15–30 tuổi|
|Phong cách hình ảnh|Low-poly / stylized, để nhìn gần vẫn đẹp và dễ đồng bộ asset|
|Game tham khảo|Tower Defense Simulator, All Star Tower Defense|
|Phạm vi|Đồ án 3 người, 4 tuần, có Claude Code hỗ trợ viết code|



### **Điểm độc đáo đề xuất:** 

- **May rủi nằm ngoài trận, chiến thuật nằm trong trận:** gacha và ghép quyết định bạn có tướng gì; trong trận, chiến thắng phụ thuộc vào cách chọn deck, vị trí đặt, thứ tự nâng cấp và vị trí đứng của Triệu Hồi Sư. 

- **Triệu Hồi Sư là quân cờ thứ 16 trên sân:** Hào quang Chỉ huy buff tướng đứng gần, Thiên thạch chỉ ném được trong tầm 20m, nên người chơi phải liên tục chọn chỗ đứng. 

- **Ghép bản thừa:** 3 bản thừa cùng độ hiếm ghép thành 1 tướng ngẫu nhiên bậc kế tiếp, nên quay trùng không bị phí. 

- **Ngắm tướng và quái ở cự ly gần:** góc nhìn thứ 3 cho thấy rõ hình ảnh, hiệu ứng, Thiên thạch rơi và Ultimate của tướng Legendary. 

**Ngoài phạm vi bản 1 tháng (để sau nếu còn thời gian):** Nhập Hồn, độ hiếm Mythic và Secret, lên sao và mảnh, Bùa May mắn, Index, Thử thách ngày, Co-op, bảng xếp hạng online, địa hình gò cao/cầu/dốc, tốc độ ×3, cắt cảnh gacha và Ultimate. 

## **2. Core gameplay loop** 

Game có hai vòng lặp: vòng trong trận lặp lại mỗi wave (30–65 giây), vòng ngoài trận lặp lại mỗi màn (12 –16 phút). 



<!-- Start of picture text -->
MGi wave 1a mét vong: ha quai, nhan Vang, tiéu Vang<br>wave tiép theo (trong tran)<br>Wave bat dau Quai di theo duéng Tuéng + Thién thach Tiéu Vang, di chuyén<br>Quai ra ti Céng Lot thanh: trv mau Ha quai, + Vang méi con Bat/ Nang tuéng, diing buff<br>tran méi hét wave cudi hoae Thanh vo<br>Chon deck Gacha ngoai tran Nhan thuéng Két thtic man<br>5 tudng ti Tai Quay, ghép trong Tui Ngoc<br>Thang 1-3 sao/Thua<br><!-- End of picture text -->

core loop · vòng trong trận (hàng trên) và vòng ngoài trận (hàng dưới) 

Trong trận không có yếu tố ngẫu nhiên (không có chí mạng, không có tỉ lệ kích hoạt). Quyết định quan trọng nhất nằm ở ô Tiêu Vàng: đặt thêm tướng mới (phủ rộng), nâng cấp tướng đang có (tập trung), hay tích Vàng chờ đặt tướng Legendary. Quyết định thứ hai là **đứng ở đâu** : cạnh cụm tướng chủ lực để buff, hay chạy tới đoạn đường sắp vỡ để ném Thiên thạch. Mọi yếu tố may rủi (gacha, ghép) nằm ở vòng ngoài trận. 

## **3. Thiết kế map** 

Map gồm Cổng Enemy, một con đường cố định uốn lượn và Thành trì, kích thước khoảng 60 × 60 m (chạy nhanh từ Cổng về Thành dưới 8 giây). Hai bên đường là vùng đất đặt tướng tự do; Triệu Hồi Sư đi bộ trên đó. Quái chạy một mạch từ Cổng đến Thành (khoảng 35 giây với quái thường), không tấn công tướng hay người chơi; nhiệm vụ duy nhất của người chơi là cản quái. 



<!-- Start of picture text -->
D&t tu do: tuéng trong khuc cua ban dugc hai doan duéng<br>Céng YON Thanh<br>Enemy ‘A q th<br>' @ 1 ,<br>4 1 nude<br>vach dé A Trev Hoisu<br>Dat dat duge Tuéng — {} Tamdanh Khéng dat duge (nude, da) & Trigu Hi su<br><!-- End of picture text -->

sơ đồ map mẫu nhìn từ trên xuống · đặt tướng tự do, tối đa 15 tướng 

Quy tắc đặt tướng tự do: 

- **Đặt ở bất kỳ đâu trên vùng đất hợp lệ** trong tầm nhìn của camera; không đặt trên đường quái, nước, vách đá hay chồng lên tướng khác (mỗi tướng chiếm một vòng tròn bán kính 0.8 m). Không giới hạn khoảng cách tới Triệu Hồi Sư. 

- **Khi đặt sẽ hiện bóng mờ xem trước và vòng tầm đánh:** xanh là hợp lệ, đỏ là không. 

- **Giới hạn quân số:** tối đa 15 tướng trên sân, và mỗi tướng có giới hạn số con theo độ hiếm (mục 8). Quân số đầy thì phải bán bớt hoặc dồn Vàng vào nâng cấp. 

- **Vị trí vẫn quan trọng:** đặt trong khúc cua thì bắn được hai đoạn đường; gần Cổng hợp Khống chế; gần Thành là tuyến cuối, hợp DPS đơn. Đặt tướng thành cụm thì Hào quang Chỉ huy phủ được nhiều tướng hơn. 

Bản 1 tháng chỉ làm **1 map** ; 3 màn chiến dịch dùng chung map, khác nhau ở dữ liệu wave. 

Trong Unity: đường là Spline; vùng đặt tướng là Layer Placeable trên mesh địa hình; khi đặt, raycast từ con trỏ chuột xuống mặt đất và dùng Physics.CheckSphere để kiểm tra chồng lấn. Map cần được dựng thật (địa hình, cây, đá, ánh sáng) vì người chơi nhìn gần. 

## **4. Triệu Hồi Sư và camera** 

Người chơi luôn ở góc nhìn thứ 3, điều khiển Triệu Hồi Sư đi lại trên map. Triệu Hồi Sư không có máu và không bị quái tấn công; vai trò của nhân vật là **buff tướng bằng Hào quang Chỉ huy** và **ném Thiên thạch** . 

Camera: Cinemachine Third Person Follow; giữ chuột phải để xoay, cuộn chuột để zoom từ sau vai lên cao khoảng 60°; camera tự tránh xuyên tường, đá. Con trỏ chuột luôn hiện (ẩn tạm khi đang giữ chuột phải). 

|Phím|Hành động|
|---|---|
|WASD, Shift|Di chuyển (6 m/s), giữ Shift để chạy nhanh (9 m/s)|
|1 – 5|Chọn tướng trong deck để đặt; sau khi đặt, nếu còn đủ Vàng và còn chỗ thì giữ nguyên<br>chế độ đặt để đặt tiếp|
|Q|Ngắm Thiên thạch (hiện vòng tầm 20m quanh Triệu Hồi Sư và vòng nổ 4m dưới chuột)|
|Chuột trái|Đặt tướng / chọn tướng trên sân / thả Thiên thạch|
|Chuột phải|Giữ để xoay camera; bấm nhanh (nhả trong 0,2 giây, chuột gần như không di chuyển)<br>để hủy đặt tướng hoặc hủy ngắm Thiên thạch|
|Cuộn chuột|Zoom camera|
|U / X|Nâng cấp / bán tướng đang chọn|
|R|Ultimate của tướng Legendary đang chọn (khi đầy Nộ)|
|N|Bỏ qua thời gian chờ, gọi wave tiếp theo (như nút Bỏ qua wave)|
|Esc|Theo thứ tự ưu tiên: hủy ngắm Thiên thạch hoặc hủy đặt tướng → đóng bảng thông tin<br>tướng → mở Tạm dừng|
|F1|BảngDebug (chỉ trongEditor và bản Development)|



|Phím<br>Hành động|
|---|



### **Hào quang Chỉ huy (bị động, luôn bật):** 

- Tướng trong bán kính 6 m quanh Triệu Hồi Sư được **+25% tốc đánh** . Dưới chân Triệu Hồi Sư có vòng sáng 6 m; tướng đang được buff có hiệu ứng tia sáng nhỏ. 

- Cộng dồn với buff của tướng Hỗ trợ theo quy tắc: **cùng một tướng (ví dụ 2 Thánh Nữ) thì chỉ lấy mức mạnh nhất, khác tướng thì cộng** ; tổng buff tốc đánh tối đa +60%. 

- Mục đích: cho người chơi lý do để di chuyển. Khi boss xuất hiện, chạy tới cụm tướng đang đánh boss là một quyết định có giá trị. 

### **Thiên thạch (kỹ năng chủ động duy nhất của người chơi):** 

- Bấm Q để ngắm, chuột trái để thả. Chỉ thả được trong tầm **20 m** quanh Triệu Hồi Sư; ngoài tầm thì vòng ngắm chuyển đỏ. 

- Thiên thạch rơi sau 0.8 giây (có bóng đỏ báo trước trên mặt đất), nổ bán kính **4 m** . 

- Sát thương True (bỏ qua giáp) = **3 × máu của một quái Thường ở wave hiện tại** : mọi quái thường và quái đặc biệt trong vùng nổ chết ngay (máu cao nhất là Giáp nặng ×1.5), boss mất khoảng 20% máu. Nhờ vậy phép luôn hữu ích từ wave 1 đến cuối màn và trong Vô tận. 

- Đánh trúng cả quái Bay. Hồi chiêu **45 giây** (tính theo thời gian game, nên ở ×2 hồi nhanh gấp đôi theo thời gian thực). 

- Game feel: rung camera nhẹ, quái bị hất tung, số sát thương lớn bay lên. 

## **5. Gacha, Túi và Deck (ngoài trận)** 

Mọi yếu tố ngẫu nhiên nằm ngoài trận: người chơi tiêu Ngọc (tiền ngoài trận, khác với Vàng trong trận) để quay gacha; tướng nhận được vào Túi (kho tướng lâu dài, một tướng có thể có nhiều bản sao); trước mỗi trận chọn 5 tướng từ Túi làm deck. 

Gacha: một banner cố định chứa toàn bộ tướng. Quay ra độ hiếm trước, rồi chọn ngẫu nhiên một tướng thuộc độ hiếm đó. Tỉ lệ đề xuất (cân bằng sau khi chơi thử): 

|Độ hiếm|Tỉ lệ quay|Màu viền|Hệ số sức mạnh|Cách có được|
|---|---|---|---|---|
|Common|65%|Xám|×1|Gacha|
|Rare|25%|Xanh dương|×1.6|Gacha, ghép 3 Common<br>thừa|
|Epic|8.5%|Tím|×2.5|Gacha, ghép 3 Rare thừa|
|Legendary|1.5%|Vàng cam|×4|Gacha, ghép 3 Epic thừa|



Hệ số sức mạnh được nén lại (×1 → ×4) để một màn cân bằng được cho cả người chỉ có tướng Common lẫn người có Legendary. Độ hiếm cao chủ yếu mang lại **cơ chế** (Ultimate, kỹ năng mạnh hơn) và sức mạnh trên mỗi ô quân số, không phải sức mạnh trên mỗi đồng Vàng. 

Quy tắc: 

- **Giá quay:** x1 = 100 Ngọc; x10 = 900 Ngọc và đảm bảo ít nhất 1 Epic trở lên. 

- **Pity:** 25 lượt liên tiếp không ra Legendary thì lượt thứ 25 chắc chắn ra Legendary. Bộ đếm được lưu lại qua các lần quay và hiện trên màn hình Gacha. 

- **Ghép (trong Túi):** chọn 3 bản thừa bất kỳ cùng độ hiếm (Common, Rare hoặc Epic) → nhận 1 tướng ngẫu nhiên thuộc độ hiếm kế tiếp. "Bản thừa" là bản thứ 2 trở đi của một tướng; luôn giữ lại ít nhất 1 bản mỗi tướng, nên ghép không bao giờ làm mất tướng trong deck. 

- **Người chơi mới:** nhận sẵn 5 tướng khởi đầu (Cung thủ Làng, Phù thủy Lửa, Lính Băng, Tu sĩ Làng, Người Tuyết) và 1.000 Ngọc, nên vào trận được ngay. 5 tướng này tính là đã sở hữu từ đầu, không nhận thưởng "lần đầu sở hữu". 

- **Deck:** chọn đúng 5 tướng khác nhau từ Túi trước mỗi trận. Không đổi deck giữa trận. 

- **Hiệu ứng ra tướng:** mỗi độ hiếm có tia sáng màu riêng và âm thanh riêng; ra Legendary có thêm rung màn hình và làm chậm thời gian 1 giây. 

Ghi chú: đồ án không dùng tiền thật; Ngọc chỉ kiếm được trong game (mục 8) và tỉ lệ gacha được công khai trên màn hình Gacha. 

## **6. Tướng: vai trò, hệ và nâng cấp** 

Mỗi tướng có 3 thuộc tính: độ hiếm, vai trò và hệ. Bản 1 tháng có **9 tướng** (4 Common, 2 Rare, 2 Epic, 1 Legendary). 

### **4 vai trò:** 

- **DPS đơn (sát thương Vật lý):** sát thương cao lên 1 mục tiêu, **luôn ưu tiên boss nếu boss trong tầm** ; tướng DPS đơn tầm xa khắc chế quái Bay. 

- **Pháp sư (AoE, sát thương Phép):** sát thương lan, khắc chế bầy quái đông và quái Giáp nặng. 

- **Khống chế (sát thương Phép):** làm chậm, khắc chế quái Nhanh. 

- **Hỗ trợ (sát thương Vật lý, thấp):** tăng tốc đánh hoặc sát thương cho tướng trong bán kính 5 m. 

**3 hệ:** hệ quyết định màu hiệu ứng và hiệu ứng trạng thái mà đòn đánh gây ra. Không có Synergy: số lượng tướng cùng hệ trên sân không tạo thêm buff. 

|Hệ|Màu VFX|Hiệu ứng trạng thái|
|---|---|---|
|Lửa|Cam|Bỏng: 20% sát thương đòn mỗi giây trong 3 giây|
|Băng|Xanh nhạt|Làm chậm 30% trong 2 giây (tối đa 60%, boss chịu một nửa)|
|Lôi|Tím|Sét nảy sang tối đa 2 quái gần (trong 3 m), mỗi lần nảy giảm 30%|



**Danh sách tướng** (sát thương, tốc đánh, tầm đánh của từng tướng nằm ở sheet Tuong trong file Can bang - Fate Bastion.xlsx): 

|Tướng|Độ hiếm|Hệ|Vai trò|Đòn đánh / kỹ năng bị động|
|---|---|---|---|---|
|Cung thủ Làng|Common|Lôi|DPS đơn (tầm<br>xa)|Bắn 1 mũi tên mỗi 0.8 giây, sét nảy theo hệ<br>Lôi|
|Phù thủy Lửa|Common|Lửa|Pháp sư|Quả cầu lửa nổ vùng 2 m|
|Lính Băng|Common|Băng|Khống chế|Ném băng 1 mục tiêu, làm chậm|
|Tu sĩ Làng|Common|Lửa|Hỗ trợ|+15% sát thươngcho tướngtrong5 m|



|Tướng|Độ hiếm|Hệ|Vai trò|Đòn đánh / kỹ năng bị động|
|---|---|---|---|---|
|Người Tuyết|Rare|Băng|Khống chế|Vùng tuyết 3 m làm chậm liên tục|
|Pháp sư Sấm|Rare|Lôi|Pháp sư|Sét đánh vùng 2.5 m, nảy sang quái gần|
|Chiến binh Sấm|Epic|Lôi|DPS đơn (cận<br>chiến)|Chém mạnh; mỗi đòn thứ 3 gây gấp đôi;<br>không đánh được quái Bay|
|Thánh Nữ|Epic|Lửa|Hỗ trợ|+30% tốc đánh cho tướng trong 5 m|
|Long Vương|Legendary|Lửa|Pháp sư|Thở lửa hình nón trước mặt; có Ultimate|



**Ultimate (chỉ Legendary):** thanh Nộ +5 mỗi lần tướng ra đòn (tính theo lần đánh, không tính theo số quái trúng), tối đa 100. Đầy Nộ thì bấm nút Ultimate trong bảng thông tin tướng hoặc phím R; Ultimate tự ngắm vào điểm đông quái nhất trong tầm. 

- **Long Vương – Rồng Thiêng Giáng Thế:** bay lên và phun lửa dọc đoạn đường gần nhất trong 5 giây. 

Ultimate không có cắt cảnh camera, chỉ có hiệu ứng lớn, rung camera và âm thanh riêng, để không ngắt nhịp chơi. 

**Nâng cấp trong trận (mất khi hết trận):** bấm vào từng tướng trên sân, trả Vàng để lên cấp 1→3. Mỗi cấp tăng sát thương, tốc đánh và tầm đánh. Giá và chỉ số từng cấp ở mục 8. 

## **7. Quái và wave** 

Mỗi loại quái ép người chơi cần một vai trò tướng khác, nên đội hình chỉ toàn tướng hiếm vẫn có thể thua. Màn hình Chọn màn hiển thị các loại quái sẽ xuất hiện, để người chơi chọn deck phù hợp trước khi vào trận. 

|Loại quái|Đặc điểm|Khắc chế bằng|
|---|---|---|
|Thường|Máu và tốc độ trung bình|Mọi tướng|
|Nhanh|Tốc độ ×1.8, máu ×0.5|Khống chế|
|Giáp nặng|Giảm 50% sát thương Vật lý, máu ×1.5, chậm hơn|Pháp sư, Thiên thạch|
|Bay|Đi đường bay riêng ngắn hơn, máu ×0.8; tướng cận<br>chiến không đánh được|DPS đơn tầm xa, Thiên thạch|
|Boss|Wave 5, 10, 15; máu ×15; giáp và kháng phép 20%; cứ<br>12 giây gọi 3 quái Nhanh|DPS đơn (ưu tiên boss), Hào<br>quang Chỉ huy|



Boss có thanh máu lớn nổi trên đầu và âm thanh báo khi xuất hiện. Máu gốc, tốc độ, Vàng thưởng của từng loại quái và số quái mỗi wave của cả 3 màn nằm ở sheet Quai và Wave trong file cân bằng. 

Công thức tăng độ khó (máu quái tăng theo số wave n, hệ số 1.12 là giá trị khởi điểm để cân bằng): 

<mark>HP(n) = HP_{0} \cdot 1.12^{\,n-1} \cdot k_{màn}</mark> 

<mark>HP_0</mark> là máu gốc của từng loại quái (sheet Quai); <mark>k_màn</mark> là hệ số máu theo màn: màn 1 = 1.0, màn 2 = 1.15, màn 3 = 1.3 (Vô tận dùng hệ số của màn 3). Thiên thạch cũng nhân cùng hệ số này nên vẫn giết ngay quái không phải boss ở mọi màn. 

**Thắng / thua:** Thành trì có 20 máu. Quái thường lọt vào trừ 1, quái đặc biệt trừ 2, boss trừ 10. Về 0 là thua; sống sót hết wave cuối của màn là thắng. Đánh giá sao theo máu Thành còn lại: ≥ 15 là 3 sao, ≥ 8 là 2 sao, còn sống là 1 sao. 

**Cấu trúc một màn:** 15 wave, mỗi wave khoảng 30–65 giây (các nhóm quái sinh song song trong 8–25 giây, cộng thời gian hạ hết quái; sheet Wave cột calc_duration là giới hạn trên; quái thường cần khoảng 35 giây để đi hết đường), chuẩn bị 15 giây trước wave 1, nghỉ 8 giây giữa các wave; boss ở wave 5, 10, 15. Dữ liệu wave lưu trong ScriptableObject để chỉnh mà không cần sửa code. Máu quái lưu bằng float. 

## **8. Kinh tế và tiến trình** 

Game có hai loại tiền tách biệt: **Vàng** chỉ tồn tại trong một trận (đặt, nâng cấp tướng), **Ngọc** tích lũy ngoài trận (quay gacha). Trong trận không có bảng nâng cấp chung; mọi quyết định là "đặt thêm tướng hay nâng tướng đang có". 

**Nguồn Vàng trong trận:** bắt đầu 300; hạ quái thường +5, quái đặc biệt +10, boss +150; thưởng cuối wave 50 + 10 × số wave; Bỏ qua thời gian nghỉ +50. 

**Đặt tướng:** giá và số con tối đa theo độ hiếm; tổng quân số trên sân tối đa 15. 

|Độ hiếm|Giá đặt|Số con tối đa mỗi tướng|
|---|---|---|
|Common|100|6|
|Rare|200|5|
|Epic|350|4|
|Legendary|600|2|



**Nâng cấp tướng trong trận** (giá tính theo % giá đặt, làm tròn xuống; chỉ số cộng dồn so với cấp 1): 

|Cấp|Giá nâng|Sát thương|Tốc đánh|Tầm đánh|
|---|---|---|---|---|
|1|—|+0%|+0%|+0%|
|2|75%|+40%|+10%|+10%|
|3|150%|+100%|+20%|+15%|



Ví dụ: một tướng Common (giá đặt 100) lên cấp 3 tốn thêm 75 + 150 = 225 Vàng. 

**Bán tướng:** hoàn 50% tổng Vàng đã bỏ vào tướng đó (giá đặt + tiền nâng cấp, làm tròn xuống). Không thể thu hồi tướng về Túi. 

### **Tiến trình ngoài trận:** 

- **Ngọc khởi đầu:** 1.000. 

- **Qua màn:** lần đầu qua màn +300; mỗi sao mới đạt +50 (chơi lại chỉ nhận phần sao cao hơn lần trước). 

- **Vô tận:** +10 mỗi 5 wave sống sót. 

- **Lần đầu sở hữu một tướng:** +50 (chỉ 4 tướng ngoài bộ khởi đầu). 

Ước tính (sheet Gacha của file cân bằng): chơi hết chiến dịch 3 sao được 2.550 Ngọc, tức 27 lượt quay nếu dùng x10, đủ chạm pity 25, nên người chơi hết chiến dịch chắc chắn có ít nhất 1 Legendary. Bảng Debug giúp người chấm xem đủ tính năng nhanh. 

## **9. Chế độ chơi và HUD** 

Chế độ chơi: 

1. **Chiến dịch:** 3 màn trên cùng 1 map, mỗi màn 15 wave. Màn 1 làm quen (Thường, Nhanh), màn 2 thêm Giáp nặng, màn 3 thêm Bay và boss mạnh hơn. 

2. **Vô tận:** mở sau khi qua màn 1. Dùng 15 wave của **màn 3** (có đủ 4 loại quái); sau wave 15, lặp lại mẫu wave 6–15 của màn 3 và máu quái tiếp tục ×1.12 mỗi wave; boss mỗi 5 wave. Kỷ lục số wave lưu trên máy. 

HUD trong trận giữ tối giản để người chơi ngắm hình ảnh tướng và quái; không có minimap, không xem trước wave trong trận. 



<!-- Start of picture text -->
HUD trong tran: 5 6 deck dé dat tudng, 6 Q cho Thién thach<br>Wave 8/15 - 0:18 Téc x2 Cai dat<br>BO qua wave<br><q Qu;i sip ti Thanh:<br>Thanh Nd - Cap 2/3<br>chi hign khi bm vao tuéng<br>Epic: Lita - Hé tro<br>Sat thuong - Téc danh -Tam (thé gidi 3D: Trigu Héi Su va vong Hao quang 6m,<br>Ultimate: chi tuéng Legendary tudng, quai, vong tm Thién thach 20m khi bam Q)<br>Nang (U) 525 || Ban (x) 306<br>Vang 450 Thiénthach | 1.Cung tha 2 Phi thy 3 NgudiTuyét {4 Thanh Na 5 Long Vuong<br>Quan s6.1215._ | @:ndi0:27 100-216 100 - 4/6 200 3/5 350-1/4 600 -thigu Vang<br><!-- End of picture text -->

bố cục HUD trong trận · khung nét đứt = chỉ hiện khi cần 

- **Trên cùng:** Wave hiện tại / tổng và đồng hồ, thanh máu Thành (nháy đỏ khi bị trừ); góc phải có nút Tốc độ ×1/×2 và Cài đặt. 

- **Bỏ qua wave:** bấm sẽ hiện hộp xác nhận "Bỏ qua thời gian chờ? Wave sau bắt đầu ngay, bạn nhận +50 Vàng", có ô "Không hỏi lại". Phím tắt: N. 

- **Cảnh báo:** mũi tên đỏ ở rìa màn hình chỉ hướng quái sắp vào Thành, kèm âm thanh. Vì không có minimap nên cảnh báo phải thật rõ. 

- **Dưới cùng:** Vàng, Quân số (nháy khi đầy), ô Thiên thạch (Q) kèm đồng hồ hồi chiêu, và 5 ô deck. Mỗi ô deck ghi tên tướng, giá đặt và số con đã đặt / tối đa; ô mờ đi khi thiếu Vàng hoặc đã đủ số con. Bấm ô (hoặc phím 1–5) rồi bấm xuống đất để đặt. 

**Bảng thông tin tướng** (chỉ hiện khi bấm vào tướng, bấm ra ngoài hoặc Esc để đóng; dưới chân tướng hiện vòng tầm đánh): 

|Mục|Nội dung|
|---|---|
|Đầu bảng|Tên, độ hiếm (màu viền), hệ, vai trò, cấp trong trận (1–3)|
|Chỉ số|Sát thương, Tốc đánh, Tầm đánh, DPS; khi di chuột lên nút Nâng cấp thì hiện<br>chỉ số cấp sau màu xanh|
|Nâng cấp|Nút kèm giá Vàng của cấp kế tiếp; ẩn khi đạt cấp 3|
|Ultimate (Legendary)|Thanh Nộ và nút kích hoạt (R)|
|Bán|Hiện số Vàng nhận lại (50% tổng đã bỏ vào)|



### **Màn hình ngoài trận:** 

- **Sảnh chính:** Chơi, Gacha, Túi & Deck, Cài đặt, Thoát. 

- **Chọn màn:** 3 màn + Vô tận, số sao đã đạt, danh sách loại quái sẽ xuất hiện. 

- **Gacha:** nút quay x1 / x10, số Ngọc, bộ đếm pity, bảng tỉ lệ công khai, hiệu ứng ra tướng. 

- **Túi & Deck (chung một màn):** lưới tướng sắp theo độ hiếm, bản sao xếp chồng (×3); nút Ghép (chọn độ hiếm, có hộp xác nhận); 5 ô deck phía trên, kéo tướng từ lưới vào; bắt buộc đủ 5 tướng khác nhau mới vào trận được. 

- **Kết quả trận:** Thắng/Thua, số sao, Ngọc nhận, Chơi lại / Về sảnh. 

- **Tạm dừng / Cài đặt:** âm lượng (Tổng, Nhạc, Hiệu ứng), bật/tắt rung camera, hỏi khi bỏ qua wave. 

**Hướng dẫn ở màn 1:** 4 hộp gợi ý ngắn hiện lần lượt theo sự kiện (đặt tướng đầu tiên, nâng cấp, Hào quang Chỉ huy, Thiên thạch); có nút Bỏ qua; chỉ hiện lần đầu (lưu cờ trong save). 

**Bảng Debug (F1, chỉ trong Editor và bản Development):** cộng Ngọc, nhận một tướng bất kỳ, nhảy tới wave bất kỳ, bất tử Thành, đổi tốc độ ×5 để test. Code Debug bọc trong <mark>#if UNITY_EDITOR || DEVELOPMENT_BUILD</mark> nên không có trong bản Release. **Bản nộp** là build Release (IL2CPP); **buổi bảo vệ** chạy bản Development build để dùng Bảng Debug khi cần. 

**Cảm giác (game feel):** số sát thương bay lên, rung nhẹ khi boss xuất hiện và khi Thiên thạch nổ, âm thanh riêng cho từng độ hiếm. 

## **10. Tech stack và quy ước** 

Cả nhóm 3 người dùng chung một stack cố định: Unity 6.3 LTS + URP, viết C# trong VS Code, quản lý bằng Git + GitHub. Không nâng phiên bản Unity hay thêm package giữa chừng nếu chưa cả nhóm đồng ý. 

Engine và build: 

|Hạng mục|Lựa chọn|Ghi chú|
|---|---|---|
|Engine|Unity 6.3 LTS|Cả 3 máy cài đúng cùng một bản vá<br>qua Unity Hub|
|Render pipeline|URP|Bật SRP Batcher, GPU Instancing|
|Nền tảng|Windows x64||
|Scripting backend|Mono khi phát triển, IL2CPP cho bản nộp||



### Package Unity: 

|Mục đích|Package|
|---|---|
|Điều khiển|Input System (Action Map Summoner và UI)|
|Camera|Cinemachine 3.x (Third Person Follow, Deoccluder, Impulse)|
|Đường đi của quái|Splines|
|UI|uGUI + TextMeshPro (font Dynamic có đủ dấu tiếng Việt)|
|Hiệu ứng|Particle System (Shuriken)|
|Lưu dữ liệu|Newtonsoft Json (com.unity.nuget.newtonsoft-json)|
|Kiểm thử|Unity Test Framework|
|Đo hiệu năng|Profiler, Memory Profiler|
|IDE|Visual Studio Editor (com.unity.ide.visualstudio), dùng cho cả VS Code|
|Tween UI (bên thứ ba duy nhất)|PrimeTween (miễn phí, không tạo rác bộ nhớ)|



Không dùng: Addressables, Netcode, VFX Graph, DOTS/ECS, UniTask. 

### Công cụ nhóm: 

|Hạng mục|Công cụ|
|---|---|
|IDE|VS Code + extension Unity (Microsoft) và C# Dev Kit; trong Unity chọn<br>External Script Editor = VS Code|
|Trợ lý code|Claude Code chạy trong thư mục dự án, dùng chung CLAUDE.md trong repo|
|Mã nguồn|Git + GitHub, Git LFS cho .fbx, .png, .psd, .wav, .mp3 (theo dõi hạn mức LFS<br>miễn phí của GitHub)|
|Công việc|GitHub Projects (Kanban), mỗi task một issue|
|3D / 2D|Blender; Krita hoặc GIMP|
|Asset|Quaternius, Kenney, Mixamo, Unity Asset Store (miễn phí)|



Quy trình Git cho 3 người: 

- **Thiết lập một lần (ngày 1):** .gitignore mẫu cho Unity, .gitattributes cho LFS, Project Settings → Editor: Asset Serialization = Force Text, Version Control = Visible Meta Files; cấu hình UnityYAMLMerge làm công cụ merge. 

- **main luôn chạy được.** Mỗi task một nhánh feature/<tên>, merge qua Pull Request có ít nhất 1 người khác review. 

- **Tránh xung đột Scene:** mọi thứ làm thành Prefab; mỗi người có scene thử riêng Scenes/Sandbox/<tên>; scene chính chỉ một người sửa tại một thời điểm (báo trước trong nhóm). 

- **Commit nhỏ, thường xuyên;** pull main mỗi ngày trước khi làm. 

Phân vai: 

|Người|Phụ trách|
|---|---|
|A – Gameplay|Chiến đấu, tướng, quái, wave, kỹ năng và Ultimate, Hào quang Chỉ huy, Thiên<br>thạch, lưu dữ liệu (cùng B), hướng dẫn màn 1|
|B – Hệ thống và UI|Đặt tướng và nâng cấp, luồng trận, gacha, Túi, ghép, deck, lưu dữ liệu (cùng<br>A), gắn dữ liệu vào HUD|
|C – Art và kỹ thuật hình ảnh|Triệu Hồi Sư và camera, dựng map, import model, animation, VFX, âm thanh,<br>bố cục UI, tối ưu hiệu năng, build|



Quy ước code (ghi vào CLAUDE.md): 

- **Thư mục** Assets/_Project/{Scripts, Data, Prefabs, Art, Audio, Scenes}; asset ngoài để trong Assets/ThirdParty/. 

- **Namespace và Assembly Definition theo module:** FateBastion.Core, .Combat, .Heroes, .Enemies, .Player, .Meta, .UI. 

- **Các hệ thống giao tiếp qua event;** UI không gọi thẳng vào logic; Singleton chỉ dùng cho vài Manager. 

- **Mọi con số cân bằng nằm trong ScriptableObject,** không viết cứng trong code. 

- **Đặt tên:** PascalCase cho class và method, _camelCase cho field private, [SerializeField] private thay vì public field. 

Máy tham chiếu và mục tiêu hiệu năng: 

|Cấu hình|Mục tiêu|
|---|---|
|Intel Core i5-12500H + GeForce RTX 3050 (laptop)|≥ 60 FPS ở 1080p, chất lượng Cao, 100 quái trên màn<br>hình|
|Cùng CPU, chỉ GPU tích hợp Intel UHD|≥ 30 FPS ở 1080p, chất lượng Thấp|



Lưu ý: laptop có 2 GPU nên cần vào Windows Settings → Display → Graphics, đặt Unity Editor và file game dùng High performance (RTX 3050); khi đo hiệu năng phải cắm sạc. Đo FPS trên GPU tích hợp ngay cuối tuần 1 để phát hiện sớm rủi ro. 

## **11. Kiến trúc kỹ thuật trong Unity** 

Nguyên tắc chính: dữ liệu tách khỏi code. Tướng, quái, wave, tỉ lệ gacha đều là ScriptableObject, nên cân bằng game chỉ cần sửa số trong Inspector. 

|Hệ thống|Thành phần Unity|Trách nhiệm|
|---|---|---|
|GameManager|MonoBehaviour + state machine|Trạng thái: Chuẩn bị → Đang wave → Nghỉ →<br>Thắng/Thua; tốc độ ×1/×2|
|PlayerController|CharacterController + Input System|Di chuyển, chạy nhanh của Triệu Hồi Sư|
|CameraRig|Cinemachine (Third Person Follow)|Xoay, zoom, tránh vật cản, rung camera|
|CommandAura|Danh sách tướng trên sân + khoảng<br>cách|Buff +25% tốc đánh cho tướng trong 6 m<br>quanh Triệu Hồi Sư|
|MeteorAbility|SkillData (SO) + raycast mặt đất|Ngắm, kiểm tra tầm 20 m, hồi chiêu 45 giây,<br>gây sát thương vùng|
|WaveManager|WaveData (SO)|Sinh quái theo lịch tại Cổng Enemy, xử lý Bỏ<br>qua wave|
|Đường đi|Unity Splines|Quái đi theo đường; không cần NavMesh|
|Enemy|EnemyData (SO) + EnemyController|Máu, giáp, tốc độ, hiệu ứng trạng thái; thanh<br>máu nổi trên đầu|
|Hero|HeroData (SO) + HeroController|Chỉ số, độ hiếm, hệ, kỹ năng; tự tìm mục tiêu<br>và tấn công|
|HeroLevel|HeroLevelTable (SO)|Giá và chỉ số cấp 1–3 trong trận|
|Kỹ năng|SkillData (SO)|Sát thương, hồi chiêu, kiểu vùng, prefab hiệu<br>ứng; Ultimate và thanh Nộ|
|PlacementSystem|Raycast + Layer Placeable +<br>Physics.CheckSphere|5 ô deck, bóng mờ xem trước, giá và giới hạn<br>số con theo độ hiếm, quân số 15, bán|
|GachaSystem|GachaTable (SO) + System.Random|Quay x1/x10, pity lưu lại (ngoài trận)|
|Collection (Túi)|Service class|Bản sao tướng, ghép 3 bản thừa cùng độ<br>hiếm, deck 5 tướng|
|Pooling|UnityEngine.Pool.ObjectPool|Tái dùng quái, tướng, đạn, hiệu ứng, số sát<br>thương|
|Events|C# event / SO event channel|OnEnemyKilled, OnWaveEnd,<br>OnGoldChanged... để HUD không phụ thuộc<br>logic|
|Save|Newtonsoft Json trong<br>Application.persistentDataPath|Ngọc, Túi, deck, pity, tiến độ màn, cài đặt|
|UI|uGUI + TextMeshPro|HUD, 5 ô deck, ô Thiên thạch, bảng thông tin|



|Hệ thống|Thành phần Unity|Trách nhiệm|
|---|---|---|
|||tướng; màn hình Gacha, Túi & Deck|



Lưu ý kỹ thuật: 

- **RNG có seed:** giúp tái hiện lỗi và viết test tỉ lệ gacha (quay 100.000 lần, kiểm tra phân bố đúng bảng). 

- **Tìm mục tiêu:** dùng Physics.OverlapSphereNonAlloc hoặc danh sách quái đang sống, quét 5–10 lần/giây thay vì mỗi frame. 

- **Tiến độ trên đường:** mỗi quái lưu quãng đường đã đi, để "Đầu đoàn" chỉ là so sánh một số. 

- **Tầm đánh tính trên mặt phẳng ngang (XZ):** quái Bay bay cao 2 m nên không dùng khoảng cách 3D. 

- **Triệu Hồi Sư và camera dùng Time.unscaledDeltaTime:** ở ×2 nhân vật vẫn đi đúng tốc độ; khi tạm dừng, Action Map Summoner bị tắt nên nhân vật đứng yên. Cinemachine Brain bật Ignore Time Scale. 

- **Đòn đánh chạy theo timer logic,** không phụ thuộc Animation Event: tướng nằm ngoài camera vẫn gây sát thương đúng, test được bằng code; animation chỉ là hình ảnh đi kèm. 

- **Time.timeScale chỉ do TimeController sửa** (×1, ×2, tạm dừng), tránh nhiều hệ thống cùng đổi. 

- **Unit test:** Unity Test Framework cho gacha, ghép, công thức sát thương, kinh tế và lưu/tải. 

Hiệu năng và hình ảnh (do nhìn gần ở góc thứ 3): dùng LOD và GPU Instancing cho quái đông; phong cách low-poly để dễ đồng bộ; model miễn phí từ Quaternius, Kenney, animation từ Mixamo. 

## **12. Pipeline tạo tướng** 

Mỗi tướng gồm 3 lớp tách biệt: dữ liệu (HeroData), hành vi (script dùng chung) và hình ảnh (model, animation, VFX). Nhờ vậy thêm tướng mới gần như không cần viết code, mất khoảng 30–60 phút mỗi tướng. 

Dữ liệu tướng: 

[CreateAssetMenu(menuName = "Game/Hero")] public class HeroData : ScriptableObject { public string id, displayName; public Rarity rarity;            // Common, Rare, Epic, Legendary public Element element;          // Fire (Lửa), Ice (Băng), Lightning (Lôi) public Role role;                // SingleDps, Mage, Control, Support public DamageType damageType;    // Physical / Magic public Sprite icon, portrait; public float damage, attacksPerSecond, range; public AttackType attackType;    // Melee, Projectile, Area, Cone public Projectile projectilePrefab; public SkillData passive;        // bị động; tạo tự động từ heroes.csv (<id>_passive) public SkillData ultimate;       // chỉ Legendary, còn lại để trống public GameObject modelPrefab; public AnimatorOverrideController animations; 

#### public GameObject rarityAura; } 

Chỉ số thực tế được tính lại khi tướng vào sân hoặc khi có thay đổi (lên cấp, vào/ra Hào quang), không tính mỗi frame: 

<mark>D_{thực} = D_{gốc} \cdot k_{độ\,hiếm} \cdot k_{cấp}</mark> Vòng đời trong trận: 

1. **Ngoài trận:** tướng nằm trong Túi dưới dạng dữ liệu (id tướng, số bản sao). Người chơi chọn 5 tướng vào deck. 

2. **Chọn ô deck:** bấm ô hoặc phím 1–5; model hiện dạng bóng mờ theo chuột kèm vòng tầm đánh. 

3. **Đặt xuống:** kiểm tra đủ Vàng, chưa đạt số con tối đa, quân số dưới 15, chỗ đất hợp lệ; trừ Vàng, lấy từ HeroPool.Get() và gọi Init(heroData) ở cấp 1. 

4. **Hoạt động theo state machine:** Đứng yên ↔ Tấn công (theo quái trong tầm); Legendary đầy Nộ → chờ người chơi bấm Ultimate. 

5. **Nâng cấp:** trả Vàng, tăng cấp, tính lại chỉ số; cấp 3 bật hào quang nhẹ. 

6. **Bán hoặc hết trận:** trả tướng về pool (Release); bán thì hoàn 50% tổng Vàng đã bỏ vào. 

Cấu trúc Prefab (dùng Prefab Variant: sửa Hero_Base là mọi tướng tự cập nhật): 

#### Hero_Base 

- ├── HeroController     state machine Đứng yên / Tấn công / Ultimate 

- ├── HeroStats          tính chỉ số cuối cùng (cấp, Hào quang, buff Hỗ trợ) 

- ├── TargetingModule    tìm quái 5–10 lần/giây 

- ├── AttackModule       đánh thường theo timer, bắn đạn 

- ├── SkillRunner        bị động, Ultimate 

- ├── RangeIndicator     vòng tầm đánh dưới chân 

- └── ModelRoot          nơi đặt model 3D 

Hero_PhuThuyLua (Variant)  → thay model Hero_LongVuong  (Variant)  → thay model + hào quang Legendary Asset: 

- **Chọn một bộ asset cùng phong cách low-poly cho toàn game:** Quaternius (CC0, có sẵn rig), Kenney; nếu có kinh phí thì Synty POLYGON. 

- **Tái sử dụng:** cùng thân người, đổi màu vật liệu, vũ khí, mũ, áo choàng. Chỉ Long Vương cần model riêng. 

- **Độ hiếm thể hiện qua:** kích thước (Legendary to hơn khoảng 1.3 lần), hào quang (Epic ánh tím, Legendary ánh vàng), viền vật liệu. 

- **VFX:** gói hiệu ứng cartoon miễn phí trên Asset Store, đổi màu theo hệ. 

Animation: 

- **Model import với Rig = Humanoid** để mọi clip dùng chung cho mọi model người, kể cả Triệu Hồi Sư (Idle, Walk, Run, Cast). 

- **Clip lấy từ Mixamo:** tải FBX for Unity; clip chỉ lấy chuyển động chọn Without Skin; clip đi và chạy tick In Place. 

- **Một Animator Controller gốc cho mọi tướng;** mỗi tướng dùng Animator Override Controller để thay clip (bắn cung, niệm phép, chém). 

- **Tốc đánh tăng thì tăng tốc độ clip Attack tương ứng,** để hình ảnh khớp với timer logic. 

|State|Dùng khi|Tham số|
|---|---|---|
|Spawn|Tướng vừa được đặt|Tự chạy|
|Idle|Đứng chờ|—|
|Attack|Đánh thường|Attack (trigger)|
|Ultimate|Ultimate (Legendary)|Ultimate (trigger)|



Quy trình thêm một tướng mới: 

1. Import model, đặt Rig = Humanoid. 

2. Tạo Prefab Variant từ Hero_Base, đặt model vào ModelRoot. 

3. Tạo Animator Override Controller, kéo các clip Mixamo vào. 

4. Tạo HeroData: chỉ số, SkillData bị động, icon, hào quang độ hiếm. 

5. Thêm tướng vào GachaTable. 

6. Kiểm tra trong Play Mode: đặt tướng, tự đánh, nâng cấp, đứng trong Hào quang, Ultimate (nếu có). 

Số liệu của 9 tướng, 5 loại quái và 45 wave nằm trong file **Can bang - Fate Bastion.xlsx** (xuất sẵn heroes.csv, enemies.csv, waves.csv). Claude viết công cụ Editor "Import Balance CSV" để tạo hoặc cập nhật HeroData, EnemyData, WaveData theo id; cân bằng chỉ cần sửa file Excel rồi import lại. 

## **13. Tối ưu hiệu năng** 

Mục tiêu: chơi 50 wave Vô tận hoặc nhiều trận liên tiếp mà bộ nhớ đi ngang và không khựng. Nguyên nhân lag phổ biến nhất của game TD là Instantiate / Destroy liên tục, tạo rác bộ nhớ khiến Garbage Collector (GC) dừng game vài chục mili giây mỗi lần dọn. 

**1. Object Pooling:** không bao giờ Destroy, chỉ tắt và dùng lại. Dùng UnityEngine.Pool.ObjectPool<T>, tạo sẵn (prewarm) lúc load màn. 

|Đối tượng|Cách pool|
|---|---|
|Quái|Mỗi loại quái một pool; chết hoặc tới Thành thì Release|
|Đạn, cầu lửa, mũi tên|Pool lớn nhất trong game|
|Hiệu ứng kỹ năng, Thiên thạch,<br>Ultimate|Trả về pool trong OnParticleSystemStopped|
|Số sát thương bay lên|Pool riêng, dễ bị quên|
|Thanh máu nổi trên đầu quái|Gắn sẵn vào prefab quái|
|AudioSource|Pool 20–30 cái cho âm thanh đánh|



Khi dùng pool: luôn gọi ResetState() khi lấy ra (máu, hiệu ứng bỏng/chậm); StopAllCoroutines() và hủy đăng ký event khi trả về; tướng kiểm tra cờ IsAlive trước khi bắn mục tiêu. 

**2. Không tạo rác mỗi frame:** 

- Không dùng LINQ, new List, nối string trong Update(); cache GetComponent trong Awake(). 

- Dùng Physics.OverlapSphereNonAlloc với mảng dựng sẵn. 

- Text HUD chỉ cập nhật khi giá trị đổi, không cập nhật mỗi frame. 

### **3. Giảm số phép tính:** 

- Tướng tìm mục tiêu 5–10 lần/giây, không phải mỗi frame. 

- Hào quang Chỉ huy kiểm tra khoảng cách 5 lần/giây, chỉ tính lại chỉ số tướng khi trạng thái vào/ra thay đổi. 

- Một EnemyManager cập nhật tất cả quái thay vì mỗi quái một Update(). 

- Giới hạn tối đa 100 quái cùng lúc; wave đông thì sinh dồn dần. 

### **4. Tối ưu hình ảnh (góc nhìn thứ 3):** 

- GPU Instancing và SRP Batcher cho quái giống nhau; LOD cho quái ở xa. 

- Animator Culling Mode = Cull Update Transforms để quái ngoài màn hình không tính animation (an toàn vì đòn đánh không phụ thuộc animation). 

- Đặt Max Particles hợp lý, nhất là Thiên thạch và Ultimate của Long Vương. 

**5. Khi kết thúc trận và chuyển màn:** trả mọi quái, đạn, hiệu ứng về pool và xóa danh sách; trong màn hình loading gọi Resources.UnloadUnusedAssets() và System.GC.Collect(); bật Incremental GC trong Project Settings → Player. 

### **6. Kiểm chứng (đưa vào báo cáo đồ án):** 

- Unity Profiler: cột GC Alloc trong lúc chơi gần 0 B mỗi frame. 

- Chạy Vô tận 50 wave ở ×2 (bật bất tử Thành trong Bảng Debug), theo dõi Memory Profiler: bộ nhớ đi ngang là đạt, tăng dần là còn rò rỉ. 

- Ghi FPS trung bình và FPS thấp nhất trước và sau khi tối ưu. 

## **14. Lộ trình phát triển với Claude** 

Lộ trình 4 tuần. Mốc quan trọng nhất là **cuối tuần 1 có bản graybox chơi được trọn 1 màn** ; **cuối ngày 21 khóa tính năng** (feature freeze), tuần 4 chỉ sửa lỗi, cân bằng và làm báo cáo. 

Đặc tả chi tiết từng hệ thống và thứ tự triển khai cho Claude nằm ở tab Đặc tả kỹ thuật. 



<!-- Start of picture text -->
4 tuan: choi dude tron 1 man graybox vao cudi tudn 1<br>r) Tuan 1- Core loop graybox (méc: chai tron 1 man)<br>$0 ngay 1-2 (interface, data, Import Balance CSV); quai theo Spline, wave, dat/ban tuéng, tuéng ty danh, Vang, mau Thanh,<br>théng/thua; Trigu Héi Su + camera; chon asset pack<br>Claude: $0, DamageCalculator, WaveManager, PlacementSystem, GameManager + unit test<br>Tuan 2- N6i dung tran<br>Nang cép 3 cp, 3 h@, 4 loal qudi + boss, Hao quang Chi huy, Thién thach, Ultimate Long Vuang, HUD tran; nhap da ligu<br>wave 3 man + V6 tan tu file can bang<br>Claude: SkillData, StatusEtfect, MeteorAbility, CommandAura, HUD nhan di ligu qua event<br>Tuan 3 - Meta va hinh anh (feature freeze cuéi ngay 21)<br>B: Gacha x1/x10 + pity, Tui, Ghép, Deck, man hinh sénh «A: luu JSON, huéng dn man 1 C: dung map that, anh sng, am<br>thanh<br>Claude: GachaService + test 100.000 lugt, SaveService, TutorialController<br>Tuan 4 - Can bang, build, bao cdo<br>Playtest, sifa Idi, Debug panel, build IL2CPP, do Profiler, quay video demo<br>Claude: phan tich log playtest, review code, soan bdo céo va slide bao vé<br><!-- End of picture text -->

lộ trình 4 tuần · chỉnh theo lịch đồ án thực tế 

|Tuần|A – Gameplay|B – Hệ thống và UI|C – Art và kỹ thuật|
|---|---|---|---|
|1|S0 cùng nhóm; S1 Chiến đấu,<br>S2 Quái và Wave|S0 cùng nhóm; S4 Đặt tướng,<br>S5 Luồng trận|S0 cùng nhóm; S3 Triệu Hồi Sư<br>và Camera, map graybox; chọn<br>và tải asset pack|
|2|S6 Kỹ năng và Ultimate, S7 Hào<br>quang và Thiên thạch, boss;<br>Import Balance CSV, dữ liệu<br>wave 3 màn + Vô tận|Nâng cấp 3 cấp, S10 HUD trận|Import model, animation, VFX<br>cơ bản|
|3|S9 Lưu trữ (hỗ trợ B), hướng<br>dẫn màn 1, cân bằng lần 1|S8 Gacha/Túi/Ghép/Deck, màn<br>hình sảnh|Dựng map thật, ánh sáng, S11<br>âm thanh, bố cục UI|
|4|Cân bằng, sửa lỗi|Bảng Debug, sửa lỗi UI|Build IL2CPP, đo Profiler, quay<br>video demo|



**Báo cáo đồ án:** dựng khung báo cáo từ tuần 2; chụp Profiler, bảng cân bằng và ảnh màn hình ngay khi có, không dồn sang tuần 4. 

**Phao cứu sinh** (nếu trễ, cắt theo thứ tự): Ultimate → Ghép → Vô tận → tốc độ ×2. Ba trụ cốt lõi (gacha + deck, đặt và nâng tướng, Triệu Hồi Sư với Hào quang và Thiên thạch) không được cắt. 

Cách làm việc với Claude trong mỗi tuần: 

1. **Ngày 1–2:** cho Claude dựng cấu trúc project, viết CLAUDE.md và các interface/data dùng chung (HeroData, EnemyData, IDamageable, GameEvents) để 3 người làm song song ngay. 

2. **Giao từng tính năng nhỏ, kèm tiêu chí xong.** Ví dụ: "Viết MergeService: chọn 3 bản thừa cùng độ hiếm, tạo 1 tướng ngẫu nhiên bậc kế; viết test cho trường hợp thiếu bản thừa và trường hợp Epic → Legendary". 

3. **Cho Claude lên kế hoạch trước,** duyệt rồi mới sửa code. 

4. **Mỗi tối một người chơi thử 1 màn,** báo lại cảm giác cụ thể ("boss wave 10 quá trâu, chết 3/3 lần"). 

5. **Commit git sau mỗi tính năng chạy ổn.** 

Lưu ý: Claude không mở được Unity Editor để kéo thả prefab hay chơi thử, nên phần gắn component, dựng scene và cảm nhận độ vui là việc của nhóm. Model 3D, animation và hiệu ứng nên lấy từ Quaternius, Kenney, Mixamo và Unity Asset Store (có nhiều gói miễn phí) để tập trung thời gian vào gameplay. 

## **15. Lịch sử phiên bản** 

|Phiên bản|Ngày|Thay đổi chính|
|---|---|---|
|v1.0|02/10/2026|Bản đầu: 6 độ hiếm, Nhập Hồn, lên sao, lộ trình 12 tuần|
|v1.1|04/10/2026|Rút gọn cho 1 tháng: bỏ Nhập Hồn; thêm Hào quang Chỉ huy và Thiên thạch; 4<br>độ hiếm, 9 tướng, 4 loại quái + boss, 3 màn + Vô tận; lộ trình 4 tuần|
|v1.2|04/10/2026|Sửa sau review: phím Shift, Esc, N; quy tắc cộng buff; vai trò DPS đơn ưu tiên<br>boss; Vô tận dùng màn 3; bản Release và Development; thêm file cân bằng;<br>boss ×15, pity 25|
|v1.3|05/10/2026|Thời lượng wave 30–65 giây; hệ số máu theo màn; quy tắc import CSV và mã<br>enum; kiểu đánh MeleeHit; ngưỡng bấm chuột phải; boss có giáp 20%|



