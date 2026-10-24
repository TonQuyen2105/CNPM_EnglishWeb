/**
 * Modal Service - Quản lý hộp thoại Modal
 */

import { icon } from './icons.js';

class ModalService {
  constructor() {
    this.activeModals = [];
    this._bindEvents();
  }

  _bindEvents() {
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && this.activeModals.length > 0) {
        this.close(this.activeModals[this.activeModals.length - 1].id);
      }
    });
  }

  /**
   * Mở modal mới
   * @param {Object} options 
   * @returns {string} modalId
   */
  open({ id = null, title = '', body = '', footer = '', maxWidth = '560px', onClose = null }) {
    const modalId = id || `modal_${Date.now()}`;

    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';
    backdrop.id = modalId;

    backdrop.innerHTML = `
      <div class="modal-dialog" style="max-width: ${maxWidth};" role="dialog" aria-modal="true" aria-labelledby="${modalId}_title">
        <div class="modal-header">
          <h3 class="modal-title" id="${modalId}_title">${title}</h3>
          <button type="button" class="btn-icon modal-close-btn" aria-label="Đóng">
            ${icon('x', { size: 20 })}
          </button>
        </div>
        <div class="modal-body">
          ${typeof body === 'string' ? body : ''}
        </div>
        ${footer ? `<div class="modal-footer">${footer}</div>` : ''}
      </div>
    `;

    if (typeof body !== 'string' && body instanceof HTMLElement) {
      backdrop.querySelector('.modal-body').appendChild(body);
    }

    // Sự kiện đóng modal
    const closeBtns = backdrop.querySelectorAll('.modal-close-btn, [data-dismiss="modal"]');
    closeBtns.forEach(btn => btn.addEventListener('click', () => this.close(modalId)));

    backdrop.addEventListener('click', (e) => {
      if (e.target === backdrop) {
        this.close(modalId);
      }
    });

    document.body.appendChild(backdrop);
    // Trigger animation
    requestAnimationFrame(() => {
      backdrop.classList.add('open');
    });

    this.activeModals.push({ id: modalId, element: backdrop, onClose });
    document.body.style.overflow = 'hidden';

    return modalId;
  }

  /**
   * Đóng modal theo ID
   * @param {string} modalId 
   */
  close(modalId) {
    const index = this.activeModals.findIndex(m => m.id === modalId);
    if (index === -1) return;

    const modalObj = this.activeModals[index];
    modalObj.element.classList.remove('open');

    setTimeout(() => {
      if (modalObj.element.parentNode) {
        modalObj.element.parentNode.removeChild(modalObj.element);
      }
      this.activeModals.splice(index, 1);
      if (this.activeModals.length === 0) {
        document.body.style.overflow = '';
      }
      if (typeof modalObj.onClose === 'function') {
        modalObj.onClose();
      }
    }, 200);
  }

  /**
   * Đóng tất cả modal
   */
  closeAll() {
    [...this.activeModals].forEach(m => this.close(m.id));
  }
}

export const modal = new ModalService();
