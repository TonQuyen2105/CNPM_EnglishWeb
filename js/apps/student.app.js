/**
 * Student Portal App Bootstrap - Cổng Học Viên EnglishLearning
 * Kiến trúc MVC thuần ES6 Modules
 */

import { auth } from '../core/auth.js';
import { Router } from '../core/router.js';
import { initSeedDatabase } from '../data/seed.js';
import { LayoutView } from '../views/shared/LayoutView.js';
import { FlashcardModel } from '../models/index.js';
import { confirmDialog } from '../ui/confirmDialog.js';

// Controllers
import { StudentDashboardController } from '../controllers/student/StudentDashboardController.js';
import { StudentCoursesController } from '../controllers/student/StudentCoursesController.js';
import { StudentMyClassesController } from '../controllers/student/StudentMyClassesController.js';
import { StudentScheduleController } from '../controllers/student/StudentScheduleController.js';
import { StudentLessonPlayerController } from '../controllers/student/StudentLessonPlayerController.js';
import { StudentFlashcardsController } from '../controllers/student/StudentFlashcardsController.js';
import { StudentExamsController } from '../controllers/student/StudentExamsController.js';
import { StudentResultsController } from '../controllers/student/StudentResultsController.js';
import { StudentProgressController } from '../controllers/student/StudentProgressController.js';
import { StudentReviewsController } from '../controllers/student/StudentReviewsController.js';
import { StudentProfileController } from '../controllers/student/StudentProfileController.js';

// Khởi tạo Database nếu chưa có
initSeedDatabase();

// Route Guard bảo vệ quyền 'student'
if (!auth.requireAuth('student')) {
  // Đã được guard tự động chuyển hướng về trang đăng nhập
} else {
  initStudentApp();
}

async function initStudentApp() {
  const appContainer = document.getElementById('app');
  if (!appContainer) return;

  const currentUser = auth.getCurrentUser();
  const dueFlashcards = await FlashcardModel.getDueCards(currentUser.id);
  const streakDays = 5;

  // 1. Render Top Navbar Shell Layout
  appContainer.innerHTML = LayoutView.renderStudentLayout(currentUser, dueFlashcards.length, streakDays);

  // 2. Kích hoạt tương tác Dropdown (Thông báo & Avatar)
  setupDropdowns();

  // 3. Xử lý nút Đăng xuất
  const logoutBtn = document.getElementById('btn-student-logout');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', async () => {
      const ok = await confirmDialog({
        title: 'Đăng xuất tài khoản',
        message: 'Bạn có chắc chắn muốn đăng xuất khỏi cổng Học viên EnglishLearning?',
        confirmText: 'Đăng xuất',
        cancelText: 'Ở lại',
        type: 'info'
      });
      if (ok) {
        auth.logout();
      }
    });
  }

  // 4. Khởi tạo Router Hash-based cho 11 phân hệ
  const mainContent = document.getElementById('app-main-content');
  const router = new Router({ container: mainContent });

  // Hook cập nhật active state trên Top Navbar và Mobile Bottom Bar
  router.beforeEach((to, from, next) => {
    // Top Nav Links
    document.querySelectorAll('.student-nav-menu .student-nav-link').forEach(link => {
      const route = link.getAttribute('data-route');
      if (route && (to.path === route || (route !== '/dashboard' && to.path.startsWith(route)))) {
        link.classList.add('active');
      } else {
        link.classList.remove('active');
      }
    });

    // Mobile Bottom Tab Items
    document.querySelectorAll('.student-bottom-bar .student-tab-item').forEach(item => {
      const route = item.getAttribute('data-route');
      if (route && (to.path === route || (route !== '/dashboard' && to.path.startsWith(route)))) {
        item.classList.add('active');
      } else {
        item.classList.remove('active');
      }
    });

    // Đóng dropdowns khi chuyển trang
    closeAllDropdowns();
    next();
  });

  // Đăng ký 11 routes chính và các parameterized sub-routes
  // 1. Dashboard
  router.register('/dashboard', StudentDashboardController, { title: 'Bảng học tập' });

  // 2. Khám phá khoá học
  router.register('/courses', StudentCoursesController, { title: 'Khám phá khóa học' });
  router.register('/courses/:id', StudentCoursesController, { title: 'Chi tiết khóa học' });

  // 3. Lớp của tôi
  router.register('/my-classes', StudentMyClassesController, { title: 'Lớp học của tôi' });
  router.register('/my-classes/:id', StudentMyClassesController, { title: 'Chi tiết lớp học' });

  // 4. Thời khoá biểu
  router.register('/schedule', StudentScheduleController, { title: 'Thời khóa biểu' });

  // 5. Trình phát bài học (Lesson Player)
  router.register('/learn/:courseId/:lessonId', StudentLessonPlayerController, { title: 'Học bài giảng' });

  // 6. Flashcards (SRS)
  router.register('/flashcards', StudentFlashcardsController, { title: 'Thẻ từ vựng SRS' });

  // 7. Luyện thi & Màn làm bài
  router.register('/exams', StudentExamsController, { title: 'Luyện thi & Đánh giá' });
  router.register('/exams/:id/take', StudentExamsController, { title: 'Làm bài thi' });

  // 8. Kết quả thi & Lịch sử
  router.register('/results', StudentResultsController, { title: 'Kết quả bài thi' });
  router.register('/results/:id', StudentResultsController, { title: 'Chi tiết kết quả' });

  // 9. Tiến độ & Thành tích
  router.register('/progress', StudentProgressController, { title: 'Tiến độ học tập' });

  // 10. Đánh giá khoá học
  router.register('/reviews', StudentReviewsController, { title: 'Đánh giá khóa học' });

  // 11. Hồ sơ học sinh
  router.register('/profile', StudentProfileController, { title: 'Hồ sơ cá nhân' });

  // Khởi chạy router
  router.init('/dashboard');
}

/**
 * Xử lý mở / đóng Dropdown Menu
 */
function setupDropdowns() {
  const bellBtn = document.getElementById('student-bell-btn');
  const bellMenu = document.getElementById('student-bell-menu');
  const avatarBtn = document.getElementById('student-avatar-btn');
  const avatarMenu = document.getElementById('student-avatar-menu');

  if (bellBtn && bellMenu) {
    bellBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      const isOpen = bellMenu.classList.contains('show');
      closeAllDropdowns();
      if (!isOpen) bellMenu.classList.add('show');
    });
  }

  if (avatarBtn && avatarMenu) {
    avatarBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      const isOpen = avatarMenu.classList.contains('show');
      closeAllDropdowns();
      if (!isOpen) avatarMenu.classList.add('show');
    });
  }

  document.addEventListener('click', () => {
    closeAllDropdowns();
  });
}

function closeAllDropdowns() {
  document.querySelectorAll('.dropdown-menu.show').forEach(m => m.classList.remove('show'));
}
