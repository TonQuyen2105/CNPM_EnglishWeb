/**
 * EnrollmentsController - Điều phối quản lý đăng ký và xuất dữ liệu CSV
 */

import { EnrollmentsView } from '../../views/admin/EnrollmentsView.js';
import { EnrollmentModel, CourseClassModel, UserModel } from '../../models/index.js';
import { DataTable } from '../../ui/dataTable.js';
import { Pagination } from '../../ui/pagination.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';
import { formatDate, formatCurrency, renderStatusBadge } from '../../core/utils.js';
import { icon } from '../../ui/icons.js';

export class EnrollmentsController {
  static async render(params, query, container) {
    this.container = container;
    this.enrollments = [];
    this.classes = [];
    this.students = [];
    this.filteredData = [];
    this.currentPage = 1;
    this.pageSize = 10;
    this.classFilter = 'all';
    this.statusFilter = 'all';
    this.searchTerm = '';

    await this.initData();
  }

  static async initData() {
    const [allEnrollments, allClasses, allStudents] = await Promise.all([
      EnrollmentModel.getAll(),
      CourseClassModel.getAll(),
      UserModel.getStudents()
    ]);

    this.classes = allClasses;
    this.students = allStudents;

    // Join thông tin chi tiết
    this.enrollments = allEnrollments.map(e => {
      const student = allStudents.find(s => s.id === e.user_id) || {};
      const cls = allClasses.find(c => c.id === e.class_id) || {};
      return {
        ...e,
        student_name: student.full_name || 'Học viên',
        student_email: student.email || '',
        class_code: cls.class_code || 'Lớp',
        class_name: cls.class_name || '',
        price: cls.price || 0
      };
    });

    this.container.innerHTML = EnrollmentsView.renderLayout(this.classes);
    this.applyFilters();
    this.bindEvents();
  }

  static applyFilters() {
    let result = [...this.enrollments];

    if (this.classFilter !== 'all') {
      result = result.filter(e => String(e.class_id) === String(this.classFilter));
    }

    if (this.statusFilter !== 'all') {
      result = result.filter(e => e.status === this.statusFilter);
    }

    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      result = result.filter(e => 
        (e.student_name && e.student_name.toLowerCase().includes(q)) ||
        (e.student_email && e.student_email.toLowerCase().includes(q)) ||
        (e.class_code && e.class_code.toLowerCase().includes(q))
      );
    }

    this.filteredData = result;
    this.currentPage = 1;
    this.renderTable();
  }

  static renderTable() {
    const tableOutlet = this.container.querySelector('#enrollments-table-outlet');
    const paginationOutlet = this.container.querySelector('#enrollments-pagination-outlet');
    if (!tableOutlet || !paginationOutlet) return;

    const start = (this.currentPage - 1) * this.pageSize;
    const pageData = this.filteredData.slice(start, start + this.pageSize);

    this.dataTable = new DataTable({
      columns: [
        {
          key: 'student_name',
          label: 'Học viên',
          sortable: true,
          render: (val, row) => `
            <div>
              <strong>${val}</strong>
              <div style="font-size: 0.75rem; color: var(--text-muted);">${row.student_email}</div>
            </div>
          `
        },
        {
          key: 'class_code',
          label: 'Lớp học ghi danh',
          sortable: true,
          render: (val, row) => `
            <div>
              <strong style="color: var(--primary-600);">${val}</strong>
              <div style="font-size: 0.75rem; color: var(--text-secondary);">${row.class_name}</div>
            </div>
          `
        },
        {
          key: 'price',
          label: 'Học phí ước tính',
          sortable: true,
          render: (val) => `<strong>${formatCurrency(val)}</strong>`
        },
        {
          key: 'enrolled_at',
          label: 'Ngày ghi danh',
          sortable: true,
          render: (val) => formatDate(val)
        },
        {
          key: 'status',
          label: 'Trạng thái',
          sortable: true,
          render: (val) => renderStatusBadge(val)
        },
        {
          key: 'actions',
          label: 'Cập nhật trạng thái',
          render: (_, row) => `
            <div style="display: flex; gap: 4px;">
              ${row.status === 'active' ? `
                <button type="button" class="btn btn-secondary btn-sm btn-mark-completed" data-id="${row.id}" title="Hoàn tất khóa học">
                  Hoàn tất
                </button>
                <button type="button" class="btn btn-danger-soft btn-sm btn-mark-cancelled" data-id="${row.id}" title="Hủy đăng ký">
                  Hủy
                </button>
              ` : `
                <button type="button" class="btn btn-secondary btn-sm btn-mark-active" data-id="${row.id}" title="Kích hoạt lại">
                  Kích hoạt lại
                </button>
              `}
            </div>
          `
        }
      ],
      data: pageData,
      emptyMessage: 'Không tìm thấy lượt ghi danh nào.'
    });

    tableOutlet.innerHTML = this.dataTable.render();
    this.dataTable.attachEvents(tableOutlet);

    this.pagination = new Pagination({
      totalItems: this.filteredData.length,
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

  static bindEvents() {
    const classFilter = this.container.querySelector('#enrollment-class-filter');
    const statusFilter = this.container.querySelector('#enrollment-status-filter');
    const searchInput = this.container.querySelector('#enrollment-search-input');
    const resetBtn = this.container.querySelector('#btn-reset-enrollment-filters');
    const exportBtn = this.container.querySelector('#btn-export-csv');

    if (classFilter) {
      classFilter.addEventListener('change', (e) => {
        this.classFilter = e.target.value;
        this.applyFilters();
      });
    }

    if (statusFilter) {
      statusFilter.addEventListener('change', (e) => {
        this.statusFilter = e.target.value;
        this.applyFilters();
      });
    }

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.applyFilters();
      });
    }

    if (resetBtn) {
      resetBtn.addEventListener('click', () => {
        this.classFilter = 'all';
        this.statusFilter = 'all';
        this.searchTerm = '';
        classFilter.value = 'all';
        statusFilter.value = 'all';
        searchInput.value = '';
        this.applyFilters();
        toast.info('Đã đặt lại bộ lọc đăng ký.');
      });
    }

    if (exportBtn) {
      exportBtn.addEventListener('click', () => this.exportCSV());
    }
  }

  static bindRowEvents(container) {
    const completeBtns = container.querySelectorAll('.btn-mark-completed');
    completeBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        await EnrollmentModel.update(id, { status: 'completed' });
        toast.success('Đã cập nhật trạng thái: Đã hoàn tất khóa học!');
        await this.initData();
      });
    });

    const cancelBtns = container.querySelectorAll('.btn-mark-cancelled');
    cancelBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const ok = await confirmDialog({
          title: 'Hủy đăng ký học viên',
          message: 'Bạn có chắc chắn muốn hủy đăng ký của học viên này không?',
          confirmText: 'Xác nhận hủy',
          isDanger: true
        });

        if (ok) {
          await EnrollmentModel.update(id, { status: 'cancelled' });
          toast.success('Đã chuyển trạng thái đăng ký sang Hủy!');
          await this.initData();
        }
      });
    });

    const activeBtns = container.querySelectorAll('.btn-mark-active');
    activeBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        await EnrollmentModel.update(id, { status: 'active' });
        toast.success('Đã kích hoạt lại trạng thái học tập!');
        await this.initData();
      });
    });
  }

  // Xuất file CSV tiếng Việt chuẩn UTF-8
  static exportCSV() {
    if (this.filteredData.length === 0) {
      toast.warning('Không có dữ liệu để xuất file.');
      return;
    }

    const headers = ['Mã ĐK', 'Học viên', 'Email', 'Mã lớp', 'Tên lớp', 'Học phí ước tính (VND)', 'Ngày ghi danh', 'Trạng thái'];
    const rows = this.filteredData.map(e => [
      e.id,
      `"${(e.student_name || '').replace(/"/g, '""')}"`,
      `"${(e.student_email || '').replace(/"/g, '""')}"`,
      `"${e.class_code}"`,
      `"${(e.class_name || '').replace(/"/g, '""')}"`,
      e.price || 0,
      `"${formatDate(e.enrolled_at)}"`,
      `"${e.status}"`
    ]);

    const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `Dang_ky_EnglishLearning_${Date.now()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    toast.success('Đã xuất file CSV thành công!');
  }
}
