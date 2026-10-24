/**
 * TeacherScheduleController - Điều phối Thời khóa biểu Giảng dạy
 */

import { auth } from '../../core/auth.js';
import { TeacherModel } from '../../models/TeacherModel.js';
import { CourseClassModel } from '../../models/CourseClassModel.js';
import { ClassScheduleModel } from '../../models/ClassScheduleModel.js';
import { ClassLessonModel } from '../../models/ClassLessonModel.js';
import { TimeSlotModel } from '../../models/TimeSlotModel.js';
import { TeacherScheduleView } from '../../views/teacher/TeacherScheduleView.js';

export class TeacherScheduleController {
  static weekOffset = 0;
  static viewMode = 'week';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    const teacher = await TeacherModel.getByUserId(currentUser.id);
    if (!teacher) return;

    await TeacherScheduleController.updateView(container, teacher);
  }

  static async updateView(container, teacher) {
    // 1. Lấy danh sách time slots
    const timeSlots = await TimeSlotModel.getAll(ts => ts.is_active === 1);

    // 2. Lấy lớp của giáo viên
    const myClasses = await CourseClassModel.getByTeacherId(teacher.id);
    const myClassIds = myClasses.map(c => c.id);

    // 3. Lấy thời khóa biểu chi tiết của các lớp này
    const allSchedules = await ClassScheduleModel.getAllDetailed();
    const mySchedules = allSchedules.filter(s => myClassIds.includes(s.class_id));

    // 4. Lấy danh sách bài học/buổi học
    const monthLessons = await ClassLessonModel.getByTeacher(teacher.id);

    // 5. Tính toán ngày trong tuần theo weekOffset
    // Mốc thời gian hệ thống: 24/10/2026 (Thứ 7)
    const baseDate = new Date('2026-10-24T08:00:00Z');
    baseDate.setDate(baseDate.getDate() + (TeacherScheduleController.weekOffset * 7));

    // Tìm ngày Thứ 2 của tuần đó (Thứ 2 = day 1 in getDay() if Sunday=0)
    const currentDayOfWeek = baseDate.getDay(); // 0 is Sunday, 6 is Saturday
    const distanceToMonday = currentDayOfWeek === 0 ? -6 : 1 - currentDayOfWeek;
    const monday = new Date(baseDate);
    monday.setDate(baseDate.getDate() + distanceToMonday);

    const daysOfWeek = [];
    const dayLabels = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật'];
    const dayNumbers = [2, 3, 4, 5, 6, 7, 1];

    for (let i = 0; i < 7; i++) {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i);
      const isToday = d.toISOString().slice(0, 10) === '2026-10-24';
      daysOfWeek.push({
        label: dayLabels[i],
        dayOfWeek: dayNumbers[i],
        dateStr: `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`,
        isToday
      });
    }

    const currentWeekLabel = TeacherScheduleController.weekOffset === 0 
      ? `Tuần này (${daysOfWeek[0].dateStr} - ${daysOfWeek[6].dateStr})` 
      : `${daysOfWeek[0].dateStr} - ${daysOfWeek[6].dateStr}`;

    // 6. Xây dựng Ma trận Thời khóa biểu [timeSlotId][dayOfWeek]
    const scheduleMatrix = {};
    timeSlots.forEach(ts => {
      scheduleMatrix[ts.id] = {};
    });

    mySchedules.forEach(sched => {
      if (scheduleMatrix[sched.time_slot_id]) {
        // Tìm lớp để lấy tên khóa học
        const cls = myClasses.find(c => c.id === sched.class_id) || {};
        scheduleMatrix[sched.time_slot_id][sched.day_of_week] = {
          ...sched,
          course_title: cls.course_title || 'Khóa học',
          status: 'scheduled'
        };
      }
    });

    // Render HTML
    container.innerHTML = TeacherScheduleView.render({
      viewMode: TeacherScheduleController.viewMode,
      currentWeekLabel,
      timeSlots,
      daysOfWeek,
      scheduleMatrix,
      monthLessons
    });

    // Gắn sự kiện
    TeacherScheduleController.bindEvents(container, teacher);
  }

  static bindEvents(container, teacher) {
    // 1. Chuyển chế độ xem: Tuần / Tháng
    container.querySelectorAll('.btn-view-mode').forEach(btn => {
      btn.addEventListener('click', () => {
        TeacherScheduleController.viewMode = btn.getAttribute('data-mode');
        TeacherScheduleController.updateView(container, teacher);
      });
    });

    // 2. Chuyển tuần trước
    const prevBtn = container.querySelector('.btn-prev-week');
    if (prevBtn) {
      prevBtn.addEventListener('click', () => {
        TeacherScheduleController.weekOffset--;
        TeacherScheduleController.updateView(container, teacher);
      });
    }

    // 3. Chuyển tuần sau
    const nextBtn = container.querySelector('.btn-next-week');
    if (nextBtn) {
      nextBtn.addEventListener('click', () => {
        TeacherScheduleController.weekOffset++;
        TeacherScheduleController.updateView(container, teacher);
      });
    }

    // 4. Về tuần hiện tại
    const currentBtn = container.querySelector('.btn-current-week');
    if (currentBtn) {
      currentBtn.addEventListener('click', () => {
        TeacherScheduleController.weekOffset = 0;
        TeacherScheduleController.updateView(container, teacher);
      });
    }
  }
}
