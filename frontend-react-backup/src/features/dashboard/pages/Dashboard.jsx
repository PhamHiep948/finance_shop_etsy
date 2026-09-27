import { useEffect, useRef, useState } from "react";
import Chart from "chart.js/auto";
import { useNavigate } from "react-router-dom";
import { I } from "../../../lib/icons";
import { useFinance } from "../../../lib/store";
import { EXPENSE_CATEGORIES, INCOME_CATEGORIES } from "../../../lib/data";
import { catName, groupByCat, inRange, money } from "../../../lib/format";
import { useDashboard } from "../hooks/useDashboard";

const localISO = (d) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
const net = (x) => Number(x.amountAfterTax ?? x.amount) || 0;

// ------------------------------------------------------------------
// KPI Card — accent border trái, không có icon vô nghĩa
// ------------------------------------------------------------------
function KpiCard({ label, value, sub, accent = "#2563EB", negative }) {
  return (
    <article className="ds-kpi" style={{ "--accent": accent }}>
      <p className="ds-kpi-label">{label}</p>
      <p className={`ds-kpi-value${negative ? " ds-kpi-neg" : ""}`}>{value}</p>
      <p className="ds-kpi-sub">{sub}</p>
    </article>
  );
}

// ------------------------------------------------------------------
// Segment: Today / 7d / Month
// ------------------------------------------------------------------
function RangePill({ label, active, onClick }) {
  return (
    <button type="button" className={`ds-range-pill${active ? " active" : ""}`} onClick={onClick}>
      {label}
    </button>
  );
}

// ------------------------------------------------------------------
// Main
// ------------------------------------------------------------------
export default function Dashboard() {
  const { incomes, expenses, ccy, setCcy, can } = useFinance();
  const nav = useNavigate();

  const [tab, setTab] = useState("overview");
  const [range, setRange] = useState("7days");
  const today = new Date();

  function getFrom(r) {
    const d = new Date(today);
    if (r === "today") return localISO(d);
    if (r === "7days") { d.setDate(d.getDate() - 6); return localISO(d); }
    return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-01`;
  }
  const dashFrom = getFrom(range);
  const dashTo = localISO(today);
  const { summary, error: summaryError } = useDashboard(dashFrom, dashTo, incomes, expenses);

  const inc = incomes.filter((x) => inRange(x.incomeDate, dashFrom, dashTo));
  const exp = expenses.filter((x) => inRange(x.expenseDate, dashFrom, dashTo));
  const tin = inc.reduce((a, x) => a + Number(x.amount), 0);

  const channelData = groupByCat(inc, INCOME_CATEGORIES).map((c, i) => ({
    ...c,
    color: ["#2563EB", "#059669", "#F1641E", "#7C3AED"][i % 4],
  }));

  const last7Days = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(today);
    d.setDate(d.getDate() - (6 - i));
    const key = localISO(d);
    const label = `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`;
    const income = incomes.filter((x) => x.incomeDate === key).reduce((s, x) => s + net(x), 0);
    const expense = expenses.filter((x) => x.expenseDate === key).reduce((s, x) => s + net(x), 0);
    return { label, income, expense };
  });

  const topSKU = [...inc]
    .sort((a, b) => b.amount - a.amount)
    .slice(0, 5)
    .map((r, i) => ({ rank: i + 1, name: r.description, code: r.orderCode || "—", qty: r.productQty, amount: r.amount }));

  const recentTransactions = [
    ...incomes.map((x) => ({
      id: `in-${x.id}`,
      type: "income",
      date: x.incomeDate,
      desc: x.description,
      cat: catName(INCOME_CATEGORIES, x.categoryId),
      code: x.orderCode || x.referenceCode || `IN-${String(x.id).padStart(3, "0")}`,
      amount: x.amount,
      targetUrl: "/incomes",
    })),
    ...expenses.map((x) => ({
      id: `ex-${x.id}`,
      type: "expense",
      date: x.expenseDate,
      desc: x.description,
      cat: catName(EXPENSE_CATEGORIES, x.categoryId),
      code: x.recipient || `EXP-${String(x.id).padStart(3, "0")}`,
      amount: x.amount,
      targetUrl: "/expenses",
    })),
  ]
    .sort((a, b) => new Date(b.date) - new Date(a.date))
    .slice(0, 5);

  const barRef = useRef(null);
  const donutRef = useRef(null);

  useEffect(() => {
    if (tab !== "overview") return;
    if (!barRef.current) return;
    const chart = new Chart(barRef.current, {
      type: "bar",
      data: {
        labels: last7Days.map((d) => d.label),
        datasets: [
          {
            label: "Doanh thu",
            data: last7Days.map((d) => d.income),
            backgroundColor: "#2563EB",
            borderRadius: 3,
            barPercentage: 0.5,
            categoryPercentage: 0.65,
          },
          {
            label: "Chi phí",
            data: last7Days.map((d) => d.expense),
            backgroundColor: "#FCA5A5",
            borderRadius: 3,
            barPercentage: 0.5,
            categoryPercentage: 0.65,
          },
        ],
      },
      options: {
        animation: { duration: 350 },
        plugins: { legend: { display: false }, tooltip: { mode: "index", intersect: false } },
        scales: {
          y: {
            beginAtZero: true,
            grid: { color: "#F3F4F6", drawBorder: false },
            border: { display: false },
            ticks: { font: { size: 11, family: "Inter" }, color: "#9CA3AF", callback: (v) => v >= 1000 ? `${(v / 1000).toFixed(0)}k` : v },
          },
          x: {
            grid: { display: false },
            border: { display: false },
            ticks: { font: { size: 11, family: "Inter" }, color: "#9CA3AF" },
          },
        },
        maintainAspectRatio: false,
      },
    });
    return () => chart.destroy();
  }, [range, tab, incomes, expenses]);

  useEffect(() => {
    if (tab !== "overview") return;
    if (!donutRef.current) return;
    const chart = new Chart(donutRef.current, {
      type: "doughnut",
      data: {
        labels: channelData.map((c) => c.name),
        datasets: [{ data: channelData.map((c) => c.pct), backgroundColor: channelData.map((c) => c.color), borderWidth: 2, borderColor: "#fff" }],
      },
      options: {
        plugins: { legend: { display: false }, tooltip: { callbacks: { label: (ctx) => ` ${ctx.label}: ${ctx.parsed}%` } } },
        cutout: "68%",
        maintainAspectRatio: false,
      },
    });
    return () => chart.destroy();
  }, [range, tab, incomes]);

  const TABS = [
    ["overview", "Tổng quan"],
    ["by-channel", "Theo kênh"],
    ["top-sku", "Top sản phẩm"],
  ];

  const rangeLabel = range === "today" ? "hôm nay" : range === "7days" ? "7 ngày qua" : "tháng này";

  return (
    <div className="ds-root">
      {/* ── Sub Header Actions ── */}
      <div className="ds-header">
        <div className="ds-header-left">
          <div className="ds-tabs">
            {TABS.map(([k, l]) => (
              <button key={k} type="button" className={`ds-tab${tab === k ? " active" : ""}`} onClick={() => setTab(k)}>
                {l}
              </button>
            ))}
          </div>
        </div>
        <div className="ds-header-right">
          <div className="ds-range-group">
            <RangePill label="Hôm nay" active={range === "today"} onClick={() => setRange("today")} />
            <RangePill label="7 ngày" active={range === "7days"} onClick={() => setRange("7days")} />
            <RangePill label="Tháng này" active={range === "month"} onClick={() => setRange("month")} />
          </div>
          <select className="ds-ccy-select" value={ccy} onChange={(e) => setCcy(e.target.value)}>
            <option value="USD">USD ($)</option>
            <option value="EUR">EUR (€)</option>
          </select>
          {can("reportRead") && (
            <button className="ds-export-btn" type="button" onClick={() => nav("/reports")}>
              <I name="download" />
              <span>Xuất</span>
            </button>
          )}
        </div>
      </div>

      {/* ── Overview ── */}
      {tab === "overview" && (
        <>
          {summaryError ? <div className="alert-error" style={{ marginBottom: 12 }}>{summaryError}</div> : null}
          {/* KPI row */}
          <div className="ds-kpi-row">
            <KpiCard
              label="Doanh thu (sau thuế)"
              value={summary ? money(summary.totalIncome, ccy) : "—"}
              sub={`${inc.length} khoản thu · ${rangeLabel}`}
              accent="#2563EB"
            />
            <KpiCard
              label="Chi phí (sau thuế)"
              value={summary ? money(summary.totalExpense, ccy) : "—"}
              sub={`${exp.length} khoản chi · ${rangeLabel}`}
              accent="#DC2626"
              negative
            />
            <KpiCard
              label="Lợi nhuận ròng"
              value={summary ? money(summary.netResult, ccy) : "—"}
              sub="Doanh thu trừ chi phí"
              accent="#059669"
              negative={summary ? summary.netResult < 0 : false}
            />
            <KpiCard
              label="Số giao dịch"
              value={summary ? String(summary.transactionCount) : "—"}
              sub="Thu và chi trong kỳ"
              accent="#7C3AED"
            />
          </div>

          {/* Charts */}
          <div className="ds-grid">
            <div className="ds-main-col">
              {/* Bar chart */}
              <article className="ds-card">
                <div className="ds-card-head">
                  <div>
                    <h2 className="ds-card-title">Xu hướng 7 ngày</h2>
                    <p className="ds-card-sub">Doanh thu và chi phí (sau thuế) theo ngày</p>
                  </div>
                  <div className="ds-legend">
                    <span><i className="ds-dot" style={{ background: "#2563EB" }} />Doanh thu</span>
                    <span><i className="ds-dot" style={{ background: "#FCA5A5" }} />Chi phí</span>
                  </div>
                </div>
                <div className="ds-chart-area"><canvas ref={barRef} /></div>
              </article>

              {/* Giao dịch gần đây */}
              <article className="ds-card ds-recent-card">
                <div className="ds-card-head">
                  <div>
                    <h2 className="ds-card-title">Giao dịch gần đây</h2>
                    <p className="ds-card-sub">Khoản thu chi mới nhất được ghi nhận</p>
                  </div>
                  <button
                    type="button"
                    className="ds-card-link-btn"
                    onClick={() => nav("/incomes")}
                  >
                    <span>Xem tất cả</span>
                    <I name="chevron-right" size={13} />
                  </button>
                </div>
                <div className="ds-recent-list">
                  {recentTransactions.map((tx) => (
                    <div
                      key={tx.id}
                      className="ds-tx-row"
                      onClick={() => nav(tx.targetUrl)}
                      role="button"
                      tabIndex={0}
                      title={`Bấm để xem chi tiết: ${tx.desc}`}
                    >
                      <div className={`ds-tx-icon-wrap ${tx.type}`}>
                        <I name={tx.type === "income" ? "trending-up" : "trending-down"} size={14} />
                      </div>
                      <div className="ds-tx-main">
                        <span className="ds-tx-desc">{tx.desc}</span>
                        <div className="ds-tx-meta">
                          <span className="ds-tx-code">{tx.code}</span>
                          <span className="ds-tx-dot">·</span>
                          <span className="ds-tx-cat">{tx.cat}</span>
                        </div>
                      </div>
                      <div className="ds-tx-date">
                        {tx.date.split("-").reverse().slice(0, 2).join("/")}
                      </div>
                      <div className={`ds-tx-amount ${tx.type}`}>
                        {tx.type === "income" ? `+${money(tx.amount, ccy)}` : `-${money(tx.amount, ccy)}`}
                      </div>
                    </div>
                  ))}
                </div>
              </article>
            </div>

            {/* Donut + SKU */}
            <div className="ds-side-col">
              {/* Donut */}
              <article className="ds-card ds-donut-card">
                <div className="ds-card-head">
                  <div>
                    <h2 className="ds-card-title">Theo kênh bán</h2>
                    <p className="ds-card-sub">Tỷ trọng doanh thu</p>
                  </div>
                </div>
                <div className="ds-donut-wrap">
                  <div className="ds-donut-canvas">
                    <canvas ref={donutRef} />
                    <div className="ds-donut-inner">
                      <span className="ds-donut-val">{money(tin, ccy)}</span>
                      <span className="ds-donut-tag">tổng</span>
                    </div>
                  </div>
                  <ul className="ds-channel-list">
                    {channelData.map((c) => (
                      <li key={c.name} className="ds-channel-item">
                        <span className="ds-channel-dot" style={{ background: c.color }} />
                        <span className="ds-channel-name">{c.name}</span>
                        <div className="ds-channel-bar-wrap">
                          <div className="ds-channel-bar" style={{ width: `${c.pct}%`, background: c.color }} />
                        </div>
                        <span className="ds-channel-pct">{c.pct}%</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </article>

              {/* Top SKU */}
              <article className="ds-card ds-sku-card">
                <div className="ds-card-head">
                  <div>
                    <h2 className="ds-card-title">Top sản phẩm</h2>
                    <p className="ds-card-sub">Doanh thu cao nhất kỳ này</p>
                  </div>
                </div>
                <ol className="ds-sku-list">
                  {topSKU.length === 0 && <li className="ds-empty">Chưa có dữ liệu.</li>}
                  {topSKU.map((s) => (
                    <li key={s.rank} className="ds-sku-item">
                      <span className="ds-sku-rank">{s.rank}</span>
                      <div className="ds-sku-detail">
                        <span className="ds-sku-name">{s.name}</span>
                        {s.code !== "—" && <span className="ds-sku-code">{s.code}</span>}
                      </div>
                      {s.qty && <span className="ds-sku-qty">{s.qty} đv</span>}
                      <span className="ds-sku-amt">{money(s.amount, ccy)}</span>
                    </li>
                  ))}
                </ol>
              </article>
            </div>
          </div>
        </>
      )}

      {/* ── By-Channel ── */}
      {tab === "by-channel" && (
        <article className="ds-card">
          <div className="ds-card-head">
            <div>
              <h2 className="ds-card-title">Doanh thu theo kênh bán</h2>
              <p className="ds-card-sub">Kỳ: {rangeLabel}</p>
            </div>
          </div>
          <ul className="ds-channel-detail">
            {channelData.map((c, i) => (
              <li key={c.name} className="ds-channel-detail-row">
                <span className="ds-sku-rank">{i + 1}</span>
                <span className="ds-channel-dot" style={{ background: c.color }} />
                <span className="ds-channel-name-lg">{c.name}</span>
                <div className="ds-channel-bar-wrap ds-channel-bar-lg">
                  <div className="ds-channel-bar" style={{ width: `${c.pct}%`, background: c.color }} />
                </div>
                <span className="ds-channel-pct">{c.pct}%</span>
                <span className="ds-sku-amt">{money(tin * c.pct / 100, ccy)}</span>
              </li>
            ))}
          </ul>
        </article>
      )}

      {/* ── Top SKU ── */}
      {tab === "top-sku" && (
        <article className="ds-card" style={{ padding: 0 }}>
          <div className="ds-card-head" style={{ padding: "16px 20px 12px" }}>
            <div>
              <h2 className="ds-card-title">Top sản phẩm theo doanh thu</h2>
              <p className="ds-card-sub">Kỳ: {rangeLabel}</p>
            </div>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th style={{ width: 40 }}>#</th>
                  <th>Sản phẩm</th>
                  <th>Mã đơn</th>
                  <th className="amount">SL</th>
                  <th className="amount">Doanh thu</th>
                </tr>
              </thead>
              <tbody>
                {topSKU.length === 0 && <tr><td colSpan={5}><div className="empty">Không có dữ liệu trong kỳ.</div></td></tr>}
                {topSKU.map((s) => (
                  <tr key={s.rank}>
                    <td><span className="ds-sku-rank">{s.rank}</span></td>
                    <td><span style={{ fontWeight: 500 }}>{s.name}</span></td>
                    <td><span className="muted">{s.code}</span></td>
                    <td className="amount">{s.qty ?? "—"}</td>
                    <td className="amount"><strong>{money(s.amount, ccy)}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      )}
    </div>
  );
}

export function catLabel(kind, id) {
  return catName(kind === "income" ? INCOME_CATEGORIES : EXPENSE_CATEGORIES, id);
}

export function Kpi({ label, value, delta, up }) {
  const deltaCls = String(delta || "").startsWith("-") || up === false ? "down" : "up";
  return (
    <article className="card kpi">
      <div className="label">{label}</div>
      <div className="value num">{value}</div>
      {delta ? <div className={`chg ${deltaCls}`}>{delta}</div> : null}
    </article>
  );
}
