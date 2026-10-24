/**
 * TeacherGradingController - Điều phối Hàng đợi & Quy trình Chấm bài tự luận
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { UserExamModel } from '../../models/UserExamModel.js';
import { TeacherGradingView } from '../../views/teacher/TeacherGradingView.js';
import { toast } from '../../ui/toast.js';
import { eventBus } from '../../core/eventBus.js';

export class TeacherGradingController {
  static activeAnswerId = null;

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});
    const query = (arg2 && typeof arg2 === 'object' && !arg2.nodeType) ? arg2 : {};

    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) return;

    // Kiểm tra query param nếu có
    if (query && query.id) {
      TeacherGradingController.activeAnswerId = Number(query.id);
    } else if (params && params.id) {
      TeacherGradingController.activeAnswerId = Number(params.id);
    }

    await TeacherGradingController.updateView(container, teacher);
  }

  static async updateView(container, teacher) {
    const pendingList = await UserExamModel.getPendingReviewSubmissions(teacher.id);

    // Cập nhật badge trên sidebar
    TeacherGradingController.updateSidebarBadge(pendingList.length);

    if (TeacherGradingController.activeAnswerId) {
      const currentSub = pendingList.find(s => s.id === TeacherGradingController.activeAnswerId);
      if (currentSub) {
        container.innerHTML = TeacherGradingView.renderGradingScreen({
          submission: currentSub,
          remainingCount: pendingList.length,
          totalInQueue: pendingList.length
        });
        TeacherGradingController.bindGradingEvents(container, teacher, currentSub, pendingList);
        return;
      } else {
        // Nếu bài vừa chấm xong không còn trong queue
        TeacherGradingController.activeAnswerId = null;
      }
    }

    // Mặc định render Hàng đợi
    container.innerHTML = TeacherGradingView.renderQueue(pendingList);
    TeacherGradingController.bindQueueEvents(container, teacher, pendingList);
  }

  static bindQueueEvents(container, teacher, pendingList) {
    // 1. Mở chấm một bài cụ thể
    container.querySelectorAll('.btn-open-grading, .btn-start-first').forEach(btn => {
      btn.addEventListener('click', () => {
        const answerId = Number(btn.getAttribute('data-answer-id'));
        TeacherGradingController.activeAnswerId = answerId;
        TeacherGradingController.updateView(container, teacher);
      });
    });
  }

  static bindGradingEvents(container, teacher, submission, pendingList) {
    // 1. Quay lại hàng đợi
    container.querySelectorAll('.btn-back-to-queue').forEach(btn => {
      btn.addEventListener('click', () => {
        TeacherGradingController.activeAnswerId = null;
        TeacherGradingController.updateView(container, teacher);
      });
    });

    // 2. Gợi ý nhận xét nhanh (Quick Feedback)
    container.querySelectorAll('.btn-quick-feedback').forEach(btn => {
      btn.addEventListener('click', () => {
        const text = btn.getAttribute('data-text');
        const feedbackArea = document.getElementById('grade-feedback-input');
        if (feedbackArea) {
          if (feedbackArea.value.trim()) {
            feedbackArea.value += ' ' + text;
          } else {
            feedbackArea.value = text;
          }
          feedbackArea.focus();
        }
      });
    });

    // 3. Validation điểm số khi gõ
    const scoreInput = document.getElementById('grade-score-input');
    const scoreError = document.getElementById('grade-score-error');
    if (scoreInput) {
      scoreInput.addEventListener('input', () => {
        const val = Number(scoreInput.value);
        if (val > submission.max_points || val < 0) {
          scoreError.style.display = 'block';
        } else {
          scoreError.style.display = 'none';
        }
      });
    }

    // 4. Submit chấm bài
    const form = document.getElementById('form-grading-submit');
    if (form) {
      form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const scoreVal = Number(scoreInput.value);
        if (isNaN(scoreVal) || scoreVal < 0) {
          toast.error('Vui lòng nhập số điểm hợp lệ!');
          return;
        }

        if (scoreVal > submission.max_points) {
          toast.error(`Điểm không được vượt quá tối đa ${submission.max_points} đ!`);
          return;
        }

        const feedbackInput = document.getElementById('grade-feedback-input');
        const feedback = feedbackInput ? feedbackInput.value.trim() : '';

        // Gọi Model chấm bài
        const result = await UserExamModel.gradeSubmission(submission.id, {
          score_earned: scoreVal,
          feedback,
          teacher_id: teacher.id
        });

        if (result && result.userExam) {
          toast.success(`Đã chấm xong! Tổng điểm bài thi của học viên: ${result.userExam.total_score} đ (${result.userExam.status === 'passed' ? 'ĐẠT' : 'CHƯA ĐẠT'})`);
        } else {
          toast.success(`Đã lưu điểm (${scoreVal} đ) cho học viên ${submission.student_name}!`);
        }

        // Tự động tìm bài tiếp theo trong hàng đợi
        const remaining = pendingList.filter(s => s.id !== submission.id);
        if (remaining.length > 0) {
          TeacherGradingController.activeAnswerId = remaining[0].id;
        } else {
          TeacherGradingController.activeAnswerId = null;
          toast.info('🎉 Bạn đã hoàn thành tất cả các bài thi trong hàng đợi!');
        }

        TeacherGradingController.updateView(container, teacher);
      });
    }
  }

  static updateSidebarBadge(count) {
    const badge = document.getElementById('sidebar-pending-badge');
    if (badge) {
      if (count > 0) {
        badge.textContent = count;
        badge.style.display = 'inline-flex';
      } else {
        badge.style.display = 'none';
      }
    }
  }
}
