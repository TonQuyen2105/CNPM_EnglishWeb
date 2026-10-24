/**
 * CoursesView - Giao diện Danh sách Khóa học (Lưới Card)
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, renderLevelBadge, escapeHtml } from '../../core/utils.js';

export class CoursesView {
  static renderLayout(subjects = []) {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý chương trình khóa học</h1>
          <p class="page-header-subtitle">Hệ thống 12 khóa học tiếng Anh chuẩn hóa theo các cấp độ</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-course">
            ${icon('plus', { size: 16 })}
            <span>Tạo khóa học mới</span>
          </button>
        </div>
      </div>

      <!-- Toolbar Lọc & Tìm Kiếm -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-4">
            <div class="input-with-icon">
              <span class="input-icon">${icon('search', { size: 16 })}</span>
              <input type="text" id="course-search-input" class="form-control" placeholder="Tìm theo tên khóa học...">
            </div>
          </div>
          <div class="col-span-3">
            <select id="course-subject-filter" class="form-control">
              <option value="all">Tất cả môn học</option>
              ${subjects.map(s => `<option value="${s.id}">${escapeHtml(s.name)}</option>`).join('')}
            </select>
          </div>
          <div class="col-span-3">
            <select id="course-level-filter" class="form-control">
              <option value="all">Tất cả trình độ</option>
              <option value="beginner">Cơ bản (Beginner)</option>
              <option value="intermediate">Trung cấp (Intermediate)</option>
              <option value="advanced">Nâng cao (Advanced)</option>
            </select>
          </div>
          <div class="col-span-2">
            <select id="course-published-filter" class="form-control">
              <option value="all">Mọi trạng thái</option>
              <option value="published">Đã xuất bản</option>
              <option value="draft">Bản nháp</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Courses Grid Cards -->
      <div id="courses-grid-outlet" class="grid grid-cols-3 gap-6"></div>
    `;
  }

  static renderCourseCard(course) {
    const isPublished = course.is_published === 1 || course.is_published === true;
    return `
      <div class="card card-hover" style="display: flex; flex-direction: column; overflow: hidden;">
        <div style="position: relative; height: 160px; overflow: hidden; background: var(--neutral-100);">
          <img 
            src="${course.thumbnail_url || 'https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=400'}" 
            alt="${escapeHtml(course.title)}" 
            style="width: 100%; height: 100%; object-fit: cover;"
          >
          <div style="position: absolute; top: 10px; left: 10px; display: flex; gap: 6px;">
            ${renderLevelBadge(course.level)}
          </div>
          <div style="position: absolute; top: 10px; right: 10px;">
            ${isPublished 
              ? `<span class="badge badge-success"><span class="badge-dot"></span>Đang mở</span>`
              : `<span class="badge badge-neutral"><span class="badge-dot"></span>Bản nháp</span>`
            }
          </div>
        </div>

        <div class="card-body" style="flex: 1; display: flex; flex-direction: column;">
          <div style="font-size: 0.75rem; color: var(--primary-600); font-weight: 700; text-transform: uppercase; margin-bottom: 4px;">
            ${escapeHtml(course.subject_name || 'Môn học')}
          </div>
          <h3 style="font-size: 1.0625rem; font-weight: 700; margin-bottom: 0.5rem; line-height: 1.3;">
            <a href="#/courses/${course.id}" style="color: var(--text-primary);">${escapeHtml(course.title)}</a>
          </h3>
          <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5; margin-bottom: 1rem; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; flex: 1;">
            ${escapeHtml(course.description || 'Chưa có mô tả khóa học.')}
          </p>

          <div style="display: flex; align-items: center; justify-content: space-between; padding-top: 0.75rem; border-top: 1px dashed var(--border-color); font-size: 0.8125rem;">
            <span style="color: var(--text-muted);">${course.lesson_count || 0} bài học</span>
            <strong style="font-size: 1.125rem; color: var(--primary-600);">${formatCurrency(course.price)}</strong>
          </div>
        </div>

        <div class="card-footer" style="display: flex; justify-content: space-between; background: var(--neutral-50);">
          <a href="#/courses/${course.id}" class="btn btn-secondary btn-sm">
            ${icon('edit', { size: 14 })}
            <span>Quản lý & Bài học</span>
          </a>
          <button type="button" class="btn ${isPublished ? 'btn-danger-soft' : 'btn-primary'} btn-sm btn-toggle-publish" data-id="${course.id}" data-published="${isPublished ? '1' : '0'}">
            ${isPublished ? 'Ẩn khóa học' : 'Xuất bản'}
          </button>
        </div>
      </div>
    `;
  }

  static renderCourseModal(subjects, course = null) {
    const isEdit = !!course;
    return `
      <form id="course-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-course-title">
            Tên khóa học <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-course-title" 
            class="form-control" 
            placeholder="ví dụ: Tiếng Anh Giao Tiếp Cơ Bản" 
            value="${isEdit ? escapeHtml(course.title) : ''}" 
            required
          >
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-course-subject">
              Môn học <span class="required">*</span>
            </label>
            <select id="modal-course-subject" class="form-control" required>
              ${subjects.map(s => `
                <option value="${s.id}" ${isEdit && course.subject_id === s.id ? 'selected' : ''}>
                  ${escapeHtml(s.name)}
                </option>
              `).join('')}
            </select>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-course-level">Trình độ đào tạo</label>
            <select id="modal-course-level" class="form-control">
              <option value="beginner" ${isEdit && course.level === 'beginner' ? 'selected' : ''}>Cơ bản (Beginner)</option>
              <option value="intermediate" ${isEdit && course.level === 'intermediate' ? 'selected' : ''}>Trung cấp (Intermediate)</option>
              <option value="advanced" ${isEdit && course.level === 'advanced' ? 'selected' : ''}>Nâng cao (Advanced)</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-course-price">Học phí (VND) <span class="required">*</span></label>
            <input 
              type="number" 
              id="modal-course-price" 
              class="form-control" 
              placeholder="ví dụ: 2500000" 
              value="${isEdit ? course.price : 2500000}" 
              required
            >
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-course-thumbnail">Link ảnh bìa (URL)</label>
            <input 
              type="url" 
              id="modal-course-thumbnail" 
              class="form-control" 
              placeholder="https://images.unsplash.com/photo-..." 
              value="${isEdit && course.thumbnail_url ? escapeHtml(course.thumbnail_url) : ''}"
            >
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-course-desc">Mô tả tóm tắt</label>
          <textarea id="modal-course-desc" class="form-control" rows="3" placeholder="Mục tiêu khóa học và kết quả đạt được...">${isEdit ? escapeHtml(course.description || '') : ''}</textarea>
        </div>

        <div class="form-group">
          <label class="form-check">
            <input type="checkbox" id="modal-course-published" ${!isEdit || course.is_published === 1 || course.is_published === true ? 'checked' : ''}>
            <span>Xuất bản khóa học để học viên có thể đăng ký</span>
          </label>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-course">
            ${isEdit ? 'Lưu thay đổi' : 'Tạo khóa học'}
          </button>
        </div>
      </form>
    `;
  }
}
