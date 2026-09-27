// Dữ liệu tĩnh, định dạng số/tiền/ngày và các hàm tính toán dùng chung.
const FX_USD_TO_EUR = 0.92;
const INCOME_CATEGORIES = [{ id: 1, name: "Bán hàng" }, { id: 2, name: "Thu khác" }];
const EXPENSE_CATEGORIES = [
  { id: 3, name: "Nguyên vật liệu" }, { id: 4, name: "Bao bì / đóng gói" },
  { id: 5, name: "Vận chuyển" }, { id: 6, name: "Quảng cáo" },
  { id: 7, name: "Phí dịch vụ" }, { id: 8, name: "Lương nhân viên" },
  { id: 9, name: "Điện / nước / Internet" }, { id: 10, name: "Thuê mặt bằng" },
  { id: 11, name: "Công cụ / thiết bị" }, { id: 12, name: "Chi khác" },
];

const CHART_INCOME = "#0071e3";
const CHART_EXPENSE = "#b3261e";
const CHART_PALETTE = ["#0071e3", "#b3261e", "#1d7a4c", "#9a6700", "#0058b8", "#2b93f0"];

/** Escape chuỗi trước khi chèn vào HTML. */
function esc(v) {
  return String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

const INCOME_COLS = [
  { id: "order", label: "Mã đơn", def: true },
  { id: "product", label: "Sản phẩm", def: true, lock: true },
  { id: "category", label: "Loại thu", def: true },
  { id: "date", label: "Ngày", def: true },
  { id: "region", label: "Khu vực", def: true },
  { id: "qty", label: "SL", def: true },
  { id: "item", label: "Tiền hàng", def: false },
  { id: "discount", label: "Giảm giá", def: false },
  { id: "ship", label: "Vận chuyển", def: false },
  { id: "amount", label: "Trước thuế", def: false },
  { id: "taxPercent", label: "% thuế", def: true },
  { id: "tax", label: "Thuế (tiền)", def: false },
  { id: "afterTax", label: "Sau thuế", def: true },
  { id: "source", label: "Nguồn", def: true },
];
const EXPENSE_COLS = [
  { id: "product", label: "Nội dung", def: true, lock: true },
  { id: "payee", label: "Bên nhận", def: true },
  { id: "category", label: "Loại chi", def: true },
  { id: "date", label: "Ngày", def: true },
  { id: "origin", label: "Phạm vi", def: true },
  { id: "amount", label: "Trước thuế", def: false },
  { id: "taxPercent", label: "% thuế", def: true },
  { id: "afterTax", label: "Sau thuế", def: true },
  { id: "source", label: "Nguồn", def: true },
];

function colDefs(kind) {
  return kind === "expense" ? EXPENSE_COLS : INCOME_COLS;
}
function defaultColMap(kind) {
  return Object.fromEntries(colDefs(kind).map((c) => [c.id, Boolean(c.def)]));
}
function loadCols(kind) {
  const base = defaultColMap(kind);
  try {
    const saved = JSON.parse(localStorage.getItem("fm_cols_v3_" + kind) || "null");
    if (!saved || typeof saved !== "object") return base;
    colDefs(kind).forEach((c) => {
      if (c.lock) base[c.id] = true;
      else if (saved[c.id] != null) base[c.id] = Boolean(saved[c.id]);
    });
  } catch {
    /* ignore */
  }
  return base;
}
function loadColOrder(kind) {
  const ids = colDefs(kind).map((c) => c.id);
  try {
    const saved = JSON.parse(localStorage.getItem("fm_col_order_v3_" + kind) || "null");
    if (!Array.isArray(saved)) return ids;
    return [...saved.filter((id) => ids.includes(id)), ...ids.filter((id) => !saved.includes(id))];
  } catch {
    return ids;
  }
}
function saveCols(kind, map, order) {
  localStorage.setItem("fm_cols_v3_" + kind, JSON.stringify(map));
  if (Array.isArray(order)) localStorage.setItem("fm_col_order_v3_" + kind, JSON.stringify(order));
}

function round2(n) {
  return Math.round((Number(n) || 0) * 100) / 100;
}
function afterTax(amount, pct) {
  return Math.round((Number(amount) || 0) * (1 + (Number(pct) || 0) / 100) * 100) / 100;
}
function afterTaxOf(r) {
  return afterTax(r.amount, r.taxPercent);
}
function toDisplay(n, ccy) {
  const v = Number(n) || 0;
  return ccy === "EUR" ? round2(v * FX_USD_TO_EUR) : v;
}
function fromDisplay(n, ccy) {
  if (n === "" || n == null) return null;
  const v = Number(n);
  if (!Number.isFinite(v)) return null;
  return ccy === "EUR" ? round2(v / FX_USD_TO_EUR) : round2(v);
}
function fromDisplayNum(n, ccy, fallback = 0) {
  const v = fromDisplay(n, ccy);
  return v == null ? fallback : v;
}
function money(n, ccy) {
  const v = toDisplay(n, ccy);
  if (ccy === "EUR") return new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }).format(v);
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(v);
}
function usd(n) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(Number(n) || 0);
}
const pctLabel = (n) => `${Number(n) || 0}%`;
const dmy = (iso) => (iso || "").split("-").reverse().join("/");
const sourceLabel = (v) => (v === "ETSY" ? "Etsy" : v === "EXCEL_IMPORT" ? "Excel" : "Nhập tay");
const ETSY_READONLY = "Khoản nhập từ Etsy không được sửa, chỉ có thể xóa.";
const saleRegionLabel = (v) => (v === "IN_EU" ? "Trong EU" : v === "OUTSIDE_EU" ? "Ngoài EU" : "—");
const originScopeLabel = (v) => (v === "INTERNATIONAL" ? "Quốc tế" : v === "DOMESTIC" ? "Nội địa" : "—");
function catName(list, id) {
  return list.find((c) => c.id === id)?.name || "—";
}
function catTone(id) {
  return `cat-tone-${Math.abs((Number(id) || 0) - 1) % 8}`;
}
function monthKey(iso) {
  return (iso || "").slice(0, 7);
}
function lastDayOfMonth(ym) {
  const [y, m] = String(ym).split("-").map(Number);
  const d = new Date(y, m, 0).getDate();
  return `${y}-${String(m).padStart(2, "0")}-${String(d).padStart(2, "0")}`;
}
function monthLabel(key) {
  const m = Number(key.slice(5));
  return Number.isFinite(m) ? `T${m}` : key;
}
function monthlyFrom(inc, exp) {
  const map = {};
  inc.forEach((x) => {
    const k = monthKey(x.incomeDate);
    if (!k) return;
    map[k] = map[k] || { key: k, m: monthLabel(k), income: 0, expense: 0 };
    map[k].income += Number(x.amount);
  });
  exp.forEach((x) => {
    const k = monthKey(x.expenseDate);
    if (!k) return;
    map[k] = map[k] || { key: k, m: monthLabel(k), income: 0, expense: 0 };
    map[k].expense += Number(x.amount);
  });
  return Object.values(map).sort((a, b) => a.key.localeCompare(b.key));
}
function groupByCat(list, cats) {
  const map = {};
  list.forEach((e) => {
    const n = catName(cats, e.categoryId);
    map[n] = (map[n] || 0) + e.amount;
  });
  const total = Object.values(map).reduce((a, b) => a + b, 0) || 1;
  return Object.entries(map).map(([name, amount]) => ({
    name,
    amount,
    pct: Number(((amount / total) * 100).toFixed(1)),
  }));
}
function sum(list) {
  return list.reduce((a, x) => a + Number(x.amount), 0);
}
function inRange(date, from, to) {
  if (from && date < from) return false;
  if (to && date > to) return false;
  return true;
}
function recStatus(id, kind, rec) {
  const code = rec?.recordStatus;
  if (code === "PENDING") return { k: "pending", t: "Chờ xử lý" };
  if (code === "DRAFT") return { k: "draft", t: "Bản nháp" };
  if (code === "COMPLETED") return { k: "done", t: kind === "expense" ? "Hoàn tất" : "Hoàn thành" };
  const n = Number(id) % 7;
  if (kind === "expense") {
    if (n === 3) return { k: "pending", t: "Chờ xử lý" };
    return { k: "done", t: "Hoàn tất" };
  }
  if (n === 2) return { k: "pending", t: "Chờ xử lý" };
  if (n === 5) return { k: "draft", t: "Bản nháp" };
  return { k: "done", t: "Hoàn thành" };
}
function salesChannelLabel(v) {
  const map = {
    ETSY_STORE: "Etsy Store",
    WEBSITE_DIRECT: "Website Direct",
    INSTAGRAM_SHOP: "Instagram Shop",
    LOCAL_MARKET: "Local Market",
    B2B_WHOLESALE: "B2B Wholesale",
  };
  return map[v] || "—";
}
function paymentMethodLabel(v) {
  const map = {
    CREDIT_CARD: "Thẻ tín dụng",
    BANK_TRANSFER: "Chuyển khoản",
    CASH: "Tiền mặt",
    PAYPAL: "PayPal",
  };
  return map[v] || "—";
}
function valNum(v) {
  return v === 0 || v ? v : "";
}
/** Ngày theo giờ máy, dạng YYYY-MM-DD. */
function localISO(d) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}
/** Sinh các <option>; `selected` so sánh dạng chuỗi. */
function optionsHtml(items, selected) {
  return items
    .map(([v, label]) => `<option value="${esc(v)}"${String(v) === String(selected ?? "") ? " selected" : ""}>${esc(label)}</option>`)
    .join("");
}
const CCY_OPTIONS = [["USD", "USD ($)"], ["EUR", "EUR (€)"]];

/**
 * Card "Phân bổ ngoại tệ": ước tính tỷ trọng doanh thu theo loại tiền dựa trên khu vực bán
 * (đơn bán trong EU coi là thu bằng EUR, còn lại USD). `cls` = class của khung card.
 */
function fxCardHtml(incomes, ccy, cls = "card") {
  const total = sum(incomes);
  const eur = sum(incomes.filter((x) => x.saleRegion === "IN_EU"));
  const eurPct = total ? Math.round((eur / total) * 100) : 0;
  const usdPct = total ? 100 - eurPct : 0;
  const row = (label, pct, amount, barCls, extra = "") => `
    <div class="fx-line"><span>${label}</span><b>${pct}% <em>· ${esc(money(amount, ccy))}</em></b></div>
    <div class="fx-bar"><div class="${barCls}" style="width: ${pct}%;${extra}"></div></div>`;
  return `
    <article class="${cls} fx-card">
      <h3 class="section-title">Phân bổ ngoại tệ</h3>
      <p class="muted">Tỷ trọng doanh thu theo loại tiền · ước tính theo khu vực bán (trong EU = EUR)</p>
      <div class="fx-row">
        ${row("$ USD (Đô la Mỹ)", usdPct, total - eur, "fx-usd")}
        ${row("€ EUR (Euro)", eurPct, eur, "fx-eur", " margin-left: auto")}
      </div>
    </article>`;
}
