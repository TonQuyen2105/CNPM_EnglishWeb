/**
 * FacilitiesView - Giao diện Quản lý Cơ sở vật chất (2 Tabs: Phòng học & Khung giờ)
 */

import { icon } from '../../ui/icons.js';
import { escapeHtml } from '../../core/utils.js';

export class FacilitiesView {
  static renderLayout(activeTab = 'rooms') {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Quản lý cơ sở vật chất</h1>
          <p class="page-header-subtitle">Hệ thống phòng học đa năng và các khung giờ ca học chuẩn tại trung tâm</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-primary" id="btn-add-facility">
            ${icon('plus', { size: 16 })}
            <span>${activeTab === 'rooms' ? 'Thêm phòng học mới' : 'Thêm khung giờ mới'}</span>
          </button>
        </div>
      </div>

      <!-- 2 Tabs Navigation -->
      <div class="tabs-nav">
        <button type="button" class="tab-btn ${activeTab === 'rooms' ? 'active' : ''}" data-tab="rooms">
          ${icon('map-pin', { size: 16 })}
          <span>Phòng học đào tạo</span>
        </button>
        <button type="button" class="tab-btn ${activeTab === 'slots' ? 'active' : ''}" data-tab="slots">
          ${icon('clock', { size: 16 })}
          <span>Khung giờ & Ca học</span>
        </button>
      </div>

      <!-- Table Outlet -->
      <div id="facilities-table-outlet"></div>
    `;
  }

  static renderRoomsTable(rooms) {
    return `
      <div class="card">
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Tên phòng</th>
                <th>Vị trí</th>
                <th>Sức chứa</th>
                <th>Trang thiết bị & Mô tả</th>
                <th>Kích hoạt</th>
                <th style="text-align: right;">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              ${rooms.length === 0 ? `<tr><td colspan="6" class="text-center p-6 text-muted">Chưa có phòng học nào.</td></tr>` : ''}
              ${rooms.map(r => `
                <tr>
                  <td><strong>${escapeHtml(r.name)}</strong></td>
                  <td>${escapeHtml(r.location)}</td>
                  <td><span class="badge badge-primary">${r.capacity} chỗ</span></td>
                  <td><small style="color: var(--text-secondary);">${escapeHtml(r.description || '—')}</small></td>
                  <td>
                    <label class="form-switch">
                      <input type="checkbox" class="room-status-switch" data-id="${r.id}" ${r.is_active === 1 || r.is_active === true ? 'checked' : ''}>
                      <span class="switch-slider"></span>
                    </label>
                  </td>
                  <td style="text-align: right;">
                    <button type="button" class="btn btn-secondary btn-sm btn-edit-room" data-id="${r.id}">
                      ${icon('edit', { size: 14 })}
                    </button>
                    <button type="button" class="btn btn-danger-soft btn-sm btn-delete-room" data-id="${r.id}">
                      ${icon('trash', { size: 14 })}
                    </button>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  static renderSlotsTable(slots) {
    return `
      <div class="card">
        <div class="table-container" style="border: none; border-radius: 0;">
          <table class="table">
            <thead>
              <tr>
                <th>Tên ca học</th>
                <th>Thời gian</th>
                <th>Mô tả / Lịch áp dụng</th>
                <th>Kích hoạt</th>
                <th style="text-align: right;">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              ${slots.length === 0 ? `<tr><td colspan="5" class="text-center p-6 text-muted">Chưa có khung giờ nào.</td></tr>` : ''}
              ${slots.map(s => `
                <tr>
                  <td><strong>${escapeHtml(s.name)}</strong></td>
                  <td>
                    <span class="badge badge-info">${s.start_time} - ${s.end_time}</span>
                  </td>
                  <td><small style="color: var(--text-secondary);">${escapeHtml(s.description || '—')}</small></td>
                  <td>
                    <label class="form-switch">
                      <input type="checkbox" class="slot-status-switch" data-id="${s.id}" ${s.is_active === 1 || s.is_active === true ? 'checked' : ''}>
                      <span class="switch-slider"></span>
                    </label>
                  </td>
                  <td style="text-align: right;">
                    <button type="button" class="btn btn-secondary btn-sm btn-edit-slot" data-id="${s.id}">
                      ${icon('edit', { size: 14 })}
                    </button>
                    <button type="button" class="btn btn-danger-soft btn-sm btn-delete-slot" data-id="${s.id}">
                      ${icon('trash', { size: 14 })}
                    </button>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  static renderRoomModal(room = null) {
    const isEdit = !!room;
    return `
      <form id="room-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-room-name">Tên phòng học <span class="required">*</span></label>
          <input type="text" id="modal-room-name" class="form-control" placeholder="ví dụ: Phòng Lab 102" value="${isEdit ? escapeHtml(room.name) : ''}" required>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-room-location">Vị trí tòa nhà</label>
            <input type="text" id="modal-room-location" class="form-control" placeholder="ví dụ: Tầng 2 - Tòa A" value="${isEdit ? escapeHtml(room.location) : ''}" required>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-room-capacity">Sức chứa tối đa (chỗ)</label>
            <input type="number" id="modal-room-capacity" class="form-control" value="${isEdit ? room.capacity : 25}" min="5" max="100" required>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-room-desc">Mô tả thiết bị</label>
          <textarea id="modal-room-desc" class="form-control" rows="3" placeholder="Máy chiếu, máy tính, bảng thông minh...">${isEdit ? escapeHtml(room.description || '') : ''}</textarea>
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-room">${isEdit ? 'Lưu thay đổi' : 'Tạo phòng học'}</button>
        </div>
      </form>
    `;
  }

  static renderSlotModal(slot = null) {
    const isEdit = !!slot;
    return `
      <form id="slot-modal-form">
        <div class="form-group">
          <label class="form-label" for="modal-slot-name">Tên ca học <span class="required">*</span></label>
          <input type="text" id="modal-slot-name" class="form-control" placeholder="ví dụ: Ca Tối 3" value="${isEdit ? escapeHtml(slot.name) : ''}" required>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="form-group">
            <label class="form-label" for="modal-slot-start">Giờ bắt đầu</label>
            <input type="time" id="modal-slot-start" class="form-control" value="${isEdit ? slot.start_time : '18:00'}" required>
          </div>

          <div class="form-group">
            <label class="form-label" for="modal-slot-end">Giờ kết thúc</label>
            <input type="time" id="modal-slot-end" class="form-control" value="${isEdit ? slot.end_time : '19:30'}" required>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label" for="modal-slot-desc">Ghi chú áp dụng</label>
          <input type="text" id="modal-slot-desc" class="form-control" placeholder="ví dụ: T2, T4, T6 tối" value="${isEdit ? escapeHtml(slot.description || '') : ''}">
        </div>

        <div class="modal-footer" style="margin: 1.5rem -1.25rem -1.25rem; padding: 1rem 1.25rem;">
          <button type="button" class="btn btn-secondary" data-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary" id="btn-save-slot">${isEdit ? 'Lưu thay đổi' : 'Tạo ca học'}</button>
        </div>
      </form>
    `;
  }
}
