/**
 * TeacherProfileView - Giao diện Hồ sơ Giảng viên & Đổi mật khẩu
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, getInitials } from '../../core/utils.js';

export class TeacherProfileView {
  static render(teacher) {
    const initials = getInitials(teacher.full_name || 'Teacher');
    const isVerified = teacher.is_verified === 1 || teacher.is_verified === true;

    return `
      <div class="teacher-profile">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
            Hồ Sơ Giảng Viên
          </h1>
          <p class="page-header-subtitle" style="margin: 4px 0 0 0; color: var(--text-secondary); font-size: 0.875rem;">
            Cập nhật trình độ học vấn, kinh nghiệm giảng dạy và bảo mật tài khoản
          </p>
        </div>

        <div class="grid grid-cols-3 gap-6" style="align-items: flex-start;">
          
          <!-- Cột Trái: Avatar Card & Verification Badge (1/3) -->
          <div style="display: flex; flex-direction: column; gap: 1.25rem;">
            <!-- Profile Summary Card -->
            <div class="card" style="padding: 1.5rem; text-align: center;">
              <div style="position: relative; width: 90px; height: 90px; margin: 0 auto 1rem;">
                <div class="avatar" id="profile-avatar-preview" style="width: 90px; height: 90px; font-size: 1.75rem; border: 3px solid var(--primary-100);">
                  ${teacher.avatar_url 
                    ? `<img src="${teacher.avatar_url}" alt="${escapeHtml(teacher.full_name)}" style="width: 100%; height: 100%; object-fit: cover;">` 
                    : initials}
                </div>
              </div>

              <h3 style="margin: 0 0 4px 0; font-size: 1.125rem; font-weight: 800; color: var(--text-primary);">
                ${escapeHtml(teacher.full_name)}
              </h3>
              <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 0.75rem;">
                ${escapeHtml(teacher.qualification || 'Giảng viên')}
              </div>

              <!-- Trạng thái xác minh (Read-only Badge) -->
              <div style="background: ${isVerified ? '#F0FDF4' : '#FEFCE8'}; border: 1px solid ${isVerified ? '#86EFAC' : '#FDE047'}; border-radius: var(--radius-md); padding: 0.75rem; text-align: left;">
                <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 4px;">
                  <span style="font-size: 0.75rem; font-weight: 700; color: ${isVerified ? '#15803D' : '#A16207'};">
                    Trạng thái hồ sơ:
                  </span>
                  <span class="badge ${isVerified ? 'badge-success' : 'badge-warning'}" style="font-size: 0.7rem; font-weight: 700;">
                    ${isVerified ? '✓ Đã xác minh' : '⏳ Chờ xác minh'}
                  </span>
                </div>
                <div style="font-size: 0.75rem; color: var(--text-muted); line-height: 1.4;">
                  ${isVerified 
                    ? 'Bằng cấp và hồ sơ sư phạm đã được Ban Quản trị phê duyệt chính thức.' 
                    : 'Hồ sơ đang chờ Ban Quản trị trung tâm kiểm tra tính hợp lệ của văn bằng.'}
                </div>
                <div style="font-size: 0.65rem; color: var(--text-muted); margin-top: 4px; font-style: italic;">
                  * Quyền hạn phê duyệt thuộc về Quản trị viên
                </div>
              </div>
            </div>

            <!-- Teaching Bio Card -->
            <div class="card" style="padding: 1.25rem;">
              <h4 style="margin: 0 0 0.5rem 0; font-size: 0.875rem; font-weight: 700;">Về giảng viên:</h4>
              <p style="margin: 0; font-size: 0.8125rem; line-height: 1.6; color: var(--text-secondary); white-space: pre-wrap;">
                ${escapeHtml(teacher.bio || 'Chưa cập nhật tiểu sử giới thiệu.')}
              </p>
            </div>
          </div>

          <!-- Cột Phải: Form Chỉnh sửa thông tin & Đổi mật khẩu (2/3) -->
          <div style="grid-column: span 2; display: flex; flex-direction: column; gap: 1.5rem;">
            
            <!-- Box 1: Thông tin cá nhân & Chuyên môn -->
            <div class="card" style="padding: 1.5rem;">
              <h3 style="margin: 0 0 1.25rem 0; font-size: 1.0625rem; font-weight: 800; color: var(--text-primary); border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
                Thông Tin Chuyên Môn & Hồ Sơ
              </h3>

              <form id="form-update-profile" style="display: flex; flex-direction: column; gap: 1rem;">
                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="form-label">Họ và tên: <span class="text-danger">*</span></label>
                    <input type="text" id="teacher-name-input" class="form-control" required value="${escapeHtml(teacher.full_name)}">
                  </div>

                  <div>
                    <label class="form-label">Email tài khoản:</label>
                    <input type="email" class="form-control" disabled value="${escapeHtml(teacher.email)}" style="background-color: var(--neutral-100); cursor: not-allowed;" title="Email tài khoản không thể chỉnh sửa">
                  </div>
                </div>

                <div>
                  <label class="form-label">URL Ảnh đại diện (Avatar):</label>
                  <input type="url" id="teacher-avatar-input" class="form-control" value="${escapeHtml(teacher.avatar_url || '')}" placeholder="https://example.com/avatar.jpg">
                </div>

                <div>
                  <label class="form-label">Trình độ & Bằng cấp (Qualification): <span class="text-danger">*</span></label>
                  <input type="text" id="teacher-qual-input" class="form-control" required value="${escapeHtml(teacher.qualification || '')}" placeholder="VD: ThS. Ngôn ngữ Anh ứng dụng, IELTS 8.5, TESOL Certificate">
                </div>

                <div>
                  <label class="form-label">Tiểu sử & Kinh nghiệm giảng dạy (Bio):</label>
                  <textarea id="teacher-bio-input" class="form-control" rows="4" placeholder="Giới thiệu phương pháp giảng dạy, phong cách truyền cảm hứng và các thành tích nổi bật...">${escapeHtml(teacher.bio || '')}</textarea>
                </div>

                <div style="text-align: right; margin-top: 0.5rem;">
                  <button type="submit" class="btn btn-primary" style="font-weight: 700; padding: 8px 20px;">
                    <span class="btn-icon-left">${icon('check', { size: 16 })}</span> Lưu thay đổi hồ sơ
                  </button>
                </div>
              </form>
            </div>

            <!-- Box 2: Đổi mật khẩu -->
            <div class="card" style="padding: 1.5rem;">
              <h3 style="margin: 0 0 1.25rem 0; font-size: 1.0625rem; font-weight: 800; color: var(--text-primary); border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem; display: flex; align-items: center; gap: 8px;">
                <span>${icon('lock', { size: 18 })}</span>
                Bảo Mật & Đổi Mật Khẩu
              </h3>

              <form id="form-change-password" style="display: flex; flex-direction: column; gap: 1rem;">
                <div class="grid grid-cols-3 gap-4">
                  <div>
                    <label class="form-label">Mật khẩu hiện tại: <span class="text-danger">*</span></label>
                    <input type="password" id="current-pwd-input" class="form-control" required placeholder="Nhập mật khẩu cũ">
                  </div>

                  <div>
                    <label class="form-label">Mật khẩu mới: <span class="text-danger">*</span></label>
                    <input type="password" id="new-pwd-input" class="form-control" required minlength="6" placeholder="Tối thiểu 6 ký tự">
                  </div>

                  <div>
                    <label class="form-label">Xác nhận mật khẩu: <span class="text-danger">*</span></label>
                    <input type="password" id="confirm-pwd-input" class="form-control" required minlength="6" placeholder="Nhập lại mật khẩu mới">
                  </div>
                </div>

                <div style="text-align: right; margin-top: 0.5rem;">
                  <button type="submit" class="btn btn-outline" style="font-weight: 700;">
                    Đổi mật khẩu
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
