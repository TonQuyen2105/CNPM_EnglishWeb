/**
 * Teacher Portal App Bootstrap
 * Khởi tạo Router + Layout theo đúng kiến trúc MVC đề bài
 */

import { auth } from '../core/auth.js';
import { Router } from '../core/router.js';
import { LayoutView } from '../views/shared/LayoutView.js';
import { confirmDialog } from '../ui/confirmDialog.js';
import { initSeedDatabase } from '../data/seed.js';
import { TeacherModel, UserExamModel } from '../models/index.js';

// 9 Controllers của Teacher Portal
import { TeacherDashboardController } from '../controllers/teacher/TeacherDashboardController.js';
import { TeacherClassesController } from '../controllers/teacher/TeacherClassesController.js';
import { TeacherScheduleController } from '../controllers/teacher/TeacherScheduleController.js';
import { TeacherLessonsController } from '../controllers/teacher/TeacherLessonsController.js';
import { TeacherQuestionsController } from '../controllers/teacher/TeacherQuestionsController.js';
import { TeacherExamsController } from '../controllers/teacher/TeacherExamsController.js';
import { TeacherGradingController } from '../controllers/teacher/TeacherGradingController.js';
import { TeacherResultsController } from '../controllers/teacher/TeacherResultsController.js';
import { TeacherProfileController } from '../controllers/teacher/TeacherProfileController.js';

// 1. Khởi tạo Database nếu chưa có & Route Guard
initSeedDatabase();
if (!auth.requireAuth('teacher')) {
  // Tự động redirect về trang đăng nhập nếu chưa auth hoặc sai role
} else {
  initTeacherApp();
}

async function initTeacherApp() {
  const currentUser = auth.getCurrentUser();
  if (!currentUser) return;

  // Lấy thông tin giảng viên và số bài chờ chấm
  const teacherProfile = await TeacherModel.getByUserId(currentUser.id) || {};
  const pendingList = await UserExamModel.getPendingReviewSubmissions(teacherProfile.id);
  const pendingCount = pendingList ? pendingList.length : 0;

  // 2. Khởi tạo Layout Shell vào #app
  const appEl = document.getElementById('app');
  if (appEl) {
    appEl.innerHTML = LayoutView.renderTeacherLayout(currentUser, teacherProfile, pendingCount);
  }

  // 3. Gắn các sự kiện của Layout
  bindLayoutEvents();

  // 4. Khởi tạo Hash-based Client-Side Router
  const mainContainer = document.getElementById('app-main-content');
  const router = new Router({ container: mainContainer });

  // Middleware cập nhật active link trên sidebar
  router.beforeEach((to, from, next) => {
    document.querySelectorAll('.sidebar-nav .nav-item').forEach(item => {
      const route = item.getAttribute('data-route');
      if (route && (to.path === route || (route !== '/dashboard' && to.path.startsWith(route)))) {
        item.classList.add('active');
      } else {
        item.classList.remove('active');
      }
    });

    // Tự động đóng mobile drawer khi chuyển trang
    const sidebar = document.getElementById('app-sidebar');
    const backdrop = document.getElementById('sidebar-backdrop');
    if (sidebar) sidebar.classList.remove('open');
    if (backdrop) backdrop.classList.remove('active');

    window.scrollTo({ top: 0, behavior: 'smooth' });
    next();
  });

  // 5. Đăng ký 9 trang nghiệp vụ
  router.register('/dashboard', TeacherDashboardController, { title: 'Bảng điều khiển Giảng viên' });
  router.register('/classes', TeacherClassesController, { title: 'Lớp của tôi' });
  router.register('/classes/:id', TeacherClassesController, { title: 'Chi tiết lớp học' });
  router.register('/schedule', TeacherScheduleController, { title: 'Lịch dạy tuần' });
  router.register('/lessons', TeacherLessonsController, { title: 'Soạn bài giảng' });
  router.register('/questions', TeacherQuestionsController, { title: 'Ngân hàng câu hỏi' });
  router.register('/exams', TeacherExamsController, { title: 'Đề thi của tôi' });
  router.register('/grading', TeacherGradingController, { title: 'Chấm bài tự luận' });
  router.register('/results', TeacherResultsController, { title: 'Kết quả học viên' });
  router.register('/profile', TeacherProfileController, { title: 'Hồ sơ giảng viên' });

  // 6. Khởi chạy router
  router.init('/dashboard');
}

/**
 * Gắn các sự kiện điều khiển toàn bộ Layout
 */
function bindLayoutEvents() {
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.getElementById('sidebar-backdrop');
  const toggleBtn = document.getElementById('menu-toggle-btn');

  function toggleSidebar(open) {
    if (open) {
      if (sidebar) sidebar.classList.add('open');
      if (backdrop) backdrop.classList.add('active');
    } else {
      if (sidebar) sidebar.classList.remove('open');
      if (backdrop) backdrop.classList.remove('active');
    }
  }

  if (toggleBtn) toggleBtn.addEventListener('click', () => toggleSidebar(true));
  if (backdrop) backdrop.addEventListener('click', () => toggleSidebar(false));

  // Avatar Dropdown & Bell Dropdown Toggle
  const avatarBtn = document.getElementById('avatar-dropdown-btn');
  const avatarMenu = document.getElementById('avatar-dropdown-menu');
  const bellBtn = document.getElementById('bell-dropdown-btn');
  const bellMenu = document.getElementById('bell-dropdown-menu');

  if (avatarBtn && avatarMenu) {
    avatarBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      avatarMenu.classList.toggle('show');
      if (bellMenu) bellMenu.classList.remove('show');
    });
  }

  if (bellBtn && bellMenu) {
    bellBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      bellMenu.classList.toggle('show');
      if (avatarMenu) avatarMenu.classList.remove('show');
    });
  }

  document.addEventListener('click', () => {
    if (avatarMenu) avatarMenu.classList.remove('show');
    if (bellMenu) bellMenu.classList.remove('show');
  });

  // Đăng xuất
  const handleLogout = async () => {
    const ok = await confirmDialog({
      title: 'Đăng xuất tài khoản',
      message: 'Bạn có chắc chắn muốn kết thúc phiên làm việc của Giảng viên?',
      confirmText: 'Đăng xuất',
      cancelText: 'Hủy'
    });
    if (ok) {
      auth.logout();
    }
  };

  const topbarLogout = document.getElementById('btn-topbar-logout');
  const sidebarLogout = document.getElementById('sidebar-logout-btn');

  if (topbarLogout) topbarLogout.addEventListener('click', handleLogout);
  if (sidebarLogout) sidebarLogout.addEventListener('click', handleLogout);
}
