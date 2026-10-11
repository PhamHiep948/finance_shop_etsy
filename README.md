# Finance Shop Etsy

Ứng dụng quản lý thu chi cho shop handmade bán trên Etsy: ghi khoản thu / chi, tự đồng bộ đơn Etsy, tách VAT cho đơn trong EU, gắn tiền xưởng / tiền ship vào từng đơn, quy đổi USD ↔ EUR theo tỷ giá tháng, báo cáo phục vụ khai thuế, lịch sử chỉnh sửa, thùng rác và khóa sổ theo quý.

## Kiến trúc

- **Backend** `backend/` — ASP.NET Core 9, Clean Architecture theo module (dựa trên khung CMS_VSF): `Domain → Infrastructure → Service (CQRS: MediatR + FluentValidation) → Presentation → Host`; EF Core 9 + Pomelo trên MySQL / MariaDB; chương trình `Fin.Migration` quản lý database.
- **Frontend** `frontend/` — HTML / CSS / JavaScript thuần, được API phục vụ tại cùng địa chỉ.
- **Tài liệu** `docs/` — phạm vi, chức năng, use case, dữ liệu, API, quy tắc nghiệp vụ, hướng dẫn.

## Chạy nhanh

```bash
# 0. XAMPP: Start MySQL; tạo database "finance" (utf8mb4) nếu chưa có
cd backend
dotnet tool restore
dotnet run --project Migrations/Fin.Migration     # tạo / nâng cấp bảng + dữ liệu mặc định (chạy lại an toàn)
dotnet run --project Host/Fin.Host.Api            # http://localhost:5000
```

Chi tiết cấu hình, Etsy và lỗi thường gặp: [docs/huong-dan/chay-du-an.md](docs/huong-dan/chay-du-an.md).

## Kiểm thử

```bash
cd backend
dotnet build Fin.Finance.sln
dotnet test Tests/Fin.Tests.Unit
```

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

`backend-legacy/` là backend cũ (Minimal API + SQL viết tay), giữ lại để đối chiếu; có thể xóa khi đã yên tâm với backend mới.
