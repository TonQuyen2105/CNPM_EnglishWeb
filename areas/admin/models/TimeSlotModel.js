/**
 * TimeSlotModel - Quản lý các ca học / khung giờ
 */

import { BaseModel } from './BaseModel.js';

class TimeSlotModelClass extends BaseModel {
  constructor() {
    super('time_slots');
  }

  async getActive() {
    return this.getAll(t => t.is_active === 1 || t.is_active === true);
  }
}

export const TimeSlotModel = new TimeSlotModelClass();
