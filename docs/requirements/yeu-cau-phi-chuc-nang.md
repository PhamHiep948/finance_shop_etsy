# Yêu cầu phi chức năng

## Môi trường

| Mục | Yêu cầu |
|---|---|
| Máy chạy | Windows 10/11, chạy local, một người dùng |
| Runtime | .NET SDK 9.0.x |
| Database | MariaDB 10.4 (XAMPP) hoặc MySQL 8; bảng mã `utf8mb4` |
| Trình duyệt | Chrome / Edge bản mới |
| Cổng | API + giao diện: `5000`; MySQL XAMPP: theo cấu hình (máy dev dùng `3307`) |

## Dữ liệu

- **Toàn vẹn:** khoản và lịch sử của nó được lưu trong cùng một transaction (thêm, sửa, nhập đơn Etsy, tiền xưởng).
- **Không mất dữ liệu khi nâng cấp:** `Fin.Migration` nhận diện database cũ và không tạo lại bảng; mọi thay đổi bảng đi qua migration có tên.
- **Xóa mềm:** mọi lệnh xóa từ giao diện chỉ chuyển vào thùng rác; xóa vĩnh viễn cần thao tác riêng hoặc sau 30 ngày.
- **Sao lưu:** người dùng tự `mysqldump` (xem [DATABASE.md](../DATABASE.md)) trước khi nâng cấp.

## Bảo mật

- Key Etsy (`etsy.settings.json`) và token (`etsy.tokens.json`) nằm ngoài git (`backend/.gitignore`).
- Kết nối Etsy dùng OAuth 2 + PKCE; `state` dùng một lần.
- File tải lên: kiểm tra đuôi file và dung lượng, lưu tên ngẫu nhiên, trả về kèm `X-Content-Type-Options: nosniff`.
- CORS chỉ mở cho các origin cấu hình trong `Cors:Origins`.
- Lỗi hệ thống không trả chi tiết nội bộ (chuỗi kết nối, tên bảng) trừ khi bật `AppSetting:EnableErrorDetail`.

## Hiệu năng

- Danh sách phân trang tối đa 100 dòng / trang; frontend tải đủ các trang một lần khi mở ứng dụng.
- Số chứng từ mỗi khoản đếm ngay trong câu truy vấn danh sách (không truy vấn từng dòng).
- Tỷ giá ECB được giữ trong bộ nhớ (ngày đã qua không đổi; tỷ giá mới nhất làm mới sau 6 giờ).
- File JS / CSS gửi kèm `Cache-Control: no-cache`: sửa xong chỉ cần F5.

## Chất lượng code

- Build không cảnh báo; analyzer chặn `DateTime.Now`, `.Result`, `.Wait()`, `new HttpClient()`.
- Comment trong code bằng tiếng Anh, không comment mô tả trên khai báo; chuỗi hiển thị tiếng Việt.
- Unit test cho quy tắc nghiệp vụ trọng yếu (`Fin.Tests.Unit`, xUnit).

## Vận hành

- Đồng bộ Etsy tự động chạy trong tiến trình API: tắt API thì không đồng bộ.
- Đổi `etsy.settings.json` có hiệu lực ngay, không cần khởi động lại.
- Log ra console (ASP.NET logging); câu lệnh SQL chỉ hiện khi hạ mức log `Microsoft.EntityFrameworkCore.Database.Command`.
