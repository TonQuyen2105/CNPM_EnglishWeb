/**
 * DashboardController - Điều phối dữ liệu và logic Bảng điều khiển Quản trị viên
 */

import { DashboardView } from '../../views/admin/DashboardView.js';
import {
  UserModel,
  TeacherModel,
  CourseModel,
  CourseClassModel,
  EnrollmentModel
} from '../../models/index.js';
import { toast } from '../../ui/toast.js';

export class DashboardController {
  static async render(params, query, container) {
    try {
      // 1. Tải dữ liệu từ các Models
      const [
        students,
        teachersWithUser,
        classesWithDetails,
        courses,
        enrollmentStats
      ] = await Promise.all([
        UserModel.getStudents(),
        TeacherModel.getAllWithUser(),
        CourseClassModel.getAllWithDetails(),
        CourseModel.getAll(),
        EnrollmentModel.calculateEstimatedRevenue()
      ]);

      const activeStudents = students.filter(s => s.is_active !== 0 && s.is_active !== false);
      const unverifiedTeachers = teachersWithUser.filter(t => t.is_verified === 0 || t.is_verified === false);
      const ongoingClasses = classesWithDetails.filter(c => c.status === 'ongoing');
      const upcomingClasses = classesWithDetails.filter(c => c.status === 'upcoming');

      // 2. Chuẩn bị dữ liệu biểu đồ cột: Đăng ký theo các tháng gần nhất
      const monthlyChartData = [
        { label: 'Tháng 5', value: 18 },
        { label: 'Tháng 6', value: 24 },
        { label: 'Tháng 7', value: 31 },
        { label: 'Tháng 8', value: 28 },
        { label: 'Tháng 9', value: 45 },
        { label: 'Tháng 10', value: 36 }
      ];

      // 3. Chuẩn bị dữ liệu biểu đồ Donut: Cấp độ khóa học
      const beginnerCount = courses.filter(c => c.level === 'beginner').length;
      const interCount = courses.filter(c => c.level === 'intermediate').length;
      const advCount = courses.filter(c => c.level === 'advanced').length;

      const levelChartData = [
        { label: 'Cơ bản (Beginner)', value: beginnerCount, color: '#3B82F6' },
        { label: 'Trung cấp (Intermediate)', value: interCount, color: '#F59E0B' },
        { label: 'Nâng cao (Advanced)', value: advCount, color: '#EF4444' }
      ];

      // 4. Lấy danh sách đăng ký mới nhất (tối đa 5 dòng)
      const allEnrollments = await EnrollmentModel.getAll();
      const recentList = [...allEnrollments].reverse().slice(0, 5).map(e => {
        const student = students.find(s => s.id === e.user_id) || {};
        const cls = classesWithDetails.find(c => c.id === e.class_id) || {};
        return {
          ...e,
          student_name: student.full_name || 'Học viên',
          student_email: student.email || '',
          class_code: cls.class_code || 'Lớp',
          class_name: cls.class_name || ''
        };
      });

      // 5. Render View
      container.innerHTML = DashboardView.render({
        stats: {
          activeStudents: activeStudents.length,
          totalStudents: students.length,
          totalTeachers: teachersWithUser.length,
          unverifiedCount: unverifiedTeachers.length,
          ongoingClasses: ongoingClasses.length,
          upcomingClassesCount: upcomingClasses.length,
          estimatedRevenue: enrollmentStats.totalEstimated,
          totalEnrollments: enrollmentStats.count
        },
        monthlyChartData,
        levelChartData,
        recentEnrollments: recentList,
        unverifiedTeachers,
        upcomingClasses
      });

      // 6. Gắn các sự kiện (Events)
      this.attachEvents(container);

    } catch (err) {
      console.error('Lỗi khi tải Dashboard:', err);
      container.innerHTML = `<div class="card p-6 text-danger">Có lỗi khi tải dữ liệu bảng điều khiển.</div>`;
    }
  }

  static attachEvents(container) {
    // Sự kiện nút làm mới
    const refreshBtn = container.querySelector('#btn-refresh-dashboard');
    if (refreshBtn) {
      refreshBtn.addEventListener('click', () => {
        this.render({}, {}, container);
        toast.info('Đã cập nhật dữ liệu bảng điều khiển mới nhất.');
      });
    }

    // Sự kiện duyệt nhanh giáo viên
    const verifyBtns = container.querySelectorAll('.btn-quick-verify');
    verifyBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const teacherId = btn.getAttribute('data-id');
        btn.classList.add('loading');
        btn.disabled = true;

        const updated = await TeacherModel.update(teacherId, { is_verified: 1 });
        if (updated) {
          toast.success('Đã xác minh hồ sơ giảng viên thành công!');
          this.render({}, {}, container);
        } else {
          btn.classList.remove('loading');
          btn.disabled = false;
          toast.error('Không thể cập nhật hồ sơ giảng viên.');
        }
      });
    });
  }
}
