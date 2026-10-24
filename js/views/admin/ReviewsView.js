/**
 * ReviewsView - Giao diện Quản lý Đánh giá khóa học
 */

import { icon } from '../../ui/icons.js';
import { formatDate, escapeHtml } from '../../core/utils.js';

export class ReviewsView {
  static renderLayout(courses = []) {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý đánh giá khóa học</h1>
          <p class="page-header-subtitle">Phản hồi và chất lượng giảng dạy từ góc nhìn của học viên</p>
        </div>
      </div>

      <!-- Toolbar Lọc Đánh Giá -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-6">
            <select id="review-course-filter" class="form-control">
              <option value="all">Tất cả các khóa học</option>
              ${courses.map(c => `<option value="${c.id}">${escapeHtml(c.title)}</option>`).join('')}
            </select>
          </div>
          <div class="col-span-4">
            <select id="review-rating-filter" class="form-control">
              <option value="all">Tất cả mức sao đánh giá</option>
              <option value="5">★★★★★ 5 sao (Rất hài lòng)</option>
              <option value="4">★★★★☆ 4 sao (Hài lòng)</option>
              <option value="3">★★★☆☆ 3 sao (Bình thường)</option>
              <option value="2">★★☆☆☆ 2 sao (Chưa đạt)</option>
              <option value="1">★☆☆☆☆ 1 sao (Kém)</option>
            </select>
          </div>
          <div class="col-span-2 text-right">
            <button type="button" class="btn btn-secondary" id="btn-reset-review-filters" style="width: 100%;">
              ${icon('refresh', { size: 14 })}
              <span>Đặt lại</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Reviews List Outlet -->
      <div id="reviews-list-outlet" class="flex flex-col gap-4"></div>
    `;
  }

  static renderReviewCard(r) {
    return `
      <div class="card" style="padding: 1.25rem;">
        <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;">
          <div style="display: flex; gap: 0.75rem; align-items: flex-start;">
            <div class="avatar avatar-md">
              ${r.avatar_url ? `<img src="${r.avatar_url}" alt="${r.user_name}">` : (r.user_name ? r.user_name.charAt(0) : 'U')}
            </div>
            <div>
              <div style="display: flex; align-items: center; gap: 8px;">
                <strong style="font-size: 0.9375rem;">${escapeHtml(r.user_name || 'Học viên')}</strong>
                <span style="font-size: 0.8125rem; color: var(--text-muted);">đã đánh giá khóa học</span>
                <strong style="color: var(--primary-600); font-size: 0.875rem;">${escapeHtml(r.course_title)}</strong>
              </div>
              <div style="display: flex; align-items: center; gap: 6px; margin: 4px 0;">
                <span style="color: var(--warning-main); font-size: 0.875rem;">${'★'.repeat(r.rating)}${'☆'.repeat(5 - r.rating)}</span>
                <span class="badge badge-warning">${r.rating} / 5</span>
                <small style="color: var(--text-muted); margin-left: 6px;">${formatDate(r.created_at)}</small>
              </div>
              <p style="color: var(--text-secondary); margin: 0.5rem 0 0; line-height: 1.5; font-size: 0.875rem;">
                ${escapeHtml(r.comment || 'Không có bình luận văn bản.')}
              </p>
            </div>
          </div>

          <button type="button" class="btn btn-danger-soft btn-sm btn-delete-review" data-id="${r.id}" title="Xóa đánh giá không phù hợp">
            ${icon('trash', { size: 14 })}
            <span>Xóa</span>
          </button>
        </div>
      </div>
    `;
  }
}
