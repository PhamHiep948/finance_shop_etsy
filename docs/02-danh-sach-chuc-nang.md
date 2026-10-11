# Danh sách chức năng

| ID | Chức năng | Màn hình | Trạng thái |
|---|---|---|---|
| F01 | Tổng quan (dashboard) | `#/dashboard` | Có |
| F02 | Quản lý khoản thu | `#/incomes` | Có |
| F03 | Quản lý khoản chi | `#/expenses` | Có |
| F04 | Tiền xưởng / tiền ship gắn với khoản thu | panel chi tiết khoản thu | Có |
| F05 | Chứng từ đính kèm | panel chi tiết | Có |
| F06 | Loại thu / loại chi / phạm vi khoản chi | hộp "Loại thu", "Loại chi", "Phạm vi" | Có |
| F07 | Đồng bộ đơn Etsy (tay + tự động) | hộp "Đồng bộ Etsy" | Có |
| F08 | Tỷ giá EUR → USD theo tháng | `#/fx` | Có |
| F09 | Báo cáo + nhóm loại + xuất Excel / PDF | `#/reports` | Có |
| F10 | Lịch sử chỉnh sửa | `#/history` | Có |
| F11 | Thùng rác (30 ngày) | `#/trash` | Có |
| F12 | Khóa sổ theo quý / tháng | toàn ứng dụng | Có |

## F01 Tổng quan

Doanh thu, chi phí, lợi nhuận và số giao dịch trong 7 ngày qua; biểu đồ xu hướng; doanh thu theo kênh; sản phẩm bán chạy; phân bổ ngoại tệ.

## F02 / F03 Khoản thu, khoản chi

- Bảng nhiều cột, **chọn cột hiển thị** và thứ tự cột (lưu trên trình duyệt).
- 3 ô tổng **theo đúng bộ lọc đang chọn**: số đơn / số khoản, tổng trước thuế, tổng sau thuế.
- Bộ lọc: thời gian (gõ `dd/mm/yyyy` hoặc chọn lịch), loại, nguồn (nhập tay / Etsy / từ khoản thu), khu vực (trong / ngoài EU) hoặc phạm vi.
- Tìm kiếm: tên sản phẩm, mã đơn, **ID hóa đơn tiền xưởng / ship** (khoản thu); nội dung, bên nhận, ID hóa đơn, mã đơn (khoản chi).
- Bấm vào dòng → **panel chi tiết**; nhấp đúp vào từng trường trong panel để sửa nhanh; nút Sửa mở form đầy đủ, Hủy / Cập nhật quay lại chi tiết.
- Cột **Tỷ giá** (tỷ giá tháng của khoản, `*` khi chưa nhập) và cột **Tiền xưởng / Tiền ship**.
- Mã đơn Etsy là link mở đơn trong Shop Manager của Etsy.

## F04 Tiền xưởng / tiền ship

Nhập số tiền đã trả, % thuế trong số tiền, ID hóa đơn, ngày chi → app tạo / cập nhật **một khoản chi gắn với khoản thu** (nguồn "Từ khoản thu"). Ngoài EU gán loại "Tiền xưởng", trong EU gán loại "Phí ship". Nhập 0 là bỏ.

## F05 Chứng từ

Ảnh, PDF, Word, Excel, CSV, TXT, ZIP; tối đa 10 file / lần, 10 MB / file. Ảnh / PDF / TXT xem ngay trên trình duyệt, loại khác tải về.

## F06 Loại và phạm vi

- Loại thu, loại chi: thêm, đổi tên, xóa (chỉ xóa khi không còn khoản nào dùng, kể cả trong thùng rác).
- Phạm vi khoản chi: thêm, đổi tên, **xóa tự do** — khoản đang dùng chuyển về để trống phạm vi.

## F07 Đồng bộ Etsy

- Kết nối tài khoản Etsy bằng OAuth (PKCE); tự tìm Shop ID.
- Lấy đơn đã thanh toán trong khoảng ngày (tối đa 1 năm / lần), xem trước, chọn đơn rồi nhập.
- **Tự động đồng bộ**: bật / tắt, chu kỳ 15 phút · 30 phút · 1 giờ · 6 giờ · 1 ngày; mỗi lần lấy đơn 14 ngày gần nhất; nút "Đồng bộ ngay".
- Đơn Etsy nhập vào là dữ liệu gốc: chỉ xem / xóa, không sửa (trừ tiền xưởng / ship).

## F08 Tỷ giá

Nhập `1 EUR = ? USD` cho từng tháng (0,5 – 3); xem kèm tỷ giá ECB trung bình tháng để tham khảo; tháng chưa nhập dùng mặc định 1.

## F09 Báo cáo

Lọc theo thời gian, loại giao dịch, nguồn, **chọn nhiều loại thu / chi**, **nhóm loại** (đặt tên, quản lý dạng danh sách – chi tiết); tab Tổng quan / Theo ngày / Theo tháng / Theo loại thu / Theo loại chi; xuất Excel, PDF.

## F10 – F12

Lịch sử ghi mọi thao tác thêm / sửa / xóa / khôi phục / xóa vĩnh viễn và thay đổi từng trường; thùng rác giữ 30 ngày; khóa sổ chặn thêm / sửa / xóa trong kỳ đã qua (cho phép thêm GraceDays ngày đầu kỳ sau).
