/**
 * site.js - Tương tác giao diện dùng chung cho tất cả các trang HTML
 * Hỗ trợ mở/đóng Sidebar mobile, Dropdown thông báo/avatar, Modal, Tabs
 */

document.addEventListener('DOMContentLoaded', () => {
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

  // 2. Dropdown Avatar & Notifications
  const avatarBtn = document.getElementById('avatar-dropdown-btn');
  const avatarMenu = document.getElementById('avatar-dropdown-menu');
  const notifBtn = document.getElementById('notifications-btn');
  const notifMenu = document.getElementById('notifications-dropdown');

  if (avatarBtn && avatarMenu) {
    avatarBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      if (notifMenu) notifMenu.classList.remove('show');
      avatarMenu.classList.toggle('show');
    });
  }

  if (notifBtn && notifMenu) {
    notifBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      if (avatarMenu) avatarMenu.classList.remove('show');
      notifMenu.classList.toggle('show');
    });
  }

  // Đóng dropdown khi click ra ngoài
  document.addEventListener('click', (e) => {
    if (avatarMenu && !avatarMenu.contains(e.target) && avatarBtn && !avatarBtn.contains(e.target)) {
      avatarMenu.classList.remove('show');
    }
    if (notifMenu && !notifMenu.contains(e.target) && notifBtn && !notifBtn.contains(e.target)) {
      notifMenu.classList.remove('show');
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

  // 4. Modal Dialogs: Đóng khi nhấn nút đóng hoặc backdrop
  document.addEventListener('click', (e) => {
    const dismissBtn = e.target.closest('[data-dismiss="modal"]') || e.target.closest('.modal-close');
    if (dismissBtn) {
      const modal = dismissBtn.closest('.modal-backdrop') || document.querySelector('.modal-backdrop.show');
      if (modal) {
        modal.classList.remove('show');
        setTimeout(() => {
          if (modal.classList.contains('temp-modal')) {
            modal.remove();
          }
        }, 200);
      }
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
