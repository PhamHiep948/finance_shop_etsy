// Khung giao diện (sidebar, topbar, hộp xác nhận, toast) và điều hướng theo hash (#/dashboard...).
const NAV_ITEMS = [
  { to: "/dashboard", label: "Tổng quan", icon: "layout-dashboard" },
  { to: "/incomes", label: "Khoản thu", icon: "trending-up" },
  { to: "/expenses", label: "Khoản chi", icon: "trending-down" },
  { to: "/reports", label: "Báo cáo", icon: "pie-chart" },
  { to: "/history", label: "Lịch sử", icon: "history" },
  { to: "/trash", label: "Thùng rác", icon: "trash-2" },
];

const PAGE_METAS = {
  "/dashboard": { breadcrumb: "Tổng quan", desc: "Tổng quan tình hình tài chính & kinh doanh" },
  "/incomes": { breadcrumb: "Khoản thu", desc: "Quản lý doanh thu từ đơn hàng & bán lẻ" },
  "/expenses": { breadcrumb: "Khoản chi", desc: "Theo dõi chi phí hoạt động & đối soát thuế" },
  "/reports": { breadcrumb: "Báo cáo", desc: "Báo cáo tài chính & phân tích lợi nhuận" },
  "/history": { breadcrumb: "Lịch sử", desc: "Nhật ký thêm, sửa, xóa khoản thu chi và loại" },
  "/trash": { breadcrumb: "Thùng rác", desc: "Khôi phục hoặc xóa vĩnh viễn các khoản đã xóa" },
};

const ROUTES = {
  "/dashboard": DashboardPage,
  "/incomes": IncomesPage,
  "/expenses": ExpensesPage,
  "/reports": ReportsPage,
  "/history": HistoryPage,
  "/trash": TrashPage,
};

let darkMode = localStorage.getItem("fm_theme") === "dark";
let confirmHandler = null;
const current = { path: null, page: null, query: new URLSearchParams() };

function renderShell() {
  document.getElementById("root").innerHTML = `
    <aside class="sidebar-new">
      <a href="#/dashboard" class="sb-brand" title="Mina Store — Quản lý tài chính">
        <div class="sb-brand-wrap">
          <div class="sb-brand-icon-img" style="width: 40px; height: 40px" aria-hidden="true">
            <img src="assets/mina-store-logo.jpg" alt="Mina Store Logo" width="40" height="40" class="brand-logo-img" loading="eager" />
          </div>
          <div class="sb-brand-text">
            <span class="sb-brand-name">
              <span class="brand-name-mina">Mina</span>
              <span class="brand-name-store">Store</span>
            </span>
            <span class="sb-brand-sub">
              <span class="brand-sub-dot" aria-hidden="true"></span>
              <span>Quản lý tài chính</span>
            </span>
          </div>
        </div>
      </a>
      <nav class="nav-new">
        <div class="nav-group-new">
          ${NAV_ITEMS.map((n) => `
            <a href="#${n.to}" title="${n.label}" class="nav-btn-new" data-path="${n.to}">
              ${icon(n.icon, 18)}
              <span class="nav-label-new">${n.label}</span>
            </a>`).join("")}
        </div>
      </nav>
    </aside>

    <div class="workspace-new">
      <header class="workspace-topbar">
        <div class="topbar-left">
          <div class="topbar-breadcrumb-row">
            <div class="topbar-breadcrumb">
              <a href="#/dashboard" class="topbar-root" title="Về trang Tổng quan">
                ${icon("layout-dashboard", 14)}
                <span>Tổng quan</span>
              </a>
              <span id="crumb-tail"></span>
            </div>
          </div>
          <div class="topbar-subtitle" id="page-desc"></div>
        </div>
        <div class="topbar-right">
          <div class="workspace-tools">
            <button type="button" class="workspace-tool-btn" id="theme-btn"></button>
          </div>
        </div>
      </header>
      <section class="page" id="page"></section>
    </div>

    <div class="modal-back" id="confirm-modal">
      <div class="modal modal-confirm">
        <div class="modal-confirm-icon">${icon("triangle-alert")}</div>
        <h3 id="confirm-title">Xác nhận xóa</h3>
        <p id="confirm-text"></p>
        <p class="confirm-note" id="confirm-note" hidden></p>
        <div class="modal-actions">
          <button class="btn secondary" type="button" id="confirm-cancel">Hủy bỏ</button>
          <button class="btn danger" type="button" id="confirm-ok"></button>
        </div>
      </div>
    </div>

    <div class="toast-wrap"></div>`;

  document.getElementById("theme-btn").addEventListener("click", () => {
    darkMode = !darkMode;
    applyTheme();
  });

  const modal = document.getElementById("confirm-modal");
  modal.addEventListener("click", (e) => { if (e.target === modal) closeConfirm(); });
  document.getElementById("confirm-cancel").addEventListener("click", closeConfirm);
  document.getElementById("confirm-ok").addEventListener("click", () => {
    const fn = confirmHandler;
    closeConfirm();
    fn?.();
  });
}

function applyTheme() {
  document.documentElement.dataset.theme = darkMode ? "dark" : "light";
  localStorage.setItem("fm_theme", darkMode ? "dark" : "light");
  const btn = document.getElementById("theme-btn");
  btn.innerHTML = icon(darkMode ? "sun" : "moon", 17);
  btn.title = darkMode ? "Giao diện sáng" : "Giao diện tối";
  btn.setAttribute("aria-label", darkMode ? "Chuyển sang giao diện sáng" : "Chuyển sang giao diện tối");
  btn.setAttribute("aria-pressed", String(darkMode));
}

/**
 * Hộp xác nhận. `opts`: { title, okLabel, note } — note là dòng lưu ý nhỏ bên dưới.
 * Mặc định nút là "Xóa"; chỉ dùng "Xóa vĩnh viễn" khi dữ liệu không khôi phục được.
 */
function openConfirm(text, onOk, opts = {}) {
  confirmHandler = onOk;
  document.getElementById("confirm-title").textContent = opts.title || "Xác nhận xóa";
  document.getElementById("confirm-text").textContent = text;
  const note = document.getElementById("confirm-note");
  note.hidden = !opts.note;
  note.innerHTML = opts.note ? `${icon("info", 14)}<span>${esc(opts.note)}</span>` : "";
  document.getElementById("confirm-ok").innerHTML = `${icon(opts.icon || "trash-2")} ${esc(opts.okLabel || "Xóa")}`;
  document.getElementById("confirm-modal").classList.add("open");
}
const TRASH_NOTE = "Khoản bị xóa sẽ được chuyển vào Thùng rác và có thể khôi phục trong 30 ngày.";

function closeConfirm() {
  confirmHandler = null;
  document.getElementById("confirm-modal").classList.remove("open");
}

/** Chuyển trang, ví dụ navigate("/incomes?new=1"). `replace` = không thêm vào lịch sử. */
function navigate(to, replace = false) {
  if (replace) {
    history.replaceState(null, "", `#${to}`);
    route();
  } else {
    location.hash = to;
  }
}

function parseHash() {
  const raw = location.hash.slice(1) || "/dashboard";
  const [path, qs = ""] = raw.split("?");
  return { path, query: new URLSearchParams(qs) };
}

function updateShell(path) {
  const meta = PAGE_METAS[path];
  document.querySelectorAll(".nav-btn-new").forEach((a) => a.classList.toggle("active", a.dataset.path === path));
  document.getElementById("crumb-tail").innerHTML = path !== "/dashboard"
    ? `<span class="topbar-sep">/</span><span class="topbar-cur">${meta.breadcrumb}</span>`
    : "";
  document.getElementById("page-desc").textContent = meta.desc;
}

function route() {
  const { path, query } = parseHash();
  const page = ROUTES[path];
  if (!page) {
    navigate("/dashboard", true);
    return;
  }
  const changed = path !== current.path;
  if (changed) {
    current.page?.destroy();
    current.path = path;
    current.page = page;
    page.init();
    updateShell(path);
  }
  current.query = query;
  renderPage();
  if (changed) {
    window.scrollTo({ top: 0, left: 0, behavior: "auto" });
    document.querySelectorAll(".table-wrap").forEach((el) => { el.scrollLeft = 0; });
  }
}

function renderPage() {
  if (current.page) current.page.render(document.getElementById("page"), current.query);
}

// ---- Khởi động
renderShell();
applyTheme();
onStoreChange(renderPage);
window.addEventListener("hashchange", route);
route();
reload();
loadFx();
