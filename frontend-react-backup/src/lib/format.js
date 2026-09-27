import { FX_USD_TO_EUR } from "./data";

export const INCOME_COLS = [
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
export const EXPENSE_COLS = [
  { id: "product", label: "Nội dung", def: true, lock: true },
  { id: "payee", label: "Người nhận", def: true },
  { id: "category", label: "Loại chi", def: true },
  { id: "date", label: "Ngày", def: true },
  { id: "origin", label: "Phạm vi", def: true },
  { id: "amount", label: "Trước thuế", def: false },
  { id: "taxPercent", label: "% thuế", def: true },
  { id: "afterTax", label: "Sau thuế", def: true },
  { id: "source", label: "Nguồn", def: true },
];

export function colDefs(kind) {
  return kind === "expense" ? EXPENSE_COLS : INCOME_COLS;
}
export function defaultColMap(kind) {
  return Object.fromEntries(colDefs(kind).map((c) => [c.id, Boolean(c.def)]));
}
export function loadCols(kind) {
  const defs = colDefs(kind);
  const base = defaultColMap(kind);
  try {
    const saved = JSON.parse(localStorage.getItem("fm_cols_v3_" + kind) || "null");
    if (!saved || typeof saved !== "object") return base;
    defs.forEach((c) => {
      if (c.lock) base[c.id] = true;
      else if (saved[c.id] != null) base[c.id] = Boolean(saved[c.id]);
    });
  } catch {
    /* ignore */
  }
  return base;
}
export function loadColOrder(kind) {
  const ids = colDefs(kind).map((c) => c.id);
  try {
    const saved = JSON.parse(localStorage.getItem("fm_col_order_v3_" + kind) || "null");
    if (!Array.isArray(saved)) return ids;
    return [...saved.filter((id) => ids.includes(id)), ...ids.filter((id) => !saved.includes(id))];
  } catch {
    return ids;
  }
}
export function saveCols(kind, map, order) {
  localStorage.setItem("fm_cols_v3_" + kind, JSON.stringify(map));
  if (Array.isArray(order)) localStorage.setItem("fm_col_order_v3_" + kind, JSON.stringify(order));
}

export function round2(n) {
  return Math.round((Number(n) || 0) * 100) / 100;
}
export function afterTax(amount, pct) {
  return Math.round((Number(amount) || 0) * (1 + (Number(pct) || 0) / 100) * 100) / 100;
}
export function afterTaxOf(r) {
  return afterTax(r.amount, r.taxPercent);
}
export function toDisplay(n, ccy) {
  const v = Number(n) || 0;
  return ccy === "EUR" ? round2(v * FX_USD_TO_EUR) : v;
}
export function fromDisplay(n, ccy) {
  if (n === "" || n == null) return null;
  const v = Number(n);
  if (!Number.isFinite(v)) return null;
  return ccy === "EUR" ? round2(v / FX_USD_TO_EUR) : round2(v);
}
export function fromDisplayNum(n, ccy, fallback = 0) {
  const v = fromDisplay(n, ccy);
  return v == null ? fallback : v;
}
export function money(n, ccy) {
  const v = toDisplay(n, ccy);
  if (ccy === "EUR") return new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }).format(v);
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(v);
}
export function usd(n) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(Number(n) || 0);
}
export const pctLabel = (n) => `${Number(n) || 0}%`;
export const dmy = (iso) => (iso || "").split("-").reverse().join("/");
export const srcLabel = (s) => (s === "EXCEL_IMPORT" ? "Excel" : "Nhập tay");
export const saleRegionLabel = (v) => (v === "IN_EU" ? "Trong EU" : v === "OUTSIDE_EU" ? "Ngoài EU" : "—");
export const originScopeLabel = (v) => (v === "INTERNATIONAL" ? "Quốc tế" : v === "DOMESTIC" ? "Nội địa" : "—");
export function catName(list, id) {
  return list.find((c) => c.id === id)?.name || "—";
}
export function catTone(id) {
  return `cat-tone-${Math.abs((Number(id) || 0) - 1) % 8}`;
}
export function initials(name) {
  return (name || "?")
    .split(" ")
    .map((p) => p[0])
    .join("")
    .slice(-2)
    .toUpperCase();
}
export function monthKey(iso) {
  return (iso || "").slice(0, 7);
}
export function lastDayOfMonth(ym) {
  const [y, m] = String(ym).split("-").map(Number);
  const d = new Date(y, m, 0).getDate();
  return `${y}-${String(m).padStart(2, "0")}-${String(d).padStart(2, "0")}`;
}
export function monthLabel(key) {
  const m = Number(key.slice(5));
  return Number.isFinite(m) ? `T${m}` : key;
}
export function monthlyFrom(inc, exp) {
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
export function groupByCat(list, cats) {
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
export function sum(list) {
  return list.reduce((a, x) => a + Number(x.amount), 0);
}
export function inRange(date, from, to) {
  if (from && date < from) return false;
  if (to && date > to) return false;
  return true;
}
export function isActive(r) {
  return r && !r.deletedAt;
}
export function nextId(list) {
  return Math.max(0, ...list.map((x) => x.id)) + 1;
}
export function isExcelName(name) {
  return /\.(xlsx?|csv)$/i.test(name || "");
}
export function fileMeta(file, fallback) {
  if (!file) return fallback || null;
  return { name: file.name, type: file.type || "", size: file.size || 0 };
}
export function recStatus(id, kind, rec) {
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
export function salesChannelLabel(v) {
  const map = {
    ETSY_STORE: "Etsy Store",
    WEBSITE_DIRECT: "Website Direct",
    INSTAGRAM_SHOP: "Instagram Shop",
    LOCAL_MARKET: "Local Market",
    B2B_WHOLESALE: "B2B Wholesale",
  };
  return map[v] || "—";
}
export function paymentMethodLabel(v) {
  const map = {
    CREDIT_CARD: "Thẻ tín dụng",
    BANK_TRANSFER: "Chuyển khoản",
    CASH: "Tiền mặt",
    PAYPAL: "PayPal",
  };
  return map[v] || "—";
}

export function ccyHint(ccy) {
  return `Dữ liệu một bộ (USD). Đổi tiền xem: 1 USD = ${FX_USD_TO_EUR} EUR (mock).`;
}
export function valNum(v) {
  return v === 0 || v ? v : "";
}
