import { useEffect, useRef, useState } from "react";
import Chart from "chart.js/auto";
import { I } from "../../../lib/icons";
import { FX_USD_TO_EUR, INCOME_CATEGORIES, EXPENSE_CATEGORIES } from "../../../lib/data";
import { dmy, groupByCat, inRange, lastDayOfMonth, money, monthlyFrom, sum } from "../../../lib/format";
import { CHART_EXPENSE, CHART_INCOME, CHART_PALETTE } from "../../../lib/theme";
import { useFinance } from "../../../lib/store";

export default function Reports() {
  const { incomes, expenses, ccy, setCcy, toast } = useFinance();
  const [tab, setTab] = useState("overview");
  const allMonths = [...new Set([...incomes.map((x) => x.incomeDate), ...expenses.map((x) => x.expenseDate)].filter(Boolean).map((d) => d.slice(0, 7)))].sort();
  const spanFrom = allMonths.length ? `${allMonths[0]}-01` : "";
  const spanTo = allMonths.length ? lastDayOfMonth(allMonths[allMonths.length - 1]) : "";
  const [range, setRange] = useState(null);
  const from = range ? range.from : spanFrom;
  const to = range ? range.to : spanTo;
  const setFrom = (v) => setRange({ from: v, to });
  const setTo = (v) => setRange({ from, to: v });
  const [kind, setKind] = useState("ALL");
  const [src, setSrc] = useState("");
  const [cat, setCat] = useState("");
  const [filters, setFilters] = useState(false);
  const barRef = useRef(null);
  const pieRef = useRef(null);
  const inRef = useRef(null);
  const outRef = useRef(null);

  let inc = incomes.filter((x) => inRange(x.incomeDate, from, to));
  let exp = expenses.filter((x) => inRange(x.expenseDate, from, to));
  if (src) {
    inc = inc.filter((x) => x.source === src);
    exp = exp.filter((x) => x.source === src);
  }
  if (kind === "INCOME") exp = [];
  if (kind === "EXPENSE") inc = [];
  if (cat.startsWith("INCOME:")) {
    const id = cat.slice(7);
    inc = inc.filter((x) => String(x.categoryId) === id);
    exp = [];
  } else if (cat.startsWith("EXPENSE:")) {
    const id = cat.slice(8);
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
  if (kind !== "EXPENSE") INCOME_CATEGORIES.forEach((c) => catOpts.push({ v: `INCOME:${c.id}`, n: `Thu · ${c.name}` }));
  if (kind !== "INCOME") EXPENSE_CATEGORIES.forEach((c) => catOpts.push({ v: `EXPENSE:${c.id}`, n: `Chi · ${c.name}` }));

  useEffect(() => {
    const charts = [];
    if (tab === "overview" && barRef.current) {
      charts.push(new Chart(barRef.current, {
        type: "line",
        data: {
          labels: monthly.length ? monthly.map((x) => x.m) : ["Không có dữ liệu"],
          datasets: [
            { label: "Doanh thu", data: monthly.map((x) => (ccy === "EUR" ? x.income * FX_USD_TO_EUR : x.income)), borderColor: CHART_INCOME, backgroundColor: "rgba(0,113,227,0.12)", fill: true, tension: 0.35, pointRadius: 3, pointBackgroundColor: CHART_INCOME, borderWidth: 2 },
            { label: "Chi phí", data: monthly.map((x) => (ccy === "EUR" ? x.expense * FX_USD_TO_EUR : x.expense)), borderColor: CHART_EXPENSE, backgroundColor: "transparent", fill: false, tension: 0.35, pointRadius: 3, pointBackgroundColor: CHART_EXPENSE, borderWidth: 2 },
          ],
        },
        options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, grid: { color: "#EEF1F4" } }, x: { grid: { display: false } } }, maintainAspectRatio: false },
      }));
    }
    const cats = groupByCat(inc, INCOME_CATEGORIES);
    if (tab === "overview" && pieRef.current && cats.length) {
      const colors = CHART_PALETTE;
      charts.push(new Chart(pieRef.current, {
        type: "doughnut",
        data: { labels: cats.map((d) => d.name), datasets: [{ data: cats.map((d) => d.amount), backgroundColor: cats.map((_, i) => colors[i % colors.length]), borderWidth: 0 }] },
        options: { plugins: { legend: { display: false } }, cutout: "62%", maintainAspectRatio: false },
      }));
    }
    const doughnut = (el, rows) => {
      if (!el || !rows.length) return;
      charts.push(new Chart(el, {
        type: "doughnut",
        data: { labels: rows.map((x) => x.name), datasets: [{ data: rows.map((x) => x.amount), backgroundColor: rows.map((_, i) => CHART_PALETTE[i % CHART_PALETTE.length]), borderWidth: 0 }] },
        options: { plugins: { legend: { display: false } }, cutout: "70%", maintainAspectRatio: false },
      }));
    };
    if (tab === "in") doughnut(inRef.current, groupByCat(inc, INCOME_CATEGORIES));
    if (tab === "out") doughnut(outRef.current, groupByCat(exp, EXPENSE_CATEGORIES));
    return () => charts.forEach((c) => c.destroy());
  }, [tab, ccy, from, to, kind, src, cat, tin, tex]);

  const days = {};
  inc.forEach((x) => { days[x.incomeDate] = days[x.incomeDate] || { d: x.incomeDate, income: 0, expense: 0 }; days[x.incomeDate].income += x.amount; });
  exp.forEach((x) => { days[x.expenseDate] = days[x.expenseDate] || { d: x.expenseDate, income: 0, expense: 0 }; days[x.expenseDate].expense += x.amount; });
  const daily = Object.values(days).sort((a, b) => a.d.localeCompare(b.d));
  const tabs = [["overview", "Tổng quan"], ["daily", "Theo ngày"], ["monthly", "Theo tháng"], ["in", "Theo loại thu"], ["out", "Theo loại chi"]];
  const colors = CHART_PALETTE;

  return (
    <>
      <div className="rpt-filterbar">
        <label className="rpt-field">
          <span>Kỳ báo cáo</span>
          <select aria-label="Kỳ báo cáo" value={monthVal} onChange={(e) => {
            const v = e.target.value;
            if (v === "all") setRange(null);
            else setRange({ from: `${v}-01`, to: lastDayOfMonth(v) });
          }}>
            <option value="all">Tất cả kỳ</option>
            {allMonths.map((m) => <option key={m} value={m}>Tháng {Number(m.slice(5))}, {m.slice(0, 4)}</option>)}
          </select>
        </label>
        <label className="rpt-field">
          <span>Từ ngày</span>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="rpt-field">
          <span>Đến ngày</span>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <label className="rpt-field">
          <span>Tiền tệ</span>
          <select value={ccy} onChange={(e) => setCcy(e.target.value)}>
            <option value="USD">USD ($)</option>
            <option value="EUR">EUR (€)</option>
          </select>
        </label>
        {filters ? (
          <>
            <label className="rpt-field">
              <span>Loại giao dịch</span>
              <select value={kind} onChange={(e) => { setKind(e.target.value); setCat(""); }}>
                <option value="ALL">Tất cả</option>
                <option value="INCOME">Chỉ khoản thu</option>
                <option value="EXPENSE">Chỉ khoản chi</option>
              </select>
            </label>
            <label className="rpt-field">
              <span>Nguồn</span>
              <select value={src} onChange={(e) => setSrc(e.target.value)}>
                <option value="">Tất cả</option>
                <option value="MANUAL">Nhập tay</option>
              </select>
            </label>
            <label className="rpt-field">
              <span>Nhóm</span>
              <select value={cat} onChange={(e) => setCat(e.target.value)}>
                <option value="">Tất cả</option>
                {catOpts.map((c) => <option key={c.v} value={c.v}>{c.n}</option>)}
              </select>
            </label>
          </>
        ) : null}
      </div>
      <div className="underline-tabs rpt-tabs-bar">
        {tabs.map(([k, l]) => <button key={k} className={`tab ${tab === k ? "on" : ""}`} type="button" onClick={() => setTab(k)}>{l}</button>)}
        <div className="rpt-tabs-actions">
          <button className={`btn ghost sm${filters ? " on" : ""}`} type="button" onClick={() => setFilters((v) => !v)}>
            <I name="list-filter" /> Bộ lọc
          </button>

        </div>
      </div>
      {tab === "overview" ? (
        <>
          <div className="kpis rpt-kpis">
            {[
              { label: `Tổng doanh thu (${ccy})`, value: money(tin, ccy) },
              { label: `Lợi nhuận ròng (${ccy})`, value: money(profit, ccy) },
              { label: `Chi phí vận hành (${ccy})`, value: money(tex, ccy) },
              { label: "Số giao dịch", value: String(inc.length + exp.length) },
              { label: `Doanh thu TB / khoản thu (${ccy})`, value: money(averageIncome, ccy) },
              { label: `Thuế đầu ra ước tính (${ccy})`, value: money(incomeTax, ccy) },
            ].map((k) => (
              <article className="card rpt-kpi" key={k.label}>
                <div className="rpt-kpi-label">{k.label}</div>
                <div className="rpt-kpi-value num">{k.value}</div>
              </article>
            ))}
          </div>
          <div className="rpt-split">
            <article className="card">
              <div className="card-head">
                <div><h3 className="section-title">Xu hướng Tài chính</h3><p className="muted">Doanh thu và chi phí theo kỳ đã chọn · {ccy}</p></div>
                <div className="legend"><span><i className="dot" style={{ background: CHART_INCOME }} />Doanh thu</span><span><i className="dot" style={{ background: CHART_EXPENSE }} />Chi phí</span></div>
              </div>
              <div className="chart-wrap rpt-trend"><canvas ref={barRef} /></div>
            </article>
            <article className="card">
              <h3 className="section-title">Cơ cấu Doanh mục</h3>
              <p className="muted" style={{ marginBottom: 12 }}>Tỷ trọng doanh thu theo nhóm sản phẩm</p>
              <div className="donut-row">
                <div className="chart-donut"><canvas ref={pieRef} /></div>
                <div className="donut-legend-side">
                  {groupByCat(inc, INCOME_CATEGORIES).map((d, i) => (
                    <div key={d.name}><span><i className="swatch" style={{ background: colors[i % colors.length] }} />{d.name}</span><b>{tin ? Math.round((d.amount / tin) * 100) : 0}% · {money(d.amount, ccy)}</b></div>
                  ))}
                </div>
              </div>
            </article>
          </div>
          <div className="rpt-split">
            <article className="card" style={{ padding: 0 }}>
              <div className="card-head" style={{ padding: "16px 16px 8px" }}>
                <div><h3 className="section-title">Sản phẩm Bán chạy</h3><p className="muted">Danh sách các mặt hàng đóng góp doanh thu lớn nhất</p></div>
              </div>
              <div className="table-wrap">
                <table className="rpt-table">
                  <thead><tr><th>Tên sản phẩm</th><th className="amount">SL</th><th className="amount">Doanh thu</th><th>Mã đơn</th></tr></thead>
                  <tbody>
                    {[...inc].sort((a, b) => Number(b.amount) - Number(a.amount)).slice(0, 5).map((p) => (
                      <tr key={p.id}><td>{p.description}</td><td className="amount muted">{p.productQty || "—"}</td><td className="amount">{money(p.amount, ccy)}</td><td className="amount muted">{p.orderCode || "—"}</td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </article>
          </div>
        </>
      ) : null}
      {tab === "daily" || tab === "monthly" ? (
        <article className="card" style={{ padding: 0 }}>
          <div className="table-wrap">
            <table>
              <thead><tr><th>{tab === "daily" ? "Ngày" : "Tháng"}</th><th className="amount">Thu</th><th className="amount">Chi</th><th className="amount">Chênh lệch</th></tr></thead>
              <tbody>
                {(tab === "daily" ? daily : monthly).map((x) => (
                  <tr key={x.d || x.key}>
                    <td>{tab === "daily" ? dmy(x.d) : `${x.m}/${x.key.slice(0, 4)}`}</td>
                    <td className="amount plus">{money(x.income, ccy)}</td>
                    <td className="amount minus">{money(x.expense, ccy)}</td>
                    <td className="amount">{money(x.income - x.expense, ccy)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      ) : null}
      {(tab === "in" || tab === "out") ? (() => {
        const isIn = tab === "in";
        const groups = isIn
          ? groupByCat(inc, INCOME_CATEGORIES)
          : groupByCat(exp, EXPENSE_CATEGORIES);
        const sorted = [...groups].sort((a, b) => b.amount - a.amount);
        return (
          <div className="grid-2">
            <article className="card">
              <h3 className="section-title">{isIn ? "Theo loại thu" : "Theo loại chi"}</h3>
              <p className="muted" style={{ fontSize: 12, marginBottom: 12 }}>
                {isIn ? "Tỷ trọng doanh thu theo danh mục" : "Tỷ trọng chi phí theo danh mục"} · {ccy}
              </p>
              <div className="chart-sm"><canvas ref={isIn ? inRef : outRef} /></div>
            </article>
            <article className="card cat-detail-card">
              <div className="cat-detail-head">
                <h3 className="section-title">Chi tiết danh mục</h3>
                <span className="cat-detail-count">{sorted.length} danh mục</span>
              </div>
              {sorted.length === 0 ? (
                <div className="empty" style={{ padding: "32px 0" }}>Không có dữ liệu trong kỳ đã chọn.</div>
              ) : (
                <div className="cat-detail-list">
                  {sorted.map((x, i) => (
                    <div className="cat-detail-row" key={x.name}>
                      <div className="cat-detail-top">
                        <div className="cat-detail-label">
                          <span className="cat-swatch" style={{ background: colors[i % colors.length] }} />
                          <span className="cat-detail-name">{x.name}</span>
                        </div>
                        <div className="cat-detail-right">
                          <span className="cat-detail-pct">{x.pct}%</span>
                          <b className="cat-detail-amount num">{money(x.amount, ccy)}</b>
                        </div>
                      </div>
                      <div className="cat-progress-track">
                        <div
                          className="cat-progress-fill"
                          style={{ width: `${x.pct}%`, background: colors[i % colors.length] }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </article>
          </div>
        );
      })() : null}
    </>
  );
}
