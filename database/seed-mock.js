// Sinh dữ liệu mock (khoản thu/chi từ 01/06/2026 đến hôm nay).
//   node database/seed-mock.js            -> gửi thẳng vào API (backend phải đang chạy ở localhost:5000)
//   node database/seed-mock.js --sql      -> chỉ ghi file database/mock_data.sql để import bằng phpMyAdmin
const fs = require("fs");
const path = require("path");

const API = process.env.API_URL || "http://localhost:5000/api/v1";
const START = new Date(2026, 5, 1);
const END = new Date();

// Bộ sinh số ngẫu nhiên có seed để lần nào chạy cũng ra cùng dữ liệu.
let seed = 20260601;
const rand = () => ((seed = (seed * 1664525 + 1013904223) % 4294967296) / 4294967296);
const pick = (arr) => arr[Math.floor(rand() * arr.length)];
const between = (a, b) => a + Math.floor(rand() * (b - a + 1));
const r2 = (n) => Math.round(n * 100) / 100;
const iso = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

const PRODUCTS = [
  ["Lily Flower Crochet Bouquet", 12.9], ["Tulip Crochet Keychain", 5.49], ["Sunflower Hair Clip", 6.5],
  ["Mini Amigurumi Bear", 18], ["Rose Crochet Bouquet (5 stems)", 24.9], ["Daisy Tote Bag", 29],
  ["Lavender Crochet Pot", 15.5], ["Cherry Blossom Brooch", 7.9], ["Crochet Cat Plush", 21],
  ["Custom Name Bouquet", 34],
];
const CHANNELS = ["ETSY_STORE", "ETSY_STORE", "ETSY_STORE", "WEBSITE_DIRECT", "INSTAGRAM_SHOP", "LOCAL_MARKET", "B2B_WHOLESALE"];
const OTHER_INCOME = [["Workshop móc len cuối tuần", 60, 120], ["Hoàn phí vận chuyển từ Etsy", 5, 18], ["Bán len thừa cho khách quen", 15, 40]];

function incomeRow(date) {
  const [name, unit] = pick(PRODUCTS);
  const qty = rand() < 0.7 ? 1 : between(2, 4);
  const itemTotal = r2(unit * qty);
  const discount = rand() < 0.2 ? r2(itemTotal * 0.1) : 0;
  const subtotal = r2(itemTotal - discount);
  const shipping = pick([0, 0, 3.5, 4.9, 6.9]);
  const inEU = rand() < 0.65;
  const taxPercent = inEU ? pick([19, 19, 20]) : 0;
  const amount = r2(subtotal + shipping);
  return {
    date, description: name, categoryId: 1, amount, taxPercent,
    orderCode: String(between(4100000000, 4199999999)),
    saleRegion: inEU ? "IN_EU" : "OUTSIDE_EU", salesChannel: pick(CHANNELS), productQty: qty,
    unitPrice: unit, itemTotal, discountAmount: discount, discountCode: discount ? "MINA10" : "",
    subtotal, shippingAmount: shipping, taxAmount: r2(amount * taxPercent / 100),
    referenceCode: "", note: rand() < 0.1 ? "Khách yêu cầu gói quà." : "",
  };
}

function otherIncomeRow(date) {
  const [name, min, max] = pick(OTHER_INCOME);
  return { date, description: name, categoryId: 2, amount: between(min, max), taxPercent: 0, referenceCode: `TK-${between(1000, 9999)}`, note: "" };
}

const EXPENSES = {
  3: [["Len cotton milk 50 cuộn", "Len Việt Store", 45, 90], ["Kim móc & phụ kiện", "Shopee - Kim Móc Xinh", 12, 30]],
  4: [["Hộp quà kraft + giấy lụa", "Bao Bì An Phát", 20, 45], ["Sticker cảm ơn in logo", "In Nhanh 24h", 10, 25]],
  5: [["Phí gửi hàng quốc tế", "DHL Express", 30, 85], ["Nhãn vận chuyển Etsy", "Etsy Shipping Labels", 15, 40]],
  6: [["Quảng cáo Etsy Ads", "Etsy Ads", 20, 50], ["Chạy quảng cáo Instagram", "Meta Ads", 25, 60]],
  7: [["Phí giao dịch Etsy", "Etsy Inc.", 15, 40], ["Phí cổng thanh toán PayPal", "PayPal", 5, 15]],
};
const PAYEE_SCOPE = { "DHL Express": "INTERNATIONAL", "Etsy Shipping Labels": "INTERNATIONAL", "Etsy Ads": "INTERNATIONAL", "Meta Ads": "INTERNATIONAL", "Etsy Inc.": "INTERNATIONAL", PayPal: "INTERNATIONAL" };

function expenseRow(date, categoryId, name, payee, amount, taxPercent, paymentMethod) {
  return {
    date, description: name, categoryId, amount: r2(amount), taxPercent,
    payee, originScope: PAYEE_SCOPE[payee] || "DOMESTIC", paymentMethod, note: "",
  };
}

function build() {
  const incomes = [];
  const expenses = [];
  for (let d = new Date(START); d <= END; d.setDate(d.getDate() + 1)) {
    const date = iso(d);
    const n = between(0, 2) + (d.getDay() === 0 || d.getDay() === 6 ? 1 : 0);
    for (let i = 0; i < n; i++) incomes.push(incomeRow(date));
    if (rand() < 0.06) incomes.push(otherIncomeRow(date));

    const day = d.getDate();
    if (day === 1) expenses.push(expenseRow(date, 10, "Thuê mặt bằng xưởng", "Chủ nhà - Cô Lan", 200, 0, "BANK_TRANSFER"));
    if (day === 5) expenses.push(expenseRow(date, 8, "Lương phụ việc bán thời gian", "Nguyễn Thu Hà", 300, 0, "BANK_TRANSFER"));
    if (day === 10) expenses.push(expenseRow(date, 9, "Internet + điện tháng", "VNPT", between(40, 65), 10, "BANK_TRANSFER"));
    if (day === 20 && d.getMonth() % 2 === 0) expenses.push(expenseRow(date, 11, "Máy cuốn len / dụng cụ", "Tiki", between(25, 80), 10, "CREDIT_CARD"));
    if (rand() < 0.22) {
      const cat = pick([3, 3, 4, 5, 5, 6, 7]);
      const [name, payee, min, max] = pick(EXPENSES[cat]);
      expenses.push(expenseRow(date, cat, name, payee, between(min * 100, max * 100) / 100, pick([0, 8, 10]), pick(["CREDIT_CARD", "BANK_TRANSFER", "PAYPAL", "CASH"])));
    }
    if (rand() < 0.03) expenses.push(expenseRow(date, 12, "Chi phí lặt vặt", "Cửa hàng tiện lợi", between(3, 20), 0, "CASH"));
  }
  return { incomes, expenses };
}

const DETAIL_KEYS = ["referenceCode", "unitPrice", "itemTotal", "discountAmount", "discountCode", "subtotal", "shippingAmount", "taxAmount", "note"];
const afterTax = (r) => r2(r.amount * (1 + r.taxPercent / 100));

function toSql({ incomes, expenses }) {
  const q = (v) => (v === undefined || v === null || v === "" ? "NULL" : typeof v === "number" ? String(v) : `'${String(v).replace(/'/g, "''")}'`);
  const rows = [...incomes.map((r) => ["INCOME", r]), ...expenses.map((r) => ["EXPENSE", r])].map(([kind, r]) => {
    const details = Object.fromEntries(DETAIL_KEYS.filter((k) => r[k] !== undefined).map((k) => [k, r[k]]));
    return `('${kind}',${q(r.date)},${q(r.description)},${r.categoryId},${r.amount},${r.taxPercent},${afterTax(r)},'USD',${q(r.orderCode)},${q(r.saleRegion)},${q(r.salesChannel)},${q(r.productQty)},${q(r.payee)},${q(r.originScope)},${q(r.paymentMethod)},${q(JSON.stringify(details))})`;
  });
  return `-- Dữ liệu mock cho Mina Store (import sau finance_local.sql).
-- Muốn xóa dữ liệu cũ trước khi import thì bỏ comment 2 dòng dưới:
-- DELETE FROM attachments;
-- DELETE FROM ledger_entries;
USE finance;
INSERT INTO ledger_entries (kind,date,description,category_id,amount,tax_percent,amount_after_tax,currency_code,order_code,sale_region,sales_channel,product_qty,payee,origin_scope,payment_method,details) VALUES
${rows.join(",\n")};
`;
}

async function post(kind, r) {
  const res = await fetch(`${API}/${kind}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...r, amountAfterTax: afterTax(r), currencyCode: "USD" }),
  });
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json();
}

(async () => {
  const data = build();
  fs.writeFileSync(path.join(__dirname, "mock_data.sql"), toSql(data), "utf8");
  console.log(`Đã ghi database/mock_data.sql (${data.incomes.length} khoản thu, ${data.expenses.length} khoản chi).`);
  if (process.argv.includes("--sql")) return;
  for (const r of data.incomes) await post("incomes", r);
  for (const r of data.expenses) await post("expenses", r);
  console.log(`Đã thêm qua API: ${data.incomes.length} khoản thu, ${data.expenses.length} khoản chi.`);
})().catch((e) => { console.error(e.message); process.exit(1); });
