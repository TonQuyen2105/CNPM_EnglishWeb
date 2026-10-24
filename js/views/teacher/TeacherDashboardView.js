/**
 * TeacherDashboardView - Giao diện Bảng điều khiển Giảng viên
 * Lời chào, lịch dạy hôm nay, 4 stat card, danh sách bài chờ chấm, tiến độ lớp học
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherDashboardView {
  /**
   * Render trang Dashboard
   * @param {Object} data 
   * @returns {string} HTML string
   */
  static render(data) {
    const { teacher, stats, todaySchedule, pendingGrading, activeClasses } = data;

    return `
      <div class="teacher-dashboard">
        <!-- Header & Lời chào -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem;">
              <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
                Chào mừng Thầy/Cô ${escapeHtml(teacher.full_name)} 👋
              </h1>
              ${teacher.is_verified 
                ? '<span class="badge badge-success" title="Hồ sơ đã được BQT phê duyệt">✓ Đã xác minh</span>' 
                : '<span class="badge badge-warning" title="Hồ sơ đang chờ phê duyệt">⏳ Chờ duyệt</span>'}
            </div>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Chúc Thầy/Cô một ngày giảng dạy tràn đầy năng lượng và hiệu quả!
            </p>
          </div>

          <div style="display: flex; gap: 0.75rem;">
            <a href="#/lessons" class="btn btn-outline" style="font-size: 0.8125rem;">
              <span class="btn-icon-left">${icon('book-open', { size: 16 })}</span> Soạn bài giảng
            </a>
            <a href="#/grading" class="btn btn-primary" style="font-size: 0.8125rem;">
              <span class="btn-icon-left">${icon('edit', { size: 16 })}</span> Chấm bài tự luận
              ${stats.pendingGradingCount > 0 ? `<span class="badge badge-warning" style="background: white; color: var(--primary-600); font-weight: 700; margin-left: 4px;">${stats.pendingGradingCount}</span>` : ''}
            </a>
          </div>
        </div>

        <!-- 4 Stat Cards -->
        <div class="grid grid-cols-4 gap-4" style="margin-bottom: 1.75rem;">
          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Lớp đang dạy</span>
              <span class="stat-value">${stats.classCount}</span>
              <span class="stat-sub text-success">
                <span class="badge-dot"></span> Đang hoạt động
              </span>
            </div>
            <div class="stat-icon primary">
              ${icon('layers', { size: 22 })}
            </div>
          </div>

          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Tổng học sinh</span>
              <span class="stat-value">${stats.studentCount}</span>
              <span class="stat-sub text-muted">Học viên chính thức</span>
            </div>
            <div class="stat-icon info">
              ${icon('users', { size: 22 })}
            </div>
          </div>

          <div class="stat-card" style="${stats.pendingGradingCount > 0 ? 'border-color: #FCD34D;' : ''}">
            <div class="stat-info">
              <span class="stat-label">Bài chờ chấm</span>
              <span class="stat-value" style="${stats.pendingGradingCount > 0 ? 'color: var(--warning-main);' : ''}">
                ${stats.pendingGradingCount}
              </span>
              <span class="stat-sub ${stats.pendingGradingCount > 0 ? 'text-warning' : 'text-success'}">
                ${stats.pendingGradingCount > 0 ? '⚠️ Cần xử lý sớm' : '✓ Đã hoàn tất'}
              </span>
            </div>
            <div class="stat-icon warning">
              ${icon('edit', { size: 22 })}
            </div>
          </div>

          <div class="stat-card">
            <div class="stat-info">
              <span class="stat-label">Đề thi đã tạo</span>
              <span class="stat-value">${stats.examCount}</span>
              <span class="stat-sub text-muted">Ngân hàng đề của tôi</span>
            </div>
            <div class="stat-icon success">
              ${icon('award', { size: 22 })}
            </div>
          </div>
        </div>

        <!-- Layout 2 Cột: Lịch hôm nay & Tiến độ (Trái) + Bài chờ chấm (Phải) -->
        <div class="grid grid-cols-3 gap-6">
          <!-- Cột Trái (2 phần) -->
          <div style="grid-column: span 2; display: flex; flex-direction: column; gap: 1.5rem;">
            
            <!-- Box: Lịch dạy hôm nay -->
            <div class="card" style="padding: 1.25rem 1.5rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
                <div>
                  <h3 style="font-size: 1rem; font-weight: 700; margin: 0; display: flex; align-items: center; gap: 8px;">
                    <span style="color: var(--primary-600);">${icon('calendar', { size: 18 })}</span>
                    Lịch dạy hôm nay & Buổi học sắp tới
                  </h3>
                  <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 2px;">
                    Khung thời gian biểu các lớp bạn phụ trách
                  </div>
                </div>
                <a href="#/schedule" class="btn btn-sm btn-ghost" style="font-size: 0.75rem;">
                  Xem toàn tuần &rarr;
                </a>
              </div>

              ${todaySchedule && todaySchedule.length > 0 ? `
                <div class="timeline" style="display: flex; flex-direction: column; gap: 1rem;">
                  ${todaySchedule.map(item => `
                    <div style="display: flex; gap: 1rem; align-items: flex-start; padding: 0.875rem 1rem; background-color: var(--neutral-50); border-radius: var(--radius-md); border-left: 4px solid ${item.status === 'completed' ? 'var(--success-main)' : 'var(--primary-600)'};">
                      <div style="min-width: 95px; text-align: center; border-right: 1px solid var(--border-color); padding-right: 0.75rem;">
                        <div style="font-weight: 800; font-size: 0.9375rem; color: var(--text-primary);">${escapeHtml(item.start_time)}</div>
                        <div style="font-size: 0.7rem; color: var(--text-muted);">${escapeHtml(item.end_time)}</div>
                        <span class="badge ${item.status === 'completed' ? 'badge-success' : 'badge-primary'}" style="margin-top: 4px; font-size: 0.65rem;">
                          ${item.status === 'completed' ? 'Đã học' : 'Sắp tới'}
                        </span>
                      </div>
                      <div style="flex: 1;">
                        <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                          <div>
                            <div style="font-weight: 700; font-size: 0.875rem; color: var(--text-primary);">
                              <span class="badge badge-info" style="margin-right: 6px; font-weight: 700;">${escapeHtml(item.class_code)}</span>
                              ${escapeHtml(item.course_title)}
                            </div>
                            <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 3px;">
                              <strong>Bài giảng:</strong> ${escapeHtml(item.lesson_title)}
                            </div>
                          </div>
                          <div style="text-align: right; font-size: 0.75rem; color: var(--text-muted);">
                            <div style="display: flex; align-items: center; gap: 4px; justify-content: flex-end;">
                              <span>${icon('map-pin', { size: 13 })}</span>
                              <strong>${escapeHtml(item.room_name)}</strong>
                            </div>
                            <div>${escapeHtml(item.schedule_date ? formatDate(item.schedule_date) : 'Ca định kỳ')}</div>
                          </div>
                        </div>
                        ${item.notes ? `
                          <div style="margin-top: 0.5rem; font-size: 0.75rem; background: white; padding: 4px 8px; border-radius: 4px; border: 1px dashed var(--border-color); color: var(--text-secondary);">
                            📝 <em>${escapeHtml(item.notes)}</em>
                          </div>
                        ` : ''}
                      </div>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <div style="text-align: center; padding: 2rem 1rem; color: var(--text-muted);">
                  <div style="margin-bottom: 0.5rem; color: var(--neutral-400);">${icon('check-circle', { size: 36 })}</div>
                  <div style="font-weight: 600; font-size: 0.875rem;">Hôm nay bạn không có ca dạy nào</div>
                  <div style="font-size: 0.75rem;">Hãy chuẩn bị giáo án cho các buổi học tiếp theo hoặc chấm các bài tập còn tồn đọng.</div>
                </div>
              `}
            </div>

            <!-- Box: Tiến độ các lớp học -->
            <div class="card" style="padding: 1.25rem 1.5rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
                <h3 style="font-size: 1rem; font-weight: 700; margin: 0; display: flex; align-items: center; gap: 8px;">
                  <span style="color: var(--primary-600);">${icon('layers', { size: 18 })}</span>
                  Tiến độ các lớp phụ trách
                </h3>
                <a href="#/classes" class="btn btn-sm btn-ghost" style="font-size: 0.75rem;">
                  Quản lý lớp &rarr;
                </a>
              </div>

              ${activeClasses && activeClasses.length > 0 ? `
                <div style="display: flex; flex-direction: column; gap: 1rem;">
                  ${activeClasses.map(cls => `
                    <div style="border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; transition: transform var(--transition-fast);" class="hover-shadow">
                      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                        <div>
                          <span class="badge ${cls.role === 'main' ? 'badge-primary' : 'badge-secondary'}" style="font-size: 0.7rem; font-weight: 700; margin-right: 6px;">
                            ${cls.role === 'main' ? 'GV Chính' : 'Trợ giảng'}
                          </span>
                          <strong style="font-size: 0.9375rem; color: var(--text-primary);">${escapeHtml(cls.class_code)}</strong>
                          <span style="color: var(--text-muted); font-size: 0.8125rem;"> - ${escapeHtml(cls.course_title)}</span>
                        </div>
                        <a href="#/classes/${cls.id}" class="btn btn-sm btn-outline" style="font-size: 0.75rem; padding: 2px 8px;">
                          Chi tiết &rarr;
                        </a>
                      </div>

                      <div style="display: flex; justify-content: space-between; font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 6px;">
                        <span>Tiến độ buổi học: <strong>${cls.completed_lessons} / ${cls.total_lessons || 1}</strong> buổi (${cls.progress_percent}%)</span>
                        <span>Sĩ số: <strong>${cls.enrolled_count} / ${cls.max_students}</strong> học viên</span>
                      </div>

                      <div style="width: 100%; height: 8px; background: var(--neutral-200); border-radius: var(--radius-full); overflow: hidden;">
                        <div style="width: ${cls.progress_percent}%; height: 100%; background: linear-gradient(90deg, var(--primary-500), var(--primary-700)); border-radius: var(--radius-full);"></div>
                      </div>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <div style="text-align: center; padding: 1.5rem; color: var(--text-muted); font-size: 0.8125rem;">
                  Chưa có lớp nào được phân công.
                </div>
              `}
            </div>

          </div>

          <!-- Cột Phải (1 phần): Danh sách bài thi chờ chấm & Phím tắt -->
          <div style="display: flex; flex-direction: column; gap: 1.5rem;">
            
            <!-- Box: Bài thi tự luận chờ chấm -->
            <div class="card" style="padding: 1.25rem 1.5rem;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
                <h3 style="font-size: 1rem; font-weight: 700; margin: 0; display: flex; align-items: center; gap: 8px;">
                  <span style="color: var(--warning-main);">${icon('edit', { size: 18 })}</span>
                  Bài thi chờ chấm (${pendingGrading.length})
                </h3>
                <a href="#/grading" class="btn btn-sm btn-ghost" style="font-size: 0.75rem;">
                  Chấm tất cả
                </a>
              </div>

              ${pendingGrading && pendingGrading.length > 0 ? `
                <div style="display: flex; flex-direction: column; gap: 0.875rem;">
                  ${pendingGrading.slice(0, 4).map(sub => `
                    <div style="padding: 0.75rem; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: var(--neutral-50);">
                      <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 4px;">
                        <strong style="font-size: 0.8125rem; color: var(--text-primary);">${escapeHtml(sub.student_name)}</strong>
                        <span style="font-size: 0.7rem; color: var(--text-muted);">${formatDate(sub.submitted_at)}</span>
                      </div>
                      <div style="font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 6px;">
                        ${escapeHtml(sub.exam_title)}
                      </div>
                      <div style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 8px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                        <em>"${escapeHtml(sub.text_answer || sub.question_text)}"</em>
                      </div>
                      <a href="#/grading" class="btn btn-sm btn-primary" style="width: 100%; font-size: 0.75rem; padding: 4px 8px; justify-content: center;">
                        Chấm bài ngay &rarr;
                      </a>
                    </div>
                  `).join('')}
                  ${pendingGrading.length > 4 ? `
                    <a href="#/grading" style="text-align: center; font-size: 0.75rem; color: var(--primary-600); font-weight: 600; text-decoration: none;">
                      + Xem thêm ${pendingGrading.length - 4} bài khác
                    </a>
                  ` : ''}
                </div>
              ` : `
                <div style="text-align: center; padding: 2rem 1rem; color: var(--text-muted);">
                  <div style="color: var(--success-main); margin-bottom: 0.5rem;">${icon('check-circle', { size: 36 })}</div>
                  <div style="font-weight: 700; font-size: 0.875rem;">Tất cả bài thi đã được chấm!</div>
                  <div style="font-size: 0.75rem; margin-top: 4px;">Hiện không có học viên nào nộp bài tự luận chờ duyệt.</div>
                </div>
              `}
            </div>

            <!-- Box: Phím tắt tác nghiệp -->
            <div class="card" style="padding: 1.25rem 1.5rem;">
              <h3 style="font-size: 0.9375rem; font-weight: 700; margin: 0 0 1rem 0; color: var(--text-primary);">
                Thao tác nhanh
              </h3>
              <div style="display: flex; flex-direction: column; gap: 0.5rem;">
                <a href="#/questions" class="btn btn-outline" style="justify-content: flex-start; font-size: 0.8125rem;">
                  <span class="btn-icon-left">${icon('plus', { size: 16 })}</span> Thêm câu hỏi trắc nghiệm / tự luận
                </a>
                <a href="#/exams" class="btn btn-outline" style="justify-content: flex-start; font-size: 0.8125rem;">
                  <span class="btn-icon-left">${icon('file-text', { size: 16 })}</span> Thiết kế đề thi mới
                </a>
                <a href="#/results" class="btn btn-outline" style="justify-content: flex-start; font-size: 0.8125rem;">
                  <span class="btn-icon-left">${icon('trending', { size: 16 })}</span> Xem phổ điểm học viên
                </a>
              </div>
            </div>

          </div>
        </div>
      </div>
    `;
  }
}
