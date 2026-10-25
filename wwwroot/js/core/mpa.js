/**
 * MPA Helper - Cung cấp cơ chế khởi tạo và điều hướng độc lập cho từng file HTML
 */

import { auth } from './auth.js';
import { initSeedDatabase } from '../data/seed.js';
import { confirmDialog } from '../ui/confirmDialog.js';
import { icon } from '../ui/icons.js';
import { getInitials } from './utils.js';

/**
 * Bản đồ ánh xạ từ Hash route sang tên file HTML tương ứng
 */
const ROUTE_TO_PAGE_MAP = {
  // Admin
  '/dashboard': 'dashboard.html',
  '/users': 'users.html',
  '/teachers': 'teachers.html',
  '/subjects': 'subjects.html',
  '/courses': 'courses.html',
  '/classes': 'classes.html',
  '/schedules': 'schedules.html',
  '/facilities': 'facilities.html',
  '/enrollments': 'enrollments.html',
  '/reviews': 'reviews.html',
  '/exams': 'exams.html',
  '/reports': 'reports.html',

  // Teacher
  '/schedule': 'schedule.html',
  '/lessons': 'lessons.html',
  '/questions': 'questions.html',
  '/grading': 'grading.html',
  '/results': 'results.html',
  '/profile': 'profile.html',

  // Student
  '/my-classes': 'my-classes.html',
  '/flashcards': 'flashcards.html'
};

/**
 * Chuyển đổi một hash path (ví dụ: #/courses/3, #/learn/1/2) thành URL file HTML tương ứng
 */
export function convertHashToMpaUrl(hash) {
  if (!hash || !hash.startsWith('#/')) return hash;
  const path = hash.slice(1); // bỏ dấu #

  // 1. Các route có 2 tham số: #/learn/:courseId/:lessonId
  const learnMatch = path.match(/^\/learn\/([^/]+)\/([^/]+)$/);
  if (learnMatch) {
    return `learn.html?courseId=${learnMatch[1]}&lessonId=${learnMatch[2]}`;
  }

  // 2. Các route làm bài thi: #/exams/:id/take
  const takeExamMatch = path.match(/^\/exams\/([^/]+)\/take$/);
  if (takeExamMatch) {
    return `take-exam.html?id=${takeExamMatch[1]}`;
  }

  // 3. Các route chi tiết có 1 tham số: :id
  const detailMatch = path.match(/^\/([^/]+)\/([^/]+)$/);
  if (detailMatch) {
    const resource = detailMatch[1];
    const id = detailMatch[2];
    if (resource === 'courses') return `course-detail.html?id=${id}`;
    if (resource === 'classes' || resource === 'my-classes') return `class-detail.html?id=${id}`;
    if (resource === 'results') return `result-detail.html?id=${id}`;
  }

  // 4. Các route tĩnh
  if (ROUTE_TO_PAGE_MAP[path]) {
    return ROUTE_TO_PAGE_MAP[path];
  }

  return hash;
}

/**
 * Khởi tạo trang MPA độc lập
 */
export async function initMpaPage({
  portal = 'admin',
  activeRoute = '/dashboard',
  pageTitle = '',
  controller = null
}) {
  // 1. Khởi tạo Database mẫu
  initSeedDatabase();

  // 2. Kiểm tra quyền truy cập (Route Guard)
  if (!auth.requireAuth(portal)) {
    return;
  }

  const currentUser = auth.getCurrentUser() || {};
  if (pageTitle) {
    document.title = `${pageTitle} - EnglishLearning`;
  }

  // 3. Cập nhật thông tin User trên Topbar / Avatar
  updateUserInterface(currentUser, portal);

  // 4. Thiết lập sự kiện Dropdown & Đăng xuất
  setupDropdownsAndLogout(portal);

  // 5. Chuyển đổi toàn bộ link hash thành link HTML và chặn sự kiện click
  setupMpaLinkInterception();

  // 6. Chạy Controller của trang hiện tại
  if (controller && typeof controller.render === 'function') {
    const container = document.getElementById('app-main-content');
    if (container) {
      const urlParams = new URLSearchParams(window.location.search);
      const params = {};
      for (const [k, v] of urlParams.entries()) {
        params[k] = v;
      }
      await controller.render(container, params);
      
      // Chuyển đổi lại các thẻ <a> sau khi View đã render xong
      convertLinksInContainer(container);
    }
  }
}

function updateUserInterface(currentUser, portal) {
  const initials = getInitials(currentUser.full_name || 'User');

  // Avatar text / image
  document.querySelectorAll('#sidebar-user-avatar, #topbar-user-avatar, #student-avatar-btn .avatar').forEach(el => {
    if (currentUser.avatar_url) {
      el.innerHTML = `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}" style="width: 100%; height: 100%; border-radius: 50%; object-fit: cover;">`;
    } else {
      el.textContent = initials;
    }
  });

  // User name
  document.querySelectorAll('#sidebar-user-name, #topbar-user-name, #dropdown-profile-name, .student-user-profile-name').forEach(el => {
    el.textContent = currentUser.full_name || 'Người dùng';
  });
}

function setupDropdownsAndLogout(portal) {
  // Bell dropdown
  const bellBtn = document.getElementById('bell-dropdown-btn') || document.getElementById('student-bell-btn');
  const bellMenu = document.getElementById('bell-dropdown-menu') || document.getElementById('student-bell-menu');
  if (bellBtn && bellMenu) {
    bellBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      const isOpen = bellMenu.classList.contains('show');
      closeAllDropdowns();
      if (!isOpen) bellMenu.classList.add('show');
    });
  }

  // Avatar dropdown
  const avatarBtn = document.getElementById('avatar-dropdown-btn') || document.getElementById('student-avatar-btn');
  const avatarMenu = document.getElementById('avatar-dropdown-menu') || document.getElementById('student-avatar-menu');
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

  // Mobile menu toggle
  const menuToggle = document.getElementById('menu-toggle-btn');
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.getElementById('sidebar-backdrop');
  if (menuToggle && sidebar) {
    menuToggle.addEventListener('click', () => {
      sidebar.classList.toggle('open');
      if (backdrop) backdrop.classList.toggle('active');
    });
  }
  if (backdrop && sidebar) {
    backdrop.addEventListener('click', () => {
      sidebar.classList.remove('open');
      backdrop.classList.remove('active');
    });
  }

  // Logout
  const logoutBtns = [
    document.getElementById('sidebar-logout-btn'),
    document.getElementById('btn-topbar-logout'),
    document.getElementById('btn-student-logout')
  ].filter(Boolean);

  logoutBtns.forEach(btn => {
    btn.addEventListener('click', async (e) => {
      e.preventDefault();
      const confirmed = await confirmDialog({
        title: 'Đăng xuất tài khoản',
        message: 'Bạn có chắc chắn muốn đăng xuất khỏi hệ thống EnglishLearning?',
        confirmText: 'Đăng xuất',
        cancelText: 'Ở lại',
        type: 'info'
      });
      if (confirmed) {
        auth.logout();
      }
    });
  });
}

function closeAllDropdowns() {
  document.querySelectorAll('.dropdown-menu.show').forEach(m => m.classList.remove('show'));
}

function convertLinksInContainer(container) {
  if (!container) return;
  container.querySelectorAll('a[href^="#/"]').forEach(a => {
    const hash = a.getAttribute('href');
    const newUrl = convertHashToMpaUrl(hash);
    if (newUrl !== hash) {
      a.setAttribute('href', newUrl);
    }
  });
}

function setupMpaLinkInterception() {
  // Chuyển đổi tất cả link ban đầu trên trang
  convertLinksInContainer(document.body);

  // Đón bắt sự kiện click cho các thẻ sinh động sau này
  document.addEventListener('click', (e) => {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href');
    if (href && href.startsWith('#/')) {
      const newUrl = convertHashToMpaUrl(href);
      if (newUrl !== href) {
        e.preventDefault();
        window.location.href = newUrl;
      }
    }
  });
}
