/**
 * Toast Notification Service
 * Hiển thị thông báo nổi thông minh
 */

import { icon } from './icons.js';

class ToastService {
  constructor() {
    this.container = null;
  }

  _ensureContainer() {
    if (!this.container || !document.body.contains(this.container)) {
      let el = document.querySelector('.toast-container');
      if (!el) {
        el = document.createElement('div');
        el.className = 'toast-container';
        document.body.appendChild(el);
      }
      this.container = el;
    }
    return this.container;
  }

  /**
   * Hiển thị một Toast
   * @param {Object} options 
   */
  show({ message, title = '', type = 'info', duration = 3500 }) {
    const container = this._ensureContainer();

    const toastEl = document.createElement('div');
    toastEl.className = `toast toast-${type}`;

    let iconName = 'info';
    let defaultTitle = 'Thông báo';
    if (type === 'success') {
      iconName = 'check-circle';
      defaultTitle = 'Thành công';
    } else if (type === 'warning') {
      iconName = 'alert-circle';
      defaultTitle = 'Cảnh báo';
    } else if (type === 'error') {
      iconName = 'x-circle';
      defaultTitle = 'Lỗi';
    }

    toastEl.innerHTML = `
      <div class="toast-icon">
        ${icon(iconName, { size: 20 })}
      </div>
      <div class="toast-content">
        <div class="toast-title">${title || defaultTitle}</div>
        <div class="toast-message">${message}</div>
      </div>
      <button type="button" class="toast-close" aria-label="Đóng">
        ${icon('x', { size: 16 })}
      </button>
    `;

    const closeBtn = toastEl.querySelector('.toast-close');
    const dismiss = () => {
      toastEl.classList.add('toast-hiding');
      setTimeout(() => {
        if (toastEl.parentNode) {
          toastEl.parentNode.removeChild(toastEl);
        }
      }, 250);
    };

    closeBtn.addEventListener('click', dismiss);

    if (duration > 0) {
      setTimeout(dismiss, duration);
    }

    container.appendChild(toastEl);
  }

  success(message, title = 'Thành công') {
    this.show({ message, title, type: 'success' });
  }

  error(message, title = 'Có lỗi xảy ra') {
    this.show({ message, title, type: 'error', duration: 4500 });
  }

  warning(message, title = 'Cảnh báo') {
    this.show({ message, title, type: 'warning' });
  }

  info(message, title = 'Thông tin') {
    this.show({ message, title, type: 'info' });
  }
}

export const toast = new ToastService();
