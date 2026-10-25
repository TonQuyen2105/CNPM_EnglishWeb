/**
 * CourseReviewModel - Quản lý đánh giá và phản hồi khóa học
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class CourseReviewModelClass extends BaseModel {
  constructor() {
    super('course_reviews');
  }

  async getByCourse(courseId) {
    const list = await this.getAll(r => String(r.course_id) === String(courseId));
    const users = storage.getTable('users') || [];

    return list.map(r => {
      const user = users.find(u => u.id === r.user_id) || {};
      return {
        ...r,
        user_name: user.full_name || 'Học viên ẩn danh',
        avatar_url: user.avatar_url || ''
      };
    });
  }

  /**
   * Lấy danh sách review do học viên này đã viết
   */
  async getByUser(userId) {
    const list = await this.getAll(r => String(r.user_id) === String(userId));
    const courses = storage.getTable('courses') || [];

    return list.map(r => {
      const course = courses.find(c => c.id === r.course_id) || {};
      return {
        ...r,
        course_title: course.title || 'Khóa học',
        thumbnail_url: course.thumbnail_url || ''
      };
    });
  }

  /**
   * Tạo hoặc cập nhật đánh giá khóa học (mỗi học viên 1 đánh giá / khóa)
   */
  async submitReview({ userId, courseId, rating, comment }) {
    const list = await this.getAll();
    const existing = list.find(r => String(r.user_id) === String(userId) && String(r.course_id) === String(courseId));

    if (existing) {
      return this.update(existing.id, {
        rating: Number(rating) || 5,
        comment: comment.trim(),
        updated_at: new Date().toISOString()
      });
    }

    return this.create({
      user_id: Number(userId),
      course_id: Number(courseId),
      rating: Number(rating) || 5,
      comment: comment.trim(),
      created_at: new Date().toISOString()
    });
  }
}

export const CourseReviewModel = new CourseReviewModelClass();
