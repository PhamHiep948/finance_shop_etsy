// Gọi API backend C#.
// Khi trang được backend phục vụ (cổng 5000) thì dùng cùng origin; nếu mở bằng Live Server... thì gọi localhost:5000.
// Có thể ghi đè bằng cách đặt window.FM_API_URL trước khi tải file này.
const API_BASE = window.FM_API_URL
  || (location.protocol.startsWith("http") && location.port === "5000" ? location.origin : "http://localhost:5000");

class ApiError extends Error {
  constructor(status, message, code) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

/** `body` có thể là object (gửi JSON) hoặc FormData (upload file). */
async function apiRequest(path, { method = "GET", body } = {}) {
  const isForm = body instanceof FormData;
  let res;
  try {
    const init = { method, headers: body !== undefined && !isForm ? { "Content-Type": "application/json" } : {} };
    if (body !== undefined) init.body = isForm ? body : JSON.stringify(body);
    res = await fetch(`${API_BASE}/api/v1${path}`, init);
  } catch {
    throw new ApiError(0, "Không kết nối được máy chủ. Hãy kiểm tra backend đang chạy.", "NETWORK");
  }

  if (!res.ok) {
    const data = await res.json().catch(() => null);
    const detail = data?.errors ? Object.values(data.errors).flat().join(" ") : data?.detail;
    throw new ApiError(res.status, detail || data?.title || `Lỗi ${res.status}`, data?.errorCode);
  }
  return res;
}

/** Gọi API trả JSON. */
async function api(path, options) {
  const res = await apiRequest(path, options);
  if (res.status === 204) return null;
  return res.json().catch(() => null);
}

/** Đường dẫn xem / tải một chứng từ. */
function attachmentUrl(id, download = false) {
  return `${API_BASE}/api/v1/attachments/${id}${download ? "?download=1" : ""}`;
}

/** Lấy toàn bộ các trang của một danh sách phân trang. */
async function fetchAllPages(path) {
  const items = [];
  for (let page = 1; ; page++) {
    const sep = path.includes("?") ? "&" : "?";
    const res = await api(`${path}${sep}page=${page}&pageSize=100`);
    items.push(...res.items);
    if (page >= res.totalPages) return items;
  }
}
