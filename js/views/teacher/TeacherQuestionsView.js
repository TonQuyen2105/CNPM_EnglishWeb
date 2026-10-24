/**
 * TeacherQuestionsView - Giao diện Ngân hàng Câu hỏi & Form động 4 loại câu
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml } from '../../core/utils.js';

export class TeacherQuestionsView {
  static render(data) {
    const { questions, lessons, selectedType, selectedLessonId, searchQuery } = data;

    const questionTypes = [
      { id: 'all', label: 'Tất cả loại câu hỏi' },
      { id: 'multiple_choice', label: 'Trắc nghiệm (Multiple Choice)' },
      { id: 'fill_blank', label: 'Điền từ (Fill Blank)' },
      { id: 'pronunciation', label: 'Phát âm (Pronunciation)' },
      { id: 'essay', label: 'Tự luận / Viết bài (Essay)' }
    ];

    return `
      <div class="teacher-questions">
        <!-- Header -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Ngân Hàng Câu Hỏi (${questions.length})
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Kho dữ liệu câu hỏi trắc nghiệm, điền khuyết, phát âm và tự luận để tạo đề thi
            </p>
          </div>

          <button type="button" class="btn btn-primary btn-create-question" style="font-size: 0.8125rem;">
            <span class="btn-icon-left">${icon('plus', { size: 16 })}</span> Thêm câu hỏi mới
          </button>
        </div>

        <!-- Filter Bar: Search + Type + Lesson -->
        <div class="card" style="padding: 1rem 1.25rem; margin-bottom: 1.5rem;">
          <div class="grid grid-cols-3 gap-4">
            <!-- Search -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Tìm kiếm đề bài:</label>
              <input type="text" id="filter-search" class="form-control" style="font-size: 0.8125rem;" placeholder="Nhập từ khóa câu hỏi..." value="${escapeHtml(searchQuery || '')}">
            </div>

            <!-- Type -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Loại câu hỏi:</label>
              <select id="filter-type" class="form-control" style="font-size: 0.8125rem;">
                ${questionTypes.map(t => `
                  <option value="${t.id}" ${selectedType === t.id ? 'selected' : ''}>${t.label}</option>
                `).join('')}
              </select>
            </div>

            <!-- Lesson -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Theo bài học:</label>
              <select id="filter-lesson" class="form-control" style="font-size: 0.8125rem;">
                <option value="all">Tất cả bài học</option>
                ${lessons.map(l => `
                  <option value="${l.id}" ${String(l.id) === String(selectedLessonId) ? 'selected' : ''}>
                    Bài ${l.order_index}: ${escapeHtml(l.title)}
                  </option>
                `).join('')}
              </select>
            </div>
          </div>
        </div>

        <!-- Table Danh sách câu hỏi -->
        <div class="card" style="padding: 0; overflow: hidden;">
          ${questions.length > 0 ? `
            <div class="table-responsive">
              <table class="table" style="margin: 0;">
                <thead>
                  <tr>
                    <th style="width: 60px; text-align: center;">Mã</th>
                    <th>Nội dung câu hỏi & Chi tiết đáp án</th>
                    <th style="width: 140px;">Loại câu</th>
                    <th style="width: 150px;">Bài học liên kết</th>
                    <th style="width: 80px; text-align: center;">Điểm</th>
                    <th style="width: 110px; text-align: right;">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  ${questions.map(q => `
                    <tr>
                      <td style="text-align: center; font-weight: 700; color: var(--text-muted);">
                        #${q.id}
                      </td>
                      <td>
                        <div style="font-weight: 600; color: var(--text-primary); margin-bottom: 4px; font-size: 0.875rem;">
                          ${escapeHtml(q.question_text)}
                        </div>

                        <!-- Render tóm tắt đáp án theo loại -->
                        ${TeacherQuestionsView.renderAnswerPreview(q)}
                      </td>
                      <td>
                        <span class="badge ${TeacherQuestionsView.getBadgeByType(q.question_type)}" style="font-size: 0.7rem;">
                          ${TeacherQuestionsView.getTypeLabel(q.question_type)}
                        </span>
                      </td>
                      <td style="font-size: 0.8125rem; color: var(--text-secondary);">
                        ${escapeHtml(q.lesson_title || 'Bài học #' + q.lesson_id)}
                      </td>
                      <td style="text-align: center; font-weight: 700; color: var(--primary-600); font-size: 0.8125rem;">
                        ${q.default_points || 10} đ
                      </td>
                      <td style="text-align: right;">
                        <div style="display: flex; gap: 4px; justify-content: flex-end;">
                          <button type="button" class="btn btn-sm btn-icon btn-edit-question" data-question-id="${q.id}" title="Chỉnh sửa">
                            ${icon('edit', { size: 14 })}
                          </button>
                          <button type="button" class="btn btn-sm btn-icon text-danger btn-delete-question" data-question-id="${q.id}" title="Xóa câu hỏi">
                            ${icon('trash', { size: 14 })}
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
              <div style="color: var(--neutral-400); margin-bottom: 0.75rem;">${icon('file-text', { size: 40 })}</div>
              <h4 style="font-size: 1rem; font-weight: 700; margin-bottom: 0.25rem;">Không tìm thấy câu hỏi</h4>
              <p style="font-size: 0.8125rem; margin-bottom: 1rem;">Không có câu hỏi nào khớp với bộ lọc hoặc từ khóa tìm kiếm.</p>
              <button type="button" class="btn btn-sm btn-primary btn-create-question">
                + Tạo câu hỏi mới
              </button>
            </div>
          `}
        </div>
      </div>
    `;
  }

  static renderAnswerPreview(q) {
    if (q.question_type === 'multiple_choice' && q.options && q.options.length > 0) {
      return `
        <div style="display: flex; flex-wrap: wrap; gap: 6px; margin-top: 4px;">
          ${q.options.map(opt => `
            <span style="font-size: 0.75rem; padding: 2px 8px; border-radius: 4px; border: 1px solid ${opt.is_correct ? '#86EFAC' : 'var(--border-color)'}; background: ${opt.is_correct ? '#F0FDF4' : 'white'}; color: ${opt.is_correct ? '#15803D' : 'var(--text-secondary)'}; font-weight: ${opt.is_correct ? '700' : 'normal'};">
              ${opt.is_correct ? '✓ ' : ''}${escapeHtml(opt.option_text)}
            </span>
          `).join('')}
        </div>
      `;
    }

    if (q.question_type === 'fill_blank' && q.answers && q.answers.length > 0) {
      return `
        <div style="font-size: 0.75rem; color: var(--text-secondary); margin-top: 4px;">
          <strong>Đáp án đúng:</strong> 
          ${q.answers.map(a => `<code style="background: var(--neutral-100); padding: 1px 4px; border-radius: 3px; font-weight: 700; color: var(--primary-600);">${escapeHtml(a.answer_text)}</code>`).join(', ')}
        </div>
      `;
    }

    if (q.question_type === 'pronunciation') {
      return `
        <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 4px; display: flex; align-items: center; gap: 6px;">
          <span>${icon('volume', { size: 12 })}</span>
          <span>Kiểm tra phát âm và độ chuẩn xác ngữ điệu</span>
        </div>
      `;
    }

    if (q.question_type === 'essay') {
      return `
        <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 4px; display: flex; align-items: center; gap: 6px;">
          <span>${icon('edit', { size: 12 })}</span>
          <span>Bài tập viết luận - Giáo viên chấm điểm và nhận xét thủ công</span>
        </div>
      `;
    }

    return '';
  }

  static getBadgeByType(type) {
    switch (type) {
      case 'multiple_choice': return 'badge-primary';
      case 'fill_blank': return 'badge-info';
      case 'pronunciation': return 'badge-warning';
      case 'essay': return 'badge-secondary';
      default: return 'badge-secondary';
    }
  }

  static getTypeLabel(type) {
    switch (type) {
      case 'multiple_choice': return 'Trắc nghiệm';
      case 'fill_blank': return 'Điền từ';
      case 'pronunciation': return 'Phát âm';
      case 'essay': return 'Tự luận';
      default: return type;
    }
  }

  /**
   * Render HTML cho Modal Form Động theo Question Type
   * @param {Object} q 
   * @param {Array} lessons 
   * @returns {string}
   */
  static renderQuestionForm(q = null, lessons = []) {
    const isEdit = !!q;
    const type = q ? q.question_type : 'multiple_choice';
    const text = q ? q.question_text : '';
    const points = q ? (q.default_points || 10) : 10;
    const lessonId = q ? q.lesson_id : (lessons[0] ? lessons[0].id : 1);
    const audioUrl = q ? (q.audio_url || '') : '';

    return `
      <form id="form-question-dynamic" style="display: flex; flex-direction: column; gap: 1rem;">
        <div class="grid grid-cols-3 gap-4">
          <!-- Loại câu hỏi -->
          <div>
            <label class="form-label">Loại câu hỏi: <span class="text-danger">*</span></label>
            <select id="modal-question-type" class="form-control" ${isEdit ? 'disabled' : ''}>
              <option value="multiple_choice" ${type === 'multiple_choice' ? 'selected' : ''}>Trắc nghiệm (Multiple Choice)</option>
              <option value="fill_blank" ${type === 'fill_blank' ? 'selected' : ''}>Điền từ (Fill Blank)</option>
              <option value="pronunciation" ${type === 'pronunciation' ? 'selected' : ''}>Phát âm (Pronunciation)</option>
              <option value="essay" ${type === 'essay' ? 'selected' : ''}>Tự luận (Essay)</option>
            </select>
          </div>

          <!-- Bài học liên kết -->
          <div>
            <label class="form-label">Bài học liên kết:</label>
            <select id="modal-question-lesson" class="form-control">
              ${lessons.map(l => `
                <option value="${l.id}" ${String(l.id) === String(lessonId) ? 'selected' : ''}>
                  Bài ${l.order_index}: ${escapeHtml(l.title)}
                </option>
              `).join('')}
            </select>
          </div>

          <!-- Điểm mặc định -->
          <div>
            <label class="form-label">Điểm mặc định: <span class="text-danger">*</span></label>
            <input type="number" id="modal-question-points" class="form-control" min="1" max="100" required value="${points}">
          </div>
        </div>

        <!-- Đề bài câu hỏi -->
        <div>
          <label class="form-label">Nội dung đề bài / Yêu cầu: <span class="text-danger">*</span></label>
          <textarea id="modal-question-text" class="form-control" rows="3" required placeholder="Nhập câu hỏi hoặc đề bài luận...">${escapeHtml(text)}</textarea>
        </div>

        <!-- Section 1: Multiple Choice Options -->
        <div id="section-multiple-choice" class="dynamic-type-section" style="display: ${type === 'multiple_choice' ? 'block' : 'none'}; border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; background: var(--neutral-50);">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
            <label class="form-label" style="margin: 0; font-weight: 700;">Các lựa chọn đáp án (Tick chọn phương án đúng):</label>
            <button type="button" class="btn btn-sm btn-outline btn-add-option" style="font-size: 0.75rem;">
              + Thêm lựa chọn
            </button>
          </div>
          <div id="mc-options-list" style="display: flex; flex-direction: column; gap: 0.5rem;">
            ${q && q.options && q.options.length > 0 ? q.options.map((opt, i) => `
              <div class="mc-option-row" style="display: flex; align-items: center; gap: 8px;">
                <input type="radio" name="mc_correct" class="mc-correct-radio" ${opt.is_correct ? 'checked' : ''} title="Đánh dấu là đáp án đúng">
                <input type="text" class="form-control form-control-sm mc-text-input" placeholder="Nội dung lựa chọn..." value="${escapeHtml(opt.option_text)}">
                <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-option" title="Xóa">✕</button>
              </div>
            `).join('') : `
              <div class="mc-option-row" style="display: flex; align-items: center; gap: 8px;">
                <input type="radio" name="mc_correct" class="mc-correct-radio" checked title="Đánh dấu là đáp án đúng">
                <input type="text" class="form-control form-control-sm mc-text-input" placeholder="Lựa chọn A..." value="">
                <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-option" title="Xóa">✕</button>
              </div>
              <div class="mc-option-row" style="display: flex; align-items: center; gap: 8px;">
                <input type="radio" name="mc_correct" class="mc-correct-radio" title="Đánh dấu là đáp án đúng">
                <input type="text" class="form-control form-control-sm mc-text-input" placeholder="Lựa chọn B..." value="">
                <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-option" title="Xóa">✕</button>
              </div>
            `}
          </div>
        </div>

        <!-- Section 2: Fill in the Blank Answers -->
        <div id="section-fill-blank" class="dynamic-type-section" style="display: ${type === 'fill_blank' ? 'block' : 'none'}; border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; background: var(--neutral-50);">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
            <label class="form-label" style="margin: 0; font-weight: 700;">Danh sách từ/cụm từ chấp nhận đúng:</label>
            <button type="button" class="btn btn-sm btn-outline btn-add-answer" style="font-size: 0.75rem;">
              + Thêm đáp án chấp nhận
            </button>
          </div>
          <div id="fb-answers-list" style="display: flex; flex-direction: column; gap: 0.5rem;">
            ${q && q.answers && q.answers.length > 0 ? q.answers.map(ans => `
              <div class="fb-answer-row" style="display: flex; align-items: center; gap: 8px;">
                <input type="text" class="form-control form-control-sm fb-text-input" placeholder="Từ / Cụm từ đúng..." value="${escapeHtml(ans.answer_text)}">
                <label style="display: flex; align-items: center; gap: 4px; font-size: 0.75rem; white-space: nowrap; cursor: pointer;">
                  <input type="checkbox" class="fb-case-checkbox" ${ans.is_case_sensitive ? 'checked' : ''}> Phân biệt HOA/thường
                </label>
                <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-answer" title="Xóa">✕</button>
              </div>
            `).join('') : `
              <div class="fb-answer-row" style="display: flex; align-items: center; gap: 8px;">
                <input type="text" class="form-control form-control-sm fb-text-input" placeholder="Từ / Cụm từ đúng (VD: goes)..." value="">
                <label style="display: flex; align-items: center; gap: 4px; font-size: 0.75rem; white-space: nowrap; cursor: pointer;">
                  <input type="checkbox" class="fb-case-checkbox"> Phân biệt HOA/thường
                </label>
                <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-answer" title="Xóa">✕</button>
              </div>
            `}
          </div>
        </div>

        <!-- Section 3: Pronunciation Audio URLs -->
        <div id="section-pronunciation" class="dynamic-type-section" style="display: ${type === 'pronunciation' ? 'block' : 'none'}; border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; background: var(--neutral-50);">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="form-label" style="font-size: 0.8125rem;">URL File âm thanh mẫu (Native audio):</label>
              <input type="url" id="modal-audio-url" class="form-control" placeholder="https://cdn.example.com/audio/sample.mp3" value="${escapeHtml(audioUrl)}">
            </div>
            <div>
              <label class="form-label" style="font-size: 0.8125rem;">Tiêu chuẩn phát âm / Phiên âm IPA:</label>
              <input type="text" id="modal-ipa-ref" class="form-control" placeholder="VD: /prəˌnʌnsiˈeɪʃn/">
            </div>
          </div>
        </div>

        <!-- Section 4: Essay (Tự luận) -->
        <div id="section-essay" class="dynamic-type-section" style="display: ${type === 'essay' ? 'block' : 'none'}; border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; background: #FEF9C3; color: #854D0E;">
          <div style="font-size: 0.8125rem;">
            <strong>📝 Lưu ý câu hỏi tự luận:</strong> Bài nộp của học viên sẽ xuất hiện trong hàng đợi <em>Chấm bài tự luận</em>. Giáo viên sẽ trực tiếp cho điểm và nhập nhận xét chi tiết sau khi học viên nộp bài.
          </div>
        </div>
      </form>
    `;
  }
}
