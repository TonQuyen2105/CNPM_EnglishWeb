/**
 * StudentDashboardView - Giao diện Trang chủ / Bảng học tập cho Học viên
 */

import { icon } from '../../ui/icons.js';
import { formatDate } from '../../core/utils.js';

export class StudentDashboardView {
  static render({
    currentUser = {},
    streakDays = 5,
    stats = {},
    upcomingClassSession = null,
    inProgressLesson = null,
    dueFlashcardsCount = 0,
    upcomingExams = []
  }) {
    const greeting = this._getGreeting();

    return `
      <div class="student-dashboard">
        <!-- Hero Banner Gradient Xanh Nhạt Thân Thiện -->
        <div class="student-hero-banner">
          <div class="flex items-center justify-between flex-wrap gap-4" style="position: relative; z-index: 2;">
            <div>
              <div style="display: inline-flex; align-items: center; gap: 8px; padding: 4px 12px; background: rgba(255,255,255,0.7); border-radius: var(--radius-full); font-size: 0.8125rem; font-weight: 700; color: var(--primary-700); margin-bottom: 0.75rem;">
                <span>🔥 Chuỗi ${streakDays} ngày chăm chỉ</span>
                <span>•</span>
                <span>Hôm nay bạn đã sẵn sàng bứt phá chưa?</span>
              </div>
              <h1 style="font-size: 1.75rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
                ${greeting}, ${currentUser.full_name || 'Học viên'}! 👋
              </h1>
              <p style="color: var(--text-secondary); max-width: 580px; font-size: 0.9375rem; line-height: 1.5;">
                "Live as if you were to die tomorrow. Learn as if you were to live forever." Hãy dành 15 phút mỗi ngày để hoàn thiện mục tiêu tiếng Anh của bạn.
              </p>
            </div>

            <div class="flex items-center gap-3">
              ${inProgressLesson ? `
                <a href="#/learn/${inProgressLesson.course.id}/${inProgressLesson.lesson.id}" class="btn btn-primary" style="padding: 12px 20px; font-size: 0.9375rem; box-shadow: 0 4px 14px rgba(37,99,235,0.3);">
                  <span class="nav-icon">${icon('play', { size: 18 })}</span>
                  <span>Tiếp tục học ngay</span>
                </a>
              ` : `
                <a href="#/courses" class="btn btn-primary" style="padding: 12px 20px;">
                  <span class="nav-icon">${icon('compass', { size: 18 })}</span>
                  <span>Khám phá khóa học</span>
                </a>
              `}
              <a href="#/flashcards" class="btn btn-secondary" style="background: white;">
                <span class="nav-icon">${icon('repeat', { size: 18 })}</span>
                <span>Ôn thẻ SRS (${dueFlashcardsCount})</span>
              </a>
            </div>
          </div>
        </div>

        <!-- 4 Thẻ Thống Kê Chính -->
        <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Bài học đã hoàn thành</span>
              <span class="stat-value">${stats.completedLessons || 0}</span>
              <span class="stat-sub text-success">
                <span>/ ${stats.totalLessons || 0} bài trong khóa</span>
              </span>
            </div>
            <div class="stat-icon primary">${icon('check-circle', { size: 24 })}</div>
          </div>

          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Điểm trung bình kiểm tra</span>
              <span class="stat-value">${stats.averageScore ? stats.averageScore + '/100' : 'Chưa có'}</span>
              <span class="stat-sub ${stats.averageScore >= 80 ? 'text-success' : 'text-primary'}">
                ${stats.averageScore >= 80 ? '✓ Xếp loại Giỏi' : 'Đang duy trì tốt'}
              </span>
            </div>
            <div class="stat-icon success">${icon('award', { size: 24 })}</div>
          </div>

          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Từ vựng đã tích lũy</span>
              <span class="stat-value">${stats.wordsLearned || 0}</span>
              <span class="stat-sub text-warning">
                ${dueFlashcardsCount > 0 ? `🔥 ${dueFlashcardsCount} từ cần ôn hôm nay` : '✓ Đã ôn tập đủ hôm nay'}
              </span>
            </div>
            <div class="stat-icon warning">${icon('book-open', { size: 24 })}</div>
          </div>

          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Lớp đang theo học</span>
              <span class="stat-value">${stats.activeClassesCount || 0}</span>
              <span class="stat-sub text-info">Đang hoạt động</span>
            </div>
            <div class="stat-icon info">${icon('layers', { size: 24 })}</div>
          </div>
        </div>

        <!-- Nội Dung Chia 2 Cột: Trái (Bài dở dang + Buổi học sắp tới), Phải (Flashcards + Đề thi) -->
        <div class="grid grid-cols-12 gap-6">
          <!-- Cột Trái: 8 Cột -->
          <div class="col-span-8 flex flex-col gap-6">
            
            <!-- Widget: Tiếp tục học dở dang -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--primary-600);">${icon('play-circle', { size: 22 })}</span>
                  <h2 style="font-size: 1.125rem; font-weight: 700;">Tiếp tục học bài gần nhất</h2>
                </div>
                ${inProgressLesson ? `
                  <span class="badge badge-primary">Khóa: ${inProgressLesson.course.title}</span>
                ` : ''}
              </div>

              ${inProgressLesson ? `
                <div style="background: var(--neutral-50); border-radius: var(--radius-lg); padding: 1.25rem; border: 1px solid var(--border-color);">
                  <div class="flex items-start justify-between gap-4 flex-wrap" style="margin-bottom: 1rem;">
                    <div>
                      <div class="flex items-center gap-2" style="margin-bottom: 0.375rem;">
                        <span class="badge badge-info" style="text-transform: uppercase; font-size: 0.6875rem;">
                          ${inProgressLesson.lesson.content_type || 'Lesson'}
                        </span>
                        <span style="font-size: 0.8125rem; color: var(--text-muted);">
                          Bài ${inProgressLesson.lesson.order_index}
                        </span>
                      </div>
                      <h3 style="font-size: 1.0625rem; font-weight: 700; color: var(--text-primary); margin-bottom: 0.25rem;">
                        ${inProgressLesson.lesson.title}
                      </h3>
                      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">
                        Lớp: <strong>${inProgressLesson.cls.class_name} (${inProgressLesson.cls.class_code})</strong>
                      </p>
                    </div>

                    <a href="#/learn/${inProgressLesson.course.id}/${inProgressLesson.lesson.id}" class="btn btn-primary btn-sm" style="border-radius: var(--radius-full); padding: 8px 16px;">
                      <span>Vào học ngay</span>
                      <span class="nav-icon">${icon('arrow-right', { size: 16 })}</span>
                    </a>
                  </div>

                  <!-- Thanh tiến độ hoàn thành khóa học -->
                  <div>
                    <div class="flex items-center justify-between" style="font-size: 0.75rem; margin-bottom: 4px; color: var(--text-secondary);">
                      <span>Tiến độ toàn khóa</span>
                      <strong>${inProgressLesson.progress_percent}% (${inProgressLesson.completed_count}/${inProgressLesson.total_count} bài)</strong>
                    </div>
                    <div class="progress" style="height: 8px; border-radius: var(--radius-full); background: var(--neutral-200);">
                      <div class="progress-bar" style="width: ${inProgressLesson.progress_percent}%; background: var(--primary-600); border-radius: var(--radius-full);"></div>
                    </div>
                  </div>
                </div>
              ` : `
                <div class="empty-state" style="padding: 2rem 1rem;">
                  <div style="color: var(--text-muted); margin-bottom: 0.5rem;">${icon('book-open', { size: 36 })}</div>
                  <p style="color: var(--text-secondary); margin-bottom: 1rem;">Bạn chưa đăng ký lớp học nào hoặc đã hoàn thành tất cả bài giảng.</p>
                  <a href="#/courses" class="btn btn-outline-primary btn-sm">Khám phá các khóa học mới</a>
                </div>
              `}
            </div>

            <!-- Widget: Buổi học sắp tới -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--accent-600);">${icon('calendar', { size: 22 })}</span>
                  <h2 style="font-size: 1.125rem; font-weight: 700;">Buổi học sắp tới</h2>
                </div>
                <a href="#/schedule" class="text-sm font-semibold" style="color: var(--primary-600); text-decoration: none;">
                  Xem toàn bộ lịch học →
                </a>
              </div>

              ${upcomingClassSession ? `
                <div style="background: linear-gradient(135deg, #F0FDF4 0%, #DCFCE7 100%); border-radius: var(--radius-lg); padding: 1.25rem; border: 1px solid #BBF7D0;">
                  <div class="flex items-center justify-between flex-wrap gap-3">
                    <div class="flex items-center gap-4">
                      <div style="width: 52px; height: 52px; border-radius: var(--radius-md); background: #16A34A; color: white; display: flex; flex-direction: column; align-items: center; justify-content: center; font-weight: 800;">
                        <span style="font-size: 0.7rem; text-transform: uppercase;">Tháng ${new Date(upcomingClassSession.schedule_date || Date.now()).getMonth() + 1}</span>
                        <span style="font-size: 1.125rem; line-height: 1;">${new Date(upcomingClassSession.schedule_date || Date.now()).getDate()}</span>
                      </div>
                      <div>
                        <div class="flex items-center gap-2" style="margin-bottom: 2px;">
                          <span class="badge badge-success" style="font-size: 0.6875rem;">Buổi số ${upcomingClassSession.lesson_order || 1}</span>
                          <span style="font-size: 0.8125rem; font-weight: 700; color: #166534;">${upcomingClassSession.class_name} (${upcomingClassSession.class_code})</span>
                        </div>
                        <h4 style="font-size: 1rem; font-weight: 700; color: #14532D; margin: 0 0 4px 0;">
                          ${upcomingClassSession.lesson_title || 'Nội dung thực hành theo giáo trình'}
                        </h4>
                        <div class="flex items-center gap-4 text-xs" style="color: #15803D;">
                          <span class="flex items-center gap-1">${icon('clock', { size: 14 })} ${upcomingClassSession.time_slot_name || '18:00 - 19:30'}</span>
                          <span class="flex items-center gap-1">${icon('map-pin', { size: 14 })} ${upcomingClassSession.room_name || 'Phòng 201'}</span>
                          <span class="flex items-center gap-1">${icon('user', { size: 14 })} GV: ${upcomingClassSession.teacher_name || 'Giảng viên'}</span>
                        </div>
                      </div>
                    </div>

                    <a href="#/my-classes" class="btn btn-sm" style="background: #16A34A; color: white;">
                      Chi tiết lớp
                    </a>
                  </div>
                </div>
              ` : `
                <div style="padding: 1.5rem; text-align: center; color: var(--text-muted); background: var(--neutral-50); border-radius: var(--radius-lg);">
                  <p style="margin: 0;">Hiện tại bạn không có buổi học trực tiếp nào trong 7 ngày tới.</p>
                </div>
              `}
            </div>

          </div>

          <!-- Cột Phải: 4 Cột -->
          <div class="col-span-4 flex flex-col gap-6">

            <!-- Card Flashcard SRS Review -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; background: linear-gradient(180deg, #FFFFFF 0%, #FFFBEB 100%); border: 1px solid #FDE68A;">
              <div class="flex items-center justify-between" style="margin-bottom: 1rem;">
                <div class="flex items-center gap-2">
                  <span style="color: #D97706;">${icon('repeat', { size: 20 })}</span>
                  <h3 style="font-size: 1rem; font-weight: 700; color: #78350F; margin: 0;">Ôn tập Flashcards</h3>
                </div>
                <span class="badge badge-warning" style="font-weight: 800;">${dueFlashcardsCount} từ</span>
              </div>

              <p style="font-size: 0.8125rem; color: #92400E; margin-bottom: 1.25rem; line-height: 1.5;">
                Thuật toán Spaced Repetition (SRS) giúp bạn ghi nhớ từ vựng vĩnh viễn bằng cách nhắc lại đúng thời điểm não bộ sắp quên.
              </p>

              <div style="text-align: center; margin-bottom: 1.25rem;">
                <div style="font-size: 2.25rem; font-weight: 800; color: #B45309; line-height: 1;">
                  ${dueFlashcardsCount}
                </div>
                <div style="font-size: 0.75rem; color: #B45309; font-weight: 600; text-transform: uppercase; margin-top: 4px;">
                  Từ cần ôn luyện hôm nay
                </div>
              </div>

              <a href="#/flashcards" class="btn btn-warning w-full" style="justify-content: center; font-weight: 700;">
                <span class="nav-icon">${icon('play', { size: 16 })}</span>
                <span>Bắt đầu phiên ôn tập</span>
              </a>
            </div>

            <!-- Card Đề thi sắp tới -->
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--primary-600);">${icon('award', { size: 20 })}</span>
                  <h3 style="font-size: 1rem; font-weight: 700; margin: 0;">Đề thi & Đánh giá</h3>
                </div>
                <a href="#/exams" class="text-xs font-semibold" style="color: var(--primary-600); text-decoration: none;">Tất cả →</a>
              </div>

              ${upcomingExams.length > 0 ? `
                <div class="flex flex-col gap-3">
                  ${upcomingExams.map(ex => `
                    <div style="padding: 0.875rem; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: var(--neutral-50);">
                      <div class="flex items-center justify-between" style="margin-bottom: 4px;">
                        <span class="badge badge-primary" style="font-size: 0.6875rem;">${ex.duration_minutes} phút</span>
                        <span style="font-size: 0.75rem; color: var(--text-muted);">Đạt: ${ex.pass_score}đ</span>
                      </div>
                      <h5 style="font-size: 0.875rem; font-weight: 700; margin-bottom: 4px; color: var(--text-primary); line-height: 1.3;">
                        ${ex.title}
                      </h5>
                      <div class="flex items-center justify-between" style="margin-top: 8px;">
                        <span style="font-size: 0.75rem; color: var(--text-secondary);">Còn ${ex.attempts_left} lượt thi</span>
                        <a href="#/exams/${ex.id}/take" class="btn btn-outline-primary btn-sm" style="padding: 2px 10px; font-size: 0.75rem;">
                          Vào thi
                        </a>
                      </div>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <div style="padding: 1.25rem; text-align: center; color: var(--text-muted); font-size: 0.8125rem;">
                  Không có bài kiểm tra nào sắp tới.
                </div>
              `}
            </div>

          </div>
        </div>
      </div>
    `;
  }

  static _getGreeting() {
    const hour = new Date().getHours();
    if (hour < 12) return 'Chào buổi sáng';
    if (hour < 18) return 'Chào buổi chiều';
    return 'Chào buổi tối';
  }
}
