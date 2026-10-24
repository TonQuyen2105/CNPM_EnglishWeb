/**
 * ClassTeacherModel - Quản lý phân công giáo viên vào lớp (main / assistant)
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class ClassTeacherModelClass extends BaseModel {
  constructor() {
    super('class_teachers');
  }

  async getByClass(classId) {
    const list = await this.getAll(ct => String(ct.class_id) === String(classId));
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];

    return list.map(ct => {
      const teacher = teachers.find(t => t.id === ct.teacher_id) || {};
      const user = users.find(u => u.id === teacher.user_id) || {};
      return {
        ...ct,
        teacher_name: user.full_name || 'Giáo viên',
        teacher_email: user.email || '',
        avatar_url: user.avatar_url || '',
        qualification: teacher.qualification || ''
      };
    });
  }

  async assignTeacher(classId, teacherId, role = 'main') {
    const list = await this.getAll();
    const existing = list.find(ct => String(ct.class_id) === String(classId) && String(ct.teacher_id) === String(teacherId));
    if (existing) {
      return this.update(existing.id, { role, assigned_at: new Date().toISOString() });
    }
    return this.create({
      class_id: Number(classId),
      teacher_id: Number(teacherId),
      role,
      assigned_at: new Date().toISOString()
    });
  }

  async removeTeacher(classId, teacherId) {
    const list = await this.getAll();
    const existing = list.find(ct => String(ct.class_id) === String(classId) && String(ct.teacher_id) === String(teacherId));
    if (existing) {
      return this.delete(existing.id);
    }
    return false;
  }
}

export const ClassTeacherModel = new ClassTeacherModelClass();
