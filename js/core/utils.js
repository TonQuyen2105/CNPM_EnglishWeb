/**
 * Utility Functions - Tiện ích định dạng ngày tháng, tiền tệ, chuỗi và hỗ trợ UI
 */

/**
 * Định dạng tiền tệ VND
 * @param {number|string} amount 
 * @returns {string} ví dụ: "1.500.000 ₫"
 */
export function formatCurrency(amount) {
  const num = Number(amount) || 0;
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0
  }).format(num);
}

/**
 * Định dạng ngày tháng tiếng Việt
 * @param {string|Date} dateStr 
 * @param {boolean} includeTime 
 * @returns {string} ví dụ: "24/10/2026" hoặc "14:30 24/10/2026"
 */
export function formatDate(dateStr, includeTime = false) {
  if (!dateStr) return '—';
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;

    const pad = (n) => (n < 10 ? '0' + n : n);
    const day = pad(d.getDate());
    const month = pad(d.getMonth() + 1);
    const year = d.getFullYear();

    if (includeTime) {
      const hours = pad(d.getHours());
      const mins = pad(d.getMinutes());
      return `${hours}:${mins} ${day}/${month}/${year}`;
    }

    return `${day}/${month}/${year}`;
  } catch {
    return dateStr;
  }
}

/**
 * Trả về thời gian tương đối (ví dụ: "vừa xong", "5 phút trước", "3 ngày trước")
 * @param {string|Date} dateStr 
 * @returns {string}
 */
export function formatRelativeTime(dateStr) {
  if (!dateStr) return '';
  const now = new Date();
  const past = new Date(dateStr);
  const diffSec = Math.floor((now - past) / 1000);

  if (diffSec < 60) return 'Vừa xong';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin} phút trước`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour} giờ trước`;
  const diffDay = Math.floor(diffHour / 24);
  if (diffDay < 30) return `${diffDay} ngày trước`;
  return formatDate(dateStr);
}

/**
 * Debounce hàm
 * @param {Function} fn 
 * @param {number} delay 
 * @returns {Function}
 */
export function debounce(fn, delay = 300) {
  let timer = null;
  return function (...args) {
    clearTimeout(timer);
    timer = setTimeout(() => fn.apply(this, args), delay);
  };
}

/**
 * Thoát ký tự HTML chống XSS
 * @param {string} str 
 * @returns {string}
 */
export function escapeHtml(str) {
  if (typeof str !== 'string') return '';
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

/**
 * Tạo ID tự sinh đơn giản
 * @param {string} prefix 
 * @returns {string}
 */
export function generateId(prefix = 'id') {
  return `${prefix}_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
}

/**
 * Lấy chữ cái đầu đại diện avatar
 * @param {string} name 
 * @returns {string}
 */
export function getInitials(name = '') {
  if (!name) return 'U';
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}

/**
 * Tính phần trăm an toàn
 * @param {number} part 
 * @param {number} total 
 * @returns {number}
 */
export function calculatePercentage(part = 0, total = 0) {
  if (!total || total <= 0) return 0;
  return Math.min(100, Math.round((part / total) * 100));
}

/**
 * Chuyển trạng thái lớp sang badge HTML
 * @param {string} status 
 * @returns {string}
 */
export function renderStatusBadge(status) {
  const map = {
    upcoming: { text: 'Sắp khai giảng', class: 'badge-info' },
    ongoing: { text: 'Đang diễn ra', class: 'badge-success' },
    completed: { text: 'Đã hoàn thành', class: 'badge-neutral' },
    cancelled: { text: 'Đã hủy', class: 'badge-danger' },
    active: { text: 'Đang hoạt động', class: 'badge-success' },
    inactive: { text: 'Tạm khóa', class: 'badge-danger' },
    scheduled: { text: 'Theo lịch', class: 'badge-info' },
    in_progress: { text: 'Đang làm', class: 'badge-warning' },
    submitted: { text: 'Đã nộp', class: 'badge-info' },
    passed: { text: 'Đạt yêu cầu', class: 'badge-success' },
    failed: { text: 'Chưa đạt', class: 'badge-danger' },
    pending_review: { text: 'Chờ chấm', class: 'badge-warning' },
    graded: { text: 'Đã chấm', class: 'badge-success' },
    auto_graded: { text: 'Chấm tự động', class: 'badge-info' }
  };

  const item = map[status] || { text: status, class: 'badge-neutral' };
  return `<span class="badge ${item.class}"><span class="badge-dot"></span>${escapeHtml(item.text)}</span>`;
}

/**
 * Chuyển cấp độ sang badge HTML
 * @param {string} level 
 * @returns {string}
 */
export function renderLevelBadge(level) {
  const map = {
    beginner: { text: 'Cơ bản', class: 'badge-info' },
    intermediate: { text: 'Trung cấp', class: 'badge-warning' },
    advanced: { text: 'Nâng cao', class: 'badge-danger' }
  };
  const item = map[level] || { text: level, class: 'badge-neutral' };
  return `<span class="badge ${item.class}">${escapeHtml(item.text)}</span>`;
}
