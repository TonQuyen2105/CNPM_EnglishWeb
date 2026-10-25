/**
 * EventBus - Hệ thống Publish/Subscribe trung gian cho toàn ứng dụng
 */
class EventBus {
  constructor() {
    this.events = {};
  }

  /**
   * Đăng ký lắng nghe sự kiện
   * @param {string} event 
   * @param {Function} callback 
   * @returns {Function} Hàm unsubscribe
   */
  on(event, callback) {
    if (!this.events[event]) {
      this.events[event] = [];
    }
    this.events[event].push(callback);

    return () => this.off(event, callback);
  }

  /**
   * Hủy đăng ký lắng nghe sự kiện
   * @param {string} event 
   * @param {Function} callback 
   */
  off(event, callback) {
    if (!this.events[event]) return;
    this.events[event] = this.events[event].filter(cb => cb !== callback);
  }

  /**
   * Phát đi sự kiện cùng dữ liệu đính kèm
   * @param {string} event 
   * @param {any} data 
   */
  emit(event, data) {
    if (!this.events[event]) return;
    this.events[event].forEach(cb => {
      try {
        cb(data);
      } catch (err) {
        console.error(`Lỗi khi xử lý sự kiện "${event}":`, err);
      }
    });
  }

  /**
   * Đăng ký nhận sự kiện 1 lần duy nhất
   */
  once(event, callback) {
    const unregister = this.on(event, (data) => {
      unregister();
      callback(data);
    });
  }
}

export const eventBus = new EventBus();
