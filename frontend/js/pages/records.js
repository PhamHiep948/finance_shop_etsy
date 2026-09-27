// Trang danh sách Khoản thu (#/incomes) và Khoản chi (#/expenses),
// gồm modal chi tiết, form thêm/sửa và hộp chọn cột.
const PAGE_SIZE = 10;

function kpiCardHtml({ label, value, delta }) {
  const deltaCls = String(delta || "").startsWith("-") ? "down" : "up";
  return `
    <article class="card kpi">
      <div class="label">${esc(label)}</div>
      <div class="value num">${esc(value)}</div>
      ${delta ? `<div class="chg ${deltaCls}">${esc(delta)}</div>` : ""}
    </article>`;
}

// ---------------------------------------------------------------- Panel bên phải (dùng chung cho chi tiết và form)

/** Một dòng "nhãn | giá trị" trong panel. `value` là HTML đã escape. */
function drRow(iconName, label, value, extraCls = "") {
  return `
    <div class="dr-row${extraCls ? ` ${extraCls}` : ""}">
      <span class="dr-label">${icon(iconName, 15)}<span>${label}</span></span>
      <span class="dr-value">${value}</span>
    </div>`;
}

/** Một dòng nhập liệu: nhãn bên trái, ô nhập bên phải. `control` là HTML của input/select/textarea. */
function drInput(iconName, label, control, { required = false, hint = "", cls = "" } = {}) {
  return `
    <label class="dr-row dr-input${cls ? ` ${cls}` : ""}">
      <span class="dr-label">${icon(iconName, 15)}<span>${label}${required ? ' <span class="req">*</span>' : ""}</span></span>
      <span class="dr-value">${control}${hint ? `<small class="dr-hint">${hint}</small>` : ""}</span>
    </label>`;
}

const dash = '<span class="muted">—</span>';
const regionText = (v) => (v ? `<span class="tag-text ${v === "IN_EU" ? "tx-eu" : "tx-out"}">${saleRegionLabel(v)}</span>` : dash);
const scopeText = (v) => (v ? `<span class="tag-text ${v === "INTERNATIONAL" ? "tx-int" : "tx-dom"}">${originScopeLabel(v)}</span>` : dash);

// ---------------------------------------------------------------- Form thêm / sửa

function sectionPrefs() {
  try {
    return JSON.parse(localStorage.getItem("fm_form_sections") || "{}") || {};
  } catch {
    return {};
  }
}

/** Mục có thể thu gọn trong form (kiểu "Form · Kitchen Staff  ⌄"). */
function formSectionHtml({ id, title, sub, defaultOpen, body }) {
  const p = sectionPrefs();
  const open = p[id] == null ? defaultOpen : Boolean(p[id]);
  return `
    <div class="form-section dr-accordion${open ? "" : " is-collapsed"}" data-section="${id}">
      <button type="button" class="form-section-head${open ? "" : " is-closed"}" data-action="toggle-section"
        aria-expanded="${open}" aria-controls="${id}-body">
        <span class="dr-acc-title"><b>${esc(title)}</b><span class="muted"> · ${esc(sub)}</span></span>
        <span class="section-toggle-btn" aria-hidden="true">
          <span class="section-toggle-label">${open ? "Thu gọn" : "Mở rộng"}</span>
          ${icon(open ? "chevron-up" : "chevron-down", 18)}
        </span>
      </button>
      <div class="form-section-body" id="${id}-body"><div class="dr-rows">${body}</div></div>
    </div>`;
}

function toggleFormSection(btn) {
  const section = btn.closest(".form-section");
  const id = section.dataset.section;
  const open = section.classList.contains("is-collapsed");
  section.classList.toggle("is-collapsed", !open);
  btn.classList.toggle("is-closed", !open);
  btn.setAttribute("aria-expanded", String(open));
  btn.querySelector(".section-toggle-label").textContent = open ? "Thu gọn" : "Mở rộng";
  btn.querySelector("svg").outerHTML = icon(open ? "chevron-up" : "chevron-down", 18);
  const p = sectionPrefs();
  p[id] = open;
  localStorage.setItem("fm_form_sections", JSON.stringify(p));
}

function recordFormHtml(kind, rec, animate) {
  const { ccy } = store;
  const isIncome = kind === "income";
  const today = store.lock.today;
  const cats = isIncome ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;
  const firstCat = cats[0]?.id ?? "";
  const r = rec || (isIncome
    ? { incomeDate: today, description: "", categoryId: firstCat, amount: "", taxPercent: "", orderCode: "", saleRegion: "", productQty: "", unitPrice: "", itemTotal: "", discountAmount: "", discountCode: "", subtotal: "", shippingAmount: "", taxAmount: "", referenceCode: "", note: "" }
    : { expenseDate: today, description: "", categoryId: firstCat, amount: "", recipient: "", originScope: "DOMESTIC", taxPercent: "", note: "" });
  const disp = (v) => (v === 0 || v ? toDisplay(v, ccy) : "");
  const amount = r.amount === "" ? "" : toDisplay(r.amount, ccy);
  const taxPercent = valNum(r.taxPercent);
  const after = amount === "" ? "" : afterTax(amount, taxPercent);
  const catOptions = optionsHtml(cats.map((c) => [c.id, c.name]), r.categoryId);
  const typeLabel = isIncome ? "Khoản thu" : "Khoản chi";
  const dateName = isIncome ? "incomeDate" : "expenseDate";
  const minDate = store.lock.enabled ? firstOpenDate() : "";
  const num = (name, value, extra = "") => `<input name="${name}" type="number" step="0.01" min="0" value="${esc(value)}" ${extra}/>`;

  const main = [
    drInput("calendar", isIncome ? "Ngày thu" : "Ngày chi",
      `<input name="${dateName}" type="date" required value="${esc(r[dateName])}"${minDate ? ` min="${minDate}"` : ""} />`,
      { required: true, hint: minDate ? `Tháng đã khóa sổ không nhập được. Ngày sớm nhất: ${dmy(minDate)}.` : "" }),
    drInput("tags", isIncome ? "Loại thu" : "Loại chi", `<select name="categoryId">${catOptions}</select>`, { required: true }),
    drInput("file-text", isIncome ? "Tên sản phẩm" : "Nội dung",
      `<input name="description" required value="${esc(r.description)}"${isIncome ? ' placeholder="VD: Lily Flower"' : ""} />`, { required: true }),
    isIncome ? "" : drInput("user", "Bên nhận", `<input name="recipient" value="${esc(r.recipient)}" placeholder="Cửa hàng, nhà cung cấp…" />`),
    drInput("circle-dollar-sign", `Trước thuế (${ccy})`, `<input name="amount" type="number" step="0.01" required value="${esc(amount)}" />`, { required: true }),
    drInput("percent", "% thuế", `<input name="taxPercent" type="number" step="0.01" min="0" max="100" value="${esc(taxPercent)}" placeholder="0" />`),
    drInput("receipt", `Sau thuế (${ccy})`,
      `<input name="amountAfterTax" type="number" step="0.01" readonly tabindex="-1" class="is-computed" value="${esc(after)}" />`,
      { hint: "Tự tính từ số tiền trước thuế và % thuế.", cls: "is-readonly" }),
  ].join("");

  const sections = isIncome
    ? formSectionHtml({
        id: "income-source", title: "Nguồn thu", sub: "Mã đơn, khu vực, số lượng",
        defaultOpen: !!(r.orderCode || r.saleRegion || r.productQty || r.unitPrice),
        body: [
          drInput("hash", "Mã đơn hàng", `<input name="orderCode" value="${esc(r.orderCode)}" placeholder="VD: 4154185113" />`),
          drInput("globe", "Khu vực bán", `<select name="saleRegion">${optionsHtml([["", "—"], ["IN_EU", "Trong EU"], ["OUTSIDE_EU", "Ngoài EU"]], r.saleRegion)}</select>`),
          drInput("package", "Số lượng", `<input name="productQty" type="number" min="1" step="1" value="${esc(r.productQty || "")}" />`),
          drInput("circle-dollar-sign", `Đơn giá (${ccy})`, num("unitPrice", disp(r.unitPrice), 'placeholder="5.49" ')),
        ].join(""),
      })
      + formSectionHtml({
        id: "income-fees", title: "Chi tiết phí", sub: "Tiền hàng, giảm giá, vận chuyển, thuế",
        defaultOpen: !!(r.itemTotal || r.discountAmount || r.discountCode || r.subtotal || r.shippingAmount || r.taxAmount),
        body: [
          drInput("circle-dollar-sign", `Tiền hàng (${ccy})`, num("itemTotal", disp(r.itemTotal))),
          drInput("tags", `Giảm giá (${ccy})`, num("discountAmount", disp(r.discountAmount))),
          drInput("hash", "Mã giảm giá", `<input name="discountCode" value="${esc(r.discountCode)}" placeholder="AGSALE43" />`),
          drInput("circle-dollar-sign", `Tạm tính (${ccy})`, num("subtotal", disp(r.subtotal))),
          drInput("truck", `Vận chuyển (${ccy})`, num("shippingAmount", disp(r.shippingAmount))),
          drInput("receipt", `Thuế (${ccy})`, num("taxAmount", disp(r.taxAmount))),
        ].join(""),
      })
      + formSectionHtml({
        id: "income-extra", title: "Ghi chú", sub: "Mã tham chiếu, ghi chú",
        defaultOpen: !!(r.referenceCode || r.note),
        body: [
          drInput("hash", "Mã tham chiếu", `<input name="referenceCode" value="${esc(r.referenceCode)}" />`),
          drInput("sticky-note", "Ghi chú", `<textarea name="note" rows="3">${esc(r.note)}</textarea>`, { cls: "is-multiline" }),
        ].join(""),
      })
    : formSectionHtml({
        id: "expense-detail", title: "Phạm vi", sub: "Nội địa hoặc quốc tế", defaultOpen: true,
        body: drInput("map-pin", "Phạm vi nguồn",
          `<select name="originScope">${optionsHtml([["DOMESTIC", "Nội địa"], ["INTERNATIONAL", "Quốc tế"]], r.originScope || "DOMESTIC")}</select>`, { required: true }),
      })
      + formSectionHtml({
        id: "expense-extra", title: "Ghi chú", sub: "Ghi chú khoản chi", defaultOpen: !!r.note,
        body: drInput("sticky-note", "Ghi chú", `<textarea name="note" rows="3">${esc(r.note)}</textarea>`, { cls: "is-multiline" }),
      });

  return `
    <aside class="drawer${animate ? " is-entering" : ""}" role="dialog" aria-label="${rec ? "Sửa" : "Thêm"} ${typeLabel.toLowerCase()}">
      <button class="drawer-close" type="button" aria-label="Đóng" data-action="close-form">${icon("x", 18)}</button>
      <form class="record-form drawer-form">
        <div class="drawer-nav">
          <span class="drawer-kicker">${rec ? `${icon("pencil", 14)} Cập nhật` : `${icon("plus-circle", 14)} Giao dịch mới`} <span class="muted">/ ${typeLabel}</span></span>
        </div>
        <div class="drawer-head">
          <div>
            <h2>${rec ? esc(rec.description) : (isIncome ? "Thêm khoản thu" : "Thêm khoản chi")}</h2>
            <p class="muted">${rec ? `Sửa ${typeLabel.toLowerCase()} #${rec.id} rồi bấm Cập nhật.` : "Nhập các trường có dấu *. Chi tiết bổ sung có thể thêm sau."}</p>
          </div>
        </div>
        <div class="drawer-body">
          <section class="dr-section">
            <h4>Thông tin chính</h4>
            <div class="dr-rows">${main}</div>
          </section>
          <section class="dr-section">
            <h4>Chi tiết bổ sung</h4>
            ${sections}
          </section>
          <section class="dr-section">
            <h4>${icon("paperclip", 13)} Chứng từ đính kèm</h4>
            ${rec?.attachmentCount ? `<p class="attach-note">Khoản này đã có ${rec.attachmentCount} chứng từ (xem trong màn hình chi tiết). File chọn dưới đây sẽ được thêm vào.</p>` : ""}
            <label class="attach-drop" data-drop="form">
              ${icon("upload", 20)}
              <span><b>Chọn file</b> hoặc kéo thả vào đây · ảnh, PDF, Word, Excel · tối đa 10 MB</span>
              <input type="file" multiple accept="${ATTACH_ACCEPT}" data-upload="form" hidden />
            </label>
            <ul class="attach-pending" data-slot="pending"></ul>
          </section>
        </div>
        <div class="drawer-foot">
          <button type="button" class="btn secondary" data-action="close-form">Hủy</button>
          <button class="btn primary" type="submit">${rec ? "Cập nhật" : isIncome ? "Lưu khoản thu" : "Lưu khoản chi"}</button>
        </div>
      </form>
    </aside>`;
}

// ---------------------------------------------------------------- Chứng từ

/** Danh sách file đã chọn trong form (chưa tải lên). */
function pendingFilesHtml(files) {
  return files.map((f, i) => `
    <li>
      ${icon(f.type.startsWith("image/") ? "image" : "file", 15)}
      <span class="attach-pending-name">${esc(f.name)}</span>
      <span class="muted">${fileSize(f.size)}</span>
      <button type="button" class="icon-ghost" aria-label="Bỏ file ${esc(f.name)}" data-action="pending-remove" data-index="${i}">${icon("x", 14)}</button>
    </li>`).join("");
}

/** Lưới chứng từ đã lưu trong panel chi tiết. `att` = { loading, error, items }. */
function attachmentsHtml(att, locked) {
  if (!att || att.loading) return '<div class="attach-empty">Đang tải chứng từ…</div>';
  if (att.error) return `<div class="attach-empty">${esc(att.error)}</div>`;
  if (!att.items.length) {
    return `<div class="attach-empty">${icon("paperclip", 18)}<span>${locked ? "Không có chứng từ." : "Chưa có chứng từ. Bấm <b>Tải lên</b> hoặc kéo thả file vào đây."}</span></div>`;
  }
  return `<div class="attach-grid">${att.items.map((a) => {
    const isImage = a.contentType.startsWith("image/");
    const ext = (a.originalName.match(/\.([^.]+)$/)?.[1] || "file").toUpperCase();
    const created = new Date(a.createdAt).toLocaleDateString("vi-VN");
    return `
      <div class="attach-card">
        <a class="attach-thumb" href="${attachmentUrl(a.id)}" target="_blank" rel="noopener" title="Xem ${esc(a.originalName)}">
          ${isImage ? `<img src="${attachmentUrl(a.id)}" alt="${esc(a.originalName)}" loading="lazy" />` : `<span class="attach-ext">${esc(ext)}</span>`}
        </a>
        <div class="attach-meta">
          <span class="attach-name" title="${esc(a.originalName)}">${esc(a.originalName)}</span>
          <span class="muted">${fileSize(a.size)} · ${created}</span>
        </div>
        <div class="attach-actions">
          <a class="icon-ghost" href="${attachmentUrl(a.id)}" target="_blank" rel="noopener" title="Xem">${icon("eye", 15)}</a>
          <a class="icon-ghost" href="${attachmentUrl(a.id, true)}" title="Tải về">${icon("download", 15)}</a>
          ${locked ? "" : `<button type="button" class="icon-ghost danger" title="Xóa" data-action="att-delete" data-id="${a.id}" data-name="${esc(a.originalName)}">${icon("trash-2", 15)}</button>`}
        </div>
      </div>`;
  }).join("")}</div>`;
}

// ---------------------------------------------------------------- Panel chi tiết

/** `nav` = { index, total } để hiện nút Trước / Sau trong danh sách đang lọc. */
function recordDetailHtml(kind, rec, att, nav, animate) {
  const isIncome = kind === "income";
  const close = `<button class="drawer-close" type="button" aria-label="Đóng" data-action="close-view">${icon("x", 18)}</button>`;
  if (!rec) {
    return `
      <aside class="drawer" role="dialog">
        ${close}
        <div class="drawer-head"><div><h2>Không tìm thấy bản ghi</h2><p class="muted">Khoản này có thể đã bị xóa.</p></div></div>
      </aside>`;
  }
  const date = rec[isIncome ? "incomeDate" : "expenseDate"];
  const locked = isLocked(date);
  const st = recStatus(rec.id, kind, rec);
  const statusCls = st.k === "done" ? "ok" : st.k === "pending" ? "warn" : "neutral";
  const cats = isIncome ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;
  const catLabel = esc(catName(cats, rec.categoryId));
  const amount = Number(rec.amount) || 0;
  const taxAmt = Math.round((afterTaxOf(rec) - amount) * 100) / 100;
  const total = afterTaxOf(rec);
  const disc = Number(rec.discountAmount) || 0;
  const ship = Number(rec.shippingAmount) || 0;
  const code = isIncome ? rec.orderCode || rec.referenceCode || `IN-${String(rec.id).padStart(3, "0")}` : `EXP-${String(rec.id).padStart(3, "0")}`;
  const when = (v) => (v ? new Date(v).toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "—");
  const lockedTitle = locked ? ` title="${esc(lockMessage(date))}" disabled` : "";
  const fromEtsy = rec.source === "ETSY";
  const editTitle = locked ? lockedTitle : fromEtsy ? ` title="${esc(ETSY_READONLY)}" disabled` : "";

  const basic = isIncome
    ? [
        drRow("calendar", "Ngày thu", esc(dmy(date))),
        drRow("tags", "Loại thu", `<span class="badge record-list-label ${catTone(rec.categoryId)}">${catLabel}</span>`),
        drRow("hash", "Mã đơn", rec.orderCode ? `<span class="cell-code">${esc(rec.orderCode)}</span>` : dash),
        drRow("store", "Kênh bán", esc(salesChannelLabel(rec.salesChannel))),
        drRow("globe", "Khu vực", regionText(rec.saleRegion)),
        drRow("package", "Số lượng", esc(rec.productQty || "—")),
        rec.referenceCode ? drRow("hash", "Mã tham chiếu", esc(rec.referenceCode)) : "",
        rec.note ? drRow("sticky-note", "Ghi chú", esc(rec.note), "is-multiline") : "",
      ]
    : [
        drRow("calendar", "Ngày chi", esc(dmy(date))),
        drRow("tags", "Loại chi", `<span class="cat-text ${catTone(rec.categoryId)}">${catLabel}</span>`),
        drRow("user", "Bên nhận", esc(rec.recipient || "—")),
        drRow("map-pin", "Phạm vi", scopeText(rec.originScope)),
        drRow("credit-card", "Phương thức", esc(paymentMethodLabel(rec.paymentMethod))),
        rec.note ? drRow("sticky-note", "Ghi chú", esc(rec.note), "is-multiline") : "",
      ];

  const money2 = (v) => esc(usd(v));
  const finance = isIncome
    ? [
        rec.itemTotal ? drRow("circle-dollar-sign", "Tiền hàng", money2(rec.itemTotal)) : "",
        disc ? drRow("tags", "Giảm giá", `<span class="minus">− ${money2(disc)}</span>${rec.discountCode ? ` <span class="muted">(${esc(rec.discountCode)})</span>` : ""}`) : "",
        ship ? drRow("truck", "Vận chuyển", money2(ship)) : "",
        drRow("circle-dollar-sign", "Trước thuế", money2(amount)),
        drRow("percent", `Thuế (${pctLabel(rec.taxPercent)})`, money2(taxAmt)),
        drRow("receipt", "Tổng thu", `<b class="plus">${money2(total)}</b> <span class="muted">≈ ${esc(money(total, "EUR"))}</span>`, "is-total income"),
      ]
    : [
        drRow("circle-dollar-sign", "Trước thuế", money2(amount)),
        drRow("percent", `Thuế (${pctLabel(rec.taxPercent)})`, money2(taxAmt)),
        drRow("receipt", "Tổng thanh toán", `<b class="minus">${money2(total)}</b> <span class="muted">≈ ${esc(money(total, "EUR"))}</span>`, "is-total expense"),
      ];

  return `
    <aside class="drawer${animate ? " is-entering" : ""}" role="dialog" aria-label="Chi tiết ${isIncome ? "khoản thu" : "khoản chi"}">
      ${close}
      <div class="drawer-nav">
        <button type="button" class="drawer-nav-btn" data-action="view-prev"${nav.index <= 0 ? " disabled" : ""}>${icon("arrow-left", 16)} Trước</button>
        <span class="drawer-pos muted">${nav.index >= 0 ? `${nav.index + 1} / ${nav.total}` : ""}</span>
        <button type="button" class="drawer-nav-btn" data-action="view-next"${nav.index < 0 || nav.index >= nav.total - 1 ? " disabled" : ""}>Sau ${icon("arrow-right", 16)}</button>
      </div>
      <div class="drawer-head">
        <div class="drawer-title">
          <h2>${esc(rec.description)}</h2>
          <p class="muted">${isIncome ? "Khoản thu" : "Khoản chi"} · <span class="dr-link">${catLabel}</span> · ${esc(code)}</p>
        </div>
        <div class="drawer-actions">
          <a class="dr-icon-btn" href="#/history?type=${rec.kind}&id=${rec.id}" title="Lịch sử chỉnh sửa">${icon("history", 16)}</a>
          <button class="dr-icon-btn danger" type="button" data-action="delete"${locked ? lockedTitle : ' title="Xóa (chuyển vào thùng rác)"'}>${icon("trash-2", 16)}</button>
          <button class="btn dr-main-btn" type="button" data-action="edit"${editTitle}>${icon(locked || fromEtsy ? "lock" : "pencil", 15)} ${locked ? "Đã khóa" : fromEtsy ? "Không sửa được" : "Sửa"}</button>
        </div>
      </div>
      <div class="drawer-body">
        <section class="dr-section">
          <h4>Trạng thái</h4>
          <div class="dr-rows">
            ${drRow("circle-check", "Trạng thái", `<span class="badge ${statusCls}">${st.t}</span>`)}
            ${drRow(locked ? "lock" : "lock-open", "Khóa sổ", locked
              ? `<span class="lock-text locked">Đã khóa sổ tháng ${Number(date.slice(5, 7))}/${date.slice(0, 4)}</span>`
              : `<span class="lock-text open">Đang mở · sửa được đến hết ${esc(dmy(lastEditableDay(date)))}</span>`)}
            ${fromEtsy ? drRow("store", "Nguồn", '<span class="lock-text locked">Nhập từ Etsy · chỉ xem hoặc xóa, không sửa</span>') : ""}
            ${drRow("clock", "Tạo lúc", esc(when(rec.createdAt)))}
            ${rec.updatedAt && rec.updatedAt !== rec.createdAt ? drRow("pencil", "Cập nhật", esc(when(rec.updatedAt))) : ""}
          </div>
        </section>
        <section class="dr-section">
          <h4>Thông tin chi tiết</h4>
          <div class="dr-rows">${basic.join("")}</div>
        </section>
        <section class="dr-section">
          <h4>Chi tiết tài chính</h4>
          <div class="dr-rows">${finance.join("")}</div>
          <p class="dr-foot-note muted">Tỷ giá quy đổi: 1 USD = ${Number(FX_USD_TO_EUR).toLocaleString("vi-VN")} EUR</p>
        </section>
        <section class="dr-section dr-attach"${locked ? "" : ' data-drop="detail"'}>
          <div class="dr-section-head">
            <h4>${icon("paperclip", 13)} Chứng từ đính kèm${att?.items?.length ? ` (${att.items.length})` : ""}</h4>
            ${locked ? "" : `
              <label class="btn secondary sm attach-upload-btn">
                ${icon("upload", 14)} Tải lên
                <input type="file" multiple accept="${ATTACH_ACCEPT}" data-upload="detail" hidden />
              </label>`}
          </div>
          <div data-slot="attachments">${attachmentsHtml(att, locked)}</div>
        </section>
      </div>
    </aside>`;
}

// ---------------------------------------------------------------- Trang danh sách

function createRecordsPage(kind) {
  const isIncome = kind === "income";
  const basePath = isIncome ? "/incomes" : "/expenses";
  const dateKey = isIncome ? "incomeDate" : "expenseDate";
  const defsById = Object.fromEntries(colDefs(kind).map((c) => [c.id, c]));
  let s = null;

  const list = () => (isIncome ? store.incomes : store.expenses);
  const cats = () => (isIncome ? INCOME_CATEGORIES : EXPENSE_CATEGORIES);

  function filtered() {
    const qq = s.q.toLowerCase();
    return list().filter((r) => {
      const text = isIncome ? `${r.description} ${r.referenceCode || ""} ${r.orderCode || ""}` : `${r.description} ${r.recipient || ""}`;
      const date = r[dateKey];
      return (
        (!qq || text.toLowerCase().includes(qq)) &&
        (!s.cat || String(r.categoryId) === s.cat) &&
        (!s.src || r.source === s.src) &&
        (!s.region || r.saleRegion === s.region) &&
        (!s.origin || r.originScope === s.origin) &&
        (!s.from || date >= s.from) &&
        (!s.to || date <= s.to)
      );
    }).sort((a, b) => String(b[dateKey]).localeCompare(String(a[dateKey])) || b.id - a.id);
  }

  function kpis() {
    const { ccy } = store;
    const now = new Date();
    const ym = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
    const thisKey = ym(now);
    const prevKey = ym(new Date(now.getFullYear(), now.getMonth() - 1, 1));
    const inMonth = (k) => list().filter((x) => String(x[dateKey]).startsWith(k));
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
        { label: "Tổng sau thuế", value: money(cur.reduce((t, x) => t + x.amountAfterTax, 0), ccy), delta: "Đã gồm thuế trong tháng" },
      ];
    }
    const intl = sum(cur.filter((x) => x.originScope === "INTERNATIONAL"));
    const share = total ? ((intl / total) * 100).toFixed(1) : "0.0";
    return [
      { label: "Tổng chi phí (tháng)", value: money(total, ccy), delta: pct(total, prevTotal) },
      { label: "Giao dịch", value: String(cur.length), delta: diff(cur.length, prev.length) },
      { label: "Chi phí quốc tế", value: money(intl, ccy), delta: `${share}% tổng chi phí tháng` },
      { label: "Thuế & phí ước tính", value: money(cur.reduce((t, x) => t + (x.amountAfterTax - x.amount), 0), ccy), delta: "Phần thuế của chi phí trong tháng" },
    ];
  }

  function headerCell(id) {
    const numeric = ["qty", "amount", "taxPercent", "afterTax", "item", "discount", "ship", "tax"].includes(id);
    return `<th${numeric ? ' class="amount"' : ""}>${esc(defsById[id]?.label)}</th>`;
  }

  function bodyCell(id, r) {
    const { ccy } = store;
    const st = recStatus(r.id, kind, r);
    const statusCls = st.k === "done" ? "ok" : st.k === "pending" ? "warn" : "neutral";
    const srcCls = r.source === "ETSY" ? "src-etsy" : r.source === "EXCEL_IMPORT" ? "src-excel" : "src-manual";
    const sign = isIncome ? "plus" : "minus";
    switch (id) {
      case "date": return `<td><span class="cell-date">${esc(dmy(r[dateKey]))}</span></td>`;
      case "product": return `
        <td>
          <div class="cell-product-wrap">
            <button type="button" class="cell-link cell-product-name">${esc(r.description)}</button>
            <span class="badge cell-status-badge ${statusCls}">${st.t}</span>
            ${r.attachmentCount ? `<span class="cell-attach" title="${r.attachmentCount} chứng từ">${icon("paperclip", 12)}${r.attachmentCount}</span>` : ""}
            ${r.source === "ETSY" ? '<span class="cell-src-etsy" title="Nhập từ Etsy · chỉ xem / xóa">Etsy</span>' : ""}
            ${isLocked(r[dateKey]) ? `<span class="cell-lock" title="${esc(lockMessage(r[dateKey]))}">${icon("lock", 12)}</span>` : ""}
          </div>
        </td>`;
      case "category": return isIncome
        ? `<td><span class="badge record-list-label ${catTone(r.categoryId)}">${esc(catName(cats(), r.categoryId))}</span></td>`
        : `<td><span class="cat-text ${catTone(r.categoryId)}">${esc(catName(cats(), r.categoryId))}</span></td>`;
      case "order": return `<td><span class="cell-code">${esc(r.orderCode || "—")}</span></td>`;
      case "payee": return `<td><span class="cell-payee">${esc(r.recipient || "—")}</span></td>`;
      case "qty": return `<td class="amount"><span class="cell-qty">${esc(r.productQty || "—")}</span></td>`;
      case "amount": return `<td class="amount"><span class="${sign}">${isIncome ? "+" : "−"}${money(r.amount, ccy)}</span></td>`;
      case "taxPercent": return `<td class="amount"><span class="cell-tax-pct">${pctLabel(r.taxPercent)}</span></td>`;
      case "afterTax": return `<td class="amount"><strong class="${sign}">${money(afterTaxOf(r), ccy)}</strong></td>`;
      case "region": return `<td>${regionText(r.saleRegion)}</td>`;
      case "origin": return `<td>${scopeText(r.originScope)}</td>`;
      case "source": return `<td><span class="badge ${srcCls}">${sourceLabel(r.source)}</span></td>`;
      case "item": return `<td class="amount">${r.itemTotal ? money(r.itemTotal, ccy) : dash}</td>`;
      case "discount": return `<td class="amount">${Number(r.discountAmount) ? `<span class="minus">−${money(r.discountAmount, ccy)}</span>` : dash}</td>`;
      case "ship": return `<td class="amount">${Number(r.shippingAmount) ? money(r.shippingAmount, ccy) : dash}</td>`;
      case "tax": return `<td class="amount">${money(Number(r.taxAmount) || 0, ccy)}</td>`;
      default: return "<td></td>";
    }
  }

  /** Bảng + phân trang (vẽ lại riêng khi gõ tìm kiếm để ô tìm kiếm không mất focus). */
  function tableHtml() {
    const rowsAll = filtered();
    const pages = Math.max(1, Math.ceil(rowsAll.length / PAGE_SIZE));
    const p = Math.min(s.page, pages);
    const rows = rowsAll.slice((p - 1) * PAGE_SIZE, p * PAGE_SIZE);
    const visibleOrder = s.colOrder.filter((id) => s.cols[id]);

    const body = rows.length
      ? rows.map((r) => `<tr class="clickable" data-action="view" data-id="${r.id}">${visibleOrder.map((id) => bodyCell(id, r)).join("")}</tr>`).join("")
      : `<tr>
          <td colspan="${visibleOrder.length}">
            <div class="empty-state">
              <div class="empty-state-icon">${icon(isIncome ? "trending-up" : "receipt")}</div>
              <strong>${isIncome ? "Chưa có khoản thu nào" : "Chưa có khoản chi nào"}</strong>
              <p>${s.q || s.cat || s.src ? "Không có kết quả khớp với bộ lọc hiện tại." : isIncome ? "Bắt đầu bằng cách thêm khoản thu đầu tiên." : "Bắt đầu bằng cách thêm khoản chi đầu tiên."}</p>
            </div>
          </td>
        </tr>`;

    const pageNums = Array.from({ length: pages }, (_, i) => i + 1).filter((i) => i === 1 || i === pages || Math.abs(i - p) <= 2);
    const pager = rowsAll.length
      ? `<div class="pager">
          <span class="muted">Hiển thị ${(p - 1) * PAGE_SIZE + 1} – ${Math.min(p * PAGE_SIZE, rowsAll.length)} trên tổng số ${rowsAll.length} bản ghi</span>
          <div class="pager-pages">
            <button type="button" class="btn ghost pager-btn" data-action="page" data-page="${p - 1}"${p <= 1 ? " disabled" : ""}>Trước</button>
            ${pageNums.map((i) => `<button type="button" class="btn ${i === p ? "primary" : "ghost"} pager-btn" data-action="page" data-page="${i}">${i}</button>`).join("")}
            <button type="button" class="btn ghost pager-btn" data-action="page" data-page="${p + 1}"${p >= pages ? " disabled" : ""}>Sau</button>
          </div>
        </div>`
      : "";

    return `
      <div class="table-wrap">
        <table class="data-table">
          <thead><tr>${visibleOrder.map(headerCell).join("")}</tr></thead>
          <tbody>${body}</tbody>
        </table>
      </div>
      ${pager}`;
  }

  function filtersHtml() {
    const active = (v) => (v ? " filter-active" : "");
    const scopeSelect = isIncome
      ? `<select class="toolbar-ctrl${active(s.region)}" data-filter="region">${optionsHtml([["", "Khu vực: Tất cả"], ["IN_EU", "Trong EU"], ["OUTSIDE_EU", "Ngoài EU"]], s.region)}</select>`
      : `<select class="toolbar-ctrl${active(s.origin)}" data-filter="origin">${optionsHtml([["", "Phạm vi: Tất cả"], ["DOMESTIC", "Nội địa"], ["INTERNATIONAL", "Quốc tế"]], s.origin)}</select>`;
    return `
      <div class="record-toolbar in-card-toolbar">
        <div class="toolbar-filters">
          <select class="toolbar-ctrl${active(s.cat)}" data-filter="cat">
            ${optionsHtml([["", `${isIncome ? "Loại thu" : "Loại chi"}: Tất cả`], ...cats().map((c) => [c.id, c.name])], s.cat)}
          </select>
          <select class="toolbar-ctrl${active(s.src)}" data-filter="src">
            ${optionsHtml([["", "Nguồn: Tất cả"], ["MANUAL", "Nhập tay"], ["ETSY", "Etsy"], ["EXCEL_IMPORT", "Excel"]], s.src)}
          </select>
          ${scopeSelect}
          <select class="toolbar-ctrl" data-action="ccy">${optionsHtml(CCY_OPTIONS, store.ccy)}</select>
        </div>
        <div class="toolbar-divider"></div>
        <div class="toolbar-daterange">
          ${icon("calendar")}
          <input class="toolbar-ctrl toolbar-date${active(s.from)}" type="date" data-filter="from" value="${esc(s.from)}" />
          <span class="toolbar-date-sep">→</span>
          <input class="toolbar-ctrl toolbar-date${active(s.to)}" type="date" data-filter="to" value="${esc(s.to)}" />
        </div>
      </div>`;
  }

  function colsModalHtml() {
    if (!s.colsOpen) return `<div class="modal-back" data-close="cols"></div>`;
    return `
      <div class="modal-back open" data-close="cols">
        <div class="modal cols-modal">
          <h3>${isIncome ? "Cột danh sách khoản thu" : "Cột danh sách khoản chi"}</h3>
          <p>Chọn cột cần hiển thị và dùng mũi tên để thay đổi thứ tự từ trái sang phải.</p>
          <div class="col-picker-actions">
            <button class="btn ghost" type="button" data-action="cols-all">Chọn tất cả</button>
            <button class="btn ghost" type="button" data-action="cols-none">Bỏ chọn</button>
          </div>
          <div class="col-picker">
            ${s.draftOrder.map((id, index) => {
              const c = defsById[id];
              return `
                <div class="col-pick${c.lock ? " is-lock" : ""}">
                  <label>
                    <input type="checkbox" data-col="${c.id}"${s.draftCols[c.id] ? " checked" : ""}${c.lock ? " disabled" : ""} />
                    <span>${esc(c.label)}${c.lock ? " · luôn hiện" : ""}</span>
                  </label>
                  <span class="col-order-actions">
                    <button type="button" aria-label="Đưa cột ${esc(c.label)} sang trái" data-action="col-move" data-id="${c.id}" data-step="-1"${index === 0 ? " disabled" : ""}>←</button>
                    <button type="button" aria-label="Đưa cột ${esc(c.label)} sang phải" data-action="col-move" data-id="${c.id}" data-step="1"${index === s.draftOrder.length - 1 ? " disabled" : ""}>→</button>
                  </span>
                </div>`;
            }).join("")}
          </div>
          <div class="modal-actions">
            <button class="btn ghost" type="button" data-action="cols-default">Mặc định</button>
            <button class="btn secondary" type="button" data-action="cols-cancel">Hủy</button>
            <button class="btn primary" type="button" data-action="cols-apply">Áp dụng</button>
          </div>
        </div>
      </div>`;
  }

  // ---------------------------------------------------------------- Đồng bộ Etsy

  function etsyStatusHtml(st) {
    if (!st) return '<div class="etsy-status">Đang kiểm tra kết nối…</div>';
    if (st.mock) return "";
    if (!st.configured) {
      return `
        <div class="etsy-status error">
          ${icon("triangle-alert", 16)}
          <div><b>Chưa cấu hình key Etsy.</b> Điền <code>ApiKey</code>, <code>SharedSecret</code>, <code>ShopId</code> trong
          <code>backend/etsy.settings.json</code>. Callback URL cần khai báo trên Etsy: <code>${esc(st.redirectUri)}</code></div>
        </div>`;
    }
    if (!st.connected) {
      return `
        <div class="etsy-status">
          ${icon("link", 16)}
          <div><b>Đã có key, chưa đăng nhập Etsy.</b> Bấm nút bên phải để cấp quyền đọc đơn hàng của shop <code>${esc(st.shopId)}</code>.</div>
          <a class="btn primary sm" href="${API_BASE}/api/v1/etsy/connect">${icon("link", 14)} Kết nối Etsy</a>
        </div>`;
    }
    return `
      <div class="etsy-status ok">
        ${icon("check", 16)}
        <div><b>Đã kết nối shop ${esc(st.shopId)}.</b> Đơn đã nhập sẽ không bị nhập trùng.</div>
        <button class="btn ghost sm" type="button" data-action="etsy-disconnect">Ngắt kết nối</button>
      </div>`;
  }

  function etsyModalHtml() {
    const e = s.etsy;
    if (!e.open) return `<div class="modal-back" data-close="etsy"></div>`;
    const orders = e.orders || [];
    const picked = orders.filter((o) => e.selected.has(o.receiptId));
    const pickedTotal = picked.reduce((t, o) => t + Number(o.amount) + Number(o.tax), 0);
    const selectable = orders.filter((o) => !o.imported);
    const allPicked = selectable.length > 0 && selectable.every((o) => e.selected.has(o.receiptId));
    let body;
    if (e.loading) body = '<div class="etsy-empty">Đang tải đơn hàng từ Etsy…</div>';
    else if (e.error) body = `<div class="etsy-empty error">${esc(e.error)}</div>`;
    else if (!e.orders) body = '<div class="etsy-empty"><span>Chọn khoảng ngày rồi bấm <b>Tải đơn hàng</b>.</span></div>';
    else if (!orders.length) body = '<div class="etsy-empty">Không có đơn nào trong khoảng ngày này.</div>';
    else {
      body = `
        <div class="table-wrap etsy-table-wrap">
          <table class="etsy-table">
            <thead><tr>
              <th><input type="checkbox" data-etsy-all${allPicked ? " checked" : ""}${selectable.length ? "" : " disabled"} aria-label="Chọn tất cả" /></th>
              <th>Ngày</th><th>Mã đơn</th><th>Sản phẩm</th><th class="amount">SL</th><th>Nơi nhận</th>
              <th class="amount">Trước thuế</th><th class="amount">Thuế</th><th class="amount">Tổng</th>
            </tr></thead>
            <tbody>
              ${orders.map((o) => `
                <tr class="${o.imported ? "is-imported" : ""}">
                  <td><input type="checkbox" data-etsy-pick="${esc(o.receiptId)}"${e.selected.has(o.receiptId) ? " checked" : ""}${o.imported ? " disabled" : ""} /></td>
                  <td>${esc(dmy(o.date))}</td>
                  <td><span class="cell-code">${esc(o.receiptId)}</span></td>
                  <td>${esc(o.description)}${o.imported ? ' <span class="badge ok">Đã nhập</span>' : ""}${o.note ? `<div class="muted etsy-note">${esc(o.note)}</div>` : ""}</td>
                  <td class="amount">${o.quantity}</td>
                  <td>${esc(o.country)} · <span class="muted">${o.saleRegion === "IN_EU" ? "EU" : "Ngoài EU"}</span></td>
                  <td class="amount">${esc(usd(o.amount))}</td>
                  <td class="amount muted">${esc(usd(o.tax))}</td>
                  <td class="amount"><b>${esc(usd(Number(o.amount) + Number(o.tax)))}</b></td>
                </tr>`).join("")}
            </tbody>
          </table>
        </div>`;
    }
    return `
      <div class="modal-back open" data-close="etsy">
        <div class="modal etsy-modal">
          <div class="etsy-head">
            <h3>${icon("store", 18)} Đồng bộ đơn hàng Etsy</h3>
            <button class="icon-ghost" type="button" aria-label="Đóng" data-action="etsy-close">${icon("x")}</button>
          </div>
          ${etsyStatusHtml(e.status)}
          <form class="etsy-range" data-form="etsy-load">
            <label>Từ ngày <input type="date" name="from" value="${esc(e.from)}" required /></label>
            <label>Đến ngày <input type="date" name="to" value="${esc(e.to)}" required /></label>
            <button class="btn secondary sm" type="submit"${e.loading || !e.status?.ready ? " disabled" : ""}>${icon("refresh-cw", 14)} Tải đơn hàng</button>
            ${orders.length ? `<span class="muted etsy-count">${orders.length} đơn · ${orders.filter((o) => o.imported).length} đã nhập</span>` : ""}
          </form>
          ${body}
          <div class="modal-actions etsy-foot">
            <span class="muted">${picked.length ? `Đã chọn ${picked.length} đơn · ${esc(usd(pickedTotal))} (gồm thuế)` : "Đơn nhập vào sẽ thuộc loại thu mặc định, kênh bán Etsy Store."}</span>
            <button class="btn secondary" type="button" data-action="etsy-close">Đóng</button>
            <button class="btn primary" type="button" data-action="etsy-import"${picked.length && !e.importing ? "" : " disabled"}>
              ${e.importing ? "Đang nhập…" : `Nhập ${picked.length || ""} đơn`}
            </button>
          </div>
        </div>
      </div>`;
  }

  async function openEtsy() {
    const today = new Date();
    const from = new Date(today);
    from.setDate(from.getDate() - 30);
    s.etsy = { ...s.etsy, open: true, status: null, error: "", from: s.etsy.from || localISO(from), to: s.etsy.to || localISO(today) };
    render(root);
    try {
      s.etsy.status = await etsyStatus();
    } catch (err) {
      s.etsy.status = { ready: false, configured: false, mock: false, redirectUri: "" };
      s.etsy.error = err.message;
    }
    if (s.etsy.open) render(root);
  }

  async function loadEtsyOrders() {
    s.etsy = { ...s.etsy, loading: true, error: "", orders: null, selected: new Set() };
    render(root);
    try {
      const orders = await etsyOrders(s.etsy.from, s.etsy.to);
      s.etsy.orders = orders;
      s.etsy.selected = new Set(orders.filter((o) => !o.imported).map((o) => o.receiptId));
    } catch (err) {
      s.etsy.error = err.message;
    }
    s.etsy.loading = false;
    if (s.etsy.open) render(root);
  }

  async function importEtsy() {
    const picked = (s.etsy.orders || []).filter((o) => s.etsy.selected.has(o.receiptId));
    if (!picked.length) return;
    s.etsy.importing = true;
    render(root);
    try {
      const res = await etsyImport(picked);
      toast(`Đã nhập ${res.imported} đơn Etsy${res.skipped ? `, bỏ qua ${res.skipped} đơn trùng` : ""}${res.locked ? `, ${res.locked} đơn thuộc tháng đã khóa sổ` : ""}`);
      s.etsy.importing = false;
      await reload();
      await loadEtsyOrders();
    } catch (err) {
      toast(err.message);
      s.etsy.importing = false;
      render(root);
    }
  }

  /** Modal thêm / đổi tên / xóa loại thu (hoặc loại chi). */
  function catsModalHtml() {
    if (!s.catsOpen) return `<div class="modal-back" data-close="cats"></div>`;
    const label = isIncome ? "loại thu" : "loại chi";
    const rows = cats().map((c) => {
      const used = Number(c.used) || 0;
      if (s.catEditId === c.id) {
        return `
          <li class="cat-row is-editing">
            <form class="cat-inline-form" data-form="cat-edit" data-id="${c.id}">
              <input name="name" value="${esc(c.name)}" maxlength="150" required aria-label="Tên ${label}" />
              <button class="btn primary sm" type="submit">${icon("check", 14)} Lưu</button>
              <button class="btn ghost sm" type="button" data-action="cat-edit-cancel">Hủy</button>
            </form>
          </li>`;
      }
      return `
        <li class="cat-row">
          <span class="badge record-list-label ${catTone(c.id)}">${esc(c.name)}</span>
          <span class="cat-used muted">${used ? `${used} khoản` : "Chưa dùng"}</span>
          <span class="cat-row-actions">
            <button type="button" class="icon-ghost" title="Đổi tên" data-action="cat-edit" data-id="${c.id}">${icon("pencil", 15)}</button>
            <button type="button" class="icon-ghost danger" data-action="cat-delete" data-id="${c.id}"
              title="${used ? `Đang dùng bởi ${used} khoản, không thể xóa` : "Xóa"}"${used ? " disabled" : ""}>${icon("trash-2", 15)}</button>
          </span>
        </li>`;
    }).join("");
    return `
      <div class="modal-back open" data-close="cats">
        <div class="modal cats-modal">
          <h3>${icon("tags", 17)} Quản lý ${label}</h3>
          <p>Thêm, đổi tên hoặc xóa ${label}. Chỉ xóa được loại chưa có khoản nào sử dụng.</p>
          <form class="cat-inline-form cat-add-form" data-form="cat-add">
            <input name="name" placeholder="Tên ${label} mới, ví dụ: Bán sỉ" maxlength="150" required aria-label="Tên ${label} mới" />
            <button class="btn primary sm" type="submit">${icon("plus", 14)} Thêm</button>
          </form>
          <ul class="cat-list">${rows || `<li class="cat-row muted">Chưa có ${label} nào.</li>`}</ul>
          <div class="modal-actions">
            <button class="btn secondary" type="button" data-action="cats-close">Đóng</button>
          </div>
        </div>
      </div>`;
  }

  let root = null;
  let query = new URLSearchParams();

  function formTarget() {
    const formNew = query.get("new") === "1";
    const formEdit = query.get("edit");
    if (!formNew && !formEdit) return null;
    const rec = formEdit ? list().find((x) => x.id === Number(formEdit)) || null : null;
    if (rec?.source === "ETSY") return null; // Khoản Etsy không có form sửa.
    return { rec };
  }

  function closeForm() {
    navigate(basePath, true);
  }

  function render(el, q) {
    root = el;
    if (q) query = q;
    const hasFilters = Boolean(s.cat || s.src || s.region || s.origin || s.from || s.to);
    const form = formTarget();
    const viewRec = s.viewId ? list().find((x) => x.id === s.viewId) : null;
    // Quay về từ trang đăng nhập Etsy (?etsy=connected / ?etsy=error).
    const etsyResult = query.get("etsy");
    if (etsyResult) {
      toast(etsyResult === "connected" ? "Đã kết nối tài khoản Etsy" : "Kết nối Etsy không thành công");
      navigate(basePath, true);
      openEtsy();
      return;
    }
    // Mở form khác (thêm mới / sửa bản ghi khác) thì bỏ danh sách file đang chọn.
    const formKey = form ? query.toString() : "";
    if (formKey !== s.formKey) { s.formKey = formKey; s.pendingFiles = []; s.formAnim = Boolean(form); }

    root.innerHTML = `
      <div class="kpis">${kpis().map(kpiCardHtml).join("")}</div>
      <article class="card" style="padding: 0">
        <div class="card-head list-card-head">
          <div class="list-card-left">
            <label class="search-box list-search">
              ${icon("search")}
              <input type="search" data-filter="q" placeholder="${isIncome ? "Tìm sản phẩm, mã đơn..." : "Tìm nội dung, bên nhận..."}" value="${esc(s.q)}" />
            </label>
            <button class="btn ghost sm list-filter-btn${s.showFilters ? " active" : ""}${hasFilters ? " has-filter" : ""}" type="button" data-action="toggle-filters">
              ${icon("sliders-horizontal", 14)}
              <span>Bộ lọc</span>
              ${hasFilters ? '<span class="filter-active-dot"></span>' : ""}
              ${icon(s.showFilters ? "chevron-up" : "chevron-down", 12)}
            </button>
          </div>
          <div class="list-card-actions">
            ${isIncome ? `<button class="btn ghost sm etsy-btn" type="button" data-action="etsy-open">${icon("store")} Đồng bộ Etsy</button>` : ""}
            <button class="btn ghost sm" type="button" data-action="open-cats">${icon("tags")} ${isIncome ? "Loại thu" : "Loại chi"}</button>
            <button class="btn ghost sm" type="button" data-action="open-cols">${icon("columns-3")} Cột hiển thị</button>
            <button class="btn ghost sm" type="button" data-action="clear-filters">${icon("rotate-ccw")} Xóa bộ lọc</button>
            <button class="btn primary sm" type="button" data-action="new">
              ${icon("plus", 14)} ${isIncome ? "Thêm khoản thu" : "Thêm khoản chi"}
            </button>
          </div>
        </div>
        ${s.showFilters ? filtersHtml() : ""}
        <div data-slot="table">${tableHtml()}</div>
      </article>

      ${colsModalHtml()}
      ${catsModalHtml()}
      ${etsyModalHtml()}

      <div class="drawer-back${s.viewId ? " open" : ""}" data-close="view">
        <div data-slot="detail">${s.viewId ? recordDetailHtml(kind, viewRec, s.att, viewNav(), s.viewAnim) : ""}</div>
      </div>

      <div class="drawer-back${form ? " open" : ""}" data-close="form">
        <div>${form ? recordFormHtml(kind, form.rec, s.formAnim) : ""}</div>
      </div>`;
    s.viewAnim = false;
    s.formAnim = false;

    if (form) refreshPending();
    bindRoot(root, {
      click: onClick, change: onChange, input: onInput, submit: onSubmit,
      dragover: onDragOver, dragleave: onDragLeave, drop: onDrop,
    });
  }

  function refreshTable() {
    root.querySelector('[data-slot="table"]').innerHTML = tableHtml();
  }

  function refreshPending() {
    const slot = root.querySelector('[data-slot="pending"]');
    if (slot) slot.innerHTML = pendingFilesHtml(s.pendingFiles);
  }

  function refreshDetail() {
    const slot = root.querySelector('[data-slot="detail"]');
    if (slot && s.viewId) slot.innerHTML = recordDetailHtml(kind, list().find((x) => x.id === s.viewId), s.att, viewNav(), false);
  }

  /** Vị trí của khoản đang xem trong danh sách đang lọc (cho nút Trước / Sau). */
  function viewNav() {
    const ids = filtered().map((r) => r.id);
    return { ids, index: ids.indexOf(s.viewId), total: ids.length };
  }

  /** Mở panel chi tiết và tải danh sách chứng từ của bản ghi. */
  async function openView(id, animate = true) {
    s.viewId = id;
    s.viewAnim = animate;
    s.att = { id, loading: true, items: [] };
    render(root);
    await loadAttachments(id);
  }

  async function loadAttachments(id) {
    try {
      const items = await listAttachments(kind, id);
      if (s.viewId === id) s.att = { id, items };
    } catch (e) {
      if (s.viewId === id) s.att = { id, items: [], error: e.message };
    }
    refreshDetail();
  }

  async function uploadToDetail(files) {
    const ok = acceptFiles(files);
    if (!ok.length || !s.viewId) return;
    const id = s.viewId;
    s.att = { ...s.att, loading: true };
    refreshDetail();
    try {
      await uploadAttachments(kind, id, ok);
      toast(`Đã tải lên ${ok.length} chứng từ`);
    } catch (e) {
      toast(e.message);
    }
    await loadAttachments(id);
    reload();
  }

  function addPending(files) {
    s.pendingFiles = [...s.pendingFiles, ...acceptFiles(files)];
    refreshPending();
  }

  // ---- Kéo thả file vào vùng chứng từ (form hoặc chi tiết)
  function onDragOver(e) {
    const zone = e.target.closest("[data-drop]");
    if (!zone) return;
    e.preventDefault();
    zone.classList.add("is-dragover");
  }
  function onDragLeave(e) {
    const zone = e.target.closest("[data-drop]");
    if (zone && !zone.contains(e.relatedTarget)) zone.classList.remove("is-dragover");
  }
  function onDrop(e) {
    const zone = e.target.closest("[data-drop]");
    if (!zone) return;
    e.preventDefault();
    zone.classList.remove("is-dragover");
    if (zone.dataset.drop === "form") addPending(e.dataTransfer.files);
    else uploadToDetail(e.dataTransfer.files);
  }

  function onClick(e) {
    // Bấm ra ngoài (vào nền mờ) để đóng modal.
    if (e.target.classList.contains("modal-back") || e.target.classList.contains("drawer-back")) {
      const which = e.target.dataset.close;
      if (which === "cols") { s.colsOpen = false; render(root); }
      else if (which === "cats") { s.catsOpen = false; s.catEditId = null; render(root); }
      else if (which === "etsy") { s.etsy.open = false; render(root); }
      else if (which === "view") { s.viewId = null; render(root); }
      else if (which === "form") closeForm();
      return;
    }
    const el = e.target.closest("[data-action]");
    if (!el || !root.contains(el)) return;
    switch (el.dataset.action) {
      case "toggle-filters": s.showFilters = !s.showFilters; render(root); break;
      case "clear-filters":
        Object.assign(s, { q: "", cat: "", src: "", region: "", origin: "", from: "", to: "", page: 1 });
        render(root);
        break;
      case "new": navigate(`${basePath}?new=1`); break;
      case "page": s.page = Number(el.dataset.page); refreshTable(); break;
      case "view": openView(Number(el.dataset.id)); break;
      case "view-prev":
      case "view-next": {
        const n = viewNav();
        const next = n.ids[n.index + (el.dataset.action === "view-next" ? 1 : -1)];
        if (next) openView(next, false);
        break;
      }
      case "pending-remove":
        s.pendingFiles = s.pendingFiles.filter((_, i) => i !== Number(el.dataset.index));
        refreshPending();
        break;
      case "att-delete": {
        const attId = Number(el.dataset.id);
        const recId = s.viewId;
        openConfirm(`Xóa chứng từ "${el.dataset.name}"? File sẽ bị xóa vĩnh viễn.`, async () => {
          try {
            await deleteAttachment(attId);
            toast("Đã xóa chứng từ");
          } catch (err) {
            toast(err.message);
          }
          await loadAttachments(recId);
          reload();
        }, { title: "Xóa chứng từ", okLabel: "Xóa chứng từ", note: "File chứng từ bị xóa sẽ không khôi phục được." });
        break;
      }
      case "etsy-open": openEtsy(); break;
      case "etsy-close": s.etsy.open = false; render(root); break;
      case "etsy-import": importEtsy(); break;
      case "etsy-disconnect":
        openConfirm("Ngắt kết nối tài khoản Etsy? Bạn có thể kết nối lại bất cứ lúc nào.", async () => {
          try { await etsyDisconnect(); toast("Đã ngắt kết nối Etsy"); } catch (err) { toast(err.message); }
          openEtsy();
        }, { title: "Ngắt kết nối Etsy", okLabel: "Ngắt kết nối", icon: "link" });
        break;
      case "open-cats": s.catsOpen = true; s.catEditId = null; render(root); break;
      case "cats-close": s.catsOpen = false; s.catEditId = null; render(root); break;
      case "cat-edit":
        s.catEditId = Number(el.dataset.id);
        render(root);
        root.querySelector('[data-form="cat-edit"] input')?.select();
        break;
      case "cat-edit-cancel": s.catEditId = null; render(root); break;
      case "cat-delete": {
        const cat = cats().find((c) => c.id === Number(el.dataset.id));
        openConfirm(`Xóa ${isIncome ? "loại thu" : "loại chi"} "${cat?.name}"?`, async () => {
          if (await removeCategory(kind, cat.id)) toast(`Đã xóa "${cat.name}"`);
        }, { title: "Xóa loại" });
        break;
      }
      case "close-view": s.viewId = null; render(root); break;
      case "edit": {
        const id = s.viewId;
        if (list().find((x) => x.id === id)?.source === "ETSY") { toast(ETSY_READONLY); break; }
        s.viewId = null;
        navigate(`${basePath}?edit=${id}`);
        break;
      }
      case "delete": {
        const id = s.viewId;
        openConfirm(isIncome ? "Bạn có chắc muốn xóa khoản thu này?" : "Bạn có chắc muốn xóa khoản chi này?", () => {
          s.viewId = null;
          softDelete(kind, id);
        }, { note: TRASH_NOTE });
        break;
      }
      case "close-form": closeForm(); break;
      case "toggle-section": toggleFormSection(el); break;
      case "open-cols":
        s.draftCols = loadCols(kind);
        s.draftOrder = loadColOrder(kind);
        s.colsOpen = true;
        render(root);
        break;
      case "cols-all": s.draftCols = Object.fromEntries(colDefs(kind).map((c) => [c.id, true])); render(root); break;
      case "cols-none": s.draftCols = Object.fromEntries(colDefs(kind).map((c) => [c.id, Boolean(c.lock)])); render(root); break;
      case "col-move": {
        const order = [...s.draftOrder];
        const from = order.indexOf(el.dataset.id);
        const to = from + Number(el.dataset.step);
        if (from >= 0 && to >= 0 && to < order.length) {
          [order[from], order[to]] = [order[to], order[from]];
          s.draftOrder = order;
        }
        render(root);
        break;
      }
      case "cols-default":
        localStorage.removeItem("fm_cols_v3_" + kind);
        localStorage.removeItem("fm_col_order_v3_" + kind);
        s.cols = defaultColMap(kind);
        s.colOrder = colDefs(kind).map((c) => c.id);
        s.colsOpen = false;
        render(root);
        break;
      case "cols-cancel": s.colsOpen = false; render(root); break;
      case "cols-apply":
        saveCols(kind, s.draftCols, s.draftOrder);
        s.cols = s.draftCols;
        s.colOrder = s.draftOrder;
        s.colsOpen = false;
        render(root);
        break;
    }
  }

  function onChange(e) {
    const t = e.target;
    if (t.dataset.action === "ccy") { setCcy(t.value); return; }
    if (t.dataset.col) { s.draftCols = { ...s.draftCols, [t.dataset.col]: t.checked }; return; }
    if (t.dataset.etsyPick !== undefined) {
      if (t.checked) s.etsy.selected.add(t.dataset.etsyPick);
      else s.etsy.selected.delete(t.dataset.etsyPick);
      render(root);
      return;
    }
    if (t.dataset.etsyAll !== undefined) {
      const selectable = (s.etsy.orders || []).filter((o) => !o.imported).map((o) => o.receiptId);
      s.etsy.selected = new Set(t.checked ? selectable : []);
      render(root);
      return;
    }
    if (t.closest('[data-form="etsy-load"]')) { s.etsy[t.name] = t.value; return; }
    if (t.dataset.upload) {
      if (t.dataset.upload === "form") addPending(t.files);
      else uploadToDetail(t.files);
      t.value = "";
      return;
    }
    const key = t.dataset.filter;
    if (key && key !== "q") {
      s[key] = t.value;
      s.page = 1;
      render(root);
    }
  }

  function onInput(e) {
    const t = e.target;
    if (t.dataset.filter === "q") {
      s.q = t.value;
      s.page = 1;
      refreshTable();
      return;
    }
    // Tự tính tiền sau thuế trong form.
    const form = t.closest("form.record-form");
    if (form && (t.name === "amount" || t.name === "taxPercent")) {
      const amount = form.elements.amount.value;
      form.elements.amountAfterTax.value = amount === "" ? "" : afterTax(amount, form.elements.taxPercent.value);
    }
  }

  async function onSubmit(e) {
    const form = e.target;
    if (form.dataset.form === "etsy-load") {
      e.preventDefault();
      s.etsy.from = form.elements.from.value;
      s.etsy.to = form.elements.to.value;
      loadEtsyOrders();
      return;
    }
    const catForm = form.dataset.form;
    if (catForm === "cat-add" || catForm === "cat-edit") {
      e.preventDefault();
      const name = form.elements.name.value.trim();
      if (!name) return;
      if (catForm === "cat-add") {
        if (await addCategory(kind, name)) toast(`Đã thêm "${name}"`);
      } else {
        s.catEditId = null;
        if (await renameCategory(kind, Number(form.dataset.id), name)) toast(`Đã đổi tên thành "${name}"`);
        else render(root);
      }
      return;
    }
    if (!form.matches("form.record-form")) return;
    e.preventDefault();
    const rec = formTarget()?.rec;
    const fd = new FormData(form);
    const submitBtn = form.querySelector('button[type="submit"]');
    submitBtn.disabled = true;
    const payload = isIncome ? incomePayload(fd, store.ccy) : expensePayload(fd, store.ccy);
    const saved = await saveRecord(kind, { ...payload, source: rec?.source || "MANUAL" }, rec?.id, s.pendingFiles);
    if (saved) {
      s.pendingFiles = [];
      closeForm();
    } else {
      submitBtn.disabled = false;
    }
  }

  return {
    init() {
      s = {
        q: "", cat: "", src: "", region: "", origin: "", from: "", to: "", page: 1,
        showFilters: false, cols: loadCols(kind), colOrder: loadColOrder(kind),
        colsOpen: false, draftCols: {}, draftOrder: [], viewId: null,
        att: null, pendingFiles: [], formKey: "", catsOpen: false, catEditId: null, viewAnim: false, formAnim: false,
        etsy: { open: false, from: "", to: "", orders: null, selected: new Set() },
      };
    },
    render,
    destroy() {},
  };
}

const IncomesPage = createRecordsPage("income");
const ExpensesPage = createRecordsPage("expense");
