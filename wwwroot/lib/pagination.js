/**
 * Pagination Component - Phân trang dữ liệu
 */

import { icon } from './icons.js';

export class Pagination {
  /**
   * @param {Object} options 
   * @param {number} options.totalItems Tổng số phần tử
   * @param {number} options.pageSize Kích thước trang
   * @param {number} options.currentPage Trang hiện tại (1-based)
   * @param {Function} options.onPageChange Callback khi đổi trang
   */
  constructor({ totalItems = 0, pageSize = 10, currentPage = 1, onPageChange = null }) {
    this.totalItems = totalItems;
    this.pageSize = pageSize;
    this.currentPage = currentPage;
    this.onPageChange = onPageChange;
  }

  get totalPages() {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  render() {
    if (this.totalItems <= 0) return '';

    const startItem = (this.currentPage - 1) * this.pageSize + 1;
    const endItem = Math.min(this.currentPage * this.pageSize, this.totalItems);

    let pagesHtml = '';
    const total = this.totalPages;

    for (let p = 1; p <= total; p++) {
      if (p === 1 || p === total || (p >= this.currentPage - 1 && p <= this.currentPage + 1)) {
        pagesHtml += `
          <button type="button" class="page-btn ${p === this.currentPage ? 'active' : ''}" data-page="${p}">
            ${p}
          </button>
        `;
      } else if (p === this.currentPage - 2 || p === this.currentPage + 2) {
        pagesHtml += `<span style="padding: 0 4px; color: var(--text-muted);">...</span>`;
      }
    }

    return `
      <div class="pagination">
        <div class="pagination-info">
          Hiển thị <strong>${startItem} - ${endItem}</strong> trong tổng số <strong>${this.totalItems}</strong> bản ghi
        </div>
        <div class="pagination-pages">
          <button type="button" class="page-btn" data-page="${this.currentPage - 1}" ${this.currentPage <= 1 ? 'disabled' : ''} aria-label="Trang trước">
            ${icon('chevron-left', { size: 16 })}
          </button>
          ${pagesHtml}
          <button type="button" class="page-btn" data-page="${this.currentPage + 1}" ${this.currentPage >= total ? 'disabled' : ''} aria-label="Trang sau">
            ${icon('chevron-right', { size: 16 })}
          </button>
        </div>
      </div>
    `;
  }

  attachEvents(container) {
    if (!container) return;
    const buttons = container.querySelectorAll('.page-btn:not(:disabled)');
    buttons.forEach(btn => {
      btn.addEventListener('click', () => {
        const page = parseInt(btn.getAttribute('data-page'), 10);
        if (!isNaN(page) && page >= 1 && page <= this.totalPages && page !== this.currentPage) {
          this.currentPage = page;
          if (typeof this.onPageChange === 'function') {
            this.onPageChange(page);
          }
        }
      });
    });
  }
}
