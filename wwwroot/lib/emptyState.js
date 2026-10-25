/**
 * Empty State Component - Hiển thị giao diện khi danh sách trống
 */

import { icon } from './icons.js';
import { escapeHtml } from '../core/utils.js';

/**
 * Render HTML cho Empty State
 * @param {Object} options 
 * @returns {string}
 */
export function renderEmptyState({
  iconName = 'layers',
  title = 'Không tìm thấy dữ liệu',
  message = 'Chưa có thông tin nào được ghi nhận hoặc không có kết quả phù hợp với bộ lọc.',
  actionText = '',
  actionId = '',
  actionIcon = 'plus'
}) {
  const actionButton = actionText
    ? `
      <button type="button" class="btn btn-primary" id="${escapeHtml(actionId)}">
        ${actionIcon ? icon(actionIcon, { size: 16 }) : ''}
        <span>${escapeHtml(actionText)}</span>
      </button>
    `
    : '';

  return `
    <div class="empty-state">
      <div class="empty-state-icon">
        ${icon(iconName, { size: 40 })}
      </div>
      <h3 class="empty-state-title">${escapeHtml(title)}</h3>
      <p class="empty-state-text">${escapeHtml(message)}</p>
      ${actionButton}
    </div>
  `;
}
