/**
 * StudentExamsController - Điều khiển Màn hình Luyện thi & Làm bài thi
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  ExamModel,
  UserExamModel,
  EnrollmentModel
} from '../../models/index.js';
import { StudentExamsView } from '../../views/student/StudentExamsView.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';

export class StudentExamsController {
  static _timerInterval = null;

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    // Dọn dẹp timer cũ nếu có
    if (this._timerInterval) {
      clearInterval(this._timerInterval);
      this._timerInterval = null;
    }

    if (params && params.id) {
      return this._renderTakeExam(container, params.id);
    }

    return this._renderList(container);
  }

  static async _renderList(container) {
    const currentUser = auth.getCurrentUser();
    const myEnrollments = await EnrollmentModel.getByUser(currentUser.id);
    const activeCourseIds = myEnrollments.map(e => e.course_id || (e.class ? e.class.course_id : null)).filter(Boolean);

    const allExams = await ExamModel.getAll();
    const publishedExams = allExams.filter(e => 
      (e.is_published === 1 || e.is_published === true) && 
      (activeCourseIds.length === 0 || activeCourseIds.includes(e.course_id) || !e.course_id)
    );

    const myAttempts = await UserExamModel.getByUser(currentUser.id);

    container.innerHTML = StudentExamsView.renderExamList({
      exams: publishedExams,
      myAttempts
    });
  }

  static async _renderTakeExam(container, examId) {
    const currentUser = auth.getCurrentUser();
    const examWithQuestions = await ExamModel.getWithQuestions(Number(examId));

    if (!examWithQuestions) {
      container.innerHTML = `
        <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
          <h3>Không tìm thấy đề thi yêu cầu</h3>
          <a href="#/exams" class="btn btn-primary" style="margin-top: 1rem;">Quay lại danh sách đề thi</a>
        </div>
      `;
      return;
    }

    // Khôi phục bài làm nháp từ localStorage nếu có
    const draftKey = `exam_draft_${examId}_${currentUser.id}`;
    let draftAnswers = {};
    try {
      const saved = localStorage.getItem(draftKey);
      if (saved) draftAnswers = JSON.parse(saved);
    } catch (e) {
      console.warn('Lỗi đọc bản nháp đề thi:', e);
    }

    container.innerHTML = StudentExamsView.renderTakeExam({
      exam: examWithQuestions,
      questions: examWithQuestions.questions || [],
      draftAnswers
    });

    this._bindTakeExamEvents(container, currentUser, examWithQuestions, draftKey);
  }

  static _bindTakeExamEvents(container, currentUser, exam, draftKey) {
    const questions = exam.questions || [];
    let draftAnswers = {};
    try {
      const saved = localStorage.getItem(draftKey);
      if (saved) draftAnswers = JSON.parse(saved);
    } catch (e) {}

    // 1. Đồng hồ đếm ngược
    const durationMinutes = Number(exam.duration_minutes) || 30;
    let secondsLeft = durationMinutes * 60;

    const timerPill = container.querySelector('#exam-timer-pill');
    const timerText = container.querySelector('#exam-timer-text');

    const updateTimerDisplay = () => {
      const m = Math.floor(secondsLeft / 60);
      const s = secondsLeft % 60;
      if (timerText) {
        timerText.textContent = `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
      }

      // Đổi sang đỏ khi <= 5 phút (300 giây)
      if (secondsLeft <= 300 && timerPill) {
        timerPill.classList.add('urgent');
      }
    };

    updateTimerDisplay();

    this._timerInterval = setInterval(async () => {
      secondsLeft--;
      if (secondsLeft <= 0) {
        clearInterval(this._timerInterval);
        this._timerInterval = null;
        toast.warning('Hết giờ làm bài! Hệ thống đang tự động nộp bài thi của bạn...');
        await this._submitExam(container, currentUser, exam, draftKey, true);
      } else {
        updateTimerDisplay();
      }
    }, 1000);

    // 2. Lắng nghe thay đổi đáp án để tự lưu nháp và cập nhật Palette
    const inputs = container.querySelectorAll('.exam-ans-input');
    inputs.forEach(input => {
      input.addEventListener('change', (e) => {
        const name = e.target.name; // exam_ans_12
        const qId = name.replace('exam_ans_', '');
        const val = e.target.value.trim();

        if (val) {
          draftAnswers[qId] = val;
        } else {
          delete draftAnswers[qId];
        }

        try {
          localStorage.setItem(draftKey, JSON.stringify(draftAnswers));
        } catch (err) {}

        // Cập nhật nút palette
        const paletteBtn = container.querySelector(`.exam-palette-btn[data-qid="${qId}"]`);
        if (paletteBtn) {
          if (val) {
            paletteBtn.classList.add('answered');
          } else {
            paletteBtn.classList.remove('answered');
          }
        }
      });
    });

    // 3. Nhấp nút Palette để cuộn nhanh đến câu hỏi
    container.querySelectorAll('.exam-palette-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const idx = e.currentTarget.getAttribute('data-target-index');
        const box = container.querySelector(`#question-box-${idx}`);
        if (box) {
          box.scrollIntoView({ behavior: 'smooth', block: 'center' });
          box.style.transition = 'box-shadow 0.3s';
          box.style.boxShadow = '0 0 0 3px rgba(37,99,235,0.4)';
          setTimeout(() => {
            box.style.boxShadow = '';
          }, 1200);
        }
      });
    });

    // 4. Nút nộp bài thi
    const submitBtn = container.querySelector('#btn-submit-exam');
    if (submitBtn) {
      submitBtn.addEventListener('click', async () => {
        const answeredCount = Object.keys(draftAnswers).filter(k => draftAnswers[k]).length;
        const unansweredCount = questions.length - answeredCount;

        let confirmMsg = 'Bạn có chắc chắn muốn nộp bài thi này?';
        if (unansweredCount > 0) {
          confirmMsg = `Bạn vẫn còn ${unansweredCount} câu hỏi chưa trả lời. Bạn có chắc chắn muốn nộp bài ngay bây giờ?`;
        }

        const confirmed = await confirmDialog({
          title: 'Xác nhận nộp bài thi',
          message: confirmMsg,
          confirmText: 'Nộp bài ngay',
          cancelText: 'Tiếp tục làm bài',
          type: unansweredCount > 0 ? 'warning' : 'info'
        });

        if (confirmed) {
          await this._submitExam(container, currentUser, exam, draftKey, false);
        }
      });
    }
  }

  static async _submitExam(container, currentUser, exam, draftKey, isTimeout = false) {
    if (this._timerInterval) {
      clearInterval(this._timerInterval);
      this._timerInterval = null;
    }

    let draftAnswers = {};
    try {
      const saved = localStorage.getItem(draftKey);
      if (saved) draftAnswers = JSON.parse(saved);
    } catch (e) {}

    const questions = exam.questions || [];
    const formattedAnswers = questions.map(q => {
      const val = draftAnswers[q.id] || null;
      if (q.question_type === 'multiple_choice') {
        return {
          question_id: q.id,
          selected_option_id: val ? Number(val) : null,
          text_answer: null
        };
      } else {
        return {
          question_id: q.id,
          selected_option_id: null,
          text_answer: val || ''
        };
      }
    });

    try {
      const result = await UserExamModel.submitExamAttempt({
        userId: currentUser.id,
        examId: exam.id,
        answers: formattedAnswers,
        startedAt: new Date(Date.now() - (exam.duration_minutes || 30) * 60000).toISOString()
      });

      // Xóa bản nháp
      try {
        localStorage.removeItem(draftKey);
      } catch (err) {}

      toast.success(isTimeout ? 'Hết giờ: Bài thi đã được tự động nộp thành công!' : 'Đã nộp bài thi thành công!');
      
      // Chuyển tới màn kết quả thi
      if (result && result.user_exam_id) {
        window.location.hash = `#/results/${result.user_exam_id}`;
      } else {
        window.location.hash = '#/results';
      }
    } catch (err) {
      toast.error('Có lỗi xảy ra khi nộp bài: ' + err.message);
    }
  }
}
