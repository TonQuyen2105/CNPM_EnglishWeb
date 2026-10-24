/**
 * StudentProfileView - Giao diện Hồ sơ cá nhân của Học viên
 */

import { icon } from '../../ui/icons.js';
import { formatDate } from '../../core/utils.js';

export class StudentProfileView {
  static render({
    currentUser = {},
    stats = {}
  }) {
    return `
      <div class="student-profile-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Hồ sơ cá nhân</h1>
            <p class="page-header-subtitle">
              Quản lý thông tin tài khoản, ảnh đại diện và bảo mật đăng nhập.
            </p>
          </div>
        </div>

        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (4 cột): Card Tóm tắt & Avatar cá nhân -->
          <div class="col-span-4 flex flex-col gap-6">
            <div class="card" style="border-radius: var(--radius-xl); padding: 2rem; border: 1px solid var(--border-color); text-align: center;">
              <div style="position: relative; width: 100px; height: 100px; margin: 0 auto 1rem;">
                <div class="avatar avatar-xl" style="width: 100px; height: 100px; border-radius: 50%; background: linear-gradient(135deg, #2563EB, #10B981); color: white; font-size: 2.25rem; font-weight: 800; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 15px rgba(37,99,235,0.25); overflow: hidden;" id="profile-avatar-preview">
                  ${currentUser.avatar_url 
                    ? `<img src="${currentUser.avatar_url}" alt="${currentUser.full_name}" style="width: 100%; height: 100%; object-fit: cover;">` 
                    : (currentUser.full_name ? currentUser.full_name.charAt(0) : 'H')}
                </div>
              </div>

              <h3 style="font-size: 1.25rem; font-weight: 800; color: var(--text-primary); margin-bottom: 4px;" id="profile-name-display">
                ${currentUser.full_name}
              </h3>
              <p style="font-size: 0.8125rem; color: var(--text-muted); margin-bottom: 1rem;">
                ${currentUser.email}
              </p>
              <span class="badge badge-success" style="padding: 4px 12px; font-size: 0.75rem;">
                Học viên chính thức
              </span>

              <!-- Tóm tắt thống kê học tập -->
              <div class="grid grid-cols-2 gap-3" style="margin-top: 1.5rem; padding-top: 1.25rem; border-top: 1px solid var(--border-color); text-align: left;">
                <div style="background: var(--neutral-50); padding: 10px; border-radius: var(--radius-md);">
                  <div style="font-size: 0.7rem; color: var(--text-muted); text-transform: uppercase;">Lớp đang học</div>
                  <div style="font-size: 1.125rem; font-weight: 800; color: var(--primary-700);">${stats.activeClasses || 0}</div>
                </div>
                <div style="background: var(--neutral-50); padding: 10px; border-radius: var(--radius-md);">
                  <div style="font-size: 0.7rem; color: var(--text-muted); text-transform: uppercase;">Bài đã xong</div>
                  <div style="font-size: 1.125rem; font-weight: 800; color: #16A34A;">${stats.completedLessons || 0}</div>
                </div>
                <div style="background: var(--neutral-50); padding: 10px; border-radius: var(--radius-md);">
                  <div style="font-size: 0.7rem; color: var(--text-muted); text-transform: uppercase;">Từ vựng SRS</div>
                  <div style="font-size: 1.125rem; font-weight: 800; color: #D97706;">${stats.totalFlashcards || 0}</div>
                </div>
                <div style="background: var(--neutral-50); padding: 10px; border-radius: var(--radius-md);">
                  <div style="font-size: 0.7rem; color: var(--text-muted); text-transform: uppercase;">Điểm cao nhất</div>
                  <div style="font-size: 1.125rem; font-weight: 800; color: var(--primary-700);">${stats.bestScore || 0}đ</div>
                </div>
              </div>

              <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 1.25rem;">
                Thành viên từ: ${formatDate(currentUser.created_at)}
              </div>
            </div>
          </div>

          <!-- Cột Phải (8 cột): Form Sửa Thông Tin & Đổi Mật Khẩu -->
          <div class="col-span-8 flex flex-col gap-6">
            
            <!-- Card 1: Chỉnh sửa thông tin cá nhân -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.75rem; border: 1px solid var(--border-color);">
              <div class="flex items-center gap-2" style="margin-bottom: 1.25rem;">
                <span style="color: var(--primary-600);">${icon('user', { size: 22 })}</span>
                <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Thông tin tài khoản</h3>
              </div>

              <form id="form-update-profile">
                <div class="form-group" style="margin-bottom: 1.25rem;">
                  <label class="form-label" style="font-weight: 600;">Địa chỉ Email (Chỉ đọc)</label>
                  <input type="email" class="form-control" value="${currentUser.email}" disabled style="background: var(--neutral-100); cursor: not-allowed;">
                  <small style="color: var(--text-muted); font-size: 0.75rem;">Email định danh tài khoản không thể tự ý thay đổi.</small>
                </div>

                <div class="form-group" style="margin-bottom: 1.25rem;">
                  <label class="form-label" style="font-weight: 600;">Họ và tên (*)</label>
                  <input type="text" id="profile-fullname-input" class="form-control" required value="${currentUser.full_name}">
                </div>

                <div class="form-group" style="margin-bottom: 1.5rem;">
                  <label class="form-label" style="font-weight: 600;">Đường dẫn Avatar (URL)</label>
                  <input type="url" id="profile-avatar-input" class="form-control" placeholder="https://..." value="${currentUser.avatar_url || ''}">
                  <small style="color: var(--text-muted); font-size: 0.75rem;">Dán liên kết ảnh đại diện trực tiếp để làm mới hình ảnh đại diện của bạn.</small>
                </div>

                <div style="text-align: right;">
                  <button type="submit" class="btn btn-primary" style="font-weight: 700; padding: 10px 24px;">
                    Lưu thông tin hồ sơ
                  </button>
                </div>
              </form>
            </div>

            <!-- Card 2: Đổi mật khẩu bảo mật -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.75rem; border: 1px solid var(--border-color);">
              <div class="flex items-center gap-2" style="margin-bottom: 1.25rem;">
                <span style="color: var(--danger-main);">${icon('lock', { size: 22 })}</span>
                <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Bảo mật & Đổi mật khẩu</h3>
              </div>

              <form id="form-change-password">
                <div class="form-group" style="margin-bottom: 1.25rem;">
                  <label class="form-label" style="font-weight: 600;">Mật khẩu hiện tại (*)</label>
                  <input type="password" id="input-current-password" class="form-control" required placeholder="Nhập mật khẩu hiện tại...">
                </div>

                <div class="grid grid-cols-2 gap-4" style="margin-bottom: 1.5rem;">
                  <div class="form-group">
                    <label class="form-label" style="font-weight: 600;">Mật khẩu mới (*)</label>
                    <input type="password" id="input-new-password" class="form-control" required minlength="6" placeholder="Ít nhất 6 ký tự...">
                  </div>
                  <div class="form-group">
                    <label class="form-label" style="font-weight: 600;">Xác nhận mật khẩu mới (*)</label>
                    <input type="password" id="input-confirm-password" class="form-control" required minlength="6" placeholder="Nhập lại mật khẩu mới...">
                  </div>
                </div>

                <div style="text-align: right;">
                  <button type="submit" class="btn btn-warning" style="font-weight: 700; padding: 10px 24px;">
                    Cập nhật mật khẩu
                  </button>
                </div>
              </form>
            </div>

          </div>

        </div>
      </div>
    `;
  }
}
