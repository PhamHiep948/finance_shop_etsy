// Trang Báo cáo (#/reports).
const ReportsPage = {
  state: null,
  charts: [],

  init() {
    this.state = { tab: "overview", range: null, kind: "ALL", src: "", cat: "", filters: false };
  },

  destroy() {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];
  },

  async exportReport(btn) {
    const r = this.report;
    if (!r || (!r.inc.length && !r.exp.length)) {
      toast("Không có dữ liệu trong kỳ đã chọn để xuất.");
      return;
    }
    const isPdf = btn.dataset.format === "pdf";
    const label = btn.querySelector("span");
    const old = label.textContent;
    btn.disabled = true;
    label.textContent = "Đang xuất…";
    try {
      await (isPdf ? exportReportPdf(r) : exportReportExcel(r));
      toast(isPdf ? "Đã xuất báo cáo PDF" : "Đã xuất báo cáo Excel");
    } catch (e) {
      console.error(e);
      toast(e.message || "Xuất báo cáo thất bại");
    } finally {
      btn.disabled = false;
      label.textContent = old;
    }
  },

  render(root) {
    this.destroy();
    const s = this.state;
    const { incomes, expenses, ccy } = store;
    const colors = CHART_PALETTE;

    const allMonths = [...new Set([...incomes.map((x) => x.incomeDate), ...expenses.map((x) => x.expenseDate)].filter(Boolean).map((d) => d.slice(0, 7)))].sort();
    const spanFrom = allMonths.length ? `${allMonths[0]}-01` : "";
    const spanTo = allMonths.length ? lastDayOfMonth(allMonths[allMonths.length - 1]) : "";
    const from = s.range ? s.range.from : spanFrom;
    const to = s.range ? s.range.to : spanTo;

    let inc = incomes.filter((x) => inRange(x.incomeDate, from, to));
    let exp = expenses.filter((x) => inRange(x.expenseDate, from, to));
    if (s.src) {
      inc = inc.filter((x) => x.source === s.src);
      exp = exp.filter((x) => x.source === s.src);
    }
    if (s.kind === "INCOME") exp = [];
    if (s.kind === "EXPENSE") inc = [];
    if (s.cat.startsWith("INCOME:")) {
      const id = s.cat.slice(7);
      inc = inc.filter((x) => String(x.categoryId) === id);
      exp = [];
    } else if (s.cat.startsWith("EXPENSE:")) {
      const id = s.cat.slice(8);
      exp = exp.filter((x) => String(x.categoryId) === id);
      inc = [];
    }
    const tin = sum(inc);
    const tex = sum(exp);
    const profit = tin - tex;
    const averageIncome = inc.length ? tin / inc.length : 0;
    const incomeTax = inc.reduce((total, row) => total + (Number(row.taxAmount) || Number(row.amount) * (Number(row.taxPercent) || 0) / 100), 0);
    const monthly = monthlyFrom(inc, exp);
    const monthVal = from.slice(0, 7) === to.slice(0, 7) ? from.slice(0, 7) : "all";
    const catOpts = [];
    if (s.kind !== "EXPENSE") INCOME_CATEGORIES.forEach((c) => catOpts.push([`INCOME:${c.id}`, `Thu · ${c.name}`]));
    if (s.kind !== "INCOME") EXPENSE_CATEGORIES.forEach((c) => catOpts.push([`EXPENSE:${c.id}`, `Chi · ${c.name}`]));

    const days = {};
    inc.forEach((x) => { days[x.incomeDate] = days[x.incomeDate] || { d: x.incomeDate, income: 0, expense: 0 }; days[x.incomeDate].income += x.amount; });
    exp.forEach((x) => { days[x.expenseDate] = days[x.expenseDate] || { d: x.expenseDate, income: 0, expense: 0 }; days[x.expenseDate].expense += x.amount; });
    const daily = Object.values(days).sort((a, b) => a.d.localeCompare(b.d));
    const incCats = groupByCat(inc, INCOME_CATEGORIES);

    // Dữ liệu dùng cho xuất Excel / PDF (đúng theo bộ lọc đang chọn).
    const kindLabel = { ALL: "Tất cả giao dịch", INCOME: "Chỉ khoản thu", EXPENSE: "Chỉ khoản chi" }[s.kind];
    const catLabel = s.cat ? catOpts.find(([v]) => v === s.cat)?.[1] : "";
    this.report = {
      from, to, ccy, inc, exp, tin, tex, profit, averageIncome, incomeTax, monthly, daily,
      filterLabel: [kindLabel, catLabel, s.src ? `Nguồn: ${sourceLabel(s.src)}` : ""].filter(Boolean).join(" · "),
    };
    const tabs =[["overview", "Tổng quan"], ["daily", "Theo ngày"], ["monthly", "Theo tháng"], ["in", "Theo loại thu"], ["out", "Theo loại chi"]];

    // ---- Thanh bộ lọc
    const filterbar = `
      <div class="rpt-filterbar">
        <label class="rpt-field">
          <span>Kỳ báo cáo</span>
          <select aria-label="Kỳ báo cáo" data-field="month">
            ${optionsHtml([["all", "Tất cả kỳ"], ...allMonths.map((m) => [m, `Tháng ${Number(m.slice(5))}, ${m.slice(0, 4)}`])], monthVal)}
          </select>
        </label>
        <label class="rpt-field"><span>Từ ngày</span><input type="date" data-field="from" value="${esc(from)}" /></label>
        <label class="rpt-field"><span>Đến ngày</span><input type="date" data-field="to" value="${esc(to)}" /></label>
        <label class="rpt-field">
          <span>Tiền tệ</span>
          <select data-field="ccy">${optionsHtml(CCY_OPTIONS, ccy)}</select>
        </label>
        ${s.filters ? `
          <label class="rpt-field">
            <span>Loại giao dịch</span>
            <select data-field="kind">${optionsHtml([["ALL", "Tất cả"], ["INCOME", "Chỉ khoản thu"], ["EXPENSE", "Chỉ khoản chi"]], s.kind)}</select>
          </label>
          <label class="rpt-field">
            <span>Nguồn</span>
            <select data-field="src">${optionsHtml([["", "Tất cả"], ["MANUAL", "Nhập tay"], ["ETSY", "Etsy"]], s.src)}</select>
          </label>
          <label class="rpt-field">
            <span>Nhóm</span>
            <select data-field="cat">${optionsHtml([["", "Tất cả"], ...catOpts], s.cat)}</select>
          </label>` : ""}
      </div>
      <div class="underline-tabs rpt-tabs-bar">
        ${tabs.map(([k, l]) => `<button class="tab ${s.tab === k ? "on" : ""}" type="button" data-action="tab" data-value="${k}">${l}</button>`).join("")}
        <div class="rpt-tabs-actions">
          <button class="btn ghost sm${s.filters ? " on" : ""}" type="button" data-action="toggle-filters">
            ${icon("list-filter")} Bộ lọc
          </button>
          <button class="btn sm export-btn excel" type="button" data-action="export" data-format="excel" title="Tải báo cáo dạng Excel (.xlsx)">
            ${icon("file-spreadsheet", 15)} <span>Xuất Excel</span>
          </button>
          <button class="btn sm export-btn pdf" type="button" data-action="export" data-format="pdf" title="Tải báo cáo dạng PDF">
            ${icon("file-down", 15)} <span>Xuất PDF</span>
          </button>
        </div>
      </div>`;

    // ---- Nội dung từng tab
    let content = "";
    if (s.tab === "overview") {
      const kpis = [
        [`Tổng doanh thu (${ccy})`, money(tin, ccy)],
        [`Lợi nhuận ròng (${ccy})`, money(profit, ccy)],
        [`Chi phí vận hành (${ccy})`, money(tex, ccy)],
        ["Số giao dịch", String(inc.length + exp.length)],
        [`Doanh thu TB / khoản thu (${ccy})`, money(averageIncome, ccy)],
        [`Thuế đầu ra ước tính (${ccy})`, money(incomeTax, ccy)],
      ];
      const topRows = inc.filter((x) => x.orderCode || x.productQty).sort((a, b) => Number(b.amount) - Number(a.amount)).slice(0, 5);
      content = `
        <div class="kpis rpt-kpis">
          ${kpis.map(([label, value]) => `
            <article class="card rpt-kpi">
              <div class="rpt-kpi-label">${esc(label)}</div>
              <div class="rpt-kpi-value num">${esc(value)}</div>
            </article>`).join("")}
        </div>
        <div class="rpt-split">
          <article class="card">
            <div class="card-head">
              <div><h3 class="section-title">Xu hướng Tài chính</h3><p class="muted">Doanh thu và chi phí theo kỳ đã chọn · ${ccy}</p></div>
              <div class="legend"><span><i class="dot" style="background: ${CHART_INCOME}"></i>Doanh thu</span><span><i class="dot" style="background: ${CHART_EXPENSE}"></i>Chi phí</span></div>
            </div>
            <div class="chart-wrap rpt-trend"><canvas id="rpt-trend"></canvas></div>
          </article>
          <article class="card">
            <h3 class="section-title">Cơ cấu Doanh mục</h3>
            <p class="muted" style="margin-bottom: 12px">Tỷ trọng doanh thu theo nhóm sản phẩm</p>
            <div class="donut-row">
              <div class="chart-donut"><canvas id="rpt-pie"></canvas></div>
              <div class="donut-legend-side">
                ${incCats.map((d, i) => `
                  <div><span><i class="swatch" style="background: ${colors[i % colors.length]}"></i>${esc(d.name)}</span><b>${tin ? Math.round((d.amount / tin) * 100) : 0}% · ${esc(money(d.amount, ccy))}</b></div>`).join("")}
              </div>
            </div>
          </article>
        </div>
        <div class="rpt-split">
          <article class="card" style="padding: 0">
            <div class="card-head" style="padding: 16px 16px 8px">
              <div><h3 class="section-title">Sản phẩm Bán chạy</h3><p class="muted">Danh sách các mặt hàng đóng góp doanh thu lớn nhất</p></div>
            </div>
            <div class="table-wrap">
              <table class="rpt-table">
                <thead><tr><th>Tên sản phẩm</th><th class="amount">SL</th><th class="amount">Doanh thu</th><th>Mã đơn</th></tr></thead>
                <tbody>
                  ${topRows.map((p) => `<tr><td>${esc(p.description)}</td><td class="amount muted">${esc(p.productQty || "—")}</td><td class="amount">${esc(money(p.amount, ccy))}</td><td class="amount muted">${esc(p.orderCode || "—")}</td></tr>`).join("")}
                </tbody>
              </table>
            </div>
          </article>
          ${fxCardHtml(inc, ccy)}
        </div>`;
    } else if (s.tab === "daily" || s.tab === "monthly") {
      const isDaily = s.tab === "daily";
      content = `
        <article class="card" style="padding: 0">
          <div class="table-wrap">
            <table>
              <thead><tr><th>${isDaily ? "Ngày" : "Tháng"}</th><th class="amount">Thu</th><th class="amount">Chi</th><th class="amount">Chênh lệch</th></tr></thead>
              <tbody>
                ${(isDaily ? daily : monthly).map((x) => `
                  <tr>
                    <td>${esc(isDaily ? dmy(x.d) : `${x.m}/${x.key.slice(0, 4)}`)}</td>
                    <td class="amount plus">${esc(money(x.income, ccy))}</td>
                    <td class="amount minus">${esc(money(x.expense, ccy))}</td>
                    <td class="amount">${esc(money(x.income - x.expense, ccy))}</td>
                  </tr>`).join("")}
              </tbody>
            </table>
          </div>
        </article>`;
    } else {
      const isIn = s.tab === "in";
      const groups = isIn ? incCats : groupByCat(exp, EXPENSE_CATEGORIES);
      const sorted = [...groups].sort((a, b) => b.amount - a.amount);
      content = `
        <div class="grid-2">
          <article class="card">
            <h3 class="section-title">${isIn ? "Theo loại thu" : "Theo loại chi"}</h3>
            <p class="muted" style="font-size: 12px; margin-bottom: 12px">
              ${isIn ? "Tỷ trọng doanh thu theo danh mục" : "Tỷ trọng chi phí theo danh mục"} · ${ccy}
            </p>
            <div class="chart-sm"><canvas id="rpt-cat"></canvas></div>
          </article>
          <article class="card cat-detail-card">
            <div class="cat-detail-head">
              <h3 class="section-title">Chi tiết danh mục</h3>
              <span class="cat-detail-count">${sorted.length} danh mục</span>
            </div>
            ${sorted.length === 0
              ? '<div class="empty" style="padding: 32px 0">Không có dữ liệu trong kỳ đã chọn.</div>'
              : `<div class="cat-detail-list">
                  ${sorted.map((x, i) => `
                    <div class="cat-detail-row">
                      <div class="cat-detail-top">
                        <div class="cat-detail-label">
                          <span class="cat-swatch" style="background: ${colors[i % colors.length]}"></span>
                          <span class="cat-detail-name">${esc(x.name)}</span>
                        </div>
                        <div class="cat-detail-right">
                          <span class="cat-detail-pct">${x.pct}%</span>
                          <b class="cat-detail-amount num">${esc(money(x.amount, ccy))}</b>
                        </div>
                      </div>
                      <div class="cat-progress-track">
                        <div class="cat-progress-fill" style="width: ${x.pct}%; background: ${colors[i % colors.length]}"></div>
                      </div>
                    </div>`).join("")}
                </div>`}
          </article>
        </div>`;
    }

    root.innerHTML = filterbar + content;

    bindRoot(root, {
      click: (e) => {
        const el = e.target.closest("[data-action]");
        if (!el) return;
        if (el.dataset.action === "export") { this.exportReport(el); return; }
        if (el.dataset.action === "tab") s.tab = el.dataset.value;
        else if (el.dataset.action === "toggle-filters") s.filters = !s.filters;
        this.render(root);
      },
      change: (e) => {
        const { field } = e.target.dataset;
        const v = e.target.value;
        if (!field) return;
        if (field === "ccy") { setCcy(v); return; }
        if (field === "month") s.range = v === "all" ? null : { from: `${v}-01`, to: lastDayOfMonth(v) };
        else if (field === "from") s.range = { from: v, to };
        else if (field === "to") s.range = { from, to: v };
        else if (field === "kind") { s.kind = v; s.cat = ""; }
        else s[field] = v;
        this.render(root);
      },
    });

    // ---- Biểu đồ
    if (typeof Chart === "undefined") return;
    const doughnut = (el, rows, cutout) => {
      if (!el || !rows.length) return;
      this.charts.push(new Chart(el, {
        type: "doughnut",
        data: { labels: rows.map((x) => x.name), datasets: [{ data: rows.map((x) => x.amount), backgroundColor: rows.map((_, i) => colors[i % colors.length]), borderWidth: 0 }] },
        options: { plugins: { legend: { display: false } }, cutout, maintainAspectRatio: false },
      }));
    };
    if (s.tab === "overview") {
      const conv = (v) => (ccy === "EUR" ? v * FX_USD_TO_EUR : v);
      this.charts.push(new Chart(root.querySelector("#rpt-trend"), {
        type: "line",
        data: {
          labels: monthly.length ? monthly.map((x) => x.m) : ["Không có dữ liệu"],
          datasets: [
            { label: "Doanh thu", data: monthly.map((x) => conv(x.income)), borderColor: CHART_INCOME, backgroundColor: "rgba(0,113,227,0.12)", fill: true, tension: 0.35, pointRadius: 3, pointBackgroundColor: CHART_INCOME, borderWidth: 2 },
            { label: "Chi phí", data: monthly.map((x) => conv(x.expense)), borderColor: CHART_EXPENSE, backgroundColor: "transparent", fill: false, tension: 0.35, pointRadius: 3, pointBackgroundColor: CHART_EXPENSE, borderWidth: 2 },
          ],
        },
        options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, grid: { color: "#EEF1F4" } }, x: { grid: { display: false } } }, maintainAspectRatio: false },
      }));
      doughnut(root.querySelector("#rpt-pie"), incCats, "62%");
    }
    if (s.tab === "in") doughnut(root.querySelector("#rpt-cat"), incCats, "70%");
    if (s.tab === "out") doughnut(root.querySelector("#rpt-cat"), groupByCat(exp, EXPENSE_CATEGORIES), "70%");
  },
};
