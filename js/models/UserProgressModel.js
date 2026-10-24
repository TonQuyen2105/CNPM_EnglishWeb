/**
 * UserProgressModel - Quản lý tiến độ học tập và điểm số của từng bài học
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class UserProgressModelClass extends BaseModel {
  constructor() {
    super('user_progress');
  }

  async getByUser(userId) {
    return this.getAll(p => String(p.user_id) === String(userId));
  }

  async getProgressByCourse(userId, courseId) {
    const list = await this.getByUser(userId);
    return list;
  }

  /**
   * Đánh dấu hoàn thành một bài học (kèm điểm số nếu có)
   */
  async markCompleted(userId, lessonId, score = 100) {
    const list = await this.getAll();
    const existing = list.find(p => String(p.user_id) === String(userId) && String(p.lesson_id) === String(lessonId));

    if (existing) {
      return this.update(existing.id, {
        is_completed: 1,
        score: Number(score) || 100,
        completed_at: new Date().toISOString(),
        updated_at: new Date().toISOString()
      });
    }

    return this.create({
      user_id: Number(userId),
      lesson_id: Number(lessonId),
      is_completed: 1,
      score: Number(score) || 100,
      completed_at: new Date().toISOString()
    });
  }

  /**
   * Lấy bài học dở dang gần nhất của học viên để tiếp tục học
   */
  async getLatestInProgress(userId) {
    const enrollments = storage.getTable('enrollments') || [];
    const myActiveEnrollments = enrollments.filter(e => String(e.user_id) === String(userId) && e.status === 'active');
    if (myActiveEnrollments.length === 0) return null;

    const classes = storage.getTable('course_classes') || [];
    const courses = storage.getTable('courses') || [];
    const lessons = storage.getTable('lessons') || [];
    const progress = await this.getByUser(userId);

    // Tìm trong các lớp đang học
    for (const enroll of myActiveEnrollments) {
      const cls = classes.find(c => c.id === enroll.class_id);
      if (!cls) continue;
      const course = courses.find(c => c.id === cls.course_id);
      if (!course) continue;

      const courseLessons = lessons.filter(l => l.course_id === cls.course_id).sort((a, b) => a.order_index - b.order_index);
      
      // Tìm bài học đầu tiên chưa hoàn thành
      const uncompletedLesson = courseLessons.find(l => {
        const p = progress.find(item => item.lesson_id === l.id);
        return !p || !p.is_completed;
      });

      if (uncompletedLesson) {
        const completedCount = courseLessons.filter(l => {
          const p = progress.find(item => item.lesson_id === l.id);
          return p && p.is_completed;
        }).length;

        return {
          lesson: uncompletedLesson,
          course,
          cls,
          progress_percent: Math.round((completedCount / courseLessons.length) * 100),
          completed_count: completedCount,
          total_count: courseLessons.length
        };
      }
    }

    // Nếu đã hoàn thành hết, lấy bài đầu tiên của lớp đầu tiên
    const firstEnroll = myActiveEnrollments[0];
    const firstCls = classes.find(c => c.id === firstEnroll.class_id);
    const firstCourse = firstCls ? courses.find(c => c.id === firstCls.course_id) : null;
    const firstLesson = firstCls ? lessons.find(l => l.course_id === firstCls.course_id) : null;

    if (firstLesson) {
      return {
        lesson: firstLesson,
        course: firstCourse,
        cls: firstCls,
        progress_percent: 100,
        completed_count: 1,
        total_count: 1
      };
    }

    return null;
  }
}

export const UserProgressModel = new UserProgressModelClass();
