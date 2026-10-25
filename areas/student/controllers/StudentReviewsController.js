/**
 * StudentReviewsController - Điều khiển màn hình Đánh giá khóa học của Học viên
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import { EnrollmentModel, CourseModel, CourseReviewModel } from '../../models/index.js';
import { StudentReviewsView } from '../../views/student/StudentReviewsView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';

export class StudentReviewsController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    const currentUser = auth.getCurrentUser();
    const myEnrollments = await EnrollmentModel.getByUser(currentUser.id);
    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];
    const subjects = storage.getTable('subjects') || [];

    // Lọc danh sách các khóa học học viên đã tham gia
    const courseIdSet = new Set();
    myEnrollments.forEach(e => {
      const cls = classes.find(c => c.id === e.class_id);
      if (cls && cls.course_id) courseIdSet.add(cls.course_id);
    });

    const enrolledCourses = Array.from(courseIdSet).map(cId => {
      const c = courses.find(item => item.id === cId) || {};
      const subj = subjects.find(s => s.id === c.subject_id) || {};
      return {
        ...c,
        subject_name: subj.name || ''
      };
    });

    const myReviews = await CourseReviewModel.getByUser(currentUser.id);

    container.innerHTML = StudentReviewsView.render({
      enrolledCourses,
      myReviews
    });

    this._bindEvents(container, currentUser);
  }

  static _bindEvents(container, currentUser) {
    container.querySelectorAll('.btn-open-review-form').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const courseId = e.currentTarget.getAttribute('data-course-id');
        const courseTitle = e.currentTarget.getAttribute('data-course-title');
        let currentRating = Number(e.currentTarget.getAttribute('data-rating')) || 5;
        const currentComment = e.currentTarget.getAttribute('data-comment') || '';

        modal.open({
          title: `Đánh giá: ${courseTitle}`,
          content: `
            <form id="form-course-review-submit">
              <div class="form-group" style="margin-bottom: 1.25rem; text-align: center;">
                <label class="form-label" style="display: block; margin-bottom: 0.5rem; font-weight: 700;">Mức độ hài lòng của bạn</label>
                <div class="rating-stars-picker" id="modal-star-picker" style="font-size: 2rem; justify-content: center;">
                  ${[1, 2, 3, 4, 5].map(star => `
                    <button type="button" class="rating-star-btn ${star <= currentRating ? 'active' : ''}" data-star="${star}">
                      ★
                    </button>
                  `).join('')}
                </div>
                <input type="hidden" id="modal-rating-val" value="${currentRating}">
              </div>

              <div class="form-group" style="margin-bottom: 1.5rem;">
                <label class="form-label" style="font-weight: 700;">Nhận xét chi tiết (*)</label>
                <textarea id="modal-review-comment" class="form-control" rows="4" required placeholder="Chia sẻ trải nghiệm học tập, chất lượng bài giảng và giảng viên...">${currentComment}</textarea>
              </div>

              <div style="display: flex; justify-content: flex-end; gap: 8px;">
                <button type="button" class="btn btn-secondary" id="btn-modal-cancel">Hủy</button>
                <button type="submit" class="btn btn-primary">Gửi đánh giá</button>
              </div>
            </form>
          `
        });

        const starPicker = document.getElementById('modal-star-picker');
        const ratingInput = document.getElementById('modal-rating-val');
        if (starPicker && ratingInput) {
          starPicker.querySelectorAll('.rating-star-btn').forEach(starBtn => {
            starBtn.addEventListener('click', () => {
              const selectedStar = Number(starBtn.getAttribute('data-star'));
              ratingInput.value = selectedStar;
              currentRating = selectedStar;
              starPicker.querySelectorAll('.rating-star-btn').forEach(s => {
                const sVal = Number(s.getAttribute('data-star'));
                if (sVal <= selectedStar) {
                  s.classList.add('active');
                } else {
                  s.classList.remove('active');
                }
              });
            });
          });
        }

        const cancelBtn = document.getElementById('btn-modal-cancel');
        if (cancelBtn) cancelBtn.addEventListener('click', () => modal.close());

        const form = document.getElementById('form-course-review-submit');
        if (form) {
          form.addEventListener('submit', async (ev) => {
            ev.preventDefault();
            const rating = Number(document.getElementById('modal-rating-val').value) || 5;
            const comment = document.getElementById('modal-review-comment').value.trim();

            await CourseReviewModel.submitReview({
              userId: currentUser.id,
              courseId: Number(courseId),
              rating,
              comment
            });

            modal.close();
            toast.success('Cảm ơn bạn đã gửi đánh giá cho khóa học!');
            this.render(container);
          });
        }
      });
    });
  }
}
