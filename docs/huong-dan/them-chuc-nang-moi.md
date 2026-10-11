# Thêm một chức năng mới

Làm đủ các bước theo đúng thứ tự, không bỏ tầng. Ví dụ xuyên suốt: thêm **"Ngày hóa đơn"** cho khoản chi.

## 1. Domain — `Fin.Domain.Finance`

Thêm trường vào entity (hoặc entity mới trong `Entities/`). Quy tắc thuần (tính toán, chuyển trạng thái) đặt thành method của entity.

```csharp
public DateOnly? InvoiceDate { get; set; }
```

## 2. Ánh xạ bảng + migration — `Fin.Infrastructure.Finance`

- Khai báo cột trong `Contexts/Configurations/{Entity}Configuration.cs` (tên cột `snake_case`).
- Sinh migration và chạy:

```bash
cd backend
dotnet ef migrations add AddInvoiceDate --project Modules/Finance/Fin.Infrastructure.Finance --startup-project Migrations/Fin.Migration --context FinanceContext --output-dir Migrations
dotnet run --project Migrations/Fin.Migration
```

Đọc lại file migration sinh ra; dài bất thường nghĩa là snapshot lệch — dừng lại kiểm tra.

## 3. DTO — `Fin.Model.Dto.Finance`

Thêm trường vào `Requests/LedgerEntryRequest.cs` và `Responses/LedgerEntryResponse.cs`; cập nhật `Mappers/LedgerEntryMapper.cs` và `Services/LedgerEntryAssembler.cs`.
Nếu cần lưu lịch sử thay đổi trường này: thêm vào danh sách `TrackedFields` của `AuditTrail`.

## 4. Use case — `Fin.Service.Finance/UseCases/{TínhNăng}/`

Với chức năng mới hoàn toàn:

```text
UseCases/Invoices/
  Commands/SetInvoiceDateCommand.cs            : IRequest<Response<…>>
  Commands/Handlers/SetInvoiceDateHandler.cs   : IRequestHandler<…>
  Validators/SetInvoiceDateValidator.cs        : AbstractValidator<…>
```

- Handler dùng `IFinanceUnitOfWork` (không dùng `FinanceContext` trực tiếp), ghi lịch sử bằng `AuditTrail`, kiểm tra khóa sổ bằng `PeriodLockPolicy` nếu đổi dữ liệu theo ngày.
- Lỗi nghiệp vụ trả `Response<T>.Fail(ErrorCodeEnum.X, "câu báo tiếng Việt")`; thêm mã mới vào `ErrorCodeEnum` (Fin.Infrastructure.Shared) với `[Description]` tiếng Việt.
- Truy vấn mới vào database → thêm method vào `I{Entity}Repository` + `{Entity}Repository`.
- MediatR và FluentValidation tự tìm handler / validator trong assembly, **không** cần đăng ký thêm. Dịch vụ dùng chung mới → đăng ký trong `Extensions/FinanceServiceExtensions.cs`.

## 5. Controller — `Fin.Presentation.Finance/Controllers/v1`

Controller chỉ nhận HTTP và gửi command:

```csharp
[HttpPut("{id:long}/invoice-date")]
public async Task<IActionResult> SetInvoiceDateAsync(long id, InvoiceDateRequest request) =>
    Ok(await Mediator.Send(new SetInvoiceDateCommand { Id = id, Date = request.Date }));
```

## 6. Frontend — `frontend/js`

- Hàm gọi API trong `store.js` (dùng `api(...)` — đã tự lấy `data` và hiện lỗi).
- Giao diện trong `pages/*.js`; ô ngày dùng `dateFieldHtml(...)`, số tiền dùng `money(...)` để theo USD / EUR.

## 7. Kiểm chứng (bắt buộc)

1. `dotnet build Fin.Finance.sln` → 0 Warning, 0 Error.
2. Unit test cho quy tắc mới trong `Tests/Fin.Tests.Unit`, chạy `dotnet test Tests/Fin.Tests.Unit`.
3. Gọi API bằng `curl` / Swagger, ghi lại mã trạng thái và `succeeded`.
4. Mở màn hình, thử đủ 3 đường **Làm → Hoàn tác → Làm lại** (ví dụ: thêm → xóa → khôi phục).
5. Cập nhật tài liệu liên quan trong `docs/` (chức năng, API, quy tắc nghiệp vụ, database).

## Quy ước bắt buộc

- Mỗi class một file; namespace dạng khối.
- Không `/// <summary>`; comment tiếng Anh, chỉ giải thích *vì sao*, đặt trong thân hàm.
- Chuỗi người dùng nhìn thấy: tiếng Việt.
- Không `DateTime.Now` / `Today` (dùng `TimeProvider`), không `.Result` / `.Wait()`, không `new HttpClient()`.
- Version gói NuGet chỉ khai ở `backend/Directory.Packages.props`.
