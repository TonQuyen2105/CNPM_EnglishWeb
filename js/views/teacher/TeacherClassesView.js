/**
 * TeacherClassesView - Giao diện Quản lý Lớp của tôi & Chi tiết lớp học
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherClassesView {
  /**
   * Render danh sách các lớp phụ trách
   * @param {Array} classes 
   * @returns {string}
   */
  static renderList(classes = []) {
    return `
      <div class="teacher-classes">
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Lớp Học Phụ Trách (${classes.length})
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Danh sách các lớp bạn được phân công làm Giảng viên chính hoặc Trợ giảng
            </p>
          </div>
        </div>

        ${classes.length > 0 ? `
          <div class="grid grid-cols-3 gap-6">
            ${classes.map(cls => `
              <div class="card" style="display: flex; flex-direction: column; overflow: hidden; border: 1px solid var(--border-color); border-radius: var(--radius-lg); transition: all var(--transition-fast);" class="hover-shadow">
                <!-- Thumbnail hoặc Banner -->
                <div style="height: 120px; background: linear-gradient(135deg, #1E293B, #0F172A); position: relative; padding: 1rem; display: flex; flex-direction: column; justify-content: space-between;">
                  <div style="display: flex; justify-content: space-between; align-items: center;">
                    <span class="badge ${cls.role === 'main' ? 'badge-primary' : 'badge-secondary'}" style="font-weight: 700; font-size: 0.75rem;">
                      ${cls.role === 'main' ? 'Giảng viên chính' : 'Trợ giảng'}
                    </span>
                    <span class="badge ${cls.status === 'in_progress' ? 'badge-success' : 'badge-warning'}" style="font-size: 0.7rem;">
                      ${cls.status === 'in_progress' ? 'Đang diễn ra' : (cls.status === 'upcoming' ? 'Sắp mở' : 'Đã kết thúc')}
                    </span>
                  </div>
                  <div>
                    <div style="color: white; font-weight: 800; font-size: 1.125rem;">
                      ${escapeHtml(cls.class_code)}
                    </div>
                    <div style="color: #94A3B8; font-size: 0.75rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                      ${escapeHtml(cls.course_title)}
                    </div>
                  </div>
                </div>

                <!-- Body card -->
                <div style="padding: 1.25rem; flex: 1; display: flex; flex-direction: column; justify-content: space-between;">
                  <div>
                    <!-- Lịch học rút gọn -->
                    <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 0.75rem; display: flex; align-items: center; gap: 6px;">
                      <span>${icon('calendar', { size: 14 })}</span>
                      <span>
                        ${cls.schedules && cls.schedules.length > 0 
                          ? cls.schedules.map(s => `Thứ ${s.day_of_week === 1 ? 'CN' : s.day_of_week} (${s.time_range})`).join(', ') 
                          : 'Chưa xếp lịch'}
                      </span>
                    </div>

                    <!-- Phòng học -->
                    <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 1rem; display: flex; align-items: center; gap: 6px;">
                      <span>${icon('map-pin', { size: 14 })}</span>
                      <span>${cls.schedules && cls.schedules[0] ? cls.schedules[0].room_name : 'Phòng học cơ sở'}</span>
                    </div>

                    <!-- Tiến độ buổi học -->
                    <div style="margin-bottom: 1rem;">
                      <div style="display: flex; justify-content: space-between; font-size: 0.75rem; margin-bottom: 4px;">
                        <span style="color: var(--text-secondary);">Tiến độ chương trình:</span>
                        <strong style="color: var(--primary-600);">${cls.completed_lessons}/${cls.total_lessons || 1} buổi (${cls.progress_percent}%)</strong>
                      </div>
                      <div style="width: 100%; height: 6px; background: var(--neutral-200); border-radius: var(--radius-full); overflow: hidden;">
                        <div style="width: ${cls.progress_percent}%; height: 100%; background: var(--primary-600); border-radius: var(--radius-full);"></div>
                      </div>
                    </div>

                    <!-- Sĩ số -->
                    <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.8125rem; padding: 0.5rem 0.75rem; background: var(--neutral-50); border-radius: var(--radius-md); margin-bottom: 1rem;">
                      <span style="color: var(--text-secondary);">Sĩ số học viên:</span>
                      <strong style="color: var(--text-primary); font-size: 0.875rem;">${cls.enrolled_count} / ${cls.max_students}</strong>
                    </div>
                  </div>

                  <!-- Nút hành động -->
                  <a href="#/classes/${cls.id}" class="btn btn-primary" style="width: 100%; justify-content: center; font-size: 0.8125rem;">
                    Vào chi tiết lớp học &rarr;
                  </a>
                </div>
              </div>
            `).join('')}
          </div>
        ` : `
          <div class="card" style="padding: 3rem; text-align: center; color: var(--text-muted);">
            <div style="margin-bottom: 1rem; color: var(--neutral-400);">${icon('layers', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Bạn chưa có lớp phụ trách</h3>
            <p style="font-size: 0.875rem;">Khi Quản trị viên phân công giảng dạy cho bạn, danh sách lớp học sẽ hiển thị tại đây.</p>
          </div>
        `}
      </div>
    `;
  }

  /**
   * Render trang Chi tiết Lớp học gồm 3 Tabs (Học sinh, Buổi học, Lịch học)
   * @param {Object} data 
   * @returns {string}
   */
  static renderDetail(data) {
    const { cls, activeTab, students, lessons, schedules, courseLessons } = data;

    return `
      <div class="teacher-class-detail">
        <!-- Header -->
        <div style="margin-bottom: 1.25rem;">
          <a href="#/classes" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none; display: inline-flex; align-items: center; gap: 4px; margin-bottom: 0.5rem;">
            &larr; Quay lại danh sách lớp
          </a>
          <div style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem;">
            <div>
              <div style="display: flex; align-items: center; gap: 8px;">
                <h1 style="margin: 0; font-size: 1.5rem; font-weight: 800; color: var(--text-primary);">
                  ${escapeHtml(cls.class_name)} (${escapeHtml(cls.class_code)})
                </h1>
                <span class="badge ${cls.status === 'in_progress' ? 'badge-success' : 'badge-primary'}">
                  ${cls.status === 'in_progress' ? 'Đang học' : cls.status}
                </span>
                <span class="badge badge-info">
                  ${cls.role === 'main' ? 'Giảng viên chính' : 'Trợ giảng'}
                </span>
              </div>
              <p style="margin: 4px 0 0 0; color: var(--text-secondary); font-size: 0.875rem;">
                Khóa: <strong>${escapeHtml(cls.course_title || '')}</strong> &bull; Sĩ số: <strong>${students.length}/${cls.max_students}</strong> học viên &bull; Hoàn thành: <strong>${cls.completed_lessons}/${cls.total_lessons || 1}</strong> buổi
              </p>
            </div>
          </div>
        </div>

        <!-- 3 Tabs Navigation -->
        <div class="tabs" style="margin-bottom: 1.5rem; border-bottom: 1px solid var(--border-color); display: flex; gap: 0.5rem;">
          <button type="button" class="tab-btn ${activeTab === 'students' ? 'active' : ''}" data-tab="students" style="padding: 0.75rem 1.25rem; font-weight: 700; font-size: 0.875rem; border: none; background: none; cursor: pointer; border-bottom: 2px solid ${activeTab === 'students' ? 'var(--primary-600)' : 'transparent'}; color: ${activeTab === 'students' ? 'var(--primary-600)' : 'var(--text-secondary)'};">
            <span style="margin-right: 6px;">${icon('users', { size: 16 })}</span>
            Danh sách Học sinh (${students.length})
          </button>

          <button type="button" class="tab-btn ${activeTab === 'lessons' ? 'active' : ''}" data-tab="lessons" style="padding: 0.75rem 1.25rem; font-weight: 700; font-size: 0.875rem; border: none; background: none; cursor: pointer; border-bottom: 2px solid ${activeTab === 'lessons' ? 'var(--primary-600)' : 'transparent'}; color: ${activeTab === 'lessons' ? 'var(--primary-600)' : 'var(--text-secondary)'};">
            <span style="margin-right: 6px;">${icon('book-open', { size: 16 })}</span>
            Buổi học & Tiến trình (${lessons.length})
          </button>

          <button type="button" class="tab-btn ${activeTab === 'schedules' ? 'active' : ''}" data-tab="schedules" style="padding: 0.75rem 1.25rem; font-weight: 700; font-size: 0.875rem; border: none; background: none; cursor: pointer; border-bottom: 2px solid ${activeTab === 'schedules' ? 'var(--primary-600)' : 'transparent'}; color: ${activeTab === 'schedules' ? 'var(--primary-600)' : 'var(--text-secondary)'};">
            <span style="margin-right: 6px;">${icon('calendar', { size: 16 })}</span>
            Lịch học hàng tuần
          </button>
        </div>

        <!-- Tab 1: Học sinh -->
        <div class="tab-content" id="tab-students" style="display: ${activeTab === 'students' ? 'block' : 'none'};">
          <div class="card" style="padding: 0; overflow: hidden;">
            <div style="padding: 1rem 1.25rem; border-bottom: 1px solid var(--border-color); display: flex; justify-content: space-between; align-items: center;">
              <h3 style="margin: 0; font-size: 0.9375rem; font-weight: 700;">Học viên trong lớp</h3>
              <div style="font-size: 0.8125rem; color: var(--text-muted);">
                Theo dõi tiến độ hoàn thành bài và điểm trung bình
              </div>
            </div>

            <div class="table-responsive">
              <table class="table">
                <thead>
                  <tr>
                    <th>Học viên</th>
                    <th>Email</th>
                    <th>Ngày ghi danh</th>
                    <th>Tiến độ bài học</th>
                    <th>Điểm TB bài thi</th>
                    <th>Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  ${students.length > 0 ? students.map(s => `
                    <tr>
                      <td>
                        <div style="display: flex; align-items: center; gap: 8px;">
                          <div class="avatar avatar-sm" style="background: var(--primary-100); color: var(--primary-700); font-weight: 700;">
                            ${escapeHtml(s.student_name.slice(0, 2).toUpperCase())}
                          </div>
                          <strong>${escapeHtml(s.student_name)}</strong>
                        </div>
                      </td>
                      <td style="color: var(--text-secondary); font-size: 0.8125rem;">${escapeHtml(s.student_email)}</td>
                      <td style="font-size: 0.8125rem;">${formatDate(s.enrolled_at)}</td>
                      <td>
                        <div style="display: flex; align-items: center; gap: 8px;">
                          <div style="width: 80px; height: 6px; background: var(--neutral-200); border-radius: var(--radius-full); overflow: hidden;">
                            <div style="width: ${s.progress_percent}%; height: 100%; background: var(--success-main);"></div>
                          </div>
                          <span style="font-size: 0.75rem; font-weight: 600;">${s.completed_count}/${s.total_course_lessons || 1}</span>
                        </div>
                      </td>
                      <td>
                        ${s.avg_score !== null 
                          ? `<span class="badge ${s.avg_score >= 50 ? 'badge-success' : 'badge-danger'}" style="font-weight: 700;">${s.avg_score} đ</span>` 
                          : '<span style="color: var(--text-muted); font-size: 0.75rem;">Chưa thi</span>'}
                      </td>
                      <td>
                        <span class="badge badge-success">Đang theo học</span>
                      </td>
                    </tr>
                  `).join('') : `
                    <tr>
                      <td colspan="6" style="text-align: center; padding: 2rem; color: var(--text-muted);">
                        Lớp học chưa có học viên nào ghi danh.
                      </td>
                    </tr>
                  `}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Tab 2: Buổi học (class_lessons) -->
        <div class="tab-content" id="tab-lessons" style="display: ${activeTab === 'lessons' ? 'block' : 'none'};">
          <div class="card" style="padding: 1.25rem 1.5rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
              <div>
                <h3 style="margin: 0; font-size: 1rem; font-weight: 700;">Tiến trình các buổi học của lớp</h3>
                <p style="margin: 2px 0 0 0; font-size: 0.8125rem; color: var(--text-secondary);">
                  Cập nhật trạng thái buổi học (Đã học / Sắp tới / Hủy), ghi chú nhận xét và bài học liên kết
                </p>
              </div>
            </div>

            ${lessons.length > 0 ? `
              <div class="timeline" style="display: flex; flex-direction: column; gap: 1rem;">
                ${lessons.map((item, idx) => `
                  <div style="display: flex; gap: 1rem; align-items: flex-start; padding: 1rem; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: ${item.status === 'completed' ? '#F0FDF4' : 'var(--neutral-50)'};">
                    <!-- Thứ tự buổi -->
                    <div style="min-width: 50px; height: 50px; border-radius: var(--radius-md); background: ${item.status === 'completed' ? 'var(--success-main)' : 'var(--primary-600)'}; color: white; display: flex; flex-direction: column; align-items: center; justify-content: center; font-weight: 800;">
                      <span style="font-size: 0.65rem; text-transform: uppercase;">Buổi</span>
                      <span style="font-size: 1.125rem; line-height: 1;">${idx + 1}</span>
                    </div>

                    <!-- Thông tin buổi học -->
                    <div style="flex: 1;">
                      <div style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.5rem;">
                        <div>
                          <strong style="font-size: 0.9375rem; color: var(--text-primary);">
                            ${escapeHtml(item.lesson_title)}
                          </strong>
                          <span class="badge badge-info" style="font-size: 0.7rem; margin-left: 6px;">
                            ${escapeHtml(item.content_type)}
                          </span>
                        </div>
                        <div style="font-size: 0.8125rem; color: var(--text-muted); display: flex; align-items: center; gap: 6px;">
                          <span>${icon('calendar', { size: 14 })}</span>
                          <strong>${item.schedule_date ? formatDate(item.schedule_date) : 'Chưa định ngày'}</strong>
                        </div>
                      </div>

                      <!-- Ghi chú của giáo viên -->
                      <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 0.75rem;">
                        <strong>Ghi chú giáo viên:</strong> 
                        <span id="notes-text-${item.id}">${escapeHtml(item.notes || 'Chưa có ghi chú nào.')}</span>
                      </div>

                      <!-- Toolbar hành động của buổi -->
                      <div style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem; padding-top: 0.5rem; border-top: 1px dashed var(--border-color);">
                        <div style="display: flex; align-items: center; gap: 6px;">
                          <span style="font-size: 0.75rem; font-weight: 600; color: var(--text-secondary);">Trạng thái:</span>
                          <select class="form-control form-control-sm select-session-status" data-session-id="${item.id}" style="width: 140px; font-size: 0.75rem; padding: 2px 6px;">
                            <option value="scheduled" ${item.status === 'scheduled' ? 'selected' : ''}>⏳ Sắp tới</option>
                            <option value="completed" ${item.status === 'completed' ? 'selected' : ''}>✓ Đã hoàn thành</option>
                            <option value="cancelled" ${item.status === 'cancelled' ? 'selected' : ''}>✕ Đã hủy</option>
                          </select>
                        </div>

                        <div style="display: flex; gap: 6px;">
                          <button type="button" class="btn btn-sm btn-outline btn-edit-notes" data-session-id="${item.id}" data-notes="${escapeHtml(item.notes || '')}" style="font-size: 0.75rem; padding: 2px 8px;">
                            <span class="btn-icon-left">${icon('edit', { size: 12 })}</span> Ghi chú
                          </button>
                          <button type="button" class="btn btn-sm btn-outline btn-assign-lesson" data-session-id="${item.id}" data-lesson-id="${item.lesson_id}" style="font-size: 0.75rem; padding: 2px 8px;">
                            <span class="btn-icon-left">${icon('book-open', { size: 12 })}</span> Đổi bài
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                `).join('')}
              </div>
            ` : `
              <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
                Chưa có buổi học nào được tạo cho lớp này.
              </div>
            `}
          </div>
        </div>

        <!-- Tab 3: Lịch học (schedules) -->
        <div class="tab-content" id="tab-schedules" style="display: ${activeTab === 'schedules' ? 'block' : 'none'};">
          <div class="card" style="padding: 1.25rem 1.5rem;">
            <h3 style="margin: 0 0 1rem 0; font-size: 1rem; font-weight: 700;">Khung thời gian học định kỳ hàng tuần</h3>
            
            ${schedules.length > 0 ? `
              <div class="grid grid-cols-2 gap-4">
                ${schedules.map(s => `
                  <div style="padding: 1rem; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: var(--neutral-50);">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                      <span class="badge badge-primary" style="font-size: 0.8125rem; font-weight: 700;">
                        Thứ ${s.day_of_week === 1 ? 'Chủ Nhật' : s.day_of_week}
                      </span>
                      <strong style="color: var(--primary-600); font-size: 0.875rem;">${s.time_range}</strong>
                    </div>
                    <div style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 4px;">
                      <strong>Phòng học:</strong> ${escapeHtml(s.room_name)} (${escapeHtml(s.classroom_location || 'Tòa nhà chính')})
                    </div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">
                      Sức chứa phòng: ${s.classroom_capacity || 30} chỗ ngồi
                    </div>
                  </div>
                `).join('')}
              </div>
            ` : `
              <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
                Chưa có khung thời khóa biểu định kỳ cho lớp.
              </div>
            `}
          </div>
        </div>

      </div>
    `;
  }
}
