/**
 * ClassScheduleModel - Quản lý lịch học thời khóa biểu của các lớp
 * Bao gồm logic kiểm tra xung đột phòng học, xung đột giáo viên và sức chứa phòng
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class ClassScheduleModelClass extends BaseModel {
  constructor() {
    super('class_schedules');
  }

  async getAllDetailed() {
    const schedules = await this.getAll();
    const classes = storage.getTable('course_classes') || [];
    const rooms = storage.getTable('classrooms') || [];
    const slots = storage.getTable('time_slots') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];

    return schedules.map(s => {
      const cls = classes.find(c => c.id === s.class_id) || {};
      const room = rooms.find(r => r.id === s.classroom_id) || {};
      const slot = slots.find(t => t.id === s.time_slot_id) || {};

      const teacherAssign = classTeachers.find(ct => ct.class_id === s.class_id && ct.role === 'main');
      let teacherName = 'Chưa phân công';
      let teacherId = null;
      if (teacherAssign) {
        teacherId = teacherAssign.teacher_id;
        const t = teachers.find(item => item.id === teacherAssign.teacher_id);
        if (t) {
          const u = users.find(user => user.id === t.user_id);
          if (u) teacherName = u.full_name;
        }
      }

      return {
        ...s,
        class_name: cls.class_name || 'Lớp học',
        class_code: cls.class_code || '',
        class_status: cls.status || '',
        max_students: cls.max_students || 0,
        classroom_name: room.name || 'Phòng học',
        classroom_location: room.location || '',
        classroom_capacity: room.capacity || 0,
        slot_name: slot.name || 'Ca học',
        start_time: slot.start_time || '',
        end_time: slot.end_time || '',
        teacher_name: teacherName,
        teacher_id: teacherId
      };
    });
  }

  async getByClass(classId) {
    const all = await this.getAllDetailed();
    return all.filter(s => String(s.class_id) === String(classId));
  }

  /**
   * Kiểm tra xung đột lịch học:
   * 1. Trùng phòng vào cùng ca và thứ
   * 2. Trùng giáo viên phụ trách vào cùng ca và thứ
   * 3. Sĩ số lớp vượt quá sức chứa phòng
   * @returns {{ hasConflict: boolean, message: string }}
   */
  async validateSchedule({ classId, classroomId, timeSlotId, dayOfWeek, excludeId = null }) {
    const list = await this.getAll();
    const classes = storage.getTable('course_classes') || [];
    const rooms = storage.getTable('classrooms') || [];
    const classTeachers = storage.getTable('class_teachers') || [];

    const targetClass = classes.find(c => String(c.id) === String(classId));
    const targetRoom = rooms.find(r => String(r.id) === String(classroomId));

    // 1. Kiểm tra sức chứa phòng
    if (targetClass && targetRoom && targetClass.max_students > targetRoom.capacity) {
      return {
        hasConflict: true,
        message: `Sĩ số lớp (${targetClass.max_students} học viên) vượt quá sức chứa tối đa của phòng "${targetRoom.name}" (${targetRoom.capacity} chỗ)!`
      };
    }

    // Lấy giáo viên chính của lớp này
    const currentMainTeacher = classTeachers.find(ct => String(ct.class_id) === String(classId) && ct.role === 'main');

    for (const item of list) {
      if (excludeId && String(item.id) === String(excludeId)) continue;

      const sameDayAndSlot = Number(item.day_of_week) === Number(dayOfWeek) && String(item.time_slot_id) === String(timeSlotId);
      if (!sameDayAndSlot) continue;

      // 2. Kiểm tra trùng phòng
      if (String(item.classroom_id) === String(classroomId)) {
        const cls = classes.find(c => c.id === item.class_id);
        return {
          hasConflict: true,
          message: `Phòng "${targetRoom ? targetRoom.name : 'này'}" đã được xếp cho lớp [${cls ? cls.class_code : 'khác'}] vào khung giờ này!`
        };
      }

      // 3. Kiểm tra trùng giáo viên
      if (currentMainTeacher) {
        const otherMainTeacher = classTeachers.find(ct => ct.class_id === item.class_id && ct.role === 'main');
        if (otherMainTeacher && otherMainTeacher.teacher_id === currentMainTeacher.teacher_id) {
          const cls = classes.find(c => c.id === item.class_id);
          return {
            hasConflict: true,
            message: `Giảng viên phụ trách đã có lịch dạy lớp [${cls ? cls.class_code : 'khác'}] trong cùng khung giờ và thứ này!`
          };
        }
      }
    }

    return { hasConflict: false, message: '' };
  }
}

export const ClassScheduleModel = new ClassScheduleModelClass();
