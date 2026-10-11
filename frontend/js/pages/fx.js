// Trang Tỷ giá (#/fx): tự nhập tỷ giá 1 EUR = ? USD cho từng tháng.
// Tháng đã nhập: quy đổi theo số này. Tháng chưa nhập (vd tháng hiện tại chưa có thông báo): mặc định 1 EUR = 1 USD.
const FxPage = {
  s: null,

  init() {
    const now = new Date();
    this.s = { rows: [], loading: false, loaded: false, error: "", from: "", to: this.ym(now) };
  },

  destroy() {},

  /** Gợi ý tháng dạng số "06/2026" cho ô tháng (gõ tay hoặc chọn): 4 năm trước đến 12 tháng sau. */
  monthListHtml() {
    const now = new Date();
    const items = [];
    for (let i = 12; i >= -47; i--) items.push(this.mmyyyy(this.ym(new Date(now.getFullYear(), now.getMonth() + i, 1))));
    return `<datalist id="fx-month-list">${items.map((v) => `<option value="${v}"></option>`).join("")}</datalist>`;
  },

  /** "2026-06" → "06/2026". */
  mmyyyy(key) {
    return `${key.slice(5)}/${key.slice(0, 4)}`;
  },

  /** Đọc tháng gõ tay: "6/2026", "06/2026", "06-2026", "2026-06" → "2026-06"; sai thì null. */
  parseMonth(text) {
    const t = String(text).trim();
    let m = t.match(/^(\d{1,2})\s*[/.\-]\s*(\d{4})$/);
    if (m) return Number(m[1]) >= 1 && Number(m[1]) <= 12 ? `${m[2]}-${m[1].padStart(2, "0")}` : null;
    m = t.match(/^(\d{4})\s*[/.\-]\s*(\d{1,2})$/);
    if (m) return Number(m[2]) >= 1 && Number(m[2]) <= 12 ? `${m[1]}-${m[2].padStart(2, "0")}` : null;
    return null;
  },

  ym(d) {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
  },

  /** Mặc định hiện từ tháng sớm nhất có dữ liệu (ít nhất 6 tháng gần đây), tối đa 36 tháng. */
  defaultFrom() {
    const now = new Date();
    const sixAgo = this.ym(new Date(now.getFullYear(), now.getMonth() - 5, 1));
    const limit = this.ym(new Date(now.getFullYear(), now.getMonth() - 35, 1));
    const first = [...store.incomes.map((x) => x.incomeDate), ...store.expenses.map((x) => x.expenseDate)]
      .filter(Boolean).map((d) => d.slice(0, 7)).sort()[0];
    const from = first && first < sixAgo ? first : sixAgo;
    return from < limit ? limit : from;
  },

  async load() {
    const s = this.s;
    if (!s.from) s.from = this.defaultFrom();
    s.loading = true;
    try {
      s.rows = await fxMonths(s.from, s.to);
      s.error = "";
    } catch (e) {
      s.error = e.message;
    }
    s.loading = false;
    s.loaded = true;
    if (current.page === this) this.render(this.root);
  },

  async save(month, value) {
    const rate = Number(value);
    const row = this.s.rows.find((r) => r.month === month);
    const label = `tháng ${Number(month.slice(5))}/${month.slice(0, 4)}`;
    try {
      if (value === "" || !(rate > 0)) {
        if (row?.manual == null) return;
        await deleteFxMonthly(month);
        if (row) row.manual = null;
        toast(`Đã xóa tỷ giá ${label}, dùng mặc định 1 EUR = 1 USD`);
      } else {
        const saved = await setFxMonthly(month, rate);
        if (row) row.manual = saved.eurToUsd;
        toast(`Đã lưu tỷ giá ${label}: 1 EUR = ${fmtRate(saved.eurToUsd)} USD`);
      }
    } catch (e) {
      toast(e.message);
    }
    loadFx();
    this.render(this.root);
  },

  render(root) {
    this.root = root;
    const s = this.s;
    if (!s.loaded && !s.loading) this.load();
    const thisMonth = this.ym(new Date());
    const cur = s.rows.find((r) => r.month === thisMonth);

    const rowHtml = (r) => {
      const [y, m] = r.month.split("-").map(Number);
      const isNow = r.month === thisMonth;
      const hasManual = r.manual != null;
      return `
        <tr class="${isNow ? "fx-now" : ""}">
          <td><b>Tháng ${m}/${y}</b>${isNow ? ' <span class="badge ok">Tháng này</span>' : ""}</td>
          <td class="muted">Quý ${Math.floor((m - 1) / 3) + 1}/${y}</td>
          <td>
            <div class="fx-input">
              <span>1 EUR =</span>
              <input type="number" step="0.0001" min="0" data-month="${r.month}" value="${hasManual ? Number(r.manual) : ""}"
                placeholder="1" aria-label="Tỷ giá tháng ${m}/${y}" />
              <span>USD</span>
            </div>
          </td>
          <td class="amount">${r.ecb != null
            ? `${fmtRate(r.ecb)}${hasManual && Number(r.manual) === Number(r.ecb) ? "" : ` <button class="btn ghost sm" type="button" data-action="use-ecb" data-month="${r.month}">Dùng</button>`}`
            : '<span class="muted">—</span>'}</td>
          <td>${hasManual
            ? '<span class="badge ok">Tự nhập</span>'
            : '<span class="badge info">Mặc định 1</span>'}</td>
          <td>${hasManual ? `<button class="icon-ghost" type="button" data-action="clear" data-month="${r.month}" title="Xóa tỷ giá tự nhập" aria-label="Xóa tỷ giá tự nhập">${icon("x", 15)}</button>` : ""}</td>
        </tr>`;
    };

    let body;
    if (s.error) body = `<tr><td colspan="6"><div class="empty-state"><strong>Không tải được tỷ giá</strong><p>${esc(s.error)}</p></div></td></tr>`;
    else if (!s.loaded) body = '<tr><td colspan="6"><div class="empty-state"><p>Đang tải…</p></div></td></tr>';
    else body = s.rows.map(rowHtml).join("");

    root.innerHTML = `
      <article class="card fx-current">
        <div>
          <div class="muted">Tỷ giá tháng này (${Number(thisMonth.slice(5))}/${thisMonth.slice(0, 4)})</div>
          <div class="fx-current-value">${cur?.manual != null
            ? `1 EUR = ${fmtRate(cur.manual)} USD <span class="badge ok">Tự nhập</span>`
            : `1 EUR = 1 USD <span class="badge info">Mặc định · chưa nhập</span>`}</div>
        </div>
      </article>
      <article class="card" style="padding: 0">
        <div class="card-head list-card-head">
          <div class="list-card-left">
            ${this.monthListHtml()}
            <label class="fx-range">Từ tháng <input class="toolbar-ctrl fx-month" data-field="from" list="fx-month-list" inputmode="numeric"
              value="${esc(this.mmyyyy(s.from))}" placeholder="MM/YYYY" maxlength="7" /></label>
            <label class="fx-range">Đến tháng <input class="toolbar-ctrl fx-month" data-field="to" list="fx-month-list" inputmode="numeric"
              value="${esc(this.mmyyyy(s.to))}" placeholder="MM/YYYY" maxlength="7" /></label>
          </div>
        </div>
        <div class="table-wrap">
          <table class="data-table fx-table">
            <thead><tr>
              <th>Tháng</th><th>Quý</th><th>Tỷ giá tự nhập</th><th class="amount">ECB trung bình tháng</th><th>Đang dùng</th><th></th>
            </tr></thead>
            <tbody>${body}</tbody>
          </table>
        </div>
      </article>`;

    bindRoot(root, {
      click: (e) => {
        const el = e.target.closest("[data-action]");
        if (!el) return;
        const month = el.dataset.month;
        const row = s.rows.find((r) => r.month === month);
        if (el.dataset.action === "use-ecb" && row?.ecb != null) this.save(month, String(row.ecb));
        if (el.dataset.action === "clear") this.save(month, "");
      },
      change: (e) => {
        const t = e.target;
        if (t.dataset.month) { this.save(t.dataset.month, t.value.trim()); return; }
        if (t.dataset.field === "from" || t.dataset.field === "to") {
          const month = this.parseMonth(t.value);
          if (!month) { toast("Nhập tháng dạng MM/YYYY, ví dụ 06/2026."); t.value = this.mmyyyy(s[t.dataset.field]); return; }
          s[t.dataset.field] = month;
          if (s.from > s.to) [s.from, s.to] = [s.to, s.from];
          this.load();
        }
      },
      keydown: (e) => {
        if (e.key === "Enter" && e.target.dataset.month) e.target.blur();
      },
    });
  },
};

/** Tỷ giá dạng 1,1593 (tối đa 4 chữ số thập phân). */
function fmtRate(v) {
  return Number(v).toLocaleString("vi-VN", { maximumFractionDigits: 4 });
}
