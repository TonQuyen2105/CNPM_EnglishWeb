/**
 * Skeleton Loading Component - Khung hiển thị tạm khi đang tải dữ liệu
 */

/**
 * Tạo HTML Skeleton cho bảng dữ liệu
 * @param {number} rows Số hàng
 * @param {number} cols Số cột
 * @returns {string}
 */
export function renderTableSkeleton(rows = 5, cols = 4) {
  let rowsHtml = '';
  for (let r = 0; r < rows; r++) {
    let cellsHtml = '';
    for (let c = 0; c < cols; c++) {
      cellsHtml += `
        <td>
          <div class="skeleton skeleton-text" style="width: ${c === 0 ? '70%' : c === cols - 1 ? '40%' : '90%'};"></div>
        </td>
      `;
    }
    rowsHtml += `<tr>${cellsHtml}</tr>`;
  }

  return `
    <div class="table-container">
      <table class="table">
        <tbody>
          ${rowsHtml}
        </tbody>
      </table>
    </div>
  `;
}

/**
 * Tạo HTML Skeleton cho danh sách Card
 * @param {number} count Số lượng card
 * @returns {string}
 */
export function renderCardsSkeleton(count = 3) {
  let cardsHtml = '';
  for (let i = 0; i < count; i++) {
    cardsHtml += `
      <div class="card" style="padding: 1.5rem;">
        <div class="skeleton skeleton-rect" style="height: 140px; margin-bottom: 1rem; border-radius: 12px;"></div>
        <div class="skeleton skeleton-text" style="width: 80%; margin-bottom: 0.5rem;"></div>
        <div class="skeleton skeleton-text" style="width: 50%; margin-bottom: 1rem;"></div>
        <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1rem;">
          <div class="skeleton skeleton-avatar"></div>
          <div class="skeleton skeleton-text" style="width: 60px;"></div>
        </div>
      </div>
    `;
  }
  return `<div class="grid grid-auto-fit gap-6">${cardsHtml}</div>`;
}

/**
 * Tạo HTML Skeleton cho hàng loạt Stat Cards
 * @param {number} count 
 * @returns {string}
 */
export function renderStatCardsSkeleton(count = 4) {
  let html = '';
  for (let i = 0; i < count; i++) {
    html += `
      <div class="stat-card">
        <div style="flex: 1;">
          <div class="skeleton skeleton-text" style="width: 45%; margin-bottom: 8px;"></div>
          <div class="skeleton skeleton-text" style="width: 70%; height: 28px; margin-bottom: 8px;"></div>
          <div class="skeleton skeleton-text" style="width: 35%;"></div>
        </div>
        <div class="skeleton" style="width: 48px; height: 48px; border-radius: 12px;"></div>
      </div>
    `;
  }
  return `<div class="grid grid-cols-4 gap-6">${html}</div>`;
}
