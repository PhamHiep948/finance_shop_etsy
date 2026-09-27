import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { I } from "../lib/icons";
import { useFinance } from "../lib/store";
import { BrandLogo } from "./BrandLogo";

const NAV_GROUPS = [
  {
    label: "MENU CHÍNH",
    items: [
      { to: "/dashboard", label: "Tổng quan", perm: "dashboard", icon: "layout-dashboard" },
      { to: "/incomes", label: "Khoản thu", perm: "incomeRead", icon: "trending-up" },
      { to: "/expenses", label: "Khoản chi", perm: "expenseRead", icon: "trending-down" },
      { to: "/reports", label: "Báo cáo", perm: "reportRead", icon: "pie-chart" },
    ],
  },

];

export default function Shell() {
  const { can, confirm, setConfirm } = useFinance();
  const location = useLocation();
  const [darkMode, setDarkMode] = useState(() => localStorage.getItem("fm_theme") === "dark");

  const PAGE_METAS = {
    "/dashboard": { breadcrumb: "Tổng quan", desc: "Tổng quan tình hình tài chính & kinh doanh" },
    "/incomes": { breadcrumb: "Khoản thu", desc: "Quản lý doanh thu từ đơn hàng & bán lẻ" },
    "/expenses": { breadcrumb: "Khoản chi", desc: "Theo dõi chi phí hoạt động & đối soát thuế" },
    "/reports": { breadcrumb: "Báo cáo", desc: "Báo cáo tài chính & phân tích lợi nhuận" },

  };

  const pageMeta = PAGE_METAS[location.pathname] || {
    breadcrumb: "Tổng quan",
    desc: "Tổng quan tình hình tài chính & kinh doanh",
  };

  useEffect(() => {
    localStorage.removeItem("fm_rail");
    document.documentElement.classList.remove("rail-min");
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = darkMode ? "dark" : "light";
    localStorage.setItem("fm_theme", darkMode ? "dark" : "light");
  }, [darkMode]);


  return (
    <>
      <aside className="sidebar-new">
        {/* Brand */}
        <NavLink to="/dashboard" className="sb-brand" title="Mina Store — Quản lý tài chính">
          <BrandLogo iconSize={40} subText="Quản lý tài chính" />
        </NavLink>

        {/* Nav */}
        <nav className="nav-new">
          {NAV_GROUPS.map((group) => {
            const visible = group.items.filter((n) => can(n.perm));
            if (!visible.length) return null;
            return (
              <div key={group.label} className="nav-group-new">
                {visible.map((n) => (
                  <NavLink
                    key={n.to}
                    to={n.to}
                    title={n.label}
                    className={({ isActive }) => `nav-btn-new${isActive ? " active" : ""}`}
                  >
                    <I name={n.icon} size={18} />
                    <span className="nav-label-new">{n.label}</span>
                  </NavLink>
                ))}
              </div>
            );
          })}
        </nav>


      </aside>

      <div className="workspace-new">
        <header className="workspace-topbar">
          <div className="topbar-left">
            <div className="topbar-breadcrumb-row">
              <div className="topbar-breadcrumb">
                <NavLink to="/dashboard" className="topbar-root" title="Về trang Tổng quan">
                  <I name="layout-dashboard" size={14} />
                  <span>Tổng quan</span>
                </NavLink>
                {location.pathname !== "/dashboard" && (
                  <>
                    <span className="topbar-sep">/</span>
                    <span className="topbar-cur">{pageMeta.breadcrumb}</span>
                  </>
                )}
              </div>
            </div>
            <div className="topbar-subtitle">{pageMeta.desc}</div>
          </div>

          <div className="topbar-right">
            <label className="workspace-search">
              <I name="search" size={15} />
              <input type="search" placeholder="Tìm kiếm nhanh..." aria-label="Tìm kiếm nhanh" />
            </label>

            <div className="workspace-tools">
              <button type="button" className="workspace-tool-btn"
                aria-label={darkMode ? "Chuyển sang giao diện sáng" : "Chuyển sang giao diện tối"}
                title={darkMode ? "Giao diện sáng" : "Giao diện tối"}
                aria-pressed={darkMode} onClick={() => setDarkMode((value) => !value)}>
                <I name={darkMode ? "sun" : "moon"} size={17} />
              </button>
            </div>
          </div>
        </header>

        <section className="page"><Outlet /></section>
      </div>

      <div className={`modal-back${confirm ? " open" : ""}`} onClick={() => setConfirm(null)}>
        <div className="modal modal-confirm" onClick={(e) => e.stopPropagation()}>
          <div className="modal-confirm-icon"><I name="triangle-alert" /></div>
          <h3>Xác nhận xóa</h3>
          <p>{confirm?.text}</p>
          <div className="modal-actions">
            <button className="btn secondary" type="button" onClick={() => setConfirm(null)}>Hủy bỏ</button>
            <button className="btn danger" type="button" onClick={() => { const fn = confirm?.onOk; setConfirm(null); fn?.(); }}>
              <I name="trash-2" /> Xóa vĩnh viễn
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
