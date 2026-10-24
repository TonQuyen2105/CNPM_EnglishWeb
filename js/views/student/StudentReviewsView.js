/**
 * StudentReviewsView - Giao diện Đánh giá khóa học
 */

import { icon } from '../../ui/icons.js';
import { formatDate } from '../../core/utils.js';

export class StudentReviewsView {
  static render({
    enrolledCourses = [],
    myReviews = []
  }) {
    return `
      <div class="student-reviews-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Đánh giá & Nhận xét khóa học</h1>
            <p class="page-header-subtitle">
              Chia sẻ cảm nhận và đánh giá chất lượng các khóa học bạn đã hoặc đang tham gia.
            </p>
          </div>
        </div>

        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (6 cột): Danh sách khóa học có thể viết đánh giá -->
          <div class="col-span-6 flex flex-col gap-4">
            <h3 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
              Khóa học của bạn
            </h3>

            ${enrolledCourses.length > 0 ? `
              <div class="flex flex-col gap-4">
                ${enrolledCourses.map(c => {
                  const existingReview = myReviews.find(r => r.course_id === c.id);

                  return `
                    <div class="card" style="border-radius: var(--radius-xl); padding: 1.25rem; border: 1px solid var(--border-color); background: white;">
                      <div class="flex items-start justify-between gap-4" style="margin-bottom: 0.75rem;">
                        <div>
                          <span class="badge badge-primary" style="font-size: 0.6875rem;">${c.subject_name || 'Khóa học'}</span>
                          <h4 style="font-size: 1rem; font-weight: 800; color: var(--text-primary); margin: 4px 0;">
                            ${c.title}
                          </h4>
                        </div>

                        ${existingReview ? `
                          <span class="badge badge-success" style="font-size: 0.75rem;">
                            Đã đánh giá (${existingReview.rating}★)
                          </span>
                        ` : `
                          <span class="badge badge-warning" style="font-size: 0.75rem;">
                            Chưa nhận xét
                          </span>
                        `}
                      </div>

                      <div style="margin-top: 1rem; padding-top: 0.75rem; border-top: 1px dashed var(--border-color); display: flex; align-items: center; justify-content: space-between;">
                        <span style="font-size: 0.75rem; color: var(--text-muted);">
                          ${existingReview ? `Cập nhật: ${formatDate(existingReview.updated_at || existingReview.created_at)}` : 'Hãy đóng góp ý kiến của bạn'}
                        </span>
                        <button type="button" class="btn btn-outline-primary btn-sm btn-open-review-form" data-course-id="${c.id}" data-course-title="${c.title}" data-rating="${existingReview ? existingReview.rating : 5}" data-comment="${existingReview ? existingReview.comment : ''}" style="border-radius: var(--radius-full); font-weight: 700;">
                          ${existingReview ? 'Chỉnh sửa đánh giá' : 'Viết đánh giá ngay'}
                        </button>
                      </div>
                    </div>
                  `;
                }).join('')}
              </div>
            ` : `
              <div class="card" style="padding: 2.5rem; text-align: center; border-radius: var(--radius-xl);">
                <p style="color: var(--text-muted); margin: 0;">Bạn chưa đăng ký khóa học nào để đánh giá.</p>
              </div>
            `}
          </div>

          <!-- Cột Phải (6 cột): Danh sách Đánh giá đã gửi của tôi -->
          <div class="col-span-6 flex flex-col gap-4">
            <h3 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
              Đánh giá đã gửi (${myReviews.length})
            </h3>

            ${myReviews.length > 0 ? `
              <div class="flex flex-col gap-4">
                ${myReviews.map(rev => `
                  <div class="card" style="border-radius: var(--radius-xl); padding: 1.25rem; border: 1px solid var(--border-color); background: var(--neutral-50);">
                    <div class="flex items-center justify-between" style="margin-bottom: 0.5rem;">
                      <h4 style="font-size: 0.9375rem; font-weight: 700; color: var(--text-primary); margin: 0;">
                        ${rev.course_title || 'Khóa học'}
                      </h4>
                      <div style="color: #F59E0B; font-size: 0.9375rem;">
                        ${'★'.repeat(rev.rating)}${'☆'.repeat(5 - rev.rating)}
                      </div>
                    </div>

                    <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0 0 0.75rem 0; line-height: 1.5;">
                      "${rev.comment || 'Khóa học rất hữu ích.'}"
                    </p>

                    <div style="font-size: 0.7rem; color: var(--text-muted);">
                      Đã gửi ngày ${formatDate(rev.created_at)}
                    </div>
                  </div>
                `).join('')}
              </div>
            ` : `
              <div class="card" style="padding: 2.5rem; text-align: center; border-radius: var(--radius-xl); background: var(--neutral-50);">
                <p style="color: var(--text-muted); margin: 0;">Bạn chưa viết nhận xét nào.</p>
              </div>
            `}
          </div>

        </div>
      </div>
    `;
  }
}
