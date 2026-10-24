/**
 * ClassDetailView - Giao diện Chi tiết Lớp học (4 Tabs: Giáo viên, Lịch học, Học sinh, Buổi học)
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, formatDate, renderStatusBadge, escapeHtml } from '../../core/utils.js';

export class ClassDetailView {
  static render({ cls, activeTab = 'teachers' }) {
    const dayNames = { 1: 'Chủ Nhật', 2: 'Thứ 2', 3: 'Thứ 3', 4: 'Thứ 4', 5: 'Thứ 5', 6: 'Thứ 6', 7: 'Thứ 7' };

    return `
      <!-- Breadcrumb -->
      <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem;">
        <a href="#/classes" class="btn btn-secondary btn-sm">
          ${icon('arrow-left', { size: 14 })}
          <span>Trở về danh sách lớp</span>
        </a>
        <span style="color: var(--text-muted);">/</span>
        <span style="font-weight: 600; font-size: 0.875rem;">Lớp [${cls.class_code}] ${escapeHtml(cls.class_name)}</span>
      </div>

      <!-- Class Header Summary -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1.5rem;">
        <div style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem;">
          <div>
            <div style="display: flex; gap: 8px; align-items: center; margin-bottom: 6px;">
              <span class="badge badge-primary">${escapeHtml(cls.class_code)}</span>
              ${renderStatusBadge(cls.status)}
              <span style="font-size: 0.8125rem; color: var(--text-muted);">Khóa: <strong>${escapeHtml(cls.course ? cls.course.title : '')}</strong></span>
            </div>
            <h2 style="font-size: 1.375rem; font-weight: 800; margin-bottom: 4px;">${escapeHtml(cls.class_name)}</h2>
            <div style="font-size: 0.8125rem; color: var(--text-secondary);">
              Thời gian: <strong>${formatDate(cls.start_date)}</strong> đến <strong>${formatDate(cls.end_date)}</strong> • 
              Sĩ số hiện tại: <strong>${cls.enrolled_count}/${cls.max_students}</strong> học viên • 
              Học phí: <strong>${formatCurrency(cls.price)}</strong>
            </div>
          </div>

          <div style="display: flex; gap: 8px;">
            <button type="button" class="btn btn-secondary btn-sm" id="btn-edit-class-info">
              ${icon('edit', { size: 14 })}
              <span>Sửa thông tin lớp</span>
            </button>
            <a href="#/schedules" class="btn btn-primary btn-sm">
              ${icon('calendar', { size: 14 })}
              <span>Xem trên TKB</span>
            </a>
          </div>
        </div>
      </div>

      <!-- 4 Tabs Navigation -->
      <div class="tabs-nav">
        <button type="button" class="tab-btn ${activeTab === 'teachers' ? 'active' : ''}" data-tab="teachers">
          ${icon('graduation-cap', { size: 16 })}
          <span>Giáo viên phụ trách</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'schedules' ? 'active' : ''}" data-tab="schedules">
          ${icon('calendar', { size: 16 })}
          <span>Lịch học cố định</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'students' ? 'active' : ''}" data-tab="students">
          ${icon('users', { size: 16 })}
          <span>Danh sách học sinh (${cls.enrolled_count})</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'lessons' ? 'active' : ''}" data-tab="lessons">
          ${icon('book', { size: 16 })}
          <span>Tiến độ buổi học</span>
        </button>
      </div>

      <!-- Tab Content Outlet -->
      <div id="class-tab-content">
        ${activeTab === 'teachers' ? this.renderTeachersTab(cls) : ''}
        ${activeTab === 'schedules' ? this.renderSchedulesTab(cls, dayNames) : ''}
        ${activeTab === 'students' ? this.renderStudentsTab(cls) : ''}
        ${activeTab === 'lessons' ? this.renderLessonsTab(cls) : ''}
      </div>
    `;
  }

  // TAB 1: GIÁO VIÊN
  static renderTeachersTab(cls) {
    const teachers = cls.assigned_teachers || [];
    return `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Phân công giảng dạy</h3>
          <button type="button" class="btn btn-primary btn-sm" id="btn-assign-teacher">
            ${icon('plus', { size: 14 })}
            <span>Phân công giáo viên</span>
          </button>
        </div>
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Giảng viên</th>
                <th>Bằng cấp chuyên môn</th>
                <th>Vai trò phân công</th>
                <th>Thời gian gán</th>
                <th style="text-align: right;">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              ${teachers.length === 0 ? `
                <tr><td colspan="5" class="text-center p-6 text-muted">Lớp này chưa được phân công giáo viên nào.</td></tr>
              ` : ''}
              ${teachers.map(t => `
                <tr>
                  <td>
                    <div style="display: flex; align-items: center; gap: 0.75rem;">
                      <div class="avatar avatar-sm">
                        <img src="${t.avatar_url || 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150'}" alt="${t.teacher_name}">
                      </div>
                      <div>
                        <strong>${escapeHtml(t.teacher_name)}</strong>
                        <div style="font-size: 0.75rem; color: var(--text-muted);">${t.teacher_email}</div>
                      </div>
                    </div>
                  </td>
                  <td>${escapeHtml(t.qualification || '—')}</td>
                  <td>
                    <span class="badge ${t.role === 'main' ? 'badge-primary' : 'badge-neutral'}">
                      ${t.role === 'main' ? 'Giáo viên chính' : 'Trợ giảng'}
                    </span>
                  </td>
                  <td>${formatDate(t.assigned_at)}</td>
                  <td style="text-align: right;">
                    <button type="button" class="btn btn-danger-soft btn-sm btn-remove-teacher" data-teacher-id="${t.teacher_id}" data-name="${escapeHtml(t.teacher_name)}">
                      ${icon('trash', { size: 14 })}
                      <span>Gỡ phân công</span>
                    </button>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // TAB 2: LỊCH HỌC
  static renderSchedulesTab(cls, dayNames) {
    const schedules = cls.schedules || [];
    return `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Lịch học hàng tuần của lớp</h3>
          <a href="#/schedules" class="btn btn-secondary btn-sm">Xem toàn bộ TKB</a>
        </div>
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Thứ trong tuần</th>
                <th>Khung giờ</th>
                <th>Phòng học</th>
                <th>Vị trí</th>
              </tr>
            </thead>
            <tbody>
              ${schedules.length === 0 ? `
                <tr><td colspan="4" class="text-center p-6 text-muted">Lớp này chưa được xếp lịch học cụ thể. Hãy vào mục "Xếp lịch (TKB)".</td></tr>
              ` : ''}
              ${schedules.map(s => `
                <tr>
                  <td><strong>${dayNames[s.day_of_week] || `Thứ ${s.day_of_week}`}</strong></td>
                  <td>
                    <span class="badge badge-info">${s.start_time} - ${s.end_time}</span>
                    <small style="color: var(--text-muted); margin-left: 4px;">(${s.slot_name})</small>
                  </td>
                  <td><strong>${s.classroom_name}</strong></td>
                  <td>${s.location || '—'}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // TAB 3: HỌC SINH
  static renderStudentsTab(cls) {
    const students = cls.enrolled_students || [];
    return `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Học viên ghi danh (${students.length} / ${cls.max_students})</h3>
          <span class="badge badge-info">${cls.max_students - students.length} chỗ còn lại</span>
        </div>
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Học viên</th>
                <th>Email</th>
                <th>Ngày ghi danh</th>
                <th>Trạng thái học</th>
                <th style="text-align: right;">Cập nhật trạng thái</th>
              </tr>
            </thead>
            <tbody>
              ${students.length === 0 ? `
                <tr><td colspan="5" class="text-center p-6 text-muted">Chưa có học sinh nào đăng ký lớp này.</td></tr>
              ` : ''}
              ${students.map(s => `
                <tr>
                  <td>
                    <div style="display: flex; align-items: center; gap: 0.75rem;">
                      <div class="avatar avatar-xs">${s.student_name ? s.student_name.charAt(0) : 'U'}</div>
                      <strong>${escapeHtml(s.student_name)}</strong>
                    </div>
                  </td>
                  <td>${s.student_email}</td>
                  <td>${formatDate(s.enrolled_at)}</td>
                  <td>${renderStatusBadge(s.status)}</td>
                  <td style="text-align: right;">
                    <select class="form-control select-enrollment-status" data-enrollment-id="${s.id}" style="width: auto; display: inline-block; padding: 2px 28px 2px 8px; font-size: 0.75rem; height: 32px;">
                      <option value="active" ${s.status === 'active' ? 'selected' : ''}>Đang học (Active)</option>
                      <option value="completed" ${s.status === 'completed' ? 'selected' : ''}>Đã hoàn thành</option>
                      <option value="cancelled" ${s.status === 'cancelled' ? 'selected' : ''}>Đã hủy</option>
                    </select>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // TAB 4: BUỔI HỌC (CLASS_LESSONS)
  static renderLessonsTab(cls) {
    const lessons = cls.class_lessons || [];
    return `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Danh sách các buổi học (${lessons.length})</h3>
        </div>
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Buổi</th>
                <th>Bài giảng</th>
                <th>Kỹ năng</th>
                <th>Ngày học dự kiến</th>
                <th>Trạng thái</th>
                <th>Ghi chú giảng dạy</th>
              </tr>
            </thead>
            <tbody>
              ${lessons.length === 0 ? `
                <tr><td colspan="6" class="text-center p-6 text-muted">Lớp này chưa có danh sách buổi học cụ thể.</td></tr>
              ` : ''}
              ${lessons.map((l, idx) => `
                <tr>
                  <td><strong>Buổi ${idx + 1}</strong></td>
                  <td><strong>${escapeHtml(l.lesson_title)}</strong></td>
                  <td><span class="badge badge-neutral">${l.content_type.toUpperCase()}</span></td>
                  <td>${formatDate(l.schedule_date)}</td>
                  <td>${renderStatusBadge(l.status)}</td>
                  <td><small style="color: var(--text-secondary);">${escapeHtml(l.notes || '—')}</small></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  static renderAssignTeacherModal(teachers) {
    return `
      <form id="assign-teacher-form">
        <div class="form-group">
          <label class="form-label" for="modal-assign-teacher-select">
            Chọn giảng viên <span class="required">*</span>
          </label>
          <select id="modal-assign-teacher-select" class="form-control" required>
            ${teachers.map(t => `
              <option value="${t.id}">${escapeHtml(t.full_name)} (${escapeHtml(t.qualification || 'Giảng viên')})</option>
            `).join('')}
          </select>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-assign-teacher-role">Vai trò phụ trách</label>
          <select id="modal-assign-teacher-role" class="form-control">
            <option value="main">Giáo viên chính (Main Teacher)</option>
            <option value="assistant">Trợ giảng (Teaching Assistant)</option>
          </select>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-submit-assign">Xác nhận phân công</button>
        </div>
      </form>
    `;
  }
}
