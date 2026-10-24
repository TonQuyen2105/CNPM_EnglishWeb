/**
 * ClassesController - Điều phối quản lý lớp học và chi tiết phân công, sĩ số
 */

import { ClassesView } from '../../views/admin/ClassesView.js';
import { ClassDetailView } from '../../views/admin/ClassDetailView.js';
import {
  CourseClassModel,
  CourseModel,
  TeacherModel,
  ClassTeacherModel,
  EnrollmentModel,
  ClassLessonModel
} from '../../models/index.js';
import { DataTable } from '../../ui/dataTable.js';
import { Pagination } from '../../ui/pagination.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';
import { formatDate, renderStatusBadge } from '../../core/utils.js';
import { icon } from '../../ui/icons.js';

export class ClassesController {
  // ROUTE 1: DANH SÁCH LỚP HỌC (#/classes)
  static async render(params, query, container) {
    this.container = container;
    this.classes = [];
    this.courses = [];
    this.filteredClasses = [];
    this.currentPage = 1;
    this.pageSize = 10;
    this.searchTerm = '';
    this.statusFilter = 'all';

    container.innerHTML = ClassesView.renderLayout();
    this.courses = await CourseModel.getAll();
    await this.loadClasses();
    this.bindListEvents();
  }

  static async loadClasses() {
    this.classes = await CourseClassModel.getAllWithDetails();
    this.applyFilters();
  }

  static applyFilters() {
    let result = [...this.classes];

    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      result = result.filter(c => 
        (c.class_code && c.class_code.toLowerCase().includes(q)) ||
        (c.class_name && c.class_name.toLowerCase().includes(q))
      );
    }

    if (this.statusFilter !== 'all') {
      result = result.filter(c => c.status === this.statusFilter);
    }

    this.filteredClasses = result;
    this.currentPage = 1;
    this.renderTable();
  }

  static renderTable() {
    const tableOutlet = this.container.querySelector('#classes-table-outlet');
    const paginationOutlet = this.container.querySelector('#classes-pagination-outlet');
    if (!tableOutlet || !paginationOutlet) return;

    const start = (this.currentPage - 1) * this.pageSize;
    const pageData = this.filteredClasses.slice(start, start + this.pageSize);

    this.dataTable = new DataTable({
      columns: [
        {
          key: 'class_code',
          label: 'Mã & Tên lớp',
          sortable: true,
          render: (val, row) => `
            <div>
              <strong style="color: var(--primary-600);">${val}</strong>
              <div style="font-size: 0.8125rem; font-weight: 600;">${row.class_name}</div>
              <div style="font-size: 0.75rem; color: var(--text-muted);">${row.course_title}</div>
            </div>
          `
        },
        {
          key: 'main_teacher_name',
          label: 'Giáo viên phụ trách',
          sortable: true,
          render: (val) => val === 'Chưa phân công' 
            ? `<span style="color: var(--text-muted); font-style: italic;">Chưa phân công</span>` 
            : `<strong>${val}</strong>`
        },
        {
          key: 'start_date',
          label: 'Thời gian học',
          sortable: true,
          render: (val, row) => `
            <div style="font-size: 0.75rem;">
              <span>${formatDate(val)}</span> &rarr; <span>${formatDate(row.end_date)}</span>
            </div>
          `
        },
        {
          key: 'enrolled_count',
          label: 'Sĩ số',
          sortable: true,
          render: (val, row) => `
            <div>
              <strong>${val || 0} / ${row.max_students}</strong>
              <div class="progress-bar-container" style="height: 6px; margin-top: 4px; max-width: 80px;">
                <div class="progress-bar-fill" style="width: ${Math.min(100, Math.round(((val || 0) / row.max_students) * 100))}%;"></div>
              </div>
            </div>
          `
        },
        {
          key: 'status',
          label: 'Trạng thái',
          sortable: true,
          render: (val) => renderStatusBadge(val)
        },
        {
          key: 'actions',
          label: 'Thao tác',
          render: (_, row) => `
            <div style="display: flex; gap: 6px;">
              <a href="#/classes/${row.id}" class="btn btn-secondary btn-sm" title="Chi tiết lớp học">
                ${icon('eye', { size: 14 })}
                <span>Chi tiết</span>
              </a>
              <button type="button" class="btn btn-secondary btn-sm btn-edit-class" data-id="${row.id}" title="Sửa thông tin">
                ${icon('edit', { size: 14 })}
              </button>
            </div>
          `
        }
      ],
      data: pageData,
      emptyMessage: 'Không có lớp học nào phù hợp với bộ lọc.'
    });

    tableOutlet.innerHTML = this.dataTable.render();
    this.dataTable.attachEvents(tableOutlet);

    this.pagination = new Pagination({
      totalItems: this.filteredClasses.length,
      pageSize: this.pageSize,
      currentPage: this.currentPage,
      onPageChange: (newPage) => {
        this.currentPage = newPage;
        this.renderTable();
      }
    });

    paginationOutlet.innerHTML = this.pagination.render();
    this.pagination.attachEvents(paginationOutlet);

    this.bindRowEvents(tableOutlet);
  }

  static bindListEvents() {
    const searchInput = this.container.querySelector('#class-search-input');
    const statusFilter = this.container.querySelector('#class-status-filter');
    const addBtn = this.container.querySelector('#btn-add-class');

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.applyFilters();
      });
    }

    if (statusFilter) {
      statusFilter.addEventListener('change', (e) => {
        this.statusFilter = e.target.value;
        this.applyFilters();
      });
    }

    if (addBtn) {
      addBtn.addEventListener('click', () => this.openClassModal(null));
    }
  }

  static bindRowEvents(container) {
    const editBtns = container.querySelectorAll('.btn-edit-class');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const cls = await CourseClassModel.getById(id);
        if (cls) this.openClassModal(cls);
      });
    });
  }

  static openClassModal(cls = null) {
    const isEdit = !!cls;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa lớp học' : 'Mở lớp học mới',
      body: ClassesView.renderClassModal(this.courses, cls),
      maxWidth: '600px'
    });

    const form = document.getElementById('class-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const code = document.getElementById('modal-class-code').value.trim();
      const courseId = parseInt(document.getElementById('modal-class-course').value, 10);
      const name = document.getElementById('modal-class-name').value.trim();
      const start = document.getElementById('modal-class-start').value;
      const end = document.getElementById('modal-class-end').value;
      const max = parseInt(document.getElementById('modal-class-max').value, 10) || 20;
      const price = parseFloat(document.getElementById('modal-class-price').value) || 0;
      const status = document.getElementById('modal-class-status').value;

      if (!code || !name) {
        toast.warning('Vui lòng điền mã lớp và tên lớp.');
        return;
      }

      if (isEdit) {
        await CourseClassModel.update(cls.id, {
          class_code: code,
          course_id: courseId,
          class_name: name,
          start_date: start,
          end_date: end,
          max_students: max,
          price,
          status
        });
        toast.success('Đã cập nhật thông tin lớp học thành công!');
      } else {
        await CourseClassModel.create({
          class_code: code,
          course_id: courseId,
          class_name: name,
          start_date: start,
          end_date: end,
          max_students: max,
          price,
          status
        });
        toast.success('Đã tạo lớp học mới thành công!');
      }

      modal.close(modalId);
      await this.loadClasses();
    });
  }

  // ROUTE 2: CHI TIẾT LỚP HỌC (#/classes/:id)
  static async renderDetail(params, query, container) {
    this.container = container;
    this.classId = params.id;
    this.activeTab = query && query.tab ? query.tab : 'teachers';

    await this.loadClassDetail();
  }

  static async loadClassDetail() {
    this.cls = await CourseClassModel.getByIdWithDetails(this.classId);
    if (!this.cls) {
      this.container.innerHTML = `
        <div class="card p-8 text-center text-danger">
          Không tìm thấy thông tin lớp học (#${this.classId}).
          <div style="margin-top: 1rem;"><a href="#/classes" class="btn btn-secondary">Quay lại danh sách</a></div>
        </div>
      `;
      return;
    }

    // Tải thông tin giáo viên phụ trách & buổi học
    this.cls.assigned_teachers = await ClassTeacherModel.getByClass(this.classId);
    this.cls.class_lessons = await ClassLessonModel.getByClass(this.classId);

    this.container.innerHTML = ClassDetailView.render({
      cls: this.cls,
      activeTab: this.activeTab
    });

    this.bindDetailEvents();
  }

  static bindDetailEvents() {
    // 1. Chuyển tab
    const tabBtns = this.container.querySelectorAll('.tab-btn');
    tabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        this.activeTab = btn.getAttribute('data-tab');
        this.loadClassDetail();
      });
    });

    // 2. Nút sửa thông tin lớp
    const editInfoBtn = this.container.querySelector('#btn-edit-class-info');
    if (editInfoBtn) {
      editInfoBtn.addEventListener('click', () => this.openClassModal(this.cls));
    }

    // 3. Tab Giáo viên: Nút phân công giáo viên mới
    const assignBtn = this.container.querySelector('#btn-assign-teacher');
    if (assignBtn) {
      assignBtn.addEventListener('click', async () => {
        const teachers = await TeacherModel.getAllWithUser();
        const modalId = modal.open({
          title: `Phân công giảng dạy cho lớp ${this.cls.class_code}`,
          body: ClassDetailView.renderAssignTeacherModal(teachers),
          maxWidth: '500px'
        });

        const form = document.getElementById('assign-teacher-form');
        if (form) {
          form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const teacherId = document.getElementById('modal-assign-teacher-select').value;
            const role = document.getElementById('modal-assign-teacher-role').value;

            await ClassTeacherModel.assignTeacher(this.cls.id, teacherId, role);
            toast.success('Đã phân công giáo viên vào lớp thành công!');
            modal.close(modalId);
            await this.loadClassDetail();
          });
        }
      });
    }

    // 4. Tab Giáo viên: Gỡ giáo viên khỏi lớp
    const removeTeacherBtns = this.container.querySelectorAll('.btn-remove-teacher');
    removeTeacherBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const teacherId = btn.getAttribute('data-teacher-id');
        const teacherName = btn.getAttribute('data-name');

        const ok = await confirmDialog({
          title: 'Gỡ phân công giáo viên',
          message: `Bạn có chắc muốn gỡ giáo viên "${teacherName}" khỏi lớp này không?`,
          confirmText: 'Gỡ phân công',
          isDanger: true
        });

        if (ok) {
          await ClassTeacherModel.removeTeacher(this.cls.id, teacherId);
          toast.success(`Đã gỡ giáo viên ${teacherName} khỏi lớp!`);
          await this.loadClassDetail();
        }
      });
    });

    // 5. Tab Học sinh: Đổi trạng thái học viên
    const statusSelects = this.container.querySelectorAll('.select-enrollment-status');
    statusSelects.forEach(sel => {
      sel.addEventListener('change', async (e) => {
        const enrollmentId = sel.getAttribute('data-enrollment-id');
        const newStatus = sel.value;

        await EnrollmentModel.update(enrollmentId, { status: newStatus });
        toast.success(`Đã cập nhật trạng thái học viên sang: ${newStatus}`);
        await this.loadClassDetail();
      });
    });
  }
}
