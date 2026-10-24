/**
 * UsersView - Giao diện Quản lý người dùng (Users)
 */

import { icon } from '../../ui/icons.js';
import { formatDate, escapeHtml } from '../../core/utils.js';

export class UsersView {
  static renderLayout() {
    return `
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý người dùng</h1>
          <p class="page-header-subtitle">Danh sách toàn bộ tài khoản Quản trị, Giảng viên và Học viên</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-user">
            ${icon('plus', { size: 16 })}
            <span>Thêm tài khoản mới</span>
          </button>
        </div>
      </div>

      <!-- Filters & Search Toolbar -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-5">
            <div class="input-with-icon">
              <span class="input-icon">${icon('search', { size: 16 })}</span>
              <input type="text" id="user-search-input" class="form-control" placeholder="Tìm theo tên hoặc email...">
            </div>
          </div>
          <div class="col-span-3">
            <select id="user-role-filter" class="form-control">
              <option value="all">Tất cả vai trò</option>
              <option value="admin">Quản trị viên (Admin)</option>
              <option value="teacher">Giảng viên (Teacher)</option>
              <option value="student">Học viên (Student)</option>
            </select>
          </div>
          <div class="col-span-2">
            <select id="user-status-filter" class="form-control">
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang hoạt động</option>
              <option value="inactive">Đã khóa</option>
            </select>
          </div>
          <div class="col-span-2 text-right">
            <button type="button" class="btn btn-secondary" id="btn-reset-user-filters" style="width: 100%;">
              ${icon('refresh', { size: 14 })}
              <span>Đặt lại</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Data Table Outlet -->
      <div id="users-table-outlet"></div>
      
      <!-- Pagination Outlet -->
      <div id="users-pagination-outlet"></div>
    `;
  }

  static renderRoleBadge(role) {
    switch (role) {
      case 'admin':
        return `<span class="badge badge-primary"><span class="badge-dot"></span>Quản trị viên</span>`;
      case 'teacher':
        return `<span class="badge badge-info"><span class="badge-dot"></span>Giảng viên</span>`;
      case 'student':
        return `<span class="badge badge-neutral"><span class="badge-dot"></span>Học viên</span>`;
      default:
        return `<span class="badge badge-neutral">${role}</span>`;
    }
  }

  static renderUserModal(user = null) {
    const isEdit = !!user;
    return `
      <form id="user-modal-form" novalidate>
        <div class="form-group">
          <label class="form-label" for="modal-user-name">
            Họ và tên <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-user-name" 
            class="form-control" 
            placeholder="ví dụ: Nguyễn Văn An" 
            value="${isEdit ? escapeHtml(user.full_name) : ''}" 
            required
          >
          <div class="form-error" id="err-modal-name">Vui lòng nhập họ tên.</div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-user-email">
            Địa chỉ Email <span class="required">*</span>
          </label>
          <input 
            type="email" 
            id="modal-user-email" 
            class="form-control" 
            placeholder="ví dụ: an.nguyen@el.com" 
            value="${isEdit ? escapeHtml(user.email) : ''}" 
            required
          >
          <div class="form-error" id="err-modal-email">Vui lòng nhập email hợp lệ.</div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-user-role">
              Vai trò (Role) <span class="required">*</span>
            </label>
            <select id="modal-user-role" class="form-control" ${isEdit && user.role === 'admin' ? 'disabled' : ''}>
              <option value="student" ${isEdit && user.role === 'student' ? 'selected' : ''}>Học viên (Student)</option>
              <option value="teacher" ${isEdit && user.role === 'teacher' ? 'selected' : ''}>Giảng viên (Teacher)</option>
              <option value="admin" ${isEdit && user.role === 'admin' ? 'selected' : ''}>Quản trị viên (Admin)</option>
            </select>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-user-status">Trạng thái</label>
            <select id="modal-user-status" class="form-control">
              <option value="1" ${!isEdit || user.is_active === 1 || user.is_active === true ? 'selected' : ''}>Hoạt động</option>
              <option value="0" ${isEdit && (user.is_active === 0 || user.is_active === false) ? 'selected' : ''}>Tạm khóa</option>
            </select>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-user-avatar">Đường dẫn ảnh đại diện (URL)</label>
          <input 
            type="url" 
            id="modal-user-avatar" 
            class="form-control" 
            placeholder="https://images.unsplash.com/photo-..." 
            value="${isEdit && user.avatar_url ? escapeHtml(user.avatar_url) : ''}"
          >
          <div class="form-hint">Để trống sẽ tự động lấy avatar mặc định theo tên.</div>
        </div>

        ${!isEdit ? `
          <div class="form-group">
            <label class="form-label" for="modal-user-password">Mật khẩu khởi tạo</label>
            <input type="text" id="modal-user-password" class="form-control" value="123456" readonly style="background-color: var(--neutral-100);">
            <div class="form-hint">Mật khẩu mặc định hệ thống là 123456. Người dùng có thể đổi sau khi đăng nhập.</div>
          </div>
        ` : ''}

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy bỏ</button>
          <button type="submit" class="btn btn-primary" id="btn-save-user">
            ${isEdit ? 'Lưu thay đổi' : 'Tạo tài khoản'}
          </button>
        </div>
      </form>
    `;
  }
}
