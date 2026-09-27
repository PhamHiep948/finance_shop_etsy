import { useEffect, useLayoutEffect } from "react";
import { HashRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { FinanceProvider, useFinance } from "./lib/store";
import Shell from "./components/Shell";
import Dashboard from "./features/dashboard/pages/Dashboard";
import RecordList from "./pages/RecordList";
import Reports from "./features/reports/pages/Reports";

function BodyClass() {
  const { toasts } = useFinance();
  const loc = useLocation();
  useEffect(() => {
    document.body.classList.remove("login-mode");
    document.body.classList.add("app");
  }, [loc.pathname]);
  return (
    <div className="toast-wrap">
      {toasts.map((t) => (
        <div key={t.id} className="toast">{t.msg}</div>
      ))}
    </div>
  );
}

function NavigationEffects() {
  const { pathname } = useLocation();

  useLayoutEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: "auto" });
    document.querySelectorAll(".table-wrap").forEach((el) => {
      el.scrollLeft = 0;
    });
  }, [pathname]);

  return null;
}

function AppRoutes() {
  return (
    <>
      <BodyClass />
      <NavigationEffects />
      <Routes>
        <Route element={<Shell />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/incomes" element={<RecordList key="income" kind="income" />} />
          <Route path="/expenses" element={<RecordList key="expense" kind="expense" />} />
          <Route path="/reports" element={<Reports />} />
        </Route>
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </>
  );
}

export default function App() {
  return (
    <FinanceProvider>
      <HashRouter>
        <AppRoutes />
      </HashRouter>
    </FinanceProvider>
  );
}
