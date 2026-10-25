/**
 * TeacherProfileController - Điều phối Hồ sơ Giảng viên & Đổi mật khẩu
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { UserModel } from '../../models/UserModel.js';
import { TeacherProfileView } from '../../views/teacher/TeacherProfileView.js';
import { toast } from '../../ui/toast.js';

export class TeacherProfileController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) {
      container.innerHTML = `
        <div class="empty-state">
          <h3>Không tìm thấy hồ sơ giảng viên</h3>
        </div>
      `;
      return;
    }

    container.innerHTML = TeacherProfileView.render(teacher);
    TeacherProfileController.bindEvents(container, teacher);
  }

  static bindEvents(container, teacher) {
    // 1. Cập nhật hồ sơ thông tin giảng viên
    const profileForm = container.querySelector('#form-update-profile');
    if (profileForm) {
      profileForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const nameInput = document.getElementById('teacher-name-input');
        const avatarInput = document.getElementById('teacher-avatar-input');
        const qualInput = document.getElementById('teacher-qual-input');
        const bioInput = document.getElementById('teacher-bio-input');

        if (!nameInput.value.trim() || !qualInput.value.trim()) {
          toast.error('Họ tên và trình độ bằng cấp không được để trống!');
          return;
        }

        const updated = await TeacherModel.updateProfile(teacher.id, {
          full_name: nameInput.value.trim(),
          avatar_url: avatarInput.value.trim(),
          qualification: qualInput.value.trim(),
          bio: bioInput.value.trim()
        });

        if (updated) {
          toast.success('Đã cập nhật hồ sơ giảng viên thành công!');

          // Cập nhật tên và avatar trên Topbar & Sidebar ngay lập tức
          const sidebarName = document.getElementById('sidebar-user-name');
          if (sidebarName) sidebarName.textContent = updated.full_name;

          const topbarName = document.querySelector('.user-profile-trigger div div:first-child');
          if (topbarName) topbarName.textContent = updated.full_name;

          TeacherProfileController.render(container);
        }
      });
    }

    // 2. Đổi mật khẩu
    const pwdForm = container.querySelector('#form-change-password');
    if (pwdForm) {
      pwdForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const currentPwd = document.getElementById('current-pwd-input').value;
        const newPwd = document.getElementById('new-pwd-input').value;
        const confirmPwd = document.getElementById('confirm-pwd-input').value;

        // Kiểm tra mật khẩu hiện tại
        const user = await UserModel.authenticate(teacher.email, currentPwd);
        if (!user) {
          toast.error('Mật khẩu hiện tại không chính xác!');
          return;
        }

        if (newPwd.length < 6) {
          toast.error('Mật khẩu mới phải có tối thiểu 6 ký tự!');
          return;
        }

        if (newPwd !== confirmPwd) {
          toast.error('Mật khẩu xác nhận không khớp với mật khẩu mới!');
          return;
        }

        await TeacherModel.updateProfile(teacher.id, { password: newPwd });
        toast.success('Đã đổi mật khẩu thành công! Hãy ghi nhớ mật khẩu mới.');
        pwdForm.reset();
      });
    }
  }
}
