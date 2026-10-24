/**
 * TeacherLessonsView - Giao diện Soạn bài giảng & Quản lý bài học
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherLessonsView {
  /**
   * Render trang quản lý bài giảng
   * @param {Object} data 
   * @returns {string}
   */
  static render(data) {
    const { courses, selectedCourseId, lessons, selectedType } = data;

    const contentTypes = [
      { id: 'all', label: 'Tất cả loại' },
      { id: 'vocabulary', label: 'Từ vựng (Vocabulary)' },
      { id: 'grammar', label: 'Ngữ pháp (Grammar)' },
      { id: 'listening', label: 'Nghe (Listening)' },
      { id: 'reading', label: 'Đọc (Reading)' },
      { id: 'speaking', label: 'Nói (Speaking)' },
      { id: 'writing', label: 'Viết (Writing)' },
      { id: 'quiz', label: 'Bài kiểm tra (Quiz)' }
    ];

    return `
      <div class="teacher-lessons">
        <!-- Header -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Soạn Thảo Giáo Trình & Bài Giảng
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Biên tập giáo trình số, nội dung bài học kèm xem trước trực quan
            </p>
          </div>

          <button type="button" class="btn btn-primary btn-create-lesson" style="font-size: 0.8125rem;">
            <span class="btn-icon-left">${icon('plus', { size: 16 })}</span> Soạn bài học mới
          </button>
        </div>

        <!-- Filter Bar: Chọn Khóa học + Lọc Content Type -->
        <div class="card" style="padding: 1rem 1.25rem; margin-bottom: 1.5rem;">
          <div style="display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;">
            <!-- Select Course -->
            <div style="flex: 1; min-width: 250px;">
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Khóa học:</label>
              <select id="filter-course" class="form-control" style="font-size: 0.8125rem;">
                ${courses.map(c => `
                  <option value="${c.id}" ${String(c.id) === String(selectedCourseId) ? 'selected' : ''}>
                    ${escapeHtml(c.title)} (${c.level ? c.level.toUpperCase() : 'ALL'})
                  </option>
                `).join('')}
              </select>
            </div>

            <!-- Select Content Type -->
            <div style="min-width: 200px;">
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Loại nội dung:</label>
              <select id="filter-type" class="form-control" style="font-size: 0.8125rem;">
                ${contentTypes.map(t => `
                  <option value="${t.id}" ${selectedType === t.id ? 'selected' : ''}>${t.label}</option>
                `).join('')}
              </select>
            </div>
          </div>
        </div>

        <!-- Danh sách bài học theo thứ tự (order_index) -->
        <div class="card" style="padding: 0; overflow: hidden;">
          <div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border-color); display: flex; justify-content: space-between; align-items: center;">
            <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700;">
              Danh sách bài học (${lessons.length} bài)
            </h3>
            <span style="font-size: 0.75rem; color: var(--text-muted);">Sắp xếp theo thứ tự giảng dạy</span>
          </div>

          ${lessons.length > 0 ? `
            <div class="table-responsive">
              <table class="table" style="margin: 0;">
                <thead>
                  <tr>
                    <th style="width: 70px; text-align: center;">STT</th>
                    <th>Tên bài giảng</th>
                    <th>Thể loại</th>
                    <th>Xem trước nội dung tóm tắt</th>
                    <th style="width: 140px; text-align: right;">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  ${lessons.map(lesson => `
                    <tr>
                      <td style="text-align: center;">
                        <span class="badge badge-secondary" style="font-weight: 800; font-size: 0.8125rem;">
                          #${lesson.order_index}
                        </span>
                      </td>
                      <td>
                        <strong style="color: var(--text-primary); font-size: 0.875rem;">${escapeHtml(lesson.title)}</strong>
                        <div style="font-size: 0.7rem; color: var(--text-muted); margin-top: 2px;">
                          Cập nhật: ${formatDate(lesson.updated_at || lesson.created_at)}
                        </div>
                      </td>
                      <td>
                        <span class="badge ${TeacherLessonsView.getTypeBadgeClass(lesson.content_type)}" style="font-size: 0.7rem;">
                          ${escapeHtml(lesson.content_type)}
                        </span>
                      </td>
                      <td style="color: var(--text-secondary); font-size: 0.8125rem; max-width: 380px;">
                        <div style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                          ${escapeHtml(lesson.content || 'Chưa có nội dung chi tiết')}
                        </div>
                      </td>
                      <td style="text-align: right;">
                        <div style="display: flex; gap: 4px; justify-content: flex-end;">
                          <button type="button" class="btn btn-sm btn-outline btn-edit-lesson" data-lesson-id="${lesson.id}" title="Chỉnh sửa bài giảng" style="padding: 4px 8px; font-size: 0.75rem;">
                            ${icon('edit', { size: 14 })} Soạn bài
                          </button>
                        </div>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          ` : `
            <div style="text-align: center; padding: 3rem; color: var(--text-muted);">
              <div style="color: var(--neutral-400); margin-bottom: 0.75rem;">${icon('book-open', { size: 40 })}</div>
              <h4 style="font-size: 1rem; font-weight: 700; margin-bottom: 0.25rem;">Chưa có bài học nào</h4>
              <p style="font-size: 0.8125rem; margin-bottom: 1rem;">Khóa học này chưa có nội dung bài giảng thuộc thể loại đã lọc.</p>
              <button type="button" class="btn btn-sm btn-primary btn-create-lesson">
                + Thêm bài giảng đầu tiên
              </button>
            </div>
          `}
        </div>
      </div>
    `;
  }

  static getTypeBadgeClass(type) {
    switch (type) {
      case 'vocabulary': return 'badge-primary';
      case 'grammar': return 'badge-info';
      case 'listening': return 'badge-warning';
      case 'reading': return 'badge-success';
      case 'speaking': return 'badge-danger';
      case 'writing': return 'badge-secondary';
      case 'quiz': return 'badge-primary';
      default: return 'badge-secondary';
    }
  }

  /**
   * Render HTML cho form soạn thảo bài giảng có Live Preview
   * @param {Object} lesson 
   * @param {number} nextOrderIndex 
   * @returns {string}
   */
  static renderEditorForm(lesson = null, nextOrderIndex = 1) {
    const isEdit = !!lesson;
    const title = lesson ? lesson.title : '';
    const orderIndex = lesson ? lesson.order_index : nextOrderIndex;
    const contentType = lesson ? lesson.content_type : 'vocabulary';
    const content = lesson ? lesson.content : '';

    return `
      <form id="form-lesson-editor" style="display: flex; flex-direction: column; gap: 1rem;">
        <div class="grid grid-cols-3 gap-4">
          <div style="grid-column: span 2;">
            <label class="form-label">Tên bài học: <span class="text-danger">*</span></label>
            <input type="text" id="lesson-title-input" class="form-control" required value="${escapeHtml(title)}" placeholder="VD: Lesson 1: Greetings & Daily Introductions">
          </div>
          <div>
            <label class="form-label">Thứ tự bài: <span class="text-danger">*</span></label>
            <input type="number" id="lesson-order-input" class="form-control" min="1" required value="${orderIndex}">
          </div>
        </div>

        <div>
          <label class="form-label">Loại nội dung giảng dạy:</label>
          <select id="lesson-type-input" class="form-control">
            <option value="vocabulary" ${contentType === 'vocabulary' ? 'selected' : ''}>Từ vựng (Vocabulary)</option>
            <option value="grammar" ${contentType === 'grammar' ? 'selected' : ''}>Ngữ pháp (Grammar)</option>
            <option value="listening" ${contentType === 'listening' ? 'selected' : ''}>Nghe (Listening)</option>
            <option value="reading" ${contentType === 'reading' ? 'selected' : ''}>Đọc (Reading)</option>
            <option value="speaking" ${contentType === 'speaking' ? 'selected' : ''}>Nói (Speaking)</option>
            <option value="writing" ${contentType === 'writing' ? 'selected' : ''}>Viết (Writing)</option>
            <option value="quiz" ${contentType === 'quiz' ? 'selected' : ''}>Bài tập ôn tập (Quiz)</option>
          </select>
        </div>

        <!-- Editor 2 Cột: Textarea + Live Preview -->
        <div>
          <label class="form-label" style="display: flex; justify-content: space-between;">
            <span>Nội dung bài giảng (Hỗ trợ văn bản & ký hiệu định dạng):</span>
            <span style="font-size: 0.75rem; color: var(--primary-600); font-weight: normal;">Live Preview hiển thị ngay bên dưới</span>
          </label>
          
          <div class="grid grid-cols-2 gap-4">
            <!-- Cột trái: Textarea -->
            <div>
              <textarea id="lesson-content-input" class="form-control" rows="10" placeholder="Soạn nội dung giáo án tại đây...
Ví dụ:
1. Mục tiêu bài học:
- Nắm vững 10 từ vựng chào hỏi
- Tự tin giới thiệu bản thân

2. Từ vựng trọng tâm:
- Hello / Hi (Xin chào)
- How are you? (Bạn có khỏe không?)

3. Bài tập thực hành:
Thực hành đóng vai theo cặp.">${escapeHtml(content)}</textarea>
            </div>

            <!-- Cột phải: Live Preview Box -->
            <div style="border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 0.875rem 1rem; background-color: var(--neutral-50); overflow-y: auto; max-height: 250px;">
              <div style="font-size: 0.75rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; margin-bottom: 0.5rem; letter-spacing: 0.05em;">
                Xem trước trực tiếp (Live Preview)
              </div>
              <div id="lesson-live-preview" style="font-size: 0.875rem; line-height: 1.6; white-space: pre-wrap; color: var(--text-primary);">
                ${escapeHtml(content) || '<span style="color: var(--text-muted); font-style: italic;">Gõ nội dung vào ô bên trái để xem trước...</span>'}
              </div>
            </div>
          </div>
        </div>
      </form>
    `;
  }
}
