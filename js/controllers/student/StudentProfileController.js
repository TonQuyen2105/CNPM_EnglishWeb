/**
 * StudentProfileController - Điều khiển màn hình Hồ sơ học sinh
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  UserModel,
  EnrollmentModel,
  UserProgressModel,
  FlashcardModel,
  UserExamModel
} from '../../models/index.js';
import { StudentProfileView } from '../../views/student/StudentProfileView.js';
import { toast } from '../../ui/toast.js';

export class StudentProfileController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    // Lấy số liệu thống kê
    const myEnrollments = await EnrollmentModel.getByUser(currentUser.id);
    const activeClasses = myEnrollments.filter(e => e.status === 'active').length;

    const myProgress = await UserProgressModel.getByUser(currentUser.id);
    const completedLessons = myProgress.filter(p => p.is_completed === 1 || p.is_completed === true).length;

    const myFlashcards = await FlashcardModel.getByUser(currentUser.id);

    const myAttempts = await UserExamModel.getByUser(currentUser.id);
    const bestScore = myAttempts.length > 0
      ? Math.max(...myAttempts.map(a => Number(a.total_score) || 0))
      : 0;

    container.innerHTML = StudentProfileView.render({
      currentUser,
      stats: {
        activeClasses,
        completedLessons,
        totalFlashcards: myFlashcards.length,
        bestScore
      }
    });

    this._bindEvents(container, currentUser);
  }

  static _bindEvents(container, currentUser) {
    // 1. Live avatar preview
    const avatarInput = container.querySelector('#profile-avatar-input');
    const avatarPreview = container.querySelector('#profile-avatar-preview');
    if (avatarInput && avatarPreview) {
      avatarInput.addEventListener('input', (e) => {
        const url = e.target.value.trim();
        if (url) {
          avatarPreview.innerHTML = `<img src="${url}" alt="Preview" style="width: 100%; height: 100%; object-fit: cover;">`;
        } else {
          avatarPreview.innerHTML = currentUser.full_name ? currentUser.full_name.charAt(0) : 'H';
        }
      });
    }

    // 2. Cập nhật thông tin cá nhân
    const profileForm = container.querySelector('#form-update-profile');
    if (profileForm) {
      profileForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fullName = container.querySelector('#profile-fullname-input').value.trim();
        const avatarUrl = container.querySelector('#profile-avatar-input').value.trim();

        if (!fullName) {
          toast.error('Vui lòng nhập họ và tên.');
          return;
        }

        const updated = await UserModel.update(currentUser.id, {
          full_name: fullName,
          avatar_url: avatarUrl,
          updated_at: new Date().toISOString()
        });

        // Cập nhật session user
        const newSessionUser = {
          ...currentUser,
          full_name: fullName,
          avatar_url: avatarUrl
        };
        storage.set('session_user', newSessionUser);

        // Cập nhật lại UI topbar
        const navName = document.querySelector('#student-avatar-btn .d-none div');
        if (navName) navName.textContent = fullName;

        toast.success('Đã cập nhật thông tin hồ sơ cá nhân thành công!');
        this.render(container);
      });
    }

    // 3. Đổi mật khẩu
    const passwordForm = container.querySelector('#form-change-password');
    if (passwordForm) {
      passwordForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const currentPass = container.querySelector('#input-current-password').value;
        const newPass = container.querySelector('#input-new-password').value;
        const confirmPass = container.querySelector('#input-confirm-password').value;

        // Lấy thông tin user mới nhất từ DB
        const dbUser = await UserModel.getById(currentUser.id);
        if (!dbUser || dbUser.password !== currentPass) {
          toast.error('Mật khẩu hiện tại không chính xác!');
          return;
        }

        if (newPass.length < 6) {
          toast.error('Mật khẩu mới phải có ít nhất 6 ký tự.');
          return;
        }

        if (newPass !== confirmPass) {
          toast.error('Xác nhận mật khẩu mới không khớp!');
          return;
        }

        await UserModel.update(currentUser.id, {
          password: newPass,
          updated_at: new Date().toISOString()
        });

        toast.success('Đổi mật khẩu thành công! Hãy ghi nhớ mật khẩu mới của bạn.');
        passwordForm.reset();
      });
    }
  }
}
