/**
 * site.js - Tương tác giao diện dùng chung cho tất cả các trang HTML
 * Hỗ trợ mở/đóng Sidebar mobile, Dropdown thông báo/avatar, Modal, Tabs, Đồng bộ Profile
 */

// Đồng bộ thông tin học sinh ngay lập tức khi file JS được nạp (chống giật layout)
function syncStudentUserSession() {
  try {
    const raw = localStorage.getItem('student_user') || localStorage.getItem('session_user') || localStorage.getItem('user');
    let name = 'Trần Hoàng';
    let email = 'student@englishlearning.example';
    let userId = null;
    let role = 'Học viên chính thức';

    if (raw) {
      const u = JSON.parse(raw);
      if (u.full_name || u.name) name = u.full_name || u.name;
      if (u.email) email = u.email;
      if (u.id) userId = u.id;
      if (u.role_label) role = u.role_label;
      else if (u.role === 'student') role = 'Học viên chính thức';
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

    // Nếu có userId, fetch backend /api/v1/student/profile để cập nhật dữ liệu mới nhất (streak, name, role)
    if (userId) {
      fetch(`/api/v1/student/profile?user_id=${userId}`, {
        headers: { 'Accept': 'application/json' }
      })
      .then(res => res.ok ? res.json() : null)
      .then(json => {
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

// Chạy đồng bộ session ngay khi script load
if (typeof window !== 'undefined') {
  syncStudentUserSession();
}

document.addEventListener('DOMContentLoaded', () => {
  // Đồng bộ lại sau khi DOM đã sẵn sàng
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

  // 2. Dropdown Avatar & Notifications - Quản lý tập trung bằng Event Delegation
  document.addEventListener('click', (e) => {
    const avatarTrigger = e.target.closest('#avatar-dropdown-btn, .user-profile-trigger');
    const notifTrigger = e.target.closest('#notifications-btn, .notification-wrapper > button');
    const avatarMenu = document.getElementById('avatar-dropdown-menu');
    const notifMenu = document.getElementById('notifications-dropdown');

    if (avatarTrigger) {
      e.preventDefault();
      e.stopPropagation();
      if (notifMenu) notifMenu.classList.remove('show');
      if (avatarMenu) avatarMenu.classList.toggle('show');
      return;
    }

    if (notifTrigger) {
      e.preventDefault();
      e.stopPropagation();
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
    }
  });

  // 3. Xử lý Đăng xuất (Tương thích cả file:// và chạy qua Server)
  const logoutBtns = document.querySelectorAll('#btn-topbar-logout, #sidebar-logout-btn, #btn-student-logout, .btn-logout, a[href*="logout"]');
  logoutBtns.forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      try {
        localStorage.removeItem('session_user');
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
