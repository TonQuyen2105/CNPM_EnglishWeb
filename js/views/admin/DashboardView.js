/**
 * DashboardView - Giao diện Bảng điều khiển Quản trị viên
 */

import { icon } from '../../ui/icons.js';
import { formatCurrency, formatDate, renderStatusBadge } from '../../core/utils.js';
import { renderBarChart, renderDonutChart } from '../../ui/charts.js';

export class DashboardView {
  static render({
    stats,
    monthlyChartData,
    levelChartData,
    recentEnrollments,
    unverifiedTeachers,
    upcomingClasses
  }) {
    return `
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Bảng điều khiển Quản trị</h1>
          <p class="page-header-subtitle">Tổng hợp chỉ số vận hành trung tâm và các hạng mục cần xử lý</p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn btn-secondary" id="btn-refresh-dashboard">
            ${icon('refresh', { size: 16 })}
            <span>Làm mới dữ liệu</span>
          </button>
          <a href="#/classes" class="btn btn-primary">
            ${icon('plus', { size: 16 })}
            <span>Mở lớp học mới</span>
          </a>
        </div>
      </div>

      <!-- 4 Stat Cards -->
      <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Tổng học sinh</span>
            <span class="stat-value">${stats.activeStudents}</span>
            <span class="stat-sub text-success">
              ${icon('trending', { size: 14 })} ${stats.totalStudents} tổng số tài khoản
            </span>
          </div>
          <div class="stat-icon primary">${icon('users', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Đội ngũ giảng viên</span>
            <span class="stat-value">${stats.totalTeachers}</span>
            <span class="stat-sub ${stats.unverifiedCount > 0 ? 'text-warning' : 'text-muted'}">
              ${stats.unverifiedCount > 0 ? `⚠️ ${stats.unverifiedCount} chờ duyệt hồ sơ` : 'Tất cả đã xác minh'}
            </span>
          </div>
          <div class="stat-icon accent">${icon('graduation-cap', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Lớp đang diễn ra</span>
            <span class="stat-value">${stats.ongoingClasses}</span>
            <span class="stat-sub text-muted">${stats.upcomingClassesCount} lớp sắp khai giảng</span>
          </div>
          <div class="stat-icon warning">${icon('book-open', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Doanh thu ước tính</span>
            <span class="stat-value" style="font-size: 1.5rem;">${formatCurrency(stats.estimatedRevenue)}</span>
            <span class="stat-sub text-muted">Ước tính từ ${stats.totalEnrollments} lượt đăng ký</span>
          </div>
          <div class="stat-icon success">${icon('award', { size: 24 })}</div>
        </div>
      </div>

      <!-- 2 Biểu đồ SVG thuần -->
      <div class="grid grid-cols-12 gap-6" style="margin-bottom: 2rem;">
        <!-- Biểu đồ cột: Đăng ký theo tháng -->
        <div class="col-span-8 card" style="padding: 1.5rem;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
            <div>
              <h3 class="card-title">Số lượng học viên đăng ký theo tháng</h3>
              <div class="card-subtitle">Thống kê dữ liệu đăng ký lớp học trong 6 tháng gần nhất</div>
            </div>
            <span class="badge badge-primary">Năm 2026</span>
          </div>
          <div>
            ${renderBarChart({ data: monthlyChartData, height: 230, color: 'var(--primary-600)' })}
          </div>
        </div>

        <!-- Biểu đồ Donut: Phân bố cấp độ khóa học -->
        <div class="col-span-4 card" style="padding: 1.5rem;">
          <div style="margin-bottom: 1.25rem;">
            <h3 class="card-title">Cấp độ đào tạo</h3>
            <div class="card-subtitle">Phân bổ 12 khóa học theo trình độ</div>
          </div>
          <div>
            ${renderDonutChart({ data: levelChartData, size: 170, centerLabel: 'Khóa học' })}
          </div>
        </div>
      </div>

      <!-- Các bảng tác nghiệp và danh sách cảnh báo -->
      <div class="grid grid-cols-12 gap-6">
        <!-- Bảng Đăng ký mới nhất (8 cols) -->
        <div class="col-span-8 card">
          <div class="card-header">
            <div>
              <h3 class="card-title">Lượt đăng ký lớp học mới nhất</h3>
              <div class="card-subtitle">Học viên vừa ghi danh vào các lớp trong hệ thống</div>
            </div>
            <a href="#/enrollments" class="btn btn-secondary btn-sm">Xem tất cả &rarr;</a>
          </div>
          <div class="table-container" style="border: none; border-radius: 0;">
            <table class="table">
              <thead>
                <tr>
                  <th>Học viên</th>
                  <th>Lớp học</th>
                  <th>Ngày đăng ký</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                ${recentEnrollments.length === 0 ? '<tr><td colspan="4" class="text-center p-4">Chưa có lượt đăng ký mới</td></tr>' : ''}
                ${recentEnrollments.map(item => `
                  <tr>
                    <td>
                      <div style="display: flex; align-items: center; gap: 0.75rem;">
                        <div class="avatar avatar-xs">${item.student_name ? item.student_name.charAt(0) : 'U'}</div>
                        <div>
                          <strong>${item.student_name}</strong>
                          <div style="font-size: 0.75rem; color: var(--text-muted);">${item.student_email}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <strong>${item.class_code}</strong>
                      <div style="font-size: 0.75rem; color: var(--text-secondary);">${item.class_name}</div>
                    </td>
                    <td>${formatDate(item.enrolled_at)}</td>
                    <td>${renderStatusBadge(item.status)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>

        <!-- Cột Phải (4 cols): Giáo viên chờ duyệt + Lớp sắp khai giảng -->
        <div class="col-span-4 flex flex-col gap-6">
          <!-- Giáo viên chờ xác minh -->
          <div class="card">
            <div class="card-header">
              <h3 class="card-title" style="display: flex; align-items: center; gap: 6px;">
                ${icon('alert-circle', { size: 16, color: 'var(--warning-main)' })}
                <span>Giáo viên chờ duyệt hồ sơ</span>
              </h3>
            </div>
            <div class="card-body" style="padding: 1rem;">
              ${unverifiedTeachers.length === 0 ? `
                <div style="text-align: center; color: var(--text-muted); font-size: 0.8125rem; padding: 1rem 0;">
                  ${icon('check-circle', { size: 24, color: 'var(--success-main)', className: 'mx-auto' })}
                  <p style="margin-top: 0.5rem;">Tất cả giáo viên đã được xác minh</p>
                </div>
              ` : `
                <div class="flex flex-col gap-3">
                  ${unverifiedTeachers.map(t => `
                    <div style="display: flex; align-items: center; justify-content: space-between; padding: 0.625rem; background: var(--warning-bg); border: 1px solid var(--warning-border); border-radius: var(--radius-lg);">
                      <div style="min-width: 0; flex: 1;">
                        <strong style="font-size: 0.8125rem; display: block;" class="text-truncate">${t.full_name}</strong>
                        <small style="color: var(--text-secondary);">${t.qualification}</small>
                      </div>
                      <button type="button" class="btn btn-primary btn-sm btn-quick-verify" data-id="${t.id}" style="margin-left: 8px; flex-shrink: 0;">
                        Duyệt ngay
                      </button>
                    </div>
                  `).join('')}
                </div>
              `}
            </div>
          </div>

          <!-- Lớp sắp khai giảng -->
          <div class="card">
            <div class="card-header">
              <h3 class="card-title">Lớp sắp khai giảng</h3>
              <a href="#/classes" style="font-size: 0.75rem;">Chi tiết</a>
            </div>
            <div class="card-body" style="padding: 1rem;">
              ${upcomingClasses.length === 0 ? `
                <div style="text-align: center; color: var(--text-muted); font-size: 0.8125rem; padding: 1rem 0;">
                  Không có lớp sắp khai giảng
                </div>
              ` : `
                <div class="flex flex-col gap-3">
                  ${upcomingClasses.map(cls => `
                    <div style="padding: 0.75rem; border: 1px solid var(--border-color); border-radius: var(--radius-lg); background: var(--neutral-50);">
                      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">
                        <strong style="color: var(--primary-600); font-size: 0.8125rem;">${cls.class_code}</strong>
                        <span class="badge badge-info">${cls.enrolled_count}/${cls.max_students} học viên</span>
                      </div>
                      <div style="font-size: 0.8125rem; font-weight: 600; margin-bottom: 2px;">${cls.class_name}</div>
                      <div style="font-size: 0.75rem; color: var(--text-muted);">
                        Khai giảng: ${formatDate(cls.start_date)}
                      </div>
                    </div>
                  `).join('')}
                </div>
              `}
            </div>
          </div>
        </div>
      </div>
    `;
  }
}
