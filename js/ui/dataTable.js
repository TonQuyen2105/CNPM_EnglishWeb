/**
 * Data Table Component - Bảng dữ liệu tương tác
 * Hỗ trợ: sắp xếp cột, tìm kiếm tức thì, mobile card view, tùy biến cột render
 */

import { icon } from './icons.js';
import { renderEmptyState } from './emptyState.js';

export class DataTable {
  /**
   * @param {Object} options 
   * @param {Array} options.columns Danh sách cột [{ key, label, sortable, render, width }]
   * @param {Array} options.data Dữ liệu hàng
   * @param {string} options.emptyMessage Thông báo khi rỗng
   * @param {Function} options.onRowClick Sự kiện click hàng
   */
  constructor(options = {}) {
    this.columns = options.columns || [];
    this.data = options.data || [];
    this.filteredData = [...this.data];
    this.sortKey = options.sortKey || null;
    this.sortAsc = options.sortAsc !== false;
    this.emptyMessage = options.emptyMessage || 'Không có dữ liệu phù hợp';
    this.onRowClick = options.onRowClick || null;
  }

  setData(newData) {
    this.data = newData || [];
    this.filteredData = [...this.data];
    if (this.sortKey) {
      this._applySort();
    }
  }

  sort(key) {
    if (this.sortKey === key) {
      this.sortAsc = !this.sortAsc;
    } else {
      this.sortKey = key;
      this.sortAsc = true;
    }
    this._applySort();
  }

  _applySort() {
    if (!this.sortKey) return;
    this.filteredData.sort((a, b) => {
      let valA = a[this.sortKey];
      let valB = b[this.sortKey];
      if (typeof valA === 'string') valA = valA.toLowerCase();
      if (typeof valB === 'string') valB = valB.toLowerCase();
      if (valA < valB) return this.sortAsc ? -1 : 1;
      if (valA > valB) return this.sortAsc ? 1 : -1;
      return 0;
    });
  }

  search(term) {
    if (!term || !term.trim()) {
      this.filteredData = [...this.data];
    } else {
      const q = term.toLowerCase().trim();
      this.filteredData = this.data.filter(row => {
        return this.columns.some(col => {
          const val = row[col.key];
          return val !== null && val !== undefined && String(val).toLowerCase().includes(q);
        });
      });
    }
    if (this.sortKey) {
      this._applySort();
    }
  }

  render() {
    if (!this.filteredData || this.filteredData.length === 0) {
      return `
        <div class="table-container">
          ${renderEmptyState({
            title: 'Chưa có bản ghi nào',
            message: this.emptyMessage
          })}
        </div>
      `;
    }

    const headerHtml = this.columns.map(col => {
      const isSorted = this.sortKey === col.key;
      const sortIcon = col.sortable 
        ? `<span class="sort-icon" style="margin-left: 4px; display: inline-flex; vertical-align: middle;">
             ${isSorted ? (this.sortAsc ? icon('chevron-up', { size: 14 }) : icon('chevron-down', { size: 14 })) : ''}
           </span>` 
        : '';
      const sortClass = col.sortable ? 'sortable' : '';
      const widthStyle = col.width ? `style="width: ${col.width};"` : '';

      return `
        <th class="${sortClass}" data-sort="${col.key || ''}" ${widthStyle}>
          <div style="display: flex; align-items: center; justify-content: space-between;">
            <span>${col.label}</span>
            ${sortIcon}
          </div>
        </th>
      `;
    }).join('');

    const bodyHtml = this.filteredData.map((row, rowIndex) => {
      const cellsHtml = this.columns.map(col => {
        let cellContent = '';
        if (typeof col.render === 'function') {
          cellContent = col.render(row[col.key], row, rowIndex);
        } else {
          cellContent = row[col.key] !== undefined && row[col.key] !== null ? row[col.key] : '—';
        }

        return `<td data-label="${col.label}">${cellContent}</td>`;
      }).join('');

      return `<tr data-index="${rowIndex}" class="${this.onRowClick ? 'cursor-pointer' : ''}">${cellsHtml}</tr>`;
    }).join('');

    return `
      <div class="table-container">
        <table class="table table-responsive-cards">
          <thead>
            <tr>${headerHtml}</tr>
          </thead>
          <tbody>
            ${bodyHtml}
          </tbody>
        </table>
      </div>
    `;
  }

  attachEvents(container, onSortChange = null) {
    if (!container) return;
    const ths = container.querySelectorAll('th.sortable');
    ths.forEach(th => {
      th.addEventListener('click', () => {
        const key = th.getAttribute('data-sort');
        if (key) {
          this.sort(key);
          if (onSortChange) onSortChange();
        }
      });
    });

    if (this.onRowClick) {
      const rows = container.querySelectorAll('tbody tr');
      rows.forEach(tr => {
        tr.addEventListener('click', (e) => {
          // Không trigger nếu click vào button/a
          if (e.target.closest('button, a, input, .dropdown')) return;
          const idx = parseInt(tr.getAttribute('data-index'), 10);
          if (!isNaN(idx) && this.filteredData[idx]) {
            this.onRowClick(this.filteredData[idx]);
          }
        });
      });
    }
  }
}
