/**
 * TeacherResultsController - Điều phối Kết quả Khảo thí Học viên
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { ExamModel } from '../../models/ExamModel.js';
import { CourseClassModel } from '../../models/CourseClassModel.js';
import { UserExamModel } from '../../models/UserExamModel.js';
import { TeacherResultsView } from '../../views/teacher/TeacherResultsView.js';
import { modal } from '../../ui/modal.js';
import { storage } from '../../core/storage.js';

export class TeacherResultsController {
  static selectedExamId = 'all';
  static selectedClassId = 'all';
  static selectedStatus = 'all';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) return;

    await TeacherResultsController.updateView(container, teacher);
  }

  static async updateView(container, teacher) {
    const exams = await ExamModel.getByTeacher(teacher.id);
    const classes = await CourseClassModel.getByTeacherId(teacher.id);

    const filter = {
      examId: TeacherResultsController.selectedExamId !== 'all' ? TeacherResultsController.selectedExamId : null,
      classId: TeacherResultsController.selectedClassId !== 'all' ? TeacherResultsController.selectedClassId : null,
      status: TeacherResultsController.selectedStatus
    };

    const results = await UserExamModel.getResultsByTeacher(teacher.id, filter);

    // Tính toán phân bố điểm số (Histogram 5 khoảng)
    const distributionData = [
      { range: '0 - 20 đ', count: 0 },
      { range: '21 - 40 đ', count: 0 },
      { range: '41 - 60 đ', count: 0 },
      { range: '61 - 80 đ', count: 0 },
      { range: '81 - 100 đ', count: 0 }
    ];

    let totalScoreSum = 0;
    let gradedCount = 0;
    let passedCount = 0;

    results.forEach(r => {
      if (r.total_score !== null) {
        gradedCount++;
        totalScoreSum += r.total_score;
        if (r.status === 'passed') passedCount++;

        const score = r.total_score;
        if (score <= 20) distributionData[0].count++;
        else if (score <= 40) distributionData[1].count++;
        else if (score <= 60) distributionData[2].count++;
        else if (score <= 80) distributionData[3].count++;
        else distributionData[4].count++;
      }
    });

    const summaryStats = {
      totalAttempts: results.length,
      gradedCount,
      passedCount,
      passRate: gradedCount > 0 ? Math.round((passedCount / gradedCount) * 100) : 0,
      avgScore: gradedCount > 0 ? Math.round(totalScoreSum / gradedCount) : 0
    };

    container.innerHTML = TeacherResultsView.render({
      results,
      exams,
      classes,
      selectedExamId: TeacherResultsController.selectedExamId,
      selectedClassId: TeacherResultsController.selectedClassId,
      selectedStatus: TeacherResultsController.selectedStatus,
      distributionData,
      summaryStats
    });

    TeacherResultsController.bindEvents(container, teacher);
  }

  static bindEvents(container, teacher) {
    // 1. Filter Exam
    const examSelect = container.querySelector('#filter-result-exam');
    if (examSelect) {
      examSelect.addEventListener('change', (e) => {
        TeacherResultsController.selectedExamId = e.target.value;
        TeacherResultsController.updateView(container, teacher);
      });
    }

    // 2. Filter Class
    const classSelect = container.querySelector('#filter-result-class');
    if (classSelect) {
      classSelect.addEventListener('change', (e) => {
        TeacherResultsController.selectedClassId = e.target.value;
        TeacherResultsController.updateView(container, teacher);
      });
    }

    // 3. Filter Status
    const statusSelect = container.querySelector('#filter-result-status');
    if (statusSelect) {
      statusSelect.addEventListener('change', (e) => {
        TeacherResultsController.selectedStatus = e.target.value;
        TeacherResultsController.updateView(container, teacher);
      });
    }

    // 4. Xem chi tiết bài làm
    container.querySelectorAll('.btn-view-submission').forEach(btn => {
      btn.addEventListener('click', async () => {
        const userExamId = Number(btn.getAttribute('data-user-exam-id'));
        const userExams = await UserExamModel.getResultsByTeacher(teacher.id);
        const ue = userExams.find(item => item.id === userExamId);
        if (!ue) return;

        // Lấy chi tiết các câu trả lời
        const allAnswers = storage.getTable('user_exam_answers') || [];
        const myAnswers = allAnswers.filter(a => a.user_exam_id === userExamId);
        const questions = storage.getTable('questions') || [];
        const options = storage.getTable('question_options') || [];
        const examQuestions = storage.getTable('exam_questions') || [];

        const detailedAnswers = myAnswers.map(ans => {
          const q = questions.find(item => item.id === ans.question_id) || {};
          const opt = options.find(o => o.id === ans.selected_option_id);
          const eq = examQuestions.find(item => item.exam_id === ue.exam_id && item.question_id === ans.question_id);

          return {
            ...ans,
            question_text: q.question_text || '',
            question_type: q.question_type || 'multiple_choice',
            selected_option_text: opt ? opt.option_text : '',
            max_points: eq ? eq.points : (q.default_points || 20)
          };
        });

        modal.open({
          title: `Bài làm của thí sinh: ${ue.student_name}`,
          content: TeacherResultsView.renderSubmissionDetail(ue, detailedAnswers),
          size: 'large',
          confirmText: 'Đóng',
          cancelText: null
        });
      });
    });
  }
}
