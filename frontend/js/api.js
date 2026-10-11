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
    throw new ApiError(res.status, errorMessage(data) || `Lỗi ${res.status}`, data?.errorCode);
  }
  return res;
}

/** Câu báo lỗi trong body: Response<T> ({ message, errors: [] }) hoặc ProblemDetails ({ title, errors: {} }). */
function errorMessage(data) {
  if (!data) return "";
  if (Array.isArray(data.errors) && data.errors.length) return data.errors.join(" ");
  if (data.errors && typeof data.errors === "object") return Object.values(data.errors).flat().join(" ");
  return data.message || data.detail || data.title || "";
}

/**
 * Gọi API trả JSON. Backend bọc kết quả trong Response<T> { succeeded, message, errorCode, errors, data }:
 * thành công thì trả về `data`, thất bại thì ném ApiError với câu báo lỗi của backend.
 */
async function api(path, options) {
  const res = await apiRequest(path, options);
  if (res.status === 204) return null;
  const body = await res.json().catch(() => null);
  if (body && typeof body === "object" && "succeeded" in body) {
    if (!body.succeeded) throw new ApiError(res.status, errorMessage(body) || "Có lỗi xảy ra.", body.errorCode);
    return body.data ?? null;
  }
  return body;
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
