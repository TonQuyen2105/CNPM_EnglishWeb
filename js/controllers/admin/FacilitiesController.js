/**
 * FacilitiesController - Điều phối quản lý Cơ sở vật chất (Phòng học & Khung giờ)
 */

import { FacilitiesView } from '../../views/admin/FacilitiesView.js';
import { ClassroomModel, TimeSlotModel } from '../../models/index.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';

export class FacilitiesController {
  static async render(params, query, container) {
    this.container = container;
    this.activeTab = query && query.tab ? query.tab : 'rooms';

    container.innerHTML = FacilitiesView.renderLayout(this.activeTab);
    await this.loadTabContent();
    this.bindTabEvents();
  }

  static async loadTabContent() {
    const tableOutlet = this.container.querySelector('#facilities-table-outlet');
    if (!tableOutlet) return;

    if (this.activeTab === 'rooms') {
      this.rooms = await ClassroomModel.getAll();
      tableOutlet.innerHTML = FacilitiesView.renderRoomsTable(this.rooms);
      this.bindRoomEvents(tableOutlet);
    } else {
      this.slots = await TimeSlotModel.getAll();
      tableOutlet.innerHTML = FacilitiesView.renderSlotsTable(this.slots);
      this.bindSlotEvents(tableOutlet);
    }
  }

  static bindTabEvents() {
    const tabBtns = this.container.querySelectorAll('.tab-btn');
    tabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        this.activeTab = btn.getAttribute('data-tab');
        this.container.innerHTML = FacilitiesView.renderLayout(this.activeTab);
        this.loadTabContent();
        this.bindTabEvents();
      });
    });

    const addBtn = this.container.querySelector('#btn-add-facility');
    if (addBtn) {
      addBtn.addEventListener('click', () => {
        if (this.activeTab === 'rooms') {
          this.openRoomModal(null);
        } else {
          this.openSlotModal(null);
        }
      });
    }
  }

  static bindRoomEvents(container) {
    // Switch active
    const switches = container.querySelectorAll('.room-status-switch');
    switches.forEach(sw => {
      sw.addEventListener('change', async () => {
        const id = sw.getAttribute('data-id');
        const active = sw.checked ? 1 : 0;
        await ClassroomModel.update(id, { is_active: active });
        toast.success(`Đã ${active ? 'kích hoạt' : 'tạm ngưng'} phòng học!`);
      });
    });

    // Sửa
    const editBtns = container.querySelectorAll('.btn-edit-room');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const room = await ClassroomModel.getById(id);
        if (room) this.openRoomModal(room);
      });
    });

    // Xóa
    const delBtns = container.querySelectorAll('.btn-delete-room');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const room = await ClassroomModel.getById(id);
        if (!room) return;

        const ok = await confirmDialog({
          title: 'Xóa phòng học',
          message: `Bạn có chắc muốn xóa phòng học "${room.name}"?`,
          confirmText: 'Xóa phòng học',
          isDanger: true
        });

        if (ok) {
          await ClassroomModel.delete(id);
          toast.success(`Đã xóa phòng học ${room.name}!`);
          await this.loadTabContent();
        }
      });
    });
  }

  static bindSlotEvents(container) {
    // Switch active
    const switches = container.querySelectorAll('.slot-status-switch');
    switches.forEach(sw => {
      sw.addEventListener('change', async () => {
        const id = sw.getAttribute('data-id');
        const active = sw.checked ? 1 : 0;
        await TimeSlotModel.update(id, { is_active: active });
        toast.success(`Đã ${active ? 'kích hoạt' : 'tạm ngưng'} ca học!`);
      });
    });

    // Sửa
    const editBtns = container.querySelectorAll('.btn-edit-slot');
    editBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const slot = await TimeSlotModel.getById(id);
        if (slot) this.openSlotModal(slot);
      });
    });

    // Xóa
    const delBtns = container.querySelectorAll('.btn-delete-slot');
    delBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const slot = await TimeSlotModel.getById(id);
        if (!slot) return;

        const ok = await confirmDialog({
          title: 'Xóa ca học',
          message: `Bạn có chắc muốn xóa ca học "${slot.name}" (${slot.start_time} - ${slot.end_time})?`,
          confirmText: 'Xóa ca học',
          isDanger: true
        });

        if (ok) {
          await TimeSlotModel.delete(id);
          toast.success(`Đã xóa ca học ${slot.name}!`);
          await this.loadTabContent();
        }
      });
    });
  }

  static openRoomModal(room = null) {
    const isEdit = !!room;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa phòng học' : 'Thêm phòng học mới',
      body: FacilitiesView.renderRoomModal(room),
      maxWidth: '520px'
    });

    const form = document.getElementById('room-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const name = document.getElementById('modal-room-name').value.trim();
      const location = document.getElementById('modal-room-location').value.trim();
      const capacity = parseInt(document.getElementById('modal-room-capacity').value, 10) || 20;
      const desc = document.getElementById('modal-room-desc').value.trim();

      if (!name) {
        toast.warning('Vui lòng nhập tên phòng học.');
        return;
      }

      if (isEdit) {
        await ClassroomModel.update(room.id, {
          name,
          location,
          capacity,
          description: desc
        });
        toast.success('Đã cập nhật phòng học thành công!');
      } else {
        await ClassroomModel.create({
          name,
          location,
          capacity,
          description: desc,
          is_active: 1
        });
        toast.success('Đã thêm phòng học mới thành công!');
      }

      modal.close(modalId);
      await this.loadTabContent();
    });
  }

  static openSlotModal(slot = null) {
    const isEdit = !!slot;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa khung giờ ca học' : 'Thêm khung giờ ca học mới',
      body: FacilitiesView.renderSlotModal(slot),
      maxWidth: '520px'
    });

    const form = document.getElementById('slot-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const name = document.getElementById('modal-slot-name').value.trim();
      const start = document.getElementById('modal-slot-start').value;
      const end = document.getElementById('modal-slot-end').value;
      const desc = document.getElementById('modal-slot-desc').value.trim();

      if (!name || !start || !end) {
        toast.warning('Vui lòng nhập đầy đủ thông tin ca học.');
        return;
      }

      if (isEdit) {
        await TimeSlotModel.update(slot.id, {
          name,
          start_time: start,
          end_time: end,
          description: desc
        });
        toast.success('Đã cập nhật khung giờ thành công!');
      } else {
        await TimeSlotModel.create({
          name,
          start_time: start,
          end_time: end,
          description: desc,
          is_active: 1
        });
        toast.success('Đã thêm khung giờ mới thành công!');
      }

      modal.close(modalId);
      await this.loadTabContent();
    });
  }
}
