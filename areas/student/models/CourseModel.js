/**
 * CourseModel - Quản lý khóa học & join dữ liệu môn học, bài học
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class CourseModelClass extends BaseModel {
  constructor() {
    super('courses');
  }

  /**
   * Lấy tất cả khóa học kèm tên môn học
   */
  async getAllWithSubject() {
    const courses = await this.getAll();
    const subjects = storage.getTable('subjects') || [];
    const lessons = storage.getTable('lessons') || [];

    return courses.map(c => {
      const subj = subjects.find(s => s.id === c.subject_id) || {};
      const courseLessons = lessons.filter(l => l.course_id === c.id);
      return {
        ...c,
        subject_name: subj.name || 'Tổng hợp',
        lesson_count: courseLessons.length
      };
    });
  }

  async getByIdWithDetails(id) {
    const course = await this.getById(id);
    if (!course) return null;

    const subjects = storage.getTable('subjects') || [];
    const lessons = storage.getTable('lessons') || [];
    const reviews = storage.getTable('course_reviews') || [];

    const subj = subjects.find(s => s.id === course.subject_id) || {};
    const courseLessons = lessons.filter(l => l.course_id === course.id).sort((a, b) => a.order_index - b.order_index);
    const courseReviews = reviews.filter(r => r.course_id === course.id);

    const avgRating = courseReviews.length > 0 
      ? (courseReviews.reduce((sum, r) => sum + r.rating, 0) / courseReviews.length).toFixed(1)
      : 5.0;

    return {
      ...course,
      subject_name: subj.name || '',
      lessons: courseLessons,
      reviews: courseReviews,
      rating_avg: avgRating,
      review_count: courseReviews.length
    };
  }

  async getByLevel(level) {
    return this.getAll(c => c.level === level);
  }
}

export const CourseModel = new CourseModelClass();
