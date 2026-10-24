/**
 * StudentScheduleView - Giao diện Thời khóa biểu tuần của Học viên
 */

import { icon } from '../../ui/icons.js';

export class StudentScheduleView {
  static render({
    timeSlots = [],
    schedules = [],
    viewMode = 'grid' // 'grid' | 'list'
  }) {
    const days = [
      { key: 2, label: 'Thứ Hai' },
      { key: 3, label: 'Thứ Ba' },
      { key: 4, label: 'Thứ Tư' },
      { key: 5, label: 'Thứ Năm' },
      { key: 6, label: 'Thứ Sáu' },
      { key: 7, label: 'Thứ Bảy' },
      { key: 8, label: 'Chủ Nhật' }
    ];

    return `
      <div class="student-schedule-page">
        <!-- Header & View Mode Switcher -->
        <div class="flex items-center justify-between flex-wrap gap-4" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Thời khóa biểu cá nhân</h1>
            <p class="page-header-subtitle">
              Xem lịch học hàng tuần của các lớp bạn đang theo học.
            </p>
          </div>

          <div class="flex items-center gap-2">
            <button type="button" class="btn ${viewMode === 'grid' ? 'btn-primary' : 'btn-secondary'} btn-sm" id="btn-view-grid" style="border-radius: var(--radius-full);">
              <span class="nav-icon">${icon('calendar', { size: 16 })}</span>
              <span>Dạng lưới tuần</span>
            </button>
            <button type="button" class="btn ${viewMode === 'list' ? 'btn-primary' : 'btn-secondary'} btn-sm" id="btn-view-list" style="border-radius: var(--radius-full);">
              <span class="nav-icon">${icon('menu', { size: 16 })}</span>
              <span>Dạng danh sách</span>
            </button>
          </div>
        </div>

        ${viewMode === 'grid' ? this._renderGridView(days, timeSlots, schedules) : this._renderListView(days, schedules)}
      </div>
    `;
  }

  static _renderGridView(days, timeSlots, schedules) {
    return `
      <div class="card" style="border-radius: var(--radius-xl); overflow: hidden; border: 1px solid var(--border-color); padding: 0;">
        <div style="overflow-x: auto;">
          <table class="table-timetable" style="width: 100%; border-collapse: collapse; min-width: 800px;">
            <thead>
              <tr style="background: var(--neutral-50); border-bottom: 1px solid var(--border-color);">
                <th style="padding: 1rem; width: 140px; text-align: center; font-size: 0.8125rem; font-weight: 700; color: var(--text-muted); border-right: 1px solid var(--border-color);">
                  Khung giờ
                </th>
                ${days.map(d => `
                  <th style="padding: 1rem; text-align: center; font-size: 0.8125rem; font-weight: 700; color: var(--text-primary); border-right: 1px solid var(--border-color);">
                    ${d.label}
                  </th>
                `).join('')}
              </tr>
            </thead>
            <tbody>
              ${timeSlots.map(slot => `
                <tr style="border-bottom: 1px solid var(--border-color);">
                  <td style="padding: 1rem 0.5rem; text-align: center; background: var(--neutral-50); border-right: 1px solid var(--border-color); font-size: 0.75rem;">
                    <div style="font-weight: 700; color: var(--text-primary);">${slot.name}</div>
                    <div style="color: var(--text-muted); margin-top: 2px;">${slot.start_time} - ${slot.end_time}</div>
                  </td>
                  ${days.map(day => {
                    const matchSchedule = schedules.find(s => s.day_of_week === day.key && s.time_slot_id === slot.id);
                    return `
                      <td style="padding: 0.5rem; vertical-align: top; border-right: 1px solid var(--border-color); min-height: 85px;">
                        ${matchSchedule ? `
                          <div style="background: linear-gradient(135deg, #EFF6FF, #DBEAFE); border-left: 3px solid var(--primary-600); border-radius: var(--radius-md); padding: 8px; font-size: 0.75rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
                            <div style="font-weight: 800; color: var(--primary-800); margin-bottom: 2px;">
                              ${matchSchedule.class_code}
                            </div>
                            <div style="color: var(--text-primary); font-weight: 600; line-height: 1.2; margin-bottom: 4px;">
                              ${matchSchedule.class_name}
                            </div>
                            <div style="color: var(--text-secondary); font-size: 0.7rem; display: flex; align-items: center; gap: 4px;">
                              <span>📍 ${matchSchedule.room_name}</span>
                            </div>
                          </div>
                        ` : ''}
                      </td>
                    `;
                  }).join('')}
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  static _renderListView(days, schedules) {
    return `
      <div class="flex flex-col gap-4">
        ${days.map(day => {
          const daySchedules = schedules.filter(s => s.day_of_week === day.key);
          if (daySchedules.length === 0) return '';

          return `
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.25rem; border: 1px solid var(--border-color);">
              <h3 style="font-size: 1rem; font-weight: 800; color: var(--primary-700); margin-bottom: 0.75rem; display: flex; align-items: center; gap: 6px;">
                <span>📅</span> <span>${day.label}</span>
              </h3>

              <div class="grid grid-cols-2 gap-4">
                ${daySchedules.map(s => `
                  <div style="background: var(--neutral-50); border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 1rem; display: flex; flex-direction: column; justify-content: space-between;">
                    <div>
                      <div class="flex items-center justify-between" style="margin-bottom: 4px;">
                        <span class="badge badge-primary">${s.class_code}</span>
                        <span style="font-size: 0.75rem; font-weight: 700; color: var(--primary-600);">${s.time_range}</span>
                      </div>
                      <h4 style="font-size: 0.9375rem; font-weight: 700; color: var(--text-primary); margin: 4px 0;">
                        ${s.class_name}
                      </h4>
                      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">
                        Phòng: <strong>${s.room_name}</strong>
                      </p>
                    </div>

                    <div style="margin-top: 0.75rem; padding-top: 0.5rem; border-top: 1px dashed var(--border-color); font-size: 0.75rem; color: var(--text-muted);">
                      Giảng viên: ${s.teacher_name || 'Đang cập nhật'}
                    </div>
                  </div>
                `).join('')}
              </div>
            </div>
          `;
        }).join('')}

        ${schedules.length === 0 ? `
          <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
            <p style="color: var(--text-muted); margin: 0;">Bạn chưa có lịch học nào trong tuần.</p>
          </div>
        ` : ''}
      </div>
    `;
  }
}
