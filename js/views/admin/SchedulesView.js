/**
 * SchedulesView - Giao diện Thời khóa biểu Lưới (Timetable Grid) và Bộ lọc Xếp lịch
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml } from '../../core/utils.js';

export class SchedulesView {
  static renderLayout({ rooms, classes, teachers }) {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Xếp lịch đào tạo (Thời khóa biểu)</h1>
          <p class="page-header-subtitle">Lưới phân bổ ca học, phòng học và giáo viên trong tuần (Tự động chống trùng lịch)</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-schedule">
            ${icon('plus', { size: 16 })}
            <span>Thêm ca học vào TKB</span>
          </button>
        </div>
      </div>

      <!-- Toolbar Lọc TKB -->
      <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
        <div class="grid grid-cols-12 gap-4 items-center">
          <div class="col-span-4">
            <select id="schedule-room-filter" class="form-control">
              <option value="all">Tất cả phòng học (Toàn trung tâm)</option>
              ${rooms.map(r => `<option value="${r.id}">${escapeHtml(r.name)} (${r.capacity} chỗ - ${escapeHtml(r.location)})</option>`).join('')}
            </select>
          </div>
          <div class="col-span-4">
            <select id="schedule-class-filter" class="form-control">
              <option value="all">Tất cả các lớp học</option>
              ${classes.map(c => `<option value="${c.id}">[${c.class_code}] ${escapeHtml(c.class_name)}</option>`).join('')}
            </select>
          </div>
          <div class="col-span-4">
            <select id="schedule-teacher-filter" class="form-control">
              <option value="all">Tất cả giảng viên</option>
              ${teachers.map(t => `<option value="${t.id}">${escapeHtml(t.full_name)}</option>`).join('')}
            </select>
          </div>
        </div>
      </div>

      <!-- Timetable Grid Outlet -->
      <div id="timetable-grid-outlet"></div>
    `;
  }

  static renderTimetableGrid(timeSlots, schedules) {
    const days = [
      { id: 2, label: 'Thứ 2' },
      { id: 3, label: 'Thứ 3' },
      { id: 4, label: 'Thứ 4' },
      { id: 5, label: 'Thứ 5' },
      { id: 6, label: 'Thứ 6' },
      { id: 7, label: 'Thứ 7' },
      { id: 1, label: 'Chủ Nhật' }
    ];

    let headerHtml = `<div class="timetable-header-cell">Khung giờ / Thứ</div>`;
    days.forEach(d => {
      headerHtml += `<div class="timetable-header-cell">${d.label}</div>`;
    });

    let bodyRowsHtml = '';
    timeSlots.forEach(slot => {
      bodyRowsHtml += `
        <div class="timetable-time-cell">
          <strong>${slot.start_time} - ${slot.end_time}</strong>
          <div style="font-size: 0.7rem; color: var(--text-muted);">${escapeHtml(slot.name)}</div>
        </div>
      `;

      days.forEach(d => {
        // Tìm các lịch trùng slot và day
        const matched = schedules.filter(s => 
          Number(s.time_slot_id) === Number(slot.id) && Number(s.day_of_week) === Number(d.id)
        );

        if (matched.length === 0) {
          bodyRowsHtml += `<div class="timetable-slot-cell"></div>`;
        } else {
          const cardsHtml = matched.map(m => `
            <div class="timetable-class-card" style="margin-bottom: 4px; position: relative;">
              <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                <strong style="color: var(--primary-700);">${escapeHtml(m.class_code)}</strong>
                <button type="button" class="btn-icon btn-delete-schedule" data-id="${m.id}" title="Xóa ca này khỏi lịch" style="padding: 0; color: var(--danger-main);">
                  ${icon('x', { size: 12 })}
                </button>
              </div>
              <div style="font-weight: 600; font-size: 0.75rem; color: var(--text-primary); margin: 2px 0;">${escapeHtml(m.class_name)}</div>
              <div style="font-size: 0.7rem; color: var(--text-secondary);">
                📍 <strong>${escapeHtml(m.classroom_name)}</strong>
              </div>
              <div style="font-size: 0.7rem; color: var(--text-muted);">
                👤 ${escapeHtml(m.teacher_name)}
              </div>
            </div>
          `).join('');

          bodyRowsHtml += `<div class="timetable-slot-cell">${cardsHtml}</div>`;
        }
      });
    });

    return `
      <div class="card" style="padding: 1rem; overflow-x: auto;">
        <div class="timetable" style="min-width: 900px;">
          ${headerHtml}
          ${bodyRowsHtml}
        </div>
      </div>
    `;
  }

  static renderAddScheduleModal({ classes, rooms, timeSlots }) {
    return `
      <form id="add-schedule-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-sched-class">
            Chọn lớp học <span class="required">*</span>
          </label>
          <select id="modal-sched-class" class="form-control" required>
            ${classes.map(c => `
              <option value="${c.id}">[${c.class_code}] ${escapeHtml(c.class_name)} (Sĩ số: ${c.max_students})</option>
            `).join('')}
          </select>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-sched-room">
              Phòng học <span class="required">*</span>
            </label>
            <select id="modal-sched-room" class="form-control" required>
              ${rooms.map(r => `
                <option value="${r.id}">${escapeHtml(r.name)} (Sức chứa: ${r.capacity} chỗ - ${escapeHtml(r.location)})</option>
              `).join('')}
            </select>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-sched-slot">
              Khung giờ <span class="required">*</span>
            </label>
            <select id="modal-sched-slot" class="form-control" required>
              ${timeSlots.map(t => `
                <option value="${t.id}">${t.start_time} - ${t.end_time} (${escapeHtml(t.name)})</option>
              `).join('')}
            </select>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-sched-day">
            Thứ trong tuần <span class="required">*</span>
          </label>
          <select id="modal-sched-day" class="form-control" required>
            <option value="2">Thứ 2</option>
            <option value="3">Thứ 3</option>
            <option value="4">Thứ 4</option>
            <option value="5">Thứ 5</option>
            <option value="6">Thứ 6</option>
            <option value="7">Thứ 7</option>
            <option value="1">Chủ Nhật</option>
          </select>
        </div>

        <div id="schedule-conflict-warning" style="display: none; padding: 0.75rem 1rem; background: var(--danger-bg); border: 1px solid var(--danger-border); border-radius: var(--radius-md); color: var(--danger-text); font-size: 0.8125rem; margin-bottom: 1rem;"></div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-submit-schedule">Xác nhận xếp lịch</button>
        </div>
      </form>
    `;
  }
}
