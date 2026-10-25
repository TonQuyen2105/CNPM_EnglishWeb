/**
 * TeacherQuestionsController - Điều phối Ngân hàng Câu hỏi
 */

import { auth } from '../../core/auth.js';
import { QuestionModel } from '../../models/QuestionModel.js';
import { LessonModel } from '../../models/LessonModel.js';
import { TeacherQuestionsView } from '../../views/teacher/TeacherQuestionsView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { storage } from '../../core/storage.js';

export class TeacherQuestionsController {
  static selectedType = 'all';
  static selectedLessonId = 'all';
  static searchQuery = '';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    await TeacherQuestionsController.updateView(container);
  }

  static async updateView(container) {
    const lessons = await LessonModel.getAll();
    const allQuestions = await QuestionModel.getAll();
    const options = storage.getTable('question_options') || [];
    const answers = storage.getTable('question_answers') || [];

    // Join thông tin options, answers và lesson_title
    let detailedQuestions = allQuestions.map(q => {
      const lesson = lessons.find(l => l.id === q.lesson_id);
      return {
        ...q,
        lesson_title: lesson ? `Bài ${lesson.order_index}: ${lesson.title}` : `Bài #${q.lesson_id}`,
        options: options.filter(opt => opt.question_id === q.id),
        answers: answers.filter(ans => ans.question_id === q.id)
      };
    });

    // Lọc theo loại
    if (TeacherQuestionsController.selectedType !== 'all') {
      detailedQuestions = detailedQuestions.filter(q => q.question_type === TeacherQuestionsController.selectedType);
    }

    // Lọc theo bài học
    if (TeacherQuestionsController.selectedLessonId !== 'all') {
      detailedQuestions = detailedQuestions.filter(q => String(q.lesson_id) === String(TeacherQuestionsController.selectedLessonId));
    }

    // Lọc theo từ khóa tìm kiếm
    if (TeacherQuestionsController.searchQuery.trim()) {
      const query = TeacherQuestionsController.searchQuery.trim().toLowerCase();
      detailedQuestions = detailedQuestions.filter(q => 
        (q.question_text && q.question_text.toLowerCase().includes(query)) ||
        (q.options && q.options.some(opt => opt.option_text && opt.option_text.toLowerCase().includes(query)))
      );
    }

    container.innerHTML = TeacherQuestionsView.render({
      questions: detailedQuestions,
      lessons,
      selectedType: TeacherQuestionsController.selectedType,
      selectedLessonId: TeacherQuestionsController.selectedLessonId,
      searchQuery: TeacherQuestionsController.searchQuery
    });

    TeacherQuestionsController.bindEvents(container, lessons);
  }

  static bindEvents(container, lessons) {
    // 1. Search
    const searchInput = container.querySelector('#filter-search');
    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        TeacherQuestionsController.searchQuery = e.target.value;
        TeacherQuestionsController.updateView(container);
      });
    }

    // 2. Type Filter
    const typeSelect = container.querySelector('#filter-type');
    if (typeSelect) {
      typeSelect.addEventListener('change', (e) => {
        TeacherQuestionsController.selectedType = e.target.value;
        TeacherQuestionsController.updateView(container);
      });
    }

    // 3. Lesson Filter
    const lessonSelect = container.querySelector('#filter-lesson');
    if (lessonSelect) {
      lessonSelect.addEventListener('change', (e) => {
        TeacherQuestionsController.selectedLessonId = e.target.value;
        TeacherQuestionsController.updateView(container);
      });
    }

    // 4. Tạo câu hỏi mới
    container.querySelectorAll('.btn-create-question').forEach(btn => {
      btn.addEventListener('click', () => {
        modal.open({
          title: 'Thêm câu hỏi mới vào ngân hàng',
          content: TeacherQuestionsView.renderQuestionForm(null, lessons),
          size: 'large',
          confirmText: 'Lưu câu hỏi',
          cancelText: 'Hủy',
          onOpen: () => {
            TeacherQuestionsController.bindModalDynamicForm();
          },
          onConfirm: async () => {
            return await TeacherQuestionsController.handleSaveQuestion(null, container);
          }
        });
      });
    });

    // 5. Chỉnh sửa câu hỏi
    container.querySelectorAll('.btn-edit-question').forEach(btn => {
      btn.addEventListener('click', async () => {
        const qId = btn.getAttribute('data-question-id');
        const question = await QuestionModel.getWithDetails(qId);
        if (!question) return;

        modal.open({
          title: `Chỉnh sửa câu hỏi #${question.id}`,
          content: TeacherQuestionsView.renderQuestionForm(question, lessons),
          size: 'large',
          confirmText: 'Cập nhật',
          cancelText: 'Hủy',
          onOpen: () => {
            TeacherQuestionsController.bindModalDynamicForm();
          },
          onConfirm: async () => {
            return await TeacherQuestionsController.handleSaveQuestion(question.id, container);
          }
        });
      });
    });

    // 6. Xóa câu hỏi
    container.querySelectorAll('.btn-delete-question').forEach(btn => {
      btn.addEventListener('click', async () => {
        const qId = btn.getAttribute('data-question-id');
        const confirmed = await confirmDialog({
          title: 'Xóa câu hỏi',
          message: 'Bạn có chắc chắn muốn xóa câu hỏi này khỏi ngân hàng đề? Các bài thi cũ có thể bị ảnh hưởng.',
          confirmText: 'Xóa câu hỏi',
          cancelText: 'Hủy'
        });

        if (confirmed) {
          await QuestionModel.deleteWithDetails(qId);
          toast.success('Đã xóa câu hỏi khỏi hệ thống!');
          TeacherQuestionsController.updateView(container);
        }
      });
    });
  }

  /**
   * Quản lý sự kiện form động trong Modal (chuyển loại, thêm/bớt options/answers)
   */
  static bindModalDynamicForm() {
    const typeSelect = document.getElementById('modal-question-type');
    if (typeSelect) {
      typeSelect.addEventListener('change', (e) => {
        const val = e.target.value;
        document.querySelectorAll('.dynamic-type-section').forEach(sec => sec.style.display = 'none');
        if (val === 'multiple_choice') document.getElementById('section-multiple-choice').style.display = 'block';
        if (val === 'fill_blank') document.getElementById('section-fill-blank').style.display = 'block';
        if (val === 'pronunciation') document.getElementById('section-pronunciation').style.display = 'block';
        if (val === 'essay') document.getElementById('section-essay').style.display = 'block';
      });
    }

    // Thêm option trắc nghiệm
    const addOptBtn = document.querySelector('.btn-add-option');
    if (addOptBtn) {
      addOptBtn.addEventListener('click', () => {
        const list = document.getElementById('mc-options-list');
        const row = document.createElement('div');
        row.className = 'mc-option-row';
        row.style.cssText = 'display: flex; align-items: center; gap: 8px;';
        row.innerHTML = `
          <input type="radio" name="mc_correct" class="mc-correct-radio" title="Đánh dấu là đáp án đúng">
          <input type="text" class="form-control form-control-sm mc-text-input" placeholder="Lựa chọn mới..." value="">
          <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-option" title="Xóa">✕</button>
        `;
        list.appendChild(row);
      });
    }

    // Thêm answer điền từ
    const addAnsBtn = document.querySelector('.btn-add-answer');
    if (addAnsBtn) {
      addAnsBtn.addEventListener('click', () => {
        const list = document.getElementById('fb-answers-list');
        const row = document.createElement('div');
        row.className = 'fb-answer-row';
        row.style.cssText = 'display: flex; align-items: center; gap: 8px;';
        row.innerHTML = `
          <input type="text" class="form-control form-control-sm fb-text-input" placeholder="Từ / Cụm từ đúng..." value="">
          <label style="display: flex; align-items: center; gap: 4px; font-size: 0.75rem; white-space: nowrap; cursor: pointer;">
            <input type="checkbox" class="fb-case-checkbox"> Phân biệt HOA/thường
          </label>
          <button type="button" class="btn btn-sm btn-icon text-danger btn-remove-answer" title="Xóa">✕</button>
        `;
        list.appendChild(row);
      });
    }

    // Xóa row option hoặc answer (Event delegation)
    const form = document.getElementById('form-question-dynamic');
    if (form) {
      form.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-remove-option')) {
          const row = e.target.closest('.mc-option-row');
          if (row && document.querySelectorAll('.mc-option-row').length > 1) {
            row.remove();
          } else {
            toast.warning('Cần tối thiểu 1 lựa chọn trắc nghiệm');
          }
        }
        if (e.target.classList.contains('btn-remove-answer')) {
          const row = e.target.closest('.fb-answer-row');
          if (row && document.querySelectorAll('.fb-answer-row').length > 1) {
            row.remove();
          } else {
            toast.warning('Cần tối thiểu 1 đáp án');
          }
        }
      });
    }
  }

  /**
   * Lưu câu hỏi từ form modal
   */
  static async handleSaveQuestion(existingId = null, container) {
    const textInput = document.getElementById('modal-question-text');
    const typeSelect = document.getElementById('modal-question-type');
    const lessonSelect = document.getElementById('modal-question-lesson');
    const pointsInput = document.getElementById('modal-question-points');

    if (!textInput || !textInput.value.trim()) {
      toast.error('Vui lòng nhập nội dung đề bài câu hỏi!');
      return false;
    }

    const questionType = typeSelect.value;
    const lessonId = Number(lessonSelect.value);
    const defaultPoints = Number(pointsInput.value) || 10;
    const questionText = textInput.value.trim();

    const options = [];
    if (questionType === 'multiple_choice') {
      const rows = document.querySelectorAll('.mc-option-row');
      rows.forEach(r => {
        const text = r.querySelector('.mc-text-input').value.trim();
        const isCorrect = r.querySelector('.mc-correct-radio').checked;
        if (text) {
          options.push({ option_text: text, is_correct: isCorrect });
        }
      });

      if (options.length < 2) {
        toast.error('Vui lòng nhập tối thiểu 2 lựa chọn trắc nghiệm!');
        return false;
      }
      if (!options.some(o => o.is_correct)) {
        toast.error('Vui lòng chọn 1 đáp án đúng (tick radio)!');
        return false;
      }
    }

    const answers = [];
    if (questionType === 'fill_blank') {
      const rows = document.querySelectorAll('.fb-answer-row');
      rows.forEach(r => {
        const text = r.querySelector('.fb-text-input').value.trim();
        const isCase = r.querySelector('.fb-case-checkbox').checked;
        if (text) {
          answers.push({ answer_text: text, is_case_sensitive: isCase });
        }
      });

      if (answers.length === 0) {
        toast.error('Vui lòng nhập tối thiểu 1 từ/cụm từ đúng!');
        return false;
      }
    }

    let audioUrl = '';
    if (questionType === 'pronunciation') {
      const audioInput = document.getElementById('modal-audio-url');
      audioUrl = audioInput ? audioInput.value.trim() : '';
    }

    if (existingId) {
      await QuestionModel.updateWithDetails(existingId, {
        lesson_id: lessonId,
        question_text: questionText,
        question_type: questionType,
        default_points: defaultPoints,
        audio_url: audioUrl,
        options,
        answers
      });
      toast.success('Đã cập nhật câu hỏi thành công!');
    } else {
      await QuestionModel.createWithDetails({
        lesson_id: lessonId,
        question_text: questionText,
        question_type: questionType,
        default_points: defaultPoints,
        audio_url: audioUrl,
        options,
        answers
      });
      toast.success('Đã thêm câu hỏi vào ngân hàng thành công!');
    }

    TeacherQuestionsController.updateView(container);
    return true;
  }
}
