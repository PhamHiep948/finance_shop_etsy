import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { EXPENSE_CATEGORIES, INCOME_CATEGORIES } from "./data";
import { api, fetchAllPages } from "./api";
import { afterTax, fromDisplay, fromDisplayNum } from "./format";

const Ctx = createContext(null);
const KIND = {
  income: { path: "/incomes", dateKey: "incomeDate", label: "khoản thu" },
  expense: { path: "/expenses", dateKey: "expenseDate", label: "khoản chi" },
};
const mapRow = (key) => (r) => ({
  ...r, [key]: r.date, amount: Number(r.amount), taxPercent: Number(r.taxPercent),
  amountAfterTax: Number(r.amountAfterTax), currency: r.currencyCode,
  recipient: r.payee || "", source: "MANUAL",
});
const bodyOf = (key, p) => ({
  ...p, date: p[key], description: p.description, categoryId: p.categoryId,
  amount: p.amount, taxPercent: p.taxPercent, amountAfterTax: p.amountAfterTax,
  currencyCode: "USD", orderCode: p.orderCode || null,
  saleRegion: p.saleRegion || null, salesChannel: p.salesChannel || null,
  productQty: p.productQty || null, payee: p.recipient || null,
  originScope: p.originScope || null, paymentMethod: p.paymentMethod || null,
});
export function FinanceProvider({ children }) {
  const [ccy, setCcy] = useState("USD");
  const [toasts, setToasts] = useState([]);
  const [confirm, setConfirm] = useState(null);
  const [incomes, setIncomes] = useState([]);
  const [expenses, setExpenses] = useState([]);
  const [loading, setLoading] = useState(true);
  const toast = useCallback((msg) => {
    const id = Date.now() + Math.random();
    setToasts((t) => [...t, { id, msg }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3000);
  }, []);
  const reload = useCallback(async () => {
    setLoading(true);
    try {
      const [incomeCats, expenseCats, incomeRows, expenseRows] = await Promise.all([
        api("/categories/income"), api("/categories/expense"),
        fetchAllPages("/incomes"), fetchAllPages("/expenses"),
      ]);
      INCOME_CATEGORIES.splice(0, INCOME_CATEGORIES.length, ...incomeCats);
      EXPENSE_CATEGORIES.splice(0, EXPENSE_CATEGORIES.length, ...expenseCats);
      setIncomes(incomeRows.map(mapRow("incomeDate")));
      setExpenses(expenseRows.map(mapRow("expenseDate")));
    } catch (e) { toast(e.message); }
    finally { setLoading(false); }
  }, [toast]);
  useEffect(() => { reload(); }, [reload]);
  const saveRecord = useCallback(async (kind, payload, id) => {
    const config = KIND[kind];
    const previous = id ? (kind === "income" ? incomes : expenses).find((r) => r.id === id) : null;
    const body = bodyOf(config.dateKey, { salesChannel: previous?.salesChannel, paymentMethod: previous?.paymentMethod, ...payload });
    try {
      await api(`${config.path}${id ? `/${id}` : ""}`, { method: id ? "PUT" : "POST", body });
      await reload();
      toast(id ? `Đã cập nhật ${config.label}` : `Đã thêm ${config.label}`);
      return true;
    } catch (e) { toast(e.message); return false; }
  }, [incomes, expenses, reload, toast]);
  const softDelete = useCallback(async (kind, id) => {
    try { await api(`${KIND[kind].path}/${id}`, { method: "DELETE" }); await reload(); toast("Đã xóa khoản"); }
    catch (e) { toast(e.message); }
  }, [reload, toast]);
  const value = useMemo(() => ({
    current: null, ccy, setCcy, toasts, toast, confirm, setConfirm, loading,
    can: () => true, canEditOwn: () => true, canDeleteOwn: () => true,
    incomes, expenses, allIncomes: incomes, allExpenses: expenses,
    saveIncome: (p, id) => saveRecord("income", p, id),
    saveExpense: (p, id) => saveRecord("expense", p, id),
    softDelete, reload, fromDisplay, fromDisplayNum, afterTax,
  }), [ccy, toasts, toast, confirm, loading, incomes, expenses, saveRecord, softDelete, reload]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}
export function useFinance() {
  const value = useContext(Ctx);
  if (!value) throw new Error("FinanceProvider is missing");
  return value;
}

export function incomePayload(fd, ccy) {
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

export function expensePayload(fd, ccy) {
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
