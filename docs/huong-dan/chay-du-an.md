# Cài đặt và chạy dự án

## 1. Chuẩn bị

| Công cụ | Kiểm tra |
|---|---|
| .NET SDK 9.0.x | `dotnet --list-sdks` |
| XAMPP (MariaDB) đang chạy | mở XAMPP Control Panel → MySQL **Start** |
| Trình duyệt Chrome / Edge | — |

Frontend là HTML / JS thuần, **không cần Node.js**.

## 2. Cấu hình

1. Mở `backend/Host/Fin.Host.Api/appsettings.json`, sửa chuỗi kết nối cho đúng máy:

   ```json
   "ConnectionStrings": {
     "Finance": "Server=127.0.0.1;Port=3307;Database=finance;User ID=root;Password="
   }
   ```

   Làm tương tự ở `backend/Migrations/Fin.Migration/appsettings.json`.
   Nếu không muốn sửa file chung, tạo `appsettings.Development.json` (đã có mẫu `appsettings.Development.example.json`, file này không đưa lên git).

2. (Nếu dùng Etsy) chép `backend/Host/Fin.Host.Api/etsy.settings.example.json` thành `etsy.settings.json` cùng thư mục, điền `ApiKey`, `SharedSecret`. Callback URL khai báo trên Etsy: `http://localhost:5000/api/v1/etsy/callback`.

## 3. Tạo / nâng cấp database

```bash
cd backend
dotnet tool restore
dotnet run --project Migrations/Fin.Migration
```

- Database chưa có bảng: tạo đủ bảng + dữ liệu mặc định.
- Database của backend cũ: giữ nguyên dữ liệu, chỉ bổ sung phần mới (xem [DATABASE.md](../DATABASE.md)).
- Kết quả đúng: dòng cuối `Database is up to date`; chạy lần 2 báo `Pending migrations for FinanceContext: 0`.

Database `finance` phải đã được tạo (trống cũng được): `CREATE DATABASE finance CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;`

## 4. Chạy ứng dụng

```bash
cd backend
dotnet run --project Host/Fin.Host.Api
```

| Địa chỉ | Nội dung |
|---|---|
| `http://localhost:5000` | Giao diện |
| `http://localhost:5000/health` | `{"status":"healthy"}` |
| `http://localhost:5000/swagger` | Danh sách API (môi trường Development) |

Muốn tắt khóa sổ tạm thời (ví dụ nhập bù dữ liệu quý trước): đặt `PeriodLock:Enabled` = `false` trong `appsettings.json`, hoặc chạy với biến môi trường `PeriodLock__Enabled=false`.

## 5. Kiểm thử

```bash
cd backend
dotnet build Fin.Finance.sln          # phải 0 Warning, 0 Error
dotnet test Tests/Fin.Tests.Unit       # unit test quy tắc nghiệp vụ
```

Kết quả đợt chuyển kiến trúc (11/10/2026): build 0 cảnh báo; 24/24 unit test đạt; 35/35 kiểm tra API ghi dữ liệu đạt trên bản sao database.

## 6. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| "Không kết nối được MySQL…" | XAMPP chưa Start MySQL, sai cổng / mật khẩu trong `ConnectionStrings:Finance`, hoặc chưa chạy `Fin.Migration` |
| Trang trắng / không đổi sau khi sửa JS | Tải lại trang (F5); file tĩnh không bị cache lâu |
| Cổng 5000 đang bận | Tắt tiến trình đang chiếm cổng, hoặc đổi `applicationUrl` trong `Properties/launchSettings.json` (nhớ đổi cả Callback URL Etsy) |
| Etsy báo từ chối key | App Etsy còn "Pending", hoặc chép sai Keystring / Shared Secret trong `etsy.settings.json` |
| Build lỗi `RS0030` | Dùng API bị cấm (`DateTime.Now`, `.Result`, `new HttpClient()`); đổi sang `TimeProvider`, `await`, `IHttpClientFactory` |
