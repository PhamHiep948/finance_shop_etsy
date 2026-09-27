const BASE = import.meta.env.VITE_API_URL || "http://localhost:5000";

export class ApiError extends Error {
  constructor(status, message, code) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

async function request(path, { method = "GET", body } = {}) {
  const isForm = typeof FormData !== "undefined" && body instanceof FormData;
  let res;
  try {
    const init = {
      method,
      headers: {
        ...(body !== undefined && !isForm ? { "Content-Type": "application/json" } : {}),
      },
    };
    if (body !== undefined) init.body = isForm ? body : JSON.stringify(body);
    res = await fetch(`${BASE}/api/v1${path}`, init);
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

/** Gọi API trả JSON. `body` có thể là object (JSON) hoặc FormData (upload file). */
export async function api(path, options) {
  const res = await request(path, options);
  if (res.status === 204) return null;
  return res.json().catch(() => null);
}

/** Gọi API trả file; trả về { blob, filename }. */
export async function apiBlob(path, options) {
  const res = await request(path, options);
  const cd = res.headers.get("Content-Disposition") || "";
  const filename = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(cd)?.[1];
  return { blob: await res.blob(), filename: filename ? decodeURIComponent(filename) : null };
}

/** Lấy toàn bộ các trang của một danh sách phân trang. */
export async function fetchAllPages(path) {
  const items = [];
  for (let page = 1; ; page++) {
    const sep = path.includes("?") ? "&" : "?";
    const res = await api(`${path}${sep}page=${page}&pageSize=100`);
    items.push(...res.items);
    if (page >= res.totalPages) return items;
  }
}
