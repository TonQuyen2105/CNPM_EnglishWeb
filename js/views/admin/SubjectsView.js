/**
 * SubjectsView - Giao diện Quản lý Môn học
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml } from '../../core/utils.js';

export class SubjectsView {
  static renderLayout() {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý môn học đào tạo</h1>
          <p class="page-header-subtitle">Danh mục các chuyên ngành tiếng Anh cốt lõi tại trung tâm</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-subject">
            ${icon('plus', { size: 16 })}
            <span>Thêm môn học mới</span>
          </button>
        </div>
      </div>

      <div class="grid grid-cols-3 gap-6" id="subjects-cards-outlet"></div>
    `;
  }

  static renderSubjectCard(subject, courseCount = 0) {
    const isActive = subject.is_active === 1 || subject.is_active === true;
    return `
      <div class="card card-hover" style="display: flex; flex-direction: column;">
        <div class="card-body" style="flex: 1;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem;">
            <div style="width: 40px; height: 40px; border-radius: var(--radius-lg); background: var(--primary-100); color: var(--primary-600); display: flex; align-items: center; justify-content: center;">
              ${icon('layers', { size: 20 })}
            </div>
            <label class="form-switch" title="Trạng thái hiển thị">
              <input type="checkbox" class="subject-status-switch" data-id="${subject.id}" ${isActive ? 'checked' : ''}>
              <span class="switch-slider"></span>
            </label>
          </div>

          <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem; color: var(--text-primary);">
            ${escapeHtml(subject.name)}
          </h3>

          <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5; margin-bottom: 1rem; min-height: 40px;">
            ${escapeHtml(subject.description || 'Chưa có mô tả chi tiết.')}
          </p>

          <div style="display: flex; align-items: center; justify-content: space-between; padding-top: 0.75rem; border-top: 1px dashed var(--border-color); font-size: 0.75rem;">
            <span style="color: var(--text-muted);">Số khóa học:</span>
            <strong class="badge badge-primary">${courseCount} khóa học</strong>
          </div>
        </div>

        <div class="card-footer" style="display: flex; justify-content: flex-end; gap: 8px;">
          <button type="button" class="btn btn-secondary btn-sm btn-edit-subject" data-id="${subject.id}">
            ${icon('edit', { size: 14 })}
            <span>Sửa</span>
          </button>
          <button type="button" class="btn btn-danger-soft btn-sm btn-delete-subject" data-id="${subject.id}">
            ${icon('trash', { size: 14 })}
            <span>Xóa</span>
          </button>
        </div>
      </div>
    `;
  }

  static renderSubjectModal(subject = null) {
    const isEdit = !!subject;
    return `
      <form id="subject-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-subject-name">
            Tên môn học <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-subject-name" 
            class="form-control" 
            placeholder="ví dụ: Tiếng Anh Thương Mại" 
            value="${isEdit ? escapeHtml(subject.name) : ''}" 
            required
          >
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-subject-desc">Mô tả tóm tắt</label>
          <textarea 
            id="modal-subject-desc" 
            class="form-control" 
            rows="3" 
            placeholder="Mô tả mục tiêu và đối tượng đào tạo của môn học..."
          >${isEdit ? escapeHtml(subject.description || '') : ''}</textarea>
        </div>

        <div class="form-group">
          <label class="form-check">
            <input type="checkbox" id="modal-subject-active" ${!isEdit || subject.is_active === 1 || subject.is_active === true ? 'checked' : ''}>
            <span>Kích hoạt môn học ngay trên hệ thống</span>
          </label>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-subject">
            ${isEdit ? 'Lưu thay đổi' : 'Tạo môn học'}
          </button>
        </div>
      </form>
    `;
  }
}
