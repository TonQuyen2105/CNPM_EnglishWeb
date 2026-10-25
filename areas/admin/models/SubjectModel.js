/**
 * SubjectModel - Quản lý chuyên ngành / Môn học
 */

import { BaseModel } from './BaseModel.js';

class SubjectModelClass extends BaseModel {
  constructor() {
    super('subjects');
  }

  async getActive() {
    return this.getAll(s => s.is_active === 1 || s.is_active === true);
  }
}

export const SubjectModel = new SubjectModelClass();
