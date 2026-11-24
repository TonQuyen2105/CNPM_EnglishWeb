/**
 * site.js - Tương tác giao diện dùng chung cho tất cả các trang HTML
 * Hỗ trợ mở/đóng Sidebar mobile, Dropdown thông báo/avatar, Modal, Tabs, Đồng bộ Profile
 */

// Xác định phân hệ hiện tại theo đường dẫn / cấu trúc trang
function getCurrentPortalRole() {
  try {
    const pathname = (window.location && window.location.pathname) ? window.location.pathname : '';
    if (/(^|\/)(areas\/)?admin(\/|$)/i.test(pathname)) return 'admin';
    if (/(^|\/)(areas\/)?teacher(\/|$)/i.test(pathname)) return 'teacher';
    if (/(^|\/)(areas\/)?student(\/|$)/i.test(pathname)
      || !!document.querySelector('.student-navbar-wrapper')
      || !!document.getElementById('user-avatar-topbar')) {
      return 'student';
    }
  } catch (_) {}
  return null;
}

function redirectToLoginWithReason(reason) {
  try {
    const pathname = (window.location && window.location.pathname) ? window.location.pathname : '';
    if (/(^|\/)login\.html$/i.test(pathname)) return;
    if (window.__portalSessionRedirecting) return;
    window.__portalSessionRedirecting = true;
    const safeReason = encodeURIComponent(reason || 'role_changed');
    window.location.replace(`/login.html?reason=${safeReason}`);
  } catch (_) {}
}

function getPortalStorageKey(role) {
  switch (String(role || '').trim().toLowerCase()) {
    case 'admin':
      return 'admin_user';
    case 'teacher':
      return 'teacher_user';
    case 'student':
      return 'student_user';
    default:
      return null;
  }
}

/**
 * Đọc thông tin user từ localStorage theo đúng vai trò cổng hiện tại.
 * - Chỉ đọc duy nhất key độc lập của expectedRole ('admin_user' / 'teacher_user' / 'student_user').
 * - Bắt buộc (u.role || '').toLowerCase() === expectedRole; nếu không tồn tại hoặc sai vai trò -> trả về null.
 */
function getStrictPortalUser(expectedRole) {
  const targetRole = String(expectedRole || '').trim().toLowerCase();
  const storageKey = getPortalStorageKey(targetRole);
  if (!storageKey) return null;

  try {
    const raw = localStorage.getItem(storageKey);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (parsed && typeof parsed === 'object' && String(parsed.role || '').trim().toLowerCase() === targetRole) {
      return parsed;
    }
  } catch (_) {}

  return null;
}

function enforcePortalRoleGuard() {
  try {
    const portalRole = getCurrentPortalRole();
    if (!portalRole) return true;

    const user = getStrictPortalUser(portalRole);
    if (!user) {
      const storageKey = getPortalStorageKey(portalRole);
      if (storageKey) {
        try {
          if (localStorage.getItem(storageKey) !== null) {
            localStorage.removeItem(storageKey);
          }
        } catch (_) {}
      }
      redirectToLoginWithReason('auth_required');
      return false;
    }

    return true;
  } catch (_) {
    return true;
  }
}

let __lastSyncedStudentProfileId = null;

// Đồng bộ thông tin học sinh ngay lập tức khi file JS được nạp (chỉ chạy trên cổng Học sinh, tránh ghi đè dropdown Admin/Teacher)
function syncStudentUserSession() {
  try {
    if (getCurrentPortalRole() !== 'student') return;
    if (!enforcePortalRoleGuard()) return;

    const u = getStrictPortalUser('student');
    let name = 'Trần Hoàng';
    let email = 'student@englishlearning.example';
    let userId = null;
    let role = 'Học viên chính thức';

    if (u) {
      if (u.full_name || u.name) name = u.full_name || u.name;
      if (u.email) email = u.email;
      if (u.id) userId = u.id;
      if (u.role_label) role = u.role_label;
      else if (String(u.role || '').toLowerCase() === 'student') role = 'Học viên chính thức';
    }

    const parts = name.trim().split(/\s+/);
    const initials = parts.length === 1 
      ? parts[0].slice(0, 2).toUpperCase() 
      : (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();

    const av = document.getElementById('user-avatar-topbar');
    const nm = document.getElementById('user-name-topbar');
    const em = document.getElementById('user-email-topbar');
    const dn = document.getElementById('user-name-dropdown');
    const de = document.getElementById('user-email-dropdown');
    const rb = document.getElementById('user-role-badge');

    if (av) av.textContent = initials;
    if (nm) nm.textContent = name;
    if (em) { em.textContent = email; em.title = email; }
    if (dn) dn.textContent = name;
    if (de) de.textContent = email;
    if (rb) rb.textContent = role;

    // Đảm bảo mục "Tiến độ học tập" luôn luôn có mặt trong Dropdown
    const menu = document.getElementById('avatar-dropdown-menu');
    if (menu && !menu.querySelector('a[href*="progress.html"]')) {
      const divider = menu.querySelector('.dropdown-divider');
      const a = document.createElement('a');
      a.href = 'progress.html';
      a.className = 'dropdown-item';
      a.id = 'menu-item-progress';
      a.innerHTML = '<span>Tiến độ học tập</span>';
      if (divider) {
        menu.insertBefore(a, divider);
      } else {
        menu.appendChild(a);
      }
    }

    // Chỉ gọi /api/v1/student/profile khi userId thuộc tài khoản role === 'student' hợp lệ
    if (userId && __lastSyncedStudentProfileId !== String(userId)) {
      __lastSyncedStudentProfileId = String(userId);
      fetch(`/api/v1/student/profile?user_id=${encodeURIComponent(userId)}`, {
        headers: { 'Accept': 'application/json' }
      })
      .then(res => res.ok ? res.json() : null)
      .then(json => {
        if (!getStrictPortalUser('student')) return;
        if (json && json.success && json.data) {
          const d = json.data;
          const freshName = d.full_name || name;
          const freshEmail = d.email || email;
          const freshParts = freshName.trim().split(/\s+/);
          const freshInitials = freshParts.length === 1 
            ? freshParts[0].slice(0, 2).toUpperCase() 
            : (freshParts[0][0] + freshParts[freshParts.length - 1][0]).toUpperCase();

          if (av) av.textContent = freshInitials;
          if (nm) nm.textContent = freshName;
          if (em) { em.textContent = freshEmail; em.title = freshEmail; }
          if (dn) dn.textContent = freshName;
          if (de) de.textContent = freshEmail;

          const streakEl = document.getElementById('nav-streak-text');
          if (streakEl && d.streak_days != null) {
            streakEl.textContent = `${d.streak_days} ngày`;
          }
        }
      })
      .catch(() => {});
    }
  } catch (_) {}
}

// Chạy kiểm tra vai trò cổng & đồng bộ session ngay khi script load
if (typeof window !== 'undefined') {
  window.getPortalStorageKey = getPortalStorageKey;
  window.getStrictPortalUser = getStrictPortalUser;
  if (enforcePortalRoleGuard()) {
    syncStudentUserSession();
  }

  // Bọc window.fetch riêng cho cổng Admin: tự động hiện Top Progress Bar + tự đăng nhập lại khi 401/403
  if (getCurrentPortalRole() === 'admin' && typeof window.fetch === 'function' && !window.__adminFetchAuthWrapped) {
    window.__adminFetchAuthWrapped = true;
    const originalFetch = window.fetch.bind(window);
    window.__adminSilentFetch = originalFetch;
    let activeRequests = 0;
    let progressVal = 0;
    let trickleTimer = null;
    let hideTimer = null;

    function getTopProgressBar() {
      let bar = document.getElementById('admin-top-progress');
      if (!bar && (document.body || document.documentElement)) {
        bar = document.createElement('div');
        bar.id = 'admin-top-progress';
        bar.setAttribute('aria-hidden', 'true');
        (document.body || document.documentElement).appendChild(bar);
      }
      return bar;
    }

    function startTopProgress() {
      activeRequests++;
      if (activeRequests > 1) return;
      if (hideTimer) {
        clearTimeout(hideTimer);
        hideTimer = null;
      }
      const bar = getTopProgressBar();
      if (!bar) return;
      progressVal = 24;
      bar.style.transition = 'none';
      bar.style.width = '0%';
      void bar.offsetWidth;
      bar.style.transition = 'width 220ms ease-out, opacity 200ms ease';
      bar.classList.add('is-active');
      bar.style.width = `${progressVal}%`;
      if (trickleTimer) clearInterval(trickleTimer);
      trickleTimer = setInterval(() => {
        if (progressVal < 86) {
          progressVal += Math.max(1.5, (86 - progressVal) * 0.14);
          bar.style.width = `${progressVal.toFixed(1)}%`;
        }
      }, 240);
    }

    function finishTopProgress() {
      activeRequests = Math.max(0, activeRequests - 1);
      if (activeRequests > 0) return;
      if (trickleTimer) {
        clearInterval(trickleTimer);
        trickleTimer = null;
      }
      const bar = getTopProgressBar();
      if (!bar) return;
      progressVal = 100;
      bar.style.width = '100%';
      hideTimer = setTimeout(() => {
        if (activeRequests === 0) {
          bar.classList.remove('is-active');
          setTimeout(() => {
            if (activeRequests === 0) bar.style.width = '0%';
          }, 190);
        }
      }, 160);
    }

    window.fetch = async function (input, init) {
      let isAdminApi = false;
      const isSilent = !!(init && init.__silent);
      try {
        const rawUrl = typeof input === 'string'
          ? input
          : (input && typeof input.url === 'string' ? input.url : '');
        const pathname = rawUrl ? new URL(rawUrl, window.location.origin).pathname : '';
        isAdminApi = /^\/(Admin\/api|api\/admin)(\/|$)/i.test(pathname);
      } catch (_) {}

      const trackProgress = isAdminApi && !isSilent;
      if (trackProgress) startTopProgress();
      try {
        const res = await originalFetch(input, init);
        if (isAdminApi && !isSilent && (res.status === 401 || res.status === 403)) {
          try { localStorage.removeItem('admin_user'); } catch (_) {}
          redirectToLoginWithReason('auth_required');
        }
        return res;
      } finally {
        if (trackProgress) finishTopProgress();
      }
    };
  }

  // Chỉ lắng nghe thay đổi của đúng key thuộc cổng hiện tại (hoàn toàn bỏ qua 2 cổng còn lại)
  window.addEventListener('storage', (e) => {
    try {
      const portalRole = getCurrentPortalRole();
      if (!portalRole) return;
      const myKey = getPortalStorageKey(portalRole);
      if (!myKey) return;

      if (e.key === null) {
        redirectToLoginWithReason('session_changed');
        return;
      }

      if (e.key !== myKey) return;

      if (!e.newValue) {
        redirectToLoginWithReason('session_changed');
        return;
      }

      let newUser = null;
      let oldUser = null;
      try { newUser = JSON.parse(e.newValue); } catch (_) {}
      try { if (e.oldValue) oldUser = JSON.parse(e.oldValue); } catch (_) {}

      const newRole = newUser && typeof newUser === 'object'
        ? String(newUser.role || '').trim().toLowerCase()
        : '';
      const roleInvalid = !newUser || newRole !== portalRole;
      const userChanged = !!(oldUser && newUser && oldUser.id != null && newUser.id != null && String(oldUser.id) !== String(newUser.id));

      if (roleInvalid || userChanged) {
        redirectToLoginWithReason('session_changed');
      }
    } catch (_) {}
  });
}

document.addEventListener('DOMContentLoaded', () => {
  // Kiểm tra lại vai trò cổng & đồng bộ sau khi DOM đã sẵn sàng
  if (!enforcePortalRoleGuard()) return;
  syncStudentUserSession();

  // 1. Sidebar Mobile Toggle
  const sidebarToggleBtn = document.getElementById('btn-sidebar-toggle');
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.getElementById('sidebar-backdrop');

  if (sidebarToggleBtn && sidebar && backdrop) {
    sidebarToggleBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      sidebar.classList.toggle('sidebar-open');
      backdrop.classList.toggle('active');
    });

    backdrop.addEventListener('click', () => {
      sidebar.classList.remove('sidebar-open');
      backdrop.classList.remove('active');
    });
  }

  // 1b. Sidebar Accordion Group Toggle ([data-nav-group-toggle]) & Admin Hover Prefetch
  document.addEventListener('click', (e) => {
    const groupToggle = e.target.closest('[data-nav-group-toggle]');
    if (!groupToggle) return;
    e.preventDefault();
    const group = groupToggle.closest('.nav-group');
    if (!group) return;
    const isOpen = group.classList.toggle('is-open');
    groupToggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
  });

  if (getCurrentPortalRole() === 'admin' && window.location.protocol !== 'file:') {
    const prefetchedAdminUrls = new Set();
    const prefetchedApiTimestamps = new Map();
    const API_PREFETCH_COOLDOWN_MS = 20000;
    const ADMIN_PAGE_PREFETCH_APIS = {
      '/Admin/dashboard.html': '/Admin/api/Dashboard/Summary',
      '/Admin/reports.html': '/Admin/api/Reports/Summary?quarter=0',
      '/Admin/subjects.html': '/Admin/api/Subjects/List?search=&status=all',
      '/Admin/courses.html': '/Admin/api/Courses/List?lookups=true',
      '/Admin/course-objectives.html': '/Admin/api/Courses/List?lookups=false',
      '/Admin/classes.html': '/Admin/api/Classes/List',
      '/Admin/schedules.html': '/Admin/api/Schedules/Weekly',
      '/Admin/facilities.html': '/Admin/api/Facilities/List',
      '/Admin/enrollments.html': '/Admin/api/Enrollments/List?page=1&pageSize=15',
      '/Admin/teachers.html': '/Admin/api/Teachers/List?search=&verified=all',
      '/Admin/users.html': '/Admin/api/Users/List?search=&role=all&status=all&page=1&pageSize=10',
      '/Admin/exams.html': '/Admin/api/Exams/List?search=&courseId=0&published=all&lookups=true',
      '/Admin/reviews.html': '/Admin/api/Reviews/List?search=&courseId=0&rating=0&page=1&pageSize=8&lookups=true'
    };

    document.addEventListener('pointerenter', (e) => {
      if (!e.target || typeof e.target.closest !== 'function') return;
      const link = e.target.closest('.sidebar-nav a[href^="/Admin/"][href$=".html"]');
      if (!link) return;
      const href = link.getAttribute('href');
      if (!href || link.pathname === window.location.pathname) return;

      if (!prefetchedAdminUrls.has(href)) {
        prefetchedAdminUrls.add(href);
        const prefetchTag = document.createElement('link');
        prefetchTag.rel = 'prefetch';
        prefetchTag.href = href;
        document.head.appendChild(prefetchTag);
      }

      const apiUrl = ADMIN_PAGE_PREFETCH_APIS[link.pathname] || ADMIN_PAGE_PREFETCH_APIS[href];
      if (apiUrl) {
        const now = Date.now();
        const lastFetchedAt = prefetchedApiTimestamps.get(apiUrl) || 0;
        if (now - lastFetchedAt > API_PREFETCH_COOLDOWN_MS) {
          prefetchedApiTimestamps.set(apiUrl, now);
          const silentFetch = typeof window.__adminSilentFetch === 'function'
            ? window.__adminSilentFetch
            : window.fetch;
          silentFetch(apiUrl, {
            method: 'GET',
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
          }).catch(() => {});
        }
      }
    }, { capture: true, passive: true });
  }

  // 2. Dropdown Avatar, Notifications & Admin Tools Menu - Quản lý tập trung bằng Event Delegation
  if (getCurrentPortalRole() === 'admin' && !document.getElementById('admin-tools-dropdown-style')) {
    const styleEl = document.createElement('style');
    styleEl.id = 'admin-tools-dropdown-style';
    styleEl.textContent = [
      '.admin-tools-dropdown { position: relative; display: inline-flex; }',
      '.admin-tools-menu { position: absolute; top: calc(100% + 6px); right: 0; min-width: 205px; background: #FFFFFF; border: 1px solid var(--border-color, #E2E8F0); border-radius: 10px; box-shadow: 0 10px 25px rgba(15, 23, 42, 0.12); padding: 5px; z-index: 60; display: none; flex-direction: column; gap: 2px; }',
      '.admin-tools-menu.show { display: flex; }',
      '.admin-tools-menu > button, .admin-tools-menu > a { width: 100% !important; justify-content: flex-start !important; text-align: left !important; background: transparent !important; border: none !important; box-shadow: none !important; color: var(--text-primary, #0F172A) !important; font-size: 0.8125rem !important; font-weight: 500 !important; padding: 0.5rem 0.75rem !important; border-radius: 7px !important; height: auto !important; gap: 8px !important; display: flex !important; align-items: center !important; text-decoration: none !important; cursor: pointer; }',
      '.admin-tools-menu > button:hover:not(:disabled), .admin-tools-menu > a:hover:not(.disabled) { background: var(--neutral-100, #F1F5F9) !important; color: var(--primary-700, #1D4ED8) !important; }',
      '.admin-tools-menu > button.btn-primary { background: #EFF6FF !important; color: #1D4ED8 !important; font-weight: 600 !important; }',
      '.admin-tools-menu > button:disabled { opacity: 0.5 !important; cursor: not-allowed !important; pointer-events: none !important; }'
    ].join('\n');
    document.head.appendChild(styleEl);
  }

  document.addEventListener('click', (e) => {
    const toolsTrigger = e.target.closest('[data-admin-tools-toggle]');
    const clickedToolItem = e.target.closest('.admin-tools-menu > button, .admin-tools-menu > a');
    const avatarTrigger = e.target.closest('#avatar-dropdown-btn, .user-profile-trigger');
    const notifTrigger = e.target.closest('#notifications-btn, .notification-wrapper > button');
    const avatarMenu = document.getElementById('avatar-dropdown-menu');
    const notifMenu = document.getElementById('notifications-dropdown');

    if (toolsTrigger) {
      e.preventDefault();
      e.stopPropagation();
      const wrapper = toolsTrigger.closest('.admin-tools-dropdown');
      const targetMenu = wrapper ? wrapper.querySelector('.admin-tools-menu') : null;
      document.querySelectorAll('.admin-tools-menu.show').forEach(m => {
        if (m !== targetMenu) m.classList.remove('show');
      });
      if (avatarMenu) avatarMenu.classList.remove('show');
      if (notifMenu) notifMenu.classList.remove('show');
      if (targetMenu) targetMenu.classList.toggle('show');
      return;
    }

    if (clickedToolItem && !clickedToolItem.disabled) {
      setTimeout(() => {
        document.querySelectorAll('.admin-tools-menu.show').forEach(m => m.classList.remove('show'));
      }, 0);
    } else if (!e.target.closest('.admin-tools-dropdown')) {
      document.querySelectorAll('.admin-tools-menu.show').forEach(m => m.classList.remove('show'));
    }

    if (avatarTrigger) {
      e.preventDefault();
      e.stopPropagation();
      document.querySelectorAll('.admin-tools-menu.show').forEach(m => m.classList.remove('show'));
      if (notifMenu) notifMenu.classList.remove('show');
      if (avatarMenu) avatarMenu.classList.toggle('show');
      return;
    }

    if (notifTrigger) {
      e.preventDefault();
      e.stopPropagation();
      document.querySelectorAll('.admin-tools-menu.show').forEach(m => m.classList.remove('show'));
      if (avatarMenu) avatarMenu.classList.remove('show');
      if (notifMenu) notifMenu.classList.toggle('show');
      return;
    }

    // Đóng dropdown khi click ra ngoài menu
    if (avatarMenu && !avatarMenu.contains(e.target)) {
      avatarMenu.classList.remove('show');
    }
    if (notifMenu && !notifMenu.contains(e.target)) {
      notifMenu.classList.remove('show');
    }
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      const avatarMenu = document.getElementById('avatar-dropdown-menu');
      const notifMenu = document.getElementById('notifications-dropdown');
      if (avatarMenu) avatarMenu.classList.remove('show');
      if (notifMenu) notifMenu.classList.remove('show');
      document.querySelectorAll('.admin-tools-menu.show').forEach(m => m.classList.remove('show'));
    }
  });

  // 3. Xử lý Đăng xuất độc lập theo từng cổng (Tương thích cả file:// và chạy qua Server)
  const logoutBtns = document.querySelectorAll('#btn-topbar-logout, #sidebar-logout-btn, #btn-student-logout, .btn-logout, .sidebar-footer a.text-danger, a[href*="logout"]');
  logoutBtns.forEach(btn => {
    if (btn.hasAttribute('data-teacher-logout')) return;
    btn.addEventListener('click', async (e) => {
      e.preventDefault();
      const portalRole = getCurrentPortalRole();
      const storageKey = getPortalStorageKey(portalRole);
      if (window.location.protocol !== 'file:') {
        try {
          const logoutUrl = `/Account/Session?role=${encodeURIComponent(portalRole || '')}`;
          const response = await fetch(logoutUrl, { method: 'DELETE', credentials: 'same-origin' });
          if (!response.ok) throw new Error('Logout failed');
        } catch (_) {
          alert('Chưa đăng xuất được. Vui lòng thử lại.');
          return;
        }
      }
      try {
        if (storageKey) {
          localStorage.removeItem(storageKey);
        }
      } catch (_) {}

      if (window.location.protocol === 'file:') {
        window.location.href = '../../../index.html';
      } else {
        window.location.href = '/';
      }
    });
  });

  // 4. Modal Dialogs: Mở & Đóng tương tác
  document.addEventListener('click', (e) => {
    // Mở modal khi bấm vào nút có data-modal-target hoặc data-toggle="modal"
    const openBtn = e.target.closest('[data-modal-target]') || e.target.closest('[data-toggle="modal"]');
    if (openBtn) {
      e.preventDefault();
      const modalId = openBtn.getAttribute('data-modal-target') || openBtn.getAttribute('data-target')?.replace('#', '');
      const modal = document.getElementById(modalId);
      if (modal) {
        modal.classList.add('open', 'show');
      }
      return;
    }

    // Đóng khi nhấn nút đóng hoặc backdrop
    const dismissBtn = e.target.closest('[data-dismiss="modal"]') || e.target.closest('.modal-close');
    if (dismissBtn) {
      e.preventDefault();
      const modal = dismissBtn.closest('.modal-backdrop') || document.querySelector('.modal-backdrop.open, .modal-backdrop.show');
      if (modal) {
        modal.classList.remove('open', 'show');
        setTimeout(() => {
          if (modal.classList.contains('temp-modal')) modal.remove();
        }, 200);
      }
      return;
    }

    // Đóng khi click ngoài hộp thoại modal (vào vùng mờ backdrop)
    if (e.target.classList.contains('modal-backdrop')) {
      e.target.classList.remove('open', 'show');
    }
  });

  // Xử lý submit các form thêm dữ liệu trong Modal (hiển thị thông báo thành công và đóng modal)
  document.addEventListener('submit', (e) => {
    const modalForm = e.target.closest('.modal-form, [data-modal-form]');
    if (modalForm) {
      e.preventDefault();
      const submitBtn = modalForm.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.textContent = 'Đang lưu...';
      }
      setTimeout(() => {
        const modal = modalForm.closest('.modal-backdrop');
        if (modal) modal.classList.remove('open', 'show');
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.textContent = 'Lưu thông tin';
        }
        alert('Thành công: Đã lưu thông tin mới vào hệ thống mô phỏng!');
        modalForm.reset();
      }, 500);
    }
  });

  // 5. Tabs Switching (Tab panes)
  const tabButtons = document.querySelectorAll('.tab-btn, .tab-item[data-tab]');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-tab');
      const container = btn.closest('.tabs-container') || document;
      
      // Bỏ active tất cả tab buttons cùng nhóm
      btn.parentElement.querySelectorAll('.tab-btn, .tab-item').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      // Ẩn các pane và hiện pane tương ứng
      if (targetId) {
        container.querySelectorAll('.tab-pane').forEach(pane => {
          if (pane.id === targetId) {
            pane.classList.add('active');
            pane.style.display = 'block';
          } else {
            pane.classList.remove('active');
            pane.style.display = 'none';
          }
        });
      }
    });
  });

  // 6. Flashcard 3D Flip (nếu có trên trang)
  const flashcard = document.getElementById('flashcard-flipper');
  const flipBtn = document.getElementById('btn-flip-card');
  if (flashcard) {
    const toggleFlip = () => flashcard.classList.toggle('flipped');
    flashcard.addEventListener('click', toggleFlip);
    if (flipBtn) flipBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      toggleFlip();
    });
  }
});

/**
 * Tạo gợi ý Mô tả chung riêng biệt, phù hợp với tên môn học, khóa học và cấp độ.
 */
window.buildCourseDescriptionSuggestion = function (title, subjectName, level) {
  const cleanTitle = String(title || '').trim();
  const cleanSubject = String(subjectName || '').trim();
  const normLevel = String(level || 'beginner').trim().toLowerCase();
  const combined = `${cleanTitle} ${cleanSubject}`.toLowerCase();

  const audienceByLevel = {
    beginner: 'Phù hợp cho người mới bắt đầu hoặc mất gốc muốn xây dựng nền tảng vững chắc.',
    intermediate: 'Dành cho học viên đã có kiến thức nền tảng và cần bứt phá lên trình độ trung cấp.',
    advanced: 'Dành cho học viên trình độ khá giỏi muốn hoàn thiện kỹ năng chuyên sâu và đạt chuẩn đầu ra cao.'
  };

  const rules = [
    {
      match: /ielts/,
      knowledge: `Trang bị chiến lược làm bài IELTS cho môn "${cleanTitle || 'Luyện thi IELTS'}", phát triển từ vựng học thuật, cấu trúc câu ghi điểm và luyện tập sát đề thi thật 4 kỹ năng.`,
      audience: {
        beginner: 'Dành cho học viên làm quen định dạng IELTS, củng cố nền tảng từ band 4.0 - 5.5.',
        intermediate: 'Dành cho học viên hướng tới mục tiêu IELTS 6.0 - 7.0, tối ưu Listening, Reading, Writing Task 1-2 và Speaking.',
        advanced: 'Dành cho học viên chinh phục band IELTS 7.5+ với tư duy phản biện và ngôn ngữ học thuật C1-C2.'
      }
    },
    {
      match: /toeic/,
      knowledge: `Hệ thống hóa từ vựng thương mại, ngữ pháp trọng tâm và chiến thuật xử lý bẫy đề TOEIC trong môn "${cleanTitle || 'Luyện thi TOEIC'}".`,
      audience: {
        beginner: 'Dành cho sinh viên và người đi làm bắt đầu luyện thi TOEIC mục tiêu 450 - 550+.',
        intermediate: 'Dành cho học viên cần tăng tốc điểm số TOEIC 650 - 750+ phục vụ tốt nghiệp và công việc.',
        advanced: 'Dành cho học viên mục tiêu TOEIC 800+ và hoàn thiện cả 4 kỹ năng chuẩn quốc tế ETS.'
      }
    },
    {
      match: /thương mại|business|đàm phán|công sở/,
      knowledge: `Cung cấp mẫu câu giao tiếp công sở, viết email, thuyết trình và đàm phán thực tế trong môn "${cleanTitle || 'Tiếng Anh Thương Mại'}".`,
      audience: {
        beginner: 'Dành cho nhân sự mới đi làm cần sử dụng tiếng Anh cơ bản trong giao tiếp văn phòng.',
        intermediate: 'Dành cho nhân viên văn phòng làm việc với đối tác, họp nhóm và trao đổi công việc bằng tiếng Anh.',
        advanced: 'Dành cho quản lý và chuyên viên thường xuyên đàm phán, thuyết trình trong môi trường đa quốc gia.'
      }
    },
    {
      match: /giao tiếp|communication|phản xạ/,
      knowledge: `Tập trung phát triển phản xạ nghe nói tự nhiên, mẫu câu ứng dụng theo tình huống thực tế của môn "${cleanTitle || 'Tiếng Anh Giao Tiếp'}".`,
      audience: {
        beginner: 'Dành cho người mất gốc hoặc ngại nói muốn giao tiếp trôi chảy các chủ đề đời sống hàng ngày.',
        intermediate: 'Dành cho người học muốn mở rộng vốn diễn đạt, thảo luận và phản biện tự tin hơn.',
        advanced: 'Dành cho học viên muốn giao tiếp lưu loát, tự nhiên như người bản xứ trong mọi ngữ cảnh.'
      }
    },
    {
      match: /phát âm|ipa|ngữ điệu|pronunciation/,
      knowledge: `Chuẩn hóa bảng phiên âm quốc tế IPA, trọng âm, nối âm và ngữ điệu tự nhiên trong môn "${cleanTitle || 'Phát âm chuẩn'}".`,
      audience: {
        beginner: 'Dành cho người học muốn sửa lỗi phát âm sai, phát âm rõ âm cuối ngay từ đầu.',
        intermediate: 'Dành cho học viên muốn cải thiện ngữ điệu, nói có nhấn nhá và nghe hiểu người bản xứ tốt hơn.',
        advanced: 'Dành cho học viên tinh chỉnh giọng điệu tự nhiên phục vụ thuyết trình và thi nói.'
      }
    },
    {
      match: /ngữ pháp|grammar|viết|writing/,
      knowledge: `Hệ thống hóa kiến thức ngữ pháp trọng tâm và cách vận dụng chính xác vào câu văn trong môn "${cleanTitle || 'Ngữ pháp tiếng Anh'}".`,
      audience: {
        beginner: 'Dành cho người học cần nắm vững các thì cơ bản, từ loại và cấu trúc câu đơn - câu ghép.',
        intermediate: 'Dành cho học viên cần củng cố ngữ pháp nâng cao phục vụ đọc hiểu và viết đoạn văn mạch lạc.',
        advanced: 'Dành cho học viên rèn luyện cấu trúc học thuật phức tạp (đảo ngữ, mệnh đề rút gọn, viết luận).'
      }
    },
    {
      match: /thiếu nhi|trẻ em|kids|starters|movers|flyers|cambridge/,
      knowledge: `Thiết kế nội dung trực quan qua hình ảnh, bài hát và hoạt động tương tác sinh động cho môn "${cleanTitle || 'Tiếng Anh Thiếu Nhi'}".`,
      audience: {
        beginner: 'Dành cho các bé mới làm quen tiếng Anh (6 - 10 tuổi), khơi gợi hứng thú nghe nói tự nhiên.',
        intermediate: 'Dành cho học sinh tiểu học và THCS ôn luyện chứng chỉ Cambridge Movers / Flyers.',
        advanced: 'Dành cho học sinh thiếu niên phát triển toàn diện kỹ năng giao tiếp và tư duy bằng tiếng Anh.'
      }
    }
  ];

  const matched = rules.find(r => r.match.test(combined));
  const knowledge = matched
    ? matched.knowledge
    : `Trang bị kiến thức và kỹ năng trọng tâm của môn học "${cleanTitle || cleanSubject || 'Tiếng Anh'}"${cleanSubject ? ` thuộc khóa học ${cleanSubject}` : ''}.`;
  const audience = matched
    ? (matched.audience[normLevel] || matched.audience.beginner)
    : (audienceByLevel[normLevel] || audienceByLevel.beginner);

  return {
    knowledge,
    audience,
    fullText: `• Mô tả chung:\n\t• ${knowledge}\n\t• ${audience}`
  };
};

// ===== Helpers hiển thị bài học lấy file PDF / Video từ Supabase Storage =====
(function () {
  const SUPABASE_URL = 'https://wymdewnklhrnmirbvgxi.supabase.co';
  const DEFAULT_STORAGE_BUCKET = 'Lesson_media';

  function esc(str) {
    if (str === null || str === undefined) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function buildStoragePublicUrl(mediaObj) {
    if (!mediaObj) return null;
    if (typeof mediaObj === 'string') {
      if (/^https?:\/\//i.test(mediaObj)) return mediaObj;
      return `${SUPABASE_URL}/storage/v1/object/public/${DEFAULT_STORAGE_BUCKET}/${mediaObj.replace(/^\/+/, '')}`;
    }
    if (mediaObj.Url || mediaObj.url) return mediaObj.Url || mediaObj.url;
    const key = mediaObj.Key || mediaObj.key || mediaObj.Path || mediaObj.path;
    if (!key) return null;
    if (/^https?:\/\//i.test(key)) return key;

    const bucket = mediaObj.Bucket || mediaObj.bucket || DEFAULT_STORAGE_BUCKET;
    const cleanKey = String(key).replace(/^\/+/, '');
    if (cleanKey.startsWith(bucket + '/')) {
      return `${SUPABASE_URL}/storage/v1/object/public/${cleanKey}`;
    }
    return `${SUPABASE_URL}/storage/v1/object/public/${bucket}/${cleanKey}`;
  }

  window.parseLessonMedia = function (lessonOrContent) {
    if (lessonOrContent && typeof lessonOrContent === 'object' && lessonOrContent.media) {
      const m = lessonOrContent.media;
      return {
        isStructured: !!m.isStructured,
        text: (m.text || '').trim(),
        pdfUrl: m.pdfUrl || null,
        pdfName: m.pdfName || 'Tài liệu PDF',
        pdfBucket: m.pdfBucket || DEFAULT_STORAGE_BUCKET,
        videoUrl: m.videoUrl || null,
        videoName: m.videoName || 'Video bài giảng',
        videoBucket: m.videoBucket || DEFAULT_STORAGE_BUCKET,
        requiresPublicBucket: !!m.requiresPublicBucket
      };
    }

    const raw = (typeof lessonOrContent === 'string' ? lessonOrContent : (lessonOrContent?.content || '')).trim();
    if (!raw) {
      return { isStructured: false, text: '', pdfUrl: null, pdfName: '', videoUrl: null, videoName: '' };
    }

    const prefix = 'EL_LESSON_MEDIA_V1';
    if (raw.startsWith(prefix)) {
      const jsonPart = raw.slice(prefix.length).trim();
      try {
        const parsed = JSON.parse(jsonPart);
        const pdfObj = parsed.Pdf || parsed.pdf || null;
        const videoObj = parsed.Video || parsed.video || null;
        return {
          isStructured: true,
          text: (parsed.Text || parsed.text || '').trim(),
          pdfUrl: buildStoragePublicUrl(pdfObj),
          pdfName: pdfObj?.FileName || pdfObj?.Name || pdfObj?.Key || 'Tài liệu PDF',
          videoUrl: buildStoragePublicUrl(videoObj),
          videoName: videoObj?.FileName || videoObj?.Name || videoObj?.Key || 'Video bài giảng'
        };
      } catch (e) {
        console.warn('Không parse được JSON bài học:', e);
      }
    }

    return { isStructured: false, text: raw, pdfUrl: null, pdfName: '', videoUrl: null, videoName: '' };
  };

  window.renderLessonReadOnlyItem = function (lesson, typeLabel) {
    const media = window.parseLessonMedia(lesson);
    const badges = [];
    if (media.pdfUrl) badges.push('<span class="badge badge-warning" style="margin-left: 6px;">PDF</span>');
    if (media.videoUrl) badges.push('<span class="badge badge-success" style="margin-left: 6px;">Video</span>');

    let previewSub = '';
    if (media.pdfUrl || media.videoUrl) {
      const parts = [];
      if (media.pdfUrl) parts.push(`PDF: ${esc(media.pdfName)}`);
      if (media.videoUrl) parts.push(`Video: ${esc(media.videoName)}`);
      previewSub = `<div style="font-size: 0.78rem; color: var(--primary-600); margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${parts.join(' • ')}</div>`;
    } else if (media.text) {
      previewSub = `<div style="font-size: 0.78rem; color: var(--text-muted); margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${esc(media.text)}</div>`;
    }

    return `
      <div style="padding: 10px 12px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between; align-items: center; gap: 12px;">
        <div style="min-width: 0;">
          <div>
            <strong>Bài ${lesson.orderIndex}:</strong> ${esc(lesson.title)}
            <span class="badge badge-primary" style="margin-left: 6px;">${esc(typeLabel)}</span>
            ${badges.join('')}
          </div>
          ${previewSub}
        </div>
        <div style="display: inline-flex; gap: 6px; flex-shrink: 0;">
          <button type="button" class="btn btn-sm btn-secondary" data-view-lesson="${lesson.id}">Xem bài học</button>
        </div>
      </div>
    `;
  };

  window.openLessonMediaModal = function (lesson, typeLabel, updatedLabel, openModalFn) {
    if (!lesson) return;
    const media = window.parseLessonMedia(lesson);

    document.getElementById('lv-modal-title').textContent = `Bài ${lesson.orderIndex}: ${lesson.title || ''}`;
    document.getElementById('lv-order-badge').textContent = `Bài ${lesson.orderIndex}`;
    document.getElementById('lv-type-badge').textContent = typeLabel;
    document.getElementById('lv-updated-at').textContent = updatedLabel;
    document.getElementById('lv-title').textContent = lesson.title || '--';

    const textSection = document.getElementById('lv-text-section');
    const contentEl = document.getElementById('lv-content');
    if (textSection && contentEl) {
      if (media.text) {
        textSection.style.display = 'block';
        contentEl.textContent = media.text;
      } else {
        textSection.style.display = 'none';
        contentEl.textContent = '';
      }
    }

    const viewerEl = document.getElementById('lv-media-viewer');
    const extLink = document.getElementById('lv-open-external');
    const primaryUrl = media.pdfUrl || media.videoUrl;

    if (extLink) {
      if (primaryUrl) {
        extLink.href = primaryUrl;
        extLink.style.display = 'inline-flex';
      } else {
        extLink.style.display = 'none';
      }
    }

    if (viewerEl) {
      let html = '';
      if (media.requiresPublicBucket) {
        const bucketLabel = esc(media.pdfBucket || media.videoBucket || DEFAULT_STORAGE_BUCKET);
        html += `
          <div style="padding: 0.75rem 1rem; background: #FEF3C7; color: #92400E; border-bottom: 1px solid #F59E0B; font-size: 0.825rem; line-height: 1.5;">
            Bucket <strong>${bucketLabel}</strong> trên Supabase Storage đang ở chế độ <strong>Private</strong> (<code>public = false</code>), nên trình duyệt chưa thể mở trực tiếp qua <code>/object/public/</code>.
            Hãy bật <strong>Public bucket</strong> cho <strong>${bucketLabel}</strong> trong Supabase Storage (hoặc thêm <code>Supabase:ServiceRoleKey</code> vào <code>appsettings.json</code>).
          </div>`;
      }
      if (media.videoUrl) {
        html += `
          <div style="padding: 0.75rem;">
            <video controls style="width: 100%; max-height: 420px; border-radius: 6px; background: #000;" src="${esc(media.videoUrl)}">
              Trình duyệt không hỗ trợ phát video trực tiếp.
            </video>
          </div>`;
      }
      if (media.pdfUrl) {
        html += `
          <iframe src="${esc(media.pdfUrl)}" style="width: 100%; height: 480px; border: none; display: block;" title="Tài liệu bài giảng"></iframe>`;
      }
      if (!html) {
        html = '<div style="padding: 1.25rem; text-align: center; color: var(--text-muted); font-size: 0.875rem;">Giảng viên chưa đính kèm tệp PDF/Video từ Storage cho bài học này.</div>';
      }
      viewerEl.innerHTML = html;
    }

    if (typeof openModalFn === 'function') {
      openModalFn('modal-lesson-view');
    } else {
      const m = document.getElementById('modal-lesson-view');
      if (m) m.classList.add('open', 'show');
    }
  };
})();
