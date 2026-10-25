/**
 * StudentMyClassesController - Điều khiển màn hình Lớp của tôi
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  EnrollmentModel,
  CourseClassModel,
  CourseModel,
  LessonModel,
  UserProgressModel
} from '../../models/index.js';
import { StudentMyClassesView } from '../../views/student/StudentMyClassesView.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';

export class StudentMyClassesController {
  static _currentTab = 'active';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    if (params && params.id) {
      return this._renderDetail(container, params.id);
    }

    return this._renderList(container);
  }

  static async _renderList(container) {
    const currentUser = auth.getCurrentUser();
    const myDetailedEnrollments = await EnrollmentModel.getDetailedByUser(currentUser.id);

    container.innerHTML = StudentMyClassesView.renderList({
      enrollments: myDetailedEnrollments,
      currentTab: this._currentTab
    });

    // Tab buttons
    container.querySelectorAll('.tab-item-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        this._currentTab = e.currentTarget.getAttribute('data-tab');
        this._renderList(container);
      });
    });
  }

  static async _renderDetail(container, classId) {
    const currentUser = auth.getCurrentUser();
    const cls = await CourseClassModel.getById(Number(classId));
    if (!cls) {
      container.innerHTML = `
        <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
          <h3>Không tìm thấy lớp học</h3>
          <a href="#/my-classes" class="btn btn-primary" style="margin-top: 1rem;">Quay lại Lớp của tôi</a>
        </div>
      `;
      return;
    }

    const course = await CourseModel.getById(cls.course_id) || {};
    const enrollments = await EnrollmentModel.getAll(e => String(e.user_id) === String(currentUser.id) && String(e.class_id) === String(cls.id));
    const enrollment = enrollments[0] || null;

    const allLessons = storage.getTable('lessons') || [];
    const courseLessons = allLessons
      .filter(l => l.course_id === cls.course_id)
      .sort((a, b) => a.order_index - b.order_index);

    const userProgress = await UserProgressModel.getByUser(currentUser.id);

    container.innerHTML = StudentMyClassesView.renderDetail({
      cls,
      course,
      enrollment,
      lessons: courseLessons,
      userProgress
    });

    const cancelBtn = container.querySelector('#btn-cancel-enrollment');
    if (cancelBtn) {
      cancelBtn.addEventListener('click', async () => {
        const enrollmentId = cancelBtn.getAttribute('data-enrollment-id');
        const confirmed = await confirmDialog({
          title: 'Hủy đăng ký lớp học',
          message: 'Bạn có chắc chắn muốn hủy đăng ký lớp này? Bạn sẽ không còn nhận được lịch học và thông báo từ lớp.',
          confirmText: 'Xác nhận hủy',
          cancelText: 'Giữ lại',
          type: 'danger'
        });

        if (!confirmed) return;

        const res = await EnrollmentModel.cancelEnrollment(enrollmentId, currentUser.id);
        if (res.success) {
          toast.success('Đã hủy đăng ký lớp học.');
          this._renderDetail(container, classId);
        } else {
          toast.error(res.message || 'Không thể hủy lớp học.');
        }
      });
    }
  }
}
