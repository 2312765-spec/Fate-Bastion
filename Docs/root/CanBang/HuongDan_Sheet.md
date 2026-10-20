| Bảng cân bằng – Thủ Thành Vận Mệnh (bản 1 tháng)                                                                                                                                           |
|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Cách đọc màu                                                                                                                                                                               |
| Chữ xanh dương = số nhập tay (được phép sửa để cân bằng). Chữ đen = công thức, không sửa. Chữ xanh lá = lấy từ sheet khác.                                                                 |
| Ô nền vàng = giả định quan trọng cần kiểm chứng khi chơi thử.                                                                                                                              |
| Các sheet                                                                                                                                                                                  |
| HangSo: hằng số dùng chung (máu tăng theo wave, Vàng khởi đầu, Thiên thạch, Hào quang...).                                                                                                 |
| DoHiem, CapDo: bảng độ hiếm và nâng cấp trong trận (khớp mục 5, 8 của GDD).                                                                                                                |
| Tuong: 9 tướng. Hàng 1 là tên trường (đừng sửa), hàng 2 là mô tả tiếng Việt (bắt đầu bằng #, công cụ import bỏ qua). Cột A–O được import; cột calc_* chỉ để so sánh.                       |
| Quai: 5 loại quái. Cột A–J là dữ liệu cho EnemyData.                                                                                                                                       |
| Wave: 3 màn × 15 wave. Cột A–H là dữ liệu cho WaveData; cột I trở đi mô phỏng máu, Vàng và độ khó từng wave.                                                                               |
| Gacha: ước tính Ngọc kiếm được trong chiến dịch, số lượt quay và khả năng có Legendary.                                                                                                    |
| Cách dùng với Claude Code                                                                                                                                                                  |
| 1. Cân bằng trong file này, nhìn cột ""Tỉ lệ sức mạnh"" ở sheet Wave: khoảng 1.2–2.0 là vừa, dưới 1.0 là quá khó, trên 2.5 là quá dễ.                                                      |
| 2. Xuất sheet Tuong → heroes.csv, Quai → enemies.csv, Wave → waves.csv bằng File → Save As → CSV UTF-8, lưu vào Data/Balance/ trong repo. Giữ nguyên hàng 1 (tên trường) và hàng 2 (#...). |
|    Cột hệ, vai trò, kiểu đánh, buff dùng mã tiếng Anh khớp enum trong code (xem hàng 2). Bay/Boss ghi TRUE/FALSE.                                                                          |
| 3. Trong Unity chạy Tools → Import Balance CSV (công cụ viết ở S0, quy tắc ánh xạ ở bảng S0 trong file Đặc tả).                                                                            |
| 4. Chơi thử, ghi cảm giác vào cột Ghi chú, chỉnh số chữ xanh dương, xuất lại CSV, import lại.                                                                                              |
| Lưu ý: mô phỏng ở sheet Wave rất thô (không tính vị trí đặt, AoE, làm chậm, Thiên thạch). Chỉ dùng để so sánh độ khó giữa các wave và các màn, không thay được chơi thử.                   |
