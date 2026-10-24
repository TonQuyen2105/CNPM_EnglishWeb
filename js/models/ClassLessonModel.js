/**
 * ClassLessonModel - Quản lý buổi học chi tiết của từng lớp học
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class ClassLessonModelClass extends BaseModel {
  constructor() {
    super('class_lessons');
  }

  async getByClass(classId) {
    const list = await this.getAll(cl => String(cl.class_id) === String(classId));
    const lessons = storage.getTable('lessons') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];

    return list.map(cl => {
      const lesson = lessons.find(l => l.id === cl.lesson_id) || {};
      let teacherName = 'Chưa phân công';
      if (cl.teacher_id) {
        const t = teachers.find(item => item.id === cl.teacher_id);
        if (t) {
          const u = users.find(user => user.id === t.user_id);
          if (u) teacherName = u.full_name;
        }
      }

      return {
        ...cl,
        lesson_title: lesson.title || `Bài học #${cl.lesson_id}`,
        content_type: lesson.content_type || 'vocabulary',
        teacher_name: teacherName
      };
    });
  }

  /**
   * Lấy toàn bộ buổi học của các lớp mà giáo viên phụ trách
   * @param {number|string} teacherId 
   */
  async getByTeacher(teacherId) {
    const classTeachers = storage.getTable('class_teachers') || [];
    const myClassIds = classTeachers
      .filter(ct => String(ct.teacher_id) === String(teacherId))
      .map(ct => ct.class_id);

    const allLessons = await this.getAll(cl => 
      myClassIds.includes(cl.class_id) || String(cl.teacher_id) === String(teacherId)
    );

    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];
    const lessons = storage.getTable('lessons') || [];
    const schedules = storage.getTable('class_schedules') || [];
    const rooms = storage.getTable('classrooms') || [];
    const slots = storage.getTable('time_slots') || [];

    return allLessons.map(cl => {
      const cls = classes.find(c => c.id === cl.class_id) || {};
      const course = courses.find(c => c.id === cls.course_id) || {};
      const lesson = lessons.find(l => l.id === cl.lesson_id) || {};
      const sched = schedules.find(s => s.id === cl.schedule_id) || {};
      const room = rooms.find(r => r.id === sched.classroom_id) || {};
      const slot = slots.find(s => s.id === sched.time_slot_id) || {};

      return {
        ...cl,
        class_code: cls.class_code || '',
        class_name: cls.class_name || '',
        course_title: course.title || '',
        lesson_title: lesson.title || `Bài #${cl.lesson_id}`,
        content_type: lesson.content_type || 'vocabulary',
        room_name: room.name || 'P.Học',
        room_location: room.location || '',
        slot_name: slot.name || '',
        start_time: slot.start_time || '18:00',
        end_time: slot.end_time || '19:30',
        day_of_week: sched.day_of_week || 2
      };
    }).sort((a, b) => new Date(a.schedule_date) - new Date(b.schedule_date));
  }

  /**
   * Cập nhật trạng thái buổi học (scheduled | completed | cancelled) và ghi chú
   */
  async updateStatus(id, status, notes = null) {
    const data = { status, updated_at: new Date().toISOString() };
    if (notes !== null) data.notes = notes;
    return this.update(id, data);
  }

  /**
   * Gán bài học lý thuyết/bài tập vào buổi học
   */
  async assignLesson(id, lessonId) {
    return this.update(id, { lesson_id: Number(lessonId), updated_at: new Date().toISOString() });
  }
}

export const ClassLessonModel = new ClassLessonModelClass();
