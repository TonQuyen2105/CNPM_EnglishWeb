/**
 * CoursesController - Điều phối quản lý khóa học và chi tiết bài học
 */

import { CoursesView } from '../../views/admin/CoursesView.js';
import { CourseDetailView } from '../../views/admin/CourseDetailView.js';
import { CourseModel, SubjectModel, LessonModel, CourseClassModel } from '../../models/index.js';
import { modal } from '../../ui/modal.js';
import { confirmDialog } from '../../ui/confirmDialog.js';
import { toast } from '../../ui/toast.js';

export class CoursesController {
  // ROUTE 1: DANH SÁCH KHÓA HỌC (#/courses)
  static async render(params, query, container) {
    this.container = container;
    this.courses = [];
    this.subjects = [];
    this.searchTerm = '';
    this.subjectFilter = 'all';
    this.levelFilter = 'all';
    this.publishedFilter = 'all';

    this.subjects = await SubjectModel.getAll();
    container.innerHTML = CoursesView.renderLayout(this.subjects);
    await this.loadCourses();
    this.bindListEvents();
  }

  static async loadCourses() {
    this.courses = await CourseModel.getAllWithSubject();
    this.renderCourseGrid();
  }

  static renderCourseGrid() {
    const gridOutlet = this.container.querySelector('#courses-grid-outlet');
    if (!gridOutlet) return;

    let filtered = [...this.courses];

    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      filtered = filtered.filter(c => c.title && c.title.toLowerCase().includes(q));
    }

    if (this.subjectFilter !== 'all') {
      filtered = filtered.filter(c => String(c.subject_id) === String(this.subjectFilter));
    }

    if (this.levelFilter !== 'all') {
      filtered = filtered.filter(c => c.level === this.levelFilter);
    }

    if (this.publishedFilter !== 'all') {
      const isPub = this.publishedFilter === 'published' ? 1 : 0;
      filtered = filtered.filter(c => (c.is_published === 1 || c.is_published === true ? 1 : 0) === isPub);
    }

    if (filtered.length === 0) {
      gridOutlet.innerHTML = `
        <div class="col-span-3 card p-8 text-center text-muted">
          Không tìm thấy khóa học nào phù hợp với bộ lọc.
        </div>
      `;
      return;
    }

    gridOutlet.innerHTML = filtered.map(c => CoursesView.renderCourseCard(c)).join('');
    this.bindCardEvents(gridOutlet);
  }

  static bindListEvents() {
    const searchInput = this.container.querySelector('#course-search-input');
    const subjectFilter = this.container.querySelector('#course-subject-filter');
    const levelFilter = this.container.querySelector('#course-level-filter');
    const publishedFilter = this.container.querySelector('#course-published-filter');
    const addBtn = this.container.querySelector('#btn-add-course');

    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this.searchTerm = e.target.value.trim();
        this.renderCourseGrid();
      });
    }

    if (subjectFilter) {
      subjectFilter.addEventListener('change', (e) => {
        this.subjectFilter = e.target.value;
        this.renderCourseGrid();
      });
    }

    if (levelFilter) {
      levelFilter.addEventListener('change', (e) => {
        this.levelFilter = e.target.value;
        this.renderCourseGrid();
      });
    }

    if (publishedFilter) {
      publishedFilter.addEventListener('change', (e) => {
        this.publishedFilter = e.target.value;
        this.renderCourseGrid();
      });
    }

    if (addBtn) {
      addBtn.addEventListener('click', () => this.openCourseModal(null));
    }
  }

  static bindCardEvents(container) {
    const toggleBtns = container.querySelectorAll('.btn-toggle-publish');
    toggleBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const currentPub = btn.getAttribute('data-published') === '1';
        const newStatus = currentPub ? 0 : 1;

        btn.classList.add('loading');
        btn.disabled = true;

        await CourseModel.update(id, { is_published: newStatus });
        toast.success(`Đã ${newStatus ? 'xuất bản' : 'chuyển sang bản nháp'} khóa học!`);
        await this.loadCourses();
      });
    });
  }

  static openCourseModal(course = null) {
    const isEdit = !!course;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa khóa học' : 'Tạo khóa học mới',
      body: CoursesView.renderCourseModal(this.subjects, course),
      maxWidth: '560px'
    });

    const form = document.getElementById('course-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const title = document.getElementById('modal-course-title').value.trim();
      const subjectId = parseInt(document.getElementById('modal-course-subject').value, 10);
      const level = document.getElementById('modal-course-level').value;
      const price = parseFloat(document.getElementById('modal-course-price').value) || 0;
      const thumbnail = document.getElementById('modal-course-thumbnail').value.trim();
      const desc = document.getElementById('modal-course-desc').value.trim();
      const published = document.getElementById('modal-course-published').checked ? 1 : 0;

      if (!title) {
        toast.warning('Vui lòng nhập tên khóa học.');
        return;
      }

      if (isEdit) {
        await CourseModel.update(course.id, {
          title,
          subject_id: subjectId,
          level,
          price,
          thumbnail_url: thumbnail || course.thumbnail_url,
          description: desc,
          is_published: published
        });
        toast.success('Đã cập nhật khóa học thành công!');
      } else {
        await CourseModel.create({
          title,
          subject_id: subjectId,
          level,
          price,
          thumbnail_url: thumbnail || 'https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=400',
          description: desc,
          is_published: published
        });
        toast.success('Đã tạo khóa học mới thành công!');
      }

      modal.close(modalId);
      await this.loadCourses();
    });
  }

  // ROUTE 2: CHI TIẾT KHÓA HỌC (#/courses/:id)
  static async renderDetail(params, query, container) {
    this.container = container;
    this.courseId = params.id;
    this.activeTab = query && query.tab ? query.tab : 'lessons';

    await this.loadCourseDetail();
  }

  static async loadCourseDetail() {
    this.course = await CourseModel.getByIdWithDetails(this.courseId);
    if (!this.course) {
      this.container.innerHTML = `
        <div class="card p-8 text-center text-danger">
          Không tìm thấy thông tin khóa học (#${this.courseId}).
          <div style="margin-top: 1rem;"><a href="#/courses" class="btn btn-secondary">Quay lại danh sách</a></div>
        </div>
      `;
      return;
    }

    this.subjects = await SubjectModel.getAll();
    const allClasses = await CourseClassModel.getAllWithDetails();
    this.course.classes = allClasses.filter(c => String(c.course_id) === String(this.course.id));

    this.container.innerHTML = CourseDetailView.render({
      course: this.course,
      subjects: this.subjects,
      activeTab: this.activeTab
    });

    this.bindDetailEvents();
  }

  static bindDetailEvents() {
    // 1. Chuyển Tab
    const tabBtns = this.container.querySelectorAll('.tab-btn');
    tabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const targetTab = btn.getAttribute('data-tab');
        this.activeTab = targetTab;
        this.loadCourseDetail();
      });
    });

    // 2. Tab Bài học: Lọc theo loại bài học
    const typeFilter = this.container.querySelector('#lesson-type-filter');
    if (typeFilter) {
      typeFilter.addEventListener('change', (e) => {
        const val = e.target.value;
        const rows = this.container.querySelectorAll('.lesson-row');
        rows.forEach(row => {
          if (val === 'all' || row.getAttribute('data-type') === val) {
            row.style.display = '';
          } else {
            row.style.display = 'none';
          }
        });
      });
    }

    // 3. Tab Bài học: Thêm bài học mới
    const addLessonBtn = this.container.querySelector('#btn-add-lesson');
    if (addLessonBtn) {
      addLessonBtn.addEventListener('click', () => this.openLessonModal(null));
    }

    // 4. Tab Bài học: Di chuyển thứ tự Lên / Xuống (order_index)
    const moveUpBtns = this.container.querySelectorAll('.btn-move-lesson-up');
    const moveDownBtns = this.container.querySelectorAll('.btn-move-lesson-down');

    moveUpBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        await this.swapLessonOrder(id, -1);
      });
    });

    moveDownBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        await this.swapLessonOrder(id, 1);
      });
    });

    // 5. Tab Bài học: Sửa / Xóa bài học
    const editLessonBtns = this.container.querySelectorAll('.btn-edit-lesson');
    editLessonBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const lesson = await LessonModel.getById(id);
        if (lesson) this.openLessonModal(lesson);
      });
    });

    const delLessonBtns = this.container.querySelectorAll('.btn-delete-lesson');
    delLessonBtns.forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = btn.getAttribute('data-id');
        const lesson = await LessonModel.getById(id);
        if (!lesson) return;

        const ok = await confirmDialog({
          title: 'Xác nhận xóa bài học',
          message: `Bạn có chắc muốn xóa bài học "${lesson.title}" khỏi khóa học?`,
          confirmText: 'Xóa bài học',
          isDanger: true
        });

        if (ok) {
          await LessonModel.delete(id);
          toast.success('Đã xóa bài học thành công!');
          await this.loadCourseDetail();
        }
      });
    });

    // 6. Tab Thông tin: Lưu form thông tin khóa học
    const infoForm = this.container.querySelector('#course-info-form');
    if (infoForm) {
      infoForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const title = document.getElementById('edit-course-title').value.trim();
        const subjectId = parseInt(document.getElementById('edit-course-subject').value, 10);
        const level = document.getElementById('edit-course-level').value;
        const price = parseFloat(document.getElementById('edit-course-price').value) || 0;
        const thumbnail = document.getElementById('edit-course-thumbnail').value.trim();
        const desc = document.getElementById('edit-course-desc').value.trim();
        const published = document.getElementById('edit-course-published').checked ? 1 : 0;

        await CourseModel.update(this.course.id, {
          title,
          subject_id: subjectId,
          level,
          price,
          thumbnail_url: thumbnail || this.course.thumbnail_url,
          description: desc,
          is_published: published
        });

        toast.success('Đã cập nhật thông tin khóa học thành công!');
        await this.loadCourseDetail();
      });
    }
  }

  // Đổi thứ tự 2 bài học kề nhau
  static async swapLessonOrder(lessonId, direction) {
    const lessons = [...this.course.lessons].sort((a, b) => a.order_index - b.order_index);
    const currIndex = lessons.findIndex(l => String(l.id) === String(lessonId));
    if (currIndex === -1) return;

    const targetIndex = currIndex + direction;
    if (targetIndex < 0 || targetIndex >= lessons.length) return;

    const currentLesson = lessons[currIndex];
    const targetLesson = lessons[targetIndex];

    const tempOrder = currentLesson.order_index;
    currentLesson.order_index = targetLesson.order_index;
    targetLesson.order_index = tempOrder;

    await LessonModel.update(currentLesson.id, { order_index: currentLesson.order_index });
    await LessonModel.update(targetLesson.id, { order_index: targetLesson.order_index });

    toast.success('Đã cập nhật thứ tự bài học!');
    await this.loadCourseDetail();
  }

  static openLessonModal(lesson = null) {
    const isEdit = !!lesson;
    const modalId = modal.open({
      title: isEdit ? 'Chỉnh sửa bài học' : 'Thêm bài học mới',
      body: CourseDetailView.renderLessonModal(this.course.id, lesson),
      maxWidth: '540px'
    });

    const form = document.getElementById('lesson-modal-form');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
      e.preventDefault();

      const title = document.getElementById('modal-lesson-title').value.trim();
      const type = document.getElementById('modal-lesson-type').value;
      const order = parseInt(document.getElementById('modal-lesson-order').value, 10) || 1;
      const content = document.getElementById('modal-lesson-content').value.trim();

      if (!title) {
        toast.warning('Vui lòng nhập tên bài học.');
        return;
      }

      if (isEdit) {
        await LessonModel.update(lesson.id, {
          title,
          content_type: type,
          order_index: order,
          content
        });
        toast.success('Đã cập nhật bài học thành công!');
      } else {
        await LessonModel.create({
          course_id: Number(this.course.id),
          title,
          content_type: type,
          order_index: order || (this.course.lessons.length + 1),
          content
        });
        toast.success('Đã thêm bài học mới thành công!');
      }

      modal.close(modalId);
      await this.loadCourseDetail();
    });
  }
}
