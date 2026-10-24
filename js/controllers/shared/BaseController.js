/**
 * BaseController - Lớp Controller cơ sở dùng chung
 * Cung cấp các phương thức tiện ích cho các Controller con
 */

export class BaseController {
  constructor(container) {
    this.container = container;
  }

  /**
   * Phương thức render mặc định cần được ghi đè bởi Controller con
   */
  async render(params = {}, query = {}, container = null) {
    throw new Error('Phương thức render() phải được cài đặt trong lớp con.');
  }

  /**
   * Gán sự kiện tiện ích với ủy quyền (Event delegation)
   */
  on(eventName, selector, handler) {
    if (!this.container) return;
    this.container.addEventListener(eventName, (e) => {
      const target = e.target.closest(selector);
      if (target && this.container.contains(target)) {
        handler.call(target, e);
      }
    });
  }
}
