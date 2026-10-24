/**
 * ReportsView - Giao diện Báo cáo Thống kê & Biểu đồ SVG
 */

import { icon } from '../../ui/icons.js';
import { renderBarChart, renderDonutChart, renderLineChart } from '../../ui/charts.js';
import { formatCurrency, escapeHtml } from '../../core/utils.js';

export class ReportsView {
  static render({
    stats,
    topCoursesChartData,
    levelDistributionData,
    avgScoreByCourse,
    topTeachers
  }) {
    return `
      <div class="page-header">
        <div>
          <h1 class="page-header-title">Báo cáo & Thống kê học vụ</h1>
          <p class="page-header-subtitle">Đo lường tiến độ hoàn thành bài giảng, điểm số và hiệu quả đào tạo trung tâm</p>
        </div>
      </div>

      <!-- 4 Stat Summary Cards -->
      <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Tỷ lệ hoàn thành bài</span>
            <span class="stat-value">${stats.completionRate}%</span>
            <span class="stat-sub text-success">
              ${icon('trending', { size: 14 })} ${stats.completedLessons} bài hoàn tất
            </span>
          </div>
          <div class="stat-icon primary">${icon('check-circle', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Điểm trung bình hệ thống</span>
            <span class="stat-value">${stats.overallAvgScore} / 100</span>
            <span class="stat-sub text-muted">Từ bài quiz và tiến độ</span>
          </div>
          <div class="stat-icon success">${icon('award', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Học viên hoạt động</span>
            <span class="stat-value">${stats.activeStudentCount}</span>
            <span class="stat-sub text-muted">${stats.totalStudentCount} học viên toàn viện</span>
          </div>
          <div class="stat-icon accent">${icon('users', { size: 24 })}</div>
        </div>

        <div class="stat-card">
          <div class="stat-info">
            <span class="stat-label">Ước tính doanh thu</span>
            <span class="stat-value" style="font-size: 1.5rem;">${formatCurrency(stats.estimatedRevenue)}</span>
            <span class="stat-sub text-muted">tổng price enrollments</span>
          </div>
          <div class="stat-icon warning">${icon('layers', { size: 24 })}</div>
        </div>
      </div>

      <!-- Biểu đồ phân tích -->
      <div class="grid grid-cols-12 gap-6" style="margin-bottom: 2rem;">
        <!-- Cột trái: Top khóa học được ghi danh nhiều nhất -->
        <div class="col-span-8 card" style="padding: 1.5rem;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
            <div>
              <h3 class="card-title">Top khóa học được đăng ký nhiều nhất</h3>
              <div class="card-subtitle">Thống kê số lượng học viên ghi danh theo từng khóa học</div>
            </div>
            <span class="badge badge-primary">Dữ liệu ghi danh</span>
          </div>
          <div>
            ${renderBarChart({ data: topCoursesChartData, height: 240, color: 'var(--primary-600)' })}
          </div>
        </div>

        <!-- Cột phải: Phân bổ khóa học theo cấp độ -->
        <div class="col-span-4 card" style="padding: 1.5rem;">
          <div style="margin-bottom: 1.25rem;">
            <h3 class="card-title">Phân bổ cấp độ</h3>
            <div class="card-subtitle">Tỉ lệ khóa học theo Level</div>
          </div>
          <div>
            ${renderDonutChart({ data: levelDistributionData, size: 170, centerLabel: 'Trình độ' })}
          </div>
        </div>
      </div>

      <!-- 2 Bảng Báo Cáo Chuyên Sâu: Điểm TB Khóa học & Top Giảng Viên -->
      <div class="grid grid-cols-12 gap-6">
        <!-- Điểm trung bình theo khóa (7 cols) -->
        <div class="col-span-7 card">
          <div class="card-header">
            <div>
              <h3 class="card-title">Điểm trung bình theo khóa học</h3>
              <div class="card-subtitle">Kết quả kiểm tra tiến độ của học viên</div>
            </div>
          </div>
          <div class="table-container" style="border: none; border-radius: 0;">
            <table class="table">
              <thead>
                <tr>
                  <th>Khóa học</th>
                  <th>Cấp độ</th>
                  <th>Học phí</th>
                  <th style="text-align: right;">Điểm TB kiểm tra</th>
                </tr>
              </thead>
              <tbody>
                ${avgScoreByCourse.map(c => `
                  <tr>
                    <td><strong>${escapeHtml(c.title)}</strong></td>
                    <td><span class="badge badge-neutral">${c.level}</span></td>
                    <td>${formatCurrency(c.price)}</td>
                    <td style="text-align: right;">
                      <strong class="badge ${c.avg_score >= 80 ? 'badge-success' : c.avg_score >= 65 ? 'badge-info' : 'badge-warning'}">
                        ${c.avg_score} đ
                      </strong>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>

        <!-- Top Giảng Viên (5 cols) -->
        <div class="col-span-5 card">
          <div class="card-header">
            <div>
              <h3 class="card-title">Top giảng viên tiêu biểu</h3>
              <div class="card-subtitle">Theo số lượng lớp học phụ trách</div>
            </div>
            <a href="#/teachers" class="btn btn-secondary btn-sm">Xem tất cả</a>
          </div>
          <div class="card-body" style="padding: 1rem;">
            <div class="flex flex-col gap-3">
              ${topTeachers.map((t, idx) => `
                <div style="display: flex; align-items: center; justify-content: space-between; padding: 0.75rem; border: 1px solid var(--border-color); border-radius: var(--radius-lg); background: var(--bg-surface);">
                  <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <span style="font-weight: 800; color: var(--primary-600); width: 20px;">#${idx + 1}</span>
                    <div class="avatar avatar-sm">
                      <img src="${t.avatar_url || 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150'}" alt="${t.full_name}">
                    </div>
                    <div>
                      <strong style="font-size: 0.875rem;">${escapeHtml(t.full_name)}</strong>
                      <div style="font-size: 0.75rem; color: var(--text-muted);">${escapeHtml(t.qualification)}</div>
                    </div>
                  </div>
                  <span class="badge badge-primary">${t.class_count} lớp</span>
                </div>
              `).join('')}
            </div>
          </div>
        </div>
      </div>
    `;
  }
}
