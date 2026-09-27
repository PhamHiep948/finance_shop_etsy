// Trạng thái dùng chung của ứng dụng (thay cho FinanceProvider/useFinance).
const KIND = {
  income: { path: "/incomes", dateKey: "incomeDate", label: "khoản thu" },
  expense: { path: "/expenses", dateKey: "expenseDate", label: "khoản chi" },
};
const mapRow = (key) => (r) => ({
  ...r, [key]: r.date, amount: Number(r.amount), taxPercent: Number(r.taxPercent),
  amountAfterTax: Number(r.amountAfterTax), currency: r.currencyCode,
  recipient: r.payee || "", source: r.source || "MANUAL",
});
const bodyOf = (key, p) => ({
  ...p, date: p[key], description: p.description, categoryId: p.categoryId,
  amount: p.amount, taxPercent: p.taxPercent, amountAfterTax: p.amountAfterTax,
  currencyCode: "USD", orderCode: p.orderCode || null,
  saleRegion: p.saleRegion || null, salesChannel: p.salesChannel || null,
  productQty: p.productQty || null, payee: p.recipient || null,
  originScope: p.originScope || null, paymentMethod: p.paymentMethod || null,
});

const store = {
  ccy: "USD", incomes: [], expenses: [], loading: true,
  // Khóa sổ theo tháng (lấy từ backend /settings).
  lock: { enabled: false, graceDays: 1, today: localISO(new Date()) },
};

/** Ngày cuối cùng còn được sửa các khoản thuộc tháng của `iso` (mùng GraceDays của tháng sau). */
function lastEditableDay(iso) {
  const [y, m] = String(iso).split("-").map(Number);
  return localISO(new Date(y, m, store.lock.graceDays));
}
/** Khoản có ngày `iso` thuộc tháng đã khóa sổ? */
function isLocked(iso) {
  return Boolean(store.lock.enabled && iso && store.lock.today > lastEditableDay(iso));
}
/** Ngày sớm nhất còn được nhập khoản mới. */
function firstOpenDate() {
  const [y, m] = store.lock.today.split("-").map(Number);
  const prevMonth = localISO(new Date(y, m - 2, 1));
  return isLocked(prevMonth) ? localISO(new Date(y, m - 1, 1)) : prevMonth;
}
function lockMessage(iso) {
  const [y, m] = String(iso).split("-").map(Number);
  return `Tháng ${m}/${y} đã khóa sổ (hạn chỉnh sửa: ${dmy(lastEditableDay(iso))}).`;
}
const storeListeners = [];

/** Đăng ký hàm được gọi mỗi khi dữ liệu / tiền tệ thay đổi. */
function onStoreChange(fn) {
  storeListeners.push(fn);
}
function emitStore() {
  storeListeners.forEach((fn) => fn());
}
function setCcy(v) {
  store.ccy = v;
  emitStore();
}

/** Hiện thông báo góc màn hình. `action` = { label, onClick } để thêm nút (ví dụ "Hoàn tác"). */
function toast(msg, action) {
  const wrap = document.querySelector(".toast-wrap");
  if (!wrap) return;
  const el = document.createElement("div");
  el.className = "toast";
  const text = document.createElement("span");
  text.textContent = msg;
  el.appendChild(text);
  if (action) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "toast-action";
    btn.textContent = action.label;
    btn.addEventListener("click", () => { el.remove(); action.onClick(); });
    el.appendChild(btn);
  }
  wrap.appendChild(el);
  setTimeout(() => el.remove(), action ? 6000 : 3000);
}

async function reload() {
  store.loading = true;
  try {
    const [incomeCats, expenseCats, incomeRows, expenseRows, settings] = await Promise.all([
      api("/categories/income"), api("/categories/expense"),
      fetchAllPages("/incomes"), fetchAllPages("/expenses"),
      api("/settings").catch(() => null),
    ]);
    if (settings?.periodLock) store.lock = settings.periodLock;
    INCOME_CATEGORIES.splice(0, INCOME_CATEGORIES.length, ...incomeCats);
    EXPENSE_CATEGORIES.splice(0, EXPENSE_CATEGORIES.length, ...expenseCats);
    store.incomes = incomeRows.map(mapRow("incomeDate"));
    store.expenses = expenseRows.map(mapRow("expenseDate"));
  } catch (e) {
    toast(e.message);
  } finally {
    store.loading = false;
    emitStore();
  }
}

/** Lưu khoản thu/chi, rồi tải lên các chứng từ `files` (nếu có). Trả về bản ghi đã lưu hoặc null. */
async function saveRecord(kind, payload, id, files = []) {
  const config = KIND[kind];
  const previous = id ? (kind === "income" ? store.incomes : store.expenses).find((r) => r.id === id) : null;
  const body = bodyOf(config.dateKey, { salesChannel: previous?.salesChannel, paymentMethod: previous?.paymentMethod, ...payload });
  let saved;
  try {
    saved = await api(`${config.path}${id ? `/${id}` : ""}`, { method: id ? "PUT" : "POST", body });
  } catch (e) {
    toast(e.message);
    return null;
  }
  let uploadError = "";
  if (files.length) {
    try { await uploadAttachments(kind, saved.id, files); }
    catch (e) { uploadError = e.message; }
  }
  await reload();
  toast(uploadError
    ? `Đã lưu ${config.label} nhưng tải chứng từ lỗi: ${uploadError}`
    : `${id ? "Đã cập nhật" : "Đã thêm"} ${config.label}${files.length ? ` kèm ${files.length} chứng từ` : ""}`);
  return saved;
}

// ---- Chứng từ đính kèm
function listAttachments(kind, id) {
  return api(`${KIND[kind].path}/${id}/attachments`);
}
function uploadAttachments(kind, id, files) {
  const fd = new FormData();
  [...files].forEach((f) => fd.append("files", f));
  return api(`${KIND[kind].path}/${id}/attachments`, { method: "POST", body: fd });
}
function deleteAttachment(attachmentId) {
  return api(`/attachments/${attachmentId}`, { method: "DELETE" });
}

// ---- Loại thu / loại chi
async function categoryAction(kind, method, id, name) {
  try {
    await api(`/categories/${kind}${id ? `/${id}` : ""}`, { method, body: name === undefined ? undefined : { name } });
    await reload();
    return true;
  } catch (e) {
    toast(e.message);
    return false;
  }
}
const addCategory = (kind, name) => categoryAction(kind, "POST", null, name);
const renameCategory = (kind, id, name) => categoryAction(kind, "PUT", id, name);
const removeCategory = (kind, id) => categoryAction(kind, "DELETE", id);

/** Chuyển khoản vào thùng rác, kèm nút "Hoàn tác" trên thông báo. */
async function softDelete(kind, id) {
  try {
    await api(`${KIND[kind].path}/${id}`, { method: "DELETE" });
    await reload();
    toast(`Đã chuyển ${KIND[kind].label} vào thùng rác`, { label: "Hoàn tác", onClick: () => restoreEntry(id) });
  } catch (e) {
    toast(e.message);
  }
}

// ---- Thùng rác
const listTrash = () => api("/trash");
async function restoreEntry(id, quiet = false) {
  try {
    await api(`/trash/${id}/restore`, { method: "POST" });
    await reload();
    if (!quiet) toast("Đã khôi phục khoản");
    return true;
  } catch (e) {
    toast(e.message);
    return false;
  }
}
const purgeEntry = (id) => api(`/trash/${id}`, { method: "DELETE" });
const emptyTrash = () => api("/trash", { method: "DELETE" });

// ---- Lịch sử chỉnh sửa
function listAudit(params) {
  const qs = new URLSearchParams(Object.entries(params).filter(([, v]) => v !== "" && v != null));
  return api(`/audit?${qs}`);
}

// ---- Etsy
const etsyStatus = () => api("/etsy/status");
const etsyOrders = (from, to) => api(`/etsy/orders?from=${from}&to=${to}`);
const etsyImport = (orders) => api("/etsy/import", { method: "POST", body: { orders } });
const etsyDisconnect = () => api("/etsy/disconnect", { method: "POST" });

function incomePayload(fd, ccy) {
  return {
    incomeDate: fd.get("incomeDate"),
    description: fd.get("description"),
    categoryId: Number(fd.get("categoryId")),
    amount: fromDisplayNum(fd.get("amount"), ccy),
    currency: "USD",
    referenceCode: fd.get("referenceCode"),
    orderCode: String(fd.get("orderCode") || "").trim(),
    saleRegion: String(fd.get("saleRegion") || ""),
    productQty: fd.get("productQty") ? Number(fd.get("productQty")) : null,
    unitPrice: fromDisplay(fd.get("unitPrice"), ccy),
    itemTotal: fromDisplay(fd.get("itemTotal"), ccy),
    discountAmount: fromDisplayNum(fd.get("discountAmount"), ccy),
    discountCode: String(fd.get("discountCode") || "").trim(),
    subtotal: fromDisplay(fd.get("subtotal"), ccy),
    shippingAmount: fromDisplayNum(fd.get("shippingAmount"), ccy),
    taxAmount: fromDisplayNum(fd.get("taxAmount"), ccy),
    taxPercent: Number(fd.get("taxPercent") || 0),
    amountAfterTax: afterTax(fromDisplayNum(fd.get("amount"), ccy), fd.get("taxPercent")),
    source: "MANUAL",
    note: fd.get("note"),
  };
}

function expensePayload(fd, ccy) {
  return {
    expenseDate: fd.get("expenseDate"),
    description: fd.get("description"),
    categoryId: Number(fd.get("categoryId")),
    amount: fromDisplayNum(fd.get("amount"), ccy),
    currency: "USD",
    recipient: fd.get("recipient"),
    originScope: String(fd.get("originScope") || "DOMESTIC"),
    taxPercent: Number(fd.get("taxPercent") || 0),
    amountAfterTax: afterTax(fromDisplayNum(fd.get("amount"), ccy), fd.get("taxPercent")),
    source: "MANUAL",
    note: fd.get("note"),
  };
}

/** Gán các trình xử lý sự kiện cho vùng trang (ghi đè các trình xử lý của trang trước). */
function bindRoot(root, handlers = {}) {
  root.onclick = handlers.click || null;
  root.onchange = handlers.change || null;
  root.oninput = handlers.input || null;
  root.onsubmit = handlers.submit || null;
  root.ondragover = handlers.dragover || null;
  root.ondragleave = handlers.dragleave || null;
  root.ondrop = handlers.drop || null;
}

function fileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
const ATTACH_ACCEPT = ".jpg,.jpeg,.png,.gif,.webp,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.zip";
const ATTACH_MAX = 10 * 1024 * 1024;

/** Lọc file hợp lệ (đuôi + dung lượng), báo lỗi các file bị bỏ qua. */
function acceptFiles(fileList) {
  const allowed = ATTACH_ACCEPT.split(",");
  const ok = [];
  [...fileList].forEach((f) => {
    const ext = (f.name.match(/\.[^.]+$/)?.[0] || "").toLowerCase();
    if (!allowed.includes(ext)) toast(`Bỏ qua "${f.name}": định dạng không hỗ trợ.`);
    else if (f.size > ATTACH_MAX) toast(`Bỏ qua "${f.name}": vượt quá 10 MB.`);
    else ok.push(f);
  });
  return ok;
}
