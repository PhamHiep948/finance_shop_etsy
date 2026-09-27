import { useMemo } from "react";
export function useDashboard(dateFrom, dateTo, incomes, expenses = []) {
  const summary = useMemo(() => {
    const inc = incomes.filter((r) => r.incomeDate >= dateFrom && r.incomeDate <= dateTo);
    const exp = expenses.filter((r) => r.expenseDate >= dateFrom && r.expenseDate <= dateTo);
    const totalIncome = inc.reduce((sum, r) => sum + Number(r.amountAfterTax ?? r.amount), 0);
    const totalExpense = exp.reduce((sum, r) => sum + Number(r.amountAfterTax ?? r.amount), 0);
    return { totalIncome, totalExpense, netResult: totalIncome - totalExpense, transactionCount: inc.length + exp.length };
  }, [dateFrom, dateTo, incomes, expenses]);
  return { summary, error: "" };
}
