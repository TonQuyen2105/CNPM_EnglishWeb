/**
 * Confirm Dialog Service - Hộp thoại xác nhận thao tác nguy hiểm / quan trọng
 * Sử dụng Promise tiện lợi cho async/await
 */

import { modal } from './modal.js';
import { icon } from './icons.js';
import { escapeHtml } from '../core/utils.js';

/**
 * Hiển thị hộp thoại xác nhận
 * @param {Object} options 
 * @returns {Promise<boolean>}
 */
export function confirmDialog({
  title = 'Xác nhận hành động',
  message = 'Bạn có chắc chắn muốn thực hiện thao tác này? Thao tác không thể hoàn tác.',
  confirmText = 'Xác nhận',
  cancelText = 'Hủy bỏ',
  isDanger = false
}) {
  return new Promise((resolve) => {
    let resolved = false;

    const bodyHtml = `
      <div style="display: flex; gap: 1rem; align-items: flex-start;">
        <div style="
          width: 44px; 
          height: 44px; 
          border-radius: 50%; 
          background-color: ${isDanger ? 'var(--danger-bg)' : 'var(--warning-bg)'}; 
          color: ${isDanger ? 'var(--danger-main)' : 'var(--warning-main)'};
          display: flex; 
          align-items: center; 
          justify-content: center; 
          flex-shrink: 0;
        ">
          ${icon(isDanger ? 'alert-circle' : 'info', { size: 24 })}
        </div>
        <div style="flex: 1;">
          <p style="margin: 0; color: var(--text-secondary); line-height: 1.5; font-size: 0.9375rem;">
            ${escapeHtml(message)}
          </p>
        </div>
      </div>
    `;

    const footerHtml = `
      <button type="button" class="btn btn-secondary" id="confirm-dialog-cancel">
        ${escapeHtml(cancelText)}
      </button>
      <button type="button" class="btn ${isDanger ? 'btn-danger' : 'btn-primary'}" id="confirm-dialog-ok">
        ${escapeHtml(confirmText)}
      </button>
    `;

    const modalId = modal.open({
      title,
      body: bodyHtml,
      footer: footerHtml,
      maxWidth: '460px',
      onClose: () => {
        if (!resolved) {
          resolved = true;
          resolve(false);
        }
      }
    });

    const modalEl = document.getElementById(modalId);
    if (!modalEl) {
      resolve(false);
      return;
    }

    const cancelBtn = modalEl.querySelector('#confirm-dialog-cancel');
    const okBtn = modalEl.querySelector('#confirm-dialog-ok');

    cancelBtn.addEventListener('click', () => {
      resolved = true;
      modal.close(modalId);
      resolve(false);
    });

    okBtn.addEventListener('click', () => {
      resolved = true;
      modal.close(modalId);
      resolve(true);
    });
  });
}
