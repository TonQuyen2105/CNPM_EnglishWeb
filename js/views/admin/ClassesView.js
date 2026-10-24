/**
 * ClassesView - Giao diện Quản lý Danh sách Lớp học
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, formatDate, renderStatusBadge, escapeHtml } from '../../core/utils.js';

export class ClassesView {
  static renderLayout() {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý lớp học</h1>
          <p class="page-header-subtitle">Theo dõi lịch khai giảng, sĩ số học viên và giáo viên phụ trách</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-class">
            ${icon('plus', { size: 16 })}
            <span>Mở lớp học mới</span>
          </button>
        </div>
      </div>

      <!-- Toolbar Lọc & Tìm Kiếm -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-5">
            <div class="input-with-icon">
              <span class="input-icon">${icon('search', { size: 16 })}</span>
              <input type="text" id="class-search-input" class="form-control" placeholder="Tìm theo mã lớp (COMM-01), tên lớp...">
            </div>
          </div>
          <div class="col-span-4">
            <select id="class-status-filter" class="form-control">
              <option value="all">Tất cả trạng thái lớp</option>
              <option value="upcoming">Sắp khai giảng (Upcoming)</option>
              <option value="ongoing">Đang diễn ra (Ongoing)</option>
              <option value="completed">Đã kết thúc (Completed)</option>
              <option value="cancelled">Đã hủy (Cancelled)</option>
            </select>
          </div>
          <div class="col-span-3 text-right">
            <a href="#/schedules" class="btn btn-secondary" style="width: 100%;">
              ${icon('calendar', { size: 14 })}
              <span>Xem Thời khóa biểu</span>
            </a>
          </div>
        </div>
      </div>

      <!-- Data Table Outlet -->
      <div id="classes-table-outlet"></div>
      <div id="classes-pagination-outlet"></div>
    `;
  }

  static renderClassModal(courses, cls = null) {
    const isEdit = !!cls;
    return `
      <form id="class-modal-form">
        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-class-code">
              Mã lớp học <span class="required">*</span>
            </label>
            <input 
              type="text" 
              id="modal-class-code" 
              class="form-control" 
              placeholder="ví dụ: COMM-03" 
              value="${isEdit ? escapeHtml(cls.class_code) : ''}" 
              required
            >
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-class-course">
              Khóa học trực thuộc <span class="required">*</span>
            </label>
            <select id="modal-class-course" class="form-control" required>
              ${courses.map(c => `
                <option value="${c.id}" ${isEdit && cls.course_id === c.id ? 'selected' : ''}>
                  ${escapeHtml(c.title)}
                </option>
              `).join('')}
            </select>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-class-name">
            Tên lớp hiển thị <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-class-name" 
            class="form-control" 
            placeholder="ví dụ: Giao Tiếp Cơ Bản K03 (Buổi tối)" 
            value="${isEdit ? escapeHtml(cls.class_name) : ''}" 
            required
          >
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-class-start">Ngày khai giảng <span class="required">*</span></label>
            <input 
              type="date" 
              id="modal-class-start" 
              class="form-control" 
              value="${isEdit && cls.start_date ? cls.start_date.substring(0, 10) : '2026-11-01'}" 
              required
            >
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-class-end">Ngày bế giảng <span class="required">*</span></label>
            <input 
              type="date" 
              id="modal-class-end" 
              class="form-control" 
              value="${isEdit && cls.end_date ? cls.end_date.substring(0, 10) : '2027-01-15'}" 
              required
            >
          </div>
        </div>

        <div class="grid grid-cols-3 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-class-max">Sĩ số tối đa</label>
            <input 
              type="number" 
              id="modal-class-max" 
              class="form-control" 
              value="${isEdit ? cls.max_students : 20}" 
              min="5" 
              max="50"
            >
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-class-price">Học phí (VND)</label>
            <input 
              type="number" 
              id="modal-class-price" 
              class="form-control" 
              value="${isEdit ? cls.price : 2500000}"
            >
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-class-status">Trạng thái lớp</label>
            <select id="modal-class-status" class="form-control">
              <option value="upcoming" ${isEdit && cls.status === 'upcoming' ? 'selected' : ''}>Sắp khai giảng</option>
              <option value="ongoing" ${!isEdit || cls.status === 'ongoing' ? 'selected' : ''}>Đang diễn ra</option>
              <option value="completed" ${isEdit && cls.status === 'completed' ? 'selected' : ''}>Đã kết thúc</option>
              <option value="cancelled" ${isEdit && cls.status === 'cancelled' ? 'selected' : ''}>Đã hủy</option>
            </select>
          </div>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-class">
            ${isEdit ? 'Lưu thay đổi' : 'Tạo lớp học'}
          </button>
        </div>
      </form>
    `;
  }
}
