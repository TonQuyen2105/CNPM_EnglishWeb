/**
 * StudentMyClassesView - Giao diện Quản lý Lớp học của tôi
 */

import { icon } from '../../ui/icons.js';

export class StudentMyClassesView {
  static renderList({ enrollments = [], currentTab = 'active' }) {
    const activeCount = enrollments.filter(e => e.status === 'active').length;
    const completedCount = enrollments.filter(e => e.status === 'completed').length;
    const cancelledCount = enrollments.filter(e => e.status === 'cancelled').length;

    const filtered = enrollments.filter(e => e.status === currentTab);

    return `
      <div class="student-my-classes-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Lớp học của tôi</h1>
            <p class="page-header-subtitle">
              Theo dõi lộ trình, thời khóa biểu và tiến độ hoàn thành các khóa học bạn đã đăng ký.
            </p>
          </div>
        </div>

        <!-- Tabs Chuyển đổi trạng thái -->
        <div class="tabs-nav" style="display: flex; gap: 8px; border-bottom: 1px solid var(--border-color); margin-bottom: 2rem;">
          <button type="button" class="tab-item-btn ${currentTab === 'active' ? 'active' : ''}" data-tab="active" style="padding: 10px 20px; font-weight: 700; font-size: 0.875rem; border: none; background: none; border-bottom: 2px solid ${currentTab === 'active' ? 'var(--primary-600)' : 'transparent'}; color: ${currentTab === 'active' ? 'var(--primary-600)' : 'var(--text-secondary)'}; cursor: pointer;">
            Đang học (${activeCount})
          </button>
          <button type="button" class="tab-item-btn ${currentTab === 'completed' ? 'active' : ''}" data-tab="completed" style="padding: 10px 20px; font-weight: 700; font-size: 0.875rem; border: none; background: none; border-bottom: 2px solid ${currentTab === 'completed' ? 'var(--primary-600)' : 'transparent'}; color: ${currentTab === 'completed' ? 'var(--primary-600)' : 'var(--text-secondary)'}; cursor: pointer;">
            Đã hoàn thành (${completedCount})
          </button>
          <button type="button" class="tab-item-btn ${currentTab === 'cancelled' ? 'active' : ''}" data-tab="cancelled" style="padding: 10px 20px; font-weight: 700; font-size: 0.875rem; border: none; background: none; border-bottom: 2px solid ${currentTab === 'cancelled' ? 'var(--primary-600)' : 'transparent'}; color: ${currentTab === 'cancelled' ? 'var(--primary-600)' : 'var(--text-secondary)'}; cursor: pointer;">
            Đã hủy (${cancelledCount})
          </button>
        </div>

        <!-- Danh sách Lớp theo tab -->
        ${filtered.length > 0 ? `
          <div class="grid grid-cols-2 gap-6">
            ${filtered.map(item => this._renderClassCard(item)).join('')}
          </div>
        ` : `
          <div class="card" style="padding: 3.5rem; text-align: center; border-radius: var(--radius-xl);">
            <div style="color: var(--text-muted); margin-bottom: 0.75rem;">${icon('layers', { size: 48 })}</div>
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 0.5rem;">Không có lớp nào trong mục này</h3>
            <p style="color: var(--text-secondary); max-width: 450px; margin: 0 auto 1.5rem;">
              ${currentTab === 'active' 
                ? 'Bạn hiện chưa đăng ký lớp học nào đang diễn ra. Hãy khám phá ngay các khóa học mới!' 
                : 'Mục này hiện đang trống.'}
            </p>
            ${currentTab === 'active' ? `
              <a href="#/courses" class="btn btn-primary" style="display: inline-flex;">
                ${icon('compass', { size: 16 })}
                <span style="margin-left: 6px;">Tìm kiếm khóa học ngay</span>
              </a>
            ` : ''}
          </div>
        `}
      </div>
    `;
  }

  static _renderClassCard(e) {
    return `
      <div class="card" style="border-radius: var(--radius-xl); overflow: hidden; border: 1px solid var(--border-color); display: flex; flex-direction: column;">
        <div style="padding: 1.5rem; flex: 1;">
          <div class="flex items-start justify-between gap-4" style="margin-bottom: 1rem;">
            <div>
              <span class="badge badge-primary" style="font-weight: 800; margin-bottom: 4px;">${e.class_code}</span>
              <h3 style="font-size: 1.125rem; font-weight: 800; color: var(--text-primary); margin: 0 0 4px 0;">
                ${e.class_name}
              </h3>
              <div style="font-size: 0.8125rem; color: var(--primary-700); font-weight: 600;">
                Khóa: ${e.course_title}
              </div>
            </div>

            <span class="badge ${e.status === 'active' ? 'badge-success' : e.status === 'completed' ? 'badge-primary' : 'badge-danger'}">
              ${e.status === 'active' ? 'Đang theo học' : e.status === 'completed' ? 'Hoàn thành' : 'Đã hủy'}
            </span>
          </div>

          <!-- Thông tin Giảng viên & Lịch học -->
          <div style="background: var(--neutral-50); border-radius: var(--radius-md); padding: 0.875rem 1rem; border: 1px solid var(--border-color); margin-bottom: 1.25rem;">
            <div class="flex items-center gap-2" style="font-size: 0.8125rem; margin-bottom: 6px; color: var(--text-primary);">
              <span style="color: var(--primary-600);">${icon('user', { size: 14 })}</span>
              <span>Giảng viên: <strong>${e.main_teacher_name || 'Đang phân công'}</strong></span>
            </div>
            <div class="flex items-start gap-2" style="font-size: 0.8125rem; color: var(--text-secondary);">
              <span style="color: var(--accent-600); margin-top: 2px;">${icon('calendar', { size: 14 })}</span>
              <div>
                ${e.schedules && e.schedules.length > 0 
                  ? e.schedules.map(s => `<div>${s.time_range} • ${s.room_name}</div>`).join('') 
                  : 'Lịch học linh hoạt'}
              </div>
            </div>
          </div>

          <!-- Tiến độ bài học -->
          <div>
            <div class="flex items-center justify-between" style="font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 4px;">
              <span>Tiến độ học tập</span>
              <strong>${e.progress_percent || 0}% (${e.completed_lessons || 0}/${e.total_lessons || 0} bài)</strong>
            </div>
            <div class="progress" style="height: 8px; border-radius: var(--radius-full); background: var(--neutral-200);">
              <div class="progress-bar" style="width: ${e.progress_percent || 0}%; background: var(--primary-600); border-radius: var(--radius-full);"></div>
            </div>
          </div>
        </div>

        <!-- Footer Card -->
        <div style="padding: 1rem 1.5rem; background: var(--neutral-50); border-top: 1px solid var(--border-color); display: flex; align-items: center; justify-content: space-between;">
          <a href="#/my-classes/${e.class_id}" class="btn btn-outline-primary btn-sm" style="border-radius: var(--radius-full);">
            <span>Xem chi tiết lớp</span>
            <span class="nav-icon">${icon('arrow-right', { size: 14 })}</span>
          </a>

          ${e.status === 'active' ? `
            <a href="#/courses/${e.course_id}" class="text-xs font-semibold" style="color: var(--text-secondary); text-decoration: none;">
              Xem trang khóa học
            </a>
          ` : ''}
        </div>
      </div>
    `;
  }

  static renderDetail({ cls = {}, course = {}, enrollment = {}, lessons = [], userProgress = [] }) {
    const completedLessonIds = userProgress
      .filter(p => p.is_completed === 1 || p.is_completed === true)
      .map(p => p.lesson_id);

    return `
      <div class="student-class-detail-page">
        <!-- Back Link -->
        <div style="margin-bottom: 1.25rem;">
          <a href="#/my-classes" class="btn btn-secondary btn-sm" style="background: white; border-radius: var(--radius-full);">
            <span class="nav-icon">${icon('arrow-left', { size: 16 })}</span>
            <span>Quay lại Lớp của tôi</span>
          </a>
        </div>

        <!-- Header Lớp Học -->
        <div class="card" style="border-radius: var(--radius-xl); padding: 2rem; border: 1px solid var(--border-color); margin-bottom: 2rem;">
          <div class="flex items-start justify-between gap-4 flex-wrap">
            <div>
              <div class="flex items-center gap-3" style="margin-bottom: 0.5rem;">
                <span class="badge badge-primary" style="font-weight: 800; font-size: 0.8125rem;">${cls.class_code}</span>
                <span class="badge badge-success">${cls.status === 'ongoing' ? 'Đang diễn ra' : cls.status}</span>
              </div>
              <h1 style="font-size: 1.75rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.25rem;">
                ${cls.class_name}
              </h1>
              <div style="font-size: 0.9375rem; color: var(--primary-700); font-weight: 600;">
                Khóa: ${course.title}
              </div>
            </div>

            <div class="flex items-center gap-3">
              ${enrollment && enrollment.status === 'active' ? `
                <button type="button" class="btn btn-outline-danger btn-sm" id="btn-cancel-enrollment" data-enrollment-id="${enrollment.id}">
                  Hủy đăng ký lớp này
                </button>
              ` : ''}
            </div>
          </div>
        </div>

        <!-- Danh sách bài học của lớp -->
        <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color);">
          <div class="flex items-center justify-between" style="margin-bottom: 1.5rem;">
            <div class="flex items-center gap-2">
              <span style="color: var(--primary-600);">${icon('book-open', { size: 22 })}</span>
              <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Lộ trình bài học & Trạng thái</h3>
            </div>
            <span class="badge badge-info">${completedLessonIds.length} / ${lessons.length} bài hoàn thành</span>
          </div>

          <div class="flex flex-col gap-3">
            ${lessons.map(lesson => {
              const isCompleted = completedLessonIds.includes(lesson.id);
              const progressRecord = userProgress.find(p => p.lesson_id === lesson.id);

              return `
                <div style="display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.25rem; border: 1px solid var(--border-color); border-radius: var(--radius-lg); background: ${isCompleted ? '#F0FDF4' : 'var(--neutral-50)'};">
                  <div class="flex items-center gap-4">
                    <div style="width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 0.8125rem; background: ${isCompleted ? '#16A34A' : '#E2E8F0'}; color: ${isCompleted ? 'white' : 'var(--text-secondary)'};">
                      ${isCompleted ? '✓' : lesson.order_index}
                    </div>

                    <div>
                      <div class="flex items-center gap-2">
                        <span class="badge badge-primary" style="font-size: 0.6875rem; text-transform: uppercase;">
                          ${lesson.content_type}
                        </span>
                        <h4 style="font-size: 0.9375rem; font-weight: 700; margin: 0; color: var(--text-primary);">
                          ${lesson.title}
                        </h4>
                      </div>
                      ${isCompleted && progressRecord ? `
                        <div style="font-size: 0.75rem; color: #166534; margin-top: 2px;">
                          Đã hoàn thành • Điểm đánh giá: <strong>${progressRecord.score || 100}/100</strong>
                        </div>
                      ` : `
                        <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 2px;">
                          Chưa hoàn thành
                        </div>
                      `}
                    </div>
                  </div>

                  <a href="#/learn/${course.id}/${lesson.id}" class="btn ${isCompleted ? 'btn-outline-success' : 'btn-primary'} btn-sm" style="border-radius: var(--radius-full); padding: 6px 16px;">
                    <span>${isCompleted ? 'Ôn lại bài' : 'Bắt đầu học'}</span>
                    <span class="nav-icon">${icon('play', { size: 14 })}</span>
                  </a>
                </div>
              `;
            }).join('')}
          </div>
        </div>
      </div>
    `;
  }
}
