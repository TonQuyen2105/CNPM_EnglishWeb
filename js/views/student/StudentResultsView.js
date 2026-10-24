/**
 * StudentResultsView - Giao diện Kết quả thi & Lịch sử khảo thí
 */

import { icon } from '../../ui/icons.js';
import { formatDate } from '../../core/utils.js';

export class StudentResultsView {
  /**
   * Render danh sách toàn bộ lịch sử thi của học viên
   */
  static renderHistoryList({ attempts = [] }) {
    return `
      <div class="student-results-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Kết quả & Lịch sử thi</h1>
            <p class="page-header-subtitle">
              Xem lại điểm số, phân tích bài làm và nhận xét của giảng viên cho các bài kiểm tra đã nộp.
            </p>
          </div>
        </div>

        ${attempts.length > 0 ? `
          <div class="card" style="border-radius: var(--radius-xl); overflow: hidden; border: 1px solid var(--border-color); padding: 0;">
            <div class="table-responsive">
              <table class="table" style="width: 100%; border-collapse: collapse;">
                <thead>
                  <tr style="background: var(--neutral-50); border-bottom: 1px solid var(--border-color); font-size: 0.8125rem; color: var(--text-muted);">
                    <th style="padding: 1rem 1.25rem; text-align: left;">Đề thi</th>
                    <th style="padding: 1rem 1.25rem; text-align: left;">Lần thi</th>
                    <th style="padding: 1rem 1.25rem; text-align: left;">Ngày nộp</th>
                    <th style="padding: 1rem 1.25rem; text-align: center;">Điểm số</th>
                    <th style="padding: 1rem 1.25rem; text-align: center;">Kết quả</th>
                    <th style="padding: 1rem 1.25rem; text-align: right;">Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  ${attempts.map(att => {
                    const isPassed = Number(att.total_score) >= Number(att.pass_score || 50);
                    return `
                      <tr style="border-bottom: 1px solid var(--border-color);">
                        <td style="padding: 1rem 1.25rem;">
                          <div style="font-weight: 700; color: var(--text-primary);">${att.exam_title || 'Đề thi'}</div>
                          <span style="font-size: 0.75rem; color: var(--text-muted);">${att.duration_minutes || 30} phút • Điểm đạt: ${att.pass_score || 50}đ</span>
                        </td>
                        <td style="padding: 1rem 1.25rem; font-size: 0.875rem;">
                          Lần ${att.attempt_number || 1}
                        </td>
                        <td style="padding: 1rem 1.25rem; font-size: 0.8125rem; color: var(--text-secondary);">
                          ${formatDate(att.submitted_at || att.created_at)}
                        </td>
                        <td style="padding: 1rem 1.25rem; text-align: center;">
                          <span style="font-size: 1.125rem; font-weight: 800; color: ${isPassed ? 'var(--success-main)' : 'var(--danger-main)'};">
                            ${att.total_score !== null ? att.total_score : 'Đang chấm'}
                          </span>
                        </td>
                        <td style="padding: 1rem 1.25rem; text-align: center;">
                          <span class="badge ${isPassed ? 'badge-success' : 'badge-danger'}">
                            ${isPassed ? '✓ Đạt' : '✗ Chưa đạt'}
                          </span>
                        </td>
                        <td style="padding: 1rem 1.25rem; text-align: right;">
                          <a href="#/results/${att.id}" class="btn btn-outline-primary btn-sm" style="border-radius: var(--radius-full); font-weight: 600;">
                            Xem chi tiết bài làm
                          </a>
                        </td>
                      </tr>
                    `;
                  }).join('')}
                </tbody>
              </table>
            </div>
          </div>
        ` : `
          <div class="card" style="padding: 3.5rem; text-align: center; border-radius: var(--radius-xl);">
            <div style="color: var(--text-muted); margin-bottom: 0.75rem;">${icon('award', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Bạn chưa thực hiện bài thi nào</h3>
            <p style="color: var(--text-secondary); max-width: 450px; margin: 0 auto 1.5rem;">
              Hãy tham gia làm các bài kiểm tra đánh giá để nhận phản hồi và ghi nhận điểm số.
            </p>
            <a href="#/exams" class="btn btn-primary" style="display: inline-flex;">
              Khám phá đề thi ngay
            </a>
          </div>
        `}
      </div>
    `;
  }

  /**
   * Render Chi tiết bài thi đã làm (bảng điểm + phân tích từng câu)
   */
  static renderAttemptDetail({ attempt = {}, answers = [], exam = {}, otherAttempts = [] }) {
    const isPassed = Number(attempt.total_score) >= Number(exam.pass_score || 50);

    return `
      <div class="student-result-detail-page">
        <!-- Back Link -->
        <div style="margin-bottom: 1.25rem;">
          <a href="#/results" class="btn btn-secondary btn-sm" style="background: white; border-radius: var(--radius-full);">
            <span class="nav-icon">${icon('arrow-left', { size: 16 })}</span>
            <span>Quay lại Danh sách kết quả</span>
          </a>
        </div>

        <!-- Banner Tổng Điểm & Kết Quả -->
        <div class="card" style="border-radius: var(--radius-xl); padding: 2rem; border: 1px solid var(--border-color); margin-bottom: 2rem; background: ${isPassed ? 'linear-gradient(135deg, #F0FDF4, #DCFCE7)' : 'linear-gradient(135deg, #FEF2F2, #FEE2E2)'};">
          <div class="flex items-center justify-between flex-wrap gap-4">
            <div>
              <div class="flex items-center gap-2" style="margin-bottom: 0.5rem;">
                <span class="badge ${isPassed ? 'badge-success' : 'badge-danger'}" style="font-size: 0.8125rem; font-weight: 800;">
                  ${isPassed ? '✓ CHÚC MỪNG: BẠN ĐÃ ĐẠT BÀI THI' : '✗ BẠN CHƯA ĐẠT ĐIỂM YÊU CẦU'}
                </span>
                <span style="font-size: 0.8125rem; color: var(--text-muted);">Lần thi: ${attempt.attempt_number || 1}</span>
              </div>
              <h1 style="font-size: 1.75rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.25rem;">
                ${exam.title || 'Bài kiểm tra năng lực'}
              </h1>
              <p style="font-size: 0.875rem; color: var(--text-secondary); margin: 0;">
                Hoàn thành lúc: ${formatDate(attempt.submitted_at || attempt.created_at)} • Điểm chuẩn: ${exam.pass_score || 50}/100
              </p>
            </div>

            <div style="text-align: right;">
              <div style="font-size: 3rem; font-weight: 800; line-height: 1; color: ${isPassed ? '#16A34A' : '#DC2626'};">
                ${attempt.total_score || 0}
              </div>
              <div style="font-size: 0.8125rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase;">
                Tổng điểm đạt được
              </div>
            </div>
          </div>
        </div>

        <!-- 2 Cột: Chi tiết từng câu (8 cột) & Lịch sử các lần thi trước (4 cột) -->
        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (8 cột): Phân tích chi tiết từng câu hỏi -->
          <div class="col-span-8 flex flex-col gap-4">
            <h3 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
              Phân tích chi tiết bài làm (${answers.length} câu)
            </h3>

            ${answers.map((ans, idx) => {
              const q = ans.question || {};
              const isCorrect = ans.is_correct === 1 || ans.is_correct === true;
              const isPending = ans.grading_status === 'pending_review';

              return `
                <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color); background: ${isPending ? '#FFFBEB' : isCorrect ? '#F0FDF4' : '#FEF2F2'};">
                  <div class="flex items-center justify-between" style="margin-bottom: 0.75rem;">
                    <div class="flex items-center gap-2">
                      <span class="badge ${isPending ? 'badge-warning' : isCorrect ? 'badge-success' : 'badge-danger'}" style="font-weight: 800;">
                        Câu ${idx + 1}
                      </span>
                      <span class="badge badge-primary" style="font-size: 0.6875rem; text-transform: uppercase;">
                        ${q.question_type || 'Trắc nghiệm'}
                      </span>
                    </div>

                    <div>
                      ${isPending ? `
                        <span class="badge badge-warning">⏳ Chờ giáo viên chấm</span>
                      ` : `
                        <strong style="color: ${isCorrect ? 'var(--success-main)' : 'var(--danger-main)'}; font-size: 0.875rem;">
                          +${ans.score_earned || 0} / ${ans.max_points || q.points || 1} điểm
                        </strong>
                      `}
                    </div>
                  </div>

                  <h4 style="font-size: 0.9375rem; font-weight: 700; color: var(--text-primary); margin-bottom: 1rem; line-height: 1.5;">
                    ${q.question_text || 'Câu hỏi'}
                  </h4>

                  <!-- Trắc nghiệm hoặc tự luận -->
                  ${q.question_type === 'multiple_choice' ? `
                    <div class="flex flex-col gap-2" style="font-size: 0.875rem;">
                      ${(q.options || []).map(opt => {
                        const isStudentPick = opt.id === ans.selected_option_id;
                        const isRightOpt = opt.is_correct === 1 || opt.is_correct === true;
                        
                        let optBg = 'white';
                        let optBorder = 'var(--border-color)';
                        if (isStudentPick && isRightOpt) {
                          optBg = '#DCFCE7'; optBorder = '#86EFAC';
                        } else if (isStudentPick && !isRightOpt) {
                          optBg = '#FEE2E2'; optBorder = '#FCA5A5';
                        } else if (!isStudentPick && isRightOpt) {
                          optBg = '#DCFCE7'; optBorder = '#86EFAC';
                        }

                        return `
                          <div style="padding: 8px 12px; border-radius: var(--radius-md); background: ${optBg}; border: 1px solid ${optBorder}; display: flex; align-items: center; justify-content: space-between;">
                            <span>${opt.option_text}</span>
                            <span>
                              ${isStudentPick ? '<span class="badge badge-primary" style="font-size: 0.6875rem;">Bạn chọn</span>' : ''}
                              ${isRightOpt ? '<span class="badge badge-success" style="font-size: 0.6875rem; margin-left: 4px;">Đáp án đúng</span>' : ''}
                            </span>
                          </div>
                        `;
                      }).join('')}
                    </div>
                  ` : `
                    <!-- Tự luận / điền từ / phát âm -->
                    <div style="background: white; border-radius: var(--radius-md); padding: 1rem; border: 1px solid var(--border-color); font-size: 0.875rem; margin-bottom: 0.75rem;">
                      <div style="font-weight: 600; color: var(--text-muted); font-size: 0.75rem; margin-bottom: 4px;">Câu trả lời của bạn:</div>
                      <div style="color: var(--text-primary);">${ans.text_answer || 'Không có câu trả lời'}</div>
                    </div>

                    ${ans.teacher_feedback ? `
                      <div style="background: #EFF6FF; border-left: 3px solid var(--primary-600); border-radius: var(--radius-md); padding: 0.75rem 1rem; font-size: 0.8125rem;">
                        <strong style="color: var(--primary-800); display: block; margin-bottom: 2px;">Nhận xét từ giáo viên:</strong>
                        <span style="color: var(--primary-900);">${ans.teacher_feedback}</span>
                      </div>
                    ` : ''}
                  `}
                </div>
              `;
            }).join('')}
          </div>

          <!-- Cột Phải (4 cột): Lịch sử thi & Nút thi lại -->
          <div class="col-span-4 flex flex-col gap-6">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color);">
              <h3 style="font-size: 1rem; font-weight: 800; color: var(--text-primary); margin-bottom: 1rem;">
                Các lần thi của đề này
              </h3>

              ${otherAttempts && otherAttempts.length > 0 ? `
                <div class="flex flex-col gap-3">
                  ${otherAttempts.map(att => `
                    <a href="#/results/${att.id}" style="display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; border-radius: var(--radius-md); border: 1px solid ${att.id === attempt.id ? 'var(--primary-600)' : 'var(--border-color)'}; background: ${att.id === attempt.id ? '#EFF6FF' : 'var(--neutral-50)'}; text-decoration: none;">
                      <div>
                        <div style="font-weight: 700; font-size: 0.8125rem; color: var(--text-primary);">
                          Lần thi số ${att.attempt_number}
                        </div>
                        <span style="font-size: 0.7rem; color: var(--text-muted);">${formatDate(att.submitted_at)}</span>
                      </div>

                      <span style="font-weight: 800; font-size: 1rem; color: ${Number(att.total_score) >= (exam.pass_score || 50) ? 'var(--success-main)' : 'var(--danger-main)'};">
                        ${att.total_score}đ
                      </span>
                    </a>
                  `).join('')}
                </div>
              ` : `
                <p style="color: var(--text-muted); font-size: 0.8125rem; margin: 0;">Đây là lần thi đầu tiên của bạn cho đề này.</p>
              `}

              <div style="margin-top: 1.5rem; padding-top: 1rem; border-top: 1px solid var(--border-color);">
                <a href="#/exams/${exam.id}/take" class="btn btn-primary w-full" style="justify-content: center; font-weight: 700;">
                  Làm lại bài thi này
                </a>
              </div>
            </div>
          </div>

        </div>
      </div>
    `;
  }
}
