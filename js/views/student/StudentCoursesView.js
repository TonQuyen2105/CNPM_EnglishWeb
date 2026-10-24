/**
 * StudentCoursesView - Giao diện Khám phá & Chi tiết Khóa học
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency } from '../../core/utils.js';

export class StudentCoursesView {
  /**
   * Render danh sách khóa học kèm bộ lọc và tìm kiếm
   */
  static renderCourseList({
    courses = [],
    subjects = [],
    selectedSubject = '',
    selectedLevel = '',
    selectedPrice = '',
    sortBy = 'newest',
    keyword = ''
  }) {
    return `
      <div class="student-courses-page">
        <!-- Header & Search -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Khám phá Khóa học</h1>
            <p class="page-header-subtitle">
              Tìm kiếm khóa học phù hợp với trình độ và lộ trình nâng cao năng lực tiếng Anh của bạn.
            </p>
          </div>
        </div>

        <!-- Thanh Tìm kiếm và Bộ lọc -->
        <div class="card" style="padding: 1.25rem; border-radius: var(--radius-xl); margin-bottom: 2rem;">
          <div class="grid grid-cols-12 gap-4 items-center">
            
            <!-- Ô tìm kiếm keyword -->
            <div class="col-span-4">
              <div class="search-input-wrapper" style="position: relative;">
                <span style="position: absolute; left: 12px; top: 50%; transform: translateY(-50%); color: var(--text-muted);">
                  ${icon('search', { size: 16 })}
                </span>
                <input 
                  type="text" 
                  id="course-search-input" 
                  class="form-control" 
                  style="padding-left: 36px; border-radius: var(--radius-full);" 
                  placeholder="Tìm khóa học, từ khóa..." 
                  value="${keyword || ''}"
                >
              </div>
            </div>

            <!-- Lọc môn học -->
            <div class="col-span-3">
              <select id="course-filter-subject" class="form-control" style="border-radius: var(--radius-full);">
                <option value="">Tất cả môn học</option>
                ${subjects.map(s => `
                  <option value="${s.id}" ${String(s.id) === String(selectedSubject) ? 'selected' : ''}>
                    ${s.name}
                  </option>
                `).join('')}
              </select>
            </div>

            <!-- Lọc trình độ (Level) -->
            <div class="col-span-2">
              <select id="course-filter-level" class="form-control" style="border-radius: var(--radius-full);">
                <option value="">Tất cả trình độ</option>
                <option value="beginner" ${selectedLevel === 'beginner' ? 'selected' : ''}>Mới bắt đầu</option>
                <option value="intermediate" ${selectedLevel === 'intermediate' ? 'selected' : ''}>Trung cấp</option>
                <option value="advanced" ${selectedLevel === 'advanced' ? 'selected' : ''}>Nâng cao</option>
              </select>
            </div>

            <!-- Sắp xếp -->
            <div class="col-span-3">
              <select id="course-sort-by" class="form-control" style="border-radius: var(--radius-full);">
                <option value="newest" ${sortBy === 'newest' ? 'selected' : ''}>Mới nhất</option>
                <option value="rating" ${sortBy === 'rating' ? 'selected' : ''}>Đánh giá cao nhất</option>
                <option value="price_asc" ${sortBy === 'price_asc' ? 'selected' : ''}>Học phí: Thấp đến cao</option>
                <option value="price_desc" ${sortBy === 'price_desc' ? 'selected' : ''}>Học phí: Cao đến thấp</option>
              </select>
            </div>

          </div>
        </div>

        <!-- Lưới Card Khóa Học -->
        ${courses.length > 0 ? `
          <div class="grid grid-cols-3 gap-6">
            ${courses.map(course => this._renderCourseCard(course)).join('')}
          </div>
        ` : `
          <div class="card" style="padding: 3rem; text-align: center; border-radius: var(--radius-xl);">
            <div style="color: var(--text-muted); margin-bottom: 0.75rem;">${icon('book-open', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Không tìm thấy khóa học nào</h3>
            <p style="color: var(--text-secondary); max-width: 400px; margin: 0 auto 1.25rem;">
              Thử thay đổi từ khóa tìm kiếm hoặc điều chỉnh lại các tiêu chí bộ lọc.
            </p>
            <button type="button" class="btn btn-outline-primary btn-sm" id="btn-reset-filters">
              Xóa bộ lọc
            </button>
          </div>
        `}
      </div>
    `;
  }

  static _renderCourseCard(c) {
    const levelBadgeMap = {
      beginner: '<span class="badge badge-success">Beginner</span>',
      intermediate: '<span class="badge badge-info">Intermediate</span>',
      advanced: '<span class="badge badge-danger">Advanced</span>'
    };

    return `
      <div class="card course-card" style="border-radius: var(--radius-xl); overflow: hidden; display: flex; flex-direction: column; transition: transform 0.2s, box-shadow 0.2s; border: 1px solid var(--border-color);">
        <!-- Thumbnail -->
        <div style="height: 180px; position: relative; overflow: hidden; background: #E2E8F0;">
          <img 
            src="${c.thumbnail_url || 'https://images.unsplash.com/photo-1546410531-bb4caa6b424d?auto=format&fit=crop&w=600&q=80'}" 
            alt="${c.title}" 
            style="width: 100%; height: 100%; object-fit: cover;"
            onerror="this.src='https://images.unsplash.com/photo-1546410531-bb4caa6b424d?auto=format&fit=crop&w=600&q=80'"
          >
          <div style="position: absolute; top: 12px; left: 12px;">
            ${levelBadgeMap[c.level] || '<span class="badge badge-primary">Mọi trình độ</span>'}
          </div>
          <div style="position: absolute; top: 12px; right: 12px; background: rgba(0,0,0,0.6); color: white; padding: 2px 8px; border-radius: var(--radius-full); font-size: 0.75rem; font-weight: 700; display: flex; align-items: center; gap: 4px;">
            <span style="color: #FBBF24;">★</span> ${c.rating_avg || '5.0'}
          </div>
        </div>

        <!-- Content -->
        <div style="padding: 1.25rem; flex: 1; display: flex; flex-direction: column; justify-content: space-between;">
          <div>
            <div style="font-size: 0.75rem; font-weight: 700; color: var(--primary-600); text-transform: uppercase; margin-bottom: 4px;">
              ${c.subject_name || 'Tiếng Anh giao tiếp'}
            </div>
            <h3 style="font-size: 1.0625rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem; line-height: 1.4;">
              ${c.title}
            </h3>
            <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 1rem; line-height: 1.5; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
              ${c.description || 'Khóa học thiết kế chuẩn khung tham chiếu quốc tế.'}
            </p>
          </div>

          <div>
            <div class="flex items-center justify-between" style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 0.75rem; padding-top: 0.5rem; border-top: 1px dashed var(--border-color);">
              <span class="flex items-center gap-1">${icon('book-open', { size: 14 })} ${c.lesson_count || 0} bài học</span>
              <span class="flex items-center gap-1">${icon('users', { size: 14 })} ${c.open_classes_count || 0} lớp đang mở</span>
            </div>

            <div class="flex items-center justify-between">
              <div>
                <span style="font-size: 1.125rem; font-weight: 800; color: var(--primary-700);">
                  ${formatCurrency(c.price || 0)}
                </span>
              </div>
              <a href="#/courses/${c.id}" class="btn btn-outline-primary btn-sm" style="border-radius: var(--radius-full); padding: 6px 14px; font-weight: 700;">
                Xem chi tiết
              </a>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  /**
   * Render Chi tiết Khóa học
   */
  static renderCourseDetail({
    course = {},
    openClasses = [],
    isEnrolledInCourse = false,
    enrolledClassId = null
  }) {
    const levelBadgeMap = {
      beginner: '<span class="badge badge-success">Beginner - Mới bắt đầu</span>',
      intermediate: '<span class="badge badge-info">Intermediate - Trung cấp</span>',
      advanced: '<span class="badge badge-danger">Advanced - Nâng cao</span>'
    };

    const contentTypeIconMap = {
      vocabulary: 'layers',
      grammar: 'file-text',
      listening: 'volume-2',
      reading: 'book-open',
      speaking: 'mic',
      writing: 'edit',
      quiz: 'award'
    };

    return `
      <div class="student-course-detail">
        <!-- Breadcrumb / Back button -->
        <div style="margin-bottom: 1.25rem;">
          <a href="#/courses" class="btn btn-secondary btn-sm" style="background: white; border-radius: var(--radius-full);">
            <span class="nav-icon">${icon('arrow-left', { size: 16 })}</span>
            <span>Quay lại Khám phá</span>
          </a>
        </div>

        <!-- Hero Header Khóa Học -->
        <div class="card" style="border-radius: var(--radius-xl); overflow: hidden; margin-bottom: 2rem; border: 1px solid var(--border-color);">
          <div class="grid grid-cols-12">
            <div class="col-span-7" style="padding: 2rem;">
              <div class="flex items-center gap-3" style="margin-bottom: 0.75rem;">
                <span class="badge badge-primary">${course.subject_name || 'Khóa học'}</span>
                ${levelBadgeMap[course.level] || ''}
              </div>
              <h1 style="font-size: 1.75rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.75rem; line-height: 1.3;">
                ${course.title}
              </h1>
              <p style="font-size: 0.9375rem; color: var(--text-secondary); line-height: 1.6; margin-bottom: 1.5rem;">
                ${course.description || 'Chương trình giảng dạy chuyên sâu, kết hợp phương pháp hiện đại và phản xạ tự nhiên giúp học viên tự tin làm chủ tiếng Anh.'}
              </p>

              <div class="flex items-center gap-6" style="padding-top: 1rem; border-top: 1px solid var(--border-color);">
                <div>
                  <div style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase; font-weight: 600;">Học phí trọn khóa</div>
                  <div style="font-size: 1.5rem; font-weight: 800; color: var(--primary-700);">${formatCurrency(course.price || 0)}</div>
                </div>
                <div>
                  <div style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase; font-weight: 600;">Đánh giá</div>
                  <div class="flex items-center gap-1" style="font-size: 1.125rem; font-weight: 800; color: #D97706;">
                    <span>★</span> <span>${course.rating_avg || '5.0'}</span>
                    <span style="font-size: 0.75rem; color: var(--text-muted); font-weight: normal;">(${course.review_count || 0} nhận xét)</span>
                  </div>
                </div>
                <div>
                  <div style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase; font-weight: 600;">Giáo trình</div>
                  <div style="font-size: 1.125rem; font-weight: 700; color: var(--text-primary);">${course.lessons ? course.lessons.length : 0} bài học</div>
                </div>
              </div>
            </div>

            <div class="col-span-5" style="background: #F1F5F9; position: relative;">
              <img 
                src="${course.thumbnail_url || 'https://images.unsplash.com/photo-1546410531-bb4caa6b424d?auto=format&fit=crop&w=600&q=80'}" 
                alt="${course.title}" 
                style="width: 100%; height: 100%; object-fit: cover;"
                onerror="this.src='https://images.unsplash.com/photo-1546410531-bb4caa6b424d?auto=format&fit=crop&w=600&q=80'"
              >
            </div>
          </div>
        </div>

        <!-- 2 Cột: Danh sách Lớp mở tuyển sinh & Mục lục Bài học -->
        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (7 cột): Danh sách Lớp học đang mở đăng ký -->
          <div class="col-span-7 flex flex-col gap-6">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--primary-600);">${icon('layers', { size: 22 })}</span>
                  <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Lớp học đang mở đăng ký</h3>
                </div>
                <span class="badge badge-info">${openClasses.length} lớp học</span>
              </div>

              ${openClasses.length > 0 ? `
                <div class="flex flex-col gap-4">
                  ${openClasses.map(cls => {
                    const spotsLeft = Math.max(0, (cls.max_students || 20) - (cls.enrolled_count || 0));
                    const isFull = spotsLeft <= 0;
                    const isMyClass = enrolledClassId === cls.id;

                    return `
                      <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 1.25rem; background: ${isMyClass ? '#EFF6FF' : 'var(--neutral-50)'};">
                        <div class="flex items-start justify-between gap-4 flex-wrap" style="margin-bottom: 0.75rem;">
                          <div>
                            <div class="flex items-center gap-2" style="margin-bottom: 4px;">
                              <span class="badge badge-primary" style="font-weight: 800;">${cls.class_code}</span>
                              <h4 style="font-size: 1rem; font-weight: 700; margin: 0; color: var(--text-primary);">${cls.class_name}</h4>
                            </div>
                            <div style="font-size: 0.8125rem; color: var(--text-secondary);">
                              <span>Giảng viên: <strong>${cls.teacher_name || 'Đang cập nhật'}</strong></span>
                            </div>
                          </div>

                          <div>
                            ${isMyClass ? `
                              <span class="badge badge-success" style="padding: 6px 12px; font-size: 0.8125rem;">
                                ✓ Đang theo học
                              </span>
                            ` : isFull ? `
                              <span class="badge badge-danger" style="padding: 6px 12px; font-size: 0.8125rem;">
                                Đã hết chỗ
                              </span>
                            ` : `
                              <button type="button" class="btn btn-primary btn-sm btn-enroll-class" data-class-id="${cls.id}" data-class-code="${cls.class_code}" data-class-name="${cls.class_name}">
                                Đăng ký lớp này
                              </button>
                            `}
                          </div>
                        </div>

                        <!-- Lịch học & Sĩ số -->
                        <div class="grid grid-cols-2 gap-3" style="font-size: 0.8125rem; color: var(--text-secondary); background: white; padding: 0.75rem; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
                          <div>
                            <span class="flex items-center gap-1" style="font-weight: 600; color: var(--text-primary);">
                              ${icon('calendar', { size: 14 })} Lịch học:
                            </span>
                            <span style="font-size: 0.75rem;">${cls.schedule_summary || 'Theo thông báo trung tâm'}</span>
                          </div>
                          <div>
                            <span class="flex items-center gap-1" style="font-weight: 600; color: var(--text-primary);">
                              ${icon('users', { size: 14 })} Sĩ số:
                            </span>
                            <span style="font-size: 0.75rem; color: ${isFull ? 'var(--danger-main)' : 'var(--success-main)'}; font-weight: 700;">
                              Còn ${spotsLeft} / ${cls.max_students || 20} chỗ trống
                            </span>
                          </div>
                        </div>
                      </div>
                    `;
                  }).join('')}
                </div>
              ` : `
                <div style="padding: 2rem; text-align: center; color: var(--text-muted); background: var(--neutral-50); border-radius: var(--radius-lg);">
                  Hiện tại chưa có lớp mới mở cho khóa học này. Vui lòng quay lại sau!
                </div>
              `}
            </div>

            <!-- Đánh giá từ các học viên khác -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: #D97706;">${icon('star', { size: 22 })}</span>
                  <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Đánh giá của học viên</h3>
                </div>
                <span style="font-size: 0.8125rem; color: var(--text-muted);">${course.reviews ? course.reviews.length : 0} nhận xét</span>
              </div>

              ${course.reviews && course.reviews.length > 0 ? `
                <div class="flex flex-col gap-4">
                  ${course.reviews.map(rev => `
                    <div style="padding: 1rem; border-radius: var(--radius-md); background: var(--neutral-50); border: 1px solid var(--border-color);">
                      <div class="flex items-center justify-between" style="margin-bottom: 0.375rem;">
                        <span style="font-weight: 700; font-size: 0.875rem; color: var(--text-primary);">${rev.user_name || 'Học viên'}</span>
                        <div style="color: #F59E0B; font-size: 0.8125rem;">
                          ${'★'.repeat(rev.rating)}${'☆'.repeat(5 - rev.rating)}
                        </div>
                      </div>
                      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0; line-height: 1.5;">
                        ${rev.comment || 'Khóa học rất bổ ích và thiết thực.'}
                      </p>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <div style="padding: 1.5rem; text-align: center; color: var(--text-muted); font-size: 0.8125rem;">
                  Chưa có nhận xét nào cho khóa học này.
                </div>
              `}
            </div>
          </div>

          <!-- Cột Phải (5 cột): Mục lục Giáo trình & Bài học -->
          <div class="col-span-5">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; position: sticky; top: 90px;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--primary-600);">${icon('book-open', { size: 22 })}</span>
                  <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Giáo trình bài học</h3>
                </div>
                <span class="badge badge-primary">${course.lessons ? course.lessons.length : 0} bài</span>
              </div>

              ${course.lessons && course.lessons.length > 0 ? `
                <div class="flex flex-col gap-2" style="max-height: 520px; overflow-y: auto; padding-right: 4px;">
                  ${course.lessons.map(l => `
                    <div class="flex items-center justify-between" style="padding: 0.75rem 1rem; border-radius: var(--radius-md); background: var(--neutral-50); border: 1px solid var(--border-color);">
                      <div class="flex items-center gap-3">
                        <span style="width: 28px; height: 28px; border-radius: 50%; background: #EFF6FF; color: var(--primary-600); display: flex; align-items: center; justify-content: center; font-size: 0.75rem; font-weight: 700;">
                          ${l.order_index}
                        </span>
                        <div>
                          <div style="font-size: 0.8125rem; font-weight: 700; color: var(--text-primary); line-height: 1.3;">
                            ${l.title}
                          </div>
                          <span style="font-size: 0.7rem; color: var(--text-muted); text-transform: uppercase;">
                            ${l.content_type}
                          </span>
                        </div>
                      </div>

                      <span style="color: var(--text-muted);">
                        ${icon(contentTypeIconMap[l.content_type] || 'file-text', { size: 16 })}
                      </span>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <p style="color: var(--text-muted); text-align: center; margin: 1rem 0;">Chưa cập nhật bài học cho khóa này.</p>
              `}
            </div>
          </div>

        </div>
      </div>
    `;
  }
}
