/**
 * SchedulesController - Điều phối Thời khóa biểu và Xếp lịch thông minh
 */

import { SchedulesView } from '../../views/admin/SchedulesView.js';
import {
  ClassScheduleModel,
  ClassroomModel,
  TimeSlotModel,
  CourseClassModel,
  TeacherModel
} from '../../models/index.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';

export class SchedulesController {
  static async render(params, query, container) {
    this.container = container;
    this.rooms = [];
    this.classes = [];
    this.teachers = [];
    this.timeSlots = [];
    this.allSchedules = [];

    this.roomFilter = 'all';
    this.classFilter = 'all';
    this.teacherFilter = 'all';

    await this.initData();
  }

  static async initData() {
    [
      this.rooms,
      this.classes,
      this.teachers,
      this.timeSlots,
      this.allSchedules
    ] = await Promise.all([
      ClassroomModel.getActive(),
      CourseClassModel.getAll(),
      TeacherModel.getAllWithUser(),
      TimeSlotModel.getActive(),
      ClassScheduleModel.getAllDetailed()
    ]);

    this.container.innerHTML = SchedulesView.renderLayout({
      rooms: this.rooms,
      classes: this.classes,
      teachers: this.teachers
    });

    this.renderTimetable();
    this.bindEvents();
  }

  static renderTimetable() {
    const gridOutlet = this.container.querySelector('#timetable-grid-outlet');
    if (!gridOutlet) return;

    let filtered = [...this.allSchedules];

    if (this.roomFilter !== 'all') {
      filtered = filtered.filter(s => String(s.classroom_id) === String(this.roomFilter));
    }

    if (this.classFilter !== 'all') {
      filtered = filtered.filter(s => String(s.class_id) === String(this.classFilter));
    }

    if (this.teacherFilter !== 'all') {
      filtered = filtered.filter(s => String(s.teacher_id) === String(this.teacherFilter));
    }

    gridOutlet.innerHTML = SchedulesView.renderTimetableGrid(this.timeSlots, filtered);
    this.bindGridEvents(gridOutlet);
  }

  static bindEvents() {
    const roomFilter = this.container.querySelector('#schedule-room-filter');
    const classFilter = this.container.querySelector('#schedule-class-filter');
    const teacherFilter = this.container.querySelector('#schedule-teacher-filter');
    const addBtn = this.container.querySelector('#btn-add-schedule');

    if (roomFilter) {
      roomFilter.addEventListener('change', (e) => {
        this.roomFilter = e.target.value;
        this.renderTimetable();
      });
    }

    if (classFilter) {
      classFilter.addEventListener('change', (e) => {
        this.classFilter = e.target.value;
        this.renderTimetable();
      });
    }

    if (teacherFilter) {
      teacherFilter.addEventListener('change', (e) => {
        this.teacherFilter = e.target.value;
        this.renderTimetable();
      });
    }

    if (addBtn) {
      addBtn.addEventListener('click', () => this.openAddScheduleModal());
    }
  }

  static bindGridEvents(container) {
    const delBtns = container.querySelectorAll('.btn-delete-schedule');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const ok = await confirmDialog({
          title: 'Hủy ca học trên thời khóa biểu',
          message: 'Bạn có chắc chắn muốn hủy ca học này khỏi thời khóa biểu?',
          confirmText: 'Xác nhận hủy ca',
          isDanger: true
        });

        if (ok) {
          await ClassScheduleModel.delete(id);
          toast.success('Đã xóa ca học khỏi thời khóa biểu thành công!');
          this.allSchedules = await ClassScheduleModel.getAllDetailed();
          this.renderTimetable();
        }
      });
    });
  }

  static openAddScheduleModal() {
    const modalId = modal.open({
      title: 'Thêm ca học vào thời khóa biểu',
      body: SchedulesView.renderAddScheduleModal({
        classes: this.classes,
        rooms: this.rooms,
        timeSlots: this.timeSlots
      }),
      maxWidth: '560px'
    });

    const form = document.getElementById('add-schedule-modal-form');
    const warningBox = document.getElementById('schedule-conflict-warning');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      warningBox.style.display = 'none';

      const classId = document.getElementById('modal-sched-class').value;
      const classroomId = document.getElementById('modal-sched-room').value;
      const timeSlotId = document.getElementById('modal-sched-slot').value;
      const dayOfWeek = parseInt(document.getElementById('modal-sched-day').value, 10);

      // Kiểm tra xung đột thông minh
      const validation = await ClassScheduleModel.validateSchedule({
        classId,
        classroomId,
        timeSlotId,
        dayOfWeek
      });

      if (validation.hasConflict) {
        warningBox.textContent = `⚠️ XUNG ĐỘT LỊCH HỌC: ${validation.message}`;
        warningBox.style.display = 'block';
        toast.error(validation.message, 'Xung đột lịch học');
        return;
      }

      const submitBtn = document.getElementById('btn-submit-schedule');
      submitBtn.classList.add('loading');
      submitBtn.disabled = true;

      await ClassScheduleModel.create({
        class_id: Number(classId),
        classroom_id: Number(classroomId),
        time_slot_id: Number(timeSlotId),
        day_of_week: dayOfWeek
      });

      toast.success('Đã xếp lịch học vào thời khóa biểu thành công!');
      modal.close(modalId);
      this.allSchedules = await ClassScheduleModel.getAllDetailed();
      this.renderTimetable();
    });
  }
}
