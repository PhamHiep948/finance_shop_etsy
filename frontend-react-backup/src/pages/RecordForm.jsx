import { useState } from "react";
import { ChevronDown, ChevronUp } from "lucide-react";
import { INCOME_CATEGORIES, EXPENSE_CATEGORIES } from "../lib/data";
import { I } from "../lib/icons";
import { afterTax, toDisplay, valNum } from "../lib/format";
import { expensePayload, incomePayload, useFinance } from "../lib/store";

function sectionPrefs() {
  try {
    return JSON.parse(localStorage.getItem("fm_form_sections") || "{}") || {};
  } catch {
    return {};
  }
}

function FormSection({ id, title, sub, defaultOpen, children }) {
  const [open, setOpen] = useState(() => {
    const p = sectionPrefs();
    return p[id] == null ? defaultOpen : Boolean(p[id]);
  });
  function toggle() {
    const next = !open;
    setOpen(next);
    const p = sectionPrefs();
    p[id] = next;
    localStorage.setItem("fm_form_sections", JSON.stringify(p));
  }
  return (
    <div className={`form-section${open ? "" : " is-collapsed"}`}>
      <button
        type="button"
        className={`form-section-head${open ? "" : " is-closed"}`}
        onClick={toggle}
        aria-expanded={open}
        aria-controls={`${id}-body`}
      >
        <div className="form-section-copy">
          <h3 className="form-section-title">{title}</h3>
          <p className="muted form-section-sub">{sub}</p>
        </div>
        <span className="section-toggle-btn" aria-hidden="true">
          <span className="section-toggle-label">{open ? "Thu gọn" : "Mở rộng"}</span>
          {open ? <ChevronUp aria-hidden="true" /> : <ChevronDown aria-hidden="true" />}
        </span>
      </button>
      <div className="form-section-body" id={`${id}-body`}>{children}</div>
    </div>
  );
}

export default function RecordForm({ kind, rec, onClose }) {
  const { ccy, saveIncome, saveExpense } = useFinance();
  const isIncome = kind === "income";
  const today = new Date().toLocaleDateString("en-CA");
  const r = rec || (isIncome
    ? { incomeDate: today, description: "", categoryId: 1, amount: "", taxPercent: "", orderCode: "", saleRegion: "", productQty: "", unitPrice: "", itemTotal: "", discountAmount: "", discountCode: "", subtotal: "", shippingAmount: "", taxAmount: "", referenceCode: "", note: "" }
    : { expenseDate: today, description: "", categoryId: 3, amount: "", recipient: "", originScope: "DOMESTIC", taxPercent: "", note: "" });

  const [amount, setAmount] = useState(r.amount === "" ? "" : toDisplay(r.amount, ccy));
  const [taxPercent, setTaxPercent] = useState(valNum(r.taxPercent));
  const [itemTotal, setItemTotal] = useState(r.itemTotal === 0 || r.itemTotal ? toDisplay(r.itemTotal, ccy) : "");
  const [discount, setDiscount] = useState(r.discountAmount === 0 || r.discountAmount ? toDisplay(r.discountAmount, ccy) : "");
  const [subtotal, setSubtotal] = useState(r.subtotal === 0 || r.subtotal ? toDisplay(r.subtotal, ccy) : "");
  const [qty, setQty] = useState(r.productQty || "");
  const [unit, setUnit] = useState(r.unitPrice === 0 || r.unitPrice ? toDisplay(r.unitPrice, ccy) : "");
  const [ship, setShip] = useState(r.shippingAmount === 0 || r.shippingAmount ? toDisplay(r.shippingAmount, ccy) : "");
  const [taxAmt, setTaxAmt] = useState(r.taxAmount === 0 || r.taxAmount ? toDisplay(r.taxAmount, ccy) : "");
  const [manual, setManual] = useState({});

  const after = amount === "" ? "" : afterTax(amount, taxPercent);

  async function onSubmit(e) {
    e.preventDefault();
    const fd = new FormData(e.target);
    const ok = isIncome
      ? await saveIncome({ ...incomePayload(fd, ccy), source: rec?.source || "MANUAL" }, rec?.id)
      : await saveExpense({ ...expensePayload(fd, ccy), source: rec?.source || "MANUAL" }, rec?.id);
    if (ok) onClose();
  }

  const typeLabel = isIncome ? "Khoản thu" : "Khoản chi";
  const cats = isIncome ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;

  return (
    <div className="form-window record-form-window">
      <div className="form-window-head">
        <div>
          <div className="exp-kicker">{rec ? <><I name="pencil" /> Cập nhật</> : <><I name="plus-circle" /> Giao dịch mới</>} <span className="muted">/</span> <span className="muted">{typeLabel}</span></div>
          <h2>{rec ? (isIncome ? "Sửa khoản thu" : "Sửa khoản chi") : (isIncome ? "Thêm khoản thu" : "Thêm khoản chi")}</h2>
          <p className="muted">{rec ? "Chỉnh sửa thông tin, phân loại và chứng từ rồi lưu thay đổi." : "Nhập các trường bắt buộc. Chi tiết bổ sung có thể thêm sau."}</p>
        </div>
        <button className="icon-ghost" type="button" aria-label="Đóng" onClick={onClose}><I name="x" /></button>
      </div>
      <div className="form-window-body">
        <form className="card record-form" onSubmit={onSubmit} style={{ boxShadow: "none", border: 0, padding: 0 }}>
          <div className="form-primary-card">
            <div className="form-grid">
              {isIncome ? (
                <>
                  <label className="field"><span>Ngày thu <span className="req">*</span></span><input name="incomeDate" type="date" required defaultValue={r.incomeDate} /></label>
                  <label className="field"><span>Loại thu <span className="req">*</span></span>
                    <select name="categoryId" defaultValue={r.categoryId}>{cats.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select>
                  </label>
                  <label className="field span-2"><span>Tên sản phẩm <span className="req">*</span></span><input name="description" required defaultValue={r.description || ""} placeholder="Lily Flower" /></label>
                </>
              ) : (
                <>
                  <label className="field"><span>Ngày chi <span className="req">*</span></span><input name="expenseDate" type="date" required defaultValue={r.expenseDate} /></label>
                  <label className="field"><span>Loại chi <span className="req">*</span></span>
                    <select name="categoryId" defaultValue={r.categoryId}>{cats.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select>
                  </label>
                  <label className="field span-2"><span>Nội dung <span className="req">*</span></span><input name="description" required defaultValue={r.description || ""} /></label>
                </>
              )}
              <label className="field"><span>Số tiền trước thuế ({ccy}) <span className="req">*</span></span>
                <input name="amount" type="number" step="0.01" required value={amount} onChange={(e) => { setManual((m) => ({ ...m, amount: 1 })); setAmount(e.target.value); }} />
              </label>
              {!isIncome ? <label className="field"><span>Người nhận</span><input name="recipient" defaultValue={r.recipient || ""} /></label> : null}
              <label className="field"><span>% thuế</span><input name="taxPercent" type="number" step="0.01" min="0" max="100" value={taxPercent} onChange={(e) => setTaxPercent(e.target.value)} placeholder="0" /></label>
              <label className="field"><span>Tiền sau thuế ({ccy})</span>
                <input name="amountAfterTax" type="number" step="0.01" readOnly tabIndex={-1} className="is-computed" value={after} />
                <small className="muted">Tự tính từ số tiền trước thuế và % thuế, không nhập tay.</small>
              </label>
            </div>
          </div>
          {isIncome ? (
            <>
              <FormSection id="income-source" title="Chi tiết nguồn thu" sub="Mã đơn, khu vực bán, số lượng, đơn giá" defaultOpen={!!(r.orderCode || r.saleRegion || r.productQty || r.unitPrice)}>
                <div className="form-grid">
                  <label className="field"><span>Mã đơn hàng</span><input name="orderCode" defaultValue={r.orderCode || ""} placeholder="VD: 4154185113" /></label>
                  <label className="field"><span>Bán đi đâu</span>
                    <select name="saleRegion" defaultValue={r.saleRegion || ""}>
                      <option value="">—</option>
                      <option value="IN_EU">Trong EU</option>
                      <option value="OUTSIDE_EU">Ngoài EU</option>
                    </select>
                  </label>
                  <label className="field"><span>Số lượng sản phẩm</span><input name="productQty" type="number" min="1" step="1" value={qty} onChange={(e) => setQty(e.target.value)} /></label>
                  <label className="field"><span>Đơn giá ({ccy})</span><input name="unitPrice" type="number" step="0.01" min="0" value={unit} onChange={(e) => setUnit(e.target.value)} placeholder="5.49" /></label>
                </div>
              </FormSection>
              <FormSection id="income-fees" title="Chi tiết phí — Tổng đơn hàng" sub="Tiền hàng − Giảm giá = Tạm tính; Tạm tính + Vận chuyển + Thuế = Số tiền" defaultOpen={!!(r.itemTotal || r.discountAmount || r.discountCode || r.subtotal || r.shippingAmount || r.taxAmount)}>
                <div className="form-grid">
                  <label className="field"><span>Tiền hàng ({ccy})</span><input name="itemTotal" type="number" step="0.01" min="0" value={itemTotal} onChange={(e) => { setManual((m) => ({ ...m, itemTotal: 1 })); setItemTotal(e.target.value); }} /></label>
                  <label className="field"><span>Giảm giá ({ccy})</span><input name="discountAmount" type="number" step="0.01" min="0" value={discount} onChange={(e) => setDiscount(e.target.value)} /></label>
                  <label className="field"><span>Mã giảm giá</span><input name="discountCode" defaultValue={r.discountCode || ""} placeholder="AGSALE43" /></label>
                  <label className="field"><span>Tạm tính ({ccy})</span><input name="subtotal" type="number" step="0.01" min="0" value={subtotal} onChange={(e) => { setManual((m) => ({ ...m, subtotal: 1 })); setSubtotal(e.target.value); }} /></label>
                  <label className="field"><span>Vận chuyển ({ccy})</span><input name="shippingAmount" type="number" step="0.01" min="0" value={ship} onChange={(e) => setShip(e.target.value)} /></label>
                  <label className="field"><span>Thuế ({ccy})</span><input name="taxAmount" type="number" step="0.01" min="0" value={taxAmt} onChange={(e) => setTaxAmt(e.target.value)} /></label>
                </div>
              </FormSection>
              <FormSection id="income-extra" title="Ghi chú" sub="Mã tham chiếu và ghi chú" defaultOpen={!!(r.referenceCode || r.note)}>
                <div className="form-grid">
                  <label className="field span-2"><span>Mã tham chiếu</span><input name="referenceCode" defaultValue={r.referenceCode || ""} /></label>
                  <label className="field span-2"><span>Ghi chú</span><textarea name="note" defaultValue={r.note || ""} /></label>
                </div>
              </FormSection>
            </>
          ) : (
            <>
              <FormSection id="expense-detail" title="Phạm vi nguồn" sub="Nội địa hoặc quốc tế" defaultOpen>
                <div className="form-grid">
                  <label className="field"><span>Phạm vi nguồn <span className="req">*</span></span>
                    <select name="originScope" defaultValue={r.originScope || "DOMESTIC"}>
                      <option value="DOMESTIC">Nội địa</option>
                      <option value="INTERNATIONAL">Quốc tế</option>
                    </select>
                  </label>
                </div>
              </FormSection>
              <FormSection id="expense-extra" title="Ghi chú" sub="Ghi chú khoản chi" defaultOpen={!!(r.note)}>
                <div className="form-grid">
                  <label className="field span-2"><span>Ghi chú</span><textarea name="note" defaultValue={r.note || ""} /></label>
                </div>
              </FormSection>
            </>
          )}
          <div className="form-actions">
            <button type="button" className="btn secondary" onClick={onClose}>Hủy</button>
            <button className="btn primary" type="submit">{rec ? "Cập nhật" : isIncome ? "Lưu khoản thu" : "Lưu khoản chi"}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
