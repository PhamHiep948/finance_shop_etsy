# Kiến trúc thông tin

## Cây màn hình

```text
Ứng dụng (http://localhost:5000)
├── Thanh trên cùng: đường dẫn trang · nút $ USD / € EUR · nút giao diện sáng / tối
├── #/dashboard   Tổng quan
├── #/incomes     Khoản thu
│   ├── Hộp Đồng bộ Etsy (kết nối, tự động đồng bộ, tỷ giá tháng, chọn đơn để nhập)
│   ├── Hộp Loại thu
│   ├── Hộp Cột hiển thị
│   ├── Panel chi tiết (nhấp đúp để sửa nhanh, chứng từ, tiền xưởng / ship)
│   └── Form thêm / sửa
├── #/expenses    Khoản chi (giống Khoản thu, thêm hộp Phạm vi; không có Etsy, tiền xưởng)
├── #/reports     Báo cáo (bộ lọc 1 hàng, cửa sổ Nhóm loại, xuất Excel / PDF)
├── #/fx          Tỷ giá tháng
├── #/history     Lịch sử chỉnh sửa
└── #/trash       Thùng rác
```

## Quy ước tương tác

| Tình huống | Hành vi |
|---|---|
| Bấm vào một dòng của bảng | Mở panel chi tiết |
| Nhấp đúp vào trường trong panel chi tiết | Trường thành ô nhập; Enter / bấm ra ngoài = lưu, Esc = hủy |
| Trường bắt buộc | Có dấu `*` đỏ ở cả form và panel chi tiết |
| Hủy / Cập nhật trong form sửa | Quay lại panel chi tiết của khoản đó |
| Bấm vùng nền mờ | Đóng hẳn panel / form |
| Ô ngày | Gõ `dd/mm/yyyy` (tự nhảy ngày → tháng → năm) hoặc chọn trên lịch; áp dụng khi Enter / Tab / bấm ra ngoài |
| Đổi USD / EUR | Mọi số tiền quy đổi theo tỷ giá tháng của từng khoản |
| Mã đơn Etsy | Link mở đơn trong Shop Manager (tab mới) |
| Khoản Etsy | Chỉ xem / xóa; riêng tiền xưởng / ship sửa được |
| Kỳ đã khóa sổ | Biểu tượng ổ khóa; nút sửa / xóa bị vô hiệu |

## Trạng thái màn hình

- **Đang tải:** dữ liệu tải một lần khi mở ứng dụng, các trang dùng chung kho dữ liệu (`store.js`).
- **Trống:** bảng hiện hướng dẫn "Bắt đầu bằng cách thêm…".
- **Lỗi:** thông báo góc màn hình (toast) với câu báo tiếng Việt lấy từ `message` của API.
- **Thành công:** toast ngắn ("Đã thêm khoản thu", "Đã lưu tiền xưởng / tiền ship…").

## Ánh xạ màn hình ↔ API

| Màn hình | API chính |
|---|---|
| Tổng quan, Khoản thu, Khoản chi | `GET /incomes`, `GET /expenses`, `GET /categories/{type}`, `GET /settings`, `GET /fx`, `GET /fx/monthly` |
| Form / panel khoản thu, chi | `POST`, `PUT`, `DELETE /incomes|expenses/{id}`, `PUT /incomes/{id}/cost`, `…/{id}/attachments` |
| Đồng bộ Etsy | `/etsy/status`, `/etsy/connect`, `/etsy/orders`, `/etsy/import`, `/etsy/autosync` |
| Báo cáo | dữ liệu đã tải + `/category-groups` |
| Tỷ giá | `/fx/months`, `PUT|DELETE /fx/monthly/{month}` |
| Lịch sử | `GET /audit` |
| Thùng rác | `GET|DELETE /trash`, `POST /trash/{id}/restore`, `DELETE /trash/{id}` |

Chi tiết từng API: [api/README.md](api/README.md).
