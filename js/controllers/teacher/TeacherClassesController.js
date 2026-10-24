/**
 * TeacherClassesController - Điều phối Danh sách Lớp phụ trách & Chi tiết Lớp học
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { CourseClassModel } from '../../models/CourseClassModel.js';
import { ClassLessonModel } from '../../models/ClassLessonModel.js';
import { LessonModel } from '../../models/LessonModel.js';
import { EnrollmentModel } from '../../models/EnrollmentModel.js';
import { UserProgressModel } from '../../models/UserProgressModel.js';
import { UserExamModel } from '../../models/UserExamModel.js';
import { TeacherClassesView } from '../../views/teacher/TeacherClassesView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { storage } from '../../core/storage.js';

export class TeacherClassesController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) return;

    // Nếu có classId trong params -> render trang Chi tiết lớp
    if (params && params.id) {
      await TeacherClassesController.renderDetail(container, teacher, params.id);
    } else {
      await TeacherClassesController.renderList(container, teacher);
    }
  }

  /**
   * Render danh sách các lớp
   */
  static async renderList(container, teacher) {
    const classes = await CourseClassModel.getByTeacherId(teacher.id);
    container.innerHTML = TeacherClassesView.renderList(classes);
  }

  /**
   * Render chi tiết một lớp học với 3 Tabs
   */
  static async renderDetail(container, teacher, classId) {
    const myClasses = await CourseClassModel.getByTeacherId(teacher.id);
    const cls = myClasses.find(c => String(c.id) === String(classId));

    if (!cls) {
      container.innerHTML = `
        <div class="empty-state">
          <h3>Không tìm thấy lớp học</h3>
          <p>Lớp học không tồn tại hoặc bạn không được phân công phụ trách lớp này.</p>
          <a href="#/classes" class="btn btn-primary">&larr; Quay lại danh sách</a>
        </div>
      `;
      return;
    }

    // 1. Dữ liệu Tab Học sinh
    const allEnrollments = storage.getTable('enrollments') || [];
    const classEnrollments = allEnrollments.filter(e => e.class_id === cls.id && e.status !== 'cancelled');
    const users = storage.getTable('users') || [];
    const progressList = storage.getTable('user_progress') || [];
    const courseLessons = await LessonModel.getByCourse(cls.course_id);
    const userExams = storage.getTable('user_exams') || [];

    const students = classEnrollments.map(e => {
      const u = users.find(user => user.id === e.user_id) || {};
      
      // Tiến độ bài học
      const userProg = progressList.filter(p => p.user_id === e.user_id && p.status === 'completed');
      const completedCount = userProg.length;
      const progressPercent = courseLessons.length > 0 ? Math.round((completedCount / courseLessons.length) * 100) : 0;

      // Điểm trung bình thi
      const myExams = userExams.filter(ue => ue.user_id === e.user_id && ue.total_score !== null);
      let avgScore = null;
      if (myExams.length > 0) {
        const sum = myExams.reduce((total, item) => total + item.total_score, 0);
        avgScore = Math.round(sum / myExams.length);
      }

      return {
        ...e,
        student_name: u.full_name || 'Học viên',
        student_email: u.email || '',
        completed_count: completedCount,
        total_course_lessons: courseLessons.length,
        progress_percent: progressPercent,
        avg_score: avgScore
      };
    });

    // 2. Dữ liệu Tab Buổi học (class_lessons)
    const lessons = await ClassLessonModel.getByClass(cls.id);

    // 3. Dữ liệu Tab Lịch học
    const schedules = cls.schedules || [];

    // Mặc định tab 'students'
    let activeTab = 'students';

    function renderView() {
      container.innerHTML = TeacherClassesView.renderDetail({
        cls,
        activeTab,
        students,
        lessons,
        schedules,
        courseLessons
      });
      TeacherClassesController.bindDetailEvents(container, teacher, cls, courseLessons, (newTab) => {
        activeTab = newTab;
        renderView();
      });
    }

    renderView();
  }

  /**
   * Gắn các sự kiện tương tác trong trang chi tiết lớp
   */
  static bindDetailEvents(container, teacher, cls, courseLessons, onTabChange) {
    // 1. Chuyển Tab
    container.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const targetTab = btn.getAttribute('data-tab');
        if (targetTab) onTabChange(targetTab);
      });
    });

    // 2. Đổi trạng thái buổi học (Select scheduled | completed | cancelled)
    container.querySelectorAll('.select-session-status').forEach(select => {
      select.addEventListener('change', async (e) => {
        const sessionId = select.getAttribute('data-session-id');
        const newStatus = select.value;
        await ClassLessonModel.updateStatus(sessionId, newStatus);
        toast.success(`Đã cập nhật trạng thái buổi học sang "${newStatus === 'completed' ? 'Đã hoàn thành' : (newStatus === 'scheduled' ? 'Sắp tới' : 'Đã hủy')}"`);
      });
    });

    // 3. Chỉnh sửa ghi chú buổi học (Modal)
    container.querySelectorAll('.btn-edit-notes').forEach(btn => {
      btn.addEventListener('click', () => {
        const sessionId = btn.getAttribute('data-session-id');
        const currentNotes = btn.getAttribute('data-notes') || '';

        modal.open({
          title: 'Ghi chú buổi học',
          content: `
            <form id="form-edit-notes">
              <div class="form-group">
                <label class="form-label">Nội dung ghi chú & nhận xét buổi dạy:</label>
                <textarea id="session-notes-input" class="form-control" rows="4" placeholder="VD: Lớp học sôi nổi, cần ôn lại ngữ pháp thì tương lai đơn...">${currentNotes}</textarea>
              </div>
            </form>
          `,
          confirmText: 'Lưu ghi chú',
          cancelText: 'Hủy',
          onConfirm: async () => {
            const input = document.getElementById('session-notes-input');
            const newNotes = input ? input.value.trim() : '';
            await ClassLessonModel.updateStatus(sessionId, null, newNotes);
            const notesEl = document.getElementById(`notes-text-${sessionId}`);
            if (notesEl) notesEl.textContent = newNotes || 'Chưa có ghi chú nào.';
            btn.setAttribute('data-notes', newNotes);
            toast.success('Đã lưu ghi chú buổi học thành công!');
          }
        });
      });
    });

    // 4. Gán bài học cho buổi học (Modal)
    container.querySelectorAll('.btn-assign-lesson').forEach(btn => {
      btn.addEventListener('click', () => {
        const sessionId = btn.getAttribute('data-session-id');
        const currentLessonId = btn.getAttribute('data-lesson-id');

        const optionsHtml = courseLessons.map(l => `
          <option value="${l.id}" ${String(l.id) === String(currentLessonId) ? 'selected' : ''}>
            Bài ${l.order_index}: ${l.title} (${l.content_type})
          </option>
        `).join('');

        modal.open({
          title: 'Chọn bài học cho buổi này',
          content: `
            <div class="form-group">
              <label class="form-label">Chọn bài học từ giáo trình:</label>
              <select id="select-assign-lesson" class="form-control">
                ${optionsHtml}
              </select>
            </div>
          `,
          confirmText: 'Xác nhận gán',
          cancelText: 'Hủy',
          onConfirm: async () => {
            const selectEl = document.getElementById('select-assign-lesson');
            if (selectEl) {
              const newLessonId = selectEl.value;
              await ClassLessonModel.assignLesson(sessionId, newLessonId);
              toast.success('Đã liên kết bài học thành công!');
              // Re-render chi tiết
              TeacherClassesController.renderDetail(container, teacher, cls.id);
            }
          }
        });
      });
    });
  }
}
