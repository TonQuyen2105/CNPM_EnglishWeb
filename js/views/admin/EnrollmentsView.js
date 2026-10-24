/**
 * EnrollmentsView - Giao diện Quản lý Đăng ký học & Xuất CSV
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, formatDate, renderStatusBadge, escapeHtml } from '../../core/utils.js';

export class EnrollmentsView {
  static renderLayout(classes = []) {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý đăng ký học (Enrollments)</h1>
          <p class="page-header-subtitle">Theo dõi toàn bộ học viên ghi danh vào các lớp và tính toán doanh thu ước tính</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-secondary" id="btn-export-csv">
            ${icon('layers', { size: 16 })}
            <span>Xuất file Excel/CSV</span>
          </button>
        </div>
      </div>

      <!-- Toolbar Lọc & Tìm Kiếm -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-4">
            <select id="enrollment-class-filter" class="form-control">
              <option value="all">Tất cả các lớp học</option>
              ${classes.map(c => `<option value="${c.id}">[${c.class_code}] ${escapeHtml(c.class_name)}</option>`).join('')}
            </select>
          </div>
          <div class="col-span-3">
            <select id="enrollment-status-filter" class="form-control">
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang học (Active)</option>
              <option value="completed">Đã hoàn thành</option>
              <option value="cancelled">Đã hủy</option>
            </select>
          </div>
          <div class="col-span-3">
            <input type="text" id="enrollment-search-input" class="form-control" placeholder="Tìm tên học viên hoặc email...">
          </div>
          <div class="col-span-2 text-right">
            <button type="button" class="btn btn-secondary" id="btn-reset-enrollment-filters" style="width: 100%;">
              ${icon('refresh', { size: 14 })}
              <span>Đặt lại</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Data Table Outlet -->
      <div id="enrollments-table-outlet"></div>
      <div id="enrollments-pagination-outlet"></div>
    `;
  }
}
