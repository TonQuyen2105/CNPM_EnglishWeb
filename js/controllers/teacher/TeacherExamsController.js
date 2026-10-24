/**
 * TeacherExamsController - Điều phối Đề thi & Trình dựng đề 2 cột
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { ExamModel } from '../../models/ExamModel.js';
import { CourseModel } from '../../models/CourseModel.js';
import { QuestionModel } from '../../models/QuestionModel.js';
import { LessonModel } from '../../models/LessonModel.js';
import { TeacherExamsView } from '../../views/teacher/TeacherExamsView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';

export class TeacherExamsController {
  static activeExamBuilderId = null;
  static builderFilterType = 'all';
  static builderSearchQuery = '';
  static builderSelectedQuestions = [];

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) return;

    // Kiểm tra nếu đang ở chế độ Builder
    if (TeacherExamsController.activeExamBuilderId) {
      await TeacherExamsController.renderBuilder(container, teacher, TeacherExamsController.activeExamBuilderId);
    } else {
      await TeacherExamsController.renderList(container, teacher);
    }
  }

  /**
   * Render danh sách đề thi
   */
  static async renderList(container, teacher) {
    const exams = await ExamModel.getByTeacher(teacher.id);
    const courses = await CourseModel.getAll();

    container.innerHTML = TeacherExamsView.renderList(exams, courses);
    TeacherExamsController.bindListEvents(container, teacher, courses);
  }

  static bindListEvents(container, teacher, courses) {
    // 1. Mở Trình Dựng Đề Thi (Builder)
    container.querySelectorAll('.btn-open-builder').forEach(btn => {
      btn.addEventListener('click', async () => {
        const examId = Number(btn.getAttribute('data-exam-id'));
        TeacherExamsController.activeExamBuilderId = examId;
        TeacherExamsController.builderFilterType = 'all';
        TeacherExamsController.builderSearchQuery = '';

        // Tải các câu hỏi đã có sẵn trong đề
        const detailedExam = await ExamModel.getWithQuestions(examId);
        TeacherExamsController.builderSelectedQuestions = detailedExam ? detailedExam.questions.map((q, idx) => ({
          question_id: q.id,
          question_text: q.question_text,
          question_type: q.question_type,
          points: q.points || q.default_points || 10,
          order_index: idx + 1
        })) : [];

        TeacherExamsController.renderBuilder(container, teacher, examId);
      });
    });

    // 2. Tạo đề thi mới
    container.querySelectorAll('.btn-create-exam').forEach(btn => {
      btn.addEventListener('click', () => {
        modal.open({
          title: 'Tạo đề kiểm tra / bài thi mới',
          content: TeacherExamsView.renderExamForm(null, courses),
          size: 'medium',
          confirmText: 'Tạo đề & Mở Trình dựng',
          cancelText: 'Hủy',
          onConfirm: async () => {
            const titleInput = document.getElementById('exam-title-input');
            const descInput = document.getElementById('exam-desc-input');
            const courseInput = document.getElementById('exam-course-input');
            const durationInput = document.getElementById('exam-duration-input');
            const passInput = document.getElementById('exam-pass-input');
            const attemptsInput = document.getElementById('exam-attempts-input');
            const publishInput = document.getElementById('exam-publish-input');

            if (!titleInput || !titleInput.value.trim()) {
              toast.error('Vui lòng nhập tên đề thi!');
              return false;
            }

            const newExam = await ExamModel.create({
              course_id: Number(courseInput.value),
              created_by_teacher_id: Number(teacher.id),
              title: titleInput.value.trim(),
              description: descInput ? descInput.value.trim() : '',
              duration_minutes: Number(durationInput.value) || 45,
              pass_score: Number(passInput.value) || 50,
              max_attempts: attemptsInput.value ? Number(attemptsInput.value) : null,
              is_published: publishInput.checked ? 1 : 0
            });

            toast.success('Đã tạo đề thi thành công! Đang mở trình thiết kế câu hỏi...');
            
            // Mở thẳng trình dựng đề cho đề vừa tạo
            TeacherExamsController.activeExamBuilderId = newExam.id;
            TeacherExamsController.builderSelectedQuestions = [];
            TeacherExamsController.renderBuilder(container, teacher, newExam.id);
          }
        });
      });
    });

    // 3. Sửa thông tin đề
    container.querySelectorAll('.btn-edit-exam').forEach(btn => {
      btn.addEventListener('click', async () => {
        const examId = btn.getAttribute('data-exam-id');
        const exam = await ExamModel.getById(examId);
        if (!exam) return;

        modal.open({
          title: `Chỉnh sửa đề thi #${exam.id}`,
          content: TeacherExamsView.renderExamForm(exam, courses),
          size: 'medium',
          confirmText: 'Cập nhật',
          cancelText: 'Hủy',
          onConfirm: async () => {
            const titleInput = document.getElementById('exam-title-input');
            const descInput = document.getElementById('exam-desc-input');
            const courseInput = document.getElementById('exam-course-input');
            const durationInput = document.getElementById('exam-duration-input');
            const passInput = document.getElementById('exam-pass-input');
            const attemptsInput = document.getElementById('exam-attempts-input');
            const publishInput = document.getElementById('exam-publish-input');

            if (!titleInput || !titleInput.value.trim()) {
              toast.error('Vui lòng nhập tên đề thi!');
              return false;
            }

            await ExamModel.update(exam.id, {
              course_id: Number(courseInput.value),
              title: titleInput.value.trim(),
              description: descInput ? descInput.value.trim() : '',
              duration_minutes: Number(durationInput.value) || 45,
              pass_score: Number(passInput.value) || 50,
              max_attempts: attemptsInput.value ? Number(attemptsInput.value) : null,
              is_published: publishInput.checked ? 1 : 0
            });

            toast.success('Đã cập nhật thông tin đề thi!');
            TeacherExamsController.renderList(container, teacher);
          }
        });
      });
    });

    // 4. Xóa đề thi
    container.querySelectorAll('.btn-delete-exam').forEach(btn => {
      btn.addEventListener('click', async () => {
        const examId = btn.getAttribute('data-exam-id');
        const ok = await confirmDialog({
          title: 'Xóa đề thi',
          message: 'Bạn có chắc chắn muốn xóa đề thi này? Toàn bộ cấu trúc câu hỏi của đề sẽ bị xóa.',
          confirmText: 'Xóa vĩnh viễn',
          cancelText: 'Hủy'
        });

        if (ok) {
          await ExamModel.delete(examId);
          toast.success('Đã xóa đề thi thành công!');
          TeacherExamsController.renderList(container, teacher);
        }
      });
    });

    // 5. Toggle xuất bản
    container.querySelectorAll('.toggle-exam-publish').forEach(input => {
      input.addEventListener('change', async () => {
        const examId = input.getAttribute('data-exam-id');
        const isPub = input.checked ? 1 : 0;
        await ExamModel.update(examId, { is_published: isPub });
        toast.success(`Đã chuyển đề thi sang trạng thái ${isPub ? '"Đang mở"' : '"Bản nháp"'}`);
      });
    });
  }

  /**
   * Render Trình Dựng Đề Thi 2 Cột
   */
  static async renderBuilder(container, teacher, examId) {
    const exam = await ExamModel.getById(examId);
    if (!exam) {
      TeacherExamsController.activeExamBuilderId = null;
      TeacherExamsController.renderList(container, teacher);
      return;
    }

    const allQuestions = await QuestionModel.getAll();
    const lessons = await LessonModel.getAll();

    // Map tên bài học cho ngân hàng
    let availableQuestions = allQuestions.map(q => {
      const lesson = lessons.find(l => l.id === q.lesson_id);
      return {
        ...q,
        lesson_title: lesson ? `Bài ${lesson.order_index}: ${lesson.title}` : ''
      };
    });

    // Lọc theo search & type
    if (TeacherExamsController.builderFilterType !== 'all') {
      availableQuestions = availableQuestions.filter(q => q.question_type === TeacherExamsController.builderFilterType);
    }
    if (TeacherExamsController.builderSearchQuery.trim()) {
      const qLower = TeacherExamsController.builderSearchQuery.trim().toLowerCase();
      availableQuestions = availableQuestions.filter(q => q.question_text && q.question_text.toLowerCase().includes(qLower));
    }

    container.innerHTML = TeacherExamsView.renderBuilder({
      exam,
      availableQuestions,
      selectedQuestions: TeacherExamsController.builderSelectedQuestions,
      filterType: TeacherExamsController.builderFilterType,
      searchQuery: TeacherExamsController.builderSearchQuery
    });

    TeacherExamsController.bindBuilderEvents(container, teacher, exam);
  }

  static bindBuilderEvents(container, teacher, exam) {
    // 1. Đóng Builder quay lại danh sách
    const backBtn = container.querySelector('.btn-back-to-exams');
    if (backBtn) {
      backBtn.addEventListener('click', () => {
        TeacherExamsController.activeExamBuilderId = null;
        TeacherExamsController.renderList(container, teacher);
      });
    }

    // 2. Tìm kiếm trong ngân hàng
    const searchInput = container.querySelector('#builder-search');
    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        TeacherExamsController.builderSearchQuery = e.target.value;
        TeacherExamsController.renderBuilder(container, teacher, exam.id);
      });
    }

    // 3. Lọc loại câu hỏi
    const typeSelect = container.querySelector('#builder-type-filter');
    if (typeSelect) {
      typeSelect.addEventListener('change', (e) => {
        TeacherExamsController.builderFilterType = e.target.value;
        TeacherExamsController.renderBuilder(container, teacher, exam.id);
      });
    }

    // 4. Thêm câu hỏi vào đề
    container.querySelectorAll('.btn-add-to-exam').forEach(btn => {
      btn.addEventListener('click', async () => {
        const qId = Number(btn.getAttribute('data-question-id'));
        const q = await QuestionModel.getById(qId);
        if (!q) return;

        TeacherExamsController.builderSelectedQuestions.push({
          question_id: q.id,
          question_text: q.question_text,
          question_type: q.question_type,
          points: q.default_points || 10,
          order_index: TeacherExamsController.builderSelectedQuestions.length + 1
        });

        toast.info(`Đã thêm câu hỏi vào đề thi!`);
        TeacherExamsController.renderBuilder(container, teacher, exam.id);
      });
    });

    // 5. Xóa câu hỏi khỏi đề
    container.querySelectorAll('.btn-remove-from-exam').forEach(btn => {
      btn.addEventListener('click', () => {
        const index = Number(btn.getAttribute('data-index'));
        TeacherExamsController.builderSelectedQuestions.splice(index, 1);
        TeacherExamsController.renderBuilder(container, teacher, exam.id);
      });
    });

    // 6. Di chuyển lên (Move Up)
    container.querySelectorAll('.btn-move-up').forEach(btn => {
      btn.addEventListener('click', () => {
        const index = Number(btn.getAttribute('data-index'));
        if (index > 0) {
          const temp = TeacherExamsController.builderSelectedQuestions[index];
          TeacherExamsController.builderSelectedQuestions[index] = TeacherExamsController.builderSelectedQuestions[index - 1];
          TeacherExamsController.builderSelectedQuestions[index - 1] = temp;
          TeacherExamsController.renderBuilder(container, teacher, exam.id);
        }
      });
    });

    // 7. Di chuyển xuống (Move Down)
    container.querySelectorAll('.btn-move-down').forEach(btn => {
      btn.addEventListener('click', () => {
        const index = Number(btn.getAttribute('data-index'));
        if (index < TeacherExamsController.builderSelectedQuestions.length - 1) {
          const temp = TeacherExamsController.builderSelectedQuestions[index];
          TeacherExamsController.builderSelectedQuestions[index] = TeacherExamsController.builderSelectedQuestions[index + 1];
          TeacherExamsController.builderSelectedQuestions[index + 1] = temp;
          TeacherExamsController.renderBuilder(container, teacher, exam.id);
        }
      });
    });

    // 8. Chỉnh sửa điểm từng câu (Real-time recalculate total points)
    container.querySelectorAll('.sq-point-input').forEach(input => {
      input.addEventListener('input', () => {
        const index = Number(input.getAttribute('data-index'));
        const newPoint = Number(input.value) || 0;
        TeacherExamsController.builderSelectedQuestions[index].points = newPoint;

        // Tính lại tổng điểm
        const newTotal = TeacherExamsController.builderSelectedQuestions.reduce((sum, q) => sum + (Number(q.points) || 0), 0);
        const totalEl = document.getElementById('builder-total-score');
        if (totalEl) totalEl.textContent = newTotal;
      });
    });

    // 9. Lưu cấu trúc đề thi
    const saveBtn = container.querySelector('.btn-save-builder');
    if (saveBtn) {
      saveBtn.addEventListener('click', async () => {
        if (TeacherExamsController.builderSelectedQuestions.length === 0) {
          toast.warning('Đề thi cần có tối thiểu 1 câu hỏi!');
          return;
        }

        const payload = TeacherExamsController.builderSelectedQuestions.map((q, idx) => ({
          question_id: q.question_id,
          points: Number(q.points) || 10,
          order_index: idx + 1
        }));

        await ExamModel.saveExamQuestions(exam.id, payload);
        toast.success(`Đã lưu cấu trúc đề thi (${payload.length} câu) thành công!`);
        TeacherExamsController.activeExamBuilderId = null;
        TeacherExamsController.renderList(container, teacher);
      });
    }
  }
}
