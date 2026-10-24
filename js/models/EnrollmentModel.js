/**
 * EnrollmentModel - Quản lý đăng ký học viên & tính doanh thu ước tính
 * Lưu ý: DB không có bảng thanh toán; doanh thu chỉ là ước tính từ tổng price của enrollments
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class EnrollmentModelClass extends BaseModel {
  constructor() {
    super('enrollments');
  }

  async getByUser(userId) {
    const list = await this.getAll(e => String(e.user_id) === String(userId));
    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];

    return list.map(e => {
      const cls = classes.find(c => c.id === e.class_id) || {};
      const course = courses.find(c => c.id === cls.course_id) || {};
      return {
        ...e,
        class_name: cls.class_name || '',
        class_code: cls.class_code || '',
        status_class: cls.status || '',
        course_title: course.title || '',
        course_thumbnail: course.thumbnail_url || ''
      };
    });
  }

  /**
   * Lấy chi tiết các lớp học viên tham gia kèm tiến độ, lịch học và giảng viên
   */
  async getDetailedByUser(userId) {
    const list = await this.getAll(e => String(e.user_id) === String(userId));
    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];
    const schedules = storage.getTable('class_schedules') || [];
    const rooms = storage.getTable('classrooms') || [];
    const slots = storage.getTable('time_slots') || [];
    const lessons = storage.getTable('lessons') || [];
    const userProgress = storage.getTable('user_progress') || [];

    return list.map(e => {
      const cls = classes.find(c => c.id === e.class_id) || {};
      const course = courses.find(c => c.id === cls.course_id) || {};

      // Giảng viên chính
      const mainAssign = classTeachers.find(ct => ct.class_id === cls.id && ct.role === 'main');
      let teacherName = 'Chưa phân công';
      if (mainAssign) {
        const t = teachers.find(item => item.id === mainAssign.teacher_id);
        if (t) {
          const u = users.find(user => user.id === t.user_id);
          if (u) teacherName = u.full_name;
        }
      }

      // Lịch học tuần
      const mySchedules = schedules
        .filter(s => s.class_id === cls.id)
        .map(s => {
          const room = rooms.find(r => r.id === s.classroom_id) || {};
          const slot = slots.find(sl => sl.id === s.time_slot_id) || {};
          return {
            ...s,
            room_name: room.name || '',
            slot_name: slot.name || '',
            time_range: slot.start_time ? `${slot.start_time} - ${slot.end_time}` : ''
          };
        });

      // Tiến độ bài học
      const courseLessons = lessons.filter(l => l.course_id === cls.course_id);
      const completedCount = userProgress.filter(p => 
        String(p.user_id) === String(userId) && 
        courseLessons.some(cl => cl.id === p.lesson_id) && 
        (p.is_completed === 1 || p.is_completed === true)
      ).length;

      const progressPercent = courseLessons.length > 0 
        ? Math.round((completedCount / courseLessons.length) * 100) 
        : 0;

      return {
        ...e,
        course_id: cls.course_id,
        course_title: course.title || '',
        course_thumbnail: course.thumbnail_url || '',
        course_level: course.level || 'beginner',
        class_name: cls.class_name || '',
        class_code: cls.class_code || '',
        class_status: cls.status || 'ongoing',
        max_students: cls.max_students || 20,
        main_teacher_name: teacherName,
        schedules: mySchedules,
        total_lessons: courseLessons.length,
        completed_lessons: completedCount,
        progress_percent: progressPercent
      };
    });
  }

  /**
   * Đăng ký tham gia lớp học
   */
  async enrollClass(userId, classId) {
    const allEnrollments = await this.getAll();
    const classes = storage.getTable('course_classes') || [];
    const targetClass = classes.find(c => String(c.id) === String(classId));

    if (!targetClass) {
      return { success: false, message: 'Lớp học không tồn tại trong hệ thống.' };
    }

    // Kiểm tra đã đăng ký trước đó chưa
    const existing = allEnrollments.find(e => 
      String(e.user_id) === String(userId) && 
      String(e.class_id) === String(classId) && 
      e.status !== 'cancelled'
    );
    if (existing) {
      return { success: false, message: 'Bạn đã ghi danh lớp học này rồi!' };
    }

    // Kiểm tra sĩ số lớp
    const activeCount = allEnrollments.filter(e => String(e.class_id) === String(classId) && e.status !== 'cancelled').length;
    if (activeCount >= (targetClass.max_students || 20)) {
      return { success: false, message: 'Lớp học đã đủ sĩ số tối đa. Vui lòng chọn lớp khác.' };
    }

    const newEnrollment = await this.create({
      user_id: Number(userId),
      class_id: Number(classId),
      status: 'active',
      enrolled_at: new Date().toISOString()
    });

    return { success: true, enrollment: newEnrollment };
  }

  /**
   * Hủy đăng ký lớp học
   */
  async cancelEnrollment(enrollmentId, userId) {
    const enroll = await this.getById(enrollmentId);
    if (!enroll || String(enroll.user_id) !== String(userId)) {
      return { success: false, message: 'Không tìm thấy lượt ghi danh hoặc không có quyền.' };
    }

    await this.update(enrollmentId, { status: 'cancelled', updated_at: new Date().toISOString() });
    return { success: true };
  }

  async getByClass(classId) {
    const list = await this.getAll(e => String(e.class_id) === String(classId));
    const users = storage.getTable('users') || [];

    return list.map(e => {
      const user = users.find(u => u.id === e.user_id) || {};
      return {
        ...e,
        student_name: user.full_name || '',
        student_email: user.email || '',
        avatar_url: user.avatar_url || ''
      };
    });
  }

  /**
   * Tính toán doanh thu ước tính từ các lượt đăng ký học
   * @returns {Promise<{totalEstimated: number, count: number}>}
   */
  async calculateEstimatedRevenue() {
    const enrollments = await this.getAll(e => e.status !== 'cancelled');
    const classes = storage.getTable('course_classes') || [];

    let totalEstimated = 0;
    enrollments.forEach(e => {
      const cls = classes.find(c => c.id === e.class_id);
      if (cls && cls.price) {
        totalEstimated += Number(cls.price) || 0;
      }
    });

    return {
      totalEstimated,
      count: enrollments.length
    };
  }
}

export const EnrollmentModel = new EnrollmentModelClass();
