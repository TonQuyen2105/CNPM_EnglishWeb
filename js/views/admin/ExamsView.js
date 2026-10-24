/**
 * ExamsView - Giao diện Quản lý Ngân hàng Đề thi
 */

import { icon } from '../../ui/icons.js';
import { formatDate, escapeHtml } from '../../core/utils.js';

export class ExamsView {
  static renderLayout() {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Ngân hàng đề thi & Khảo thí</h1>
          <p class="page-header-subtitle">Tổng quan các bài kiểm tra định kỳ, đề thi thử IELTS/TOEIC và tỷ lệ đạt của học viên</p>
        </div>
      </div>

      <!-- Toolbar Tìm kiếm -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-8">
            <div class="input-with-icon">
              <span class="input-icon">${icon('search', { size: 16 })}</span>
              <input type="text" id="exam-search-input" class="form-control" placeholder="Tìm theo tiêu đề đề thi...">
            </div>
          </div>
          <div class="col-span-4">
            <select id="exam-status-filter" class="form-control">
              <option value="all">Tất cả trạng thái xuất bản</option>
              <option value="published">Đang mở cho thi (Published)</option>
              <option value="draft">Bản nháp (Draft)</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Exams Grid Outlet -->
      <div id="exams-grid-outlet" class="grid grid-cols-3 gap-6"></div>
    `;
  }

  static renderExamCard(exam, stats) {
    const isPub = exam.is_published === 1 || exam.is_published === true;
    return `
      <div class="card card-hover" style="display: flex; flex-direction: column;">
        <div class="card-body" style="flex: 1;">
          <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem;">
            <span class="badge ${isPub ? 'badge-success' : 'badge-neutral'}">
              <span class="badge-dot"></span>${isPub ? 'Đang mở' : 'Bản nháp'}
            </span>
            <label class="form-switch" title="Bật/Tắt xuất bản">
              <input type="checkbox" class="exam-publish-switch" data-id="${exam.id}" ${isPub ? 'checked' : ''}>
              <span class="switch-slider"></span>
            </label>
          </div>

          <h3 style="font-size: 1.0625rem; font-weight: 700; margin-bottom: 0.5rem; line-height: 1.3;">
            ${escapeHtml(exam.title)}
          </h3>

          <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5; margin-bottom: 1rem; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
            ${escapeHtml(exam.description || 'Chưa có mô tả chi tiết.')}
          </p>

          <div style="display: flex; flex-direction: column; gap: 4px; font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 1rem; background: var(--neutral-50); padding: 0.75rem; border-radius: var(--radius-md);">
            <div>⏱ Thời lượng: <strong>${exam.duration_minutes} phút</strong> • Điểm đạt: <strong>${exam.pass_score}đ</strong></div>
            <div>👨‍🏫 Tạo bởi: <strong>${escapeHtml(exam.teacher_name || 'Giáo viên')}</strong></div>
            <div>📚 Thuộc khóa: <strong>${escapeHtml(exam.course_title || 'Tự do')}</strong></div>
          </div>

          <!-- Thống kê lượt thi & tỷ lệ đạt -->
          <div style="border-top: 1px dashed var(--border-color); padding-top: 0.75rem; display: flex; justify-content: space-between; align-items: center; font-size: 0.8125rem;">
            <div>
              <span style="color: var(--text-muted);">Số lượt thi:</span>
              <strong style="margin-left: 4px;">${stats.totalAttempts}</strong>
            </div>
            <div>
              <span style="color: var(--text-muted);">Tỉ lệ đạt:</span>
              <strong class="badge ${stats.passRate >= 70 ? 'badge-success' : stats.passRate >= 50 ? 'badge-warning' : 'badge-danger'}">
                ${stats.passRate}%
              </strong>
            </div>
          </div>
        </div>

        <div class="card-footer" style="display: flex; justify-content: flex-end; background: var(--neutral-50);">
          <button type="button" class="btn btn-secondary btn-sm btn-preview-exam" data-id="${exam.id}">
            ${icon('eye', { size: 14 })}
            <span>Xem chi tiết đề thi</span>
          </button>
        </div>
      </div>
    `;
  }
}
