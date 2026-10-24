/**
 * StudentProgressView - Giao diện Tiến độ học tập, Biểu đồ SVG & Huy hiệu thành tích
 */

import { icon } from '../../ui/icons.js';

export class StudentProgressView {
  static render({
    streakDays = 5,
    courseProgressList = [],
    scoreHistory = [],
    badges = [],
    overallStats = {}
  }) {
    return `
      <div class="student-progress-page">
        <!-- Header -->
        <div class="page-header" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Tiến độ học tập & Thành tích</h1>
            <p class="page-header-subtitle">
              Theo dõi biểu đồ điểm số, mức độ hoàn thành các khóa học và bộ sưu tập huy hiệu của bạn.
            </p>
          </div>
        </div>

        <!-- Banner Chuỗi Học Liên Tục (Streak) & Thống Kê Tổng Quan -->
        <div class="student-hero-banner" style="margin-bottom: 2rem;">
          <div class="flex items-center justify-between flex-wrap gap-4">
            <div class="flex items-center gap-4">
              <div style="font-size: 3.5rem; line-height: 1;">🔥</div>
              <div>
                <div style="font-size: 1.75rem; font-weight: 800; color: #92400E; margin-bottom: 4px;">
                  Chuỗi học liên tục ${streakDays} ngày
                </div>
                <p style="color: #B45309; font-size: 0.9375rem; margin: 0; line-height: 1.4;">
                  Bạn đang giữ vững phong độ rất tốt! Học liên tục mỗi ngày giúp tăng 70% khả năng phản xạ tự nhiên.
                </p>
              </div>
            </div>

            <div class="flex items-center gap-6" style="background: rgba(255,255,255,0.7); padding: 1rem 1.5rem; border-radius: var(--radius-xl); border: 1px solid rgba(253, 230, 138, 0.8);">
              <div>
                <div style="font-size: 0.75rem; color: var(--text-muted); font-weight: 700; text-transform: uppercase;">Bài đã học</div>
                <div style="font-size: 1.5rem; font-weight: 800; color: var(--primary-700);">${overallStats.completedLessons || 0} bài</div>
              </div>
              <div>
                <div style="font-size: 0.75rem; color: var(--text-muted); font-weight: 700; text-transform: uppercase;">Điểm TB</div>
                <div style="font-size: 1.5rem; font-weight: 800; color: #16A34A;">${overallStats.avgScore || 0}/100</div>
              </div>
              <div>
                <div style="font-size: 0.75rem; color: var(--text-muted); font-weight: 700; text-transform: uppercase;">Huy hiệu</div>
                <div style="font-size: 1.5rem; font-weight: 800; color: #D97706;">${badges.filter(b => b.isUnlocked).length} / ${badges.length}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 2 Cột: Biểu đồ SVG Điểm số & Tỷ lệ hoàn thành khóa học -->
        <div class="grid grid-cols-12 gap-6" style="margin-bottom: 2rem;">
          
          <!-- Cột Trái (7 cột): Biểu đồ SVG Điểm số theo thời gian -->
          <div class="col-span-7">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color); height: 100%; display: flex; flex-direction: column; justify-content: space-between;">
              <div>
                <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                  <div class="flex items-center gap-2">
                    <span style="color: var(--primary-600);">${icon('trending', { size: 22 })}</span>
                    <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Biểu đồ điểm số theo thời gian</h3>
                  </div>
                  <span class="badge badge-info">Điểm các bài thi</span>
                </div>

                <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 1.5rem;">
                  Đồ thị thể hiện kết quả kiểm tra đánh giá của bạn qua từng mốc thời gian (thang điểm 100).
                </p>

                <!-- SVG Bar/Line Chart thuần -->
                ${this._renderSvgScoreChart(scoreHistory)}
              </div>
            </div>
          </div>

          <!-- Cột Phải (5 cột): Tỷ lệ hoàn thành các khóa học -->
          <div class="col-span-5">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color); height: 100%;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <div class="flex items-center gap-2">
                  <span style="color: var(--primary-600);">${icon('layers', { size: 22 })}</span>
                  <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Tiến độ các khóa học</h3>
                </div>
              </div>

              ${courseProgressList.length > 0 ? `
                <div class="flex flex-col gap-4">
                  ${courseProgressList.map(cp => `
                    <div style="background: var(--neutral-50); padding: 1rem 1.25rem; border-radius: var(--radius-lg); border: 1px solid var(--border-color);">
                      <div class="flex items-center justify-between" style="margin-bottom: 6px;">
                        <span style="font-weight: 700; font-size: 0.875rem; color: var(--text-primary);">${cp.title}</span>
                        <strong style="color: var(--primary-700); font-size: 0.875rem;">${cp.progress_percent}%</strong>
                      </div>
                      <div style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 8px;">
                        Đã học ${cp.completed_lessons} / ${cp.total_lessons} bài
                      </div>
                      <div class="progress" style="height: 8px; border-radius: var(--radius-full); background: var(--neutral-200);">
                        <div class="progress-bar" style="width: ${cp.progress_percent}%; background: ${cp.progress_percent === 100 ? '#16A34A' : 'var(--primary-600)'}; border-radius: var(--radius-full);"></div>
                      </div>
                    </div>
                  `).join('')}
                </div>
              ` : `
                <p style="color: var(--text-muted); text-align: center; margin: 2rem 0;">Bạn chưa đăng ký khóa học nào.</p>
              `}
            </div>
          </div>

        </div>

        <!-- Bộ Sưu Tập Huy Hiệu Thành Tích -->
        <div class="card" style="border-radius: var(--radius-xl); padding: 1.75rem; border: 1px solid var(--border-color);">
          <div class="flex items-center justify-between" style="margin-bottom: 1.5rem;">
            <div class="flex items-center gap-2">
              <span style="color: #D97706;">${icon('award', { size: 24 })}</span>
              <h3 style="font-size: 1.125rem; font-weight: 800; margin: 0; color: var(--text-primary);">
                Bộ sưu tập Huy hiệu thành tích
              </h3>
            </div>
            <span class="badge badge-warning" style="font-weight: 800;">
              ${badges.filter(b => b.isUnlocked).length} / ${badges.length} đã mở khóa
            </span>
          </div>

          <div class="grid grid-cols-4 gap-6">
            ${badges.map(b => `
              <div style="padding: 1.5rem; border-radius: var(--radius-xl); border: 1px solid ${b.isUnlocked ? '#FDE68A' : 'var(--border-color)'}; background: ${b.isUnlocked ? 'linear-gradient(180deg, #FFFFFF, #FFFBEB)' : 'var(--neutral-50)'}; opacity: ${b.isUnlocked ? '1' : '0.65'}; display: flex; flex-direction: column; align-items: center; text-align: center;">
                <div style="width: 64px; height: 64px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 2rem; margin-bottom: 1rem; background: ${b.isUnlocked ? '#FEF3C7' : '#E2E8F0'}; box-shadow: ${b.isUnlocked ? '0 4px 12px rgba(245,158,11,0.2)' : 'none'};">
                  ${b.icon}
                </div>

                <h4 style="font-size: 0.9375rem; font-weight: 800; color: var(--text-primary); margin-bottom: 4px;">
                  ${b.title}
                </h4>
                <p style="font-size: 0.75rem; color: var(--text-secondary); margin-bottom: 1rem; line-height: 1.4;">
                  ${b.description}
                </p>

                <span class="badge ${b.isUnlocked ? 'badge-success' : 'badge-secondary'}" style="font-size: 0.6875rem; margin-top: auto;">
                  ${b.isUnlocked ? '✓ Đã mở khóa' : '🔒 Chưa đạt'}
                </span>
              </div>
            `).join('')}
          </div>
        </div>
      </div>
    `;
  }

  static _renderSvgScoreChart(scores = []) {
    if (!scores || scores.length === 0) {
      scores = [
        { label: 'Bài 1', score: 65 },
        { label: 'Bài 2', score: 75 },
        { label: 'Bài 3', score: 70 },
        { label: 'Bài 4', score: 85 },
        { label: 'Bài 5', score: 90 }
      ];
    }

    const svgWidth = 560;
    const svgHeight = 220;
    const paddingLeft = 40;
    const paddingBottom = 30;
    const chartWidth = svgWidth - paddingLeft - 20;
    const chartHeight = svgHeight - paddingBottom - 20;

    const stepX = chartWidth / Math.max(1, scores.length - 1);

    const points = scores.map((item, idx) => {
      const x = paddingLeft + idx * stepX;
      const y = svgHeight - paddingBottom - (item.score / 100) * chartHeight;
      return { x, y, score: item.score, label: item.label };
    });

    const pathD = points.length > 1
      ? points.reduce((acc, pt, idx) => (idx === 0 ? `M ${pt.x} ${pt.y}` : `${acc} L ${pt.x} ${pt.y}`), '')
      : `M 40 100 L 520 100`;

    const areaD = points.length > 1
      ? `${pathD} L ${points[points.length - 1].x} ${svgHeight - paddingBottom} L ${points[0].x} ${svgHeight - paddingBottom} Z`
      : '';

    return `
      <div style="width: 100%; overflow-x: auto;">
        <svg viewBox="0 0 ${svgWidth} ${svgHeight}" style="width: 100%; height: auto; max-height: 240px; display: block;">
          <defs>
            <linearGradient id="scoreAreaGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="#2563EB" stop-opacity="0.25"/>
              <stop offset="100%" stop-color="#2563EB" stop-opacity="0.0"/>
            </linearGradient>
          </defs>

          <!-- Trục Y (0, 25, 50, 75, 100) -->
          ${[0, 25, 50, 75, 100].map(val => {
            const y = svgHeight - paddingBottom - (val / 100) * chartHeight;
            return `
              <line x1="${paddingLeft}" y1="${y}" x2="${svgWidth - 20}" y2="${y}" stroke="#E2E8F0" stroke-dasharray="4 4" />
              <text x="${paddingLeft - 8}" y="${y + 4}" fill="#94A3B8" font-size="10" text-anchor="end">${val}</text>
            `;
          }).join('')}

          <!-- Vùng diện tích Gradient -->
          ${areaD ? `<path d="${areaD}" fill="url(#scoreAreaGrad)" />` : ''}

          <!-- Đường nối điểm -->
          <path d="${pathD}" fill="none" stroke="#2563EB" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />

          <!-- Các điểm chấm tròn và nhãn trục X -->
          ${points.map(pt => `
            <circle cx="${pt.x}" cy="${pt.y}" r="5" fill="#2563EB" stroke="#FFFFFF" stroke-width="2" />
            <text x="${pt.x}" y="${pt.y - 10}" fill="#1E293B" font-size="11" font-weight="bold" text-anchor="middle">${pt.score}đ</text>
            <text x="${pt.x}" y="${svgHeight - 10}" fill="#64748B" font-size="10" text-anchor="middle">${pt.label}</text>
          `).join('')}
        </svg>
      </div>
    `;
  }
}
