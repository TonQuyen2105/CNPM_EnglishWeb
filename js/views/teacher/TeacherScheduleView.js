/**
 * TeacherScheduleView - Giao diện Thời khóa biểu Giảng dạy
 * Lưới tuần (time_slots x Thứ), chế độ xem tháng, điều hướng tuần, màu sắc trạng thái
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml, formatDate } from '../../core/utils.js';

export class TeacherScheduleView {
  /**
   * Render trang Thời khóa biểu
   * @param {Object} data 
   * @returns {string}
   */
  static render(data) {
    const { 
      viewMode, 
      currentWeekLabel, 
      timeSlots, 
      daysOfWeek, 
      scheduleMatrix, 
      monthLessons 
    } = data;

    return `
      <div class="teacher-schedule">
        <!-- Header & Điều hướng -->
        <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title" style="margin: 0; font-size: 1.5rem; font-weight: 800;">
              Thời Khóa Biểu Giảng Dạy
            </h1>
            <p class="page-header-subtitle" style="margin: 0; color: var(--text-secondary); font-size: 0.875rem;">
              Theo dõi lịch đứng lớp, phòng học và ca dạy được phân công
            </p>
          </div>

          <!-- Controls: Switch Chế độ xem & Điều hướng tuần -->
          <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
            <!-- View Mode Switcher -->
            <div class="btn-group" style="display: flex; background: var(--neutral-100); padding: 2px; border-radius: var(--radius-md);">
              <button type="button" class="btn btn-sm btn-view-mode ${viewMode === 'week' ? 'btn-white shadow-sm' : 'btn-ghost'}" data-mode="week" style="font-size: 0.75rem; padding: 4px 10px;">
                <span class="btn-icon-left">${icon('calendar', { size: 14 })}</span> Theo tuần
              </button>
              <button type="button" class="btn btn-sm btn-view-mode ${viewMode === 'month' ? 'btn-white shadow-sm' : 'btn-ghost'}" data-mode="month" style="font-size: 0.75rem; padding: 4px 10px;">
                <span class="btn-icon-left">${icon('layers', { size: 14 })}</span> Danh sách tháng
              </button>
            </div>

            <!-- Week Navigator -->
            <div style="display: flex; align-items: center; gap: 4px; background: white; border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 2px;">
              <button type="button" class="btn-icon btn-prev-week" title="Tuần trước" style="padding: 4px;">
                ${icon('chevron-left', { size: 16 })}
              </button>
              <button type="button" class="btn btn-sm btn-ghost btn-current-week" style="font-size: 0.75rem; font-weight: 700; padding: 4px 8px;">
                ${currentWeekLabel}
              </button>
              <button type="button" class="btn-icon btn-next-week" title="Tuần sau" style="padding: 4px;">
                ${icon('chevron-right', { size: 16 })}
              </button>
            </div>
          </div>
        </div>

        <!-- Chú thích & Banner quyền hạn -->
        <div style="background-color: var(--primary-50); border: 1px solid var(--primary-200); border-radius: var(--radius-md); padding: 0.75rem 1rem; margin-bottom: 1.5rem; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
          <div style="display: flex; align-items: center; gap: 8px; font-size: 0.8125rem; color: var(--primary-900);">
            <span style="color: var(--primary-600);">${icon('info', { size: 18 })}</span>
            <span><strong>Chế độ chỉ xem:</strong> Lịch học và xếp phòng do Quản trị viên phòng đào tạo điều phối thống nhất.</span>
          </div>

          <!-- Color Legend -->
          <div style="display: flex; align-items: center; gap: 1rem; font-size: 0.75rem;">
            <div style="display: flex; align-items: center; gap: 6px;">
              <span style="width: 10px; height: 10px; border-radius: 2px; background: #22C55E;"></span>
              <span>Đã hoàn thành</span>
            </div>
            <div style="display: flex; align-items: center; gap: 6px;">
              <span style="width: 10px; height: 10px; border-radius: 2px; background: #2563EB;"></span>
              <span>Sắp diễn ra</span>
            </div>
            <div style="display: flex; align-items: center; gap: 6px;">
              <span style="width: 10px; height: 10px; border-radius: 2px; background: #EF4444;"></span>
              <span>Nghỉ / Đã hủy</span>
            </div>
          </div>
        </div>

        <!-- Chế độ 1: Weekly Timetable Grid -->
        ${viewMode === 'week' ? `
          <div class="card" style="padding: 0; overflow-x: auto;">
            <table class="table" style="margin: 0; min-width: 900px; border-collapse: separate; border-spacing: 0;">
              <thead>
                <tr style="background-color: var(--neutral-100);">
                  <th style="width: 120px; text-align: center; border-right: 1px solid var(--border-color); padding: 0.75rem;">
                    Khung giờ
                  </th>
                  ${daysOfWeek.map(d => `
                    <th style="text-align: center; padding: 0.75rem; border-right: 1px solid var(--border-color); ${d.isToday ? 'background: #EFF6FF;' : ''}">
                      <div style="font-weight: 800; font-size: 0.875rem; color: ${d.isToday ? 'var(--primary-700)' : 'var(--text-primary)'};">
                        ${d.label}
                      </div>
                      <div style="font-size: 0.7rem; font-weight: 500; color: var(--text-muted); margin-top: 2px;">
                        ${d.dateStr}
                      </div>
                    </th>
                  `).join('')}
                </tr>
              </thead>
              <tbody>
                ${timeSlots.map(slot => `
                  <tr>
                    <!-- Tên ca học & Giờ -->
                    <td style="text-align: center; vertical-align: middle; background: var(--neutral-50); border-right: 1px solid var(--border-color); border-bottom: 1px solid var(--border-color); padding: 0.75rem 0.5rem;">
                      <div style="font-weight: 700; font-size: 0.8125rem; color: var(--text-primary);">${escapeHtml(slot.name)}</div>
                      <div style="font-size: 0.7rem; color: var(--text-muted); margin-top: 2px;">
                        ${escapeHtml(slot.start_time)} - ${escapeHtml(slot.end_time)}
                      </div>
                    </td>

                    <!-- 7 Ngày trong tuần -->
                    ${daysOfWeek.map(day => {
                      const session = scheduleMatrix[slot.id] ? scheduleMatrix[slot.id][day.dayOfWeek] : null;

                      let cellBg = 'white';
                      let borderColor = 'var(--border-color)';
                      if (session) {
                        if (session.status === 'completed') {
                          cellBg = '#F0FDF4';
                          borderColor = '#86EFAC';
                        } else if (session.status === 'cancelled') {
                          cellBg = '#FEF2F2';
                          borderColor = '#FCA5A5';
                        } else {
                          cellBg = '#EFF6FF';
                          borderColor = '#BFDBFE';
                        }
                      }

                      return `
                        <td style="vertical-align: top; border-right: 1px solid var(--border-color); border-bottom: 1px solid var(--border-color); padding: 6px; height: 95px; width: 14.28%;">
                          ${session ? `
                            <div style="height: 100%; background: ${cellBg}; border: 1px solid ${borderColor}; border-radius: var(--radius-md); padding: 8px 10px; display: flex; flex-direction: column; justify-content: space-between;">
                              <div>
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2px;">
                                  <strong style="font-size: 0.8125rem; color: var(--text-primary);">${escapeHtml(session.class_code)}</strong>
                                  <span style="font-size: 0.65rem; font-weight: 700; color: ${session.status === 'completed' ? '#15803D' : '#1D4ED8'};">
                                    ${session.status === 'completed' ? '✓ Đã dạy' : 'Đang học'}
                                  </span>
                                </div>
                                <div style="font-size: 0.75rem; color: var(--text-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${escapeHtml(session.course_title)}">
                                  ${escapeHtml(session.course_title)}
                                </div>
                              </div>

                              <div style="display: flex; justify-content: space-between; align-items: flex-end; font-size: 0.7rem; color: var(--text-muted); margin-top: 4px; padding-top: 4px; border-top: 1px dashed ${borderColor};">
                                <span style="display: flex; align-items: center; gap: 2px;">
                                  ${icon('map-pin', { size: 11 })}
                                  <strong>${escapeHtml(session.classroom_name || 'P301')}</strong>
                                </span>
                                <span>${escapeHtml(session.location || 'Khu A')}</span>
                              </div>
                            </div>
                          ` : `
                            <div style="height: 100%; display: flex; align-items: center; justify-content: center; color: var(--neutral-300); font-size: 0.7rem;">
                              -
                            </div>
                          `}
                        </td>
                      `;
                    }).join('')}
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        ` : `
          <!-- Chế độ 2: Danh sách các buổi trong tháng -->
          <div class="card" style="padding: 1.25rem 1.5rem;">
            <h3 style="font-size: 1rem; font-weight: 700; margin: 0 0 1.25rem 0;">
              Lịch trình toàn bộ các buổi học (${monthLessons.length} buổi)
            </h3>

            ${monthLessons.length > 0 ? `
              <div class="table-responsive">
                <table class="table">
                  <thead>
                    <tr>
                      <th>Ngày học</th>
                      <th>Khung giờ</th>
                      <th>Mã lớp</th>
                      <th>Khóa học</th>
                      <th>Bài giảng</th>
                      <th>Phòng học</th>
                      <th>Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${monthLessons.map(l => `
                      <tr>
                        <td>
                          <strong>${l.schedule_date ? formatDate(l.schedule_date) : 'Định kỳ'}</strong>
                        </td>
                        <td style="font-size: 0.8125rem;">${l.start_time} - ${l.end_time}</td>
                        <td>
                          <span class="badge badge-info" style="font-weight: 700;">${escapeHtml(l.class_code)}</span>
                        </td>
                        <td style="font-size: 0.8125rem; font-weight: 600;">${escapeHtml(l.course_title)}</td>
                        <td style="font-size: 0.8125rem; color: var(--text-secondary);">${escapeHtml(l.lesson_title)}</td>
                        <td style="font-size: 0.8125rem;">
                          <span style="display: inline-flex; align-items: center; gap: 4px;">
                            ${icon('map-pin', { size: 12 })}
                            ${escapeHtml(l.room_name)}
                          </span>
                        </td>
                        <td>
                          <span class="badge ${l.status === 'completed' ? 'badge-success' : (l.status === 'cancelled' ? 'badge-danger' : 'badge-primary')}">
                            ${l.status === 'completed' ? 'Đã học' : (l.status === 'cancelled' ? 'Đã hủy' : 'Sắp tới')}
                          </span>
                        </td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>
            ` : `
              <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
                Chưa có buổi học nào trong danh sách.
              </div>
            `}
          </div>
        `}
      </div>
    `;
  }
}
