# Finance Shop Etsy

Ứng dụng quản lý thu chi cho shop handmade bán trên Etsy: ghi khoản thu / chi, tự đồng bộ đơn Etsy, tách VAT cho đơn trong EU, gắn tiền xưởng / tiền ship vào từng đơn, quy đổi USD ↔ EUR theo tỷ giá tháng, báo cáo phục vụ khai thuế, lịch sử chỉnh sửa, thùng rác và khóa sổ theo quý.

## Kiến trúc

- **Backend** `backend/` — ASP.NET Core 9, Clean Architecture theo module (dựa trên khung CMS_VSF): `Domain → Infrastructure → Service (CQRS: MediatR + FluentValidation) → Presentation → Host`; EF Core 9 + Pomelo trên MySQL / MariaDB; chương trình `Fin.Migration` quản lý database.
- **Frontend** `frontend/` — HTML / CSS / JavaScript thuần, được API phục vụ tại cùng địa chỉ.
- **Tài liệu** `docs/` — phạm vi, chức năng, use case, dữ liệu, API, quy tắc nghiệp vụ, hướng dẫn.

## Cách chạy

### 1. Chuẩn bị

| Công cụ | Ghi chú |
|---|---|
| .NET SDK 9.0.x | kiểm tra: `dotnet --list-sdks` |
| XAMPP (MariaDB / MySQL) | mở XAMPP Control Panel → MySQL **Start** |
| Chrome / Edge | frontend là HTML / JS thuần, không cần Node.js |

Tạo database (chỉ làm một lần, trống cũng được):

```sql
CREATE DATABASE finance CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
```

### 2. Cấu hình

Sửa chuỗi kết nối cho đúng máy (cổng, mật khẩu) ở **cả hai** file:

- `backend/Host/Fin.Host.Api/appsettings.json`
- `backend/Migrations/Fin.Migration/appsettings.json`

```json
"ConnectionStrings": {
  "Finance": "Server=127.0.0.1;Port=3307;Database=finance;User ID=root;Password="
}
```

Không muốn sửa file chung thì chép `appsettings.Development.example.json` thành `appsettings.Development.json` (file này không đưa lên git).

Nếu dùng Etsy: chép `backend/Host/Fin.Host.Api/etsy.settings.example.json` thành `etsy.settings.json`, điền `ApiKey`, `SharedSecret`. Callback URL khai báo trên Etsy: `http://localhost:5000/api/v1/etsy/callback`.

### 3. Tạo / nâng cấp database

```bash
cd backend
dotnet tool restore
dotnet run --project Migrations/Fin.Migration
```

Tạo đủ bảng + dữ liệu mặc định; chạy lại nhiều lần vẫn an toàn. Kết quả đúng: dòng cuối `Database is up to date`.

### 4. Chạy ứng dụng

```bash
cd backend
dotnet run --project Host/Fin.Host.Api
```

| Địa chỉ | Nội dung |
|---|---|
| http://localhost:5000 | Giao diện |
| http://localhost:5000/swagger | Danh sách API |
| http://localhost:5000/health | Kiểm tra API (`{"status":"healthy"}`) |

Dừng bằng `Ctrl+C`.

### 5. Kiểm thử

Tắt API trước khi build (API đang chạy sẽ khóa file `.dll`, báo lỗi `MSB3021`).

```bash
cd backend
dotnet build Fin.Finance.sln
dotnet test Tests/Fin.Tests.Unit
```

### Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Không kết nối được MySQL | XAMPP chưa Start MySQL, sai cổng / mật khẩu trong `ConnectionStrings:Finance`, hoặc chưa chạy `Fin.Migration` |
| Build lỗi `MSB3021` (file đang bị dùng) | API vẫn đang chạy — `Ctrl+C` rồi build lại |
| Cổng 5000 đang bận | Tắt tiến trình chiếm cổng, hoặc đổi `applicationUrl` trong `Host/Fin.Host.Api/Properties/launchSettings.json` (nhớ đổi cả Callback URL Etsy) |
| Etsy từ chối key | App Etsy còn "Pending", hoặc sai Keystring / Shared Secret trong `etsy.settings.json` |

Chi tiết hơn: [docs/huong-dan/chay-du-an.md](docs/huong-dan/chay-du-an.md).

## Tài liệu

Bắt đầu từ [docs/README.md](docs/README.md).

| Cần biết | Đọc |
|---|---|
| Ứng dụng làm gì, không làm gì | [docs/01-pham-vi.md](docs/01-pham-vi.md), [docs/02-danh-sach-chuc-nang.md](docs/02-danh-sach-chuc-nang.md) |
| Backend chia tầng thế nào | [docs/08-kien-truc-backend.md](docs/08-kien-truc-backend.md) |
| Gọi API nào | [docs/api/README.md](docs/api/README.md) |
| Bảng, migration | [docs/DATABASE.md](docs/DATABASE.md) |
| Quy tắc tính tiền, thuế, khóa sổ… | [docs/requirements/quy-tac-nghiep-vu.md](docs/requirements/quy-tac-nghiep-vu.md) |
| Thêm chức năng mới | [docs/huong-dan/them-chuc-nang-moi.md](docs/huong-dan/them-chuc-nang-moi.md) |
