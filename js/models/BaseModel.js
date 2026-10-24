/**
 * BaseModel - Lớp trừu tượng Repository / Model cơ sở
 * Đóng gói toàn bộ thao tác CRUD và lưu trữ, sẵn sàng thay thế bằng fetch() API
 */

import { storage } from '../core/storage.js';

export class BaseModel {
  /**
   * @param {string} tableName Tên bảng trong Database
   * @param {string} primaryKey Khóa chính (mặc định 'id')
   */
  constructor(tableName, primaryKey = 'id') {
    this.tableName = tableName;
    this.primaryKey = primaryKey;
  }

  /**
   * Lấy toàn bộ bản ghi
   * @param {Function} filterFn Hàm lọc dữ liệu
   * @returns {Promise<Array>}
   */
  async getAll(filterFn = null) {
    const list = storage.getTable(this.tableName) || [];
    if (typeof filterFn === 'function') {
      return list.filter(filterFn);
    }
    return [...list];
  }

  /**
   * Lấy một bản ghi theo ID
   * @param {number|string} id 
   * @returns {Promise<Object|null>}
   */
  async getById(id) {
    const list = storage.getTable(this.tableName) || [];
    const item = list.find(row => String(row[this.primaryKey]) === String(id));
    return item ? { ...item } : null;
  }

  /**
   * Tạo bản ghi mới
   * @param {Object} data 
   * @returns {Promise<Object>}
   */
  async create(data) {
    const list = storage.getTable(this.tableName) || [];
    
    // Tự sinh ID tăng dần nếu khóa chính là số
    let nextId = 1;
    if (list.length > 0) {
      const maxId = Math.max(...list.map(r => Number(r[this.primaryKey]) || 0));
      nextId = maxId + 1;
    }

    const now = new Date().toISOString();
    const newRecord = {
      [this.primaryKey]: data[this.primaryKey] || nextId,
      ...data,
      created_at: data.created_at || now,
      updated_at: data.updated_at || now
    };

    list.push(newRecord);
    storage.setTable(this.tableName, list);

    return { ...newRecord };
  }

  /**
   * Cập nhật bản ghi theo ID
   * @param {number|string} id 
   * @param {Object} data 
   * @returns {Promise<Object|null>}
   */
  async update(id, data) {
    const list = storage.getTable(this.tableName) || [];
    const index = list.findIndex(row => String(row[this.primaryKey]) === String(id));

    if (index === -1) return null;

    const now = new Date().toISOString();
    const updatedRecord = {
      ...list[index],
      ...data,
      [this.primaryKey]: list[index][this.primaryKey], // Khóa chính không đổi
      updated_at: now
    };

    list[index] = updatedRecord;
    storage.setTable(this.tableName, list);

    return { ...updatedRecord };
  }

  /**
   * Xóa mềm (Soft Delete): chuyển cờ is_active = 0 hoặc is_published = 0
   * @param {number|string} id 
   * @param {string} flagField 
   * @returns {Promise<boolean>}
   */
  async softDelete(id, flagField = 'is_active') {
    const item = await this.getById(id);
    if (!item) return false;

    const updatePayload = { [flagField]: 0 };
    const res = await this.update(id, updatePayload);
    return !!res;
  }

  /**
   * Xóa cứng một bản ghi
   * @param {number|string} id 
   * @returns {Promise<boolean>}
   */
  async delete(id) {
    const list = storage.getTable(this.tableName) || [];
    const initialLength = list.length;
    const filtered = list.filter(row => String(row[this.primaryKey]) !== String(id));

    if (filtered.length === initialLength) return false;

    storage.setTable(this.tableName, filtered);
    return true;
  }
}
