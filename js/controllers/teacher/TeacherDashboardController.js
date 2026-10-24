/**
 * TeacherDashboardController - Điều khiển Bảng điều khiển Giảng viên
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { CourseClassModel } from '../../models/CourseClassModel.js';
import { ClassLessonModel } from '../../models/ClassLessonModel.js';
import { UserExamModel } from '../../models/UserExamModel.js';
import { ExamModel } from '../../models/ExamModel.js';
import { TeacherDashboardView } from '../../views/teacher/TeacherDashboardView.js';

export class TeacherDashboardController {
  /**
   * Khởi tạo và render trang Dashboard cho Giảng viên
   */
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) {
      container.innerHTML = `
        <div class="empty-state">
          <h3>Không tìm thấy hồ sơ giảng viên</h3>
          <p>Tài khoản của bạn chưa được liên kết với hồ sơ giảng viên trong hệ thống.</p>
        </div>
      `;
      return;
    }

    // 1. Lấy danh sách lớp do giáo viên phụ trách
    const myClasses = await CourseClassModel.getByTeacherId(teacher.id);

    // 2. Tính tổng số học viên
    const totalStudents = myClasses.reduce((sum, cls) => sum + (cls.enrolled_count || 0), 0);

    // 3. Lấy danh sách bài thi chờ chấm
    const pendingGrading = await UserExamModel.getPendingReviewSubmissions(teacher.id);

    // 4. Lấy danh sách đề thi do giáo viên tạo
    const myExams = await ExamModel.getByTeacher(teacher.id);

    // 5. Lấy lịch dạy của giáo viên
    const myLessons = await ClassLessonModel.getByTeacher(teacher.id);

    // Lọc lịch dạy hôm nay hoặc các buổi học sắp tới
    const todayStr = new Date().toISOString().slice(0, 10);
    let todaySchedule = myLessons.filter(l => l.schedule_date === todayStr);

    // Nếu hôm nay không có ca, hiển thị 3 buổi học tiếp theo
    if (todaySchedule.length === 0) {
      todaySchedule = myLessons.slice(0, 3);
    }

    const stats = {
      classCount: myClasses.length,
      studentCount: totalStudents,
      pendingGradingCount: pendingGrading.length,
      examCount: myExams.length
    };

    container.innerHTML = TeacherDashboardView.render({
      teacher,
      stats,
      todaySchedule,
      pendingGrading,
      activeClasses: myClasses
    });
  }
}
