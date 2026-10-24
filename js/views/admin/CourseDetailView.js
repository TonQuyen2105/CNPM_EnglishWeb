/**
 * CourseDetailView - Giao diện Chi tiết Khóa học (4 Tabs: Thông tin, Bài học, Lớp học, Đánh giá)
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, formatDate, renderLevelBadge, escapeHtml, renderStatusBadge } from '../../core/utils.js';

export class CourseDetailView {
  static render({ course, subjects, activeTab = 'lessons' }) {
    return `
      <!-- Breadcrumb & Back -->
      <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem;">
        <a href="#/courses" class="btn btn-secondary btn-sm">
          ${icon('arrow-left', { size: 14 })}
          <span>Trở về danh sách</span>
        </a>
        <span style="color: var(--text-muted);">/</span>
        <span style="font-weight: 600; font-size: 0.875rem;">${escapeHtml(course.title)}</span>
      </div>

      <!-- Course Header Summary Banner -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1.5rem;">
        <div style="display: flex; gap: 1.5rem; align-items: center; flex-wrap: wrap;">
          <div style="width: 120px; height: 80px; border-radius: var(--radius-lg); overflow: hidden; flex-shrink: 0;">
            <img src="${course.thumbnail_url}" alt="${escapeHtml(course.title)}" style="width: 100%; height: 100%; object-fit: cover;">
          </div>
          <div style="flex: 1; min-width: 260px;">
            <div style="display: flex; gap: 6px; align-items: center; margin-bottom: 4px;">
              <span class="badge badge-primary">${escapeHtml(course.subject_name)}</span>
              ${renderLevelBadge(course.level)}
              ${course.is_published 
                ? '<span class="badge badge-success">Đang mở</span>' 
                : '<span class="badge badge-neutral">Bản nháp</span>'}
            </div>
            <h2 style="font-size: 1.375rem; font-weight: 800; margin-bottom: 4px;">${escapeHtml(course.title)}</h2>
            <div style="font-size: 0.8125rem; color: var(--text-secondary);">
              Học phí: <strong style="color: var(--primary-600);">${formatCurrency(course.price)}</strong> • 
              Tổng số bài học: <strong>${course.lessons.length}</strong> • 
              Đánh giá: <strong>★ ${course.rating_avg}</strong> (${course.review_count} lượt)
            </div>
          </div>
        </div>
      </div>

      <!-- 4 Tabs Navigation -->
      <div class="tabs-nav">
        <button type="button" class="tab-btn ${activeTab === 'lessons' ? 'active' : ''}" data-tab="lessons">
          ${icon('book-open', { size: 16 })}
          <span>Chương trình Bài học (${course.lessons.length})</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'info' ? 'active' : ''}" data-tab="info">
          ${icon('edit', { size: 16 })}
          <span>Thông tin khóa học</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'classes' ? 'active' : ''}" data-tab="classes">
          ${icon('layers', { size: 16 })}
          <span>Lớp học liên kết</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'reviews' ? 'active' : ''}" data-tab="reviews">
          ${icon('star', { size: 16 })}
          <span>Đánh giá học viên (${course.review_count})</span>
        </button>
      </div>

      <!-- Tab Content Outlet -->
      <div id="course-tab-content">
        ${activeTab === 'lessons' ? this.renderLessonsTab(course) : ''}
        ${activeTab === 'info' ? this.renderInfoTab(course, subjects) : ''}
        ${activeTab === 'classes' ? this.renderClassesTab(course) : ''}
        ${activeTab === 'reviews' ? this.renderReviewsTab(course) : ''}
      </div>
    `;
  }

  // TAB 1: BÀI HỌC (ĐỔI THỨ TỰ ORDER_INDEX, LỌC CONTENT_TYPE, CRUD BÀI HỌC)
  static renderLessonsTab(course) {
    const lessons = course.lessons || [];
    return `
      <div class="card">
        <div class="card-header" style="flex-wrap: wrap; gap: 0.75rem;">
          <div style="display: flex; align-items: center; gap: 0.75rem;">
            <h3 class="card-title">Danh sách bài học (${lessons.length})</h3>
            <select id="lesson-type-filter" class="form-control" style="width: auto; height: 34px; padding: 2px 28px 2px 10px; font-size: 0.75rem;">
              <option value="all">Tất cả loại bài</option>
              <option value="vocabulary">Từ vựng (Vocabulary)</option>
              <option value="grammar">Ngữ pháp (Grammar)</option>
              <option value="listening">Luyện nghe (Listening)</option>
              <option value="reading">Đọc hiểu (Reading)</option>
              <option value="speaking">Nói & Phát âm (Speaking)</option>
              <option value="writing">Viết luận (Writing)</option>
              <option value="quiz">Trắc nghiệm (Quiz)</option>
            </select>
          </div>
          <button type="button" class="btn btn-primary btn-sm" id="btn-add-lesson">
            ${icon('plus', { size: 14 })}
            <span>Thêm bài học mới</span>
          </button>
        </div>

        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table" id="lessons-table">
            <thead>
              <tr>
                <th style="width: 80px;">Thứ tự</th>
                <th>Tên bài học</th>
                <th>Phân loại kỹ năng</th>
                <th>Ngày tạo</th>
                <th style="text-align: right;">Thao tác</th>
              </tr>
            </thead>
            <tbody id="lessons-tbody">
              ${lessons.length === 0 ? `
                <tr><td colspan="5" class="text-center p-6 text-muted">Chưa có bài học nào trong khóa này.</td></tr>
              ` : ''}
              ${lessons.map((l, idx) => `
                <tr class="lesson-row" data-id="${l.id}" data-type="${l.content_type}" data-order="${l.order_index}">
                  <td>
                    <div style="display: flex; align-items: center; gap: 4px;">
                      <span class="badge badge-neutral" style="font-weight: 700;">#${l.order_index}</span>
                      <div style="display: flex; flex-direction: column; gap: 1px;">
                        <button type="button" class="btn-icon btn-move-lesson-up" data-id="${l.id}" title="Đưa lên" ${idx === 0 ? 'disabled' : ''} style="padding: 1px;">
                          ${icon('chevron-up', { size: 12 })}
                        </button>
                        <button type="button" class="btn-icon btn-move-lesson-down" data-id="${l.id}" title="Đưa xuống" ${idx === lessons.length - 1 ? 'disabled' : ''} style="padding: 1px;">
                          ${icon('chevron-down', { size: 12 })}
                        </button>
                      </div>
                    </div>
                  </td>
                  <td>
                    <strong>${escapeHtml(l.title)}</strong>
                    <div style="font-size: 0.75rem; color: var(--text-muted); max-width: 450px;" class="text-truncate">
                      ${escapeHtml(l.content || '')}
                    </div>
                  </td>
                  <td>
                    <span class="badge ${this.getContentTypeBadgeClass(l.content_type)}">
                      ${l.content_type.toUpperCase()}
                    </span>
                  </td>
                  <td>${formatDate(l.created_at)}</td>
                  <td style="text-align: right;">
                    <button type="button" class="btn btn-secondary btn-sm btn-edit-lesson" data-id="${l.id}">
                      ${icon('edit', { size: 14 })}
                    </button>
                    <button type="button" class="btn btn-danger-soft btn-sm btn-delete-lesson" data-id="${l.id}">
                      ${icon('trash', { size: 14 })}
                    </button>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  static getContentTypeBadgeClass(type) {
    switch (type) {
      case 'vocabulary': return 'badge-primary';
      case 'grammar': return 'badge-info';
      case 'listening': return 'badge-warning';
      case 'speaking': return 'badge-success';
      case 'reading': return 'badge-primary';
      case 'writing': return 'badge-danger';
      case 'quiz': return 'badge-neutral';
      default: return 'badge-neutral';
    }
  }

  // TAB 2: THÔNG TIN KHÓA HỌC
  static renderInfoTab(course, subjects) {
    return `
      <div class="card" style="max-width: 800px; padding: 1.5rem;">
        <h3 class="card-title" style="margin-bottom: 1.25rem;">Chỉnh sửa thông tin khóa học</h3>
        <form id="course-info-form">
          <div class="form-group">
            <label class="form-label" for="edit-course-title">Tên khóa học <span class="required">*</span></label>
            <input type="text" id="edit-course-title" class="form-control" value="${escapeHtml(course.title)}" required>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="form-group">
              <label class="form-label" for="edit-course-subject">Môn học</label>
              <select id="edit-course-subject" class="form-control">
                ${subjects.map(s => `
                  <option value="${s.id}" ${s.id === course.subject_id ? 'selected' : ''}>
                    ${escapeHtml(s.name)}
                  </option>
                `).join('')}
              </select>
            </div>

            <div class="form-group">
              <label class="form-label" for="edit-course-level">Trình độ</label>
              <select id="edit-course-level" class="form-control">
                <option value="beginner" ${course.level === 'beginner' ? 'selected' : ''}>Cơ bản (Beginner)</option>
                <option value="intermediate" ${course.level === 'intermediate' ? 'selected' : ''}>Trung cấp (Intermediate)</option>
                <option value="advanced" ${course.level === 'advanced' ? 'selected' : ''}>Nâng cao (Advanced)</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="form-group">
              <label class="form-label" for="edit-course-price">Học phí (VND)</label>
              <input type="number" id="edit-course-price" class="form-control" value="${course.price}" required>
            </div>

            <div class="form-group">
              <label class="form-label" for="edit-course-thumbnail">URL Ảnh bìa</label>
              <input type="url" id="edit-course-thumbnail" class="form-control" value="${escapeHtml(course.thumbnail_url || '')}">
            </div>
          </div>

          <div class="form-group">
            <label class="form-label" for="edit-course-desc">Mô tả chi tiết</label>
            <textarea id="edit-course-desc" class="form-control" rows="4">${escapeHtml(course.description || '')}</textarea>
          </div>

          <div class="form-group">
            <label class="form-check">
              <input type="checkbox" id="edit-course-published" ${course.is_published ? 'checked' : ''}>
              <span style="font-weight: 600;">Xuất bản khóa học trên trang chủ</span>
            </label>
          </div>

          <div style="display: flex; gap: 0.75rem; justify-content: flex-end; margin-top: 1rem;">
            <button type="submit" class="btn btn-primary" id="btn-save-course-info">Lưu cập nhật</button>
          </div>
        </form>
      </div>
    `;
  }

  // TAB 3: CÁC LỚP HỌC THUỘC KHÓA NÀY
  static renderClassesTab(course) {
    const classes = course.classes || [];
    return `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Các lớp học thuộc khóa này (${classes.length})</h3>
          <a href="#/classes" class="btn btn-primary btn-sm">Mở lớp mới</a>
        </div>
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Mã lớp</th>
                <th>Tên lớp</th>
                <th>Thời gian</th>
                <th>Sĩ số</th>
                <th>Trạng thái</th>
                <th style="text-align: right;">Chi tiết</th>
              </tr>
            </thead>
            <tbody>
              ${classes.length === 0 ? `
                <tr><td colspan="6" class="text-center p-6 text-muted">Chưa có lớp nào được mở cho khóa này.</td></tr>
              ` : ''}
              ${classes.map(c => `
                <tr>
                  <td><strong>${c.class_code}</strong></td>
                  <td>${escapeHtml(c.class_name)}</td>
                  <td>${formatDate(c.start_date)} - ${formatDate(c.end_date)}</td>
                  <td>${c.enrolled_count || 0}/${c.max_students}</td>
                  <td>${renderStatusBadge(c.status)}</td>
                  <td style="text-align: right;">
                    <a href="#/classes/${c.id}" class="btn btn-secondary btn-sm">Xem lớp &rarr;</a>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // TAB 4: ĐÁNH GIÁ (REVIEWS)
  static renderReviewsTab(course) {
    const reviews = course.reviews || [];
    return `
      <div class="card" style="padding: 1.5rem;">
        <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h3 class="card-title">Đánh giá từ học viên (${reviews.length})</h3>
            <div class="card-subtitle">Phản hồi thực tế sau quá trình hoàn thành khóa học</div>
          </div>
          <div style="display: flex; align-items: center; gap: 0.5rem; background: var(--warning-bg); padding: 0.5rem 1rem; border-radius: var(--radius-lg); border: 1px solid var(--warning-border);">
            <span style="font-size: 1.5rem; font-weight: 800; color: var(--warning-text);">${course.rating_avg}</span>
            <div style="color: var(--warning-main); font-size: 1rem;">★★★★★</div>
          </div>
        </div>

        ${reviews.length === 0 ? `
          <div class="text-center p-8 text-muted">Chưa có đánh giá nào cho khóa học này.</div>
        ` : `
          <div class="flex flex-col gap-4">
            ${reviews.map(r => `
              <div style="padding: 1rem; border: 1px solid var(--border-color); border-radius: var(--radius-lg); background: var(--bg-surface);">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                  <div style="display: flex; align-items: center; gap: 0.5rem;">
                    <div class="avatar avatar-xs">${r.user_name ? r.user_name.charAt(0) : 'U'}</div>
                    <strong style="font-size: 0.875rem;">${escapeHtml(r.user_name || 'Học viên')}</strong>
                  </div>
                  <div style="display: flex; align-items: center; gap: 6px;">
                    <span style="color: var(--warning-main); font-size: 0.8125rem;">${'★'.repeat(r.rating)}${'☆'.repeat(5 - r.rating)}</span>
                    <small style="color: var(--text-muted);">${formatDate(r.created_at)}</small>
                  </div>
                </div>
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin: 0; line-height: 1.5;">
                  ${escapeHtml(r.comment)}
                </p>
              </div>
            `).join('')}
          </div>
        `}
      </div>
    `;
  }

  static renderLessonModal(courseId, lesson = null) {
    const isEdit = !!lesson;
    return `
      <form id="lesson-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-lesson-title">
            Tên bài học <span class="required">*</span>
          </label>
          <input 
            type="text" 
            id="modal-lesson-title" 
            class="form-control" 
            placeholder="ví dụ: Bài 1: Từ vựng mở rộng & Phản xạ" 
            value="${isEdit ? escapeHtml(lesson.title) : ''}" 
            required
          >
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-lesson-type">Loại bài học</label>
            <select id="modal-lesson-type" class="form-control">
              <option value="vocabulary" ${isEdit && lesson.content_type === 'vocabulary' ? 'selected' : ''}>Từ vựng (Vocabulary)</option>
              <option value="grammar" ${isEdit && lesson.content_type === 'grammar' ? 'selected' : ''}>Ngữ pháp (Grammar)</option>
              <option value="listening" ${isEdit && lesson.content_type === 'listening' ? 'selected' : ''}>Luyện nghe (Listening)</option>
              <option value="reading" ${isEdit && lesson.content_type === 'reading' ? 'selected' : ''}>Đọc hiểu (Reading)</option>
              <option value="speaking" ${isEdit && lesson.content_type === 'speaking' ? 'selected' : ''}>Nói & Phát âm (Speaking)</option>
              <option value="writing" ${isEdit && lesson.content_type === 'writing' ? 'selected' : ''}>Viết luận (Writing)</option>
              <option value="quiz" ${isEdit && lesson.content_type === 'quiz' ? 'selected' : ''}>Kiểm tra (Quiz)</option>
            </select>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-lesson-order">Thứ tự bài học</label>
            <input 
              type="number" 
              id="modal-lesson-order" 
              class="form-control" 
              value="${isEdit ? lesson.order_index : 1}" 
              min="1"
            >
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-lesson-content">Nội dung bài giảng & Tài liệu</label>
          <textarea 
            id="modal-lesson-content" 
            class="form-control" 
            rows="5" 
            placeholder="Nội dung tóm tắt kiến thức, tài liệu đọc hoặc link audio/video..."
          >${isEdit ? escapeHtml(lesson.content || '') : ''}</textarea>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-lesson">
            ${isEdit ? 'Lưu thay đổi' : 'Thêm bài học'}
          </button>
        </div>
      </form>
    `;
  }
}
