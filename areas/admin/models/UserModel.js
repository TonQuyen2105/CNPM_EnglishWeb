/**
 * UserModel - Quản lý tài khoản người dùng
 */

import { BaseModel } from './BaseModel.js';

class UserModelClass extends BaseModel {
  constructor() {
    super('users');
  }

  async getByRole(role) {
    return this.getAll(u => u.role === role);
  }

  async getStudents() {
    return this.getByRole('student');
  }

  async getTeachers() {
    return this.getByRole('teacher');
  }

  async getByEmail(email) {
    if (!email) return null;
    const list = await this.getAll();
    return list.find(u => u.email.toLowerCase() === email.toLowerCase()) || null;
  }
}

export const UserModel = new UserModelClass();
