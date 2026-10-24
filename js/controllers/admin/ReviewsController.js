/**
 * ReviewsController - Điều phối quản lý đánh giá khóa học
 */

import { ReviewsView } from '../../views/admin/ReviewsView.js';
import { CourseReviewModel, CourseModel, UserModel } from '../../models/index.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';

export class ReviewsController {
  static async render(params, query, container) {
    this.container = container;
    this.reviews = [];
    this.courses = [];
    this.users = [];
    this.courseFilter = 'all';
    this.ratingFilter = 'all';

    await this.initData();
  }

  static async initData() {
    const [allReviews, allCourses, allUsers] = await Promise.all([
      CourseReviewModel.getAll(),
      CourseModel.getAll(),
      UserModel.getAll()
    ]);

    this.courses = allCourses;
    this.users = allUsers;

    this.reviews = allReviews.map(r => {
      const u = allUsers.find(user => user.id === r.user_id) || {};
      const c = allCourses.find(course => course.id === r.course_id) || {};
      return {
        ...r,
        user_name: u.full_name || 'Học viên',
        avatar_url: u.avatar_url || '',
        course_title: c.title || 'Khóa học'
      };
    });

    this.container.innerHTML = ReviewsView.renderLayout(this.courses);
    this.renderList();
    this.bindEvents();
  }

  static renderList() {
    const listOutlet = this.container.querySelector('#reviews-list-outlet');
    if (!listOutlet) return;

    let filtered = [...this.reviews];

    if (this.courseFilter !== 'all') {
      filtered = filtered.filter(r => String(r.course_id) === String(this.courseFilter));
    }

    if (this.ratingFilter !== 'all') {
      filtered = filtered.filter(r => Number(r.rating) === Number(this.ratingFilter));
    }

    if (filtered.length === 0) {
      listOutlet.innerHTML = `
        <div class="card p-8 text-center text-muted">
          Không có đánh giá nào phù hợp với bộ lọc hiện tại.
        </div>
      `;
      return;
    }

    listOutlet.innerHTML = filtered.map(r => ReviewsView.renderReviewCard(r)).join('');
    this.bindCardEvents(listOutlet);
  }

  static bindEvents() {
    const courseFilter = this.container.querySelector('#review-course-filter');
    const ratingFilter = this.container.querySelector('#review-rating-filter');
    const resetBtn = this.container.querySelector('#btn-reset-review-filters');

    if (courseFilter) {
      courseFilter.addEventListener('change', (e) => {
        this.courseFilter = e.target.value;
        this.renderList();
      });
    }

    if (ratingFilter) {
      ratingFilter.addEventListener('change', (e) => {
        this.ratingFilter = e.target.value;
        this.renderList();
      });
    }

    if (resetBtn) {
      resetBtn.addEventListener('click', () => {
        this.courseFilter = 'all';
        this.ratingFilter = 'all';
        courseFilter.value = 'all';
        ratingFilter.value = 'all';
        this.renderList();
        toast.info('Đã đặt lại bộ lọc đánh giá.');
      });
    }
  }

  static bindCardEvents(container) {
    const delBtns = container.querySelectorAll('.btn-delete-review');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const ok = await confirmDialog({
          title: 'Xóa đánh giá này?',
          message: 'Bạn có chắc chắn muốn xóa đánh giá này khỏi hệ thống không?',
          confirmText: 'Xóa đánh giá',
          isDanger: true
        });

        if (ok) {
          await CourseReviewModel.delete(id);
          toast.success('Đã xóa đánh giá thành công!');
          await this.initData();
        }
      });
    });
  }
}
