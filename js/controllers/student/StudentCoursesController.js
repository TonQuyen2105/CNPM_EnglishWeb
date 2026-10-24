/**
 * StudentCoursesController - Quản lý tương tác Khám phá & Đăng ký Khóa học
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  CourseModel,
  SubjectModel,
  CourseClassModel,
  EnrollmentModel
} from '../../models/index.js';
import { StudentCoursesView } from '../../views/student/StudentCoursesView.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';

export class StudentCoursesController {
  static _state = {
    keyword: '',
    subjectId: '',
    level: '',
    sortBy: 'newest'
  };

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    if (params && params.id) {
      return this._renderDetail(container, params.id);
    }

    return this._renderList(container);
  }

  static async _renderList(container) {
    const currentUser = auth.getCurrentUser();
    const allCourses = await CourseModel.getAllWithSubject();
    const subjects = await SubjectModel.getAll(s => s.is_active === 1 || s.is_active === true);
    const classes = storage.getTable('course_classes') || [];
    const reviews = storage.getTable('course_reviews') || [];

    // Chỉ hiển thị các khóa học đã xuất bản (is_published = 1)
    let publishedCourses = allCourses.filter(c => c.is_published === 1 || c.is_published === true);

    // Bổ sung số lớp đang mở & sao đánh giá
    publishedCourses = publishedCourses.map(c => {
      const openClasses = classes.filter(cls => cls.course_id === c.id && (cls.status === 'upcoming' || cls.status === 'ongoing'));
      const courseReviews = reviews.filter(r => r.course_id === c.id);
      const avgRating = courseReviews.length > 0 
        ? (courseReviews.reduce((sum, r) => sum + r.rating, 0) / courseReviews.length).toFixed(1)
        : '5.0';

      return {
        ...c,
        open_classes_count: openClasses.length,
        rating_avg: avgRating,
        review_count: courseReviews.length
      };
    });

    // Lọc theo từ khóa
    if (this._state.keyword) {
      const kw = this._state.keyword.toLowerCase().trim();
      publishedCourses = publishedCourses.filter(c => 
        (c.title && c.title.toLowerCase().includes(kw)) ||
        (c.description && c.description.toLowerCase().includes(kw)) ||
        (c.subject_name && c.subject_name.toLowerCase().includes(kw))
      );
    }

    // Lọc theo môn học
    if (this._state.subjectId) {
      publishedCourses = publishedCourses.filter(c => String(c.subject_id) === String(this._state.subjectId));
    }

    // Lọc theo trình độ
    if (this._state.level) {
      publishedCourses = publishedCourses.filter(c => c.level === this._state.level);
    }

    // Sắp xếp
    if (this._state.sortBy === 'newest') {
      publishedCourses.sort((a, b) => b.id - a.id);
    } else if (this._state.sortBy === 'rating') {
      publishedCourses.sort((a, b) => Number(b.rating_avg) - Number(a.rating_avg));
    } else if (this._state.sortBy === 'price_asc') {
      publishedCourses.sort((a, b) => Number(a.price || 0) - Number(b.price || 0));
    } else if (this._state.sortBy === 'price_desc') {
      publishedCourses.sort((a, b) => Number(b.price || 0) - Number(a.price || 0));
    }

    container.innerHTML = StudentCoursesView.renderCourseList({
      courses: publishedCourses,
      subjects,
      selectedSubject: this._state.subjectId,
      selectedLevel: this._state.level,
      sortBy: this._state.sortBy,
      keyword: this._state.keyword
    });

    this._bindListEvents(container);
  }

  static _bindListEvents(container) {
    const searchInput = container.querySelector('#course-search-input');
    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this._state.keyword = e.target.value;
        this._renderList(container);
      });
    }

    const subjectFilter = container.querySelector('#course-filter-subject');
    if (subjectFilter) {
      subjectFilter.addEventListener('change', (e) => {
        this._state.subjectId = e.target.value;
        this._renderList(container);
      });
    }

    const levelFilter = container.querySelector('#course-filter-level');
    if (levelFilter) {
      levelFilter.addEventListener('change', (e) => {
        this._state.level = e.target.value;
        this._renderList(container);
      });
    }

    const sortSelect = container.querySelector('#course-sort-by');
    if (sortSelect) {
      sortSelect.addEventListener('change', (e) => {
        this._state.sortBy = e.target.value;
        this._renderList(container);
      });
    }

    const resetBtn = container.querySelector('#btn-reset-filters');
    if (resetBtn) {
      resetBtn.addEventListener('click', () => {
        this._state = { keyword: '', subjectId: '', level: '', sortBy: 'newest' };
        this._renderList(container);
      });
    }
  }

  static async _renderDetail(container, courseId) {
    const currentUser = auth.getCurrentUser();
    const course = await CourseModel.getByIdWithDetails(Number(courseId));

    if (!course) {
      container.innerHTML = `
        <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
          <h3>Không tìm thấy khóa học yêu cầu</h3>
          <p style="color: var(--text-muted); margin-bottom: 1.5rem;">Khóa học có thể đã bị gỡ hoặc không tồn tại.</p>
          <a href="#/courses" class="btn btn-primary">Quay lại danh sách khóa học</a>
        </div>
      `;
      return;
    }

    // Lấy các lớp đang mở (upcoming / ongoing)
    const allClasses = storage.getTable('course_classes') || [];
    const enrollments = storage.getTable('enrollments') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];
    const schedules = storage.getTable('class_schedules') || [];
    const slots = storage.getTable('time_slots') || [];
    const rooms = storage.getTable('classrooms') || [];

    const myEnrollments = enrollments.filter(e => String(e.user_id) === String(currentUser.id) && e.status !== 'cancelled');
    const myEnrolledClassIds = myEnrollments.map(e => e.class_id);

    const openClasses = allClasses
      .filter(cls => String(cls.course_id) === String(course.id) && (cls.status === 'upcoming' || cls.status === 'ongoing'))
      .map(cls => {
        const enrolledCount = enrollments.filter(e => e.class_id === cls.id && e.status !== 'cancelled').length;
        
        // Giảng viên
        const mainAssign = classTeachers.find(ct => ct.class_id === cls.id && ct.role === 'main');
        let teacherName = 'Chưa phân công';
        if (mainAssign) {
          const t = teachers.find(item => item.id === mainAssign.teacher_id);
          if (t) {
            const u = users.find(user => user.id === t.user_id);
            if (u) teacherName = u.full_name;
          }
        }

        // Tóm tắt lịch học
        const classSchedules = schedules.filter(s => s.class_id === cls.id);
        const dayNames = { 2: 'Thứ 2', 3: 'Thứ 3', 4: 'Thứ 4', 5: 'Thứ 5', 6: 'Thứ 6', 7: 'Thứ 7', 8: 'Chủ nhật' };
        const scheduleSummary = classSchedules.length > 0
          ? classSchedules.map(s => {
              const sl = slots.find(item => item.id === s.time_slot_id);
              const rm = rooms.find(item => item.id === s.classroom_id);
              return `${dayNames[s.day_of_week] || ''} (${sl ? sl.start_time + '-' + sl.end_time : ''}${rm ? ', ' + rm.name : ''})`;
            }).join(' | ')
          : 'Lịch học linh hoạt';

        return {
          ...cls,
          enrolled_count: enrolledCount,
          teacher_name: teacherName,
          schedule_summary: scheduleSummary
        };
      });

    // Kiểm tra học viên có đang học lớp nào trong khóa này không
    const enrolledClass = openClasses.find(cls => myEnrolledClassIds.includes(cls.id));

    // Bổ sung tên học viên cho từng review
    const allUsers = storage.getTable('users') || [];
    if (course.reviews) {
      course.reviews = course.reviews.map(r => {
        const u = allUsers.find(user => user.id === r.user_id);
        return {
          ...r,
          user_name: u ? u.full_name : 'Học viên ẩn danh'
        };
      });
    }

    container.innerHTML = StudentCoursesView.renderCourseDetail({
      course,
      openClasses,
      isEnrolledInCourse: !!enrolledClass,
      enrolledClassId: enrolledClass ? enrolledClass.id : null
    });

    this._bindDetailEvents(container, courseId);
  }

  static _bindDetailEvents(container, courseId) {
    const enrollButtons = container.querySelectorAll('.btn-enroll-class');
    enrollButtons.forEach(btn => {
      btn.addEventListener('click', async (e) => {
        const classId = e.currentTarget.getAttribute('data-class-id');
        const classCode = e.currentTarget.getAttribute('data-class-code');
        const className = e.currentTarget.getAttribute('data-class-name');
        const currentUser = auth.getCurrentUser();

        const confirmed = await confirmDialog({
          title: `Xác nhận đăng ký lớp học`,
          message: `Bạn có chắc chắn muốn đăng ký tham gia lớp "${className} (${classCode})"?`,
          confirmText: 'Đăng ký ngay',
          cancelText: 'Suy nghĩ thêm',
          type: 'info'
        });

        if (!confirmed) return;

        const res = await EnrollmentModel.enrollClass(currentUser.id, classId);
        if (res.success) {
          toast.success(`Đăng ký thành công lớp ${classCode}! Chúc bạn có kỳ học bổ ích.`);
          this._renderDetail(container, courseId);
        } else {
          toast.error(res.message || 'Không thể đăng ký lớp học này.');
        }
      });
    });
  }
}
