/**
 * Teacher Views Generator with rich mock HTML
 */

export function getTeacherNav(activePage) {
  const items = [
    { section: 'Tổng quan' },
    { id: 'dashboard.html', label: 'Bảng điều khiển', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>' },
    
    { section: 'Giảng dạy & Lớp học' },
    { id: 'classes.html', label: 'Lớp của tôi', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 2 7 12 12 22 7 12 2"></polygon><polyline points="2 17 12 22 22 17"></polyline><polyline points="2 12 12 17 22 12"></polyline></svg>' },
    { id: 'schedule.html', label: 'Lịch dạy tuần', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>' },
    { id: 'lessons.html', label: 'Soạn bài giảng', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"></path><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"></path></svg>' },

    { section: 'Khảo thí & Chấm thi' },
    { id: 'questions.html', label: 'Ngân hàng câu hỏi', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>' },
    { id: 'exams.html', label: 'Đề thi của tôi', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="8" r="7"></circle><polyline points="8.21 13.89 7 23 12 20 17 23 15.79 13.88"></polyline></svg>' },
    { id: 'grading.html', label: 'Chấm bài tự luận', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>' },
    { id: 'results.html', label: 'Sổ điểm học viên', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path><rect x="8" y="2" width="8" height="4" rx="1" ry="1"></rect></svg>' },

    { section: 'Cá nhân' },
    { id: 'profile.html', label: 'Hồ sơ chuyên môn', icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>' }
  ];

  return items.map(item => {
    if (item.section) return `<div class="nav-section-title">${item.section}</div>`;
    const isActive = activePage === item.id ? 'active' : '';
    return `
      <a href="${item.id}" class="nav-item ${isActive}">
        <span class="nav-icon">${item.icon}</span>
        <span>${item.label}</span>
      </a>
    `;
  }).join('');
}

export function wrapTeacherPage({ title, activePage, content }) {
  const navHtml = getTeacherNav(activePage);
  return `<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${title} - EnglishTeacher</title>
  
  <!-- CSS Design System -->
  <link rel="stylesheet" href="../../../wwwroot/css/tokens.css">
  <link rel="stylesheet" href="../../../wwwroot/css/base.css">
  <link rel="stylesheet" href="../../../wwwroot/css/layout.css">
  <link rel="stylesheet" href="../../../wwwroot/css/components.css">

  <style>
    .notification-wrapper { position: relative; }
    .notification-badge-dot {
      position: absolute; top: 4px; right: 4px; width: 8px; height: 8px;
      border-radius: 50%; background-color: var(--danger-main); border: 2px solid white;
    }
    .user-profile-trigger {
      display: flex; align-items: center; gap: var(--space-2);
      padding: 4px 8px 4px 4px; border-radius: var(--radius-full);
      cursor: pointer; border: 1px solid transparent; transition: all var(--transition-fast);
    }
    .user-profile-trigger:hover {
      background-color: var(--neutral-100); border-color: var(--border-color);
    }
  </style>
</head>
<body>
  <div class="app-layout">
    <!-- Mobile Backdrop -->
    <div class="sidebar-backdrop" id="sidebar-backdrop"></div>

    <!-- Sidebar Trắng Hiện Đại -->
    <aside class="app-sidebar" id="app-sidebar" style="background-color: #FFFFFF;">
      <div class="sidebar-header">
        <a href="dashboard.html" class="brand-logo" style="text-decoration: none;">
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
        ${navHtml}
      </nav>

      <div class="sidebar-footer">
        <a href="../../../index.html" class="nav-item text-danger">
          <span class="nav-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
          </span>
          <span>Đăng xuất</span>
        </a>
      </div>
    </aside>

    <!-- Main Container -->
    <div class="app-main">
      <!-- Topbar Header -->
      <header class="app-topbar">
        <div style="display: flex; align-items: center; gap: 1rem;">
          <button type="button" class="btn-icon d-lg-none" id="btn-sidebar-toggle" aria-label="Mở menu">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="3" y1="12" x2="21" y2="12"></line>
              <line x1="3" y1="6" x2="21" y2="6"></line>
              <line x1="3" y1="18" x2="21" y2="18"></line>
            </svg>
          </button>
          <div class="breadcrumb" style="font-size: 0.875rem;">
            <span>Cổng Giảng Viên</span>
            <span style="color: var(--text-muted); margin: 0 6px;">/</span>
            <span style="font-weight: 600; color: var(--text-primary);">${title}</span>
          </div>
        </div>

        <div class="topbar-actions" style="display: flex; align-items: center; gap: 12px;">
          <!-- Thông báo Dropdown -->
          <div class="dropdown notification-wrapper">
            <button type="button" class="btn-icon" id="notifications-btn" title="Thông báo giảng dạy">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
              </svg>
              <span class="notification-badge-dot"></span>
            </button>
            <div class="dropdown-menu" id="notifications-dropdown" style="width: 320px; right: 0;">
              <div style="padding: 0.75rem 1rem; border-bottom: 1px solid var(--border-color); font-weight: 700; font-size: 0.8125rem;">
                Thông báo Lớp học
              </div>
              <div style="max-height: 240px; overflow-y: auto;">
                <a href="grading.html" class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                  <strong style="display: block; font-size: 0.75rem; color: var(--primary-600);">📝 4 bài viết IELTS mới nộp</strong>
                  <small style="color: var(--text-muted);">Lớp IELTS-B01 cần chấm trước 20:00 ngày mai</small>
                </a>
                <a href="schedule.html" class="dropdown-item" style="display: block; padding: 0.625rem 1rem;">
                  <strong style="display: block; font-size: 0.75rem;">🔔 Lịch dạy hôm nay lúc 18:00</strong>
                  <small style="color: var(--text-muted);">Phòng LAB-01: Lớp Tiếng Anh Giao Tiếp</small>
                </a>
              </div>
            </div>
          </div>

          <!-- Avatar & Profile Dropdown -->
          <div class="dropdown">
            <div class="user-profile-trigger" id="avatar-dropdown-btn" role="button" tabindex="0">
              <div class="avatar avatar-sm" style="background-color: var(--accent-600); color: white;">LM</div>
              <div style="text-align: left; line-height: 1.2;" class="d-none d-md-block">
                <div style="font-size: 0.8125rem; font-weight: 600;">Lê Thị Mai</div>
                <div style="font-size: 0.7rem; color: var(--text-muted);">teacher@el.com</div>
              </div>
              <span style="color: var(--text-muted); margin-left: 4px;">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </span>
            </div>

            <div class="dropdown-menu" id="avatar-dropdown-menu" style="min-width: 200px; right: 0;">
              <div style="padding: 0.625rem 1rem; border-bottom: 1px solid var(--border-color);">
                <div style="font-weight: 700; font-size: 0.875rem;">Lê Thị Mai, M.Ed</div>
                <span class="badge badge-success" style="margin-top: 4px;">Giảng viên IELTS 8.5</span>
              </div>
              <a href="profile.html" class="dropdown-item">
                <span>Hồ sơ cá nhân</span>
              </a>
              <a href="classes.html" class="dropdown-item">
                <span>Lớp của tôi</span>
              </a>
              <div class="dropdown-divider"></div>
              <a href="../../../index.html" class="dropdown-item text-danger" id="btn-topbar-logout">
                <span>Đăng xuất</span>
              </a>
            </div>
          </div>
        </div>
      </header>

      <!-- Vùng Nội Dung Chính -->
      <main class="page-container" id="app-main-content">
        ${content}
      </main>
    </div>
  </div>

  <!-- Shared Interactive Script -->
  <script src="../../../wwwroot/js/site.js"></script>
</body>
</html>`;
}
