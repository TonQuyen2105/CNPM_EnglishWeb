/**
 * TeacherModel - Quản lý thông tin chi tiết giảng viên & bằng cấp
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class TeacherModelClass extends BaseModel {
  constructor() {
    super('teachers');
  }

  /**
   * Lấy danh sách giáo viên kèm thông tin user (join users)
   */
  async getAllWithUser() {
    const teachers = await this.getAll();
    const users = storage.getTable('users') || [];
    
    return teachers.map(t => {
      const u = users.find(user => user.id === t.user_id) || {};
      return {
        ...t,
        full_name: u.full_name || 'Giáo viên',
        email: u.email || '',
        avatar_url: u.avatar_url || '',
        is_active: u.is_active !== undefined ? u.is_active : 1
      };
    });
  }

  async getByUserId(userId) {
    const list = await this.getAll();
    const teacher = list.find(t => String(t.user_id) === String(userId));
    if (!teacher) return null;
    return this.getByIdWithUser(teacher.id);
  }

  async getByIdWithUser(id) {
    const teacher = await this.getById(id);
    if (!teacher) return null;
    const users = storage.getTable('users') || [];
    const user = users.find(u => u.id === teacher.user_id) || {};
    return {
      ...teacher,
      full_name: user.full_name || '',
      email: user.email || '',
      avatar_url: user.avatar_url || '',
      is_active: user.is_active
    };
  }

  /**
   * Cập nhật hồ sơ giảng viên (cả bảng teachers và users)
   */
  async updateProfile(teacherId, { bio, qualification, avatar_url, full_name, password }) {
    const teacher = await this.getById(teacherId);
    if (!teacher) return null;

    // Cập nhật bảng teachers
    await this.update(teacherId, {
      bio: bio !== undefined ? bio : teacher.bio,
      qualification: qualification !== undefined ? qualification : teacher.qualification,
      updated_at: new Date().toISOString()
    });

    // Cập nhật bảng users
    const users = storage.getTable('users') || [];
    const userIndex = users.findIndex(u => u.id === teacher.user_id);
    if (userIndex !== -1) {
      if (avatar_url !== undefined) users[userIndex].avatar_url = avatar_url;
      if (full_name !== undefined) users[userIndex].full_name = full_name;
      if (password) users[userIndex].password = password;
      users[userIndex].updated_at = new Date().toISOString();
      storage.setTable('users', users);

      // Cập nhật session nếu là user hiện tại
      const sessionUser = storage.get('session_user');
      if (sessionUser && sessionUser.id === teacher.user_id) {
        storage.set('session_user', {
          ...sessionUser,
          full_name: users[userIndex].full_name,
          avatar_url: users[userIndex].avatar_url
        });
      }
    }

    return this.getByIdWithUser(teacherId);
  }
}

export const TeacherModel = new TeacherModelClass();
