import { Fragment, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { EXPENSE_CATEGORIES, FX_USD_TO_EUR, INCOME_CATEGORIES } from "../lib/data";
import { I } from "../lib/icons";
import {
  afterTaxOf, catName, catTone, colDefs, defaultColMap, dmy, loadColOrder, loadCols, money, originScopeLabel,
  paymentMethodLabel, pctLabel, recStatus, saleRegionLabel, salesChannelLabel, saveCols, sum, usd,
} from "../lib/format";

import { useFinance } from "../lib/store";
import RecordForm from "./RecordForm";
import { Kpi } from "../features/dashboard/pages/Dashboard";

const PAGE_SIZE = 10;

function RecordDetail({ kind, rec, onClose, onEdit, onDelete, toast }) {
  const { canEditOwn, canDeleteOwn } = useFinance();
  const isIncome = kind === "income";
  if (!rec) {
    return (
      <div className="exp-modal">
        <div className="exp-head"><h2>Không tìm thấy bản ghi</h2><button className="icon-ghost" type="button" aria-label="Đóng" onClick={onClose}><I name="x" /></button></div>
        <p className="muted">Khoản này có thể đã bị xóa.</p>
      </div>
    );
  }
  const st = recStatus(rec.id, kind, rec);
  const code = isIncome ? rec.orderCode || rec.referenceCode || `IN-${String(rec.id).padStart(3, "0")}` : `EXP-${String(rec.id).padStart(3, "0")}`;
  const canChange = isIncome ? canEditOwn(rec, "incomeUpdate") : canEditOwn(rec, "expenseUpdate");
  const canRemove = isIncome ? canDeleteOwn(rec, "incomeDelete") : canDeleteOwn(rec, "expenseDelete");
  const item = Number(rec.itemTotal) || Number(rec.amount) || 0;
  const disc = Number(rec.discountAmount) || 0;
  const ship = Number(rec.shippingAmount) || 0;
  const taxAmt = isIncome ? Number(rec.taxAmount) || 0 : Math.round((afterTaxOf(rec) - (Number(rec.amount) || 0)) * 100) / 100;
  const tot = isIncome ? Number(rec.amount) || 0 : afterTaxOf(rec);
  const fxRate = Number(FX_USD_TO_EUR).toLocaleString("vi-VN");
  return (
    <div className="exp-modal">
      <div className="exp-head">
        <div>
          <div className="exp-kicker"><I name="file-text" /> Hồ sơ chi tiết <span className="muted">/</span> <span className="muted">{code}</span></div>
          <h2>{rec.description}</h2>
          <p className="exp-subtitle">
            <strong>Thông tin chi tiết</strong>
            <span>{isIncome ? "Khoản thu, nguồn đơn hàng và chứng từ đi kèm." : "Khoản chi, phân loại thuế và các chứng từ đi kèm."}</span>
          </p>
        </div>
        <button className="icon-ghost" type="button" aria-label="Đóng" onClick={onClose}><I name="x" /></button>
      </div>
      <div className="exp-grid">
        <div>
          <p className="muted" style={{ fontSize: 11, fontWeight: 700, letterSpacing: ".04em", marginBottom: 8 }}><I name="clipboard-list" /> THÔNG TIN CƠ BẢN</p>
          <div className="kv">
            {isIncome ? (
              <>
                <span>Ngày thu</span><b>{rec.incomeDate}</b>
                <span>Danh mục</span><b>{catName(INCOME_CATEGORIES, rec.categoryId)}</b>
                <span>Nguồn</span><b>{salesChannelLabel(rec.salesChannel)}</b>
                <span>Khu vực</span><b>{saleRegionLabel(rec.saleRegion)}</b>
                <span>Mã đơn</span><b>{code}</b>
                <span>Số lượng</span><b>{rec.productQty || "—"}</b>
                <span>Trạng thái</span><b>{st.t}</b>
              </>
            ) : (
              <>
                <span>Ngày thực hiện</span><b>{rec.expenseDate}</b>
                <span>Danh mục</span><b>{catName(EXPENSE_CATEGORIES, rec.categoryId)}</b>
                <span>Người nhận</span><b>{rec.recipient || "—"}</b>
                <span>Phạm vi</span><b>{originScopeLabel(rec.originScope)}</b>
                <span>Phương thức</span><b>{paymentMethodLabel(rec.paymentMethod)}</b>
                <span>Trạng thái</span><b>{st.t}</b>
              </>
            )}
          </div>

        </div>
        <div>
          <div className="fin-box">
            <p className="muted" style={{ fontSize: 11, fontWeight: 700, marginBottom: 8 }}>CHI TIẾT TÀI CHÍNH</p>
            {isIncome ? (
              <>
                <div className="line"><span>Tạm tính (Subtotal)</span><b>{usd(item)}</b></div>
                <div className="line"><span>Giảm giá (Discount)</span><b>{disc ? `− ${usd(disc)}` : "—"}</b></div>
                <div className="line"><span>Vận chuyển (Shipping)</span><b>{ship ? usd(ship) : "—"}</b></div>
                <div className="line"><span>Thuế (Tax/VAT)</span><b>{usd(taxAmt)}</b></div>
                <div className="total income-total"><span>Tổng thu<br /><small style={{ fontWeight: 500, color: "var(--text-secondary)", fontSize: 11 }}>Tỷ giá quy đổi: 1 USD = {fxRate} EUR</small></span><span>{usd(tot)}</span></div>
              </>
            ) : (
              <>
                <div className="line"><span>Số tiền</span><b>{usd(rec.amount)}</b></div>
                <div className="line"><span>Thuế ({pctLabel(rec.taxPercent)})</span><b>{usd(taxAmt)}</b></div>
                <div className="line"><span>Người nhận</span><b>{rec.recipient || "—"}</b></div>
                <div className="total expense-total"><span>Tổng thanh toán<br /><small style={{ fontWeight: 500, color: "var(--text-secondary)", fontSize: 11 }}>Tỷ giá quy đổi: 1 USD = {fxRate} EUR</small></span><span>{usd(tot)}</span></div>
              </>
            )}
          </div>
        </div>
      </div>
      <div className="exp-foot">
        <div className="head-actions">
          <button className="btn secondary" type="button" onClick={onClose}>Đóng</button>
          {canChange ? <button className="btn secondary" type="button" onClick={onEdit}>Sửa</button> : null}
          {canRemove ? <button className="btn danger" type="button" onClick={onDelete}>Xóa</button> : null}
          <button className="btn primary" type="button" onClick={() => { toast(isIncome ? "Đã xác nhận đối soát (mock)" : "Đã xác nhận thanh toán (mock)"); onClose(); }}>{isIncome ? "Xác nhận đối soát" : "Xác nhận thanh toán"}</button>
        </div>
      </div>
    </div>
  );
}

export default function RecordList({ kind }) {
  const isIncome = kind === "income";
  const { incomes, expenses, ccy, setCcy, can, setConfirm, softDelete, toast } = useFinance();
  const [params, setParams] = useSearchParams();
  const [q, setQ] = useState("");
  const [cat, setCat] = useState("");
  const [src, setSrc] = useState("");
  const [region, setRegion] = useState("");
  const [origin, setOrigin] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [colsOpen, setColsOpen] = useState(false);
  const [cols, setCols] = useState(() => loadCols(kind));
  const [colOrder, setColOrder] = useState(() => loadColOrder(kind));
  const [draftCols, setDraftCols] = useState(cols);
  const [draftOrder, setDraftOrder] = useState(colOrder);
  const [viewId, setViewId] = useState(null);
  const [showFilters, setShowFilters] = useState(false);
  const hasFilters = Boolean(cat || src || region || origin || from || to);

  const list = isIncome ? incomes : expenses;
  const filtered = useMemo(() => {
    const qq = q.toLowerCase();
    return list.filter((r) => {
      const text = isIncome ? `${r.description} ${r.referenceCode || ""} ${r.orderCode || ""}` : `${r.description} ${r.recipient || ""}`;
      const date = isIncome ? r.incomeDate : r.expenseDate;
      return (
        (!qq || text.toLowerCase().includes(qq)) &&
        (!cat || String(r.categoryId) === cat) &&
        (!src || r.source === src) &&
        (!region || r.saleRegion === region) &&
        (!origin || r.originScope === origin) &&
        (!from || date >= from) &&
        (!to || date <= to)
      );
    }).sort((a, b) => (isIncome ? b.incomeDate.localeCompare(a.incomeDate) : b.expenseDate.localeCompare(a.expenseDate)) || b.id - a.id);
  }, [list, q, cat, src, region, origin, from, to, isIncome]);

  const pages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const p = Math.min(page, pages);
  const rows = filtered.slice((p - 1) * PAGE_SIZE, p * PAGE_SIZE);
  const cats = isIncome ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;
  const formNew = params.get("new") === "1";
  const formEdit = params.get("edit");
  const formRec = formEdit ? list.find((x) => x.id === Number(formEdit)) : null;
  const viewRec = viewId ? list.find((x) => x.id === viewId) : null;

  function closeForm() {
    const next = new URLSearchParams(params);
    next.delete("new");
    next.delete("edit");
    setParams(next, { replace: true });
  }

  function openCols() {
    setDraftCols(loadCols(kind));
    setDraftOrder(loadColOrder(kind));
    setColsOpen(true);
  }

  function moveDraftCol(id, step) {
    setDraftOrder((order) => {
      const fromIndex = order.indexOf(id);
      const toIndex = fromIndex + step;
      if (fromIndex < 0 || toIndex < 0 || toIndex >= order.length) return order;
      const next = [...order];
      [next[fromIndex], next[toIndex]] = [next[toIndex], next[fromIndex]];
      return next;
    });
  }

  function colOn(id) {
    return Boolean(cols[id]);
  }

  const kpis = useMemo(() => {
    const dateKey = isIncome ? "incomeDate" : "expenseDate";
    const now = new Date();
    const ym = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
    const thisKey = ym(now);
    const prevKey = ym(new Date(now.getFullYear(), now.getMonth() - 1, 1));
    const inMonth = (k) => list.filter((x) => String(x[dateKey]).startsWith(k));
    const cur = inMonth(thisKey);
    const prev = inMonth(prevKey);
    const total = sum(cur);
    const prevTotal = sum(prev);
    const pct = (a, b) => (b ? `${a >= b ? "+" : ""}${(((a - b) / b) * 100).toFixed(1)}% so với tháng trước` : "Chưa có dữ liệu tháng trước");
    const diff = (a, b) => `${a >= b ? "+" : "-"}${Math.abs(a - b)} so với tháng trước`;
    if (isIncome) {
      return [
        { label: "Tổng doanh thu (tháng)", value: money(total, ccy), delta: pct(total, prevTotal) },
        { label: "Đơn hàng mới", value: String(cur.length), delta: diff(cur.length, prev.length) },
        { label: "Doanh thu trung bình", value: money(cur.length ? total / cur.length : 0, ccy), delta: `${cur.length} giao dịch trong tháng` },
        { label: "Tổng sau thuế", value: money(cur.reduce((s, x) => s + x.amountAfterTax, 0), ccy), delta: "Đã gồm thuế trong tháng" },
      ];
    }
    const intl = sum(cur.filter((x) => x.originScope === "INTERNATIONAL"));
    const share = total ? ((intl / total) * 100).toFixed(1) : "0.0";
    return [
      { label: "Tổng chi phí (tháng)", value: money(total, ccy), delta: pct(total, prevTotal) },
      { label: "Giao dịch", value: String(cur.length), delta: diff(cur.length, prev.length) },
      { label: "Chi phí quốc tế", value: money(intl, ccy), delta: `${share}% tổng chi phí tháng` },
      { label: "Thuế & phí ước tính", value: money(cur.reduce((s, x) => s + (x.amountAfterTax - x.amount), 0), ccy), delta: "Phần thuế của chi phí trong tháng" },
    ];
  }, [isIncome, list, ccy]);
  const defsById = Object.fromEntries(colDefs(kind).map((c) => [c.id, c]));
  const visibleOrder = colOrder.filter(colOn);

  function headerCell(id) {
    const numeric = ["qty", "amount", "taxPercent", "afterTax", "item", "discount", "ship", "tax"].includes(id);
    return <th key={id} className={numeric ? "amount" : undefined}>{defsById[id]?.label}</th>;
  }

  function bodyCell(id, r) {
    const st = recStatus(r.id, kind, r);
    const statusCls = st.k === "done" ? "ok" : st.k === "pending" ? "warn" : "neutral";
    const regionCls = r.saleRegion === "IN_EU" ? "region-eu" : r.saleRegion === "OUTSIDE_EU" ? "region-out" : "";
    const originCls = r.originScope === "INTERNATIONAL" ? "scope-int" : r.originScope === "DOMESTIC" ? "scope-dom" : "";
    const srcCls = r.source === "EXCEL_IMPORT" ? "src-excel" : "src-manual";
    const cells = {
      date: (
        <td>
          <span className="cell-date">{dmy(isIncome ? r.incomeDate : r.expenseDate)}</span>
        </td>
      ),
      product: (
        <td>
          <div className="cell-product-wrap">
            <button type="button" className="cell-link cell-product-name">{r.description}</button>
            <span className={`badge cell-status-badge ${statusCls}`}>{st.t}</span>
          </div>
        </td>
      ),
      category: (
        <td>
          <span className={`badge record-list-label ${catTone(r.categoryId)}`}>{catName(cats, r.categoryId)}</span>
        </td>
      ),
      order: <td><span className="cell-code">{r.orderCode || "—"}</span></td>,
      payee: <td><span className="cell-payee">{r.recipient || "—"}</span></td>,
      qty: <td className="amount"><span className="cell-qty">{r.productQty || "—"}</span></td>,
      amount: (
        <td className="amount">
          <span className={isIncome ? "plus" : "minus"}>
            {isIncome ? "+" : "−"}{money(r.amount, ccy)}
          </span>
        </td>
      ),
      taxPercent: (
        <td className="amount">
          <span className="cell-tax-pct">{pctLabel(r.taxPercent)}</span>
        </td>
      ),
      afterTax: (
        <td className="amount">
          <strong className={isIncome ? "plus" : "minus"}>
            {money(afterTaxOf(r), ccy)}
          </strong>
        </td>
      ),
      region: (
        <td>
          {r.saleRegion
            ? <span className={`badge record-list-label ${regionCls}`}>{saleRegionLabel(r.saleRegion)}</span>
            : <span className="muted">—</span>}
        </td>
      ),
      origin: (
        <td>
          {r.originScope
            ? <span className={`badge record-list-label ${originCls}`}>{originScopeLabel(r.originScope)}</span>
            : <span className="muted">—</span>}
        </td>
      ),
      source: (
        <td>
          <span className={`badge ${srcCls}`}>
            {r.source === "EXCEL_IMPORT" ? "Excel" : "Nhập tay"}
          </span>
        </td>
      ),
      item: <td className="amount">{r.itemTotal ? money(r.itemTotal, ccy) : <span className="muted">—</span>}</td>,
      discount: <td className="amount">{Number(r.discountAmount) ? <span className="minus">−{money(r.discountAmount, ccy)}</span> : <span className="muted">—</span>}</td>,
      ship: <td className="amount">{Number(r.shippingAmount) ? money(r.shippingAmount, ccy) : <span className="muted">—</span>}</td>,
      tax: <td className="amount">{money(Number(r.taxAmount) || 0, ccy)}</td>,
    };
    return <Fragment key={id}>{cells[id]}</Fragment>;
  }

  return (
    <>
      <div className="kpis">{kpis.map((k) => <Kpi key={k.label} {...k} />)}</div>
      <article className="card" style={{ padding: 0 }}>
        <div className="card-head list-card-head">
          <div className="list-card-left">
            <label className="search-box list-search">
              <I name="search" />
              <input
                type="search"
                placeholder={isIncome ? "Tìm sản phẩm, mã đơn..." : "Tìm nội dung, người nhận..."}
                value={q}
                onChange={(e) => { setQ(e.target.value); setPage(1); }}
              />
            </label>
            <button
              className={`btn ghost sm list-filter-btn${showFilters ? " active" : ""}${hasFilters ? " has-filter" : ""}`}
              type="button"
              onClick={() => setShowFilters((v) => !v)}
            >
              <I name="sliders-horizontal" size={14} />
              <span>Bộ lọc</span>
              {hasFilters && <span className="filter-active-dot" />}
              <I name={showFilters ? "chevron-up" : "chevron-down"} size={12} />
            </button>
          </div>
          <div className="list-card-actions">
            <button className="btn ghost sm" type="button" onClick={openCols}><I name="columns-3" /> Cột hiển thị</button>
            <button className="btn ghost sm" type="button" onClick={() => { setQ(""); setCat(""); setSrc(""); setRegion(""); setOrigin(""); setFrom(""); setTo(""); setPage(1); }}><I name="rotate-ccw" /> Xóa bộ lọc</button>
            {can(isIncome ? "incomeCreate" : "expenseCreate") ? (
              <button className="btn primary sm" type="button" onClick={() => setParams({ new: "1" })}>
                <I name="plus" size={14} /> {isIncome ? "Thêm khoản thu" : "Thêm khoản chi"}
              </button>
            ) : null}
          </div>
        </div>
        {showFilters && (
          <div className="record-toolbar in-card-toolbar">
            <div className="toolbar-filters">
              <select className={`toolbar-ctrl${cat ? " filter-active" : ""}`} value={cat} onChange={(e) => { setCat(e.target.value); setPage(1); }}>
                <option value="">{isIncome ? "Loại thu" : "Loại chi"}: Tất cả</option>
                {cats.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              <select className={`toolbar-ctrl${src ? " filter-active" : ""}`} value={src} onChange={(e) => { setSrc(e.target.value); setPage(1); }}>
                <option value="">Nguồn: Tất cả</option>
                <option value="MANUAL">Nhập tay</option>
                <option value="EXCEL_IMPORT">Excel</option>
              </select>
              {isIncome ? (
                <select className={`toolbar-ctrl${region ? " filter-active" : ""}`} value={region} onChange={(e) => { setRegion(e.target.value); setPage(1); }}>
                  <option value="">Khu vực: Tất cả</option>
                  <option value="IN_EU">Trong EU</option>
                  <option value="OUTSIDE_EU">Ngoài EU</option>
                </select>
              ) : (
                <select className={`toolbar-ctrl${origin ? " filter-active" : ""}`} value={origin} onChange={(e) => { setOrigin(e.target.value); setPage(1); }}>
                  <option value="">Phạm vi: Tất cả</option>
                  <option value="DOMESTIC">Nội địa</option>
                  <option value="INTERNATIONAL">Quốc tế</option>
                </select>
              )}
              <select className="toolbar-ctrl" value={ccy} onChange={(e) => setCcy(e.target.value)}>
                <option value="USD">USD ($)</option>
                <option value="EUR">EUR (€)</option>
              </select>
            </div>
            <div className="toolbar-divider" />
            <div className="toolbar-daterange">
              <I name="calendar" />
              <input className={`toolbar-ctrl toolbar-date${from ? " filter-active" : ""}`} type="date" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
              <span className="toolbar-date-sep">→</span>
              <input className={`toolbar-ctrl toolbar-date${to ? " filter-active" : ""}`} type="date" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
            </div>
          </div>
        )}
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>{visibleOrder.map(headerCell)}</tr>
            </thead>
            <tbody>
              {rows.length > 0 ? rows.map((r) => (
                  <tr key={r.id} className="clickable" onClick={() => setViewId(r.id)}>
                    {visibleOrder.map((id) => bodyCell(id, r))}
                  </tr>
                )) : (
                <tr>
                  <td colSpan={visibleOrder.length}>
                    <div className="empty-state">
                      <div className="empty-state-icon">
                        <I name={isIncome ? "trending-up" : "receipt"} />
                      </div>
                      <strong>{isIncome ? "Chưa có khoản thu nào" : "Chưa có khoản chi nào"}</strong>
                      <p>{q || cat || src ? "Không có kết quả khớp với bộ lọc hiện tại." : isIncome ? "Bắt đầu bằng cách thêm khoản thu đầu tiên." : "Bắt đầu bằng cách thêm khoản chi đầu tiên."}</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        {filtered.length ? (
          <div className="pager">
            <span className="muted">Hiển thị {(p - 1) * PAGE_SIZE + 1} – {Math.min(p * PAGE_SIZE, filtered.length)} trên tổng số {filtered.length} bản ghi</span>
            <div className="pager-pages">
              <button type="button" className="btn ghost pager-btn" disabled={p <= 1} onClick={() => setPage(p - 1)}>Trước</button>
              {Array.from({ length: pages }, (_, i) => i + 1).filter((i) => i === 1 || i === pages || Math.abs(i - p) <= 2).map((i) => (
                <button key={i} type="button" className={`btn ${i === p ? "primary" : "ghost"} pager-btn`} onClick={() => setPage(i)}>{i}</button>
              ))}
              <button type="button" className="btn ghost pager-btn" disabled={p >= pages} onClick={() => setPage(p + 1)}>Sau</button>
            </div>
          </div>
        ) : null}
      </article>

      {isIncome ? (
        <div className="grid-2">
          <article className="card">
            <h3 className="section-title">Ghi chú vận hành</h3>
            <div className="note-stack">
              <div className="alert-box info"><I name="info" /><div><b>Chính sách Thuế EU (VAT)</b>Loại bỏ đăng ký thuế VAT 19% cho đơn hàng tại Đức và 20% cho đơn hàng tại Pháp đối với mặt hàng handmade dưới 150€ từ 01/06/2024.</div></div>
              <div className="alert-box warn"><I name="triangle-alert" /><div><b>Đối soát cuối tháng</b>Lưu ý đối soát với đối tác cổng thanh toán Stripe và PayPal vào ngày 28 hàng tháng.</div></div>
            </div>
          </article>
          <article className="card">
            <h3 className="section-title">Phân bổ ngoại tệ</h3>
            <p className="muted">Tỷ trọng doanh thu theo loại tiền</p>
            <div className="fx-row" style={{ marginTop: 16 }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: 12, marginBottom: 6 }}><span>$ USD (Đô la Mỹ)</span><b>68%</b></div>
              <div className="fx-bar"><div className="fx-usd" /></div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: 12, margin: "12px 0 6px" }}><span>€ EUR (Euro)</span><b>32%</b></div>
              <div className="fx-bar"><div className="fx-eur" style={{ width: "32%", marginLeft: "auto" }} /></div>
            </div>
          </article>
        </div>
      ) : null}

      <div className={`modal-back${colsOpen ? " open" : ""}`} onClick={() => setColsOpen(false)}>
        <div className="modal cols-modal" onClick={(e) => e.stopPropagation()}>
          <h3>{isIncome ? "Cột danh sách khoản thu" : "Cột danh sách khoản chi"}</h3>
          <p>Chọn cột cần hiển thị và dùng mũi tên để thay đổi thứ tự từ trái sang phải.</p>
          <div className="col-picker-actions">
            <button className="btn ghost" type="button" onClick={() => setDraftCols(Object.fromEntries(colDefs(kind).map((c) => [c.id, true])))}>Chọn tất cả</button>
            <button className="btn ghost" type="button" onClick={() => setDraftCols(Object.fromEntries(colDefs(kind).map((c) => [c.id, Boolean(c.lock)])))}>Bỏ chọn</button>
          </div>
          <div className="col-picker">
            {draftOrder.map((id, index) => {
              const c = defsById[id];
              return (
                <div key={c.id} className={`col-pick${c.lock ? " is-lock" : ""}`}>
                  <label>
                    <input type="checkbox" checked={Boolean(draftCols[c.id])} disabled={c.lock} onChange={(e) => setDraftCols((m) => ({ ...m, [c.id]: e.target.checked }))} />
                    <span>{c.label}{c.lock ? " · luôn hiện" : ""}</span>
                  </label>
                  <span className="col-order-actions">
                    <button type="button" aria-label={`Đưa cột ${c.label} sang trái`} disabled={index === 0} onClick={() => moveDraftCol(c.id, -1)}>←</button>
                    <button type="button" aria-label={`Đưa cột ${c.label} sang phải`} disabled={index === draftOrder.length - 1} onClick={() => moveDraftCol(c.id, 1)}>→</button>
                  </span>
                </div>
              );
            })}
          </div>
          <div className="modal-actions">
            <button className="btn ghost" type="button" onClick={() => { localStorage.removeItem("fm_cols_v3_" + kind); localStorage.removeItem("fm_col_order_v3_" + kind); const m = defaultColMap(kind); const order = colDefs(kind).map((c) => c.id); setCols(m); setColOrder(order); setColsOpen(false); }}>Mặc định</button>
            <button className="btn secondary" type="button" onClick={() => setColsOpen(false)}>Hủy</button>
            <button className="btn primary" type="button" onClick={() => { saveCols(kind, draftCols, draftOrder); setCols(draftCols); setColOrder(draftOrder); setColsOpen(false); }}>Áp dụng</button>
          </div>
        </div>
      </div>

      <div className={`modal-back${viewId ? " open" : ""}`} onClick={() => setViewId(null)}>
        <div onClick={(e) => e.stopPropagation()}>
          {viewId ? (
            <RecordDetail
              kind={kind}
              rec={viewRec}
              onClose={() => setViewId(null)}
              onEdit={() => { setViewId(null); setParams({ edit: String(viewId) }); }}
              onDelete={() => {
                setConfirm({
                  text: isIncome ? "Bạn có chắc muốn xóa khoản thu này?" : "Bạn có chắc muốn xóa khoản chi này?",
                  onOk: () => { softDelete(kind, viewId); setViewId(null); },
                });
              }}
              toast={toast}
            />
          ) : null}
        </div>
      </div>

      <div className={`modal-back${formNew || formEdit ? " open" : ""}`} onClick={closeForm}>
        <div onClick={(e) => e.stopPropagation()}>
          {formNew || formEdit ? <RecordForm kind={kind} rec={formRec} onClose={closeForm} /> : null}
        </div>
      </div>
    </>
  );
}
