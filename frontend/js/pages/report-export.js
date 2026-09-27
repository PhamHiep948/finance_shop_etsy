// Xuất báo cáo ra Excel (ExcelJS) và PDF (pdfmake). Thư viện chỉ được tải khi bấm nút xuất.
// `r` là dữ liệu báo cáo do ReportsPage tính: { from, to, ccy, filterLabel, inc, exp, tin, tex, profit, ... }
const EXPORT_LIBS = {
  excel: ["https://cdn.jsdelivr.net/npm/exceljs@4.4.0/dist/exceljs.min.js"],
  pdf: [
    "https://cdn.jsdelivr.net/npm/pdfmake@0.2.20/build/pdfmake.min.js",
    "https://cdn.jsdelivr.net/npm/pdfmake@0.2.20/build/vfs_fonts.js",
  ],
};
const REPORT_PALETTE = ["#4F46E5", "#059669", "#F59E0B", "#DC2626", "#0EA5E9", "#8B5CF6", "#EC4899", "#14B8A6", "#F97316", "#64748B"];
const RC = {
  ink: "#1E1B4B", text: "#334155", muted: "#64748B", line: "#E2E8F0", soft: "#F8FAFC",
  primary: "#4F46E5", primarySoft: "#EEF2FF", income: "#2563EB", expense: "#DC2626", profit: "#059669",
};

const loadedScripts = {};
function loadScript(src) {
  loadedScripts[src] = loadedScripts[src] || new Promise((resolve, reject) => {
    const el = document.createElement("script");
    el.src = src;
    el.onload = resolve;
    el.onerror = () => {
      delete loadedScripts[src];
      el.remove();
      reject(new Error("Không tải được thư viện xuất file. Hãy kiểm tra kết nối mạng."));
    };
    document.head.appendChild(el);
  });
  return loadedScripts[src];
}
async function loadLibs(list) {
  for (const src of list) await loadScript(src); // tuần tự: vfs_fonts cần pdfmake tải trước
}

// ---------------------------------------------------------------- Tính toán dùng chung

const periodLabel = (r) => (r.from || r.to ? `${dmy(r.from) || "…"} – ${dmy(r.to) || "…"}` : "Tất cả kỳ");
const reportFileName = (r, ext) => `bao-cao-tai-chinh_${r.from || "tat-ca"}_${r.to || "tat-ca"}.${ext}`;
const nowLabel = () => new Date().toLocaleString("vi-VN", { hour: "2-digit", minute: "2-digit", day: "2-digit", month: "2-digit", year: "numeric" });
const pctOf = (part, total) => (total ? (part / total) * 100 : 0);
const fmtPct = (n) => `${n.toFixed(1).replace(".", ",")}%`;
const netOf = (x) => Number(x.amountAfterTax ?? x.amount) || 0;

/** Gom theo loại: tên, số khoản, số tiền, tỷ trọng (%), sắp giảm dần. */
function groupCategories(list, cats) {
  const map = {};
  list.forEach((x) => {
    const name = catName(cats, x.categoryId);
    map[name] = map[name] || { name, count: 0, amount: 0 };
    map[name].count += 1;
    map[name].amount += Number(x.amount) || 0;
  });
  const total = Object.values(map).reduce((a, g) => a + g.amount, 0);
  return Object.values(map)
    .map((g) => ({ ...g, pct: pctOf(g.amount, total) }))
    .sort((a, b) => b.amount - a.amount);
}

/** Top sản phẩm theo doanh thu (gộp theo tên sản phẩm). */
function topProducts(inc, limit = 10) {
  const map = {};
  // Chỉ tính khoản thu bán hàng (có mã đơn hoặc số lượng), bỏ các khoản thu khác như workshop, hoàn phí.
  inc.filter((x) => x.orderCode || x.productQty).forEach((x) => {
    const key = x.description;
    map[key] = map[key] || { name: key, orders: 0, qty: 0, amount: 0 };
    map[key].orders += 1;
    map[key].qty += Number(x.productQty) || 0;
    map[key].amount += Number(x.amount) || 0;
  });
  return Object.values(map).sort((a, b) => b.amount - a.amount).slice(0, limit);
}

/** Tất cả giao dịch trong kỳ, sắp theo ngày. */
function allTransactions(r) {
  return [
    ...r.inc.map((x) => ({ type: "Thu", date: x.incomeDate, desc: x.description, cat: catName(INCOME_CATEGORIES, x.categoryId), party: x.orderCode || "", ...pick3(x) })),
    ...r.exp.map((x) => ({ type: "Chi", date: x.expenseDate, desc: x.description, cat: catName(EXPENSE_CATEGORIES, x.categoryId), party: x.recipient || "", ...pick3(x) })),
  ].sort((a, b) => a.date.localeCompare(b.date) || a.type.localeCompare(b.type));
}
function pick3(x) {
  const amount = Number(x.amount) || 0;
  const after = netOf(x);
  return { amount, taxPercent: Number(x.taxPercent) || 0, tax: Math.round((after - amount) * 100) / 100, after };
}

/** Một vài nhận xét tự động cho phần "Điểm nổi bật". */
function reportInsights(r, incCats, expCats) {
  const out = [];
  const margin = pctOf(r.profit, r.tin);
  out.push(r.profit >= 0
    ? `Kỳ này có lãi ${money(r.profit, r.ccy)}, biên lợi nhuận ${fmtPct(margin)} trên doanh thu.`
    : `Kỳ này lỗ ${money(-r.profit, r.ccy)}: chi phí vượt doanh thu ${fmtPct(pctOf(-r.profit, r.tin || 1))}.`);
  if (r.monthly.length > 1) {
    const best = [...r.monthly].sort((a, b) => b.income - a.income)[0];
    out.push(`Tháng có doanh thu cao nhất: ${best.m}/${best.key.slice(0, 4)} với ${money(best.income, r.ccy)}.`);
  }
  if (incCats[0]) out.push(`Nguồn thu chính: "${incCats[0].name}" chiếm ${fmtPct(incCats[0].pct)} doanh thu (${incCats[0].count} khoản).`);
  if (expCats[0]) out.push(`Khoản chi lớn nhất: "${expCats[0].name}" chiếm ${fmtPct(expCats[0].pct)} tổng chi phí.`);
  return out;
}

function downloadBlob(blob, name) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// ---------------------------------------------------------------- Excel

async function exportReportExcel(r) {
  await loadLibs(EXPORT_LIBS.excel);
  const wb = new ExcelJS.Workbook();
  wb.creator = "Mina Store";
  wb.created = new Date();

  const argb = (hex) => "FF" + hex.replace("#", "").toUpperCase();
  const fill = (hex) => ({ type: "pattern", pattern: "solid", fgColor: { argb: argb(hex) } });
  const thin = { style: "thin", color: { argb: argb(RC.line) } };
  const border = { top: thin, left: thin, bottom: thin, right: thin };
  const moneyFmt = r.ccy === "EUR" ? '#,##0.00 "€";[Red]-#,##0.00 "€"' : '"$"#,##0.00;[Red]-"$"#,##0.00';
  const pctFmt = "0.0%";
  const dateFmt = "dd/mm/yyyy";
  const v = (n) => round2(toDisplay(n, r.ccy));
  const asDate = (iso) => {
    const [y, m, d] = String(iso).split("-").map(Number);
    return new Date(Date.UTC(y, m - 1, d));
  };
  const col = (n) => String.fromCharCode(64 + n);

  /** Tiêu đề 3 dòng đầu mỗi sheet. */
  function sheetTitle(ws, title, width) {
    ws.mergeCells(1, 1, 1, width);
    const t = ws.getCell(1, 1);
    t.value = `MINA STORE — ${title}`;
    t.font = { bold: true, size: 14, color: { argb: "FFFFFFFF" } };
    t.fill = fill(RC.primary);
    t.alignment = { vertical: "middle", indent: 1 };
    ws.getRow(1).height = 30;
    ws.mergeCells(2, 1, 2, width);
    const s = ws.getCell(2, 1);
    s.value = `Kỳ báo cáo: ${periodLabel(r)}  ·  Tiền tệ: ${r.ccy}  ·  ${r.filterLabel}  ·  Xuất lúc ${nowLabel()}`;
    s.font = { italic: true, size: 9, color: { argb: argb(RC.muted) } };
    s.alignment = { indent: 1 };
  }

  /**
   * Vẽ một bảng bắt đầu từ `startRow`. `columns`: [{ header, width, fmt, align }].
   * `rows`: mảng giá trị. `totals`: dòng tổng (giá trị hoặc { formula }). Trả về dòng kế tiếp còn trống.
   */
  function table(ws, startRow, columns, rows, totals, caption) {
    let rowNo = startRow;
    if (caption) {
      const c = ws.getCell(rowNo, 1);
      c.value = caption;
      c.font = { bold: true, size: 11, color: { argb: argb(RC.ink) } };
      rowNo += 1;
    }
    const head = ws.getRow(rowNo);
    columns.forEach((c, i) => {
      const cell = head.getCell(i + 1);
      cell.value = c.header;
      cell.font = { bold: true, color: { argb: "FFFFFFFF" } };
      cell.fill = fill(RC.ink);
      cell.border = border;
      cell.alignment = { vertical: "middle", horizontal: c.align || (c.fmt ? "right" : "left"), wrapText: true };
    });
    head.height = 22;
    const headerRow = rowNo;
    rows.forEach((values, idx) => {
      rowNo += 1;
      const row = ws.getRow(rowNo);
      values.forEach((val, i) => {
        const cell = row.getCell(i + 1);
        cell.value = val;
        cell.border = border;
        if (columns[i].fmt) cell.numFmt = columns[i].fmt;
        cell.alignment = { vertical: "middle", horizontal: columns[i].align || (columns[i].fmt ? "right" : "left") };
        if (idx % 2 === 1) cell.fill = fill(RC.soft);
      });
    });
    if (totals) {
      rowNo += 1;
      const row = ws.getRow(rowNo);
      totals.forEach((val, i) => {
        const cell = row.getCell(i + 1);
        cell.value = typeof val === "function" ? val(headerRow + 1, rowNo - 1) : val;
        cell.font = { bold: true, color: { argb: argb(RC.ink) } };
        cell.fill = fill(RC.primarySoft);
        cell.border = { ...border, top: { style: "medium", color: { argb: argb(RC.primary) } } };
        if (columns[i].fmt) cell.numFmt = columns[i].fmt;
        cell.alignment = { horizontal: columns[i].align || (columns[i].fmt ? "right" : "left") };
      });
    }
    return { next: rowNo + 2, headerRow, lastRow: rowNo };
  }
  const sumOf = (c) => (first, last) => ({ formula: `SUM(${c}${first}:${c}${last})` });
  const setWidths = (ws, widths) => widths.forEach((w, i) => { ws.getColumn(i + 1).width = w; });

  const incCats = groupCategories(r.inc, INCOME_CATEGORIES);
  const expCats = groupCategories(r.exp, EXPENSE_CATEGORIES);

  // 1. Tổng quan
  const ov = wb.addWorksheet("Tổng quan", { views: [{ showGridLines: false }] });
  setWidths(ov, [34, 20, 20, 20, 14]);
  sheetTitle(ov, "BÁO CÁO TÀI CHÍNH", 5);
  let next = table(ov, 4, [{ header: "Chỉ tiêu" }, { header: "Giá trị", fmt: moneyFmt }], [
    ["Tổng doanh thu (trước thuế)", v(r.tin)],
    ["Tổng chi phí (trước thuế)", v(r.tex)],
    ["Lợi nhuận ròng", v(r.profit)],
    ["Doanh thu TB / khoản thu", v(r.averageIncome)],
    ["Thuế đầu ra ước tính", v(r.incomeTax)],
  ], null, "Chỉ tiêu chính").next;
  const cnt = ov.getRow(next - 1);
  cnt.getCell(1).value = `Số giao dịch: ${r.inc.length + r.exp.length} (${r.inc.length} khoản thu, ${r.exp.length} khoản chi)`;
  cnt.getCell(1).font = { italic: true, color: { argb: argb(RC.muted) } };
  // Lợi nhuận và biên LN dùng công thức để sửa số trong Excel vẫn tự tính lại.
  const monthRows = r.monthly.map((x) => [`Tháng ${Number(x.key.slice(5))}/${x.key.slice(0, 4)}`, v(x.income), v(x.expense), null, null]);
  const mt = table(ov, next + 1, [
    { header: "Tháng" }, { header: "Doanh thu", fmt: moneyFmt }, { header: "Chi phí", fmt: moneyFmt },
    { header: "Lợi nhuận", fmt: moneyFmt }, { header: "Biên LN", fmt: pctFmt },
  ], monthRows, ["Tổng cộng", sumOf("B"), sumOf("C"), sumOf("D"),
    (first, last) => ({ formula: `IF(B${last + 1}=0,0,D${last + 1}/B${last + 1})` })], "Tổng hợp theo tháng");
  for (let i = mt.headerRow + 1; i < mt.lastRow; i++) {
    ov.getCell(`D${i}`).value = { formula: `B${i}-C${i}` };
    ov.getCell(`E${i}`).value = { formula: `IF(B${i}=0,0,D${i}/B${i})` };
  }

  // 2. Theo ngày
  const dailySheet = wb.addWorksheet("Theo ngày", { views: [{ state: "frozen", ySplit: 4, showGridLines: false }] });
  setWidths(dailySheet, [16, 18, 18, 18]);
  sheetTitle(dailySheet, "THU CHI THEO NGÀY", 4);
  const dt = table(dailySheet, 4, [
    { header: "Ngày", fmt: dateFmt, align: "left" }, { header: "Thu", fmt: moneyFmt }, { header: "Chi", fmt: moneyFmt }, { header: "Chênh lệch", fmt: moneyFmt },
  ], r.daily.map((d) => [asDate(d.d), v(d.income), v(d.expense), null]), ["Tổng cộng", sumOf("B"), sumOf("C"), sumOf("D")]);
  for (let i = dt.headerRow + 1; i < dt.lastRow; i++) dailySheet.getCell(`D${i}`).value = { formula: `B${i}-C${i}` };
  dailySheet.autoFilter = { from: "A4", to: `D${dt.lastRow - 1}` };

  // 3. Theo loại
  const catSheet = wb.addWorksheet("Theo loại", { views: [{ showGridLines: false }] });
  setWidths(catSheet, [32, 12, 20, 14]);
  sheetTitle(catSheet, "CƠ CẤU THEO LOẠI", 4);
  const catCols = [{ header: "Loại" }, { header: "Số khoản", fmt: "0", align: "center" }, { header: "Số tiền", fmt: moneyFmt }, { header: "Tỷ trọng", fmt: pctFmt }];
  const catRows = (list) => list.map((c) => [c.name, c.count, v(c.amount), c.pct / 100]);
  next = table(catSheet, 4, catCols, catRows(incCats), ["Tổng doanh thu", sumOf("B"), sumOf("C"), incCats.length ? 1 : 0], "Doanh thu theo loại thu").next;
  table(catSheet, next, catCols, catRows(expCats), ["Tổng chi phí", sumOf("B"), sumOf("C"), expCats.length ? 1 : 0], "Chi phí theo loại chi");

  // 4. Top sản phẩm
  const topSheet = wb.addWorksheet("Top sản phẩm", { views: [{ showGridLines: false }] });
  setWidths(topSheet, [6, 36, 10, 10, 18, 12]);
  sheetTitle(topSheet, "TOP SẢN PHẨM BÁN CHẠY", 6);
  table(topSheet, 4, [
    { header: "#", align: "center" }, { header: "Sản phẩm" }, { header: "Số đơn", fmt: "0", align: "center" },
    { header: "SL", fmt: "0", align: "center" }, { header: "Doanh thu", fmt: moneyFmt }, { header: "Tỷ trọng", fmt: pctFmt },
  ], topProducts(r.inc).map((p, i) => [i + 1, p.name, p.orders, p.qty, v(p.amount), pctOf(p.amount, r.tin) / 100]));

  // 5. Khoản thu chi tiết
  const incSheet = wb.addWorksheet("Khoản thu", { views: [{ state: "frozen", ySplit: 4, showGridLines: false }] });
  const incCols = [
    { header: "Ngày", fmt: dateFmt, align: "left", width: 12 }, { header: "Mã đơn", width: 14 }, { header: "Sản phẩm / nội dung", width: 34 },
    { header: "Loại thu", width: 16 }, { header: "Kênh bán", width: 16 }, { header: "Khu vực", width: 12 },
    { header: "SL", fmt: "0", align: "center", width: 6 }, { header: "Đơn giá", fmt: moneyFmt, width: 12 },
    { header: "Tiền hàng", fmt: moneyFmt, width: 13 }, { header: "Giảm giá", fmt: moneyFmt, width: 12 },
    { header: "Vận chuyển", fmt: moneyFmt, width: 12 }, { header: "Trước thuế", fmt: moneyFmt, width: 14 },
    { header: "% thuế", fmt: '0"%"', align: "center", width: 8 }, { header: "Thuế", fmt: moneyFmt, width: 12 },
    { header: "Sau thuế", fmt: moneyFmt, width: 14 }, { header: "Ghi chú", width: 28 },
  ];
  setWidths(incSheet, incCols.map((c) => c.width));
  sheetTitle(incSheet, "DANH SÁCH KHOẢN THU", incCols.length);
  const incRows = [...r.inc].sort((a, b) => a.incomeDate.localeCompare(b.incomeDate)).map((x) => {
    const t = pick3(x);
    return [
      asDate(x.incomeDate), x.orderCode || "", x.description, catName(INCOME_CATEGORIES, x.categoryId),
      salesChannelLabel(x.salesChannel), saleRegionLabel(x.saleRegion), Number(x.productQty) || null,
      x.unitPrice != null && x.unitPrice !== "" ? v(x.unitPrice) : null, x.itemTotal != null && x.itemTotal !== "" ? v(x.itemTotal) : null,
      v(Number(x.discountAmount) || 0), v(Number(x.shippingAmount) || 0), v(t.amount), t.taxPercent, v(t.tax), v(t.after), x.note || "",
    ];
  });
  const it = table(incSheet, 4, incCols, incRows,
    ["Tổng cộng", "", `${incRows.length} khoản`, "", "", "", sumOf("G"), "", sumOf("I"), sumOf("J"), sumOf("K"), sumOf("L"), "", sumOf("N"), sumOf("O"), ""]);
  incSheet.autoFilter = { from: "A4", to: `${col(incCols.length)}${it.lastRow - 1}` };

  // 6. Khoản chi chi tiết
  const expSheet = wb.addWorksheet("Khoản chi", { views: [{ state: "frozen", ySplit: 4, showGridLines: false }] });
  const expCols = [
    { header: "Ngày", fmt: dateFmt, align: "left", width: 12 }, { header: "Nội dung", width: 34 }, { header: "Loại chi", width: 22 },
    { header: "Bên nhận", width: 22 }, { header: "Phạm vi", width: 11 }, { header: "Phương thức", width: 14 },
    { header: "Trước thuế", fmt: moneyFmt, width: 14 }, { header: "% thuế", fmt: '0"%"', align: "center", width: 8 },
    { header: "Thuế", fmt: moneyFmt, width: 12 }, { header: "Sau thuế", fmt: moneyFmt, width: 14 }, { header: "Ghi chú", width: 28 },
  ];
  setWidths(expSheet, expCols.map((c) => c.width));
  sheetTitle(expSheet, "DANH SÁCH KHOẢN CHI", expCols.length);
  const expRows = [...r.exp].sort((a, b) => a.expenseDate.localeCompare(b.expenseDate)).map((x) => {
    const t = pick3(x);
    return [
      asDate(x.expenseDate), x.description, catName(EXPENSE_CATEGORIES, x.categoryId), x.recipient || "",
      originScopeLabel(x.originScope), paymentMethodLabel(x.paymentMethod), v(t.amount), t.taxPercent, v(t.tax), v(t.after), x.note || "",
    ];
  });
  const et = table(expSheet, 4, expCols, expRows,
    ["Tổng cộng", `${expRows.length} khoản`, "", "", "", "", sumOf("G"), "", sumOf("I"), sumOf("J"), ""]);
  expSheet.autoFilter = { from: "A4", to: `${col(expCols.length)}${et.lastRow - 1}` };

  const buffer = await wb.xlsx.writeBuffer();
  downloadBlob(new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }), reportFileName(r, "xlsx"));
}

// ---------------------------------------------------------------- PDF

/** Vẽ biểu đồ Chart.js ra ảnh PNG (không hiển thị trên trang). */
function chartImage(config, width, height) {
  const holder = document.createElement("div");
  holder.style.cssText = `position:fixed;left:-10000px;top:0;width:${width}px;height:${height}px;`;
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  holder.appendChild(canvas);
  document.body.appendChild(holder);
  const chart = new Chart(canvas, {
    ...config,
    options: { ...config.options, responsive: false, animation: false, devicePixelRatio: 3 },
  });
  const url = canvas.toDataURL("image/png");
  chart.destroy();
  holder.remove();
  return url;
}

async function imageDataUrl(src) {
  try {
    const blob = await (await fetch(src)).blob();
    return await new Promise((resolve) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => resolve(null);
      reader.readAsDataURL(blob);
    });
  } catch {
    return null;
  }
}

async function exportReportPdf(r) {
  await loadLibs(EXPORT_LIBS.pdf);
  if (typeof Chart === "undefined") throw new Error("Chưa tải được thư viện biểu đồ.");
  const m = (n) => money(n, r.ccy);
  const conv = (n) => toDisplay(n, r.ccy);
  const logo = await imageDataUrl("assets/mina-store-logo.jpg");
  const incCats = groupCategories(r.inc, INCOME_CATEGORIES);
  const expCats = groupCategories(r.exp, EXPENSE_CATEGORIES);
  const products = topProducts(r.inc);
  const period = periodLabel(r);
  const exportedAt = nowLabel();
  const W = 515; // bề rộng vùng nội dung A4 (595 - 2 × 40)

  // ---- Ảnh biểu đồ
  const tick = { font: { size: 11 }, color: "#64748B" };
  const trendImg = chartImage({
    type: "bar",
    data: {
      labels: r.monthly.length ? r.monthly.map((x) => `${x.m}/${x.key.slice(2, 4)}`) : ["—"],
      datasets: [
        { type: "line", label: "Lợi nhuận", data: r.monthly.map((x) => conv(x.income - x.expense)), borderColor: RC.profit, backgroundColor: RC.profit, borderWidth: 2.5, pointRadius: 3.5, tension: 0.3, order: 0 },
        { label: "Doanh thu", data: r.monthly.map((x) => conv(x.income)), backgroundColor: RC.income, borderRadius: 4, barPercentage: 0.7, categoryPercentage: 0.6, order: 1 },
        { label: "Chi phí", data: r.monthly.map((x) => conv(x.expense)), backgroundColor: "#F87171", borderRadius: 4, barPercentage: 0.7, categoryPercentage: 0.6, order: 1 },
      ],
    },
    options: {
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, grid: { color: "#EEF1F5" }, border: { display: false }, ticks: { ...tick, callback: (val) => (Math.abs(val) >= 1000 ? `${(val / 1000).toFixed(1)}k` : val) } },
        x: { grid: { display: false }, border: { display: false }, ticks: tick },
      },
    },
  }, 1030, 430);
  // Biểu đồ tròn: tối đa 6 phần, các loại nhỏ gộp thành "Khác".
  const compact = (cats) => (cats.length > 6
    ? [...cats.slice(0, 5), { name: `Khác (${cats.length - 5} loại)`, amount: cats.slice(5).reduce((a, c) => a + c.amount, 0), other: true }]
    : cats);
  const sliceColor = (c, i) => (c.other ? "#CBD5E1" : REPORT_PALETTE[i % REPORT_PALETTE.length]);
  const donut = (cats) => (cats.length ? chartImage({
    type: "doughnut",
    data: { labels: cats.map((c) => c.name), datasets: [{ data: cats.map((c) => c.amount), backgroundColor: cats.map(sliceColor), borderColor: "#fff", borderWidth: 3 }] },
    options: { plugins: { legend: { display: false } }, cutout: "64%" },
  }, 260, 260) : null);
  const incDonut = donut(compact(incCats));
  const expDonut = donut(compact(expCats));

  // ---- Các khối dựng sẵn
  const swatch = (color, size = 7) => ({ canvas: [{ type: "rect", x: 0, y: 1.5, w: size, h: size, r: 1.5, color }], width: size + 5 });
  const bar = (pct, color, w = 90) => ({
    canvas: [
      { type: "rect", x: 0, y: 3, w, h: 5, r: 2.5, color: "#E8ECF3" },
      { type: "rect", x: 0, y: 3, w: Math.max(2, (w * Math.min(pct, 100)) / 100), h: 5, r: 2.5, color },
    ],
  });
  const th = (text, alignment = "left") => ({ text, bold: true, color: "#FFFFFF", fontSize: 8, alignment });
  const num = (text, extra = {}) => ({ text, alignment: "right", noWrap: true, ...extra });
  const tableLayout = (hasTotal = false) => ({
    hLineWidth: (i, node) => (i === 0 || i === 1 || i === node.table.body.length ? 0 : hasTotal && i === node.table.body.length - 1 ? 1 : 0.5),
    vLineWidth: () => 0,
    hLineColor: (i, node) => (hasTotal && i === node.table.body.length - 1 ? RC.primary : RC.line),
    fillColor: (row, node) => (row === 0 ? RC.ink : hasTotal && row === node.table.body.length - 1 ? RC.primarySoft : row % 2 === 0 ? RC.soft : null),
    paddingLeft: () => 7, paddingRight: () => 7, paddingTop: () => 5, paddingBottom: () => 5,
  });
  const section = (title, sub, extra = {}) => ({
    margin: [0, 18, 0, 8],
    ...extra,
    stack: [
      { columns: [{ canvas: [{ type: "rect", x: 0, y: 1, w: 3.5, h: 13, r: 1.5, color: RC.primary }], width: 10 }, { text: title, fontSize: 12.5, bold: true, color: RC.ink }] },
      sub ? { text: sub, fontSize: 8.5, color: RC.muted, margin: [10, 2, 0, 0] } : "",
    ],
  });
  const kpi = (label, value, accent, sub) => ({
    table: {
      widths: ["*"],
      body: [[{
        stack: [
          { text: label.toUpperCase(), fontSize: 6.8, bold: true, color: RC.muted, characterSpacing: 0.4 },
          { text: value, fontSize: 15.5, bold: true, color: accent, margin: [0, 5, 0, 3] },
          { text: sub, fontSize: 7.5, color: RC.muted },
        ],
      }]],
    },
    layout: {
      hLineWidth: () => 0, vLineWidth: (i) => (i === 0 ? 3 : 0), vLineColor: () => accent,
      fillColor: () => RC.soft, paddingLeft: () => 11, paddingRight: () => 8, paddingTop: () => 9, paddingBottom: () => 9,
    },
  });
  /** Chú thích biểu đồ tròn (cùng cách gộp "Khác" với biểu đồ). */
  const catLegend = (cats, total) => {
    const rows = compact(cats);
    return {
      table: {
        widths: [10, "*", 36],
        body: rows.map((c, i) => [
          swatch(sliceColor(c, i)),
          { text: c.name, fontSize: 8 },
          num(fmtPct(pctOf(c.amount, total)), { fontSize: 8, bold: true }),
        ]),
      },
      layout: { hLineWidth: () => 0, vLineWidth: () => 0, paddingLeft: () => 0, paddingRight: () => 2, paddingTop: () => 2.5, paddingBottom: () => 2.5 },
    };
  };
  const donutBlock = (title, img, cats, total, emptyText) => ({
    width: "*",
    table: {
      widths: ["*"],
      body: [[{
        stack: [
          { text: title, bold: true, fontSize: 10, color: RC.ink, margin: [0, 0, 0, 8] },
          img
            ? {
                columns: [
                  { width: 84, stack: [{ image: img, width: 84, height: 84 }] },
                  { width: "*", stack: [catLegend(cats, total)], margin: [10, 2, 0, 0] },
                ],
              }
            : { text: emptyText, color: RC.muted, fontSize: 9, italics: true },
        ],
      }]],
    },
    layout: {
      hLineWidth: () => 1, vLineWidth: () => 1, hLineColor: () => RC.line, vLineColor: () => RC.line,
      paddingLeft: () => 12, paddingRight: () => 12, paddingTop: () => 10, paddingBottom: () => 10,
    },
  });
  const legendRow = (items) => ({
    table: { widths: items.flatMap(() => [8, "auto"]), body: [items.flatMap(([color, label]) => [swatch(color), { text: label, fontSize: 8, color: RC.muted, margin: [0, 0, 12, 0] }])] },
    layout: { hLineWidth: () => 0, vLineWidth: () => 0, paddingLeft: () => 0, paddingRight: () => 2, paddingTop: () => 0, paddingBottom: () => 0 },
    margin: [10, 0, 0, 6],
  });
  const catTable = (cats, total, color, label) => ({
    table: {
      headerRows: 1,
      widths: ["*", 44, 100, 42, 76],
      body: [
        [th(label), th("Số khoản", "center"), th("Tỷ trọng"), th("%", "right"), th("Số tiền", "right")],
        ...cats.map((c, i) => [
          { columns: [swatch(REPORT_PALETTE[i % REPORT_PALETTE.length]), { text: c.name }] },
          { text: String(c.count), alignment: "center" },
          bar(c.pct, color),
          num(fmtPct(c.pct), { color: RC.muted }),
          num(m(c.amount), { bold: true }),
        ]),
        [{ text: "Tổng cộng", bold: true }, { text: String(cats.reduce((a, c) => a + c.count, 0)), alignment: "center", bold: true }, "", num("100%", { bold: true }), num(m(total), { bold: true })],
      ],
    },
    layout: tableLayout(true),
  });

  const margin = pctOf(r.profit, r.tin);
  const insights = reportInsights(r, incCats, expCats);
  const tx = allTransactions(r);

  const content = [
    // Tiêu đề
    {
      columns: [
        {
          stack: [
            { text: "Báo cáo tài chính", fontSize: 24, bold: true, color: RC.ink },
            { text: `Kỳ báo cáo ${period}`, fontSize: 10.5, color: RC.text, margin: [0, 3, 0, 0] },
          ],
        },
        {
          width: 170,
          table: {
            widths: ["*"],
            body: [[{
              stack: [
                { text: [{ text: "Tiền tệ: ", color: RC.muted }, { text: r.ccy, bold: true }], fontSize: 8.5 },
                { text: [{ text: "Phạm vi: ", color: RC.muted }, { text: r.filterLabel, bold: true }], fontSize: 8.5, margin: [0, 2, 0, 0] },
                { text: [{ text: "Giao dịch: ", color: RC.muted }, { text: `${r.inc.length} thu · ${r.exp.length} chi`, bold: true }], fontSize: 8.5, margin: [0, 2, 0, 0] },
              ],
            }]],
          },
          layout: { hLineWidth: () => 0, vLineWidth: () => 0, fillColor: () => RC.primarySoft, paddingLeft: () => 10, paddingRight: () => 10, paddingTop: () => 8, paddingBottom: () => 8 },
        },
      ],
      margin: [0, 0, 0, 16],
    },
    // KPI
    {
      columns: [
        kpi("Tổng doanh thu", m(r.tin), RC.income, `${r.inc.length} khoản thu · trước thuế`),
        kpi("Tổng chi phí", m(r.tex), RC.expense, `${r.exp.length} khoản chi · trước thuế`),
        kpi("Lợi nhuận ròng", m(r.profit), r.profit >= 0 ? RC.profit : RC.expense, `Biên lợi nhuận ${fmtPct(margin)}`),
      ],
      columnGap: 10,
    },
    {
      columns: [
        kpi("Số giao dịch", String(r.inc.length + r.exp.length), RC.ink, "Thu và chi trong kỳ"),
        kpi("Doanh thu TB / khoản", m(r.averageIncome), RC.primary, "Giá trị trung bình mỗi khoản thu"),
        kpi("Thuế đầu ra ước tính", m(r.incomeTax), "#B45309", "Tổng thuế trên các khoản thu"),
      ],
      columnGap: 10,
      margin: [0, 10, 0, 0],
    },
    // Điểm nổi bật
    {
      margin: [0, 16, 0, 0],
      table: {
        widths: ["*"],
        body: [[{
          stack: [
            { text: "Điểm nổi bật", bold: true, color: RC.primary, fontSize: 10, margin: [0, 0, 0, 5] },
            { ul: insights.map((t) => ({ text: t, margin: [0, 1.5, 0, 1.5] })), fontSize: 9, color: RC.text, markerColor: RC.primary },
          ],
        }]],
      },
      layout: {
        hLineWidth: () => 1, vLineWidth: () => 1, hLineColor: () => "#C7D2FE", vLineColor: () => "#C7D2FE",
        fillColor: () => "#FAFAFF", paddingLeft: () => 12, paddingRight: () => 12, paddingTop: () => 10, paddingBottom: () => 10,
      },
    },
    // Xu hướng
    section("Xu hướng thu chi theo tháng", `Doanh thu, chi phí (cột) và lợi nhuận (đường) · ${r.ccy}`),
    legendRow([[RC.income, "Doanh thu"], ["#F87171", "Chi phí"], [RC.profit, "Lợi nhuận"]]),
    { image: trendImg, width: W, height: W * 430 / 1030 },
    // Cơ cấu (giữ nguyên khối, không bị tách trang)
    {
      unbreakable: true,
      stack: [
        section("Cơ cấu doanh thu & chi phí", "Tỷ trọng theo loại thu / loại chi"),
        {
          columns: [
            donutBlock("Doanh thu theo loại", incDonut, incCats, r.tin, "Không có khoản thu trong kỳ."),
            donutBlock("Chi phí theo loại", expDonut, expCats, r.tex, "Không có khoản chi trong kỳ."),
          ],
          columnGap: 12,
        },
      ],
    },
    // Tổng hợp tháng
    section("Tổng hợp theo tháng"),
    {
      table: {
        headerRows: 1,
        widths: ["*", 90, 90, 90, 60],
        body: [
          [th("Tháng"), th("Doanh thu", "right"), th("Chi phí", "right"), th("Lợi nhuận", "right"), th("Biên LN", "right")],
          ...r.monthly.map((x) => {
            const p = x.income - x.expense;
            return [
              { text: `Tháng ${Number(x.key.slice(5))}/${x.key.slice(0, 4)}` },
              num(m(x.income), { color: RC.income }),
              num(m(x.expense), { color: RC.expense }),
              num(m(p), { bold: true, color: p >= 0 ? RC.profit : RC.expense }),
              num(fmtPct(pctOf(p, x.income)), { color: RC.muted }),
            ];
          }),
          [{ text: "Tổng cộng", bold: true }, num(m(r.tin), { bold: true }), num(m(r.tex), { bold: true }),
            num(m(r.profit), { bold: true, color: r.profit >= 0 ? RC.profit : RC.expense }), num(fmtPct(margin), { bold: true })],
        ],
      },
      layout: tableLayout(true),
    },
    section("Doanh thu theo loại thu"),
    incCats.length ? catTable(incCats, r.tin, RC.income, "Loại thu") : { text: "Không có dữ liệu.", color: RC.muted, italics: true },
    section("Chi phí theo loại chi"),
    expCats.length ? catTable(expCats, r.tex, RC.expense, "Loại chi") : { text: "Không có dữ liệu.", color: RC.muted, italics: true },
    section("Top sản phẩm bán chạy", "Xếp theo doanh thu trước thuế, gộp theo tên sản phẩm"),
    products.length ? {
      table: {
        headerRows: 1,
        widths: [22, "*", 44, 36, 80, 50],
        body: [
          [th("#", "center"), th("Sản phẩm"), th("Số đơn", "center"), th("SL", "center"), th("Doanh thu", "right"), th("Tỷ trọng", "right")],
          ...products.map((p, i) => [
            { text: String(i + 1), alignment: "center", bold: true, color: i < 3 ? RC.primary : RC.muted },
            { text: p.name, bold: i < 3 },
            { text: String(p.orders), alignment: "center" },
            { text: String(p.qty || "—"), alignment: "center" },
            num(m(p.amount), { bold: true }),
            num(fmtPct(pctOf(p.amount, r.tin)), { color: RC.muted }),
          ]),
        ],
      },
      layout: tableLayout(false),
    } : { text: "Không có dữ liệu.", color: RC.muted, italics: true },
    // Phụ lục
    section("Phụ lục · Danh sách giao dịch", `${tx.length} giao dịch trong kỳ, sắp theo ngày`, { pageBreak: "before" }),
    {
      fontSize: 7.6,
      table: {
        headerRows: 1,
        widths: [44, 24, "*", 78, 54, 26, 58],
        body: [
          [th("Ngày"), th("Loại", "center"), th("Nội dung"), th("Danh mục"), th("Trước thuế", "right"), th("Thuế", "right"), th("Sau thuế", "right")],
          ...tx.map((t) => [
            { text: dmy(t.date), noWrap: true },
            { text: t.type, alignment: "center", bold: true, color: t.type === "Thu" ? RC.income : RC.expense },
            { text: t.party ? [t.desc, { text: `  ${t.party}`, color: RC.muted }] : t.desc },
            { text: t.cat, color: RC.text },
            num(m(t.amount)),
            num(`${t.taxPercent}%`, { color: RC.muted }),
            num(`${t.type === "Chi" ? "−" : "+"}${m(t.after)}`, { bold: true, color: t.type === "Thu" ? RC.profit : RC.expense }),
          ]),
        ],
      },
      layout: tableLayout(false),
    },
  ];

  const doc = {
    pageSize: "A4",
    pageMargins: [40, 76, 40, 50],
    info: { title: `Báo cáo tài chính ${period} — Mina Store`, author: "Mina Store", subject: "Báo cáo tài chính" },
    defaultStyle: { font: "Roboto", fontSize: 9, color: RC.text, lineHeight: 1.15 },
    header: () => ({
      margin: [40, 22, 40, 0],
      stack: [
        {
          columns: [
            logo ? { image: logo, width: 30, height: 30 } : { text: "", width: 0 },
            {
              stack: [
                { text: [{ text: "Mina", color: RC.ink }, { text: " Store", color: "#C0616B" }], bold: true, fontSize: 13 },
                { text: "Quản lý tài chính", fontSize: 7.5, color: RC.muted },
              ],
              margin: [logo ? 8 : 0, 2, 0, 0],
            },
            {
              stack: [
                { text: "BÁO CÁO TÀI CHÍNH", bold: true, fontSize: 8.5, color: RC.primary, characterSpacing: 1, alignment: "right" },
                { text: period, fontSize: 8, color: RC.muted, alignment: "right", margin: [0, 2, 0, 0] },
              ],
              margin: [0, 3, 0, 0],
            },
          ],
        },
        { canvas: [{ type: "line", x1: 0, y1: 8, x2: W, y2: 8, lineWidth: 0.8, lineColor: RC.line }] },
      ],
    }),
    footer: (page, pages) => ({
      margin: [40, 16, 40, 0],
      columns: [
        { text: `Mina Store · Xuất lúc ${exportedAt}`, fontSize: 7.5, color: RC.muted },
        { text: `Trang ${page} / ${pages}`, fontSize: 7.5, color: RC.muted, alignment: "right" },
      ],
    }),
    content,
  };

  await new Promise((resolve) => pdfMake.createPdf(doc).download(reportFileName(r, "pdf"), resolve));
}
