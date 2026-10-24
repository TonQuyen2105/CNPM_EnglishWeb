/**
 * TeacherResultsView - Giao diện Báo cáo Kết quả Học viên & Biểu đồ SVG phân bố điểm
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherResultsView {
  static render(data) {
    const { 
      results, 
      exams, 
      classes, 
      selectedExamId, 
      selectedClassId, 
      selectedStatus,
      distributionData,
      summaryStats 
    } = data;

    return `
      <div class="teacher-results">
        <!-- Header -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Kết Quả Khảo Thí Học Viên
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Theo dõi điểm số, phổ điểm phân bố và chi tiết bài làm của từng học viên
            </p>
          </div>
        </div>

        <!-- Filter Bar -->
        <div class="card" style="padding: 1rem 1.25rem; margin-bottom: 1.5rem;">
          <div class="grid grid-cols-3 gap-4">
            <!-- Filter Đề thi -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Đề thi:</label>
              <select id="filter-result-exam" class="form-control" style="font-size: 0.8125rem;">
                <option value="all">Tất cả đề thi của tôi</option>
                ${exams.map(e => `
                  <option value="${e.id}" ${String(e.id) === String(selectedExamId) ? 'selected' : ''}>
                    ${escapeHtml(e.title)}
                  </option>
                `).join('')}
              </select>
            </div>

            <!-- Filter Lớp học -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Lớp học:</label>
              <select id="filter-result-class" class="form-control" style="font-size: 0.8125rem;">
                <option value="all">Tất cả các lớp phụ trách</option>
                ${classes.map(c => `
                  <option value="${c.id}" ${String(c.id) === String(selectedClassId) ? 'selected' : ''}>
                    ${escapeHtml(c.class_code)} - ${escapeHtml(c.course_title)}
                  </option>
                `).join('')}
              </select>
            </div>

            <!-- Filter Kết quả -->
            <div>
              <label class="form-label" style="font-size: 0.75rem; margin-bottom: 4px; font-weight: 700;">Kết quả:</label>
              <select id="filter-result-status" class="form-control" style="font-size: 0.8125rem;">
                <option value="all" ${selectedStatus === 'all' ? 'selected' : ''}>Tất cả trạng thái</option>
                <option value="passed" ${selectedStatus === 'passed' ? 'selected' : ''}>Đạt yêu cầu (Passed)</option>
                <option value="failed" ${selectedStatus === 'failed' ? 'selected' : ''}>Chưa đạt (Failed)</option>
                <option value="submitted" ${selectedStatus === 'submitted' ? 'selected' : ''}>Chờ duyệt tự luận</option>
              </select>
            </div>
          </div>
        </div>

        <!-- 3 Summary Stat Cards -->
        <div class="grid grid-cols-3 gap-4" style="margin-bottom: 1.5rem;">
          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Tổng lượt nộp bài</span>
              <span class="stat-value">${summaryStats.totalAttempts}</span>
              <span class="stat-sub text-muted">Lượt làm bài</span>
            </div>
            <div class="stat-icon primary">${icon('file-text', { size: 22 })}</div>
          </div>

          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Tỉ lệ đạt (Pass Rate)</span>
              <span class="stat-value text-success">${summaryStats.passRate}%</span>
              <span class="stat-sub text-success">
                <span class="badge-dot"></span> ${summaryStats.passedCount}/${summaryStats.gradedCount || 1} bài đạt
              </span>
            </div>
            <div class="stat-icon success">${icon('check-circle', { size: 22 })}</div>
          </div>

          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Điểm trung bình</span>
              <span class="stat-value" style="color: var(--accent-600);">${summaryStats.avgScore} đ</span>
              <span class="stat-sub text-muted">Thang điểm 100</span>
            </div>
            <div class="stat-icon warning">${icon('trending', { size: 22 })}</div>
          </div>
        </div>

        <!-- Biểu đồ Phân Bố Điểm SVG Thuần -->
        <div class="card" style="padding: 1.25rem 1.5rem; margin-bottom: 1.5rem;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.5rem;">
            <h3 style="margin: 0; font-size: 1rem; font-weight: 700; display: flex; align-items: center; gap: 6px;">
              <span style="color: var(--primary-600);">${icon('trending', { size: 18 })}</span>
              Phổ Điểm Thi (Phân Bố Số Lượng Theo Khoảng Điểm)
            </h3>
            <span style="font-size: 0.75rem; color: var(--text-muted);">Biểu đồ cột SVG</span>
          </div>

          ${TeacherResultsView.renderSvgDistributionChart(distributionData)}
        </div>

        <!-- Bảng Kết Quả Chi Tiết -->
        <div class="card" style="padding: 0; overflow: hidden;">
          <div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border-color); display: flex; justify-content: space-between; align-items: center;">
            <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700;">
              Danh sách chi tiết bài làm (${results.length})
            </h3>
          </div>

          ${results.length > 0 ? `
            <div class="table-responsive">
              <table class="table" style="margin: 0;">
                <thead>
                  <tr>
                    <th>Học viên</th>
                    <th>Lớp học</th>
                    <th>Đề thi</th>
                    <th>Thời gian nộp</th>
                    <th style="text-align: center;">Điểm số</th>
                    <th>Kết quả</th>
                    <th style="width: 140px; text-align: right;">Chi tiết</th>
                  </tr>
                </thead>
                <tbody>
                  ${results.map(r => `
                    <tr>
                      <td>
                        <div style="display: flex; align-items: center; gap: 8px;">
                          <div class="avatar avatar-sm" style="background: var(--neutral-100); color: var(--neutral-800); font-weight: 700;">
                            ${escapeHtml(r.student_name.slice(0, 2).toUpperCase())}
                          </div>
                          <div>
                            <strong style="color: var(--text-primary); font-size: 0.875rem;">${escapeHtml(r.student_name)}</strong>
                            <div style="font-size: 0.75rem; color: var(--text-muted);">${escapeHtml(r.student_email)}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span class="badge badge-secondary" style="font-size: 0.75rem;">
                          ${escapeHtml(r.class_code || 'Tự do')}
                        </span>
                      </td>
                      <td>
                        <strong style="color: var(--text-primary); font-size: 0.8125rem;">${escapeHtml(r.exam_title)}</strong>
                      </td>
                      <td style="font-size: 0.8125rem; color: var(--text-secondary);">
                        ${formatDate(r.submitted_at || r.started_at)}
                      </td>
                      <td style="text-align: center;">
                        ${r.total_score !== null ? `
                          <strong style="font-size: 1rem; color: ${r.total_score >= r.pass_score ? 'var(--success-main)' : 'var(--danger-main)'};">
                            ${r.total_score}
                          </strong>
                          <span style="font-size: 0.75rem; color: var(--text-muted);">/${r.pass_score}đ pass</span>
                        ` : `
                          <span class="badge badge-warning" style="font-size: 0.75rem;">Chờ chấm</span>
                        `}
                      </td>
                      <td>
                        <span class="badge ${TeacherResultsView.getStatusBadgeClass(r.status)}">
                          ${TeacherResultsView.getStatusLabel(r.status)}
                        </span>
                      </td>
                      <td style="text-align: right;">
                        <button type="button" class="btn btn-sm btn-outline btn-view-submission" data-user-exam-id="${r.id}" style="font-size: 0.75rem; padding: 4px 8px;">
                          Xem bài làm &rarr;
                        </button>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          ` : `
            <div style="text-align: center; padding: 3rem; color: var(--text-muted);">
              Chưa có kết quả làm bài nào phù hợp với bộ lọc hiện tại.
            </div>
          `}
        </div>
      </div>
    `;
  }

  /**
   * Vẽ biểu đồ phân bố điểm số bằng SVG thuần
   */
  static renderSvgDistributionChart(data = []) {
    const width = 600;
    const height = 180;
    const padding = { top: 25, right: 30, bottom: 35, left: 40 };

    const maxCount = Math.max(...data.map(d => d.count), 5);
    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;
    const barWidth = 60;
    const step = chartWidth / data.length;

    return `
      <div style="display: flex; justify-content: center; width: 100%; overflow-x: auto;">
        <svg viewBox="0 0 ${width} ${height}" style="max-width: 650px; width: 100%; height: auto; font-family: inherit;">
          <!-- Trục hoành Grid Line -->
          <line x1="${padding.left}" y1="${height - padding.bottom}" x2="${width - padding.right}" y2="${height - padding.bottom}" stroke="#E2E8F0" stroke-width="1.5" />

          ${data.map((d, i) => {
            const barH = d.count > 0 ? (d.count / maxCount) * chartHeight : 2;
            const x = padding.left + i * step + (step - barWidth) / 2;
            const y = height - padding.bottom - barH;

            // Màu cột: đỏ nếu kém, xanh lá nếu cao
            let barFill = 'url(#grad-blue)';
            if (i === 0) barFill = 'url(#grad-red)';
            if (i >= 3) barFill = 'url(#grad-green)';

            return `
              <g>
                <!-- Cột -->
                <rect x="${x}" y="${y}" width="${barWidth}" height="${barH}" rx="4" fill="${barFill}" />
                
                <!-- Số lượng trên đầu cột -->
                <text x="${x + barWidth / 2}" y="${y - 6}" font-size="11" font-weight="700" text-anchor="middle" fill="#475569">
                  ${d.count} hv
                </text>

                <!-- Nhãn trục hoành -->
                <text x="${x + barWidth / 2}" y="${height - padding.bottom + 18}" font-size="10.5" font-weight="600" text-anchor="middle" fill="#64748B">
                  ${d.range}
                </text>
              </g>
            `;
          }).join('')}

          <!-- Định nghĩa Gradient màu -->
          <defs>
            <linearGradient id="grad-blue" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="#3B82F6" />
              <stop offset="100%" stop-color="#1D4ED8" />
            </linearGradient>
            <linearGradient id="grad-green" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="#10B981" />
              <stop offset="100%" stop-color="#047857" />
            </linearGradient>
            <linearGradient id="grad-red" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="#F87171" />
              <stop offset="100%" stop-color="#DC2626" />
            </linearGradient>
          </defs>
        </svg>
      </div>
    `;
  }

  static getStatusBadgeClass(status) {
    switch (status) {
      case 'passed': return 'badge-success';
      case 'failed': return 'badge-danger';
      case 'submitted': return 'badge-warning';
      default: return 'badge-secondary';
    }
  }

  static getStatusLabel(status) {
    switch (status) {
      case 'passed': return '✓ Đạt (Passed)';
      case 'failed': return '✕ Chưa đạt';
      case 'submitted': return '⏳ Chờ duyệt';
      default: return 'Đang thi';
    }
  }

  /**
   * Render Modal Chi Tiết Toàn Bộ Bài Làm Của Thí Sinh
   */
  static renderSubmissionDetail(userExam, answers = []) {
    return `
      <div style="display: flex; flex-direction: column; gap: 1rem;">
        <!-- Header Info -->
        <div style="background: var(--neutral-50); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 0.875rem 1rem; display: flex; justify-content: space-between; align-items: center;">
          <div>
            <div style="font-weight: 800; font-size: 1rem; color: var(--text-primary);">${escapeHtml(userExam.exam_title)}</div>
            <div style="font-size: 0.75rem; color: var(--text-muted);">Thí sinh: <strong>${escapeHtml(userExam.student_name)}</strong> &bull; Nộp bài: ${formatDate(userExam.submitted_at)}</div>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 1.25rem; font-weight: 800; color: ${userExam.total_score >= userExam.pass_score ? 'var(--success-main)' : 'var(--danger-main)'};">
              ${userExam.total_score !== null ? `${userExam.total_score} đ` : 'Chờ chấm'}
            </div>
            <div style="font-size: 0.75rem; color: var(--text-muted);">Pass: ${userExam.pass_score} đ</div>
          </div>
        </div>

        <!-- Answers List -->
        <div style="max-height: 480px; overflow-y: auto; display: flex; flex-direction: column; gap: 0.875rem; padding-right: 4px;">
          ${answers.map((a, idx) => `
            <div style="border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; background: white;">
              <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 6px;">
                <span class="badge badge-primary" style="font-size: 0.75rem; font-weight: 700;">Câu #${idx + 1} (${a.question_type})</span>
                <span style="font-weight: 700; font-size: 0.8125rem; color: ${a.score_earned > 0 ? 'var(--success-main)' : 'var(--danger-main)'};">
                  ${a.score_earned !== null ? `${a.score_earned} / ${a.max_points || 20} đ` : 'Chưa chấm'}
                </span>
              </div>

              <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 8px;">
                ${escapeHtml(a.question_text)}
              </div>

              <div style="font-size: 0.8125rem; background: var(--neutral-50); padding: 8px 12px; border-radius: var(--radius-sm); margin-bottom: 6px;">
                <strong>Bài làm học sinh:</strong>
                <div style="margin-top: 4px; color: var(--text-primary); white-space: pre-wrap;">
                  ${escapeHtml(a.text_answer || a.selected_option_text || '(Không có câu trả lời)')}
                </div>
              </div>

              ${a.feedback ? `
                <div style="font-size: 0.75rem; color: #1D4ED8; background: #EFF6FF; padding: 6px 10px; border-radius: var(--radius-sm); border-left: 3px solid #3B82F6;">
                  <strong>Nhận xét của giáo viên:</strong> ${escapeHtml(a.feedback)}
                </div>
              ` : ''}
            </div>
          `).join('')}
        </div>
      </div>
    `;
  }
}
