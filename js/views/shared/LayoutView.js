/**
 * LayoutView - Giao diện Layout Shell dùng chung cho các phân hệ
 * Hỗ trợ render Sidebar, Topbar và khung chứa Main Content
 */

import { icon } from '../../ui/icons.js';
import { getInitials } from '../../core/utils.js';

export class LayoutView {
  /**
   * Render toàn bộ khung Layout cho Admin
   * @param {Object} currentUser 
   * @returns {string} HTML string
   */
  static renderAdminLayout(currentUser = {}) {
    const initials = getInitials(currentUser.full_name || 'Admin');

    return `
      <div class="app-layout">
        <!-- Mobile Backdrop -->
        <div class="sidebar-backdrop" id="sidebar-backdrop"></div>

        <!-- Sidebar Trắng Hiện Đại -->
        <aside class="app-sidebar" id="app-sidebar" style="background-color: #FFFFFF;">
          <div class="sidebar-header">
            <a href="#/dashboard" class="brand-logo">
              <div class="logo-icon">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
                  <path d="M22 10v6M2 10l10-5 10 5-10 5z"></path>
                  <path d="M6 12v5c3 3 9 3 12 0v-5"></path>
                </svg>
              </div>
              <span>EnglishAdmin</span>
            </a>
          </div>

          <nav class="sidebar-nav">
            <div class="nav-section-title">Tổng quan</div>
            <a href="#/dashboard" class="nav-item active" data-route="/dashboard">
              <span class="nav-icon">${icon('home', { size: 18 })}</span>
              <span>Bảng điều khiển</span>
            </a>

            <div class="nav-section-title">Quản lý đào tạo</div>
            <a href="#/subjects" class="nav-item" data-route="/subjects">
              <span class="nav-icon">${icon('layers', { size: 18 })}</span>
              <span>Môn học</span>
            </a>
            <a href="#/courses" class="nav-item" data-route="/courses">
              <span class="nav-icon">${icon('book-open', { size: 18 })}</span>
              <span>Khóa học & Bài học</span>
            </a>
            <a href="#/classes" class="nav-item" data-route="/classes">
              <span class="nav-icon">${icon('book', { size: 18 })}</span>
              <span>Lớp học</span>
            </a>
            <a href="#/schedules" class="nav-item" data-route="/schedules">
              <span class="nav-icon">${icon('calendar', { size: 18 })}</span>
              <span>Xếp lịch (TKB)</span>
            </a>
            <a href="#/facilities" class="nav-item" data-route="/facilities">
              <span class="nav-icon">${icon('map-pin', { size: 18 })}</span>
              <span>Cơ sở vật chất</span>
            </a>

            <div class="nav-section-title">Học viên & Đăng ký</div>
            <a href="#/enrollments" class="nav-item" data-route="/enrollments">
              <span class="nav-icon">${icon('file-text', { size: 18 })}</span>
              <span>Đăng ký học</span>
            </a>
            <a href="#/users" class="nav-item" data-route="/users">
              <span class="nav-icon">${icon('users', { size: 18 })}</span>
              <span>Quản lý người dùng</span>
            </a>
            <a href="#/teachers" class="nav-item" data-route="/teachers">
              <span class="nav-icon">${icon('graduation-cap', { size: 18 })}</span>
              <span>Giáo viên & Hồ sơ</span>
            </a>

            <div class="nav-section-title">Khảo thí & Báo cáo</div>
            <a href="#/exams" class="nav-item" data-route="/exams">
              <span class="nav-icon">${icon('award', { size: 18 })}</span>
              <span>Ngân hàng đề thi</span>
            </a>
            <a href="#/reviews" class="nav-item" data-route="/reviews">
              <span class="nav-icon">${icon('star', { size: 18 })}</span>
              <span>Đánh giá khóa học</span>
            </a>
            <a href="#/reports" class="nav-item" data-route="/reports">
              <span class="nav-icon">${icon('trending', { size: 18 })}</span>
              <span>Báo cáo & Thống kê</span>
            </a>
          </nav>

          <div class="sidebar-footer">
            <div class="sidebar-user">
              <div class="avatar avatar-sm" id="sidebar-user-avatar">${initials}</div>
              <div class="sidebar-user-info">
                <div class="sidebar-user-name" id="sidebar-user-name">${currentUser.full_name || 'Quản trị viên'}</div>
                <div class="sidebar-user-role">Super Admin</div>
              </div>
              <button type="button" class="btn-icon" id="sidebar-logout-btn" title="Đăng xuất">
                <span class="nav-icon">${icon('log-out', { size: 18 })}</span>
              </button>
            </div>
          </div>
        </aside>

        <!-- Main Wrapper -->
        <div class="app-main">
          <!-- Topbar -->
          <header class="app-topbar">
            <div class="topbar-left">
              <button type="button" class="menu-toggle-btn" id="menu-toggle-btn" aria-label="Mở menu">
                <span class="nav-icon">${icon('menu', { size: 20 })}</span>
              </button>
              
              <!-- Thanh tìm kiếm nhanh toàn hệ thống -->
              <div class="topbar-search-box">
                <span class="search-icon">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8"></circle>
                    <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                  </svg>
                </span>
                <input type="text" id="global-search-input" class="form-control" placeholder="Tìm kiếm nhanh... (nhấn Enter)">
              </div>
            </div>

            <div class="topbar-right">
              <!-- Chuông thông báo Dropdown -->
              <div class="dropdown notification-wrapper">
                <button type="button" class="btn-icon" id="bell-dropdown-btn" title="Thông báo hệ thống">
                  <span class="nav-icon">${icon('bell', { size: 18 })}</span>
                  <span class="notification-badge-dot"></span>
                </button>
                <div class="dropdown-menu" id="bell-dropdown-menu" style="width: 300px; right: 0;">
                  <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border-color); font-weight: 700; font-size: 0.8125rem;">
                    Thông báo mới nhất
                  </div>
                  <div style="max-height: 240px; overflow-y: auto;">
                    <div class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                      <strong style="display: block; font-size: 0.75rem;">1 Giáo viên mới chờ xác minh</strong>
                      <small style="color: var(--text-muted);">Phạm Quang Huy đang đợi duyệt hồ sơ</small>
                    </div>
                    <div class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                      <strong style="display: block; font-size: 0.75rem;">1 Bài thi tự luận mới nộp</strong>
                      <small style="color: var(--text-muted);">Học viên đã nộp bài thi IELTS Writing</small>
                    </div>
                    <div class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                      <strong style="display: block; font-size: 0.75rem;">3 Lớp sắp khai giảng</strong>
                      <small style="color: var(--text-muted);">COMM-01, TOEIC-A01, KIDS-01</small>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Avatar & Profile Dropdown -->
              <div class="dropdown">
                <div class="user-profile-trigger" id="avatar-dropdown-btn" role="button" tabindex="0">
                  <div class="avatar avatar-sm" id="topbar-user-avatar">${initials}</div>
                  <div style="text-align: left; line-height: 1.2;" class="d-none d-md-block">
                    <div style="font-size: 0.8125rem; font-weight: 600;" id="topbar-user-name">${currentUser.full_name || 'Quản trị viên'}</div>
                    <div style="font-size: 0.7rem; color: var(--text-muted);">${currentUser.email || 'admin@el.com'}</div>
                  </div>
                  <span class="nav-icon" style="color: var(--text-muted);">${icon('chevron-down', { size: 14 })}</span>
                </div>

                <div class="dropdown-menu" id="avatar-dropdown-menu" style="min-width: 200px;">
                  <div style="padding: 0.625rem 1rem; border-bottom: 1px solid var(--border-color);">
                    <div style="font-weight: 700; font-size: 0.875rem;" id="dropdown-profile-name">${currentUser.full_name || 'Quản trị viên'}</div>
                    <span class="badge badge-primary" style="margin-top: 4px;">Quản trị viên cấp cao</span>
                  </div>
                  <a href="javascript:void(0)" class="dropdown-item" id="btn-view-profile">
                    <span class="nav-icon">${icon('user', { size: 16 })}</span>
                    <span>Hồ sơ cá nhân</span>
                  </a>
                  <a href="javascript:void(0)" class="dropdown-item" id="btn-change-password">
                    <span class="nav-icon">${icon('lock', { size: 16 })}</span>
                    <span>Đổi mật khẩu</span>
                  </a>
                  <a href="../styleguide.html" target="_blank" class="dropdown-item">
                    <span class="nav-icon">${icon('layers', { size: 16 })}</span>
                    <span>Xem Style Guide</span>
                  </a>
                  <div class="dropdown-divider"></div>
                  <a href="javascript:void(0)" class="dropdown-item text-danger" id="btn-topbar-logout">
                    <span class="nav-icon">${icon('log-out', { size: 16 })}</span>
                    <span>Đăng xuất</span>
                  </a>
                </div>
              </div>
            </div>
          </header>

          <!-- Vùng Nội Dung Động Render bởi Controller -->
          <main class="page-container" id="app-main-content">
            <!-- Nội dung trang con sẽ được render tại đây -->
          </main>
        </div>
      </div>
    `;
  }

  /**
   * Render toàn bộ khung Layout cho Giảng viên (Teacher)
   * Tone xanh trắng thân thiện, sidebar gọn gàng, ưu tiên "Việc cần làm hôm nay"
   * @param {Object} currentUser 
   * @param {Object} teacherProfile 
   * @param {number} pendingCount Số bài thi chờ chấm
   * @returns {string} HTML string
   */
  static renderTeacherLayout(currentUser = {}, teacherProfile = {}, pendingCount = 0) {
    const initials = getInitials(currentUser.full_name || 'Teacher');
    const isVerified = teacherProfile && (teacherProfile.is_verified === 1 || teacherProfile.is_verified === true);

    return `
      <div class="app-layout">
        <!-- Mobile Backdrop -->
        <div class="sidebar-backdrop" id="sidebar-backdrop"></div>

        <!-- Sidebar Trắng Hiện Đại Cho Giảng Viên -->
        <aside class="app-sidebar" id="app-sidebar" style="background-color: #FFFFFF;">
          <div class="sidebar-header">
            <a href="#/dashboard" class="brand-logo">
              <div class="logo-icon" style="background: linear-gradient(135deg, #0EA5E9, #2563EB);">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
                  <path d="M22 10v6M2 10l10-5 10 5-10 5z"></path>
                  <path d="M6 12v5c3 3 9 3 12 0v-5"></path>
                </svg>
              </div>
              <span>EnglishTeacher</span>
            </a>
          </div>

          <nav class="sidebar-nav">
            <div class="nav-section-title">Tổng quan</div>
            <a href="#/dashboard" class="nav-item active" data-route="/dashboard">
              <span class="nav-icon">${icon('home', { size: 18 })}</span>
              <span>Bảng điều khiển</span>
            </a>

            <div class="nav-section-title">Giảng dạy & Lớp học</div>
            <a href="#/classes" class="nav-item" data-route="/classes">
              <span class="nav-icon">${icon('layers', { size: 18 })}</span>
              <span>Lớp của tôi</span>
            </a>
            <a href="#/schedule" class="nav-item" data-route="/schedule">
              <span class="nav-icon">${icon('calendar', { size: 18 })}</span>
              <span>Lịch dạy tuần</span>
            </a>
            <a href="#/lessons" class="nav-item" data-route="/lessons">
              <span class="nav-icon">${icon('book-open', { size: 18 })}</span>
              <span>Soạn bài giảng</span>
            </a>

            <div class="nav-section-title">Khảo thí & Chấm thi</div>
            <a href="#/questions" class="nav-item" data-route="/questions">
              <span class="nav-icon">${icon('file-text', { size: 18 })}</span>
              <span>Ngân hàng câu hỏi</span>
            </a>
            <a href="#/exams" class="nav-item" data-route="/exams">
              <span class="nav-icon">${icon('award', { size: 18 })}</span>
              <span>Đề thi của tôi</span>
            </a>
            <a href="#/grading" class="nav-item" data-route="/grading">
              <span class="nav-icon">${icon('edit', { size: 18 })}</span>
              <span>Chấm bài tự luận</span>
              ${pendingCount > 0 ? `<span class="nav-badge" id="sidebar-pending-badge">${pendingCount}</span>` : ''}
            </a>
            <a href="#/results" class="nav-item" data-route="/results">
              <span class="nav-icon">${icon('trending', { size: 18 })}</span>
              <span>Kết quả học viên</span>
            </a>

            <div class="nav-section-title">Cá nhân</div>
            <a href="#/profile" class="nav-item" data-route="/profile">
              <span class="nav-icon">${icon('user', { size: 18 })}</span>
              <span>Hồ sơ giảng viên</span>
            </a>
          </nav>

          <div class="sidebar-footer">
            <div class="sidebar-user">
              <div class="avatar avatar-sm" id="sidebar-user-avatar">
                ${currentUser.avatar_url ? `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}">` : initials}
              </div>
              <div class="sidebar-user-info">
                <div class="sidebar-user-name" id="sidebar-user-name">${currentUser.full_name || 'Giảng viên'}</div>
                <div class="sidebar-user-role">
                  ${isVerified 
                    ? '<span class="text-success" style="font-size: 0.7rem; font-weight: 600;">✓ Đã xác minh</span>' 
                    : '<span class="text-warning" style="font-size: 0.7rem; font-weight: 600;">⏳ Chờ xác minh</span>'}
                </div>
              </div>
              <button type="button" class="btn-icon" id="sidebar-logout-btn" title="Đăng xuất">
                <span class="nav-icon">${icon('log-out', { size: 18 })}</span>
              </button>
            </div>
          </div>
        </aside>

        <!-- Main Wrapper -->
        <div class="app-main">
          <!-- Topbar -->
          <header class="app-topbar">
            <div class="topbar-left">
              <button type="button" class="menu-toggle-btn" id="menu-toggle-btn" aria-label="Mở menu">
                <span class="nav-icon">${icon('menu', { size: 20 })}</span>
              </button>
              
              <!-- Tiêu đề cổng hoặc thanh tìm kiếm -->
              <div style="font-size: 0.9375rem; font-weight: 700; color: var(--text-primary); display: flex; align-items: center; gap: 8px;">
                <span>Cổng Tác Nghiệp Giảng Viên</span>
                ${isVerified 
                  ? `<span class="badge badge-success" style="font-size: 0.7rem;"><span class="badge-dot"></span>Giảng viên chính thức</span>`
                  : `<span class="badge badge-warning" style="font-size: 0.7rem;"><span class="badge-dot"></span>Hồ sơ đang kiểm định</span>`}
              </div>
            </div>

            <div class="topbar-right">
              <!-- Nút chuông thông báo -->
              <div class="dropdown notification-wrapper">
                <button type="button" class="btn-icon" id="bell-dropdown-btn" title="Thông báo công việc">
                  <span class="nav-icon">${icon('bell', { size: 18 })}</span>
                  ${pendingCount > 0 ? '<span class="notification-badge-dot"></span>' : ''}
                </button>
                <div class="dropdown-menu" id="bell-dropdown-menu" style="width: 290px; right: 0;">
                  <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border-color); font-weight: 700; font-size: 0.8125rem;">
                    Việc cần làm hôm nay
                  </div>
                  <div style="padding: 0.75rem 1rem; font-size: 0.8125rem;">
                    ${pendingCount > 0 
                      ? `<a href="#/grading" style="color: var(--primary-600); font-weight: 600; text-decoration: none;">📝 Bạn có ${pendingCount} bài thi tự luận đang chờ chấm điểm</a>` 
                      : '<span style="color: var(--text-muted);">Không có bài thi nào đang chờ chấm</span>'}
                  </div>
                </div>
              </div>

              <!-- Avatar Dropdown -->
              <div class="dropdown">
                <div class="user-profile-trigger" id="avatar-dropdown-btn" role="button" tabindex="0">
                  <div class="avatar avatar-sm" id="topbar-user-avatar">
                    ${currentUser.avatar_url ? `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}">` : initials}
                  </div>
                  <div style="text-align: left; line-height: 1.2;" class="d-none d-md-block">
                    <div style="font-size: 0.8125rem; font-weight: 600;">${currentUser.full_name || 'Giảng viên'}</div>
                    <div style="font-size: 0.7rem; color: var(--text-muted);">${currentUser.email || ''}</div>
                  </div>
                  <span class="nav-icon" style="color: var(--text-muted);">${icon('chevron-down', { size: 14 })}</span>
                </div>

                <div class="dropdown-menu" id="avatar-dropdown-menu" style="min-width: 200px;">
                  <div style="padding: 0.625rem 1rem; border-bottom: 1px solid var(--border-color);">
                    <div style="font-weight: 700; font-size: 0.875rem;">${currentUser.full_name}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${teacherProfile.qualification || 'Giảng viên'}</div>
                  </div>
                  <a href="#/profile" class="dropdown-item">
                    <span class="nav-icon">${icon('user', { size: 16 })}</span>
                    <span>Hồ sơ giảng viên</span>
                  </a>
                  <a href="#/grading" class="dropdown-item">
                    <span class="nav-icon">${icon('edit', { size: 16 })}</span>
                    <span>Chấm bài tập</span>
                  </a>
                  <div class="dropdown-divider"></div>
                  <a href="javascript:void(0)" class="dropdown-item text-danger" id="btn-topbar-logout">
                    <span class="nav-icon">${icon('log-out', { size: 16 })}</span>
                    <span>Đăng xuất</span>
                  </a>
                </div>
              </div>
            </div>
          </header>

          <!-- Vùng Nội Dung Động Cho Teacher Controller -->
          <main class="page-container" id="app-main-content">
            <!-- Nội dung controller render tại đây -->
          </main>
        </div>
      </div>
    `;
  }

  /**
   * Render toàn bộ khung Layout cho Học sinh (Student)
   * Top Navbar hiện đại, không dùng sidebar, phong cách tươi sáng, thân thiện, hợp cả mobile
   * @param {Object} currentUser 
   * @param {number} flashcardDueCount Số thẻ flashcard cần ôn hôm nay
   * @param {number} streakDays Chuỗi ngày học liên tục
   * @returns {string} HTML string
   */
  static renderStudentLayout(currentUser = {}, flashcardDueCount = 0, streakDays = 5) {
    const initials = getInitials(currentUser.full_name || 'Học viên');

    return `
      <div class="student-layout">
        <!-- Top Navbar -->
        <header class="student-navbar">
          <div class="student-navbar-inner">
            <div style="display: flex; align-items: center; gap: 14px; flex-shrink: 0;">
              <a href="#/dashboard" class="brand-logo" style="text-decoration: none; display: inline-flex; align-items: center; gap: 8px; flex-shrink: 0;">
                <div class="logo-icon" style="background: linear-gradient(135deg, #10B981, #2563EB); width: 34px; height: 34px;">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
                    <path d="M22 10v6M2 10l10-5 10 5-10 5z"></path>
                    <path d="M6 12v5c3 3 9 3 12 0v-5"></path>
                  </svg>
                </div>
                <span style="font-size: 1.0625rem; font-weight: 800; color: var(--primary-700); white-space: nowrap;">EnglishStudent</span>
              </a>

              <!-- Desktop Nav Links -->
              <nav class="student-nav-menu">
                <a href="#/dashboard" class="student-nav-link" data-route="/dashboard">
                  <span class="nav-icon">${icon('home', { size: 16 })}</span>
                  <span>Trang chủ</span>
                </a>
                <a href="#/courses" class="student-nav-link" data-route="/courses">
                  <span class="nav-icon">${icon('book-open', { size: 16 })}</span>
                  <span>Khám phá</span>
                </a>
                <a href="#/my-classes" class="student-nav-link" data-route="/my-classes">
                  <span class="nav-icon">${icon('layers', { size: 16 })}</span>
                  <span>Lớp của tôi</span>
                </a>
                <a href="#/schedule" class="student-nav-link" data-route="/schedule">
                  <span class="nav-icon">${icon('calendar', { size: 16 })}</span>
                  <span>Thời khóa biểu</span>
                </a>
                <a href="#/flashcards" class="student-nav-link" data-route="/flashcards">
                  <span class="nav-icon">${icon('repeat', { size: 16 })}</span>
                  <span>Flashcards</span>
                  ${flashcardDueCount > 0 ? `<span class="badge badge-warning" style="padding: 1px 6px; font-size: 0.6875rem; border-radius: 9999px; margin-left: 2px;" id="nav-due-flashcard">${flashcardDueCount}</span>` : ''}
                </a>
                <a href="#/exams" class="student-nav-link" data-route="/exams">
                  <span class="nav-icon">${icon('award', { size: 16 })}</span>
                  <span>Luyện thi</span>
                </a>
                <a href="#/progress" class="student-nav-link" data-route="/progress">
                  <span class="nav-icon">${icon('trending', { size: 16 })}</span>
                  <span>Tiến độ</span>
                </a>
              </nav>
            </div>

            <!-- Right Controls -->
            <div style="display: flex; align-items: center; gap: 10px; flex-shrink: 0;">
              <!-- Streak Pill -->
              <div class="student-streak-pill" title="Chuỗi ngày học liên tục của bạn!">
                <span>🔥</span>
                <span>${streakDays} ngày</span>
              </div>

              <!-- Chuông thông báo -->
              <div class="dropdown notification-wrapper">
                <button type="button" class="btn-icon" id="student-bell-btn" title="Thông báo học tập">
                  <span class="nav-icon">${icon('bell', { size: 18 })}</span>
                  ${flashcardDueCount > 0 ? '<span class="notification-badge-dot"></span>' : ''}
                </button>
                <div class="dropdown-menu" id="student-bell-menu" style="width: 300px; right: 0;">
                  <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border-color); font-weight: 700; font-size: 0.8125rem;">
                    Thông báo học tập
                  </div>
                  <div style="max-height: 250px; overflow-y: auto;">
                    ${flashcardDueCount > 0 ? `
                      <a href="#/flashcards" class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                        <strong style="display: block; font-size: 0.75rem; color: var(--primary-600);">🎯 Ôn tập từ vựng hôm nay</strong>
                        <small style="color: var(--text-muted);">Bạn có ${flashcardDueCount} từ vựng Spaced Repetition đến hạn ôn</small>
                      </a>
                    ` : ''}
                    <a href="#/schedule" class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                      <strong style="display: block; font-size: 0.75rem;">📅 Lịch học tuần mới</strong>
                      <small style="color: var(--text-muted);">Đừng quên kiểm tra phòng học và thời gian các buổi học sắp tới</small>
                    </a>
                    <a href="#/exams" class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                      <strong style="display: block; font-size: 0.75rem;">🏆 Đề thi định kỳ</strong>
                      <small style="color: var(--text-muted);">Bài kiểm tra giữa kỳ đã được mở để làm thử</small>
                    </a>
                  </div>
                </div>
              </div>

              <!-- User Avatar Dropdown -->
              <div class="dropdown">
                <div class="student-user-profile-trigger" id="student-avatar-btn" role="button" tabindex="0">
                  <div class="avatar avatar-sm" style="background: linear-gradient(135deg, #2563EB, #10B981); color: white; font-weight: 700; width: 32px; height: 32px; font-size: 0.8125rem; flex-shrink: 0;">
                    ${currentUser.avatar_url ? `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}" style="width: 100%; height: 100%; border-radius: 50%; object-fit: cover;">` : initials}
                  </div>
                  <div class="student-user-profile-info d-none d-md-block">
                    <div class="student-user-profile-name">${currentUser.full_name || 'Học viên'}</div>
                    <div class="student-user-profile-role">Học viên</div>
                  </div>
                  <span class="nav-icon" style="color: var(--text-muted); margin-left: 2px;">${icon('chevron-down', { size: 14 })}</span>
                </div>

                <div class="dropdown-menu" id="student-avatar-menu" style="min-width: 210px; right: 0;">
                  <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border-color);">
                    <div style="font-weight: 700; font-size: 0.875rem;">${currentUser.full_name}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${currentUser.email || ''}</div>
                  </div>
                  <a href="#/profile" class="dropdown-item">
                    <span class="nav-icon">${icon('user', { size: 16 })}</span>
                    <span>Hồ sơ cá nhân</span>
                  </a>
                  <a href="#/reviews" class="dropdown-item">
                    <span class="nav-icon">${icon('star', { size: 16 })}</span>
                    <span>Đánh giá khóa học</span>
                  </a>
                  <a href="../styleguide.html" target="_blank" class="dropdown-item">
                    <span class="nav-icon">${icon('layers', { size: 16 })}</span>
                    <span>Xem Style Guide</span>
                  </a>
                  <div class="dropdown-divider"></div>
                  <a href="javascript:void(0)" class="dropdown-item text-danger" id="btn-student-logout">
                    <span class="nav-icon">${icon('log-out', { size: 16 })}</span>
                    <span>Đăng xuất</span>
                  </a>
                </div>
              </div>
            </div>
          </div>
        </header>

        <!-- Main Content Container -->
        <main class="student-content-container" id="app-main-content">
          <!-- Dynamic view content -->
        </main>

        <!-- Mobile Bottom Tab Bar -->
        <nav class="student-bottom-bar">
          <a href="#/dashboard" class="student-tab-item active" data-route="/dashboard">
            <span class="nav-icon">${icon('home', { size: 20 })}</span>
            <span>Trang chủ</span>
          </a>
          <a href="#/courses" class="student-tab-item" data-route="/courses">
            <span class="nav-icon">${icon('book-open', { size: 20 })}</span>
            <span>Khám phá</span>
          </a>
          <a href="#/my-classes" class="student-tab-item" data-route="/my-classes">
            <span class="nav-icon">${icon('layers', { size: 20 })}</span>
            <span>Lớp học</span>
          </a>
          <a href="#/flashcards" class="student-tab-item" data-route="/flashcards">
            <span class="nav-icon">${icon('repeat', { size: 20 })}</span>
            <span>Flashcard</span>
          </a>
          <a href="#/profile" class="student-tab-item" data-route="/profile">
            <span class="nav-icon">${icon('user', { size: 20 })}</span>
            <span>Cá nhân</span>
          </a>
        </nav>
      </div>
    `;
  }
}
