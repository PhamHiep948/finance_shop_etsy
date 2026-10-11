# Phạm vi

## Bài toán

Một shop handmade bán chủ yếu trên Etsy, giao hàng cho khách ở Mỹ (ngoài EU) và ở Đức / châu Âu (trong EU). Chủ shop đang ghi thu chi bằng Google Sheets theo quý:

- sheet đơn Mỹ: giá bán, phí ship khách trả, **tiền xưởng** (trả cho xưởng in / làm hàng), lợi nhuận;
- sheet đơn Đức: tách **thuế 19%** ra khỏi tổng thu, **phí ship** shop tự trả (đã gồm 19% thuế đầu vào);
- sheet phí Etsy theo tháng (quảng cáo, giao dịch, listing, hoàn tiền) để khai thuế Đức (Anlage EÜR);
- mỗi tháng quy đổi USD → EUR theo **tỷ giá tháng**.

Làm tay dễ sai (ví dụ dùng nhầm tỷ giá của tháng trước) và mất thời gian chép đơn từ Etsy.

## Mục tiêu

| Mục tiêu | Cách ứng dụng đáp ứng |
|---|---|
| Ghi thu chi ở một nơi | Khoản thu / khoản chi có loại, ngày, số tiền trước / sau thuế, chứng từ đính kèm |
| Không chép tay đơn Etsy | Đồng bộ đơn Etsy (bấm tay hoặc tự động theo chu kỳ), không nhập trùng |
| Đúng thuế EU | Đơn giao trong EU: tiền nhận về là tiền **sau thuế**, app tự tách phần VAT |
| Đúng tỷ giá | Tỷ giá 1 EUR = ? USD nhập theo **tháng**; mọi phép quy đổi của tháng dùng chung một số |
| Biết lãi từng đơn | Tiền xưởng / tiền ship gắn vào khoản thu, tự sinh khoản chi tương ứng |
| Báo cáo khai thuế | Báo cáo theo kỳ, lọc nhiều loại, nhóm loại đặt tên tự do, xuất Excel / PDF |
| Không sửa nhầm sổ cũ | Khóa sổ theo quý (hoặc tháng), lịch sử chỉnh sửa, thùng rác 30 ngày |

## Ngoài phạm vi

- Đăng nhập, phân quyền nhiều người dùng (ứng dụng chạy trên máy cá nhân, một người dùng).
- Kế toán kép, hóa đơn điện tử, cổng thanh toán, quản lý kho / SKU.
- Tự lấy phí Etsy (Ads, transaction, listing) qua API — đã khảo sát (`payment-account/ledger-entries`) nhưng chưa làm.
- Trạng thái đơn từ Etsy (chưa gửi / đã gửi hàng) — hiện mọi khoản đã ghi đều là "Hoàn thành".
- Triển khai lên server / Docker — ứng dụng chạy local tại `http://localhost:5000`.
