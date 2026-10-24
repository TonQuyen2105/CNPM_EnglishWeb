/**
 * TeacherExamsView - Giao diện Đề thi của tôi & Trình dựng đề 2 cột (Exam Builder)
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherExamsView {
  /**
   * Render danh sách đề thi của tôi
   */
  static renderList(exams = [], courses = []) {
    return `
      <div class="teacher-exams">
        <!-- Header -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Đề Thi Của Tôi (${exams.length})
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Quản lý và thiết kế các bộ đề thi trắc nghiệm, tự luận và đánh giá năng lực học viên
            </p>
          </div>

          <button type="button" class="btn btn-primary btn-create-exam" style="font-size: 0.8125rem;">
            <span class="btn-icon-left">${icon('plus', { size: 16 })}</span> Tạo đề thi mới
          </button>
        </div>

        ${exams.length > 0 ? `
          <div class="grid grid-cols-3 gap-6">
            ${exams.map(e => `
              <div class="card" style="display: flex; flex-direction: column; justify-content: space-between; border: 1px solid var(--border-color); border-radius: var(--radius-lg); overflow: hidden; transition: all var(--transition-fast);" class="hover-shadow">
                <!-- Card Header -->
                <div style="padding: 1.25rem; border-bottom: 1px solid var(--border-color);">
                  <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.5rem;">
                    <span class="badge badge-info" style="font-size: 0.7rem; font-weight: 700;">
                      ${escapeHtml(e.course_title)}
                    </span>
                    <label style="display: flex; align-items: center; gap: 4px; font-size: 0.75rem; cursor: pointer;">
                      <input type="checkbox" class="toggle-exam-publish" data-exam-id="${e.id}" ${e.is_published ? 'checked' : ''}>
                      <span class="${e.is_published ? 'text-success' : 'text-muted'}" style="font-weight: 600;">
                        ${e.is_published ? 'Đang mở' : 'Bản nháp'}
                      </span>
                    </label>
                  </div>

                  <h3 style="margin: 0 0 0.5rem 0; font-size: 1.0625rem; font-weight: 800; color: var(--text-primary); line-height: 1.3;">
                    ${escapeHtml(e.title)}
                  </h3>
                  <p style="margin: 0; color: var(--text-secondary); font-size: 0.8125rem; line-height: 1.4; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
                    ${escapeHtml(e.description || 'Chưa có mô tả chi tiết cho bài thi này.')}
                  </p>
                </div>

                <!-- Stats in Card -->
                <div style="padding: 1rem 1.25rem; background: var(--neutral-50); border-bottom: 1px solid var(--border-color); font-size: 0.8125rem; display: grid; grid-cols-2; grid-template-columns: 1fr 1fr; gap: 8px;">
                  <div>
                    <span style="color: var(--text-muted);">Thời gian:</span>
                    <strong>${e.duration_minutes} phút</strong>
                  </div>
                  <div>
                    <span style="color: var(--text-muted);">Điểm đạt:</span>
                    <strong style="color: var(--success-main);">${e.pass_score} đ</strong>
                  </div>
                  <div>
                    <span style="color: var(--text-muted);">Số câu hỏi:</span>
                    <strong>${e.total_questions || 0} câu</strong>
                  </div>
                  <div>
                    <span style="color: var(--text-muted);">Tổng điểm:</span>
                    <strong style="color: var(--primary-600);">${e.total_points || 0} đ</strong>
                  </div>
                  <div>
                    <span style="color: var(--text-muted);">Lượt làm bài:</span>
                    <strong>${e.attempts_count || 0} lượt</strong>
                  </div>
                  <div>
                    <span style="color: var(--text-muted);">Số lần tối đa:</span>
                    <strong>${e.max_attempts ? e.max_attempts + ' lần' : 'Không giới hạn'}</strong>
                  </div>
                </div>

                <!-- Card Actions -->
                <div style="padding: 1rem 1.25rem; display: flex; gap: 8px;">
                  <button type="button" class="btn btn-primary btn-open-builder" data-exam-id="${e.id}" style="flex: 1; justify-content: center; font-size: 0.8125rem;">
                    <span class="btn-icon-left">${icon('layers', { size: 14 })}</span> Dựng đề thi
                  </button>
                  <button type="button" class="btn btn-outline btn-edit-exam" data-exam-id="${e.id}" title="Sửa thông tin" style="padding: 6px 10px;">
                    ${icon('edit', { size: 14 })}
                  </button>
                  <button type="button" class="btn btn-outline text-danger btn-delete-exam" data-exam-id="${e.id}" title="Xóa đề" style="padding: 6px 10px;">
                    ${icon('trash', { size: 14 })}
                  </button>
                </div>
              </div>
            `).join('')}
          </div>
        ` : `
          <div class="card" style="padding: 3rem; text-align: center; color: var(--text-muted);">
            <div style="color: var(--neutral-400); margin-bottom: 0.75rem;">${icon('award', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Bạn chưa tạo đề thi nào</h3>
            <p style="font-size: 0.875rem; margin-bottom: 1rem;">Hãy tạo đề thi mới và chọn các câu hỏi từ ngân hàng câu hỏi để học sinh làm bài.</p>
            <button type="button" class="btn btn-primary btn-create-exam">
              + Tạo đề thi đầu tiên
            </button>
          </div>
        `}
      </div>
    `;
  }

  /**
   * Render Trình Dựng Đề Thi 2 Cột (Exam Builder)
   * Cột trái: Ngân hàng câu hỏi <-> Cột phải: Câu hỏi trong đề
   */
  static renderBuilder(data) {
    const { exam, availableQuestions, selectedQuestions, filterType, searchQuery } = data;

    const totalScore = selectedQuestions.reduce((sum, q) => sum + (Number(q.points) || 0), 0);

    return `
      <div class="exam-builder">
        <!-- Top Sticky Header -->
        <div style="background: white; border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 1rem 1.5rem; margin-bottom: 1.5rem; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; box-shadow: var(--shadow-sm);">
          <div>
            <div style="display: flex; align-items: center; gap: 8px;">
              <button type="button" class="btn btn-sm btn-ghost btn-back-to-exams" style="padding: 4px 8px;">
                &larr; Đóng Builder
              </button>
              <h2 style="margin: 0; font-size: 1.25rem; font-weight: 800; color: var(--text-primary);">
                Thiết kế đề: ${escapeHtml(exam.title)}
              </h2>
            </div>
            <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 4px;">
              Khóa: <strong>${escapeHtml(exam.course_title || '')}</strong> &bull; Thời gian: <strong>${exam.duration_minutes} phút</strong> &bull; Điểm đạt: <strong>${exam.pass_score} đ</strong>
            </div>
          </div>

          <!-- Total Points Badge & Save Button -->
          <div style="display: flex; align-items: center; gap: 1rem;">
            <div style="text-align: right;">
              <div style="font-size: 0.75rem; color: var(--text-muted);">Tổng điểm bài thi</div>
              <div style="font-size: 1.375rem; font-weight: 800; color: ${totalScore >= exam.pass_score ? 'var(--primary-600)' : 'var(--warning-main)'};">
                <span id="builder-total-score">${totalScore}</span> / 100 đ
              </div>
            </div>

            <button type="button" class="btn btn-primary btn-save-builder" style="font-size: 0.875rem; padding: 8px 16px;">
              <span class="btn-icon-left">${icon('check', { size: 16 })}</span> Lưu cấu trúc đề (${selectedQuestions.length} câu)
            </button>
          </div>
        </div>

        <!-- 2 Cột Builder -->
        <div class="grid grid-cols-2 gap-6" style="align-items: flex-start;">
          
          <!-- Cột Trái: Ngân hàng câu hỏi -->
          <div class="card" style="padding: 1.25rem; display: flex; flex-direction: column; gap: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
              <h3 style="margin: 0; font-size: 1rem; font-weight: 700; display: flex; align-items: center; gap: 6px;">
                <span>${icon('file-text', { size: 18 })}</span>
                Ngân hàng câu hỏi (${availableQuestions.length})
              </h3>
              <span style="font-size: 0.75rem; color: var(--text-muted);">Nhấn [+] để thêm vào đề</span>
            </div>

            <!-- Filters Cột Trái -->
            <div style="display: flex; gap: 8px;">
              <input type="text" id="builder-search" class="form-control form-control-sm" placeholder="Tìm câu hỏi..." value="${escapeHtml(searchQuery || '')}" style="font-size: 0.8125rem;">
              <select id="builder-type-filter" class="form-control form-control-sm" style="width: 140px; font-size: 0.8125rem;">
                <option value="all" ${filterType === 'all' ? 'selected' : ''}>Tất cả loại</option>
                <option value="multiple_choice" ${filterType === 'multiple_choice' ? 'selected' : ''}>Trắc nghiệm</option>
                <option value="fill_blank" ${filterType === 'fill_blank' ? 'selected' : ''}>Điền từ</option>
                <option value="pronunciation" ${filterType === 'pronunciation' ? 'selected' : ''}>Phát âm</option>
                <option value="essay" ${filterType === 'essay' ? 'selected' : ''}>Tự luận</option>
              </select>
            </div>

            <!-- Danh sách câu hỏi có sẵn -->
            <div style="max-height: 550px; overflow-y: auto; display: flex; flex-direction: column; gap: 0.75rem; padding-right: 4px;">
              ${availableQuestions.length > 0 ? availableQuestions.map(q => {
                const isAlreadyIn = selectedQuestions.some(sq => sq.question_id === q.id);

                return `
                  <div style="border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 0.875rem; background: ${isAlreadyIn ? 'var(--neutral-100)' : 'white'}; opacity: ${isAlreadyIn ? '0.6' : '1'};">
                    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 4px;">
                      <span class="badge badge-secondary" style="font-size: 0.65rem;">
                        ${q.question_type}
                      </span>
                      <span style="font-weight: 700; color: var(--primary-600); font-size: 0.75rem;">
                        ${q.default_points || 10} đ
                      </span>
                    </div>

                    <div style="font-size: 0.8125rem; font-weight: 600; color: var(--text-primary); margin-bottom: 6px;">
                      ${escapeHtml(q.question_text)}
                    </div>

                    <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 6px; border-top: 1px dashed var(--border-color);">
                      <span style="font-size: 0.7rem; color: var(--text-muted);">${escapeHtml(q.lesson_title || '')}</span>
                      <button type="button" class="btn btn-sm ${isAlreadyIn ? 'btn-ghost' : 'btn-primary'} btn-add-to-exam" data-question-id="${q.id}" ${isAlreadyIn ? 'disabled' : ''} style="font-size: 0.75rem; padding: 2px 8px;">
                        ${isAlreadyIn ? '✓ Đã trong đề' : '+ Thêm vào đề'}
                      </button>
                    </div>
                  </div>
                `;
              }).join('') : `
                <div style="text-align: center; padding: 2rem; color: var(--text-muted); font-size: 0.8125rem;">
                  Không tìm thấy câu hỏi phù hợp trong ngân hàng.
                </div>
              `}
            </div>
          </div>

          <!-- Cột Phải: Câu hỏi đã chọn trong đề thi -->
          <div class="card" style="padding: 1.25rem; display: flex; flex-direction: column; gap: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
              <h3 style="margin: 0; font-size: 1rem; font-weight: 700; display: flex; align-items: center; gap: 6px;">
                <span>${icon('award', { size: 18 })}</span>
                Câu hỏi trong đề thi (${selectedQuestions.length} câu)
              </h3>
              <span style="font-size: 0.75rem; color: var(--text-muted);">Dùng [↑] [↓] để đổi vị trí</span>
            </div>

            <!-- Danh sách câu trong đề -->
            <div id="builder-selected-list" style="max-height: 600px; overflow-y: auto; display: flex; flex-direction: column; gap: 0.75rem; padding-right: 4px;">
              ${selectedQuestions.length > 0 ? selectedQuestions.map((sq, idx) => `
                <div class="selected-question-row" data-index="${idx}" style="border: 1px solid #BFDBFE; border-radius: var(--radius-md); padding: 0.875rem; background: #F8FAFC;">
                  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <span class="badge badge-primary" style="font-weight: 800; font-size: 0.75rem;">
                        Câu #${idx + 1}
                      </span>
                      <span class="badge badge-secondary" style="font-size: 0.65rem;">
                        ${sq.question_type}
                      </span>
                    </div>

                    <!-- Điểm từng câu -->
                    <div style="display: flex; align-items: center; gap: 4px;">
                      <span style="font-size: 0.75rem; color: var(--text-secondary); font-weight: 600;">Điểm:</span>
                      <input type="number" class="form-control form-control-sm sq-point-input" data-index="${idx}" value="${sq.points || 10}" min="1" max="100" style="width: 60px; font-weight: 700; text-align: center; font-size: 0.8125rem;">
                    </div>
                  </div>

                  <div style="font-size: 0.8125rem; font-weight: 600; color: var(--text-primary); margin-bottom: 8px; line-height: 1.4;">
                    ${escapeHtml(sq.question_text)}
                  </div>

                  <!-- Control: Di chuyển Lên/Xuống & Xóa -->
                  <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 6px; border-top: 1px dashed var(--border-color);">
                    <div style="display: flex; gap: 4px;">
                      <button type="button" class="btn btn-sm btn-icon btn-move-up" data-index="${idx}" ${idx === 0 ? 'disabled' : ''} title="Di chuyển lên">
                        ↑
                      </button>
                      <button type="button" class="btn btn-sm btn-icon btn-move-down" data-index="${idx}" ${idx === selectedQuestions.length - 1 ? 'disabled' : ''} title="Di chuyển xuống">
                        ↓
                      </button>
                    </div>

                    <button type="button" class="btn btn-sm btn-ghost text-danger btn-remove-from-exam" data-index="${idx}" style="font-size: 0.75rem; padding: 2px 6px;">
                      ✕ Bỏ khỏi đề
                    </button>
                  </div>
                </div>
              `).join('') : `
                <div style="text-align: center; padding: 3rem 1rem; color: var(--text-muted); border: 2px dashed var(--border-color); border-radius: var(--radius-md);">
                  <div style="color: var(--neutral-400); margin-bottom: 0.5rem;">${icon('plus-circle', { size: 36 })}</div>
                  <h4 style="font-size: 0.9375rem; font-weight: 700; margin-bottom: 0.25rem;">Chưa có câu hỏi nào trong đề</h4>
                  <p style="font-size: 0.75rem;">Hãy bấm nút "+ Thêm vào đề" ở cột bên trái để chọn các câu hỏi phù hợp.</p>
                </div>
              `}
            </div>
          </div>

        </div>
      </div>
    `;
  }

  /**
   * Render HTML cho Modal Tạo / Sửa Đề thi
   */
  static renderExamForm(exam = null, courses = []) {
    const isEdit = !!exam;
    const title = exam ? exam.title : '';
    const desc = exam ? (exam.description || '') : '';
    const courseId = exam ? exam.course_id : (courses[0] ? courses[0].id : 1);
    const duration = exam ? exam.duration_minutes : 45;
    const passScore = exam ? exam.pass_score : 50;
    const maxAttempts = exam ? exam.max_attempts : 3;
    const isPublished = exam ? (exam.is_published === 1) : true;

    return `
      <form id="form-exam-meta" style="display: flex; flex-direction: column; gap: 1rem;">
        <div>
          <label class="form-label">Tên đề thi: <span class="text-danger">*</span></label>
          <input type="text" id="exam-title-input" class="form-control" required value="${escapeHtml(title)}" placeholder="VD: Đề Kiểm Tra Giữa Kỳ Tiếng Anh B1">
        </div>

        <div>
          <label class="form-label">Khóa học áp dụng:</label>
          <select id="exam-course-input" class="form-control">
            ${courses.map(c => `
              <option value="${c.id}" ${String(c.id) === String(courseId) ? 'selected' : ''}>
                ${escapeHtml(c.title)}
              </option>
            `).join('')}
          </select>
        </div>

        <div>
          <label class="form-label">Mô tả mục tiêu đề thi:</label>
          <textarea id="exam-desc-input" class="form-control" rows="2" placeholder="Ghi chú phạm vi kiến thức, thời lượng và lưu ý cho thí sinh...">${escapeHtml(desc)}</textarea>
        </div>

        <div class="grid grid-cols-3 gap-4">
          <div>
            <label class="form-label">Thời gian làm bài: <span class="text-danger">*</span></label>
            <div style="display: flex; align-items: center; gap: 6px;">
              <input type="number" id="exam-duration-input" class="form-control" min="5" max="180" required value="${duration}">
              <span style="font-size: 0.8125rem; color: var(--text-muted);">phút</span>
            </div>
          </div>

          <div>
            <label class="form-label">Điểm đạt (Pass): <span class="text-danger">*</span></label>
            <div style="display: flex; align-items: center; gap: 6px;">
              <input type="number" id="exam-pass-input" class="form-control" min="1" max="100" required value="${passScore}">
              <span style="font-size: 0.8125rem; color: var(--text-muted);">/100</span>
            </div>
          </div>

          <div>
            <label class="form-label">Số lần thi tối đa:</label>
            <input type="number" id="exam-attempts-input" class="form-control" min="1" max="20" value="${maxAttempts || ''}" placeholder="Trống = Vô hạn">
          </div>
        </div>

        <div>
          <label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
            <input type="checkbox" id="exam-publish-input" ${isPublished ? 'checked' : ''}>
            <span style="font-weight: 600; font-size: 0.875rem;">Xuất bản đề thi (Cho phép học viên trong khóa bắt đầu làm bài)</span>
          </label>
        </div>
      </form>
    `;
  }
}
