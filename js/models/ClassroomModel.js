/**
 * ClassroomModel - Quản lý phòng học
 */

import { BaseModel } from './BaseModel.js';

class ClassroomModelClass extends BaseModel {
  constructor() {
    super('classrooms');
  }

  async getActive() {
    return this.getAll(r => r.is_active === 1 || r.is_active === true);
  }
}

export const ClassroomModel = new ClassroomModelClass();
