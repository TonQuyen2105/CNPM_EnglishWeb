/**
 * StudentExamsView - Giao diện Danh sách đề thi & Màn làm bài thi
 */

import { icon } from '../../ui/icons.js';

export class StudentExamsView {
  static renderExamList({ exams = [], myAttempts = [] }) {
    return `
      <div class="student-exams-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Luyện thi & Đánh giá năng lực</h1>
            <p class="page-header-subtitle">
              Làm bài thi định kỳ của các khóa học bạn đang theo học để đánh giá tiến độ và nhận chứng chỉ.
            </p>
          </div>
        </div>

        ${exams.length > 0 ? `
          <div class="grid grid-cols-3 gap-6">
            ${exams.map(ex => this._renderExamCard(ex, myAttempts)).join('')}
          </div>
        ` : `
          <div class="card" style="padding: 3.5rem; text-align: center; border-radius: var(--radius-xl);">
            <div style="color: var(--text-muted); margin-bottom: 0.75rem;">${icon('award', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Chưa có đề thi nào được mở</h3>
            <p style="color: var(--text-secondary); max-width: 450px; margin: 0 auto;">
              Các đề thi sẽ xuất hiện tại đây khi giáo viên phát hành đề cho khóa học của bạn.
            </p>
          </div>
        `}
      </div>
    `;
  }

  static _renderExamCard(ex, myAttempts) {
    const attempts = myAttempts.filter(a => a.exam_id === ex.id);
    const maxAttempts = ex.max_attempts || 3;
    const attemptsLeft = Math.max(0, maxAttempts - attempts.length);
    const bestScore = attempts.length > 0
      ? Math.max(...attempts.map(a => Number(a.total_score) || 0))
      : null;

    return `
      <div class="card" style="border-radius: var(--radius-xl); overflow: hidden; border: 1px solid var(--border-color); display: flex; flex-direction: column; justify-content: space-between;">
        <div style="padding: 1.5rem;">
          <div class="flex items-center justify-between" style="margin-bottom: 0.75rem;">
            <span class="badge badge-primary">${ex.course_title || 'Đề thi tổng hợp'}</span>
            <span class="badge ${attemptsLeft > 0 ? 'badge-success' : 'badge-danger'}">
              Còn ${attemptsLeft} / ${maxAttempts} lượt thi
            </span>
          </div>

          <h3 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem; line-height: 1.4;">
            ${ex.title}
          </h3>
          <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 1.25rem; line-height: 1.5;">
            ${ex.description || 'Bài kiểm tra đánh giá năng lực ngôn ngữ toàn diện.'}
          </p>

          <div class="grid grid-cols-2 gap-3" style="background: var(--neutral-50); padding: 0.75rem 1rem; border-radius: var(--radius-md); border: 1px solid var(--border-color); font-size: 0.8125rem; margin-bottom: 1rem;">
            <div>
              <span style="color: var(--text-muted);">${icon('clock', { size: 14 })} Thời lượng:</span>
              <strong style="color: var(--text-primary); margin-left: 4px;">${ex.duration_minutes} phút</strong>
            </div>
            <div>
              <span style="color: var(--text-muted);">${icon('award', { size: 14 })} Điểm đạt:</span>
              <strong style="color: var(--text-primary); margin-left: 4px;">${ex.pass_score}đ</strong>
            </div>
          </div>

          ${bestScore !== null ? `
            <div class="flex items-center justify-between" style="font-size: 0.75rem; color: var(--text-secondary); padding-top: 0.5rem;">
              <span>Điểm cao nhất đã đạt:</span>
              <strong style="color: ${bestScore >= ex.pass_score ? 'var(--success-main)' : 'var(--danger-main)'}; font-size: 0.875rem;">
                ${bestScore}/100 (${bestScore >= ex.pass_score ? 'Đạt' : 'Chưa đạt'})
              </strong>
            </div>
          ` : ''}
        </div>

        <div style="padding: 1rem 1.5rem; background: var(--neutral-50); border-top: 1px solid var(--border-color); display: flex; align-items: center; justify-content: space-between;">
          ${attemptsLeft > 0 ? `
            <a href="#/exams/${ex.id}/take" class="btn btn-primary w-full" style="justify-content: center; font-weight: 700;">
              <span class="nav-icon">${icon('edit', { size: 16 })}</span>
              <span>Bắt đầu làm bài thi</span>
            </a>
          ` : `
            <button type="button" class="btn btn-secondary w-full" disabled style="justify-content: center;">
              Đã hết lượt làm bài
            </button>
          `}
        </div>
      </div>
    `;
  }

  /**
   * Render Màn hình Làm bài thi trực tuyến
   */
  static renderTakeExam({ exam = {}, questions = [], draftAnswers = {} }) {
    return `
      <div class="student-take-exam-page">
        <!-- Floating / Sticky Top Bar với Đồng hồ đếm ngược -->
        <div class="card" style="position: sticky; top: 72px; z-index: 50; border-radius: var(--radius-lg); padding: 0.875rem 1.5rem; margin-bottom: 1.5rem; border: 1px solid var(--border-color); box-shadow: var(--shadow-md); background: white;">
          <div class="flex items-center justify-between flex-wrap gap-4">
            <div>
              <span class="badge badge-primary" style="font-size: 0.6875rem; text-transform: uppercase;">Đang làm bài</span>
              <h2 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin: 2px 0 0 0;">
                ${exam.title}
              </h2>
            </div>

            <div class="flex items-center gap-4">
              <!-- Đồng hồ đếm ngược -->
              <div class="timer-pill" id="exam-timer-pill">
                <span class="nav-icon">${icon('clock', { size: 18 })}</span>
                <span id="exam-timer-text">--:--</span>
              </div>

              <button type="button" class="btn btn-success" id="btn-submit-exam" style="font-weight: 800; padding: 8px 20px;">
                <span>Nộp bài thi</span>
              </button>
            </div>
          </div>
        </div>

        <!-- 2 Cột: Danh sách câu hỏi (8 cột) & Bảng Palette câu hỏi (4 cột) -->
        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (8 cột): Danh sách câu hỏi -->
          <div class="col-span-8 flex flex-col gap-6" id="exam-questions-list">
            ${questions.map((q, idx) => this._renderQuestionItem(q, idx, draftAnswers[q.id])).join('')}
          </div>

          <!-- Cột Phải (4 cột): Bảng Palette câu hỏi -->
          <div class="col-span-4">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color); position: sticky; top: 160px;">
              <h3 style="font-size: 1rem; font-weight: 800; margin-bottom: 1rem; color: var(--text-primary);">
                Danh sách câu hỏi (${questions.length} câu)
              </h3>
              <p style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 1rem;">
                Bấm vào số câu để chuyển nhanh đến câu hỏi tương ứng. Màu xanh nhạt biểu thị câu đã trả lời.
              </p>

              <div class="flex flex-wrap gap-2" id="exam-palette-container">
                ${questions.map((q, idx) => {
                  const isAnswered = draftAnswers[q.id] != null && draftAnswers[q.id] !== '';
                  return `
                    <button type="button" class="exam-palette-btn ${isAnswered ? 'answered' : ''}" data-target-index="${idx}" data-qid="${q.id}">
                      ${idx + 1}
                    </button>
                  `;
                }).join('')}
              </div>

              <div style="margin-top: 1.5rem; padding-top: 1rem; border-top: 1px solid var(--border-color); font-size: 0.75rem; color: var(--text-secondary); display: flex; flex-direction: column; gap: 6px;">
                <div class="flex items-center gap-2">
                  <span style="width: 12px; height: 12px; border-radius: 3px; background: #EFF6FF; border: 1px solid #BFDBFE;"></span>
                  <span>Đã trả lời (tự lưu nháp)</span>
                </div>
                <div class="flex items-center gap-2">
                  <span style="width: 12px; height: 12px; border-radius: 3px; background: #FFFFFF; border: 1px solid var(--border-color);"></span>
                  <span>Chưa trả lời</span>
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>
    `;
  }

  static _renderQuestionItem(q, idx, draftValue) {
    return `
      <div class="card exam-question-box" id="question-box-${idx}" data-qid="${q.id}" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color);">
        <div class="flex items-center justify-between" style="margin-bottom: 1rem;">
          <div class="flex items-center gap-2">
            <span class="badge badge-primary" style="font-weight: 800; font-size: 0.8125rem;">Câu ${idx + 1}</span>
            <span class="badge badge-info" style="font-size: 0.6875rem; text-transform: uppercase;">${q.question_type}</span>
          </div>
          <span style="font-size: 0.8125rem; font-weight: 700; color: var(--text-muted);">${q.points || 1} điểm</span>
        </div>

        <h4 style="font-size: 1rem; font-weight: 700; color: var(--text-primary); margin-bottom: 1.25rem; line-height: 1.5;">
          ${q.question_text}
        </h4>

        <!-- Tùy theo loại câu hỏi -->
        ${q.question_type === 'multiple_choice' ? `
          <div class="flex flex-col gap-2">
            ${(q.options || []).map((opt, oIdx) => `
              <label style="display: flex; align-items: center; gap: 12px; padding: 10px 14px; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: var(--neutral-50); cursor: pointer; transition: all var(--transition-fast);">
                <input type="radio" name="exam_ans_${q.id}" value="${opt.id}" class="exam-ans-input" ${String(draftValue) === String(opt.id) ? 'checked' : ''}>
                <span style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">
                  <strong>${['A', 'B', 'C', 'D'][oIdx] || oIdx + 1}.</strong> ${opt.option_text}
                </span>
              </label>
            `).join('')}
          </div>
        ` : q.question_type === 'fill_blank' ? `
          <div>
            <label class="form-label" style="font-size: 0.8125rem;">Nhập câu trả lời của bạn:</label>
            <input type="text" class="form-control exam-ans-input" name="exam_ans_${q.id}" value="${draftValue || ''}" placeholder="Nhập đáp án chính xác..." style="background: white;">
          </div>
        ` : q.question_type === 'pronunciation' ? `
          <div>
            <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 8px;">
              Luyện phát âm câu trên. Bạn có thể nhập phiên âm hoặc ghi chú phát âm:
            </p>
            <input type="text" class="form-control exam-ans-input" name="exam_ans_${q.id}" value="${draftValue || ''}" placeholder="Ví dụ: Đã đọc to và phát âm theo phiên âm chuẩn..." style="background: white;">
          </div>
        ` : `
          <!-- essay -->
          <div>
            <label class="form-label" style="font-size: 0.8125rem;">Bài viết tự luận:</label>
            <textarea class="form-control exam-ans-input" name="exam_ans_${q.id}" rows="5" placeholder="Viết bài làm tự luận của bạn tại đây..." style="background: white;">${draftValue || ''}</textarea>
          </div>
        `}
      </div>
    `;
  }
}
