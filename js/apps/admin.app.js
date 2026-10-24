/**
 * Admin Portal App Bootstrap
 * Khởi tạo Router + Layout theo đúng đặc tả kiến trúc đề bài
 */

import { auth } from '../core/auth.js';
import { Router } from '../core/router.js';
import { LayoutView } from '../views/shared/LayoutView.js';
import { modal } from '../ui/modal.js';
import { toast } from '../ui/toast.js';
import { confirmDialog } from '../ui/confirmDialog.js';
import { initSeedDatabase } from '../data/seed.js';
import { UserModel } from '../models/index.js';
import { escapeHtml, getInitials } from '../core/utils.js';

// 12 Controllers của Admin Portal
import { DashboardController } from '../controllers/admin/DashboardController.js';
import { UsersController } from '../controllers/admin/UsersController.js';
import { TeachersController } from '../controllers/admin/TeachersController.js';
import { SubjectsController } from '../controllers/admin/SubjectsController.js';
import { CoursesController } from '../controllers/admin/CoursesController.js';
import { ClassesController } from '../controllers/admin/ClassesController.js';
import { SchedulesController } from '../controllers/admin/SchedulesController.js';
import { FacilitiesController } from '../controllers/admin/FacilitiesController.js';
import { EnrollmentsController } from '../controllers/admin/EnrollmentsController.js';
import { ReviewsController } from '../controllers/admin/ReviewsController.js';
import { ExamsController } from '../controllers/admin/ExamsController.js';
import { ReportsController } from '../controllers/admin/ReportsController.js';

// 1. Kiểm tra xác thực & Route Guard
initSeedDatabase();
if (!auth.requireAuth('admin')) {
  // Đã tự redirect về trang đăng nhập nếu chưa auth
} else {
  initAdminApp();
}

function initAdminApp() {
  const currentUser = auth.getCurrentUser();

  // 2. Khởi tạo Layout Shell vào #app
  const appEl = document.getElementById('app');
  if (appEl) {
    appEl.innerHTML = LayoutView.renderAdminLayout(currentUser);
  }

  // 3. Gắn các sự kiện của Layout (Drawer, Dropdowns, Profile, Password, Logout, Search)
  bindLayoutEvents(currentUser);

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

  // Đăng ký 12 trang nghiệp vụ
  router.register('/dashboard', DashboardController, { title: 'Bảng điều khiển' });
  router.register('/users', UsersController, { title: 'Quản lý người dùng' });
  router.register('/teachers', TeachersController, { title: 'Quản lý giảng viên' });
  router.register('/subjects', SubjectsController, { title: 'Quản lý môn học' });
  
  // Khóa học & Chi tiết khóa học
  router.register('/courses', CoursesController, { title: 'Quản lý khóa học' });
  router.register('/courses/:id', {
    render: (params, query, cont) => CoursesController.renderDetail(params, query, cont)
  }, { title: 'Chi tiết khóa học' });

  // Lớp học & Chi tiết lớp học
  router.register('/classes', ClassesController, { title: 'Quản lý lớp học' });
  router.register('/classes/:id', {
    render: (params, query, cont) => ClassesController.renderDetail(params, query, cont)
  }, { title: 'Chi tiết lớp học' });

  // Xếp lịch (TKB)
  router.register('/schedules', SchedulesController, { title: 'Thời khóa biểu' });

  // Cơ sở vật chất (Phòng học & Khung giờ)
  router.register('/facilities', FacilitiesController, { title: 'Cơ sở vật chất' });

  // Đăng ký học (Enrollments)
  router.register('/enrollments', EnrollmentsController, { title: 'Quản lý ghi danh' });

  // Đánh giá khóa học
  router.register('/reviews', ReviewsController, { title: 'Đánh giá khóa học' });

  // Ngân hàng đề thi
  router.register('/exams', ExamsController, { title: 'Ngân hàng đề thi' });

  // Báo cáo & Thống kê
  router.register('/reports', ReportsController, { title: 'Báo cáo học vụ' });

  // Fallback route cho /revenue
  router.register('/revenue', ReportsController, { title: 'Báo cáo học vụ' });

  // 5. Khởi chạy router tại hash hiện tại hoặc mặc định /dashboard
  router.init('/dashboard');
}

/**
 * Gắn các sự kiện điều khiển toàn bộ Layout
 */
function bindLayoutEvents(currentUser) {
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
      title: 'Đăng xuất khỏi hệ thống',
      message: 'Bạn có chắc chắn muốn kết thúc phiên làm việc của Quản trị viên?',
      confirmText: 'Đăng xuất ngay',
      cancelText: 'Ở lại'
    });
    if (ok) {
      auth.logout();
    }
  };

  const topbarLogout = document.getElementById('btn-topbar-logout');
  const sidebarLogout = document.getElementById('sidebar-logout-btn');
  if (topbarLogout) topbarLogout.addEventListener('click', handleLogout);
  if (sidebarLogout) sidebarLogout.addEventListener('click', handleLogout);

  // Modal Hồ sơ cá nhân
  const profileBtn = document.getElementById('btn-view-profile');
  if (profileBtn) {
    profileBtn.addEventListener('click', () => {
      const initials = getInitials(currentUser.full_name);
      modal.open({
        title: 'Hồ sơ tài khoản Quản trị viên',
        body: `
          <div style="display: flex; gap: 1rem; align-items: center; margin-bottom: 1.25rem;">
            <div class="avatar avatar-xl">
              ${currentUser.avatar_url ? `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}">` : initials}
            </div>
            <div>
              <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 2px;">${escapeHtml(currentUser.full_name)}</h3>
              <div style="font-size: 0.8125rem; color: var(--text-muted);">${currentUser.email}</div>
              <span class="badge badge-primary" style="margin-top: 6px;">Quản trị viên toàn hệ thống</span>
            </div>
          </div>
          <div style="background: var(--neutral-50); padding: 1rem; border-radius: var(--radius-lg); font-size: 0.8125rem; line-height: 1.6;">
            <div>• <strong>Mã tài khoản ID:</strong> #${currentUser.id}</div>
            <div>• <strong>Trạng thái:</strong> Đang hoạt động</div>
            <div>• <strong>Phạm vi quyền:</strong> Toàn quyền quản trị cơ sở dữ liệu, lịch học, nhân sự và báo cáo</div>
          </div>
        `,
        footer: '<button type="button" class="btn btn-primary" data-dismiss="modal">Đóng</button>'
      });
    });
  }

  // Modal Đổi mật khẩu
  const changePwdBtn = document.getElementById('btn-change-password');
  if (changePwdBtn) {
    changePwdBtn.addEventListener('click', () => {
      const modalId = modal.open({
        title: 'Đổi mật khẩu tài khoản',
        body: `
          <form id="change-pwd-form">
            <div class="form-group">
              <label class="form-label" for="old-pwd">Mật khẩu hiện tại <span class="required">*</span></label>
              <input type="password" id="old-pwd" class="form-control" placeholder="Nhập mật khẩu hiện tại (demo: 123456)" required>
            </div>
            <div class="form-group">
              <label class="form-label" for="new-pwd">Mật khẩu mới <span class="required">*</span></label>
              <input type="password" id="new-pwd" class="form-control" placeholder="Tối thiểu 6 ký tự" required>
            </div>
            <div class="form-group">
              <label class="form-label" for="confirm-pwd">Nhập lại mật khẩu mới <span class="required">*</span></label>
              <input type="password" id="confirm-pwd" class="form-control" placeholder="Xác nhận lại mật khẩu mới" required>
            </div>
            <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
              <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
              <button type="submit" class="btn btn-primary" id="btn-submit-pwd">Cập nhật mật khẩu</button>
            </div>
          </form>
        `,
        maxWidth: '460px'
      });

      const pwdForm = document.getElementById('change-pwd-form');
      if (pwdForm) {
        pwdForm.addEventListener('submit', async (e) => {
          e.preventDefault();
          const oldPwd = document.getElementById('old-pwd').value;
          const newPwd = document.getElementById('new-pwd').value;
          const confirmPwd = document.getElementById('confirm-pwd').value;

          const dbUser = await UserModel.getById(currentUser.id);
          if (!dbUser || dbUser.password !== oldPwd) {
            toast.error('Mật khẩu hiện tại không chính xác!');
            return;
          }
          if (newPwd.length < 6) {
            toast.warning('Mật khẩu mới phải có ít nhất 6 ký tự.');
            return;
          }
          if (newPwd !== confirmPwd) {
            toast.warning('Mật khẩu xác nhận không khớp.');
            return;
          }

          await UserModel.update(currentUser.id, { password: newPwd });
          toast.success('Đã đổi mật khẩu thành công! Hãy ghi nhớ mật khẩu mới.');
          modal.close(modalId);
        });
      }
    });
  }

  // Global Search
  const globalSearchInput = document.getElementById('global-search-input');
  if (globalSearchInput) {
    globalSearchInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        const val = globalSearchInput.value.trim();
        if (val) {
          window.location.hash = `#/users?search=${encodeURIComponent(val)}`;
          globalSearchInput.value = '';
        }
      }
    });
  }
}
