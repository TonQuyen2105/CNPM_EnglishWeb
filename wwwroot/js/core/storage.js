/**
 * Storage Service - Wrapper localStorage với serialization & key prefix
 */

const STORAGE_PREFIX = 'el_';

class StorageService {
  constructor(prefix = STORAGE_PREFIX) {
    this.prefix = prefix;
  }

  _getKey(key) {
    return `${this.prefix}${key}`;
  }

  /**
   * Lấy dữ liệu từ localStorage
   * @param {string} key 
   * @param {any} defaultValue 
   * @returns {any}
   */
  get(key, defaultValue = null) {
    try {
      const item = localStorage.getItem(this._getKey(key));
      return item !== null ? JSON.parse(item) : defaultValue;
    } catch (e) {
      console.error(`Lỗi đọc localStorage key "${key}":`, e);
      return defaultValue;
    }
  }

  /**
   * Lưu dữ liệu vào localStorage
   * @param {string} key 
   * @param {any} value 
   */
  set(key, value) {
    try {
      localStorage.setItem(this._getKey(key), JSON.stringify(value));
      return true;
    } catch (e) {
      console.error(`Lỗi ghi localStorage key "${key}":`, e);
      return false;
    }
  }

  /**
   * Xoá một khoá
   * @param {string} key 
   */
  remove(key) {
    try {
      localStorage.removeItem(this._getKey(key));
    } catch (e) {
      console.error(`Lỗi xoá localStorage key "${key}":`, e);
    }
  }

  /**
   * Lấy dữ liệu 1 bảng
   * @param {string} tableName 
   * @returns {Array}
   */
  getTable(tableName) {
    return this.get(`tbl_${tableName}`, []);
  }

  /**
   * Ghi toàn bộ dữ liệu 1 bảng
   * @param {string} tableName 
   * @param {Array} rows 
   */
  setTable(tableName, rows) {
    return this.set(`tbl_${tableName}`, rows);
  }

  /**
   * Xoá toàn bộ dữ liệu có prefix `el_`
   */
  clearAll() {
    try {
      const keysToRemove = [];
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith(this.prefix)) {
          keysToRemove.push(key);
        }
      }
      keysToRemove.forEach(k => localStorage.removeItem(k));
    } catch (e) {
      console.error('Lỗi khi xoá toàn bộ Storage:', e);
    }
  }
}

export const storage = new StorageService();
