/**
 * TeacherLessonsController - Điều phối Soạn bài giảng & Quản lý bài học
 */

import { auth } from '../../core/auth.js';
import { CourseModel } from '../../models/CourseModel.js';
import { LessonModel } from '../../models/LessonModel.js';
import { TeacherLessonsView } from '../../views/teacher/TeacherLessonsView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { escapeHtml } from '../../core/utils.js';

export class TeacherLessonsController {
  static selectedCourseId = null;
  static selectedType = 'all';

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    // Lấy danh sách khóa học
    const courses = await CourseModel.getAll();
    if (!courses || courses.length === 0) {
      container.innerHTML = `
        <div class="empty-state">
          <h3>Chưa có khóa học nào</h3>
          <p>Hệ thống chưa có khóa học để soạn bài giảng.</p>
        </div>
      `;
      return;
    }

    if (!TeacherLessonsController.selectedCourseId) {
      TeacherLessonsController.selectedCourseId = courses[0].id;
    }

    await TeacherLessonsController.updateView(container, courses);
  }

  static async updateView(container, courses) {
    // Lấy danh sách bài học của khóa học được chọn
    let lessons = await LessonModel.getByCourse(TeacherLessonsController.selectedCourseId);

    // Lọc theo content_type nếu có
    if (TeacherLessonsController.selectedType !== 'all') {
      lessons = lessons.filter(l => l.content_type === TeacherLessonsController.selectedType);
    }

    // Sắp xếp theo order_index
    lessons.sort((a, b) => (a.order_index || 0) - (b.order_index || 0));

    container.innerHTML = TeacherLessonsView.render({
      courses,
      selectedCourseId: TeacherLessonsController.selectedCourseId,
      lessons,
      selectedType: TeacherLessonsController.selectedType
    });

    TeacherLessonsController.bindEvents(container, courses, lessons);
  }

  static bindEvents(container, courses, currentLessons) {
    // 1. Thay đổi Khóa học
    const courseSelect = container.querySelector('#filter-course');
    if (courseSelect) {
      courseSelect.addEventListener('change', (e) => {
        TeacherLessonsController.selectedCourseId = Number(e.target.value);
        TeacherLessonsController.updateView(container, courses);
      });
    }

    // 2. Lọc loại nội dung
    const typeSelect = container.querySelector('#filter-type');
    if (typeSelect) {
      typeSelect.addEventListener('change', (e) => {
        TeacherLessonsController.selectedType = e.target.value;
        TeacherLessonsController.updateView(container, courses);
      });
    }

    // 3. Soạn bài học mới
    container.querySelectorAll('.btn-create-lesson').forEach(btn => {
      btn.addEventListener('click', () => {
        const nextOrderIndex = currentLessons.length > 0 
          ? Math.max(...currentLessons.map(l => l.order_index || 0)) + 1 
          : 1;

        modal.open({
          title: 'Soạn thảo bài giảng mới',
          content: TeacherLessonsView.renderEditorForm(null, nextOrderIndex),
          size: 'large',
          confirmText: 'Lưu bài giảng',
          cancelText: 'Hủy',
          onOpen: () => {
            TeacherLessonsController.bindLivePreview();
          },
          onConfirm: async () => {
            const titleInput = document.getElementById('lesson-title-input');
            const orderInput = document.getElementById('lesson-order-input');
            const typeInput = document.getElementById('lesson-type-input');
            const contentInput = document.getElementById('lesson-content-input');

            if (!titleInput || !titleInput.value.trim()) {
              toast.error('Vui lòng nhập tên bài học!');
              return false;
            }

            await LessonModel.create({
              course_id: Number(TeacherLessonsController.selectedCourseId),
              title: titleInput.value.trim(),
              order_index: Number(orderInput.value) || 1,
              content_type: typeInput.value,
              content: contentInput.value.trim()
            });

            toast.success('Đã tạo và lưu bài giảng thành công!');
            TeacherLessonsController.updateView(container, courses);
          }
        });
      });
    });

    // 4. Chỉnh sửa bài giảng
    container.querySelectorAll('.btn-edit-lesson').forEach(btn => {
      btn.addEventListener('click', async () => {
        const lessonId = btn.getAttribute('data-lesson-id');
        const lesson = await LessonModel.getById(lessonId);
        if (!lesson) return;

        modal.open({
          title: `Chỉnh sửa: ${lesson.title}`,
          content: TeacherLessonsView.renderEditorForm(lesson),
          size: 'large',
          confirmText: 'Cập nhật bài giảng',
          cancelText: 'Hủy',
          onOpen: () => {
            TeacherLessonsController.bindLivePreview();
          },
          onConfirm: async () => {
            const titleInput = document.getElementById('lesson-title-input');
            const orderInput = document.getElementById('lesson-order-input');
            const typeInput = document.getElementById('lesson-type-input');
            const contentInput = document.getElementById('lesson-content-input');

            if (!titleInput || !titleInput.value.trim()) {
              toast.error('Vui lòng nhập tên bài học!');
              return false;
            }

            await LessonModel.update(lesson.id, {
              title: titleInput.value.trim(),
              order_index: Number(orderInput.value) || 1,
              content_type: typeInput.value,
              content: contentInput.value.trim()
            });

            toast.success('Đã cập nhật bài giảng thành công!');
            TeacherLessonsController.updateView(container, courses);
          }
        });
      });
    });
  }

  /**
   * Lắng nghe sự kiện gõ trên textarea để render Live Preview tức thì
   */
  static bindLivePreview() {
    const textarea = document.getElementById('lesson-content-input');
    const preview = document.getElementById('lesson-live-preview');
    if (textarea && preview) {
      textarea.addEventListener('input', () => {
        const text = textarea.value.trim();
        if (text) {
          preview.innerHTML = escapeHtml(text);
        } else {
          preview.innerHTML = '<span style="color: var(--text-muted); font-style: italic;">Gõ nội dung vào ô bên trái để xem trước...</span>';
        }
      });
    }
  }
}
