/**
 * LessonModel - Quản lý bài học thuộc khóa học
 */

import { BaseModel } from './BaseModel.js';

class LessonModelClass extends BaseModel {
  constructor() {
    super('lessons');
  }

  async getByCourse(courseId) {
    const list = await this.getAll(l => String(l.course_id) === String(courseId));
    return list.sort((a, b) => a.order_index - b.order_index);
  }
}

export const LessonModel = new LessonModelClass();
