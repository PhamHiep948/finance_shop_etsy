# Tiêu chí nghiệm thu

Mỗi tiêu chí kiểm được bằng thao tác trên giao diện hoặc gọi API. Cột "Đã kiểm" ghi kết quả đợt chuyển sang kiến trúc mới (11/10/2026), chạy trên bản sao database demo.

| ID | Chức năng | Tiêu chí | Đã kiểm |
|---|---|---|---|
| AC01 | F02 | Thêm khoản thu 20 USD, thuế 10% → sau thuế 22 USD | ✔ API |
| AC02 | F02 | Gán loại chi cho khoản thu → lỗi "Danh mục không thuộc loại thu/chi đã chọn." | ✔ API |
| AC03 | F02 | Dữ liệu thiếu / sai → HTTP 400, câu "Kiểm tra lại ngày, mô tả, danh mục, số tiền và thuế." | ✔ API |
| AC04 | F02 | Bấm dòng mở chi tiết; Sửa → Hủy / Cập nhật quay lại chi tiết; bấm nền mờ đóng hẳn | ✔ Giao diện |
| AC05 | F02 | Tìm "xw-1004" ra đúng đơn có tiền xưởng mang ID đó | ✔ Giao diện |
| AC06 | F04 | Tiền ship trong EU 5,79 @19% → khoản chi trước thuế 4,87, sau thuế 5,79, gắn đúng khoản thu | ✔ API |
| AC07 | F04 | Sửa tiền ship lần 2 cập nhật đúng khoản chi cũ; sửa khoản chi trên trang Khoản chi vẫn giữ liên kết | ✔ API |
| AC08 | F04 | Nhập 0 → khoản chi gắn kèm vào thùng rác | ✔ API |
| AC09 | F05 | Tải lên `.txt` → xem inline; `?download=1` → tải về; `.exe` bị chặn; xóa → 404 | ✔ API |
| AC10 | F06 | Trùng tên loại bị chặn; loại đang dùng không xóa được; phạm vi đang dùng xóa được và khoản về trống | ✔ API |
| AC11 | F07 | Nhập 0 đơn → "Chưa chọn đơn nào để nhập."; thiếu ngày → 400 | ✔ API |
| AC12 | F07 | Đơn đã nhập (kể cả trong thùng rác) không nhập lại | ✔ Mã nguồn + thử trước đây |
| AC13 | F08 | Tỷ giá 0,0005 bị chặn; 1,1513 lưu được; xóa lần 2 báo không tìm thấy | ✔ API |
| AC14 | F09 | Tick loại trong khung chọn không vẽ lại trang; bấm ra ngoài đóng khung và áp dụng | ✔ Giao diện |
| AC15 | F09 | Ô ngày gõ "29082026" thành 29/08/2026, Tab sang ô kế tiếp | ✔ Giao diện |
| AC16 | F11 | Xóa khoản thu kéo theo khoản chi gắn kèm; khôi phục khôi phục cả hai; xóa vĩnh viễn biến mất khỏi thùng rác | ✔ API |
| AC17 | F12 | Ngày thuộc quý đã khóa (sau ngày ân hạn) → không thêm / sửa / xóa | ✔ Unit test |
| AC18 | Dữ liệu | Chạy `Fin.Migration` trên database cũ: giữ nguyên toàn bộ khoản, chuyển liên kết tiền xưởng sang cột mới | ✔ 35 khoản, 9 liên kết |
| AC19 | Dữ liệu | Chạy `Fin.Migration` lần 2 → "Pending migrations: 0" | ✔ |

**Chưa kiểm được trong đợt này:** gọi Etsy thật (lấy đơn, đồng bộ tự động) vì cần tài khoản đã kết nối — kiểm bằng nút "Đồng bộ ngay" trên máy thật.
