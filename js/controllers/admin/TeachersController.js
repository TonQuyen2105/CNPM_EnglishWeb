/**
 * TeachersController - Điều phối quản lý giảng viên và phê duyệt hồ sơ
 */

import { TeachersView } from '../../views/admin/TeachersView.js';
import { TeacherModel } from '../../models/index.js';
import { storage } from '../../core/storage.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';

export class TeachersController {
  static async render(params, query, container) {
    this.container = container;
    this.teachers = [];
    this.classAssignments = [];
    this.classes = [];
    this.searchTerm = '';
    this.verifyFilter = 'all';

    container.innerHTML = TeachersView.renderLayout();
    await this.loadData();
    this.bindEvents();
  }

  static async loadData() {
    this.teachers = await TeacherModel.getAllWithUser();
    this.classAssignments = storage.getTable('class_teachers') || [];
    this.classes = storage.getTable('course_classes') || [];
    this.renderGrid();
  }

  static renderGrid() {
    const gridOutlet = this.container.querySelector('#teachers-grid-outlet');
    if (!gridOutlet) return;

    let filtered = [...this.teachers];

    // Lọc theo search
    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      filtered = filtered.filter(t => 
        (t.full_name && t.full_name.toLowerCase().includes(q)) ||
        (t.qualification && t.qualification.toLowerCase().includes(q)) ||
        (t.bio && t.bio.toLowerCase().includes(q))
      );
    }

    // Lọc theo verify
    if (this.verifyFilter === 'verified') {
      filtered = filtered.filter(t => t.is_verified === 1 || t.is_verified === true);
    } else if (this.verifyFilter === 'unverified') {
      filtered = filtered.filter(t => t.is_verified === 0 || t.is_verified === false);
    }

    if (filtered.length === 0) {
      gridOutlet.innerHTML = `
        <div class="col-span-3 card p-8 text-center text-muted">
          Không tìm thấy giảng viên nào phù hợp.
        </div>
      `;
      return;
    }

    gridOutlet.innerHTML = filtered.map(t => {
      // Tìm các lớp giáo viên này đang được gán
      const assigned = this.classAssignments
        .filter(ca => ca.teacher_id === t.id)
        .map(ca => {
          const cls = this.classes.find(c => c.id === ca.class_id) || {};
          return {
            class_id: ca.class_id,
            role: ca.role,
            class_code: cls.class_code || 'Lớp',
            class_name: cls.class_name || ''
          };
        });

      return TeachersView.renderTeacherCard(t, assigned);
    }).join('');

    this.bindCardEvents(gridOutlet);
  }

  static bindEvents() {
    const searchInput = this.container.querySelector('#teacher-search-input');
    const verifyFilter = this.container.querySelector('#teacher-verify-filter');

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.renderGrid();
      });
    }

    if (verifyFilter) {
      verifyFilter.addEventListener('change', (e) => {
        this.verifyFilter = e.target.value;
        this.renderGrid();
      });
    }
  }

  static bindCardEvents(container) {
    // Nút toggle xác minh
    const toggleBtns = container.querySelectorAll('.btn-toggle-verify');
    toggleBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const currentVerified = btn.getAttribute('data-verified') === '1';
        const newStatus = currentVerified ? 0 : 1;

        btn.classList.add('loading');
        btn.disabled = true;

        await TeacherModel.update(id, { is_verified: newStatus });
        toast.success(`Đã ${newStatus ? 'phê duyệt xác minh' : 'hủy xác minh'} giảng viên thành công!`);
        await this.loadData();
      });
    });

    // Nút sửa hồ sơ
    const editBtns = container.querySelectorAll('.btn-edit-teacher');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const teacher = await TeacherModel.getByIdWithUser(id);
        if (teacher) this.openEditModal(teacher);
      });
    });
  }

  static openEditModal(teacher) {
    const modalId = modal.open({
      title: 'Chỉnh sửa hồ sơ chuyên môn giảng viên',
      body: TeachersView.renderTeacherModal(teacher),
      maxWidth: '540px'
    });

    const form = document.getElementById('teacher-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const qual = document.getElementById('modal-teacher-qualification').value.trim();
      const bio = document.getElementById('modal-teacher-bio').value.trim();
      const verified = document.getElementById('modal-teacher-verified').checked ? 1 : 0;

      if (!qual) {
        toast.warning('Vui lòng nhập bằng cấp chuyên môn.');
        return;
      }

      await TeacherModel.update(teacher.id, {
        qualification: qual,
        bio,
        is_verified: verified
      });

      toast.success('Đã cập nhật hồ sơ giảng viên thành công!');
      modal.close(modalId);
      await this.loadData();
    });
  }
}
