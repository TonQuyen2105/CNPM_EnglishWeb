/**
 * ExamsController - Điều phối quản lý đề thi và thống kê tỷ lệ đạt
 */

import { ExamsView } from '../../views/admin/ExamsView.js';
import { ExamModel } from '../../models/index.js';
import { storage } from '../../core/storage.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { escapeHtml } from '../../core/utils.js';

export class ExamsController {
  static async render(params, query, container) {
    this.container = container;
    this.exams = [];
    this.userExams = [];
    this.searchTerm = '';
    this.statusFilter = 'all';

    await this.initData();
  }

  static async initData() {
    this.exams = await ExamModel.getAllWithTeacher();
    this.userExams = storage.getTable('user_exams') || [];

    this.container.innerHTML = ExamsView.renderLayout();
    this.renderGrid();
    this.bindEvents();
  }

  static renderGrid() {
    const gridOutlet = this.container.querySelector('#exams-grid-outlet');
    if (!gridOutlet) return;

    let filtered = [...this.exams];

    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      filtered = filtered.filter(e => e.title && e.title.toLowerCase().includes(q));
    }

    if (this.statusFilter !== 'all') {
      const isPub = this.statusFilter === 'published' ? 1 : 0;
      filtered = filtered.filter(e => (e.is_published === 1 || e.is_published === true ? 1 : 0) === isPub);
    }

    if (filtered.length === 0) {
      gridOutlet.innerHTML = `
        <div class="col-span-3 card p-8 text-center text-muted">
          Không tìm thấy bài thi nào phù hợp.
        </div>
      `;
      return;
    }

    gridOutlet.innerHTML = filtered.map(exam => {
      const attempts = this.userExams.filter(ue => ue.exam_id === exam.id);
      const passed = attempts.filter(ue => ue.status === 'passed');
      const passRate = attempts.length > 0 ? Math.round((passed.length / attempts.length) * 100) : 0;

      return ExamsView.renderExamCard(exam, {
        totalAttempts: attempts.length,
        passRate
      });
    }).join('');

    this.bindCardEvents(gridOutlet);
  }

  static bindEvents() {
    const searchInput = this.container.querySelector('#exam-search-input');
    const statusFilter = this.container.querySelector('#exam-status-filter');

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.renderGrid();
      });
    }

    if (statusFilter) {
      statusFilter.addEventListener('change', (e) => {
        this.statusFilter = e.target.value;
        this.renderGrid();
      });
    }
  }

  static bindCardEvents(container) {
    // Switch xuất bản
    const switches = container.querySelectorAll('.exam-publish-switch');
    switches.forEach(sw => {
      sw.addEventListener('change', async () => {
        const id = sw.getAttribute('data-id');
        const pub = sw.checked ? 1 : 0;
        await ExamModel.update(id, { is_published: pub });
        toast.success(`Đã ${pub ? 'mở xuất bản' : 'chuyển sang bản nháp'} đề thi!`);
      });
    });

    // Xem trước câu hỏi
    const previewBtns = container.querySelectorAll('.btn-preview-exam');
    previewBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const examDetail = await ExamModel.getWithQuestions(id);
        if (!examDetail) return;

        modal.open({
          title: `Đề thi: ${examDetail.title}`,
          body: `
            <div style="margin-bottom: 1rem; color: var(--text-secondary); font-size: 0.875rem;">
              <p>${escapeHtml(examDetail.description)}</p>
              <div>Thời lượng: <strong>${examDetail.duration_minutes} phút</strong> • Điểm đạt: <strong>${examDetail.pass_score}đ</strong></div>
            </div>
            <h4 style="font-size: 0.9375rem; margin-bottom: 0.75rem;">Danh sách câu hỏi trong đề (${examDetail.questions.length})</h4>
            <div class="flex flex-col gap-3" style="max-height: 350px; overflow-y: auto;">
              ${examDetail.questions.length === 0 ? '<div class="text-muted">Đề thi chưa có câu hỏi nào.</div>' : ''}
              ${examDetail.questions.map((q, idx) => `
                <div style="padding: 0.75rem; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: var(--neutral-50);">
                  <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
                    <strong style="font-size: 0.8125rem;">Câu ${idx + 1} (${q.points} điểm)</strong>
                    <span class="badge badge-info">${q.question_type}</span>
                  </div>
                  <div style="font-size: 0.875rem; margin-bottom: 4px;">${escapeHtml(q.question_text)}</div>
                  ${q.options && q.options.length > 0 ? `
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 4px; font-size: 0.75rem;">
                      ${q.options.map(opt => `
                        <div style="padding: 4px 6px; border-radius: 4px; ${opt.is_correct ? 'background: var(--success-bg); color: var(--success-text); font-weight: 700;' : 'background: white;'}">
                          • ${escapeHtml(opt.option_text)} ${opt.is_correct ? '✓ (Đáp án)' : ''}
                        </div>
                      `).join('')}
                    </div>
                  ` : ''}
                </div>
              `).join('')}
            </div>
          `,
          footer: '<button type="button" class="btn btn-primary" data-dismiss="modal">Đóng</button>'
        });
      });
    });
  }
}
