/**
 * CourseClassModel - Quản lý lớp học, phân công giáo viên, lịch học và số lượng học viên
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class CourseClassModelClass extends BaseModel {
  constructor() {
    super('course_classes');
  }

  /**
   * Lấy danh sách lớp học kèm thông tin chi tiết (Khóa học, Giảng viên chính, Số học viên)
   */
  async getAllWithDetails() {
    const classes = await this.getAll();
    const courses = storage.getTable('courses') || [];
    const enrollments = storage.getTable('enrollments') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];

    return classes.map(cls => {
      const course = courses.find(c => c.id === cls.course_id) || {};
      const activeEnrollments = enrollments.filter(e => e.class_id === cls.id && e.status !== 'cancelled');
      
      // Tìm giáo viên chính
      const mainAssign = classTeachers.find(ct => ct.class_id === cls.id && ct.role === 'main');
      let teacherName = 'Chưa phân công';
      if (mainAssign) {
        const teacher = teachers.find(t => t.id === mainAssign.teacher_id);
        if (teacher) {
          const user = users.find(u => u.id === teacher.user_id);
          if (user) teacherName = user.full_name;
        }
      }

      return {
        ...cls,
        course_title: course.title || '',
        enrolled_count: activeEnrollments.length,
        main_teacher_name: teacherName
      };
    });
  }

  async getByIdWithDetails(id) {
    const cls = await this.getById(id);
    if (!cls) return null;

    const courses = storage.getTable('courses') || [];
    const enrollments = storage.getTable('enrollments') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const classSchedules = storage.getTable('class_schedules') || [];
    const classrooms = storage.getTable('classrooms') || [];
    const timeSlots = storage.getTable('time_slots') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];

    const course = courses.find(c => c.id === cls.course_id) || {};
    const enrolledStudents = enrollments
      .filter(e => e.class_id === cls.id)
      .map(e => {
        const user = users.find(u => u.id === e.user_id) || {};
        return {
          ...e,
          student_name: user.full_name || '',
          student_email: user.email || '',
          student_avatar: user.avatar_url || ''
        };
      });

    // Lịch học
    const schedules = classSchedules
      .filter(cs => cs.class_id === cls.id)
      .map(cs => {
        const room = classrooms.find(r => r.id === cs.classroom_id) || {};
        const slot = timeSlots.find(ts => ts.id === cs.time_slot_id) || {};
        return {
          ...cs,
          classroom_name: room.name || '',
          location: room.location || '',
          slot_name: slot.name || '',
          start_time: slot.start_time || '',
          end_time: slot.end_time || ''
        };
      });

    return {
      ...cls,
      course,
      schedules,
      enrolled_students: enrolledStudents,
      enrolled_count: enrolledStudents.filter(s => s.status !== 'cancelled').length
    };
  }

  async getByStatus(status) {
    return this.getAll(c => c.status === status);
  }

  /**
   * Lấy danh sách lớp do giáo viên phụ trách (giảng viên chính hoặc trợ giảng)
   * @param {number|string} teacherId 
   */
  async getByTeacherId(teacherId) {
    const classTeachers = storage.getTable('class_teachers') || [];
    const myAssignments = classTeachers.filter(ct => String(ct.teacher_id) === String(teacherId));
    
    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];
    const enrollments = storage.getTable('enrollments') || [];
    const classLessons = storage.getTable('class_lessons') || [];
    const classSchedules = storage.getTable('class_schedules') || [];
    const rooms = storage.getTable('classrooms') || [];
    const slots = storage.getTable('time_slots') || [];

    return myAssignments.map(assign => {
      const cls = classes.find(c => c.id === assign.class_id) || {};
      const course = courses.find(c => c.id === cls.course_id) || {};
      const activeEnrollments = enrollments.filter(e => e.class_id === cls.id && e.status !== 'cancelled');
      const lessons = classLessons.filter(cl => cl.class_id === cls.id);
      const completedLessons = lessons.filter(cl => cl.status === 'completed');

      const schedules = classSchedules
        .filter(cs => cs.class_id === cls.id)
        .map(cs => {
          const room = rooms.find(r => r.id === cs.classroom_id) || {};
          const slot = slots.find(s => s.id === cs.time_slot_id) || {};
          return {
            ...cs,
            room_name: room.name || '',
            slot_name: slot.name || '',
            time_range: slot.start_time ? `${slot.start_time} - ${slot.end_time}` : ''
          };
        });

      return {
        ...cls,
        assignment_id: assign.id,
        role: assign.role || 'main', // 'main' | 'assistant'
        course_title: course.title || '',
        course_level: course.level || 'beginner',
        thumbnail_url: course.thumbnail_url || '',
        enrolled_count: activeEnrollments.length,
        total_lessons: lessons.length,
        completed_lessons: completedLessons.length,
        progress_percent: lessons.length > 0 ? Math.round((completedLessons.length / lessons.length) * 100) : 0,
        schedules
      };
    });
  }
}

export const CourseClassModel = new CourseClassModelClass();
