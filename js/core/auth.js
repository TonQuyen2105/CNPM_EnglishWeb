/**
 * Authentication & Session Service
 * Quản lý phiên làm việc, đăng nhập, phân quyền, route guard
 */

import { storage } from './storage.js';
import { eventBus } from './eventBus.js';

const SESSION_KEY = 'session_user';

class AuthService {
  constructor() {
    this._user = storage.get(SESSION_KEY, null);
  }

  /**
   * Lấy người dùng đang đăng nhập
   * @returns {Object|null}
   */
  getCurrentUser() {
    return this._user;
  }

  /**
   * Kiểm tra đã đăng nhập chưa
   * @returns {boolean}
   */
  isAuthenticated() {
    return !!this._user;
  }

  /**
   * Kiểm tra vai trò của người dùng
   * @param {string|string[]} roles 
   * @returns {boolean}
   */
  hasRole(roles) {
    if (!this._user) return false;
    const roleList = Array.isArray(roles) ? roles : [roles];
    return roleList.includes(this._user.role);
  }

  /**
   * Đăng nhập với email và mật khẩu
   * @param {string} email 
   * @param {string} password 
   * @returns {Promise<{success: boolean, user?: Object, error?: string}>}
   */
  async login(email, password) {
    // Đảm bảo dữ liệu bảng users tồn tại
    const users = storage.getTable('users');
    const normalizedEmail = (email || '').trim().toLowerCase();

    const user = users.find(u => u.email.toLowerCase() === normalizedEmail);

    if (!user) {
      return { success: false, error: 'Email hoặc mật khẩu không chính xác.' };
    }

    if (user.password !== password) {
      return { success: false, error: 'Email hoặc mật khẩu không chính xác.' };
    }

    if (user.is_active === 0 || user.is_active === false) {
      return { success: false, error: 'Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.' };
    }

    // Lưu session (ẩn mật khẩu khi lưu session)
    const { password: _, ...safeUser } = user;
    this._user = safeUser;
    storage.set(SESSION_KEY, safeUser);

    eventBus.emit('auth:login', safeUser);

    return { success: true, user: safeUser };
  }

  /**
   * Đăng xuất
   */
  logout() {
    this._user = null;
    storage.remove(SESSION_KEY);
    eventBus.emit('auth:logout');
    
    // Tìm đường dẫn về trang đăng nhập
    const isSubDir = window.location.pathname.includes('/admin/') || 
                     window.location.pathname.includes('/teacher/') || 
                     window.location.pathname.includes('/student/');
    window.location.href = isSubDir ? '../index.html' : './index.html';
  }

  /**
   * Chuyển hướng tới cổng tương ứng với role
   * @param {string} role 
   */
  redirectToPortal(role = null) {
    const targetRole = role || (this._user ? this._user.role : null);
    if (!targetRole) {
      window.location.href = './index.html';
      return;
    }

    const path = window.location.pathname;
    const isInSubFolder = path.includes('/admin/') || path.includes('/teacher/') || path.includes('/student/');
    const prefix = isInSubFolder ? '../' : './';

    switch (targetRole) {
      case 'admin':
        window.location.href = `${prefix}admin/index.html#/dashboard`;
        break;
      case 'teacher':
        window.location.href = `${prefix}teacher/index.html#/dashboard`;
        break;
      case 'student':
        window.location.href = `${prefix}student/index.html#/dashboard`;
        break;
      default:
        window.location.href = `${prefix}index.html`;
    }
  }

  /**
   * Route Guard: Kiểm tra quyền truy cập của trang
   * @param {string|string[]} allowedRoles 
   * @returns {boolean}
   */
  requireAuth(allowedRoles = []) {
    if (!this.isAuthenticated()) {
      const isInSubFolder = window.location.pathname.includes('/admin/') || 
                            window.location.pathname.includes('/teacher/') || 
                            window.location.pathname.includes('/student/');
      window.location.href = isInSubFolder ? '../index.html' : './index.html';
      return false;
    }

    if (allowedRoles.length > 0 && !this.hasRole(allowedRoles)) {
      alert('Bạn không có quyền truy cập vào khu vực này!');
      this.redirectToPortal();
      return false;
    }

    return true;
  }
}

export const auth = new AuthService();
