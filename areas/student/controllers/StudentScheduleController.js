/**
 * StudentScheduleController - Điều khiển Thời khóa biểu học viên
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import { EnrollmentModel, TimeSlotModel } from '../../models/index.js';
import { StudentScheduleView } from '../../views/student/StudentScheduleView.js';

export class StudentScheduleController {
  static _viewMode = 'grid';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    const currentUser = auth.getCurrentUser();
    const myEnrollments = await EnrollmentModel.getDetailedByUser(currentUser.id);
    const activeEnrollments = myEnrollments.filter(e => e.status === 'active');
    const enrolledClassIds = activeEnrollments.map(e => e.class_id);

    const timeSlots = await TimeSlotModel.getAll();
    timeSlots.sort((a, b) => (a.start_time > b.start_time ? 1 : -1));

    const allSchedules = storage.getTable('class_schedules') || [];
    const rooms = storage.getTable('classrooms') || [];

    // Lọc lịch học của các lớp học viên đang học
    const mySchedules = allSchedules
      .filter(s => enrolledClassIds.includes(s.class_id))
      .map(s => {
        const clsEnroll = activeEnrollments.find(e => e.class_id === s.class_id) || {};
        const room = rooms.find(r => r.id === s.classroom_id) || {};
        const slot = timeSlots.find(sl => sl.id === s.time_slot_id) || {};

        return {
          ...s,
          class_code: clsEnroll.class_code || '',
          class_name: clsEnroll.class_name || '',
          teacher_name: clsEnroll.main_teacher_name || '',
          room_name: room.name || 'Phòng học',
          time_range: slot.start_time ? `${slot.start_time} - ${slot.end_time}` : ''
        };
      });

    container.innerHTML = StudentScheduleView.render({
      timeSlots,
      schedules: mySchedules,
      viewMode: this._viewMode
    });

    const gridBtn = container.querySelector('#btn-view-grid');
    const listBtn = container.querySelector('#btn-view-list');

    if (gridBtn) {
      gridBtn.addEventListener('click', () => {
        this._viewMode = 'grid';
        this.render(container);
      });
    }

    if (listBtn) {
      listBtn.addEventListener('click', () => {
        this._viewMode = 'list';
        this.render(container);
      });
    }
  }
}
