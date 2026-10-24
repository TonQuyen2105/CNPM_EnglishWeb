/**
 * SubjectsController - Điều phối quản lý môn học
 */

import { SubjectsView } from '../../views/admin/SubjectsView.js';
import { SubjectModel, CourseModel } from '../../models/index.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';

export class SubjectsController {
  static async render(params, query, container) {
    this.container = container;
    this.subjects = [];
    this.courses = [];

    container.innerHTML = SubjectsView.renderLayout();
    await this.loadData();
    this.bindEvents();
  }

  static async loadData() {
    this.subjects = await SubjectModel.getAll();
    this.courses = await CourseModel.getAll();
    this.renderCards();
  }

  static renderCards() {
    const cardsOutlet = this.container.querySelector('#subjects-cards-outlet');
    if (!cardsOutlet) return;

    if (this.subjects.length === 0) {
      cardsOutlet.innerHTML = `
        <div class="col-span-3 card p-8 text-center text-muted">
          Chưa có môn học nào. Hãy bấm "Thêm môn học mới".
        </div>
      `;
      return;
    }

    cardsOutlet.innerHTML = this.subjects.map(s => {
      const count = this.courses.filter(c => c.subject_id === s.id).length;
      return SubjectsView.renderSubjectCard(s, count);
    }).join('');

    this.bindCardEvents(cardsOutlet);
  }

  static bindEvents() {
    const addBtn = this.container.querySelector('#btn-add-subject');
    if (addBtn) {
      addBtn.addEventListener('click', () => this.openSubjectModal(null));
    }
  }

  static bindCardEvents(container) {
    // Switch bật/tắt
    const switches = container.querySelectorAll('.subject-status-switch');
    switches.forEach(sw => {
      sw.addEventListener('change', async () => {
        const id = sw.getAttribute('data-id');
        const active = sw.checked ? 1 : 0;
        await SubjectModel.update(id, { is_active: active });
        toast.success(`Đã ${active ? 'kích hoạt' : 'ẩn'} môn học thành công!`);
        await this.loadData();
      });
    });

    // Sửa
    const editBtns = container.querySelectorAll('.btn-edit-subject');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const subject = await SubjectModel.getById(id);
        if (subject) this.openSubjectModal(subject);
      });
    });

    // Xóa
    const delBtns = container.querySelectorAll('.btn-delete-subject');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const subject = await SubjectModel.getById(id);
        if (!subject) return;

        const relatedCourses = this.courses.filter(c => c.subject_id === subject.id);

        if (relatedCourses.length > 0) {
          const ok = await confirmDialog({
            title: 'Môn học đang có khóa học liên kết',
            message: `Môn học "${subject.name}" hiện đang chứa ${relatedCourses.length} khóa học. Hệ thống sẽ chuyển trạng thái môn học sang ngưng hoạt động (soft-delete) để bảo toàn dữ liệu.`,
            confirmText: 'Chuyển ngưng hoạt động',
            isDanger: true
          });
          if (ok) {
            await SubjectModel.update(id, { is_active: 0 });
            toast.success(`Đã ngưng hoạt động môn học "${subject.name}"!`);
            await this.loadData();
          }
        } else {
          const ok = await confirmDialog({
            title: 'Xác nhận xóa môn học',
            message: `Bạn có chắc muốn xóa vĩnh viễn môn học "${subject.name}"?`,
            confirmText: 'Xóa vĩnh viễn',
            isDanger: true
          });
          if (ok) {
            await SubjectModel.delete(id);
            toast.success(`Đã xóa môn học "${subject.name}"!`);
            await this.loadData();
          }
        }
      });
    });
  }

  static openSubjectModal(subject = null) {
    const isEdit = !!subject;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa môn học' : 'Thêm môn học mới',
      body: SubjectsView.renderSubjectModal(subject),
      maxWidth: '500px'
    });

    const form = document.getElementById('subject-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const name = document.getElementById('modal-subject-name').value.trim();
      const desc = document.getElementById('modal-subject-desc').value.trim();
      const active = document.getElementById('modal-subject-active').checked ? 1 : 0;

      if (!name) {
        toast.warning('Vui lòng nhập tên môn học.');
        return;
      }

      if (isEdit) {
        await SubjectModel.update(subject.id, {
          name,
          description: desc,
          is_active: active
        });
        toast.success('Đã cập nhật môn học thành công!');
      } else {
        await SubjectModel.create({
          name,
          description: desc,
          is_active: active
        });
        toast.success('Đã tạo môn học mới thành công!');
      }

      modal.close(modalId);
      await this.loadData();
    });
  }
}
