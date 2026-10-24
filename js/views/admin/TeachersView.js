/**
 * TeachersView - Giao diện Quản lý Giáo viên (Teachers)
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml } from '../../core/utils.js';

export class TeachersView {
  static renderLayout() {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý đội ngũ giảng viên</h1>
          <p class="page-header-subtitle">Hồ sơ chuyên môn, chứng chỉ quốc tế và các lớp học đang phụ trách</p>
        </div>
        <div class="page-header-actions">
          <a href="#/users" class="btn btn-secondary">
            ${icon('users', { size: 16 })}
            <span>Quản lý tài khoản</span>
          </a>
        </div>
      </div>

      <!-- Toolbar Lọc & Tìm Kiếm -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-8">
            <div class="input-with-icon">
              <span class="input-icon">${icon('search', { size: 16 })}</span>
              <input type="text" id="teacher-search-input" class="form-control" placeholder="Tìm theo tên giáo viên, học vị, chứng chỉ...">
            </div>
          </div>
          <div class="col-span-4">
            <select id="teacher-verify-filter" class="form-control">
              <option value="all">Tất cả trạng thái hồ sơ</option>
              <option value="verified">Đã xác minh (Verified)</option>
              <option value="unverified">Chờ xác minh (Pending)</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Teachers Grid Cards Outlet -->
      <div id="teachers-grid-outlet" class="grid grid-cols-3 gap-6"></div>
    `;
  }

  static renderTeacherCard(teacher, assignedClasses = []) {
    const isVerified = teacher.is_verified === 1 || teacher.is_verified === true;
    return `
      <div class="card card-hover" style="display: flex; flex-direction: column;">
        <div class="card-body" style="flex: 1;">
          <div style="display: flex; align-items: flex-start; justify-content: space-between; gap: 0.75rem; margin-bottom: 1rem;">
            <div style="display: flex; align-items: center; gap: 0.75rem;">
              <div class="avatar avatar-lg">
                <img src="${teacher.avatar_url || 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150'}" alt="${teacher.full_name}">
              </div>
              <div>
                <h4 style="font-size: 1rem; margin-bottom: 2px;">${escapeHtml(teacher.full_name)}</h4>
                <div style="font-size: 0.75rem; color: var(--text-muted);">${teacher.email}</div>
              </div>
            </div>
            ${isVerified 
              ? `<span class="badge badge-success" title="Hồ sơ đã được kiểm định"><span class="badge-dot"></span>Đã duyệt</span>`
              : `<span class="badge badge-warning" title="Đang chờ kiểm định hồ sơ"><span class="badge-dot"></span>Chờ duyệt</span>`
            }
          </div>

          <div style="margin-bottom: 0.75rem;">
            <div style="font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 2px;">
              Bằng cấp & Chứng chỉ
            </div>
            <div style="font-size: 0.8125rem; font-weight: 600; color: var(--primary-700); background: var(--primary-50); padding: 4px 8px; border-radius: var(--radius-sm);">
              ${escapeHtml(teacher.qualification || 'Chưa cập nhật')}
            </div>
          </div>

          <div style="margin-bottom: 1rem;">
            <div style="font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 2px;">
              Giới thiệu
            </div>
            <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5; margin: 0; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
              ${escapeHtml(teacher.bio || 'Chưa có tiểu sử tóm tắt.')}
            </p>
          </div>

          <div>
            <div style="font-size: 0.75rem; font-weight: 700; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 6px;">
              Lớp đang phụ trách (${assignedClasses.length})
            </div>
            ${assignedClasses.length === 0 ? `
              <div style="font-size: 0.75rem; color: var(--text-muted); font-style: italic;">Chưa được phân công lớp nào</div>
            ` : `
              <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                ${assignedClasses.map(cls => `
                  <a href="#/classes/${cls.class_id}" class="badge badge-info" style="font-size: 0.7rem; text-decoration: none;" title="${cls.class_name}">
                    ${cls.class_code} (${cls.role === 'main' ? 'Chính' : 'Trợ giảng'})
                  </a>
                `).join('')}
              </div>
            `}
          </div>
        </div>

        <div class="card-footer" style="background: var(--neutral-50); display: flex; justify-content: space-between;">
          <button type="button" class="btn btn-secondary btn-sm btn-edit-teacher" data-id="${teacher.id}">
            ${icon('edit', { size: 14 })}
            <span>Sửa hồ sơ</span>
          </button>
          <button type="button" class="btn ${isVerified ? 'btn-danger-soft' : 'btn-primary'} btn-sm btn-toggle-verify" data-id="${teacher.id}" data-verified="${isVerified ? '1' : '0'}">
            ${isVerified ? 'Hủy duyệt' : 'Xác minh ngay'}
          </button>
        </div>
      </div>
    `;
  }

  static renderTeacherModal(teacher) {
    return `
      <form id="teacher-modal-form">
        <div class="form-group">
          <label class="form-label">Giảng viên</label>
          <input type="text" class="form-control" value="${escapeHtml(teacher.full_name)}" disabled style="background-color: var(--neutral-100);">
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-teacher-qualification">
            Bằng cấp & Chứng chỉ <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-teacher-qualification" 
            class="form-control" 
            value="${escapeHtml(teacher.qualification || '')}" 
            placeholder="ví dụ: MA in Applied Linguistics, IELTS 8.5, TESOL" 
            required
          >
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-teacher-bio">Tiểu sử & Kinh nghiệm giảng dạy</label>
          <textarea id="modal-teacher-bio" class="form-control" rows="3" placeholder="Mô tả tóm tắt thế mạnh chuyên môn...">${escapeHtml(teacher.bio || '')}</textarea>
        </div>

        <div class="form-group">
          <label class="form-check">
            <input type="checkbox" id="modal-teacher-verified" ${teacher.is_verified === 1 || teacher.is_verified === true ? 'checked' : ''}>
            <span style="font-weight: 600;">Đã xác minh và phê duyệt hồ sơ chuyên môn</span>
          </label>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-teacher">Lưu thay đổi</button>
        </div>
      </form>
    `;
  }
}
