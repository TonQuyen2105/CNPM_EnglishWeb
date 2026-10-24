/**
 * StudentResultsController - Điều khiển màn hình Kết quả thi của Học viên
 */

import { auth } from '../../core/auth.js';
import { UserExamModel, ExamModel } from '../../models/index.js';
import { StudentResultsView } from '../../views/student/StudentResultsView.js';

export class StudentResultsController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    if (params && params.id) {
      return this._renderDetail(container, params.id);
    }

    return this._renderList(container);
  }

  static async _renderList(container) {
    const currentUser = auth.getCurrentUser();
    const attempts = await UserExamModel.getByUser(currentUser.id);
    attempts.sort((a, b) => new Date(b.created_at || b.submitted_at) - new Date(a.created_at || a.submitted_at));

    container.innerHTML = StudentResultsView.renderHistoryList({
      attempts
    });
  }

  static async _renderDetail(container, userExamId) {
    const currentUser = auth.getCurrentUser();
    const details = await UserExamModel.getAttemptDetails(userExamId, currentUser.id);

    if (!details) {
      container.innerHTML = `
        <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
          <h3>Không tìm thấy kết quả bài thi</h3>
          <a href="#/results" class="btn btn-primary" style="margin-top: 1rem;">Quay lại danh sách kết quả</a>
        </div>
      `;
      return;
    }

    // Lấy các lần thi khác của đề này
    const allAttempts = await UserExamModel.getByUser(currentUser.id);
    const otherAttempts = allAttempts.filter(a => a.exam_id === details.exam.id);

    container.innerHTML = StudentResultsView.renderAttemptDetail({
      attempt: details.user_exam,
      answers: details.answers,
      exam: details.exam,
      otherAttempts
    });
  }
}
