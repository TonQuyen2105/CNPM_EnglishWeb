/**
 * TeacherGradingView - Giao diện Hàng đợi Chấm thi & Màn hình chấm chia 2 phần (Split-Screen)
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherGradingView {
  /**
   * Render danh sách hàng đợi các bài thi chờ chấm (Queue)
   */
  static renderQueue(submissions = []) {
    return `
      <div class="teacher-grading-queue">
        <!-- Header -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <div style="display: flex; align-items: center; gap: 8px;">
              <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
                Chấm Bài Thi Tự Luận
              </h1>
              ${submissions.length > 0 
                ? `<span class="badge badge-warning" style="font-weight: 700;">${submissions.length} bài cần chấm</span>` 
                : `<span class="badge badge-success">Đã hoàn tất</span>`}
            </div>
            <p class="page-header-subtitle" style="margin: 4px 0 0 0; color: var(--text-secondary); font-size: 0.875rem;">
              Đánh giá bài làm tự luận, viết đoạn văn, phát âm và đưa nhận xét chuyên môn cho học viên
            </p>
          </div>

          ${submissions.length > 0 ? `
            <button type="button" class="btn btn-primary btn-start-first" data-answer-id="${submissions[0].id}" style="font-size: 0.8125rem;">
              <span class="btn-icon-left">${icon('edit', { size: 16 })}</span> Bắt đầu chấm bài đầu tiên
            </button>
          ` : ''}
        </div>

        ${submissions.length > 0 ? `
          <div class="card" style="padding: 0; overflow: hidden;">
            <div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border-color); display: flex; justify-content: space-between; align-items: center;">
              <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700;">
                Hàng đợi bài nộp của học viên (${submissions.length} bài)
              </h3>
              <span style="font-size: 0.75rem; color: var(--text-muted);">Sắp xếp theo thứ tự nộp bài</span>
            </div>

            <div class="table-responsive">
              <table class="table" style="margin: 0;">
                <thead>
                  <tr>
                    <th>Học viên</th>
                    <th>Đề thi</th>
                    <th>Dạng bài / Câu hỏi</th>
                    <th style="width: 100px; text-align: center;">Điểm tối đa</th>
                    <th>Thời gian nộp</th>
                    <th style="width: 130px; text-align: right;">Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  ${submissions.map((sub, idx) => `
                    <tr>
                      <td>
                        <div style="display: flex; align-items: center; gap: 8px;">
                          <div class="avatar avatar-sm" style="background: var(--primary-100); color: var(--primary-700); font-weight: 700;">
                            ${escapeHtml(sub.student_name.slice(0, 2).toUpperCase())}
                          </div>
                          <div>
                            <strong style="color: var(--text-primary);">${escapeHtml(sub.student_name)}</strong>
                            <div style="font-size: 0.75rem; color: var(--text-muted);">${escapeHtml(sub.student_email)}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <strong style="font-size: 0.8125rem; color: var(--primary-700);">${escapeHtml(sub.exam_title)}</strong>
                      </td>
                      <td style="max-width: 320px;">
                        <span class="badge ${sub.question_type === 'essay' ? 'badge-primary' : 'badge-warning'}" style="font-size: 0.65rem; margin-bottom: 2px;">
                          ${sub.question_type === 'essay' ? 'Tự luận' : 'Phát âm'}
                        </span>
                        <div style="font-size: 0.8125rem; color: var(--text-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                          ${escapeHtml(sub.question_text)}
                        </div>
                      </td>
                      <td style="text-align: center; font-weight: 700; color: var(--primary-600); font-size: 0.875rem;">
                        ${sub.max_points || 20} đ
                      </td>
                      <td style="font-size: 0.8125rem; color: var(--text-secondary);">
                        ${formatDate(sub.submitted_at)}
                      </td>
                      <td style="text-align: right;">
                        <button type="button" class="btn btn-sm btn-primary btn-open-grading" data-answer-id="${sub.id}" style="font-size: 0.75rem; padding: 4px 10px;">
                          <span class="btn-icon-left">${icon('edit', { size: 12 })}</span> Chấm bài
                        </button>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>
        ` : `
          <div class="card" style="padding: 3.5rem 1.5rem; text-align: center; color: var(--text-muted);">
            <div style="color: var(--success-main); margin-bottom: 1rem;">
              ${icon('check-circle', { size: 52 })}
            </div>
            <h3 style="font-size: 1.25rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
              Hàng đợi trống - Toàn bộ bài thi đã được chấm điểm!
            </h3>
            <p style="font-size: 0.875rem; max-width: 480px; margin: 0 auto 1.5rem; color: var(--text-secondary);">
              Hiện không còn học viên nào nộp bài tự luận chờ đánh giá. Kết quả đã cập nhật tức thì đến bảng điểm học viên.
            </p>
            <a href="#/results" class="btn btn-outline" style="display: inline-flex; align-items: center; gap: 6px;">
              ${icon('trending', { size: 16 })} Xem phổ điểm và kết quả tổng hợp
            </a>
          </div>
        `}
      </div>
    `;
  }

  /**
   * Render Màn hình Chấm bài Split-Screen (Chia 2 phần)
   * Cột trái: Đề bài + Bài làm học sinh (text, audio)
   * Cột phải: Panel nhập điểm, feedback, nút Lưu & câu kế tiếp
   */
  static renderGradingScreen(data) {
    const { submission, remainingCount, totalInQueue } = data;

    // Tính số từ trong bài làm học viên
    const wordCount = submission.text_answer 
      ? submission.text_answer.trim().split(/\s+/).filter(Boolean).length 
      : 0;

    return `
      <div class="teacher-grading-screen">
        <!-- Top Navigation Bar -->
        <div style="background: white; border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 0.875rem 1.25rem; margin-bottom: 1.25rem; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
          <div style="display: flex; align-items: center; gap: 10px;">
            <button type="button" class="btn btn-sm btn-outline btn-back-to-queue" style="font-size: 0.75rem; padding: 4px 8px;">
              &larr; Hàng đợi
            </button>
            <div>
              <div style="font-weight: 800; font-size: 0.9375rem; color: var(--text-primary);">
                Chấm bài thi: ${escapeHtml(submission.exam_title)}
              </div>
              <div style="font-size: 0.75rem; color: var(--text-muted);">
                Thí sinh: <strong>${escapeHtml(submission.student_name)}</strong> (${escapeHtml(submission.student_email)}) &bull; Nộp lúc: ${formatDate(submission.submitted_at)}
              </div>
            </div>
          </div>

          <div style="display: flex; align-items: center; gap: 8px;">
            <span class="badge badge-warning" style="font-weight: 700; font-size: 0.75rem;">
              Còn lại: ${remainingCount} bài chờ chấm
            </span>
          </div>
        </div>

        <!-- Split Screen: Cột Trái (60%) & Cột Phải (40%) -->
        <div class="grid grid-cols-5 gap-6" style="align-items: flex-start;">
          
          <!-- Cột Trái: Đề bài & Bài làm của học sinh (3/5) -->
          <div style="grid-column: span 3; display: flex; flex-direction: column; gap: 1.25rem;">
            
            <!-- Box 1: Đề bài câu hỏi -->
            <div class="card" style="padding: 1.25rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.5rem;">
                <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700; display: flex; align-items: center; gap: 6px;">
                  <span>${icon('file-text', { size: 16 })}</span>
                  Đề bài & Yêu cầu
                </h3>
                <span class="badge badge-primary" style="font-weight: 700;">
                  Thang điểm: ${submission.max_points} đ
                </span>
              </div>

              <div style="font-size: 0.9375rem; line-height: 1.6; color: var(--text-primary); font-weight: 600;">
                ${escapeHtml(submission.question_text)}
              </div>
            </div>

            <!-- Box 2: Bài làm của học sinh -->
            <div class="card" style="padding: 1.25rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.5rem;">
                <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700; display: flex; align-items: center; gap: 6px;">
                  <span style="color: var(--primary-600);">${icon('user', { size: 16 })}</span>
                  Bài làm của học viên
                </h3>
                <span style="font-size: 0.75rem; color: var(--text-muted); font-weight: 600;">
                  Độ dài: <strong>${wordCount} từ</strong>
                </span>
              </div>

              <!-- Nội dung văn bản tự luận -->
              ${submission.text_answer ? `
                <div style="background: var(--neutral-50); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem 1.25rem; font-size: 0.9375rem; line-height: 1.7; color: var(--text-primary); white-space: pre-wrap; font-family: inherit;">
                  ${escapeHtml(submission.text_answer)}
                </div>
              ` : `
                <div style="font-size: 0.8125rem; color: var(--text-muted); font-style: italic; padding: 1rem; background: var(--neutral-50); border-radius: var(--radius-md);">
                  (Học viên không để lại nội dung văn bản)
                </div>
              `}

              <!-- Audio Player nếu có -->
              ${submission.audio_url ? `
                <div style="margin-top: 1rem; padding: 0.75rem 1rem; background: #EFF6FF; border: 1px solid #BFDBFE; border-radius: var(--radius-md);">
                  <div style="font-size: 0.75rem; font-weight: 700; color: #1D4ED8; margin-bottom: 6px; display: flex; align-items: center; gap: 6px;">
                    <span>${icon('volume', { size: 14 })}</span> Bản ghi âm bài nói của học viên:
                  </div>
                  <audio controls style="width: 100%; height: 36px;">
                    <source src="${escapeHtml(submission.audio_url)}" type="audio/mpeg">
                    Trình duyệt của bạn không hỗ trợ thẻ audio.
                  </audio>
                </div>
              ` : ''}
            </div>

          </div>

          <!-- Cột Phải: Panel Nhập điểm & Nhận xét của Giáo viên (2/5) -->
          <div style="grid-column: span 2;">
            <div class="card" style="padding: 1.25rem 1.5rem; position: sticky; top: 1.5rem; border: 2px solid var(--primary-200);">
              <h3 style="margin: 0 0 1rem 0; font-size: 1.0625rem; font-weight: 800; color: var(--text-primary); display: flex; align-items: center; gap: 8px;">
                <span style="color: var(--primary-600);">${icon('edit', { size: 18 })}</span>
                Đánh Giá & Nhận Xét
              </h3>

              <form id="form-grading-submit" style="display: flex; flex-direction: column; gap: 1.25rem;">
                <!-- Nhập điểm -->
                <div>
                  <label class="form-label" style="display: flex; justify-content: space-between; align-items: center;">
                    <span style="font-weight: 700;">Điểm số đạt được: <span class="text-danger">*</span></span>
                    <span style="font-size: 0.75rem; color: var(--text-muted);">Tối đa: <strong>${submission.max_points} đ</strong></span>
                  </label>
                  <div style="display: flex; align-items: center; gap: 8px;">
                    <input type="number" id="grade-score-input" class="form-control" min="0" max="${submission.max_points}" step="0.5" required placeholder="0 - ${submission.max_points}" style="font-size: 1.25rem; font-weight: 800; color: var(--primary-700); height: 48px; text-align: center;">
                    <span style="font-weight: 800; font-size: 1.125rem; color: var(--text-muted);">/ ${submission.max_points} đ</span>
                  </div>
                  <div id="grade-score-error" style="font-size: 0.75rem; color: var(--danger-main); margin-top: 4px; display: none;">
                    Điểm số không được vượt quá ${submission.max_points} đ!
                  </div>
                </div>

                <!-- Nhận xét chi tiết -->
                <div>
                  <label class="form-label" style="font-weight: 700;">Nhận xét chuyên môn cho học sinh:</label>
                  <textarea id="grade-feedback-input" class="form-control" rows="5" placeholder="Nhận xét chi tiết về từ vựng, ngữ pháp, tính mạch lạc hoặc phát âm..."></textarea>
                </div>

                <!-- Phím gợi ý nhận xét nhanh (Quick Feedback Snippets) -->
                <div>
                  <div style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 6px; font-weight: 600;">Gợi ý nhận xét nhanh:</div>
                  <div style="display: flex; flex-wrap: wrap; gap: 6px;">
                    <button type="button" class="btn btn-sm btn-ghost btn-quick-feedback" data-text="Bài viết tốt, bố cục rõ ràng, sử dụng từ vựng phong phú.">
                      👍 Tốt & mạch lạc
                    </button>
                    <button type="button" class="btn btn-sm btn-ghost btn-quick-feedback" data-text="Cần lưu ý lỗi chia thì quá khứ và cách nối câu linh hoạt hơn.">
                      ⚠️ Lỗi chia thì
                    </button>
                    <button type="button" class="btn btn-sm btn-ghost btn-quick-feedback" data-text="Ý tưởng hay và sáng tạo, cần phát triển thêm ví dụ thực tế.">
                      💡 Cần thêm ví dụ
                    </button>
                  </div>
                </div>

                <!-- Action Buttons -->
                <div style="padding-top: 0.5rem; border-top: 1px solid var(--border-color); display: flex; flex-direction: column; gap: 8px;">
                  <button type="submit" class="btn btn-primary" style="height: 44px; font-weight: 700; justify-content: center; font-size: 0.875rem;">
                    <span class="btn-icon-left">${icon('check', { size: 16 })}</span> Lưu điểm & Chấm bài kế tiếp
                  </button>
                  <button type="button" class="btn btn-ghost btn-back-to-queue" style="font-size: 0.8125rem; justify-content: center;">
                    Bỏ qua & Quay lại hàng đợi
                  </button>
                </div>
              </form>
            </div>
          </div>

        </div>
      </div>
    `;
  }
}
