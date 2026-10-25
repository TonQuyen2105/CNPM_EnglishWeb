/**
 * UsersController - Điều phối quản lý tài khoản người dùng
 */

import { UsersView } from '../../views/admin/UsersView.js';
import { UserModel, TeacherModel } from '../../models/index.js';
import { DataTable } from '../../ui/dataTable.js';
import { Pagination } from '../../ui/pagination.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';
import { formatDate, getInitials } from '../../core/utils.js';
import { icon } from '../../ui/icons.js';

export class UsersController {
  static async render(params, query, container) {
    this.container = container;
    this.allUsers = [];
    this.filteredUsers = [];
    this.currentPage = 1;
    this.pageSize = 10;
    this.searchTerm = '';
    this.roleFilter = 'all';
    this.statusFilter = 'all';

    container.innerHTML = UsersView.renderLayout();
    await this.loadData();
    this.bindFilterEvents();
    this.bindActionEvents();
  }

  static async loadData() {
    this.allUsers = await UserModel.getAll();
    this.applyFilters();
  }

  static applyFilters() {
    let result = [...this.allUsers];

    // Lọc theo search
    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      result = result.filter(u => 
        (u.full_name && u.full_name.toLowerCase().includes(q)) ||
        (u.email && u.email.toLowerCase().includes(q))
      );
    }

    // Lọc theo role
    if (this.roleFilter !== 'all') {
      result = result.filter(u => u.role === this.roleFilter);
    }

    // Lọc theo trạng thái
    if (this.statusFilter !== 'all') {
      const activeVal = this.statusFilter === 'active' ? 1 : 0;
      result = result.filter(u => {
        const isAct = u.is_active === 1 || u.is_active === true ? 1 : 0;
        return isAct === activeVal;
      });
    }

    this.filteredUsers = result;
    this.currentPage = 1;
    this.renderTableAndPagination();
  }

  static renderTableAndPagination() {
    const tableOutlet = this.container.querySelector('#users-table-outlet');
    const paginationOutlet = this.container.querySelector('#users-pagination-outlet');
    if (!tableOutlet || !paginationOutlet) return;

    // Cắt theo trang
    const start = (this.currentPage - 1) * this.pageSize;
    const pageData = this.filteredUsers.slice(start, start + this.pageSize);

    // Cấu hình DataTable
    this.dataTable = new DataTable({
      columns: [
        {
          key: 'full_name',
          label: 'Họ tên & Email',
          sortable: true,
          render: (val, row) => `
            <div style="display: flex; align-items: center; gap: 0.75rem;">
              <div class="avatar avatar-sm">
                ${row.avatar_url ? `<img src="${row.avatar_url}" alt="${val}">` : getInitials(val)}
              </div>
              <div>
                <strong>${val}</strong>
                <div style="font-size: 0.75rem; color: var(--text-muted);">${row.email}</div>
              </div>
            </div>
          `
        },
        {
          key: 'role',
          label: 'Vai trò',
          sortable: true,
          render: (val) => UsersView.renderRoleBadge(val)
        },
        {
          key: 'is_active',
          label: 'Kích hoạt',
          sortable: true,
          render: (val, row) => `
            <label class="form-switch" title="Chuyển trạng thái hoạt động">
              <input type="checkbox" class="user-status-switch" data-id="${row.id}" ${val === 1 || val === true ? 'checked' : ''}>
              <span class="switch-slider"></span>
            </label>
          `
        },
        {
          key: 'created_at',
          label: 'Ngày tạo',
          sortable: true,
          render: (val) => formatDate(val)
        },
        {
          key: 'actions',
          label: 'Thao tác',
          render: (_, row) => `
            <div style="display: flex; gap: 6px;">
              <button type="button" class="btn btn-secondary btn-sm btn-edit-user" data-id="${row.id}" title="Chỉnh sửa">
                ${icon('edit', { size: 14 })}
              </button>
              <button type="button" class="btn btn-secondary btn-sm btn-reset-pwd" data-id="${row.id}" title="Đặt lại mật khẩu">
                ${icon('lock', { size: 14 })}
              </button>
              <button type="button" class="btn btn-danger-soft btn-sm btn-delete-user" data-id="${row.id}" title="Khóa/Xóa tài khoản">
                ${icon('trash', { size: 14 })}
              </button>
            </div>
          `
        }
      ],
      data: pageData,
      emptyMessage: 'Không tìm thấy người dùng nào phù hợp với bộ lọc.'
    });

    tableOutlet.innerHTML = this.dataTable.render();
    this.dataTable.attachEvents(tableOutlet);

    // Pagination
    this.pagination = new Pagination({
      totalItems: this.filteredUsers.length,
      pageSize: this.pageSize,
      currentPage: this.currentPage,
      onPageChange: (newPage) => {
        this.currentPage = newPage;
        this.renderTableAndPagination();
      }
    });

    paginationOutlet.innerHTML = this.pagination.render();
    this.pagination.attachEvents(paginationOutlet);

    // Gắn sự kiện các nút trên từng hàng
    this.bindRowEvents(tableOutlet);
  }

  static bindFilterEvents() {
    const searchInput = this.container.querySelector('#user-search-input');
    const roleFilter = this.container.querySelector('#user-role-filter');
    const statusFilter = this.container.querySelector('#user-status-filter');
    const resetBtn = this.container.querySelector('#btn-reset-user-filters');

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.applyFilters();
      });
    }

    if (roleFilter) {
      roleFilter.addEventListener('change', (e) => {
        this.roleFilter = e.target.value;
        this.applyFilters();
      });
    }

    if (statusFilter) {
      statusFilter.addEventListener('change', (e) => {
        this.statusFilter = e.target.value;
        this.applyFilters();
      });
    }

    if (resetBtn) {
      resetBtn.addEventListener('click', () => {
        this.searchTerm = '';
        this.roleFilter = 'all';
        this.statusFilter = 'all';
        searchInput.value = '';
        roleFilter.value = 'all';
        statusFilter.value = 'all';
        this.applyFilters();
        toast.info('Đã đặt lại bộ lọc người dùng.');
      });
    }
  }

  static bindActionEvents() {
    const addBtn = this.container.querySelector('#btn-add-user');
    if (addBtn) {
      addBtn.addEventListener('click', () => this.openUserModal(null));
    }
  }

  static bindRowEvents(container) {
    // 1. Switch khóa/mở is_active
    const switches = container.querySelectorAll('.user-status-switch');
    switches.forEach(sw => {
      sw.addEventListener('change', async (e) => {
        const userId = sw.getAttribute('data-id');
        const newStatus = sw.checked ? 1 : 0;
        await UserModel.update(userId, { is_active: newStatus });
        toast.success(`Đã ${newStatus ? 'kích hoạt' : 'tạm khóa'} tài khoản thành công!`);
        await this.loadData();
      });
    });

    // 2. Nút sửa
    const editBtns = container.querySelectorAll('.btn-edit-user');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const user = await UserModel.getById(id);
        if (user) this.openUserModal(user);
      });
    });

    // 3. Reset mật khẩu
    const resetBtns = container.querySelectorAll('.btn-reset-pwd');
    resetBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const user = await UserModel.getById(id);
        if (!user) return;

        const ok = await confirmDialog({
          title: 'Đặt lại mật khẩu',
          message: `Bạn có chắc muốn đặt lại mật khẩu cho tài khoản "${user.email}" về mặc định "123456"?`,
          confirmText: 'Đặt lại mật khẩu'
        });

        if (ok) {
          await UserModel.update(id, { password: '123456' });
          toast.success(`Đã đặt lại mật khẩu cho ${user.full_name} về "123456"!`);
        }
      });
    });

    // 4. Xóa / Khóa tài khoản
    const delBtns = container.querySelectorAll('.btn-delete-user');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const user = await UserModel.getById(id);
        if (!user) return;

        const ok = await confirmDialog({
          title: 'Xóa hoặc khóa tài khoản',
          message: `Bạn có chắc chắn muốn khóa tài khoản "${user.full_name}" (${user.email})? Tài khoản sẽ chuyển sang trạng thái ngưng hoạt động.`,
          confirmText: 'Khóa tài khoản',
          isDanger: true
        });

        if (ok) {
          await UserModel.softDelete(id);
          toast.success(`Đã khóa tài khoản ${user.full_name}!`);
          await this.loadData();
        }
      });
    });
  }

  static openUserModal(user = null) {
    const isEdit = !!user;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa tài khoản người dùng' : 'Thêm tài khoản người dùng mới',
      body: UsersView.renderUserModal(user),
      maxWidth: '540px'
    });

    const form = document.getElementById('user-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const name = document.getElementById('modal-user-name').value.trim();
      const email = document.getElementById('modal-user-email').value.trim();
      const role = document.getElementById('modal-user-role').value;
      const status = parseInt(document.getElementById('modal-user-status').value, 10);
      const avatar = document.getElementById('modal-user-avatar').value.trim();

      let hasErr = false;
      if (!name) {
        document.getElementById('err-modal-name').style.display = 'block';
        hasErr = true;
      }
      if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        document.getElementById('err-modal-email').style.display = 'block';
        hasErr = true;
      }
      if (hasErr) return;

      const submitBtn = document.getElementById('btn-save-user');
      submitBtn.classList.add('loading');
      submitBtn.disabled = true;

      try {
        if (isEdit) {
          await UserModel.update(user.id, {
            full_name: name,
            email,
            role,
            is_active: status,
            avatar_url: avatar || user.avatar_url
          });
          toast.success('Cập nhật tài khoản người dùng thành công!');
        } else {
          // Kiểm tra email trùng
          const existing = await UserModel.getByEmail(email);
          if (existing) {
            toast.error('Email này đã được sử dụng bởi một tài khoản khác!');
            submitBtn.classList.remove('loading');
            submitBtn.disabled = false;
            return;
          }

          const newUser = await UserModel.create({
            full_name: name,
            email,
            password: '123456',
            role,
            is_active: status,
            avatar_url: avatar || `https://images.unsplash.com/photo-${Date.now()}?w=150`
          });

          // Nếu tạo user role=teacher -> Tự tạo hồ sơ teachers
          if (role === 'teacher') {
            await TeacherModel.create({
              user_id: newUser.id,
              bio: 'Giảng viên chuyên môn tại trung tâm EnglishLearning.',
              qualification: 'Chứng chỉ giảng dạy tiếng Anh',
              is_verified: 0
            });
            toast.info('Đã tự động khởi tạo hồ sơ giảng viên chờ xác minh.');
          }

          toast.success('Đã tạo tài khoản người dùng mới thành công!');
        }

        modal.close(modalId);
        await this.loadData();
      } catch (err) {
        console.error(err);
        toast.error('Có lỗi xảy ra khi lưu người dùng.');
      }
    });
  }
}
